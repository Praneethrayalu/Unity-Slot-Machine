using SlotGame.Core.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// The machine's side lever. Pressing it (pointer down, like grabbing a real lever) starts a
    /// spin, and the lever animates (Animator "Pull" trigger) on every spin, whichever control
    /// started it. Lives on the lever's invisible hit area so clicks are precise.
    /// </summary>
    public sealed class LeverController : MonoBehaviour, IPointerDownHandler
    {
        private static readonly int PullTrigger = Animator.StringToHash("Pull");

        [SerializeField] private SlotMachineController controller;
        [SerializeField] private Animator animator;

        private void OnEnable()
        {
            controller.SpinStarted += OnSpinStarted;
        }

        private void OnDisable()
        {
            controller.SpinStarted -= OnSpinStarted;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // The lever only starts spins; it does not quick-stop like the button does.
            if (controller.State == SlotState.Idle)
            {
                controller.RequestSpin();
            }
        }

        private void OnSpinStarted(SpinResult spin)
        {
            animator.SetTrigger(PullTrigger);
        }
    }
}
