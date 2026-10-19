using Survival.Domain.Ids;
using Survival.Domain.Theme;

namespace Survival.Domain.Tests;

/// <summary>Theme A hook UI kit: every art key the hook view asks for is bound in pack.json and ships as a PNG.</summary>
public sealed class HookArtKitTests
{
    private static readonly string[] Resources =
    {
        SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood, SurvIds.ResFood, SurvIds.ResHard, SurvIds.ResSpeedupM
    };

    private static readonly string[] Plots = { SurvIds.BasePlotHq01, "base.plot.farm_01", "base.plot.lumber_01" };

    [Fact]
    public void Pack_binds_every_hook_kit_key_to_an_existing_png()
    {
        var pack = TestPaths.LoadPack();
        var packRoot = Path.Combine(RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a");
        var keys = HookArtKeys.Required
            .Concat(Resources.Select(HookArtKeys.ForResource))
            .Concat(Plots.Select(HookArtKeys.ForPlot))
            .Append(SurvIds.ThemeAStoreIcon)
            .Append(SurvIds.ThemeABuildingHqKeep);
        foreach (var key in keys)
        {
            Assert.True(pack.TryArt(key, out var rel), key + " missing from pack.json art");
            var path = Path.Combine(packRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), key + " -> " + rel + " not on disk");
            var (w, h, colorType) = PngHeader(path);
            Assert.True(w >= 32 && h >= 16, key + " too small");
            Assert.True(colorType == 6 || colorType == 4, key + " must be an RGBA (transparent) PNG");
        }
    }

    [Fact]
    public void Store_icon_and_castle_no_longer_point_at_the_full_screen_splash()
    {
        var pack = TestPaths.LoadPack();
        Assert.True(pack.TryArt(SurvIds.ThemeAStoreIcon, out var store));
        Assert.DoesNotContain("SPLASH", store, StringComparison.OrdinalIgnoreCase);
        Assert.True(pack.TryArt(SurvIds.ThemeABuildingHqKeep, out var keep));
        Assert.DoesNotContain("SPLASH", keep, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Art_slices_are_bound_keys_with_borders_inside_the_image()
    {
        var pack = TestPaths.LoadPack();
        var packRoot = Path.Combine(RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a");
        Assert.True(pack.TrySlice(HookArtKeys.Panel, out _));
        Assert.True(pack.TrySlice(HookArtKeys.ButtonGold, out _));
        foreach (var pair in pack.ArtSlices)
        {
            Assert.True(pack.TryArt(pair.Key, out var rel), pair.Key + " sliced but not bound");
            var (w, h, _) = PngHeader(Path.Combine(packRoot, rel.Replace('/', Path.DirectorySeparatorChar)));
            var s = pair.Value;
            Assert.True(s.Left + s.Right < w, pair.Key + " horizontal borders overlap");
            Assert.True(s.Bottom + s.Top < h, pair.Key + " vertical borders overlap");
        }
    }

    [Fact]
    public void Bad_art_slice_is_rejected()
    {
        var bad = "{\"theme_id\":\"t\",\"systems_api\":\"" + SurvIds.SystemsApi + "\",\"art\":{},\"art_slices\":{\"k\":[1,2,3]}}";
        Assert.Throws<FormatException>(() => ThemePackBinder.Parse(bad, "{}", "{\"pins\":[]}", "{}"));
    }

    [Fact]
    public void Plot_and_resource_keys_follow_the_white_label_convention()
    {
        Assert.Equal("theme_a.hook.plot.farm_01", HookArtKeys.ForPlot("base.plot.farm_01"));
        Assert.Equal("theme_a.hook.icon.res.hard", HookArtKeys.ForResource(SurvIds.ResHard));
        Assert.Equal(HookArtKeys.GemsTier1, HookArtKeys.ForGemTier(0));
        Assert.Equal(HookArtKeys.GemsTier3, HookArtKeys.ForGemTier(5));
    }

    private static (int Width, int Height, int ColorType) PngHeader(string path)
    {
        var bytes = new byte[29];
        using (var fs = File.OpenRead(path))
        {
            Assert.Equal(29, fs.Read(bytes, 0, 29));
        }

        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        int Be(int o) => (bytes[o] << 24) | (bytes[o + 1] << 16) | (bytes[o + 2] << 8) | bytes[o + 3];
        return (Be(16), Be(20), bytes[25]);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
