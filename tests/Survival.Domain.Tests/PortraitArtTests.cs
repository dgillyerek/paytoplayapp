using System.Security.Cryptography;
using Survival.Domain.Campaign;
using Survival.Domain.Runner;
using Survival.Domain.Theme;

namespace Survival.Domain.Tests;

/// <summary>
/// Campaign and runner VS portraits all resolve to a shipped 512×512 RGB PNG. Four of the
/// new-character files are still 3D-render placeholders (flat studio backdrop), not painted
/// portraits like Bonequill and Nightfang. Design replaces those PNGs in place; when a hash
/// below no longer matches, drop that entry.
/// </summary>
public sealed class PortraitArtTests
{
    /// <summary>8-bit truecolor RGB, same as the painted Bonequill and Nightfang portraits.</summary>
    const int PngRgb = 2;

    /// <summary>
    /// 3D-model stills on a flat backdrop, shipped so the VS card is not blank. Replace in place
    /// at Assets/ThemePack/fantasy_kingdom_a/art/heroes/portraits/portrait_{name}.png (512×512,
    /// 8-bit RGB PNG, no alpha). Keep the existing .meta GUID. Style: a painted upper-body
    /// illustration with a scenic background and the same painterly lighting as Bonequill and
    /// Nightfang, not a studio render.
    /// </summary>
    static readonly (string Id, string Sha256)[] PlaceholderRenderPortraits =
    {
        ("foe.malzareth", "0044ce82a86572597239bf36c721387d262a36ab7e888dc71fbe83c89bf38121"),
        ("hero.kaelen", "b9aef27896902a4aa06f18a98b5739bb9a808036fa669870953985704618a00d"),
        ("hero.seraphine", "4f80d15d3c392e34c43713a6d053b8464a2303f80939825922fd13b0a6d0c15d"),
        ("foe.vorgath", "a09d54c792b41af5a32f5f3e9bfd4fbae538042e22ebf09dfe1af7eef24e3c78"),
    };

    [Fact]
    public void Every_roster_unit_resolves_a_512_rgb_portrait_file()
    {
        var pack = TestPaths.LoadPack();
        var root = Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a");
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var hero in Camp.Catalog().Heroes)
        {
            Assert.False(string.IsNullOrWhiteSpace(hero.PortraitKey), hero.Id + " has no portrait key");
            Assert.True(pack.TryArt(hero.PortraitKey, out var rel), hero.Id + " " + hero.PortraitKey + " missing from pack.json art");
            var path = Path.Combine(root, rel);
            Assert.True(File.Exists(path), hero.Id + " → " + rel);
            var (w, h, colorType) = Layouts.PngHeader(path);
            Assert.True(w == 512 && h == 512 && colorType == PngRgb,
                hero.Id + " portrait must be 512×512 8-bit RGB (color type 2), got " + w + "×" + h + " type " + colorType);
            seen.Add(hero.PortraitKey);
        }

        foreach (var unit in Runner.Catalog().Units.Values)
        {
            var key = RunnerArtKeys.Portrait(unit.Roster);
            Assert.True(pack.TryArt(key, out var rel), unit.Roster + " " + key + " missing from pack.json art");
            var path = Path.Combine(root, rel);
            Assert.True(File.Exists(path), unit.Roster + " → " + rel);
            var (w, h, colorType) = Layouts.PngHeader(path);
            Assert.True(w == 512 && h == 512 && colorType == PngRgb,
                unit.Roster + " portrait must be 512×512 8-bit RGB (color type 2), got " + w + "×" + h + " type " + colorType);
            seen.Add(key);
        }

        Assert.True(seen.Count >= 16, "expected the full campaign roster to resolve a portrait, got " + seen.Count);
    }

    [Fact]
    public void Placeholder_render_portraits_are_only_the_known_new_characters()
    {
        var pack = TestPaths.LoadPack();
        var catalog = Camp.Catalog();
        var root = Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a");

        foreach (var (id, sha) in PlaceholderRenderPortraits)
        {
            var hero = catalog.Hero(id);
            Assert.NotNull(hero);
            Assert.True(pack.TryArt(hero!.PortraitKey, out var rel), id);
            var path = Path.Combine(root, rel);
            var got = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            Assert.True(got == sha,
                id + " portrait hash changed. If Design dropped in a painted 512×512 portrait (scenic background, not a flat studio render), remove this entry from PlaceholderRenderPortraits. If the placeholder was revised, update the hash. path=" + rel + " sha256=" + got);
        }
    }
}
