using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>Sideways steering is a critically damped spring: it eases in and out, never overshoots, and stays responsive.</summary>
public sealed class RunnerSteeringTests
{
    private const float Dt = 1f / 30f;

    [Fact]
    public void The_hero_eases_to_the_steer_point_without_overshoot_and_is_still_quick()
    {
        var run = new RunnerRun(Runner.Catalog(), Runner.Catalog().First);
        run.SteerTo(2f);
        var xs = new List<float>();
        var vs = new List<float>();
        for (var i = 0; i < 45; i++)
        {
            run.Step(Dt);
            xs.Add(run.Hero.X);
            vs.Add(run.Hero.VX);
        }

        // Eases in: the first step moves less than the second (no snap), and it never passes the target.
        Assert.True(xs[0] < xs[1] - xs[0], "eases in");
        Assert.All(xs, x => Assert.True(x <= 2f + 1e-4f, "overshot to " + x));
        Assert.All(vs, v => Assert.True(v >= -1e-4f && v <= RunnerRules.SteerSpeed + 1e-3f, "speed " + v));
        // Responsive: most of the 2 m within a third of a second, settled inside 0.6 s.
        Assert.True(xs[9] > 1.6f, "after 0.33 s only at " + xs[9]);
        Assert.InRange(xs[17], 1.97f, 2.0001f);
        Assert.True(Math.Abs(vs[^1]) < 0.05f, "settles");
    }

    [Fact]
    public void Followers_trail_their_slot_smoothly_back_rows_a_little_later()
    {
        var c = Runner.Tiny(@"{ ""at"": 6, ""kind"": ""box"", ""x"": 0, ""hp"": 1, ""reward"": ""followers"", ""unit"": ""archer"", ""count"": 6 }", length: 200f);
        var run = new RunnerRun(c, c.First);
        for (var i = 0; i < 150 && run.Followers < 6; i++)
        {
            run.Step(Dt);
        }

        Assert.Equal(6, run.Followers);
        for (var i = 0; i < 90; i++)
        {
            run.Step(Dt);
        }

        // Swing the hero across and watch each follower's sideways step: bounded (no snapping) and the back row later.
        run.SteerTo(3f);
        var frontRow = run.Squad.Skip(1).Min(m => RunnerFormation.RowOf(m.Slot));
        var front = run.Squad.Skip(1).First(m => RunnerFormation.RowOf(m.Slot) == frontRow);
        var backRow = run.Squad.Skip(1).Max(m => RunnerFormation.RowOf(m.Slot));
        var back = run.Squad.Skip(1).First(m => RunnerFormation.RowOf(m.Slot) == backRow);
        var frontStart = front.X;
        var backStart = back.X;
        var frontAt = new List<float>();
        var backAt = new List<float>();
        for (var i = 0; i < 12; i++)
        {
            var fx = front.X;
            var bx = back.X;
            run.Step(Dt);
            Assert.True(Math.Abs(front.X - fx) <= (RunnerRules.FollowMaxSpeed * Dt) + 1e-3f, "front snapped");
            Assert.True(Math.Abs(back.X - bx) <= (RunnerRules.FollowMaxSpeed * Dt) + 1e-3f, "back snapped");
            frontAt.Add(front.X - frontStart);
            backAt.Add(back.X - backStart);
        }

        Assert.True(backRow > frontRow);
        Assert.True(backAt[5] < frontAt[5], "back rows trail a little later");
        Assert.True(frontAt[^1] > 2f, "but they keep up");
    }

    [Fact]
    public void Smooth_damp_matches_the_standard_spring_and_respects_the_speed_cap()
    {
        var v = 0f;
        var x = 0f;
        for (var i = 0; i < 300; i++)
        {
            x = RunnerSteering.SmoothDamp(x, 10f, ref v, 0.1f, 5f, Dt);
            Assert.True(v <= 5f + 1e-3f);
        }

        Assert.Equal(10f, x, 3);
        Assert.Equal(0f, v, 3);
        Assert.Equal(1f, RunnerSideBar.BoxAlpha(5));
        Assert.Equal(0f, RunnerSideBar.BoxAlpha(0));
    }
}
