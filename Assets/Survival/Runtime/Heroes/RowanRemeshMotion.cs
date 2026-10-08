using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Male Rowan remesh compare (Meshy to hum_pipeline, 2026-10-07).
    /// Same mixamorig contract as the tip blenderig: rest bind, 1 second Scene walk
    /// at 30 fps (Blender frames 1–31, Unity last frame 30). Does not replace
    /// ROWAN_blenderig or the earlier feminine ROWAN_remesh files.
    /// Armpit webbing is a tight A-pose heat caveat for twist/walk QC, not a Demo wire blocker.
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
        public const string BaseColorFile = "ROWAN_male_basecolor_0.jpg";
        public const string NormalFile = "ROWAN_male_normal_2.jpg";

        public const string BoneRoot = MixamoHumanoidBones.Root;

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName };
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
