using System;

namespace Grove.Domain.Board
{
    /// <summary>
    /// Occupant of a single cell. Count is 1 or 2 of the same piece;
    /// reaching the recipe input count (3) is handled by the merge loop, not stored.
    /// </summary>
    public readonly record struct PieceStack(PieceId Id, int Count)
    {
        public int Count { get; } = Count > 0
            ? Count
            : throw new ArgumentOutOfRangeException(nameof(Count), Count, "Stack count must be > 0.");

        public PieceStack WithCount(int count) => new(Id, count);
    }
}
