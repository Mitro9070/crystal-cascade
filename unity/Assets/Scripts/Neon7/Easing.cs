using UnityEngine;

namespace Neon7
{
    /// <summary>CSS cubic-bezier кривые прототипа.</summary>
    public static class Easing
    {
        // cubic-bezier(0.34, 1.4, 0.64, 1) — падение шара
        public static float Drop(float t) => Bezier(t, 0.34f, 1.4f, 0.64f, 1f);
        // cubic-bezier(0.3, 1.6, 0.5, 1) — squash
        public static float SquashCurve(float t) => Bezier(t, 0.3f, 1.6f, 0.5f, 1f);
        // cubic-bezier(0.2, 0.8, 0.3, 1) — искры
        public static float Spark(float t) => Bezier(t, 0.2f, 0.8f, 0.3f, 1f);
        public static float Ease(float t) => Bezier(t, 0.25f, 0.1f, 0.25f, 1f);
        public static float EaseOut(float t) => Bezier(t, 0f, 0f, 0.58f, 1f);
        public static float EaseInOut(float t) => Bezier(t, 0.42f, 0f, 0.58f, 1f);

        /// <summary>Решение CSS cubic-bezier(x1,y1,x2,y2) для progress t (0..1).</summary>
        public static float Bezier(float t, float x1, float y1, float x2, float y2)
        {
            t = Mathf.Clamp01(t);
            float u = t;
            for (int i = 0; i < 8; i++) // Newton-Raphson по X
            {
                float x = CurveX(u, x1, x2) - t;
                if (Mathf.Abs(x) < 1e-5f) break;
                float d = CurveDx(u, x1, x2);
                if (Mathf.Abs(d) < 1e-6f) break;
                u -= x / d;
                u = Mathf.Clamp01(u);
            }
            return CurveY(u, y1, y2);
        }

        private static float CurveX(float u, float x1, float x2)
        {
            float v = 1f - u;
            return 3f * v * v * u * x1 + 3f * v * u * u * x2 + u * u * u;
        }

        private static float CurveY(float u, float y1, float y2)
        {
            float v = 1f - u;
            return 3f * v * v * u * y1 + 3f * v * u * u * y2 + u * u * u;
        }

        private static float CurveDx(float u, float x1, float x2)
        {
            float v = 1f - u;
            return 3f * v * v * x1 + 6f * v * u * (x2 - x1) + 3f * u * u * (1f - x2);
        }
    }
}
