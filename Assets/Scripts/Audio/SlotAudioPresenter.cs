using SlotGame.Core.Game;
using SlotGame.Gameplay;
using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>
    /// Translates machine events into sound. Keeping this out of the controller means gameplay
    /// code has zero audio dependencies and the soundscape can be changed in one place.
    /// </summary>
    public sealed class SlotAudioPresenter : MonoBehaviour
    {
        [SerializeField] private SlotMachineController controller;
        [SerializeField] private AudioManager audioManager;

        [Tooltip("Each successive reel stop is pitched up a little, a classic slot cue.")]
        [SerializeField] private float reelStopPitchStep = 0.06f;

        private void OnEnable()
        {
            controller.SpinStarted += OnSpinStarted;
            controller.ReelStopped += OnReelStopped;
            controller.AnticipationStarted += OnAnticipation;
            controller.SpinCompleted += OnSpinCompleted;
            controller.SpinRejected += OnSpinRejected;
        }

        private void OnDisable()
        {
            controller.SpinStarted -= OnSpinStarted;
            controller.ReelStopped -= OnReelStopped;
            controller.AnticipationStarted -= OnAnticipation;
            controller.SpinCompleted -= OnSpinCompleted;
            controller.SpinRejected -= OnSpinRejected;
        }

        private void OnSpinStarted(SpinResult spin)
        {
            audioManager.Play(SoundId.LeverPull);
            audioManager.StartLoop(SoundId.ReelSpinLoop);
        }

        private void OnReelStopped(int reelIndex)
        {
            audioManager.Play(SoundId.ReelStop, 1f + reelStopPitchStep * reelIndex);
            if (reelIndex == controller.Reels.Count - 1)
            {
                audioManager.StopLoop(SoundId.ReelSpinLoop);
                audioManager.StopLoop(SoundId.Anticipation);
            }
        }

        private void OnAnticipation(int reelIndex)
        {
            audioManager.StartLoop(SoundId.Anticipation);
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            switch (tier)
            {
                case WinTier.Small:
                    audioManager.Play(SoundId.WinSmall);
                    break;
                case WinTier.Big:
                    audioManager.Play(SoundId.WinBig);
                    break;
                case WinTier.Jackpot:
                    audioManager.Play(SoundId.Jackpot);
                    break;
            }

            if (spin.Win.FreeSpinsAwarded > 0)
            {
                audioManager.Play(SoundId.FreeSpins);
            }
        }

        private void OnSpinRejected(SpinRejection rejection)
        {
            audioManager.StopLoop(SoundId.ReelSpinLoop);
        }
    }
}
