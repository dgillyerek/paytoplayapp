using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Runner;
using Xunit;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>
/// Generated levels look different from one another (Derek 2026-10-27: "it seems like each item is in the same place"): the
/// beats sit at uneven gaps, each beat is one of several set pieces, and two levels in a row never share their item positions.
/// </summary>
public sealed class RunnerLayoutVarietyTests
{
    private readonly ITestOutputHelper _out;

    public RunnerLayoutVarietyTests(ITestOutputHelper output) => _out = output;

    /// <summary>The boxes, traps and barriers a player steers round (not the closing set piece or the boss).</summary>
    private static List<RunnerRowDef> Items(RunnerLevelDef l) =>
        l.Rows.Where(r => !r.Finale && r.Kind is RunnerRowKind.Box or RunnerRowKind.Hazard or RunnerRowKind.Barrier).ToList();

    private static int Lane(float x) => x < -0.8f ? -1 : x > 0.8f ? 1 : 0;

    /// <summary>Share of <paramref name="a"/>'s items that <paramref name="b"/> has too: same kind, same lane, within 1 m.</summary>
    public static double Similarity(RunnerLevelDef a, RunnerLevelDef b)
    {
        var ia = Items(a);
        var ib = Items(b);
        var hit = ia.Count(r => ib.Any(q => q.Kind == r.Kind && Lane(q.X) == Lane(r.X) && Math.Abs(q.At - r.At) <= 1f));
        return ia.Count == 0 ? 0 : hit / (double)ia.Count;
    }

    [Fact]
    public void Two_levels_in_a_row_put_their_boxes_traps_and_barriers_in_different_places()
    {
        var c = Runner.Catalog();
        var worst = 0.0;
        for (var n = 3; n <= c.Levels.Count; n++)
        {
            var s = Similarity(c.ByNumber(n - 1), c.ByNumber(n));
            worst = Math.Max(worst, s);
            _out.WriteLine($"L{n - 1} vs L{n}: {s:0.00}");
            Assert.True(s < 0.5, $"L{n - 1} and L{n} share {s:P0} of their item positions");
        }

        // The levels Derek compared look different all ways round.
        foreach (var (a, b) in new[] { (10, 11), (11, 12), (10, 12) })
        {
            Assert.True(Similarity(c.ByNumber(a), c.ByNumber(b)) < 0.5, $"L{a} vs L{b}");
        }

        _out.WriteLine($"worst {worst:0.00}");
    }

    [Fact]
    public void No_two_levels_in_a_row_run_the_same_set_piece_sequence_and_the_pieces_are_mixed()
    {
        var c = Runner.Catalog();
        var p = c.Progression!;
        var used = new HashSet<RunnerSetPiece>();
        RunnerSetPiece[]? last = null;
        for (var n = 2; n <= c.Levels.Count; n++)
        {
            var count = RunnerProgression.BeatCount(c.ByNumber(n).Length);
            var seq = RunnerProgression.SetPieces(p, n, count);
            used.UnionWith(seq);
            if (last != null)
            {
                Assert.False(seq.SequenceEqual(last), $"L{n} repeats L{n - 1}'s set pieces");
            }

            for (var i = 3; i < seq.Length; i++)
            {
                Assert.NotEqual(seq[i - 1], seq[i]);
            }

            // Past the opening a level mixes at least three kinds of set piece.
            if (seq.Length >= 8)
            {
                Assert.True(seq.Skip(2).Distinct().Count() >= 3, $"L{n}: {string.Join(",", seq)}");
            }

            last = seq;
        }

        Assert.Equal(Enum.GetValues<RunnerSetPiece>().Length, used.Count);
    }

    [Fact]
    public void Beats_sit_9_to_18_m_apart_with_tight_and_breathing_stretches_and_never_drift_a_whole_beat_from_the_even_spacing()
    {
        var c = Runner.Catalog();
        var p = c.Progression!;
        var tight = 0;
        var breathing = 0;
        var maxDrift = 0f;
        for (var n = 2; n <= c.Levels.Count; n++)
        {
            var count = RunnerProgression.BeatCount(c.ByNumber(n).Length);
            var z = RunnerProgression.BeatStarts(p, n, count);
            Assert.Equal(RunnerProgression.BeatStarts(p, n, count), z);
            for (var i = 1; i < z.Length; i++)
            {
                var gap = z[i] - z[i - 1];
                Assert.InRange(gap, RunnerProgression.MinBeatGap, RunnerProgression.MaxBeatGap);
                maxDrift = Math.Max(maxDrift, Math.Abs(z[i] - (12f + (13f * i))));
                tight += gap <= 11.5f ? 1 : 0;
                breathing += gap >= 15f ? 1 : 0;
            }

            // The last beat stands where it always did, so the finale and the boss keep their places.
            Assert.Equal(12f + (13f * (count - 1)), z[^1], 2);
        }

        _out.WriteLine($"tight {tight} breathing {breathing} max drift {maxDrift}");
        Assert.True(tight >= 50 && breathing >= 50, $"tight {tight} breathing {breathing}");
        // The draw keeps within MaxDrift of the even beat; fitting the gaps back to the level's length (and the half-metre
        // rounding) can push a beat a few metres further (10.5 m at most on the shipped levels), never a whole beat.
        Assert.True(maxDrift <= 11f, $"max drift {maxDrift}");
    }

    [Fact]
    public void Every_trap_and_side_barrier_leaves_an_escape_lane_and_no_enemy_spawns_against_a_wall()
    {
        var c = Runner.Catalog();
        foreach (var l in c.Levels.Skip(1))
        {
            foreach (var r in l.Rows.Where(r => !r.Finale && (r.Hazard != null || (r.Barrier != null && !r.Barrier.FullWidth(l.HalfWidth)))))
            {
                Assert.True(RunnerProgression.OpenRoad(l.Rows, r.At, l.HalfWidth) >= RunnerProgression.EscapeGap - 0.01f, $"L{l.Number} at {r.At}: no escape lane");
            }

            foreach (var w in l.Rows.Where(r => r.Barrier != null && r.Barrier.FullWidth(l.HalfWidth)))
            {
                Assert.DoesNotContain(l.Rows, r => r.Kind == RunnerRowKind.Enemy && !r.Finale && r.At >= w.At && r.At < w.At + RunnerProgression.WallStandOff - 0.01f);
            }
        }
    }

    private static bool IsPowerUp(RunnerRowDef r) =>
        r.Kind == RunnerRowKind.Box && r.Box!.Kind is RunnerBoxKind.Weapon or RunnerBoxKind.Rate or RunnerBoxKind.Range or RunnerBoxKind.Heal;

    /// <summary>Share of <paramref name="a"/>'s power-up crates that <paramref name="b"/> has too: same kind, same lane, within 1 m.</summary>
    public static double PowerUpSimilarity(RunnerLevelDef a, RunnerLevelDef b)
    {
        var ia = a.Rows.Where(IsPowerUp).ToList();
        var ib = b.Rows.Where(IsPowerUp).ToList();
        var hit = ia.Count(r => ib.Any(q => q.Box!.Kind == r.Box!.Kind && Lane(q.X) == Lane(r.X) && Math.Abs(q.At - r.At) <= 1f));
        return ia.Count == 0 ? 0 : hit / (double)ia.Count;
    }

    [Fact]
    public void Power_ups_stand_in_different_places_level_to_level()
    {
        // Derek 2026-10-27: "it stil feels like the power ups are in the same place every level". Before: the first crate was
        // always damage at 26.5 m, the kinds came in the same order every level, and neighbours shared up to a third of them.
        var c = Runner.Catalog();
        var shares = new List<double>();
        for (var n = 3; n <= c.Levels.Count; n++)
        {
            var s = PowerUpSimilarity(c.ByNumber(n - 1), c.ByNumber(n));
            shares.Add(s);
            Assert.True(s < 0.3, $"L{n - 1} and L{n} share {s:P0} of their power-up positions");
        }

        _out.WriteLine($"power-up share mean {shares.Average():0.00} max {shares.Max():0.00}");
        Assert.True(shares.Average() < 0.1, $"neighbours share {shares.Average():P0} of their power-ups on average");

        var firsts = Enumerable.Range(2, c.Levels.Count - 1).Select(n => c.ByNumber(n).Rows.First(IsPowerUp)).ToList();
        var zs = firsts.Select(r => r.At).ToList();
        _out.WriteLine($"first power-up z {zs.Min():0.0}-{zs.Max():0.0} m, {zs.Select(z => Math.Round(z)).Distinct().Count()} distinct");
        Assert.True(zs.Max() - zs.Min() >= 8f, "the first power-up stands within a few metres on every level");
        // The opening beats sit at fixed distances, so the first crate lands in a few bands (beat 0 or 1, early or late in it);
        // what matters is that no one spot (2 m, same side) holds most levels any more - it used to hold all of them.
        var spots = firsts.GroupBy(r => (Math.Round(r.At / 2f), Math.Sign(r.X))).Select(g => g.Count()).ToList();
        _out.WriteLine($"first power-up: {spots.Count} spots, busiest holds {spots.Max()} of {firsts.Count}");
        Assert.True(spots.Count >= 8, $"the first power-up keeps to {spots.Count} spots");
        Assert.True(spots.Max() <= firsts.Count / 4, $"one spot holds the first power-up on {spots.Max()} of {firsts.Count} levels");
        // The damage and rate crates keep their slots (balance); the range and heal crates come in each level's own order.
        var orders = Enumerable.Range(2, c.Levels.Count - 1)
            .Select(n => string.Join(",", c.ByNumber(n).Rows.Where(IsPowerUp).Select(r => r.Box!.Kind).Take(6)))
            .Where(o => o.Contains(nameof(RunnerBoxKind.Heal))).ToList();
        Assert.True(orders.Distinct().Count() >= 3, $"power-up kinds come in {orders.Distinct().Count()} orders");
    }

    [Fact]
    public void Power_ups_keep_their_kinds_and_never_stand_in_a_trap_or_barrier()
    {
        var c = Runner.Catalog();
        for (var n = 2; n <= c.Levels.Count; n++)
        {
            var l = c.ByNumber(n);
            foreach (var r in l.Rows.Where(r => r.Kind == RunnerRowKind.Box && !r.Finale))
            {
                Assert.False(RunnerProgression.BoxBlocked(l.Rows, r.At, r.X, l.HalfWidth, r), $"L{n}: {r.Box!.Kind} box at {r.At:0.0} m, x {r.X:0.0} stands in a trap or barrier");
                Assert.InRange(r.X, -l.HalfWidth, l.HalfWidth);
            }

            // The damage and rate crates keep their slots: the first power-up is damage, the second rate.
            var ups = l.Rows.Where(IsPowerUp).ToList();
            Assert.Equal(RunnerBoxKind.Weapon, ups[0].Box!.Kind);
            Assert.Equal(RunnerBoxKind.Rate, ups[1].Box!.Kind);
        }
    }
}
