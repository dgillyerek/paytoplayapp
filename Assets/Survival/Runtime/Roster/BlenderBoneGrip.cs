using System;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// A prop held at a fixed offset in a bone's own space, taken straight from Blender (pose-bone space: y along the bone).
    /// Unity's FBX import keeps each bone's local axes but flips handedness by negating local X, so a Blender bone-space
    /// point (x, y, z) is (-x, y, z) in the imported bone and a rotation quaternion (x, y, z, w) becomes (x, -y, -z, w).
    /// Two child bone heads recorded in the same Blender bone space let the runtime confirm that mapping and recover the
    /// import scale from the real imported skeleton instead of a world-space fit.
    /// </summary>
    public sealed class BlenderBoneGrip
    {
        public BlenderBoneGrip(float[] gripPos, float[] gripRot, string childA, float[] childAHead, string childB, float[] childBHead, float[] handCentre,
            float rollRestDeg = 0f, float rollDrawDeg = 0f, float[]? rollKeys = null)
        {
            RollRestDeg = rollRestDeg;
            RollDrawDeg = rollDrawDeg;
            RollKeys = rollKeys ?? new[] { 0f, 0f, 0f, 0f };
            GripPos = gripPos ?? throw new ArgumentNullException(nameof(gripPos));
            GripRot = gripRot ?? throw new ArgumentNullException(nameof(gripRot));
            ChildA = childA;
            ChildAHead = childAHead;
            ChildB = childB;
            ChildBHead = childBHead;
            HandCentre = handCentre;
        }

        /// <summary>Grip centre (prop pivot) in Blender bone space, metres.</summary>
        public float[] GripPos { get; }

        /// <summary>Prop rotation in Blender bone space, quaternion x, y, z, w.</summary>
        public float[] GripRot { get; }

        public string ChildA { get; }
        public float[] ChildAHead { get; }
        public string ChildB { get; }
        public float[] ChildBHead { get; }

        /// <summary>Centre of the closed hand around the grip axis, Blender bone space (worst sample over rest, walk and attack).</summary>
        public float[] HandCentre { get; }

        /// <summary>Roll about the prop's long axis (Blender prop-local z, after <see cref="GripRot"/>) in rest and walk.</summary>
        public float RollRestDeg { get; }

        /// <summary>Roll held in the attack from <see cref="RollKeys"/>[1] to [2] (nock to release).</summary>
        public float RollDrawDeg { get; }

        /// <summary>Attack frames: start easing to the draw roll, reach it, leave it, back to the rest roll (smoothstep eases).</summary>
        public float[] RollKeys { get; }

        public float RollDeg(bool attack, float frame)
        {
            float a = RollKeys[0], b = RollKeys[1], c = RollKeys[2], e = RollKeys[3];
            if (!attack || frame <= a || frame >= e)
            {
                return RollRestDeg;
            }

            if (frame < b)
            {
                return RollRestDeg + (RollDrawDeg - RollRestDeg) * Smooth((frame - a) / (b - a));
            }

            if (frame <= c)
            {
                return RollDrawDeg;
            }

            return RollDrawDeg + (RollRestDeg - RollDrawDeg) * Smooth((frame - c) / (e - c));
        }

        private static float Smooth(float x)
        {
            x = Math.Max(0f, Math.Min(1f, x));
            return x * x * (3f - 2f * x);
        }

        /// <summary>Blender bone-space prop rotation for a roll: GripRot * Rz(roll), quaternion x, y, z, w.</summary>
        public float[] BlenderRotation(float rollDeg)
        {
            var h = rollDeg * Math.PI / 360.0;
            float rz = (float)Math.Sin(h), rw = (float)Math.Cos(h);
            float x = GripRot[0], y = GripRot[1], z = GripRot[2], w = GripRot[3];
            // (x,y,z,w) * (0,0,rz,rw)
            return new[] { x * rw + y * rz, y * rw - x * rz, z * rw + w * rz, w * rw - z * rz };
        }

        /// <summary>Unity bone-space prop rotation for a roll (mirror-X of <see cref="BlenderRotation"/>).</summary>
        public float[] UnityRotation(float rollDeg) => ToUnityRotation(BlenderRotation(rollDeg));

        public static float[] ToUnityPoint(float[] p) => new[] { -p[0], p[1], p[2] };

        public static float[] ToUnityRotation(float[] q) => new[] { q[0], -q[1], -q[2], q[3] };

        /// <summary>Imported-bone-space grip position, in the imported bone's units.</summary>
        public float[] UnityGripPos(float unitsPerMetre)
        {
            var p = ToUnityPoint(GripPos);
            return new[] { p[0] * unitsPerMetre, p[1] * unitsPerMetre, p[2] * unitsPerMetre };
        }

        public float[] UnityGripRot() => ToUnityRotation(GripRot);

        /// <summary>Distance (metres) between the grip centre and the closed-hand centre after the Unity axis conversion.</summary>
        public float GripToHandCentreUnity()
        {
            var a = ToUnityPoint(GripPos);
            var b = ToUnityPoint(HandCentre);
            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            var dz = a[2] - b[2];
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
