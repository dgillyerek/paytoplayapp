using System;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// Attack VFX travel in actor space. +Z is the actor root forward (the lunge and the
    /// facing), not a head or jaw bone axis and not a sideways sweep.
    /// </summary>
    public static class AttackEmission
    {
        public const int SlashClawCount = 3;

        /// <summary>Lateral claw spacing as a fraction of the slash height. Constant, so it does not add sideways travel.</summary>
        public const float SlashLaneFraction = 0.22f;

        /// <summary>Spark cone half-angle. Particles stay inside this of actor forward for their whole lifetime.</summary>
        public const float SlashConeHalfAngleDegrees = 4f;

        public const float MaxTravelAngleDegrees = 5f;

        public static float Progress(float frame, float first, float last)
        {
            var span = last - first;
            if (span < 1f)
            {
                span = 1f;
            }

            var p = (frame - first) / span;
            if (p < 0f)
            {
                return 0f;
            }

            if (p > 1f)
            {
                return 1f;
            }

            return p;
        }

        /// <summary>
        /// One claw tip in actor space: <paramref name="right"/> and <paramref name="up"/> are a
        /// fixed lane, <paramref name="forward"/> runs from the mouth out along root forward.
        /// </summary>
        public static void StraightAheadTip(
            int index,
            int count,
            float progress,
            float reach,
            float laneSpacing,
            out float right,
            out float up,
            out float forward)
        {
            if (count < 1)
            {
                count = 1;
            }

            if (index < 0)
            {
                index = 0;
            }

            if (index >= count)
            {
                index = count - 1;
            }

            var p = progress < 0f ? 0f : (progress > 1f ? 1f : progress);
            var mid = (count - 1) * 0.5f;
            right = (index - mid) * laneSpacing;
            up = 0f;
            forward = p * (reach < 0f ? 0f : reach);
        }

        public static float AngleFromForwardDegrees(float dx, float dy, float dz)
        {
            var len = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
            if (len < 1e-8)
            {
                return 0f;
            }

            var dot = dz / len;
            if (dot > 1.0)
            {
                dot = 1.0;
            }

            if (dot < -1.0)
            {
                dot = -1.0;
            }

            return (float)(Math.Acos(dot) * (180.0 / Math.PI));
        }

        /// <summary>
        /// Largest angle between a claw's step and the actor forward, sampled across the
        /// visible lifetime. <paramref name="actorYawDegrees"/> turns the actor so a
        /// world-sideways or bone-local path would not pass.
        /// </summary>
        public static float MaxSlashTravelAngleDegrees(
            float firstFrame,
            float lastFrame,
            float reach,
            float laneSpacing,
            int clawCount,
            float actorYawDegrees,
            int steps)
        {
            if (clawCount < 1)
            {
                clawCount = 1;
            }

            if (steps < 2)
            {
                steps = 2;
            }

            var yaw = actorYawDegrees * (Math.PI / 180.0);
            var sin = Math.Sin(yaw);
            var cos = Math.Cos(yaw);
            var max = 0.0;
            var prev = new double[clawCount * 3];
            var have = false;
            for (var s = 0; s <= steps; s++)
            {
                var frame = firstFrame + ((lastFrame - firstFrame) * (s / (double)steps));
                var p = Progress((float)frame, firstFrame, lastFrame);
                for (var i = 0; i < clawCount; i++)
                {
                    StraightAheadTip(i, clawCount, p, reach, laneSpacing, out var right, out var up, out var fwd);
                    var wx = (cos * right) + (sin * fwd);
                    var wy = (double)up;
                    var wz = (-sin * right) + (cos * fwd);
                    var k = i * 3;
                    if (have)
                    {
                        var dx = wx - prev[k];
                        var dy = wy - prev[k + 1];
                        var dz = wz - prev[k + 2];
                        var len = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                        if (len > 1e-8)
                        {
                            var dot = ((dx * sin) + (dz * cos)) / len;
                            if (dot > 1.0)
                            {
                                dot = 1.0;
                            }

                            if (dot < -1.0)
                            {
                                dot = -1.0;
                            }

                            var ang = Math.Acos(dot) * (180.0 / Math.PI);
                            if (ang > max)
                            {
                                max = ang;
                            }
                        }
                    }

                    prev[k] = wx;
                    prev[k + 1] = wy;
                    prev[k + 2] = wz;
                }

                have = true;
            }

            return (float)max;
        }
    }
}
