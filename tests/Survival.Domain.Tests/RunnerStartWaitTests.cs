using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-29: "don't start the level until the first button is pressed or if on mobile until the units are dragged".
/// A run told to wait does not advance until the first steer (or <see cref="RunnerRun.Start"/>); runs start at once by
/// default, so the balance sim is unaffected.
/// </summary>
public sealed class RunnerStartWaitTests
{
    private static RunnerRun NewRun()
    {
        var c = Runner.Catalog();
        var level = c.ByNumber(5);
        return new RunnerRun(c, level, RunnerBalance.Uniform(c, 1.0));
    }

    [Fact]
    public void A_waiting_run_does_not_advance_before_the_first_input()
    {
        var run = NewRun();
        run.WaitForStart();
        var z = run.Hero.Z;
        var x = run.Hero.X;
        var foes = run.Foes.Select(f => (f.X, f.Z)).ToList();
        for (var i = 0; i < 300; i++)
        {
            Assert.Empty(run.Step(1f / 30f));
        }

        Assert.True(run.WaitingForStart);
        Assert.Equal(0f, run.Seconds);
        Assert.Equal(0f, run.Distance);
        Assert.Equal(z, run.Hero.Z);
        Assert.Equal(x, run.Hero.X);
        Assert.Equal(foes, run.Foes.Select(f => (f.X, f.Z)).ToList());
        Assert.Equal(RunnerOutcome.Running, run.Outcome);
    }

    [Fact]
    public void The_first_steer_starts_the_run_and_steers()
    {
        var run = NewRun();
        run.WaitForStart();
        run.Step(1f);
        run.SteerBy(1f);
        Assert.False(run.WaitingForStart);
        Assert.Equal(1f, run.SteerTarget, 3);
        run.Step(0.5f);
        Assert.True(run.Seconds > 0.49f);
        Assert.True(run.Distance > 0f);
        Assert.True(run.Hero.X > 0f);
    }

    [Fact]
    public void A_press_starts_without_moving_the_squad_sideways()
    {
        var run = NewRun();
        run.WaitForStart();
        var x = run.Hero.X;
        run.Start();
        Assert.False(run.WaitingForStart);
        run.Step(0.5f);
        Assert.True(run.Distance > 0f);
        Assert.Equal(x, run.Hero.X, 3);
    }

    [Fact]
    public void Runs_start_at_once_unless_told_to_wait()
    {
        var run = NewRun();
        Assert.False(run.WaitingForStart);
        run.Step(0.5f);
        Assert.True(run.Distance > 0f);
    }
}
