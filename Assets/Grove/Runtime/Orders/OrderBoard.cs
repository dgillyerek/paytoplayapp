using System;
using System.Collections.Generic;
using Grove.Domain.Board;

namespace Grove.Domain.Orders
{
    /// <summary>Thin DEV-005: three sequential orders. Deliver consumes matching pieces from the board.</summary>
    public sealed class OrderBoard
    {
        private readonly IReadOnlyList<OrderSpec> _orders;

        public OrderBoard(IReadOnlyList<OrderSpec> orders)
        {
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
        }

        public IReadOnlyList<OrderSpec> All => _orders;

        public int CompletedCount { get; private set; }

        public bool AllComplete => CompletedCount >= _orders.Count;

        public OrderSpec? Active =>
            CompletedCount < _orders.Count ? _orders[CompletedCount] : null;

        public bool CanDeliver(BoardGrid board) =>
            Active is { } order && board.Has(order.Requirements);

        public bool TryDeliver(BoardGrid board)
        {
            if (Active is not { } order)
            {
                return false;
            }

            if (!board.TryConsume(order.Requirements))
            {
                return false;
            }

            CompletedCount++;
            return true;
        }
    }
}
