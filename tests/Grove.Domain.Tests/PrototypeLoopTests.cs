using Grove.Domain.Board;
using Grove.Domain.Energy;
using Grove.Domain.Merge;
using Grove.Domain.Producer;
using Grove.Domain.Orders;

namespace Grove.Domain.Tests;

public sealed class PrototypeLoopTests
{
    [Fact]
    public void Produce_merge_to_wildflower_t3_and_complete_order_1()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.NotNull(catalog.GardenCrate);
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var energy = new EnergyWallet(catalog.Energy, clock);
        var crate = new GardenCrate(catalog.GardenCrate!, ScriptedRandom.Always(0), ftueFreeTapRemaining: true);
        var session = new MergeSession(new BoardGrid(), catalog.Recipes);
        var tap = new CrateTapService(crate, session.Board, energy, clock);
        var orders = new OrderBoard(catalog.Orders);

        for (var i = 0; i < 9; i++)
        {
            var spit = tap.TryTap();
            Assert.IsType<SpitResult.Ok>(spit);
            Assert.Equal(GroveCatalog.WildflowerT1, ((SpitResult.Ok)spit).Item);
        }

        MergeThree(session, new GridPos(0, 0), new GridPos(0, 1), new GridPos(0, 2));
        MergeThree(session, new GridPos(0, 3), new GridPos(0, 4), new GridPos(1, 0));
        MergeThree(session, new GridPos(1, 1), new GridPos(1, 2), new GridPos(1, 3));

        Assert.True(session.Board.TryGet(new GridPos(0, 1), out var a) && a.Id.Equals(GroveCatalog.WildflowerT2));
        Assert.True(session.Board.TryGet(new GridPos(0, 4), out var b) && b.Id.Equals(GroveCatalog.WildflowerT2));
        Assert.True(session.Board.TryGet(new GridPos(1, 2), out var c) && c.Id.Equals(GroveCatalog.WildflowerT2));

        Assert.IsType<DragResult.Applied>(session.TryDrag(new GridPos(0, 1), new GridPos(0, 4)));
        Assert.True(session.Board.TryGet(new GridPos(0, 4), out var stacked) && stacked.Count == 2);
        var merged = session.TryDrag(new GridPos(1, 2), new GridPos(0, 4));
        var applied = Assert.IsType<DragResult.Applied>(merged);
        Assert.Equal(GroveCatalog.WildflowerT3, applied.Merge!.Produced);

        Assert.Equal("order_1", orders.Active!.Id);
        Assert.True(orders.TryDeliver(session.Board));
        Assert.Equal(1, orders.CompletedCount);
        Assert.Equal("order_2", orders.Active!.Id);
        Assert.Equal(0, session.Board.CountItem(GroveCatalog.WildflowerT3));
    }

    private static void MergeThree(MergeSession session, GridPos a, GridPos b, GridPos c)
    {
        Assert.IsType<DragResult.Applied>(session.TryDrag(a, b));
        var result = session.TryDrag(c, b);
        var applied = Assert.IsType<DragResult.Applied>(result);
        Assert.NotNull(applied.Merge);
    }
}
