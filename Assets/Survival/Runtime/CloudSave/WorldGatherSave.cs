#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Serialization;
using Survival.Domain.World;

namespace Survival.Domain.CloudSave
{
    /// <summary>
    /// Local + cloud persistence for world gather remaining stock (Quarry etc.).
    /// <see cref="WorldMarchLoop.Available"/> starts from catalog and was previously memory-only, so each launch
    /// reset the Quarry to full. This save keeps remaining stock (and an optional regen timestamp for a future refill).
    /// Prefs key: <see cref="PrefsKey"/>.
    /// </summary>
    public static class WorldGatherSave
    {
        public const string PrefsKey = "survival.world.gather.save.v1";
        public const int Version = 1;

        public static string Capture(WorldMap map, double nowSeconds)
        {
            var sb = new StringBuilder();
            sb.Append("{\"v\":").Append(Version.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"gather\":{");
            var first = true;
            foreach (var node in map.Nodes)
            {
                if (!map.TryGetMarch(node.Def.Id, out var loop) || !loop.ConsumeYieldFromStock)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(Q(loop.NodeId)).Append(":{\"available\":")
                    .Append(loop.Available.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"updatedAt\":").Append(nowSeconds.ToString("R", CultureInfo.InvariantCulture));
                if (loop.RegenAt is double regen && regen > 0)
                {
                    sb.Append(",\"regenAt\":").Append(regen.ToString("R", CultureInfo.InvariantCulture));
                }
                sb.Append('}');
            }

            sb.Append("}}");
            return sb.ToString();
        }

        public static bool Apply(string? json, WorldMap map)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                if (JsonRead.Parse(json) is not JsonNode.Obj root)
                {
                    return false;
                }

                if (!root.TryGet("gather", out var g) || g is not JsonNode.Obj go)
                {
                    return false;
                }

                foreach (var pair in go.Fields)
                {
                    if (pair.Value is not JsonNode.Obj entry)
                    {
                        continue;
                    }

                    if (!map.TryGetMarch(pair.Key, out var loop) || !loop.ConsumeYieldFromStock)
                    {
                        continue;
                    }

                    if (entry.TryGet("available", out var av) && av is JsonNode.Num n)
                    {
                        loop.RestoreAvailable((int)n.Value);
                    }

                    if (entry.TryGet("regenAt", out var rn) && rn is JsonNode.Num rnum && rnum.Value > 0)
                    {
                        loop.RestoreRegenAt(rnum.Value);
                    }
                    else
                    {
                        loop.RestoreRegenAt(null);
                    }
                }

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static void FillDocument(CloudSaveDocument doc, WorldMap map, double nowSeconds)
        {
            doc.Gather.Clear();
            foreach (var node in map.Nodes)
            {
                if (!map.TryGetMarch(node.Def.Id, out var loop) || !loop.ConsumeYieldFromStock)
                {
                    continue;
                }

                doc.Gather[loop.NodeId] = new CloudSaveDocument.GatherStock
                {
                    Available = loop.Available,
                    UpdatedAt = nowSeconds,
                    RegenAt = loop.RegenAt,
                };
            }
        }

        public static void ApplyDocument(CloudSaveDocument doc, WorldMap map)
        {
            foreach (var kv in doc.Gather)
            {
                if (!map.TryGetMarch(kv.Key, out var loop) || !loop.ConsumeYieldFromStock)
                {
                    continue;
                }

                loop.RestoreAvailable(kv.Value.Available);
                loop.RestoreRegenAt(kv.Value.RegenAt);
            }
        }

        private static string Q(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s ?? "")
            {
                if (c is '"' or '\\')
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            return sb.Append('"').ToString();
        }
    }
}
