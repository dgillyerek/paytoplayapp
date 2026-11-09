#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Survival.Domain.Kids
{
    /// <summary>
    /// Guild chat rules for child mode. Off: no chat tab content at all. Presets: the player can only send a preset id
    /// (the wire body is <c>[[p:&lt;id&gt;]]</c>, which the server checks), and only sees preset messages. Free (13+ only):
    /// normal chat. Report and block work in every mode. Display names in restricted modes are generated, never typed.
    /// </summary>
    public static class ChatSafety
    {
        private static readonly Regex PresetBody = new Regex("^\\[\\[p:([a-z0-9_]{1,32})\\]\\]$", RegexOptions.CultureInvariant);

        public static string EncodePreset(string id)
        {
            if (!PresetBody.IsMatch("[[p:" + id + "]]"))
            {
                throw new ArgumentException("preset ids are 1-32 of a-z 0-9 _", nameof(id));
            }

            return "[[p:" + id + "]]";
        }

        public static bool TryPreset(string body, out string id)
        {
            var m = PresetBody.Match(body ?? "");
            id = m.Success ? m.Groups[1].Value : "";
            return m.Success;
        }

        public static bool CanRead(ChildSafetyPolicy p) => p.Chat != ChatMode.Off;

        public static bool CanSendFreeText(ChildSafetyPolicy p) => p.Chat == ChatMode.Free;

        public static bool CanSendPresets(ChildSafetyPolicy p) => p.Chat != ChatMode.Off;

        /// <summary>Whether a typed display name is allowed (13+ free-chat only).</summary>
        public static bool CanTypeName(ChildSafetyPolicy p) => p.Chat == ChatMode.Free && !p.IsChild;

        /// <summary>
        /// Text to show for one message, or null to hide it: blocked senders are hidden for everyone; presets mode hides
        /// anything that is not a known preset; presets render from the pack text (localized by the caller's lookup).
        /// </summary>
        public static string? Visible(ChildSafetyPolicy p, long senderId, string body, ISet<long> blocked, Func<string, string?> presetText)
        {
            if (!CanRead(p) || blocked.Contains(senderId))
            {
                return null;
            }

            if (TryPreset(body, out var id))
            {
                return presetText(id);
            }

            return p.Chat == ChatMode.Free ? body : null;
        }

        private static readonly string[] Adjectives = { "Brave", "Swift", "Bold", "Clever", "Mighty", "Noble", "Quiet", "Lucky", "Bright", "Steady" };
        private static readonly string[] Nouns = { "Knight", "Archer", "Ranger", "Squire", "Mage", "Builder", "Scout", "Warden", "Smith", "Rider" };

        /// <summary>A generated, PII-free display name from a stable seed (the device id), e.g. "Swift Ranger 417".</summary>
        public static string SafeName(string seed)
        {
            unchecked
            {
                var h = 17;
                foreach (var c in seed ?? "")
                {
                    h = h * 31 + c;
                }

                var u = (uint)h;
                return new StringBuilder().Append(Adjectives[u % 10]).Append(' ').Append(Nouns[u / 10 % 10]).Append(' ').Append(100 + u / 100 % 900).ToString();
            }
        }

        /// <summary>The server-side shape check for generated names (see Backend/alliance/src/safety.mjs SAFE_NAME).</summary>
        public static bool IsSafeName(string name) => Regex.IsMatch(name ?? "", "^[A-Z][a-z]+ [A-Z][a-z]+ [0-9]{3}$");

        public static string ChatModeId(ChatMode m) => m == ChatMode.Free ? "free" : m == ChatMode.Presets ? "presets" : "off";
    }
}
