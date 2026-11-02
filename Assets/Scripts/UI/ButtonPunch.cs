using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// "Juicy" press feedback for a button: it squashes while held and springs back with a small
    /// overshoot on release. Purely visual; the Button still owns the click.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.9f;
        [SerializeField] private float stiffness = 520f;
        [SerializeField] private float damping = 18f;

        private Selectable _selectable;
        private Vector3 _restScale;
        private float _scale = 1f;
        private float _velocity;
        private float _target = 1f;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _restScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable == null || _selectable.IsInteractable())
            {
                _target = pressedScale;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _target = 1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _target = 1f;
        }

        private void Update()
        {
            // Damped spring towards the target scale: overshoots slightly on release, never jitters.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            _velocity += ((_target - _scale) * stiffness - _velocity * damping) * dt;
            _scale += _velocity * dt;
            transform.localScale = _restScale * _scale;
        }

        private void OnDisable()
        {
            _scale = _target = 1f;
            _velocity = 0f;
            transform.localScale = _restScale;
        }
    }
}
