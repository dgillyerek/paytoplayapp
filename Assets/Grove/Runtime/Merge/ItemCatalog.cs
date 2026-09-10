using System;
using System.Collections.Generic;
using System.Linq;
using Grove.Domain.Board;

namespace Grove.Domain.Merge
{
    /// <summary>Lookup of item definitions keyed by piece id.</summary>
    public sealed class ItemCatalog
    {
        private readonly Dictionary<string, ItemDefinition> _byId;
        private readonly ItemDefinition[] _all;

        public ItemCatalog(IEnumerable<ItemDefinition> items)
        {
            _byId = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (item.Tier < 1)
                {
                    throw new ArgumentException($"Item '{item.Id}' has invalid tier {item.Tier}.", nameof(items));
                }

                if (string.IsNullOrWhiteSpace(item.Chain))
                {
                    throw new ArgumentException($"Item '{item.Id}' is missing a chain id.", nameof(items));
                }

                if (!_byId.TryAdd(item.Id.Value, item))
                {
                    throw new ArgumentException($"Duplicate item id '{item.Id}'.", nameof(items));
                }
            }

            _all = _byId.Values.ToArray();
        }

        public IReadOnlyList<ItemDefinition> All => _all;

        public bool TryGet(PieceId id, out ItemDefinition item) =>
            _byId.TryGetValue(id.Value, out item!);

        public ItemDefinition Require(PieceId id) =>
            TryGet(id, out var item)
                ? item
                : throw new InvalidOperationException($"No item '{id}'.");

        public IReadOnlyList<ItemDefinition> Chain(string chainId) =>
            _all.Where(i => string.Equals(i.Chain, chainId, StringComparison.Ordinal))
                .OrderBy(i => i.Tier)
                .ToArray();

        public int MaxTier(string chainId)
        {
            var max = 0;
            foreach (var item in _all)
            {
                if (string.Equals(item.Chain, chainId, StringComparison.Ordinal) && item.Tier > max)
                {
                    max = item.Tier;
                }
            }

            return max;
        }
    }
}
