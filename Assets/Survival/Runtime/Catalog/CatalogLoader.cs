using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;

namespace Survival.Domain.Catalog
{
    /// <summary>Loads SURV-P0 JSON tables. Adding a building or node is a data edit, not a systems fork.</summary>
    public static class CatalogLoader
    {
        public const string DictionaryFileName = "surv-p0-stable-id-dictionary.json";
        public const string SystemsFileName = "systems.json";
        public const string BuildingsFileName = "buildings.json";
        public const string ResearchFileName = "research.json";
        public const string HeroesFileName = "heroes.json";
        public const string AllianceFileName = "alliance.json";
        public const string WorldFileName = "world.json";
        public const string BattlesFileName = "battles.json";
        public const string EnergyFileName = "energy.json";
        public const string EventsFileName = "events.json";
        public const string IapFileName = "iap.json";
        public const string FtueFileName = "ftue.json";
        public const string ResourcesFileName = "resources.json";

        public static SurvivalCatalog LoadFromDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Catalog directory is required.", nameof(directory));
            }

            string Read(string name) => File.ReadAllText(Path.Combine(directory, name));

            var dictionary = JsonRead.Parse(Read(DictionaryFileName)) as JsonNode.Obj
                             ?? throw new FormatException("Dictionary root must be an object.");
            AssertSystemsApi(dictionary);
            return FromJson(
                Read(SystemsFileName),
                Read(BuildingsFileName),
                Read(ResearchFileName),
                Read(HeroesFileName),
                Read(AllianceFileName),
                Read(WorldFileName),
                Read(BattlesFileName),
                Read(EnergyFileName),
                Read(EventsFileName),
                Read(IapFileName),
                Read(FtueFileName),
                Read(ResourcesFileName));
        }

        public static SurvivalCatalog FromJson(
            string systemsJson,
            string buildingsJson,
            string researchJson,
            string heroesJson,
            string allianceJson,
            string worldJson,
            string battlesJson,
            string energyJson,
            string eventsJson,
            string iapJson,
            string ftueJson,
            string resourcesJson)
        {
            var modules = ReadIdArray(systemsJson, "modules");
            var resources = ReadIdArray(resourcesJson, "resources");
            var buildings = ReadBuildings(buildingsJson);
            var research = ReadResearch(researchJson);
            var heroes = ReadHeroes(heroesJson);
            var alliance = ReadIdArray(allianceJson, "alliance");
            var (types, nodes) = ReadWorld(worldJson);
            var battles = ReadIdArray(battlesJson, "battles");
            var energy = ReadEnergy(energyJson);
            var events = ReadEvents(eventsJson);
            var skus = ReadSkus(iapJson);
            var ftue = ReadIdArray(ftueJson, "steps");
            RejectForbidden(modules, resources, buildings, research, heroes, alliance, types, nodes, battles, events, skus, ftue);
            return new SurvivalCatalog(
                SurvIds.SystemsApi,
                modules,
                resources,
                buildings,
                research,
                heroes,
                alliance,
                types,
                nodes,
                battles,
                energy,
                events,
                skus,
                ftue);
        }

        public static void AssertDictionaryMatches(string dictionaryJson)
        {
            var root = RequireObj(JsonRead.Parse(dictionaryJson), "dictionary");
            AssertSystemsApi(root);
            AssertListEquals(root, "modules", SurvIds.Modules);
            AssertListEquals(root, "resources", SurvIds.Resources);
            AssertListEquals(root, "buildings", SurvIds.Buildings);
            AssertListEquals(root, "world_node_instances_slice", SurvIds.WorldNodeInstances);
        }

        private static void AssertSystemsApi(JsonNode.Obj root)
        {
            var api = ReqString(root, "systems_api");
            if (!string.Equals(api, SurvIds.SystemsApi, StringComparison.Ordinal))
            {
                throw new FormatException($"systems_api must be {SurvIds.SystemsApi}, got '{api}'.");
            }
        }

        private static void AssertListEquals(JsonNode.Obj root, string key, IReadOnlyList<string> expected)
        {
            var actual = StringList(RequireArr(root, key));
            if (actual.Count != expected.Count)
            {
                throw new FormatException($"{key}: expected {expected.Count} ids, got {actual.Count}.");
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal))
                {
                    throw new FormatException($"{key}[{i}]: expected '{expected[i]}', got '{actual[i]}'.");
                }
            }
        }

        private static IReadOnlyList<string> ReadIdArray(string json, string key)
        {
            var root = RequireObj(JsonRead.Parse(json), key);
            return StringList(RequireArr(root, key));
        }

        private static IReadOnlyList<BuildingDef> ReadBuildings(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "buildings");
            var arr = RequireArr(root, "buildings");
            var list = new List<BuildingDef>(arr.Items.Count);
            foreach (var item in arr.Items)
            {
                var obj = RequireObj(item, "building");
                var id = ReqId(obj, "id");
                list.Add(new BuildingDef(id, OptStringList(obj, "requires")));
            }

            return list;
        }

        private static IReadOnlyList<ResearchDef> ReadResearch(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "research");
            var arr = RequireArr(root, "research");
            var list = new List<ResearchDef>(arr.Items.Count);
            foreach (var item in arr.Items)
            {
                var obj = RequireObj(item, "research");
                list.Add(new ResearchDef(
                    ReqId(obj, "id"),
                    ReqString(obj, "branch"),
                    OptStringList(obj, "requires")));
            }

            return list;
        }

        private static IReadOnlyList<HeroDef> ReadHeroes(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "heroes");
            var arr = RequireArr(root, "heroes");
            var list = new List<HeroDef>(arr.Items.Count);
            foreach (var item in arr.Items)
            {
                var obj = RequireObj(item, "hero");
                var stats = new Dictionary<string, int>(StringComparer.Ordinal);
                if (obj.TryGet("stats", out var statsNode) && statsNode is JsonNode.Obj statsObj)
                {
                    foreach (var pair in statsObj.Fields)
                    {
                        if (SurvIds.IsForbidden(pair.Key))
                        {
                            throw new FormatException($"Forbidden stat id '{pair.Key}'.");
                        }

                        stats[pair.Key] = AsInt(pair.Value, pair.Key);
                    }
                }

                list.Add(new HeroDef(ReqId(obj, "slot"), ReqId(obj, "archetype"), ReqId(obj, "rarity"), stats));
            }

            return list;
        }

        private static (IReadOnlyList<WorldNodeTypeDef> Types, IReadOnlyList<WorldNodeDef> Nodes) ReadWorld(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "world");
            var typeArr = RequireArr(root, "node_types");
            var types = new List<WorldNodeTypeDef>(typeArr.Items.Count);
            foreach (var item in typeArr.Items)
            {
                var obj = RequireObj(item, "node_type");
                types.Add(new WorldNodeTypeDef(ReqId(obj, "id"), ReqString(obj, "design_role")));
            }

            var nodeArr = RequireArr(root, "nodes");
            var nodes = new List<WorldNodeDef>(nodeArr.Items.Count);
            foreach (var item in nodeArr.Items)
            {
                var obj = RequireObj(item, "node");
                nodes.Add(new WorldNodeDef(ReqId(obj, "id"), ReqId(obj, "type")));
            }

            if (types.Count != SurvIds.WorldNodeTypes.Count)
            {
                throw new FormatException("World node types must match SURV-P0 (6 types).");
            }

            return (types, nodes);
        }

        private static EnergyConfig ReadEnergy(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "energy");
            var costs = new Dictionary<string, int>(StringComparer.Ordinal);
            if (root.TryGet("action_costs", out var costNode) && costNode is JsonNode.Obj costObj)
            {
                foreach (var pair in costObj.Fields)
                {
                    costs[ReqIdValue(pair.Key)] = AsInt(pair.Value, pair.Key);
                }
            }

            return new EnergyConfig(
                ReqId(root, "meter"),
                ReqInt(root, "cap"),
                ReqInt(root, "regen_seconds_per_point"),
                costs);
        }

        private static IReadOnlyList<EventDef> ReadEvents(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "events");
            var arr = RequireArr(root, "events");
            var list = new List<EventDef>(arr.Items.Count);
            foreach (var item in arr.Items)
            {
                var obj = RequireObj(item, "event");
                list.Add(new EventDef(ReqId(obj, "type")));
            }

            return list;
        }

        private static IReadOnlyList<SkuDef> ReadSkus(string json)
        {
            var root = RequireObj(JsonRead.Parse(json), "iap");
            var arr = RequireArr(root, "skus");
            var list = new List<SkuDef>(arr.Items.Count);
            foreach (var item in arr.Items)
            {
                if (item is JsonNode.Str str)
                {
                    list.Add(new SkuDef(ReqIdValue(str.Value)));
                    continue;
                }

                var obj = RequireObj(item, "sku");
                list.Add(new SkuDef(ReqId(obj, "id")));
            }

            return list;
        }

        private static void RejectForbidden(
            IReadOnlyList<string> modules,
            IReadOnlyList<string> resources,
            IReadOnlyList<BuildingDef> buildings,
            IReadOnlyList<ResearchDef> research,
            IReadOnlyList<HeroDef> heroes,
            IReadOnlyList<string> alliance,
            IReadOnlyList<WorldNodeTypeDef> types,
            IReadOnlyList<WorldNodeDef> nodes,
            IReadOnlyList<string> battles,
            IReadOnlyList<EventDef> events,
            IReadOnlyList<SkuDef> skus,
            IReadOnlyList<string> ftue)
        {
            void Check(string id)
            {
                if (SurvIds.IsForbidden(id))
                {
                    throw new FormatException($"Forbidden id '{id}' in survival_core catalog.");
                }
            }

            foreach (var id in modules) Check(id);
            foreach (var id in resources) Check(id);
            foreach (var b in buildings) Check(b.Id);
            foreach (var r in research) Check(r.Id);
            foreach (var h in heroes)
            {
                Check(h.SlotId);
                Check(h.ArchetypeId);
                Check(h.RarityId);
            }

            foreach (var id in alliance) Check(id);
            foreach (var t in types) Check(t.Id);
            foreach (var n in nodes)
            {
                Check(n.Id);
                Check(n.NodeTypeId);
            }

            foreach (var id in battles) Check(id);
            foreach (var e in events) Check(e.TypeId);
            foreach (var s in skus) Check(s.Id);
            foreach (var id in ftue) Check(id);
        }

        private static IReadOnlyList<string> OptStringList(JsonNode.Obj obj, string key)
        {
            if (obj.TryGet(key, out var node) && node is JsonNode.Arr arr)
            {
                return StringList(arr);
            }

            return Array.Empty<string>();
        }

        private static List<string> StringList(JsonNode.Arr arr)
        {
            var list = new List<string>(arr.Items.Count);
            foreach (var item in arr.Items)
            {
                if (item is not JsonNode.Str str)
                {
                    throw new FormatException("Expected string id.");
                }

                list.Add(ReqIdValue(str.Value));
            }

            return list;
        }

        private static JsonNode.Obj RequireObj(JsonNode node, string name) =>
            node as JsonNode.Obj ?? throw new FormatException($"Expected object for {name}.");

        private static JsonNode.Arr RequireArr(JsonNode.Obj root, string key)
        {
            if (!root.TryGet(key, out var node) || node is not JsonNode.Arr arr)
            {
                throw new FormatException($"Expected array '{key}'.");
            }

            return arr;
        }

        private static string ReqString(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node) || node is not JsonNode.Str str || string.IsNullOrWhiteSpace(str.Value))
            {
                throw new FormatException($"Expected string '{key}'.");
            }

            return str.Value;
        }

        private static string ReqId(JsonNode.Obj obj, string key) => ReqIdValue(ReqString(obj, key));

        private static string ReqIdValue(string id)
        {
            if (SurvIds.IsForbidden(id))
            {
                throw new FormatException($"Forbidden id '{id}'.");
            }

            return id;
        }

        private static int ReqInt(JsonNode.Obj obj, string key)
        {
            if (!obj.TryGet(key, out var node))
            {
                throw new FormatException($"Expected number '{key}'.");
            }

            return AsInt(node, key);
        }

        private static int AsInt(JsonNode node, string key)
        {
            if (node is JsonNode.Num num)
            {
                return num.AsInt();
            }

            if (node is JsonNode.Str str
                && int.TryParse(str.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw new FormatException($"Expected integer '{key}'.");
        }
    }
}
