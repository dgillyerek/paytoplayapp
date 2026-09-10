#nullable enable
using System;

namespace Grove.Domain.Board
{
    /// <summary>
    /// Occupant of a single cell. Count is 1 or 2 of the same piece;
    /// reaching the recipe input count (3) is handled by the merge loop, not stored.
    /// </summary>
    public readonly struct PieceStack : IEquatable<PieceStack>
    {
        public PieceStack(PieceId id, int count)
        {
            Id = id;
            Count = count > 0
                ? count
                : throw new ArgumentOutOfRangeException(nameof(count), count, "Stack count must be > 0.");
        }

        public PieceId Id { get; }

        public int Count { get; }

        public PieceStack WithCount(int count) => new PieceStack(Id, count);

        public bool Equals(PieceStack other) => Id.Equals(other.Id) && Count == other.Count;

        public override bool Equals(object? obj) => obj is PieceStack other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, Count);

        public static bool operator ==(PieceStack left, PieceStack right) => left.Equals(right);

        public static bool operator !=(PieceStack left, PieceStack right) => !left.Equals(right);
    }
}
