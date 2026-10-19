#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Economy
{
    /// <summary>Player balances keyed by SURV-P0 res.* ids (res.soft gold, res.stone, res.wood, res.food, res.hard gems, res.speedup_m).</summary>
    public sealed class Wallet
    {
        private readonly Dictionary<string, int> _amounts;

        public Wallet(IReadOnlyDictionary<string, int>? starting = null)
        {
            _amounts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (starting != null)
            {
                foreach (var pair in starting)
                {
                    _amounts[pair.Key] = Math.Max(0, pair.Value);
                }
            }
        }

        /// <summary>(resource id, new amount, delta).</summary>
        public event Action<string, int, int>? Changed;

        public IReadOnlyDictionary<string, int> Balances => _amounts;

        public int Get(string resourceId) => _amounts.TryGetValue(resourceId, out var v) ? v : 0;

        /// <summary>Adds (or removes with a negative delta), clamped at 0. Returns the new balance.</summary>
        public int Add(string resourceId, int delta)
        {
            var before = Get(resourceId);
            var next = before + delta;
            if (next < 0)
            {
                next = 0;
            }

            _amounts[resourceId] = next;
            if (next != before)
            {
                Changed?.Invoke(resourceId, next, next - before);
            }

            return next;
        }

        public bool CanAfford(string resourceId, int amount) => amount <= 0 || Get(resourceId) >= amount;

        public bool CanAfford(IReadOnlyDictionary<string, int> costs)
        {
            foreach (var pair in costs)
            {
                if (!CanAfford(pair.Key, pair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TrySpend(string resourceId, int amount)
        {
            if (!CanAfford(resourceId, amount))
            {
                return false;
            }

            if (amount > 0)
            {
                Add(resourceId, -amount);
            }

            return true;
        }

        /// <summary>All-or-nothing spend.</summary>
        public bool TrySpend(IReadOnlyDictionary<string, int> costs)
        {
            if (!CanAfford(costs))
            {
                return false;
            }

            foreach (var pair in costs)
            {
                if (pair.Value > 0)
                {
                    Add(pair.Key, -pair.Value);
                }
            }

            return true;
        }

        public void Grant(IReadOnlyDictionary<string, int> rewards)
        {
            foreach (var pair in rewards)
            {
                if (pair.Value != 0)
                {
                    Add(pair.Key, pair.Value);
                }
            }
        }
    }
}
