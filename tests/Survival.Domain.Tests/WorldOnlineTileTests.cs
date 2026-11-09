using Survival.Domain.Theme;
using Survival.Domain.World;

namespace Survival.Domain.Tests;

public sealed class WorldOnlineTileTests
{
    [Theory]
    [InlineData("food", 1)]
    [InlineData("wood", 4)]
    [InlineData("stone", 7)]
    [InlineData("camp", 1)]
    [InlineData("camp", 5)]
    [InlineData("camp", 9)]
    [InlineData("camp", 30)]
    public void Every_server_tile_kind_has_painted_art_in_the_pack(string kind, int level)
    {
        var key = WorldArtKeys.OnlineTile(kind, level);
        var pack = File.ReadAllText(Path.Combine(Root(), "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json"));
        Assert.Contains("\"" + key + "\"", pack, StringComparison.Ordinal);
    }

    [Fact]
    public void Online_castles_use_painted_village_art()
    {
        var pack = File.ReadAllText(Path.Combine(Root(), "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json"));
        Assert.Contains("\"" + WorldArtKeys.Village(WorldEntityKind.RivalVillage) + "\"", pack, StringComparison.Ordinal);
        Assert.Contains("\"" + WorldArtKeys.Village(WorldEntityKind.AllyVillage) + "\"", pack, StringComparison.Ordinal);
    }

    [Fact]
    public void Tile_markers_are_pushed_off_buildings_and_name_plates()
    {
        var layout = Layouts.WorldWithNodes();
        var tiles = layout.Entities.Select((e, i) => new WorldEntity("tile_" + i, WorldEntityKind.ResourceNode, "food", "", 1, 0, 0, e.X, e.Y)).ToList();
        var placed = WorldSpacing.Separate(tiles, layout.Entities);
        var bad = WorldSpacing.Overlaps(placed.Concat(layout.Entities).ToList());
        Assert.True(bad.Count == 0, "Overlaps:\n" + string.Join("\n", bad));
    }

    private static string Root()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets")))
        {
            d = d.Parent;
        }

        return d!.FullName;
    }
}
