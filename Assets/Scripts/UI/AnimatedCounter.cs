using System;
using System.Collections;
using SlotGame.Utilities;
using TMPro;
using UnityEngine;

namespace SlotGame.UI
{
    /// <summary>
    /// Numeric label that counts up/down to a target with an ease-out curve and a small "punch"
    /// scale when it lands. Used for credits, win amount and the win popup.
    /// </summary>
    public sealed class AnimatedCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string prefix = string.Empty;
        [SerializeField] private string numberFormat = "N0";

        [Tooltip("Scale applied briefly when a count finishes (1 = none).")]
        [SerializeField] private float landingPunch = 1.15f;

        [Tooltip("Minimum seconds between Ticked events (drives the coin tick sound).")]
        [SerializeField] private float tickInterval = 0.06f;

        private long _displayed;
        private Coroutine _routine;
        private Vector3 _baseScale = Vector3.one;

        /// <summary>Raised periodically while counting, at most once per <see cref="tickInterval"/>.</summary>
        public event Action Ticked;

        public long DisplayedValue => _displayed;

        public bool IsAnimating => _routine != null;

        private void Awake()
        {
            _baseScale = label.rectTransform.localScale;
        }

        public void SetInstant(long value)
        {
            StopRunning();
            _displayed = value;
            Render();
        }

        /// <summary>Counts from the displayed value to <paramref name="target"/> over <paramref name="duration"/> seconds.</summary>
        public void AnimateTo(long target, float duration)
        {
            StopRunning();
            if (duration <= 0f || !isActiveAndEnabled)
            {
                SetInstant(target);
                return;
            }

            _routine = StartCoroutine(CountRoutine(_displayed, target, duration));
        }

        /// <summary>Jumps straight to the end of a running count.</summary>
        public void Complete(long target)
        {
            SetInstant(target);
        }

        private IEnumerator CountRoutine(long from, long to, float duration)
        {
            float lastTick = -1f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float progress = Easing.OutCubic(t / duration);
                long value = from + (long)Math.Round((to - from) * (double)progress);
                if (value != _displayed)
                {
                    _displayed = value;
                    Render();

                    if (t - lastTick >= tickInterval)
                    {
                        lastTick = t;
                        Ticked?.Invoke();
                    }
                }

                yield return null;
            }

            _displayed = to;
            Render();
            yield return Punch();
            _routine = null;
        }

        private IEnumerator Punch()
        {
            const float duration = 0.25f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(Mathf.PI * (t / duration));
                label.rectTransform.localScale = _baseScale * Mathf.Lerp(1f, landingPunch, k);
                yield return null;
            }

            label.rectTransform.localScale = _baseScale;
        }

        private void StopRunning()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (label != null)
            {
                label.rectTransform.localScale = _baseScale;
            }
        }

        private void Render()
        {
            label.text = prefix + _displayed.ToString(numberFormat);
        }
    }
}
