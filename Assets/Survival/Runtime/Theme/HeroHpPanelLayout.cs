#nullable enable
using System;

namespace Survival.Domain.Theme
{
    /// <summary>
    /// The hero HP panel (Derek 2026-10-28: "find a better place for hero health bars. maybe a section in one of the
    /// corners"). Heroes no longer carry a world HP bar in the runner or the battle; one compact row per hero sits in the
    /// top-left corner under the top row instead (Derek 2026-10-28, from the mock: "make them slightly smaller and put
    /// them in the top left"; rows are ~82% of the first cut; then "make the bars less wide but a little taller not much": ~74% of that width, bar ~+22%). See <see cref="TopHudLayout"/>. Each row: framed portrait, name / level, a horizontal HP bar with a damage trail, dimmed once the hero
    /// is down. Sizes are reference units on the 1080 x 1920 canvas (the HUD scales with height).
    /// </summary>
    public static class HeroHpPanelLayout
    {
        public const int MaxRows = 5;

        public const float Margin = 16f;
        public const float Padding = 7f;
        public const float RowHeight = 56f;
        public const float RowGap = 5f;


        /// <summary>The red trail behind the fill holds this long after a hit, then drains at <see cref="TrailDrainPerSecond"/>.</summary>
        public const float TrailHoldSeconds = 0.35f;
        public const float TrailDrainPerSecond = 0.9f;

        /// <summary>The row frame flashes this long after a hit.</summary>
        public const float FlashSeconds = 0.25f;

        /// <summary>Alpha of a fallen hero's row.</summary>
        public const float DeadAlpha = 0.45f;

        /// <summary>Rows shown for <paramref name="heroes"/> heroes (0..<see cref="MaxRows"/>).</summary>
        public static int Rows(int heroes) => Math.Max(0, Math.Min(MaxRows, heroes));

        /// <summary>
        /// Runner follower rows, one per unit type under the hero rows (Derek 2026-10-28: "new rows with the cumulative
        /// health of the secondary units split up by unit type"): a little shorter, after a thin divider.
        /// </summary>
        public const int MaxUnitRows = 4;
        public const float UnitRowHeight = 46f;
        public const float DividerHeight = 6f;

        /// <summary>Unit-type rows shown for <paramref name="types"/> types (0..<see cref="MaxUnitRows"/>).</summary>
        public static int UnitRows(int types) => Math.Max(0, Math.Min(MaxUnitRows, types));

        /// <summary>Panel height for <paramref name="heroes"/> heroes and <paramref name="unitTypes"/> follower types (0 when empty).</summary>
        public static float Height(int heroes, int unitTypes = 0)
        {
            var n = Rows(heroes);
            var u = UnitRows(unitTypes);
            if (n + u == 0)
            {
                return 0f;
            }

            return (Padding * 2f) + (n * RowHeight) + (u * UnitRowHeight) + ((n + u - 1) * RowGap) + (u > 0 && n > 0 ? DividerHeight : 0f);
        }

        /// <summary>Top of unit row <paramref name="index"/> below the panel's top edge, after <paramref name="heroes"/> hero rows.</summary>
        public static float UnitRowTop(int heroes, int index)
        {
            var n = Rows(heroes);
            return Padding + (n * (RowHeight + RowGap)) + (n > 0 ? DividerHeight : 0f) + (Math.Max(0, index) * (UnitRowHeight + RowGap));
        }

        /// <summary>
        /// Fixed panel width (Derek 2026-10-28: "make the bars the same size on both the runner and battle"): one number for
        /// every panel in both scenes (both HUD canvases scale with height from the same 1080 x 1920 reference), so rows and
        /// bars are pixel-identical. ~0.28 of a 9 : 19.5 phone.
        /// </summary>
        public const float PanelWidth = 250f;

        /// <summary>Panel width; the same on every screen and in both scenes (see <see cref="PanelWidth"/>).</summary>
        public static float Width(float safeWidth) => PanelWidth;

        /// <summary>Row width inside the panel.</summary>
        public const float RowWidth = PanelWidth - (2f * Padding);

        /// <summary>Row-local geometry (x from the row's left edge, y up from its bottom), shared by every row in both scenes.</summary>
        public const float PortraitSlot = RowHeight - 10f, Inset = 6f, NameGap = 8f, RightPad = 8f, TagWidth = 40f;
        public const float BarHeight = 22f, BarBottom = 4f;

        public readonly struct Span
        {
            public readonly float X0, Y0, X1, Y1;

            public Span(float x0, float y0, float x1, float y1)
            {
                X0 = x0;
                Y0 = y0;
                X1 = x1;
                Y1 = y1;
            }

            public float Width => X1 - X0;

            public Span Mirror(bool right) => right ? new Span(RowWidth - X1, Y0, RowWidth - X0, Y1) : this;

            public override string ToString() => $"[{X0:0.##},{Y0:0.##} - {X1:0.##},{Y1:0.##}]";
        }

        public readonly struct RowRects
        {
            public readonly Span Portrait, Name, Tag, Bar;

            public RowRects(Span portrait, Span name, Span tag, Span bar)
            {
                Portrait = portrait;
                Name = name;
                Tag = tag;
                Bar = bar;
            }
        }

        /// <summary>
        /// Where a row's parts go. The bar has one fixed rect for every row (hero, follower type, enemy; any name; either
        /// scene); the name is held to its own box and shrinks or clips instead of moving the bar (Derek 2026-10-28: "make
        /// all the health bars the same size. they are slightly different based on the characters name"). Follower rows
        /// are shorter, with a smaller portrait centred in the same slot. <paramref name="right"/> mirrors the row for the
        /// battle's enemy panel (portrait on the outer, right edge).
        /// </summary>
        public static RowRects Rects(bool unitRow, bool right)
        {
            var h = unitRow ? UnitRowHeight : RowHeight;
            var size = h - 10f;
            var px = Inset + ((PortraitSlot - size) * 0.5f);
            var left = Inset + PortraitSlot + NameGap;
            var bar = new Span(left, BarBottom, RowWidth - RightPad, BarBottom + BarHeight);
            var textY0 = bar.Y1 + 1f;
            var tag = new Span(RowWidth - RightPad - TagWidth, textY0, RowWidth - RightPad, h - 2f);
            var name = new Span(left, textY0, tag.X0 - 4f, h - 2f);
            return new RowRects(new Span(px, (h - size) * 0.5f, px + size, ((h - size) * 0.5f) + size).Mirror(right), name.Mirror(right), tag.Mirror(right), bar.Mirror(right));
        }

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

        /// <summary>Type sizes for a row (name, level tag, HP numbers, initial).</summary>
        public const int NameFont = 17, MinNameFont = 10, TagFont = 14, HpFont = 13, InitialFont = 23;

        /// <summary>Distance from the safe area's top edge to the panel: under the HUD's top row, never closer than the margin.</summary>
        public static float TopOffset(float canvasHeight, float safeTopInset) =>
            Math.Max(Margin, ((1f - TopHudLayout.PanelTop) * canvasHeight) - Math.Max(0f, safeTopInset));
    }
}
