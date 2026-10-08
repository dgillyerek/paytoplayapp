using Survival.Domain.Roster;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Stormcrest Design Blender rig. Derek's hand-built skeleton: 44 bones plus a
    /// non-deforming root at the origin (45 total). Custom Generic, not Mixamo.
    /// Rest is the bind pose. Wing flap, walk, and attack are fresh 0–30 Scene takes at 30 fps.
    /// Basecolor and normal are embedded. No cloth. HOLD merge until Derek Game-view PASS.
    /// </summary>
    public static class StormcrestMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/stormcrest";
        public const string DesignRigDir = "design/survival-theme-a-fantasy/heroes/anim/stormcrest/blender_rig";
        public const string RestFileName = "STORMCREST_blenderig.fbx";
        public const string WalkFileName = "STORMCREST_blenderig_walk.fbx";
        public const string WingFlapFileName = "STORMCREST_blenderig_wingflap.fbx";
        public const string BlendFileName = "STORMCREST_blenderig.blend";
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string WingFlapThemePackRel = ThemePackDir + "/" + WingFlapFileName;
        public const string AttackPoseName = BlenderRigAttackSpec.PoseName;
        public const string AttackFileName = StormcrestAttack.FileName;
        public const string RejectedMixamoFileName = "STORMCREST_rig.fbx";

        public const string RestPoseName = BlenderRigSpec.RestPoseName;
        public const string WingFlapPoseName = "wing flap";
        public const string WalkPoseName = "walk";
        public const string WalkTakeName = "Scene";
        public const float WalkSeconds = 1f;
        public const float WalkFrameRate = 30f;
        public const int WalkLastFrame = 30;
        public const int BoneCount = 45;

        public const string BlendMd5 = "98d5a29d9cc2516b67d032bf9aebc1e6";
        public const string RestMd5 = "35dc5e9c396281225ca4734646bdd964";
        public const string WalkMd5 = "2c56cda37920642e4eef461436f04920";
        public const string WingFlapMd5 = "e4943eb627c722465a6e37fe2674ea39";

        public const string TextureFolder = "STORMCREST_blenderig.fbm";
        public const string BaseColorFile = "STORMCREST_basecolor_0.jpg";
        public const string NormalFile = "STORMCREST_normal_2.jpg";

        public const string BoneRoot = "root";

        public static readonly string[] PoseNames = { RestPoseName, WingFlapPoseName, WalkPoseName, AttackPoseName };

        public static readonly string[] BoneNames =
        {
            "root",
            "spine",
            "thigh.L",
            "shin.L",
            "foot.L",
            "toe.L",
            "thigh.R",
            "shin.R",
            "foot.R",
            "toe.R",
            "keel",
            "neck",
            "neck_02",
            "head",
            "wing_arm.L",
            "wing_forearm.L",
            "wing_hand.L",
            "wing_feather_a.L",
            "wing_feather_b.L",
            "wing_feather_c.L",
            "wing_feather_d.L",
            "wing_pin.L",
            "wing_elbow.L",
            "wing_pin_b.L",
            "wing_arm.R",
            "wing_forearm.R",
            "wing_hand.R",
            "wing_feather_a.R",
            "wing_feather_b.R",
            "wing_feather_c.R",
            "wing_feather_d.R",
            "wing_feather_e.R",
            "hips",
            "tail_01",
            "tail_02",
            "tail_03",
            "tail_04",
            "hind_thigh.L",
            "hind_shin.L",
            "hind_foot.L",
            "hind_toe.L",
            "hind_thigh.R",
            "hind_shin.R",
            "hind_foot.R",
            "hind_toe.R"
        };

        public static readonly BlenderRigSpec Spec = new BlenderRigSpec(
            "Stormcrest",
            ThemePackDir,
            RestFileName,
            RejectedMixamoFileName,
            WingFlapPoseName,
            WingFlapFileName,
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
                    WalkPoseName,
                    WalkFileName,
                    WalkTakeName,
                    WalkSeconds,
                    WalkFrameRate,
                    WalkLastFrame),
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
