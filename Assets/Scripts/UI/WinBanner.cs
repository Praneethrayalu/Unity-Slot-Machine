using SlotGame.Core.Game;
using SlotGame.Gameplay;
using SlotGame.Utilities;
using TMPro;
using UnityEngine;

namespace SlotGame.UI
{
    /// <summary>
    /// "WIN! +20" banner that slams in over the reels for small wins (bigger wins get the popup).
    /// Scale punch with an overshoot, a short hold, then a fade.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WinBanner : MonoBehaviour
    {
        [SerializeField] private SlotMachineController controller;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private float punchTime = 0.35f;
        [SerializeField] private float holdTime = 1.1f;
        [SerializeField] private float fadeTime = 0.35f;

        private CanvasGroup _group;
        private float _elapsed = -1f;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            controller.SpinCompleted += OnSpinCompleted;
            controller.SpinStarted += OnSpinStarted;
        }

        private void OnDisable()
        {
            controller.SpinCompleted -= OnSpinCompleted;
            controller.SpinStarted -= OnSpinStarted;
        }

        private void OnSpinStarted(SpinResult spin)
        {
            _elapsed = -1f;
            _group.alpha = 0f;
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            if (tier != WinTier.Small)
            {
                return;
            }

            titleLabel.text = spin.Win.FreeSpinsAwarded > 0 ? "FREE SPINS!" : "WIN!";
            amountLabel.text = $"+{spin.Win.Payout:N0}";
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float scale;
            float alpha;
            if (_elapsed < punchTime)
            {
                float p = _elapsed / punchTime;
                scale = Mathf.LerpUnclamped(0.2f, 1f, Easing.OutBack(p, 2.6f));
                alpha = Mathf.Clamp01(p * 3f);
            }
            else if (_elapsed < punchTime + holdTime)
            {
                scale = 1f + 0.04f * Mathf.Sin((_elapsed - punchTime) * 9f);
                alpha = 1f;
            }
            else
            {
                float p = (_elapsed - punchTime - holdTime) / fadeTime;
                scale = 1f + 0.15f * p;
                alpha = 1f - p;
                if (p >= 1f)
                {
                    _elapsed = -1f;
                    alpha = 0f;
                }
            }

            transform.localScale = Vector3.one * scale;
            _group.alpha = alpha;
        }
    }
}
