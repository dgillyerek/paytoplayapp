using Grove.Domain.Board;

namespace Grove.Domain.Producer
{
    public abstract record SpitResult
    {
        private SpitResult()
        {
        }

        public sealed record Ok(PieceId Item) : SpitResult
        {
            /// <summary>Board cell the spit landed on. Set by <c>CrateTapService</c> for the arc flyer.</summary>
            public GridPos? At { get; init; }
        }

        public sealed record Failed(string Reason) : SpitResult;
    }
}
