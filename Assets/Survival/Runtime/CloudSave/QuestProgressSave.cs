#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Quest;
using Survival.Domain.Serialization;

namespace Survival.Domain.CloudSave
{
    /// <summary>
    /// Local quest progress keyed by chain id (quest.chain.starter). Survives relaunch; works with the 3-step chain
    /// and the 32-step Theme A chain on PR #202 (same chain id). Prefs: <see cref="PrefsKey"/>.
    /// </summary>
    public static class QuestProgressSave
    {
        public const string PrefsKey = "survival.quest.progress.v1";
        public const int Version = 1;

        public static string Capture(string chainId, QuestSystem quests)
        {
            var sb = new StringBuilder();
            sb.Append("{\"v\":").Append(Version.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"quests\":{");
            sb.Append(Q(chainId)).Append(":{\"index\":").Append(quests.Index.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"progress\":").Append(quests.Progress.ToString(CultureInfo.InvariantCulture)).Append('}');
            sb.Append("}}");
            return sb.ToString();
        }

        public static bool Apply(string? json, string chainId, QuestSystem quests)
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

                if (!root.TryGet("quests", out var qs) || qs is not JsonNode.Obj qo)
                {
                    return false;
                }

                if (!qo.TryGet(chainId, out var entry) || entry is not JsonNode.Obj eo)
                {
                    return false;
                }

                var index = eo.TryGet("index", out var ix) && ix is JsonNode.Num n ? (int)n.Value : 0;
                var progress = eo.TryGet("progress", out var pr) && pr is JsonNode.Num p ? (int)p.Value : 0;
                quests.Restore(index, progress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static void FillDocument(CloudSaveDocument doc, string chainId, QuestSystem quests)
        {
            doc.Quests[chainId] = new CloudSaveDocument.QuestProgress
            {
                Index = quests.Index,
                Progress = quests.Progress,
            };
        }

        public static void ApplyDocument(CloudSaveDocument doc, string chainId, QuestSystem quests)
        {
            if (doc.Quests.TryGetValue(chainId, out var q))
            {
                quests.Restore(q.Index, q.Progress);
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
