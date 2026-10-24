#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Gift;
using Survival.Domain.Ids;
using Survival.Domain.Quest;
using Survival.Domain.Serialization;
using Survival.Domain.Store;

namespace Survival.Domain.Catalog
{
    /// <summary>World + quest "hook" slice data: base plots, rush rules, starter quests, free gifts, stores.</summary>
    public sealed record HookCatalog(
        RushRules Rush,
        int BuildSlots,
        IReadOnlyDictionary<string, int> StartingWallet,
        IReadOnlyList<BasePlotDef> Plots,
        QuestChainDef StarterChain,
        GiftConfig Gifts,
        IReadOnlyList<StoreDef> Stores,
        IReadOnlyList<BuildableDef> Buildables);

    public static class HookCatalogLoader
    {
        public const string BaseEconomyFileName = "base_economy.json";
        public const string QuestsFileName = "quests.json";
        public const string GiftsFileName = "gifts.json";
        public const string StoresFileName = "stores.json";

        public static HookCatalog LoadFromDirectory(string directory) => LoadFromDirectory(directory, File.ReadAllText);

        /// <summary>Same as <see cref="LoadFromDirectory(string)"/>, reading each file (full path) through <paramref name="readAllText"/>
        /// (a player build reads the baked copies, not the disk).</summary>
        public static HookCatalog LoadFromDirectory(string directory, Func<string, string> readAllText) =>
            FromJson(
                readAllText(Path.Combine(directory, BaseEconomyFileName)),
                readAllText(Path.Combine(directory, QuestsFileName)),
                readAllText(Path.Combine(directory, GiftsFileName)),
                readAllText(Path.Combine(directory, StoresFileName)));

        public static HookCatalog FromJson(string baseEconomyJson, string questsJson, string giftsJson, string storesJson)
        {
            var economy = Obj(JsonRead.Parse(baseEconomyJson), "base_economy");
            var rushNode = Obj(Req(economy, "rush"), "rush");
            var rush = new RushRules(
                Int(rushNode, "free_finish_seconds"),
                Num(rushNode, "gems_per_minute"),
                Int(rushNode, "min_gems"));
            var plots = new List<BasePlotDef>();
            foreach (var item in Arr(economy, "plots"))
            {
                plots.Add(ReadPlot(Obj(item, "plot")));
            }

            var buildables = new List<BuildableDef>();
            if (economy.TryGet("buildables", out var bNode) && bNode is JsonNode.Arr bArr)
            {
                foreach (var item in bArr.Items)
                {
                    buildables.Add(ReadBuildable(Obj(item, "buildable"), plots));
                }
            }

            return new HookCatalog(
                rush,
                OptInt(economy, "build_slots", 1),
                Amounts(economy, "starting_wallet"),
                plots,
                ReadChain(Obj(JsonRead.Parse(questsJson), "quests")),
                ReadGifts(Obj(JsonRead.Parse(giftsJson), "gifts")),
                ReadStores(Obj(JsonRead.Parse(storesJson), "stores")),
                buildables);
        }

        /// <summary>Theme pack store/products.json: { "products": [ { "sku", "product_id", "price_label" } ] }.</summary>
        public static IReadOnlyDictionary<string, ProductDef> ParseProducts(string productsJson)
        {
            var root = Obj(JsonRead.Parse(productsJson), "products");
            var map = new Dictionary<string, ProductDef>(StringComparer.Ordinal);
            foreach (var item in Arr(root, "products"))
            {
                var o = Obj(item, "product");
                var sku = Id(o, "sku");
                map[sku] = new ProductDef(sku, Str(o, "product_id"), OptStr(o, "price_label") ?? "");
            }

            return map;
        }

        /// <summary>
        /// { "type", "building", "produces"?, "build_cost", "build_seconds", "unlock_at_castle": [..],
        ///   "levels_from": "base.plot.farm_01" | "levels": [..], "cost_growth"?, "time_growth"? }
        /// </summary>
        private static BuildableDef ReadBuildable(JsonNode.Obj o, IReadOnlyList<BasePlotDef> plots)
        {
            IReadOnlyList<BuildingLevelDef>? levels = null;
            var from = OptStr(o, "levels_from");
            if (from != null)
            {
                foreach (var p in plots)
                {
                    if (string.Equals(p.Id, from, StringComparison.Ordinal))
                    {
                        levels = p.Levels;
                    }
                }

                if (levels == null)
                {
                    throw new FormatException($"Buildable levels_from '{from}' is not a plot.");
                }
            }
            else
            {
                levels = ReadLevels(o);
            }

            var unlock = new List<int>();
            if (o.TryGet("unlock_at_castle", out var u) && u is JsonNode.Arr ua)
            {
                foreach (var n in ua.Items)
                {
                    unlock.Add(n is JsonNode.Num num ? num.AsInt() : throw new FormatException("unlock_at_castle must be numbers."));
                }
            }

            var produces = OptStr(o, "produces");
            if (produces != null)
            {
                CheckId(produces);
            }

            var type = Str(o, "type");
            return new BuildableDef(
                type,
                Id(o, "building"),
                produces,
                Amounts(o, "build_cost"),
                Int(o, "build_seconds"),
                unlock,
                levels,
                o.TryGet("cost_growth", out var cg) && cg is JsonNode.Num cgn ? cgn.Value : 1.6,
                o.TryGet("time_growth", out var tg) && tg is JsonNode.Num tgn ? tgn.Value : 1.5);
        }

        private static List<BuildingLevelDef> ReadLevels(JsonNode.Obj o)
        {
            var levels = new List<BuildingLevelDef>();
            foreach (var item in Arr(o, "levels"))
            {
                var l = Obj(item, "level");
                levels.Add(new BuildingLevelDef(
                    Int(l, "level"),
                    OptInt(l, "per_hour", 0),
                    OptInt(l, "capacity", 0),
                    OptInt(l, "min_collect", 0),
                    Amounts(l, "upgrade_cost"),
                    OptInt(l, "upgrade_seconds", 0)));
            }

            return levels;
        }

        private static BasePlotDef ReadPlot(JsonNode.Obj o)
        {
            var levels = new List<BuildingLevelDef>();
            foreach (var item in Arr(o, "levels"))
            {
                var l = Obj(item, "level");
                levels.Add(new BuildingLevelDef(
                    Int(l, "level"),
                    OptInt(l, "per_hour", 0),
                    OptInt(l, "capacity", 0),
                    OptInt(l, "min_collect", 0),
                    Amounts(l, "upgrade_cost"),
                    OptInt(l, "upgrade_seconds", 0)));
            }

            var produces = OptStr(o, "produces");
            if (produces != null)
            {
                CheckId(produces);
            }

            return new BasePlotDef(
                Id(o, "id"),
                Id(o, "building"),
                produces,
                OptInt(o, "start_level", 1),
                o.TryGet("start_fill", out var fill) && fill is JsonNode.Num fn ? fn.Value : 0,
                levels);
        }

        private static QuestChainDef ReadChain(JsonNode.Obj root)
        {
            var chain = Obj(Req(root, "chain"), "chain");
            var steps = new List<QuestStepDef>();
            foreach (var item in Arr(chain, "steps"))
            {
                var s = Obj(item, "step");
                steps.Add(new QuestStepDef(
                    Id(s, "id"),
                    Id(s, "objective"),
                    Id(s, "target"),
                    Int(s, "count"),
                    Amounts(s, "rewards")));
            }

            return new QuestChainDef(Id(chain, "id"), steps);
        }

        private static GiftConfig ReadGifts(JsonNode.Obj root)
        {
            var pool = new List<GiftRewardDef>();
            foreach (var item in Arr(root, "pool"))
            {
                var g = Obj(item, "gift");
                pool.Add(new GiftRewardDef(
                    Id(g, "id"),
                    Id(g, "resource"),
                    Int(g, "min"),
                    Int(g, "max"),
                    Int(g, "weight")));
            }

            return new GiftConfig(
                Id(root, "id"),
                Id(root, "store"),
                Id(root, "offer"),
                Int(root, "min_seconds"),
                Int(root, "max_seconds"),
                Int(root, "daily_cap"),
                pool);
        }

        private static IReadOnlyList<StoreDef> ReadStores(JsonNode.Obj root)
        {
            var stores = new List<StoreDef>();
            foreach (var item in Arr(root, "stores"))
            {
                var s = Obj(item, "store");
                var offers = new List<OfferDef>();
                foreach (var offerNode in Arr(s, "offers"))
                {
                    var o = Obj(offerNode, "offer");
                    var kind = Str(o, "kind");
                    if (kind != OfferKinds.Sku && kind != OfferKinds.Currency && kind != OfferKinds.Free)
                    {
                        throw new FormatException($"Unknown offer kind '{kind}'.");
                    }

                    var sku = OptStr(o, "sku");
                    if (sku != null)
                    {
                        CheckId(sku);
                    }

                    offers.Add(new OfferDef(
                        Id(o, "id"),
                        kind,
                        sku,
                        Amounts(o, "price"),
                        Amounts(o, "grants"),
                        OptStr(o, "tag")));
                }

                stores.Add(new StoreDef(Id(s, "id"), offers));
            }

            return stores;
        }

        private static JsonNode Req(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) ? n : throw new FormatException($"Expected '{key}'.");

        private static JsonNode.Obj Obj(JsonNode node, string name) =>
            node as JsonNode.Obj ?? throw new FormatException($"Expected object for {name}.");

        private static List<JsonNode> Arr(JsonNode.Obj o, string key) =>
            Req(o, key) is JsonNode.Arr a ? a.Items : throw new FormatException($"Expected array '{key}'.");

        private static string Str(JsonNode.Obj o, string key) =>
            Req(o, key) is JsonNode.Str s && !string.IsNullOrWhiteSpace(s.Value)
                ? s.Value
                : throw new FormatException($"Expected string '{key}'.");

        private static string? OptStr(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Str s && !string.IsNullOrWhiteSpace(s.Value) ? s.Value : null;

        private static string Id(JsonNode.Obj o, string key) => CheckId(Str(o, key));

        private static string CheckId(string id) =>
            SurvIds.IsForbidden(id) ? throw new FormatException($"Forbidden id '{id}'.") : id;

        private static double Num(JsonNode.Obj o, string key) =>
            Req(o, key) is JsonNode.Num n ? n.Value : throw new FormatException($"Expected number '{key}'.");

        private static int Int(JsonNode.Obj o, string key)
        {
            var node = Req(o, key);
            if (node is JsonNode.Num n)
            {
                return n.AsInt();
            }

            if (node is JsonNode.Str s && int.TryParse(s.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw new FormatException($"Expected integer '{key}'.");
        }

        private static int OptInt(JsonNode.Obj o, string key, int fallback) =>
            o.TryGet(key, out var n) && n is not JsonNode.Null ? Int(o, key) : fallback;

        private static IReadOnlyDictionary<string, int> Amounts(JsonNode.Obj o, string key)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            if (o.TryGet(key, out var node) && node is JsonNode.Obj amounts)
            {
                foreach (var pair in amounts.Fields)
                {
                    map[CheckId(pair.Key)] = Int(amounts, pair.Key);
                }
            }

            return map;
        }
    }
}
