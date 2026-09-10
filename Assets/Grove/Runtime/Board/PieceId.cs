using System;

namespace Grove.Domain.Board
{
    /// <summary>Stable catalog key for a merge-tier piece (e.g. "pebble", "sprout").</summary>
    public readonly record struct PieceId(string Value)
    {
        public string Value { get; } = string.IsNullOrWhiteSpace(Value)
            ? throw new ArgumentException("Piece id is required.", nameof(Value))
            : Value;

        public override string ToString() => Value;

        public static implicit operator string(PieceId id) => id.Value;
    }
}
