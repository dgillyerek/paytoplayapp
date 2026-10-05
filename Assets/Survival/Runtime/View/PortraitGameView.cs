namespace Survival.Domain.View
{
    /// <summary>1080×1920 Game-view framing shared by the working Theme A roster scenes.</summary>
    public static class PortraitGameView
    {
        public const float Width = 1080f;
        public const float Height = 1920f;
        public const float FovDegrees = 54f;
        public const float Near = 0.25f;
        public const float Far = 4000f;
        public const float PitchMin = -25f;
        public const float PitchMax = 35f;
        public const float ThreeQuarterYawDegrees = 45f;
        public const float RearYawDegrees = 180f;

        /// <summary>
        /// Distance that fits the world bounds in a portrait 54° view.
        /// Uniform. Does not resize the mesh.
        /// </summary>
        public static float FrameDistance(float sizeX, float sizeY, float sizeZ)
        {
            var halfV = FovDegrees * System.Math.PI / 360d;
            var tanV = System.Math.Tan(halfV);
            var tanH = tanV * (Width / Height);
            var distY = (sizeY * 0.5d) / tanV;
            var distX = (sizeX * 0.5d) / tanH;
            var distZ = (sizeZ * 0.5d) / tanH;
            var dist = System.Math.Max(distY, System.Math.Max(distX, distZ));
            if (dist < 1.5d)
            {
                dist = 1.5d;
            }

            return (float)(dist * 1.35d);
        }

        public static void OrbitEye(
            float lookX,
            float lookY,
            float lookZ,
            float distance,
            float yawDegrees,
            float pitchDegrees,
            out float x,
            out float y,
            out float z)
        {
            var oy = distance * 0.08d;
            var oz = distance;
            var yaw = yawDegrees * (System.Math.PI / 180d);
            var cos = System.Math.Cos(yaw);
            var sin = System.Math.Sin(yaw);
            var rx = oz * sin;
            var rz = oz * cos;
            var pitch = pitchDegrees * (System.Math.PI / 180d);
            var pc = System.Math.Cos(pitch);
            var ps = System.Math.Sin(pitch);
            var horiz = System.Math.Sqrt((rx * rx) + (rz * rz));
            var ny = (oy * pc) - (horiz * ps);
            var nh = (oy * ps) + (horiz * pc);
            if (horiz > 1e-5d)
            {
                rx *= nh / horiz;
                rz *= nh / horiz;
            }

            x = (float)(lookX + rx);
            y = (float)(lookY + ny);
            z = (float)(lookZ + rz);
        }
    }
}
