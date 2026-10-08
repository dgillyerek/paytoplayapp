using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
/// <summary>
/// Rowan no-cape (Design blender_rig_nocape_20261008, look PASS 2026-10-08).
/// Mixamo-compatible mixamorig, 22 bones. Rest bind, 1 s Scene walk at 30 fps,
/// attack add-on (attack_20261008). Replaces the male remesh + walkfix_male_20261007
/// ThemePack contents (file names kept). Bones bound by name.
/// </summary>
    public static class RowanRemeshMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/rowan_remesh_male";
        public const string RestFileName = "ROWAN_male_blenderig.fbx";
        public const string WalkFileName = "ROWAN_male_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = RowanAttack.FileName;
        public const string RejectedMixamoFileName = "ROWAN_male_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = MixamoHumanoidBones.Count;

        public const string TextureFolder = "ROWAN_male_blenderig.fbm";
        public const string BaseColorFile = "ROWAN_nocape_basecolor_0.jpg";
        public const string NormalFile = "ROWAN_nocape_normal_2.jpg";

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
            "mixamorig:LeftToeBase",
            "mixamorig:RightUpLeg",
            "mixamorig:RightLeg",
            "mixamorig:RightFoot",
            "mixamorig:RightToeBase"
        };



        /// <summary>Rest/walk bow: Design props/ROWAN_bow.fbx on RightHand (NOTE names RightHand for rest and walk). Hidden while the attack plays (attack switches to the baked bow_grip track).</summary>
        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "ROWAN_bow_rest",
                "ROWAN_bow.fbx",
                "80758fce88e2a9313a34d171e334ae00",
                "mixamorig:RightHand",
                new[] { 0.50543f, 0.89572f, 0.09054f, 0.11469f, 0.51484f, -0.02885f, 0.84909f },
                new[] { 0.32532f, 1.48009f, 0.28893f },
                "Design props/ROWAN_bow.fbx on RightHand at rest and in the walk (NOTE; rest_world from ROWAN_bow_meta.json). Hidden during attack.")
        };

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
            "ROWAN_nocape_metal_rough_1.png",
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
