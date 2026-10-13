using Survival.Domain.Roster;

namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill fresh rebuild (2026-10-13): Derek-locked front/back look, Meshy body decimated to 200k faces, new 20-bone mixamorig
    /// rig in Blender 5.2.2 (design .../bonequill/fresh_20261013). Skull and face 100% on Head, loincloth/belt/pelvis 100% on Hips
    /// (never the legs), bracers and boots rigid on their bones, Hips to UpLeg ramp 6 cm. Rest = arms relaxed down by the sides.
    /// Walk = 1 s in-place Scene take at 30 fps, ankles about 19-21 cm apart, head forward with a 0-1.8 deg bob. Cape is a separate
    /// FBX on runtime springs (<see cref="BonequillCape"/>). Imported Generic like the locked rigs. Replaces #47 / #34.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class BonequillMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill";
        public const string DesignDir = "design/survival-theme-a-fantasy/enemies/anim/bonequill/fresh_20261013";
        public const string RestFileName = "BONEQUILL_blenderig.fbx";
        public const string WalkFileName = "BONEQUILL_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = BonequillAttack.FileName;
        public const string RejectedMixamoFileName = "BONEQUILL_rig.fbx";
        public const string RestMd5 = "849be35e2e1a53138e64792e1406322e";
        public const string WalkMd5 = "aac5948c92912e089def9384378eee84";
        public const string AttackMd5 = BonequillAttack.FileMd5;

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = 20;

        public const string TextureFolder = "BONEQUILL_blenderig.fbm";
        public const string BaseColorFile = "BONEQUILL_fresh_basecolor_0.jpg";
        public const string NormalFile = "BONEQUILL_fresh_normal_2.jpg";
        public const string MetallicRoughnessFile = "BONEQUILL_fresh_metal_rough_1.png";

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

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Bonequill",
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
