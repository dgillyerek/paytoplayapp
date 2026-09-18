using Survival.Domain.Heroes;
using Survival.Domain.Ids;

namespace Survival.Domain.Tests;

public sealed class SirAldric3DMotionTests
{
    [Fact]
    public void Loop_is_walk_then_attack_then_walk()
    {
        Assert.False(SirAldric3DMotion.IsAttacking(0f));
        Assert.False(SirAldric3DMotion.IsAttacking(SirAldric3DMotion.WalkBlockSeconds - 0.01f));
        Assert.True(SirAldric3DMotion.IsAttacking(SirAldric3DMotion.WalkBlockSeconds + 0.01f));
        Assert.False(SirAldric3DMotion.Evaluate(SirAldric3DMotion.LoopSeconds + 0.05f).Attacking);
    }

    [Fact]
    public void Walk_is_squared_rear_scabbard_character_right_opposite_stride()
    {
        for (var i = 0; i < 8; i++)
        {
            var pose = SirAldric3DMotion.Evaluate(i * SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
            Assert.False(pose.Attacking);
            Assert.False(pose.SwordDrawn);
            Assert.True(pose.FacesTop);
            Assert.True(pose.SheathedOnCharacterRight);
        }

        var a = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.25f);
        var b = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkPeriodSeconds * 0.75f);
        // −X thigh = toward world +Z = TOP. At ¼ cycle left leads TOP; at ¾ right leads TOP.
        Assert.True(a.UpLegL.X < -20f);
        Assert.True(a.UpLegR.X > 20f);
        Assert.True(b.UpLegL.X > 20f);
        Assert.True(b.UpLegR.X < -20f);
        Assert.True(a.LegR.X > 8f);
        Assert.True(b.LegL.X > 8f);
        Assert.True(a.LeadLegTowardTop);
        Assert.True(b.LeadLegTowardTop);
    }

    [Fact]
    public void RootZ_increases_during_the_loop_toward_top()
    {
        var a = SirAldric3DMotion.Evaluate(0.05f);
        var b = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkBlockSeconds - 0.05f);
        var c = SirAldric3DMotion.Evaluate(SirAldric3DMotion.LoopSeconds - 0.05f);
        Assert.True(b.RootZ > a.RootZ + 0.3f);
        Assert.True(c.RootZ > b.RootZ);
        Assert.InRange(c.RootZ, 0.8f, 2.0f);
    }

    [Fact]
    public void Attack_strikes_toward_top_then_resheaths_on_character_right()
    {
        var strike = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkBlockSeconds + SirAldric3DMotion.AttackSeconds * 0.48f);
        Assert.True(strike.Attacking);
        Assert.True(strike.SwordDrawn);
        Assert.True(strike.FacesTop);
        Assert.True(strike.StrikeTowardTop);
        Assert.InRange(strike.ArmR.X, -175f, -80f);

        var end = SirAldric3DMotion.Evaluate(SirAldric3DMotion.WalkBlockSeconds + SirAldric3DMotion.AttackSeconds - 0.02f);
        Assert.True(end.Attacking);
        Assert.False(end.SwordDrawn);
        Assert.True(end.SheathedOnCharacterRight);
    }

    [Fact]
    public void Look_targets_00_01_02_are_on_disk()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(
            root,
            "design",
            "survival-theme-a-fantasy",
            "heroes",
            "anim",
            "sir_aldric",
            "UNITY_3D_HANDOFF",
            "look_targets");
        Assert.True(new FileInfo(Path.Combine(dir, "00_fullbody_LOCKED.png")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "01_rear_LOCKED.png")).Length > 100_000);
        Assert.True(new FileInfo(Path.Combine(dir, "02_aldric_turnaround_orthos.png")).Length > 100_000);
    }

    [Fact]
    public void Play_placeholder_locked_rear_png_still_exists()
    {
        var root = FindRepoRoot();
        var packPng = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "SIR_ALDRIC_REAR_MASTER_LOCKED.png");
        Assert.True(File.Exists(packPng));
        Assert.True(new FileInfo(packPng).Length > 100_000);
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
