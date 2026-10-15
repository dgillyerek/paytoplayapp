using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan A-pose rebuild bow attack (2026-10-14 v2, Derek: an arm came through the chest on the raise; bow through the hand):
    /// both hands follow keyed waypoints in the turning torso frame so the 70 deg chest turn carries the arms. Draw arm: f3 elbow leads
    /// out to the side, f6 up outside the shoulder line, f8 hand in at head height, f10 full draw at the right jaw with the wrist flexed
    /// so the fingers point along the arrow (elbow out to the side, just above the shoulder). Bow arm: out to the side first, then
    /// straight from the shoulder (its roll solved so the bow stands within 13 deg of vertical). f14 loose (arrow flies nock to grip at
    /// 14 m/s with a hero-blue trail), f17 follow-through, out the same way, f30 rest. Arm vs chest/coat penetration measured on every
    /// frame and half-frame (design .../work/apose_20261014/pen.json): 0. Arm roll fixed: after the aim the full Arm/ForeArm basis maps the rest elbow hinge onto the IK hinge (explicit pole
    /// roll) and RightHand/LeftHand are keyed every frame; every attack frame checked for forearm flip (forearm twist 0).
    /// One bow only, its grip closed in the left hand on real finger bones (LeftHandFingers1-3 + LeftHandThumb1-2 curled 15/35/20 and 60/20 deg, fingers4.py; original Meshy hand, no sculpt) in every clip; nothing on his back. Feet planted, Head faces forward every frame.
    /// Frames baked in Unity convention (character forward = +Z) by design .../fresh_20261013/work/apose_20261014/sheet.py.
    /// 2026-10-15 (Derek): the left foot steps toward the target as he fires: lifts f2, plants about 29 cm forward at f8 (before the f14
    /// release) with the hips/weight shifting onto it (f4-f11), holds through the release, returns to rest by f26; both feet are 2-bone
    /// IK on fixed world targets (no sliding), f0 and f30 are the rest pose.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_blenderig_attack.fbx";
        public const string FileMd5 = "0c3fc1a607792ec2418990d8ea1e272b";
        public const string DesignDir = RowanMotion.DesignDir;
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs modelled in Blender for the fresh rebuild (design .../fresh_20261013/props).</summary>
        public static readonly string[] PropFileNames =
        {
            "ROWAN_bow.fbx",
            "ROWAN_arrow.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "a147b7d3ab59893e9b563decc1241dac",
            "079abe232bead0a8fee5d019441fe4d5"
        };

        /// <summary>Bow grip in mixamorig:LeftHand bone space from Blender (design work/apose_20261014/grip_unity.json; same fix as Bonequill #63):
        /// pivot at the grip centre on the hand centreline, bow axis along the knuckle line. Unity parents the bow to the hand bone at this offset
        /// (mirror-X bone-space conversion) instead of fitting a world pose. Rolled about the long axis per frame (work/apose_20261014/bowroll.py):
        /// string away from the body when carried (230 deg), turned toward him from nock to release (40 deg, eased f2-8, held to f17, back by f24); clearance in
        /// work/apose_20261014/string_clearance.json.</summary>
        public static readonly BlenderBoneGrip BowGrip = new BlenderBoneGrip(
            new[] { 0.03200f, 0.12800f, 0.00866f },
            new[] { 0f, 0f, 1f, 0f },   // grip axis flip (180 deg about bone z); the roll about the bow's long axis is per frame below
            "mixamorig:LeftHandFingers1",
            new[] { -0.02264f, 0.13500f, 0.00866f },
            "mixamorig:LeftHandThumb1",
            new[] { 0.02908f, 0.06549f, 0.05844f },
            new[] { 0.02421f, 0.12533f, 0.00866f },
            230f,   // rest/walk carry: string on the side away from the body and coat
            40f,    // nock to release: string toward him
            new[] { 2f, 8f, 17f, 24f });

        /// <summary>The single bow, a child of LeftHand at <see cref="BowGrip"/> in rest, walk and attack. Stands in for the attack's ROWAN_bow track.</summary>
        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "ROWAN_bow_rest",
                "ROWAN_bow.fbx",
                "a147b7d3ab59893e9b563decc1241dac",
                "mixamorig:LeftHand",
                new[] { -0.35101f, 0.85809f, 0.09459f, 0.54675f, 0.00535f, -0.03011f, 0.83674f },
                new[] { 0.048f, 1.3323f, 0.1643f },
                "Single wooden bow, grip closed in the left-hand finger-bone grip, in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's ROWAN_bow track so one bow serves every clip.",
                "ROWAN_bow",
                BowGrip)
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Rowan",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0f, 0.96f, -0.02f, 0f, 1.555f, -0.01f, -0.452f, 1.005f, 0.025f, 0.452f, 1.005f, 0.025f },
            new[]
            {
                new AttackTrack(
                    "ROWAN_bow",
                    AttackTrackKind.Held,
                    "ROWAN_bow.fbx",
                    "mixamorig:LeftHand",
                    10,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.048f, 1.3323f, 0.1643f },
                    new[]
                    {
                        new AttackFrame(-0.35101f, 0.85809f, 0.09459f, 0.54675f, 0.00535f, -0.03011f, 0.83674f, 1f, true),
                        new AttackFrame(-0.406f, 0.87787f, 0.11465f, 0.54016f, -0.02484f, -0.06857f, 0.8384f, 1f, true),
                        new AttackFrame(-0.50779f, 0.94715f, 0.19625f, 0.4925f, -0.0884f, -0.14789f, 0.85309f, 1f, true),
                        new AttackFrame(-0.53485f, 1.06149f, 0.34589f, 0.3521f, -0.08328f, -0.23542f, 0.90204f, 1f, true),
                        new AttackFrame(-0.45552f, 1.11976f, 0.48935f, 0.26278f, -0.00891f, -0.28841f, 0.9207f, 1f, true),
                        new AttackFrame(-0.30902f, 1.16732f, 0.63038f, 0.1721f, 0.07796f, -0.29602f, 0.93631f, 1f, true),
                        new AttackFrame(-0.14287f, 1.28414f, 0.75326f, 0.03159f, 0.12405f, -0.2152f, 0.96814f, 1f, true),
                        new AttackFrame(-0.03262f, 1.41201f, 0.80141f, -0.09698f, 0.16394f, -0.09172f, 0.9774f, 1f, true),
                        new AttackFrame(-0.00719f, 1.46667f, 0.82046f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.00953f, 1.46081f, 0.8439f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01125f, 1.45652f, 0.86105f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.04092f, 1.42617f, 0.87127f, -0.12645f, 0.16169f, -0.06403f, 0.97661f, 1f, true),
                        new AttackFrame(-0.11897f, 1.35273f, 0.86827f, -0.05247f, 0.11808f, -0.13104f, 0.98292f, 1f, true),
                        new AttackFrame(-0.22426f, 1.26137f, 0.82708f, 0.04204f, 0.07673f, -0.2134f, 0.97304f, 1f, true),
                        new AttackFrame(-0.33005f, 1.17844f, 0.73874f, 0.13991f, 0.04823f, -0.27351f, 0.95042f, 1f, true),
                        new AttackFrame(-0.41994f, 1.12202f, 0.62607f, 0.22519f, 0.01506f, -0.29158f, 0.92954f, 1f, true),
                        new AttackFrame(-0.49109f, 1.10476f, 0.52129f, 0.27127f, -0.03637f, -0.28044f, 0.92002f, 1f, true),
                        new AttackFrame(-0.53955f, 1.08915f, 0.42297f, 0.30997f, -0.0856f, -0.25509f, 0.91188f, 1f, true),
                        new AttackFrame(-0.5526f, 1.04188f, 0.31967f, 0.37485f, -0.10541f, -0.21705f, 0.89513f, 1f, true),
                        new AttackFrame(-0.5381f, 0.97421f, 0.22217f, 0.47041f, -0.11678f, -0.16413f, 0.85915f, 1f, true),
                        new AttackFrame(-0.48361f, 0.92199f, 0.15618f, 0.51352f, -0.07804f, -0.1193f, 0.84615f, 1f, true),
                        new AttackFrame(-0.4214f, 0.88445f, 0.11794f, 0.53721f, -0.03688f, -0.07619f, 0.83919f, 1f, true),
                        new AttackFrame(-0.37121f, 0.86425f, 0.09957f, 0.54547f, -0.00636f, -0.04319f, 0.83699f, 1f, true),
                        new AttackFrame(-0.35101f, 0.85809f, 0.09459f, 0.54675f, 0.00535f, -0.03011f, 0.83674f, 1f, true)
                    },
                    "single wooden bow, grip closed in the finger-bone grip (grip.json) in every frame; the RowanMotion idle bow stands in for it"),
                new AttackTrack(
                    "ROWAN_arrow",
                    AttackTrackKind.Held,
                    "ROWAN_arrow.fbx",
                    "mixamorig:RightHand",
                    10,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.033f, 0.0381f, 0.795f },
                    new[]
                    {
                        new AttackFrame(0.38157f, 0.92744f, 0.05665f, 0.0342f, 0.43485f, -0.70361f, 0.56095f, 1f, false),
                        new AttackFrame(0.46168f, 0.96445f, 0.02314f, -0.00217f, 0.4477f, -0.65086f, 0.61314f, 1f, false),
                        new AttackFrame(0.548f, 1.05105f, -0.0811f, -0.09514f, 0.44641f, -0.59559f, 0.66101f, 1f, false),
                        new AttackFrame(0.54853f, 1.12583f, -0.21241f, -0.216f, 0.44428f, -0.56215f, 0.66328f, 1f, false),
                        new AttackFrame(0.47625f, 1.21861f, -0.31055f, -0.26378f, 0.36901f, -0.53241f, 0.7147f, 1f, false),
                        new AttackFrame(0.37085f, 1.35587f, -0.31388f, -0.23068f, 0.18359f, -0.45548f, 0.84001f, 1f, false),
                        new AttackFrame(0.29953f, 1.43647f, -0.28752f, -0.20836f, 0.05553f, -0.32954f, 0.91919f, 1f, false),
                        new AttackFrame(0.26707f, 1.51099f, -0.18161f, -0.05248f, -0.11943f, -0.20408f, 0.97022f, 1f, false),
                        new AttackFrame(0.25669f, 1.53845f, -0.06369f, 0.09708f, -0.17207f, -0.13647f, 0.97074f, 1f, true),
                        new AttackFrame(0.22059f, 1.52803f, 0.03826f, 0.09586f, -0.22082f, -0.04231f, 0.96967f, 1f, true),
                        new AttackFrame(0.18605f, 1.5028f, 0.12815f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.20829f, 1.50794f, 0.12426f, -0.02483f, -0.11629f, -0.05177f, 0.99155f, 1f, false),
                        new AttackFrame(0.2397f, 1.51772f, 0.10944f, -0.10284f, -0.10417f, -0.11165f, 0.98291f, 1f, false),
                        new AttackFrame(0.25585f, 1.52258f, 0.08664f, -0.14108f, -0.10358f, -0.12893f, 0.97609f, 1f, false),
                        new AttackFrame(0.25438f, 1.52147f, 0.04902f, -0.06688f, -0.20081f, -0.11574f, 0.97047f, 1f, false),
                        new AttackFrame(0.26015f, 1.51539f, -0.01011f, 0.03455f, -0.24275f, -0.14609f, 0.9584f, 1f, false),
                        new AttackFrame(0.28101f, 1.50161f, -0.03688f, 0.04976f, -0.21978f, -0.19456f, 0.95465f, 1f, false),
                        new AttackFrame(0.30766f, 1.47834f, -0.05669f, -0.00332f, -0.23089f, -0.2455f, 0.94149f, 1f, false),
                        new AttackFrame(0.35249f, 1.42416f, -0.09639f, -0.09092f, -0.15572f, -0.36188f, 0.91462f, 1f, false),
                        new AttackFrame(0.40283f, 1.36781f, -0.11263f, -0.08493f, -0.04149f, -0.47491f, 0.87494f, 1f, false),
                        new AttackFrame(0.4644f, 1.30119f, -0.119f, -0.08414f, 0.08453f, -0.54618f, 0.82914f, 1f, false),
                        new AttackFrame(0.54592f, 1.19835f, -0.1259f, -0.16106f, 0.27471f, -0.57135f, 0.75641f, 1f, false),
                        new AttackFrame(0.5757f, 1.12454f, -0.09154f, -0.15733f, 0.37407f, -0.5813f, 0.70527f, 1f, false),
                        new AttackFrame(0.56378f, 1.07657f, -0.03692f, -0.08791f, 0.39131f, -0.59573f, 0.69588f, 1f, false),
                        new AttackFrame(0.50734f, 1.01049f, 0.01608f, -0.02216f, 0.39721f, -0.63633f, 0.66092f, 1f, false),
                        new AttackFrame(0.43456f, 0.95005f, 0.04703f, 0.01768f, 0.43894f, -0.66738f, 0.60135f, 1f, false),
                        new AttackFrame(0.38157f, 0.92744f, 0.05665f, 0.0342f, 0.43485f, -0.70361f, 0.56095f, 1f, false)
                    },
                    "arrow on the string from f8, drawn to the jaw by RightHand at f10, held to f13"),
                new AttackTrack(
                    "ROWAN_arrow_inflight",
                    AttackTrackKind.Projectile,
                    "ROWAN_arrow.fbx",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.033f, 0.0381f, 0.795f },
                    new[]
                    {
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.0643f, 1.47274f, 0.58459f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.05678f, 1.44434f, 1.03438f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.17787f, 1.41593f, 1.48416f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.29895f, 1.38753f, 1.93395f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.42004f, 1.35913f, 2.38374f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.54112f, 1.33072f, 2.83353f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.6622f, 1.30232f, 3.28332f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.78329f, 1.27392f, 3.73311f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.90437f, 1.24552f, 4.18289f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.02546f, 1.21711f, 4.63268f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.14654f, 1.18871f, 5.08247f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.26763f, 1.16031f, 5.53226f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.38871f, 1.1319f, 5.98205f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.5098f, 1.1035f, 6.43184f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.63088f, 1.0751f, 6.88162f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.75197f, 1.04669f, 7.33141f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.87305f, 1.01829f, 7.7812f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true)
                    },
                    "loosed at f14 along nock to grip (character-forward), 14 m/s"),
                new AttackTrack(
                    "ROWAN_arrow_trail_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.Trail,
                    0.04f,
                    0.8f,
                    new[] { 0.62f, 0.78f, 1f },
                    new[] { 0.25f, 0.45f, 0.85f },
                    new[] { 0.62f, 0.78f, 1f },
                    new[] { 0.03f, 0.03f, 0.8f },
                    new[]
                    {
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.18539f, 1.50114f, 0.1348f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.0643f, 1.47274f, 0.58459f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.05678f, 1.44434f, 1.03438f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.17787f, 1.41593f, 1.48416f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.29895f, 1.38753f, 1.93395f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.42004f, 1.35913f, 2.38374f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.54112f, 1.33072f, 2.83353f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.6622f, 1.30232f, 3.28332f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.78329f, 1.27392f, 3.73311f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.90437f, 1.24552f, 4.18289f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.02546f, 1.21711f, 4.63268f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.14654f, 1.18871f, 5.08247f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.26763f, 1.16031f, 5.53226f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.38871f, 1.1319f, 5.98205f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.5098f, 1.1035f, 6.43184f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.63088f, 1.0751f, 6.88162f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.75197f, 1.04669f, 7.33141f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.87305f, 1.01829f, 7.7812f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true)
                    },
                    "hero-blue trail behind the arrow")
            });
    }
}
