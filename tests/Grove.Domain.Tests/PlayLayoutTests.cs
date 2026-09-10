using Grove.Domain.Board;
using Grove.Domain.Layout;
using Grove.Domain.Merge;
using Grove.Domain.Orders;

namespace Grove.Domain.Tests;

public sealed class PlayLayoutTests
{
    [Fact]
    public void Occluding_hud_stays_outside_the_board_safe_rect_with_16dp_gap()
    {
        Assert.Equal(HudDockKind.Bottom, PlayLayout.DockKind);
        foreach (var rect in PlayLayout.OccludingHud())
        {
            Assert.False(rect.OverlapsPlayfield(), $"{rect.XMin},{rect.YMin},{rect.XMax},{rect.YMax}");
            Assert.True(PlayLayout.MeetsMinHudGap(rect), $"16dp gap fail {rect.XMin},{rect.YMin},{rect.XMax},{rect.YMax}");
        }

        Assert.False(PlayLayout.Maya.Overlaps(PlayLayout.OrderTray));
        Assert.False(PlayLayout.Crate.Overlaps(PlayLayout.Store));
        Assert.True(PlayLayout.OrderTray.YMax <= PlayLayout.BoardSafeRect.YMin);
        Assert.True(PlayLayout.EnergyLabel.YMin >= PlayLayout.BoardSafeRect.YMax);
        Assert.False(PlayLayout.EnergyLabel.Overlaps(PlayLayout.OrderTray));
        Assert.True(PlayLayout.EnergyLabel.YMin > PlayLayout.OrderTray.YMax);
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
    public void Fitted_camera_keeps_the_7x5_inside_the_board_safe_rect(float width, float height)
    {
        PlayLayout.BoardWorldBounds(out var minX, out var minY, out var maxX, out var maxY);
        var aspect = width / height;
        var frame = PlayLayout.FitBoard(minX, minY, maxX, maxY, aspect);
        Assert.True(frame.OrthographicSize >= PlayLayout.MinOrthographicSize);
        Assert.True(PlayLayout.BoardFitsPlayfield(minX, minY, maxX, maxY, frame, aspect));
    }

    [Fact]
    public void Production_copy_is_dev020_t1_t3()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.Equal(PresentationCopy.DefaultCoachCrate, catalog.Copy.CoachCrate);
        Assert.Equal("Tap the crate to grow supplies.", catalog.Copy.CoachCrate);
        Assert.Equal("Drag two matches together.", catalog.Copy.CoachMerge);
        Assert.Equal("Deliver to Maya.", catalog.Copy.CoachDeliver);
        Assert.Equal(3, catalog.Copy.OrderSlotsMax);
    }
}

public sealed class ThinTeachTests
{
    [Fact]
    public void T1_crate_then_T2_when_two_matching_then_T3_when_order_1_ready()
    {
        var catalog = CatalogLoader.LoadDefault();
        var save = new MemoryTeachSave();
        var teach = new ThinTeach(catalog.Copy, save);
        var board = new BoardGrid();
        var orders = new OrderBoard(catalog.Orders, 3);
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT1, 1));

        teach.StartAfterSplash(board, orders);
        Assert.Equal(TeachBeat.Crate, teach.Beat);
        Assert.False(teach.CanSkip(board, orders));
        Assert.Equal("Tap the crate to grow supplies.", teach.Caption);

        board.Place(new GridPos(1, 0), new PieceStack(GroveCatalog.WildflowerT1, 1));
        teach.NotifyCrateTapped(board, orders);
        Assert.Equal(TeachBeat.Merge, teach.Beat);
        Assert.True(teach.HighlightA.HasValue);
        Assert.True(teach.HighlightB.HasValue);
        Assert.Equal("Drag two matches together.", teach.Caption);
        Assert.False(teach.CanSkip(board, orders));

        teach.NotifyMatchesCombined(board, orders);
        Assert.Equal(TeachBeat.Hidden, teach.Beat);
        Assert.False(teach.OverlayVisible);

        board.Place(new GridPos(2, 0), new PieceStack(GroveCatalog.WildflowerT2, 1));
        teach.Sync(board, orders);
        Assert.Equal(TeachBeat.Deliver, teach.Beat);
        Assert.Equal("Deliver to Maya.", teach.Caption);

        orders.TryDeliver(board);
        teach.NotifyOrderDelivered(orders);
        Assert.True(teach.IsComplete);
        Assert.True(save.IsOrder1TeachDone);
    }

    [Fact]
    public void Skip_after_T1_only_if_player_already_acted()
    {
        var catalog = CatalogLoader.LoadDefault();
        var teach = new ThinTeach(catalog.Copy, new MemoryTeachSave());
        var board = new BoardGrid();
        var orders = new OrderBoard(catalog.Orders, 3);
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT1, 1));
        board.Place(new GridPos(1, 0), new PieceStack(GroveCatalog.WildflowerT1, 1));
        teach.StartAfterSplash(board, orders);
        teach.NotifyCrateTapped(board, orders);
        Assert.Equal(TeachBeat.Merge, teach.Beat);
        Assert.False(teach.TrySkip(board, orders));

        teach.NotifyMatchesCombined(board, orders);
        Assert.True(teach.Beat == TeachBeat.Hidden || teach.Beat == TeachBeat.Deliver);
    }

    [Fact]
    public void Saved_teach_or_completed_order_1_does_not_run_again()
    {
        var catalog = CatalogLoader.LoadDefault();
        var save = new MemoryTeachSave { IsOrder1TeachDone = true };
        var teach = new ThinTeach(catalog.Copy, save);
        var board = new BoardGrid();
        var orders = new OrderBoard(catalog.Orders, 3);
        teach.StartAfterSplash(board, orders);
        Assert.True(teach.IsComplete);
        Assert.False(teach.OverlayVisible);

        var fresh = new ThinTeach(catalog.Copy, new MemoryTeachSave());
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT2, 1));
        orders.TryDeliver(board);
        fresh.StartAfterSplash(board, orders);
        Assert.True(fresh.IsComplete);
    }

    [Fact]
    public void After_crate_skips_T2_if_order_1_already_ready()
    {
        var catalog = CatalogLoader.LoadDefault();
        var teach = new ThinTeach(catalog.Copy, new MemoryTeachSave());
        var board = new BoardGrid();
        var orders = new OrderBoard(catalog.Orders, 3);
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT2, 1));
        teach.StartAfterSplash(board, orders);
        teach.NotifyCrateTapped(board, orders);
        Assert.Equal(TeachBeat.Deliver, teach.Beat);
    }
}
