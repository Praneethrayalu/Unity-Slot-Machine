using System.Collections;
using UnityEngine;

namespace SlotGame.UI
{
    /// <summary>
    /// Base class for modal panels: fades a CanvasGroup in/out, blocks input behind it while open
    /// and optionally plays "Show"/"Hide" triggers on an Animator for the pop-in motion.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupPanel : MonoBehaviour
    {
        private static readonly int ShowTrigger = Animator.StringToHash("Show");
        private static readonly int HideTrigger = Animator.StringToHash("Hide");

        [SerializeField] private Animator animator;
        [SerializeField] private float fadeDuration = 0.18f;

        private CanvasGroup _group;
        private Coroutine _fade;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            _group = GetComponent<CanvasGroup>();

            // Hidden by default, unless Awake is running because Open() just activated us.
            if (!IsOpen)
            {
                SetVisible(false);
            }
        }

        protected void Open()
        {
            // Mark open before activating: activation may run Awake for the first time.
            IsOpen = true;
            gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            _group.interactable = true;
            if (animator != null)
            {
                animator.ResetTrigger(HideTrigger);
                animator.SetTrigger(ShowTrigger);
            }

            FadeTo(1f, false);
        }

        public virtual void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            if (animator != null)
            {
                animator.ResetTrigger(ShowTrigger);
                animator.SetTrigger(HideTrigger);
            }

            FadeTo(0f, true);
        }

        private void FadeTo(float alpha, bool deactivateAtEnd)
        {
            if (_fade != null)
            {
                StopCoroutine(_fade);
            }

            _fade = StartCoroutine(FadeRoutine(alpha, deactivateAtEnd));
        }

        private IEnumerator FadeRoutine(float target, bool deactivateAtEnd)
        {
            float start = _group.alpha;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                _group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
                yield return null;
            }

            _group.alpha = target;
            _fade = null;
            if (deactivateAtEnd)
            {
                gameObject.SetActive(false);
            }
        }

        private void SetVisible(bool visible)
        {
            IsOpen = visible;
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
            _group.interactable = visible;
            gameObject.SetActive(visible);
        }
    }
}
