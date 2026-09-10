using System.Collections.Generic;
using Grove.Domain.Board;

namespace Grove.Domain.Orders
{
    public sealed record OrderRequirement(PieceId Item, int Count);

    public sealed record OrderSpec(
        string Id,
        string Title,
        string Hint,
        IReadOnlyList<OrderRequirement> Requirements,
        int CoinReward = 0,
        int XpReward = 0,
        string MayaLine = "",
        bool CompletesMilestone = false)
    {
        public string SpokenLine =>
            string.IsNullOrEmpty(MayaLine) ? Hint : MayaLine;
    }
}
