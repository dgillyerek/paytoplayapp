using Grove.Domain.Board;

namespace Grove.Domain.Merge
{
    /// <summary>
    /// DEV-001 core loop: drag a stack onto another cell.
    /// Matching stacks combine; reaching the recipe count (3) produces 1 next-tier piece.
    /// Illegal drops return <see cref="DragResult.SnapBack"/> and leave the board unchanged.
    /// </summary>
    public sealed class MergeSession
    {
        public MergeSession(BoardGrid board, MergeCatalog catalog)
        {
            Board = board;
            Catalog = catalog;
        }

        public BoardGrid Board { get; }
        public MergeCatalog Catalog { get; }

        public DragResult TryDrag(GridPos from, GridPos to)
        {
            if (from == to)
            {
                return new DragResult.SnapBack("same-cell");
            }

            if (!Board.InBounds(from) || !Board.InBounds(to))
            {
                return new DragResult.SnapBack("out-of-bounds");
            }

            if (!Board.TryGet(from, out var source))
            {
                return new DragResult.SnapBack("empty-source");
            }

            if (!Board.TryGet(to, out var destination))
            {
                Board.Clear(from);
                Board[to] = source;
                return new DragResult.Applied(Merge: null);
            }

            if (!source.Id.Equals(destination.Id))
            {
                return new DragResult.SnapBack("type-mismatch");
            }

            if (!Catalog.TryGet(source.Id, out var recipe))
            {
                return new DragResult.SnapBack("no-recipe");
            }

            var combined = source.Count + destination.Count;
            if (combined > recipe.InputCount)
            {
                return new DragResult.SnapBack("overstack");
            }

            if (combined < recipe.InputCount)
            {
                Board.Clear(from);
                Board[to] = destination.WithCount(combined);
                return new DragResult.Applied(Merge: null);
            }

            // combined == recipe.InputCount → 3→1 merge
            Board.Clear(from);
            Board[to] = new PieceStack(recipe.Output, recipe.OutputCount);
            return new DragResult.Applied(new MergeOutcome(source.Id, recipe.Output, to));
        }
    }
}
