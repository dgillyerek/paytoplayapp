using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Campaign;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>Derek: "once you beat a runner level there should be no going back". Beaten levels never start again; a loss can be retried.</summary>
public class RunnerNoReplayTests
{
    private static RunnerResult R(int number, bool won) => new("x", won, 0, 0, 0, 0, 0, 0, 0, number);

    [Fact]
    public void A_beaten_level_cannot_be_started_and_only_the_next_unbeaten_level_can()
    {
        var n = Runner.Catalog().Levels.Count;
        var unlocked = 1;
        unlocked = RunnerProgress.AfterRun(unlocked, R(1, true), n);
        unlocked = RunnerProgress.AfterRun(unlocked, R(2, true), n);
        Assert.Equal(3, unlocked);

        Assert.True(RunnerProgress.IsBeaten(unlocked, 1));
        Assert.True(RunnerProgress.IsBeaten(unlocked, 2));
        Assert.False(RunnerProgress.IsBeaten(unlocked, 3));
        Assert.False(RunnerProgress.CanStart(unlocked, 1, n));
        Assert.False(RunnerProgress.CanStart(unlocked, 2, n));
        Assert.True(RunnerProgress.CanStart(unlocked, 3, n));
        Assert.False(RunnerProgress.CanStart(unlocked, 4, n));
        Assert.Equal(3, RunnerProgress.Frontier(unlocked, n));

        // No retry after a win: the level just won is beaten for good.
        Assert.False(RunnerProgress.CanRetry(unlocked, 2, won: true, n));
        Assert.False(RunnerProgress.CanRetry(unlocked, 2, won: false, n));
    }

    [Fact]
    public void A_loss_allows_a_retry_of_the_same_level()
    {
        var n = Runner.Catalog().Levels.Count;
        var unlocked = RunnerProgress.AfterRun(3, R(3, false), n);
        Assert.Equal(3, unlocked);
        Assert.True(RunnerProgress.CanRetry(unlocked, 3, won: false, n));
        Assert.True(RunnerProgress.CanStart(unlocked, 3, n));

        // The retry is won: now it is beaten and the retry button is gone.
        unlocked = RunnerProgress.AfterRun(unlocked, R(3, true), n);
        Assert.False(RunnerProgress.CanRetry(unlocked, 3, won: true, n));
        Assert.False(RunnerProgress.CanStart(unlocked, 3, n));
        Assert.True(RunnerProgress.CanStart(unlocked, 4, n));
    }

    [Fact]
    public void Beating_the_last_level_leaves_nothing_to_play()
    {
        var n = Runner.Catalog().Levels.Count;
        var unlocked = RunnerProgress.AfterRun(n, R(n, true), n);
        Assert.True(RunnerProgress.AllBeaten(unlocked, n));
        Assert.Null(RunnerProgress.Frontier(unlocked, n));
        Assert.True(RunnerProgress.IsBeaten(unlocked, n));
        Assert.All(Enumerable.Range(1, n), k => Assert.False(RunnerProgress.CanStart(unlocked, k, n)));

        // The save keeps "all beaten" (the frontier past the last level), so the village never opens a beaten level.
        var catalog = CampaignCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CampaignCatalog.FileName)));
        var campaign = new CampaignSession(catalog, new Wallet(), new ManualClock(1000));
        var last = Runner.Catalog().ByNumber(n);
        RunnerRewards.Grant(RunnerRewards.For(last, new RunnerResult(last.Id, true, 1, 1, 0, 1, 1, last.Length, 60, n)), campaign);
        var again = new CampaignSession(catalog, new Wallet(), new ManualClock(1000));
        Assert.True(again.Load(campaign.Save()));
        Assert.True(RunnerProgress.AllBeaten(again.RunnerUnlocked, n));
    }

    [Fact]
    public void The_scene_and_the_village_entry_use_the_no_replay_rules()
    {
        var dir = Path.Combine(Layouts.RepoRoot(), "Assets", "Survival", "Unity", "Runner");
        var view = File.ReadAllText(Path.Combine(dir, "RunnerLevelView.cs"));
        var hud = File.ReadAllText(Path.Combine(dir, "RunnerLevelView.Hud.cs"));
        var entry = File.ReadAllText(Path.Combine(dir, "SurvivalRunnerEntry.cs"));

        // StartRun refuses a beaten level; Retry and PlayLevel go through the same rules; Leave never restarts a won level.
        Assert.Contains("if (!RunnerProgress.CanStart(_unlocked, _level.Number, _catalog.Levels.Count))", view);
        Assert.Contains("RunnerProgress.CanRetry(_unlocked, _level.Number, won, _catalog.Levels.Count)", view);
        Assert.Contains("!RunnerProgress.CanStart(_unlocked, number, _catalog.Levels.Count)", view);
        Assert.DoesNotContain("public void Retry() => StartRun();", view);
        Assert.DoesNotContain("RunnerProgress.IsOpen(", view);

        // Result screen: Retry only when CanRetry. Level list: play button only on the frontier, beaten levels completed.
        Assert.Contains("RunnerProgress.CanRetry(_unlocked, result.Number, result.Won, _catalog.Levels.Count)", hud);
        Assert.Contains("var open = RunnerProgress.CanStart(_unlocked, level.Number, levels.Count);", hud);
        Assert.Contains("RunnerProgress.IsBeaten(_unlocked, level.Number)", hud);
        Assert.Contains("runner.completed", hud);
        Assert.DoesNotContain("RunnerProgress.IsOpen(", hud);

        // The village button never opens a level once all are beaten.
        Assert.Contains("RunnerProgress.AllBeaten(unlocked, catalog.Levels.Count)", entry);
    }

    [Fact]
    public void The_balance_model_has_no_replays()
    {
        var src = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets", "Survival", "Runtime", "Runner", "RunnerBalance.cs"));
        Assert.DoesNotContain("ReplaysPerSession", src);
        var json = File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerBalanceDef.FileName));
        Assert.DoesNotContain("replays_per_session", json);

        // Every level's win pays once: a free player at the wall earns no runner gold from beaten levels, only the village
        // (gifts and the Gold Mine). Over days 60-120 he clears a handful of levels at most (the mine makes progress slow, not
        // impossible), so non-mine gold earned in the window stays small next to what replays used to pay.
        var free = BalanceReport.Get().Economies["free"];
        var a = free.Daily[59];
        var b = free.Daily[119];
        Assert.True(b.Cleared - a.Cleared <= 6, $"free cleared day 60 L{a.Cleared}, day 120 L{b.Cleared}");
        var nonMine = (b.GoldEarned - b.MineGoldEarned) - (a.GoldEarned - a.MineGoldEarned);
        Assert.True(nonMine < 60 * 2000 + (b.Cleared - a.Cleared) * 120_000, $"free non-mine gold earned day 60-120: {nonMine:0}");
        Assert.True(b.MineGoldEarned > a.MineGoldEarned, "the Gold Mine keeps paying while stuck");
    }
}
