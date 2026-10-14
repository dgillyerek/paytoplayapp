using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan A-pose rebuild (2026-10-14, #64): Derek-approved A-pose Meshy model (1.78M faces) decimated to 200k with its texture,
    /// 20-bone mixamorig rig (no ToeBase) in Blender 5.2.2 (design .../rowan/fresh_20261013/work/apose_20261014). Face 100% on Head;
    /// hands and fingers 100% on Hand, lower forearm 100% on ForeArm, no arm vertex weighted to hips or torso; belt 100% on Hips.
    /// Coat tails below the belt are a separate spring mesh (RowanCoat), never on the legs. Rest = arms down by the sides from the
    /// A-pose. Walk = 1 s in place at 30 fps, body bones only, head forward. Bow attack in RowanAttack. Imported Generic like the
    /// locked rigs. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class RowanMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/rowan";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/fresh_20261013";
        public const string RestFileName = "ROWAN_blenderig.fbx";
        public const string WalkFileName = "ROWAN_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = RowanAttack.FileName;
        public const string RejectedMixamoFileName = "ROWAN_rig.fbx";
        public const string RestMd5 = "df35363cb2f870a1d569947af2bc0e7e";
        public const string WalkMd5 = "3876796bb9ea4db813df163128537e36";
        public const string AttackMd5 = RowanAttack.FileMd5;

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = 20;

        public const string TextureFolder = "ROWAN_blenderig.fbm";
        public const string BaseColorFile = "ROWAN_fresh_basecolor_0.jpg";
        public const string NormalFile = "ROWAN_fresh_normal_2.jpg";
        public const string MetallicRoughnessFile = "ROWAN_fresh_metal_rough_1.png";

        public const string BoneRoot = MixamoHumanoidBones.Root;

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName };
        public static readonly string[] BoneNames =
        {
            "mixamorig:Hips",
            "mixamorig:Spine",
            "mixamorig:Spine1",
            "mixamorig:Spine2",
            "mixamorig:Neck",
            "mixamorig:Head",
            "mixamorig:LeftShoulder",
            "mixamorig:LeftArm",
            "mixamorig:LeftForeArm",
            "mixamorig:LeftHand",
            "mixamorig:RightShoulder",
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

        /// <summary>Single bow on LeftHand (see RowanAttack.IdleProps).</summary>
        public static BlenderRigIdlePropSpec[] IdleProps => RowanAttack.IdleProps;

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Rowan",
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
            MetallicRoughnessFile,
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
