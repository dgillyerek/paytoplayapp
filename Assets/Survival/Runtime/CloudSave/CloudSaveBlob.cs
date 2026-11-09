#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Serialization;

namespace Survival.Domain.CloudSave
{
    /// <summary>
    /// Versioned cloud-save document. Game progress only — no personal info. Keyed on the server by the random
    /// <c>players.id</c> account from sign-in.
    /// Schema 1: campaign, village, quests (by chain id), ftue, world.gather (Quarry stock), iapApplied, grantLedger.
    /// </summary>
    public sealed class CloudSaveDocument
    {
        public const int Schema = 1;

        public double SavedAt { get; set; }
        public string? CampaignJson { get; set; }
        public string? VillageJson { get; set; }

        /// <summary>Quest progress keyed by chain id (e.g. quest.chain.starter). Works with the 3-step and 32-step chains.</summary>
        public Dictionary<string, QuestProgress> Quests { get; } = new(StringComparer.Ordinal);

        public string? FtueStepId { get; set; }

        /// <summary>World gather remaining stock keyed by node id (Quarry etc.).</summary>
        public Dictionary<string, GatherStock> Gather { get; } = new(StringComparer.Ordinal);

        public List<string> IapApplied { get; } = new();
        public Dictionary<string, Dictionary<string, int>> GrantLedger { get; } = new(StringComparer.Ordinal);

        public sealed class QuestProgress
        {
            public int Index { get; set; }
            public int Progress { get; set; }
        }

        public sealed class GatherStock
        {
            public int Available { get; set; }
            public double UpdatedAt { get; set; }

            /// <summary>Optional future regen clock (unix seconds). Null/0 = no regen scheduled.</summary>
            public double? RegenAt { get; set; }
        }

        public string Write()
        {
            var sb = new StringBuilder();
            sb.Append("{\"schema\":").Append(Schema.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"savedAt\":").Append(SavedAt.ToString("R", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(CampaignJson))
            {
                sb.Append(",\"campaign\":").Append(CampaignJson);
            }

            if (!string.IsNullOrEmpty(VillageJson))
            {
                sb.Append(",\"village\":").Append(VillageJson);
            }

            sb.Append(",\"quests\":{");
            var first = true;
            foreach (var kv in Quests)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(Q(kv.Key)).Append(":{\"index\":").Append(kv.Value.Index.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"progress\":").Append(kv.Value.Progress.ToString(CultureInfo.InvariantCulture)).Append('}');
            }

            sb.Append('}');
            if (!string.IsNullOrEmpty(FtueStepId))
            {
                sb.Append(",\"ftue\":{\"stepId\":").Append(Q(FtueStepId)).Append('}');
            }

            sb.Append(",\"world\":{\"gather\":{");
            first = true;
            foreach (var kv in Gather)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(Q(kv.Key)).Append(":{\"available\":").Append(kv.Value.Available.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"updatedAt\":").Append(kv.Value.UpdatedAt.ToString("R", CultureInfo.InvariantCulture));
                if (kv.Value.RegenAt is double regen && regen > 0)
                {
                    sb.Append(",\"regenAt\":").Append(regen.ToString("R", CultureInfo.InvariantCulture));
                }

                sb.Append('}');
            }

            sb.Append("}}");
            sb.Append(",\"iapApplied\":[");
            for (var i = 0; i < IapApplied.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(Q(IapApplied[i]));
            }

            sb.Append("],\"grantLedger\":{");
            first = true;
            foreach (var kv in GrantLedger)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(Q(kv.Key)).Append(":{");
                var firstG = true;
                foreach (var g in kv.Value)
                {
                    if (!firstG)
                    {
                        sb.Append(',');
                    }

                    firstG = false;
                    sb.Append(Q(g.Key)).Append(':').Append(g.Value.ToString(CultureInfo.InvariantCulture));
                }

                sb.Append('}');
            }

            sb.Append("}}");
            return sb.ToString();
        }

        public static CloudSaveDocument? Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                if (JsonRead.Parse(json) is not JsonNode.Obj root)
                {
                    return null;
                }

                var doc = new CloudSaveDocument();
                if (root.TryGet("savedAt", out var sa) && sa is JsonNode.Num n)
                {
                    doc.SavedAt = n.Value;
                }

                if (root.TryGet("campaign", out var c) && c is JsonNode.Obj)
                {
                    doc.CampaignJson = SliceObject(json, "campaign");
                }

                if (root.TryGet("village", out var v) && v is JsonNode.Obj)
                {
                    doc.VillageJson = SliceObject(json, "village");
                }

                if (root.TryGet("quests", out var qs) && qs is JsonNode.Obj qo)
                {
                    foreach (var pair in qo.Fields)
                    {
                        if (pair.Value is JsonNode.Obj entry)
                        {
                            doc.Quests[pair.Key] = new QuestProgress { Index = Int(entry, "index"), Progress = Int(entry, "progress") };
                        }
                    }
                }
                else if (root.TryGet("quest", out var legacy) && legacy is JsonNode.Obj lo)
                {
                    // Legacy singular shape from early drafts.
                    var chainId = Str(lo, "chainId");
                    if (!string.IsNullOrEmpty(chainId))
                    {
                        doc.Quests[chainId] = new QuestProgress { Index = Int(lo, "index"), Progress = Int(lo, "progress") };
                    }
                }

                if (root.TryGet("ftue", out var f) && f is JsonNode.Obj fo)
                {
                    doc.FtueStepId = Str(fo, "stepId");
                }

                if (root.TryGet("world", out var w) && w is JsonNode.Obj wo && wo.TryGet("gather", out var g) && g is JsonNode.Obj go)
                {
                    foreach (var pair in go.Fields)
                    {
                        if (pair.Value is not JsonNode.Obj ge)
                        {
                            continue;
                        }

                        double? regen = null;
                        if (ge.TryGet("regenAt", out var rn) && rn is JsonNode.Num rnum)
                        {
                            regen = rnum.Value;
                        }

                        doc.Gather[pair.Key] = new GatherStock
                        {
                            Available = Int(ge, "available"),
                            UpdatedAt = Num(ge, "updatedAt"),
                            RegenAt = regen,
                        };
                    }
                }

                if (root.TryGet("iapApplied", out var ia) && ia is JsonNode.Arr arr)
                {
                    foreach (var item in arr.Items)
                    {
                        if (item is JsonNode.Str s)
                        {
                            doc.IapApplied.Add(s.Value);
                        }
                    }
                }

                if (root.TryGet("grantLedger", out var gl) && gl is JsonNode.Obj glo)
                {
                    foreach (var pair in glo.Fields)
                    {
                        if (pair.Value is not JsonNode.Obj grants)
                        {
                            continue;
                        }

                        var map = new Dictionary<string, int>(StringComparer.Ordinal);
                        foreach (var gr in grants.Fields)
                        {
                            if (gr.Value is JsonNode.Num gn)
                            {
                                map[gr.Key] = (int)gn.Value;
                            }
                        }

                        doc.GrantLedger[pair.Key] = map;
                    }
                }

                return doc;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static string? SliceObject(string json, string key)
        {
            var needle = "\"" + key + "\"";
            var i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0)
            {
                return null;
            }

            i = json.IndexOf('{', i + needle.Length);
            if (i < 0)
            {
                return null;
            }

            var depth = 0;
            for (var j = i; j < json.Length; j++)
            {
                var c = json[j];
                if (c == '"')
                {
                    j++;
                    while (j < json.Length)
                    {
                        if (json[j] == '\\')
                        {
                            j += 2;
                            continue;
                        }

                        if (json[j] == '"')
                        {
                            break;
                        }

                        j++;
                    }

                    continue;
                }

                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return json.Substring(i, j - i + 1);
                    }
                }
            }

            return null;
        }

        private static string Str(JsonNode.Obj o, string key) =>
            o.Fields.TryGetValue(key, out var n) && n is JsonNode.Str s ? s.Value : "";

        private static int Int(JsonNode.Obj o, string key) =>
            o.Fields.TryGetValue(key, out var n) && n is JsonNode.Num num ? (int)num.Value : 0;

        private static double Num(JsonNode.Obj o, string key) =>
            o.Fields.TryGetValue(key, out var n) && n is JsonNode.Num num ? num.Value : 0;

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
