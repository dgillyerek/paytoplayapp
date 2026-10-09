using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
/// <summary>
/// Lyra fitted/tighten (Design blender_rig_tighten_20261008, look PASS 2026-10-08).
/// Mixamo-compatible mixamorig, 22 bones. Rest bind, 1 s Scene walk at 30 fps,
/// attack add-on (attack_20261008). Replaces prior blenderig + attack_20261007.
/// Bones bound by name. Staff stays in RightHand.
/// </summary>
    public static class LyraMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/lyra";
        public const string RestFileName = "LYRA_blenderig.fbx";
        public const string WalkFileName = "LYRA_blenderig_walk.fbx";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = LyraAttack.FileName;
        public const string RejectedMixamoFileName = "LYRA_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = MixamoHumanoidBones.Count;

        public const string TextureFolder = "LYRA_blenderig.fbm";
        public const string BaseColorFile = "LYRA_tighten_basecolor_0.jpg";
        public const string NormalFile = "LYRA_tighten_normal_2.jpg";

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



        /// <summary>Staff for every clip: Design props/LYRA_staff.fbx on RightHand. It stands in for the attack's LYRA_staff track (same mesh and grip), so the attack never loads a second, untextured copy.</summary>
        public static readonly BlenderRigIdlePropSpec[] IdleProps =
        {
            new BlenderRigIdlePropSpec(
                "LYRA_staff_rest",
                "LYRA_staff_rest.fbx",
                "69872fb5cf0f1a515b0e17ca884bccc1",
                "mixamorig:RightHand",
                new[] { 0.54468f, 1.2833f, 0.13197f, 0.01582f, -0.00068f, -0.04293f, 0.99895f },
                new[] { 0.27652f, 1.88927f, 0.11728f },
                "Design props/LYRA_staff.fbx on RightHand in rest, walk and attack (rest_world from LYRA_staff_meta.json). Stands in for the attack's LYRA_staff track so one textured staff serves every clip.",
                "LYRA_staff")
        };

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Lyra",
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
            "LYRA_tighten_metal_rough_1.png",
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
