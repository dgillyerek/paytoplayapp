namespace Survival.Domain.Enemies
{
    /// <summary>
    /// Bonequill Meshy Animate walk wire (Design handoff 2026-09-25).
    /// Walk-only. Path A cancelled. No Design PASS. No hub unlock.
    /// </summary>
    public static class BonequillAnimateWalk
    {
        public const string ThemePackFbx =
            "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk.fbx";

        public const string ThemePackAtlas =
            "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk_atlas.png";

        public const string ThemePackNormal =
            "ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk_normal.png";

        public const string ClipHint = "Walking";

        /// <summary>#25: clipAnimations.takeName must match the FBX stack.</summary>
        public const string WalkingTakeName = "target_character|target_character|target_character|Walking";

        public const string MeshName = "Bonequill_AccuRIG_BodyOnly";

        /// <summary>
        /// Mixamo/AccuRIG Humanoid import faces −Z (frontal). Yaw so back faces the
        /// Play cam and march is world +Z = screen TOP.
        /// </summary>
        public const float MixamoImportRearYawDegrees = 180f;

        public const string SoftLeftover =
            "Body-only mesh: bow (character-RIGHT) and quiver (character-LEFT) culled so AccuRIG succeeded. Design gate later — does not block this walk wire.";

        public const string Authorship =
            "Meshy Animate Walking clip AS-IS on AccuRIG Mixamo Humanoid. Atlas albedo + FBX normal bound. Path A weight-paint is CANCELLED. No Design PASS.";
    }
}
