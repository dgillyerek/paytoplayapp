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
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_blenderig_attack.fbx";
        public const string FileMd5 = "0354387bfc00896a2a4fcf6f2efa4eec";
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

        /// <summary>The single bow, rigid on LeftHand in rest, walk and attack. Stands in for the attack's ROWAN_bow track.</summary>
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
                "ROWAN_bow")
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
                        new AttackFrame(-0.30835f, 1.16898f, 0.62373f, 0.1721f, 0.07796f, -0.29602f, 0.93631f, 1f, true),
                        new AttackFrame(-0.1405f, 1.29008f, 0.72947f, 0.03159f, 0.12405f, -0.2152f, 0.96814f, 1f, true),
                        new AttackFrame(-0.02789f, 1.42382f, 0.75418f, -0.09698f, 0.16394f, -0.09172f, 0.9774f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(9e-05f, 1.48486f, 0.74769f, -0.15313f, 0.17992f, -0.03743f, 0.97097f, 1f, true),
                        new AttackFrame(-0.02892f, 1.45617f, 0.75127f, -0.12645f, 0.16169f, -0.06403f, 0.97661f, 1f, true),
                        new AttackFrame(-0.10697f, 1.38273f, 0.74827f, -0.05247f, 0.11808f, -0.13104f, 0.98292f, 1f, true),
                        new AttackFrame(-0.21277f, 1.29008f, 0.71223f, 0.04204f, 0.07673f, -0.2134f, 0.97304f, 1f, true),
                        new AttackFrame(-0.31992f, 1.20375f, 0.63749f, 0.13991f, 0.04823f, -0.27351f, 0.95042f, 1f, true),
                        new AttackFrame(-0.41173f, 1.14253f, 0.54404f, 0.22519f, 0.01506f, -0.29158f, 0.92954f, 1f, true),
                        new AttackFrame(-0.48509f, 1.11976f, 0.46129f, 0.27127f, -0.03637f, -0.28044f, 0.92002f, 1f, true),
                        new AttackFrame(-0.53575f, 1.09864f, 0.385f, 0.30997f, -0.0856f, -0.25509f, 0.91188f, 1f, true),
                        new AttackFrame(-0.55072f, 1.04657f, 0.30092f, 0.37485f, -0.10541f, -0.21705f, 0.89513f, 1f, true),
                        new AttackFrame(-0.53759f, 0.9755f, 0.21702f, 0.47041f, -0.11678f, -0.16413f, 0.85915f, 1f, true),
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
                        new AttackFrame(0.37151f, 1.35753f, -0.32053f, -0.23068f, 0.18359f, -0.45548f, 0.84001f, 1f, false),
                        new AttackFrame(0.30191f, 1.44242f, -0.31131f, -0.20836f, 0.05553f, -0.32954f, 0.91919f, 1f, false),
                        new AttackFrame(0.27179f, 1.52279f, -0.22884f, -0.05248f, -0.11943f, -0.20407f, 0.97022f, 1f, false),
                        new AttackFrame(0.26397f, 1.55664f, -0.13646f, 0.09708f, -0.17207f, -0.13647f, 0.97074f, 1f, true),
                        new AttackFrame(0.23021f, 1.55208f, -0.05795f, 0.09586f, -0.22082f, -0.04231f, 0.96967f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.22029f, 1.53794f, 0.00426f, -0.02483f, -0.11629f, -0.05177f, 0.99155f, 1f, false),
                        new AttackFrame(0.2517f, 1.54772f, -0.01056f, -0.10284f, -0.10417f, -0.11165f, 0.98291f, 1f, false),
                        new AttackFrame(0.26785f, 1.55258f, -0.03336f, -0.14108f, -0.10358f, -0.12893f, 0.97609f, 1f, false),
                        new AttackFrame(0.26638f, 1.55147f, -0.07098f, -0.06688f, -0.20081f, -0.11574f, 0.97047f, 1f, false),
                        new AttackFrame(0.27215f, 1.54539f, -0.13011f, 0.03455f, -0.24275f, -0.14609f, 0.9584f, 1f, false),
                        new AttackFrame(0.29249f, 1.53032f, -0.15173f, 0.04976f, -0.21978f, -0.19456f, 0.95465f, 1f, false),
                        new AttackFrame(0.31779f, 1.50366f, -0.15794f, -0.00332f, -0.23089f, -0.2455f, 0.94149f, 1f, false),
                        new AttackFrame(0.36069f, 1.44467f, -0.17842f, -0.09092f, -0.15572f, -0.36188f, 0.91462f, 1f, false),
                        new AttackFrame(0.40883f, 1.38281f, -0.17263f, -0.08493f, -0.04149f, -0.47491f, 0.87494f, 1f, false),
                        new AttackFrame(0.4682f, 1.31068f, -0.15697f, -0.08414f, 0.08453f, -0.54618f, 0.82914f, 1f, false),
                        new AttackFrame(0.54779f, 1.20303f, -0.14465f, -0.16106f, 0.27471f, -0.57135f, 0.75641f, 1f, false),
                        new AttackFrame(0.57621f, 1.12583f, -0.0967f, -0.15733f, 0.37407f, -0.5813f, 0.70527f, 1f, false),
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
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.0763f, 1.50274f, 0.46459f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.04478f, 1.47434f, 0.91438f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.16587f, 1.44593f, 1.36416f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.28695f, 1.41753f, 1.81395f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.40804f, 1.38913f, 2.26374f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.52912f, 1.36072f, 2.71353f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.6502f, 1.33232f, 3.16332f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.77129f, 1.30392f, 3.61311f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.89237f, 1.27552f, 4.06289f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.01346f, 1.24711f, 4.51268f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.13454f, 1.21871f, 4.96247f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.25563f, 1.19031f, 5.41226f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.37671f, 1.1619f, 5.86205f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.4978f, 1.1335f, 6.31184f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.61888f, 1.1051f, 6.76162f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.73997f, 1.07669f, 7.21141f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.86105f, 1.04829f, 7.6612f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true)
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
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, false),
                        new AttackFrame(0.0763f, 1.50274f, 0.46459f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.04478f, 1.47434f, 0.91438f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.16587f, 1.44593f, 1.36416f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.28695f, 1.41753f, 1.81395f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.40804f, 1.38913f, 2.26374f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.52912f, 1.36072f, 2.71353f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.6502f, 1.33232f, 3.16332f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.77129f, 1.30392f, 3.61311f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-0.89237f, 1.27552f, 4.06289f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.01346f, 1.24711f, 4.51268f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.13454f, 1.21871f, 4.96247f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.25563f, 1.19031f, 5.41226f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.37671f, 1.1619f, 5.86205f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.4978f, 1.1335f, 6.31184f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.61888f, 1.1051f, 6.76162f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.73997f, 1.07669f, 7.21141f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true),
                        new AttackFrame(-1.86105f, 1.04829f, 7.6612f, 0.03126f, -0.13079f, -0.00413f, 0.99091f, 1f, true)
                    },
                    "hero-blue trail behind the arrow")
            });
    }
}
