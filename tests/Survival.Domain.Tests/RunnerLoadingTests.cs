using Survival.Domain.Campaign;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>The runner's Play round trip goes through #76's painted loading screen; its bars use #76's side-bar layout.</summary>
public sealed class RunnerLoadingTests
{
    [Fact]
    public void Into_the_runner_shows_the_hero_against_the_levels_enemies_with_the_boss_last()
    {
        var c = Runner.Catalog();
        foreach (var level in c.Levels)
        {
            var info = RunnerLoading.Into(c, level, "Name " + level.Number, "Onto the road!", "tip");
            Assert.False(info.Returning);
            Assert.Equal("Name " + level.Number, info.StageName);
            Assert.Equal(new[] { RunnerArtKeys.Portrait(c.Unit(level.Hero).Roster) }, info.HeroPortraits);
            Assert.InRange(info.EnemyPortraits.Count, 1, RunnerLoading.MaxFaces);
            Assert.Equal(info.EnemyPortraits.Count, info.EnemyPortraits.Distinct().Count());
            if (level.HasBoss)
            {
                var boss = level.Rows.Single(r => r.Kind == RunnerRowKind.Boss);
                Assert.Equal(RunnerArtKeys.Portrait(c.Unit(boss.Unit).Roster), info.EnemyPortraits[^1]);
            }
        }

        // The last level brings out every foe type: capped at five faces, the boss still among them (last).
        var last = RunnerLoading.Into(c, c.Levels[^1], "x", "y", "z");
        Assert.Equal(RunnerLoading.MaxFaces, last.EnemyPortraits.Count);
        Assert.Equal("theme_a.hero.portrait.ironhowl", last.EnemyPortraits[^1]);
    }

    [Fact]
    public void Coming_back_says_how_the_last_run_went_and_shows_no_vs_row()
    {
        RunnerResult Run(bool won) => new("runner.thornwood_road", won, 1, 1, 0, 1, 1, 10f, 10f);
        Assert.Equal(RunnerLoading.BackKey, RunnerLoading.BackHeadlineKey(Array.Empty<RunnerResult>()));
        Assert.Equal(RunnerLoading.VictoryKey, RunnerLoading.BackHeadlineKey(new[] { Run(false), Run(true) }));
        Assert.Equal(RunnerLoading.DefeatKey, RunnerLoading.BackHeadlineKey(new[] { Run(true), Run(false) }));
        var back = RunnerLoading.Back("Victory!", "Thornwood Road", "tip");
        Assert.True(back.Returning);
        Assert.Empty(back.HeroPortraits);
        Assert.Empty(back.EnemyPortraits);
    }

    [Fact]
    public void Runner_loading_portraits_art_and_strings_are_in_the_pack()
    {
        var pack = TestPaths.LoadPack();
        var c = Runner.Catalog();
        foreach (var level in c.Levels)
        {
            var info = RunnerLoading.Into(c, level, "", "", "");
            foreach (var key in info.HeroPortraits.Concat(info.EnemyPortraits))
            {
                Assert.True(pack.TryArt(key, out _), key + " missing from pack.json art");
            }
        }

        foreach (var key in BattleLoadingArtKeys.All())
        {
            Assert.True(pack.TryArt(key, out _), key);
        }

        var keys = new List<string> { RunnerLoading.IntoKey, RunnerLoading.BackKey, RunnerLoading.VictoryKey, RunnerLoading.DefeatKey, "battle.loading.vs" };
        for (var i = 1; i <= RunnerLoading.TipCount; i++)
        {
            keys.Add(RunnerLoading.TipPrefix + i);
        }

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrEmpty(pack.StringOr(key, "")), key + " missing from en.json");
        }
    }

    [Fact]
    public void Runner_side_bars_use_the_battles_shared_layout_only_the_style_is_their_own()
    {
        Assert.Equal(BattleHpBarLayout.GapMetres, RunnerSideBar.GapMetres);
        Assert.Equal(BattleHpBarLayout.BackMetres, RunnerSideBar.BackMetres);
        foreach (var scale in new[] { 1f, 1.6f, 2f })
        {
            foreach (var half in new[] { 0.2f, 0.5f, 1.2f })
            {
                // The runner has no lanes: the battle's layout with a lane wide enough never to fold the bar back.
                var battleHero = BattleHpBarLayout.Offset(CampaignSide.Hero, half, scale, 1000f);
                var battleFoe = BattleHpBarLayout.Offset(CampaignSide.Villain, half, scale, 1000f);
                Assert.Equal(battleHero, RunnerSideBar.Offset(true, half, scale));
                Assert.Equal(battleFoe, RunnerSideBar.Offset(false, half, scale));
            }
        }

        Assert.Equal(BattleHpBarLayout.Span(1.8f, 1f), RunnerSideBar.Span(1.8f));
        Assert.Equal(BattleHpBarLayout.EstimatedHalfWidth(1.8f, 1f, false), RunnerSideBar.HalfWidth(1.8f));

        // The thin, fade-on-damage style stays the runner's.
        Assert.True(RunnerSideBar.WidthPx < BattleHpBarLayout.WidthPx);
        Assert.True(RunnerSideBar.MaxHeightPx < BattleHpBarLayout.MaxHeightPx);
        Assert.Equal(0f, RunnerSideBar.Alpha(100, 100, 0.5f));
        Assert.True(RunnerSideBar.Alpha(50, 100, 0.5f) > 0f);
    }
}
