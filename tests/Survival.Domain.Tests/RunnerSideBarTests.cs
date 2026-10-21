using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

public sealed class RunnerSideBarTests
{
    [Fact]
    public void Bars_stand_beside_the_body_on_its_outer_side_and_behind_it()
    {
        // Hero faces +z: its left is -x, behind is -z.
        var (hx, hz) = RunnerSideBar.WorldOffset(true, 0.5f, 1f);
        Assert.Equal(-(0.5f + RunnerSideBar.GapMetres), hx, 3);
        Assert.Equal(-RunnerSideBar.BackMetres, hz, 3);

        // Foe faces -z: its right is -x, behind is +z.
        var (fx, fz) = RunnerSideBar.WorldOffset(false, 0.5f, 1f);
        Assert.Equal(-(0.5f + RunnerSideBar.GapMetres), fx, 3);
        Assert.Equal(RunnerSideBar.BackMetres, fz, 3);

        // The 2x boss stands its bar further out.
        var (bx, _) = RunnerSideBar.WorldOffset(false, 1f, 2f);
        Assert.True(bx < fx);
    }

    [Fact]
    public void Bar_follows_body_height_and_stays_readable()
    {
        var (bottom, top) = RunnerSideBar.Span(2f);
        Assert.Equal(0.24f, bottom, 3);
        Assert.Equal(1.76f, top, 3);
        Assert.Equal(RunnerSideBar.MinHeightPx, RunnerSideBar.HeightPx(10f));
        Assert.Equal(RunnerSideBar.MaxHeightPx, RunnerSideBar.HeightPx(5000f));
        Assert.Equal(80f, RunnerSideBar.HeightPx(80f));
        Assert.True(RunnerSideBar.WidthPx <= 8f, "a sliver, not the battle's 20 px bar");
    }

    [Fact]
    public void Bars_are_hidden_at_full_hp_show_faintly_on_damage_and_fade_two_seconds_after_the_last_hit()
    {
        Assert.Equal(0f, RunnerSideBar.Alpha(100, 100, 0.5f));
        Assert.Equal(0f, RunnerSideBar.Alpha(0, 100, 0.5f));
        Assert.Equal(0f, RunnerSideBar.Alpha(60, 100, -1f));
        Assert.Equal(RunnerSideBar.ShownAlpha, RunnerSideBar.Alpha(60, 100, 1f), 3);
        Assert.True(RunnerSideBar.ShownAlpha < 1f);
        Assert.Equal(RunnerSideBar.ShownAlpha, RunnerSideBar.Alpha(60, 100, RunnerSideBar.HoldSeconds), 3);
        var mid = RunnerSideBar.Alpha(60, 100, RunnerSideBar.HoldSeconds + (RunnerSideBar.FadeSeconds * 0.5f));
        Assert.InRange(mid, 0.01f, RunnerSideBar.ShownAlpha - 0.01f);
        Assert.Equal(0f, RunnerSideBar.Alpha(60, 100, RunnerSideBar.HoldSeconds + RunnerSideBar.FadeSeconds + 0.01f));
    }
}
