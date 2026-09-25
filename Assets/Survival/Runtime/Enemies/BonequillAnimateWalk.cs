namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill Meshy Animate walk v2 (Design handoff 2026-09-25).
    /// Continuous AccuRIG body (props kept). Walk-only. Path A cancelled. No Design PASS.
    /// </summary>
    public static class BonequillAnimateWalk
    {
        public const string ThemePackFbx =
            "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk_v2.fbx";

        public const string ThemePackAtlas =
            "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk_v2_atlas.png";

        public const string ThemePackNormal =
            "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk_v2_normal.png";

        public const string ClipHint = "Walking";

        /// <summary>#25: clipAnimations.takeName must match the FBX stack.</summary>
        public const string WalkingTakeName = "Armature|Armature|Armature|Walking";

        public const string MeshName = "Bonequill_AccuRIG_Continuous_V2";

        /// <summary>
        /// AccuRIG Humanoid import faces −Z (frontal). Yaw so back faces the
        /// Play cam and march is world +Z = screen TOP.
        /// </summary>
        public const float MixamoImportRearYawDegrees = 180f;

        public const string SoftLeftover =
            "Paint bake may be softer than remesh UVs; silhouette is continuous. Side law: bow character-RIGHT, quiver character-LEFT (fused on mesh).";

        public const string Authorship =
            "Meshy Animate Walking clip AS-IS on AccuRIG Humanoid (native avatar, Hips/Spine02). Atlas albedo + normal bound. Path A weight-paint is CANCELLED. No Design PASS.";
    }
}
