using SlotGame.Core.Game;
using UnityEngine;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Short, decaying camera-style shake of the playfield on big wins and the jackpot.
    /// Moves the targets' anchored positions with smooth Perlin noise (not random jitter, which
    /// reads as a glitch) and always restores the exact resting position.
    /// </summary>
    public sealed class ScreenShake : MonoBehaviour
    {
        [SerializeField] private SlotMachineController controller;
        [SerializeField] private RectTransform[] targets = new RectTransform[0];

        [Header("Big win")]
        [SerializeField] private float bigWinAmplitude = 9f;
        [SerializeField] private float bigWinDuration = 0.45f;

        [Header("Jackpot")]
        [SerializeField] private float jackpotAmplitude = 22f;
        [SerializeField] private float jackpotDuration = 0.9f;

        [SerializeField] private float frequency = 28f;

        private Vector2[] _rest;
        private float _amplitude;
        private float _duration;
        private float _elapsed;
        private bool _shaking;

        private void Awake()
        {
            _rest = new Vector2[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _rest[i] = targets[i].anchoredPosition;
            }
        }

        private void OnEnable()
        {
            controller.SpinCompleted += OnSpinCompleted;
        }

        private void OnDisable()
        {
            controller.SpinCompleted -= OnSpinCompleted;
            Restore();
        }

        /// <summary>Starts a shake; a stronger request overrides a weaker one already playing.</summary>
        public void Shake(float amplitude, float duration)
        {
            if (_shaking && amplitude < _amplitude * (1f - _elapsed / _duration))
            {
                return;
            }

            _amplitude = amplitude;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _shaking = true;
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            if (tier == WinTier.Jackpot)
            {
                Shake(jackpotAmplitude, jackpotDuration);
            }
            else if (tier == WinTier.Big)
            {
                Shake(bigWinAmplitude, bigWinDuration);
            }
        }

        private void LateUpdate()
        {
            if (!_shaking)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float progress = _elapsed / _duration;
            if (progress >= 1f)
            {
                _shaking = false;
                Restore();
                return;
            }

            // Quadratic falloff: a hard hit that settles quickly.
            float strength = _amplitude * (1f - progress) * (1f - progress);
            float n = _elapsed * frequency;
            var offset = new Vector2(Mathf.PerlinNoise(n, 0.17f) - 0.5f, Mathf.PerlinNoise(0.71f, n) - 0.5f) * (2f * strength);

            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].anchoredPosition = _rest[i] + offset;
            }
        }

        private void Restore()
        {
            if (_rest == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].anchoredPosition = _rest[i];
            }
        }
    }
}
