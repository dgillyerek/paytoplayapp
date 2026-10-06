using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Nightfang Design Blender quadruped. Custom 33-bone rig, Generic only.
    /// Rest is the bind pose. Trot is a 1 second Scene take at 30 fps
    /// (FBX LocalStop 46186158000 ticks). Reject NIGHTFANG_rig.fbx.
    /// Basecolor and normal are embedded. No metal/roughness map.
    /// </summary>
    public static class NightfangMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/nightfang";
        public const string RestFileName = "NIGHTFANG_blenderig.fbx";
        public const string TrotFileName = "NIGHTFANG_blenderig_trot.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string TrotThemePackRel = ThemePackDir + "/" + TrotFileName;
        public const string RejectedMixamoFileName = "NIGHTFANG_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string TrotPoseName = "trot";
        public const string TrotTakeName = "Scene";
        public const float TrotSeconds = 1f;
        public const float TrotFrameRate = 30f;
        public const int TrotLastFrame = 30;
        public const int BoneCount = 33;

        public const string TextureFolder = "NIGHTFANG_blenderig.fbm";
        public const string BaseColorFile = "NIGHTFANG_basecolor_0.jpg";
        public const string NormalFile = "NIGHTFANG_normal_2.jpg";

        public const string BoneRoot = "root";

        public static readonly string[] PoseNames = { RestPoseName, TrotPoseName };

        public static readonly string[] BoneNames =
        {
            "root", "hips", "spine_01", "spine_02", "chest",
            "neck_01", "neck_02", "head", "jaw",
            "upperarm.L", "forearm.L", "hand.L", "toe.L",
            "upperarm.R", "forearm.R", "hand.R", "toe.R",
            "tail_01", "tail_02", "tail_03", "tail_04", "tail_05", "tail_06", "tail_07", "tail_08",
            "thigh.L", "shin.L", "foot.L", "toe_h.L",
            "thigh.R", "shin.R", "foot.R", "toe_h.R"
        };

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Nightfang",
            ThemePackDir,
            RestFileName,
            RejectedMixamoFileName,
            TrotPoseName,
            TrotFileName,
            TrotTakeName,
            TrotSeconds,
            TrotFrameRate,
            TrotLastFrame,
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
