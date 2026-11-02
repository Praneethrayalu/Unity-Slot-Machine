using SlotGame.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Living backdrop: slowly rotating light rays behind the cabinet and drifting bokeh orbs.
    /// Reacts to the game: rays speed up while the reels spin and flare on wins. Pure UI Images
    /// animated in code, so it stays cheap on WebGL and mobile.
    /// </summary>
    public sealed class BackgroundFx : MonoBehaviour
    {
        private struct Orb
        {
            public RectTransform Rect;
            public Image Image;
            public float Speed;
            public float SwayPhase;
            public float SwayAmount;
            public float BaseAlpha;
            public float X;
        }

        [SerializeField] private SlotMachineController controller;

        [Header("Rays")]
        [SerializeField] private RectTransform rays;
        [SerializeField] private Image raysImage;
        [SerializeField] private float idleDegreesPerSecond = 5f;
        [SerializeField] private float spinDegreesPerSecond = 28f;
        [SerializeField] private float raysAlpha = 0.3f;

        [Header("Bokeh")]
        [SerializeField] private RectTransform orbLayer;
        [SerializeField] private Sprite orbSprite;
        [SerializeField] private int orbCount = 28;
        [SerializeField] private Vector2 orbSizeRange = new Vector2(24f, 150f);
        [SerializeField] private Vector2 orbSpeedRange = new Vector2(14f, 60f);

        private static readonly Color[] OrbColors =
        {
            new Color(1f, 0.8f, 0.35f),
            new Color(1f, 0.45f, 0.8f),
            new Color(0.55f, 0.6f, 1f),
            new Color(1f, 0.95f, 0.8f),
        };

        private Orb[] _orbs;
        private float _rotationSpeed;
        private float _targetSpeed;
        private float _flare;

        private void Awake()
        {
            _rotationSpeed = _targetSpeed = idleDegreesPerSecond;
            Rect area = orbLayer.rect;
            _orbs = new Orb[orbCount];
            for (int i = 0; i < orbCount; i++)
            {
                var go = new GameObject($"Orb {i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(orbLayer, false);
                var image = go.GetComponent<Image>();
                image.sprite = orbSprite;
                image.raycastTarget = false;
                float size = Random.Range(orbSizeRange.x, orbSizeRange.y);
                var rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(size, size);

                // Big orbs are faint and slow (far away); small ones brighter and quicker.
                float depth = Mathf.InverseLerp(orbSizeRange.y, orbSizeRange.x, size);
                Color color = OrbColors[i % OrbColors.Length];
                _orbs[i] = new Orb
                {
                    Rect = rect,
                    Image = image,
                    Speed = Mathf.Lerp(orbSpeedRange.x, orbSpeedRange.y, depth),
                    SwayPhase = Random.value * Mathf.PI * 2f,
                    SwayAmount = Random.Range(10f, 40f),
                    BaseAlpha = Mathf.Lerp(0.12f, 0.42f, depth),
                    X = Random.Range(area.xMin, area.xMax),
                };
                image.color = new Color(color.r, color.g, color.b, 0f);
                rect.anchoredPosition = new Vector2(_orbs[i].X, Random.Range(area.yMin, area.yMax));
            }
        }

        private void OnEnable()
        {
            controller.SpinStarted += OnSpinStarted;
            controller.SpinCompleted += OnSpinCompleted;
        }

        private void OnDisable()
        {
            controller.SpinStarted -= OnSpinStarted;
            controller.SpinCompleted -= OnSpinCompleted;
        }

        private void OnSpinStarted(SpinResult spin)
        {
            _targetSpeed = spinDegreesPerSecond;
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            _targetSpeed = idleDegreesPerSecond;
            if (tier != WinTier.None)
            {
                _flare = tier >= WinTier.Mega ? 1f : tier == WinTier.Big ? 0.7f : 0.4f;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Ease the rotation speed so the rays "spool up" with the reels.
            _rotationSpeed = Mathf.Lerp(_rotationSpeed, _targetSpeed, 1f - Mathf.Exp(-3f * dt));
            rays.Rotate(0f, 0f, -_rotationSpeed * dt);

            _flare = Mathf.Max(0f, _flare - dt * 0.45f);
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 1.3f);
            Color rc = raysImage.color;
            rc.a = raysAlpha * pulse + 0.35f * _flare;
            raysImage.color = rc;

            Rect area = orbLayer.rect;
            float boost = 1f + 2.5f * _flare;
            for (int i = 0; i < _orbs.Length; i++)
            {
                Orb orb = _orbs[i];
                Vector2 pos = orb.Rect.anchoredPosition;
                pos.y += orb.Speed * boost * dt;
                if (pos.y > area.yMax + 80f)
                {
                    pos.y = area.yMin - 80f;
                    orb.X = Random.Range(area.xMin, area.xMax);
                }

                pos.x = orb.X + Mathf.Sin(Time.time * 0.6f + orb.SwayPhase) * orb.SwayAmount;
                orb.Rect.anchoredPosition = pos;

                Color c = orb.Image.color;
                c.a = Mathf.Min(1f, orb.BaseAlpha * (1f + 1.5f * _flare) * (0.75f + 0.25f * Mathf.Sin(Time.time * 2f + orb.SwayPhase)));
                orb.Image.color = c;
                _orbs[i] = orb;
            }
        }
    }
}
