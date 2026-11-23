#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Localization
{
    /// <summary>One shipped language: the strings file code, the name shown in its own language, and the Unity
    /// <c>SystemLanguage</c> names that pick it on first launch.</summary>
    public sealed record Locale(string Code, string NativeName, IReadOnlyList<string> SystemLanguages);

    /// <summary>
    /// Languages the game ships. Each has <c>ThemePack/&lt;pack&gt;/strings/&lt;Code&gt;.json</c>; a key missing there
    /// falls back to <c>en.json</c>. Arabic, Hindi and Thai are left out until text shaping (RTL / complex scripts) lands:
    /// legacy UGUI Text cannot draw them correctly.
    /// </summary>
    public static class Locales
    {
        public const string English = "en";

        /// <summary>PlayerPrefs key holding the player's chosen code; absent means "follow the device".</summary>
        public const string PrefKey = "survival.locale";

        public static readonly IReadOnlyList<Locale> All = new[]
        {
            new Locale("en", "English", new[] { "English" }),
            new Locale("es-419", "Español (Latinoamérica)", Array.Empty<string>()),
            new Locale("es-ES", "Español (España)", new[] { "Spanish" }),
            new Locale("pt-BR", "Português (Brasil)", new[] { "Portuguese" }),
            new Locale("de", "Deutsch", new[] { "German" }),
            new Locale("fr", "Français", new[] { "French" }),
            new Locale("it", "Italiano", new[] { "Italian" }),
            new Locale("ja", "日本語", new[] { "Japanese" }),
            new Locale("ko", "한국어", new[] { "Korean" }),
            new Locale("zh-Hans", "简体中文", new[] { "ChineseSimplified", "Chinese" }),
            new Locale("zh-Hant", "繁體中文", new[] { "ChineseTraditional" }),
            new Locale("ru", "Русский", new[] { "Russian" }),
            new Locale("tr", "Türkçe", new[] { "Turkish" }),
            new Locale("pl", "Polski", new[] { "Polish" }),
            new Locale("nl", "Nederlands", new[] { "Dutch" }),
            new Locale("id", "Bahasa Indonesia", new[] { "Indonesian" }),
            new Locale("vi", "Tiếng Việt", new[] { "Vietnamese" }),
        };

        public static Locale? Find(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            foreach (var l in All)
            {
                if (string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    return l;
                }
            }

            return null;
        }

        /// <summary>
        /// The code to load: the saved choice if it is still shipped, else the device language (Spanish devices in a
        /// Latin American region get es-419), else English.
        /// </summary>
        public static string Resolve(string? savedCode, string? systemLanguage, string? regionCode = null)
        {
            var saved = Find(savedCode);
            if (saved != null)
            {
                return saved.Code;
            }

            if (string.Equals(systemLanguage, "Spanish", StringComparison.Ordinal) && IsLatinAmerica(regionCode))
            {
                return "es-419";
            }

            foreach (var l in All)
            {
                foreach (var name in l.SystemLanguages)
                {
                    if (string.Equals(name, systemLanguage, StringComparison.Ordinal))
                    {
                        return l.Code;
                    }
                }
            }

            return English;
        }

        private static readonly HashSet<string> LatAm = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MX", "AR", "CO", "CL", "PE", "VE", "EC", "GT", "CU", "BO", "DO", "HN", "PY", "SV", "NI", "CR", "PA", "UY", "PR", "US"
        };

        private static bool IsLatinAmerica(string? region) => !string.IsNullOrEmpty(region) && LatAm.Contains(region!);
    }
}
