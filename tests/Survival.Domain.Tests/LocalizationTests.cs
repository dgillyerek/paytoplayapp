using System.Text.Json;
using System.Text.RegularExpressions;
using Survival.Domain.Localization;
using Survival.Domain.Theme;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-22: "localize the entire app for different languages". Each shipped locale is a strings file laid over
/// en.json; a key it lacks stays English. These guard the files against drift: no stray keys, no lost placeholders,
/// no locale listed without a file, and no UI key used in code that en.json is missing.
/// </summary>
public sealed class LocalizationTests
{
    private static readonly Regex Placeholder = new(@"\{\d+(?::[^}]*)?\}", RegexOptions.Compiled);

    private static string StringsDir => Path.Combine(KidsFiles.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "strings");

    private static Dictionary<string, string> Read(string code)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(StringsDir, code + ".json")));
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
    }

    public static IEnumerable<object[]> Translations() =>
        Locales.All.Where(l => l.Code != Locales.English).Select(l => new object[] { l.Code });

    [Fact]
    public void Every_listed_locale_has_a_strings_file_and_every_file_is_listed()
    {
        var files = Directory.GetFiles(StringsDir, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)!).OrderBy(c => c).ToList();
        var listed = Locales.All.Select(l => l.Code).OrderBy(c => c).ToList();
        Assert.Equal(listed, files);
        Assert.Equal(listed.Count, listed.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Locale_keys_exist_in_english_and_keep_every_placeholder(string code)
    {
        var en = Read(Locales.English);
        var loc = Read(code);
        Assert.NotEmpty(loc);
        foreach (var (key, value) in loc)
        {
            Assert.True(en.ContainsKey(key), $"{code}: '{key}' is not in en.json");
            Assert.False(string.IsNullOrWhiteSpace(value), $"{code}: '{key}' is empty");
            var want = Placeholder.Matches(en[key]).Select(m => m.Value).OrderBy(v => v);
            var got = Placeholder.Matches(value).Select(m => m.Value).OrderBy(v => v);
            Assert.True(want.SequenceEqual(got), $"{code}: '{key}' placeholders differ from English");
            _ = string.Format(value, new object[] { 1, 2, 3, 4, 5 });
        }

        // Most player-facing text is translated: only names, numbers and debug lines may stay English.
        Assert.True(loc.Count >= en.Count * 0.9, $"{code}: only {loc.Count} of {en.Count} keys translated");
    }

    [Fact]
    public void Every_ui_key_the_code_asks_for_is_in_english()
    {
        var en = Read(Locales.English);
        var lookup = new Regex(@"\b(?:S|Fmt|TapS|F|StringOr|TapFmt|_s)\(\s*""([a-z_]+\.[a-z0-9_.]+)"",\s*""", RegexOptions.Compiled);
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(Path.Combine(KidsFiles.RepoRoot(), "Assets", "Survival"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match m in lookup.Matches(File.ReadAllText(file)))
            {
                if (!en.ContainsKey(m.Groups[1].Value))
                {
                    missing.Add(m.Groups[1].Value);
                }
            }
        }

        Assert.True(missing.Count == 0, "Add to en.json so it can be translated: " + string.Join(", ", missing));
    }

    [Fact]
    public void Locale_values_replace_english_and_missing_or_empty_ones_fall_back()
    {
        var merged = ThemePackBinder.MergeStrings(
            """{"a": "Gold", "b": "Wood", "c": "Stone"}""",
            """{"a": "Or", "c": "", "z": "extra"}""");
        Assert.Equal("Or", merged["a"]);
        Assert.Equal("Wood", merged["b"]);
        Assert.Equal("Stone", merged["c"]);
        Assert.Equal("Gold", ThemePackBinder.MergeStrings("""{"a": "Gold"}""", null)["a"]);
    }

    [Theory]
    [InlineData("fr", "German", null, "fr")]
    [InlineData("xx", "German", null, "de")]
    [InlineData(null, "Spanish", "MX", "es-419")]
    [InlineData(null, "Spanish", "ES", "es-ES")]
    [InlineData(null, "ChineseTraditional", "TW", "zh-Hant")]
    [InlineData(null, "ChineseSimplified", "CN", "zh-Hans")]
    [InlineData(null, "Portuguese", "BR", "pt-BR")]
    [InlineData(null, "Arabic", "SA", "en")]
    [InlineData("", "Unknown", null, "en")]
    public void Saved_choice_wins_then_device_language_then_english(string? saved, string system, string? region, string expected)
    {
        Assert.Equal(expected, Locales.Resolve(saved, system, region));
    }

    [Fact]
    public void Shipped_locale_strings_load_through_the_pack()
    {
        var root = Path.Combine(KidsFiles.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a");
        string R(params string[] p) => File.ReadAllText(Path.Combine(new[] { root }.Concat(p).ToArray()));
        var pack = ThemePackBinder.Parse(R("pack.json"), R("strings", "en.json"), R("map", "node_pins.json"), R("ui", "hud_layout.json"), R("strings", "de.json"));
        Assert.Equal("Burg", pack.StringOr("hook.plot.base.plot.hq_01", ""));
        Assert.Equal("Sir Aldric", pack.StringOr("hero.sir_aldric.name", ""));
    }
}
