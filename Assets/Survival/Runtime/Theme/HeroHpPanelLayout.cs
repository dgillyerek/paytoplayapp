#nullable enable
using System;

namespace Survival.Domain.Theme
{
    /// <summary>
    /// The hero HP panel (Derek 2026-10-28: "find a better place for hero health bars. maybe a section in one of the
    /// corners"). Heroes no longer carry a world HP bar in the runner or the battle; one compact row per hero sits in the
    /// bottom-left corner of the safe area instead (the top of both screens holds the title, progress, buff badges, clock
    /// and Retreat). Each row: framed portrait, name / level, a horizontal HP bar with a damage trail, dimmed once the hero
    /// is down. Sizes are reference units on the 1080 x 1920 canvas (the HUD scales with height).
    /// </summary>
    public static class HeroHpPanelLayout
    {
        public const int MaxRows = 5;

        public const float Margin = 18f;
        public const float Padding = 8f;
        public const float RowHeight = 62f;
        public const float RowGap = 6f;

        /// <summary>Panel width as a share of the safe width, never past <see cref="MaxWidth"/> (tablets, landscape).</summary>
        public const float WidthShare = 0.40f;
        public const float MaxWidth = 420f;
        public const float MinWidth = 300f;

        /// <summary>The red trail behind the fill holds this long after a hit, then drains at <see cref="TrailDrainPerSecond"/>.</summary>
        public const float TrailHoldSeconds = 0.35f;
        public const float TrailDrainPerSecond = 0.9f;

        /// <summary>The row frame flashes this long after a hit.</summary>
        public const float FlashSeconds = 0.25f;

        /// <summary>Alpha of a fallen hero's row.</summary>
        public const float DeadAlpha = 0.45f;

        /// <summary>Rows shown for <paramref name="heroes"/> heroes (0..<see cref="MaxRows"/>).</summary>
        public static int Rows(int heroes) => Math.Max(0, Math.Min(MaxRows, heroes));

        /// <summary>Panel height for <paramref name="heroes"/> heroes (0 when there are none).</summary>
        public static float Height(int heroes)
        {
            var n = Rows(heroes);
            return n == 0 ? 0f : (Padding * 2f) + (n * RowHeight) + ((n - 1) * RowGap);
        }

        /// <summary>Panel width on a safe area <paramref name="safeWidth"/> reference units wide.</summary>
        public static float Width(float safeWidth) => Math.Min(MaxWidth, Math.Max(MinWidth, safeWidth * WidthShare));

        /// <summary>Top of row <paramref name="index"/> below the panel's top edge (row 0 at the top).</summary>
        public static float RowTop(int index) => Padding + (Math.Max(0, index) * (RowHeight + RowGap));

        /// <summary>The damage trail: holds after a hit, then drains to the fill; heals jump straight up.</summary>
        public static float Trail(float trail, float fraction, float secondsSinceHit, float dt)
        {
            if (fraction >= trail)
            {
                return fraction;
            }

            if (secondsSinceHit < TrailHoldSeconds)
            {
                return trail;
            }

            return Math.Max(fraction, trail - (TrailDrainPerSecond * Math.Max(0f, dt)));
        }

        /// <summary>Brightness of the hit flash, 1 at the hit fading to 0.</summary>
        public static float Flash(float secondsSinceHit) =>
            secondsSinceHit < 0f || secondsSinceHit >= FlashSeconds ? 0f : 1f - (secondsSinceHit / FlashSeconds);

        /// <summary>Row alpha: full while standing, <see cref="DeadAlpha"/> once down.</summary>
        public static float RowAlpha(bool alive) => alive ? 1f : DeadAlpha;

        /// <summary>Framed initial when a hero has no painted portrait ("Sir Aldric" → "S").</summary>
        public static string Initial(string name)
        {
            var t = (name ?? string.Empty).Trim();
            return t.Length == 0 ? "?" : char.ToUpperInvariant(t[0]).ToString();
        }

        /// <summary>Fill colour by share: green, amber below half, red below a quarter.</summary>
        public static (float R, float G, float B) FillRgb(float fraction) =>
            fraction > 0.5f ? (0.36f, 0.86f, 0.42f) : fraction > 0.25f ? (0.95f, 0.74f, 0.25f) : (0.92f, 0.30f, 0.32f);

        /// <summary>
        /// The pixels above the panel are free for screen-wide text (the runner's steer hint): bottom of that text as a
        /// share of the canvas height for a full panel.
        /// </summary>
        public static float ClearAboveShare(float canvasHeight) =>
            (Margin + Height(MaxRows) + Margin) / Math.Max(1f, canvasHeight);
    }
}
