using System;
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
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.InventoryBar));
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.Store));
        Assert.True(PlayLayout.OrderTray.YMin - PlayLayout.InventoryBar.YMax >= PlayLayout.MinGapNormY - 0.0001f);
        Assert.True(PlayLayout.OrderTray.YMin - PlayLayout.Store.YMax >= PlayLayout.MinGapNormY - 0.0001f);
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
        Assert.False(PlayLayout.InventoryBar.Overlaps(PlayLayout.OrderTray));
        Assert.False(PlayLayout.Store.Overlaps(PlayLayout.OrderTray));
        Assert.True(PlayLayout.InventoryBar.YMax <= PlayLayout.OrderTray.YMin);
        Assert.True(PlayLayout.DockBand.YMax <= PlayLayout.BoardBand.YMin);
        Assert.True(PlayLayout.BoardBand.YMin - PlayLayout.DockBand.YMax >= PlayLayout.MinGapNormY - 0.0001f);
        Assert.Equal(5, PlayLayout.InventorySlotCount);
        Assert.Equal(0.10f, PlayLayout.DesignBoardTop);
        Assert.Equal(0.66f, PlayLayout.DesignBoardBottom);
        Assert.Equal(0.70f, PlayLayout.DesignDockTop);
        Assert.True(PlayLayout.TeachHudCaption.YMax - PlayLayout.TeachHudCaption.YMin >= 0.05f);
        Assert.True(PlayLayout.TeachHudCaption.YMin >= PlayLayout.DockPlate.YMax - 0.0001f);
    }

    [Fact]
    public void Dock_order_cards_keep_12dp_gutters()
    {
        Assert.True(PlayLayout.MinDockGutterDp >= 12f);
        var trayW = PlayLayout.OrderTray.XMax - PlayLayout.OrderTray.XMin;
        var a = PlayLayout.OrderCardLocal(0, 3);
        var b = PlayLayout.OrderCardLocal(1, 3);
        var c = PlayLayout.OrderCardLocal(2, 3);
        Assert.True(a.XMin * trayW >= PlayLayout.MinDockGutterNorm - 0.0001f);
        Assert.True((1f - c.XMax) * trayW >= PlayLayout.MinDockGutterNorm - 0.0001f);
        Assert.True((b.XMin - a.XMax) * trayW >= PlayLayout.MinDockGutterNorm - 0.0001f);
        Assert.True((c.XMin - b.XMax) * trayW >= PlayLayout.MinDockGutterNorm - 0.0001f);
        Assert.False(PlayLayout.ActiveOrderCard.OverlapsPlayfield());
        Assert.True(PlayLayout.OrderTray.Contains(PlayLayout.ActiveOrderCard));
    }

    [Fact]
    public void Energy_and_charges_readouts_fit_one_line_at_1080()
    {
        var energyW = (PlayLayout.EnergyLabel.XMax - PlayLayout.EnergyLabel.XMin) * PlayLayout.ReferenceWidth;
        var crateW = (PlayLayout.CrateLabel.XMax - PlayLayout.CrateLabel.XMin) * PlayLayout.ReferenceWidth;
        Assert.True(energyW >= PlayLayout.HudNumberMinWidthPx, energyW.ToString("0.0"));
        Assert.True(crateW >= PlayLayout.CrateChargesMinWidthPx, crateW.ToString("0.0"));
        Assert.True(PlayLayout.EnergyLabel.XMin >= PlayLayout.EnergyBar.XMin - 0.0001f);
        Assert.True(PlayLayout.EnergyLabel.XMax <= PlayLayout.EnergyBar.XMax + 0.0001f);
        Assert.True(PlayLayout.EnergyLabel.XMax <= PlayLayout.CoinIcon.XMin + 0.0001f);
        Assert.False(PlayLayout.EnergyLabel.Overlaps(PlayLayout.CoinIcon));
        Assert.True(PlayLayout.EnergyLabel.XMax < 0.50f);
        Assert.False(PlayLayout.CrateLabel.OverlapsPlayfield());
        Assert.True(PlayLayout.MeetsMinHudGap(PlayLayout.CrateLabel));
        Assert.False(PlayLayout.CrateLabel.Overlaps(PlayLayout.OrderTray));
        Assert.Contains(PlayLayout.OccludingHud(), r =>
            Math.Abs(r.XMin - PlayLayout.CrateLabel.XMin) < 0.0001f
            && Math.Abs(r.YMin - PlayLayout.CrateLabel.YMin) < 0.0001f);
    }

    [Fact]
    public void Portrait_type_scale_matches_des005_phone_floors()
    {
        Assert.True(PlayLayout.TypeMinReadable >= 22);
        Assert.InRange(PlayLayout.TypeHud, 32, 36);
        Assert.InRange(PlayLayout.TypeCoin, 32, 36);
        Assert.InRange(PlayLayout.TypeGoal, 28, 32);
        Assert.InRange(PlayLayout.TypeTeach, 28, 32);
        Assert.InRange(PlayLayout.TypeOrderBody, 30, 34);
        Assert.InRange(PlayLayout.TypeOrderActive, 30, 34);
        Assert.InRange(PlayLayout.TypeButton, 30, 34);
        Assert.True(PlayLayout.TypeCrate >= 24);
        Assert.True(PlayLayout.TypeToast >= 24);
        Assert.True(PlayLayout.TypeTeachRing >= PlayLayout.TypeMinReadable);
        foreach (var size in new[]
                 {
                     PlayLayout.TypeOrderBody, PlayLayout.TypeOrderActive, PlayLayout.TypeButton,
                     PlayLayout.TypeHud, PlayLayout.TypeGoal, PlayLayout.TypeTeach, PlayLayout.TypeTeachRing,
                     PlayLayout.TypeCrate, PlayLayout.TypeSplash, PlayLayout.TypeToast, PlayLayout.TypeCoin
                 })
        {
            Assert.True(size >= PlayLayout.TypeMinReadable, size.ToString());
        }
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
