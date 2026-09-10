#nullable enable
using System;

namespace Grove.Domain.Board
{
    /// <summary>Stable catalog key for a merge-tier piece (e.g. "pebble", "sprout").</summary>
    public readonly struct PieceId : IEquatable<PieceId>
    {
        public PieceId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Piece id is required.", nameof(value))
                : value;
        }

        public string Value { get; }

        public bool Equals(PieceId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is PieceId other && Equals(other);

        public override int GetHashCode() =>
            Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;

        public static bool operator ==(PieceId left, PieceId right) => left.Equals(right);

        public static bool operator !=(PieceId left, PieceId right) => !left.Equals(right);

        public override string ToString() => Value;

        public static implicit operator string(PieceId id) => id.Value;
    }
}
