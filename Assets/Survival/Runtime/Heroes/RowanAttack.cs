using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan A-pose rebuild bow attack (2026-10-14): f0 rest, f5 arrow nocked, f10 full draw (torso side-on, 70 deg turn spread over
    /// hips and spine, left shoulder to the target; bow arm straight out from the shoulder; draw hand at the right cheek by 2-bone IK
    /// with the elbow out at shoulder height), f14 loose (arrow flies nock to grip at 14 m/s with a hero-blue trail), f17 follow-through,
    /// f30 rest. Arm roll fixed: after the aim the full Arm/ForeArm basis maps the rest elbow hinge onto the IK hinge (explicit pole
    /// roll) and RightHand/LeftHand are keyed every frame; every attack frame checked for forearm flip (forearm twist 0).
    /// One bow only, in the left hand (idle prop in every clip); nothing on his back. Feet planted, Head faces forward every frame.
    /// Frames baked in Unity convention (character forward = +Z) by design .../fresh_20261013/work/apose_20261014/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_blenderig_attack.fbx";
        public const string FileMd5 = "883756ca9bc2c4b2a9a9bc88f3ee14b8";
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
                new[] { -0.35965f, 0.89209f, 0.03532f, 0.60572f, -0.33516f, 0.13661f, 0.70859f },
                new[] { 0.048f, 1.3323f, 0.1643f },
                "Single wooden bow on LeftHand in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's ROWAN_bow track so one bow serves every clip.",
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
                        new AttackFrame(-0.35965f, 0.89209f, 0.03532f, 0.60572f, -0.33516f, 0.13661f, 0.70859f, 1f, true),
                        new AttackFrame(-0.3304f, 0.92232f, 0.09954f, 0.53178f, -0.23348f, 0.15745f, 0.79869f, 1f, true),
                        new AttackFrame(-0.2433f, 1.0328f, 0.24678f, 0.34337f, 0.01563f, 0.17021f, 0.92352f, 1f, true),
                        new AttackFrame(-0.1309f, 1.15085f, 0.42692f, 0.1594f, 0.21431f, 0.10363f, 0.95808f, 1f, true),
                        new AttackFrame(-0.02705f, 1.22652f, 0.60636f, 0.12006f, 0.17351f, 0.02345f, 0.9772f, 1f, true),
                        new AttackFrame(0.01279f, 1.25049f, 0.68272f, 0.17897f, 0.05258f, -0.00894f, 0.98241f, 1f, true),
                        new AttackFrame(0.00205f, 1.28853f, 0.69524f, 0.14401f, 0.04191f, -0.00536f, 0.98867f, 1f, true),
                        new AttackFrame(-0.00845f, 1.32876f, 0.70366f, 0.10388f, 0.03704f, -0.00215f, 0.9939f, 1f, true),
                        new AttackFrame(-0.0199f, 1.36885f, 0.71017f, 0.06757f, 0.02649f, -0.00058f, 0.99736f, 1f, true),
                        new AttackFrame(-0.03218f, 1.40838f, 0.7148f, 0.03566f, 0.0103f, 0.00007f, 0.99931f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04362f, 1.44799f, 0.715f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.05167f, 1.41049f, 0.70622f, 0.01216f, 0.02486f, -0.00424f, 0.99961f, 1f, true),
                        new AttackFrame(-0.0728f, 1.3198f, 0.66584f, 0.05002f, 0.08234f, -0.00502f, 0.99534f, 1f, true),
                        new AttackFrame(-0.11241f, 1.20224f, 0.61972f, 0.16803f, 0.05977f, -0.0026f, 0.98396f, 1f, true),
                        new AttackFrame(-0.16102f, 1.09582f, 0.57342f, 0.31505f, -0.03227f, -0.01777f, 0.94836f, 1f, true),
                        new AttackFrame(-0.19151f, 1.05688f, 0.53461f, 0.37195f, -0.0699f, -0.02713f, 0.92522f, 1f, true),
                        new AttackFrame(-0.21507f, 1.04501f, 0.4993f, 0.3808f, -0.0685f, -0.02046f, 0.92189f, 1f, true),
                        new AttackFrame(-0.2342f, 1.03276f, 0.42789f, 0.36732f, -0.01873f, 0.017f, 0.92975f, 1f, true),
                        new AttackFrame(-0.25916f, 1.00897f, 0.34111f, 0.38837f, -0.02103f, 0.05933f, 0.91935f, 1f, true),
                        new AttackFrame(-0.28737f, 0.97691f, 0.25305f, 0.43476f, -0.07511f, 0.09876f, 0.89196f, 1f, true),
                        new AttackFrame(-0.31622f, 0.94125f, 0.17135f, 0.49523f, -0.16319f, 0.12443f, 0.84418f, 1f, true),
                        new AttackFrame(-0.34223f, 0.90821f, 0.10047f, 0.56185f, -0.26075f, 0.1306f, 0.77413f, 1f, true),
                        new AttackFrame(-0.35691f, 0.89276f, 0.05098f, 0.60297f, -0.32401f, 0.13132f, 0.71708f, 1f, true),
                        new AttackFrame(-0.35965f, 0.89209f, 0.03532f, 0.60572f, -0.33516f, 0.13661f, 0.70859f, 1f, true)
                    },
                    "single wooden bow gripped 7.5 cm along LeftHand in every frame, upright at full draw; the RowanMotion idle bow stands in for it"),
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
                        new AttackFrame(0.35995f, 0.91549f, 0.02653f, 0.55335f, 0.71026f, -0.43511f, 0.00312f, 1f, false),
                        new AttackFrame(0.30913f, 0.94832f, 0.06475f, 0.61308f, 0.5813f, -0.52157f, 0.11911f, 1f, false),
                        new AttackFrame(0.18982f, 1.06117f, 0.14989f, 0.65016f, 0.30728f, -0.6283f, 0.29684f, 1f, false),
                        new AttackFrame(0.08395f, 1.16822f, 0.26904f, 0.64049f, 0.24909f, -0.64717f, 0.33001f, 1f, false),
                        new AttackFrame(0.03318f, 1.27491f, 0.31362f, 0.61153f, 0.26213f, -0.66332f, 0.34252f, 1f, false),
                        new AttackFrame(0.01982f, 1.31786f, 0.31592f, 0.59007f, 0.23904f, -0.67802f, 0.36738f, 1f, true),
                        new AttackFrame(0.02889f, 1.35699f, 0.32263f, 0.55354f, 0.26039f, -0.64806f, 0.45368f, 1f, true),
                        new AttackFrame(0.02908f, 1.42971f, 0.25256f, 0.46794f, 0.07441f, -0.55941f, 0.68011f, 1f, true),
                        new AttackFrame(0.03143f, 1.48734f, 0.15013f, 0.36377f, -0.05866f, -0.3774f, 0.84959f, 1f, true),
                        new AttackFrame(0.04169f, 1.53214f, 0.05686f, 0.24306f, -0.10623f, -0.16035f, 0.95075f, 1f, true),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.06679f, 1.5806f, -0.03687f, 0.07127f, -0.06618f, 0.00678f, 0.99524f, 1f, false),
                        new AttackFrame(0.0819f, 1.58001f, -0.06384f, 0.034f, -0.05627f, 0.02449f, 0.99754f, 1f, false),
                        new AttackFrame(0.09003f, 1.58024f, -0.0779f, 0.01144f, -0.04675f, 0.02967f, 0.9984f, 1f, false),
                        new AttackFrame(0.11027f, 1.52989f, -0.05671f, 0.13582f, -0.06292f, 0.00281f, 0.98873f, 1f, false),
                        new AttackFrame(0.15999f, 1.42139f, 0.01142f, 0.38921f, 0.03719f, -0.19012f, 0.90055f, 1f, false),
                        new AttackFrame(0.23366f, 1.27601f, 0.10624f, 0.52038f, 0.29786f, -0.35414f, 0.71768f, 1f, false),
                        new AttackFrame(0.30715f, 1.14276f, 0.17461f, 0.5171f, 0.55286f, -0.40681f, 0.51133f, 1f, false),
                        new AttackFrame(0.33748f, 1.08925f, 0.19242f, 0.49347f, 0.66225f, -0.40344f, 0.39389f, 1f, false),
                        new AttackFrame(0.33332f, 1.08085f, 0.19131f, 0.53983f, 0.59972f, -0.41484f, 0.42051f, 1f, false),
                        new AttackFrame(0.33058f, 1.06374f, 0.17452f, 0.58112f, 0.55055f, -0.43507f, 0.4122f, 1f, false),
                        new AttackFrame(0.33331f, 1.03646f, 0.14998f, 0.6064f, 0.53791f, -0.45881f, 0.3639f, 1f, false),
                        new AttackFrame(0.34005f, 1.00241f, 0.12082f, 0.61413f, 0.56033f, -0.47616f, 0.28662f, 1f, false),
                        new AttackFrame(0.34886f, 0.96605f, 0.08921f, 0.60423f, 0.60919f, -0.47678f, 0.19101f, 1f, false),
                        new AttackFrame(0.35776f, 0.93299f, 0.05773f, 0.57814f, 0.67195f, -0.45479f, 0.08606f, 1f, false),
                        new AttackFrame(0.36079f, 0.91727f, 0.03404f, 0.55625f, 0.70806f, -0.43471f, 0.01619f, 1f, false),
                        new AttackFrame(0.35995f, 0.91549f, 0.02653f, 0.55335f, 0.71026f, -0.43511f, 0.00312f, 1f, false)
                    },
                    "arrow nocked f5, drawn to the cheek by RightHand at f10, held to f13"),
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
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(-0.00449f, 1.499f, 0.4331f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.06765f, 1.41668f, 0.88808f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.13082f, 1.33436f, 1.34307f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.19398f, 1.25203f, 1.79805f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.25715f, 1.16971f, 2.25304f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.32031f, 1.08739f, 2.70802f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.38348f, 1.00507f, 3.16301f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.44664f, 0.92275f, 3.61799f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.50981f, 0.84043f, 4.07298f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.57297f, 0.75811f, 4.52796f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.63614f, 0.67579f, 4.98295f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.6993f, 0.59347f, 5.43793f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.76247f, 0.51115f, 5.89292f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.82563f, 0.42883f, 6.3479f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.8888f, 0.34651f, 6.80289f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.95196f, 0.26419f, 7.25787f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-1.01513f, 0.18187f, 7.71286f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true)
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
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(0.05868f, 1.58132f, -0.02189f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, false),
                        new AttackFrame(-0.00449f, 1.499f, 0.4331f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.06765f, 1.41668f, 0.88808f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.13082f, 1.33436f, 1.34307f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.19398f, 1.25203f, 1.79805f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.25715f, 1.16971f, 2.25304f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.32031f, 1.08739f, 2.70802f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.38348f, 1.00507f, 3.16301f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.44664f, 0.92275f, 3.61799f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.50981f, 0.84043f, 4.07298f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.57297f, 0.75811f, 4.52796f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.63614f, 0.67579f, 4.98295f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.6993f, 0.59347f, 5.43793f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.76247f, 0.51115f, 5.89292f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.82563f, 0.42883f, 6.3479f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.8888f, 0.34651f, 6.80289f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-0.95196f, 0.26419f, 7.25787f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true),
                        new AttackFrame(-1.01513f, 0.18187f, 7.71286f, 0.08917f, -0.06756f, -0.00606f, 0.9937f, 1f, true)
                    },
                    "hero-blue trail behind the arrow")
            });
    }
}
