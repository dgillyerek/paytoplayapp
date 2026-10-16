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
        public const string FileMd5 = "6d146135e121a8522c23f31b53aa6e9a";
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
        /// rest/walk carry (Derek 2026-10-15, v5d): the string rests on the INSIDE (body side) of the forearm, ~0.7 cm off the skin (no
        /// penetration), between his forearm and his body/leg, the limb further out, string side tilted up; the bow runs fore-aft beside the leg
        /// (-45 deg; anim.py CARRY solves the hand from the bow, the forearm from the hand with no wrist bend). Attack: eases -45 -> -310
        /// (= 50, string toward him) over f1-f11 the long way round (the short way sweeps the string through the wrist), held through the f14
        /// release to f17, back to -45 by f27; the carried bow swings out as the arm leaves the carry so it clears his coat and leg.
        /// Checks: work/apose_20261014/carry_check.json, string_clearance.json.</summary>
        public static readonly BlenderBoneGrip BowGrip = new BlenderBoneGrip(
            new[] { 0.03200f, 0.12800f, 0.00866f },
            new[] { 0f, 0f, 1f, 0f },   // grip axis flip (180 deg about bone z); the roll about the bow's long axis is per frame below
            "mixamorig:LeftHandFingers1",
            new[] { -0.02264f, 0.13500f, 0.00866f },
            "mixamorig:LeftHandThumb1",
            new[] { 0.02908f, 0.06549f, 0.05844f },
            new[] { 0.02421f, 0.12533f, 0.00866f },
            -45f,   // rest/walk carry: string on the body side of the forearm, ~0.7 cm off the skin, limb further out (-45 also keeps the grip corners clear of the fingers)
            -310f,  // nock to release: string toward him (= 50 deg; reached the long way round so the string never crosses the wrist)
            new[] { 1f, 11f, 17f, 27f });

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
                        new AttackFrame(-0.47715f, 0.88407f, 0.02797f, -0.60559f, -0.46548f, 0.58323f, 0.27645f, 1f, true),
                        new AttackFrame(-0.53381f, 0.91604f, 0.03671f, -0.64843f, -0.43958f, 0.55715f, 0.2755f, 1f, true),
                        new AttackFrame(-0.64312f, 1.00872f, 0.05663f, -0.77613f, -0.35136f, 0.44793f, 0.27116f, 1f, true),
                        new AttackFrame(-0.70105f, 1.09916f, 0.08367f, -0.90983f, -0.25856f, 0.24283f, 0.21537f, 1f, true),
                        new AttackFrame(-0.64355f, 1.33671f, 0.27289f, -0.5062f, 0.46688f, -0.00221f, 0.72511f, 1f, true),
                        new AttackFrame(-0.52045f, 1.54868f, 0.55062f, -0.30008f, 0.81581f, -0.16266f, 0.46685f, 1f, true),
                        new AttackFrame(-0.36362f, 1.57073f, 0.68175f, -0.14053f, 0.9605f, -0.2284f, 0.07428f, 1f, true),
                        new AttackFrame(-0.22842f, 1.56814f, 0.76251f, 0.04122f, -0.90517f, 0.25723f, 0.33587f, 1f, true),
                        new AttackFrame(-0.12131f, 1.53622f, 0.81128f, -0.06677f, -0.71735f, 0.21328f, 0.6599f, 1f, true),
                        new AttackFrame(-0.04288f, 1.47989f, 0.84414f, -0.13413f, -0.48677f, 0.10168f, 0.85716f, 1f, true),
                        new AttackFrame(-0.01125f, 1.45652f, 0.86105f, -0.15229f, -0.30863f, 0.04074f, 0.93803f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.1546f, -0.24729f, 0.0308f, 0.95603f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.1546f, -0.24729f, 0.0308f, 0.95603f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.1546f, -0.24729f, 0.0308f, 0.95603f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.1546f, -0.24729f, 0.0308f, 0.95603f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.1546f, -0.24729f, 0.0308f, 0.95603f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.1546f, -0.24729f, 0.0308f, 0.95603f, 1f, true),
                        new AttackFrame(-0.01191f, 1.45486f, 0.86769f, -0.1546f, -0.24729f, 0.0308f, 0.95603f, 1f, true),
                        new AttackFrame(-0.04092f, 1.42617f, 0.87127f, -0.14166f, -0.32733f, 0.00459f, 0.93422f, 1f, true),
                        new AttackFrame(-0.11897f, 1.35273f, 0.86827f, -0.12298f, -0.52358f, -0.06929f, 0.8402f, 1f, true),
                        new AttackFrame(-0.22426f, 1.26137f, 0.82708f, -0.14688f, -0.73788f, -0.16042f, 0.63892f, 1f, true),
                        new AttackFrame(-0.31445f, 1.1621f, 0.73932f, -0.23817f, -0.86913f, -0.2407f, 0.36049f, 1f, true),
                        new AttackFrame(-0.39127f, 1.06271f, 0.60988f, -0.40213f, -0.87049f, -0.27608f, 0.06571f, 1f, true),
                        new AttackFrame(-0.48843f, 1.00191f, 0.43888f, 0.61292f, 0.73766f, 0.218f, 0.18073f, 1f, true),
                        new AttackFrame(-0.58681f, 1.21551f, 0.36844f, -0.10147f, 0.6059f, 0.05846f, 0.78688f, 1f, true),
                        new AttackFrame(-0.64306f, 1.27944f, 0.23968f, -0.26562f, 0.32088f, 0.1509f, 0.8965f, 1f, true),
                        new AttackFrame(-0.69424f, 1.27919f, 0.15869f, -0.39595f, 0.05618f, 0.31731f, 0.85987f, 1f, true),
                        new AttackFrame(-0.72887f, 1.11988f, -0.03825f, -0.85445f, -0.29462f, 0.39426f, 0.16634f, 1f, true),
                        new AttackFrame(-0.64858f, 1.00944f, 0.00671f, -0.74859f, -0.37516f, 0.48476f, 0.25272f, 1f, true),
                        new AttackFrame(-0.53375f, 0.91588f, 0.02596f, -0.6485f, -0.44043f, 0.55624f, 0.27579f, 1f, true),
                        new AttackFrame(-0.47715f, 0.88407f, 0.02797f, -0.60559f, -0.46548f, 0.58323f, 0.27645f, 1f, true)
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
