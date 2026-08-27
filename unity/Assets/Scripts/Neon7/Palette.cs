using UnityEngine;

namespace Neon7
{
    /// <summary>
    /// Все цвета и метрики веб-прототипа (src/styles.css) в виде констант.
    /// Значения получены конвертацией oklch -> sRGB.
    /// </summary>
    public static class Palette
    {
        public static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = a;
            return c;
        }

        // фон
        public static readonly Color BgDeep = Hex("#080924");
        public static readonly Color BgDeep2 = Hex("#040212");
        public static readonly Color GlowTop = Hex("#443081", 0.55f);
        public static readonly Color GlowBottom = Hex("#005B65", 0.35f);

        // стекло
        public static readonly Color Glass = Hex("#2B314C", 0.28f);
        public static readonly Color GlassBorder = Hex("#C5CAF5", 0.18f);
        public static readonly Color GlassShadow = new Color(0f, 0f, 0f, 0.45f);
        public static readonly Color GlassInnerHighlight = new Color(1f, 1f, 1f, 0.12f);
        public const float GlassBlurPx = 18f;

        // текст
        public static readonly Color Ink = Hex("#F3F4FC");
        public static readonly Color InkDim = Hex("#B3B6CB", 0.75f);
        public static readonly Color Neon = Hex("#00D6E2");
        public static readonly Color NumInk = Hex("#070815");

        // цифры 1..7
        public static readonly Color[] Numbers =
        {
            Hex("#EBF3FC"), // 1 жемчужно-белый
            Hex("#00DFF2"), // 2 cyan
            Hex("#46DA89"), // 3 изумруд
            Hex("#F7CC4B"), // 4 янтарь
            Hex("#FF8648"), // 5 коралл
            Hex("#FF53A5"), // 6 малина
            Hex("#9658FF"), // 7 ультрамарин
        };

        public static Color ForNumber(int? num) => num.HasValue ? Numbers[num.Value - 1] : Obsidian;

        // обсидиан / лава
        public static readonly Color Obsidian = Hex("#14151F");
        public static readonly Color ObsidianHigh = Hex("#272737");
        public static readonly Color ObsidianLow = Hex("#010203");
        public static readonly Color Lava = Hex("#F9AD26");
        public static readonly Color BallDark = Hex("#131428", 0.55f);

        // подсветка колонки прицела
        public static readonly Color AimNormal = Hex("#00DFF2", 0.18f);
        public static readonly Color AimMatch = Hex("#46DA89", 0.26f);
        public static readonly Color GhostBorder = new Color(1f, 1f, 1f, 0.30f);
        public static readonly Color GridLine = new Color(1f, 1f, 1f, 0.05f);
    }

    /// <summary>Метрики в CSS-px == UI-юниты при CanvasScaler reference 420x900.</summary>
    public static class Metrics
    {
        public const float RefWidth = 420f;
        public const float RefHeight = 900f;
        public const float SidePadding = 12f;
        public const float ColumnWidth = RefWidth - SidePadding * 2f; // 396

        public const float BoardRadius = 32f;
        public const float PanelRadius = 24f;
        public const float ButtonSize = 44f;
        public const float ButtonRadius = 16f;

        public const float BallInsetRatio = 0.06f;   // ball-face inset 6%
        public const float BallGlowInner = 18f;
        public const float BallGlowOuter = 42f;

        public static float Cell(float boardWidth) => boardWidth / GameLogic.Size;
        public static float BallDiameter(float cell) => cell * (1f - BallInsetRatio * 2f);
        public static float NumberFontSize(float boardWidth) =>
            Mathf.Clamp(boardWidth * 0.042f, 14.4f, 24f);

        // тайминги (сек), см. README §5
        public const float DropMove = 0.190f;
        public const float ColumnMove = 0.140f;
        public const float Squash = 0.260f;
        public const float SpawnDelay = 0.030f;
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

        public static readonly float[] ShakeDuration = { 0.260f, 0.340f, 0.420f };
        public static readonly float[] ShakeAmplitude = { 4f, 8f, 14f };
    }
}
