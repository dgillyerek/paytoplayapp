using Grove.Domain.Board;

namespace Grove.Domain.Producer
{
    public abstract record SpitResult
    {
        private SpitResult()
        {
        }

        public sealed record Ok(PieceId Item) : SpitResult;

        public sealed record Failed(string Reason) : SpitResult;
    }
}
