using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan fresh rebuild bow attack (2026-10-13; bow idea from the old cursor/rowan-nocape-20261008 pack): f0 rest, f6 bow raised and
    /// arrow nocked, f10 full draw (chest turned 24 deg so the left shoulder leads, left arm to the target, RightHand at the jaw by
    /// 2-bone IK), f14 loose (arrow flies nock to grip at 14 m/s with a hero-blue trail), f17 string-hand recoil, f30 rest. Feet planted,
    /// Head bone faces forward every frame. The hand bow (ROWAN_bow.fbx) shows f4-24; the Meshy bow stays slung on his back (fused in
    /// the scan, rigid on Spine2). Frames baked in Unity convention (character forward = +Z) by design .../fresh_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanAttack
    {
        public const string FileName = "ROWAN_blenderig_attack.fbx";
        public const string FileMd5 = "b08f3b1bf0a41198891cc294e8e9a3df";
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
            new[] { -0.015f, 0.96f, 0.02f, -0.015f, 1.535f, 0.02f, -0.285f, 0.93f, 0.08f, 0.255f, 0.93f, 0.08f },
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
                        new AttackFrame(0.03923f, 1.36f, -0.16677f, -0.0033f, 0.01516f, -0.213f, 0.97693f, 1f, true),
                        new AttackFrame(0.0246f, 1.36f, -0.17041f, -0.01156f, 0.05303f, -0.21272f, 0.97561f, 1f, true),
                        new AttackFrame(0.00523f, 1.36f, -0.17343f, -0.02227f, 0.10213f, -0.21186f, 0.97169f, 1f, true),
                        new AttackFrame(-0.01435f, 1.36f, -0.17449f, -0.03292f, 0.15097f, -0.21047f, 0.96531f, 1f, true),
                        new AttackFrame(-0.02942f, 1.36f, -0.17395f, -0.04105f, 0.18829f, -0.20904f, 0.95873f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03543f, 1.36f, -0.17341f, -0.04429f, 0.20314f, -0.20838f, 0.95569f, 1f, true),
                        new AttackFrame(-0.03407f, 1.36f, -0.17355f, -0.04356f, 0.19977f, -0.20853f, 0.95641f, 1f, true),
                        new AttackFrame(-0.03026f, 1.36f, -0.17389f, -0.04151f, 0.19037f, -0.20895f, 0.95832f, 1f, true),
                        new AttackFrame(-0.02445f, 1.36f, -0.17426f, -0.03837f, 0.176f, -0.20955f, 0.96106f, 1f, true),
                        new AttackFrame(-0.01708f, 1.36f, -0.17448f, -0.03439f, 0.15774f, -0.21024f, 0.96423f, 1f, true),
                        new AttackFrame(-0.00859f, 1.36f, -0.17438f, -0.0298f, 0.13665f, -0.21094f, 0.96744f, 1f, true),
                        new AttackFrame(0.00055f, 1.36f, -0.17386f, -0.02482f, 0.11384f, -0.21158f, 0.97039f, 1f, true),
                        new AttackFrame(0.00988f, 1.36f, -0.17289f, -0.01971f, 0.0904f, -0.21212f, 0.97285f, 1f, true),
                        new AttackFrame(0.01894f, 1.36f, -0.1715f, -0.01471f, 0.06747f, -0.21252f, 0.97471f, 1f, true),
                        new AttackFrame(0.02726f, 1.36f, -0.16984f, -0.01007f, 0.04618f, -0.21279f, 0.97595f, 1f, true),
                        new AttackFrame(0.03443f, 1.36f, -0.1681f, -0.00603f, 0.02766f, -0.21295f, 0.97665f, 1f, true),
                        new AttackFrame(0.04004f, 1.36f, -0.16654f, -0.00284f, 0.01304f, -0.21301f, 0.97696f, 1f, true),
                        new AttackFrame(0.04369f, 1.36f, -0.16542f, -0.00075f, 0.00345f, -0.21303f, 0.97704f, 1f, true),
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
                        new AttackFrame(-0.3051f, 0.88587f, 0.12934f, 0.55925f, -0.21343f, 0.12713f, 0.79091f, 1f, false),
                        new AttackFrame(-0.30461f, 0.89632f, 0.1751f, 0.52334f, -0.19972f, 0.11897f, 0.8198f, 1f, false),
                        new AttackFrame(-0.29777f, 0.94263f, 0.30074f, 0.42831f, -0.16346f, 0.09737f, 0.88337f, 1f, false),
                        new AttackFrame(-0.27334f, 1.05799f, 0.46504f, 0.2955f, -0.11277f, 0.06717f, 0.94628f, 1f, false),
                        new AttackFrame(-0.23539f, 1.21541f, 0.57939f, 0.15527f, -0.05926f, 0.0353f, 0.98546f, 1f, true),
                        new AttackFrame(-0.20528f, 1.33007f, 0.62252f, 0.04456f, -0.01701f, 0.01013f, 0.99881f, 1f, true),
                        new AttackFrame(-0.19398f, 1.37032f, 0.6317f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.18333f, 1.37714f, 0.63216f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.15983f, 1.39226f, 0.63202f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.13641f, 1.4074f, 0.63026f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12585f, 1.41425f, 0.62894f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12858f, 1.40522f, 0.62812f, 0.01013f, -0.00387f, 0.0023f, 0.99994f, 1f, true),
                        new AttackFrame(-0.13649f, 1.37885f, 0.62496f, 0.03834f, -0.01463f, 0.00872f, 0.99912f, 1f, true),
                        new AttackFrame(-0.14951f, 1.33512f, 0.61712f, 0.08125f, -0.03101f, 0.01847f, 0.99604f, 1f, true),
                        new AttackFrame(-0.16779f, 1.27381f, 0.60032f, 0.13537f, -0.05166f, 0.03077f, 0.98897f, 1f, true),
                        new AttackFrame(-0.19119f, 1.19703f, 0.56835f, 0.19703f, -0.0752f, 0.04479f, 0.97648f, 1f, true),
                        new AttackFrame(-0.21815f, 1.11251f, 0.51558f, 0.2625f, -0.10018f, 0.05967f, 0.95786f, 1f, true),
                        new AttackFrame(-0.24502f, 1.03365f, 0.44283f, 0.32806f, -0.1252f, 0.07458f, 0.93335f, 1f, true),
                        new AttackFrame(-0.26767f, 0.97231f, 0.36048f, 0.39026f, -0.14894f, 0.08872f, 0.90424f, 1f, false),
                        new AttackFrame(-0.28409f, 0.93154f, 0.2822f, 0.44602f, -0.17022f, 0.10139f, 0.87282f, 1f, false),
                        new AttackFrame(-0.2947f, 0.90733f, 0.21719f, 0.49277f, -0.18806f, 0.11202f, 0.84218f, 1f, false),
                        new AttackFrame(-0.30093f, 0.89415f, 0.16925f, 0.52843f, -0.20167f, 0.12013f, 0.81588f, 1f, false),
                        new AttackFrame(-0.30411f, 0.88778f, 0.13962f, 0.55119f, -0.21036f, 0.1253f, 0.79764f, 1f, false),
                        new AttackFrame(-0.3051f, 0.88587f, 0.12934f, 0.55925f, -0.21343f, 0.12713f, 0.79091f, 1f, false)
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
                        new AttackFrame(0.27375f, 0.9046f, 0.12246f, 0.22841f, 0.66607f, 0.18546f, 0.68541f, 1f, false),
                        new AttackFrame(0.25274f, 0.90956f, 0.1505f, 0.22271f, 0.66807f, 0.14705f, 0.69459f, 1f, false),
                        new AttackFrame(0.18454f, 0.94263f, 0.23533f, 0.2062f, 0.66161f, 0.0361f, 0.72003f, 1f, false),
                        new AttackFrame(0.06876f, 1.05486f, 0.35078f, 0.18313f, 0.6232f, -0.13149f, 0.74886f, 1f, false),
                        new AttackFrame(-0.03425f, 1.22432f, 0.40601f, 0.16947f, 0.55716f, -0.295f, 0.75752f, 1f, false),
                        new AttackFrame(-0.08413f, 1.34162f, 0.40149f, 0.17215f, 0.50083f, -0.39836f, 0.74889f, 1f, true),
                        new AttackFrame(-0.09811f, 1.38048f, 0.39338f, 0.17665f, 0.47908f, -0.43308f, 0.74278f, 1f, true),
                        new AttackFrame(-0.11234f, 1.38469f, 0.38005f, 0.18539f, 0.45112f, -0.37071f, 0.79037f, 1f, true),
                        new AttackFrame(-0.13049f, 1.39299f, 0.31648f, 0.17232f, 0.32942f, -0.20518f, 0.90536f, 1f, true),
                        new AttackFrame(-0.11732f, 1.40747f, 0.21911f, 0.07756f, 0.10357f, -0.04884f, 0.99039f, 1f, true),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.1036f, 1.41557f, 0.15763f, 0.02616f, 0.0014f, 0.04376f, 0.9987f, 1f, false),
                        new AttackFrame(-0.09863f, 1.41094f, 0.11919f, 0.10143f, 0.09126f, 0.11591f, 0.98384f, 1f, false),
                        new AttackFrame(-0.09196f, 1.40701f, 0.09541f, 0.17548f, 0.18394f, 0.13907f, 0.9571f, 1f, false),
                        new AttackFrame(-0.08937f, 1.40443f, 0.09969f, 0.17608f, 0.18912f, 0.14256f, 0.95546f, 1f, false),
                        new AttackFrame(-0.08207f, 1.39676f, 0.11234f, 0.17951f, 0.20581f, 0.15162f, 0.94996f, 1f, false),
                        new AttackFrame(-0.07069f, 1.38301f, 0.13423f, 0.18998f, 0.23835f, 0.16298f, 0.93837f, 1f, false),
                        new AttackFrame(-0.05559f, 1.35905f, 0.16851f, 0.21309f, 0.29401f, 0.17113f, 0.9159f, 1f, false),
                        new AttackFrame(-0.03506f, 1.30997f, 0.22186f, 0.25341f, 0.38059f, 0.16739f, 0.87345f, 1f, false),
                        new AttackFrame(0.00684f, 1.18748f, 0.28833f, 0.30486f, 0.49309f, 0.14402f, 0.80199f, 1f, false),
                        new AttackFrame(0.09925f, 0.9997f, 0.26514f, 0.33166f, 0.58433f, 0.12015f, 0.73084f, 1f, false),
                        new AttackFrame(0.17424f, 0.92232f, 0.18766f, 0.31937f, 0.62703f, 0.12191f, 0.69998f, 1f, false),
                        new AttackFrame(0.21594f, 0.90556f, 0.14829f, 0.29371f, 0.64685f, 0.13705f, 0.69031f, 1f, false),
                        new AttackFrame(0.24236f, 0.90265f, 0.13191f, 0.26797f, 0.65725f, 0.1549f, 0.68718f, 1f, false),
                        new AttackFrame(0.25983f, 0.90312f, 0.12538f, 0.24704f, 0.66273f, 0.17064f, 0.68604f, 1f, false),
                        new AttackFrame(0.2702f, 0.90414f, 0.12303f, 0.23331f, 0.66531f, 0.18149f, 0.68556f, 1f, false),
                        new AttackFrame(0.27375f, 0.9046f, 0.12246f, 0.22841f, 0.66607f, 0.18546f, 0.68541f, 1f, false)
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
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.12639f, 1.41419f, 0.63955f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.15038f, 1.41156f, 1.10559f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.17436f, 1.40893f, 1.57163f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.19835f, 1.4063f, 2.03767f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.22233f, 1.40367f, 2.50372f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.24632f, 1.40104f, 2.96976f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.27031f, 1.39841f, 3.4358f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.29429f, 1.39578f, 3.90184f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.31828f, 1.39314f, 4.36789f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.34226f, 1.39051f, 4.83393f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.36625f, 1.38788f, 5.29997f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.39023f, 1.38525f, 5.76601f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.41422f, 1.38262f, 6.23206f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.4382f, 1.37999f, 6.6981f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.46219f, 1.37736f, 7.16414f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.48617f, 1.37473f, 7.63018f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.51016f, 1.3721f, 8.09623f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true)
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
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.10241f, 1.41682f, 0.1735f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, false),
                        new AttackFrame(-0.12639f, 1.41419f, 0.63955f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.15038f, 1.41156f, 1.10559f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.17436f, 1.40893f, 1.57163f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.19835f, 1.4063f, 2.03767f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.22233f, 1.40367f, 2.50372f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.24632f, 1.40104f, 2.96976f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.27031f, 1.39841f, 3.4358f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.29429f, 1.39578f, 3.90184f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.31828f, 1.39314f, 4.36789f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.34226f, 1.39051f, 4.83393f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.36625f, 1.38788f, 5.29997f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.39023f, 1.38525f, 5.76601f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.41422f, 1.38262f, 6.23206f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.4382f, 1.37999f, 6.6981f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.46219f, 1.37736f, 7.16414f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.48617f, 1.37473f, 7.63018f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true),
                        new AttackFrame(-0.51016f, 1.3721f, 8.09623f, 0.00282f, -0.02571f, -0.00007f, 0.99967f, 1f, true)
                    },
                    "hero-blue trail behind the arrow")
            });
    }
}
