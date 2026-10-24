using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// Update 8: the end boss's signature attack, the quake. A long telegraph (red road, DODGE!), every third action, lands on
/// all the road but a safe band that the squad can always reach in time.
/// </summary>
public sealed class RunnerBossQuakeTests
{
    private const float Dt = 1f / 30f;
    private const string BossRow = @"{ ""at"": 36, ""kind"": ""boss"", ""x"": 0, ""unit"": ""boss"" }";
    private readonly ITestOutputHelper _out;

    public RunnerBossQuakeTests(ITestOutputHelper output)
    {
        _out = output;
    }

    /// <summary>
    /// A player who sees the telegraph: after <paramref name="reaction"/> seconds of it he steers the squad to the quake's safe band
    /// (or out of the slam's road), then goes back to aiming like a player.
    /// </summary>
    public static float DodgingPlayer(RunnerRun run, float reaction = 0.3f)
    {
        var boss = run.Foes.FirstOrDefault(f => f.Boss && f.Hp > 0);
        if (boss != null && boss.Windup > 0f && boss.WindupTotal - boss.Windup >= reaction)
        {
            if (boss.Move == RunnerBossMove.Quake)
            {
                return Math.Sign(boss.QuakeSafeX) * 2.9f;
            }

            if (boss.Move == RunnerBossMove.Slam)
            {
                return run.Hero.X < 0f ? -2.9f : 2.9f;
            }
        }

        return Runner.AimLikeAPlayer(run);
    }

    private sealed record Fight(RunnerRun Run, List<RunnerEvent> Events, List<(float At, RunnerEventKind Kind)> Moves);

    /// <summary>Walks to the boss and fights until the run ends, steering with <paramref name="steer"/>.</summary>
    private static Fight Play(RunnerRun run, Func<RunnerRun, float> steer, float maxSeconds = 400f)
    {
        var events = new List<RunnerEvent>();
        var moves = new List<(float, RunnerEventKind)>();
        while (!run.Over && run.Seconds < maxSeconds)
        {
            run.SteerTo(steer(run));
            foreach (var e in run.Step(Dt))
            {
                events.Add(e);
                if (e.Kind is RunnerEventKind.BossEngaged or RunnerEventKind.BossWindup or RunnerEventKind.BossQuakeWindup or RunnerEventKind.BossSlam or RunnerEventKind.BossQuake)
                {
                    moves.Add((run.Seconds, e.Kind));
                }
            }
        }

        return new Fight(run, events, moves);
    }

    /// <summary>A boss with Ironhowl's real numbers (damage 40, cycle 2.4 s, slam reach) who is far too tough to kill, and a lead who can take a few hits.</summary>
    private static RunnerCatalog BossCatalog(string rows = "") => RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 5000, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""archer"": { ""roster"": ""Rowan"", ""hp"": 1, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""boss"": { ""roster"": ""Ironhowl"", ""hp"": 5000000, ""damage"": 40, ""cycle"": 2.4, ""range"": 1.8, ""radius"": 0.67 }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": 60, ""speed"": 8, ""half_width"": 4, ""max_followers"": 9,
    ""rewards"": { ""win_gold"": 100, ""gold_per_kill"": 5, ""gold_per_box"": 10, ""shards"": 3, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ " + rows + @" { ""at"": 56, ""kind"": ""boss"", ""x"": 0, ""unit"": ""boss"" } ] } ] }");

    private const string Boxes = @"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 5 }, { ""at"": 14, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 5 },";

    /// <summary>A full squad (lead + 8) walked up to the boss's arena line, the lead in lane <paramref name="x"/>, the boss just engaged.</summary>
    private static RunnerRun TinyFight(float x)
    {
        var run = Runner.Run(BossCatalog(Boxes));
        while (!run.Over && run.Distance < 18f)
        {
            run.SteerTo(0f);
            run.Step(Dt);
        }

        while (!run.Over && !run.Foes.Any(f => f.Boss && f.Engaged))
        {
            run.SteerTo(x);
            run.Step(Dt);
        }

        return run;
    }

    [Fact]
    public void The_quake_is_telegraphed_for_about_two_seconds_and_lands_exactly_then()
    {
        var f = Play(Runner.Run(Runner.Tiny(BossRow, maxFollowers: 6)), _ => 0f, 30f);
        var wind = f.Events.First(e => e.Kind == RunnerEventKind.BossQuakeWindup);
        Assert.InRange(wind.Amount, 1.6f, 2.0f);
        Assert.Equal(RunnerRules.BossQuakeWindup, wind.Amount, 3);
        Assert.True(wind.Amount > RunnerRules.BossWindup * 1.7f, "much longer than the slam's warning");

        var start = f.Moves.First(m => m.Kind == RunnerEventKind.BossQuakeWindup).At;
        var land = f.Moves.First(m => m.Kind == RunnerEventKind.BossQuake).At;
        Assert.Equal(RunnerRules.BossQuakeWindup, land - start, 1);
        Assert.InRange(land - start, 1.6f, 2.0f);
    }

    [Fact]
    public void The_quake_comes_every_third_action_never_back_to_back_with_the_slam_and_about_every_twelve_seconds()
    {
        // A tough boss that stands there so the fight runs long.
        var f = Play(Runner.Run(BossCatalog()), _ => 3.9f, 80f);

        var windups = f.Moves.Where(m => m.Kind is RunnerEventKind.BossWindup or RunnerEventKind.BossQuakeWindup).ToList();
        var lands = f.Moves.Where(m => m.Kind is RunnerEventKind.BossSlam or RunnerEventKind.BossQuake).ToList();
        Assert.True(windups.Count >= 7, "enough actions: " + windups.Count);
        for (var i = 0; i < windups.Count; i++)
        {
            var isQuake = windups[i].Kind == RunnerEventKind.BossQuakeWindup;
            Assert.Equal(i % RunnerRules.BossQuakeEvery == RunnerRules.BossQuakeSlot, isQuake);
        }

        // One at a time, and a real pause between the end of one and the start of the next (more after a quake).
        for (var i = 1; i < windups.Count; i++)
        {
            var gap = windups[i].At - lands[i - 1].At;
            var afterQuake = windups[i - 1].Kind == RunnerEventKind.BossQuakeWindup;
            Assert.True(lands[i - 1].At <= windups[i].At + 1e-3f, "no overlap");
            Assert.True(gap >= (afterQuake ? 3.0f : 2.0f), $"gap {gap:0.00} before action {i}");
        }

        var quakes = windups.Where(w => w.Kind == RunnerEventKind.BossQuakeWindup).Select(w => w.At).ToList();
        Assert.True(quakes.Count >= 2);
        for (var i = 1; i < quakes.Count; i++)
        {
            Assert.InRange(quakes[i] - quakes[i - 1], 10f, 12.5f);
        }

        _out.WriteLine("quakes at " + string.Join(", ", quakes.Select(q => q.ToString("0.0"))));
    }

    [Fact]
    public void The_safe_band_is_a_clear_wide_lane_on_the_side_away_from_the_lead()
    {
        Assert.True(RunnerRules.BossQuakeSafeHalf * 2f >= 4f, "wide enough for a whole squad");
        // 4.2 m of an 8 m road: a bit over half of it is safe, the rest is marked.
        Assert.InRange(RunnerRules.BossQuakeSafeHalf * 2f / 8f, 0.45f, 0.6f);
        Assert.Equal(-2.4f, RunnerRun.QuakeSafeCentre(2f, 0, 4f), 3);
        Assert.Equal(2.4f, RunnerRun.QuakeSafeCentre(-2f, 0, 4f), 3);
        Assert.Equal(-2.4f, RunnerRun.QuakeSafeCentre(0f, 0, 4f), 3);
        Assert.Equal(2.4f, RunnerRun.QuakeSafeCentre(0f, 1, 4f), 3);

        // The hero's own lane (anywhere he stood when it started) is never the safe one, except right at the edge of it.
        for (var x = -3.5f; x <= 3.5f; x += 0.25f)
        {
            var c = RunnerRun.QuakeSafeCentre(x, 0, 4f);
            Assert.True(Math.Abs(x - c) > RunnerRules.BossQuakeSafeHalf - 0.45f || Math.Abs(x) < 0.4f, $"x {x}");
        }
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.3f)]
    [InlineData(0.5f)]
    public void From_any_lane_the_whole_squad_reaches_the_safe_band_inside_the_telegraph(float reaction)
    {
        // Every starting lane for the lead, a full squad of 9 behind him, and a player who only reacts after `reaction` seconds.
        for (var x = -3.5f; x <= 3.51f; x += 0.5f)
        {
            var run = TinyFight(x);
            var boss = run.Foes.Single(b => b.Boss);
            var lost = 0;
            var heroHits = 0;
            var quakeSeen = false;
            var arrived = -1f;
            var guard = 0;
            while (!run.Over && guard++ < 3000 && !(quakeSeen && boss.Windup <= 0f))
            {
                run.SteerTo(DodgingPlayer(run, reaction));
                if (boss.Move == RunnerBossMove.Quake && boss.Windup > 0f)
                {
                    var elapsed = boss.WindupTotal - boss.Windup;
                    var safe = RunnerRun.QuakeSafe(boss);
                    var inside = run.Squad.All(m => !RunnerRun.QuakeCatches(boss, m.X, m.Z, m.Unit.Radius));
                    if (inside && arrived < 0f && elapsed >= reaction)
                    {
                        arrived = elapsed;
                    }

                    Assert.True(Math.Abs(boss.QuakeSafeX - RunnerRun.QuakeSafeCentre(boss.QuakeSafeX < 0f ? 1f : -1f, 0, 4f)) < 3f);
                    Assert.True(safe.MaxX - safe.MinX > 4f);
                }

                foreach (var e in run.Step(Dt))
                {
                    lost += e.Kind == RunnerEventKind.FollowerLost ? 1 : 0;
                    heroHits += e.Kind == RunnerEventKind.HeroHit && boss.Quakes > 0 && e.Amount > 100 ? 1 : 0;
                    quakeSeen |= e.Kind == RunnerEventKind.BossQuake;
                }
            }

            Assert.True(quakeSeen, $"x {x}: the quake landed");
            Assert.Equal(0, heroHits);
            Assert.True(arrived >= 0f, $"x {x}: everyone in the band before it landed");
            Assert.True(arrived <= RunnerRules.BossQuakeWindup - 0.35f, $"x {x}: in at {arrived:0.00}s of {RunnerRules.BossQuakeWindup}s");
            _out.WriteLine($"reaction {reaction:0.0}s lead at {x,4:0.0}: whole squad safe {arrived:0.00}s into the {RunnerRules.BossQuakeWindup}s warning");
        }
    }

    [Fact]
    public void The_slowest_crossing_fits_the_steer_budget_with_room_to_spare()
    {
        var worst = 0f;
        for (var x = -3.5f; x <= 3.51f; x += 0.5f)
        {
            var run = TinyFight(x);
            var boss = run.Foes.Single(b => b.Boss);
            while (!run.Over && boss.Move != RunnerBossMove.Quake)
            {
                run.SteerTo(DodgingPlayer(run, 0f));
                run.Step(Dt);
            }

            var t = 0f;
            while (!run.Over && boss.Move == RunnerBossMove.Quake && run.Squad.Any(m => RunnerRun.QuakeCatches(boss, m.X, m.Z, m.Unit.Radius)))
            {
                run.SteerTo(Math.Sign(boss.QuakeSafeX) * 2.9f);
                run.Step(Dt);
                t += Dt;
            }

            worst = Math.Max(worst, t);
        }

        _out.WriteLine($"slowest full-squad crossing {worst:0.00}s");
        Assert.True(worst <= RunnerRules.BossQuakeSteerSeconds, $"{worst:0.00}s");
        Assert.True(worst + 0.4f <= RunnerRules.BossQuakeWindup, "a 0.4 s reaction still fits");
    }

    [Fact]
    public void Standing_in_the_marked_road_when_it_lands_costs_the_followers_and_a_quarter_of_the_leads_health()
    {
        var run = TinyFight(3f);
        var boss = run.Foes.Single(b => b.Boss);
        var squadBefore = run.Followers;
        Assert.True(squadBefore >= 5, "a squad to lose: " + squadBefore);
        while (!run.Over && boss.Move != RunnerBossMove.Quake)
        {
            run.SteerTo(3f);
            run.Step(Dt);
        }

        // The hero is on the right; the band is on the other side. Hold still through the warning.
        var hpBefore = run.Hero.Hp;
        var events = new List<RunnerEvent>();
        while (!run.Over && !events.Any(e => e.Kind == RunnerEventKind.BossQuake))
        {
            run.SteerTo(3f);
            events.AddRange(run.Step(Dt));
        }

        var quake = events.First(e => e.Kind == RunnerEventKind.BossQuake);
        var hit = events.First(e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(boss.Unit.Damage * RunnerRules.BossQuakeDamage, hit.Amount, 1);
        Assert.Equal(hpBefore - (int)hit.Amount, run.Hero.Hp);
        var shipped = Runner.Catalog();
        var lead = new RunnerRun(shipped, shipped.First).Hero.Unit.Hp;
        Assert.True(hit.Amount >= lead * 0.2f, "heavy for the shipped lead: " + hit.Amount + " of " + lead);
        Assert.True(hit.Amount > boss.Unit.Damage * 2f, "more than twice a slam");
        Assert.True(quake.Amount >= 2f, "caught the hero and followers: " + quake.Amount);
        var lost = events.Count(e => e.Kind == RunnerEventKind.FollowerLost);
        Assert.Equal(quake.Amount - 1f, lost);
        Assert.True(lost >= 3, "followers fell: " + lost);
        _out.WriteLine($"standing still: {lost} followers lost of {squadBefore}, hero -{hit.Amount} ({run.Hero.Hp}/{run.Hero.Unit.Hp})");
    }

    [Fact]
    public void Dodging_the_quake_loses_nobody_and_the_lead_takes_nothing_from_it()
    {
        var run = TinyFight(0f);
        var boss = run.Foes.Single(b => b.Boss);
        while (!run.Over && boss.Move != RunnerBossMove.Quake)
        {
            run.SteerTo(DodgingPlayer(run, 0.3f));
            run.Step(Dt);
        }

        var followers = run.Followers;
        var hp = run.Hero.Hp;
        var events = new List<RunnerEvent>();
        while (!run.Over && !events.Any(e => e.Kind == RunnerEventKind.BossQuake))
        {
            run.SteerTo(DodgingPlayer(run, 0.3f));
            events.AddRange(run.Step(Dt));
        }

        Assert.Equal(0f, events.First(e => e.Kind == RunnerEventKind.BossQuake).Amount);
        Assert.DoesNotContain(events, e => e.Kind == RunnerEventKind.HeroHit);
        Assert.DoesNotContain(events, e => e.Kind == RunnerEventKind.FollowerLost);
        Assert.Equal(followers, run.Followers);
        Assert.Equal(hp, run.Hero.Hp);
    }

    [Fact]
    public void A_squad_nowhere_near_the_boss_is_not_quaked_and_the_area_is_the_road_in_front_of_him()
    {
        var run = TinyFight(0f);
        var boss = run.Foes.Single(b => b.Boss);
        var (minZ, maxZ) = RunnerRun.QuakeArea(boss);
        Assert.Equal(boss.Z, maxZ, 3);
        Assert.Equal(RunnerRules.BossQuakeDepth, maxZ - minZ, 3);
        Assert.False(RunnerRun.QuakeCatches(boss, 0f, minZ - 2f, 0.5f));
        boss.QuakeSafeX = -2.4f;
        Assert.True(RunnerRun.QuakeCatches(boss, 0f, boss.Z - 3f, 0.5f), "the middle of the road is in it (the band is to a side)");
        Assert.False(RunnerRun.QuakeCatches(boss, -2.5f, boss.Z - 3f, 0.5f));
        Assert.True(RunnerRun.QuakeCatches(boss, 2.5f, boss.Z - 3f, 0.5f));
        Assert.True(RunnerRun.QuakeCatches(boss, -0.1f, boss.Z - 3f, 0.5f));
    }

    [Fact]
    public void Every_shipped_level_a_dodging_player_wins_and_one_who_never_dodges_takes_much_more()
    {
        var c = Runner.Catalog();
        float sumDodge = 0f, sumStill = 0f;
        var stillLost = 0;
        var problems = new List<string>();
        foreach (var level in c.Levels)
        {
            var dodge = Play(new RunnerRun(c, level, RunnerShipped.Squad(c, level)), r => DodgingPlayer(r, 0.3f));
            var still = Play(new RunnerRun(c, level, RunnerShipped.Squad(c, level)), Runner.AimLikeAPlayer);
            var dq = dodge.Moves.Count(m => m.Kind == RunnerEventKind.BossQuake);
            var sq = still.Moves.Count(m => m.Kind == RunnerEventKind.BossQuake);
            var dHp = dodge.Run.Hero.HpFraction;
            var sHp = still.Run.Hero.HpFraction;
            var dLost = dodge.Events.Count(e => e.Kind == RunnerEventKind.FollowerLost && e.Amount >= 0);
            var sLost = still.Events.Count(e => e.Kind == RunnerEventKind.FollowerLost);
            var dFight = dodge.Run.Seconds - dodge.Moves.First(m => m.Kind == RunnerEventKind.BossEngaged).At;
            var sFight = still.Run.Seconds - still.Moves.First(m => m.Kind == RunnerEventKind.BossEngaged).At;
            _out.WriteLine($"L{level.Number}: dodge {dodge.Run.Outcome} fight {dFight:0.0}s quakes {dq} hero {dHp:P0} lost {dLost} | still {still.Run.Outcome} fight {sFight:0.0}s quakes {sq} hero {sHp:P0} lost {sLost}");
            if (dodge.Run.Outcome != RunnerOutcome.Won || dq < 1)
            {
                problems.Add($"L{level.Number}: {dodge.Run.Outcome}, {dq} quakes");
            }

            if (still.Run.Outcome == RunnerOutcome.Won && sHp > 0.8f)
            {
                problems.Add($"L{level.Number}: not dodging cost only {1f - sHp:P0}");
            }

            sumDodge += 1f - dHp;
            sumStill += 1f - sHp;
            stillLost += still.Run.Outcome == RunnerOutcome.Lost ? 1 : 0;
        }

        _out.WriteLine($"damage taken (sum of hero HP lost, in leads): dodging {sumDodge:0.00}, not dodging {sumStill:0.00}; not dodging lost {stillLost} levels");
        Assert.True(problems.Count == 0, "a dodging player wins every level and meets the quake on each: " + string.Join("; ", problems));
        Assert.True(sumStill >= sumDodge + 2f || stillLost >= 2, "not dodging costs a lot");
    }
}
