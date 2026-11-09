namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Vorgath, the preview dragon (Derek 2026-11-04). The body, clips and spit stream already live under
    /// preview_new_chars/vorgath. This only names them. Nothing here rewrites an FBX, the atlas or the fire strips.
    /// Skeleton: spine Bone..Bone.005 (Bone.004 is the snout, Bone.005 the jaw), Mixamo-named arms and legs, no Hips.
    /// Actions are 30 fps. Blender frame N is clip time (N - 1) / 30, the same clock as the spit-fire note.
    /// </summary>
    public static class VorgathMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath";

        public const string RigRestFileName = "VORGATH_rig_rest.fbx";
        public const string IdleFileName = "VORGATH_idle.fbx";
        public const string WalkFileName = "VORGATH_walk.fbx";
        public const string SlamFileName = "VORGATH_attack_slam.fbx";
        public const string SpitFileName = "VORGATH_attack_spit.fbx";
        public const string AtlasFileName = "VORGATH_atlas_2048.png";
        public const string MouthSocketRefFileName = "VORGATH_mouth_socket_REF.fbx";

        public const string RigRestRel = ThemePackDir + "/" + RigRestFileName;
        public const string IdleRel = ThemePackDir + "/" + IdleFileName;
        public const string WalkRel = ThemePackDir + "/" + WalkFileName;
        public const string SlamRel = ThemePackDir + "/" + SlamFileName;
        public const string SpitRel = ThemePackDir + "/" + SpitFileName;
        public const string AtlasRel = ThemePackDir + "/" + AtlasFileName;
        public const string MouthSocketRefRel = ThemePackDir + "/" + MouthSocketRefFileName;

        public const string RestPoseName = "rest";
        public const string IdlePoseName = "idle";
        public const string WalkPoseName = "walk";
        public const string SlamPoseName = "attack_slam";
        public const string SpitPoseName = "attack_spit";

        public const float FrameRate = 30f;

        /// <summary>idle is Blender frames 1-160.</summary>
        public const int IdleFrames = 160;
        public const float IdleSeconds = IdleFrames / FrameRate;

        /// <summary>walk is Blender frames 1-44 and loops.</summary>
        public const int WalkFrames = 44;
        public const float WalkSeconds = WalkFrames / FrameRate;

        /// <summary>attack_slam is Blender frames 1-112. Both hands hit the floor on frame 52.</summary>
        public const int SlamFrames = 112;
        public const float SlamSeconds = SlamFrames / FrameRate;
        public const int SlamContactFrame = 52;
        public const float SlamContactSeconds = (SlamContactFrame - 1) / FrameRate;

        /// <summary>The forearm that reaches the floor on the slam (Derek's arm chain, right side).</summary>
        public const string SlamStrikeBone = "mixamorig:RightShoulder.003";

        /// <summary>attack_spit is Blender frames 1-163. The head and jaw lunge peak on frame 60. Fire is f53-f75.</summary>
        public const int SpitFrames = 163;
        public const float SpitSeconds = SpitFrames / FrameRate;
        public const int SpitPeakFrame = 60;
        public const float SpitPeakSeconds = (SpitPeakFrame - 1) / FrameRate;

        /// <summary>FireStart. Spit clip frame 53: (53 - 1) / 30. Matches <c>VorgathSpitStream.FireStartSeconds</c>.</summary>
        public const float SpitFireStartSeconds = (53 - 1) / FrameRate;

        /// <summary>FireStop. Last stream frame is 75: (75 - 1) / 30. Matches <c>VorgathSpitStream.FireStopSeconds</c>.</summary>
        public const float SpitFireStopSeconds = (75 - 1) / FrameRate;

        /// <summary>Snout bone the mouth socket hangs from. Not a Mixamo head.</summary>
        public const string SnoutBone = "Bone.004";

        public const string SceneName = "Vorgath";

        public static readonly string[] PoseNames = { RestPoseName, IdlePoseName, WalkPoseName, SlamPoseName, SpitPoseName };
    }
}
