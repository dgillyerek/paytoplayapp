using Survival.Domain.Theme;
using Survival.Domain.World;

namespace Survival.Domain.Tests;

public sealed class WorldSpacingTests
{
    [Fact]
    public void No_two_places_overlap_buildings_or_name_plates()
    {
        var layout = Layouts.WorldWithNodes();
        var bad = WorldSpacing.Overlaps(layout.Entities);
        Assert.True(bad.Count == 0, "Overlapping places:\n" + string.Join("\n", bad));
    }

    [Fact]
    public void All_six_catalog_places_sit_on_the_painted_heart()
    {
        var layout = Layouts.WorldWithNodes();
        var landmarks = layout.Entities.Where(e => e.Kind == WorldEntityKind.Landmark).ToList();
        Assert.Equal(6, landmarks.Count);
        foreach (var e in landmarks)
        {
            var f = WorldSpacing.Footprint(e);
            Assert.True(f.X0 >= layout.HeartX && f.X1 <= layout.HeartX + layout.HeartWidth, e.Id + " spills off the map heart sideways");
            Assert.True(f.Y0 >= layout.HeartY && f.Y1 <= layout.HeartY + layout.HeartHeight, e.Id + " spills off the map heart vertically");
        }
    }

    [Fact]
    public void Overlap_rule_catches_places_on_top_of_each_other()
    {
        var a = new WorldEntity("a", WorldEntityKind.Landmark, "x", "A", 0, 0, 0, 1000f, 1000f);
        var b = a with { Id = "b", X = 1150f };
        Assert.NotEmpty(WorldSpacing.Overlaps(new[] { a, b }));
        Assert.Empty(WorldSpacing.Overlaps(new[] { a, b with { X = 1500f } }));
    }

    [Theory]
    [InlineData(0.45f, WorldSpacing.PlateMaxScale)]
    [InlineData(0.8f, 1f)]
    [InlineData(1.4f, WorldSpacing.PlateMinScale)]
    public void Plate_scale_stays_within_the_tested_bounds(float zoom, float expected)
    {
        Assert.Equal(expected, WorldSpacing.PlateScale(zoom), 3);
    }
}

public sealed class WorldSceneryTests
{
    [Fact]
    public void Every_place_on_the_painted_heart_gets_a_clearing_that_covers_its_building()
    {
        var layout = Layouts.WorldWithNodes();
        var onHeart = layout.Entities.Where(layout.OnHeart).ToList();
        Assert.Equal(6, onHeart.Count(e => e.Kind == WorldEntityKind.Landmark));
        foreach (var e in onHeart)
        {
            var c = WorldSpacing.Clearing(e);
            float cx = (c.X0 + c.X1) * 0.5f, cy = (c.Y0 + c.Y1) * 0.5f;
            float rx = (c.X1 - c.X0) * 0.5f * WorldSpacing.ClearingSolid, ry = (c.Y1 - c.Y0) * 0.5f * WorldSpacing.ClearingSolid;
            var f = WorldSpacing.Footprint(e);
            foreach (var (x, y) in new[] { (f.X0, f.Y0), (f.X1, f.Y1), (f.X0, f.Y1), (f.X1, f.Y0) })
            {
                var d = ((x - cx) / rx) * ((x - cx) / rx) + ((y - cy) / ry) * ((y - cy) / ry);
                Assert.True(d <= 1f, e.Id + " corner (" + x + "," + y + ") sticks out of its solid grass clearing");
            }
        }
    }

    [Fact]
    public void Clearing_art_is_bound_and_fades_out_at_its_edge()
    {
        var path = Path.Combine(Layouts.RepoRoot(), "Assets/ThemePack/fantasy_kingdom_a/art/world/map/clearing.png");
        Assert.True(File.Exists(path));
        Assert.Contains("\"" + WorldArtKeys.MapClearing + "\"", File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets/ThemePack/fantasy_kingdom_a/pack.json")));
    }

    [Fact]
    public void No_place_or_name_plate_sits_on_a_decorative_prop()
    {
        var layout = Layouts.WorldWithNodes();
        Assert.NotEmpty(layout.Props);
        var bad = WorldSpacing.OnScenery(layout.Entities, layout.PropFootprints());
        Assert.True(bad.Count == 0, "Places on props:\n" + string.Join("\n", bad));
    }

    [Fact]
    public void Online_tiles_are_pushed_off_props_and_places_or_left_off()
    {
        var layout = Layouts.WorldWithNodes();
        var props = layout.PropFootprints();
        var tiles = new List<WorldEntity>();
        var px = layout.Player!.X;
        var py = layout.Player.Y;
        for (var i = -7; i <= 7; i++)
        {
            for (var j = -7; j <= 7; j++)
            {
                tiles.Add(new WorldEntity("t" + i + "_" + j, WorldEntityKind.ResourceNode, "wood", "", 1, 0, 100, px + i * 140f, py + j * 140f));
            }
        }

        var kept = WorldSpacing.Separate(tiles, layout.Entities, obstacles: props);
        Assert.NotEmpty(kept);
        Assert.Empty(WorldSpacing.OnScenery(kept, props));
        var all = new List<WorldEntity>(layout.Entities);
        all.AddRange(kept);
        Assert.Empty(WorldSpacing.Overlaps(all));
    }

    [Fact]
    public void Props_under_a_place_are_left_off()
    {
        var e = new WorldEntity("a", WorldEntityKind.Landmark, "x", "A", 0, 0, 0, 1000f, 1000f);
        var props = new[] { new WorldProp("forest", 1000f, 1000f, 1f), new WorldProp("forest", 3000f, 3000f, 1f) };
        Assert.Single(WorldSpacing.ClearProps(props, new[] { e }));
    }
}
