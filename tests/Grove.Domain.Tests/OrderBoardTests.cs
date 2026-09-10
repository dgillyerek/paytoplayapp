using Grove.Domain.Board;
using Grove.Domain.Merge;
using Grove.Domain.Orders;

namespace Grove.Domain.Tests;

public sealed class OrderBoardTests
{
    [Fact]
    public void Deliver_fails_until_requirement_is_on_the_board()
    {
        var catalog = CatalogLoader.LoadDefault();
        var orders = new OrderBoard(catalog.Orders, catalog.Copy.OrderSlotsMax);
        var board = new BoardGrid();
        Assert.False(orders.TryDeliver(board));
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT2, 1));
        Assert.True(orders.TryDeliver(board));
        Assert.False(board.TryGet(new GridPos(0, 0), out _));
        Assert.Equal("order_2", orders.Active!.Id);
    }

    [Fact]
    public void Scripted_orders_one_through_six_then_front_garden_milestone()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.Equal(6, catalog.Orders.Count);
        var orders = new OrderBoard(catalog.Orders, catalog.Copy.OrderSlotsMax);
        var board = new BoardGrid();

        Assert.Equal(3, orders.MaxVisibleSlots);
        Assert.Equal(3, orders.Visible.Count);
        Assert.Equal("order_1", orders.Visible[0].Id);
        Assert.Equal("order_3", orders.Visible[2].Id);

        Deliver(board, orders, GroveCatalog.WildflowerT2);
        Deliver(board, orders, GroveCatalog.WildflowerT3);
        Deliver(board, orders, GroveCatalog.HerbT2);

        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT4, 1));
        board.Place(new GridPos(1, 0), new PieceStack(GroveCatalog.HerbT2, 1));
        Assert.True(orders.TryDeliver(board));
        Assert.Equal("order_5", orders.Active!.Id);

        Deliver(board, orders, GroveCatalog.ToolT2);
        Assert.False(orders.MilestoneReached);
        Assert.True(orders.Active!.CompletesMilestone);

        Deliver(board, orders, GroveCatalog.WildflowerT5);
        Assert.True(orders.AllComplete);
        Assert.True(orders.MilestoneReached);
        Assert.Null(orders.Active);
        Assert.Empty(orders.Visible);
        Assert.False(orders.TryDeliver(board));
    }

    [Fact]
    public void Order_five_requires_tools_t2_stick()
    {
        var catalog = CatalogLoader.LoadDefault();
        var order5 = catalog.Orders[4];
        Assert.Equal("order_5", order5.Id);
        Assert.Equal(GroveCatalog.ToolT2, order5.Requirements[0].Item);
        Assert.Equal("Stick", catalog.Items.Require(GroveCatalog.ToolT2).DisplayName);
        Assert.Equal(20, order5.CoinReward);
        Assert.Equal(10, order5.XpReward);
        Assert.Contains("stick", order5.SpokenLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Splash_stub_emits_front_garden_restored_once()
    {
        var catalog = CatalogLoader.LoadDefault();
        var splash = new SplashStub(catalog.Copy);
        Assert.Equal("Project Grove", splash.BootTitle);
        Assert.Equal("Front Garden", splash.AreaName);
        Assert.Equal("Restore the Front Garden", splash.Goal);
        Assert.Null(splash.TryConsumeMilestone(false));
        Assert.Equal("Front Garden Restored", splash.TryConsumeMilestone(true));
        Assert.Null(splash.TryConsumeMilestone(true));
    }

    private static void Deliver(BoardGrid board, OrderBoard orders, PieceId item)
    {
        board.Place(new GridPos(0, 0), new PieceStack(item, 1));
        Assert.True(orders.TryDeliver(board));
    }
}
