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
