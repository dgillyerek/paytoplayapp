#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Survival.Domain.Serialization;

namespace Survival.Domain.Ota
{
    /// <summary>
    /// JSON helpers for over-the-air data: RFC 7386 merge patch (remote balance overrides on top of the baked catalog
    /// files), a writer that turns the patched tree back into text the catalog parsers read, and a canonical form + SHA-256
    /// so the publish-time free-pace check and the phone agree on exactly which overrides were checked.
    /// </summary>
    internal static class JsonPatch
    {
        /// <summary>RFC 7386: objects merge key by key, null deletes a key, anything else (arrays too) replaces.</summary>
        public static JsonNode MergePatch(JsonNode target, JsonNode patch)
        {
            if (!(patch is JsonNode.Obj p))
            {
                return patch;
            }

            var result = new JsonNode.Obj();
            if (target is JsonNode.Obj t)
            {
                foreach (var kv in t.Fields)
                {
                    result.Fields[kv.Key] = kv.Value;
                }
            }

            foreach (var kv in p.Fields)
            {
                if (kv.Value is JsonNode.Null)
                {
                    result.Fields.Remove(kv.Key);
                    continue;
                }

                result.Fields[kv.Key] = result.Fields.TryGetValue(kv.Key, out var existing)
                    ? MergePatch(existing, kv.Value)
                    : MergePatch(JsonNode.Null.Instance, kv.Value);
            }

            return result;
        }

        /// <summary>Plain JSON text, keys in their stored order, numbers round-tripped (integers without a fraction).</summary>
        public static string Write(JsonNode node)
        {
            var sb = new StringBuilder();
            WriteNode(sb, node, canonical: false);
            return sb.ToString();
        }

        /// <summary>Canonical text: keys sorted ordinally, no whitespace, numbers in a runtime-independent decimal form.</summary>
        public static string Canonical(JsonNode node)
        {
            var sb = new StringBuilder();
            WriteNode(sb, node, canonical: true);
            return sb.ToString();
        }

        /// <summary>Lower-case hex SHA-256 of the UTF-8 canonical form.</summary>
        public static string Sha256Hex(JsonNode node)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(node)));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private static void WriteNode(StringBuilder sb, JsonNode node, bool canonical)
        {
            switch (node)
            {
                case JsonNode.Obj o:
                {
                    sb.Append('{');
                    var keys = new List<string>(o.Fields.Keys);
                    if (canonical)
                    {
                        keys.Sort(StringComparer.Ordinal);
                    }

                    for (var i = 0; i < keys.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        WriteString(sb, keys[i]);
                        sb.Append(':');
                        WriteNode(sb, o.Fields[keys[i]], canonical);
                    }

                    sb.Append('}');
                    return;
                }
                case JsonNode.Arr a:
                    sb.Append('[');
                    for (var i = 0; i < a.Items.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        WriteNode(sb, a.Items[i], canonical);
                    }

                    sb.Append(']');
                    return;
                case JsonNode.Str s:
                    WriteString(sb, s.Value);
                    return;
                case JsonNode.Num n:
                    sb.Append(FormatNumber(n.Value, canonical));
                    return;
                case JsonNode.Bool b:
                    sb.Append(b.Value ? "true" : "false");
                    return;
                default:
                    sb.Append("null");
                    return;
            }
        }

        internal static string FormatNumber(double v, bool canonical)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
            {
                throw new FormatException("JSON numbers must be finite.");
            }

            if (Math.Abs(v) < 1e15 && Math.Abs(v - Math.Round(v)) < 1e-12)
            {
                return ((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture);
            }

            if (canonical)
            {
                // System.Decimal keeps 15 significant digits and formats the same on .NET, Mono and IL2CPP, unlike "R".
                return Math.Abs(v) < 7.9e27 && Math.Abs(v) > 1e-20
                    ? ((decimal)v).ToString(CultureInfo.InvariantCulture)
                    : v.ToString("E14", CultureInfo.InvariantCulture);
            }

            return v.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
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

            sb.Append('"');
        }
    }
}
