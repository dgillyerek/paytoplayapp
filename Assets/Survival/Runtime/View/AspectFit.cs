#nullable enable
using System;

namespace Survival.Domain.View
{
    /// <summary>
    /// Keeps units and HUD the same apparent size on any portrait screen (Derek 2026-11-20, Pixel 7 Pro: "only the middle
    /// game screen scales ... not the enemies, heroes, buttons"). The game is authored on a 1080 x 1920 (9:16) Game view.
    /// On a taller phone (19.5:9 is 0.4615 wide-per-tall against 9:16's 0.5625) the horizontal field of view and the HUD
    /// width stay what they are at 9:16 and the extra height becomes more visible world; at 9:16 and on anything wider
    /// (PC, tablet, landscape) nothing changes from the authored vertical framing.
    /// </summary>
    public static class AspectFit
    {
        /// <summary>The authored Game view aspect (width / height).</summary>
        public const float ReferenceAspect = 1080f / 1920f;

        /// <summary>Tallest aspect that still gains view (9:21). Taller than this holds the 9:21 vertical view, so a very
        /// tall screen zooms in slightly rather than showing past the road's near end or the HUD's bands.</summary>
        public const float MinAspect = 9f / 21f;

        /// <summary>Camera frustum as tangents of the half-angles from the view axis (left/bottom negative).</summary>
        public readonly struct Frustum
        {
            public Frustum(double left, double right, double bottom, double top)
            {
                Left = left;
                Right = right;
                Bottom = bottom;
                Top = top;
            }

            public double Left { get; }
            public double Right { get; }
            public double Bottom { get; }
            public double Top { get; }

            /// <summary>True when the frustum is centred on the view axis (the camera's own fieldOfView can draw it).</summary>
            public bool Symmetric => Math.Abs(Top + Bottom) < 1e-9 && Math.Abs(Right + Left) < 1e-9;

            public double VerticalDegrees => (Math.Atan(Top) - Math.Atan(Bottom)) * 180d / Math.PI;

            public double HorizontalDegrees => (Math.Atan(Right) - Math.Atan(Left)) * 180d / Math.PI;
        }

        /// <summary>
        /// The frustum for a perspective camera authored at <paramref name="referenceVerticalFovDegrees"/> on 9:16, drawn
        /// on a screen <paramref name="aspect"/> wide-per-tall. Taller than 9:16: the horizontal view is the authored one
        /// and the extra height is added, <paramref name="extraBelow"/> of it under the authored frame (1 = all at the
        /// bottom, so the top edge and horizon stay put; 0.5 = split evenly). 9:16 or wider: the authored vertical view.
        /// </summary>
        public static Frustum Fit(float referenceVerticalFovDegrees, float aspect, float extraBelow = 0.5f)
        {
            var half = Math.Tan(referenceVerticalFovDegrees * Math.PI / 360d);
            if (!(aspect > 0f) || aspect >= ReferenceAspect)
            {
                var a = aspect > 0f ? aspect : ReferenceAspect;
                return new Frustum(-half * a, half * a, -half, half);
            }

            var clamped = Math.Max(aspect, MinAspect);
            var tanH = half * ReferenceAspect;
            // Width shows the authored view; the height is whatever the screen's shape gives it (up to the 9:21 cap).
            var right = tanH * aspect / clamped;
            var total = 2d * right / aspect;
            var extra = total - (2d * half);
            var below = Math.Min(1d, Math.Max(0d, extraBelow));
            return new Frustum(-right, right, -half - (extra * below), half + (extra * (1d - below)));
        }

        /// <summary>Total vertical field of view (degrees) of the centred fit: what a camera's fieldOfView gets set to.</summary>
        public static float VerticalFovDegrees(float referenceVerticalFovDegrees, float aspect) =>
            (float)Fit(referenceVerticalFovDegrees, aspect, 0.5f).VerticalDegrees;

        /// <summary>Horizontal field of view (degrees) for a vertical view on a screen <paramref name="aspect"/> wide-per-tall.</summary>
        public static double HorizontalFovDegrees(double verticalFovDegrees, double aspect) =>
            2d * Math.Atan(Math.Tan(verticalFovDegrees * Math.PI / 360d) * aspect) * 180d / Math.PI;

        /// <summary>
        /// CanvasScaler match (0 = width, 1 = height). Taller than 9:16 the canvas follows the width, so a button or a
        /// line of text is the same share of the screen's width as on the 9:16 Game view; 9:16 or wider keeps the
        /// authored height match (identical on the reference view, where both give the same scale).
        /// </summary>
        public static float CanvasMatch(float aspect) => aspect > 0f && aspect < ReferenceAspect ? 0f : 1f;

        /// <summary>
        /// The authored 9:16 frame's height as a share of the safe area's height: 1 at 9:16 or wider, less than 1 on a
        /// taller screen (0.82 on 19.5:9), where the frame is the safe width tall over 9:16.
        /// </summary>
        public static float FrameShare(float safeWidth, float safeHeight)
        {
            if (safeWidth <= 0f || safeHeight <= 0f)
            {
                return 1f;
            }

            return Math.Min(1f, safeWidth / ReferenceAspect / safeHeight);
        }

        /// <summary>
        /// Where an authored HUD box's vertical anchors go on this screen, both in 0..1 of the parent. Authored on the
        /// 9:16 frame; on a taller safe area the frame is <paramref name="share"/> of it tall, and each box keeps its
        /// shape: top boxes hang from the top, bottom boxes stand on the bottom, mid-screen ones stay centred, and a
        /// box taller than half the screen pins each edge to its own side (so a sheet grows into the extra height).
        /// The result is in safe-area space; <see cref="ToScreen"/> maps it into the parent.
        /// </summary>
        public static (float Y0, float Y1) PinY(float y0, float y1, float share)
        {
            if (share >= 1f)
            {
                return (y0, y1);
            }

            if (y1 - y0 >= 0.5f)
            {
                return (PinEdge(y0, share), PinEdge(y1, share));
            }

            var c = (y0 + y1) * 0.5f;
            if (c >= 0.6f)
            {
                return (Top(y0, share), Top(y1, share));
            }

            if (c <= 0.4f)
            {
                return (y0 * share, y1 * share);
            }

            return (0.5f + ((y0 - 0.5f) * share), 0.5f + ((y1 - 0.5f) * share));
        }

        /// <summary>A 0..1 place in the safe area to 0..1 of the full screen (safe area given in 0..1 of the screen).</summary>
        public static float ToScreen(float v, float safeMin, float safeMax) => safeMin + (v * (safeMax - safeMin));

        private static float PinEdge(float y, float share) => y < 0.5f ? y * share : Top(y, share);

        private static float Top(float y, float share) => 1f - ((1f - y) * share);
    }
}
