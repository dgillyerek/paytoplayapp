using System;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// A ThemePack prop that rides a named bone in rest and walk (Design NOTE: parented to the hand
    /// at rest and in the walk). Its Design rest pose (Blender rest_world, converted to Unity convention:
    /// character forward = +Z) is mapped onto the imported rig with the attack calibration and then bound
    /// to the bone by name at bind pose. Hidden while the attack add-on plays so no prop shows twice.
    /// </summary>
    public sealed class BlenderRigIdlePropSpec
    {
        public BlenderRigIdlePropSpec(
            string objectName,
            string propFileName,
            string propFileMd5,
            string bone,
            float[] designRest,
            float[] propExtent,
            string note,
            string attackTrackName = "")
        {
            ObjectName = objectName;
            PropFileName = propFileName;
            PropFileMd5 = propFileMd5;
            Bone = bone;
            DesignRest = designRest ?? Array.Empty<float>();
            PropExtent = propExtent ?? Array.Empty<float>();
            Note = note ?? string.Empty;
            AttackTrackName = attackTrackName ?? string.Empty;
        }

        public string ObjectName { get; }

        /// <summary>Prop FBX next to the character FBXs in the ThemePack.</summary>
        public string PropFileName { get; }

        public string PropFileMd5 { get; }

        /// <summary>Attach bone, bound by name.</summary>
        public string Bone { get; }

        /// <summary>Design rest pose in Unity convention: x, y, z, qx, qy, qz, qw.</summary>
        public float[] DesignRest { get; }

        /// <summary>Prop mesh extent at identity in Unity convention (x, y, z metres).</summary>
        public float[] PropExtent { get; }

        public string Note { get; }

        /// <summary>
        /// Attack held track this prop stands in for (same mesh, same bone, same rest grip). When set, the
        /// attack does not load a second copy: this prop stays on the bone through the attack, so one prop and
        /// one material serve rest, walk and attack. Empty: the prop hides while the attack plays.
        /// </summary>
        public string AttackTrackName { get; }

        public bool StandsInForAttackTrack => AttackTrackName.Length > 0;

        /// <summary>The attack-driver track used to load and size the prop (one rest frame, held on <see cref="Bone"/>).</summary>
        public AttackTrack ToTrack()
        {
            var r = DesignRest;
            var frame = new AttackFrame(r[0], r[1], r[2], r[3], r[4], r[5], r[6], 1f, true);
            return new AttackTrack(
                ObjectName,
                AttackTrackKind.Held,
                PropFileName,
                Bone,
                0,
                0,
                0f,
                AttackVfxShape.None,
                0f,
                0f,
                Array.Empty<float>(),
                Array.Empty<float>(),
                Array.Empty<float>(),
                PropExtent,
                new[] { frame },
                Note);
        }
    }
}
