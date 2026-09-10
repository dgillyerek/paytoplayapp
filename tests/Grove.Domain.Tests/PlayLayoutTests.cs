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
    public void Design_v1_bands_keep_orders_in_the_dock_and_crate_off_cells()
    {
        Assert.Equal(0.92f, PlayLayout.FromDesignTop(0.08f), 3);
        Assert.Equal(0.90f, PlayLayout.FromDesignTop(0.10f), 3);
        Assert.Equal(0.34f, PlayLayout.FromDesignTop(0.66f), 3);
        Assert.Equal(0.00f, PlayLayout.FromDesignTop(1f), 3);

        Assert.True(PlayLayout.EnergyBand.Contains(PlayLayout.EnergyLabel));
        Assert.True(PlayLayout.EnergyBand.Contains(PlayLayout.CoinLabel));
        Assert.True(PlayLayout.TopBarBand.Contains(PlayLayout.Maya));
        Assert.True(PlayLayout.EnergyLabel.YMin > PlayLayout.Goal.YMax);
        Assert.True(PlayLayout.Goal.YMin > PlayLayout.BoardSafeRect.YMax);
        Assert.InRange((PlayLayout.Goal.XMin + PlayLayout.Goal.XMax) * 0.5f, 0.48f, 0.52f);
        Assert.True(PlayLayout.Maya.XMin > PlayLayout.Goal.XMax);
        Assert.True(PlayLayout.Maya.XMin > PlayLayout.EnergyLabel.XMax);
        Assert.False(PlayLayout.Goal.Overlaps(PlayLayout.EnergyPill));
        Assert.False(PlayLayout.Goal.Overlaps(PlayLayout.Maya));

        Assert.True(PlayLayout.BoardBand.Contains(PlayLayout.BoardSafeRect));
        Assert.True(PlayLayout.BoardBand.Contains(PlayLayout.Crate));
        Assert.False(PlayLayout.Crate.Overlaps(PlayLayout.DockBand));
        Assert.False(PlayLayout.Crate.Overlaps(PlayLayout.BoardSafeRect));
        Assert.True(PlayLayout.BoardSafeRect.XMin - PlayLayout.Crate.XMax >= PlayLayout.MinGapNormX - 0.0001f);

        Assert.True(PlayLayout.DockBand.Contains(PlayLayout.OrderTray));
        Assert.True(PlayLayout.DockBand.Contains(PlayLayout.InventoryBar));
        Assert.True(PlayLayout.DockBand.Contains(PlayLayout.Store));
        Assert.True(PlayLayout.DockBand.Contains(PlayLayout.DockPlate));
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.BoardBand));
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.BoardSafeRect));
        Assert.True(PlayLayout.DockBand.YMax <= PlayLayout.BoardBand.YMin);
        Assert.True(PlayLayout.BoardBand.YMin - PlayLayout.DockBand.YMax >= PlayLayout.MinGapNormY - 0.0001f);
        Assert.Equal(5, PlayLayout.InventorySlotCount);
        Assert.Equal(0.10f, PlayLayout.DesignBoardTop);
        Assert.Equal(0.66f, PlayLayout.DesignBoardBottom);
        Assert.Equal(0.70f, PlayLayout.DesignDockTop);
    }

    [Fact]
    public void Dock_plate_is_a_thin_inventory_strip_and_cards_do_not_overlap()
    {
        var plateH = PlayLayout.DockPlate.YMax - PlayLayout.DockPlate.YMin;
        Assert.True(plateH <= PlayLayout.MaxDockPlateNormHeight, $"dock plate height {plateH}");
        Assert.True(PlayLayout.DockPlate.YMax < PlayLayout.OrderTray.YMin);
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.DockPlate));
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.InventoryBar));
        Assert.False(PlayLayout.Store.Overlaps(PlayLayout.OrderTray));
        Assert.False(PlayLayout.Store.Overlaps(PlayLayout.DockPlate));
        Assert.True(PlayLayout.DockPlate.Contains(PlayLayout.InventoryBar));
        Assert.True(PlayLayout.DockBand.Contains(PlayLayout.TeachSkip));

        for (var i = 0; i < 3; i++)
        {
            var a = PlayLayout.OrderCardOnScreen(i, 3);
            Assert.True(PlayLayout.OrderTray.Contains(a), $"card {i} outside tray");
            Assert.False(a.OverlapsPlayfield());
            Assert.False(a.Overlaps(PlayLayout.DockPlate));
            for (var j = i + 1; j < 3; j++)
            {
                var b = PlayLayout.OrderCardOnScreen(j, 3);
                Assert.False(a.Overlaps(b), $"card {i} overlaps card {j}");
            }
        }

        var local = PlayLayout.RelativeTo(PlayLayout.DockBand, PlayLayout.OrderTray);
        Assert.InRange(local.XMin, 0f, 1f);
        Assert.InRange(local.YMin, 0f, 1f);
        Assert.InRange(local.XMax, 0f, 1f);
        Assert.InRange(local.YMax, 0f, 1f);
    }

    [Fact]
    public void Order_card_copy_stays_compact_so_text_cannot_cover_the_board()
    {
        var catalog = CatalogLoader.LoadDefault();
        var order1 = catalog.Orders[0];
        var compact = OrderCardCopy.Compact(order1, active: true, new[] { 0 });
        Assert.Equal("Order 1\n0/1", compact);
        Assert.Equal("Order 1", OrderCardCopy.Compact(order1, active: false));
        Assert.True(compact.Split('\n').Length <= 3);
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
        Assert.Equal(ThinTeach.RingLabelCrate, teach.RingLabel);
        Assert.Equal("UI_Teach_Ring", ThinTeach.RingStub);
        Assert.Equal("UI_Teach_Hand", ThinTeach.HandStub);
        Assert.Equal("ftue_play_teach_done", ThinTeach.PersistKey);

        board.Place(new GridPos(1, 0), new PieceStack(GroveCatalog.WildflowerT1, 1));
        teach.NotifyCrateTapped(board, orders);
        Assert.Equal(TeachBeat.Merge, teach.Beat);
        Assert.True(teach.HighlightA.HasValue);
        Assert.True(teach.HighlightB.HasValue);
        Assert.Equal("Drag two matches together.", teach.Caption);
        Assert.Equal(ThinTeach.RingLabelMerge, teach.RingLabel);
        Assert.False(teach.CanSkip(board, orders));

        teach.NotifyMatchesCombined(board, orders);
        Assert.Equal(TeachBeat.Hidden, teach.Beat);
        Assert.False(teach.OverlayVisible);

        board.Place(new GridPos(2, 0), new PieceStack(GroveCatalog.WildflowerT2, 1));
        teach.Sync(board, orders);
        Assert.Equal(TeachBeat.Deliver, teach.Beat);
        Assert.Equal("Deliver to Maya.", teach.Caption);
        Assert.Equal(ThinTeach.RingLabelDeliver, teach.RingLabel);

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
