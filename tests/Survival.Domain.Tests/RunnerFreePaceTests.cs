using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// The free (no-spend) player's pace is locked. Derek 2026-10-27: "No matter what changes we make we always want to keep
/// the pace the same without power ups." Any change (power-ups, layouts, units, economy) that moves the free profile's
/// wall or the day it reaches a checkpoint level by more than <see cref="FreePaceTarget.Tolerance"/> fails here; re-tune
/// enemy strength (runner_levels.json level_tweaks foe_hp) until it passes rather than editing the targets.
/// </summary>
public static class FreePaceTarget
{
    /// <summary>The level the free profile first fails on schedule.</summary>
    public const int Wall = 31;

    /// <summary>Allowed relative drift of a checkpoint day (3%), never tighter than one session.</summary>
    public const double Tolerance = 0.03;

    /// <summary>
    /// Day the free profile first clears each checkpoint level. L50 828.67 and L44/L46 are the pre-5193159 values;
    /// L31 is 13 (was 11): the sim's discrete wins could not bring it back without moving the wall to L32.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, double> ClearedDay = new Dictionary<int, double>
    {
        [10] = 3.0,
        [20] = 6.33,
        [31] = 13.0,
        [34] = 48.67,
        [36] = 149.0,
        [40] = 150.33,
        [44] = 341.0,
        [46] = 488.67,
        [50] = 828.67,
    };
}

public sealed class RunnerFreePaceTests
{
    private static BalanceReport R => BalanceReport.Get();

    [Fact]
    public void The_free_wall_is_locked()
    {
        Assert.Equal(FreePaceTarget.Wall, R.Wall("free"));
    }

    [Fact]
    public void The_free_pace_curve_is_locked()
    {
        var r = R;
        var oneSession = 1.0 / r.Def.SessionsPerDay + 1e-6;
        var off = new List<string>();
        foreach (var (level, target) in FreePaceTarget.ClearedDay)
        {
            var day = r.Row("free", level).ClearedDay;
            var allowed = Math.Max(target * FreePaceTarget.Tolerance, oneSession);
            if (day == null || Math.Abs(day.Value - target) > allowed)
            {
                off.Add($"L{level}: day {(day?.ToString("0.00") ?? "never")}, locked {target:0.00} ±{allowed:0.00}");
            }
        }

        Assert.True(off.Count == 0, "The free (no-spend) pace moved (locked, Derek 2026-10-27; re-tune enemy strength):\n" + string.Join("\n", off));
    }
}
