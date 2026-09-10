using Grove.Domain.Board;

namespace Grove.Domain.Merge
{
    public abstract record DragResult
    {
        private DragResult()
        {
        }

        /// <summary>Move (and optional merge) was committed.</summary>
        public sealed record Applied(MergeOutcome? Merge) : DragResult;

        /// <summary>Illegal drop: board is unchanged and the piece must snap back.</summary>
        public sealed record SnapBack(string Reason) : DragResult;
    }

    public sealed record MergeOutcome(PieceId Consumed, PieceId Produced, GridPos At);
}
