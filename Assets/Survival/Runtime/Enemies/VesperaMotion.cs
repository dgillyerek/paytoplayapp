using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// ThemePack rest/walk/attack are the Design no-cape pack on Derek's edited rig (265ec84, see VesperaNoCape):
    /// 16 bones, three roots (Spine1, LeftUpLeg, RightUpLeg); Hips, Spine, Shoulders and ToeBases were deleted, so
    /// there is no Hips bone and the rig imports as Generic (binds by transform path), not Humanoid.
    /// Rest is the bind pose. Walk is a 1 second Scene take at 30 fps
    /// (FBX LocalStop 46186158000 ticks). Reject VESPERA_rig.fbx.
    /// Basecolor and normal are embedded. No metal/roughness map.
    /// </summary>
    public static class VesperaMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/vespera";
        public const string RestFileName = "VESPERA_blenderig.fbx";
        public const string WalkFileName = "VESPERA_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = VesperaAttack.FileName;
        public const string RejectedMixamoFileName = "VESPERA_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = 16;

        public const string TextureFolder = "VESPERA_blenderig.fbm";
        public const string BaseColorFile = "VESPERA_basecolor_0.jpg";
        public const string NormalFile = "VESPERA_normal_2.jpg";

        public const string BoneRoot = "mixamorig:Spine1";

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName };
        public static readonly string[] BoneNames =
        {
            "mixamorig:Spine1",
            "mixamorig:Spine2",
            "mixamorig:Neck",
            "mixamorig:Head",
            "mixamorig:LeftArm",
            "mixamorig:LeftForeArm",
            "mixamorig:LeftHand",
            "mixamorig:RightArm",
            "mixamorig:RightForeArm",
            "mixamorig:RightHand",
            "mixamorig:LeftUpLeg",
            "mixamorig:LeftLeg",
            "mixamorig:LeftFoot",
            "mixamorig:RightUpLeg",
            "mixamorig:RightLeg",
            "mixamorig:RightFoot"
        };

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Vespera",
            ThemePackDir,
            RestFileName,
            RejectedMixamoFileName,
            WalkPoseName,
            WalkFileName,
            WalkTakeName,
            WalkSeconds,
            WalkFrameRate,
            WalkLastFrame,
            BlenderRigAvatar.CustomGeneric,
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
