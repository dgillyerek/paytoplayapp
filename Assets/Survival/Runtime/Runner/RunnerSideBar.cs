#nullable enable
using System;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// Where a runner fighter's HP bar sits, following the configured battle's side bars (#76's BattleHpBarLayout):
    /// a vertical bar beside the body on its own outer side (heroes their left, foes their right), nudged back, its
    /// height following the body, drawn in screen space and clamped to a readable pixel height. Same numbers as the
    /// battle so the two read alike; swap to BattleHpBarLayout once that lands on main. Offsets are metres in the
    /// fighter's facing frame (x = its right, z = its forward).
    /// </summary>
    public static class RunnerSideBar
    {
        public const float GapMetres = 0.18f;
        public const float BackMetres = 0.3f;
        public const float BottomShare = 0.12f;
        public const float TopShare = 0.88f;
        public const float WidthPx = 20f;
        public const float MinHeightPx = 90f;
        public const float MaxHeightPx = 460f;

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
