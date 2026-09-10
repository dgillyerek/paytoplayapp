using Grove.Domain.Board;
using Grove.Domain.Merge;

namespace Grove.Domain.Tests;

public sealed class MergeLoopTests
{
    [Fact]
    public void Three_to_one_merge_produces_next_tier_at_drop_cell()
    {
        var session = NewSession();
        var a = new GridPos(0, 0);
        var b = new GridPos(1, 0);
        session.Board.Place(a, new PieceStack(GroveCatalog.Pebble, 1));
        session.Board.Place(b, new PieceStack(GroveCatalog.Pebble, 2));

        var result = session.TryDrag(a, b);

        var applied = Assert.IsType<DragResult.Applied>(result);
        Assert.NotNull(applied.Merge);
        Assert.Equal(GroveCatalog.Pebble, applied.Merge!.Consumed);
        Assert.Equal(GroveCatalog.Sprout, applied.Merge.Produced);
        Assert.Equal(b, applied.Merge.At);
        Assert.False(session.Board.TryGet(a, out _));
        Assert.True(session.Board.TryGet(b, out var produced));
        Assert.Equal(GroveCatalog.Sprout, produced.Id);
        Assert.Equal(1, produced.Count);
    }

    [Fact]
    public void Two_matching_pieces_stack_without_merging()
    {
        var session = NewSession();
        var a = new GridPos(0, 1);
        var b = new GridPos(1, 1);
        session.Board.Place(a, new PieceStack(GroveCatalog.Pebble, 1));
        session.Board.Place(b, new PieceStack(GroveCatalog.Pebble, 1));

        var result = session.TryDrag(a, b);

        var applied = Assert.IsType<DragResult.Applied>(result);
        Assert.Null(applied.Merge);
        Assert.True(session.Board.TryGet(b, out var stack));
        Assert.Equal(2, stack.Count);
        Assert.Equal(GroveCatalog.Pebble, stack.Id);
    }

    [Fact]
    public void Empty_destination_is_a_plain_move()
    {
        var session = NewSession();
        var a = new GridPos(3, 3);
        var b = new GridPos(4, 4);
        session.Board.Place(a, new PieceStack(GroveCatalog.Sapling, 1));

        var result = session.TryDrag(a, b);

        Assert.IsType<DragResult.Applied>(result);
        Assert.False(session.Board.TryGet(a, out _));
        Assert.True(session.Board.TryGet(b, out var moved));
        Assert.Equal(GroveCatalog.Sapling, moved.Id);
    }

    [Fact]
    public void Catalog_is_data_driven_three_to_one()
    {
        var catalog = GroveCatalog.CreateDefault();
        Assert.True(catalog.TryGet(GroveCatalog.Pebble, out var recipe));
        Assert.True(recipe.IsStandardThreeToOne);
        Assert.Equal(GroveCatalog.Sprout, recipe.Output);
        Assert.False(catalog.TryGet(GroveCatalog.Grove, out _));
    }

    [Fact]
    public void Full_chain_pebble_to_grove()
    {
        Assert.True(GroveCatalog.CreateDefault().TryGet(GroveCatalog.Tree, out var treeRecipe));
        Assert.Equal(GroveCatalog.Grove, treeRecipe.Output);
    }

    private static MergeSession NewSession() =>
        new(new BoardGrid(), GroveCatalog.CreateDefault());
}
