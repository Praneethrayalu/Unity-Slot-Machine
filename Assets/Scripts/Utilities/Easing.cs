using UnityEngine;

namespace SlotGame.Utilities
{
    /// <summary>
    /// Standard easing curves (t in [0,1] mapped to progress). Kept in code rather than
    /// AnimationCurves so motion stays identical across every reel and is easy to unit test.
    /// </summary>
    public static class Easing
    {
        public static float Linear(float t) => Mathf.Clamp01(t);

        public static float InQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        public static float OutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        public static float InOutSine(float t)
        {
            t = Mathf.Clamp01(t);
            return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        }

        /// <summary>Ease-out with a slight overshoot past 1 before settling (the classic "back" ease).</summary>
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t);
            float c3 = overshoot + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + overshoot * x * x;
        }

        /// <summary>Springy settle used for UI "pop" scales.</summary>
        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f || t >= 1f)
            {
                return t;
            }

            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }
}
