using Grove.Domain.Board;

namespace Grove.Domain.Merge
{
    /// <summary>Data-driven 3→1 recipe: consume <see cref="InputCount"/> of <see cref="Input"/>, spawn <see cref="Output"/>.</summary>
    public sealed record MergeRecipe(PieceId Input, int InputCount, PieceId Output, int OutputCount)
    {
        public const int DefaultInputCount = 3;
        public const int DefaultOutputCount = 1;

        public bool IsStandardThreeToOne =>
            InputCount == DefaultInputCount && OutputCount == DefaultOutputCount;
    }
}
