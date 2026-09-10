using System;
using System.Collections.Generic;

namespace Grove.Domain.Producer
{
    /// <summary>Weighted picker. When an output is marked neverConsecutive, it is omitted from the next roll after it lands.</summary>
    public sealed class WeightedTable
    {
        private readonly IReadOnlyList<WeightedOutput> _outputs;

        public WeightedTable(IReadOnlyList<WeightedOutput> outputs)
        {
            _outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
            if (_outputs.Count == 0)
            {
                throw new ArgumentException("Weighted table needs at least one output.", nameof(outputs));
            }

            TotalWeight = 0;
            foreach (var output in _outputs)
            {
                if (output.Weight <= 0)
                {
                    throw new ArgumentException($"Weight for '{output.ItemId}' must be > 0.", nameof(outputs));
                }

                TotalWeight += output.Weight;
            }
        }

        public IReadOnlyList<WeightedOutput> Outputs => _outputs;

        public int TotalWeight { get; }

        public string Roll(IRandomSource rng, string? lastItemId)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            var exclude = ShouldExclude(lastItemId) ? lastItemId : null;
            var total = SumWeights(exclude);
            if (total <= 0)
            {
                exclude = null;
                total = TotalWeight;
            }

            var roll = rng.Next(total);
            if (roll < 0 || roll >= total)
            {
                throw new InvalidOperationException($"RNG returned {roll} outside [0, {total}).");
            }

            var acc = 0;
            string? picked = null;
            foreach (var output in _outputs)
            {
                if (exclude != null && output.ItemId == exclude)
                {
                    continue;
                }

                acc += output.Weight;
                if (roll < acc)
                {
                    picked = output.ItemId;
                    break;
                }
            }

            return picked ?? _outputs[_outputs.Count - 1].ItemId;
        }

        private bool ShouldExclude(string? lastItemId)
        {
            if (string.IsNullOrEmpty(lastItemId))
            {
                return false;
            }

            foreach (var output in _outputs)
            {
                if (output.ItemId == lastItemId)
                {
                    return output.NeverConsecutive;
                }
            }

            return false;
        }

        private int SumWeights(string? excludeItemId)
        {
            if (excludeItemId is null)
            {
                return TotalWeight;
            }

            var sum = 0;
            foreach (var output in _outputs)
            {
                if (output.ItemId == excludeItemId)
                {
                    continue;
                }

                sum += output.Weight;
            }

            return sum;
        }
    }
}
