#nullable enable
namespace Survival.Domain.Runner
{
    /// <summary>
    /// Where the runner HUD's upgrade badges (crate icon + "+65% DMG" / "+30% SPD") stand along the top, as fractions of
    /// the screen width, and the type range their label shrinks through. Sized so "+100% DMG" fits inside the badge on the
    /// narrowest phone the HUD scales to (Derek 2026-10-27: the label ran past the badge's right edge), side by side
    /// right of the hero HP panel (Theme.TopHudLayout; the kills panel moved to the top row).
    /// </summary>
    public static class RunnerHudChips
    {
        public const float DamageLeft = 0.43f, SpeedLeft = 0.715f, Width = 0.27f;

        /// <summary>The icon covers the badge's left part; the label sits between these, clear of the rounded rim.</summary>
        public const float IconTo = 0.33f, TextFrom = 0.32f, TextTo = 0.95f;

        public const int MaxFont = 24, MinFont = 14;

        /// <summary>The narrowest screen (width / height) the HUD is laid out for: a 9 : 19.5 phone.</summary>
        public const float NarrowestAspect = 9f / 19.5f;

        /// <summary>Reference canvas height (the HUD scales with height).</summary>
        public const float ReferenceHeight = 1920f;

        /// <summary>A rough width (reference units) of a bold label at <paramref name="font"/> pt: 0.62 em a character.</summary>
        public static float LabelWidth(string text, int font) => text.Length * font * 0.62f;

        /// <summary>The label room inside one badge on a screen of <paramref name="aspect"/>, in reference units.</summary>
        public static float LabelRoom(float aspect) => ReferenceHeight * aspect * Width * (TextTo - TextFrom);
    }
}
