#nullable enable
using System;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// Where a runner fighter's HP bar sits, following the configured battle's side bars (#76's BattleHpBarLayout):
    /// a vertical bar beside the body on its own outer side (heroes their left, foes their right), nudged back, its
    /// height following the body, drawn in screen space and clamped to a pixel height. Same numbers as the
    /// battle so the two read alike; swap to BattleHpBarLayout once that lands on main. Offsets are metres in the
    /// fighter's facing frame (x = its right, z = its forward).
    /// </summary>
    public static class RunnerSideBar
    {
        public const float GapMetres = 0.18f;
        public const float BackMetres = 0.3f;
        public const float BottomShare = 0.12f;
        public const float TopShare = 0.88f;
        // The runner's bars are the battle's layout, slimmed down (Derek: "less conspicuous"): a 6 px sliver, short,
        // semi-transparent, and only there while something is taking damage (see Alpha).
        public const float WidthPx = 6f;
        public const float MinHeightPx = 34f;
        public const float MaxHeightPx = 110f;

        /// <summary>Opacity of a showing bar; it never goes fully solid.</summary>
        public const float ShownAlpha = 0.7f;

        /// <summary>A bar stays this long after the last hit, then fades out over <see cref="FadeSeconds"/>.</summary>
        public const float HoldSeconds = 2f;
        public const float FadeSeconds = 0.5f;

        /// <summary>Fade-in after a hit (so it doesn't pop).</summary>
        public const float RiseSeconds = 0.08f;

        /// <summary>
        /// How visible a unit's or box's bar is: hidden at full HP (and once dead), in at <see cref="ShownAlpha"/> as
        /// soon as it takes damage, held while hits keep landing, gone <see cref="HoldSeconds"/> + <see cref="FadeSeconds"/>
        /// after the last one.
        /// </summary>
        public static float Alpha(int hp, int maxHp, float secondsSinceDamage)
        {
            if (hp <= 0 || hp >= maxHp || secondsSinceDamage < 0f)
            {
                return 0f;
            }

            var rise = Math.Min(1f, secondsSinceDamage / RiseSeconds);
            var fade = secondsSinceDamage <= HoldSeconds ? 1f : Math.Max(0f, 1f - ((secondsSinceDamage - HoldSeconds) / FadeSeconds));
            return ShownAlpha * Math.Min(rise, fade);
        }

        /// <summary>Body half-width before the mesh is measured (the battle's estimate for upright bodies).</summary>
        public static float HalfWidth(float bodyHeightMetres) => Math.Max(0.05f, bodyHeightMetres) * 0.32f;

        /// <summary>-1 = the fighter's own left (heroes), +1 = its own right (foes).</summary>
        public static float SideSign(bool hero) => hero ? -1f : 1f;

        /// <summary>Foot of the bar from the fighter's origin in its facing frame.</summary>
        public static (float Right, float Forward) Offset(bool hero, float halfWidthMetres, float scale)
        {
            var s = Math.Max(0.01f, scale);
            return (SideSign(hero) * (Math.Max(0f, halfWidthMetres) + (GapMetres * s)), -BackMetres * s);
        }

        /// <summary>
        /// The same offset in runner world space (x across the road, z down it): heroes face +z, foes face -z, so
        /// both sides' bars land on the -x side of the body, behind it as seen from its own front.
        /// </summary>
        public static (float X, float Z) WorldOffset(bool hero, float halfWidthMetres, float scale)
        {
            var (right, forward) = Offset(hero, halfWidthMetres, scale);
            return hero ? (right, forward) : (-right, -forward);
        }

        public static (float Bottom, float Top) Span(float bodyHeightMetres)
        {
            var h = Math.Max(0.1f, bodyHeightMetres);
            return (h * BottomShare, h * TopShare);
        }

        public static float HeightPx(float projectedPx) => Math.Min(MaxHeightPx, Math.Max(MinHeightPx, projectedPx));
    }
}
