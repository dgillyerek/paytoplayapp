using System.Linq;
using Survival.Domain.View;
using Xunit;

namespace Survival.Domain.Tests;

public sealed class SideRailTests
{
    [Fact]
    public void Pinned_first_then_by_order()
    {
        var rail = new SideRail();
        rail.Register(new RailEntryDef("bounty", 0.39f));
        rail.Register(new RailEntryDef("orb", 0.30f));
        rail.Register(new RailEntryDef("runner", 5f, Pinned: true));
        Assert.Equal(new[] { "runner", "orb", "bounty" }, rail.Visible());
    }

    [Fact]
    public void Folds_overflow_behind_more_and_expands()
    {
        var rail = new SideRail();
        for (var i = 0; i < 7; i++)
        {
            rail.Register(new RailEntryDef("e" + i, i));
        }

        var collapsed = rail.Visible();
        Assert.Equal(SideRail.MaxCollapsed, collapsed.Count);
        Assert.Equal(SideRail.MoreId, collapsed.Last());
        Assert.Equal(3, rail.Overflow);

        rail.Toggle();
        Assert.Equal(8, rail.Visible().Count);
        Assert.Equal((1, 2), SideRail.Layout(7));
    }

    [Fact]
    public void Hidden_and_unregistered_entries_leave_the_rail()
    {
        var rail = new SideRail();
        var changes = 0;
        rail.Changed += () => changes++;
        rail.Register(new RailEntryDef("a", 1));
        rail.Register(new RailEntryDef("b", 2));
        rail.SetShown("a", false);
        Assert.Equal(new[] { "b" }, rail.Visible());
        rail.SetShown("a", true);
        rail.Unregister("b");
        Assert.Equal(new[] { "a" }, rail.Visible());
        Assert.False(rail.Register(new RailEntryDef(SideRail.MoreId, 0)));
        Assert.Equal(5, changes);
    }

    [Fact]
    public void Rail_slot_sits_on_the_left_edge_below_the_quest_strip()
    {
        var pack = TestPaths.LoadPack();
        var rail = pack.Hud["side_rail"];
        Assert.True(rail.XMin < 0.05f && rail.XMax < 0.2f);
        Assert.True(rail.YMax <= pack.Hud["quest_tracker"].YMin); // bottom-up fractions
    }
}
