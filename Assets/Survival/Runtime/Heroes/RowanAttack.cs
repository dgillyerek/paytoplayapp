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
    /// One bow only, its grip inside the left fist (fingers sculpted around it, fist.py) in every clip; nothing on his back. Feet planted, Head faces forward every frame.
    /// Frames baked in Unity convention (character forward = +Z) by design .../fresh_20261013/work/apose_20261014/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_blenderig_attack.fbx";
        public const string FileMd5 = "f2f7e410798101e94b02659d6266f443";
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
                new[] { -0.31971f, 0.90962f, 0.09395f, -0.00366f, 0.57536f, -0.81787f, 0.00573f },
                new[] { 0.048f, 1.3323f, 0.1643f },
                "Single wooden bow, grip inside the left fist, in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's ROWAN_bow track so one bow serves every clip.",
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
                        new AttackFrame(-0.31971f, 0.90962f, 0.09395f, -0.00366f, 0.57536f, -0.81787f, 0.00573f, 1f, true),
                        new AttackFrame(-0.36984f, 0.92611f, 0.11353f, -0.02758f, -0.56865f, 0.82151f, 0.03172f, 1f, true),
                        new AttackFrame(-0.46265f, 0.98681f, 0.19124f, -0.09203f, -0.52113f, 0.84144f, 0.10924f, 1f, true),
                        new AttackFrame(-0.48628f, 1.09018f, 0.32458f, -0.08469f, -0.3844f, 0.89769f, 0.19803f, 1f, true),
                        new AttackFrame(-0.41181f, 1.14247f, 0.45459f, -0.00913f, -0.29912f, 0.91957f, 0.25463f, 1f, true),
                        new AttackFrame(-0.27426f, 1.18814f, 0.57784f, 0.08043f, -0.21187f, 0.93683f, 0.26645f, 1f, true),
                        new AttackFrame(-0.11676f, 1.3035f, 0.67569f, 0.13412f, -0.07248f, 0.97007f, 0.18903f, 1f, true),
                        new AttackFrame(-0.01405f, 1.43105f, 0.69595f, 0.18228f, 0.05676f, 0.97916f, 0.06924f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(0.0101f, 1.4882f, 0.68833f, 0.20168f, 0.11362f, 0.97269f, 0.01683f, 1f, true),
                        new AttackFrame(-0.01586f, 1.46139f, 0.69264f, 0.18185f, 0.08682f, 0.97858f, 0.04209f, 1f, true),
                        new AttackFrame(-0.0863f, 1.39236f, 0.69246f, 0.13388f, 0.01276f, 0.98524f, 0.10592f, 1f, true),
                        new AttackFrame(-0.18391f, 1.3035f, 0.66102f, 0.0867f, -0.08161f, 0.97545f, 0.18527f, 1f, true),
                        new AttackFrame(-0.28452f, 1.22085f, 0.59178f, 0.05277f, -0.17887f, 0.95189f, 0.24315f, 1f, true),
                        new AttackFrame(-0.37076f, 1.1635f, 0.50509f, 0.01602f, -0.26267f, 0.92932f, 0.25907f, 1f, true),
                        new AttackFrame(-0.43937f, 1.14247f, 0.42919f, -0.03655f, -0.30658f, 0.91889f, 0.2456f, 1f, true),
                        new AttackFrame(-0.48685f, 1.12353f, 0.36001f, -0.08619f, -0.34297f, 0.9096f, 0.21811f, 1f, true),
                        new AttackFrame(-0.50144f, 1.07677f, 0.28374f, -0.10699f, -0.40587f, 0.88988f, 0.1787f, 1f, true),
                        new AttackFrame(-0.49011f, 1.01222f, 0.21128f, -0.12013f, -0.49872f, 0.84929f, 0.12473f, 1f, true),
                        new AttackFrame(-0.44089f, 0.96447f, 0.15363f, -0.08148f, -0.54165f, 0.83273f, 0.08085f, 1f, true),
                        new AttackFrame(-0.38399f, 0.93173f, 0.1169f, -0.03975f, -0.56551f, 0.82286f, 0.03896f, 1f, true),
                        new AttackFrame(-0.33814f, 0.91467f, 0.09893f, -0.00845f, -0.57398f, 0.8188f, 0.00694f, 1f, true),
                        new AttackFrame(-0.31971f, 0.90962f, 0.09395f, -0.00366f, 0.57537f, -0.81787f, 0.00573f, 1f, true)
                    },
                    "single wooden bow, grip inside the left fist (palm centre) in every frame; the RowanMotion idle bow stands in for it"),
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
                        new AttackFrame(0.38157f, 0.92744f, 0.05665f, 0.03144f, 0.43234f, -0.70392f, 0.56266f, 1f, false),
                        new AttackFrame(0.46168f, 0.96445f, 0.02314f, -0.0047f, 0.44499f, -0.65102f, 0.61492f, 1f, false),
                        new AttackFrame(0.548f, 1.05105f, -0.0811f, -0.09743f, 0.44351f, -0.59538f, 0.66282f, 1f, false),
                        new AttackFrame(0.54853f, 1.12583f, -0.21241f, -0.21815f, 0.44137f, -0.56144f, 0.66512f, 1f, false),
                        new AttackFrame(0.47625f, 1.21861f, -0.31055f, -0.26579f, 0.36589f, -0.53148f, 0.71624f, 1f, false),
                        new AttackFrame(0.37151f, 1.35753f, -0.32053f, -0.23232f, 0.17999f, -0.45464f, 0.84079f, 1f, false),
                        new AttackFrame(0.30191f, 1.44242f, -0.31131f, -0.20945f, 0.05164f, -0.32877f, 0.91944f, 1f, false),
                        new AttackFrame(0.27179f, 1.52279f, -0.22884f, -0.05303f, -0.12348f, -0.2039f, 0.96972f, 1f, false),
                        new AttackFrame(0.26397f, 1.55664f, -0.13646f, 0.09681f, -0.17609f, -0.1369f, 0.96999f, 1f, true),
                        new AttackFrame(0.23021f, 1.55208f, -0.05795f, 0.09598f, -0.22481f, -0.04272f, 0.96872f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.22029f, 1.53794f, 0.00426f, -0.02474f, -0.12038f, -0.05172f, 0.99107f, 1f, false),
                        new AttackFrame(0.2517f, 1.54772f, -0.01056f, -0.10301f, -0.10824f, -0.11127f, 0.98249f, 1f, false),
                        new AttackFrame(0.26785f, 1.55258f, -0.03336f, -0.14131f, -0.10764f, -0.1284f, 0.97568f, 1f, false),
                        new AttackFrame(0.26638f, 1.55147f, -0.07098f, -0.06706f, -0.20484f, -0.11548f, 0.96964f, 1f, false),
                        new AttackFrame(0.27215f, 1.54539f, -0.13011f, 0.03424f, -0.24673f, -0.14624f, 0.95738f, 1f, false),
                        new AttackFrame(0.29249f, 1.53032f, -0.15173f, 0.04926f, -0.22376f, -0.19477f, 0.95371f, 1f, false),
                        new AttackFrame(0.31779f, 1.50366f, -0.15794f, -0.00404f, -0.23483f, -0.24549f, 0.94052f, 1f, false),
                        new AttackFrame(0.36069f, 1.44467f, -0.17842f, -0.09213f, -0.15959f, -0.36153f, 0.91397f, 1f, false),
                        new AttackFrame(0.40883f, 1.38281f, -0.17263f, -0.08662f, -0.04523f, -0.47462f, 0.87475f, 1f, false),
                        new AttackFrame(0.4682f, 1.31068f, -0.15697f, -0.08615f, 0.08096f, -0.54592f, 0.82946f, 1f, false),
                        new AttackFrame(0.54779f, 1.20303f, -0.14465f, -0.1632f, 0.27143f, -0.57082f, 0.75753f, 1f, false),
                        new AttackFrame(0.57621f, 1.12583f, -0.0967f, -0.15955f, 0.37099f, -0.58082f, 0.7068f, 1f, false),
                        new AttackFrame(0.56378f, 1.07657f, -0.03692f, -0.09018f, 0.38827f, -0.59554f, 0.69746f, 1f, false),
                        new AttackFrame(0.50734f, 1.01049f, 0.01608f, -0.02461f, 0.39431f, -0.6364f, 0.6625f, 1f, false),
                        new AttackFrame(0.43456f, 0.95005f, 0.04703f, 0.01507f, 0.43627f, -0.66763f, 0.60309f, 1f, false),
                        new AttackFrame(0.38157f, 0.92744f, 0.05665f, 0.03144f, 0.43234f, -0.70392f, 0.56266f, 1f, false)
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
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.0726f, 1.50253f, 0.46356f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.05218f, 1.47393f, 0.91232f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.17696f, 1.44532f, 1.36109f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.30175f, 1.41671f, 1.80985f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.42653f, 1.3881f, 2.25861f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.55132f, 1.35949f, 2.70738f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.6761f, 1.33088f, 3.15614f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.80088f, 1.30227f, 3.6049f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.92567f, 1.27366f, 4.05367f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.05045f, 1.24506f, 4.50243f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.17524f, 1.21645f, 4.95119f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.30002f, 1.18784f, 5.39996f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.42481f, 1.15923f, 5.84872f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.54959f, 1.13062f, 6.29748f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.67437f, 1.10201f, 6.74624f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.79916f, 1.0734f, 7.19501f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.92394f, 1.0448f, 7.64377f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true)
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
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.19739f, 1.53114f, 0.0148f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, false),
                        new AttackFrame(0.0726f, 1.50253f, 0.46356f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.05218f, 1.47393f, 0.91232f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.17696f, 1.44532f, 1.36109f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.30175f, 1.41671f, 1.80985f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.42653f, 1.3881f, 2.25861f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.55132f, 1.35949f, 2.70738f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.6761f, 1.33088f, 3.15614f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.80088f, 1.30227f, 3.6049f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-0.92567f, 1.27366f, 4.05367f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.05045f, 1.24506f, 4.50243f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.17524f, 1.21645f, 4.95119f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.30002f, 1.18784f, 5.39996f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.42481f, 1.15923f, 5.84872f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.54959f, 1.13062f, 6.29748f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.67437f, 1.10201f, 6.74624f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.79916f, 1.0734f, 7.19501f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true),
                        new AttackFrame(-1.92394f, 1.0448f, 7.64377f, 0.03154f, -0.13486f, -0.00429f, 0.99035f, 1f, true)
                    },
                    "hero-blue trail behind the arrow")
            });
    }
}
