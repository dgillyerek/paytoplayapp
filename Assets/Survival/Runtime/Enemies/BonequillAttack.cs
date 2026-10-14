using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill gritty rebuild bow attack (2026-10-14, archer stance): f0 rest, f6 raise/nock, f10 full draw with the torso yawed
    /// ~55 deg so the bow shoulder points at the target; LeftArm straight out, RightHand at the jaw with the elbow out to the side
    /// (nothing through the ribcage or the cloak), arrow nocked between the RightHand draw fingers and re-aimed every frame through the arrow rest on top of the bow hand (f7 nock -> f13), f14 loose (arrow flies straight at the target at 14 m/s with a purple trail), f17 string-hand
    /// recoil, f30 rest. LeftHand keeps its full-draw rotation in every clip, so the bow stays upright in rest, walk and attack.
    /// Head forward. Frames baked in Unity convention (character forward = +Z) by design .../gritty_20261013/work/sheet.py.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillAttack
    {
        public const string FileName = "BONEQUILL_blenderig_attack.fbx";
        public const string FileMd5 = "9b6bfa44617c47e69d4c66f41b544049";
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
                new[] { -0.2678f, 0.97135f, 0.07164f, 0f, 0f, 0f, 1f },
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
                        new AttackFrame(-0.2678f, 0.97135f, 0.07164f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27133f, 0.97649f, 0.11291f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27712f, 1.00353f, 0.22627f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.27407f, 1.07827f, 0.37925f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.25702f, 1.1878f, 0.50196f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23783f, 1.27335f, 0.56352f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.22946f, 1.30458f, 0.58126f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.19595f, 1.32809f, 0.6006f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.11713f, 1.38367f, 0.63185f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0362f, 1.44049f, 0.64585f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00031f, 1.4653f, 0.64676f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00435f, 1.45757f, 0.64571f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.01586f, 1.43454f, 0.64195f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.03433f, 1.39496f, 0.63333f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.05983f, 1.33662f, 0.61526f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.09273f, 1.25943f, 0.57996f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.13205f, 1.17112f, 0.51851f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.17296f, 1.08984f, 0.43052f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.20835f, 1.03186f, 0.33127f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.23427f, 0.99858f, 0.24001f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.25115f, 0.98213f, 0.16693f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.26112f, 0.97487f, 0.1145f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.26622f, 0.97206f, 0.08262f, 0f, 0f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.2678f, 0.97135f, 0.07164f, 0f, 0f, 0f, 1f, 1f, true)
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
                        new AttackFrame(0.27735f, 0.83458f, -0.04385f, -0.37752f, -0.50828f, 0.26367f, 0.72774f, 1f, false),
                        new AttackFrame(0.26678f, 0.83744f, -0.0106f, -0.37007f, -0.5061f, 0.25435f, 0.73635f, 1f, false),
                        new AttackFrame(0.2312f, 0.86953f, 0.09497f, -0.35587f, -0.50385f, 0.23911f, 0.74988f, 1f, false),
                        new AttackFrame(0.16767f, 0.99399f, 0.25235f, -0.28877f, -0.53132f, 0.19896f, 0.77118f, 1f, false),
                        new AttackFrame(0.10537f, 1.18758f, 0.35212f, -0.06962f, -0.55095f, 0.04619f, 0.83035f, 1f, false),
                        new AttackFrame(0.06822f, 1.32385f, 0.3707f, 0.05858f, -0.47806f, -0.03198f, 0.87579f, 1f, false),
                        new AttackFrame(0.0555f, 1.3691f, 0.36859f, 0.08332f, -0.43964f, -0.041f, 0.89336f, 1f, false),
                        new AttackFrame(0.0489f, 1.38784f, 0.3537f, 0.06557f, -0.37321f, -0.02645f, 0.92505f, 1f, true),
                        new AttackFrame(0.03303f, 1.42947f, 0.31997f, 0.03299f, -0.21511f, -0.00727f, 0.97601f, 1f, true),
                        new AttackFrame(0.01515f, 1.47097f, 0.28544f, 0.00789f, -0.06268f, -0.0005f, 0.998f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.03537f, 1.49145f, 0.2695f, 0.00178f, -0.03927f, -0.00007f, 0.99923f, 1f, false),
                        new AttackFrame(0.08784f, 1.49532f, 0.26347f, 0.00678f, -0.10567f, -0.00072f, 0.99438f, 1f, false),
                        new AttackFrame(0.11454f, 1.49745f, 0.25749f, 0.00938f, -0.1363f, -0.00129f, 0.99062f, 1f, false),
                        new AttackFrame(0.11814f, 1.48732f, 0.25981f, 0.00636f, -0.14651f, -0.00094f, 0.98919f, 1f, false),
                        new AttackFrame(0.12796f, 1.45695f, 0.26563f, -0.00315f, -0.17521f, 0.00056f, 0.98453f, 1f, false),
                        new AttackFrame(0.14232f, 1.40418f, 0.27191f, -0.02112f, -0.2193f, 0.00475f, 0.97542f, 1f, false),
                        new AttackFrame(0.15962f, 1.32583f, 0.27279f, -0.05001f, -0.27448f, 0.0143f, 0.96019f, 1f, false),
                        new AttackFrame(0.17895f, 1.22245f, 0.25924f, -0.08994f, -0.33437f, 0.03208f, 0.93759f, 1f, false),
                        new AttackFrame(0.20039f, 1.10529f, 0.22313f, -0.13689f, -0.39202f, 0.05911f, 0.90779f, 1f, false),
                        new AttackFrame(0.22325f, 0.99708f, 0.16575f, -0.1862f, -0.44055f, 0.09395f, 0.87316f, 1f, false),
                        new AttackFrame(0.24389f, 0.91816f, 0.10066f, -0.23381f, -0.47464f, 0.1324f, 0.83816f, 1f, false),
                        new AttackFrame(0.25899f, 0.87185f, 0.04363f, -0.27663f, -0.49517f, 0.16998f, 0.80585f, 1f, false),
                        new AttackFrame(0.26848f, 0.84891f, 0.00198f, -0.31436f, -0.50576f, 0.20466f, 0.77685f, 1f, false),
                        new AttackFrame(0.27386f, 0.83904f, -0.02465f, -0.34627f, -0.50932f, 0.23448f, 0.75214f, 1f, false),
                        new AttackFrame(0.27653f, 0.83544f, -0.03917f, -0.36895f, -0.50899f, 0.25569f, 0.73446f, 1f, false),
                        new AttackFrame(0.27735f, 0.83458f, -0.04385f, -0.37752f, -0.50828f, 0.26367f, 0.72774f, 1f, false)
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
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00567f, 1.49025f, 0.73635f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.005f, 1.49094f, 1.20302f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00432f, 1.49163f, 1.66968f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00365f, 1.49232f, 2.13635f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00298f, 1.49301f, 2.60301f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.0023f, 1.4937f, 3.06968f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00163f, 1.49439f, 3.53635f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00096f, 1.49508f, 4.00301f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00029f, 1.49577f, 4.46968f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00039f, 1.49646f, 4.93634f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00106f, 1.49715f, 5.40301f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00173f, 1.49784f, 5.86967f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0024f, 1.49853f, 6.33634f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00308f, 1.49922f, 6.803f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00375f, 1.49991f, 7.26967f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00442f, 1.5006f, 7.73634f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0051f, 1.50129f, 8.203f, -0.00074f, -0.00072f, 0f, 1f, 1f, true)
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
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00634f, 1.48956f, 0.26969f, -0.00074f, -0.00072f, 0f, 1f, 1f, false),
                        new AttackFrame(0.00567f, 1.49025f, 0.73635f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.005f, 1.49094f, 1.20302f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00432f, 1.49163f, 1.66968f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00365f, 1.49232f, 2.13635f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00298f, 1.49301f, 2.60301f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.0023f, 1.4937f, 3.06968f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00163f, 1.49439f, 3.53635f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00096f, 1.49508f, 4.00301f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(0.00029f, 1.49577f, 4.46968f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00039f, 1.49646f, 4.93634f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00106f, 1.49715f, 5.40301f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00173f, 1.49784f, 5.86967f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0024f, 1.49853f, 6.33634f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00308f, 1.49922f, 6.803f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00375f, 1.49991f, 7.26967f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.00442f, 1.5006f, 7.73634f, -0.00074f, -0.00072f, 0f, 1f, 1f, true),
                        new AttackFrame(-0.0051f, 1.50129f, 8.203f, -0.00074f, -0.00072f, 0f, 1f, 1f, true)
                    },
                    "purple trail behind the arrow (colours from #47)")
            });
    }
}
