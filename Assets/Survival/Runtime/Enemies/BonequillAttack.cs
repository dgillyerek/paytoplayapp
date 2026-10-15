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
        public const string FileMd5 = "a153e5e1efc83337d06b5a3cb7054b6c";
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
        /// (work/bowroll.py): carried along the inner forearm with the string outward, turned toward him only from nock to release
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
            270f,   // rest/walk carry: bow along the inner forearm, string facing outward (away from body and legs)
            30f,    // nock to release: string toward him (eases 270 -> 30 through 150, the string sweeping round the outside)
            new[] { 2f, 10f, 17f, 26f });

        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "BONEQUILL_bow_rest",
                "BONEQUILL_bow.fbx",
                "3b25e1c4d1f4f418fb00540e4e29345f",
                "mixamorig:LeftHand",
                new[] { -0.13786f, 0.93167f, 0.15615f, -0.18149f, 0.70605f, -0.03858f, 0.68342f },
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
                        new AttackFrame(-0.13786f, 0.93167f, 0.15615f, -0.18149f, 0.70605f, -0.03858f, 0.68342f, 1f, true),
                        new AttackFrame(-0.13987f, 0.93261f, 0.16685f, -0.1779f, 0.70875f, -0.03861f, 0.68157f, 1f, true),
                        new AttackFrame(-0.14573f, 0.93698f, 0.1976f, -0.16813f, 0.71599f, -0.0387f, 0.67646f, 1f, true),
                        new AttackFrame(-0.15536f, 0.94942f, 0.24694f, -0.14956f, 0.78359f, -0.05248f, 0.60072f, 1f, true),
                        new AttackFrame(-0.16806f, 0.97664f, 0.31077f, -0.1163f, 0.91135f, -0.08061f, 0.38654f, 1f, true),
                        new AttackFrame(-0.18184f, 1.02294f, 0.37815f, -0.06801f, 0.99121f, -0.10263f, 0.04837f, 1f, true),
                        new AttackFrame(-0.19403f, 1.08288f, 0.434f, 0.0148f, -0.93328f, 0.10394f, 0.34347f, 1f, true),
                        new AttackFrame(-0.20312f, 1.1425f, 0.47021f, -0.0278f, -0.72703f, 0.08424f, 0.68085f, 1f, true),
                        new AttackFrame(-0.20909f, 1.18998f, 0.48926f, -0.05114f, -0.45164f, 0.05594f, 0.88898f, 1f, true),
                        new AttackFrame(-0.2052f, 1.22001f, 0.55725f, -0.05866f, -0.22183f, 0.03327f, 0.97275f, 1f, true),
                        new AttackFrame(-0.18242f, 1.23054f, 0.61453f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.15519f, 1.26057f, 0.63453f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.09037f, 1.33109f, 0.66672f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.0228f, 1.4022f, 0.68083f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05765f, -0.20106f, 0.02888f, 0.97745f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05106f, -0.38434f, 0.03937f, 0.92094f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.03826f, -0.62285f, 0.0519f, 0.77968f, 1f, true),
                        new AttackFrame(0.00328f, 1.42074f, 0.68102f, -0.01809f, -0.84363f, 0.06497f, 0.53268f, 1f, true),
                        new AttackFrame(-0.00801f, 1.38368f, 0.67746f, 0.01156f, -0.97584f, 0.07491f, 0.20491f, 1f, true),
                        new AttackFrame(-0.02459f, 1.31682f, 0.66378f, -0.04786f, 0.98685f, -0.07469f, 0.13516f, 1f, true),
                        new AttackFrame(-0.04579f, 1.21666f, 0.62447f, -0.0838f, 0.90384f, -0.06324f, 0.41479f, 1f, true),
                        new AttackFrame(-0.07135f, 1.10001f, 0.54036f, -0.11366f, 0.79622f, -0.04734f, 0.59234f, 1f, true),
                        new AttackFrame(-0.09718f, 1.00877f, 0.42133f, -0.13604f, 0.73874f, -0.03895f, 0.65897f, 1f, true),
                        new AttackFrame(-0.11718f, 0.9605f, 0.30821f, -0.15367f, 0.72643f, -0.03882f, 0.66871f, 1f, true),
                        new AttackFrame(-0.12962f, 0.94041f, 0.22443f, -0.16813f, 0.71599f, -0.0387f, 0.67646f, 1f, true),
                        new AttackFrame(-0.13595f, 0.93333f, 0.17365f, -0.1779f, 0.70875f, -0.03861f, 0.68157f, 1f, true),
                        new AttackFrame(-0.13786f, 0.93167f, 0.15615f, -0.18149f, 0.70605f, -0.03858f, 0.68342f, 1f, true)
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
                        new AttackFrame(0.12051f, 1.48038f, 0.26129f, 0.0033f, -0.14052f, -0.00047f, 0.99007f, 1f, false),
                        new AttackFrame(0.13614f, 1.42819f, 0.26964f, -0.01508f, -0.17543f, 0.00269f, 0.98437f, 1f, false),
                        new AttackFrame(0.15779f, 1.33486f, 0.27318f, -0.04897f, -0.22434f, 0.01129f, 0.97321f, 1f, false),
                        new AttackFrame(0.18305f, 1.19951f, 0.25398f, -0.09626f, -0.27889f, 0.02811f, 0.95507f, 1f, false),
                        new AttackFrame(0.21181f, 1.0485f, 0.19648f, -0.14631f, -0.33608f, 0.05294f, 0.92889f, 1f, false),
                        new AttackFrame(0.24014f, 0.93118f, 0.11345f, -0.19283f, -0.38966f, 0.0838f, 0.89664f, 1f, false),
                        new AttackFrame(0.26017f, 0.86869f, 0.03873f, -0.23109f, -0.42984f, 0.1148f, 0.86525f, 1f, false),
                        new AttackFrame(0.27104f, 0.84386f, -0.01032f, -0.26087f, -0.45695f, 0.14218f, 0.8384f, 1f, false),
                        new AttackFrame(0.27595f, 0.83612f, -0.03592f, -0.28252f, -0.47256f, 0.16307f, 0.81871f, 1f, false),
                        new AttackFrame(0.27735f, 0.83458f, -0.04385f, -0.29117f, -0.47764f, 0.1715f, 0.81096f, 1f, false)
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
