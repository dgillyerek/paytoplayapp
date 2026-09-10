using System;
using System.Collections.Generic;
using System.IO;
using Grove.Domain.Board;
using Grove.Domain.Energy;
using Grove.Domain.Orders;
using Grove.Domain.Producer;

namespace Grove.Domain.Merge
{
    /// <summary>
    /// Well-known piece ids plus the production catalog.
    /// Recipes come from JSON (<see cref="CatalogLoader"/>); the pebble→grove chain remains for DEV-001.
    /// </summary>
    public static class GroveCatalog
    {
        public static readonly PieceId Pebble = new("pebble");
        public static readonly PieceId Sprout = new("sprout");
        public static readonly PieceId Sapling = new("sapling");
        public static readonly PieceId Tree = new("tree");
        public static readonly PieceId Grove = new("grove");

        public static readonly PieceId WildflowerT1 = new("wildflower_t1");
        public static readonly PieceId WildflowerT2 = new("wildflower_t2");
        public static readonly PieceId WildflowerT3 = new("wildflower_t3");
        public static readonly PieceId WildflowerT4 = new("wildflower_t4");
        public static readonly PieceId WildflowerT5 = new("wildflower_t5");
        public static readonly PieceId HerbT1 = new("herb_t1");
        public static readonly PieceId TwigT1 = new("twig_t1");
        public static readonly PieceId Coin = new("coin");
        public static readonly PieceId Puff = new("puff");
        public static readonly PieceId WateringCanT1 = new("tool_t1");

        public static LoadedCatalog LoadProduction()
        {
            try
            {
                return CatalogLoader.LoadDefault();
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or FormatException)
            {
                return CreateFallback();
            }
        }

        public static MergeCatalog CreateDefault() => LoadProduction().Recipes;

        public static ItemCatalog CreateDefaultItems() => LoadProduction().Items;

        /// <summary>In-memory prototype catalog so Play Mode works even if Resources JSON is not imported yet.</summary>
        public static LoadedCatalog CreateFallback()
        {
            var items = new List<ItemDefinition>
            {
                Item("pebble", "Pebble", "grove_demo", 1),
                Item("sprout", "Sprout", "grove_demo", 2),
                Item("sapling", "Sapling", "grove_demo", 3),
                Item("tree", "Tree", "grove_demo", 4),
                Item("grove", "Grove", "grove_demo", 5),
                Item("wildflower_t1", "Wildflower T1", "wildflower", 1),
                Item("wildflower_t2", "Wildflower T2", "wildflower", 2),
                Item("wildflower_t3", "Wildflower T3", "wildflower", 3),
                Item("wildflower_t4", "Wildflower T4", "wildflower", 4),
                Item("wildflower_t5", "Wildflower T5", "wildflower", 5),
                Item("herb_t1", "Herb T1", "herb", 1),
                Item("twig_t1", "Twig T1", "twig", 1),
            };

            var recipes = new List<MergeRecipe>
            {
                Recipe(Pebble, Sprout),
                Recipe(Sprout, Sapling),
                Recipe(Sapling, Tree),
                Recipe(Tree, Grove),
                Recipe(WildflowerT1, WildflowerT2),
                Recipe(WildflowerT2, WildflowerT3),
                Recipe(WildflowerT3, WildflowerT4),
                Recipe(WildflowerT4, WildflowerT5),
            };

            var crate = new ProducerDefinition(
                "garden_crate",
                MaxCharges: 30,
                RechargeSeconds: 2,
                EnergyPerTap: 1,
                Outputs: new WeightedOutput[]
                {
                    new WeightedOutput("wildflower_t1", 90, false),
                    new WeightedOutput("herb_t1", 7, false),
                    new WeightedOutput("twig_t1", 3, false)
                });

            var orders = new OrderSpec[]
            {
                new("order_1", "Order 1 — First Bloom", "Merge wildflowers up to T3, then Deliver.",
                    new OrderRequirement[] { new OrderRequirement(WildflowerT3, 1) }),
                new("order_2", "Order 2 — Fuller Bouquet", "Deliver one Wildflower T4.",
                    new OrderRequirement[] { new OrderRequirement(WildflowerT4, 1) }),
                new("order_3", "Order 3 — Garden Show", "Deliver one Wildflower T5.",
                    new OrderRequirement[] { new OrderRequirement(WildflowerT5, 1) }),
            };

            return new LoadedCatalog(
                new ItemCatalog(items),
                new MergeCatalog(recipes),
                crate,
                EnergyConfig.Default,
                orders);
        }

        private static ItemDefinition Item(string id, string name, string chain, int tier) =>
            new(new PieceId(id), name, chain, tier, Mergeable: true);

        private static MergeRecipe Recipe(PieceId input, PieceId output) =>
            new(input, MergeRecipe.DefaultInputCount, output, MergeRecipe.DefaultOutputCount);
    }
}
