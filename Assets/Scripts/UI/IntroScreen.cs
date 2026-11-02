using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Title card shown on launch: the game's name, the rules in one line and "TAP TO PLAY".
    /// Besides presentation it has a practical job on WebGL: browsers only start audio after a
    /// user gesture, so the first tap here unlocks sound before the first spin.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class IntroScreen : MonoBehaviour
    {
        [SerializeField] private Button tapArea;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private RectTransform title;
        [SerializeField] private float fadeDuration = 0.35f;

        private CanvasGroup _group;
        private float _fade = -1f;

        /// <summary>Raised once, when the player dismisses the intro.</summary>
        public event Action Dismissed;

        public bool IsOpen { get; private set; } = true;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            tapArea.onClick.AddListener(Dismiss);
        }

        public void Dismiss()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _group.blocksRaycasts = false;
            _fade = 0f;
            Dismissed?.Invoke();
        }

        private void Update()
        {
            if (IsOpen)
            {
                float t = Time.unscaledTime;
                promptLabel.alpha = 0.55f + 0.45f * Mathf.Sin(t * 4f);
                title.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(t * 2.2f));
                return;
            }

            if (_fade >= 0f)
            {
                _fade += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(_fade / fadeDuration);
                _group.alpha = 1f - p;
                title.localScale = Vector3.one * (1f + 0.25f * p);
                if (p >= 1f)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
