using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill gritty rebuild bow attack (2026-10-14, archer stance): f0 rest, f6 raise/nock, f10 full draw with the torso yawed
    /// ~55 deg so the bow shoulder points at the target; LeftArm straight out, RightHand at the jaw with the elbow out to the side
    /// (nothing through the ribcage or the cloak), arrow nocked between the RightHand draw fingers and re-aimed every frame through the arrow rest on top of the bow hand (f7 nock -> f13), f14 loose (arrow flies straight at the target at 14 m/s with a purple trail), f17 string-hand
    /// recoil, f30 rest. LeftHand keeps its full-draw rotation in every clip, so the bow stays upright in rest, walk and attack;
    /// the bow hand is sculpted into a fist (no finger bones) and rolled so the grip sits across the palm with the fingers wrapped around it.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "eb7cd5ed5275304568eb61af9307ee1a";
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
                        new AttackFrame(-0.23036f, 0.9033f, 0.13796f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23614f, 0.93034f, 0.25133f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23309f, 1.00507f, 0.4043f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.21604f, 1.11461f, 0.52701f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.19685f, 1.20016f, 0.58857f, 0f, 0f, 0f, 1f, 1f, true),
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
                        new AttackFrame(0.00785f, 1.43446f, 0.67223f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(0.0039f, 1.42682f, 0.67176f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00731f, 1.40391f, 0.66976f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02517f, 1.36387f, 0.66414f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04959f, 1.30338f, 0.65021f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.08069f, 1.22071f, 0.61914f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.11714f, 1.12296f, 0.55944f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.1537f, 1.03119f, 0.46865f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.18346f, 0.96583f, 0.36426f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.20375f, 0.92873f, 0.26887f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.21602f, 0.91047f, 0.19351f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22273f, 0.90229f, 0.14004f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.2259f, 0.89902f, 0.10777f, 0f, 0f, 0f, 1f, 1f, true),
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
                        new AttackFrame(0.27735f, 0.83458f, -0.04385f, -0.29024f, -0.53982f, 0.20533f, 0.76302f, 1f, false),
                        new AttackFrame(0.26678f, 0.83744f, -0.0106f, -0.28456f, -0.53497f, 0.19755f, 0.77058f, 1f, false),
                        new AttackFrame(0.2312f, 0.86953f, 0.09497f, -0.26871f, -0.52938f, 0.18144f, 0.78398f, 1f, false),
                        new AttackFrame(0.16767f, 0.99399f, 0.25235f, -0.17596f, -0.54932f, 0.11962f, 0.80807f, 1f, false),
                        new AttackFrame(0.10537f, 1.18758f, 0.35212f, 0.04517f, -0.51758f, -0.02737f, 0.854f, 1f, false),
                        new AttackFrame(0.06822f, 1.32385f, 0.3707f, 0.14162f, -0.41979f, -0.0665f, 0.89403f, 1f, false),
                        new AttackFrame(0.0555f, 1.3691f, 0.36859f, 0.15788f, -0.37795f, -0.06558f, 0.9099f, 1f, false),
                        new AttackFrame(0.0489f, 1.38784f, 0.3537f, 0.12357f, -0.3188f, -0.04196f, 0.9388f, 1f, true),
                        new AttackFrame(0.03303f, 1.42947f, 0.31997f, 0.06191f, -0.18168f, -0.01146f, 0.98134f, 1f, true),
                        new AttackFrame(0.01515f, 1.47097f, 0.28544f, 0.01588f, -0.05304f, -0.00084f, 0.99847f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00009f, -0.00061f, 0f, 1f, 1f, false),
                        new AttackFrame(0.03537f, 1.49145f, 0.2695f, 0.0023f, -0.03721f, -0.00009f, 0.9993f, 1f, false),
                        new AttackFrame(0.08784f, 1.49532f, 0.26347f, 0.00704f, -0.10047f, -0.00071f, 0.99491f, 1f, false),
                        new AttackFrame(0.11454f, 1.49745f, 0.25749f, 0.00952f, -0.12985f, -0.00125f, 0.99149f, 1f, false),
                        new AttackFrame(0.11814f, 1.48732f, 0.25981f, 0.00652f, -0.1393f, -0.00092f, 0.99023f, 1f, false),
                        new AttackFrame(0.12796f, 1.45695f, 0.26563f, -0.00264f, -0.16564f, 0.00044f, 0.98618f, 1f, false),
                        new AttackFrame(0.14232f, 1.40418f, 0.27191f, -0.01887f, -0.2056f, 0.00396f, 0.97845f, 1f, false),
                        new AttackFrame(0.15962f, 1.32583f, 0.27279f, -0.04258f, -0.25517f, 0.01125f, 0.96589f, 1f, false),
                        new AttackFrame(0.17895f, 1.22245f, 0.25924f, -0.07134f, -0.31016f, 0.02335f, 0.94772f, 1f, false),
                        new AttackFrame(0.20039f, 1.10529f, 0.22313f, -0.10092f, -0.368f, 0.04022f, 0.92346f, 1f, false),
                        new AttackFrame(0.22325f, 0.99708f, 0.16575f, -0.13189f, -0.42435f, 0.06263f, 0.89365f, 1f, false),
                        new AttackFrame(0.24389f, 0.91816f, 0.10066f, -0.16632f, -0.47022f, 0.09073f, 0.86197f, 1f, false),
                        new AttackFrame(0.25899f, 0.87185f, 0.04363f, -0.20123f, -0.50188f, 0.12133f, 0.83241f, 1f, false),
                        new AttackFrame(0.26848f, 0.84891f, 0.00198f, -0.23365f, -0.52194f, 0.15125f, 0.8063f, 1f, false),
                        new AttackFrame(0.27386f, 0.83904f, -0.02465f, -0.2618f, -0.53333f, 0.17799f, 0.78444f, 1f, false),
                        new AttackFrame(0.27653f, 0.83544f, -0.03917f, -0.28233f, -0.53848f, 0.19772f, 0.76891f, 1f, false),
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
                        new AttackFrame(-0.00157f, 1.49069f, 6.80301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00214f, 1.49077f, 7.26968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0027f, 1.49085f, 7.73635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00327f, 1.49093f, 8.20301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true)
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
                        new AttackFrame(-0.00157f, 1.49069f, 6.80301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00214f, 1.49077f, 7.26968f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0027f, 1.49085f, 7.73635f, -0.00009f, -0.00061f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00327f, 1.49093f, 8.20301f, -0.00009f, -0.00061f, 0f, 1f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
