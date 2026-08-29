using UnityEngine;

namespace NeonSeven.PrototypePort.Neon7
{
    /// <summary>Canonical values adapted from Neon7/Palette.cs and Neon7/Easing.cs.</summary>
    public static class PrototypePalette
    {
        public static readonly Color[] Numbers =
        {
            Hex("#EBF3FC"),
            Hex("#00DFF2"),
            Hex("#46DA89"),
            Hex("#F7CC4B"),
            Hex("#FF8648"),
            Hex("#FF53A5"),
            Hex("#9658FF")
        };

        public static readonly Color AimNormal = Hex("#00DFF2", 0.18f);
        public static readonly Color AimMatch = Hex("#46DA89", 0.26f);
        public static readonly Color GhostBorder = new Color(1f, 1f, 1f, 0.30f);
        public static readonly Color GridLine = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color NumInk = Hex("#070815");
        public static readonly Color Obsidian = Hex("#14151F");
        public static readonly Color Lava = Hex("#F9AD26");

        public static Color ForNumber(int number)
        {
            return number >= 1 && number <= Numbers.Length ? Numbers[number - 1] : Obsidian;
        }

        private static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            color.a = alpha;
            return color;
        }
    }

    public static class PrototypeMetrics
    {
        public const int Size = 7;
        public const int RiseEvery = 5;
        public const float HiddenPieceChance = 0.15f;
        public const float HiddenStartChance = 0.25f;
        public const int BoardClearBonus = 70000;
        public const float RefWidth = 420f;
        public const float RefHeight = 900f;
        public const float BallInsetRatio = 0.06f;
        public const float BallGlowOuter = 42f;
        public const float SpawnDelay = 0.030f;
        public const float DropMove = 0.190f;
        public const float ColumnMove = 0.140f;
        public const float Squash = 0.260f;
        public const float Pop = 0.300f;
        public const float Gravity = 0.230f;
        public const float Ring = 0.520f;
        public const float Spark = 0.700f;
        public const float PopSoundStep = 0.045f;
        public const float FloatScore = 0.900f;
        public const float Banner = 1.500f;
        public const float Rise = 0.260f;
        public const float AimTween = 0.150f;
        public const float ClearPause = 0.700f;

        public static float Cell(float boardWidth) => boardWidth / 7f;
        public static float BallDiameter(float cell) => cell * (1f - BallInsetRatio * 2f);
        public static float NumberFontSize(float boardWidth) => Mathf.Clamp(boardWidth * 0.042f, 14.4f, 24f);
    }

    public static class PrototypeEasing
    {
        public static float Drop(float t) => Bezier(t, 0.34f, 1.4f, 0.64f, 1f);
        public static float Squash(float t) => Bezier(t, 0.3f, 1.6f, 0.5f, 1f);
        public static float Spark(float t) => Bezier(t, 0.2f, 0.8f, 0.3f, 1f);
        public static float Ease(float t) => Bezier(t, 0.25f, 0.1f, 0.25f, 1f);
        public static float EaseOut(float t) => Bezier(t, 0f, 0f, 0.58f, 1f);
        public static float EaseInOut(float t) => Bezier(t, 0.42f, 0f, 0.58f, 1f);

        public static float Bezier(float t, float x1, float y1, float x2, float y2)
        {
            t = Mathf.Clamp01(t);
            float u = t;
            for (int i = 0; i < 8; i++)
            {
                float x = Curve(u, x1, x2) - t;
                if (Mathf.Abs(x) < 0.00001f)
                    break;

                float derivative = CurveDerivative(u, x1, x2);
                if (Mathf.Abs(derivative) < 0.000001f)
                    break;

                u = Mathf.Clamp01(u - x / derivative);
            }

            return Curve(u, y1, y2);
        }

        private static float Curve(float u, float p1, float p2)
        {
            float v = 1f - u;
            return 3f * v * v * u * p1 + 3f * v * u * u * p2 + u * u * u;
        }

        private static float CurveDerivative(float u, float p1, float p2)
        {
            float v = 1f - u;
            return 3f * v * v * p1 + 6f * v * u * (p2 - p1) + 3f * u * u * (1f - p2);
        }
    }
}
