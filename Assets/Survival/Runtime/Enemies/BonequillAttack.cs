using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill gritty rebuild bow attack (2026-10-14, archer stance): f0 rest, f6 raise/nock, f10 full draw with the torso yawed
    /// ~55 deg so the bow shoulder points at the target; LeftArm straight out, RightHand at the jaw with the elbow out to the side
    /// (nothing through the ribcage or the cloak), f14 loose (arrow flies nock to grip at 14 m/s with a purple trail), f17 string-hand
    /// recoil, f30 rest. LeftHand keeps its full-draw rotation in every clip, so the bow stays upright in rest, walk and attack.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "06fa439afb1f8fb0d2501824f4631bb2";
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
                new[] { -0.25935f, 0.97784f, 0.06703f, 0f, 0f, 0f, 1f },
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
            new[] { 0f, 1f, -0.03f, 0f, 1.575f, -0.035f, -0.335f, 0.97f, -0.03f, 0.335f, 0.97f, -0.03f },
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
                        new AttackFrame(-0.25935f, 0.97784f, 0.06703f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.26289f, 0.98298f, 0.1083f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.26867f, 1.01002f, 0.22167f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.26562f, 1.08475f, 0.37464f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.24857f, 1.19429f, 0.49735f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22938f, 1.27984f, 0.55891f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22102f, 1.31107f, 0.57665f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17721f, 1.34062f, 0.59808f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.07218f, 1.41345f, 0.62587f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.03235f, 1.48722f, 0.62365f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07589f, 1.51773f, 0.61431f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.07222f, 1.51017f, 0.61418f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.06166f, 1.48739f, 0.61325f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.04434f, 1.44729f, 0.60969f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.01918f, 1.38595f, 0.5992f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0163f, 1.30072f, 0.57291f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.06402f, 1.19858f, 0.51787f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.11925f, 1.1033f, 0.4297f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.16996f, 1.03796f, 0.32679f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.20806f, 1.00308f, 0.23334f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23335f, 0.98714f, 0.16021f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.24867f, 0.98066f, 0.10867f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.25678f, 0.97837f, 0.07766f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.25935f, 0.97784f, 0.06703f, 0f, 0f, 0f, 1f, 1f, true)
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
                        new AttackFrame(0.28587f, 0.92425f, 0.00385f, 0.71971f, 0.33452f, 0.1585f, 0.58736f, 1f, false),
                        new AttackFrame(0.27945f, 0.92967f, 0.0309f, 0.70683f, 0.32469f, 0.13298f, 0.61424f, 1f, false),
                        new AttackFrame(0.25455f, 0.96628f, 0.11693f, 0.6534f, 0.27852f, 0.07356f, 0.70006f, 1f, false),
                        new AttackFrame(0.20354f, 1.08869f, 0.24079f, 0.51202f, 0.18262f, 0.02379f, 0.839f, 1f, false),
                        new AttackFrame(0.1496f, 1.26734f, 0.30661f, 0.31005f, 0.10573f, 0.00513f, 0.94481f, 1f, false),
                        new AttackFrame(0.11683f, 1.38754f, 0.3077f, 0.17844f, 0.07801f, -0.01196f, 0.98078f, 1f, true),
                        new AttackFrame(0.1057f, 1.42662f, 0.30107f, 0.13833f, 0.07111f, -0.0218f, 0.98759f, 1f, true),
                        new AttackFrame(0.10047f, 1.44323f, 0.28543f, 0.12302f, 0.06086f, -0.02154f, 0.9903f, 1f, true),
                        new AttackFrame(0.0869f, 1.48f, 0.24973f, 0.08445f, 0.04028f, -0.01632f, 0.99548f, 1f, true),
                        new AttackFrame(0.07047f, 1.51627f, 0.21281f, 0.0396f, 0.02305f, -0.0058f, 0.99893f, 1f, true),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.0844f, 1.53266f, 0.19022f, -0.01575f, 0.04283f, 0.03208f, 0.99844f, 1f, false),
                        new AttackFrame(0.12435f, 1.53245f, 0.17585f, -0.07074f, 0.09076f, 0.08849f, 0.98941f, 1f, false),
                        new AttackFrame(0.14449f, 1.53197f, 0.16639f, -0.09632f, 0.11555f, 0.1167f, 0.98171f, 1f, false),
                        new AttackFrame(0.14823f, 1.52371f, 0.16948f, -0.08598f, 0.11387f, 0.11728f, 0.9828f, 1f, false),
                        new AttackFrame(0.15858f, 1.49893f, 0.17795f, -0.05364f, 0.1089f, 0.11731f, 0.98565f, 1f, false),
                        new AttackFrame(0.17416f, 1.45564f, 0.1899f, 0.00665f, 0.10131f, 0.11248f, 0.98845f, 1f, false),
                        new AttackFrame(0.19355f, 1.39029f, 0.2015f, 0.10349f, 0.09496f, 0.09717f, 0.98531f, 1f, false),
                        new AttackFrame(0.21546f, 1.30097f, 0.20549f, 0.23947f, 0.10033f, 0.06848f, 0.96328f, 1f, false),
                        new AttackFrame(0.23848f, 1.19459f, 0.1921f, 0.39507f, 0.13157f, 0.03499f, 0.90851f, 1f, false),
                        new AttackFrame(0.2599f, 1.0918f, 0.15719f, 0.52993f, 0.18855f, 0.0175f, 0.82663f, 1f, false),
                        new AttackFrame(0.27573f, 1.01445f, 0.11074f, 0.61949f, 0.24923f, 0.02799f, 0.74387f, 1f, false),
                        new AttackFrame(0.28429f, 0.9675f, 0.0681f, 0.67004f, 0.29388f, 0.05862f, 0.67915f, 1f, false),
                        new AttackFrame(0.28726f, 0.94277f, 0.037f, 0.69697f, 0.31909f, 0.09537f, 0.63508f, 1f, false),
                        new AttackFrame(0.28726f, 0.93086f, 0.01751f, 0.71097f, 0.33026f, 0.128f, 0.6075f, 1f, false),
                        new AttackFrame(0.28635f, 0.9257f, 0.00714f, 0.71769f, 0.33388f, 0.15035f, 0.59232f, 1f, false),
                        new AttackFrame(0.28587f, 0.92425f, 0.00385f, 0.71971f, 0.33452f, 0.1585f, 0.58736f, 1f, false)
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
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.07747f, 1.51607f, 0.66199f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.09285f, 1.4998f, 1.12811f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.10823f, 1.48353f, 1.59424f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.12361f, 1.46726f, 2.06037f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.13899f, 1.45099f, 2.5265f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.15438f, 1.43472f, 2.99263f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.16976f, 1.41846f, 3.45876f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.18514f, 1.40219f, 3.92489f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.20052f, 1.38592f, 4.39102f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.2159f, 1.36965f, 4.85715f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.23129f, 1.35338f, 5.32328f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.24667f, 1.33712f, 5.78941f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.26205f, 1.32085f, 6.25554f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.27743f, 1.30458f, 6.72167f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.29281f, 1.28831f, 7.1878f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.3082f, 1.27204f, 7.65393f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.32358f, 1.25577f, 8.12005f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true)
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
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.06208f, 1.53233f, 0.19586f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, false),
                        new AttackFrame(0.07747f, 1.51607f, 0.66199f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.09285f, 1.4998f, 1.12811f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.10823f, 1.48353f, 1.59424f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.12361f, 1.46726f, 2.06037f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.13899f, 1.45099f, 2.5265f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.15438f, 1.43472f, 2.99263f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.16976f, 1.41846f, 3.45876f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.18514f, 1.40219f, 3.92489f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.20052f, 1.38592f, 4.39102f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.2159f, 1.36965f, 4.85715f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.23129f, 1.35338f, 5.32328f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.24667f, 1.33712f, 5.78941f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.26205f, 1.32085f, 6.25554f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.27743f, 1.30458f, 6.72167f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.29281f, 1.28831f, 7.1878f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.3082f, 1.27204f, 7.65393f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true),
                        new AttackFrame(0.32358f, 1.25577f, 8.12005f, 0.01744f, 0.01648f, 0.00029f, 0.99971f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
