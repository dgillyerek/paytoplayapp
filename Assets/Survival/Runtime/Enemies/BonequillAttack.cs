using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill fresh rebuild bow attack (2026-10-13): f0 rest, f6 bow raised and arrow nocked, f10 full draw (left arm toward the
    /// target, RightHand at the chin by 2-bone IK), f14 loose (arrow flies nock to grip at 14 m/s with a purple trail), f17 string-hand
    /// recoil, f30 rest. LeftHand keeps its full-draw rotation in every clip, so the bow stays upright in rest, walk and attack.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "0d111b1caaba96199986b7006bc2c223";
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
                new[] { -0.262f, 0.97458f, 0.06891f, 0f, 0f, 0f, 1f },
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
                        new AttackFrame(-0.262f, 0.97458f, 0.06891f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.25061f, 0.97786f, 0.1026f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.21252f, 1.00429f, 0.20435f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.14358f, 1.10366f, 0.3509f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.07986f, 1.25958f, 0.44129f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.05004f, 1.36721f, 0.46117f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04217f, 1.40266f, 0.46203f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.03856f, 1.40781f, 0.46112f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0307f, 1.41915f, 0.45881f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02298f, 1.4305f, 0.45612f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01952f, 1.43565f, 0.45477f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02097f, 1.42791f, 0.4554f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.02556f, 1.405f, 0.45656f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.03435f, 1.366f, 0.45601f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.04934f, 1.30939f, 0.44935f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.07309f, 1.23594f, 0.42931f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.10672f, 1.1537f, 0.38789f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.14653f, 1.07934f, 0.32454f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.1846f, 1.02694f, 0.25186f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.2151f, 0.9973f, 0.18556f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23688f, 0.98306f, 0.1337f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.25116f, 0.97712f, 0.09756f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.25929f, 0.97505f, 0.07616f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.262f, 0.97458f, 0.06891f, 0f, 0f, 0f, 1f, 1f, true)
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
                        new AttackFrame(0.28587f, 0.92425f, 0.00385f, 0.24791f, 0.9158f, -0.04292f, 0.31308f, 1f, false),
                        new AttackFrame(0.26458f, 0.92563f, 0.03446f, 0.26884f, 0.90197f, -0.06861f, 0.33086f, 1f, false),
                        new AttackFrame(0.19532f, 0.95072f, 0.1306f, 0.3238f, 0.84626f, -0.14573f, 0.39719f, 1f, false),
                        new AttackFrame(0.07794f, 1.06819f, 0.27268f, 0.36157f, 0.71101f, -0.27216f, 0.5382f, 1f, false),
                        new AttackFrame(-0.01873f, 1.25771f, 0.34772f, 0.31118f, 0.53512f, -0.40559f, 0.67254f, 1f, false),
                        new AttackFrame(-0.05651f, 1.38155f, 0.35319f, 0.24499f, 0.42433f, -0.48812f, 0.72226f, 1f, true),
                        new AttackFrame(-0.06491f, 1.42064f, 0.34952f, 0.21994f, 0.38964f, -0.51448f, 0.73152f, 1f, true),
                        new AttackFrame(-0.07216f, 1.42038f, 0.33347f, 0.2086f, 0.3911f, -0.45098f, 0.77469f, 1f, true),
                        new AttackFrame(-0.06661f, 1.4201f, 0.26089f, 0.15261f, 0.3374f, -0.27425f, 0.8875f, 1f, true),
                        new AttackFrame(-0.02658f, 1.41797f, 0.15047f, 0.04072f, 0.13205f, -0.07684f, 0.98742f, 1f, true),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00982f, 1.42468f, 0.0914f, -0.05382f, 0.0615f, 0.02134f, 0.99643f, 1f, false),
                        new AttackFrame(-0.00095f, 1.42496f, 0.06181f, -0.3997f, -0.32692f, 0.23557f, 0.82333f, 1f, false),
                        new AttackFrame(0.01362f, 1.41817f, 0.04337f, -0.41487f, -0.25592f, 0.28262f, 0.82614f, 1f, false),
                        new AttackFrame(0.01407f, 1.41545f, 0.04061f, -0.41448f, -0.26876f, 0.28788f, 0.82043f, 1f, false),
                        new AttackFrame(0.01511f, 1.40741f, 0.03235f, -0.41366f, -0.30745f, 0.30253f, 0.80177f, 1f, false),
                        new AttackFrame(0.0159f, 1.39336f, 0.01775f, -0.41326f, -0.3765f, 0.32444f, 0.76302f, 1f, false),
                        new AttackFrame(0.01482f, 1.3698f, -0.00573f, -0.41409f, -0.48698f, 0.34948f, 0.68502f, 1f, false),
                        new AttackFrame(0.00952f, 1.32309f, -0.04426f, -0.41298f, -0.65412f, 0.36432f, 0.5185f, 1f, false),
                        new AttackFrame(0.01253f, 1.20082f, -0.10008f, -0.3854f, -0.844f, 0.32529f, 0.18254f, 1f, false),
                        new AttackFrame(0.10395f, 1.00573f, -0.08383f, 0.32164f, 0.9074f, -0.22454f, 0.15087f, 1f, false),
                        new AttackFrame(0.19457f, 0.94153f, -0.02535f, 0.28571f, 0.90688f, -0.1564f, 0.26733f, 1f, false),
                        new AttackFrame(0.23777f, 0.92941f, -0.00191f, 0.269f, 0.90783f, -0.11385f, 0.30086f, 1f, false),
                        new AttackFrame(0.26126f, 0.92606f, 0.00472f, 0.25921f, 0.91058f, -0.08323f, 0.31101f, 1f, false),
                        new AttackFrame(0.27533f, 0.92483f, 0.00534f, 0.25291f, 0.91328f, -0.06131f, 0.31337f, 1f, false),
                        new AttackFrame(0.28323f, 0.92436f, 0.00439f, 0.24919f, 0.91513f, -0.04769f, 0.31332f, 1f, false),
                        new AttackFrame(0.28587f, 0.92425f, 0.00385f, 0.24791f, 0.9158f, -0.04292f, 0.31308f, 1f, false)
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
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.02508f, 1.44166f, 0.56537f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.04848f, 1.46694f, 1.03076f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.07188f, 1.49221f, 1.49616f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.09529f, 1.51749f, 1.96155f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.11869f, 1.54276f, 2.42694f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.14209f, 1.56804f, 2.89234f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.16549f, 1.59332f, 3.35773f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.18889f, 1.61859f, 3.82312f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2123f, 1.64387f, 4.28852f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2357f, 1.66915f, 4.75391f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2591f, 1.69442f, 5.21931f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2825f, 1.7197f, 5.6847f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.3059f, 1.74497f, 6.15009f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.32931f, 1.77025f, 6.61549f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.35271f, 1.79553f, 7.08088f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.37611f, 1.8208f, 7.54627f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.39951f, 1.84608f, 8.01167f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true)
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
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.00168f, 1.41638f, 0.09998f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, false),
                        new AttackFrame(-0.02508f, 1.44166f, 0.56537f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.04848f, 1.46694f, 1.03076f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.07188f, 1.49221f, 1.49616f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.09529f, 1.51749f, 1.96155f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.11869f, 1.54276f, 2.42694f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.14209f, 1.56804f, 2.89234f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.16549f, 1.59332f, 3.35773f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.18889f, 1.61859f, 3.82312f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2123f, 1.64387f, 4.28852f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2357f, 1.66915f, 4.75391f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2591f, 1.69442f, 5.21931f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.2825f, 1.7197f, 5.6847f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.3059f, 1.74497f, 6.15009f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.32931f, 1.77025f, 6.61549f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.35271f, 1.79553f, 7.08088f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.37611f, 1.8208f, 7.54627f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true),
                        new AttackFrame(-0.39951f, 1.84608f, 8.01167f, -0.02712f, -0.02507f, 0.00068f, 0.99932f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
