using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Oakenshield fresh rebuild (2026-10-12): Derek-approved front/back look, Meshy image-to-3D, decimated to 200k faces, new
    /// 20-bone mixamorig rig (no ToeBase) in Blender 5.2.2 (design .../oakenshield/fresh_20261012). Face and skull 100% on Head
    /// (narrow tilted Neck to Head band), every protruding leaf/spike piece rigid on one bone, the leaf skirt 100% on Hips (never the
    /// legs), thighs ramp Hips to UpLeg over 12 cm (no crease). Rest = bind pose with arms lowered 20 deg, body square to the camera.
    /// Walk = 1 s in-place Scene take at 30 fps, ankles 19 cm apart, head forward. Imported Generic like the locked rigs.
    /// Replaces the abandoned #48 pack. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class OakenshieldMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/oakenshield";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/fresh_20261012";
        public const string RestFileName = "OAKENSHIELD_blenderig.fbx";
        public const string WalkFileName = "OAKENSHIELD_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = OakenshieldAttack.FileName;
        public const string RejectedMixamoFileName = "OAKENSHIELD_rig.fbx";
        public const string RestMd5 = "9cfe65858575c081ac115117307bcca3";
        public const string WalkMd5 = "b0adb8c162165c4b6a7dd4c1bbb4cb67";
        public const string AttackMd5 = OakenshieldAttack.FileMd5;

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = 20;

        public const string TextureFolder = "OAKENSHIELD_blenderig.fbm";
        public const string BaseColorFile = "OAKENSHIELD_fresh_basecolor_0.jpg";
        public const string NormalFile = "OAKENSHIELD_fresh_normal_2.jpg";
        public const string MetallicRoughnessFile = "OAKENSHIELD_fresh_metal_rough_1.png";

        public const string BoneRoot = MixamoHumanoidBones.Root;

        // Attack 3 (2026-10-18): Mixamo "Standing 2H Magic Area Attack 02" (auto-rig of her mesh keeps the 20 mixamorig names) retargeted
        // onto OAKENSHIELD_rig (design .../oakenshield/mixamo_attacks): T-pose Mixamo bind matched to the rig bind, body bones only,
        // hips drift removed, left-foot step into the side-on wind-up (hips -58 deg, as her spear throw), squaring up as both palms drive
        // down into the ground at f52, when the roots erupt under the target. Empty-handed: the thorn spear is only conjured in attack 1.
        // Attack 2 waits for a forward-thrust download: Mixamo "Stabbing" holds her spear upright at her grip (design probe sheet).
        public const string DesignMixamoAttacksDir = "design/survival-theme-a-fantasy/heroes/anim/oakenshield/mixamo_attacks";
        public const string Attack3PoseName = "attack3";
        public const string Attack3FileName = "OAKENSHIELD_blenderig_attack3_root_snare.fbx";
        public const string Attack3Md5 = "bb933ae64a0fa880583eed00236f2663";
        public const int Attack3LastFrame = 120;
        public const int Attack3ContactFrame = 52;
        public const string ExtraTakeName = "Scene";

        public static readonly string[] AttackPoseNames = { AttackPoseName, Attack3PoseName };

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName, Attack3PoseName };
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
                    BlenderRigAttackSpec.LastFrame),
                new BlenderRigClip(Attack3PoseName, Attack3FileName, ExtraTakeName, Attack3LastFrame / WalkFrameRate, WalkFrameRate, Attack3LastFrame)
            });
    }
}
