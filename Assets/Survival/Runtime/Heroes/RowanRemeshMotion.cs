using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan remesh compare. Same mixamorig contract as the tip blenderig:
    /// rest bind, 1 second Scene walk at 30 fps. Does not replace ROWAN_blenderig.
    /// </summary>
    public static class RowanRemeshMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/rowan_remesh";
        public const string RestFileName = "ROWAN_remesh_blenderig.fbx";
        public const string WalkFileName = "ROWAN_remesh_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string RejectedMixamoFileName = "ROWAN_remesh_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = MixamoHumanoidBones.Count;

        public const string TextureFolder = "ROWAN_remesh_blenderig.fbm";
        public const string BaseColorFile = "ROWAN_remesh_basecolor_0.jpg";
        public const string NormalFile = "ROWAN_remesh_normal_2.jpg";

        public const string BoneRoot = MixamoHumanoidBones.Root;

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName };
        public static readonly string[] BoneNames = MixamoHumanoidBones.Names;

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "RowanRemesh",
            ThemePackDir,
            RestFileName,
            RejectedMixamoFileName,
            WalkPoseName,
            WalkFileName,
            WalkTakeName,
            WalkSeconds,
            WalkFrameRate,
            WalkLastFrame,
            BlenderRigAvatar.MixamoHumanoid,
            BoneCount,
            BoneNames,
            BoneRoot,
            TextureFolder,
            BaseColorFile,
            NormalFile,
            "");
    }
}
