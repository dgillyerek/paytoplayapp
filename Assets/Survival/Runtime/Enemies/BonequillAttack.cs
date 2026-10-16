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
    /// The bow hand closes on real finger bones (LeftHandFingers1-3, LeftHandThumb1-2, keyed in every clip): grip seated in the palm,
    /// fingers curled around it, thumb over the top; the hand is rolled so the grip runs across the palm.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "49664b9163204856cab9bf97f6130c6c";
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

        /// <summary>Bow grip in mixamorig:LeftHand bone space from Blender (design work/grip_unity.json): pivot at the grip
        /// centre on the hand centreline, bow axis along the knuckle line, limb in the palm. Rolled about the long axis per frame
        /// (work/bowroll.py): carried with the string resting on the inside of the forearm and the bow limb out, turned toward him only from nock to release
        /// (work/string_clearance.json). The carry arm pose is anim.py CARRY. Unity parents the bow to the
        /// hand bone at this offset (mirror-X bone-space conversion) instead of fitting a world pose.</summary>
        public static readonly BlenderBoneGrip BowGrip = new BlenderBoneGrip(
            new[] { 0.04200f, 0.09400f, -0.08380f },
            new[] { 0f, 0f, 1f, 0f },   // grip axis flip (180 deg about bone z); the roll about the bow's long axis is per frame below
            "mixamorig:LeftHandFingers1",
            new[] { -0.01620f, 0.08500f, -0.08380f },
            "mixamorig:LeftHandThumb1",
            new[] { 0.04439f, 0.03838f, -0.05479f },
            new[] { 0.04243f, 0.09263f, -0.08380f },
            10.5f,  // rest/walk carry: bow front to back beside the leg, natural hand; string 0.7 cm off the inside of the forearm (anim.py CARRY)
            30f,    // nock to release: string toward him; eases +19.5 from 10.5, string stays off the forearm
            new[] { 2f, 10f, 17f, 26f });

        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "BONEQUILL_bow_rest",
                "BONEQUILL_bow.fbx",
                "3b25e1c4d1f4f418fb00540e4e29345f",
                "mixamorig:LeftHand",
                new[] { -0.40784f, 0.91081f, -0.02139f, 0.62506f, -0.16161f, -0.15247f, 0.74829f },
                new[] { 0.048f, 1.41f, 0.22335f },
                "Bone bow on LeftHand in rest, walk and attack (rest_world from sheet.json). Stands in for the attack's BONEQUILL_bow track so one bow serves every clip.",
                "BONEQUILL_bow",
                BowGrip)
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
                        new AttackFrame(-0.41444f, 0.91197f, -0.02439f, 0.62656f, -0.1483f, -0.1807f, 0.74349f, 1f, true),
                        new AttackFrame(-0.41411f, 0.90993f, -0.00653f, 0.60286f, -0.14267f, -0.17872f, 0.76437f, 1f, true),
                        new AttackFrame(-0.41141f, 0.90815f, 0.04413f, 0.53711f, -0.12713f, -0.17216f, 0.81591f, 1f, true),
                        new AttackFrame(-0.40229f, 0.91627f, 0.1228f, 0.43749f, -0.11036f, -0.16242f, 0.87752f, 1f, true),
                        new AttackFrame(-0.38237f, 0.94412f, 0.21968f, 0.31791f, -0.10097f, -0.1465f, 0.93128f, 1f, true),
                        new AttackFrame(-0.35013f, 0.99544f, 0.31756f, 0.1982f, -0.09946f, -0.11914f, 0.9678f, 1f, true),
                        new AttackFrame(-0.3101f, 1.06248f, 0.39768f, 0.09624f, -0.10405f, -0.0818f, 0.98652f, 1f, true),
                        new AttackFrame(-0.27107f, 1.12961f, 0.45113f, 0.02143f, -0.11195f, -0.04174f, 0.99261f, 1f, true),
                        new AttackFrame(-0.24012f, 1.18371f, 0.4812f, -0.02616f, -0.12059f, -0.00716f, 0.99233f, 1f, true),
                        new AttackFrame(-0.21378f, 1.21788f, 0.55531f, -0.05224f, -0.12742f, 0.01251f, 0.99039f, 1f, true),
                        new AttackFrame(-0.18242f, 1.23054f, 0.61453f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.15519f, 1.26057f, 0.63453f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.09037f, 1.33109f, 0.66672f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.0228f, 1.4022f, 0.68083f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05972f, -0.12463f, 0.02432f, 0.99011f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06009f, -0.10913f, 0.02338f, 0.99193f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06061f, -0.08664f, 0.02202f, 0.99415f, 1f, true),
                        new AttackFrame(-0.00609f, 1.41937f, 0.6814f, -0.06139f, -0.06354f, 0.00465f, 0.99608f, 1f, true),
                        new AttackFrame(-0.04483f, 1.37863f, 0.67809f, -0.05608f, -0.04427f, -0.03914f, 0.99668f, 1f, true),
                        new AttackFrame(-0.10768f, 1.30614f, 0.66203f, -0.03066f, -0.02846f, -0.09956f, 0.99415f, 1f, true),
                        new AttackFrame(-0.19325f, 1.19807f, 0.6129f, 0.03069f, -0.01711f, -0.16267f, 0.98605f, 1f, true),
                        new AttackFrame(-0.28822f, 1.0717f, 0.50444f, 0.13619f, -0.01557f, -0.20991f, 0.96806f, 1f, true),
                        new AttackFrame(-0.36236f, 0.97307f, 0.34615f, 0.27348f, -0.0333f, -0.22876f, 0.93368f, 1f, true),
                        new AttackFrame(-0.40123f, 0.92431f, 0.1905f, 0.41317f, -0.06805f, -0.2235f, 0.88018f, 1f, true),
                        new AttackFrame(-0.4144f, 0.9101f, 0.07228f, 0.52759f, -0.10635f, -0.20572f, 0.81732f, 1f, true),
                        new AttackFrame(-0.41545f, 0.91032f, 0.00028f, 0.60089f, -0.1365f, -0.18813f, 0.76479f, 1f, true),
                        new AttackFrame(-0.41444f, 0.91197f, -0.02439f, 0.62656f, -0.1483f, -0.1807f, 0.74349f, 1f, true)
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
                        new AttackFrame(0.26429f, 1.0125f, -0.06777f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.3822f, 1.24647f, 0.07109f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.34328f, 1.52369f, -0.02065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.26049f, 1.62898f, -0.16028f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, false),
                        new AttackFrame(0.17328f, 1.64369f, -0.29065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.21875f, 1.75248f, -0.28626f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.26328f, 1.82369f, -0.26065f, 0.69197f, -0.1455f, -0.1455f, 0.69197f, 1f, true),
                        new AttackFrame(0.42506f, 1.83801f, -0.12217f, 0.77117f, -0.53412f, -0.08852f, 0.33491f, 1f, true),
                        new AttackFrame(0.57727f, 1.71216f, 0.12466f, -0.40739f, 0.77109f, 0.35127f, 0.34067f, 1f, true),
                        new AttackFrame(0.40311f, 1.54564f, 0.36567f, -0.06364f, 0.21195f, 0.30591f, 0.92598f, 1f, true),
                        new AttackFrame(0.0555f, 1.3691f, 0.36859f, 0.15223f, -0.36588f, -0.0608f, 0.91611f, 1f, true),
                        new AttackFrame(0.0489f, 1.38784f, 0.3537f, 0.11925f, -0.30734f, -0.03885f, 0.9433f, 1f, true),
                        new AttackFrame(0.03303f, 1.42947f, 0.31997f, 0.05974f, -0.17432f, -0.0106f, 0.98282f, 1f, true),
                        new AttackFrame(0.01515f, 1.47097f, 0.28544f, 0.0152f, -0.0511f, -0.00078f, 0.99858f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.03537f, 1.49145f, 0.2695f, 0.00203f, -0.03671f, -0.00007f, 0.99932f, 1f, false),
                        new AttackFrame(0.08784f, 1.49532f, 0.26347f, 0.00667f, -0.09861f, -0.00066f, 0.9951f, 1f, false),
                        new AttackFrame(0.11454f, 1.49745f, 0.25749f, 0.0091f, -0.12744f, -0.00117f, 0.9918f, 1f, false),
                        new AttackFrame(0.123f, 1.48153f, 0.26157f, 0.0062f, -0.15105f, -0.00095f, 0.98851f, 1f, false),
                        new AttackFrame(0.14609f, 1.43271f, 0.27115f, -0.00343f, -0.21572f, 0.00076f, 0.97645f, 1f, false),
                        new AttackFrame(0.18114f, 1.34448f, 0.27799f, -0.02225f, -0.30921f, 0.00724f, 0.95071f, 1f, false),
                        new AttackFrame(0.22805f, 1.21343f, 0.26445f, -0.05011f, -0.41144f, 0.02266f, 0.90978f, 1f, false),
                        new AttackFrame(0.28673f, 1.06373f, 0.20931f, -0.08f, -0.49936f, 0.04637f, 0.86145f, 1f, false),
                        new AttackFrame(0.34229f, 0.9495f, 0.1216f, -0.10484f, -0.56088f, 0.07188f, 0.81808f, 1f, false),
                        new AttackFrame(0.37645f, 0.89163f, 0.04212f, -0.1285f, -0.60104f, 0.09868f, 0.78263f, 1f, false),
                        new AttackFrame(0.39157f, 0.86949f, -0.00925f, -0.16391f, -0.62477f, 0.13633f, 0.75114f, 1f, false),
                        new AttackFrame(0.39688f, 0.86278f, -0.03579f, -0.21342f, -0.63085f, 0.18639f, 0.72232f, 1f, false),
                        new AttackFrame(0.39807f, 0.86148f, -0.04399f, -0.24219f, -0.62811f, 0.21501f, 0.70752f, 1f, false)
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
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00547f, 1.48984f, 0.73635f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00459f, 1.49013f, 1.20302f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00372f, 1.49041f, 1.66968f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00285f, 1.4907f, 2.13635f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00197f, 1.49098f, 2.60301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.0011f, 1.49127f, 3.06968f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00023f, 1.49155f, 3.53635f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00065f, 1.49184f, 4.00301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00152f, 1.49212f, 4.46968f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00239f, 1.49241f, 4.93634f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00327f, 1.49269f, 5.40301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00414f, 1.49298f, 5.86967f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00501f, 1.49327f, 6.33634f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00589f, 1.49355f, 6.80301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true)
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
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00031f, -0.00094f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00547f, 1.48984f, 0.73635f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00459f, 1.49013f, 1.20302f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00372f, 1.49041f, 1.66968f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00285f, 1.4907f, 2.13635f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00197f, 1.49098f, 2.60301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.0011f, 1.49127f, 3.06968f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00023f, 1.49155f, 3.53635f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00065f, 1.49184f, 4.00301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00152f, 1.49212f, 4.46968f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00239f, 1.49241f, 4.93634f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00327f, 1.49269f, 5.40301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00414f, 1.49298f, 5.86967f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00501f, 1.49327f, 6.33634f, -0.00031f, -0.00094f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00589f, 1.49355f, 6.80301f, -0.00031f, -0.00094f, 0f, 1f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
