using System.IO;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-27: "I don't need health bars on the secondary units in the runner."</summary>
public sealed class RunnerHpBarVisibilityTests
{
    [Fact]
    public void Only_the_boss_and_boxes_get_a_world_bar_by_default()
    {
        Assert.False(RunnerSideBar.SecondaryBars);
        // Derek 2026-10-28: hero HP moved to the corner hero panel (HeroHpPanelLayout), no bar over the hero.
        Assert.False(RunnerSideBar.Shows(RunnerBarOwner.LeadHero));
        Assert.True(RunnerSideBar.Shows(RunnerBarOwner.Boss));
        Assert.True(RunnerSideBar.Shows(RunnerBarOwner.Box));
        Assert.False(RunnerSideBar.Shows(RunnerBarOwner.Follower));
        Assert.False(RunnerSideBar.Shows(RunnerBarOwner.Foe));
        Assert.False(RunnerSideBar.Shows(RunnerBarOwner.Barrier));
    }

    [Fact]
    public void The_flag_brings_the_secondary_bars_back()
    {
        Assert.False(RunnerSideBar.Shows(RunnerBarOwner.LeadHero, secondaryBars: true));
        foreach (var owner in new[] { RunnerBarOwner.Follower, RunnerBarOwner.Foe, RunnerBarOwner.Boss, RunnerBarOwner.Box, RunnerBarOwner.Barrier })
        {
            Assert.True(RunnerSideBar.Shows(owner, secondaryBars: true));
        }
    }

    [Fact]
    public void Squad_and_foe_owners_map_the_lead_and_the_boss()
    {
        Assert.Equal(RunnerBarOwner.LeadHero, RunnerSideBar.SquadOwner(true));
        Assert.Equal(RunnerBarOwner.Follower, RunnerSideBar.SquadOwner(false));
        Assert.Equal(RunnerBarOwner.Boss, RunnerSideBar.FoeOwner(true));
        Assert.Equal(RunnerBarOwner.Foe, RunnerSideBar.FoeOwner(false));
    }

    [Fact]
    public void The_view_asks_the_policy_before_adding_a_world_bar()
    {
        var root = Layouts.RepoRoot();
        var view = File.ReadAllText(Path.Combine(root, "Assets/Survival/Unity/Runner/RunnerLevelView.cs"));
        var barriers = File.ReadAllText(Path.Combine(root, "Assets/Survival/Unity/Runner/RunnerLevelView.Barriers.cs"));
        Assert.Contains("if (RunnerSideBar.Shows(RunnerSideBar.SquadOwner(m.IsHero)))", view);
        Assert.Contains("if (RunnerSideBar.Shows(RunnerSideBar.FoeOwner(f.Boss)))", view);
        Assert.Contains("if (RunnerSideBar.Shows(RunnerBarOwner.Barrier))", barriers);
    }
}
