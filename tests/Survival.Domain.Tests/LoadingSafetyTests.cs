using Survival.Domain.Campaign;

namespace Survival.Domain.Tests;

/// <summary>The loading screen can never freeze the game: it clears on a frame, on release, or on its own last-resort clock.</summary>
public sealed class LoadingSafetyTests
{
    [Fact]
    public void A_trip_that_never_reaches_its_destination_still_clears_after_the_show_limit()
    {
        var t = new BattleTransition();
        t.Begin(0.0);
        var now = 0.0;
        for (; now < BattleTransition.ShowLimitSeconds - 0.05; now += 0.05)
        {
            t.Step(now);
        }

        Assert.Equal(BattleTransitionPhase.Holding, t.Phase);
        Assert.False(t.GaveUp);
        for (; now < BattleTransition.ShowLimitSeconds + BattleTransition.FadeOutSeconds + 0.2; now += 0.05)
        {
            t.Step(now);
        }

        Assert.True(t.GaveUp);
        Assert.True(t.Done);
        Assert.Equal(0f, t.Alpha);
    }

    [Fact]
    public void Releasing_a_failed_trip_fades_after_the_minimum_show_time_even_mid_fade_in()
    {
        var t = new BattleTransition();
        t.Begin(0.0);
        t.Step(0.05);
        t.Release();
        Assert.True(t.GaveUp);
        t.Step(BattleTransition.FadeInSeconds + 0.01);
        Assert.Equal(BattleTransitionPhase.Holding, t.Phase);
        t.Step(BattleTransition.MinShowSeconds + 0.01);
        Assert.Equal(BattleTransitionPhase.FadingOut, t.Phase);
        t.Step(BattleTransition.MinShowSeconds + BattleTransition.FadeOutSeconds + 0.05);
        Assert.True(t.Done);
    }

    [Fact]
    public void A_normal_reveal_is_not_a_give_up()
    {
        var t = new BattleTransition();
        t.Begin(0.0);
        t.Step(0.3);
        t.MarkTargetRendered();
        t.Release();
        Assert.False(t.GaveUp);
    }

    [Fact]
    public void Bonequills_bow_imports_readable_so_its_braced_string_can_be_measured_and_cut_at_runtime()
    {
        // BlenderRigAttackDriver.BuildDrawnString reads the bow's vertices / triangles (runner and BattleLocked alike);
        // with Read/Write off Unity only logs "Not allowed to access vertices" and the braced string stays.
        var meta = Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "art", "enemies", "3d", "bonequill", "BONEQUILL_bow.fbx.meta");
        Assert.Contains("isReadable: 1", File.ReadAllText(meta));
    }
}
