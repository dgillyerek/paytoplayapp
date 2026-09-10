using System;
using System.Collections.Generic;
using System.Text;

namespace Grove.Domain.Orders
{
    /// <summary>
    /// Compact on-card copy so three dock cards cannot overflow into each other or the board.
    /// Titles from Product (e.g. “Order 1”); progress is one short line per requirement.
    /// </summary>
    public static class OrderCardCopy
    {
        public static string Compact(OrderSpec order, bool active, IReadOnlyList<int>? haveCounts = null)
        {
            if (order == null)
            {
                return "";
            }

            var text = new StringBuilder();
            text.Append(string.IsNullOrEmpty(order.Title) ? "Order" : order.Title);
            if (!active || order.Requirements == null || order.Requirements.Count == 0)
            {
                return text.ToString();
            }

            var limit = Math.Min(2, order.Requirements.Count);
            for (var i = 0; i < limit; i++)
            {
                var have = haveCounts != null && i < haveCounts.Count ? haveCounts[i] : 0;
                text.Append('\n');
                text.Append(have);
                text.Append('/');
                text.Append(order.Requirements[i].Count);
            }

            return text.ToString();
        }
    }
}
