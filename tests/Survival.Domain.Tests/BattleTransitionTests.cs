using Survival.Domain.Campaign;
using Survival.Domain.Clock;

namespace Survival.Domain.Tests;

/// <summary>The painted loading screen between Play and BattleLocked: cover first, never flash, reveal on the first frame.</summary>
public sealed class BattleTransitionTests
{
    [Fact]
    public void Fades_in_then_covers_before_anything_is_swapped_underneath()
    {
        var t = new BattleTransition();
        t.Begin(10.0);
        Assert.Equal(BattleTransitionPhase.FadingIn, t.Phase);
        Assert.False(t.Covered);
        t.Step(10.0 + (BattleTransition.FadeInSeconds / 2));
        Assert.InRange(t.Alpha, 0.4f, 0.6f);
        Assert.False(t.Covered);
        t.Step(10.0 + BattleTransition.FadeInSeconds + 0.001);
        Assert.True(t.Covered);
        Assert.Equal(1f, t.Alpha);
    }

    [Fact]
    public void Holds_until_the_destination_camera_has_rendered_however_long_the_load_takes()
    {
        var t = new BattleTransition();
        t.Begin(0.0);
        for (var now = 0.0; now < 5.0; now += 0.05)
        {
            t.ReportProgress((float)(now / 5.0));
            t.Step(now);
        }

        Assert.Equal(BattleTransitionPhase.Holding, t.Phase);
        Assert.Equal(1f, t.Alpha);
        Assert.True(t.Bar <= BattleTransition.LoadedBarShare + 0.0001f, "bar must not fill before the battle camera draws");
        t.MarkTargetRendered();
        t.Step(5.05);
        Assert.Equal(BattleTransitionPhase.FadingOut, t.Phase);
        Assert.Equal(1f, t.Bar);
    }

    [Fact]
    public void A_fast_load_still_shows_the_screen_for_the_minimum_time()
    {
        var t = new BattleTransition();
        t.Begin(0.0);
        t.Step(BattleTransition.FadeInSeconds + 0.01);
        t.ReportProgress(1f);
        t.MarkTargetRendered();
        t.Step(0.3);
        Assert.Equal(BattleTransitionPhase.Holding, t.Phase);
        t.Step(BattleTransition.MinShowSeconds - 0.01);
        Assert.Equal(BattleTransitionPhase.Holding, t.Phase);
        t.Step(BattleTransition.MinShowSeconds + 0.01);
        Assert.Equal(BattleTransitionPhase.FadingOut, t.Phase);
        var fadeStart = BattleTransition.MinShowSeconds + 0.01;
        t.Step(fadeStart + (BattleTransition.FadeOutSeconds / 2));
        Assert.InRange(t.Alpha, 0.35f, 0.65f);
        t.Step(fadeStart + BattleTransition.FadeOutSeconds + 0.01);
        Assert.True(t.Done);
        Assert.Equal(0f, t.Alpha);
    }

    [Fact]
    public void Bar_eases_forward_and_never_goes_back()
    {
        var t = new BattleTransition();
        t.Begin(0.0, instant: true);
        Assert.True(t.Covered);
        var last = 0f;
        var reports = new[] { 0.2f, 0.6f, 0.4f, 0.9f, 0.1f, 1f };
        var now = 0.0;
        foreach (var r in reports)
        {
            t.ReportProgress(r);
            for (var i = 0; i < 4; i++)
            {
                now += 0.05;
                t.Step(now);
                Assert.True(t.Bar >= last, "bar went backwards");
                last = t.Bar;
            }
        }

        Assert.InRange(t.Bar, 0.5f, BattleTransition.LoadedBarShare);
    }

    [Fact]
    public void Tip_pick_is_stable_and_always_in_range()
    {
        var tips = new[] { "a", "b", "c" };
        Assert.Equal(BattleTransition.StableSeed("stage.2"), BattleTransition.StableSeed("stage.2"));
        Assert.Equal(BattleTransition.Tip(tips, 7), BattleTransition.Tip(tips, 7));
        Assert.Equal("b", BattleTransition.Tip(tips, 4));
        Assert.Equal("", BattleTransition.Tip(Array.Empty<string>(), 3));
        Assert.Contains(BattleTransition.Tip(tips, BattleTransition.StableSeed("camp.dragon_01") + 12), tips);
    }

    [Fact]
    public void Into_info_lists_both_sides_by_lane_and_back_info_has_no_vs_row()
    {
        var s = Camp.Session(new ManualClock());
        var stage = s.Catalog.Stage("stage.2")!;
        var lineup = s.Setup(stage).Build();
        var into = BattleLoadingInfo.Into(lineup, "To battle!", "tip");
        Assert.False(into.Returning);
        Assert.Equal(stage.Name, into.StageName);
        Assert.Equal(lineup.Heroes.Count, into.HeroPortraits.Count);
        Assert.Equal(lineup.Enemies.Count, into.EnemyPortraits.Count);
        Assert.All(into.HeroPortraits.Concat(into.EnemyPortraits), k => Assert.False(string.IsNullOrEmpty(k)));
        var back = BattleLoadingInfo.Back("Victory!", stage.Name, "tip");
        Assert.True(back.Returning);
        Assert.Empty(back.HeroPortraits);
        Assert.Empty(back.EnemyPortraits);
    }

    [Fact]
    public void Loading_screen_art_is_bound_in_pack_json_and_ships_and_its_strings_exist()
    {
        var pack = TestPaths.LoadPack();
        foreach (var key in BattleLoadingArtKeys.All())
        {
            Assert.True(pack.TryArt(key, out var rel), key + " missing from pack.json art");
            Assert.True(File.Exists(Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", rel)), key + " → " + rel);
        }

        foreach (var key in new[] { "battle.loading.into", "battle.loading.vs", "battle.loading.victory", "battle.loading.defeat", "battle.loading.retreat", "battle.loading.back", "battle.tip.1", "battle.tip.6" })
        {
            Assert.False(string.IsNullOrEmpty(pack.StringOr(key, "")), key + " missing from en.json");
        }
    }
}
