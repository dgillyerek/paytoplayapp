using Survival.Domain.Roster;
using Survival.Domain.View;

namespace Survival.Domain.Heroes
{
    /// <summary>
    /// Rowan Meshy compare (Design meshy_nocape_20261008): Meshy auto-rigged "Azure Ranger" on its
    /// Mixamo skeleton template, 31k tris, skinned bow (no bow prop; it warps in the attack, known).
    /// Rest = Meshy Idle_02, walk = Walking, attack = Archery Shot. Separate scene and menu next to the
    /// Blender-rig Rowan (#50) so Derek can compare. HOLD merge until Derek Game-view PASS.
    /// The FBX headers say 24 fps (TimeMode custom, CustomFrameRate 24); clip lengths follow the file.
    /// </summary>
    public static class RowanMeshyMotion
    {
        public const string Name = "RowanMeshyCompare";
        public const string Title = "ROWAN · MESHY COMPARE";
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/heroes/3d/rowan_meshy";
        public const string DesignDir = "design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_20261008";
        public const string MenuItem = "Survival/Rowan Meshy Compare (rest + walk + attack, Game view 1080x1920)";
        public const string ScenePath = "Assets/Survival/Scenes/RowanMeshyCompare.unity";

        public const string RestFileName = "ROWAN_meshy_rest.fbx";
        public const string WalkFileName = "ROWAN_meshy_walk.fbx";
        public const string AttackFileName = "ROWAN_meshy_attack.fbx";
        public const string RestMd5 = "5cb9e286fd4aa63d55c192533e98697d";
        public const string WalkMd5 = "d7680dc0f5848a78078bfb1387058fb5";
        public const string AttackMd5 = "e74bf8ae1f3d73dfe6813318a3e7641f";

        public const string TakePrefix = "target_character|target_character|target_character|";
        public const string RestTakeName = TakePrefix + "Idle_02";
        public const string WalkTakeName = TakePrefix + "Walking";
        public const string AttackTakeName = TakePrefix + "Archery_Shot";

        /// <summary>FBX GlobalSettings CustomFrameRate. Design brief said 30; the files say 24.</summary>
        public const float FileFrameRate = 24f;
        public const int RestLastFrame = 45;
        public const int WalkLastFrame = 25;
        public const int AttackLastFrame = 120;

        /// <summary>Full draw F82–F93 heads -96.8° (character's left) in Unity; +97° turns the shot to +Z.</summary>
        public const float AttackYawDegrees = 97f;
        public const int AttackReleaseFrame = 94;

        /// <summary>Rear battle camera: behind Rowan, slightly high, so the shot reads toward the top of the screen.</summary>
        public const float CameraYawDegrees = PortraitGameView.RearYawDegrees;
        public const float CameraPitchDegrees = -20f;

        public const string ArmatureName = "target_character";
        public const string BodyMeshName = "output_unwrapped";
        public const string StrayMeshName = "Icosphere";
        public const string BoneRoot = "mixamorig:Hips";
        public const int BoneCount = 88;
        public const int Triangles = 31109;

        public const string TextureFolder = "textures";
        public const string BaseColorFile = "Meshy_AI_Azure_Ranger_biped_texture_0.png";
        public const string NormalFile = "Meshy_AI_Azure_Ranger_biped_texture_0_normal.png";
        public const string MetallicFile = "Meshy_AI_Azure_Ranger_biped_texture_0_metallic.png";
        public const string RoughnessFile = "Meshy_AI_Azure_Ranger_biped_texture_0_roughness.png";
        public const string BaseColorMd5 = "84f9e81b7e049538a8c3412cc5f7d39c";
        public const string NormalMd5 = "a074379f08cc1bf2a3ac5a2211dc708c";
        public const string MetallicMd5 = "33215dc5dddf96ed7402d6e4bc59d221";
        public const string RoughnessMd5 = "716b5985cb9f629fcac2be668dd8cd4a";

        /// <summary>Every LimbNode in the three Meshy FBX exports (identical skeletons), in file order.</summary>
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
            new MeshyRigClip(MeshyRigSpec.RestPoseName, RestFileName, RestMd5, "Idle_02", RestTakeName, FileFrameRate, RestLastFrame),
            new MeshyRigClip(MeshyRigSpec.WalkPoseName, WalkFileName, WalkMd5, "Walking", WalkTakeName, FileFrameRate, WalkLastFrame),
            new MeshyRigClip(MeshyRigSpec.AttackPoseName, AttackFileName, AttackMd5, "Archery_Shot", AttackTakeName, FileFrameRate, AttackLastFrame),
            ArmatureName,
            BoneRoot,
            BoneNames,
            BodyMeshName,
            new[] { StrayMeshName },
            TextureFolder,
            BaseColorFile,
            NormalFile,
            MetallicFile,
            RoughnessFile,
            AttackYawDegrees,
            AttackReleaseFrame,
            CameraYawDegrees,
            CameraPitchDegrees);

        public static readonly BlenderRigSpec DemoSpec = Spec.ToDemoSpec();
    }
}
