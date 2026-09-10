using System;
using System.Collections.Generic;
using Grove.Domain.Board;

namespace Grove.Domain.Merge
{
    /// <summary>Lookup table of merge recipes keyed by input piece id.</summary>
    public sealed class MergeCatalog
    {
        private readonly Dictionary<string, MergeRecipe> _byInput;

        public MergeCatalog(IEnumerable<MergeRecipe> recipes)
        {
            _byInput = new Dictionary<string, MergeRecipe>(StringComparer.Ordinal);
            foreach (var recipe in recipes)
            {
                if (recipe.InputCount <= 0)
                {
                    throw new ArgumentException($"Recipe for {recipe.Input} has invalid input count.", nameof(recipes));
                }

                if (!_byInput.TryAdd(recipe.Input.Value, recipe))
                {
                    throw new ArgumentException($"Duplicate recipe for input '{recipe.Input}'.", nameof(recipes));
                }
            }
        }

        public IReadOnlyCollection<MergeRecipe> Recipes => _byInput.Values;

        public bool TryGet(PieceId input, out MergeRecipe recipe) =>
            _byInput.TryGetValue(input.Value, out recipe!);

        public MergeRecipe Require(PieceId input) =>
            TryGet(input, out var recipe)
                ? recipe
                : throw new InvalidOperationException($"No merge recipe for '{input}'.");
    }
}
