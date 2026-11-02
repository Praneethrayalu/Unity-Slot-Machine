using System.Collections;
using SlotGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// One visible cell on a reel. Owns the symbol's image and its visual states:
    /// motion blur while spinning, a pulsing glow when part of a win, and dimming when not.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ReelSymbol : MonoBehaviour
    {
        [SerializeField] private Image icon;

        [Tooltip("Soft glow drawn behind the icon during a win.")]
        [SerializeField] private Image glow;

        [Tooltip("Size of the icon inside the cell, so every symbol is aligned and scaled identically.")]
        [SerializeField] private Vector2 iconSize = new Vector2(150f, 150f);

        [SerializeField] private float winPulseScale = 1.18f;
        [SerializeField] private float winPulseSpeed = 5f;

        private static readonly Color DimmedColor = new Color(0.45f, 0.45f, 0.5f, 1f);

        private RectTransform _rectTransform;
        private SymbolDefinition _symbol;
        private Coroutine _highlightRoutine;
        private bool _blurred;

        /// <summary>The reel-space index this cell currently represents (managed by <see cref="Reel"/>).</summary>
        public int VirtualIndex { get; set; } = int.MinValue;

        public SymbolDefinition Symbol => _symbol;

        public RectTransform RectTransform => _rectTransform != null ? _rectTransform : (_rectTransform = (RectTransform)transform);

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            if (glow != null)
            {
                glow.enabled = false;
            }

            icon.rectTransform.sizeDelta = iconSize;
            icon.preserveAspect = true;
        }

        /// <summary>Displays <paramref name="symbol"/>, keeping the current blur state.</summary>
        public void Show(SymbolDefinition symbol)
        {
            _symbol = symbol;
            ApplySprite();
        }

        /// <summary>
        /// Applies speed-based motion effects: blurred sprite above the threshold and a vertical
        /// stretch proportional to speed, which together read as fast motion at 60 fps.
        /// </summary>
        public void SetMotion(bool blurred, float verticalStretch)
        {
            if (_blurred != blurred)
            {
                _blurred = blurred;
                ApplySprite();
            }

            icon.rectTransform.localScale = new Vector3(1f, verticalStretch, 1f);
        }

        public void PlayWinHighlight()
        {
            StopHighlight();
            SetDimmed(false);
            _highlightRoutine = StartCoroutine(WinPulseRoutine());
        }

        public void StopHighlight()
        {
            if (_highlightRoutine != null)
            {
                StopCoroutine(_highlightRoutine);
                _highlightRoutine = null;
            }

            RectTransform.localScale = Vector3.one;
            if (glow != null)
            {
                glow.enabled = false;
            }
        }

        public void SetDimmed(bool dimmed)
        {
            icon.color = dimmed ? DimmedColor : Color.white;
        }

        private void ApplySprite()
        {
            if (_symbol == null)
            {
                icon.enabled = false;
                return;
            }

            icon.enabled = true;
            icon.sprite = _blurred ? _symbol.BlurredSprite : _symbol.Sprite;
        }

        private IEnumerator WinPulseRoutine()
        {
            if (glow != null)
            {
                glow.enabled = true;
                Color accent = _symbol != null ? _symbol.AccentColor : Color.yellow;
                glow.color = new Color(accent.r, accent.g, accent.b, 0f);
            }

            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;

                // Smooth 0..1 pulse; the scale "breathes" and the glow brightens in sync.
                float wave = 0.5f - 0.5f * Mathf.Cos(time * winPulseSpeed);
                float scale = Mathf.Lerp(1f, winPulseScale, wave);
                RectTransform.localScale = new Vector3(scale, scale, 1f);

                if (glow != null)
                {
                    Color c = glow.color;
                    c.a = Mathf.Lerp(0.35f, 0.95f, wave);
                    glow.color = c;
                }

                yield return null;
            }
        }
    }
}
