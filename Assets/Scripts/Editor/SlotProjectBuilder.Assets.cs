using System.Collections.Generic;
using System.IO;
using System.Linq;
using SlotGame.Audio;
using SlotGame.Data;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SlotGame.Editor
{
    public static partial class SlotProjectBuilder
    {
        /// <summary>Fonts and TextMesh Pro material presets used by the UI.</summary>
        internal sealed class FontSet
        {
            public TMP_FontAsset Font;
            public Material Outline;   // white/gold text with a dark outline + drop shadow
            public Material Glow;      // big celebratory titles
        }

        /// <summary>Data assets and sprites the scene builder wires together.</summary>
        internal sealed class GameData
        {
            public SymbolDatabase Symbols;
            public PayTable PayTable;
            public ReelStripSet Strips;
            public SlotMachineConfig Config;
            public ReelMotionProfile Motion;
            public SoundLibrary Sounds;
            public Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        }

        // ---------------------------------------------------------------- TextMesh Pro

        private static void ImportTextMeshProEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null)
            {
                return;
            }

            // ImportPackage is asynchronous, so fonts cannot be created in this same pass.
            string package = Path.GetFullPath("Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(package, false);
            throw new System.InvalidOperationException(
                "TextMesh Pro Essential Resources were missing and are being imported. Run the builder again.");
        }

        // ---------------------------------------------------------------- Sprites

        private static void ConfigureSprites()
        {
            // Symbols: keep crisp and uncompressed so every symbol renders identically.
            foreach (string path in FindTextures(SymbolArtFolder))
            {
                ConfigureSingleSprite(path, Vector4.zero, uncompressed: true, maxSize: 256);
            }

            foreach (string path in FindTextures(MachineArtFolder))
            {
                ConfigureSingleSprite(path, Vector4.zero, uncompressed: true, maxSize: 1024);
            }

            ConfigureSingleSprite("Assets/Art/Backgrounds/bg_gradient.png", Vector4.zero, uncompressed: true, maxSize: 1024);

            ConfigureSingleSprite($"{EffectsArtFolder}/coin.png", Vector4.zero, true, 256);
            ConfigureSingleSprite($"{EffectsArtFolder}/soft-glow.png", Vector4.zero, true, 256);
            ConfigureSingleSprite($"{EffectsArtFolder}/bulb.png", Vector4.zero, true, 128);
            ConfigureSingleSprite($"{EffectsArtFolder}/rays.png", Vector4.zero, false, 512);
            ConfigureSingleSprite($"{EffectsArtFolder}/sparkle.png", Vector4.zero, true, 128);
            ConfigureSingleSprite($"{EffectsArtFolder}/payline.png", new Vector4(24, 0, 24, 0), true, 256);
            ConfigureSingleSprite($"{EffectsArtFolder}/rounded-rect.png", new Vector4(20, 20, 20, 20), true, 64);
            ConfigureSingleSprite($"{EffectsArtFolder}/glow-frame.png", new Vector4(40, 40, 40, 40), true, 128);

            // UI panels are 9-sliced so they scale to any size without stretching the corners.
            ConfigureSingleSprite($"{UiSpriteFolder}/popup.png", new Vector4(140, 140, 140, 140), true, 2048);
            ConfigureSingleSprite($"{UiSpriteFolder}/slot_machine_Middle_box.png", new Vector4(80, 70, 80, 70), true, 1024);

            // Button sheets: four states stacked vertically (normal, highlighted, pressed, disabled).
            string[] states = { "Normal", "Highlighted", "Pressed", "Disabled" };
            SliceVertical($"{UiSpriteFolder}/slot_machine_buttons-02.png", "Close", states, 256);
            SliceVertical($"{UiSpriteFolder}/slot_machine_buttons-03.png", "ArrowRight", states, 256);
            SliceVertical($"{UiSpriteFolder}/slot_machine_buttons-04.png", "ArrowLeft", states, 256);

            // Yes/No sheet: two columns x three rows (normal, highlighted, pressed).
            SliceYesNo($"{UiSpriteFolder}/Yes_No_Btn.png");
        }

        private static IEnumerable<string> FindTextures(string folder)
        {
            return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath);
        }

        private static TextureImporter PrepareImporter(string path, bool uncompressed, int maxSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = uncompressed
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.CompressedHQ;
            importer.spritePixelsPerUnit = 100;
            return importer;
        }

        private static void ConfigureSingleSprite(string path, Vector4 border, bool uncompressed, int maxSize)
        {
            TextureImporter importer = PrepareImporter(path, uncompressed, maxSize);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static void SliceVertical(string path, string prefix, string[] states, int cellHeight)
        {
            TextureImporter importer = PrepareImporter(path, true, 1024);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            int textureHeight = 1024;
            var rects = new List<SpriteRect>();
            for (int i = 0; i < states.Length; i++)
            {
                // Texture space is bottom-up; the first state is the top cell.
                rects.Add(NewSpriteRect($"{prefix}_{states[i]}",
                    new Rect(0, textureHeight - cellHeight * (i + 1), 256, cellHeight)));
            }

            ApplySpriteRects(importer, rects);
        }

        private static void SliceYesNo(string path)
        {
            TextureImporter importer = PrepareImporter(path, true, 1024);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            const int textureHeight = 690;
            string[] states = { "Normal", "Highlighted", "Pressed" };
            int[] rowTops = { 73, 264, 455 };
            const int rowHeight = 162;

            var rects = new List<SpriteRect>();
            for (int i = 0; i < states.Length; i++)
            {
                float y = textureHeight - rowTops[i] - rowHeight;
                rects.Add(NewSpriteRect($"Yes_{states[i]}", new Rect(60, y, 390, rowHeight)));
                rects.Add(NewSpriteRect($"No_{states[i]}", new Rect(538, y, 390, rowHeight)));
            }

            ApplySpriteRects(importer, rects);
        }

        private static SpriteRect NewSpriteRect(string name, Rect rect)
        {
            return new SpriteRect
            {
                name = name,
                rect = rect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            };
        }

        private static void ApplySpriteRects(TextureImporter importer, List<SpriteRect> rects)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Keep sprite ids stable across rebuilds so existing references do not break.
            Dictionary<string, SpriteRect> existing = provider.GetSpriteRects().ToDictionary(r => r.name);
            foreach (SpriteRect rect in rects)
            {
                if (existing.TryGetValue(rect.name, out SpriteRect previous))
                {
                    rect.spriteID = previous.spriteID;
                }
            }

            provider.SetSpriteRects(rects.ToArray());

            var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

            provider.Apply();
            importer.SaveAndReimport();
        }

        internal static Sprite LoadSprite(string path, string subSpriteName = null)
        {
            if (subSpriteName == null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    throw new FileNotFoundException($"Sprite not found at {path}");
                }

                return sprite;
            }

            Sprite sub = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == subSpriteName);
            if (sub == null)
            {
                throw new FileNotFoundException($"Sprite '{subSpriteName}' not found in {path}");
            }

            return sub;
        }

        // ---------------------------------------------------------------- Audio

        private static void ConfigureAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SoundFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- Fonts

        private static FontSet CreateFonts()
        {
            string fontPath = $"{FontFolder}/LilitaOne-Regular.ttf";
            string assetPath = $"{FontFolder}/LilitaOne SDF.asset";

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (font == null)
            {
                var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                font = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic);
                font.name = "LilitaOne SDF";

                AssetDatabase.CreateAsset(font, assetPath);
                font.atlasTextures[0].name = "LilitaOne SDF Atlas";
                AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
                font.material.name = "LilitaOne SDF Material";
                AssetDatabase.AddObjectToAsset(font.material, font);

                // Pre-bake every glyph the game prints so nothing is rasterised mid-spin.
                font.TryAddCharacters(" ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?+-=x×#%()/·'\"…");
                EditorUtility.SetDirty(font);
                AssetDatabase.SaveAssets();
            }

            // Ship a STATIC atlas: exactly the glyphs baked above, no runtime rasterisation.
            if (font.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                font.TryAddCharacters(" ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?+-=x×#%()/·'\"…");
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(font);
                AssetDatabase.SaveAssets();
            }

            var outline = new Material(font.material) { name = "LilitaOne SDF - Outline" };
            outline.EnableKeyword(ShaderUtilities.Keyword_Outline);
            outline.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
            outline.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.16f, 0.06f, 0.22f, 1f));
            outline.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            outline.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.6f));
            outline.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            outline.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            outline.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);
            outline = CreateOrReplaceAsset(outline, $"{FontFolder}/LilitaOne SDF - Outline.mat");

            var glow = new Material(font.material) { name = "LilitaOne SDF - Glow" };
            glow.EnableKeyword(ShaderUtilities.Keyword_Outline);
            glow.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            glow.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.45f, 0.12f, 0.02f, 1f));
            glow.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            glow.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(1f, 0.75f, 0.1f, 0.65f));
            glow.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.6f);
            glow.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.9f);
            glow = CreateOrReplaceAsset(glow, $"{FontFolder}/LilitaOne SDF - Glow.mat");

            return new FontSet { Font = font, Outline = outline, Glow = glow };
        }

        /// <summary>
        /// Re-reads the font and its materials from disk. Asset imports between creating the fonts and
        /// building the scene can invalidate the in-memory references (they turn into "fake null"
        /// objects), and TextMesh Pro then silently falls back to its default font.
        /// </summary>
        private static FontSet ReloadFonts()
        {
            var fonts = new FontSet
            {
                Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontFolder}/LilitaOne SDF.asset"),
                Outline = AssetDatabase.LoadAssetAtPath<Material>($"{FontFolder}/LilitaOne SDF - Outline.mat"),
                Glow = AssetDatabase.LoadAssetAtPath<Material>($"{FontFolder}/LilitaOne SDF - Glow.mat"),
            };

            if (fonts.Font == null || fonts.Outline == null || fonts.Glow == null)
            {
                throw new System.InvalidOperationException("LilitaOne font assets are missing; run Rebuild Project Assets again.");
            }

            Debug.Log($"[SlotProjectBuilder] Font: {fonts.Font.name} ({fonts.Font.atlasPopulationMode}, {fonts.Font.characterTable.Count} characters).");
            return fonts;
        }

        // ---------------------------------------------------------------- Game data

        private static GameData CreateGameData()
        {
            var data = new GameData();
            EnsureFolder($"{DataFolder}/Symbols");

            // Symbol ids are stable numbers the maths uses; names/sprites are presentation.
            SymbolDefinition seven = CreateSymbol(1, "SEVEN", "7", "slot-symbol1", new Color(1f, 0.25f, 0.2f));
            SymbolDefinition cherry = CreateSymbol(2, "CHERRY", "CHERRY", "slot-symbol2", new Color(1f, 0.3f, 0.45f));
            SymbolDefinition bell = CreateSymbol(3, "BELL", "BELL", "slot-symbol3", new Color(1f, 0.85f, 0.2f));
            SymbolDefinition bar = CreateSymbol(4, "BAR", "BAR", "slot-symbol4", new Color(0.4f, 0.8f, 1f));

            var database = ScriptableObject.CreateInstance<SymbolDatabase>();
            database.EditorSetSymbols(new[] { seven, cherry, bell, bar });
            data.Symbols = CreateOrReplaceAsset(database, $"{DataFolder}/SymbolDatabase.asset");

            // Pay table. 7 is wild; three 7s is the jackpot. Three bells also award free spins.
            var payTable = ScriptableObject.CreateInstance<PayTable>();
            payTable.EditorInitialize(new[]
            {
                new PayTableEntry(seven, 100, 0),
                new PayTableEntry(bar, 10, 0),
                new PayTableEntry(bell, 5, 3),
                new PayTableEntry(cherry, 2, 0),
            }, seven, true);
            data.PayTable = CreateOrReplaceAsset(payTable, $"{DataFolder}/PayTable.asset");

            // Reel strips (20 stops each). Composition sets the odds; the third reel carries one
            // seven fewer, the classic way to make the top prize rarer. Exact RTP for these
            // strips is 95.56% with a 20.3% hit rate (see PayTableMath / the RTP Simulator).
            var map = new Dictionary<string, SymbolDefinition>
            {
                { "7", seven }, { "BR", bar }, { "BL", bell }, { "C", cherry },
            };
            var strips = ScriptableObject.CreateInstance<ReelStripSet>();
            strips.EditorSetReels(new[]
            {
                Strip(map, "C BL C BR C BL 7 C BL BR C BL C BR BL C 7 BL C BR"),
                Strip(map, "BL C BR C 7 BL C BL C BR BL C C BR BL 7 C BL BR C"),
                Strip(map, "C BL C BR C BL 7 C BL BR C BL C BR BL C C BL C BR"),
            });
            data.Strips = CreateOrReplaceAsset(strips, $"{DataFolder}/ReelStripSet.asset");

            data.Config = LoadOrCreate<SlotMachineConfig>($"{DataFolder}/SlotMachineConfig.asset");
            data.Motion = LoadOrCreate<ReelMotionProfile>($"{DataFolder}/ReelMotionProfile.asset");

            var sounds = ScriptableObject.CreateInstance<SoundLibrary>();
            sounds.EditorSetSounds(new[]
            {
                Sound(SoundId.ButtonClick, "button_click", 0.6f, 0.05f),
                Sound(SoundId.LeverPull, "lever_pull", 0.8f, 0.03f),
                Sound(SoundId.ReelSpinLoop, "reel_spin_loop", 0.35f, 0f),
                Sound(SoundId.ReelStop, "reel_stop", 0.9f, 0.04f),
                Sound(SoundId.Anticipation, "anticipation_loop", 0.45f, 0f),
                Sound(SoundId.WinSmall, "win_small", 0.8f, 0f),
                Sound(SoundId.WinBig, "win_big", 0.9f, 0f),
                Sound(SoundId.Jackpot, "jackpot", 1f, 0f),
                Sound(SoundId.CoinTick, "coin_tick", 0.5f, 0.12f),
                Sound(SoundId.FreeSpins, "free_spins", 0.9f, 0f),
                Sound(SoundId.NoCredits, "no_credits", 0.7f, 0f),
                Sound(SoundId.BetChange, "bet_change", 0.6f, 0.02f),
            });
            data.Sounds = CreateOrReplaceAsset(sounds, $"{DataFolder}/SoundLibrary.asset");

            return data;
        }

        private static SymbolDefinition CreateSymbol(int id, string assetName, string displayName, string art, Color accent)
        {
            Sprite sharp = LoadSprite($"{SymbolArtFolder}/{art}.png");
            Sprite blurred = LoadSprite($"{SymbolArtFolder}/Blurred/{art}-blur.png");
            var symbol = ScriptableObject.CreateInstance<SymbolDefinition>();
            symbol.EditorInitialize(id, displayName, sharp, blurred, accent);
            return CreateOrReplaceAsset(symbol, $"{DataFolder}/Symbols/{assetName}.asset");
        }

        private static ReelStrip Strip(Dictionary<string, SymbolDefinition> map, string layout)
        {
            return new ReelStrip(layout.Split(' ').Select(code => map[code]));
        }

        private static SoundEntry Sound(SoundId id, string file, float volume, float pitchVariance)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{file}.wav");
            if (clip == null)
            {
                throw new FileNotFoundException($"Missing sound {file}.wav");
            }

            return new SoundEntry(id, clip, volume, pitchVariance);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
