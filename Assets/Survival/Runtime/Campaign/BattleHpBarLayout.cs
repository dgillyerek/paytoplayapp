#nullable enable
using System;

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// Where a fighter's HP bar sits in a configured battle: a vertical bar beside the body on its outer / back side
    /// (heroes on their left, foes on their right, both nudged back), so it never covers the lane between the two
    /// sides. Offsets are in the fighter's start frame (x = its right, z = its forward), in metres. Its height follows
    /// the body (the dragons at 2x get a 2x bar) and it stands clear of the body's measured half-width, wings included.
    /// The bar is drawn in screen space (always facing the camera), clamped to a readable pixel height.
    /// </summary>
    public static class BattleHpBarLayout
    {
        /// <summary>Gap between the widest point of the body and the bar, per unit of battle scale.</summary>
        public const float GapMetres = 0.18f;

        /// <summary>How far behind the body's origin the bar stands, per unit of battle scale.</summary>
        public const float BackMetres = 0.3f;

        /// <summary>Bar spans this share of the body's height (bottom..top above the ground).</summary>
        public const float BottomShare = 0.12f;
        public const float TopShare = 0.88f;

        public const float WidthPx = 20f;
        public const float MinHeightPx = 90f;
        public const float MaxHeightPx = 460f;

        /// <summary>Keep the bar this far inside its own lane edge.</summary>
        public const float LaneMarginMetres = 0.08f;

        /// <summary>Fallback body half-width before the mesh can be measured (winged bodies spread wider).</summary>
        public static float EstimatedHalfWidth(float designHeightMetres, float battleScale, bool winged) =>
            designHeightMetres * battleScale * (winged ? 0.9f : 0.32f);

        /// <summary>
        /// Body half-width at battle scale before the mesh is measured: the winged bodies from their measured spans
        /// (Stormcrest's wing buffet 1.09 m; 2x Ashwyrm 1.61 m; 2x Emberfang 1.23 m), everyone else from their height.
        /// </summary>
        public static float HalfWidthOf(string fighterName, float designHeightMetres, float battleScale)
        {
            var s = Math.Max(0.01f, battleScale);
            switch (fighterName)
            {
                case "Stormcrest":
                    return 1.09f * s;
                case "Ashwyrm":
                    return 0.805f * s;
                case "Emberfang":
                    return 0.615f * s;
                default:
                    return EstimatedHalfWidth(designHeightMetres, s, false);
            }
        }

        /// <summary>-1 = the fighter's own left, +1 = its own right.</summary>
        public static float SideSign(CampaignSide side) => side == CampaignSide.Hero ? -1f : 1f;

        /// <summary>
        /// Offset of the bar's foot from the fighter's origin, in its start frame. The sideways part clears the body
        /// (half-width + gap) but stays inside the lane; whatever the lane cannot fit sideways goes further back, so
        /// a wide wing span still leaves the bar clear of the body.
        /// </summary>
        public static (float Right, float Forward) Offset(CampaignSide side, float halfWidthMetres, float battleScale, float laneWidthMetres)
        {
            var scale = Math.Max(0.01f, battleScale);
            var wanted = Math.Max(0f, halfWidthMetres) + (GapMetres * scale);
            var room = Math.Max(0f, (laneWidthMetres * 0.5f) - LaneMarginMetres);
            var sideways = Math.Min(wanted, room);
            var back = (BackMetres * scale) + Math.Max(0f, wanted - sideways);
            return (SideSign(side) * sideways, -back);
        }

        /// <summary>Bottom and top heights of the bar above the ground, metres.</summary>
        public static (float Bottom, float Top) Span(float designHeightMetres, float battleScale)
        {
            var h = Math.Max(0.1f, designHeightMetres * Math.Max(0.01f, battleScale));
            return (h * BottomShare, h * TopShare);
        }

        /// <summary>On-screen bar height from the projected span, kept readable at any free-camera zoom.</summary>
        public static float HeightPx(float projectedPx) => Math.Min(MaxHeightPx, Math.Max(MinHeightPx, projectedPx));
    }
}
