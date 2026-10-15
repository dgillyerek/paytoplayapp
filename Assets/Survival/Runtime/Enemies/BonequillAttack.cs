using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill gritty rebuild bow attack (2026-10-14, quiver draw + archer stance), 31 frames at 30 fps: f0 rest, f2 draw hand lifts out
    /// to the side, f4 grabs an arrow at the quiver mouth over the right shoulder (arrow prop appears in the fingers), f6 pulls it up
    /// out of the quiver, f8 hand up over the right shoulder, f10 nock, f14 full draw with the torso yawed ~55 deg (bow shoulder at the
    /// target, LeftArm straight out, RightHand at the jaw, elbow out), f17 loose (arrow flies straight at the target at 14 m/s with a
    /// purple trail), f20 string-hand recoil, f30 rest. Elbow stays high and out, so the arm never crosses the hood, cloak or ribcage
    /// (checked at every half frame); the forearm has no twist relative to the upper arm, so it cannot roll-flip.
    /// In rest and walk the bow is carried lower, tipped 45 deg out (top clear of the shoulder guard), and swings up to the full-draw
    /// grip between f0 and f10; it is back to the carry by f30. Bow-vs-body/cloak clearance is checked at every half frame of every clip
    /// (work/bow_clearance.json, BonequillBowClearanceTests).
    /// The bow hand is sculpted into a fist (no finger bones) and rolled so the grip sits across the palm with the fingers wrapped around it.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "b4c84740cbeeae85ef7aeb8ae13b3e06";
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
                new[] { -0.1548f, 1.02324f, 0.20243f, -0.06445f, -0.02669f, 0.38175f, 0.92163f },
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
                        new AttackFrame(-0.1548f, 1.02324f, 0.20243f, -0.06445f, -0.02669f, 0.38175f, 0.92163f, 1f, true),
                        new AttackFrame(-0.15649f, 1.02315f, 0.21119f, -0.06273f, -0.02599f, 0.37161f, 0.9259f, 1f, true),
                        new AttackFrame(-0.16139f, 1.02422f, 0.23633f, -0.05805f, -0.02404f, 0.34386f, 0.93692f, 1f, true),
                        new AttackFrame(-0.1693f, 1.03025f, 0.27679f, -0.05105f, -0.02115f, 0.30239f, 0.95158f, 1f, true),
                        new AttackFrame(-0.17941f, 1.04703f, 0.32955f, -0.04241f, -0.01757f, 0.25123f, 0.96684f, 1f, true),
                        new AttackFrame(-0.19004f, 1.07886f, 0.38587f, -0.03287f, -0.01362f, 0.19473f, 0.98021f, 1f, true),
                        new AttackFrame(-0.19953f, 1.12212f, 0.43317f, -0.02322f, -0.00962f, 0.13755f, 0.99018f, 1f, true),
                        new AttackFrame(-0.20739f, 1.16598f, 0.46442f, -0.01428f, -0.00591f, 0.08458f, 0.9963f, 1f, true),
                        new AttackFrame(-0.21368f, 1.20116f, 0.48145f, -0.00688f, -0.00285f, 0.04076f, 0.99914f, 1f, true),
                        new AttackFrame(-0.21075f, 1.22353f, 0.54902f, -0.00185f, -0.00077f, 0.01098f, 0.99994f, 1f, true),
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
                        new AttackFrame(0.00361f, 1.42427f, 0.67143f, -0.00185f, -0.00077f, 0.01098f, 0.99994f, 1f, true),
                        new AttackFrame(-0.00825f, 1.39333f, 0.66739f, -0.00688f, -0.00285f, 0.04076f, 0.99914f, 1f, true),
                        new AttackFrame(-0.0269f, 1.33783f, 0.65426f, -0.01428f, -0.00591f, 0.08458f, 0.9963f, 1f, true),
                        new AttackFrame(-0.05237f, 1.2545f, 0.61885f, -0.02322f, -0.00962f, 0.13755f, 0.99018f, 1f, true),
                        new AttackFrame(-0.08332f, 1.15728f, 0.54389f, -0.03287f, -0.01362f, 0.19473f, 0.98021f, 1f, true),
                        new AttackFrame(-0.11285f, 1.08279f, 0.43805f, -0.04241f, -0.01757f, 0.25123f, 0.96684f, 1f, true),
                        new AttackFrame(-0.13404f, 1.04501f, 0.33808f, -0.05105f, -0.02115f, 0.30239f, 0.95158f, 1f, true),
                        new AttackFrame(-0.1466f, 1.02978f, 0.26384f, -0.05805f, -0.02404f, 0.34386f, 0.93692f, 1f, true),
                        new AttackFrame(-0.15289f, 1.02447f, 0.21829f, -0.06273f, -0.02599f, 0.37161f, 0.9259f, 1f, true),
                        new AttackFrame(-0.1548f, 1.02324f, 0.20243f, -0.06445f, -0.02669f, 0.38175f, 0.92163f, 1f, true)
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
                        new AttackFrame(0.12051f, 1.48038f, 0.26129f, 0.00126f, -0.14434f, -0.00018f, 0.98953f, 1f, false),
                        new AttackFrame(0.13614f, 1.42819f, 0.26964f, -0.02501f, -0.18321f, 0.00466f, 0.98274f, 1f, false),
                        new AttackFrame(0.15779f, 1.33486f, 0.27318f, -0.07344f, -0.23675f, 0.01795f, 0.96862f, 1f, false),
                        new AttackFrame(0.18305f, 1.19951f, 0.25398f, -0.13954f, -0.29095f, 0.04294f, 0.94553f, 1f, false),
                        new AttackFrame(0.21181f, 1.0485f, 0.19648f, -0.20478f, -0.33779f, 0.07555f, 0.91556f, 1f, false),
                        new AttackFrame(0.24014f, 0.93118f, 0.11345f, -0.25674f, -0.374f, 0.10856f, 0.88455f, 1f, false),
                        new AttackFrame(0.26017f, 0.86869f, 0.03873f, -0.29126f, -0.39883f, 0.13524f, 0.85896f, 1f, false),
                        new AttackFrame(0.27104f, 0.84386f, -0.01032f, -0.31321f, -0.4166f, 0.1555f, 0.83914f, 1f, false),
                        new AttackFrame(0.27595f, 0.83612f, -0.03592f, -0.32736f, -0.42798f, 0.1698f, 0.82513f, 1f, false),
                        new AttackFrame(0.27735f, 0.83458f, -0.04385f, -0.33276f, -0.43201f, 0.17538f, 0.81968f, 1f, false)
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
