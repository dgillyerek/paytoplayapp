using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan fresh rebuild (2026-10-13): Derek-approved hero-blue front/back look, Design's Meshy image-to-3D, decimated to 200k faces,
    /// new 20-bone mixamorig rig (no ToeBase) in Blender 5.2.2 (design .../rowan/fresh_20261013). Face and skull 100% on Head (narrow
    /// tilted Neck to Head band); tunic, coat flaps and belt pieces 100% on Hips (never the legs); short capelet rides the spine chain
    /// and shoulders; the Meshy bow slung on his back is rigid on Spine2. Arms ripped from the coat below the armpit so nothing webs.
    /// Rest = bind pose, arms hanging by the sides. Walk = 1 s in place at 30 fps, head forward with a tiny nod. Bow attack in
    /// RowanAttack. Imported Generic like the locked rigs. Replaces the failed #54/#50/#45/#35 packs. HOLD merge until Derek Game-view PASS.
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
        public const string RestMd5 = "ca2e980bfa81dcb8918ba51e375d72c3";
        public const string WalkMd5 = "384445362819cbda58c1947d6b6b3d5d";
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

        /// <summary>Quiver on Spine2 (see RowanAttack.IdleProps).</summary>
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
