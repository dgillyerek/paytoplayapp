using System;
using System.Collections.Generic;
using System.IO;
using Grove.Domain.Board;
using Grove.Domain.Energy;
using Grove.Domain.Orders;
using Grove.Domain.Producer;
using Grove.Domain.Serialization;

namespace Grove.Domain.Merge
{
    /// <summary>
    /// Loads item/recipe/producer/energy catalogs from JSON. MergeSession consumes the resulting
    /// <see cref="MergeCatalog"/>; adding a tier is a JSON edit, not a loop edit.
    /// </summary>
    public static class CatalogLoader
    {
        public static LoadedCatalog LoadDefault()
        {
            var dir = CatalogLocator.ResolveDataDirectory();
            return LoadFromDirectory(dir);
        }

        public static LoadedCatalog LoadFromDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Catalog directory is required.", nameof(directory));
            }

            var items = File.ReadAllText(Path.Combine(directory, CatalogLocator.ItemsFileName));
            var recipes = File.ReadAllText(Path.Combine(directory, CatalogLocator.RecipesFileName));
            var cratePath = Path.Combine(directory, CatalogLocator.GardenCrateFileName);
            var energyPath = Path.Combine(directory, CatalogLocator.EnergyFileName);
            var ordersPath = Path.Combine(directory, CatalogLocator.OrdersFileName);
            var crate = File.Exists(cratePath) ? File.ReadAllText(cratePath) : null;
            var energy = File.Exists(energyPath) ? File.ReadAllText(energyPath) : null;
            var orders = File.Exists(ordersPath) ? File.ReadAllText(ordersPath) : null;
            return FromJson(items, recipes, crate, energy, orders);
        }

        public static LoadedCatalog FromJson(
            string itemsJson,
            string recipesJson,
            string? gardenCrateJson = null,
            string? energyJson = null,
            string? ordersJson = null)
        {
            var items = ParseItems(itemsJson);
            var recipes = ParseRecipes(recipesJson, items);
            var crate = gardenCrateJson is null ? null : ParseProducer(gardenCrateJson, items);
            var energy = energyJson is null ? EnergyConfig.Default : ParseEnergy(energyJson);
            var orders = ordersJson is null ? Array.Empty<OrderSpec>() : ParseOrders(ordersJson, items);
            return new LoadedCatalog(items, recipes, crate, energy, orders);
        }

        private static ItemCatalog ParseItems(string json)
        {
            var root = RequireObject(JsonRead.Parse(json), "items root");
            var list = RequireArray(root, "items");
            var items = new List<ItemDefinition>(list.Items.Count);
            foreach (var node in list.Items)
            {
                var obj = RequireObject(node, "item");
                var id = new PieceId(ReqString(obj, "id"));
                var display = ReqString(obj, "displayName");
                var chain = ReqString(obj, "chain");
                var tier = ReqInt(obj, "tier");
                var mergeable = OptBool(obj, "mergeable", defaultValue: true);
                items.Add(new ItemDefinition(id, display, chain, tier, mergeable));
            }

            return new ItemCatalog(items);
        }

        private static MergeCatalog ParseRecipes(string json, ItemCatalog items)
        {
            var root = RequireObject(JsonRead.Parse(json), "recipes root");
            var list = RequireArray(root, "recipes");
            var recipes = new List<MergeRecipe>(list.Items.Count);
            foreach (var node in list.Items)
            {
                var obj = RequireObject(node, "recipe");
                var input = new PieceId(ReqString(obj, "input"));
                var output = new PieceId(ReqString(obj, "output"));
                var inputCount = OptInt(obj, "inputCount", MergeRecipe.DefaultInputCount);
                var outputCount = OptInt(obj, "outputCount", MergeRecipe.DefaultOutputCount);

                if (!items.TryGet(input, out _))
                {
                    throw new FormatException($"Recipe input '{input}' is not in the item catalog.");
                }

                if (!items.TryGet(output, out _))
                {
                    throw new FormatException($"Recipe output '{output}' is not in the item catalog.");
                }

                recipes.Add(new MergeRecipe(input, inputCount, output, outputCount));
            }

            return new MergeCatalog(recipes);
        }

        private static ProducerDefinition ParseProducer(string json, ItemCatalog items)
        {
            var root = RequireObject(JsonRead.Parse(json), "producer root");
            var id = ReqString(root, "id");
            var maxCharges = ReqInt(root, "maxCharges");
            var rechargeSeconds = ReqInt(root, "rechargeSeconds");
            var energyPerTap = OptInt(root, "energyPerTap", 1);
            if (maxCharges <= 0)
            {
                throw new FormatException("Producer maxCharges must be > 0.");
            }

            if (rechargeSeconds < 0)
            {
                throw new FormatException("Producer rechargeSeconds must be >= 0.");
            }

            var outputsNode = RequireArray(root, "outputs");
            var outputs = new List<WeightedOutput>(outputsNode.Items.Count);
            foreach (var node in outputsNode.Items)
            {
                var obj = RequireObject(node, "output");
                var itemId = ReqString(obj, "itemId");
                var weight = ReqInt(obj, "weight");
                var neverConsecutive = OptBool(obj, "neverConsecutive", defaultValue: false);
                if (weight <= 0)
                {
                    throw new FormatException($"Producer output '{itemId}' weight must be > 0.");
                }

                if (!items.TryGet(new PieceId(itemId), out _))
                {
                    throw new FormatException($"Producer output '{itemId}' is not in the item catalog.");
                }

                outputs.Add(new WeightedOutput(itemId, weight, neverConsecutive));
            }

            if (outputs.Count == 0)
            {
                throw new FormatException("Producer must declare at least one output.");
            }

            return new ProducerDefinition(id, maxCharges, rechargeSeconds, energyPerTap, outputs);
        }

        private static EnergyConfig ParseEnergy(string json)
        {
            var root = RequireObject(JsonRead.Parse(json), "energy root");
            var cap = OptInt(root, "cap", EnergyConfig.Default.Cap);
            var regen = OptInt(root, "regenSecondsPerPoint", EnergyConfig.Default.RegenSecondsPerPoint);
            var produce = OptInt(root, "produceCost", EnergyConfig.Default.ProduceCost);
            var dig = OptInt(root, "digCost", EnergyConfig.Default.DigCost);
            var ftue = OptBool(root, "ftueTopUpToCap", EnergyConfig.Default.FtueTopUpToCap);
            if (cap <= 0)
            {
                throw new FormatException("Energy cap must be > 0.");
            }

            if (regen <= 0)
            {
                throw new FormatException("Energy regenSecondsPerPoint must be > 0.");
            }

            if (produce < 0 || dig < 0)
            {
                throw new FormatException("Energy spend costs must be >= 0.");
            }

            return new EnergyConfig(cap, regen, produce, dig, ftue);
        }

        private static IReadOnlyList<OrderSpec> ParseOrders(string json, ItemCatalog items)
        {
            var root = RequireObject(JsonRead.Parse(json), "orders root");
            var list = RequireArray(root, "orders");
            var orders = new List<OrderSpec>(list.Items.Count);
            foreach (var node in list.Items)
            {
                var obj = RequireObject(node, "order");
                var id = ReqString(obj, "id");
                var title = ReqString(obj, "title");
                var hint = OptString(obj, "hint", "");
                var reqNode = RequireArray(obj, "requirements");
                var requirements = new List<OrderRequirement>(reqNode.Items.Count);
                foreach (var req in reqNode.Items)
                {
                    var reqObj = RequireObject(req, "requirement");
                    var itemId = new PieceId(ReqString(reqObj, "itemId"));
                    var count = OptInt(reqObj, "count", 1);
                    if (count <= 0)
                    {
                        throw new FormatException($"Order '{id}' requirement count must be > 0.");
                    }

                    if (!items.TryGet(itemId, out _))
                    {
                        throw new FormatException($"Order '{id}' requires unknown item '{itemId}'.");
                    }

                    requirements.Add(new OrderRequirement(itemId, count));
                }

                if (requirements.Count == 0)
                {
                    throw new FormatException($"Order '{id}' needs at least one requirement.");
                }

                orders.Add(new OrderSpec(id, title, hint, requirements));
            }

            return orders;
        }

        private static JsonNode.Obj RequireObject(JsonNode node, string what)
        {
            return node as JsonNode.Obj ?? throw new FormatException($"Expected JSON object for {what}.");
        }

        private static JsonNode.Arr RequireArray(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node) || node is not JsonNode.Arr arr)
            {
                throw new FormatException($"Expected JSON array '{key}'.");
            }

            return arr;
        }

        private static string ReqString(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node) || node is not JsonNode.Str str || string.IsNullOrWhiteSpace(str.Value))
            {
                throw new FormatException($"Expected non-empty string '{key}'.");
            }

            return str.Value;
        }

        private static string OptString(JsonNode.Obj obj, string key, string defaultValue)
        {
            if (!obj.TryGet(key, out var node) || node is JsonNode.Null)
            {
                return defaultValue;
            }

            if (node is not JsonNode.Str str)
            {
                throw new FormatException($"Expected string '{key}'.");
            }

            return str.Value;
        }

        private static int ReqInt(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node) || node is not JsonNode.Num num)
            {
                throw new FormatException($"Expected integer '{key}'.");
            }

            return num.AsInt();
        }

        private static int OptInt(JsonNode.Obj obj, string key, int defaultValue)
        {
            if (!obj.TryGet(key, out var node) || node is JsonNode.Null)
            {
                return defaultValue;
            }

            if (node is not JsonNode.Num num)
            {
                throw new FormatException($"Expected integer '{key}'.");
            }

            return num.AsInt();
        }

        private static bool OptBool(JsonNode.Obj obj, string key, bool defaultValue)
        {
            if (!obj.TryGet(key, out var node) || node is JsonNode.Null)
            {
                return defaultValue;
            }

            if (node is not JsonNode.Bool b)
            {
                throw new FormatException($"Expected bool '{key}'.");
            }

            return b.Value;
        }
    }
}
