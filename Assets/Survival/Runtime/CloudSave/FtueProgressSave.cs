#nullable enable
using System;
using System.Globalization;
using System.Text;
using Survival.Domain.Ftue;
using Survival.Domain.Serialization;

namespace Survival.Domain.CloudSave
{
    /// <summary>Local FTUE step id. Prefs: <see cref="PrefsKey"/>.</summary>
    public static class FtueProgressSave
    {
        public const string PrefsKey = "survival.ftue.progress.v1";
        public const int Version = 1;

        public static string Capture(FtueSystem ftue)
        {
            var sb = new StringBuilder();
            sb.Append("{\"v\":").Append(Version.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stepId\":").Append(Q(ftue.CurrentStepId)).Append('}');
            return sb.ToString();
        }

        public static bool Apply(string? json, FtueSystem ftue)
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

                if (root.TryGet("stepId", out var s) && s is JsonNode.Str str && !string.IsNullOrEmpty(str.Value))
                {
                    ftue.AdvanceTo(str.Value);
                    return true;
                }

                return false;
            }
            catch (FormatException)
            {
                return false;
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
