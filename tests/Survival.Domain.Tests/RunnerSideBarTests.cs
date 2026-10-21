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
        Assert.Equal(200f, RunnerSideBar.HeightPx(200f));
        Assert.Equal(20f, RunnerSideBar.WidthPx);
    }
}
