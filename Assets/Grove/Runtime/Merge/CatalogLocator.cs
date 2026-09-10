using System;
using System.Collections.Generic;
using System.IO;

namespace Grove.Domain.Merge
{
    /// <summary>
    /// Resolves the Grove JSON data directory for headless tests and editor tooling.
    /// Unity players should load the same files as <c>TextAsset</c>s via Resources and call
    /// <see cref="CatalogLoader.FromJson"/>.
    /// </summary>
    public static class CatalogLocator
    {
        public const string ItemsFileName = "items.json";
        public const string RecipesFileName = "recipes.json";
        public const string GardenCrateFileName = "garden_crate.json";
        public const string EnergyFileName = "energy.json";
        public const string OrdersFileName = "orders.json";
        public const string CopyFileName = "copy.json";

        /// <summary>When set, <see cref="ResolveDataDirectory"/> uses this path first.</summary>
        public static string? DataDirectoryOverride { get; set; }

        public static string ResolveDataDirectory()
        {
            if (!string.IsNullOrWhiteSpace(DataDirectoryOverride) &&
                File.Exists(Path.Combine(DataDirectoryOverride, ItemsFileName)))
            {
                return DataDirectoryOverride;
            }

            foreach (var candidate in Candidates())
            {
                if (File.Exists(Path.Combine(candidate, ItemsFileName)))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException(
                "Grove catalog JSON not found. Expected items.json under Assets/Grove/Resources/Grove " +
                "(copied to test output Data/) or set CatalogLocator.DataDirectoryOverride.");
        }

        private static IEnumerable<string> Candidates()
        {
            var baseDir = AppContext.BaseDirectory;
            yield return Path.Combine(baseDir, "Data");
            yield return Path.Combine(baseDir, "Grove");

            for (var dir = new DirectoryInfo(baseDir); dir != null; dir = dir.Parent)
            {
                yield return Path.Combine(dir.FullName, "Assets", "Grove", "Resources", "Grove");
                yield return Path.Combine(dir.FullName, "Data");
            }

            for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir != null; dir = dir.Parent)
            {
                yield return Path.Combine(dir.FullName, "Assets", "Grove", "Resources", "Grove");
            }
        }
    }
}
