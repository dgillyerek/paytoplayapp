using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

internal static class Camp
{
    public static CampaignCatalog Catalog() =>
        CampaignCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CampaignCatalog.FileName)));

    public static CampaignSession Session(ManualClock clock, int gold = 1_000_000, int food = 1_000_000, int gems = 0) =>
        new(Catalog(), new Wallet(new Dictionary<string, int> { [SurvIds.ResSoft] = gold, [SurvIds.ResFood] = food, [SurvIds.ResHard] = gems }), clock);
}

public sealed class CampaignCatalogTests
{
    [Fact]
    public void Every_battle_character_is_a_BattleLocked_roster_fighter_on_its_own_side()
    {
        var catalog = Camp.Catalog();
        Assert.Equal(LockedBattleRoster.All.Count, catalog.Heroes.Count);
        foreach (var f in LockedBattleRoster.All)
        {
            var def = catalog.ByFighter(f.Name);
            Assert.NotNull(def);
            Assert.Equal(f.Side == BattleSide.Hero ? CampaignSide.Hero : CampaignSide.Villain, def!.Side);
        }
    }

    [Fact]
    public void Three_heroes_start_owned_and_the_other_three_are_for_sale()
    {
        var catalog = Camp.Catalog();
        var owned = catalog.OnSide(CampaignSide.Hero).Where(h => h.StartOwned).Select(h => h.Fighter).ToList();
        Assert.Equal(new[] { "Sir Aldric", "Lyra", "Rowan" }, owned);
        foreach (var h in catalog.OnSide(CampaignSide.Hero).Where(h => !h.StartOwned))
        {
            Assert.True(h.UnlockGems > 0 && h.UnlockShards > 0, h.Id);
        }
    }

    [Fact]
    public void Stages_use_villains_in_up_to_three_lanes_and_shards_of_buyable_heroes()
    {
        var catalog = Camp.Catalog();
        Assert.Equal(6, catalog.Stages.Count);
        foreach (var s in catalog.Stages)
        {
            Assert.InRange(s.Enemies.Count, 1, BattleSetup.MaxLanes);
            Assert.Equal(s.Enemies.Count, s.Enemies.Select(e => e.HeroId).Distinct().Count());
            foreach (var e in s.Enemies)
            {
                Assert.Equal(CampaignSide.Villain, catalog.Hero(e.HeroId)!.Side);
            }

            Assert.False(catalog.Hero(s.ShardHero)!.StartOwned);
            Assert.True(s.Rewards[SurvIds.ResSoft] > 0);
        }
    }

    [Fact]
    public void Camps_turn_into_stages_by_type_and_level()
    {
        var catalog = Camp.Catalog();
        var goblin = catalog.CampStage("camp.goblin_01", "goblin", 3, 1200, "Goblin Warband");
        Assert.Equal("camp:camp.goblin_01", goblin.Id);
        Assert.Single(goblin.Enemies);
        Assert.Equal(1200, goblin.Rewards[SurvIds.ResSoft]);
        var dragon = catalog.CampStage("camp.dragon_01", "dragon", 20, 12000, "Dragon Lair");
        Assert.Equal(3, dragon.Enemies.Count);
        Assert.Equal("foe.ashwyrm", dragon.Enemies[0].HeroId);
        Assert.Equal(12, dragon.Enemies[0].Level);
    }

    [Fact]
    public void Every_campaign_art_key_is_bound_in_pack_json_and_ships()
    {
        var pack = TestPaths.LoadPack();
        foreach (var key in CampaignArtKeys.All(Camp.Catalog()))
        {
            Assert.True(pack.TryArt(key, out var rel), key + " missing from pack.json art");
            Assert.True(File.Exists(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", rel)), key + " → " + rel);
        }
    }
}

public sealed class HeroRosterTests
{
    [Fact]
    public void Stats_rise_every_level_and_with_stars()
    {
        var c = Camp.Catalog();
        var aldric = c.Hero("hero.sir_aldric")!;
        var prev = HeroStats.Of(aldric, c.Curve, 1, 1);
        for (var level = 2; level <= 10; level++)
        {
            var s = HeroStats.Of(aldric, c.Curve, level, 1);
            Assert.True(s.Hp > prev.Hp && s.Atk > prev.Atk && s.Power > prev.Power);
            prev = s;
        }

        Assert.True(HeroStats.Of(aldric, c.Curve, 10, 2).Power > HeroStats.Of(aldric, c.Curve, 10, 1).Power);
    }

    [Fact]
    public void Level_up_costs_gold_and_food_runs_a_timer_then_levels()
    {
        var clock = new ManualClock(1000);
        var s = Camp.Session(clock);
        var r = s.Roster;
        var gold = s.Wallet.Get(SurvIds.ResSoft);
        var cost = r.LevelUpCost("hero.lyra");
        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.lyra"));
        Assert.Equal(gold - cost[SurvIds.ResSoft], s.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(LevelUpCheck.Training, r.CanLevelUp("hero.lyra"));
        Assert.Equal(30, r.LevelUpSeconds("hero.lyra"));
        var levelled = new List<(string, int)>();
        r.LevelledUp += (id, lv) => levelled.Add((id, lv));
        clock.Advance(29);
        Assert.Equal(0, r.Tick());
        clock.Advance(1);
        Assert.Equal(1, r.Tick());
        Assert.Equal(2, r.Get("hero.lyra")!.Level);
        Assert.Equal(("hero.lyra", 2), levelled.Single());
        Assert.True(r.LevelUpSeconds("hero.lyra") > 30);
    }

    [Fact]
    public void Two_training_slots_and_broke_players_cannot_train()
    {
        var clock = new ManualClock();
        var s = Camp.Session(clock);
        Assert.Equal(LevelUpCheck.Ok, s.Roster.TryStartLevelUp("hero.lyra"));
        Assert.Equal(LevelUpCheck.Ok, s.Roster.TryStartLevelUp("hero.rowan"));
        Assert.Equal(LevelUpCheck.NoSlot, s.Roster.CanLevelUp("hero.sir_aldric"));
        Assert.Equal(LevelUpCheck.NotOwned, s.Roster.CanLevelUp("hero.emberfang"));

        var poor = Camp.Session(new ManualClock(), gold: 10, food: 10);
        Assert.Equal(LevelUpCheck.CantAfford, poor.Roster.TryStartLevelUp("hero.lyra"));
        Assert.Equal(10, poor.Wallet.Get(SurvIds.ResSoft));
    }

    [Fact]
    public void Free_finish_inside_the_window_and_gem_rush_outside_it()
    {
        var clock = new ManualClock();
        var s = Camp.Session(clock, gems: 100);
        var r = s.Roster;
        r.Get("hero.lyra")!.Level = 8; // 30 * 1.6^7 ≈ 805 s, outside the 300 s free window
        r.TryStartLevelUp("hero.lyra");
        Assert.False(r.CanFinishFree("hero.lyra"));
        var cost = r.RushCost("hero.lyra");
        Assert.True(cost > 0);
        Assert.True(r.TryRush("hero.lyra", out var spent));
        Assert.Equal(cost, spent);
        Assert.Equal(100 - cost, s.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(9, r.Get("hero.lyra")!.Level);

        r.TryStartLevelUp("hero.rowan");
        Assert.True(r.CanFinishFree("hero.rowan"));
        Assert.Equal(0, r.RushCost("hero.rowan"));
        Assert.True(r.TryFinishFree("hero.rowan"));
        Assert.Equal(2, r.Get("hero.rowan")!.Level);
    }

    [Fact]
    public void Star_cap_stops_training_until_shards_star_the_hero_up()
    {
        var s = Camp.Session(new ManualClock());
        var r = s.Roster;
        r.Get("hero.rowan")!.Level = 10;
        Assert.Equal(LevelUpCheck.AtStarCap, r.CanLevelUp("hero.rowan"));
        Assert.False(r.TryStarUp("hero.rowan"));
        r.AddShards("hero.rowan", 10);
        Assert.True(r.TryStarUp("hero.rowan"));
        Assert.Equal(2, r.Get("hero.rowan")!.Stars);
        Assert.Equal(0, r.Get("hero.rowan")!.Shards);
        Assert.Equal(20, r.LevelCap("hero.rowan"));
        Assert.Equal(LevelUpCheck.Ok, r.CanLevelUp("hero.rowan"));
    }

    [Fact]
    public void Locked_heroes_unlock_with_gems_or_shards()
    {
        var s = Camp.Session(new ManualClock(), gems: 350);
        var r = s.Roster;
        var unlocked = new List<string>();
        r.Unlocked += unlocked.Add;
        Assert.Equal(UnlockCheck.CantAfford, r.TryUnlockWithGems("hero.stormcrest"));
        Assert.Equal(UnlockCheck.Ok, r.TryUnlockWithGems("hero.oakenshield"));
        Assert.Equal(50, s.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(UnlockCheck.AlreadyOwned, r.TryUnlockWithGems("hero.oakenshield"));
        r.AddShards("hero.stormcrest", 50);
        Assert.Equal(UnlockCheck.Ok, r.TryUnlockWithShards("hero.stormcrest"));
        Assert.Equal(new[] { "hero.oakenshield", "hero.stormcrest" }, unlocked);
        Assert.Equal(UnlockCheck.Unknown, r.TryUnlockWithGems("foe.ashwyrm"));
    }
}

public sealed class BattleSetupTests
{
    [Fact]
    public void Auto_fill_puts_owned_heroes_in_every_lane_and_compares_power()
    {
        var s = Camp.Session(new ManualClock());
        var setup = s.Setup(s.Catalog.Stage("stage.4")!);
        Assert.Equal(3, setup.LaneCount);
        Assert.Equal(3, setup.AssignedCount);
        Assert.True(setup.HeroPower > 0 && setup.EnemyPower > 0);
        var lineup = setup.Build();
        Assert.Equal(3, lineup.Heroes.Count);
        Assert.Equal(3, lineup.Enemies.Count);
        Assert.All(lineup.Heroes, h => Assert.Equal(CampaignSide.Hero, h.Side));
        Assert.All(lineup.Enemies, e => Assert.Equal(CampaignSide.Villain, e.Side));
        Assert.Equal(setup.HeroPower, lineup.HeroPower);
    }

    [Fact]
    public void A_hero_stands_in_one_lane_and_locked_heroes_cannot_be_placed()
    {
        var s = Camp.Session(new ManualClock());
        var setup = new BattleSetup(s.Catalog, s.Roster, s.Catalog.Stage("stage.4")!);
        Assert.Equal(SetupCheck.NoHeroes, setup.CanFight);
        Assert.True(setup.Assign(0, "hero.lyra"));
        Assert.True(setup.Assign(2, "hero.lyra"));
        Assert.Null(setup.HeroAt(0));
        Assert.Equal("hero.lyra", setup.HeroAt(2));
        Assert.False(setup.Assign(1, "hero.emberfang"));
        Assert.Equal(0, setup.Toggle("hero.rowan"));
        Assert.Equal(-1, setup.Toggle("hero.rowan"));
        Assert.Equal(SetupCheck.Ok, setup.CanFight);
    }

    [Fact]
    public void Levels_scale_the_lineup_stats()
    {
        var s = Camp.Session(new ManualClock());
        var stage = s.Catalog.Stage("stage.1")!;
        var before = s.Setup(stage).Build().Heroes.Single();
        s.Roster.Get(before.CharacterId)!.Level = 6;
        var after = s.Setup(stage).Build().Heroes.Single();
        Assert.Equal(6, after.Level);
        Assert.True(after.Hp > before.Hp && after.Atk > before.Atk);
    }

    [Fact]
    public void Stages_open_in_order_and_camps_are_always_open()
    {
        var s = Camp.Session(new ManualClock());
        Assert.True(s.IsUnlocked("stage.1"));
        Assert.False(s.IsUnlocked("stage.2"));
        Assert.True(s.IsUnlocked("camp:camp.goblin_01"));
        Assert.Equal(SetupCheck.StageLocked, s.Setup(s.Catalog.Stage("stage.2")!).CanFight);
    }

    [Fact]
    public void Odds_follow_the_power_ratio()
    {
        Assert.Equal(BattleOdds.Easy, BattleSetup.OddsOf(1300, 1000));
        Assert.Equal(BattleOdds.Even, BattleSetup.OddsOf(1000, 1000));
        Assert.Equal(BattleOdds.Hard, BattleSetup.OddsOf(700, 1000));
        Assert.Equal(BattleOdds.VeryHard, BattleSetup.OddsOf(0, 1000));
    }

    [Fact]
    public void Lane_layout_centres_wide_dragon_lanes()
    {
        var c = BattleLaneLayout.Centres(new[] { BattleLaneLayout.LaneWidth(1f), BattleLaneLayout.LaneWidth(2f), BattleLaneLayout.LaneWidth(1f) });
        Assert.Equal(0f, c[1], 3);
        Assert.Equal(-c[0], c[2], 3);
        Assert.True(c[2] - c[1] > BattleLaneLayout.BaseLaneMetres);
    }
}

public sealed class BattleCombatTests
{
    private static BattleLineup Lineup(string stage = "stage.2")
    {
        var s = Camp.Session(new ManualClock());
        return s.Setup(s.Catalog.Stage(stage)!).Build();
    }

    [Fact]
    public void Hits_split_attack_across_combo_hits_and_heavy_hits_hit_harder()
    {
        Assert.Equal(80, BattleCombat.DamageOf(80, 1, false));
        Assert.Equal(20, BattleCombat.DamageOf(80, 4, false));
        Assert.Equal(108, BattleCombat.DamageOf(80, 1, true));
        Assert.Equal(1, BattleCombat.DamageOf(1, 5, false));
    }

    [Fact]
    public void Wiping_the_villains_wins_and_reports_the_result()
    {
        var combat = new BattleCombat(Lineup());
        var hero = combat.Units.First(u => u.Side == CampaignSide.Hero);
        var down = new List<CombatUnit>();
        combat.Defeated += down.Add;
        foreach (var foe in combat.Units.Where(u => u.Side == CampaignSide.Villain).ToList())
        {
            while (foe.Alive)
            {
                Assert.True(combat.Hit(hero, foe, 1, false) > 0);
            }
        }

        Assert.Equal(BattleOutcome.Won, combat.Outcome);
        Assert.Equal(2, down.Count);
        Assert.Equal(0, combat.Hit(hero, down[0], 1, false));
        var r = combat.Result();
        Assert.True(r.Won);
        Assert.Equal("stage.2", r.StageId);
        Assert.Equal(2, r.EnemiesDefeated);
    }

    [Fact]
    public void Timeout_goes_to_the_side_with_more_hp_left_and_retreat_loses()
    {
        var combat = new BattleCombat(Lineup());
        var hero = combat.Units.First(u => u.Side == CampaignSide.Hero);
        var foe = combat.Units.First(u => u.Side == CampaignSide.Villain);
        combat.Hit(hero, foe, 1, true);
        combat.Tick(119);
        Assert.Equal(BattleOutcome.Ongoing, combat.Outcome);
        combat.Tick(2);
        Assert.Equal(BattleOutcome.Won, combat.Outcome);
        Assert.True(combat.Result().TimedOut);

        var other = new BattleCombat(Lineup());
        other.Retreat();
        Assert.Equal(BattleOutcome.Lost, other.Outcome);
        Assert.True(other.Result().Retreated);
    }

    [Fact]
    public void Nearest_foe_retargets_to_standing_opponents_only()
    {
        var combat = new BattleCombat(Lineup());
        var hero = combat.Units.First(u => u.Side == CampaignSide.Hero && u.Fighter.Lane == 0);
        (float, float) Pos(CombatUnit u) => (u.Fighter.Lane * 3f, u.Side == CampaignSide.Hero ? -4f : 4f);
        var first = combat.NearestFoe(hero, Pos)!;
        Assert.Equal(0, first.Fighter.Lane);
        var killer = combat.Units.First(u => u.Side == CampaignSide.Hero);
        while (first.Alive)
        {
            combat.Hit(killer, first, 1, false);
        }

        Assert.Equal(1, combat.NearestFoe(hero, Pos)!.Fighter.Lane);
    }

    [Fact]
    public void Handoff_carries_the_lineup_and_the_result_back()
    {
        BattleHandoff.Reset();
        Assert.Null(BattleHandoff.Pending);
        var lineup = Lineup();
        BattleHandoff.Begin(lineup);
        Assert.Same(lineup, BattleHandoff.Pending);
        BattleReport? got = null;
        BattleHandoff.Completed += r => got = r;
        BattleHandoff.Complete(new BattleReport("stage.2", null, true, 30, 2, 2));
        Assert.Null(BattleHandoff.Pending);
        Assert.True(got!.Won);
        Assert.True(BattleHandoff.TakeResult()!.Won);
        Assert.Null(BattleHandoff.TakeResult());
        BattleHandoff.Reset();
    }
}

public sealed class CampaignRewardsAndSaveTests
{
    [Fact]
    public void A_win_pays_rewards_shards_and_first_clear_gems_once()
    {
        var s = Camp.Session(new ManualClock(), gold: 0, food: 0);
        var stage = s.Catalog.Stage("stage.1")!;
        var b = s.ApplyResult(new BattleReport(stage.Id, null, true, 20, 1, 1), stage);
        Assert.True(b.FirstClear);
        Assert.Equal(1500, s.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(30, s.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(6, s.Roster.Get("hero.oakenshield")!.Shards);
        Assert.Equal(6, b.AsRows()[RewardBundle.ShardKey("hero.oakenshield")]);
        Assert.True(s.IsCleared("stage.1"));
        Assert.True(s.IsUnlocked("stage.2"));

        var again = s.ApplyResult(new BattleReport(stage.Id, null, true, 20, 1, 1), stage);
        Assert.False(again.FirstClear);
        Assert.Equal(30, s.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(2, s.Wins);
    }

    [Fact]
    public void A_loss_pays_nothing_at_all()
    {
        var s = Camp.Session(new ManualClock(), gold: 0, food: 0);
        var stage = s.Catalog.Stage("stage.2")!;
        var b = s.ApplyResult(new BattleReport(stage.Id, null, false, 60, 0, 1), stage);
        Assert.False(b.Won);
        // Derek 2026-10-28: "there should be no bonuses when you are defeated in the battle".
        Assert.Empty(b.Resources);
        Assert.Empty(b.AsRows());
        Assert.False(b.FirstClear);
        Assert.Equal(0, s.Wallet.Get(SurvIds.ResSoft));
        Assert.Equal(0, s.Wallet.Get(SurvIds.ResFood));
        Assert.Equal(0, s.Wallet.Get("res.hard"));
        Assert.Equal(0, b.Shards);
        Assert.False(s.IsCleared("stage.2"));
        Assert.Equal(1, s.Battles);
        Assert.Equal(0, s.Wins);
    }

    [Fact]
    public void Bounties_progress_with_battles_and_pay_on_claim()
    {
        var s = Camp.Session(new ManualClock(), gems: 0);
        Assert.Equal("bounty.win_1", s.CurrentBounty!.Id);
        Assert.False(s.BountyReady);
        Assert.Null(s.ClaimBounty());
        var stage = s.Catalog.Stage("stage.1")!;
        s.ApplyResult(new BattleReport(stage.Id, null, true, 20, 1, 1), stage);
        Assert.True(s.BountyReady);
        var gems = s.Wallet.Get(SurvIds.ResHard);
        Assert.Equal(25, s.ClaimBounty()![SurvIds.ResHard]);
        Assert.Equal(gems + 25, s.Wallet.Get(SurvIds.ResHard));
        Assert.Equal("bounty.clear_2", s.CurrentBounty!.Id);
    }

    [Fact]
    public void Save_and_load_round_trips_heroes_timers_stages_and_wallet()
    {
        var clock = new ManualClock(5000);
        var s = Camp.Session(clock, gems: 400);
        s.Roster.TryUnlockWithGems("hero.oakenshield");
        s.Roster.Get("hero.lyra")!.Level = 4;
        s.Roster.AddShards("hero.stormcrest", 12);
        s.Roster.TryStartLevelUp("hero.rowan");
        var stage = s.Catalog.Stage("stage.1")!;
        s.ApplyResult(new BattleReport(stage.Id, null, true, 20, 1, 1), stage);
        s.ClaimBounty();
        var json = s.Save();

        clock.Advance(10);
        var t = Camp.Session(clock, gold: 1, food: 1, gems: 1);
        Assert.True(t.Load(json));
        Assert.True(t.Roster.IsOwned("hero.oakenshield"));
        Assert.Equal(4, t.Roster.Get("hero.lyra")!.Level);
        Assert.Equal(12, t.Roster.Get("hero.stormcrest")!.Shards);
        Assert.Equal(s.Wallet.Get(SurvIds.ResHard), t.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(s.Wallet.Get(SurvIds.ResSoft), t.Wallet.Get(SurvIds.ResSoft));
        Assert.True(t.IsCleared("stage.1"));
        Assert.Equal(1, t.Wins);
        Assert.Equal(1, t.BountyIndex);
        var timer = t.Roster.TrainingOf("hero.rowan")!;
        Assert.Equal(20, timer.Remaining(clock.Now), 3);
        clock.Advance(20);
        Assert.Equal(1, t.Tick());
        Assert.Equal(2, t.Roster.Get("hero.rowan")!.Level);
        Assert.True(t.Load(t.Save()));
        Assert.Equal(2, t.Roster.Get("hero.rowan")!.Level);
    }

    [Fact]
    public void A_broken_save_changes_nothing()
    {
        var s = Camp.Session(new ManualClock(), gems: 7);
        Assert.False(s.Load("{ not json"));
        Assert.Equal(7, s.Wallet.Get(SurvIds.ResHard));
        Assert.True(s.Roster.IsOwned("hero.lyra"));
    }

    [Fact]
    public void Writer_escapes_strings()
    {
        var s = Camp.Session(new ManualClock());
        var json = s.Save();
        Assert.StartsWith("{\"version\":1,", json);
        Assert.Contains("\"hero.sir_aldric\":{\"owned\":true,\"level\":1,\"stars\":1,\"shards\":0}", json);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(6, doc.RootElement.GetProperty("heroes").EnumerateObject().Count());
    }

    [Fact]
    public void Theme_a_strings_name_every_campaign_character_and_screen_key()
    {
        var root = Layouts.RepoRoot();
        var strings = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "strings", "en.json"))).RootElement;
        var catalog = CampaignCatalog.Parse(File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Resources", "Survival", CampaignCatalog.FileName)));
        foreach (var h in catalog.Heroes)
        {
            Assert.True(strings.TryGetProperty(h.Id + ".name", out _), h.Id + ".name");
        }

        var view = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SurvivalCampaignView.cs"));
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(view, "(?:S|Fmt)\\(\"(campaign\\.[^\"]+)\""))
        {
            Assert.True(strings.TryGetProperty(m.Groups[1].Value, out _), m.Groups[1].Value);
        }
    }

    [Fact]
    public void Battle_button_slot_sits_clear_of_the_other_hud_slots()
    {
        var hud = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "ui", "hud_layout.json"))).RootElement;
        Assert.True(hud.TryGetProperty("battle_button", out var b));
        double V(System.Text.Json.JsonElement e, string k) => e.GetProperty(k).GetDouble();
        foreach (var slot in hud.EnumerateObject())
        {
            if (slot.Name is "battle_button" or "map_area")
            {
                continue;
            }

            var o = slot.Value;
            var overlap = V(b, "x_min") < V(o, "x_max") && V(o, "x_min") < V(b, "x_max") && V(b, "y_top_min") < V(o, "y_top_max") && V(o, "y_top_min") < V(b, "y_top_max");
            Assert.False(overlap, "battle_button overlaps " + slot.Name);
        }

        Assert.True(hud.TryGetProperty("hero_free", out var f));
        foreach (var slot in hud.EnumerateObject())
        {
            if (slot.Name is "hero_free" or "map_area")
            {
                continue;
            }

            var o = slot.Value;
            var overlap = V(f, "x_min") < V(o, "x_max") && V(o, "x_min") < V(f, "x_max") && V(f, "y_top_min") < V(o, "y_top_max") && V(o, "y_top_min") < V(f, "y_top_max");
            Assert.False(overlap, "hero_free overlaps " + slot.Name);
        }
    }
}
