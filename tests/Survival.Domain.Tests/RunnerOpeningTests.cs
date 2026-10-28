using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Runner;
using Xunit;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// Levels open differently (Derek 2026-10-27: "on the first level there is immediately one on the left followed shortly
/// after by one on each left and right side of all the levels"). A level's opening, as a player sees it, is how far in the
/// first box stands, whether a wave comes first, and for the first three pickups (boxes within 4 m of each other count as
/// one) how many boxes there are and on which side.
/// </summary>
public sealed class RunnerOpeningTests
{
    private readonly ITestOutputHelper _out;

    public RunnerOpeningTests(ITestOutputHelper output) => _out = output;

    /// <summary>Boxes this close down the road read as one pickup (about half a second at a run).</summary>
    public const float PairMetres = 4f;

    private static char Side(float x) => x < -0.5f ? 'L' : x > 0.5f ? 'R' : 'C';

    /// <summary>The pickup events of a level: boxes within PairMetres of the first box of the event count as one.</summary>
    public static List<(float At, string Sides)> Pickups(RunnerLevelDef l)
    {
        var events = new List<(float At, string Sides)>();
        foreach (var b in l.Rows.Where(r => r.Kind == RunnerRowKind.Box).OrderBy(r => r.At))
        {
            if (events.Count > 0 && b.At - events[^1].At <= PairMetres)
            {
                var sides = new string((events[^1].Sides + Side(b.X)).OrderBy(ch => ch).ToArray());
                events[^1] = (events[^1].At, sides);
            }
            else
            {
                events.Add((b.At, Side(b.X).ToString()));
            }
        }

        return events;
    }

    public static string Signature(RunnerLevelDef l)
    {
        var ev = Pickups(l);
        var first = ev[0].At;
        var bucket = first < 10f ? "near" : first < 14f ? "12m" : first < 18f ? "far" : "late";
        var waveFirst = l.Rows.Any(r => r.Kind == RunnerRowKind.Enemy && r.At < first) ? "wave," : "";
        return bucket + ":" + waveFirst + string.Join(",", ev.Take(3).Select(e => e.Sides));
    }

    [Fact]
    public void Neighbouring_levels_open_differently_and_no_opening_is_common()
    {
        var c = Runner.Catalog();
        var sigs = new Dictionary<int, string>();
        for (var n = 2; n <= c.Levels.Count; n++)
        {
            sigs[n] = Signature(c.ByNumber(n));
        }

        foreach (var g in sigs.GroupBy(kv => kv.Value).OrderByDescending(g => g.Count()))
        {
            _out.WriteLine($"{g.Count(),2} {g.Key}  (L{string.Join(" L", g.Select(kv => kv.Key))})");
        }

        for (var n = 3; n <= c.Levels.Count; n++)
        {
            Assert.True(sigs[n] != sigs[n - 1], $"L{n - 1} and L{n} both open {sigs[n]}");
        }

        var most = sigs.GroupBy(kv => kv.Value).Max(g => g.Count());
        Assert.True(most <= Math.Ceiling(0.15 * sigs.Count), $"one opening on {most} of {sigs.Count} levels");

        // The kinds of opening a player can tell apart all turn up: a pair first, a single on the left, on the right, in
        // the middle, and a wave before any box; and the first box stands anywhere from under 10 m to over 18 m in.
        var shapes = sigs.Values.Select(s => s[(s.IndexOf(':') + 1)..]).ToList();
        Assert.Contains(shapes, s => s.Split(',')[0].Length == 2);
        Assert.Contains(shapes, s => s.StartsWith("L,", StringComparison.Ordinal));
        Assert.Contains(shapes, s => s.StartsWith("R,", StringComparison.Ordinal));
        Assert.Contains(shapes, s => s.StartsWith("C,", StringComparison.Ordinal));
        Assert.Contains(shapes, s => s.StartsWith("wave,", StringComparison.Ordinal));
        var firsts = Enumerable.Range(2, c.Levels.Count - 1).Select(n => Pickups(c.ByNumber(n))[0].At).ToList();
        Assert.True(firsts.Min() < 10f && firsts.Max() > 18f, $"first pickup {firsts.Min()}-{firsts.Max()} m");
    }
}
