using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Feel of a single reel's motion. Distances are in "symbols" (one symbol height = 1)
    /// so the same profile works at any resolution.
    /// </summary>
    [CreateAssetMenu(fileName = "ReelMotionProfile", menuName = "Slot Game/Reel Motion Profile", order = 5)]
    public sealed class ReelMotionProfile : ScriptableObject
    {
        [Header("Start")]
        [Tooltip("How far the reel kicks backwards before spinning (anticipation of a real lever pull).")]
        [Range(0f, 1f)] [SerializeField] private float windUpDistance = 0.25f;

        [Min(0.01f)] [SerializeField] private float windUpDuration = 0.12f;

        [Tooltip("Time to reach full speed after the wind-up (ease-in).")]
        [Min(0.01f)] [SerializeField] private float accelerationDuration = 0.35f;

        [Header("Spin")]
        [Tooltip("Top speed in symbols per second.")]
        [Min(1f)] [SerializeField] private float maxSpeed = 22f;

        [Tooltip("Above this speed the symbols swap to their motion-blurred sprites.")]
        [Min(0f)] [SerializeField] private float blurSpeedThreshold = 9f;

        [Tooltip("Extra vertical stretch applied to symbols at full speed (0.15 = +15%).")]
        [Range(0f, 0.5f)] [SerializeField] private float motionStretch = 0.18f;

        [Header("Stop")]
        [Tooltip("Symbols still travelled after a stop is requested (must be >= 5 so the target is not yet visible).")]
        [Min(5)] [SerializeField] private int stopDistance = 5;

        [Tooltip("How far past the target the reel overshoots before settling back (bounce).")]
        [Range(0f, 0.6f)] [SerializeField] private float bounceOvershoot = 0.22f;

        [Min(0.01f)] [SerializeField] private float bounceDuration = 0.28f;

        [Tooltip("Speed multiplier for the deceleration when the player quick-stops.")]
        [Min(1f)] [SerializeField] private float quickStopSpeedUp = 1.8f;

        public float WindUpDistance => windUpDistance;
        public float WindUpDuration => windUpDuration;
        public float AccelerationDuration => accelerationDuration;
        public float MaxSpeed => maxSpeed;
        public float BlurSpeedThreshold => blurSpeedThreshold;
        public float MotionStretch => motionStretch;
        public int StopDistance => stopDistance;
        public float BounceOvershoot => bounceOvershoot;
        public float BounceDuration => bounceDuration;
        public float QuickStopSpeedUp => quickStopSpeedUp;
    }
}
