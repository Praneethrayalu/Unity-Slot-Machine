using SlotGame.Core.Game;
using UnityEngine;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Drives the cabinet's light show (an Animator with "Celebrate" and "Jackpot" triggers)
    /// and the flashing pay line from machine events.
    /// </summary>
    public sealed class MachineLights : MonoBehaviour
    {
        private static readonly int CelebrateTrigger = Animator.StringToHash("Celebrate");
        private static readonly int JackpotTrigger = Animator.StringToHash("Jackpot");
        private static readonly int StopTrigger = Animator.StringToHash("Stop");

        [SerializeField] private SlotMachineController controller;
        [SerializeField] private Animator lightsAnimator;

        [Tooltip("Line across the reels that flashes on a win.")]
        [SerializeField] private CanvasGroup paylineGroup;

        private float _paylineFlashUntil;

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

        private void Update()
        {
            if (paylineGroup == null)
            {
                return;
            }

            // Flash while a win is shown, otherwise a faint resting line marks the pay line.
            paylineGroup.alpha = Time.time < _paylineFlashUntil
                ? 0.55f + 0.45f * Mathf.Sign(Mathf.Sin(Time.time * 18f))
                : 0.35f;
        }

        private void OnSpinStarted(SpinResult spin)
        {
            _paylineFlashUntil = 0f;
            lightsAnimator.SetTrigger(StopTrigger);
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            if (tier == WinTier.None)
            {
                return;
            }

            _paylineFlashUntil = Time.time + (tier >= WinTier.Big ? 3f : 1.2f);
            lightsAnimator.ResetTrigger(StopTrigger);
            lightsAnimator.SetTrigger(tier == WinTier.Jackpot ? JackpotTrigger : CelebrateTrigger);
        }
    }
}
