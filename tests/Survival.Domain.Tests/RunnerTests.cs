using Survival.Domain.Heroes;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

internal static class Runner
{
    public static RunnerCatalog Catalog() =>
        RunnerCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerCatalog.FileName)));

    public static RunnerCatalog Tiny(string rows, string extra = "", float length = 40f, bool allowMelee = false, int maxFollowers = 6) => RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 100, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""archer"": { ""roster"": ""Rowan"", ""hp"": 1, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""knight"": { ""roster"": ""Sir Aldric"", ""hp"": 1, ""damage"": 30, ""cycle"": 1.0, ""range"": 2.0 },
    ""wolf"": { ""roster"": ""Nightfang"", ""hp"": 20, ""damage"": 40, ""cycle"": 1.0, ""range"": 1.2, ""speed"": 2.0, ""radius"": 0.6 },
    ""brute"": { ""roster"": ""Blightroot"", ""hp"": 100000, ""damage"": 40, ""cycle"": 1.0, ""range"": 1.5, ""speed"": 2.0, ""radius"": 0.6 },
    ""boss"": { ""roster"": ""Ironhowl"", ""hp"": 450, ""damage"": 4, ""cycle"": 1.0, ""range"": 1.8, ""radius"": 1.0 }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": " + length.ToString(System.Globalization.CultureInfo.InvariantCulture) + @", ""speed"": 2, ""half_width"": 4, ""max_followers"": " + maxFollowers + @", " + extra + @"
    ""rewards"": { ""win_gold"": 100, ""gold_per_kill"": 5, ""gold_per_box"": 10, ""shards"": 3, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ " + rows + @" ] } ] }", allowMelee);

    public static RunnerRun Run(RunnerCatalog c) => new(c, c.First);

    /// <summary>Plays to the end at 30 fps; <paramref name="steer"/> picks the lane x each frame.</summary>
    public static List<RunnerEvent> Play(RunnerRun run, Func<RunnerRun, float>? steer = null, float maxSeconds = 600f)
    {
        var all = new List<RunnerEvent>();
        while (!run.Over && run.Seconds < maxSeconds)
        {
            if (steer != null)
            {
                run.SteerTo(steer(run));
            }

            all.AddRange(run.Step(1f / 30f));
        }

        return all;
    }

    /// <summary>A player who steers at the nearest box ahead, else holds the middle.</summary>
    public static float AimAtBoxes(RunnerRun run)
    {
        RunnerBox? best = null;
        foreach (var b in run.Boxes)
        {
            if (b.Z > run.Hero.Z && (best == null || b.Z < best.Z))
            {
                best = b;
            }
        }

        return best?.X ?? 0f;
    }

    /// <summary>
    /// A player who aims by steering (shots fly straight): lines up the nearest box ahead within 22 m, else the nearest enemy ahead within 16 m, else holds the line, and
    /// steers the hero round any lane trap coming up. <paramref name="upgrades"/> false skips weapon / rate boxes.
    /// </summary>
    public static float AimLikeAPlayer(RunnerRun run) => Aim(run, true);

    public static float Aim(RunnerRun run, bool upgrades)
    {
        RunnerBox? box = null;
        foreach (var b in run.Boxes)
        {
            if (b.Z > run.Hero.Z && b.Z - run.Hero.Z < 22f && (upgrades || b.Def.GivesUnits) && (box == null || b.Z < box.Z))
            {
                box = b;
            }
        }

        if (box != null)
        {
            return Dodge(run, box.X);
        }

        RunnerFoe? foe = null;
        foreach (var f in run.Foes)
        {
            if (f.Hp > 0 && f.Z > run.Hero.Z - 0.5f && f.Z - run.Hero.Z < 16f && (foe == null || f.Z < foe.Z))
            {
                foe = f;
            }
        }

        return Dodge(run, foe?.X ?? run.Hero.X);
    }

    /// <summary>Like a player: keeps the hero's line clear of any lane trap coming up in the next 9 m (the squad behind may still clip one).</summary>
    public static float Dodge(RunnerRun run, float want)
    {
        foreach (var h in run.Hazards)
        {
            var ahead = h.Z - run.Hero.Z;
            var clear = h.HalfWidth + run.Hero.Unit.Radius + 0.5f;
            if (ahead < -1f || ahead > 9f || Math.Abs(want - h.X) >= clear)
            {
                continue;
            }

            var lane = run.Level.HalfWidth - run.Hero.Unit.Radius;
            var left = h.X - clear;
            var right = h.X + clear;
            var leftOk = left >= -lane;
            var rightOk = right <= lane;
            want = leftOk && (!rightOk || Math.Abs(want - left) <= Math.Abs(want - right)) ? left : right;
        }

        return want;
    }

    /// <summary>The shipped catalog with every box of the given kinds taken out.</summary>
    public static RunnerCatalog CatalogWithout(params RunnerBoxKind[] kinds)
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerCatalog.FileName)))!;
        var rows = json["levels"]![0]!["rows"]!.AsArray();
        for (var i = rows.Count - 1; i >= 0; i--)
        {
            var reward = (string?)rows[i]!["reward"];
            if (reward != null && kinds.Any(k => string.Equals(k.ToString(), reward, StringComparison.OrdinalIgnoreCase)))
            {
                rows.RemoveAt(i);
            }
        }

        return RunnerCatalog.Parse(json.ToJsonString());
    }
}

public sealed class RunnerTests
{
    private readonly ITestOutputHelper _out;

    public RunnerTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Shipped_level_loads_with_enemy_waves_boxes_of_every_kind_and_a_boss_on_locked_rigs()
    {
        var c = Runner.Catalog();
        var level = c.First;
        Assert.Equal("runner.thornwood_road", level.Id);
        Assert.Equal("lyra", level.Hero);
        Assert.True(level.HasBoss);
        Assert.True(level.EnemyCount >= 45, "enemies " + level.EnemyCount);
        // About 40% fewer boxes than before (18 -> 11): one box a beat, not a chest and a crate on every beat.
        Assert.InRange(level.BoxCount, 9, 13);
        Assert.InRange(level.HazardCount, 1, 4);
        Assert.Contains(level.Rows, r => r.Hazard?.Kind == RunnerHazardKind.Spikes);
        Assert.Contains(level.Rows, r => r.Hazard?.Kind == RunnerHazardKind.Fire);
        foreach (var kind in Enum.GetValues<RunnerBoxKind>())
        {
            // Level 1 has no heal crate any more; healing turns up from Level 2.
            Assert.Contains(kind == RunnerBoxKind.Heal ? c.Levels[1].Rows : level.Rows, r => r.Box?.Kind == kind);
        }

        // Fodder from the locked villain roster, Ironhowl as the end boss; every rig is a locked battle fighter.
        Assert.Contains(level.Rows, r => r.Kind == RunnerRowKind.Enemy && r.Unit == "bonequill");
        Assert.Contains(level.Rows, r => r.Kind == RunnerRowKind.Enemy && r.Unit == "nightfang");
        Assert.Contains(level.Rows, r => r.Kind == RunnerRowKind.Enemy && r.Unit == "blightroot");
        Assert.Equal("ironhowl", level.Rows.Single(r => r.Kind == RunnerRowKind.Boss).Unit);
        Assert.Equal(level.Rows.Max(r => r.At), level.Rows.Single(r => r.Kind == RunnerRowKind.Boss).At);
        var names = LockedBattleRoster.All.ToDictionary(f => f.Name, f => f.Side);
        foreach (var u in c.Units.Values)
        {
            Assert.True(names.ContainsKey(u.Roster), u.Id + " roster " + u.Roster);
        }

        Assert.Equal(BattleSide.Hero, names[c.Unit(level.Hero).Roster]);
        foreach (var row in level.Rows)
        {
            Assert.InRange(Math.Abs(row.X), 0f, level.HalfWidth);
            if (row.Kind == RunnerRowKind.Hazard)
            {
                Assert.NotNull(row.Hazard);
            }
            else if (row.Kind != RunnerRowKind.Box)
            {
                Assert.Equal(BattleSide.Villain, names[c.Unit(row.Unit).Roster]);
            }
            else if (row.Box!.GivesUnits)
            {
                Assert.Equal(BattleSide.Hero, names[c.Unit(row.Box.Unit).Roster]);
            }
        }
    }

    [Fact]
    public void Rows_spawn_by_distance_as_the_squad_walks_forward_at_the_level_pace()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 10, ""kind"": ""box"", ""hp"": 9999, ""reward"": ""weapon"" },
            { ""at"": 46, ""kind"": ""box"", ""x"": 3, ""hp"": 9999, ""reward"": ""rate"" }", length: 60f));
        Assert.Single(run.Boxes);
        Assert.Equal(0f, run.Distance);
        var events = run.Step(4f);
        Assert.Equal(8f, run.Distance, 3);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.BoxSpawned);
        Assert.Equal(2, run.Boxes.Count + run.BoxesMissed);
    }

    [Fact]
    public void Only_left_right_steering_and_it_is_clamped_to_the_lane()
    {
        var run = Runner.Run(Runner.Tiny(""));
        run.SteerTo(99f);
        run.Step(2f);
        Assert.Equal(run.MaxSteer, run.Hero.X, 3);
        Assert.True(run.Hero.X < run.Level.HalfWidth);
        run.SteerBy(-100f);
        run.Step(2f);
        Assert.Equal(-run.MaxSteer, run.Hero.X, 3);
        Assert.Equal(run.Distance, run.Hero.Z, 3);
    }

    [Fact]
    public void A_box_must_be_shot_down_to_zero_hp_and_then_pays_followers_that_join_the_formation()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 14, ""kind"": ""box"", ""x"": 0, ""hp"": 30, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 3 }"));
        var box = Assert.Single(run.Boxes);
        Assert.Equal(30, box.Hp);
        var events = new List<RunnerEvent>();
        while (run.Boxes.Count > 0)
        {
            Assert.Equal(0, run.Followers);
            events.AddRange(run.Step(0.1f));
        }

        var hits = events.Where(e => e.Kind == RunnerEventKind.Hit && e.OnBox).ToList();
        Assert.Equal(3, hits.Count);
        Assert.All(hits, h => Assert.Equal(10f, h.Amount));
        Assert.Contains(events, e => e.Kind == RunnerEventKind.BoxBroken && e.Big);
        Assert.Equal(3, events.Count(e => e.Kind == RunnerEventKind.FollowerJoined));
        Assert.Equal(3, run.Followers);
        Assert.Equal(1, run.BoxesBroken);
        Assert.Equal(new[] { 0, 1, 2 }, run.Squad.Skip(1).Select(m => m.Slot));
        run.Step(3f);
        foreach (var m in run.Squad.Skip(1))
        {
            var (sx, sz) = RunnerFormation.Offset(m.Slot);
            Assert.Equal(run.Hero.X + sx, m.X, 1);
            Assert.Equal(run.Distance + sz, m.Z, 1);
        }
    }

    [Fact]
    public void A_box_the_squad_walks_past_unbroken_is_lost_nothing_is_a_free_pickup()
    {
        // Out of the aim strip on the far right: walking past it gives nothing.
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 12, ""kind"": ""box"", ""x"": 3.5, ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 5 }"));
        run.SteerTo(-4f);
        var events = Runner.Play(run, _ => -4f);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.BoxMissed);
        Assert.DoesNotContain(events, e => e.Kind == RunnerEventKind.BoxBroken);
        Assert.Equal(0, run.Followers);
        Assert.Equal(1, run.BoxesMissed);
        Assert.Equal(RunnerOutcome.Won, run.Outcome);
    }

    [Fact]
    public void Formation_rows_grow_behind_the_hero_and_bend_while_steering()
    {
        Assert.Equal((-0.5f * RunnerRules.Spacing, -0.95f * RunnerRules.Spacing), RunnerFormation.Offset(0));
        Assert.Equal(1, RunnerFormation.RowOf(1));
        Assert.Equal(2, RunnerFormation.RowOf(2));
        Assert.Equal(2, RunnerFormation.RowOf(4));
        Assert.Equal(3, RunnerFormation.RowOf(5));
        Assert.Equal(0f, RunnerFormation.Offset(3).X, 4);
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 12, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 6 }"));
        while (run.Followers < 6)
        {
            run.Step(0.1f);
        }

        run.Step(3f);
        run.SteerTo(3f);
        run.Step(0.2f);
        var front = run.Squad.Where(m => !m.IsHero && RunnerFormation.RowOf(m.Slot) == 1).Average(m => m.X - RunnerFormation.Offset(m.Slot).X);
        var back = run.Squad.Where(m => RunnerFormation.RowOf(m.Slot) == 3).Average(m => m.X - RunnerFormation.Offset(m.Slot).X);
        Assert.True(run.Hero.X > front && front > back, $"hero {run.Hero.X} front {front} back {back}");
    }

    [Fact]
    public void Weapon_and_attack_speed_boxes_upgrade_the_whole_squad_once_broken()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 12, ""kind"": ""box"", ""hp"": 20, ""reward"": ""weapon"", ""amount"": 0.5 },
            { ""at"": 14, ""kind"": ""box"", ""hp"": 30, ""reward"": ""rate"", ""amount"": 1.0 },
            { ""at"": 30, ""kind"": ""box"", ""hp"": 9999, ""reward"": ""weapon"" }"));
        var events = new List<RunnerEvent>();
        while (run.BoxesBroken < 2)
        {
            events.AddRange(run.Step(0.1f));
        }

        Assert.Equal(1.5f, run.DamageMul, 3);
        Assert.Equal(2f, run.RateMul, 3);
        Assert.Equal(2, events.Count(e => e.Kind == RunnerEventKind.Upgrade));
        events.Clear();
        while (!events.Any(e => e.Kind == RunnerEventKind.Hit))
        {
            events.AddRange(run.Step(0.05f));
        }

        Assert.Equal(15f, events.First(e => e.Kind == RunnerEventKind.Hit).Amount);
        var fires = new List<float>();
        while (fires.Count < 3)
        {
            foreach (var e in run.Step(0.01f))
            {
                if (e.Kind == RunnerEventKind.Fired && e.Id == run.Hero.Id)
                {
                    fires.Add(run.Seconds);
                }
            }
        }

        Assert.Equal(0.25f, fires[2] - fires[1], 2);
    }

    [Fact]
    public void Enemies_the_squad_walks_into_keep_hitting_it_as_it_pushes_through_followers_first()
    {
        // A brute it cannot kill: the squad walks on through it (never held up), the brute keeps swinging at whoever is
        // in reach, ahead or already past, killing the followers first and then hurting the hero, then it drops behind.
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 8, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 2 },
            { ""at"": 30, ""kind"": ""enemy"", ""unit"": ""brute"" }", length: 60f));
        var events = new List<RunnerEvent>();
        var hitWhilePassed = 0;
        while (!run.Over && run.Seconds < 120f)
        {
            var before = run.Distance;
            foreach (var e in run.Step(1f / 30f))
            {
                events.Add(e);
                if ((e.Kind == RunnerEventKind.FollowerLost || e.Kind == RunnerEventKind.HeroHit) && run.Foe(e.Other) is { Passed: true })
                {
                    hitWhilePassed++;
                }
            }

            Assert.Equal(before + (run.Level.Speed / 30f), run.Distance, 3);
        }

        var lost = events.Where(e => e.Kind == RunnerEventKind.FollowerLost).ToList();
        var firstHeroHit = events.FindIndex(e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(2, lost.Count);
        Assert.True(firstHeroHit > events.LastIndexOf(lost[^1]), "followers die before the hero is touched");
        Assert.All(lost, e => Assert.True(e.Big));
        Assert.True(run.Hero.Hp < run.Hero.Unit.Hp);
        Assert.True(hitWhilePassed > 0, "it kept hitting the squad after the hero was past it");
        Assert.True(run.Distance > 30f, "the squad walked on past it to " + run.Distance);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.FoePassed);
        Assert.Equal(2, run.Result.FollowersPeak);
    }

    [Fact]
    public void The_squad_never_stops_on_any_level_until_the_boss_arena_line()
    {
        var c = Runner.Catalog();
        foreach (var level in new[] { c.First, c.ByNumber(6), c.Levels[^1] })
        {
            foreach (var steer in new Func<RunnerRun, float>[] { Runner.AimLikeAPlayer, _ => 0f, _ => 3.9f })
            {
                var run = new RunnerRun(c, level);
                while (!run.Over && run.Seconds < 400f)
                {
                    run.SteerTo(steer(run));
                    var before = run.Distance;
                    run.Step(1f / 30f);
                    if (!run.Over)
                    {
                        // Full pace through every wave; only the arena line in front of the boss stops it.
                        Assert.Equal(Math.Min(before + (level.Speed / 30f), run.ArenaLine), run.Distance, 3);
                        Assert.Equal(run.Distance >= run.ArenaLine - 1e-3f, run.Halted);
                    }
                }

                Assert.True(run.Over);
                Assert.True(run.Distance <= run.ArenaLine + 1e-3f);
            }
        }
    }

    [Fact]
    public void A_ranged_enemy_the_squad_walks_past_turns_and_shoots_after_it_and_a_touching_body_hurts()
    {
        var c = RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 2000, ""damage"": 1, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""tower"": { ""roster"": ""Bonequill"", ""hp"": 100000, ""damage"": 10, ""cycle"": 0.6, ""range"": 8, ""ranged"": true, ""shot_speed"": 12, ""speed"": 0 }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": 40, ""speed"": 2, ""half_width"": 4,
    ""rows"": [ { ""at"": 15, ""kind"": ""enemy"", ""unit"": ""tower"" } ] } ] }");
        var run = new RunnerRun(c, c.First);
        var backShots = 0;
        var forwardShots = 0;
        var hpAtContact = -1;
        var contactHits = 0;
        var seen = new HashSet<int>();
        while (!run.Over && run.Seconds < 60f)
        {
            foreach (var e in run.Step(1f / 30f))
            {
                if (e.Kind == RunnerEventKind.HeroHit && run.Foes.Count > 0 && Math.Abs(run.Foes[0].Z - run.Hero.Z) < 0.6f)
                {
                    contactHits++;
                }
            }

            foreach (var s in run.Shots.Where(s => s.ByFoe && seen.Add(s.Id)))
            {
                if (s.Direction < 0f)
                {
                    backShots++;
                }
                else
                {
                    forwardShots++;
                }
            }

            if (hpAtContact < 0 && run.Foes.Count > 0 && run.Foes[0].Engaged)
            {
                hpAtContact = run.Hero.Hp;
            }
        }

        Assert.Equal(RunnerOutcome.Won, run.Outcome);
        Assert.True(backShots > 0 && forwardShots > 0, $"shots up the road {backShots}, after the squad {forwardShots}");
        Assert.True(contactHits > 0, "the body it walked through hit it");
        Assert.True(run.Hero.Hp < hpAtContact);
    }

    [Fact]
    public void Steering_round_an_enemy_dodges_it_and_it_is_left_behind()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 20, ""kind"": ""enemy"", ""x"": -3.4, ""unit"": ""brute"" }", length: 40f));
        var events = Runner.Play(run, _ => 3.4f);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.FoePassed);
        Assert.Equal(RunnerOutcome.Won, run.Outcome);
        Assert.Equal(run.Hero.Unit.Hp, run.Hero.Hp);
    }

    [Fact]
    public void The_squad_halts_at_the_boss_arena_line_and_fights_him_there_he_holds_his_ground()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 12, ""kind"": ""enemy"", ""unit"": ""wolf"", ""count"": 2, ""spread"": 2 },
            { ""at"": 36, ""kind"": ""boss"", ""x"": 0, ""unit"": ""boss"" }"));
        var bossUnit = run.Level.Rows.Single(r => r.Kind == RunnerRowKind.Boss);
        var reach = 1.8f + 1.0f + run.Hero.Unit.Radius;
        Assert.Equal(36f - (RunnerRules.BossArenaShare * reach), run.ArenaLine, 3);
        Assert.True(36f - run.ArenaLine < reach, "the line is inside his slam reach, so he fights");

        // Line up the wolves first (shots fly straight), then steer about at the line (he is hit when the sweep crosses his lane).
        var events = new List<RunnerEvent>();
        var haltedFor = 0f;
        var xs = new HashSet<int>();
        while (!run.Over && run.Seconds < 120f)
        {
            // (He winds up his big quake now and then: step into its clear band, as the warning asks.)
            var quaking = run.Foes.FirstOrDefault(f => f.Boss && f.Move == RunnerBossMove.Quake);
            run.SteerTo(quaking is not null ? Math.Sign(quaking.QuakeSafeX) * 2.9f : run.Foes.Any(f => !f.Boss) ? 0f : (float)Math.Sin(run.Seconds * 3f) * 3.9f);
            events.AddRange(run.Step(1f / 30f));
            if (run.Halted)
            {
                haltedFor += 1f / 30f;
                xs.Add((int)Math.Round(run.Hero.X));
                Assert.Equal(run.ArenaLine, run.Distance, 3);
                if (run.Foes.FirstOrDefault(f => f.Boss) is { } boss)
                {
                    Assert.Equal(bossUnit.At, boss.Z, 3);
                    Assert.False(boss.Walking);
                }
            }
        }

        Assert.Contains(events, e => e.Kind == RunnerEventKind.BossEngaged);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.FoeKilled && e.Big);
        Assert.Equal(RunnerOutcome.Won, run.Outcome);
        Assert.True(haltedFor > 0.5f, "stood and fought for " + haltedFor);
        Assert.True(xs.Count >= 3, "still steers left / right at the line");
        Assert.Contains(events, e => e.Kind == RunnerEventKind.Hit && e.Big);
        Assert.DoesNotContain(run.Foes, f => f.Boss);
        Assert.True(run.Kills >= 2);
    }

    [Fact]
    public void There_is_no_time_limit_at_the_boss_only_a_wiped_squad_loses()
    {
        static RunnerCatalog Arena(int bossDamage) => RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 100, ""damage"": 1, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""boss"": { ""roster"": ""Ironhowl"", ""hp"": 1000000, ""damage"": " + bossDamage + @", ""cycle"": 1.0, ""range"": 1.8, ""radius"": 1.0 }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": 30, ""speed"": 4, ""half_width"": 4,
    ""rows"": [ { ""at"": 25, ""kind"": ""boss"", ""unit"": ""boss"" } ] } ] }");

        // A boss that cannot hurt it: the squad just stands at the line and keeps fighting; nothing ends the run.
        var c = Arena(0);
        var run = new RunnerRun(c, c.First);
        var events = Runner.Play(run, maxSeconds: 600f);
        Assert.False(run.Over);
        Assert.True(run.Halted);
        Assert.Equal(run.ArenaLine, run.Distance, 3);
        Assert.DoesNotContain(events, e => e.Kind == RunnerEventKind.Lost);

        // One that can: lost only once the hero falls.
        c = Arena(10);
        run = new RunnerRun(c, c.First);
        events = Runner.Play(run);
        Assert.Equal(RunnerOutcome.Lost, run.Outcome);
        Assert.Equal(0, run.Hero.Hp);
        Assert.Single(events, e => e.Kind == RunnerEventKind.Lost);
    }

    [Fact]
    public void Reaching_the_end_wins_a_level_without_a_boss()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 12, ""kind"": ""enemy"", ""unit"": ""wolf"" }", length: 30f));
        var events = Runner.Play(run);
        Assert.Equal(RunnerOutcome.Won, run.Outcome);
        Assert.Equal(30f, run.Distance, 3);
        Assert.Single(events, e => e.Kind == RunnerEventKind.Won);
    }

    [Fact]
    public void Melee_followers_hit_what_reaches_them_ranged_units_shoot_down_the_lane()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 6, ""kind"": ""box"", ""hp"": 10, ""reward"": ""hero"", ""unit"": ""knight"" },
            { ""at"": 30, ""kind"": ""enemy"", ""unit"": ""wolf"", ""count"": 3, ""spread"": 1 }", allowMelee: true));
        var events = Runner.Play(run);
        var knight = events.First(e => e.Kind == RunnerEventKind.FollowerJoined).Id;
        Assert.Equal("knight", events.First(e => e.Kind == RunnerEventKind.FollowerJoined).Unit);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.Fired && e.Id == run.Hero.Id && e.Amount > 0);
        Assert.All(events.Where(e => e.Kind == RunnerEventKind.Fired && e.Id == knight), e => Assert.Equal(0f, e.Amount));
    }

    [Fact]
    public void Same_inputs_same_run_whatever_the_frame_rate()
    {
        var c = Runner.Catalog();
        var a = new RunnerRun(c, c.First);
        var b = new RunnerRun(c, c.First);
        while (!a.Over && a.Seconds < 400f)
        {
            a.SteerTo(Runner.AimAtBoxes(a));
            a.Step(1f / 30f);
        }

        while (!b.Over && b.Seconds < 400f)
        {
            b.SteerTo(Runner.AimAtBoxes(b));
            b.Step(1f / 15f);
        }

        Assert.Equal(a.Outcome, b.Outcome);
        Assert.Equal(a.Kills, b.Kills);
        Assert.Equal(a.BoxesBroken, b.BoxesBroken);
    }

    [Fact]
    public void Shipped_level_is_won_by_steering_into_the_boxes_and_lost_by_ignoring_them()
    {
        var c = Runner.Catalog();
        var good = new RunnerRun(c, c.First);
        // With fewer upgrade boxes the boss now outlasts a squad that never steps out of his quakes, so this player dodges.
        Runner.Play(good, r => RunnerBossQuakeTests.DodgingPlayer(r));
        _out.WriteLine($"aim: {good.Outcome} t={good.Seconds:0} kills={good.Kills} boxes={good.BoxesBroken}/{c.First.BoxCount} peak={good.FollowersPeak} left={good.Followers} hp={good.Hero.Hp}");
        Assert.Equal(RunnerOutcome.Won, good.Outcome);
        Assert.True(good.BoxesBroken >= c.First.BoxCount - 2);
        Assert.True(good.FollowersPeak >= 8);
        Assert.InRange(good.Seconds, 65f, 120f);

        var lazy = new RunnerRun(c, c.First);
        Runner.Play(lazy, _ => 3.9f);
        _out.WriteLine($"edge: {lazy.Outcome} t={lazy.Seconds:0} kills={lazy.Kills} boxes={lazy.BoxesBroken} peak={lazy.FollowersPeak} hp={lazy.Hero.Hp}");
        Assert.Equal(RunnerOutcome.Lost, lazy.Outcome);
    }

    [Fact]
    public void Shots_fly_straight_down_the_shooters_lane_so_steering_is_the_aim()
    {
        // A box off to the side: shots fly past it while the hero holds the middle, and land once she steers under it.
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 20, ""kind"": ""box"", ""x"": 3, ""hp"": 9999, ""reward"": ""weapon"" }", length: 60f));
        var fired = 0;
        var maxTravel = 0f;
        for (var t = 0; t < 255; t++)
        {
            foreach (var e in run.Step(1f / 30f))
            {
                fired += e.Kind == RunnerEventKind.Fired ? 1 : 0;
                Assert.NotEqual(RunnerEventKind.Hit, e.Kind);
                if (e.Kind == RunnerEventKind.Fired)
                {
                    Assert.Equal(run.Hero.X, e.X, 3);
                }
            }

            foreach (var s in run.Shots)
            {
                Assert.Equal(0f, s.X, 3);
                Assert.True(s.Direction > 0f);
                maxTravel = Math.Max(maxTravel, s.Travelled);
            }
        }

        Assert.True(fired >= 6, "fired " + fired);
        Assert.Equal(9999, run.Boxes.Single().Hp);
        Assert.InRange(maxTravel, 0.1f, run.Hero.Unit.Range + RunnerRules.ShotOvershoot);
        run.SteerTo(3f);
        var hits = 0;
        for (var t = 0; t < 45; t++)
        {
            hits += run.Step(1f / 30f).Count(e => e.Kind == RunnerEventKind.Hit && e.OnBox);
        }

        Assert.True(hits >= 2, "hits " + hits);
        Assert.True(run.Boxes.Single().Hp < 9999);
    }

    [Fact]
    public void Squad_shots_keep_their_lane_in_flight_and_enemy_shots_their_aimed_heading()
    {
        var shipped = Runner.Catalog();
        Assert.True(shipped.Unit("bonequill").Ranged);
        var run = new RunnerRun(shipped, shipped.First);
        // A squad shot keeps its lane; an enemy shot (aimed at a hero when fired, so it may cross lanes) keeps its
        // heading and flies straight along it: nothing homes.
        var seen = new Dictionary<int, (float X, float Z, float Dx, float Dz)>();
        for (var t = 0; t < 30 * 40 && !run.Over; t++)
        {
            run.SteerTo(Runner.AimLikeAPlayer(run));
            run.Step(1f / 30f);
            foreach (var s in run.Shots)
            {
                if (seen.TryGetValue(s.Id, out var was))
                {
                    Assert.Equal(was.Dx, s.DX);
                    Assert.Equal(was.Dz, s.DZ);
                    if (s.ByFoe)
                    {
                        var step = (float)System.Math.Sqrt(((s.X - was.X) * (s.X - was.X)) + ((s.Z - was.Z) * (s.Z - was.Z)));
                        Assert.Equal(s.DX * step, s.X - was.X, 3);
                    }
                    else
                    {
                        Assert.Equal(was.X, s.X, 4);
                    }
                }

                seen[s.Id] = (s.X, s.Z, s.DX, s.DZ);
            }
        }

        Assert.True(seen.Count > 50, "shots " + seen.Count);
    }

    [Fact]
    public void Squad_fire_rate_is_the_sped_up_attack_take_and_enemy_box_and_boss_hp_scaled_to_match()
    {
        var c = Runner.Catalog();
        static RunnerAttackPlan P(RunnerCatalog c, string id) => RunnerAttackTiming.PlanFor(c.Unit(id));

        // Derek: "heros need less firing with more damage. by about half." The heroes used to all fire every 0.40 s
        // (Lyra and Rowan asked for more than 2.5x and were capped at take / 2.5); now every 0.76 s (~1.9x slower,
        // under the cap) with ~2x the damage per shot, so the DPS and the balance curve stay about the same.
        Assert.All(new[] { "lyra", "rowan", "oakenshield" }, id => Assert.False(P(c, id).Capped));
        Assert.All(new[] { "lyra", "rowan", "oakenshield" }, id => Assert.InRange(P(c, id).Speed, 1f, RunnerAttackTiming.MaxSpeed + 1e-4f));
        Assert.All(new[] { "lyra", "rowan", "oakenshield" }, id => Assert.Equal(0.76f, P(c, id).Interval, 3));
        Assert.Equal(51, c.Unit("lyra").Damage);
        Assert.Equal(30, c.Unit("rowan").Damage);
        Assert.Equal(47, c.Unit("oakenshield").Damage);

        // Enemies tuned for the old ~0.4 s cadence; enemies are many and weak (a bit under half the old 134 HP, a
        // little lighter again now they walk 1.5x faster and spend less time under fire), the boss now has to survive
        // the walk in and be fought at the arena (his hit is a slam, his HP retuned for it).
        Assert.InRange(c.Unit("nightfang").Hp, 30, 60);
        Assert.InRange(c.Unit("ironhowl").Hp, 12000, 30000);
        Assert.All(c.First.Rows.Where(r => r.Box != null), r => Assert.True(r.Box!.Hp >= 100, "box hp " + r.Box!.Hp));
    }

    [Fact]
    public void Each_shooter_releases_on_its_battle_take_release_frame()
    {
        var lyra = RunnerAttackTiming.ClipFor("Lyra")!.Value;
        var rowan = RunnerAttackTiming.ClipFor("Rowan")!.Value;
        var oak = RunnerAttackTiming.ClipFor("Oakenshield")!.Value;
        var aldric = RunnerAttackTiming.ClipFor("Sir Aldric")!.Value;
        Assert.Equal(LyraAttack.Spec.ClipSecondsFor, lyra.ClipSeconds, 4);
        Assert.Equal(LyraAttack.ReleaseFrame / 30f, lyra.ReleaseSeconds, 4);
        Assert.Equal(RowanAttack.ReleaseFrame / 30f, rowan.ReleaseSeconds, 4);
        Assert.Equal(OakenshieldAttack.ReleaseFrame / 30f, oak.ReleaseSeconds, 4);
        Assert.Equal(SirAldricBattlePoses.AttackSeconds, aldric.ClipSeconds, 4);
        Assert.Equal(LockedBattleHits.SirAldricContactFrame / 30f, aldric.ReleaseSeconds, 4);
        Assert.Null(RunnerAttackTiming.ClipFor("Nightfang"));

        // Speeding the take up keeps the release at the same point of it.
        var plan = RunnerAttackTiming.Plan(lyra, 0.31f);
        Assert.Equal(lyra.ReleaseNormalized, plan.Release / plan.Interval, 4);
        Assert.Equal(lyra.ClipSeconds / plan.Speed, plan.Interval, 4);
        var faster = plan.Faster(1.3f);
        Assert.Equal(lyra.ReleaseNormalized, faster.Release / faster.Interval, 4);
        Assert.Equal(plan.Speed * 1.3f, faster.Speed, 4);
    }

    [Fact]
    public void A_shot_leaves_at_the_release_point_of_each_take_and_takes_chain_back_to_back()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 12, ""kind"": ""enemy"", ""unit"": ""brute"" }", length: 60f));
        var plan = run.Hero.Plan;
        Assert.Equal(2f, plan.Speed, 3);
        Assert.Equal(0.5f, plan.Interval, 3);
        Assert.Equal(LyraAttack.ReleaseFrame / 30f / 2f, plan.Release, 3);

        const float dt = 1f / 120f;
        var starts = new List<float>();
        var fires = new List<float>();
        while (fires.Count < 6 && run.Seconds < 20f)
        {
            foreach (var e in run.Step(dt))
            {
                if (e.Id != run.Hero.Id)
                {
                    continue;
                }

                if (e.Kind == RunnerEventKind.AttackStarted)
                {
                    starts.Add(run.Seconds);
                    Assert.Equal(plan.Speed, e.Amount, 3);
                }
                else if (e.Kind == RunnerEventKind.Fired)
                {
                    fires.Add(run.Seconds);
                    Assert.True(e.Amount > 0f);
                }
            }
        }

        Assert.Equal(6, fires.Count);
        for (var i = 0; i < fires.Count; i++)
        {
            Assert.InRange(fires[i] - starts[i], plan.Release - dt - 0.001f, plan.Release + 0.001f);
            if (i > 0)
            {
                Assert.InRange(starts[i] - starts[i - 1], plan.Interval - dt - 0.001f, plan.Interval + dt + 0.001f);
            }
        }
    }

    [Fact]
    public void Every_attack_take_fires_exactly_once_across_the_shipped_level()
    {
        var c = Runner.Catalog();
        var run = new RunnerRun(c, c.First);
        var starts = new Dictionary<int, int>();
        var fires = new Dictionary<int, int>();
        var members = new Dictionary<int, RunnerMember>();
        var shots = 0;
        while (!run.Over && run.Seconds < 200f)
        {
            run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
            foreach (var e in run.Step(1f / 30f))
            {
                if (e.Kind == RunnerEventKind.AttackStarted)
                {
                    starts[e.Id] = starts.GetValueOrDefault(e.Id) + 1;
                }
                else if (e.Kind == RunnerEventKind.Fired)
                {
                    fires[e.Id] = fires.GetValueOrDefault(e.Id) + 1;
                    if (starts.ContainsKey(e.Id) && e.Amount > 0f)
                    {
                        shots++;
                    }
                }
            }

            foreach (var m in run.Squad)
            {
                members[m.Id] = m;
            }
        }

        Assert.Equal(RunnerOutcome.Won, run.Outcome);
        Assert.True(starts.Values.Sum() > 500, "takes " + starts.Values.Sum());
        foreach (var (id, n) in starts)
        {
            // One release per take; only a take still running when the run ended (or its member fell) has not fired yet.
            var f = fires.GetValueOrDefault(id);
            Assert.InRange(n - f, 0, 1);
            Assert.Equal(members[id].CyclesStarted, n);
            Assert.Equal(members[id].Releases, f);
        }

        Assert.Equal(members.Values.Where(m => m.Unit.Ranged).Sum(m => m.Releases), shots);
    }

    [Fact]
    public void The_take_plays_out_then_the_member_walks_again_when_nothing_is_in_range()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 9, ""kind"": ""box"", ""hp"": 1, ""reward"": ""weapon"" }", length: 60f));
        while (!run.Step(1f / 30f).Any(e => e.Kind == RunnerEventKind.BoxBroken))
        {
            Assert.True(run.Seconds < 20f);
        }

        // The box broke while a take was playing; that take runs to its end (no cut), then no new one starts.
        Assert.True(run.Hero.Attacking);
        var events = new List<RunnerEvent>();
        for (var i = 0; i < 30; i++)
        {
            events.AddRange(run.Step(1f / 30f));
        }

        Assert.False(run.Hero.Attacking);
        Assert.DoesNotContain(events, e => e.Kind == RunnerEventKind.AttackStarted);
        Assert.Equal(run.Hero.CyclesStarted, run.Hero.Releases);
    }

    [Fact]
    public void Damage_and_attack_speed_boxes_still_shorten_the_boss_fight()
    {
        static (RunnerRun Run, float Boss) Fight(RunnerCatalog c)
        {
            var run = new RunnerRun(c, c.First);
            var engaged = -1f;
            while (!run.Over && run.Seconds < 400f)
            {
                run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
                foreach (var e in run.Step(1f / 30f))
                {
                    if (e.Kind == RunnerEventKind.BossEngaged)
                    {
                        engaged = run.Seconds;
                    }
                }
            }

            return (run, engaged < 0f ? -1f : run.Seconds - engaged);
        }

        var with = Fight(Runner.Catalog());
        var without = Fight(Runner.CatalogWithout(RunnerBoxKind.Weapon, RunnerBoxKind.Rate));
        _out.WriteLine($"with upgrades: {with.Run.Outcome} boss {with.Boss:0.0}s; without: {without.Run.Outcome} boss {without.Boss:0.0}s");
        Assert.Equal(RunnerOutcome.Won, with.Run.Outcome);
        Assert.True(with.Run.DamageMul > 1.5f && with.Run.RateMul >= 1.25f, $"damage {with.Run.DamageMul} rate {with.Run.RateMul}");
        Assert.True(without.Run.Outcome == RunnerOutcome.Lost || without.Boss > with.Boss * 1.5f, $"boss {with.Boss} vs {without.Boss}");
    }

    [Fact]
    public void Rewards_pay_gold_per_kill_and_box_and_the_win_bonus_and_shards_only_for_a_win()
    {
        var level = Runner.Tiny("").First;
        var win = RunnerRewards.For(level, new RunnerResult("t", true, 4, 2, 0, 3, 1, 40, 20));
        Assert.Equal(100 + 20 + 20, win.Resources[SurvIds.ResSoft]);
        Assert.Equal(3, win.Shards);
        Assert.Equal("hero.lyra", win.ShardHero);
        var loss = RunnerRewards.For(level, new RunnerResult("t", false, 4, 2, 0, 3, 0, 20, 20));
        Assert.Equal(40, loss.Resources[SurvIds.ResSoft]);
        Assert.Equal(0, loss.Shards);
        var rows = RunnerRewards.Rows(new[] { win, loss });
        Assert.Equal(180, rows[SurvIds.ResSoft]);
        Assert.Equal(3, rows[RewardBundle.ShardKey("hero.lyra")]);
    }

    [Fact]
    public void Runner_rewards_land_in_the_campaign_wallet_and_hero_shards_and_survive_the_save()
    {
        var catalog = CampaignCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CampaignCatalog.FileName)));
        var wallet = new Wallet();
        var campaign = new CampaignSession(catalog, wallet, new ManualClock(1000));
        var gold = wallet.Get(SurvIds.ResSoft);
        var shards = campaign.Roster.Get("hero.lyra")!.Shards;
        var level = Runner.Catalog().First;
        var bundle = RunnerRewards.For(level, new RunnerResult(level.Id, true, 10, 5, 0, 8, 6, level.Length, 120));
        RunnerRewards.Grant(bundle, campaign);
        Assert.Equal(gold + 1500 + 200 + 200, wallet.Get(SurvIds.ResSoft));
        Assert.Equal(shards + 10, campaign.Roster.Get("hero.lyra")!.Shards);
        var json = campaign.Save();
        var again = new CampaignSession(catalog, new Wallet(), new ManualClock(1000));
        Assert.True(again.Load(json));
        Assert.Equal(gold + 1900, again.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(shards + 10, again.Roster.Get("hero.lyra")!.Shards);
    }

    [Fact]
    public void Runner_levels_are_a_sequence_that_gets_harder_level_by_level()
    {
        var c = Runner.Catalog();
        Assert.True(c.Levels.Count >= 10, "levels " + c.Levels.Count);
        Assert.Equal("runner.thornwood_road", c.First.Id);
        for (var i = 0; i < c.Levels.Count; i++)
        {
            var l = c.Levels[i];
            Assert.Equal(i + 1, l.Number);
            Assert.Same(l, c.ByNumber(l.Number));
            Assert.True(l.HasBoss);
            Assert.False(string.IsNullOrWhiteSpace(l.DisplayName));
            if (i == 0)
            {
                continue;
            }

            var p = c.Levels[i - 1];
            // The road grows until the ramp's length cap and the packs until its count cap; past those only HP and damage climb.
            var cap = c.Progression!.Ramp!;
            Assert.True(l.Length > p.Length || (l.Length == cap.LengthCap && p.Length == cap.LengthCap), $"L{l.Number} length {p.Length} -> {l.Length}");
            Assert.True(l.Length <= cap.LengthCap);
            Assert.True(l.Number > cap.CountCap ? l.EnemyCount >= p.EnemyCount : l.EnemyCount > p.EnemyCount, $"L{l.Number} enemies {p.EnemyCount} -> {l.EnemyCount}");
            Assert.True(l.FoeDmgMul >= p.FoeDmgMul, $"L{l.Number} enemy damage");
            Assert.True(l.FoeHpTotal(c) > p.FoeHpTotal(c), $"L{l.Number} enemy hp");
            Assert.True(l.FoeHpMul >= p.FoeHpMul);
            Assert.True(l.BoxCount >= p.BoxCount, $"L{l.Number} boxes {p.BoxCount} -> {l.BoxCount}");
            Assert.True(l.BoxHpTotal > p.BoxHpTotal, $"L{l.Number} box hp");
            Assert.True(l.BossHp(c) > p.BossHp(c), $"L{l.Number} boss hp");
            Assert.True(l.Rewards.WinGold > p.Rewards.WinGold && l.Rewards.GoldPerKill >= p.Rewards.GoldPerKill && l.Rewards.Shards >= p.Rewards.Shards);
            Assert.True(l.Rows.Count(r => r.Box?.Kind == RunnerBoxKind.Followers) >= p.Rows.Count(r => r.Box?.Kind == RunnerBoxKind.Followers));
            Assert.True(l.HazardCount >= p.HazardCount, $"L{l.Number} hazards {p.HazardCount} -> {l.HazardCount}");
        }

        // Traps get more common and more varied: rolling logs from Level 4, and many more by the last level.
        Assert.DoesNotContain(c.ByNumber(3).Rows, r => r.Hazard?.Kind == RunnerHazardKind.Log);
        Assert.Contains(c.Levels[^1].Rows, r => r.Hazard?.Kind == RunnerHazardKind.Log);
        Assert.True(c.Levels[^1].HazardCount >= 3 * c.First.HazardCount, "hazards " + c.Levels[^1].HazardCount);

        // Later levels bring the tougher villains; every rig is still a locked villain fighter.
        Assert.Contains(c.Levels[^1].Rows, r => r.Unit == "vespera");
        Assert.Contains(c.Levels[^1].Rows, r => r.Unit == "ashwyrm");
        Assert.DoesNotContain(c.First.Rows, r => r.Unit == "vespera" || r.Unit == "ashwyrm");
        var names = LockedBattleRoster.All.ToDictionary(f => f.Name, f => f.Side);
        Assert.All(c.Levels.SelectMany(l => l.Rows).Where(r => r.Kind != RunnerRowKind.Box && r.Kind != RunnerRowKind.Hazard), r => Assert.Equal(BattleSide.Villain, names[c.Unit(r.Unit).Roster]));

        // Same seed, same levels.
        var again = Runner.Catalog();
        for (var i = 0; i < c.Levels.Count; i++)
        {
            Assert.Equal(c.Levels[i].Rows, again.Levels[i].Rows);
        }
    }

    [Fact]
    public void Every_level_is_won_by_a_steering_player_later_ones_take_longer_and_hugging_the_edge_loses()
    {
        var c = Runner.Catalog();
        var last = 0f;
        foreach (var level in c.Levels)
        {
            var good = new RunnerRun(c, level, RunnerShipped.Squad(c, level));
            var arrived = -1f;
            while (!good.Over && good.Seconds < 600f)
            {
                good.SteerTo(RunnerBossQuakeTests.DodgingPlayer(good));
                foreach (var e in good.Step(1f / 30f))
                {
                    if (e.Kind == RunnerEventKind.BossEngaged && arrived < 0f)
                    {
                        arrived = good.Seconds;
                    }
                }
            }

            var lazy = new RunnerRun(c, level, RunnerShipped.Squad(c, level));
            Runner.Play(lazy, _ => 3.9f);
            _out.WriteLine($"L{level.Number} {level.DisplayName}: {good.Outcome} {good.Seconds:0.0}s boxes {good.BoxesBroken}/{level.BoxCount} kills {good.Kills} peak {good.FollowersPeak} left {good.Followers}; edge {lazy.Outcome}");
            Assert.Equal(RunnerOutcome.Won, good.Outcome);
            Assert.Equal(RunnerOutcome.Lost, lazy.Outcome);

            // Later levels are longer roads: the squad reaches the boss later each time (the fight itself varies with the squad it arrives with).
            // Once the road hits the ramp's length cap it stops growing (the squad still speeds up to the speed cap),
            // so from there the arrival only has to stay within 10% of the last one.
            var grew = level.Number == 1 || level.Length > c.ByNumber(level.Number - 1).Length;
            Assert.True(grew ? arrived > last : arrived >= 0.9f * last, $"L{level.Number} reaches the boss at {arrived} after {last}");
            last = arrived;
        }
    }

    [Fact]
    public void A_win_opens_the_next_runner_level_and_it_is_kept_in_the_save()
    {
        var levels = Runner.Catalog();
        var n = levels.Levels.Count;
        RunnerResult R(int number, bool won) => new RunnerResult("x", won, 0, 0, 0, 0, 0, 0, 0, number);
        Assert.Equal(1, RunnerProgress.AfterRun(1, R(1, false), n));
        Assert.Equal(2, RunnerProgress.AfterRun(1, R(1, true), n));
        Assert.Equal(5, RunnerProgress.AfterRun(5, R(2, true), n));
        Assert.Equal(n + 1, RunnerProgress.AfterRun(n, R(n, true), n)); // the last level beaten: nothing left to play
        Assert.True(RunnerProgress.IsOpen(3, 3));
        Assert.False(RunnerProgress.IsOpen(3, 4));

        var catalog = CampaignCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CampaignCatalog.FileName)));
        var campaign = new CampaignSession(catalog, new Wallet(), new ManualClock(1000));
        Assert.Equal(1, campaign.RunnerUnlocked);
        var level2 = levels.ByNumber(2);
        var win = RunnerRewards.For(level2, new RunnerResult(level2.Id, true, 3, 1, 0, 2, 1, level2.Length, 90, level2.Number));
        Assert.Equal(2, win.RunnerNumber);
        RunnerRewards.Grant(win, campaign);
        Assert.Equal(3, campaign.RunnerUnlocked);
        var loss = RunnerRewards.For(levels.ByNumber(3), new RunnerResult("x", false, 3, 1, 0, 2, 0, 20, 30, 3));
        RunnerRewards.Grant(loss, campaign);
        Assert.Equal(3, campaign.RunnerUnlocked);
        var json = campaign.Save();
        Assert.Contains("\"runner_unlocked\": 3", json.Replace("\":3", "\": 3"));
        var again = new CampaignSession(catalog, new Wallet(), new ManualClock(1000));
        Assert.True(again.Load(json));
        Assert.Equal(3, again.RunnerUnlocked);

        // An older save without the field opens Level 1 only.
        var old = new CampaignSession(catalog, new Wallet(), new ManualClock(1000));
        Assert.True(old.Load(json.Replace("\"runner_unlocked\"", "\"x_runner\"")));
        Assert.Equal(1, old.RunnerUnlocked);

        // From Play, the handoff knows what is open and a banked win opens the next level for the scene's Next button.
        RunnerHandoff.Reset();
        RunnerHandoff.Begin(levels.ByNumber(3).Id, 3);
        Assert.Equal(3, RunnerHandoff.Unlocked);
        RunnerHandoff.Bank(new RunnerResult("x", true, 0, 0, 0, 0, 0, 0, 0, 3), new RewardBundle { Won = true, RunnerNumber = 3 });
        Assert.Equal(4, RunnerHandoff.Unlocked);
        RunnerHandoff.Reset();
    }

    [Fact]
    public void Rewards_grow_with_the_level()
    {
        var c = Runner.Catalog();
        var result = new RunnerResult("x", true, 20, 8, 0, 10, 5, 100, 90);
        var gold = c.Levels.Select(l => RunnerRewards.For(l, result with { LevelId = l.Id, Number = l.Number }).Resources[SurvIds.ResSoft]).ToList();
        for (var i = 1; i < gold.Count; i++)
        {
            Assert.True(gold[i] > gold[i - 1], $"L{i + 1} gold {gold[i]} after {gold[i - 1]}");
        }
    }

    [Fact]
    public void Handoff_banks_every_run_including_retries_and_pays_once()
    {
        RunnerHandoff.Reset();
        Assert.False(RunnerHandoff.FromPlay);
        RunnerHandoff.Begin("runner.thornwood_road");
        Assert.True(RunnerHandoff.FromPlay);
        var finished = 0;
        RunnerHandoff.Finished += () => finished++;
        RunnerHandoff.Bank(new RunnerResult("x", false, 1, 0, 0, 0, 0, 5, 5), new RewardBundle());
        RunnerHandoff.Bank(new RunnerResult("x", true, 9, 3, 0, 4, 4, 150, 99), new RewardBundle { Won = true });
        RunnerHandoff.Finish();
        Assert.Equal(1, finished);
        var runs = RunnerHandoff.Take();
        Assert.Equal(2, runs.Count);
        Assert.Empty(RunnerHandoff.Runs);
        Assert.False(RunnerHandoff.FromPlay);
        RunnerHandoff.Reset();
    }

    [Fact]
    public void Bad_level_json_is_rejected_with_a_clear_message()
    {
        Assert.Throws<KeyNotFoundException>(() => Runner.Tiny(@"{ ""at"": 5, ""kind"": ""enemy"", ""unit"": ""nobody"" }"));
        Assert.Throws<FormatException>(() => Runner.Tiny(@"{ ""at"": 5, ""kind"": ""box"", ""hp"": 0, ""reward"": ""weapon"" }"));
        Assert.Throws<FormatException>(() => Runner.Tiny(@"{ ""at"": 5, ""kind"": ""box"", ""hp"": 5, ""reward"": ""coins"" }"));
        Assert.Throws<FormatException>(() => Runner.Tiny(@"{ ""at"": 500, ""kind"": ""enemy"", ""unit"": ""wolf"" }"));
    }

    [Fact]
    public void Runner_painted_art_strings_and_hud_slot_are_in_the_pack()
    {
        var pack = TestPaths.LoadPack();
        foreach (var key in RunnerArtKeys.Required.Append(RunnerArtKeys.ChestHero).Append(RunnerArtKeys.ChestOpen))
        {
            Assert.True(pack.Art.ContainsKey(key), key);
            Assert.True(File.Exists(Path.Combine(RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", pack.Art[key])), pack.Art[key]);
        }

        var c = Runner.Catalog();
        foreach (var u in c.Units.Values.Where(u => u.HeroId.Length > 0))
        {
            Assert.True(pack.Art.ContainsKey(RunnerArtKeys.Portrait(u.Roster)), u.Roster);
        }

        foreach (var key in new[] { c.First.Title, "runner.button", "runner.victory", "runner.defeat", "runner.retry", "runner.continue", "runner.boss", "runner.box.weapon", "runner.box.rate", "runner.box.hero", "runner.result.kills" })
        {
            Assert.True(pack.Strings.ContainsKey(key), key);
        }

        Assert.True(pack.Hud.ContainsKey("runner_button"));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !(Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }
}
