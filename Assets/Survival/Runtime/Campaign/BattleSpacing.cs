#nullable enable
using System;

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// How close two fighters stand in the locked battle (Derek 2026-10-28: Sir Aldric and Blightroot were "essentially on
    /// top of each other"). The old rule stopped each fighter when its origin came within its own attack range of the
    /// other's origin, so it ignored how big the two bodies are, and each attack's reach slide then moved the body
    /// forward with nothing to stop it: two melee fighters lunging at once (Blightroot 0.55 m, Sir Aldric 0.35 m from
    /// 1.4 m apart) ended up inside each other.
    /// Now the walk stops at the larger of the attack range and the two bodies' front-to-back half-depths plus a
    /// striking gap, a reach slide may only close the bodies to <see cref="ContactGapMetres"/>, and anything that still
    /// ends up closer is pushed apart. Half-depths are metres at battle scale, measured from the rendered body (with
    /// <see cref="HalfDepthOf"/> as the floor before the mesh is measured).
    /// </summary>
    public static class BattleSpacing
    {
        /// <summary>Space between the two bodies while they stand and trade blows (an arm and a blade).</summary>
        public const float StrikeGapMetres = 0.45f;

        /// <summary>Closest the two bodies may come at the peak of a lunge.</summary>
        public const float ContactGapMetres = 0.08f;

        /// <summary>A measured depth may exceed the estimate by this much at most (an axis box overstates a turned body).</summary>
        public const float MeasureCap = 1.6f;

        /// <summary>
        /// Front-to-back half-depth of a body before the mesh is measured: four-legged and winged bodies are long, the
        /// rooted Blightroot spreads its claws and roots, everyone else from their height.
        /// </summary>
        public static float HalfDepthOf(string fighterName, float designHeightMetres, float battleScale)
        {
            var s = Math.Max(0.01f, battleScale);
            switch (fighterName)
            {
                case "Blightroot":
                    return 0.62f * s;
                case "Ironhowl":
                    return 0.75f * s;
                case "Nightfang":
                    return 0.6f * s;
                case "Emberfang":
                case "Ashwyrm":
                case "Stormcrest":
                    return designHeightMetres * 0.55f * s;
                default:
                    return Math.Max(0.05f, designHeightMetres) * 0.2f * s;
            }
        }

        /// <summary>The estimate raised to a measured depth, capped at <see cref="MeasureCap"/> times the estimate.</summary>
        public static float Measured(float estimate, float measured) =>
            Math.Max(estimate, Math.Min(measured, estimate * MeasureCap));

        /// <summary>Origin-to-origin distance where a fighter stops walking and starts its attack.</summary>
        public static float StopDistance(float attackRange, float selfHalfDepth, float targetHalfDepth) =>
            Math.Max(attackRange, Math.Max(0f, selfHalfDepth) + Math.Max(0f, targetHalfDepth) + StrikeGapMetres);

        /// <summary>Closest two origins may be, ever.</summary>
        public static float MinDistance(float aHalfDepth, float bHalfDepth) =>
            Math.Max(0f, aHalfDepth) + Math.Max(0f, bHalfDepth) + ContactGapMetres;

        /// <summary>
        /// The forward reach slide a fighter may take this frame. <paramref name="slotDistance"/> is origin-to-origin
        /// without either slide; <paramref name="otherSlide"/> is how far the other body already slid toward this one.
        /// A slide only gets the room the other's slide left, so two lunges at once cannot both take it all (a frame's
        /// lag between the two is caught by the overlap push). Backward slides pass through.
        /// </summary>
        public static float ClampReach(float wanted, float slotDistance, float otherSlide, float aHalfDepth, float bHalfDepth)
        {
            if (wanted <= 0f)
            {
                return wanted;
            }

            var room = Math.Max(0f, slotDistance - MinDistance(aHalfDepth, bHalfDepth));
            return Math.Min(wanted, Math.Max(0f, room - Math.Max(0f, otherSlide)));
        }

        /// <summary>How far two bodies have to be pushed apart (shared half each) so they do not overlap.</summary>
        public static float Overlap(float distance, float aHalfDepth, float bHalfDepth) =>
            Math.Max(0f, MinDistance(aHalfDepth, bHalfDepth) - Math.Max(0f, distance));
    }
}
