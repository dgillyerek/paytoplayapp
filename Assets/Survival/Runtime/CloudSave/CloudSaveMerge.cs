#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Serialization;

namespace Survival.Domain.CloudSave
{
    /// <summary>
    /// Last-write with sensible merge: max for monotonic progress (wallet, quest index, building levels),
    /// min for remaining gather stock, union for cleared / bought / iapApplied. Mirrors save.mjs.
    /// </summary>
    public static class CloudSaveMerge
    {
        public static CloudSaveDocument Merge(CloudSaveDocument? server, CloudSaveDocument? incoming)
        {
            var a = server ?? new CloudSaveDocument();
            var b = incoming ?? new CloudSaveDocument();
            var outDoc = new CloudSaveDocument
            {
                SavedAt = Math.Max(a.SavedAt, b.SavedAt),
                CampaignJson = MergeCampaignJson(a.CampaignJson, b.CampaignJson),
                VillageJson = MergeVillageJson(a.VillageJson, b.VillageJson),
                FtueStepId = string.IsNullOrEmpty(b.FtueStepId) ? a.FtueStepId : b.FtueStepId,
            };

            foreach (var chainId in new SortedSet<string>(a.Quests.Keys, StringComparer.Ordinal))
            {
                outDoc.Quests[chainId] = CloneQuest(a.Quests[chainId]);
            }

            foreach (var kv in b.Quests)
            {
                outDoc.Quests[kv.Key] = outDoc.Quests.TryGetValue(kv.Key, out var prev)
                    ? MergeQuestEntry(prev, kv.Value)
                    : CloneQuest(kv.Value);
            }

            foreach (var nodeId in new SortedSet<string>(a.Gather.Keys, StringComparer.Ordinal))
            {
                outDoc.Gather[nodeId] = CloneGather(a.Gather[nodeId]);
            }

            foreach (var kv in b.Gather)
            {
                outDoc.Gather[kv.Key] = outDoc.Gather.TryGetValue(kv.Key, out var prev)
                    ? MergeGather(prev, kv.Value)
                    : CloneGather(kv.Value);
            }

            foreach (var h in Union(a.IapApplied, b.IapApplied))
            {
                outDoc.IapApplied.Add(h);
            }

            foreach (var src in new[] { a.GrantLedger, b.GrantLedger })
            {
                foreach (var kv in src)
                {
                    if (!outDoc.GrantLedger.TryGetValue(kv.Key, out var map))
                    {
                        map = new Dictionary<string, int>(StringComparer.Ordinal);
                        outDoc.GrantLedger[kv.Key] = map;
                    }

                    foreach (var g in kv.Value)
                    {
                        map[g.Key] = g.Value;
                    }
                }
            }

            return outDoc;
        }

        private static CloudSaveDocument.QuestProgress CloneQuest(CloudSaveDocument.QuestProgress q) =>
            new() { Index = q.Index, Progress = q.Progress };

        private static CloudSaveDocument.QuestProgress MergeQuestEntry(
            CloudSaveDocument.QuestProgress a,
            CloudSaveDocument.QuestProgress b)
        {
            var index = Math.Max(a.Index, b.Index);
            var progress = a.Index == b.Index
                ? Math.Max(a.Progress, b.Progress)
                : (a.Index > b.Index ? a.Progress : b.Progress);
            return new CloudSaveDocument.QuestProgress { Index = index, Progress = progress };
        }

        private static CloudSaveDocument.GatherStock CloneGather(CloudSaveDocument.GatherStock g) =>
            new() { Available = g.Available, UpdatedAt = g.UpdatedAt, RegenAt = g.RegenAt };

        private static CloudSaveDocument.GatherStock MergeGather(
            CloudSaveDocument.GatherStock a,
            CloudSaveDocument.GatherStock b) =>
            new()
            {
                Available = Math.Min(a.Available, b.Available),
                UpdatedAt = Math.Max(a.UpdatedAt, b.UpdatedAt),
                RegenAt = a.RegenAt is double ar
                    ? (b.RegenAt is double br ? Math.Max(ar, br) : ar)
                    : b.RegenAt,
            };

        private static string? MergeCampaignJson(string? aJson, string? bJson)
        {
            if (string.IsNullOrEmpty(aJson))
            {
                return bJson;
            }

            if (string.IsNullOrEmpty(bJson))
            {
                return aJson;
            }

            if (JsonRead.Parse(aJson) is not JsonNode.Obj a || JsonRead.Parse(bJson) is not JsonNode.Obj b)
            {
                return bJson;
            }

            return RebuildCampaign(
                b,
                MergeWallet(Obj(a, "wallet"), Obj(b, "wallet")),
                MaxInt(a, b, "wins"),
                MaxInt(a, b, "battles"),
                MaxInt(a, b, "bounty"),
                MaxInt(a, b, "runner_unlocked"),
                Union(ArrStr(a, "cleared"), ArrStr(b, "cleared")),
                Union(ArrStr(a, "bought_once"), ArrStr(b, "bought_once")),
                Obj(a, "heroes"),
                Obj(b, "heroes"));
        }

        private static string RebuildCampaign(
            JsonNode.Obj b,
            Dictionary<string, int> wallet,
            int wins,
            int battles,
            int bounty,
            int runner,
            List<string> cleared,
            List<string> bought,
            JsonNode.Obj? heroesA,
            JsonNode.Obj? heroesB)
        {
            var w = new StringBuilder();
            w.Append("{\"version\":").Append(Int(b, "version").ToString(CultureInfo.InvariantCulture));
            if (b.Fields.TryGetValue("saved_at", out var sat) && sat is JsonNode.Num sn)
            {
                w.Append(",\"saved_at\":").Append(sn.Value.ToString("R", CultureInfo.InvariantCulture));
            }

            w.Append(",\"wallet\":{");
            var first = true;
            foreach (var kv in wallet)
            {
                if (!first)
                {
                    w.Append(',');
                }

                first = false;
                w.Append(Q(kv.Key)).Append(':').Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            }

            w.Append("},\"heroes\":{");
            first = true;
            var ids = new SortedSet<string>(StringComparer.Ordinal);
            if (heroesA != null)
            {
                foreach (var k in heroesA.Fields.Keys)
                {
                    ids.Add(k);
                }
            }

            if (heroesB != null)
            {
                foreach (var k in heroesB.Fields.Keys)
                {
                    ids.Add(k);
                }
            }

            foreach (var id in ids)
            {
                var xa = heroesA != null && heroesA.Fields.TryGetValue(id, out var ha) && ha is JsonNode.Obj hoa ? hoa : null;
                var xb = heroesB != null && heroesB.Fields.TryGetValue(id, out var hb) && hb is JsonNode.Obj hob ? hob : null;
                if (!first)
                {
                    w.Append(',');
                }

                first = false;
                w.Append(Q(id)).Append(":{\"owned\":").Append(Bool(xa, "owned") || Bool(xb, "owned") ? "true" : "false");
                w.Append(",\"level\":").Append(Math.Max(Int(xa, "level"), Int(xb, "level")).ToString(CultureInfo.InvariantCulture));
                w.Append(",\"stars\":").Append(Math.Max(Int(xa, "stars"), Int(xb, "stars")).ToString(CultureInfo.InvariantCulture));
                w.Append(",\"shards\":").Append(Math.Max(Int(xa, "shards"), Int(xb, "shards")).ToString(CultureInfo.InvariantCulture));
                w.Append('}');
            }

            w.Append("},\"cleared\":[");
            for (var i = 0; i < cleared.Count; i++)
            {
                if (i > 0)
                {
                    w.Append(',');
                }

                w.Append(Q(cleared[i]));
            }

            w.Append("],\"wins\":").Append(wins.ToString(CultureInfo.InvariantCulture));
            w.Append(",\"battles\":").Append(battles.ToString(CultureInfo.InvariantCulture));
            w.Append(",\"bounty\":").Append(bounty.ToString(CultureInfo.InvariantCulture));
            w.Append(",\"runner_unlocked\":").Append(runner.ToString(CultureInfo.InvariantCulture));
            w.Append(",\"bought_once\":[");
            for (var i = 0; i < bought.Count; i++)
            {
                if (i > 0)
                {
                    w.Append(',');
                }

                w.Append(Q(bought[i]));
            }

            w.Append(']');
            if (b.Fields.TryGetValue("training", out var tr))
            {
                w.Append(",\"training\":").Append(JsonWrite.Write(tr));
            }

            w.Append('}');
            return w.ToString();
        }

        private static string? MergeVillageJson(string? aJson, string? bJson)
        {
            if (string.IsNullOrEmpty(aJson))
            {
                return bJson;
            }

            if (string.IsNullOrEmpty(bJson))
            {
                return aJson;
            }

            if (JsonRead.Parse(aJson) is not JsonNode.Obj a || JsonRead.Parse(bJson) is not JsonNode.Obj b)
            {
                return bJson;
            }

            var levels = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var src in new[] { Obj(a, "levels"), Obj(b, "levels") })
            {
                if (src == null)
                {
                    continue;
                }

                foreach (var kv in src.Fields)
                {
                    if (kv.Value is JsonNode.Num n)
                    {
                        levels[kv.Key] = Math.Max(levels.TryGetValue(kv.Key, out var prev) ? prev : 0, (int)n.Value);
                    }
                }
            }

            // VillageSave stock shape: [stored, lastTick]
            var stock = new SortedDictionary<string, (double amount, double lastTick)>(StringComparer.Ordinal);
            foreach (var src in new[] { Obj(a, "stock"), Obj(b, "stock") })
            {
                if (src == null)
                {
                    continue;
                }

                foreach (var kv in src.Fields)
                {
                    double amount = 0, lastTick = 0;
                    if (kv.Value is JsonNode.Arr arr && arr.Items.Count >= 2
                        && arr.Items[0] is JsonNode.Num a0 && arr.Items[1] is JsonNode.Num a1)
                    {
                        amount = a0.Value;
                        lastTick = a1.Value;
                    }
                    else
                    {
                        continue;
                    }

                    stock[kv.Key] = stock.TryGetValue(kv.Key, out var prev)
                        ? (Math.Max(prev.amount, amount), Math.Max(prev.lastTick, lastTick))
                        : (amount, lastTick);
                }
            }

            var sb = new StringBuilder();
            sb.Append("{\"v\":").Append(Math.Max(Int(a, "v"), Int(b, "v")).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"levels\":{");
            var first = true;
            foreach (var kv in levels)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(Q(kv.Key)).Append(':').Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append("},\"built\":");
            sb.Append(b.Fields.TryGetValue("built", out var built) ? JsonWrite.Write(built) : "[]");
            sb.Append(",\"timers\":");
            sb.Append(b.Fields.TryGetValue("timers", out var timers) ? JsonWrite.Write(timers) : "[]");
            sb.Append(",\"temp_builder_until\":").Append(Num(b, "temp_builder_until").ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"pending\":");
            sb.Append(b.Fields.TryGetValue("pending", out var pending) ? JsonWrite.Write(pending) : "[]");
            sb.Append(",\"stock\":{");
            first = true;
            foreach (var kv in stock)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(Q(kv.Key)).Append(":[").Append(kv.Value.amount.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(',').Append(kv.Value.lastTick.ToString("R", CultureInfo.InvariantCulture)).Append(']');
            }

            sb.Append("}}");
            return sb.ToString();
        }

        private static Dictionary<string, int> MergeWallet(JsonNode.Obj? a, JsonNode.Obj? b)
        {
            var outMap = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var src in new[] { a, b })
            {
                if (src == null)
                {
                    continue;
                }

                foreach (var kv in src.Fields)
                {
                    if (kv.Value is JsonNode.Num n)
                    {
                        var v = (int)n.Value;
                        outMap[kv.Key] = Math.Max(outMap.TryGetValue(kv.Key, out var prev) ? prev : 0, v);
                    }
                }
            }

            return outMap;
        }

        private static List<string> Union(IEnumerable<string> a, IEnumerable<string> b)
        {
            var set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var x in a)
            {
                set.Add(x);
            }

            foreach (var x in b)
            {
                set.Add(x);
            }

            return new List<string>(set);
        }

        private static List<string> ArrStr(JsonNode.Obj o, string key)
        {
            var list = new List<string>();
            if (!o.Fields.TryGetValue(key, out var n) || n is not JsonNode.Arr arr)
            {
                return list;
            }

            foreach (var i in arr.Items)
            {
                if (i is JsonNode.Str s)
                {
                    list.Add(s.Value);
                }
            }

            return list;
        }

        private static JsonNode.Obj? Obj(JsonNode.Obj o, string key) =>
            o.Fields.TryGetValue(key, out var n) && n is JsonNode.Obj obj ? obj : null;

        private static int MaxInt(JsonNode.Obj a, JsonNode.Obj b, string key) => Math.Max(Int(a, key), Int(b, key));

        private static int Int(JsonNode.Obj? o, string key) =>
            o != null && o.Fields.TryGetValue(key, out var n) && n is JsonNode.Num num ? (int)num.Value : 0;

        private static double Num(JsonNode.Obj? o, string key) =>
            o != null && o.Fields.TryGetValue(key, out var n) && n is JsonNode.Num num ? num.Value : 0;

        private static bool Bool(JsonNode.Obj? o, string key) =>
            o != null && o.Fields.TryGetValue(key, out var n) && n is JsonNode.Bool b && b.Value;

        private static string Q(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.Append('"').ToString();
        }
    }

    internal static class JsonWrite
    {
        public static string Write(JsonNode node) => node switch
        {
            JsonNode.Obj o => WriteObj(o),
            JsonNode.Arr a => WriteArr(a),
            JsonNode.Str s => Q(s.Value),
            JsonNode.Num n => n.Value.ToString("R", CultureInfo.InvariantCulture),
            JsonNode.Bool b => b.Value ? "true" : "false",
            JsonNode.Null => "null",
            _ => "null",
        };

        private static string WriteObj(JsonNode.Obj o)
        {
            var sb = new StringBuilder("{");
            var first = true;
            foreach (var kv in o.Fields)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(Q(kv.Key)).Append(':').Append(Write(kv.Value));
            }

            return sb.Append('}').ToString();
        }

        private static string WriteArr(JsonNode.Arr a)
        {
            var sb = new StringBuilder("[");
            for (var i = 0; i < a.Items.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(Write(a.Items[i]));
            }

            return sb.Append(']').ToString();
        }

        private static string Q(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.Append('"').ToString();
        }
    }
}
