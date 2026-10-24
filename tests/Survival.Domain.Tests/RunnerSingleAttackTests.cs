using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek: "do the heros have double fire? Oakenshield looks like she throws and has another attack". The rule: one attack
/// animation and one shot per fire, in sync, and in the runner only the hero's single main projectile attack.
/// </summary>
public sealed class RunnerSingleAttackTests
{
    private const float Dt = 1f / 30f;
    private static readonly string[] RangedHeroes = { "Lyra", "Oakenshield", "Rowan" };

    private static string RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Survival")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, relative));
    }

    [Fact]
    public void Every_take_fires_exactly_one_shot_at_its_release_and_never_twice()
    {
        var c = Runner.Catalog();
        foreach (var level in c.Levels.Take(4))
        {
            var run = new RunnerRun(c, level);
            var started = new Dictionary<int, int>();
            var fired = new Dictionary<int, int>();
            var shotIds = new HashSet<int>();
            var open = new HashSet<int>();
            while (!run.Over && run.Seconds < 400f)
            {
                run.SteerTo(Runner.AimLikeAPlayer(run));
                foreach (var e in run.Step(Dt))
                {
                    if (e.Kind == RunnerEventKind.AttackStarted)
                    {
                        Assert.False(open.Contains(e.Id), $"L{level.Number} member {e.Id}: a take started while its last take had not fired");
                        open.Add(e.Id);
                        started[e.Id] = started.GetValueOrDefault(e.Id) + 1;
                    }
                    else if (e.Kind == RunnerEventKind.Fired && run.Member(e.Id) is { } m)
                    {
                        Assert.True(open.Remove(e.Id), $"L{level.Number} member {e.Id} ({m.Unit.Roster}): fired with no take open (a second attack for one take)");
                        fired[e.Id] = fired.GetValueOrDefault(e.Id) + 1;
                        if (m.Unit.Ranged)
                        {
                            Assert.True(e.Amount > 0f, "a ranged fire carries its shot");
                            Assert.True(shotIds.Add((int)e.Amount), "one shot per fire: shot ids are never shared between fires");
                        }
                    }
                }
            }

            Assert.True(fired.Count > 0, $"L{level.Number}: somebody fired");
            foreach (var (id, n) in fired)
            {
                Assert.InRange(started[id] - n, 0, 1);
            }
        }
    }

    [Theory]
    [InlineData("Lyra")]
    [InlineData("Oakenshield")]
    [InlineData("Rowan")]
    public void Runner_hero_plays_only_its_main_attack_and_the_shot_leaves_at_that_clips_release(string roster)
    {
        var clip = RunnerAttackTiming.ClipFor(roster);
        Assert.NotNull(clip);
        var spec = roster switch
        {
            "Lyra" => LyraAttack.Spec,
            "Oakenshield" => OakenshieldAttack.Spec,
            _ => RowanAttack.Spec
        };
        var fighter = roster switch
        {
            "Lyra" => LockedBattleRoster.Lyra,
            "Oakenshield" => LockedBattleRoster.Oakenshield,
            _ => LockedBattleRoster.Rowan
        };

        // The rotation's extras (thrust, snare, sweep, nova, double shot, volley) are the battle's. The runner plays the main take only.
        Assert.Equal(fighter.Attacks[0].PoseName, fighter.AttackPoseName);
        Assert.Equal(BlenderRigAttackSpec.PoseName, fighter.AttackPoseName);
        Assert.Equal(spec.ClipSecondsFor, clip!.Value.ClipSeconds, 3);
        Assert.Equal(spec.ReleaseFrame / (float)BlenderRigAttackSpec.FrameRate, clip.Value.ReleaseSeconds, 3);
    }

    [Fact]
    public void Runner_rigs_do_not_draw_their_own_projectile_flight_on_top_of_the_sim_shot()
    {
        // Confirmed in the specs: each of these rigs carries a Projectile track that starts flying at the release frame, which is
        // the same instant the sim spawns its shot, so unless the runner switches it off there are two flights per fire.
        foreach (var spec in new[] { LyraAttack.Spec, OakenshieldAttack.Spec, RowanAttack.Spec })
        {
            Assert.Contains(spec.Tracks, t => t.Kind == AttackTrackKind.Projectile);
        }

        var driver = RepoFile("Assets/Survival/Unity/BlenderRigAttackDriver.cs");
        Assert.Contains("ProjectilesEnabled { get; set; } = true;", driver);
        Assert.Contains("track.Kind == AttackTrackKind.Projectile && !ProjectilesEnabled", driver);

        var rig = RepoFile("Assets/Survival/Unity/Runner/RunnerRig.cs");
        foreach (var actor in new[] { "Lyra", "Oakenshield", "Rowan" })
        {
            var idx = rig.IndexOf("AddComponent<" + actor + "Actor>", StringComparison.Ordinal);
            Assert.True(idx >= 0, actor + " actor is built in RunnerRig");
            Assert.Contains("OwnShotsOff(a.AttackDriver)", rig.Substring(idx, 700));
        }
    }
}
