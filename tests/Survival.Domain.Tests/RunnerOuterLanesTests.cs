using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Outer lanes and side entries (runner_levels.json progression.spread.lanes, RunnerLanesDef; Derek 2026-11-20: "more
/// enemies need to come from the outside of the lanes and stay there. a lot of times you can just stay in the center without
/// moving. the same for powerups"; "the wolves for example move to the center too quickly"; "some enemies can spawn in from
/// the side ... they should come in with enough time to kill").
/// </summary>
public sealed class RunnerOuterLanesTests
{
    /// <summary>
    /// The player Derek described: parks the squad in the middle of the road and only leaves it to break a chest or a crate
    /// (it steers for those like <see cref="Runner.AimLikeAPlayer"/>, and still sidesteps traps and the boss's
    /// telegraphed slams), but never moves to line up on an enemy or out of an arrow's way.
    /// </summary>
    public static float CentreParker(RunnerRun run, float reaction = 0.3f)
    {
        var boss = run.Foes.FirstOrDefault(f => f.Boss && f.Hp > 0);
        if (boss != null && ((boss.Windup > 0f && boss.WindupTotal - boss.Windup >= reaction) || boss.SlamSwinging))
        {
            return RunnerBossQuakeTests.DodgingPlayer(run, reaction);
        }

        RunnerBox? box = null;
        foreach (var b in run.Boxes)
        {
            if (b.Z > run.Hero.Z && b.Z - run.Hero.Z < 22f && (box == null || b.Z < box.Z))
            {
                box = b;
            }
        }

        var steer = Runner.Dodge(run, box?.X ?? 0f);
        return RunnerBossQuakeTests.RockDodge(run, reaction, steer) ?? steer;
    }

    private static RunnerCatalog C => Tests.Runner.Catalog();

    private static RunnerLanesDef Lanes => C.Progression!.Spread!.Lanes!;

    /// <summary>The stream's enemies (not the boss guard or the finale) of a level, one entry per enemy.</summary>
    private static List<RunnerRowDef> Stream(RunnerLevelDef level) =>
        level.Rows.Where(r => r.Kind == RunnerRowKind.Enemy && !r.Finale).SelectMany(r => Enumerable.Repeat(r, Math.Max(1, r.Count))).ToList();

    [Fact]
    public void A_real_share_of_every_levels_enemies_hold_an_outer_lane()
    {
        var off = new List<string>();
        int all = 0, side = 0;
        for (var n = 1; n <= C.Levels.Count; n++)
        {
            var level = C.ByNumber(n);
            var stream = Stream(level);
            var outer = stream.Where(r => r.Seek == 0f).ToList();
            all += stream.Count;
            side += outer.Count(r => r.EnterX != 0f);
            var share = outer.Count / (double)stream.Count;
            if (share < 0.33 || share > 0.5 || outer.Any(r => Math.Abs(r.X) + (r.Count > 1 ? (r.Count - 1) * r.Spread * 0.5f : 0f) < 2.4f))
            {
                off.Add($"L{n}: {outer.Count}/{stream.Count} on the outer lanes");
            }
        }

        Assert.True(off.Count == 0, "Each level's stream should put 35-50% of its enemies on an outer lane:\n" + string.Join("\n", off));
        var sideShare = side / (double)all;
        Assert.InRange(sideShare, 0.08, 0.2);
    }

    [Fact]
    public void A_share_of_the_power_up_crates_sit_on_the_outer_lanes()
    {
        int crates = 0, outer = 0;
        for (var n = 1; n <= C.Levels.Count; n++)
        {
            foreach (var r in C.ByNumber(n).Rows.Where(r => r.Kind == RunnerRowKind.Box && r.Box is { GivesUnits: false }))
            {
                crates++;
                outer += Math.Abs(r.X) >= Lanes.CrateX - 0.05f ? 1 : 0;
            }
        }

        Assert.InRange(outer / (double)crates, 0.22, 0.45);
    }

    [Fact]
    public void Inner_wolves_drift_toward_the_hero_slowly_and_outer_enemies_not_at_all()
    {
        Assert.True(Lanes.Seek.TryGetValue("nightfang", out var wolf) && wolf < RunnerRules.EnemySeek * 0.5f, "the wolves cut to the centre too fast (Derek 2026-11-20)");
        var level = C.ByNumber(12);
        Assert.Contains(level.Rows, r => r.Unit == "nightfang" && r.Seek == wolf);
        Assert.All(level.Rows.Where(r => r.Kind == RunnerRowKind.Enemy && Math.Abs(r.X) >= 2.4f && !r.Finale && r.Seek >= 0f && r.Seek != wolf), r => Assert.Equal(0f, r.Seek));
    }

    /// <summary>An outer-lane enemy walks its lane down to the squad: its own line never moves toward the hero.</summary>
    [Theory]
    [InlineData(6)]
    [InlineData(18)]
    [InlineData(33)]
    public void Outer_lane_enemies_keep_their_lane(int number)
    {
        var level = C.ByNumber(number);
        var run = new RunnerRun(C, level, RunnerBalance.Uniform(C, 3.0));
        var lane = new Dictionary<int, float>();
        var moved = new List<string>();
        while (!run.Over && run.Seconds < 120f)
        {
            run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
            run.Step(1f / 30f);
            foreach (var f in run.Foes.Where(f => f.Seek == 0f && !f.Entering && !f.Passed && f.Hp > 0))
            {
                if (!lane.TryGetValue(f.Id, out var x0))
                {
                    lane[f.Id] = f.LaneX;
                }
                else if (Math.Abs(f.LaneX - x0) > 1e-3f)
                {
                    moved.Add($"foe {f.Id} lane {x0:0.00} -> {f.LaneX:0.00}");
                }
            }
        }

        Assert.NotEmpty(lane);
        Assert.Empty(moved);
    }

    /// <summary>
    /// Derek 2026-11-22: "all of the characters that 'come from the side' come at the end of the lane where all character
    /// spawn. I want some random to come from the sides up to 2/3rds of the way down the lane". Some still walk on at the far
    /// end; the rest at a random depth, none deeper than two thirds of the way from the far end toward the squad.
    /// </summary>
    [Fact]
    public void Side_entries_walk_on_at_random_depths_up_to_two_thirds_down_the_lane()
    {
        int far = 0, first = 0, middle = 0, all = 0;
        var deepest = 0f;
        for (var n = 1; n <= C.Levels.Count; n++)
        {
            foreach (var r in C.ByNumber(n).Rows.Where(r => r.Kind == RunnerRowKind.Enemy && r.EnterX != 0f))
            {
                var gap = Math.Min(r.At, RunnerRules.SpawnAhead);
                var depth = r.EnterAhead > 0f ? 1f - (r.EnterAhead / gap) : 0f;
                Assert.InRange(depth, 0f, Lanes.SideDepth + 0.03f); // rows nudged after the roll land up to ~0.69
                deepest = Math.Max(deepest, depth);
                all++;
                far += depth == 0f ? 1 : 0;
                first += depth > 0f && depth < 1f / 3f ? 1 : 0;
                middle += depth >= 1f / 3f ? 1 : 0;
            }
        }

        Assert.InRange(far / (double)all, 0.15, 0.5);
        Assert.InRange(middle / (double)all, 0.2, 0.6);
        Assert.True(first > 0, "none in the first third past the far end");
        Assert.True(deepest > 0.6f, $"deepest entry only {deepest:0.00} of the way down");
    }

    /// <summary>Every side entry's first spot (on the grass beside the road) is inside the 9:16 shot, not off the screen edge.</summary>
    [Fact]
    public void Side_entries_walk_on_inside_the_shot()
    {
        var off = new List<string>();
        for (var n = 1; n <= C.Levels.Count; n++)
        {
            foreach (var r in C.ByNumber(n).Rows.Where(r => r.Kind == RunnerRowKind.Enemy && r.EnterX != 0f))
            {
                // Measured from the lead rather than the camera's focus (which rides behind it): a nearer, narrower view.
                var ahead = r.EnterAhead > 0f ? r.EnterAhead : Math.Min(r.At, RunnerRules.SpawnAhead);
                var sx = RunnerFraming.ScreenX(r.EnterX, ahead);
                if (Math.Abs(sx) > 0.97f)
                {
                    off.Add($"L{n} at {r.At}: x {r.EnterX} {ahead:0.0} m ahead -> screen {sx:0.00}");
                }
            }
        }

        Assert.True(off.Count == 0, string.Join("\n", off.Take(10)));
        Assert.InRange(RunnerFraming.ScreenX(0f, 10f), -0.2f, 0.2f);
        Assert.True(RunnerFraming.ScreenX(-8f, 10f) < -1f && RunnerFraming.ScreenX(8f, 10f) > 1f);
    }

    /// <summary>
    /// A side entry walks in from beyond the road edge and is on the road at least <see cref="RunnerLanesDef.SideMinSeconds"/>
    /// before it reaches the squad (Derek 2026-11-20: "they should come in with enough time to kill"). Archers and casters
    /// among them start shooting from range about 2 s after they are on the road, like any ranged enemy; they do not attack
    /// while still walking in.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(12)]
    [InlineData(25)]
    [InlineData(31)]
    [InlineData(46)]
    public void Side_entries_walk_in_with_time_to_kill_them(int number)
    {
        var level = C.ByNumber(number);
        var run = new RunnerRun(C, level, RunnerBalance.Uniform(C, 64.0));
        var onRoad = new Dictionary<int, float>();
        var entering = new HashSet<int>();
        var margins = new List<float>();
        var done = new HashSet<int>();
        while (!run.Over && run.Seconds < 600f)
        {
            run.SteerTo(0f);
            run.Step(1f / 30f);
            foreach (var f in run.Foes)
            {
                if (f.Entering)
                {
                    Assert.True(Math.Abs(f.X) > level.HalfWidth - f.Unit.Radius - 0.05f || Math.Abs(f.X - f.LaneX) < 3f);
                    entering.Add(f.Id);
                    continue;
                }

                if (entering.Contains(f.Id) && !onRoad.ContainsKey(f.Id))
                {
                    onRoad[f.Id] = run.Seconds;
                }

                if (onRoad.TryGetValue(f.Id, out var t0) && !done.Contains(f.Id) && (f.Engaged || f.Z - run.Hero.Z <= RunnerLanesDef.ContactMetres))
                {
                    margins.Add(run.Seconds - t0);
                    done.Add(f.Id);
                }
            }
        }

        Assert.NotEmpty(entering);
        Assert.NotEmpty(margins);
        Assert.True(margins.Min() >= Lanes.SideMinSeconds - 0.2f, $"a side entry reached the squad {margins.Min():0.0} s after it came onto the road");
    }

    /// <summary>
    /// Derek 2026-11-20: "a lot of times you can just stay in the center without moving". With the squad strength the free
    /// pace needs on each level (the moving sim player's required power, x1.15), a player who parks in the middle and only
    /// leaves it for chests and crates loses most levels the moving player wins.
    /// </summary>
    [Fact]
    public void Parking_in_the_centre_loses_levels_that_moving_wins()
    {
        var r = BalanceReport.Get();
        int movingWins = 0, parkerWins = 0;
        var parked = new List<int>();
        Parallel.For(1, C.Levels.Count + 1, new ParallelOptions { MaxDegreeOfParallelism = 4 }, n =>
        {
            var level = C.ByNumber(n);
            var s = r.RequiredScale[n] * 1.15;
            var m = Play(level, s, run => RunnerBossQuakeTests.DodgingPlayer(run));
            var p = Play(level, s, run => CentreParker(run));
            lock (parked)
            {
                movingWins += m ? 1 : 0;
                parkerWins += m && p ? 1 : 0;
                if (m && p)
                {
                    parked.Add(n);
                }
            }
        });

        Assert.True(movingWins >= 40, $"moving player won only {movingWins}/50 at 1.15x the required power");
        // Sim, Nov 20 2026 (c4): the parker wins 13 of 48 at 1.15x, none before L29 (on main #231 it matched the mover on 18
        // levels at 1.0x); at the mover's own required power it wins 2/50 and needs median 1.79x its power. Nov 22 (side
        // entries down the lane): 11 at 1.15x, the first L26; 2/50 at 1.0x, median 1.83x.
        Assert.True(parkerWins <= movingWins / 3, $"parking in the centre still wins {parkerWins} of the {movingWins} levels moving wins: L{string.Join(", L", parked.OrderBy(x => x))}");
        Assert.True(parked.All(n => n > 25), $"parking in the centre wins an early level: L{string.Join(", L", parked.OrderBy(x => x))}");
    }

    private static bool Play(RunnerLevelDef level, double s, Func<RunnerRun, float> steer)
    {
        var run = new RunnerRun(C, level, RunnerBalance.Uniform(C, s));
        while (!run.Over && run.Seconds < 600f)
        {
            run.SteerTo(steer(run));
            run.Step(1f / 30f);
        }

        return run.Outcome == RunnerOutcome.Won;
    }
}
