#nullable enable
using System;
using Survival.Domain.Campaign;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// Where a runner fighter's HP bar sits and how it looks. The layout is the configured battle's shared
    /// <see cref="BattleHpBarLayout"/> (a vertical bar beside the body on its own outer side, heroes their left, foes
    /// their right, nudged back, its height following the body). The runner has no lanes, so the bar always clears
    /// the body sideways. Only the style is the runner's own: a thin sliver, short, semi-transparent, and faded in
    /// only while the unit is taking damage (Derek: "less conspicuous"). Offsets are metres in the fighter's facing
    /// frame (x = its right, z = its forward).
    /// </summary>
    public static class RunnerSideBar
    {
        public const float GapMetres = BattleHpBarLayout.GapMetres;
        public const float BackMetres = BattleHpBarLayout.BackMetres;
        public const float BottomShare = BattleHpBarLayout.BottomShare;
        public const float TopShare = BattleHpBarLayout.TopShare;

        // The runner's style: a 6 px sliver, short, semi-transparent, only there while something takes damage.
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
        /// Boxes (chests and upgrade crates) are different: their bar is always shown, fully opaque, from the moment the
        /// box is on screen until it breaks, so you can see at a glance how much each one still needs.
        /// </summary>
        public static float BoxAlpha(int hp) => hp > 0 ? 1f : 0f;

        /// <summary>
        /// The squad's own bars (the lead and every follower) are always shown too, fully opaque, while the member is
        /// alive (Derek 2026-10-23: the followers' 25 HP has to be seen); only enemy bars fade in on damage.
        /// </summary>
        public static float SquadAlpha(int hp) => hp > 0 ? 1f : 0f;

        /// <summary>The squad's bars are a little wider than an enemy's sliver so they read at a glance.</summary>
        public const float SquadWidthPx = 8f;

        public static float WidthFor(bool squad) => squad ? SquadWidthPx : WidthPx;

        /// <summary>The runner's road has no lanes: the bar never has to fold back to fit one.</summary>
        private const float NoLane = float.MaxValue;

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
        public static float HalfWidth(float bodyHeightMetres) => BattleHpBarLayout.EstimatedHalfWidth(Math.Max(0.05f, bodyHeightMetres), 1f, false);

        /// <summary>-1 = the fighter's own left (heroes), +1 = its own right (foes).</summary>
        public static float SideSign(bool hero) => BattleHpBarLayout.SideSign(Side(hero));

        /// <summary>Foot of the bar from the fighter's origin in its facing frame (the battle's layout, lane-free).</summary>
        public static (float Right, float Forward) Offset(bool hero, float halfWidthMetres, float scale) =>
            BattleHpBarLayout.Offset(Side(hero), halfWidthMetres, scale, NoLane);

        /// <summary>
        /// The same offset in runner world space (x across the road, z down it): heroes face +z, foes face -z, so
        /// both sides' bars land on the -x side of the body, behind it as seen from its own front.
        /// </summary>
        public static (float X, float Z) WorldOffset(bool hero, float halfWidthMetres, float scale)
        {
            var (right, forward) = Offset(hero, halfWidthMetres, scale);
            return hero ? (right, forward) : (-right, -forward);
        }

        /// <summary>Bottom and top of the bar above the ground (the battle's span; runner heights are already scaled).</summary>
        public static (float Bottom, float Top) Span(float bodyHeightMetres) => BattleHpBarLayout.Span(bodyHeightMetres, 1f);

        /// <summary>On-screen height, clamped to the runner's short sliver range.</summary>
        public static float HeightPx(float projectedPx) => Math.Min(MaxHeightPx, Math.Max(MinHeightPx, projectedPx));

        private static CampaignSide Side(bool hero) => hero ? CampaignSide.Hero : CampaignSide.Villain;
    }
}
