using Survival.Domain.Campaign;
using Survival.Domain.Roster;
using Xunit;
using Xunit.Abstractions;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-28: Sir Aldric and Blightroot fought "essentially on top of each other".</summary>
public sealed class BattleSpacingTests
{
    private static readonly string[] Heroes = { "Sir Aldric", "Lyra", "Stormcrest", "Oakenshield", "Emberfang", "Rowan" };
    private static readonly string[] Villains = { "Blightroot", "Ironhowl", "Nightfang", "Vespera", "Ashwyrm", "Bonequill" };

    private readonly ITestOutputHelper _out;

    public BattleSpacingTests(ITestOutputHelper output) => _out = output;

    private static LockedBattleFighter F(string name) => LockedBattleRoster.All.Single(f => f.Name == name);

    private static float Depth(LockedBattleFighter f) =>
        BattleSpacing.HalfDepthOf(f.Name, f.DesignHeightMetres, LockedBattleRoster.BattleScaleOf(f));

    private static float Stop(LockedBattleFighter a, LockedBattleFighter b) =>
        BattleSpacing.StopDistance(a.AttackRangeMetres, Depth(a), Depth(b));

    [Fact]
    public void Aldric_and_Blightroot_stop_with_a_gap_between_their_bodies()
    {
        var a = F("Sir Aldric");
        var b = F("Blightroot");
        var meet = Math.Max(Stop(a, b), Stop(b, a));
        Assert.True(meet > a.AttackRangeMetres, "the old 1.4 m origin range put the bodies inside each other");
        Assert.True(meet - Depth(a) - Depth(b) >= BattleSpacing.StrikeGapMetres - 1e-4f);
    }

    [Fact]
    public void Two_lunges_at_once_never_close_past_contact()
    {
        foreach (var hn in Heroes)
        {
            foreach (var vn in Villains)
            {
                var h = F(hn);
                var v = F(vn);
                var (dh, dv) = (Depth(h), Depth(v));
                var slot = Math.Min(Stop(h, v), Stop(v, h));
                // Both slide their biggest reach at once (worst case): the hero first, the villain sees the hero's slide.
                var hs = BattleSpacing.ClampReach(2f, slot, 0f, dh, dv);
                var vs = BattleSpacing.ClampReach(2f, slot, hs, dv, dh);
                Assert.True(slot - hs - vs >= BattleSpacing.MinDistance(dh, dv) - 1e-4f, hn + " vs " + vn);
            }
        }
    }

    [Fact]
    public void Ranged_fighters_already_out_of_reach_keep_their_range()
    {
        var rowan = F("Rowan");
        var bonequill = F("Bonequill");
        Assert.Equal(rowan.AttackRangeMetres, Stop(rowan, bonequill), 3);
        Assert.Equal(bonequill.AttackRangeMetres, Stop(bonequill, rowan), 3);
    }

    [Fact]
    public void Measurement_raises_the_estimate_but_is_capped_and_never_lowers_it()
    {
        Assert.Equal(0.5f, BattleSpacing.Measured(0.5f, 0.2f), 3);
        Assert.Equal(0.6f, BattleSpacing.Measured(0.5f, 0.6f), 3);
        Assert.Equal(0.5f * BattleSpacing.MeasureCap, BattleSpacing.Measured(0.5f, 9f), 3);
    }

    [Fact]
    public void Overlap_pushes_out_to_contact_and_backward_slides_pass()
    {
        Assert.Equal(0f, BattleSpacing.Overlap(2f, 0.4f, 0.4f));
        Assert.Equal(BattleSpacing.MinDistance(0.4f, 0.4f) - 0.5f, BattleSpacing.Overlap(0.5f, 0.4f, 0.4f), 4);
        Assert.Equal(-0.3f, BattleSpacing.ClampReach(-0.3f, 0.1f, 0f, 0.4f, 0.4f));
    }

    [Fact]
    public void Pairings_table()
    {
        foreach (var hn in Heroes)
        {
            foreach (var vn in Villains)
            {
                var h = F(hn);
                var v = F(vn);
                _out.WriteLine($"PAIR|{hn}|{vn}|{h.AttackRangeMetres:0.00}->{Stop(h, v):0.00}|{v.AttackRangeMetres:0.00}->{Stop(v, h):0.00}");
            }
        }
    }
}
