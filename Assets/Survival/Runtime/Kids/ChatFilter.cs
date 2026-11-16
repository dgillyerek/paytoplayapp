#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Survival.Domain.Serialization;

namespace Survival.Domain.Kids
{
    /// <summary>Outcome of filtering one chat body. Refusal: "links_not_allowed" / "personal_info", or null when it may be sent.</summary>
    public sealed record ChatFilterResult(string Text, int Masked, string? Refusal)
    {
        /// <summary>Masked or refused: the server copies it to the moderation queue.</summary>
        public bool Flagged => Masked > 0 || Refusal != null;
    }

    /// <summary>
    /// 13+ guild free-text filter (kids/chat_filter.json). Whole-word profanity masking after leetspeak folding and
    /// repeated-letter collapsing, plus optional link and contact-info refusal. The server (moderation.mjs) runs the same
    /// rules and is authoritative; the client uses this to mask what it shows. Mirror any rule change in both.
    /// </summary>
    public sealed class ChatFilter
    {
        public const string FileName = "chat_filter.json";

        private static readonly Regex Word = new Regex("[\\p{L}0-9@$]+", RegexOptions.CultureInvariant);
        private static readonly Regex Link = new Regex("(https?://|www\\.|\\b[a-z0-9-]+\\.(com|net|org|io|gg|ph|ca|au|nz|me|ly|co|app|xyz|tv)\\b)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex Email = new Regex("[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]{2,}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex Phone = new Regex("(\\+?\\d[\\d\\s().-]{7,}\\d)", RegexOptions.CultureInvariant);

        private readonly HashSet<string> _words = new HashSet<string>(StringComparer.Ordinal);

        public ChatFilter(IEnumerable<string> words, bool blockLinks = true, bool blockContactInfo = true)
        {
            foreach (var w in words)
            {
                var n = Fold(w);
                if (n.Length > 0)
                {
                    _words.Add(n);
                    _words.Add(Collapse(n));
                }
            }

            BlockLinks = blockLinks;
            BlockContactInfo = blockContactInfo;
        }

        /// <summary>No words, no link / contact rules (pack file missing).</summary>
        public static ChatFilter None { get; } = new ChatFilter(Array.Empty<string>(), false, false);

        public bool BlockLinks { get; }

        public bool BlockContactInfo { get; }

        public int WordCount => _words.Count;

        public static ChatFilter Parse(string json)
        {
            if (JsonRead.Parse(json) is not JsonNode.Obj root)
            {
                throw new FormatException("chat_filter.json must be an object.");
            }

            var words = new List<string>();
            if (root.TryGet("words", out var wn) && wn is JsonNode.Arr wa)
            {
                foreach (var i in wa.Items)
                {
                    if (i is JsonNode.Str s)
                    {
                        words.Add(s.Value);
                    }
                }
            }

            return new ChatFilter(words, Flag(root, "block_links"), Flag(root, "block_contact_info"));
        }

        public ChatFilterResult Apply(string body)
        {
            body ??= "";
            if (BlockLinks && Link.IsMatch(body))
            {
                return new ChatFilterResult(body, 0, "links_not_allowed");
            }

            if (BlockContactInfo && (Email.IsMatch(body) || Phone.IsMatch(body)))
            {
                return new ChatFilterResult(body, 0, "personal_info");
            }

            var masked = 0;
            var text = Word.Replace(body, m =>
            {
                var f = Fold(m.Value);
                if (_words.Contains(f) || _words.Contains(Collapse(f)))
                {
                    masked++;
                    return new string('*', m.Value.Length);
                }

                return m.Value;
            });
            return new ChatFilterResult(text, masked, null);
        }

        /// <summary>Lower-case and fold common leetspeak (0 o, 1 i, 3 e, 4 a, 5 s, 7 t, @ a, $ s).</summary>
        internal static string Fold(string w)
        {
            var sb = new StringBuilder(w.Length);
            foreach (var ch in w.ToLowerInvariant())
            {
                sb.Append(ch switch
                {
                    '0' => 'o',
                    '1' => 'i',
                    '3' => 'e',
                    '4' => 'a',
                    '5' => 's',
                    '7' => 't',
                    '@' => 'a',
                    '$' => 's',
                    _ => ch,
                });
            }

            return sb.ToString();
        }

        /// <summary>Runs of the same letter become one ("fuuuck" → "fuck"; list words are collapsed the same way).</summary>
        internal static string Collapse(string w)
        {
            var sb = new StringBuilder(w.Length);
            foreach (var ch in w)
            {
                if (sb.Length == 0 || sb[sb.Length - 1] != ch)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static bool Flag(JsonNode.Obj o, string key) => !o.TryGet(key, out var n) || n is not JsonNode.Bool b || b.Value;
    }
}
