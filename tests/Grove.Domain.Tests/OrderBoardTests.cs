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
        var orders = new OrderBoard(catalog.Orders);
        var board = new BoardGrid();
        Assert.False(orders.TryDeliver(board));
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT3, 1));
        Assert.True(orders.TryDeliver(board));
        Assert.False(board.TryGet(new GridPos(0, 0), out _));
        Assert.Equal("order_2", orders.Active!.Id);
    }

    [Fact]
    public void Three_orders_then_complete()
    {
        var catalog = CatalogLoader.LoadDefault();
        var orders = new OrderBoard(catalog.Orders);
        var board = new BoardGrid();
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT3, 1));
        Assert.True(orders.TryDeliver(board));
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT4, 1));
        Assert.True(orders.TryDeliver(board));
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT5, 1));
        Assert.True(orders.TryDeliver(board));
        Assert.True(orders.AllComplete);
        Assert.Null(orders.Active);
        Assert.False(orders.TryDeliver(board));
    }
}
