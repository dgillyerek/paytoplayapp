using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill fresh rebuild bow attack (2026-10-13): f0 rest, f6 bow raised and arrow nocked, f10 full draw (left arm toward the
    /// target, RightHand at the chin by 2-bone IK), f14 loose (arrow flies nock to grip at 14 m/s with a purple trail), f17 string-hand
    /// recoil, f30 rest. LeftHand keeps its full-draw rotation in every clip, so the bow stays upright in rest, walk and attack.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../fresh_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "d6c5fa40a430a89c4dc77ed44ba3a087";
        public const string DesignDir = BonequillMotion.DesignDir;
        public const int ReleaseFrame = 14;
        public const bool InPlace = false;

        /// <summary>Prop FBXs from #47 (attack_20261007), unchanged.</summary>
        public static readonly string[] PropFileNames =
        {
            "BONEQUILL_bonearrow_purple_fletch.fbx",
            "BONEQUILL_bow.fbx"
        };

        public static readonly string[] PropFileMd5s =
        {
            "9d8abdd529785dae22fcc01149e50dd3",
            "3b25e1c4d1f4f418fb00540e4e29345f"
        };

        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "BONEQUILL_bow_rest",
                "BONEQUILL_bow.fbx",
                "3b25e1c4d1f4f418fb00540e4e29345f",
                "mixamorig:LeftHand",
                new[] { -0.17549f, 0.93199f, 0.08329f, 0f, 0f, 0f, 1f },
                new[] { 0.048f, 1.41f, 0.22335f },
                "Bone bow on LeftHand in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's BONEQUILL_bow track so one bow serves every clip.",
                "BONEQUILL_bow")
        };

        public static readonly BlenderRigAttackSpec Spec = new BlenderRigAttackSpec(
            "Bonequill",
            FileName,
            FileMd5,
            DesignDir,
            ReleaseFrame,
            InPlace,
            new[] { "mixamorig:Hips", "mixamorig:Head", "mixamorig:LeftHand", "mixamorig:RightHand" },
            new[] { 0f, 0.95f, -0.04f, 0f, 1.525f, -0.04f, -0.55f, 1.07f, 0f, 0.55f, 1.07f, 0f },
            new[]
            {
                new AttackTrack(
                    "BONEQUILL_bow",
                    AttackTrackKind.Held,
                    "BONEQUILL_bow.fbx",
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
                    new[] { 0.048f, 1.41f, 0.22335f },
                    new[]
                    {
                        new AttackFrame(-0.17549f, 0.93199f, 0.08329f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.16277f, 0.93965f, 0.11829f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12545f, 0.97842f, 0.22034f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.07307f, 1.08982f, 0.36189f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.03622f, 1.25012f, 0.45377f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02232f, 1.36324f, 0.47867f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01919f, 1.40158f, 0.48123f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01518f, 1.40729f, 0.48022f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00646f, 1.41987f, 0.47767f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.0021f, 1.43246f, 0.47468f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00594f, 1.43818f, 0.47318f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00544f, 1.42968f, 0.47347f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00371f, 1.40471f, 0.47354f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00001f, 1.36283f, 0.47101f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00719f, 1.30331f, 0.46149f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01995f, 1.22781f, 0.4386f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04021f, 1.14417f, 0.39635f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.06746f, 1.06678f, 0.33543f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.09751f, 1.00815f, 0.2663f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.12514f, 0.97074f, 0.202f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.14721f, 0.94957f, 0.15024f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.16289f, 0.93858f, 0.11325f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17229f, 0.93349f, 0.09093f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17549f, 0.93199f, 0.08329f, 0f, 0f, 0f, 1f, 1f, true)
                    },
                    "bone bow gripped 6 cm along LeftHand, world-upright at full draw; the LYRA-style idle prop stands in for it"),
                new AttackTrack(
                    "BONEQUILL_bonearrow_purple_fletch",
                    AttackTrackKind.Held,
                    "BONEQUILL_bonearrow_purple_fletch.fbx",
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
                    new[] { 0.03886f, 0.0343f, 0.7925f },
                    new[]
                    {
                        new AttackFrame(0.18696f, 0.89844f, 0.0246f, -0.36459f, -0.92505f, 0.06347f, 0.08557f, 1f, false),
                        new AttackFrame(0.16626f, 0.90271f, 0.05528f, -0.39219f, -0.91559f, 0.06637f, 0.0589f, 1f, false),
                        new AttackFrame(0.10574f, 0.93619f, 0.14691f, 0.46079f, 0.88331f, -0.07492f, 0.04263f, 1f, false),
                        new AttackFrame(0.02184f, 1.05623f, 0.27375f, 0.50497f, 0.81147f, -0.10887f, 0.27325f, 1f, false),
                        new AttackFrame(-0.037f, 1.23878f, 0.34363f, 0.43042f, 0.68905f, -0.20219f, 0.54687f, 1f, false),
                        new AttackFrame(-0.05878f, 1.36545f, 0.35137f, 0.3169f, 0.58135f, -0.29512f, 0.68885f, 1f, true),
                        new AttackFrame(-0.0633f, 1.4075f, 0.34829f, 0.26986f, 0.54201f, -0.33011f, 0.72417f, 1f, true),
                        new AttackFrame(-0.07102f, 1.40528f, 0.32307f, 0.24002f, 0.51714f, -0.2699f, 0.77596f, 1f, true),
                        new AttackFrame(-0.06469f, 1.39778f, 0.23902f, 0.14851f, 0.39356f, -0.12045f, 0.89919f, 1f, true),
                        new AttackFrame(-0.02426f, 1.40584f, 0.13855f, 0.02804f, 0.15006f, -0.01394f, 0.98818f, 1f, true),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.01153f, 1.41319f, 0.09388f, -0.10323f, 0.0238f, 0.08621f, 0.99063f, 1f, false),
                        new AttackFrame(0.00222f, 1.41255f, 0.0648f, -0.27527f, -0.01359f, 0.25683f, 0.92633f, 1f, false),
                        new AttackFrame(0.02268f, 1.41541f, 0.03991f, -0.36096f, -0.10755f, 0.33458f, 0.86383f, 1f, false),
                        new AttackFrame(0.02172f, 1.41326f, 0.04036f, -0.33585f, -0.01183f, 0.34323f, 0.87708f, 1f, false),
                        new AttackFrame(0.0201f, 1.40409f, 0.03417f, -0.10567f, 0.561f, 0.31515f, 0.75815f, 1f, false),
                        new AttackFrame(-0.01159f, 1.38697f, 0.03214f, -0.37079f, -0.90938f, 0.04619f, 0.18277f, 1f, false),
                        new AttackFrame(-0.03797f, 1.3594f, 0.04127f, -0.42465f, -0.79734f, 0.14096f, 0.40503f, 1f, false),
                        new AttackFrame(-0.07184f, 1.29881f, 0.04679f, -0.42863f, -0.79513f, 0.14589f, 0.40343f, 1f, false),
                        new AttackFrame(-0.09278f, 1.17547f, 0.04818f, -0.41738f, -0.83971f, 0.1213f, 0.32553f, 1f, false),
                        new AttackFrame(-0.03938f, 1.01959f, 0.0407f, -0.3973f, -0.88395f, 0.0906f, 0.22929f, 1f, false),
                        new AttackFrame(0.05237f, 0.93546f, 0.03125f, -0.38163f, -0.90677f, 0.07236f, 0.16396f, 1f, false),
                        new AttackFrame(0.11581f, 0.90841f, 0.02638f, -0.373f, -0.91669f, 0.06506f, 0.12772f, 1f, false),
                        new AttackFrame(0.15229f, 0.90068f, 0.02468f, -0.36849f, -0.92132f, 0.06291f, 0.10693f, 1f, false),
                        new AttackFrame(0.17283f, 0.89872f, 0.02437f, -0.36611f, -0.92362f, 0.06281f, 0.0946f, 1f, false),
                        new AttackFrame(0.18354f, 0.89842f, 0.0245f, -0.36495f, -0.92472f, 0.06324f, 0.08781f, 1f, false),
                        new AttackFrame(0.18696f, 0.89844f, 0.0246f, -0.36459f, -0.92505f, 0.06347f, 0.08557f, 1f, false)
                    },
                    "arrow nocked f5, drawn to the chin by RightHand at f10, held to f13"),
                new AttackTrack(
                    "BONEQUILL_bonearrow_purple_fletch_inflight",
                    AttackTrackKind.Projectile,
                    "BONEQUILL_bonearrow_purple_fletch.fbx",
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
                    new[] { 0.03886f, 0.0343f, 0.7925f },
                    new[]
                    {
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(0.0078f, 1.44325f, 0.56218f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.01748f, 1.46978f, 1.02799f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.02717f, 1.49631f, 1.49381f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.03686f, 1.52284f, 1.95962f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.04655f, 1.54937f, 2.42543f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.05624f, 1.57589f, 2.89124f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.06593f, 1.60242f, 3.35705f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.07562f, 1.62895f, 3.82286f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.08531f, 1.65548f, 4.28867f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.095f, 1.68201f, 4.75449f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.10469f, 1.70854f, 5.2203f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.11437f, 1.73507f, 5.68611f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.12406f, 1.7616f, 6.15192f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.13375f, 1.78812f, 6.61773f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.14344f, 1.81465f, 7.08354f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.15313f, 1.84118f, 7.54935f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.16282f, 1.86771f, 8.01516f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true)
                    },
                    "loosed at f14 along nock to grip (character-forward), 14 m/s"),
                new AttackTrack(
                    "BONEQUILL_arrow_trail_vfx",
                    AttackTrackKind.Projectile,
                    "",
                    "",
                    0,
                    14,
                    14f,
                    AttackVfxShape.Trail,
                    0.05f,
                    0.9f,
                    new[] { 0.82666f, 0.48453f, 1f },
                    new[] { 0.70141f, 0.2717f, 0.85431f },
                    new[] { 0.82666f, 0.48453f, 1f },
                    new[] { 0.03f, 0.02853f, 0.9f },
                    new[]
                    {
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(-0.00189f, 1.41672f, 0.09637f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, false),
                        new AttackFrame(0.0078f, 1.44325f, 0.56218f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.01748f, 1.46978f, 1.02799f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.02717f, 1.49631f, 1.49381f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.03686f, 1.52284f, 1.95962f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.04655f, 1.54937f, 2.42543f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.05624f, 1.57589f, 2.89124f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.06593f, 1.60242f, 3.35705f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.07562f, 1.62895f, 3.82286f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.08531f, 1.65548f, 4.28867f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.095f, 1.68201f, 4.75449f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.10469f, 1.70854f, 5.2203f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.11437f, 1.73507f, 5.68611f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.12406f, 1.7616f, 6.15192f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.13375f, 1.78812f, 6.61773f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.14344f, 1.81465f, 7.08354f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.15313f, 1.84118f, 7.54935f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true),
                        new AttackFrame(0.16282f, 1.86771f, 8.01516f, -0.02844f, 0.01038f, -0.0003f, 0.99954f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
