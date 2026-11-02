using SlotGame.Audio;
using SlotGame.Gameplay;
using SlotGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SlotGame.Editor.EditorBindings;
using static SlotGame.Editor.UiFactory;

namespace SlotGame.Editor
{
    public static partial class SlotProjectBuilder
    {
        // Reference resolution. The machine art is 816x624 and is scaled to fill the screen height.
        private const float CanvasWidth = 1920f;
        private const float CanvasHeight = 1080f;
        private const float MachineScale = CanvasHeight / 624f;
        private const float MachineX = -330f;
        private const float HudX = 630f;

        // Reel windows measured from slot-machine5.png (machine-local units, origin at centre).
        private static readonly float[] ReelWindowX = { -125.5f, 4.5f, 134.5f };
        private const float ReelWindowY = -38.5f;
        private const float ReelWindowWidth = 108f;
        private const float ReelWindowHeight = 210f;
        private const float SymbolSpacing = 84f;
        private const float SymbolIconSize = 78f;

        private static readonly Color Gold = new Color(1f, 0.84f, 0.29f);
        private static readonly Color Cream = new Color(1f, 0.97f, 0.88f);

        private static void BuildScene(GameData data, FontSet fonts, AnimationSet animations)
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(PrefabFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();
            Canvas canvas = CreateCanvas();

            Image background = CreateImage("Background", canvas.transform,
                LoadSprite("Assets/Art/Backgrounds/bg_gradient.png"), Color.white, 0, 0, 0, 0);
            background.rectTransform.Stretch();

            // Living backdrop: rotating rays behind the cabinet plus drifting bokeh orbs.
            Image rays = CreateImage("Rays", canvas.transform, LoadSprite($"{EffectsArtFolder}/rays.png"),
                new Color(1f, 0.85f, 0.5f, 0.3f), MachineX, 160, 2600, 2600);
            RectTransform orbLayer = Create("Bokeh", canvas.transform).Rect().Stretch();

            // ---- Systems (non-visual) ----
            var systems = new GameObject("Systems");
            var controller = systems.AddComponent<SlotMachineController>();
            var audioManager = systems.AddComponent<AudioManager>();
            var audioPresenter = systems.AddComponent<SlotAudioPresenter>();

            // ---- Prefabs that other prefabs reference ----
            ReelSymbol symbolPrefab = CreateReelSymbolPrefab();
            PaytableRow rowPrefab = CreatePaytableRowPrefab(fonts);

            // ---- Machine ----
            MachineParts machine = BuildMachine(canvas.transform, data, fonts, animations, symbolPrefab, controller);

            // ---- HUD ----
            HudParts hud = BuildHud(canvas.transform, fonts, animations);

            // ---- Popups ----
            Transform popupRoot = Create("Popups", canvas.transform).Rect().Stretch();
            PaytablePanel paytable = BuildPaytablePanel(popupRoot, fonts, animations, rowPrefab);
            ConfirmDialog confirm = BuildConfirmDialog(popupRoot, fonts, animations);
            WinPopup winPopup = BuildWinPopup(popupRoot, fonts, animations);
            IntroScreen intro = BuildIntroScreen(popupRoot, fonts);

            // ---- Wiring ----
            Set(controller, "symbolDatabase", data.Symbols);
            Set(controller, "payTable", data.PayTable);
            Set(controller, "reelStrips", data.Strips);
            Set(controller, "config", data.Config);
            SetArray(controller, "reels", machine.Reels);

            Set(audioManager, "library", data.Sounds);
            Set(audioPresenter, "controller", controller);
            Set(audioPresenter, "audioManager", audioManager);

            var lights = systems.AddComponent<MachineLights>();
            Set(lights, "controller", controller);
            Set(lights, "lightsAnimator", machine.LightsAnimator);
            Set(lights, "paylineGroup", machine.Payline);

            Set(machine.Lever, "controller", controller);

            var backgroundFx = systems.AddComponent<BackgroundFx>();
            Set(backgroundFx, "controller", controller);
            Set(backgroundFx, "rays", rays.rectTransform);
            Set(backgroundFx, "raysImage", rays);
            Set(backgroundFx, "orbLayer", orbLayer);
            Set(backgroundFx, "orbSprite", LoadSprite($"{EffectsArtFolder}/soft-glow.png"));

            Set(machine.WinBanner, "controller", controller);

            var marquee = systems.AddComponent<MarqueeLights>();
            Set(marquee, "controller", controller);
            SetArray(marquee, "bulbs", machine.MarqueeBulbs);

            var shake = systems.AddComponent<ScreenShake>();
            Set(shake, "controller", controller);
            SetArray(shake, "targets", machine.Root.GetComponent<RectTransform>(), hud.Root.GetComponent<RectTransform>());

            var sparkles = machine.SparkleArea.gameObject.AddComponent<SparkleBurst>();
            Set(sparkles, "controller", controller);
            Set(sparkles, "area", machine.SparkleArea);
            Set(sparkles, "sparkleSprite", LoadSprite($"{EffectsArtFolder}/sparkle.png"));

            var ui = systems.AddComponent<UIManager>();
            Set(ui, "controller", controller);
            Set(ui, "audioManager", audioManager);
            Set(ui, "creditsCounter", hud.Credits);
            Set(ui, "winCounter", hud.Win);
            Set(ui, "betLabel", hud.BetLabel);
            Set(ui, "messageLabel", machine.Message);
            Set(ui, "freeSpinsBanner", machine.FreeSpinsBanner);
            Set(ui, "freeSpinsLabel", machine.FreeSpinsLabel);
            Set(ui, "sessionInfoLabel", hud.SessionInfo);
            Set(ui, "spinButton", hud.Spin);
            Set(ui, "spinButtonLabel", hud.SpinLabel);
            Set(ui, "betUpButton", hud.BetUp);
            Set(ui, "betDownButton", hud.BetDown);
            Set(ui, "autoSpinButton", hud.Auto);
            Set(ui, "maxBetButton", hud.MaxBet);
            Set(ui, "introScreen", intro);
            Set(ui, "autoSpinLabel", hud.AutoLabel);
            Set(ui, "paytableButton", hud.Paytable);
            Set(ui, "soundButton", hud.Sound);
            Set(ui, "soundLabel", hud.SoundLabel);
            Set(ui, "spinButtonAnimator", hud.SpinAnimator);
            Set(ui, "winPopup", winPopup);
            Set(ui, "paytablePanel", paytable);
            Set(ui, "confirmDialog", confirm);

            // Save reusable pieces as prefabs (popups and the whole machine).
            SavePrefab(paytable.gameObject, "PaytablePanel");
            SavePrefab(confirm.gameObject, "ConfirmDialog");
            SavePrefab(winPopup.gameObject, "WinPopup");
            SavePrefab(machine.Root, "SlotMachine");

            // Guard: every label must use the game font (a wrong font is the #1 "looks unfinished" bug).
            int wrongFont = 0;
            foreach (TMP_Text label in Object.FindObjectsOfType<TMP_Text>(true))
            {
                if (label.font != fonts.Font)
                {
                    wrongFont++;
                }
            }

            Debug.Log($"[SlotProjectBuilder] Labels with the wrong font: {wrongFont}.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ================================================================ Scene basics

        private static void CreateCamera()
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.05f, 0.16f);
            camera.orthographic = true;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas") { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, $"{PrefabFolder}/{name}.prefab", InteractionMode.AutomatedAction);
        }

        // ================================================================ Reel symbol & rows

        private static ReelSymbol CreateReelSymbolPrefab()
        {
            GameObject root = Create("ReelSymbol", null);
            root.Rect().Place(0, 0, ReelWindowWidth, SymbolSpacing);

            Image glow = CreateImage("Glow", root.transform, LoadSprite($"{EffectsArtFolder}/soft-glow.png"),
                new Color(1f, 0.85f, 0.3f, 0f), 0, 0, 130, 130);
            glow.enabled = false;

            Image icon = CreateImage("Icon", root.transform, LoadSprite($"{SymbolArtFolder}/slot-symbol1.png"),
                Color.white, 0, 0, SymbolIconSize, SymbolIconSize);
            icon.preserveAspect = true;

            var symbol = root.AddComponent<ReelSymbol>();
            Set(symbol, "icon", icon);
            Set(symbol, "glow", glow);
            Set(symbol, "iconSize", new Vector2(SymbolIconSize, SymbolIconSize));
            Set(symbol, "winPulseScale", 1.14f);

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/ReelSymbol.prefab");
            Object.DestroyImmediate(root);
            return asset.GetComponent<ReelSymbol>();
        }

        private static PaytableRow CreatePaytableRowPrefab(FontSet fonts)
        {
            GameObject root = Create("PaytableRow", null);
            root.Rect().Place(0, 0, 960, 96);

            Image background = AddImage(root, LoadSprite($"{EffectsArtFolder}/rounded-rect.png"),
                new Color(0.1f, 0.05f, 0.25f, 0.55f), false, Image.Type.Sliced, 2f);
            background.raycastTarget = false;

            var icons = new Image[3];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = CreateImage($"Icon {i + 1}", root.transform, LoadSprite($"{SymbolArtFolder}/slot-symbol1.png"),
                    Color.white, -400 + i * 84, 0, 76, 76);
                icons[i].preserveAspect = true;
            }

            TextMeshProUGUI multiplier = CreateText("Multiplier", root.transform, "x100 = 1,000", fonts.Font, fonts.Outline,
                40, Cream, 40, 0, 360, 80, TextAlignmentOptions.Left);
            TextMeshProUGUI note = CreateText("Note", root.transform, "WILD", fonts.Font, fonts.Outline,
                28, Gold, 330, 0, 280, 80, TextAlignmentOptions.Right);

            var row = root.AddComponent<PaytableRow>();
            SetArray(row, "icons", icons);
            Set(row, "multiplierLabel", multiplier);
            Set(row, "noteLabel", note);

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/PaytableRow.prefab");
            Object.DestroyImmediate(root);
            return asset.GetComponent<PaytableRow>();
        }

        // ================================================================ Machine

        private sealed class MachineParts
        {
            public GameObject Root;
            public Reel[] Reels;
            public CanvasGroup Payline;
            public Animator LightsAnimator;
            public LeverController Lever;
            public TextMeshProUGUI Message;
            public GameObject FreeSpinsBanner;
            public TextMeshProUGUI FreeSpinsLabel;
            public Image[] MarqueeBulbs;
            public WinBanner WinBanner;
            public RectTransform SparkleArea;
        }

        private static MachineParts BuildMachine(Transform canvas, GameData data, FontSet fonts, AnimationSet animations,
            ReelSymbol symbolPrefab, SlotMachineController controller)
        {
            var parts = new MachineParts();
            GameObject root = Create("SlotMachine", canvas);
            root.Rect().Place(MachineX, 0, 816, 624);
            root.transform.localScale = new Vector3(MachineScale, MachineScale, 1f);
            parts.Root = root;

            // Soft light behind the cabinet that blooms during celebrations.
            Image cabinetGlow = CreateImage("CabinetGlow", root.transform, LoadSprite($"{EffectsArtFolder}/soft-glow.png"),
                new Color(1f, 0.78f, 0.25f, 0f), 30, 0, 900, 760);

            // Layer order matters: reel backgrounds < symbols < cabinet frame < lever & effects.
            CreateImage("ReelBackground", root.transform, LoadSprite($"{MachineArtFolder}/slot-machine5.png"),
                Color.white, 0, 0, 816, 624);

            Transform reelsRoot = Create("Reels", root.transform).Rect().Stretch();
            parts.Reels = new Reel[ReelWindowX.Length];
            for (int i = 0; i < ReelWindowX.Length; i++)
            {
                parts.Reels[i] = BuildReel(reelsRoot, i, data, symbolPrefab);
            }

            // Pay line: faint at rest, flashes on wins (drawn over the symbols, under the frame).
            Image payline = CreateImage("Payline", root.transform, LoadSprite($"{EffectsArtFolder}/payline.png"),
                new Color(1f, 0.85f, 0.25f, 1f), 4.5f, ReelWindowY, 400, 12, Image.Type.Sliced, 2f);
            parts.Payline = payline.gameObject.AddComponent<CanvasGroup>();
            parts.Payline.alpha = 0.35f;
            parts.Payline.blocksRaycasts = false;

            CreateImage("Frame", root.transform, LoadSprite($"{MachineArtFolder}/slot-machine4.png"),
                Color.white, 0, 0, 816, 624);

            parts.MarqueeBulbs = BuildMarquee(root.transform);

            // Sparkles twinkle over the reel windows on wins (above the frame, below the lever).
            GameObject sparkles = Create("Sparkles", root.transform);
            parts.SparkleArea = sparkles.Rect().Place(ReelWindowX[1], ReelWindowY, 400, 230);

            // Small-win banner that slams in over the reels.
            GameObject winBannerGo = Create("WinBanner", root.transform);
            winBannerGo.Rect().Place(ReelWindowX[1], ReelWindowY + 10, 420, 200);
            winBannerGo.AddComponent<CanvasGroup>();
            CreateImage("Glow", winBannerGo.transform, LoadSprite($"{EffectsArtFolder}/soft-glow.png"),
                new Color(0.25f, 0.05f, 0.35f, 0.85f), 0, 0, 460, 230);
            TextMeshProUGUI winTitle = CreateText("Title", winBannerGo.transform, "WIN!", fonts.Font, fonts.Glow, 72, Gold, 0, 28, 420, 90);
            TextMeshProUGUI winAmount = CreateText("Amount", winBannerGo.transform, "+20", fonts.Font, fonts.Outline, 44, Cream, 0, -40, 420, 60);
            parts.WinBanner = winBannerGo.AddComponent<WinBanner>();
            Set(parts.WinBanner, "titleLabel", winTitle);
            Set(parts.WinBanner, "amountLabel", winAmount);

            // Suspense glow per reel window, above the frame so it reads clearly.
            for (int i = 0; i < parts.Reels.Length; i++)
            {
                Image glow = CreateImage($"Anticipation {i + 1}", root.transform, LoadSprite($"{EffectsArtFolder}/glow-frame.png"),
                    new Color(1f, 0.82f, 0.2f, 1f), ReelWindowX[i], ReelWindowY, ReelWindowWidth + 16, ReelWindowHeight + 16,
                    Image.Type.Sliced, 2.5f);
                glow.enabled = false;
                Set(parts.Reels[i], "anticipationGlow", glow);
            }

            Image titleGlow = CreateImage("TitleGlow", root.transform, LoadSprite($"{EffectsArtFolder}/soft-glow.png"),
                new Color(1f, 0.9f, 0.4f, 0f), 4, 205, 480, 190);

            // Lights animator animates TitleGlow / CabinetGlow by path.
            parts.LightsAnimator = root.AddComponent<Animator>();
            parts.LightsAnimator.runtimeAnimatorController = animations.MachineLights;

            // Lever: full-size layer that sprite-swaps; a separate hit area keeps clicks precise.
            GameObject leverGo = Create("Lever", root.transform);
            leverGo.Rect().Place(0, 0, 816, 624);
            AddImage(leverGo, LoadSprite($"{MachineArtFolder}/slot-machine2.png"), Color.white);
            var leverAnimator = leverGo.AddComponent<Animator>();
            leverAnimator.runtimeAnimatorController = animations.Lever;

            GameObject leverHit = Create("LeverHitArea", root.transform);
            leverHit.Rect().Place(311, -110, 104, 300);
            AddImage(leverHit, null, new Color(1f, 1f, 1f, 0f), true);

            parts.Lever = leverHit.AddComponent<LeverController>();
            Set(parts.Lever, "animator", leverAnimator);

            // Message strip on the cabinet's lower panel.
            parts.Message = CreateText("Message", root.transform, "PRESS SPIN", fonts.Font, fonts.Outline,
                24, Cream, 4, -284, 440, 34);

            // FREE SPINS banner above the reels.
            GameObject banner = Create("FreeSpinsBanner", root.transform);
            banner.Rect().Place(4, 96, 250, 38);
            AddImage(banner, LoadSprite($"{EffectsArtFolder}/rounded-rect.png"), new Color(0.55f, 0.1f, 0.75f, 0.95f),
                false, Image.Type.Sliced, 3f);
            parts.FreeSpinsLabel = CreateText("Label", banner.transform, "FREE SPINS: 3", fonts.Font, fonts.Outline,
                24, Gold, 0, 0, 240, 36);
            banner.AddComponent<Animator>().runtimeAnimatorController = animations.BannerPulse;
            banner.SetActive(false);
            parts.FreeSpinsBanner = banner;

            // Keep the unused reference warning-free: the glow is driven purely by the animator.
            _ = cabinetGlow;
            _ = titleGlow;
            return parts;
        }

        /// <summary>
        /// Bulbs around the cabinet outline, ordered as one continuous loop (left pillar upwards,
        /// over the JACKPOT arch, right pillar downwards) so a chase reads as travelling light.
        /// Positions are traced from slot-machine4.png in machine-local units.
        /// </summary>
        private static Image[] BuildMarquee(Transform machine)
        {
            Transform root = Create("Marquee", machine).Rect().Stretch();
            Sprite bulb = LoadSprite($"{EffectsArtFolder}/bulb.png");
            var points = new System.Collections.Generic.List<Vector2>();

            const float pillarX = 264f;      // |x| of the side pillars
            const float pillarBottom = -290f;
            const float pillarTop = 128f;
            const int pillarBulbs = 12;
            for (int i = 0; i < pillarBulbs; i++)
            {
                points.Add(new Vector2(-pillarX, Mathf.Lerp(pillarBottom, pillarTop, i / (pillarBulbs - 1f))));
            }

            // Arch over the JACKPOT sign: half-ellipse from the left pillar top to the right one.
            const int archBulbs = 21;
            const float archCentreY = 147f;
            const float archRadiusX = 258f;
            const float archRadiusY = 150f;
            for (int i = 1; i < archBulbs - 1; i++)
            {
                float angle = Mathf.PI * (1f - i / (archBulbs - 1f));
                points.Add(new Vector2(4f + Mathf.Cos(angle) * archRadiusX, archCentreY + Mathf.Sin(angle) * archRadiusY));
            }

            for (int i = pillarBulbs - 1; i >= 0; i--)
            {
                points.Add(new Vector2(pillarX + 8f, Mathf.Lerp(pillarBottom, pillarTop, i / (pillarBulbs - 1f))));
            }

            var bulbs = new Image[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                bulbs[i] = CreateImage($"Bulb {i + 1:00}", root, bulb, new Color(1f, 0.93f, 0.62f), points[i].x, points[i].y, 36, 36);
            }

            return bulbs;
        }

        private static Reel BuildReel(Transform parent, int index, GameData data, ReelSymbol symbolPrefab)
        {
            GameObject reelGo = Create($"Reel {index + 1}", parent);
            reelGo.Rect().Place(ReelWindowX[index], ReelWindowY, ReelWindowWidth, ReelWindowHeight);

            // The viewport clips the scrolling symbols to the window.
            GameObject viewport = Create("Viewport", reelGo.transform);
            viewport.Rect().Stretch();
            viewport.AddComponent<RectMask2D>();

            GameObject container = Create("Symbols", viewport.transform);
            container.Rect().Stretch();

            var reel = reelGo.AddComponent<Reel>();
            Set(reel, "symbolContainer", container.transform);
            Set(reel, "symbolPrefab", symbolPrefab);
            Set(reel, "symbolSpacing", SymbolSpacing);
            Set(reel, "cellCount", 5);
            Set(reel, "motion", data.Motion);

            return reel;
        }

        // ================================================================ HUD

        private sealed class HudParts
        {
            public AnimatedCounter Credits;
            public AnimatedCounter Win;
            public TextMeshProUGUI BetLabel;
            public TextMeshProUGUI SessionInfo;
            public Button Spin;
            public TextMeshProUGUI SpinLabel;
            public Animator SpinAnimator;
            public Button BetUp;
            public Button BetDown;
            public Button Auto;
            public TextMeshProUGUI AutoLabel;
            public Button MaxBet;
            public GameObject Root;
            public Button Paytable;
            public Button Sound;
            public TextMeshProUGUI SoundLabel;
        }

        private static HudParts BuildHud(Transform canvas, FontSet fonts, AnimationSet animations)
        {
            var hud = new HudParts();
            GameObject root = Create("HUD", canvas);
            root.Rect().Place(HudX, 0, 520, 1080);

            Sprite box = LoadSprite($"{UiSpriteFolder}/slot_machine_Middle_box.png");
            string sheetRight = $"{UiSpriteFolder}/slot_machine_buttons-03.png";
            string sheetLeft = $"{UiSpriteFolder}/slot_machine_buttons-04.png";

            hud.Credits = CreateMeter(root.transform, "Credits", "CREDITS", 0, 395, box, fonts, out _);
            TextMeshProUGUI betValue;
            CreateMeter(root.transform, "Bet", "BET", 0, 205, box, fonts, out betValue);
            hud.BetLabel = betValue;
            hud.Win = CreateMeter(root.transform, "Win", "WIN", 0, 15, box, fonts, out _);

            hud.BetDown = ArrowButton(root.transform, "BetDown", -248, 205, sheetLeft, "ArrowLeft");
            hud.BetUp = ArrowButton(root.transform, "BetUp", 248, 205, sheetRight, "ArrowRight");

            // SPIN: the main call to action, the cabinet display box at its native aspect.
            GameObject spin = Create("SpinButton", root.transform);
            spin.Rect().Place(0, -200, 470, 198);
            Image spinImage = AddImage(spin, box, new Color(1f, 1f, 1f), true);
            hud.Spin = TintButton(spin, spinImage);
            hud.SpinLabel = CreateText("Label", spin.transform, "SPIN", fonts.Font, fonts.Glow, 76, Gold, 0, 2, 440, 150);
            hud.SpinLabel.lineSpacing = -30;
            hud.SpinAnimator = spin.AddComponent<Animator>();
            hud.SpinAnimator.runtimeAnimatorController = animations.SpinButton;

            // Bottom: a 2x2 grid of secondary buttons.
            hud.MaxBet = SmallButton(root.transform, "MaxBetButton", "MAX BET", -128, -345, box, fonts, out _);
            hud.Auto = SmallButton(root.transform, "AutoButton", "AUTO", 128, -345, box, fonts, out hud.AutoLabel);
            hud.Paytable = SmallButton(root.transform, "PaytableButton", "PAYS", -128, -448, box, fonts, out _);
            hud.Sound = SmallButton(root.transform, "SoundButton", "SOUND", 128, -448, box, fonts, out hud.SoundLabel);

            // Juicy press feedback on every HUD button.
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                button.gameObject.AddComponent<ButtonPunch>();
            }

            hud.Root = root;

            hud.SessionInfo = CreateText("SessionInfo", root.transform, "SECURE RNG", fonts.Font, fonts.Outline,
                20, new Color(1f, 1f, 1f, 0.55f), 0, -518, 480, 30);
            return hud;
        }

        private static AnimatedCounter CreateMeter(Transform parent, string name, string caption, float x, float y,
            Sprite box, FontSet fonts, out TextMeshProUGUI value)
        {
            GameObject meter = Create($"{name}Meter", parent);
            // The cabinet's display box at its native aspect (658x277), so the art is never distorted.
            meter.Rect().Place(x, y, 440, 185);
            AddImage(meter, box, Color.white);

            CreateText("Caption", meter.transform, caption, fonts.Font, fonts.Outline, 30, Gold, 0, 36, 300, 40);
            value = CreateText("Value", meter.transform, "0", fonts.Font, fonts.Outline, 60, Cream, 0, -16, 340, 70);

            var counter = meter.AddComponent<AnimatedCounter>();
            Set(counter, "label", value);
            return counter;
        }

        private static Button ArrowButton(Transform parent, string name, float x, float y, string sheet, string prefix)
        {
            GameObject go = Create(name, parent);
            go.Rect().Place(x, y, 92, 92);
            return SpriteSwapButton(go,
                LoadSprite(sheet, $"{prefix}_Normal"),
                LoadSprite(sheet, $"{prefix}_Highlighted"),
                LoadSprite(sheet, $"{prefix}_Pressed"),
                LoadSprite(sheet, $"{prefix}_Disabled"));
        }

        private static Button SmallButton(Transform parent, string name, string text, float x, float y, Sprite box,
            FontSet fonts, out TextMeshProUGUI label)
        {
            GameObject go = Create(name, parent);
            go.Rect().Place(x, y, 252, 124);
            Image image = AddImage(go, box, Color.white, true, Image.Type.Sliced, 2f);
            Button button = TintButton(go, image);
            label = CreateText("Label", go.transform, text, fonts.Font, fonts.Outline, 34, Cream, 0, 0, 200, 80);
            return button;
        }

        // ================================================================ Popups

        private static GameObject PopupRoot(Transform parent, string name, AnimationSet animations, out Image dim)
        {
            GameObject root = Create(name, parent);
            root.Rect().Stretch();
            root.AddComponent<CanvasGroup>();
            dim = AddImage(root, null, new Color(0.03f, 0.01f, 0.08f, 0.72f), true);
            root.AddComponent<Animator>().runtimeAnimatorController = animations.Popup;
            return root;
        }

        private static GameObject PopupPanel(Transform root, float width, float height)
        {
            GameObject panel = Create("Panel", root);
            panel.Rect().Place(0, 0, width, height);
            AddImage(panel, LoadSprite($"{UiSpriteFolder}/popup.png"), Color.white, true, Image.Type.Sliced, 1.2f);
            return panel;
        }

        private static Button CloseButton(Transform panel, float x, float y)
        {
            string sheet = $"{UiSpriteFolder}/slot_machine_buttons-02.png";
            GameObject go = Create("CloseButton", panel);
            go.Rect().Place(x, y, 96, 96);
            return SpriteSwapButton(go,
                LoadSprite(sheet, "Close_Normal"),
                LoadSprite(sheet, "Close_Highlighted"),
                LoadSprite(sheet, "Close_Pressed"),
                LoadSprite(sheet, "Close_Disabled"));
        }

        private static PaytablePanel BuildPaytablePanel(Transform parent, FontSet fonts, AnimationSet animations, PaytableRow rowPrefab)
        {
            GameObject root = PopupRoot(parent, "PaytablePanel", animations, out _);
            GameObject panel = PopupPanel(root.transform, 1180, 820);

            CreateText("Title", panel.transform, "PAYTABLE", fonts.Font, fonts.Glow, 72, Gold, 0, 320, 600, 90);
            CreateText("Subtitle", panel.transform, "3 matching symbols on the centre line", fonts.Font, fonts.Outline,
                28, Cream, 0, 258, 900, 40);

            GameObject rows = Create("Rows", panel.transform);
            rows.Rect().Place(0, 10, 960, 430);
            var layout = rows.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            TextMeshProUGUI footer = CreateText("Footer", panel.transform, string.Empty, fonts.Font, fonts.Outline,
                24, new Color(1f, 1f, 1f, 0.85f), 0, -300, 1000, 80);
            footer.enableWordWrapping = true;

            Button close = CloseButton(panel.transform, 520, 340);

            var paytable = root.AddComponent<PaytablePanel>();
            Set(paytable, "animator", root.GetComponent<Animator>());
            Set(paytable, "rowPrefab", rowPrefab);
            Set(paytable, "rowContainer", rows.transform);
            Set(paytable, "footerLabel", footer);
            Set(paytable, "closeButton", close);
            return paytable;
        }

        private static ConfirmDialog BuildConfirmDialog(Transform parent, FontSet fonts, AnimationSet animations)
        {
            GameObject root = PopupRoot(parent, "ConfirmDialog", animations, out _);
            GameObject panel = PopupPanel(root.transform, 920, 500);

            TextMeshProUGUI title = CreateText("Title", panel.transform, "OUT OF CREDITS", fonts.Font, fonts.Glow,
                64, Gold, 0, 150, 800, 80);
            TextMeshProUGUI message = CreateText("Message", panel.transform, "Top up?", fonts.Font, fonts.Outline,
                34, Cream, 0, 40, 780, 100);
            message.enableWordWrapping = true;

            string sheet = $"{UiSpriteFolder}/Yes_No_Btn.png";
            GameObject yes = Create("YesButton", panel.transform);
            yes.Rect().Place(-190, -130, 300, 125);
            Button yesButton = SpriteSwapButton(yes, LoadSprite(sheet, "Yes_Normal"), LoadSprite(sheet, "Yes_Highlighted"),
                LoadSprite(sheet, "Yes_Pressed"), null);

            GameObject no = Create("NoButton", panel.transform);
            no.Rect().Place(190, -130, 300, 125);
            Button noButton = SpriteSwapButton(no, LoadSprite(sheet, "No_Normal"), LoadSprite(sheet, "No_Highlighted"),
                LoadSprite(sheet, "No_Pressed"), null);

            var dialog = root.AddComponent<ConfirmDialog>();
            Set(dialog, "animator", root.GetComponent<Animator>());
            Set(dialog, "titleLabel", title);
            Set(dialog, "messageLabel", message);
            Set(dialog, "yesButton", yesButton);
            Set(dialog, "noButton", noButton);
            return dialog;
        }

        private static IntroScreen BuildIntroScreen(Transform parent, FontSet fonts)
        {
            GameObject root = Create("IntroScreen", parent);
            root.Rect().Stretch();
            root.AddComponent<CanvasGroup>();
            Image dim = AddImage(root, null, new Color(0.04f, 0.01f, 0.1f, 0.9f), true);
            Button tap = root.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.targetGraphic = dim;

            CreateImage("Glow", root.transform, LoadSprite($"{EffectsArtFolder}/soft-glow.png"),
                new Color(1f, 0.75f, 0.25f, 0.45f), 0, 150, 1300, 620);

            GameObject title = Create("Title", root.transform);
            RectTransform titleRect = title.Rect().Place(0, 200, 1200, 260);
            CreateText("Lucky7", title.transform, "LUCKY 7", fonts.Font, fonts.Glow, 190, Gold, 0, 30, 1200, 220);
            CreateText("Slots", title.transform, "S L O T S", fonts.Font, fonts.Outline, 58, Cream, 0, -105, 900, 70);

            string[] symbolFiles = { "slot-symbol1.png", "slot-symbol2.png", "slot-symbol3.png", "slot-symbol4.png" };
            for (int i = 0; i < symbolFiles.Length; i++)
            {
                Image icon = CreateImage($"Symbol {i + 1}", root.transform, LoadSprite($"{SymbolArtFolder}/{symbolFiles[i]}"),
                    Color.white, -255 + i * 170, -60, 130, 130);
                icon.preserveAspect = true;
            }

            CreateText("Rules", root.transform, "MATCH 3 TO WIN   ·   7 IS WILD   ·   3 BELLS = FREE SPINS",
                fonts.Font, fonts.Outline, 36, Cream, 0, -200, 1500, 60);
            TextMeshProUGUI prompt = CreateText("Prompt", root.transform, "TAP TO PLAY", fonts.Font, fonts.Glow,
                78, Gold, 0, -320, 900, 110);
            CreateText("Hint", root.transform, "SPACE = SPIN   ·   TAP SPIN AGAIN TO QUICK-STOP", fonts.Font, fonts.Outline,
                26, new Color(1f, 1f, 1f, 0.6f), 0, -420, 1200, 40);

            var intro = root.AddComponent<IntroScreen>();
            Set(intro, "tapArea", tap);
            Set(intro, "promptLabel", prompt);
            Set(intro, "title", titleRect);
            return intro;
        }

        private static WinPopup BuildWinPopup(Transform parent, FontSet fonts, AnimationSet animations)
        {
            GameObject root = PopupRoot(parent, "WinPopup", animations, out Image dim);

            // Tapping anywhere collects the win.
            Button collect = root.AddComponent<Button>();
            collect.targetGraphic = dim;
            collect.transition = Selectable.Transition.None;

            GameObject panel = PopupPanel(root.transform, 1200, 780);

            Image rays = CreateImage("Rays", panel.transform, LoadSprite($"{EffectsArtFolder}/soft-glow.png"),
                new Color(1f, 0.8f, 0.3f, 0.5f), 0, 40, 1400, 1000);
            rays.transform.SetAsFirstSibling();

            TextMeshProUGUI title = CreateText("Title", panel.transform, "BIG WIN!", fonts.Font, fonts.Glow,
                112, Gold, 0, 222, 900, 130);

            var icons = new Image[3];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = CreateImage($"Symbol {i + 1}", panel.transform, LoadSprite($"{SymbolArtFolder}/slot-symbol1.png"),
                    Color.white, -140 + i * 140, 86, 120, 120);
                icons[i].preserveAspect = true;
            }

            TextMeshProUGUI amount = CreateText("Amount", panel.transform, "0", fonts.Font, fonts.Glow,
                110, Cream, 0, -56, 900, 130);
            var amountCounter = amount.gameObject.AddComponent<AnimatedCounter>();
            Set(amountCounter, "label", amount);
            Set(amountCounter, "landingPunch", 1.2f);

            TextMeshProUGUI detail = CreateText("Detail", panel.transform, "x10", fonts.Font, fonts.Outline,
                34, Gold, 0, -160, 900, 50);
            CreateText("Hint", panel.transform, "TAP TO COLLECT", fonts.Font, fonts.Outline,
                24, new Color(1f, 1f, 1f, 0.7f), 0, -228, 600, 34);

            Button close = CloseButton(panel.transform, 520, 300);

            GameObject burstGo = Create("CoinBurst", root.transform);
            burstGo.Rect().Place(0, -150, 10, 10);
            var burst = burstGo.AddComponent<CoinBurst>();
            Set(burst, "coinSprite", LoadSprite($"{EffectsArtFolder}/coin.png"));

            var popup = root.AddComponent<WinPopup>();
            Set(popup, "animator", root.GetComponent<Animator>());
            Set(popup, "titleLabel", title);
            Set(popup, "detailLabel", detail);
            Set(popup, "amountCounter", amountCounter);
            SetArray(popup, "symbolIcons", icons);
            Set(popup, "closeButton", close);
            Set(popup, "collectArea", collect);
            Set(popup, "coinBurst", burst);
            return popup;
        }
    }
}
