using System;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Connected vertex bend of the ONE locked rear sprite.
    /// Weights fall off to zero at hip/hilt so the silhouette never splits into floating limbs.
    /// </summary>
    public static class SirAldricWarp
    {
        public const int GridCols = 28;
        public const int GridRows = 36;
        public const float LegRadius = 0.16f;
        public const float SwordRadius = 0.085f;
        public const float TorsoRadius = 0.42f;

        public static void Displace(float u, float v, SirAldricMotion.Pose pose, out float nu, out float nv)
        {
            var x = u;
            var y = v;

            Bend(ref x, ref y, u, v, SirAldricMotion.Layout.HipL, SirAldricMotion.Layout.FootL, pose.LegL.RotZ, pose.LegL.X, pose.LegL.Y, LegRadius, alongFromHip: true);
            Bend(ref x, ref y, u, v, SirAldricMotion.Layout.HipR, SirAldricMotion.Layout.FootR, pose.LegR.RotZ, pose.LegR.X, pose.LegR.Y, LegRadius, alongFromHip: true);

            var torsoW = TorsoWeight(u, v);
            RotateAbout(ref x, ref y, u, v, SirAldricMotion.Layout.Spine.X, SirAldricMotion.Layout.Spine.Y, pose.Torso.RotZ, torsoW);
            x += pose.Torso.X * torsoW;
            y += pose.Torso.Y * torsoW;

            Bend(ref x, ref y, u, v, SirAldricMotion.Layout.Hilt, SirAldricMotion.Layout.Tip, pose.Sword.RotZ, pose.Sword.X, pose.Sword.Y, SwordRadius, alongFromHip: false);

            nu = x;
            nv = y;
        }

        private static float TorsoWeight(float u, float v)
        {
            var rise = SirAldricMotion.Smooth01((v - 0.38f) / 0.16f);
            var head = 1f - SirAldricMotion.Smooth01((v - 0.86f) / 0.12f);
            var mid = 1f - SirAldricMotion.Smooth01((Math.Abs(u - 0.50f) - 0.12f) / TorsoRadius);
            return rise * head * mid;
        }

        private static void Bend(
            ref float x,
            ref float y,
            float u,
            float v,
            SirAldricMotion.NPoint pivot,
            SirAldricMotion.NPoint tip,
            float rotDeg,
            float tx,
            float ty,
            float radius,
            bool alongFromHip)
        {
            BoneWeights(u, v, pivot, tip, radius, out var along, out var radial);
            var w = (alongFromHip ? along : 1f) * radial;
            if (w <= 1e-5f)
            {
                return;
            }

            RotateAbout(ref x, ref y, u, v, pivot.X, pivot.Y, rotDeg, w);
            x += tx * w;
            y += ty * w;
        }

        public static void BoneWeights(
            float u,
            float v,
            SirAldricMotion.NPoint pivot,
            SirAldricMotion.NPoint tip,
            float radius,
            out float along,
            out float radial)
        {
            var dx = tip.X - pivot.X;
            var dy = tip.Y - pivot.Y;
            var len2 = dx * dx + dy * dy;
            if (len2 < 1e-8f)
            {
                along = 0f;
                radial = 0f;
                return;
            }

            var t = ((u - pivot.X) * dx + (v - pivot.Y) * dy) / len2;
            along = SirAldricMotion.Clamp01(t);
            var px = pivot.X + dx * along;
            var py = pivot.Y + dy * along;
            var dist = Hypot(u - px, v - py);
            radial = 1f - SirAldricMotion.Smooth01((dist - 0.015f) / radius);
        }

        private static void RotateAbout(
            ref float x,
            ref float y,
            float u,
            float v,
            float px,
            float py,
            float rotDeg,
            float weight)
        {
            if (Math.Abs(rotDeg) < 1e-4f || weight <= 1e-5f)
            {
                return;
            }

            var rad = rotDeg * (MathF.PI / 180f);
            var c = MathF.Cos(rad);
            var s = MathF.Sin(rad);
            var rx = u - px;
            var ry = v - py;
            var qx = px + rx * c - ry * s;
            var qy = py + rx * s + ry * c;
            x += (qx - u) * weight;
            y += (qy - v) * weight;
        }

        private static float Hypot(float a, float b) => MathF.Sqrt(a * a + b * b);
    }
}
