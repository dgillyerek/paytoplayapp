using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>Update 5: enemies walk at the squad from the frame they appear; only the boss holds his arena.</summary>
public sealed class RunnerWalkOnSpawnTests
{
    [Fact]
    public void On_every_shipped_level_each_enemy_is_walking_on_its_first_frame_and_closes_on_the_next_only_the_boss_holds()
    {
        var c = Runner.Catalog();
        Assert.Equal(50, c.Levels.Count);
        foreach (var level in c.Levels)
        {
            var run = new RunnerRun(c, level, RunnerShipped.Squad(c, level));
            var firstZ = new Dictionary<int, float>();
            var checkedNext = new HashSet<int>();
            var seen = 0;
            while (!run.Over && run.Seconds < 300f)
            {
                run.SteerTo(Runner.AimLikeAPlayer(run));
                run.Step(1f / 30f);
                for (var i = 0; i < run.Foes.Count; i++)
                {
                    var f = run.Foes[i];
                    if (!firstZ.ContainsKey(f.Id))
                    {
                        firstZ[f.Id] = f.Z;
                        seen++;
                        if (f.Boss)
                        {
                            Assert.False(f.Walking, $"L{level.Number}: the boss holds his ground");
                        }
                        else
                        {
                            Assert.True(f.Walking, $"L{level.Number} foe {f.Id} ({f.Unit.Roster}) is walking the frame it appears");
                        }
                    }
                    else if (!f.Boss && checkedNext.Add(f.Id))
                    {
                        Assert.True(f.Z < firstZ[f.Id], $"L{level.Number} foe {f.Id} ({f.Unit.Roster}) moved toward the squad on its second frame");
                    }
                }
            }

            Assert.True(seen >= level.EnemyCount, $"L{level.Number}: saw {seen} of {level.EnemyCount}");
        }
    }

    [Fact]
    public void A_far_enemy_walks_too_there_is_no_aggro_range_to_wait_for()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 38, ""kind"": ""enemy"", ""x"": 0, ""unit"": ""brute"" }", length: 60f));
        while (run.Foes.Count == 0 && !run.Over)
        {
            run.Step(1f / 30f);
        }

        var f = run.Foes.Single();
        Assert.True(f.Walking);
        var z = f.Z;
        run.Step(1f / 30f);
        Assert.True(f.Z < z, "the Blightroot brute closes from where it spawned");
    }
}

/// <summary>Update 5: the loading-screen prewarm covers what the bots see at once, within the analytic bound.</summary>
public sealed class RunnerPrewarmPlanTests
{
    [Fact]
    public void Every_shipped_level_prewarms_at_least_the_bots_peak_of_each_kind_and_never_more_than_the_bound()
    {
        var c = Runner.Catalog();
        foreach (var level in c.Levels)
        {
            var plan = RunnerPrewarm.Plan(level, c, 0.72f, out var shots);
            var peaks = RunnerPrewarm.Peaks(level, c, 0.72f);
            var bound = RunnerPrewarm.Bound(level, c, 0.72f);
            Assert.True(shots > 0, $"L{level.Number} pools shots");
            Assert.True(shots >= peaks[RunnerPrewarm.ShotsKey], $"L{level.Number} shots {shots} >= peak {peaks[RunnerPrewarm.ShotsKey]}");
            foreach (var e in plan)
            {
                var key = RunnerPrewarm.KeyOf(e.Role, e.Roster, e.Scale);
                var b = bound.Single(x => RunnerPrewarm.KeyOf(x.Role, x.Roster, x.Scale) == key);
                Assert.True(e.Count <= b.Count, $"L{level.Number} {key}: {e.Count} <= bound {b.Count}");
                if (peaks.TryGetValue(key, out var peak))
                {
                    Assert.True(e.Count >= Math.Min(b.Count, peak), $"L{level.Number} {key}: {e.Count} covers peak {peak}");
                }
            }

            foreach (var p in peaks)
            {
                if (p.Key != RunnerPrewarm.ShotsKey)
                {
                    Assert.Contains(plan, e => RunnerPrewarm.KeyOf(e.Role, e.Roster, e.Scale) == p.Key);
                }
            }
        }
    }

    [Fact]
    public void Union_takes_the_larger_count_per_kind_and_keeps_kinds_only_one_side_has()
    {
        var a = new[] { new RunnerPrewarmEntry("Lyra", 1f, 3, RunnerPrewarmRole.Squad), new RunnerPrewarmEntry("Bonequill", 1.2f, 5, RunnerPrewarmRole.Foe) };
        var b = new[] { new RunnerPrewarmEntry("Bonequill", 1.2f, 8, RunnerPrewarmRole.Foe), new RunnerPrewarmEntry("Ironhowl", 1.5f, 1, RunnerPrewarmRole.Foe) };
        var u = RunnerPrewarm.Union(a, b);
        Assert.Equal(3, u.Count);
        Assert.Equal(3, u.Single(e => e.Roster == "Lyra").Count);
        Assert.Equal(8, u.Single(e => e.Roster == "Bonequill").Count);
        Assert.Equal(1, u.Single(e => e.Roster == "Ironhowl").Count);
    }
}
