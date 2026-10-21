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

    public static RunnerCatalog Tiny(string rows, string extra = "", float length = 40f) => RunnerCatalog.Parse(@"{
  ""units"": {
    ""hero"": { ""roster"": ""Lyra"", ""hp"": 100, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""archer"": { ""roster"": ""Rowan"", ""hp"": 1, ""damage"": 10, ""cycle"": 0.5, ""range"": 10, ""ranged"": true, ""shot_speed"": 20 },
    ""knight"": { ""roster"": ""Sir Aldric"", ""hp"": 1, ""damage"": 30, ""cycle"": 1.0, ""range"": 2.0 },
    ""wolf"": { ""roster"": ""Nightfang"", ""hp"": 20, ""damage"": 40, ""cycle"": 1.0, ""range"": 1.2, ""speed"": 2.0, ""radius"": 0.6 },
    ""brute"": { ""roster"": ""Blightroot"", ""hp"": 100000, ""damage"": 40, ""cycle"": 1.0, ""range"": 1.5, ""speed"": 2.0, ""radius"": 0.6 },
    ""boss"": { ""roster"": ""Ironhowl"", ""hp"": 450, ""damage"": 4, ""cycle"": 1.0, ""range"": 1.8, ""radius"": 1.0 }
  },
  ""levels"": [ { ""id"": ""t"", ""hero"": ""hero"", ""length"": " + length.ToString(System.Globalization.CultureInfo.InvariantCulture) + @", ""speed"": 2, ""half_width"": 4, ""max_followers"": 6, " + extra + @"
    ""rewards"": { ""win_gold"": 100, ""gold_per_kill"": 5, ""gold_per_box"": 10, ""shards"": 3, ""shard_hero"": ""hero.lyra"" },
    ""rows"": [ " + rows + @" ] } ] }");

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
        Assert.True(level.EnemyCount >= 20, "enemies " + level.EnemyCount);
        Assert.True(level.BoxCount >= 8, "boxes " + level.BoxCount);
        foreach (var kind in Enum.GetValues<RunnerBoxKind>())
        {
            Assert.Contains(level.Rows, r => r.Box?.Kind == kind);
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
            if (row.Kind != RunnerRowKind.Box)
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
            { ""at"": 38, ""kind"": ""box"", ""x"": 3, ""hp"": 9999, ""reward"": ""rate"" }"));
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
    public void Enemy_contact_holds_the_squad_kills_followers_first_then_hurts_the_hero_and_ends_the_run()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 8, ""kind"": ""box"", ""hp"": 10, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 2 },
            { ""at"": 30, ""kind"": ""enemy"", ""unit"": ""brute"" }"));
        var events = Runner.Play(run);
        var lost = events.Where(e => e.Kind == RunnerEventKind.FollowerLost).ToList();
        var firstHeroHit = events.FindIndex(e => e.Kind == RunnerEventKind.HeroHit);
        Assert.Equal(2, lost.Count);
        Assert.True(firstHeroHit > events.LastIndexOf(lost[^1]), "followers die before the hero is touched");
        Assert.All(lost, e => Assert.True(e.Big));
        Assert.Equal(RunnerOutcome.Lost, run.Outcome);
        Assert.Equal(0, run.Hero.Hp);
        Assert.True(run.Distance < 30f, "contact held the squad at " + run.Distance);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.Lost);
        Assert.False(run.Result.Won);
        Assert.Equal(2, run.Result.FollowersPeak);
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
    public void Killing_the_boss_wins_and_the_boss_blocks_the_whole_road()
    {
        var run = Runner.Run(Runner.Tiny(@"{ ""at"": 12, ""kind"": ""enemy"", ""unit"": ""wolf"", ""count"": 2, ""spread"": 2 },
            { ""at"": 36, ""kind"": ""boss"", ""x"": 0, ""unit"": ""boss"" }"));
        var events = Runner.Play(run, _ => 3.9f);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.BossEngaged);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.FoeKilled && e.Big);
        Assert.Equal(RunnerOutcome.Won, run.Outcome);
        Assert.True(run.Distance < 36f);
        Assert.Contains(events, e => e.Kind == RunnerEventKind.Hit && e.Big);
        Assert.DoesNotContain(run.Foes, f => f.Boss);
        Assert.True(run.Kills >= 2);
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
            { ""at"": 30, ""kind"": ""enemy"", ""unit"": ""wolf"", ""count"": 3, ""spread"": 1 }"));
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
        Runner.Play(good, Runner.AimAtBoxes);
        _out.WriteLine($"aim: {good.Outcome} t={good.Seconds:0} kills={good.Kills} boxes={good.BoxesBroken}/{c.First.BoxCount} peak={good.FollowersPeak} left={good.Followers} hp={good.Hero.Hp}");
        Assert.Equal(RunnerOutcome.Won, good.Outcome);
        Assert.True(good.BoxesBroken >= c.First.BoxCount - 2);
        Assert.True(good.FollowersPeak >= 8);
        Assert.InRange(good.Seconds, 80f, 240f);

        var lazy = new RunnerRun(c, c.First);
        Runner.Play(lazy, _ => 3.9f);
        _out.WriteLine($"edge: {lazy.Outcome} t={lazy.Seconds:0} kills={lazy.Kills} boxes={lazy.BoxesBroken} peak={lazy.FollowersPeak} hp={lazy.Hero.Hp}");
        Assert.Equal(RunnerOutcome.Lost, lazy.Outcome);
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
