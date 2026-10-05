namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Ironhowl Mixamo humanoid T-pose. No motion clips yet.
    /// The Meshy look JPEG is a reference still, not a UV map.
    /// </summary>
    public static class IronhowlMotion
    {
        public const string ThemePackDir = "ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl";
        public const string BodyFileName = "IRONHOWL_rig.fbx";
        public const string BodyThemePackRel = ThemePackDir + "/" + BodyFileName;
        public const string LookReferenceFileName = "Ironhowl_meshy_look.jpg";
        public const string BoneRoot = "mixamorig:Hips";

        public const string SceneName = "Ironhowl";
    }
}
