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
        public const string FileMd5 = "cde854b919aca9136169fa7a76e3c952";
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
            345f,   // rest/walk carry: string resting 0.8 cm off the inside (body side) of the forearm, bow limb out (anim.py CARRY)
            390f,   // nock to release: string toward him (= 30 deg); eases +45 from 345 so the string turns off the forearm, never through it
            new[] { 2f, 10f, 17f, 26f });

        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "BONEQUILL_bow_rest",
                "BONEQUILL_bow.fbx",
                "3b25e1c4d1f4f418fb00540e4e29345f",
                "mixamorig:LeftHand",
                new[] { -0.34593f, 0.88062f, 0.12003f, 0.03412f, -0.61994f, 0.41017f, 0.66803f },
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
                        new AttackFrame(-0.34593f, 0.88062f, 0.12003f, 0.03412f, -0.61994f, 0.41017f, 0.66803f, 1f, true),
                        new AttackFrame(-0.34597f, 0.88465f, 0.13412f, 0.03135f, -0.60067f, 0.40324f, 0.68965f, 1f, true),
                        new AttackFrame(-0.34481f, 0.89731f, 0.17359f, 0.02368f, -0.54568f, 0.38264f, 0.74515f, 1f, true),
                        new AttackFrame(-0.33954f, 0.92135f, 0.23419f, 0.01797f, -0.47195f, 0.34748f, 0.81006f, 1f, true),
                        new AttackFrame(-0.32709f, 0.96068f, 0.30855f, 0.01616f, -0.39729f, 0.29829f, 0.86771f, 1f, true),
                        new AttackFrame(-0.30619f, 1.01659f, 0.38315f, 0.01203f, -0.32604f, 0.23861f, 0.91467f, 1f, true),
                        new AttackFrame(-0.27943f, 1.08207f, 0.44208f, 0.0015f, -0.26196f, 0.17493f, 0.94909f, 1f, true),
                        new AttackFrame(-0.25273f, 1.14374f, 0.47766f, -0.01573f, -0.20797f, 0.11533f, 0.97118f, 1f, true),
                        new AttackFrame(-0.23146f, 1.19118f, 0.49378f, -0.03592f, -0.16648f, 0.06722f, 0.98309f, 1f, true),
                        new AttackFrame(-0.2109f, 1.22044f, 0.55863f, -0.05275f, -0.13982f, 0.03585f, 0.98812f, 1f, true),
                        new AttackFrame(-0.18242f, 1.23054f, 0.61453f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.15519f, 1.26057f, 0.63453f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.09037f, 1.33109f, 0.66672f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(-0.0228f, 1.4022f, 0.68083f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.05958f, -0.1304f, 0.02467f, 0.98936f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.0599f, -0.11707f, 0.02386f, 0.99103f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06072f, -0.08123f, 0.02169f, 0.99461f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06177f, -0.02917f, 0.01848f, 0.99749f, 1f, true),
                        new AttackFrame(-0.00247f, 1.42112f, 0.68318f, -0.05743f, 0.00631f, 0.02774f, 0.99794f, 1f, true),
                        new AttackFrame(-0.03103f, 1.38456f, 0.68497f, -0.04663f, -0.00017f, 0.06029f, 0.99709f, 1f, true),
                        new AttackFrame(-0.07735f, 1.31681f, 0.67748f, -0.03465f, -0.04401f, 0.11113f, 0.99223f, 1f, true),
                        new AttackFrame(-0.14014f, 1.21258f, 0.64181f, -0.02411f, -0.12033f, 0.17327f, 0.9772f, 1f, true),
                        new AttackFrame(-0.21149f, 1.08797f, 0.55467f, -0.01442f, -0.22292f, 0.23848f, 0.94511f, 1f, true),
                        new AttackFrame(-0.273f, 0.98667f, 0.42507f, -0.00217f, -0.34333f, 0.29872f, 0.89044f, 1f, true),
                        new AttackFrame(-0.31304f, 0.92803f, 0.29816f, 0.0121f, -0.45822f, 0.34774f, 0.8179f, 1f, true),
                        new AttackFrame(-0.33435f, 0.89854f, 0.20134f, 0.02368f, -0.54569f, 0.38264f, 0.74515f, 1f, true),
                        new AttackFrame(-0.34353f, 0.88477f, 0.14111f, 0.03135f, -0.60067f, 0.40324f, 0.68965f, 1f, true),
                        new AttackFrame(-0.34593f, 0.88062f, 0.12003f, 0.03412f, -0.61994f, 0.41017f, 0.66803f, 1f, true)
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
                        new AttackFrame(0.123f, 1.48153f, 0.26157f, 0.00412f, -0.14976f, -0.00062f, 0.98871f, 1f, false),
                        new AttackFrame(0.14609f, 1.43271f, 0.27115f, -0.0103f, -0.21001f, 0.00221f, 0.97764f, 1f, false),
                        new AttackFrame(0.18114f, 1.34448f, 0.27799f, -0.03403f, -0.2954f, 0.01053f, 0.95471f, 1f, false),
                        new AttackFrame(0.22805f, 1.21343f, 0.26445f, -0.06515f, -0.38852f, 0.02755f, 0.91872f, 1f, false),
                        new AttackFrame(0.28673f, 1.06373f, 0.20931f, -0.09617f, -0.4709f, 0.05173f, 0.8754f, 1f, false),
                        new AttackFrame(0.34229f, 0.9495f, 0.1216f, -0.1185f, -0.53076f, 0.07525f, 0.83582f, 1f, false),
                        new AttackFrame(0.37645f, 0.89163f, 0.04212f, -0.13101f, -0.57166f, 0.09308f, 0.8046f, 1f, false),
                        new AttackFrame(0.39157f, 0.86949f, -0.00925f, -0.13929f, -0.60054f, 0.10724f, 0.78004f, 1f, false),
                        new AttackFrame(0.39688f, 0.86278f, -0.03579f, -0.14687f, -0.61862f, 0.11914f, 0.76259f, 1f, false),
                        new AttackFrame(0.39807f, 0.86148f, -0.04399f, -0.15077f, -0.62491f, 0.12466f, 0.75579f, 1f, false)
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
