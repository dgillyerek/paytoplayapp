using Survival.Domain.Heroes;
using Survival.Domain.Ids;

namespace Survival.Domain.Tests;

public sealed class SirAldricMotionTests
{
    [Fact]
    public void Loop_is_walk_then_attack_then_walk()
    {
        Assert.False(SirAldricMotion.IsAttacking(0f));
        Assert.False(SirAldricMotion.IsAttacking(SirAldricMotion.WalkBlockSeconds - 0.01f));
        Assert.True(SirAldricMotion.IsAttacking(SirAldricMotion.WalkBlockSeconds + 0.01f));
        Assert.False(SirAldricMotion.Evaluate(SirAldricMotion.LoopSeconds + 0.05f).Attacking);
    }

    [Fact]
    public void Walk_keeps_sword_sheathed_on_viewer_right_and_faces_top()
    {
        for (var i = 0; i < 12; i++)
        {
            var pose = SirAldricMotion.Evaluate(i * SirAldricMotion.WalkPeriodSeconds * 0.25f);
            Assert.False(pose.Attacking);
            Assert.False(pose.SwordDrawn);
            Assert.True(pose.FacesTop);
            Assert.True(pose.SheathedOnViewerRight);
            Assert.InRange(pose.Sword.RotZ, -12f, 12f);
        }

        Assert.True(SirAldricMotion.Layout.Sword.CenterX > 0.5f);
    }

    [Fact]
    public void Attack_strike_points_sword_toward_top_never_toward_camera()
    {
        var strike = SirAldricMotion.Evaluate(SirAldricMotion.WalkBlockSeconds + SirAldricMotion.AttackSeconds * 0.50f);
        Assert.True(strike.Attacking);
        Assert.True(strike.SwordDrawn);
        Assert.True(strike.FacesTop);
        Assert.True(strike.StrikeTowardTop);
        Assert.InRange(strike.Sword.RotZ, 150f, 180f);
        Assert.True(strike.Root.Y >= 0f);
        Assert.InRange(strike.Root.RotZ, -20f, 20f);
    }

    [Fact]
    public void Attack_recovers_to_sheathed_viewer_right()
    {
        var end = SirAldricMotion.Evaluate(SirAldricMotion.WalkBlockSeconds + SirAldricMotion.AttackSeconds - 0.01f);
        Assert.True(end.Attacking);
        Assert.False(end.SwordDrawn);
        Assert.True(end.SheathedOnViewerRight);
        Assert.InRange(end.Sword.RotZ, -8f, 12f);
    }

    [Fact]
    public void March_stays_in_portrait_and_does_not_walk_toward_camera()
    {
        var first = SirAldricMotion.Evaluate(0.2f).MarchY;
        var later = SirAldricMotion.Evaluate(1.6f).MarchY;
        Assert.InRange(first, 0.20f, 0.80f);
        Assert.InRange(later, 0.20f, 0.80f);
    }

    [Fact]
    public void Pack_binds_squared_up_locked_rear_master_not_hud_portrait()
    {
        var pack = TestPaths.LoadPack();
        Assert.True(pack.TryArt(SurvIds.ThemeAHeroSirAldricRear, out var rel));
        Assert.Contains("SIR_ALDRIC_REAR_MASTER_LOCKED.png", rel, StringComparison.Ordinal);
        Assert.DoesNotContain("theme_a_hero_knight_01", rel, StringComparison.Ordinal);
        Assert.True(pack.TryArt(SurvIds.ThemeAHeroSirAldricRear512, out var rel512));
        Assert.Contains("SIR_ALDRIC_REAR_MASTER_LOCKED_512.png", rel512, StringComparison.Ordinal);
        Assert.True(pack.TryArt(SurvIds.ThemeAHeroKnight01, out var portrait));
        Assert.Contains("theme_a_hero_knight_01", portrait, StringComparison.Ordinal);
    }
}
