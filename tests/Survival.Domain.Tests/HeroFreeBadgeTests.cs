using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Ids;

namespace Survival.Domain.Tests;

/// <summary>The Village FREE hero badge: hero trainings inside the free-finish window, finished with one tap.</summary>
public sealed class HeroFreeBadgeTests
{
    [Fact]
    public void Only_trainings_inside_the_free_window_show_on_the_badge_and_one_tap_finishes_them_all()
    {
        var clock = new ManualClock(1000);
        var s = Camp.Session(clock, gems: 0);
        var r = s.Roster;
        Assert.Empty(r.FreeFinishable());

        r.Get("hero.lyra")!.Level = 8; // ~805 s: outside the 5-minute window
        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.lyra"));
        Assert.Empty(r.FreeFinishable());

        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.rowan")); // 30 s: free
        Assert.Equal(new[] { "hero.rowan" }, r.FreeFinishable());

        clock.Advance(600); // Lyra now under 5 minutes too; Rowan's timer ran out and levels on Tick
        r.Tick();
        Assert.Equal(new[] { "hero.lyra" }, r.FreeFinishable());
        Assert.Equal(2, r.Get("hero.rowan")!.Level);

        var done = r.FinishAllFree();
        Assert.Equal(new[] { ("hero.lyra", 9) }, done);
        Assert.Empty(r.FreeFinishable());
        Assert.Empty(r.Training);
        Assert.Equal(0, s.Wallet.Get(SurvIds.ResHard)); // free, no gems
    }

    [Fact]
    public void Two_eligible_trainings_count_two_and_finish_together()
    {
        var s = Camp.Session(new ManualClock(1000));
        var r = s.Roster;
        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.rowan"));
        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.sir_aldric"));
        Assert.Equal(2, r.FreeFinishable().Count);
        Assert.Equal(2, r.FinishAllFree().Count);
        Assert.Equal(2, r.Get("hero.rowan")!.Level);
        Assert.Equal(2, r.Get("hero.sir_aldric")!.Level);
    }
}
