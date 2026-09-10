using Grove.Domain.Layout;
using Grove.Domain.Merge;
using Grove.Domain.Orders;

namespace Grove.Domain.Tests;

public sealed class PlayLayoutTests
{
    [Fact]
    public void Occluding_hud_stays_outside_the_playfield_band()
    {
        foreach (var rect in PlayLayout.OccludingHud())
        {
            Assert.False(rect.OverlapsPlayfield(), $"{rect.XMin},{rect.YMin},{rect.XMax},{rect.YMax}");
        }

        Assert.False(PlayLayout.Maya.Overlaps(PlayLayout.OrderTray));
        Assert.False(PlayLayout.Crate.Overlaps(PlayLayout.Store));
        Assert.True(PlayLayout.OrderTray.YMin >= PlayLayout.Playfield.YMax);
        Assert.True(PlayLayout.Crate.YMax <= PlayLayout.Playfield.YMin);
        Assert.True(PlayLayout.CrateLabel.YMax <= PlayLayout.Playfield.YMin);
    }

    [Fact]
    public void Legacy_vertical_tray_would_cover_the_playfield()
    {
        var legacyTray = new NormRect(0.20f, 0.62f, 0.99f, 0.84f);
        Assert.True(legacyTray.OverlapsPlayfield());
        Assert.False(PlayLayout.OrderTray.OverlapsPlayfield());
    }

    [Theory]
    [InlineData(1080f, 1920f)]
    [InlineData(1080f, 2340f)]
    [InlineData(1920f, 1080f)]
    public void Fitted_camera_keeps_the_7x5_inside_the_playfield(float width, float height)
    {
        PlayLayout.BoardWorldBounds(out var minX, out var minY, out var maxX, out var maxY);
        var aspect = width / height;
        var frame = PlayLayout.FitBoard(minX, minY, maxX, maxY, aspect);
        Assert.True(frame.OrthographicSize >= PlayLayout.MinOrthographicSize);
        Assert.True(PlayLayout.BoardFitsPlayfield(minX, minY, maxX, maxY, frame, aspect));
    }

    [Fact]
    public void Coach_marks_are_crate_then_merge_then_deliver()
    {
        var coach = new FirstRunCoach(PresentationCopy.Default);
        Assert.False(coach.TryAdvance(CoachAdvance.Skip));
        coach.Start();
        Assert.Equal(CoachAdvance.Crate, coach.Current!.AdvanceOn);
        Assert.Equal(PresentationCopy.DefaultCoachCrate, coach.Current.Caption);
        Assert.Equal("ENV_FG_GardenCrate_Charged", coach.Current.ArtStub);

        Assert.False(coach.TryAdvance(CoachAdvance.Merge));
        Assert.True(coach.TryAdvance(CoachAdvance.Crate));
        Assert.Equal(CoachAdvance.Merge, coach.Current!.AdvanceOn);
        Assert.Equal("VFX_MergeSparkle", coach.Current.ArtStub);

        Assert.True(coach.TryAdvance(CoachAdvance.Skip));
        Assert.Equal(CoachAdvance.Deliver, coach.Current!.AdvanceOn);
        Assert.Equal("UI_OrderTray_Card", coach.Current.ArtStub);
        Assert.True(coach.TryAdvance(CoachAdvance.Deliver));
        Assert.True(coach.IsComplete);
        Assert.Null(coach.Current);
        Assert.False(coach.TryAdvance(CoachAdvance.Skip));
    }

    [Fact]
    public void Production_copy_includes_minimal_coach_lines()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.Equal(PresentationCopy.DefaultCoachCrate, catalog.Copy.CoachCrate);
        Assert.Equal(PresentationCopy.DefaultCoachMerge, catalog.Copy.CoachMerge);
        Assert.Equal(PresentationCopy.DefaultCoachDeliver, catalog.Copy.CoachDeliver);
        var marks = FirstRunCoach.CreateMarks(catalog.Copy);
        Assert.Equal(3, marks.Count);
        Assert.Contains("crate", marks[0].Caption, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("merge", marks[1].Caption, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Maya", marks[2].Caption, StringComparison.OrdinalIgnoreCase);
    }
}
