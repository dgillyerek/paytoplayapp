using System.Text;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-11-02: "we need many more battle levels. it should progress with the rest of the game. stronger foes etc."</summary>
public sealed class BattleCampaignTests
{
    private static BattleCampaignDef Def => Camp.Catalog().BattleCampaign!;

    private static int FoePower(CampaignCatalog c, StageDef s) =>
        s.Enemies.Sum(e => HeroStats.Of(c.Hero(e.HeroId)!, c.Curve, e.Level, e.Stars).Power);

    private static void Win(CampaignSession s, StageDef st) => s.ApplyResult(new BattleReport(st.Id, null, true, 20, 1, 1), st);

    [Fact]
    public void There_are_over_a_hundred_stages_in_chapters_after_the_prologue()
    {
        var c = Camp.Catalog();
        Assert.Equal(6, c.PrologueStages.Count);
        Assert.True(c.Stages.Count(s => s.InCampaign) >= 100);
        Assert.Equal(c.Stages.Count, c.Stages.Select(s => s.Id).Distinct().Count());
        for (var ch = 1; ch <= Def.Chapters.Count; ch++)
        {
            Assert.Equal(Def.StagesPerChapter, c.StagesIn(ch).Count);
        }
    }

    [Fact]
    public void Foes_get_stronger_every_stage_and_never_weaker()
    {
        var c = Camp.Catalog();
        var stages = c.Stages.Where(s => s.InCampaign && !s.Boss).ToList();
        var lastLevel = 0;
        foreach (var s in stages)
        {
            var lv = s.Enemies.Min(e => e.Level);
            Assert.True(lv >= lastLevel, s.Id + " has lower-level foes than the stage before it");
            lastLevel = lv;
        }

        var lastAvg = 0.0;
        for (var ch = 1; ch <= Def.Chapters.Count; ch++)
        {
            var avg = c.StagesIn(ch).Average(s => FoePower(c, s));
            Assert.True(avg > lastAvg, "chapter " + ch + " is not harder than the one before");
            lastAvg = avg;
        }

        Assert.True(FoePower(c, stages[^1]) > 10 * FoePower(c, stages[0]), $"{FoePower(c, stages[0])} -> {FoePower(c, stages[^1])}");
        var prologueBoss = FoePower(c, c.Stage("stage.6")!);
        Assert.True(FoePower(c, stages[0]) >= prologueBoss * 0.5, $"chapter 1 ({FoePower(c, stages[0])}) should follow on from Ashwyrm's Lair ({prologueBoss})");
    }

    [Fact]
    public void Levels_and_stars_rise_to_the_hero_cap_and_stay_within_three_lanes()
    {
        var c = Camp.Catalog();
        foreach (var s in c.Stages.Where(s => s.InCampaign))
        {
            Assert.InRange(s.Enemies.Count, 1, BattleSetup.MaxLanes);
            Assert.Equal(s.Enemies.Count, s.Enemies.Select(e => e.HeroId).Distinct().Count());
            foreach (var e in s.Enemies)
            {
                Assert.Equal(CampaignSide.Villain, c.Hero(e.HeroId)!.Side);
                Assert.InRange(e.Level, 1, 52);
                Assert.InRange(e.Stars, 1, 5);
            }
        }

        var final = c.Stages.Last();
        Assert.True(final.Boss);
        Assert.Contains(final.Enemies, e => e.HeroId == "foe.ashwyrm" && e.Level >= 50 && e.Stars == 5);
    }

    [Fact]
    public void Every_fifth_stage_is_a_boss_with_its_chapter_villain_and_better_pay()
    {
        var c = Camp.Catalog();
        for (var ch = 1; ch <= Def.Chapters.Count; ch++)
        {
            var st = c.StagesIn(ch);
            for (var i = 0; i < st.Count; i++)
            {
                Assert.Equal((i + 1) % Def.BossEvery == 0, st[i].Boss);
            }

            var boss = st[Def.BossEvery - 1];
            Assert.Contains(boss.Enemies, e => e.HeroId == Def.Chapters[ch - 1].Boss);
            Assert.True(boss.Rewards[SurvIds.ResSoft] > st[Def.BossEvery - 2].Rewards[SurvIds.ResSoft]);
            Assert.True(boss.FirstClearGems > st[0].FirstClearGems);
            Assert.False(c.Hero(boss.ShardHero)!.StartOwned);
        }
    }

    [Fact]
    public void Ashwyrm_joins_the_villain_mix_by_the_middle_chapters()
    {
        var c = Camp.Catalog();
        Assert.DoesNotContain(c.StagesIn(1).Where(s => !s.Boss).SelectMany(s => s.Enemies), e => e.HeroId == "foe.ashwyrm");
        Assert.Contains(c.StagesIn(4).SelectMany(s => s.Enemies), e => e.HeroId == "foe.ashwyrm");
    }

    [Fact]
    public void Rewards_feed_gold_food_wood_and_stone_and_grow()
    {
        var c = Camp.Catalog();
        var a = c.StagesIn(1)[0];
        var b = c.StagesIn(Def.Chapters.Count)[0];
        foreach (var k in new[] { SurvIds.ResSoft, SurvIds.ResFood, "res.wood", "res.stone" })
        {
            Assert.True(a.Rewards.ContainsKey(k), k);
            Assert.True(b.Rewards[k] > a.Rewards[k], k);
        }
    }

    [Fact]
    public void Chapters_wait_for_the_castle_and_rising_gates()
    {
        var c = Camp.Catalog();
        var gate = 0;
        foreach (var ch in Def.Chapters)
        {
            Assert.True(ch.CastleLevel >= gate);
            gate = ch.CastleLevel;
        }

        var castle = 1;
        var s = Camp.Session(new ManualClock());
        s.CastleLevel = () => castle;
        foreach (var p in c.PrologueStages)
        {
            Win(s, p);
        }

        var first = c.StagesIn(1)[0];
        Assert.Equal(c.Chapter(1)!.CastleLevel > 1 ? StageLock.Castle : StageLock.Open, s.Lock(first.Id));
        castle = c.Chapter(1)!.CastleLevel;
        Assert.Equal(StageLock.Open, s.Lock(first.Id));
        Assert.Equal(StageLock.PreviousStage, s.Lock(c.StagesIn(1)[1].Id));
        foreach (var st in c.StagesIn(1))
        {
            Win(s, st);
        }

        var next = c.StagesIn(2)[0];
        Assert.Equal(StageLock.Castle, s.Lock(next.Id));
        castle = c.Chapter(2)!.CastleLevel;
        Assert.Equal(StageLock.Open, s.Lock(next.Id));
    }

    [Fact]
    public void A_session_with_no_castle_hook_only_checks_order()
    {
        var c = Camp.Catalog();
        var s = Camp.Session(new ManualClock());
        foreach (var p in c.PrologueStages)
        {
            Win(s, p);
        }

        Assert.True(s.IsUnlocked(c.StagesIn(1)[0].Id));
    }

    [Fact]
    public void Replays_pay_a_quarter_and_no_shards_or_gems()
    {
        var c = Camp.Catalog();
        var s = Camp.Session(new ManualClock(), gold: 0, food: 0);
        foreach (var p in c.PrologueStages)
        {
            Win(s, p);
        }

        var boss = c.StagesIn(1)[Def.BossEvery - 1];
        foreach (var st in c.StagesIn(1).Take(Def.BossEvery))
        {
            Win(s, st);
        }

        var gold = s.Wallet.Get(SurvIds.ResSoft);
        var gems = s.Wallet.Get(SurvIds.ResHard);
        var shards = s.Roster.Get(boss.ShardHero)!.Shards;
        var again = s.ApplyResult(new BattleReport(boss.Id, null, true, 20, 1, 1), boss);
        Assert.False(again.FirstClear);
        Assert.Equal(gems, s.Wallet.Get(SurvIds.ResHard));
        Assert.Equal(shards, s.Roster.Get(boss.ShardHero)!.Shards);
        var paid = s.Wallet.Get(SurvIds.ResSoft) - gold;
        Assert.InRange(paid, boss.Rewards[SurvIds.ResSoft] / 4 - 1, boss.Rewards[SurvIds.ResSoft] / 4 + 1);
    }

    [Fact]
    public void A_cleared_chapter_has_one_free_chest_that_survives_a_save()
    {
        var c = Camp.Catalog();
        var s = Camp.Session(new ManualClock(), gold: 0, food: 0);
        foreach (var p in c.PrologueStages)
        {
            Win(s, p);
        }

        Assert.False(s.ChestReady(1));
        foreach (var st in c.StagesIn(1))
        {
            Win(s, st);
        }

        Assert.True(s.ChestReady(1));
        Assert.True(s.AnyChestReady);
        var gems = s.Wallet.Get(SurvIds.ResHard);
        Assert.NotNull(s.ClaimChest(1));
        Assert.Equal(gems + c.Chapter(1)!.Chest[SurvIds.ResHard], s.Wallet.Get(SurvIds.ResHard));
        Assert.Null(s.ClaimChest(1));

        var json = CampaignSave.Write(s);
        var s2 = Camp.Session(new ManualClock());
        Assert.True(CampaignSave.Read(s2, json));
        Assert.True(s2.ChapterCleared(1));
        Assert.False(s2.ChestReady(1));
        Assert.Contains(1, s2.ChestsClaimed);
    }
}

/// <summary>Free-player battle campaign progress in the balance sim, and its pull on the locked runner pace.</summary>
public sealed class BattleCampaignSimTests
{
    private static RunnerEconomy Run(BalanceReport r, BattleCampaignMode mode, bool full)
    {
        var p = r.Def.Profiles.First(x => x.Id == "free");
        var d = full ? r.Def with { StopWhenDone = false } : r.Def;
        var eco = new RunnerEconomy(r.Src, d, p, k => Math.Sqrt(r.RequiredPower[k])) { BattleCampaign = mode };
        eco.Run();
        return eco;
    }

    /// <summary>Within 3% (the pace lock's tolerance) or both never.</summary>
    private static bool Near(double? baseline, double? with) =>
        baseline == null ? with == null || with > 1000 : with != null && Math.Abs(with.Value - baseline.Value) <= Math.Max(baseline.Value * 0.03, 1.0);

    [Fact]
    public void The_campaign_is_off_in_the_locked_pace_model()
    {
        var r = BalanceReport.Get();
        Assert.Empty(r.Economies["free"].CampaignClears);
    }

    [Fact]
    public void Paying_campaign_rewards_into_the_sim_moves_L31_and_L50_only_slightly()
    {
        var r = BalanceReport.Get();
        var p = r.Def.Profiles.First(x => x.Id == "free");
        var eco = new RunnerEconomy(r.Src, r.Def, p, k => Math.Sqrt(r.RequiredPower[k])) { BattleCampaign = BattleCampaignMode.Grant };
        var tl = eco.Run();
        var l31 = tl.First(o => o.Number == 31).ClearedDay;
        var l50 = tl.First(o => o.Number == 50).ClearedDay;
        var b31 = r.Row("free", 31).ClearedDay;
        var b50 = r.Row("free", 50).ClearedDay;
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "battle_campaign_pace.txt"),
            $"L31 off {b31} grant {l31}\nL50 off {b50} grant {l50}\nclears {eco.CampaignClears.Count}\n");
        Assert.True(Near(b31, l31), $"L31 {b31} -> {l31}");
        Assert.True(Near(b50, l50), $"L50 {b50} -> {l50}");
    }

    [Fact]
    public void A_free_player_clears_the_campaign_gradually_behind_the_castle()
    {
        var r = BalanceReport.Get();
        var eco = Run(r, BattleCampaignMode.Observe, full: true);
        var c = eco.CampaignClears;
        var sb = new StringBuilder("stage,day,castle,team_power,foe_power\n");
        foreach (var x in c)
        {
            sb.Append($"{x.Stage},{x.Day:0.00},{x.Castle},{x.TeamPower},{x.FoePower}\n");
        }

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "battle_campaign_free.csv"), sb.ToString());
        var total = r.Src.Campaign.Stages.Count(s => s.InCampaign);
        Assert.True(c.Count >= 10, "a free player should get through chapter 1");
        Assert.True(c[^1].Day >= 60 || c.Count < total, "the whole campaign should not fall in the first two months");
    }
}
