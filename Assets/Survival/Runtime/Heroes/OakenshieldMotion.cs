using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield Design Blender rig. Mixamo-compatible mixamorig, 22 bones.
    /// Rest is the bind pose. Walk is a 1 second Scene take at 30 fps
    /// (FBX LocalStop 46186158000 ticks). Reject OAKENSHIELD_rig.fbx.
    /// Basecolor and normal are embedded. No metal/roughness map.
    /// </summary>
    public static class OakenshieldMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/oakenshield";
        public const string RestFileName = "OAKENSHIELD_blenderig.fbx";
        public const string WalkFileName = "OAKENSHIELD_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = OakenshieldAttack.FileName;
        public const string RejectedMixamoFileName = "OAKENSHIELD_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = MixamoHumanoidBones.Count;

        public const string TextureFolder = "OAKENSHIELD_blenderig.fbm";
        public const string BaseColorFile = "OAKENSHIELD_basecolor_0.jpg";
        public const string NormalFile = "OAKENSHIELD_normal_2.jpg";

        public const string BoneRoot = MixamoHumanoidBones.Root;

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName };
        public static readonly string[] BoneNames = MixamoHumanoidBones.Names;

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Oakenshield",
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
            "",
            new[]
            {
                new BlenderRigClip(
                    AttackPoseName,
                    AttackFileName,
                    BlenderRigAttackSpec.TakeName,
                    BlenderRigAttackSpec.ClipSeconds,
                    BlenderRigAttackSpec.FrameRate,
                    BlenderRigAttackSpec.LastFrame)
            });
    }
}
