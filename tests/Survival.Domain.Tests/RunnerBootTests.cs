using Survival.Domain.Campaign;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek: "the loading screen for the runner doesn't actually seem to work. it just sits there". The boot builds the pools
/// over many frames, reports real progress, and the run starts only when it is done; the launch plan is far smaller than the old 55 rigs.
/// </summary>
public sealed class RunnerBootTests
{
    [Fact]
    public void The_launch_plan_stays_within_the_full_plan_the_bound_and_the_squad_cap_on_every_level()
    {
        var c = Runner.Catalog();
        foreach (var level in c.Levels)
        {
            var set = RunnerPrewarm.Sets(level, c, 0.72f);
            var bound = RunnerPrewarm.Bound(level, c, 0.72f);
            Assert.True(set.LaunchShots > 0 && set.LaunchShots <= RunnerPrewarm.LaunchShotCap, $"L{level.Number} launch shots {set.LaunchShots}");
            Assert.True(set.FullShots >= set.LaunchShots, $"L{level.Number} full shots {set.FullShots}");
            var squad = 0;
            foreach (var e in set.Launch)
            {
                var key = RunnerPrewarm.KeyOf(e.Role, e.Roster, e.Scale);
                var full = set.Full.Single(x => RunnerPrewarm.KeyOf(x.Role, x.Roster, x.Scale) == key);
                var b = bound.Single(x => RunnerPrewarm.KeyOf(x.Role, x.Roster, x.Scale) == key);
                Assert.True(e.Count >= 1 && e.Count <= full.Count && e.Count <= b.Count, $"L{level.Number} {key}: launch {e.Count} <= full {full.Count} <= bound {b.Count}");
                squad += e.Role == RunnerPrewarmRole.Squad ? e.Count : 0;
            }

            Assert.True(squad <= RunnerPrewarm.SquadRigLimit, $"L{level.Number} squad rigs {squad} <= {RunnerPrewarm.SquadRigLimit}");
        }
    }

    [Fact]
    public void Level_one_launches_with_far_fewer_rigs_than_the_old_fifty_five()
    {
        var c = Runner.Catalog();
        var level = c.Levels.First();
        var set = RunnerPrewarm.Sets(level, c, 0.72f);
        var launch = RunnerPrewarm.Total(set.Launch);
        Assert.True(launch <= 40, $"L1 launch rigs {launch}");
        Assert.True(launch < RunnerPrewarm.Total(set.Full), "the margin is the part the result-screen top-up builds");
    }

    [Fact]
    public void Progress_moves_with_the_work_stays_under_one_until_finished_and_names_the_step()
    {
        var p = new RunnerBootProgress();
        Assert.Equal(0f, p.Fraction);
        p.Expect(10);
        p.Begin(RunnerBootStep.Rigs, 10);
        var last = 0f;
        for (var i = 0; i < 10; i++)
        {
            p.Did(1);
            Assert.True(p.Fraction >= last);
            last = p.Fraction;
        }

        Assert.True(p.Fraction < 1f, "never 100% before Finish");
        Assert.Equal(99, p.Percent);
        Assert.Equal("Mustering heroes and foes 10/10", p.Label());
        p.Finish();
        Assert.Equal(1f, p.Fraction);
        Assert.Equal(100, p.Percent);
        Assert.True(p.Finished);
    }

    [Fact]
    public void The_bar_gives_the_scene_load_the_first_tenth_and_the_boot_the_rest()
    {
        Assert.Equal(0f, RunnerBootProgress.Bar(0f, 0f));
        Assert.Equal(RunnerBootProgress.SceneShare, RunnerBootProgress.Bar(1f, 0f), 4);
        Assert.Equal(1f, RunnerBootProgress.Bar(1f, 1f), 4);
        Assert.True(RunnerBootProgress.Bar(1f, 0.5f) > RunnerBootProgress.Bar(1f, 0.25f));
        Assert.Equal(1f, RunnerBootProgress.Bar(5f, 5f), 4);
    }

    [Fact]
    public void The_boot_builds_while_under_budget_holds_for_the_minimum_and_gives_up_at_the_timeout()
    {
        Assert.True(RunnerBootProgress.HasFrameBudget(0));
        Assert.True(RunnerBootProgress.HasFrameBudget(RunnerBootProgress.FrameBudgetMs - 0.1));
        Assert.False(RunnerBootProgress.HasFrameBudget(RunnerBootProgress.FrameBudgetMs));
        Assert.Equal(RunnerBootVerdict.Work, RunnerBootProgress.Decide(0.2, false));
        Assert.Equal(RunnerBootVerdict.Hold, RunnerBootProgress.Decide(0.2, true));
        Assert.Equal(RunnerBootVerdict.Go, RunnerBootProgress.Decide(RunnerBootProgress.MinSeconds, true));
        Assert.Equal(RunnerBootVerdict.TimedOut, RunnerBootProgress.Decide(RunnerBootProgress.TimeoutSeconds, false));
        Assert.Equal(RunnerBootVerdict.Go, RunnerBootProgress.Decide(RunnerBootProgress.TimeoutSeconds, true));
        Assert.True(RunnerBootProgress.TimeoutSeconds < BattleTransition.ShowLimitSeconds, "the boot gives up before the loading screen's own self-clear");
    }
}
