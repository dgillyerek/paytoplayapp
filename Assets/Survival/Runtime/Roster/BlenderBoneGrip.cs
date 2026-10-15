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
        public BlenderBoneGrip(float[] gripPos, float[] gripRot, string childA, float[] childAHead, string childB, float[] childBHead, float[] handCentre)
        {
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
