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

    [Fact]
    public void Finds_two_matching_pieces_even_when_a_third_type_is_first()
    {
        var board = new BoardGrid();
        board.Place(new GridPos(0, 0), new PieceStack(new PieceId("herb_t1"), 1));
        board.Place(new GridPos(1, 0), new PieceStack(new PieceId("wildflower_t1"), 1));
        board.Place(new GridPos(2, 0), new PieceStack(new PieceId("wildflower_t1"), 1));
        Assert.True(board.TryFindTwoMatching(out var a, out var b));
        Assert.Equal(new GridPos(1, 0), a);
        Assert.Equal(new GridPos(2, 0), b);
    }
}
