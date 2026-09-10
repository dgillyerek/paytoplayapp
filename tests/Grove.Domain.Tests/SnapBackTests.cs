using Grove.Domain.Board;
using Grove.Domain.Merge;

namespace Grove.Domain.Tests;

public sealed class SnapBackTests
{
    [Fact]
    public void Type_mismatch_snaps_back_and_leaves_board_unchanged()
    {
        var session = new MergeSession(new BoardGrid(), GroveCatalog.CreateDefault());
        var from = new GridPos(0, 0);
        var to = new GridPos(1, 0);
        session.Board.Place(from, new PieceStack(GroveCatalog.Pebble, 1));
        session.Board.Place(to, new PieceStack(GroveCatalog.Sprout, 1));

        var result = session.TryDrag(from, to);

        var snap = Assert.IsType<DragResult.SnapBack>(result);
        Assert.Equal("type-mismatch", snap.Reason);
        Assert.True(session.Board.TryGet(from, out var stillPebble));
        Assert.Equal(GroveCatalog.Pebble, stillPebble.Id);
        Assert.True(session.Board.TryGet(to, out var stillSprout));
        Assert.Equal(GroveCatalog.Sprout, stillSprout.Id);
    }

    [Fact]
    public void Empty_source_snaps_back()
    {
        var session = new MergeSession(new BoardGrid(), GroveCatalog.CreateDefault());
        var result = session.TryDrag(new GridPos(0, 0), new GridPos(1, 0));
        Assert.Equal("empty-source", Assert.IsType<DragResult.SnapBack>(result).Reason);
    }

    [Fact]
    public void Same_cell_snaps_back()
    {
        var session = new MergeSession(new BoardGrid(), GroveCatalog.CreateDefault());
        session.Board.Place(new GridPos(2, 2), new PieceStack(GroveCatalog.Pebble, 1));
        var result = session.TryDrag(new GridPos(2, 2), new GridPos(2, 2));
        Assert.Equal("same-cell", Assert.IsType<DragResult.SnapBack>(result).Reason);
    }

    [Fact]
    public void Out_of_bounds_snaps_back()
    {
        var session = new MergeSession(new BoardGrid(), GroveCatalog.CreateDefault());
        session.Board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.Pebble, 1));
        var result = session.TryDrag(new GridPos(0, 0), new GridPos(9, 9));
        Assert.Equal("out-of-bounds", Assert.IsType<DragResult.SnapBack>(result).Reason);
        Assert.True(session.Board.TryGet(new GridPos(0, 0), out _));
    }

    [Fact]
    public void Max_tier_without_recipe_snaps_back()
    {
        var session = new MergeSession(new BoardGrid(), GroveCatalog.CreateDefault());
        session.Board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.Grove, 1));
        session.Board.Place(new GridPos(1, 0), new PieceStack(GroveCatalog.Grove, 1));
        var result = session.TryDrag(new GridPos(0, 0), new GridPos(1, 0));
        Assert.Equal("no-recipe", Assert.IsType<DragResult.SnapBack>(result).Reason);
    }
}
