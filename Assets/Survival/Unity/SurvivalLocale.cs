#nullable enable
using System;
using System.Globalization;
using System.IO;
using Survival.Domain.Localization;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The player's language: a saved choice (Settings → Language) or, until they pick one, the device language.
    /// Loads <c>strings/&lt;code&gt;.json</c> to lay over <c>strings/en.json</c>; any key it lacks stays English.
    /// </summary>
    public static class SurvivalLocale
    {
        private static string? _current;

        /// <summary>The active locale code (e.g. "de", "zh-Hans"); "en" when nothing else matches.</summary>
        public static string Current => _current ??= Locales.Resolve(PlayerPrefs.GetString(Locales.PrefKey, ""), Application.systemLanguage.ToString(), Region());

        /// <summary>The saved choice, "" when following the device.</summary>
        public static string Saved => PlayerPrefs.GetString(Locales.PrefKey, "");

        /// <summary>Saves the choice ("" = follow the device). The new language shows once the scene reloads.</summary>
        public static void Choose(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                PlayerPrefs.DeleteKey(Locales.PrefKey);
            }
            else
            {
                PlayerPrefs.SetString(Locales.PrefKey, code);
            }

            PlayerPrefs.Save();
            _current = null;
        }

        /// <summary>The locale overlay for a pack, or null for English or when the file is missing or unreadable.</summary>
        public static string? OverlayJson(string packRoot)
        {
            var code = Current;
            if (code == Locales.English)
            {
                return null;
            }

            var path = Path.Combine(packRoot, "strings", code + ".json");
            try
            {
                return PlayerAssets.FileExists(path) ? PlayerAssets.ReadAllText(path) : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Locale " + code + " strings unreadable, using English. " + ex.Message);
                return null;
            }
        }

        private static string Region()
        {
            try
            {
                return RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
