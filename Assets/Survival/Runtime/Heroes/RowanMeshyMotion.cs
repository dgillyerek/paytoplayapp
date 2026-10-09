using Survival.Domain.Roster;
using Survival.Domain.View;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan Meshy compare, NO-BOW pack (Design meshy_nocape_nobow_20261008): the Meshy auto-rigged
    /// "Azure Ranger" (Mixamo skeleton template) with the fused bow and string deleted from the body,
    /// plus Design's rigid bow prop. Rest = Idle_02, walk = Walking, attack = Archery Shot with the aim
    /// fixed inside the export (-99° about the hips' ground point), so no Unity body turn.
    /// Rest/walk: static bow on RightHand (meta idle/walk offset). Attack: idle bow hidden; baked
    /// per-frame bow (LeftHand grip, string draw) and arrow (hand → string → flight, release f76) play in
    /// sync. The old fused-bow pack (meshy_nocape_20261008) stays committed under design/ as superseded.
    /// Metres, 24 fps. Separate scene and menu next to the Blender-rig Rowan (#50). HOLD merge.
    /// </summary>
    public static class RowanMeshyMotion
    {
        public const string Name = "RowanMeshyCompare";
        public const string Title = "ROWAN · MESHY COMPARE";
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/rowan_meshy";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_nobow_20261008";
        public const string SupersededDesignDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_20261008";
        public const string MenuItem = "Survival/Rowan Meshy Compare (rest + walk + attack, Game view 1080x1920)";
        public const string ScenePath = "Assets/Survival/Scenes/RowanMeshyCompare.unity";

        public const string RestFileName = "ROWAN_meshy_nobow_rest.fbx";
        public const string WalkFileName = "ROWAN_meshy_nobow_walk.fbx";
        public const string AttackFileName = "ROWAN_meshy_nobow_attack.fbx";
        public const string BowAttackFileName = "ROWAN_meshy_bow_attack.fbx";
        public const string ArrowAttackFileName = "ROWAN_meshy_arrow_attack.fbx";
        public const string BowPropFileName = "props/ROWAN_bow_meshy.fbx";
        public const string ArrowPropFileName = "props/ROWAN_arrow_blue_fletch_meshy.fbx";
        public const string BowMetaFileName = "ROWAN_meshy_bow_meta.json";

        public const string RestMd5 = "7868c8337afdcad0c03a2fa4684f3798";
        public const string WalkMd5 = "d0a8262872c65b19288cf6f54f36a769";
        public const string AttackMd5 = "ed5e50e663304431cab3c57de105609e";
        public const string BowAttackMd5 = "12240e87b97897940285cd9886308a91";
        public const string ArrowAttackMd5 = "4d99e038dd431d493df6d961f41c096d";
        public const string BowPropMd5 = "698cf4720af7c8b8b3142c1c57fb7e12";
        public const string ArrowPropMd5 = "958692923580766df6187103c3e8f7bd";
        public const string BowMetaMd5 = "d3c32103af78de52c27e24920e275072";

        /// <summary>Every no-bow export carries a single AnimationStack named Scene.</summary>
        public const string TakeName = "Scene";

        /// <summary>FBX GlobalSettings CustomFrameRate (24, same as the Meshy source).</summary>
        public const float FileFrameRate = 24f;
        public const int RestLastFrame = 45;
        public const int WalkLastFrame = 25;
        public const int AttackLastFrame = 120;

        /// <summary>Design re-exported in metres (FBX UnitScaleFactor 100), so file scale stays off.</summary>
        public const bool UseFileScale = false;

        /// <summary>Arrow release (0-based, t = 3.167 s); flight is baked at ~12.5 m/s up-screen.</summary>
        public const int AttackReleaseFrame = 76;
        public const float ArrowSpeedMetresPerSecond = 12.5257f;

        /// <summary>Rear battle camera: behind Rowan, slightly high, so the shot reads toward the top of the screen.</summary>
        public const float CameraYawDegrees = PortraitGameView.RearYawDegrees;
        public const float CameraPitchDegrees = -20f;

        public const string ArmatureName = "target_character";
        public const string BodyMeshName = "output_unwrapped";
        public const string BoneRoot = "mixamorig:Hips";
        public const string RightHand = "mixamorig:RightHand";
        public const string LeftHand = "mixamorig:LeftHand";
        public const int BoneCount = 88;
        public const int Triangles = 29551;

        /// <summary>Full-draw hold frame used to bind the LeftHand fallback bow (0-based).</summary>
        public const int FallbackBindFrame = 68;

        /// <summary>
        /// Idle bow root in character space (Unity: x, y, z, qx, qy, qz, qw) at rest frame 0:
        /// RightHand pose @ meta offset_in_RightHand_rest_walk, converted with (x, y, z)b → (-x, z, -y)u.
        /// </summary>
        public static readonly float[] IdleBowCharacterPose =
        {
            0.38545f, 0.95753f, 0.15926f, 0.45467f, 0.38234f, 0.05187f, 0.80275f
        };

        /// <summary>LeftHand fallback bow root at attack frame 68 (LeftHand pose @ meta offset_in_LeftHand_attack).</summary>
        public static readonly float[] FallbackBowCharacterPose =
        {
            0.07096f, 1.41245f, 0.77648f, 0.08719f, -0.00038f, 0.01272f, 0.99611f
        };

        public const string TextureFolder = "textures";
        public const string BaseColorFile = "Meshy_AI_Azure_Ranger_biped_texture_0.png";
        public const string NormalFile = "Meshy_AI_Azure_Ranger_biped_texture_0_normal.png";
        public const string MetallicFile = "Meshy_AI_Azure_Ranger_biped_texture_0_metallic.png";
        public const string RoughnessFile = "Meshy_AI_Azure_Ranger_biped_texture_0_roughness.png";
        public const string BaseColorMd5 = "84f9e81b7e049538a8c3412cc5f7d39c";
        public const string NormalMd5 = "a074379f08cc1bf2a3ac5a2211dc708c";
        public const string MetallicMd5 = "33215dc5dddf96ed7402d6e4bc59d221";
        public const string RoughnessMd5 = "716b5985cb9f629fcac2be668dd8cd4a";

        /// <summary>Every LimbNode in the no-bow body exports (same 88 names and order as the Meshy source).</summary>
        public static readonly string[] BoneNames =
        {
            "mixamorig:Hips",
            "mixamorig:Spine",
            "mixamorig:Spine1",
            "mixamorig:Spine2",
            "mixamorig:Neck",
            "mixamorig:Head",
            "mixamorig:HeadTop_End",
            "mixamorig:HeadTop_End_end",
            "Bone_021",
            "Bone_020",
            "Bone_020_end",
            "headfront",
            "headfront_end",
            "mixamorig:LeftShoulder",
            "mixamorig:LeftArm",
            "mixamorig:LeftForeArm",
            "mixamorig:LeftHand",
            "mixamorig:LeftHandThumb1",
            "mixamorig:LeftHandThumb2",
            "mixamorig:LeftHandThumb3",
            "mixamorig:LeftHandThumb4",
            "mixamorig:LeftHandThumb4_end",
            "mixamorig:LeftHandPinky1",
            "mixamorig:LeftHandPinky2",
            "mixamorig:LeftHandPinky3",
            "mixamorig:LeftHandPinky4",
            "mixamorig:LeftHandPinky4_end",
            "mixamorig:LeftHandMiddle1",
            "mixamorig:LeftHandMiddle2",
            "mixamorig:LeftHandMiddle3",
            "mixamorig:LeftHandMiddle4",
            "mixamorig:LeftHandMiddle4_end",
            "mixamorig:LeftHandRing1",
            "mixamorig:LeftHandRing2",
            "mixamorig:LeftHandRing3",
            "mixamorig:LeftHandRing4",
            "mixamorig:LeftHandRing4_end",
            "Bone_056",
            "Bone_056_end",
            "mixamorig:RightShoulder",
            "mixamorig:RightArm",
            "mixamorig:RightForeArm",
            "mixamorig:RightHand",
            "Bone_057",
            "Bone_057_end",
            "mixamorig:RightHandIndex1",
            "mixamorig:RightHandIndex2",
            "mixamorig:RightHandIndex3",
            "mixamorig:RightHandIndex4",
            "mixamorig:RightHandIndex4_end",
            "mixamorig:RightHandThumb1",
            "mixamorig:RightHandThumb2",
            "mixamorig:RightHandThumb3",
            "mixamorig:RightHandThumb4",
            "mixamorig:RightHandThumb4_end",
            "mixamorig:RightHandPinky1",
            "mixamorig:RightHandPinky2",
            "mixamorig:RightHandPinky3",
            "mixamorig:RightHandPinky4",
            "mixamorig:RightHandPinky4_end",
            "mixamorig:RightHandMiddle1",
            "mixamorig:RightHandMiddle2",
            "mixamorig:RightHandMiddle3",
            "mixamorig:RightHandMiddle4",
            "mixamorig:RightHandMiddle4_end",
            "mixamorig:RightHandRing1",
            "mixamorig:RightHandRing2",
            "mixamorig:RightHandRing3",
            "mixamorig:RightHandRing4",
            "mixamorig:RightHandRing4_end",
            "Bone_029",
            "Bone_028",
            "Bone_027",
            "Bone_026",
            "Bone_025",
            "Bone_025_end",
            "mixamorig:LeftUpLeg",
            "mixamorig:LeftLeg",
            "mixamorig:LeftFoot",
            "mixamorig:LeftToeBase",
            "mixamorig:LeftToe_End",
            "mixamorig:LeftToe_End_end",
            "mixamorig:RightUpLeg",
            "mixamorig:RightLeg",
            "mixamorig:RightFoot",
            "mixamorig:RightToeBase",
            "mixamorig:RightToe_End",
            "mixamorig:RightToe_End_end"
        };

        public static readonly MeshyRigSpec Spec = new MeshyRigSpec(
            Name,
            Title,
            ThemePackDir,
            DesignDir,
            new MeshyRigClip(MeshyRigSpec.RestPoseName, RestFileName, RestMd5, "Idle_02", TakeName, FileFrameRate, RestLastFrame),
            new MeshyRigClip(MeshyRigSpec.WalkPoseName, WalkFileName, WalkMd5, "Walking", TakeName, FileFrameRate, WalkLastFrame),
            new MeshyRigClip(MeshyRigSpec.AttackPoseName, AttackFileName, AttackMd5, "Archery_Shot", TakeName, FileFrameRate, AttackLastFrame),
            ArmatureName,
            BoneRoot,
            BoneNames,
            BodyMeshName,
            System.Array.Empty<string>(),
            TextureFolder,
            BaseColorFile,
            NormalFile,
            MetallicFile,
            RoughnessFile,
            UseFileScale,
            new[]
            {
                new MeshyHeldProp("ROWAN_bow_idle", BowPropFileName, BowPropMd5, RightHand, MeshyRigSpec.RestPoseName, 0, IdleBowCharacterPose)
            },
            new[]
            {
                new MeshyBakedProp("ROWAN_bow_attack", BowAttackFileName, BowAttackMd5, TakeName, AttackLastFrame),
                new MeshyBakedProp("ROWAN_arrow_attack", ArrowAttackFileName, ArrowAttackMd5, TakeName, AttackLastFrame)
            },
            new[]
            {
                new MeshyHeldProp("ROWAN_bow_attack_fallback", BowPropFileName, BowPropMd5, LeftHand, MeshyRigSpec.AttackPoseName, FallbackBindFrame, FallbackBowCharacterPose)
            },
            AttackReleaseFrame,
            CameraYawDegrees,
            CameraPitchDegrees);

        public static readonly BlenderRigSpec DemoSpec = Spec.ToDemoSpec();
    }
}
