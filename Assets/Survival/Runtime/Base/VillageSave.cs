#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Clock;
using Survival.Domain.Serialization;

namespace Survival.Domain.Base
{
    /// <summary>
    /// Village save data: every plot's level, every building placed with Build (type, spot, level, still building?)
    /// and the running build / upgrade timers (wall-clock start + duration, so a timer keeps counting while the game
    /// is closed), plus congratulations rewards still waiting for their tap. Plain JSON; the Unity side keeps it in PlayerPrefs under <see cref="PrefsKey"/>.
    /// </summary>
    public static class VillageSave
    {
        public const string PrefsKey = "survival.village.save.v1";
        public const int Version = 1;

        public static string Capture(BaseEconomy economy, VillageBuild build)
        {
            var sb = new StringBuilder();
            sb.Append("{\"v\":").Append(Version).Append(",\"levels\":{");
            var first = true;
            foreach (var p in economy.Plots)
            {
                if (p.Def.BuildType != null)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                Str(sb, p.Id).Append(':').Append(p.Level.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append("},\"built\":[");
            first = true;
            foreach (var p in economy.Plots)
            {
                var spot = build.SpotOf(p.Id);
                if (p.Def.BuildType == null || spot == null)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append("{\"id\":");
                Str(sb, p.Id).Append(",\"type\":");
                Str(sb, p.Def.BuildType).Append(",\"spot\":");
                Str(sb, spot).Append(",\"level\":").Append(p.Level.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"building\":").Append(p.UnderConstruction ? "true" : "false").Append('}');
            }

            sb.Append("],\"timers\":[");
            first = true;
            foreach (var t in economy.Timers.Active)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append("{\"id\":");
                Str(sb, t.Id).Append(",\"target\":");
                Str(sb, t.TargetId).Append(",\"started\":").Append(t.StartedAt.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"seconds\":").Append(t.DurationSeconds.ToString("R", CultureInfo.InvariantCulture)).Append('}');
            }

            sb.Append("],\"pending\":[");
            first = true;
            foreach (var c in economy.Pending)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append("{\"plot\":");
                Str(sb, c.PlotId).Append(",\"level\":").Append(c.Level.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"build\":").Append(c.WasBuild ? "true" : "false").Append(",\"rewards\":{");
                var firstReward = true;
                foreach (var kv in c.Rewards)
                {
                    if (!firstReward)
                    {
                        sb.Append(',');
                    }

                    firstReward = false;
                    Str(sb, kv.Key).Append(':').Append(kv.Value.ToString(CultureInfo.InvariantCulture));
                }

                sb.Append("}}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>Apply a save onto a fresh session. Unknown plots / types are skipped. Returns false if unreadable.</summary>
        public static bool Apply(string? json, BaseEconomy economy, VillageBuild build)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            JsonNode.Obj root;
            try
            {
                if (JsonRead.Parse(json!) is not JsonNode.Obj o)
                {
                    return false;
                }

                root = o;
            }
            catch (FormatException)
            {
                return false;
            }

            if (root.TryGet("levels", out var lv) && lv is JsonNode.Obj levels)
            {
                foreach (var pair in levels.Fields)
                {
                    if (pair.Value is JsonNode.Num n)
                    {
                        economy.RestoreLevel(pair.Key, n.AsInt());
                    }
                }
            }

            if (root.TryGet("built", out var b) && b is JsonNode.Arr built)
            {
                foreach (var item in built.Items)
                {
                    if (item is not JsonNode.Obj o)
                    {
                        continue;
                    }

                    var id = S(o, "id");
                    var type = S(o, "type");
                    var spot = S(o, "spot");
                    if (id == null || type == null || spot == null)
                    {
                        continue;
                    }

                    var level = o.TryGet("level", out var l) && l is JsonNode.Num ln ? ln.AsInt() : 1;
                    var building = o.TryGet("building", out var bb) && bb is JsonNode.Bool bv && bv.Value;
                    build.Restore(id, type, spot, level, building);
                }
            }

            if (root.TryGet("timers", out var tm) && tm is JsonNode.Arr timers)
            {
                foreach (var item in timers.Items)
                {
                    if (item is not JsonNode.Obj o)
                    {
                        continue;
                    }

                    var id = S(o, "id");
                    var target = S(o, "target");
                    if (id == null || target == null || economy.Find(target) == null)
                    {
                        continue;
                    }

                    var started = o.TryGet("started", out var st) && st is JsonNode.Num sn ? sn.Value : economy.Clock.Now;
                    var seconds = o.TryGet("seconds", out var se) && se is JsonNode.Num secn ? secn.Value : 0;
                    economy.Timers.Restore(id, target, started, seconds);
                }
            }

            // Congratulations rewards still waiting for their tap.
            if (root.TryGet("pending", out var pd) && pd is JsonNode.Arr pending)
            {
                foreach (var item in pending.Items)
                {
                    if (item is not JsonNode.Obj o || S(o, "plot") is not { } plot)
                    {
                        continue;
                    }

                    var rewards = new Dictionary<string, int>();
                    if (o.TryGet("rewards", out var rw) && rw is JsonNode.Obj ro)
                    {
                        foreach (var pair in ro.Fields)
                        {
                            if (pair.Value is JsonNode.Num rn && rn.AsInt() > 0)
                            {
                                rewards[pair.Key] = rn.AsInt();
                            }
                        }
                    }

                    var level = o.TryGet("level", out var pl) && pl is JsonNode.Num pln ? pln.AsInt() : 1;
                    var wasBuild = o.TryGet("build", out var pb) && pb is JsonNode.Bool pbv && pbv.Value;
                    economy.RestorePending(plot, level, wasBuild, rewards);
                }
            }

            // A construction whose timer didn't survive (corrupt save) would be stuck: just finish it.
            foreach (var p in economy.Plots)
            {
                if (p.UnderConstruction && economy.Timers.Find(BaseEconomy.UpgradeTimerId(p.Id)) == null)
                {
                    economy.RestoreLevel(p.Id, p.Level, false);
                }
            }

            return true;
        }

        private static string? S(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Str s && !string.IsNullOrEmpty(s.Value) ? s.Value : null;

        private static StringBuilder Str(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var c in value)
            {
                if (c == '"' || c == '\\')
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            return sb.Append('"');
        }
    }
}
