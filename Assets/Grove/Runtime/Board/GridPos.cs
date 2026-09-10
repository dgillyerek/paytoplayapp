namespace Grove.Domain.Board
{
    /// <summary>Zero-based board coordinate. X is column, Y is row.</summary>
    public readonly record struct GridPos(int X, int Y)
    {
        public override string ToString() => $"({X},{Y})";
    }
}
