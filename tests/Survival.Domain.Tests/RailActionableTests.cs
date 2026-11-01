using System.Linq;
using Survival.Domain.Taps;
using Survival.Domain.View;
using Xunit;

namespace Survival.Domain.Tests;

public sealed class RailActionableTests
{
    [Fact]
    public void Daily_and_trophy_show_only_with_a_claim()
    {
        Assert.True(RailActionable.Daily(true));
        Assert.False(RailActionable.Daily(false));
        Assert.True(RailActionable.Trophy(2));
        Assert.False(RailActionable.Trophy(0));
    }

    [Fact]
    public void Crusade_shows_for_a_claim_or_while_premium_is_for_sale()
    {
        Assert.True(RailActionable.Crusade(0, hasPremium: false));
        Assert.True(RailActionable.Crusade(1, hasPremium: true));
        Assert.False(RailActionable.Crusade(0, hasPremium: true));
    }

    [Fact]
    public void Guild_shows_for_help_to_give_or_ask()
    {
        Assert.True(RailActionable.Guild(1, 0));
        Assert.True(RailActionable.Guild(0, 1));
        Assert.False(RailActionable.Guild(0, 0));
    }

    [Fact]
    public void Orb_hides_only_while_resting()
    {
        Assert.False(RailActionable.Orb(OrbState.Resting));
        Assert.True(RailActionable.Orb(OrbState.Glowing));
        Assert.True(RailActionable.Orb(OrbState.Active));
        Assert.True(RailActionable.Orb(OrbState.Complete));
    }

    [Fact]
    public void Bounty_shows_for_ready_active_or_offered()
    {
        Assert.True(RailActionable.Bounty(1, 0, false));
        Assert.True(RailActionable.Bounty(0, 1, false));
        Assert.True(RailActionable.Bounty(0, 0, true));
        Assert.False(RailActionable.Bounty(0, 0, false));
    }

    [Fact]
    public void Well_merchant_events_runner()
    {
        Assert.True(RailActionable.Well(true));
        Assert.False(RailActionable.Well(false));
        Assert.True(RailActionable.Merchant(true, false, true));
        Assert.True(RailActionable.Merchant(true, true, false));
        Assert.False(RailActionable.Merchant(true, false, false));
        Assert.False(RailActionable.Merchant(false, true, true));
        Assert.True(RailActionable.Events("Harvest Fest\nEnds 1h 27m"));
        Assert.False(RailActionable.Events(""));
        Assert.False(RailActionable.Events(null));
        Assert.True(RailActionable.Runner(allBeaten: false));
        Assert.False(RailActionable.Runner(allBeaten: true));
    }

    [Fact]
    public void Hidden_icons_reflow_with_no_gaps_and_more_disappears()
    {
        var rail = new SideRail();
        for (var i = 0; i < 7; i++)
        {
            rail.Register(new RailEntryDef("e" + i, i));
        }

        Assert.Contains(SideRail.MoreId, rail.Visible());
        foreach (var id in new[] { "e1", "e3", "e4", "e6" })
        {
            rail.SetShown(id, false);
        }

        Assert.Equal(new[] { "e0", "e2", "e5" }, rail.Visible());
        Assert.False(rail.NeedsMore);
        rail.SetShown("e6", true);
        Assert.Equal(new[] { "e0", "e2", "e5", "e6" }, rail.Visible());
    }
}
