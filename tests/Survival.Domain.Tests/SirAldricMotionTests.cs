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
        for (var i = 0; i < 8; i++)
        {
            var pose = SirAldricMotion.Evaluate(i * SirAldricMotion.WalkPeriodSeconds * 0.25f);
            Assert.False(pose.Attacking);
            Assert.False(pose.SwordDrawn);
            Assert.True(pose.FacesTop);
            Assert.True(pose.SheathedOnViewerRight);
            Assert.InRange(pose.Sword.RotZ, -12f, 12f);
        }

        Assert.True(SirAldricMotion.Layout.Sword.CenterX > 0.5f);
        var maxStep = SirAldricMotion.Evaluate(SirAldricMotion.WalkPeriodSeconds * 0.25f);
        Assert.True(maxStep.LegL.Y > 0.08f);
        Assert.True(maxStep.LegR.Y < -0.08f);
        Assert.True(Math.Abs(maxStep.LegL.RotZ) > 12f);
        var opposite = SirAldricMotion.Evaluate(SirAldricMotion.WalkPeriodSeconds * 0.75f);
        Assert.True(opposite.LegL.Y < -0.08f);
        Assert.True(opposite.LegR.Y > 0.08f);
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
    public void Warp_rest_is_identity_and_limbs_stay_connected()
    {
        var rest = SirAldricMotion.Evaluate(0f);
        SirAldricWarp.Displace(0.5f, 0.5f, rest, out var mu, out var mv);
        Assert.InRange(mu, 0.5f - 1e-4f, 0.5f + 1e-4f);
        Assert.InRange(mv, 0.5f - 1e-4f, 0.5f + 1e-4f);

        var walk = SirAldricMotion.Evaluate(SirAldricMotion.WalkPeriodSeconds * 0.25f);
        SirAldricWarp.Displace(SirAldricMotion.Layout.HipL.X, SirAldricMotion.Layout.HipL.Y, walk, out var hu, out var hv);
        SirAldricWarp.Displace(SirAldricMotion.Layout.FootL.X, SirAldricMotion.Layout.FootL.Y, walk, out var fu, out var fv);
        var hipMove = Math.Sqrt(
            (hu - SirAldricMotion.Layout.HipL.X) * (hu - SirAldricMotion.Layout.HipL.X)
            + (hv - SirAldricMotion.Layout.HipL.Y) * (hv - SirAldricMotion.Layout.HipL.Y));
        var footMove = Math.Sqrt(
            (fu - SirAldricMotion.Layout.FootL.X) * (fu - SirAldricMotion.Layout.FootL.X)
            + (fv - SirAldricMotion.Layout.FootL.Y) * (fv - SirAldricMotion.Layout.FootL.Y));
        Assert.True(hipMove < 0.04f);
        Assert.True(footMove > 0.05f);
        Assert.True(footMove > hipMove * 2f);

        var cols = SirAldricWarp.GridCols;
        var rows = SirAldricWarp.GridRows;
        var restSpacing = 1f / cols;
        for (var j = 0; j < rows; j++)
        {
            for (var i = 0; i < cols; i++)
            {
                var u0 = i / (float)cols;
                var v0 = j / (float)rows;
                var u1 = (i + 1) / (float)cols;
                var v1 = (j + 1) / (float)rows;
                SirAldricWarp.Displace(u0, v0, walk, out var x00, out var y00);
                SirAldricWarp.Displace(u1, v0, walk, out var x10, out var y10);
                SirAldricWarp.Displace(u0, v1, walk, out var x01, out var y01);
                var dx = Math.Sqrt((x10 - x00) * (x10 - x00) + (y10 - y00) * (y10 - y00));
                var dy = Math.Sqrt((x01 - x00) * (x01 - x00) + (y01 - y00) * (y01 - y00));
                Assert.True(dx < Math.Max(restSpacing * 6.0, 0.22));
                Assert.True(dy < Math.Max((1.0 / rows) * 6.0, 0.22));
            }
        }
    }

    [Fact]
    public void Warp_attack_bends_sword_tip_toward_top_hilt_stays_attached()
    {
        var strike = SirAldricMotion.Evaluate(SirAldricMotion.WalkBlockSeconds + SirAldricMotion.AttackSeconds * 0.50f);
        SirAldricWarp.Displace(SirAldricMotion.Layout.Hilt.X, SirAldricMotion.Layout.Hilt.Y, strike, out var hu, out var hv);
        SirAldricWarp.Displace(SirAldricMotion.Layout.Tip.X, SirAldricMotion.Layout.Tip.Y, strike, out var tu, out var tv);
        var hiltMove = Math.Sqrt(
            (hu - SirAldricMotion.Layout.Hilt.X) * (hu - SirAldricMotion.Layout.Hilt.X)
            + (hv - SirAldricMotion.Layout.Hilt.Y) * (hv - SirAldricMotion.Layout.Hilt.Y));
        Assert.True(hiltMove < 0.05f);
        Assert.True(tv > SirAldricMotion.Layout.Tip.Y + 0.12f);
        Assert.True(tv > hv);
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

    [Fact]
    public void Locked_rear_master_png_bytes_are_in_themepack_and_design_drop()
    {
        var root = FindRepoRoot();
        var packPng = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "SIR_ALDRIC_REAR_MASTER_LOCKED.png");
        var pack512 = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "SIR_ALDRIC_REAR_MASTER_LOCKED_512.png");
        var dropPng = Path.Combine(root, "design", "survival-theme-a-fantasy", "heroes", "anim", "sir_aldric", "UNITY_DROP_LOCKED", "SIR_ALDRIC_REAR_MASTER_LOCKED.png");
        var drop512 = Path.Combine(root, "design", "survival-theme-a-fantasy", "heroes", "anim", "sir_aldric", "UNITY_DROP_LOCKED", "SIR_ALDRIC_REAR_MASTER_LOCKED_512.png");
        Assert.True(File.Exists(packPng));
        Assert.True(File.Exists(pack512));
        Assert.True(File.Exists(dropPng));
        Assert.True(File.Exists(drop512));
        Assert.Equal(File.ReadAllBytes(dropPng), File.ReadAllBytes(packPng));
        Assert.Equal(File.ReadAllBytes(drop512), File.ReadAllBytes(pack512));
        Assert.True(new FileInfo(packPng).Length > 100_000);
        Assert.True(new FileInfo(pack512).Length > 50_000);
        Assert.False(packPng.Contains("theme_a_hero_knight_01", StringComparison.Ordinal));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("repo root");
    }
}
