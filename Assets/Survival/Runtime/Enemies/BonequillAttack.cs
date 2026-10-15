using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill gritty rebuild bow attack (2026-10-14, quiver draw + archer stance), 31 frames at 30 fps: f0 rest, f2 draw hand lifts out
    /// to the side, f4 grabs an arrow at the quiver mouth over the right shoulder (arrow prop appears in the fingers), f6 pulls it up
    /// out of the quiver, f8 hand up over the right shoulder, f10 nock, f14 full draw with the torso yawed ~55 deg (bow shoulder at the
    /// target, LeftArm straight out, RightHand at the jaw, elbow out), f17 loose (arrow flies straight at the target at 14 m/s with a
    /// purple trail), f20 string-hand recoil, f30 rest. Elbow stays high and out, so the arm never crosses the hood, cloak or ribcage
    /// (checked at every half frame); the forearm has no twist relative to the upper arm, so it cannot roll-flip. LeftHand keeps its
    /// full-draw rotation in every clip, so the bow stays upright in rest, walk and attack;
    /// the bow hand is sculpted into a fist (no finger bones) and rolled so the grip sits across the palm with the fingers wrapped around it.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "e298c5b44f44df86fbf2730d9e396746";
        public const string DesignDir = BonequillMotion.DesignDir;
        public const int ReleaseFrame = 17;
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
                new[] { -0.22682f, 0.89816f, 0.09669f, 0f, 0f, 0f, 1f },
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
                        new AttackFrame(-0.22682f, 0.89816f, 0.09669f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22817f, 0.8998f, 0.10859f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23172f, 0.90617f, 0.14279f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23632f, 0.92188f, 0.19773f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23999f, 0.95355f, 0.2689f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.24038f, 1.00507f, 0.34455f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23672f, 1.07046f, 0.40862f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.2308f, 1.13522f, 0.45197f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22508f, 1.18696f, 0.47643f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.21384f, 1.21984f, 0.54783f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.18849f, 1.23139f, 0.60631f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.16026f, 1.26149f, 0.62643f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.09311f, 1.3323f, 0.65853f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02331f, 1.40372f, 0.67197f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00127f, 1.42159f, 0.67138f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01725f, 1.38214f, 0.66704f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04685f, 1.31044f, 0.65219f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.08765f, 1.20188f, 0.60982f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.13588f, 1.07472f, 0.51727f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17826f, 0.97643f, 0.38501f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.2053f, 0.92623f, 0.26036f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.21925f, 0.90635f, 0.16952f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22523f, 0.89968f, 0.11526f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22682f, 0.89816f, 0.09669f, 0f, 0f, 0f, 1f, 1f, true)
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
                        new AttackFrame(0.17091f, 0.98637f, -0.06777f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.31112f, 1.21638f, 0.09157f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.34328f, 1.52369f, -0.02065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.26049f, 1.62898f, -0.16028f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.17328f, 1.64369f, -0.29065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.21875f, 1.75248f, -0.28626f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.26328f, 1.82369f, -0.26065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.42506f, 1.83801f, -0.12217f, 0.77104f, -0.53599f, -0.08785f, 0.33242f, 1f, true),
                        new AttackFrame(0.57727f, 1.71216f, 0.12466f, -0.40213f, 0.7717f, 0.34862f, 0.34818f, 1f, true),
                        new AttackFrame(0.40311f, 1.54564f, 0.36567f, -0.05421f, 0.20296f, 0.30201f, 0.92987f, 1f, true),
                        new AttackFrame(0.0555f, 1.3691f, 0.36859f, 0.15788f, -0.37795f, -0.06558f, 0.9099f, 1f, true),
                        new AttackFrame(0.0489f, 1.38784f, 0.3537f, 0.12357f, -0.3188f, -0.04196f, 0.9388f, 1f, true),
                        new AttackFrame(0.03303f, 1.42947f, 0.31997f, 0.06191f, -0.18168f, -0.01146f, 0.98134f, 1f, true),
                        new AttackFrame(0.01515f, 1.47097f, 0.28544f, 0.01588f, -0.05304f, -0.00084f, 0.99847f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.03537f, 1.49145f, 0.2695f, 0.0023f, -0.03721f, -0.00009f, 0.9993f, 1f, false),
                        new AttackFrame(0.08784f, 1.49532f, 0.26347f, 0.00704f, -0.10047f, -0.00071f, 0.99491f, 1f, false),
                        new AttackFrame(0.11454f, 1.49745f, 0.25749f, 0.00952f, -0.12985f, -0.00125f, 0.99149f, 1f, false),
                        new AttackFrame(0.12051f, 1.48038f, 0.26129f, 0.00445f, -0.14557f, -0.00065f, 0.98934f, 1f, false),
                        new AttackFrame(0.13614f, 1.42819f, 0.26964f, -0.01146f, -0.18822f, 0.0022f, 0.98206f, 1f, false),
                        new AttackFrame(0.15779f, 1.33486f, 0.27318f, -0.03991f, -0.24991f, 0.01031f, 0.96739f, 1f, false),
                        new AttackFrame(0.18305f, 1.19951f, 0.25398f, -0.07727f, -0.32157f, 0.02634f, 0.94336f, 1f, false),
                        new AttackFrame(0.21181f, 1.0485f, 0.19648f, -0.116f, -0.3969f, 0.05064f, 0.90909f, 1f, false),
                        new AttackFrame(0.24014f, 0.93118f, 0.11345f, -0.15926f, -0.4622f, 0.08478f, 0.86823f, 1f, false),
                        new AttackFrame(0.26017f, 0.86869f, 0.03873f, -0.20461f, -0.50435f, 0.12439f, 0.82963f, 1f, false),
                        new AttackFrame(0.27104f, 0.84386f, -0.01032f, -0.24555f, -0.5274f, 0.1625f, 0.79696f, 1f, false),
                        new AttackFrame(0.27595f, 0.83612f, -0.03592f, -0.27726f, -0.53744f, 0.19284f, 0.77272f, 1f, false),
                        new AttackFrame(0.27735f, 0.83458f, -0.04385f, -0.29024f, -0.53982f, 0.20533f, 0.76302f, 1f, false)
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
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00578f, 1.48964f, 0.73635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00521f, 1.48972f, 1.20302f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00465f, 1.4898f, 1.66968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00408f, 1.48988f, 2.13635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00351f, 1.48996f, 2.60302f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00295f, 1.49004f, 3.06968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00238f, 1.49012f, 3.53635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00182f, 1.4902f, 4.00302f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00125f, 1.49029f, 4.46968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00069f, 1.49037f, 4.93635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00012f, 1.49045f, 5.40301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00044f, 1.49053f, 5.86968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00101f, 1.49061f, 6.33635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00157f, 1.49069f, 6.80301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true)
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
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00578f, 1.48964f, 0.73635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00521f, 1.48972f, 1.20302f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00465f, 1.4898f, 1.66968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00408f, 1.48988f, 2.13635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00351f, 1.48996f, 2.60302f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00295f, 1.49004f, 3.06968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00238f, 1.49012f, 3.53635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00182f, 1.4902f, 4.00302f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00125f, 1.49029f, 4.46968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00069f, 1.49037f, 4.93635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00012f, 1.49045f, 5.40301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00044f, 1.49053f, 5.86968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00101f, 1.49061f, 6.33635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00157f, 1.49069f, 6.80301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
