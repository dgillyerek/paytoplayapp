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
    /// Tools are one chain (T1 Twig → T2 Stick) so Order 5 is produce→merge deliverable.
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
        public static readonly PieceId HerbT2 = new("herb_t2");
        public static readonly PieceId ToolT1 = new("tool_t1");
        public static readonly PieceId ToolT2 = new("tool_t2");
        public static readonly PieceId Coin = new("coin");
        public static readonly PieceId Puff = new("puff");

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
                Item("wildflower_t1", "Seed", "wildflower", 1),
                Item("wildflower_t2", "Sprout", "wildflower", 2),
                Item("wildflower_t3", "Bud", "wildflower", 3),
                Item("wildflower_t4", "Wildflower", "wildflower", 4),
                Item("wildflower_t5", "Bouquet", "wildflower", 5),
                Item("herb_t1", "Herb Sprig", "herb", 1),
                Item("herb_t2", "Herb Pot", "herb", 2),
                Item("tool_t1", "Twig", "tools", 1),
                Item("tool_t2", "Stick", "tools", 2),
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
                Recipe(HerbT1, HerbT2),
                Recipe(ToolT1, ToolT2),
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
                    new WeightedOutput("tool_t1", 3, false)
                });

            var orders = new OrderSpec[]
            {
                Order("order_1", "Order 1", "Maya wants this — tap to deliver.", 10, 5, false,
                    new OrderRequirement(WildflowerT2, 1)),
                Order("order_2", "Order 2", "A little bigger — keep stacking matches.", 15, 8, false,
                    new OrderRequirement(WildflowerT3, 1)),
                Order("order_3", "Order 3", "Herbs next — different chain, same idea.", 15, 8, false,
                    new OrderRequirement(HerbT2, 1)),
                Order("order_4", "Order 4", "Two things at once — you’ve got this.", 35, 15, false,
                    new OrderRequirement(WildflowerT4, 1),
                    new OrderRequirement(HerbT2, 1)),
                Order("order_5", "Order 5", "Grab a sturdy stick from the tools chain.", 20, 10, false,
                    new OrderRequirement(ToolT2, 1)),
                Order("order_6", "Order 6", "One bouquet and the garden’s looking alive!", 40, 20, true,
                    new OrderRequirement(WildflowerT5, 1)),
            };

            return new LoadedCatalog(
                new ItemCatalog(items),
                new MergeCatalog(recipes),
                crate,
                EnergyConfig.Default,
                orders,
                PresentationCopy.Default);
        }

        private static OrderSpec Order(
            string id,
            string title,
            string mayaLine,
            int coins,
            int xp,
            bool completesMilestone,
            params OrderRequirement[] requirements) =>
            new OrderSpec(id, title, mayaLine, requirements, coins, xp, mayaLine, completesMilestone);

        private static ItemDefinition Item(string id, string name, string chain, int tier) =>
            new(new PieceId(id), name, chain, tier, Mergeable: true);

        private static MergeRecipe Recipe(PieceId input, PieceId output) =>
            new(input, MergeRecipe.DefaultInputCount, output, MergeRecipe.DefaultOutputCount);
    }
}
