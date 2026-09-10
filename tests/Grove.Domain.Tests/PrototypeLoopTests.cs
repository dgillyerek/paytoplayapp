using Grove.Domain.Board;
using Grove.Domain.Energy;
using Grove.Domain.Merge;
using Grove.Domain.Producer;
using Grove.Domain.Orders;

namespace Grove.Domain.Tests;

public sealed class PrototypeLoopTests
{
    [Fact]
    public void Produce_merge_to_sprout_and_complete_order_1()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.NotNull(catalog.GardenCrate);
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var energy = new EnergyWallet(catalog.Energy, clock);
        var crate = new GardenCrate(catalog.GardenCrate!, ScriptedRandom.Always(0), ftueFreeTapRemaining: true);
        var session = new MergeSession(new BoardGrid(), catalog.Recipes);
        var tap = new CrateTapService(crate, session.Board, energy, clock);
        var orders = new OrderBoard(catalog.Orders);

        for (var i = 0; i < 3; i++)
        {
            var spit = tap.TryTap();
            Assert.IsType<SpitResult.Ok>(spit);
            Assert.Equal(GroveCatalog.WildflowerT1, ((SpitResult.Ok)spit).Item);
        }

        MergeThree(session, new GridPos(0, 0), new GridPos(0, 1), new GridPos(0, 2));
        Assert.True(session.Board.TryGet(new GridPos(0, 1), out var sprout) && sprout.Id.Equals(GroveCatalog.WildflowerT2));

        Assert.Equal("order_1", orders.Active!.Id);
        Assert.Equal(10, orders.Active.CoinReward);
        Assert.Equal(5, orders.Active.XpReward);
        Assert.True(orders.TryDeliver(session.Board));
        Assert.Equal(1, orders.CompletedCount);
        Assert.Equal("order_2", orders.Active!.Id);
        Assert.Equal(0, session.Board.CountItem(GroveCatalog.WildflowerT2));
    }

    [Fact]
    public void Produce_merge_twig_to_stick_satisfies_order_5_item()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.Contains(catalog.GardenCrate!.Outputs, output => output.ItemId == "tool_t1");
        Assert.True(catalog.Recipes.TryGet(GroveCatalog.ToolT1, out var recipe));
        Assert.Equal(GroveCatalog.ToolT2, recipe.Output);

        var session = new MergeSession(new BoardGrid(), catalog.Recipes);
        session.Board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.ToolT1, 1));
        session.Board.Place(new GridPos(0, 1), new PieceStack(GroveCatalog.ToolT1, 1));
        session.Board.Place(new GridPos(0, 2), new PieceStack(GroveCatalog.ToolT1, 1));
        MergeThree(session, new GridPos(0, 0), new GridPos(0, 1), new GridPos(0, 2));

        Assert.Equal(1, session.Board.CountItem(GroveCatalog.ToolT2));
        var orders = new OrderBoard(catalog.Orders);
        SkipTo(orders, session.Board, 4);
        Assert.Equal("order_5", orders.Active!.Id);
        Assert.True(orders.TryDeliver(session.Board));
        Assert.Equal("order_6", orders.Active!.Id);
        Assert.Equal(0, session.Board.CountItem(GroveCatalog.ToolT2));
    }

    private static void SkipTo(OrderBoard orders, BoardGrid board, int completed)
    {
        var fills = new[]
        {
            GroveCatalog.WildflowerT2,
            GroveCatalog.WildflowerT3,
            GroveCatalog.HerbT2,
            GroveCatalog.WildflowerT4,
        };
        for (var i = 0; i < completed; i++)
        {
            if (i == 3)
            {
                board.Place(new GridPos(2, 0), new PieceStack(GroveCatalog.HerbT2, 1));
            }

            board.Place(new GridPos(1, 0), new PieceStack(fills[i], 1));
            Assert.True(orders.TryDeliver(board));
        }
    }

    private static void MergeThree(MergeSession session, GridPos a, GridPos b, GridPos c)
    {
        Assert.IsType<DragResult.Applied>(session.TryDrag(a, b));
        var result = session.TryDrag(c, b);
        var applied = Assert.IsType<DragResult.Applied>(result);
        Assert.NotNull(applied.Merge);
    }
}
