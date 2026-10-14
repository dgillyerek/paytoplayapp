using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan fresh rebuild bow attack (2026-10-13; bow idea from the old cursor/rowan-nocape-20261008 pack): f0 rest, f6 bow raised and
    /// arrow nocked, f10 full draw (body side-on, 70 deg turn spread over hips and spine, left shoulder to the target; the bow arm
    /// extends out from the shoulder clear of the chest, the draw hand anchors on the right cheek beside the head by 2-bone IK, elbow
    /// out to the side), f14 loose (arrow flies nock to grip at 14 m/s with a hero-blue trail), f17 string-hand recoil, f30 rest. Feet planted,
    /// Head bone faces forward every frame. The hand bow (ROWAN_bow.fbx) shows f4-24; the Meshy bow stays slung on his back (fused in
    /// the scan, rigid on Spine2). Frames baked in Unity convention (character forward = +Z) by design .../fresh_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_blenderig_attack.fbx";
        public const string FileMd5 = "646c2bf6e2c98eb4b124afb5d74107eb";
        public const string DesignDir = RowanMotion.DesignDir;
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs modelled in Blender for the fresh rebuild (design .../fresh_20261013/props).</summary>
        public static readonly string[] PropFileNames =
        {
            "ROWAN_bow.fbx",
            "ROWAN_arrow.fbx",
            "ROWAN_quiver.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "a147b7d3ab59893e9b563decc1241dac",
            "079abe232bead0a8fee5d019441fe4d5",
            "874a56bf7a88264d6db945e7f6d32b99"
        };

        /// <summary>Leather quiver with arrows, rigid on Spine2 in rest, walk and attack. Stands in for the attack's ROWAN_quiver track.</summary>
        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "ROWAN_quiver_rest",
                "ROWAN_quiver.fbx",
                "874a56bf7a88264d6db945e7f6d32b99",
                "mixamorig:Spine2",
                new[] { 0.045f, 1.36f, -0.165f, 0f, 0f, -0.21303f, 0.97705f },
                new[] { 0.1092f, 0.7371f, 0.112f },
                "Leather quiver on Spine2 over the right shoulder in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's ROWAN_quiver track.",
                "ROWAN_quiver")
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Rowan",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { -0.015f, 0.96f, 0.02f, -0.015f, 1.535f, 0.02f, -0.305f, 0.965f, 0.1f, 0.275f, 0.965f, 0.1f },
            new[]
            {
                new AttackTrack(
                    "ROWAN_quiver",
                    AttackTrackKind.Held,
                    "ROWAN_quiver.fbx",
                    "mixamorig:Spine2",
                    10,
                    0,
                    0f,
                    AttackVfxShape.None,
                    0f,
                    0f,
                    new float[0],
                    new float[0],
                    new float[0],
                    new[] { 0.1092f, 0.7371f, 0.112f },
                    new[]
                    {
                        new AttackFrame(0.045f, 1.36f, -0.165f, 0f, 0f, -0.21303f, 0.97705f, 1f, true),
                        new AttackFrame(0.02804f, 1.36f, -0.16967f, -0.00964f, 0.0442f, -0.21281f, 0.97605f, 1f, true),
                        new AttackFrame(-0.01561f, 1.36f, -0.17449f, -0.0336f, 0.15409f, -0.21036f, 0.96482f, 1f, true),
                        new AttackFrame(-0.07196f, 1.36f, -0.16596f, -0.06406f, 0.2938f, -0.20317f, 0.93182f, 1f, true),
                        new AttackFrame(-0.12343f, 1.36f, -0.14146f, -0.09314f, 0.42717f, -0.19159f, 0.87872f, 1f, true),
                        new AttackFrame(-0.15688f, 1.36f, -0.11302f, -0.11417f, 0.52363f, -0.17985f, 0.82488f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16832f, 1.36f, -0.09966f, -0.12219f, 0.56041f, -0.1745f, 0.80035f, 1f, true),
                        new AttackFrame(-0.16583f, 1.36f, -0.10278f, -0.12039f, 0.55215f, -0.17575f, 0.80607f, 1f, true),
                        new AttackFrame(-0.15855f, 1.36f, -0.11122f, -0.11531f, 0.52884f, -0.17913f, 0.82155f, 1f, true),
                        new AttackFrame(-0.14656f, 1.36f, -0.12323f, -0.10738f, 0.49248f, -0.18399f, 0.84385f, 1f, true),
                        new AttackFrame(-0.12994f, 1.36f, -0.13689f, -0.09704f, 0.44506f, -0.18965f, 0.86979f, 1f, true),
                        new AttackFrame(-0.10908f, 1.36f, -0.15021f, -0.08478f, 0.38882f, -0.19544f, 0.89635f, 1f, true),
                        new AttackFrame(-0.08489f, 1.36f, -0.16149f, -0.07116f, 0.32639f, -0.20079f, 0.92092f, 1f, true),
                        new AttackFrame(-0.05875f, 1.36f, -0.1695f, -0.05688f, 0.26085f, -0.2053f, 0.94158f, 1f, true),
                        new AttackFrame(-0.03239f, 1.36f, -0.17371f, -0.04265f, 0.19562f, -0.20872f, 0.95726f, 1f, true),
                        new AttackFrame(-0.00765f, 1.36f, -0.17435f, -0.02929f, 0.13432f, -0.21101f, 0.96777f, 1f, true),
                        new AttackFrame(0.01377f, 1.36f, -0.17235f, -0.01757f, 0.08059f, -0.2123f, 0.97372f, 1f, true),
                        new AttackFrame(0.03043f, 1.36f, -0.16911f, -0.00829f, 0.03802f, -0.21287f, 0.97631f, 1f, true),
                        new AttackFrame(0.04118f, 1.36f, -0.1662f, -0.00219f, 0.01005f, -0.21302f, 0.97699f, 1f, true),
                        new AttackFrame(0.045f, 1.36f, -0.165f, 0f, 0f, -0.21303f, 0.97705f, 1f, true)
                    },
                    "leather quiver rigid on Spine2 in every frame; the RowanMotion idle quiver stands in for it"),
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
                    new[] { 0.048f, 1.3324f, 0.1643f },
                    new[]
                    {
                        new AttackFrame(-0.32307f, 0.92768f, 0.1525f, 0.44439f, -0.38604f, 0.32427f, 0.7405f, 1f, false),
                        new AttackFrame(-0.31331f, 0.9354f, 0.20274f, 0.41701f, -0.36226f, 0.30429f, 0.77606f, 1f, false),
                        new AttackFrame(-0.2736f, 0.96916f, 0.33518f, 0.34332f, -0.29824f, 0.25052f, 0.85465f, 1f, false),
                        new AttackFrame(-0.19157f, 1.05248f, 0.50259f, 0.23819f, -0.20691f, 0.1738f, 0.93287f, 1f, false),
                        new AttackFrame(-0.09033f, 1.16946f, 0.62595f, 0.12558f, -0.10909f, 0.09163f, 0.9818f, 1f, true),
                        new AttackFrame(-0.01153f, 1.25936f, 0.67949f, 0.03608f, -0.03134f, 0.02633f, 0.99851f, 1f, true),
                        new AttackFrame(0.01907f, 1.29202f, 0.69206f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01941f, 1.31042f, 0.69585f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01962f, 1.35174f, 0.70161f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01906f, 1.39341f, 0.70369f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01856f, 1.41219f, 0.70342f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01201f, 1.4042f, 0.70265f, 0.00821f, -0.00713f, 0.00599f, 0.99992f, 1f, true),
                        new AttackFrame(-0.00646f, 1.38079f, 0.69939f, 0.03104f, -0.02697f, 0.02265f, 0.9989f, 1f, true),
                        new AttackFrame(-0.0354f, 1.34166f, 0.6908f, 0.06577f, -0.05713f, 0.04799f, 0.99504f, 1f, true),
                        new AttackFrame(-0.07348f, 1.28625f, 0.67214f, 0.10951f, -0.09513f, 0.07991f, 0.98619f, 1f, true),
                        new AttackFrame(-0.11901f, 1.21607f, 0.63697f, 0.15924f, -0.13833f, 0.11619f, 0.97057f, 1f, true),
                        new AttackFrame(-0.16877f, 1.1381f, 0.57954f, 0.2118f, -0.18399f, 0.15455f, 0.94731f, 1f, true),
                        new AttackFrame(-0.21708f, 1.06511f, 0.50061f, 0.26414f, -0.22946f, 0.19274f, 0.91675f, 1f, true),
                        new AttackFrame(-0.25763f, 1.00849f, 0.41099f, 0.3134f, -0.27225f, 0.22869f, 0.88054f, 1f, false),
                        new AttackFrame(-0.28711f, 0.97089f, 0.325f, 0.35716f, -0.31026f, 0.26062f, 0.84157f, 1f, false),
                        new AttackFrame(-0.30595f, 0.94837f, 0.25257f, 0.39349f, -0.34182f, 0.28713f, 0.80367f, 1f, false),
                        new AttackFrame(-0.31655f, 0.93583f, 0.19833f, 0.42092f, -0.36565f, 0.30714f, 0.77123f, 1f, false),
                        new AttackFrame(-0.32161f, 0.92959f, 0.16437f, 0.43828f, -0.38073f, 0.31981f, 0.74878f, 1f, false),
                        new AttackFrame(-0.32307f, 0.92768f, 0.1525f, 0.44439f, -0.38604f, 0.32427f, 0.7405f, 1f, false)
                    },
                    "wooden bow gripped 6 cm along LeftHand, upright at full draw, shown f4-24 (the mesh bow stays slung on his back)"),
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
                    new[] { 0.033f, 0.0382f, 0.795f },
                    new[]
                    {
                        new AttackFrame(0.29198f, 0.94512f, 0.14276f, 0.41042f, 0.61369f, -0.06573f, 0.67129f, 1f, false),
                        new AttackFrame(0.28976f, 0.95547f, 0.15923f, 0.3937f, 0.61963f, -0.11266f, 0.6696f, 1f, false),
                        new AttackFrame(0.27399f, 0.99811f, 0.21128f, 0.34972f, 0.61744f, -0.23534f, 0.66413f, 1f, false),
                        new AttackFrame(0.22865f, 1.09884f, 0.28593f, 0.29592f, 0.5762f, -0.4032f, 0.64642f, 1f, false),
                        new AttackFrame(0.15928f, 1.23389f, 0.33053f, 0.26117f, 0.49313f, -0.57024f, 0.60286f, 1f, false),
                        new AttackFrame(0.09897f, 1.33231f, 0.33712f, 0.25575f, 0.4062f, -0.68722f, 0.54528f, 1f, true),
                        new AttackFrame(0.0747f, 1.3669f, 0.3356f, 0.25936f, 0.36728f, -0.7292f, 0.51586f, 1f, true),
                        new AttackFrame(0.07223f, 1.38019f, 0.33057f, 0.29171f, 0.35819f, -0.66092f, 0.59143f, 1f, true),
                        new AttackFrame(0.06374f, 1.41389f, 0.27681f, 0.3039f, 0.2752f, -0.42962f, 0.80457f, 1f, true),
                        new AttackFrame(0.05559f, 1.44479f, 0.17166f, 0.15466f, 0.07731f, -0.12594f, 0.97685f, 1f, true),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05248f, 1.45467f, 0.09791f, 0.00249f, -0.06976f, 0.04283f, 0.99664f, 1f, false),
                        new AttackFrame(0.05212f, 1.45243f, 0.05628f, -0.07194f, -0.14918f, 0.1195f, 0.97892f, 1f, false),
                        new AttackFrame(0.05222f, 1.45128f, 0.03323f, -0.1172f, -0.19301f, 0.1571f, 0.96142f, 1f, false),
                        new AttackFrame(0.05975f, 1.44455f, 0.03631f, -0.09232f, -0.18349f, 0.14992f, 0.96713f, 1f, false),
                        new AttackFrame(0.08114f, 1.42453f, 0.04533f, -0.01797f, -0.15188f, 0.12601f, 0.98017f, 1f, false),
                        new AttackFrame(0.11473f, 1.39007f, 0.0602f, 0.10612f, -0.08821f, 0.07823f, 0.98734f, 1f, false),
                        new AttackFrame(0.15817f, 1.33837f, 0.08047f, 0.26457f, 0.01714f, 0.00212f, 0.96421f, 1f, false),
                        new AttackFrame(0.20637f, 1.26652f, 0.10352f, 0.4101f, 0.15469f, -0.08887f, 0.89442f, 1f, false),
                        new AttackFrame(0.24904f, 1.17764f, 0.12294f, 0.49429f, 0.2921f, -0.16329f, 0.8023f, 1f, false),
                        new AttackFrame(0.27537f, 1.08896f, 0.13282f, 0.51465f, 0.40407f, -0.19925f, 0.72949f, 1f, false),
                        new AttackFrame(0.28594f, 1.02165f, 0.13495f, 0.499f, 0.48593f, -0.19741f, 0.68986f, 1f, false),
                        new AttackFrame(0.28939f, 0.98118f, 0.13525f, 0.4719f, 0.54209f, -0.17099f, 0.67395f, 1f, false),
                        new AttackFrame(0.29091f, 0.96022f, 0.13677f, 0.44615f, 0.5782f, -0.13463f, 0.66971f, 1f, false),
                        new AttackFrame(0.29168f, 0.95038f, 0.13937f, 0.4266f, 0.59949f, -0.0999f, 0.66981f, 1f, false),
                        new AttackFrame(0.29194f, 0.94624f, 0.14177f, 0.41456f, 0.61035f, -0.075f, 0.67081f, 1f, false),
                        new AttackFrame(0.29198f, 0.94512f, 0.14276f, 0.41042f, 0.61369f, -0.06573f, 0.67129f, 1f, false)
                    },
                    "arrow nocked f5, drawn to the jaw by RightHand at f10, held to f13"),
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
                    new[] { 0.033f, 0.0382f, 0.795f },
                    new[]
                    {
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.0256f, 1.42113f, 0.5839f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.00176f, 1.38638f, 1.04846f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.02912f, 1.35163f, 1.51303f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.05648f, 1.31688f, 1.9776f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.08384f, 1.28213f, 2.44216f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.1112f, 1.24738f, 2.90673f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.13856f, 1.21263f, 3.37129f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.16592f, 1.17788f, 3.83586f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.19328f, 1.14313f, 4.30043f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.22064f, 1.10838f, 4.76499f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.248f, 1.07363f, 5.22956f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.27536f, 1.03889f, 5.69413f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.30271f, 1.00414f, 6.15869f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.33007f, 0.96939f, 6.62326f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.35743f, 0.93464f, 7.08782f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.38479f, 0.89989f, 7.55239f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.41215f, 0.86514f, 8.01696f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true)
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
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.05296f, 1.45588f, 0.11933f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, false),
                        new AttackFrame(0.0256f, 1.42113f, 0.5839f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.00176f, 1.38638f, 1.04846f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.02912f, 1.35163f, 1.51303f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.05648f, 1.31688f, 1.9776f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.08384f, 1.28213f, 2.44216f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.1112f, 1.24738f, 2.90673f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.13856f, 1.21263f, 3.37129f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.16592f, 1.17788f, 3.83586f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.19328f, 1.14313f, 4.30043f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.22064f, 1.10838f, 4.76499f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.248f, 1.07363f, 5.22956f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.27536f, 1.03889f, 5.69413f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.30271f, 1.00414f, 6.15869f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.33007f, 0.96939f, 6.62326f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.35743f, 0.93464f, 7.08782f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.38479f, 0.89989f, 7.55239f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true),
                        new AttackFrame(-0.41215f, 0.86514f, 8.01696f, 0.03731f, -0.02931f, -0.00109f, 0.99887f, 1f, true)
                    },
                    "hero-blue trail behind the arrow")
            });
    }
}
