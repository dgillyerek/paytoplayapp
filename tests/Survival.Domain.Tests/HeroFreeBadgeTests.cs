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

        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.rowan")); // 30 s: free only in its last 15 s
        Assert.Empty(r.FreeFinishable());
        clock.Advance(15);
        Assert.Equal(new[] { "hero.rowan" }, r.FreeFinishable());

        clock.Advance(585); // Lyra now under 5 minutes too; Rowan's timer ran out and levels on Tick
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
        var clock = new ManualClock(1000);
        var s = Camp.Session(clock);
        var r = s.Roster;
        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.rowan"));
        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.sir_aldric"));
        Assert.Empty(r.FreeFinishable());
        clock.Advance(15); // both 30 s trainings reach their last half
        Assert.Equal(2, r.FreeFinishable().Count);
        Assert.Equal(2, r.FinishAllFree().Count);
        Assert.Equal(2, r.Get("hero.rowan")!.Level);
        Assert.Equal(2, r.Get("hero.sir_aldric")!.Level);
    }

    [Fact]
    public void Free_window_is_the_last_five_minutes_but_never_more_than_the_last_half()
    {
        var rules = new RushRules();
        Assert.Equal(15, rules.FreeWindow(30), 6);
        Assert.Equal(150, rules.FreeWindow(300), 6);
        Assert.Equal(300, rules.FreeWindow(600), 6);
        Assert.Equal(300, rules.FreeWindow(3600), 6);
    }

    [Fact]
    public void Badge_hides_while_a_campaign_screen_is_open_or_in_battle()
    {
        Assert.True(HeroRoster.ShowFreeBadge(1, inBattle: false, screenOpen: false));
        Assert.False(HeroRoster.ShowFreeBadge(1, inBattle: false, screenOpen: true));
        Assert.False(HeroRoster.ShowFreeBadge(1, inBattle: true, screenOpen: false));
        Assert.False(HeroRoster.ShowFreeBadge(0, inBattle: false, screenOpen: false));
    }

    /// <summary>
    /// Regression (Derek, Oct 28): start a short early level-up, close the Heroes panel. The village FREE badge used to
    /// appear at once and one tap finished it. Now it keeps training with the right time left (also across a save and
    /// reload) and is not free until its last half.
    /// </summary>
    [Fact]
    public void Short_early_level_up_keeps_its_timer_after_closing_the_heroes_panel()
    {
        var clock = new ManualClock(1000);
        var s = Camp.Session(clock, gems: 0);
        var r = s.Roster;
        Assert.Equal(LevelUpCheck.Ok, r.TryStartLevelUp("hero.rowan")); // Lv1 -> 2: 30 s
        var total = r.Remaining("hero.rowan");
        Assert.Equal(30, total, 3);

        // Heroes panel closed: nothing finishes, the badge has nothing to offer yet.
        clock.Advance(5);
        Assert.Equal(0, r.Tick());
        Assert.NotNull(r.TrainingOf("hero.rowan"));
        Assert.Equal(25, r.Remaining("hero.rowan"), 3);
        Assert.False(r.CanFinishFree("hero.rowan"));
        Assert.Empty(r.FreeFinishable());
        Assert.False(HeroRoster.ShowFreeBadge(r.FreeFinishable().Count, inBattle: false, screenOpen: false));
        Assert.Empty(r.FinishAllFree()); // a stray badge tap does nothing
        Assert.Equal(1, r.Get("hero.rowan")!.Level);

        // Reopen after an app restart: same timer.
        var json = CampaignSave.Write(s);
        var s2 = Camp.Session(clock, gems: 0);
        Assert.True(CampaignSave.Read(s2, json));
        Assert.Equal(25, s2.Roster.Remaining("hero.rowan"), 3);
        Assert.False(s2.Roster.CanFinishFree("hero.rowan"));

        // Last half: free.
        clock.Advance(10);
        Assert.True(s2.Roster.CanFinishFree("hero.rowan"));
        Assert.Equal(new[] { ("hero.rowan", 2) }, s2.Roster.FinishAllFree());
    }
}
