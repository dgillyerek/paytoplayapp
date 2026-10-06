namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Ironhowl Mixamo body. Bone root mixamorig:Hips. Generic import.
    /// Rest, walk, and attack are separate skinned FBX files on this skeleton.
    /// Take is mixamo.com at 30 fps (FBX TimeMode Frames30). Keys sit on every frame.
    /// Rest LocalStop 1539538600 ticks is frame 1. Walk LocalStop 47725696600 is frame 31.
    /// Attack LocalStop 40028003600 is frame 26. No mirror. No keyframe reduction.
    /// The look JPEG and the atlas are QC stills and are not bound.
    /// </summary>
    public static class IronhowlMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl";
        public const string DesignMixamoDir = "design/survival-theme-a-fantasy/enemies/anim/ironhowl/mixamo";

        public const string BodyFileName = "IRONHOWL_rig.fbx";
        public const string RestFileName = "IRONHOWL_rig_rest.fbx";
        public const string WalkFileName = "IRONHOWL_rig_walk.fbx";
        public const string AttackFileName = "IRONHOWL_rig_attack.fbx";
        public const string AtlasFileName = "IRONHOWL_rig_atlas.png";
        public const string LookReferenceFileName = "Ironhowl_meshy_look.jpg";

        public const string BodyThemePackRel = ThemePackDir + "/" + BodyFileName;
        public const string RestThemePackRel = ThemePackDir + "/" + RestFileName;
        public const string WalkThemePackRel = ThemePackDir + "/" + WalkFileName;
        public const string AttackThemePackRel = ThemePackDir + "/" + AttackFileName;

        public const string BaseColorFileName = "ironhowl_basecolor.png";
        public const string NormalFileName = "ironhowl_normal.png";
        public const string MetallicFileName = "ironhowl_metallic.png";
        public const string RoughnessFileName = "ironhowl_roughness.png";

        public const string RestPoseName = "rest";
        public const string WalkPoseName = "walk";
        public const string AttackPoseName = "attack";

        /// <summary>Verified AnimationStack on every Ironhowl Mixamo FBX. Not a Blender Scene take.</summary>
        public const string TakeName = "mixamo.com";

        public const float FrameRate = 30f;
        public const int RestLastFrame = 1;
        public const float RestSeconds = 1f / 30f;
        public const int WalkLastFrame = 31;
        public const float WalkSeconds = 31f / 30f;
        public const int AttackLastFrame = 26;
        public const float AttackSeconds = 26f / 30f;
        public const bool Mirror = false;

        public const string BodyMd5 = "2076255b828787ba8db7ea24d075f231";
        public const string RestMd5 = "2076255b828787ba8db7ea24d075f231";
        public const string WalkMd5 = "8c49b0f95c70418c2e7938036080040d";
        public const string AttackMd5 = "21cd216d46c3363ad9af8993a92daba3";
        public const string AtlasMd5 = "7c4b6b14ef204acf51139789b76d498d";

        public const string BoneRoot = "mixamorig:Hips";
        public const string SceneName = "Ironhowl";

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName };
    }
}
