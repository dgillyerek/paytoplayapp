namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Emberfang Design Blender dragon rig. Custom 55 bone names.
    /// Rest is the bind pose. Wing flap is a 1 second Scene take at 30 fps.
    /// Reject the Mixamo file EMBERFANG_rig.fbx.
    /// </summary>
    public static class EmberfangMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/emberfang";
        public const string RestFileName = "EMBERFANG_dragonrig.fbx";
        public const string FlapFileName = "EMBERFANG_dragonrig_wingflap.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string FlapThemePackRel = ThemePackDir + "/" + FlapFileName;
        public const string RejectedMixamoFileName = "EMBERFANG_rig.fbx";

        public const string RestPoseName = "rest";
        public const string FlapPoseName = "wing flap";
        public const string FlapTakeName = "Scene";
        public const float FlapSeconds = 1f;
        public const float FlapFrameRate = 30f;
        public const int FlapLastFrame = 30;
        public const int BoneCount = 55;

        public const string BoneRoot = "root";
        public const string BoneHips = "hips";
        public const string BoneSpine = "spine_01";
        public const string BoneWing = "wing_root.L";

        public static readonly string[] PoseNames = { RestPoseName, FlapPoseName };

        public static readonly string[] BoneNames =
        {
            "root", "hips", "spine_01", "spine_02", "spine_03", "chest",
            "neck_01", "neck_02", "neck_03", "head", "jaw",
            "upperarm.L", "forearm.L", "hand.L",
            "wing_root.L", "wing_arm.L", "wing_forearm.L",
            "wing_f1_a.L", "wing_f1_b.L", "wing_f2_a.L", "wing_f2_b.L",
            "wing_f3_a.L", "wing_f3_b.L", "wing_f4_a.L", "wing_f4_b.L",
            "upperarm.R", "forearm.R", "hand.R",
            "wing_root.R", "wing_arm.R", "wing_forearm.R",
            "wing_f1_a.R", "wing_f1_b.R", "wing_f2_a.R", "wing_f2_b.R",
            "wing_f3_a.R", "wing_f3_b.R", "wing_f4_a.R", "wing_f4_b.R",
            "tail_01", "tail_02", "tail_03", "tail_04", "tail_05", "tail_06", "tail_07", "tail_08",
            "thigh.L", "shin.L", "foot.L", "toe.L",
            "thigh.R", "shin.R", "foot.R", "toe.R"
        };
    }
}
