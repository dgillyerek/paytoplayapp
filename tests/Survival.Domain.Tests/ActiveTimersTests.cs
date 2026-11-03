using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.View;
using Survival.Domain.World;
using Xunit;

namespace Survival.Domain.Tests;

public class ActiveTimersTests
{
    private const double T0 = 1_800_000_000;

    private static TimerQueue Builders(ManualClock c)
    {
        var q = new TimerQueue(c, new RushRules(), 3);
        q.TryStart("base.timer.barracks", "barracks", 600, out _);
        q.TryStart("base.timer.farm", "farm", 60, out _);
        return q;
    }

    [Fact]
    public void Gathers_builds_training_and_player_marches_soonest_first()
    {
        var c = new ManualClock(T0);
        var q = Builders(c);
        var training = new[] { new TrainingTimer("lyra", T0, 300) };
        var rows = ActiveTimers.Gather(q.Active, training, null, T0);

        Assert.Equal(new[] { "base.timer.farm", "hero.train.lyra", "base.timer.barracks" }, rows.ConvertAll(r => r.Id));
        Assert.Equal(TimerKind.HeroTraining, rows[1].Kind);
        Assert.Equal("lyra", rows[1].Target);
        Assert.Equal("farm", rows[0].Target);
    }

    [Fact]
    public void Player_march_on_the_road_is_a_timer_that_goes_to_its_target()
    {
        var sim = new WorldSim(Layouts.WorldWithNodes(), 0);
        Assert.Equal(MarchSendResult.Ok, sim.SendPlayerMarch("camp.goblin_01", "attack", out var march));
        var rows = ActiveTimers.Gather(null, null, sim.Marches, 1);
        var row = Assert.Single(rows);
        Assert.Equal(TimerKind.March, row.Kind);
        Assert.Equal(TimerGoKind.WorldEntity, row.Go);
        Assert.Equal("camp.goblin_01", row.Target);
        Assert.Equal(march!.Arrival, row.EndsAt);
    }

    [Fact]
    public void Busy_builders_lists_only_building_jobs_soonest_first_and_link_to_the_plot()
    {
        var c = new ManualClock(T0);
        var q = Builders(c);
        q.TryStart("other.timer.x", "x", 10, out _);
        var rows = ActiveTimers.BusyBuilders(q.Active, T0);
        Assert.Equal(new[] { "farm", "barracks" }, rows.ConvertAll(r => r.Target));
        Assert.All(rows, r => Assert.Equal(TimerGoKind.Building, r.Go));
        Assert.Empty(ActiveTimers.BusyBuilders(null, T0));
    }

    [Fact]
    public void Deep_link_per_kind()
    {
        Assert.Equal(TimerGoKind.Building, ActiveTimers.GoFor(TimerKind.Building));
        Assert.Equal(TimerGoKind.HeroDetail, ActiveTimers.GoFor(TimerKind.HeroTraining));
        Assert.Equal(TimerGoKind.WorldEntity, ActiveTimers.GoFor(TimerKind.March));
    }

    [Fact]
    public void Finished_rows_are_ready_and_lead()
    {
        var c = new ManualClock(T0);
        var q = Builders(c);
        c.Advance(120);
        var rows = ActiveTimers.Gather(q.Active, null, null, c.Now);
        Assert.True(rows[0].IsReady(c.Now));
        Assert.Equal(1.0, rows[0].Progress(c.Now));
        Assert.False(rows[1].IsReady(c.Now));
        Assert.Equal(480, rows[1].Remaining(c.Now), 3);
        Assert.Equal(1, ActiveTimers.ReadyCount(rows, c.Now));
    }

    [Fact]
    public void Rows_rebuild_only_when_the_set_changes()
    {
        var c = new ManualClock(T0);
        var q = Builders(c);
        var first = ActiveTimers.Gather(q.Active, null, null, c.Now);
        var sig = ActiveTimers.Signature(first);

        c.Advance(30); // countdown ticks: same rows, no rebuild
        Assert.False(ActiveTimers.NeedsRebuild(sig, ActiveTimers.Gather(q.Active, null, null, c.Now)));

        c.Advance(60);
        Assert.True(q.TryComplete("base.timer.farm"));
        Assert.True(ActiveTimers.NeedsRebuild(sig, ActiveTimers.Gather(q.Active, null, null, c.Now)));
    }

    [Fact]
    public void Hourglass_only_shows_with_a_timer()
    {
        Assert.False(RailActionable.Timers(0));
        Assert.True(RailActionable.Timers(1));
    }
}
