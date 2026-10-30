using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// Update 7: enemies walk 1.5x faster, the squad is capped at 10, melee-only heroes (Sir Aldric) are off the runner,
/// the end boss only takes shots that overlap him and visibly attacks with a telegraphed slam.
/// </summary>
public sealed class RunnerCapMeleeBossTests
{
    private readonly ITestOutputHelper _out;

    public RunnerCapMeleeBossTests(ITestOutputHelper output)
    {
        _out = output;
    }

    private const string BossRow = @"{ ""at"": 36, ""kind"": ""boss"", ""x"": 0, ""unit"": ""boss"" }";

    /// <summary>Walks to the arena line standing at <paramref name="x"/> and fights there for <paramref name="seconds"/> (or until the run is over).</summary>
    private static (RunnerRun Run, List<RunnerEvent> Events) FightAt(float x, float seconds, string rows = BossRow, int maxFollowers = 6)
    {
        var run = Runner.Run(Runner.Tiny(rows, maxFollowers: maxFollowers));
        var events = new List<RunnerEvent>();
        var engaged = -1f;
        while (!run.Over && run.Seconds < 120f && (engaged < 0f || run.Seconds - engaged < seconds))
        {
            run.SteerTo(x);
            var step = run.Step(1f / 30f);
            events.AddRange(step);
            if (engaged < 0f && step.Any(e => e.Kind == RunnerEventKind.BossEngaged))
            {
                engaged = run.Seconds;
            }
        }

        return (run, events);
    }

    [Fact]
    public void Enemies_walk_one_and_a_half_times_faster_big_ones_stay_slower_and_the_boss_holds()
    {
        var c = Runner.Catalog();
        var old = new Dictionary<string, float> { ["bonequill"] = 0.9f, ["nightfang"] = 2.2f, ["blightroot"] = 0.6f, ["vespera"] = 0.8f, ["ashwyrm"] = 1.2f };
        foreach (var pair in old)
        {
            // 1.5x the original walk, then another ~20-25% on top (Oct 23 evening pass).
            Assert.InRange(c.Unit(pair.Key).Speed / (pair.Value * 1.5f), 1.19f, 1.26f);
        }

        var small = new[] { "bonequill", "nightfang", "vespera" };
        Assert.All(small, id => Assert.True(c.Unit("blightroot").Speed < c.Unit(id).Speed, id + " walks faster than Blightroot"));
        Assert.True(c.Unit("blightroot").Speed < c.Unit("ashwyrm").Speed);
        Assert.Equal(0f, c.Unit("ironhowl").Speed);
        Assert.True(c.Unit("ironhowl").Scale >= c.Unit("blightroot").Scale);
    }

    [Fact]
    public void A_walking_enemy_covers_ground_one_and_a_half_times_as_fast_in_the_sim()
    {
        static float Closed(string unit)
        {
            var c = Runner.Catalog();
            var level = c.First;
            var json = @"{ ""at"": 38, ""kind"": ""enemy"", ""x"": 0, ""unit"": """ + unit + @""" }";
            var tiny = Runner.Tiny(json, length: 80f);
            var run = new RunnerRun(tiny, tiny.First);
            while (run.Foes.Count == 0 && !run.Over)
            {
                run.Step(1f / 30f);
            }

            var foe = run.Foes.Single();
            var z0 = foe.Z;
            var t0 = run.Seconds;
            run.SteerTo(3.5f);
            while (run.Seconds - t0 < 0.5f && !run.Over)
            {
                run.Step(1f / 30f);
            }

            Assert.NotNull(level);
            return (z0 - foe.Z) / (run.Seconds - t0);
        }

        // The Tiny catalog's own wolf walks at 2.0; the shipped speeds are asserted in the test above.
        Assert.InRange(Closed("wolf"), 1.9f, 2.1f);
    }

    [Fact]
    public void The_squad_is_capped_at_ten_heroes_the_lead_included()
    {
        Assert.Equal(10, RunnerRules.SquadCap);
        Assert.Equal(9, RunnerRules.MaxFollowers);
        var c = Runner.Catalog();
        Assert.All(c.Levels, l => Assert.True(l.MaxFollowers <= RunnerRules.MaxFollowers, $"L{l.Number} max_followers {l.MaxFollowers}"));

        // A level file asking for more is clamped to the cap.
        Assert.Equal(9, Runner.Tiny("", maxFollowers: 24).First.MaxFollowers);

        foreach (var level in c.Levels)
        {
            var run = new RunnerRun(c, level, RunnerShipped.Squad(c, level));
            var most = 0;
            while (!run.Over && run.Seconds < 300f)
            {
                run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
                run.Step(1f / 30f);
                most = Math.Max(most, run.Squad.Count);
            }

            Assert.True(most <= RunnerRules.SquadCap, $"L{level.Number} squad of {most}");
            Assert.Equal(RunnerRules.SquadCap, most);
        }
    }

    [Fact]
    public void Follower_boxes_past_the_cap_give_damage_and_a_heal_instead()
    {
        // Two follower boxes (8 then 5 archers): the squad fills to 9 followers, the 4 spare ones become damage.
        var rows = @"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 8 },
            { ""at"": 12, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 5 }";
        var run = Runner.Run(Runner.Tiny(rows, maxFollowers: 24, length: 60f));
        var events = Runner.Play(run, _ => 0f);
        Assert.Equal(9, run.Followers);
        Assert.Equal(10, run.Squad.Count);
        Assert.Equal(1f + (RunnerRules.FullSquadDamage * 4), run.DamageMul, 3);
        var full = events.Where(e => e.Kind == RunnerEventKind.Upgrade && e.Unit == "squad_full").ToList();
        Assert.Single(full);
        Assert.Equal(9, events.Count(e => e.Kind == RunnerEventKind.FollowerJoined));

        // A hero chest at a full squad pays the same way.
        var hero = Runner.Run(Runner.Tiny(rows + @", { ""at"": 18, ""kind"": ""box"", ""hp"": 10, ""reward"": ""hero"", ""unit"": ""archer"" }", maxFollowers: 24, length: 60f));
        Runner.Play(hero, _ => 0f);
        Assert.Equal(9, hero.Followers);
        Assert.Equal(1f + (RunnerRules.FullSquadDamage * 5), hero.DamageMul, 3);
    }

    [Fact]
    public void A_full_squad_box_heals_the_lead()
    {
        var rows = @"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 9 },
            { ""at"": 14, ""kind"": ""enemy"", ""x"": 0, ""unit"": ""wolf"", ""count"": 1 },
            { ""at"": 24, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 2 }";
        var run = Runner.Run(Runner.Tiny(rows, maxFollowers: 24, length: 60f));
        run.Hero.Hp = 40;
        Runner.Play(run, _ => 0f);
        Assert.True(run.Hero.Hp > 40, "the spare units healed the lead: " + run.Hero.Hp);
    }

    [Fact]
    public void Melee_only_heroes_are_off_the_runner_ranged_ones_stay()
    {
        var c = Runner.Catalog();
        Assert.DoesNotContain(c.Units.Values, u => u.Roster == "Sir Aldric");
        Assert.DoesNotContain(c.Units.Keys, k => k.Contains("aldric"));
        var heroes = c.Units.Values.Where(u => !string.IsNullOrEmpty(u.HeroId)).Select(u => u.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "lyra", "oakenshield", "rowan" }, heroes);

        // The attack analysis, from the locked battle data: Sir Aldric has no projectile attack, the three kept heroes do.
        Assert.False(RunnerRoster.IsRangedFighter("Sir Aldric"));
        foreach (var name in new[] { "Lyra", "Rowan", "Oakenshield" })
        {
            Assert.True(RunnerRoster.IsRangedFighter(name), name);
        }

        // Nothing on the squad side of the file is melee-only: the start hero, every follower box and every hero chest.
        foreach (var level in c.Levels)
        {
            Assert.True(RunnerRoster.IsPlayable(c.Unit(level.Hero)), level.Id + " hero " + level.Hero);
            foreach (var row in level.Rows.Where(r => r.Kind == RunnerRowKind.Box && r.Box!.GivesUnits))
            {
                Assert.True(RunnerRoster.IsPlayable(c.Unit(row.Box!.Unit)), $"{level.Id} box at {row.At}: {row.Box.Unit}");
            }
        }

        // No aldric anywhere in the level data.
        Assert.DoesNotContain(c.Levels.SelectMany(l => l.Rows), r => r.Unit == "sir_aldric" || r.Box?.Unit == "sir_aldric");
    }

    [Fact]
    public void A_level_file_naming_a_melee_hero_as_lead_follower_or_chest_is_refused()
    {
        var chest = @"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""hero"", ""unit"": ""knight"" }";
        Assert.Throws<FormatException>(() => Runner.Tiny(chest));
        var follower = @"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""knight"", ""count"": 2 }";
        Assert.Throws<FormatException>(() => Runner.Tiny(follower));
        Assert.NotNull(Runner.Tiny(chest, allowMelee: true));

        // Archers (ranged Rowan) are fine.
        Assert.NotNull(Runner.Tiny(@"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 2 }"));
    }

    private const string TwoHeroes = @"{
  ""units"": {
    ""sir_aldric"": { ""roster"": ""Sir Aldric"", ""hero"": ""hero.sir_aldric"", ""hp"": 400, ""damage"": 30, ""cycle"": 1.0, ""range"": 2.0 },
    ""rowan"": { ""roster"": ""Rowan"", ""hero"": ""hero.rowan"", ""hp"": 1, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""lyra"": { ""roster"": ""Lyra"", ""hero"": ""hero.lyra"", ""hp"": 100, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""nightfang"": { ""roster"": ""Nightfang"", ""hp"": 20, ""damage"": 40, ""cycle"": 1.0, ""range"": 1.2, ""speed"": 2.0, ""radius"": 0.6 }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""lyra"", ""length"": 40, ""speed"": 2, ""half_width"": 4, ""max_followers"": 6,
    ""rewards"": { ""win_gold"": 100, ""gold_per_kill"": 5, ""gold_per_box"": 10, ""shards"": 3, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ ] } ] }";

    [Fact]
    public void A_saved_hero_selection_naming_a_melee_hero_falls_back_to_a_ranged_one()
    {
        var c = RunnerCatalog.Parse(TwoHeroes, true);
        Assert.Equal("lyra", RunnerRoster.PlayableHero(c, "sir_aldric")!.Id);
        Assert.Equal("rowan", RunnerRoster.PlayableHero(c, "rowan")!.Id);
        Assert.Equal("lyra", RunnerRoster.PlayableHero(c, null)!.Id);
        Assert.Equal("lyra", RunnerRoster.PlayableHero(c, "nobody")!.Id);
        Assert.Equal("lyra", RunnerRoster.PlayableHero(c, "")!.Id);
        Assert.False(RunnerRoster.IsPlayable(c.Unit("sir_aldric")));

        // The same file, parsed as the game parses it, is refused only if a melee hero is on the squad side; as a roster it is fine.
        var shipped = Runner.Catalog();
        Assert.Equal("lyra", RunnerRoster.PlayableHero(shipped, "sir_aldric")!.Id);
        Assert.Equal("oakenshield", RunnerRoster.PlayableHero(shipped, "oakenshield")!.Id);
        Assert.Equal(shipped.First.Hero, RunnerRoster.PlayableHero(shipped, shipped.First.Hero)!.Id);
    }

    [Fact]
    public void The_squad_prewarm_shrinks_with_the_cap()
    {
        var c = Runner.Catalog();
        foreach (var level in c.Levels)
        {
            var plan = RunnerPrewarm.Plan(level, c, 0.72f, out _);
            var bound = RunnerPrewarm.Bound(level, c, 0.72f);
            Assert.All(bound.Where(b => b.Role == RunnerPrewarmRole.Squad), b => Assert.True(b.Count <= RunnerRules.SquadCap + RunnerPrewarm.DyingFollowerMargin, $"L{level.Number} {b}"));
            Assert.All(plan.Where(p => p.Role == RunnerPrewarmRole.Squad), p => Assert.True(p.Count <= RunnerRules.SquadCap + RunnerPrewarm.DyingFollowerMargin, $"L{level.Number} {p}"));
            Assert.DoesNotContain(plan, p => p.Roster == "Sir Aldric");
            _out.WriteLine($"L{level.Number}: " + string.Join(", ", plan.Where(p => p.Role == RunnerPrewarmRole.Squad)));
        }
    }

    [Fact]
    public void Boss_damage_lands_only_from_shots_whose_lane_overlaps_him()
    {
        // His body is 1.0 m, a shot is 0.25 m half-wide: a shot hits within 1.25 m of his centre line, and not beyond.
        var miss = FightAt(3.4f, 8f);
        var boss = miss.Run.Foes.Single(f => f.Boss);
        Assert.Contains(miss.Events, e => e.Kind == RunnerEventKind.BossEngaged);
        Assert.Contains(miss.Events, e => e.Kind == RunnerEventKind.Fired && e.Id == miss.Run.Hero.Id && e.Amount > 0);
        Assert.Equal(boss.MaxHp, boss.Hp);
        Assert.DoesNotContain(miss.Events, e => e.Kind == RunnerEventKind.Hit && e.Big);

        var edgeOut = FightAt(1.5f, 8f).Run.Foes.Single(f => f.Boss);
        Assert.Equal(edgeOut.MaxHp, edgeOut.Hp);
        var edgeIn = FightAt(1.1f, 8f).Run.Foes.Single(f => f.Boss);
        Assert.True(edgeIn.Hp < edgeIn.MaxHp, "a shot inside his body hits");
        var lined = FightAt(0f, 8f);
        Assert.True(lined.Run.Foes.Count == 0 || lined.Run.Foes.Single(f => f.Boss).Hp < lined.Run.Foes.Single(f => f.Boss).MaxHp);
    }

    [Fact]
    public void Heroes_off_his_lane_lose_nothing_to_his_slam_and_those_in_it_do()
    {
        // Off to the side: he winds up and slams the marked road, and the hero is not on it.
        var side = FightAt(3.4f, 3f);
        Assert.Contains(side.Events, e => e.Kind == RunnerEventKind.BossWindup);
        Assert.Contains(side.Events, e => e.Kind == RunnerEventKind.BossSlam);
        Assert.All(side.Events.Where(e => e.Kind == RunnerEventKind.BossSlam), e => Assert.Equal(0f, e.Amount));
        Assert.DoesNotContain(side.Events, e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(side.Run.Hero.Unit.Hp, side.Run.Hero.Hp);

        // On the road: the same slam lands on the hero, after the windup he was shown.
        var on = FightAt(0f, 4f, BossRow);
        var windup = on.Events.First(e => e.Kind == RunnerEventKind.BossWindup);
        Assert.Equal(RunnerRules.BossWindup, windup.Amount, 3);
        var hit = on.Events.First(e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(4f, hit.Amount);
        Assert.True(on.Run.Hero.Hp < on.Run.Hero.Unit.Hp);
        var order = on.Events.Select(e => e.Kind).ToList();
        Assert.True(order.IndexOf(RunnerEventKind.BossWindup) < order.IndexOf(RunnerEventKind.BossSlam));
    }

    [Fact]
    public void The_slam_hurts_the_followers_in_front_through_their_hp_and_the_slam_area_is_the_marked_road()
    {
        var rows = @"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 4 }, " + BossRow;
        var run = Runner.Run(Runner.Tiny(rows.Replace("\"at\": 36", "\"at\": 56"), maxFollowers: 9, length: 60f));
        var events = new List<RunnerEvent>();
        var caught = 0f;
        while (!run.Over && run.Seconds < 60f)
        {
            run.SteerTo(0f);
            foreach (var e in run.Step(1f / 30f))
            {
                events.Add(e);
                if (e.Kind == RunnerEventKind.BossSlam)
                {
                    caught += e.Amount;
                }
            }
        }

        Assert.True(caught >= 1f, "the slam caught followers: " + caught);
        // The slam goes through a follower's 25 HP (it used to knock them out whatever the damage): this boss swings
        // for 4, so the first follower it catches is hit for 4 and stands; the shipped boss's 40 still drops one.
        var first = events.First(e => (e.Kind == RunnerEventKind.FollowerHit || e.Kind == RunnerEventKind.FollowerLost)
            && events.Any(s => s.Kind == RunnerEventKind.FoeSpawned && s.Id == e.Other && s.Unit == "boss"));
        Assert.Equal(RunnerEventKind.FollowerHit, first.Kind);
        Assert.Equal(4f, first.Amount);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.HeroHit);

        var tiny = Runner.Run(Runner.Tiny(BossRow));
        while (tiny.Foes.Count == 0)
        {
            tiny.Step(1f / 30f);
        }

        var boss = tiny.Foes.Single(f => f.Boss);
        var area = RunnerRun.SlamArea(boss);
        Assert.Equal(-(boss.Unit.Radius + RunnerRules.BossSlamExtra), area.MinX, 3);
        Assert.Equal(boss.Unit.Radius + RunnerRules.BossSlamExtra, area.MaxX, 3);
        Assert.Equal(boss.Z, area.MaxZ, 3);
        Assert.Equal(boss.Z - RunnerRules.BossSlamDepth, area.MinZ, 3);
    }

    [Fact]
    public void Every_shipped_boss_winds_up_and_slams_and_still_loses_to_nobody_who_lines_up_and_dodges()
    {
        var c = Runner.Catalog();
        foreach (var level in c.Levels.Where(l => l.HasBoss))
        {
            var run = new RunnerRun(c, level, RunnerShipped.Squad(c, level));
            int windups = 0, slams = 0;
            float caught = 0f;
            while (!run.Over && run.Seconds < 400f)
            {
                run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
                foreach (var e in run.Step(1f / 30f))
                {
                    windups += e.Kind == RunnerEventKind.BossWindup ? 1 : 0;
                    slams += e.Kind == RunnerEventKind.BossSlam ? 1 : 0;
                    caught += e.Kind == RunnerEventKind.BossSlam ? e.Amount : 0f;
                }
            }

            _out.WriteLine($"L{level.Number}: {run.Outcome} windups {windups} slams {slams} caught {caught} hero {run.Hero.Hp}/{run.Hero.MaxHp}");
            Assert.Equal(RunnerOutcome.Won, run.Outcome);
            Assert.True(slams >= 1 && windups >= slams, $"L{level.Number}: the boss attacked ({windups} windups, {slams} slams)");
        }
    }

    [Fact]
    public void A_player_who_does_not_line_up_never_hurts_the_boss_so_he_wins_and_one_who_dodges_loses_less()
    {
        var c = Runner.Catalog();
        var level = c.First;

        // Hugging the edge the whole run: shots miss him, his slam lands on the road in front of him, not on the edge.
        var edge = new RunnerRun(c, level);
        float bossHpAtEnd = 0f;
        while (!edge.Over && edge.Seconds < 400f)
        {
            edge.SteerTo(3.9f);
            edge.Step(1f / 30f);
            if (edge.Foes.FirstOrDefault(f => f.Boss) is { } b)
            {
                bossHpAtEnd = b.HpFraction;
            }
        }

        Assert.NotEqual(RunnerOutcome.Won, edge.Outcome);
        _out.WriteLine($"edge: {edge.Outcome}, boss left {bossHpAtEnd:0.00}");
    }
}
