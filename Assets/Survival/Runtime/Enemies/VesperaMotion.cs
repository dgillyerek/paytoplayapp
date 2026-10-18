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

        // Attacks 2 and 3 (2026-10-18): Mixamo clips animated on her own uploaded no-cape skeleton (16 bones; Mixamo's hips motion
        // rides the armature object) retargeted onto VESPERA_rig (design .../vespera/mixamo_attacks): source read in world space, the
        // T-pose arms matched to her bind, body bones only, the right foot kept on its rest spot so the left foot steps forward into
        // the side-on stance (IK steps on the blends). Each wind-up plays 1.5x slower, as her approved conjure. No orb: purple VFX only.
        public const string DesignMixamoAttacksDir = "design/survival-theme-a-fantasy/enemies/anim/vespera/mixamo_attacks";
        public const string ExtraTakeName = "Scene";

        /// <summary>Shadow lash: "Standing 1H Magic Attack 01", wind-up (source f1-22) 1.5x slower; the right hand lashes out at f50.</summary>
        public const string Attack2PoseName = "attack2";
        public const string Attack2FileName = "VESPERA_blenderig_attack2_shadow_lash.fbx";
        public const string Attack2Md5 = "cbb1313fdc9024350725fe2575560e7c";
        public const int Attack2LastFrame = 101;
        public const int Attack2ContactFrame = 50;

        /// <summary>Hex rain: "Standing 2H Magic Area Attack 02", gather (source f1-40) 1.5x slower, push down f75, arms burst up f84; hexes land f84, f88, f92.</summary>
        public const string Attack3PoseName = "attack3";
        public const string Attack3FileName = "VESPERA_blenderig_attack3_hex_rain.fbx";
        public const string Attack3Md5 = "d4035cf1ec4d5fe6522a81496b7a6fc6";
        public const int Attack3LastFrame = 139;
        public const int Attack3ContactFrame = 84;
        public const int WindupScalePercent = 150;

        public static readonly string[] AttackPoseNames = { AttackPoseName, Attack2PoseName, Attack3PoseName };

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName, Attack2PoseName, Attack3PoseName };
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
                    VesperaAttack.ClipSeconds,
                    BlenderRigAttackSpec.FrameRate,
                    VesperaAttack.LastFrame),
                new BlenderRigClip(Attack2PoseName, Attack2FileName, ExtraTakeName, Attack2LastFrame / WalkFrameRate, WalkFrameRate, Attack2LastFrame),
                new BlenderRigClip(Attack3PoseName, Attack3FileName, ExtraTakeName, Attack3LastFrame / WalkFrameRate, WalkFrameRate, Attack3LastFrame)
            });
    }
}
