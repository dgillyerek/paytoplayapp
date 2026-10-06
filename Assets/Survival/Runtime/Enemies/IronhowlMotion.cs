namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Ironhowl Mixamo body. Bone root mixamorig:Hips. Generic import.
    /// Rest, walk, and attack are separate clip FBX files on this same skeleton.
    /// The body file's AnimationStack is mixamo.com. Clip frame ranges are read
    /// from each FBX when Design drops it. Do not invent a frame range.
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

        /// <summary>Verified AnimationStack on IRONHOWL_rig.fbx. Not a Blender Scene take.</summary>
        public const string TakeName = "mixamo.com";

        public const string BoneRoot = "mixamorig:Hips";
        public const string SceneName = "Ironhowl";

        public static readonly string[] PoseNames = { RestPoseName, WalkPoseName, AttackPoseName };
    }
}
