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
        public const string FileMd5 = "c6beaeb2334a11da1c97eb4cf1fb9220";
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
        /// (work/bowroll.py): string away from the body when carried, turned toward him only from nock to release (work/string_clearance.json). Unity parents the bow to the
        /// hand bone at this offset (mirror-X bone-space conversion) instead of fitting a world pose.</summary>
        public static readonly BlenderBoneGrip BowGrip = new BlenderBoneGrip(
            new[] { 0.04200f, 0.09400f, -0.08380f },
            new[] { 0f, 0f, 1f, 0f },   // grip axis flip (180 deg about bone z); the roll about the bow's long axis is per frame below
            "mixamorig:LeftHandFingers1",
            new[] { -0.01620f, 0.08500f, -0.08380f },
            "mixamorig:LeftHandThumb1",
            new[] { 0.04439f, 0.03838f, -0.05479f },
            new[] { 0.04243f, 0.09263f, -0.08380f },
            200f,   // rest/walk carry: string on the side away from the body and legs
            30f,    // nock to release: string toward him
            new[] { 4f, 10f, 17f, 24f });

        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "BONEQUILL_bow_rest",
                "BONEQUILL_bow.fbx",
                "3b25e1c4d1f4f418fb00540e4e29345f",
                "mixamorig:LeftHand",
                new[] { -0.15073f, 1.02774f, 0.21069f, -0.17258f, 0.06964f, 0.37536f, 0.908f },
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
                        new AttackFrame(-0.15073f, 1.02774f, 0.21069f, -0.17258f, 0.06964f, 0.37536f, 0.908f, 1f, true),
                        new AttackFrame(-0.1523f, 1.02753f, 0.21946f, -0.16983f, 0.07153f, 0.36563f, 0.91233f, 1f, true),
                        new AttackFrame(-0.15692f, 1.02826f, 0.24463f, -0.16227f, 0.07662f, 0.339f, 0.92351f, 1f, true),
                        new AttackFrame(-0.16444f, 1.03376f, 0.28512f, -0.15086f, 0.08398f, 0.29919f, 0.93844f, 1f, true),
                        new AttackFrame(-0.17415f, 1.04985f, 0.3379f, -0.13661f, 0.09271f, 0.25006f, 0.95405f, 1f, true),
                        new AttackFrame(-0.18443f, 1.08088f, 0.39422f, -0.12064f, 0.10189f, 0.19576f, 0.96786f, 1f, true),
                        new AttackFrame(-0.19367f, 1.12331f, 0.4415f, -0.10425f, 0.11071f, 0.14078f, 0.97829f, 1f, true),
                        new AttackFrame(-0.20139f, 1.16639f, 0.47272f, -0.08887f, 0.11849f, 0.08982f, 0.98488f, 1f, true),
                        new AttackFrame(-0.20762f, 1.20091f, 0.48972f, -0.07601f, 0.12463f, 0.04765f, 0.98814f, 1f, true),
                        new AttackFrame(-0.20468f, 1.22283f, 0.55726f, -0.06719f, 0.12865f, 0.01898f, 0.98923f, 1f, true),
                        new AttackFrame(-0.18242f, 1.23054f, 0.61453f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(-0.15519f, 1.26057f, 0.63453f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(-0.09037f, 1.33109f, 0.66672f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(-0.0228f, 1.4022f, 0.68083f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00749f, 1.43286f, 0.68154f, -0.06393f, 0.13011f, 0.00841f, 0.9894f, 1f, true),
                        new AttackFrame(0.00327f, 1.42283f, 0.68071f, -0.06719f, 0.12865f, 0.01898f, 0.98923f, 1f, true),
                        new AttackFrame(-0.0085f, 1.39235f, 0.67657f, -0.07601f, 0.12463f, 0.04765f, 0.98814f, 1f, true),
                        new AttackFrame(-0.02699f, 1.33756f, 0.66324f, -0.08887f, 0.11849f, 0.08982f, 0.98488f, 1f, true),
                        new AttackFrame(-0.05207f, 1.25522f, 0.62756f, -0.10425f, 0.11071f, 0.14078f, 0.97829f, 1f, true),
                        new AttackFrame(-0.08219f, 1.15918f, 0.55242f, -0.12064f, 0.10189f, 0.19576f, 0.96786f, 1f, true),
                        new AttackFrame(-0.11058f, 1.0857f, 0.44655f, -0.13661f, 0.09271f, 0.25006f, 0.95405f, 1f, true),
                        new AttackFrame(-0.13084f, 1.04861f, 0.34653f, -0.15086f, 0.08398f, 0.29919f, 0.93844f, 1f, true),
                        new AttackFrame(-0.14285f, 1.03386f, 0.2722f, -0.16227f, 0.07662f, 0.339f, 0.92351f, 1f, true),
                        new AttackFrame(-0.14889f, 1.02886f, 0.22658f, -0.16983f, 0.07153f, 0.36563f, 0.91233f, 1f, true),
                        new AttackFrame(-0.15073f, 1.02774f, 0.21069f, -0.17258f, 0.06964f, 0.37536f, 0.908f, 1f, true)
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
                        new AttackFrame(0.12051f, 1.48038f, 0.26129f, 0.00083f, -0.14164f, -0.00012f, 0.98992f, 1f, false),
                        new AttackFrame(0.13614f, 1.42819f, 0.26964f, -0.02543f, -0.17978f, 0.00465f, 0.98337f, 1f, false),
                        new AttackFrame(0.15779f, 1.33486f, 0.27318f, -0.0737f, -0.23242f, 0.01767f, 0.96966f, 1f, false),
                        new AttackFrame(0.18305f, 1.19951f, 0.25398f, -0.13962f, -0.28586f, 0.04214f, 0.94711f, 1f, false),
                        new AttackFrame(0.21181f, 1.0485f, 0.19648f, -0.20488f, -0.33201f, 0.07412f, 0.91777f, 1f, false),
                        new AttackFrame(0.24014f, 0.93118f, 0.11345f, -0.25679f, -0.36769f, 0.10639f, 0.88744f, 1f, false),
                        new AttackFrame(0.26017f, 0.86869f, 0.03873f, -0.29122f, -0.39231f, 0.13248f, 0.8624f, 1f, false),
                        new AttackFrame(0.27104f, 0.84386f, -0.01032f, -0.31311f, -0.41001f, 0.15229f, 0.843f, 1f, false),
                        new AttackFrame(0.27595f, 0.83612f, -0.03592f, -0.32718f, -0.42141f, 0.16626f, 0.82929f, 1f, false),
                        new AttackFrame(0.27735f, 0.83458f, -0.04385f, -0.33254f, -0.42546f, 0.17171f, 0.82396f, 1f, false)
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
