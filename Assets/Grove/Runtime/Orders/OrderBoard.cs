using System;
using System.Collections.Generic;
using Grove.Domain.Board;

namespace Grove.Domain.Orders
{
    /// <summary>Scripted orders 1–6 (max 3 visible slots). Deliver consumes matching pieces from the board.</summary>
    public sealed class OrderBoard
    {
        public const int DefaultMaxVisibleSlots = 3;

        private readonly IReadOnlyList<OrderSpec> _orders;
        private readonly int _maxVisibleSlots;

        public OrderBoard(IReadOnlyList<OrderSpec> orders, int maxVisibleSlots = DefaultMaxVisibleSlots)
        {
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            if (maxVisibleSlots < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxVisibleSlots), maxVisibleSlots, "Need at least one order slot.");
            }

            _maxVisibleSlots = maxVisibleSlots;
        }

        public IReadOnlyList<OrderSpec> All => _orders;

        public int TotalCount => _orders.Count;

        public int MaxVisibleSlots => _maxVisibleSlots;

        public int CompletedCount { get; private set; }

        public bool AllComplete => CompletedCount >= _orders.Count;

        public bool MilestoneReached { get; private set; }

        public OrderSpec? Active =>
            CompletedCount < _orders.Count ? _orders[CompletedCount] : null;

        /// <summary>Upcoming scripted orders, capped at <see cref="MaxVisibleSlots"/> (no RNG queue).</summary>
        public IReadOnlyList<OrderSpec> Visible
        {
            get
            {
                var remaining = _orders.Count - CompletedCount;
                if (remaining <= 0)
                {
                    return Array.Empty<OrderSpec>();
                }

                var take = Math.Min(_maxVisibleSlots, remaining);
                var slots = new OrderSpec[take];
                for (var i = 0; i < take; i++)
                {
                    slots[i] = _orders[CompletedCount + i];
                }

                return slots;
            }
        }

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
            if (order.CompletesMilestone)
            {
                MilestoneReached = true;
            }

            return true;
        }
    }
}
