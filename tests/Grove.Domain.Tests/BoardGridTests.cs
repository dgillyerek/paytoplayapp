using Grove.Domain.Board;

namespace Grove.Domain.Tests;

public sealed class BoardGridTests
{
    [Fact]
    public void Board_is_7_by_5()
    {
        var board = new BoardGrid();
        Assert.Equal(7, BoardGrid.Columns);
        Assert.Equal(5, BoardGrid.Rows);
        Assert.Equal(35, board.CellCount);
        Assert.Equal(0, board.OccupiedCount);
    }

    [Fact]
    public void Place_and_read_back()
    {
        var board = new BoardGrid();
        var pos = new GridPos(6, 4);
        board.Place(pos, new PieceStack(new PieceId("pebble"), 1));
        Assert.True(board.TryGet(pos, out var stack));
        Assert.Equal("pebble", stack.Id.Value);
        Assert.Equal(1, stack.Count);
        Assert.Equal(1, board.OccupiedCount);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(7, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 5)]
    public void Out_of_bounds_is_rejected(int x, int y)
    {
        var board = new BoardGrid();
        Assert.False(board.InBounds(new GridPos(x, y)));
    }
}
