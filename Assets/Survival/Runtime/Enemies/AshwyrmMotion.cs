using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Ashwyrm Design Blender dragon. Derek hand-placed 23-bone rig, Generic only.
    /// Rest is the bind pose. Wing flap is a 1 second Scene take at 30 fps
    /// (FBX LocalStop 46186158000 ticks). Reject ASHWYRM_rig.fbx.
    /// Basecolor and normal are embedded. No metal/roughness map.
    /// </summary>
    public static class AshwyrmMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/ashwyrm";
        public const string RestFileName = "ASHWYRM_blenderig.fbx";
        public const string FlapFileName = "ASHWYRM_blenderig_wingflap.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string FlapThemePackRel = ThemePackDir + "/" + FlapFileName;
        public const string RejectedMixamoFileName = "ASHWYRM_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string FlapPoseName = "wing flap";
        public const string FlapTakeName = "Scene";
        public const float FlapSeconds = 1f;
        public const float FlapFrameRate = 30f;
        public const int FlapLastFrame = 30;
        public const int BoneCount = 23;

        public const string TextureFolder = "ASHWYRM_blenderig.fbm";
        public const string BaseColorFile = "ASHWYRM_basecolor_0.jpg";
        public const string NormalFile = "ASHWYRM_normal_2.jpg";

        public const string BoneRoot = "root";

        public static readonly string[] PoseNames = { RestPoseName, FlapPoseName };

        public static readonly string[] BoneNames =
        {
            "root", "spine",
            "wing_root.R", "wing_arm.R", "wing_f1.R", "wing_f2.R", "wing_f3.R", "wing_f4.R", "wing_inner.R",
            "wing_root.L", "wing_arm.L", "wing_f2.L", "wing_f3.L", "wing_f4.L", "wing_f1.L", "wing_inner.L",
            "thigh.R", "shin.R", "thigh.L", "shin.L",
            "tail_01", "tail_02", "tail_03"
        };

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Ashwyrm",
            ThemePackDir,
            RestFileName,
            RejectedMixamoFileName,
            FlapPoseName,
            FlapFileName,
            FlapTakeName,
            FlapSeconds,
            FlapFrameRate,
            FlapLastFrame,
            BlenderRigAvatar.CustomGeneric,
            BoneCount,
            BoneNames,
            BoneRoot,
            TextureFolder,
            BaseColorFile,
            NormalFile,
            "");
    }
}
