using Grove.Domain.Merge;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>
    /// ScriptableObject wrapper around the same JSON catalogs QA edits.
    /// Assign the TextAssets (or leave empty to load Resources/Grove/*.json).
    /// </summary>
    [CreateAssetMenu(menuName = "Grove/Catalog", fileName = "GroveCatalog")]
    public sealed class GroveCatalogAsset : ScriptableObject
    {
        [SerializeField] private TextAsset itemsJson = null!;
        [SerializeField] private TextAsset recipesJson = null!;
        [SerializeField] private TextAsset gardenCrateJson = null!;
        [SerializeField] private TextAsset energyJson = null!;

        public LoadedCatalog Load()
        {
            var items = Resolve(itemsJson, "Grove/items");
            var recipes = Resolve(recipesJson, "Grove/recipes");
            var crate = TryResolve(gardenCrateJson, "Grove/garden_crate");
            var energy = TryResolve(energyJson, "Grove/energy");
            var orders = TryResolve(null, "Grove/orders");
            var copy = TryResolve(null, "Grove/copy");
            return CatalogLoader.FromJson(items, recipes, crate, energy, orders, copy);
        }

        internal static string Resolve(TextAsset? assigned, string resourcesPath)
        {
            if (assigned != null && !string.IsNullOrWhiteSpace(assigned.text))
            {
                return assigned.text;
            }

            var loaded = Resources.Load<TextAsset>(resourcesPath);
            if (loaded == null)
            {
                throw new MissingReferenceException(
                    $"Grove catalog TextAsset missing. Assign it on GroveCatalogAsset or place a JSON at Resources/{resourcesPath}.");
            }

            return loaded.text;
        }

        internal static string? TryResolve(TextAsset? assigned, string resourcesPath)
        {
            if (assigned != null && !string.IsNullOrWhiteSpace(assigned.text))
            {
                return assigned.text;
            }

            var loaded = Resources.Load<TextAsset>(resourcesPath);
            return loaded != null ? loaded.text : null;
        }
    }
}
