using System;
using System.Numerics;
using Survival.Domain.Roster;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-27: "rowan is just turning. it doesn't look like he's firing his weapon. characters with staffs the staff
/// should be pointed toward the enemy parallel to the ground while walking. it should then kickback when firing".
/// </summary>
public sealed class RunnerWeaponAimTests
{
    private const float Dt = 1f / 60f;

    [Fact]
    public void Lyra_holds_a_staff_and_Rowan_draws_a_bow_everyone_else_plays_their_take()
    {
        Assert.Equal(RunnerAimStyle.Staff, RunnerWeaponAim.StyleFor(LockedBattleRoster.Lyra.Name));
        Assert.Equal(RunnerAimStyle.Bow, RunnerWeaponAim.StyleFor(LockedBattleRoster.Rowan.Name));
        Assert.Equal(RunnerAimStyle.None, RunnerWeaponAim.StyleFor(LockedBattleRoster.Oakenshield.Name));
        Assert.Equal(new[] { LockedBattleRoster.Lyra.Name }, RunnerWeaponAim.StaffHeroes);
    }

    [Theory]
    [InlineData(0.15f)]
    [InlineData(0.25f)]
    [InlineData(0.4f)]
    [InlineData(0.6f)]
    [InlineData(0.9f)]
    [InlineData(1.5f)]
    public void The_kick_snaps_back_then_eases_home_inside_the_fire_interval(float interval)
    {
        var peakAt = -1f;
        var peak = 0f;
        var last = 0f;
        var maxStep = 0f;
        for (var t = 0f; t < interval * 2f; t += 0.001f)
        {
            var w = RunnerKick.Weight(t, interval);
            if (w > peak)
            {
                peak = w;
                peakAt = t;
            }

            if (t > 0f)
            {
                maxStep = Math.Max(maxStep, Math.Abs(w - last));
            }

            last = w;
            if (t >= RunnerKick.IntervalShare * interval + 0.002f)
            {
                Assert.Equal(0f, w);
            }
        }

        Assert.True(peak >= 0.59f, $"peak {peak}");
        Assert.True(peakAt <= 0.04f, $"the snap peaks fast ({peakAt}s)");
        Assert.True(maxStep < 0.1f, $"continuous ({maxStep} per ms)");
    }

    [Fact]
    public void At_a_slow_rate_the_snap_and_the_ease_take_the_asked_times()
    {
        // ~80-150 ms snap, ~200-300 ms ease back.
        Assert.True(RunnerKick.SnapSeconds >= 0.08f && RunnerKick.SnapSeconds <= 0.15f);
        var ease = RunnerKick.TotalSeconds - RunnerKick.SnapSeconds;
        Assert.InRange(ease, 0.2f, 0.3f);
        Assert.Equal(1f, RunnerKick.TimeScale(1f));
    }

    [Fact]
    public void Rapid_fire_never_stacks_two_kicks_past_one_peak()
    {
        var k = new RunnerKickState();
        var max = 0f;
        var prev = 0f;
        var jump = 0f;
        for (var i = 0; i < 600; i++)
        {
            if (i % 9 == 0)
            {
                k.Fire();
            }

            k.Advance(Dt);
            max = Math.Max(max, k.Weight);
            jump = Math.Max(jump, Math.Abs(k.Weight - prev));
            prev = k.Weight;
        }

        Assert.InRange(k.Interval, 0.14f, 0.16f);
        Assert.True(max <= 1.0001f);
        Assert.True(jump <= 1f, "a new shot starts from the current kick, never a reset to zero");
    }

    [Fact]
    public void The_bow_draws_between_shots_and_is_at_full_draw_for_the_release()
    {
        foreach (var interval in new[] { 0.3f, 0.8f, 1.4f })
        {
            Assert.Equal(1f, RunnerWeaponAim.Draw(0f, interval));
            Assert.Equal(0f, RunnerWeaponAim.Draw(RunnerWeaponAim.DrawStart * interval, interval), 3);
            Assert.Equal(1f, RunnerWeaponAim.Draw(interval * 0.99f, interval));
            var prev = 1f;
            for (var t = 0f; t <= RunnerWeaponAim.DrawStart * interval; t += 0.005f)
            {
                var d = RunnerWeaponAim.Draw(t, interval);
                Assert.True(d <= prev + 1e-6f, "back to the string");
                prev = d;
            }

            prev = 0f;
            for (var t = RunnerWeaponAim.DrawStart * interval; t < interval; t += 0.005f)
            {
                var d = RunnerWeaponAim.Draw(t, interval);
                Assert.True(d >= prev - 1e-6f, "drawing");
                prev = d;
            }

            // The kick (string flick, bow tip-up) is over before the hand is back on the string.
            Assert.True(RunnerKick.Weight(0.35f * interval, interval) < 0.3f);
        }
    }

    [Theory]
    [InlineData(0.3f, -0.2f, 0.4f)]
    [InlineData(0.0f, -0.5f, 0.1f)]
    [InlineData(0.1f, 0.05f, 0.45f)]
    public void The_arm_reaches_the_hold_point_with_its_bone_lengths_kept(float x, float y, float z)
    {
        var shoulder = new Vector3(0.2f, 1.45f, 0f);
        var target = shoulder + new Vector3(x, y, z);
        var pole = shoulder + new Vector3(0.5f, -0.6f, -0.3f);
        RunnerTwoBoneIk.Solve(shoulder, 0.28f, 0.27f, target, pole, out var elbow, out var wrist);
        Assert.InRange(Vector3.Distance(shoulder, elbow), 0.2799f, 0.2801f);
        Assert.InRange(Vector3.Distance(elbow, wrist), 0.2699f, 0.2701f);
        Assert.True(Vector3.Distance(wrist, target) < 1e-3f);
        // Bent toward the pole side.
        var dir = Vector3.Normalize(target - shoulder);
        var side = (elbow - shoulder) - (dir * Vector3.Dot(elbow - shoulder, dir));
        var poleSide = (pole - shoulder) - (dir * Vector3.Dot(pole - shoulder, dir));
        Assert.True(Vector3.Dot(side, poleSide) > 0f);
    }

    [Fact]
    public void Out_of_reach_the_arm_points_straight_at_the_target()
    {
        var shoulder = Vector3.Zero;
        RunnerTwoBoneIk.Solve(shoulder, 0.3f, 0.3f, new Vector3(0f, 0f, 2f), new Vector3(0f, -1f, 0f), out var elbow, out var wrist);
        Assert.True(wrist.Z > 0.599f && wrist.Z < 0.6f);
        Assert.True(Math.Abs(elbow.Y) < 0.01f);
    }

    [Fact]
    public void The_staff_hold_is_in_front_and_below_the_shoulder_and_the_bow_reaches_out()
    {
        Assert.True(RunnerWeaponAim.StaffHold.Z > 0.2f && RunnerWeaponAim.StaffHold.Y < 0f);
        Assert.True(RunnerWeaponAim.BowHold.Z > 0.45f);
        Assert.InRange(RunnerWeaponAim.StaffGripFromButt, 0.25f, 0.45f);
        Assert.True(RunnerWeaponAim.FullDrawBehind > RunnerWeaponAim.NockBehind + 0.3f);
    }
}
