using System.Text.Json;
using System.Text.RegularExpressions;

namespace Survival.Domain.Tests;

/// <summary>
/// The committed Unity ProjectSettings ship as Theme A (Kingdom Defense, com.paytoplay.fantasykingdoma) with its icons,
/// and the flavor build code agrees with flavor.json. Project Grove is discontinued; no Grove identity may come back.
/// </summary>
public sealed class ThemeAProjectSettingsTests
{
    private static readonly string Root = FindRepoRoot();
    private static string Settings => File.ReadAllText(Path.Combine(Root, "ProjectSettings", "ProjectSettings.asset"));

    private static JsonElement Flavor =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Assets", "AppFlavor", "fantasy_kingdom_a", "flavor.json"))).RootElement;

    [Fact]
    public void Project_settings_carry_the_theme_a_store_identity()
    {
        var s = Settings;
        Assert.Contains("  companyName: PayToPlay\n", s, StringComparison.Ordinal);
        Assert.Contains("  productName: " + Flavor.GetProperty("flavor.app_name").GetString() + "\n", s, StringComparison.Ordinal);
        var id = Flavor.GetProperty("flavor.bundle_id").GetString();
        Assert.Equal("com.paytoplay.fantasykingdoma", id);
        Assert.Contains("    Android: " + id + "\n", s, StringComparison.Ordinal);
        Assert.Contains("    iPhone: " + id + "\n", s, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_settings_build_il2cpp_arm64_for_api_36_without_a_committed_key()
    {
        var s = Settings;
        Assert.Contains("  AndroidTargetSdkVersion: 36\n", s, StringComparison.Ordinal);
        Assert.Contains("  AndroidTargetArchitectures: 2\n", s, StringComparison.Ordinal);
        Assert.Matches(@"  scriptingBackend:\n(    \w+: \d\n)*    Android: 1\n", s);
        Assert.Contains("  androidUseCustomKeystore: 0\n", s, StringComparison.Ordinal);
        Assert.Matches(@"  AndroidKeystoreName: ?\n", s);
        Assert.Matches(@"  AndroidKeyaliasName: ?\n", s);
        Assert.DoesNotContain("keystorePass", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("keyaliasPass", s, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Flavor_build_code_matches_flavor_json()
    {
        var src = File.ReadAllText(Path.Combine(Root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("ThemeAProductName = \"" + Flavor.GetProperty("flavor.app_name").GetString() + "\"", src, StringComparison.Ordinal);
        Assert.Contains("ThemeABundleId = \"" + Flavor.GetProperty("flavor.bundle_id").GetString() + "\"", src, StringComparison.Ordinal);
        Assert.Contains("ThemeACompanyName = \"PayToPlay\"", src, StringComparison.Ordinal);
        Assert.Contains("PlayerSettings.companyName = ThemeACompanyName", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_android_icon_slot_and_the_default_icon_use_theme_a_store_art()
    {
        var art = Path.Combine(Root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "store");
        string Guid(string file) => Regex.Match(File.ReadAllText(Path.Combine(art, file + ".meta")), @"guid: (\w+)").Groups[1].Value;
        var icon = Guid("flavor_icon.png");
        var round = Guid("flavor_icon_round.png");
        var bg = Guid("flavor_icon_adaptive_background.png");
        var fg = Guid("flavor_icon_adaptive_foreground.png");
        Assert.True(File.Exists(Path.Combine(Root, "Assets", "AppFlavor", "fantasy_kingdom_a", "..", "..", "ThemePack", "fantasy_kingdom_a",
            Flavor.GetProperty("flavor.icon").GetString()!)), "flavor.json icon path does not exist");

        var s = Settings;
        var start = s.IndexOf("  - m_BuildTarget: Android\n    m_Icons:\n", StringComparison.Ordinal);
        Assert.True(start > 0, "no Android platform icons");
        var end = s.IndexOf("  - m_BuildTarget: ", start + 10, StringComparison.Ordinal);
        var android = s[start..end];
        var slots = Regex.Matches(android, @"    - m_Textures:\n((?:      - \{fileID: 2800000, guid: \w+, type: 3\}\n)*)      m_Width: (\d+)\n      m_Height: \d+\n      m_Kind: (\d)\n");
        Assert.Equal(18, slots.Count);
        foreach (Match slot in slots)
        {
            var guids = Regex.Matches(slot.Groups[1].Value, @"guid: (\w+)").Select(m => m.Groups[1].Value).ToArray();
            var expected = slot.Groups[3].Value switch
            {
                "2" => new[] { bg, fg },
                "1" => new[] { round },
                _ => new[] { icon }
            };
            Assert.Equal(expected, guids);
        }

        Assert.Contains("      m_Icon: {fileID: 2800000, guid: " + icon + ", type: 3}\n", s, StringComparison.Ordinal);
    }

    [Fact]
    public void Icon_art_is_square_and_large_enough()
    {
        var art = Path.Combine(Root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "store");
        foreach (var (file, min) in new[] { ("flavor_icon.png", 512), ("flavor_icon_round.png", 512), ("flavor_icon_adaptive_background.png", 432), ("flavor_icon_adaptive_foreground.png", 432) })
        {
            var (w, h) = PngSize(Path.Combine(art, file));
            Assert.Equal(w, h);
            Assert.True(w >= min, file + " is " + w + "px");
        }

        var (pw, ph) = PngSize(Path.Combine(Root, "store-listing", "fantasy_kingdom_a", "play_store_icon_512.png"));
        Assert.Equal((512, 512), (pw, ph));
    }

    [Fact]
    public void No_grove_identity_in_project_settings()
    {
        foreach (var file in new[] { "ProjectSettings.asset", "EditorSettings.asset" })
        {
            Assert.DoesNotContain("grove", File.ReadAllText(Path.Combine(Root, "ProjectSettings", file)), StringComparison.OrdinalIgnoreCase);
        }

        Assert.Empty(Directory.GetFiles(Path.Combine(Root, "Assets", "Settings"), "*Grove*", SearchOption.AllDirectories));
    }

    private static (int, int) PngSize(string path)
    {
        var b = File.ReadAllBytes(path);
        int Be(int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
        return (Be(16), Be(20));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectSettings", "ProjectVersion.txt")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
