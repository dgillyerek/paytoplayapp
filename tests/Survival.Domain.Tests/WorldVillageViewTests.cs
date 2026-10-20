using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using Survival.Domain.View;
using Survival.Domain.World;

namespace Survival.Domain.Tests;

internal static class Layouts
{
    public static WorldLayout World() =>
        WorldLayout.Parse(File.ReadAllText(Path.Combine(TestPaths.PackDir, "map", "world_layout.json")));

    public static VillageLayout Village() =>
        VillageLayout.Parse(File.ReadAllText(Path.Combine(TestPaths.PackDir, "map", "village_layout.json")));

    public static WorldLayout WorldWithNodes()
    {
        var pack = TestPaths.LoadPack();
        return World().WithLandmarks(pack.Pins.Select(p => (p.NodeId, p.NodeTypeId, p.NodeId, p.PinX, p.PinYFromTop)));
    }

    public static string RepoRoot()
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

    public static (int W, int H, int ColorType) PngHeader(string path)
    {
        var b = File.ReadAllBytes(path);
        Assert.True(b.Length > 26 && b[1] == (byte)'P' && b[2] == (byte)'N' && b[3] == (byte)'G', path + " is not a PNG");
        int Be(int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
        return (Be(16), Be(20), b[25]);
    }
}

public sealed class PlayScreenToggleTests
{
    [Fact]
    public void Starts_in_village_and_button_offers_world()
    {
        var t = new PlayScreenToggle();
        Assert.Equal(PlayScreen.Village, t.Current);
        Assert.Equal(PlayScreen.World, t.Destination);
        Assert.Equal(PlayScreenToggle.LabelToWorld, t.LabelKey);
        Assert.False(t.InTransition);
    }

    [Fact]
    public void Toggle_flips_raises_changed_and_fades_in_over_the_transition()
    {
        var t = new PlayScreenToggle(PlayScreen.Village, 0.4f);
        var seen = new List<(PlayScreen, PlayScreen)>();
        t.Changed += (a, b) => seen.Add((a, b));
        Assert.True(t.Toggle());
        Assert.Equal(PlayScreen.World, t.Current);
        Assert.Equal(PlayScreen.Village, t.Previous);
        Assert.Equal(PlayScreenToggle.LabelToVillage, t.LabelKey);
        Assert.Equal(new[] { (PlayScreen.Village, PlayScreen.World) }, seen);
        Assert.True(t.InTransition);
        Assert.Equal(0f, t.Fade);
        t.Tick(0.2f);
        Assert.Equal(0.5f, t.Progress, 3);
        Assert.Equal(0.5f, t.Fade, 3);
        t.Tick(1f);
        Assert.False(t.InTransition);
        Assert.Equal(1f, t.Fade);
    }

    [Fact]
    public void Taps_during_the_transition_and_same_screen_requests_are_ignored()
    {
        var t = new PlayScreenToggle();
        Assert.False(t.Set(PlayScreen.Village));
        Assert.True(t.Toggle());
        Assert.False(t.Toggle()); // double tap mid-fade
        Assert.Equal(PlayScreen.World, t.Current);
        t.Tick(1f);
        Assert.True(t.Set(PlayScreen.Village));
        Assert.Equal(PlayScreen.Village, t.Current);
    }
}

public sealed class CastleTierTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(15, 15)]
    [InlineData(19, 19)]
    [InlineData(20, 20)]
    [InlineData(0, 1)]
    [InlineData(99, 20)]
    public void Every_castle_level_has_its_own_painted_stage(int level, int stage)
    {
        Assert.Equal(stage, CastleTiers.For(level).Tier);
        Assert.Equal(CastleTiers.ArtPrefix + stage, CastleTiers.For(level).ArtKey);
    }

    [Fact]
    public void Twenty_stages_grow_gradually_and_every_level_up_changes_the_art()
    {
        var tiers = CastleTiers.Default;
        Assert.Equal(20, tiers.Count);
        Assert.Equal(CastleTiers.Stages, tiers.Count);
        Assert.Equal(Enumerable.Range(1, 20), tiers.Select(t => t.MinLevel));
        Assert.Equal(CastleTiers.FirstScale, tiers[0].Scale, 3);
        Assert.Equal(CastleTiers.LastScale, tiers[^1].Scale, 3);
        var step = (CastleTiers.LastScale - CastleTiers.FirstScale) / 19f;
        for (var i = 1; i < tiers.Count; i++)
        {
            Assert.True(tiers[i].Scale > tiers[i - 1].Scale);
            Assert.InRange(tiers[i].Scale - tiers[i - 1].Scale, step - 0.002f, step + 0.002f); // even, gradual growth
        }

        for (var lv = 1; lv < 20; lv++)
        {
            Assert.True(CastleTiers.TierChanged(lv, lv + 1), "lv" + lv);
            Assert.Equal(lv + 1, CastleTiers.NextTierLevel(lv));
        }

        Assert.False(CastleTiers.TierChanged(20, 20));
        Assert.Null(CastleTiers.NextTierLevel(20));
    }

    [Fact]
    public void Village_layout_stages_match_the_defaults_and_cover_every_castle_level()
    {
        var layout = Layouts.Village();
        Assert.Equal(CastleTiers.Default, layout.CastleTiers);
        var castle = Hook.Catalog().Plots.Single(p => p.Id == SurvIds.BasePlotHq01);
        Assert.Equal(20, castle.Levels.Max(l => l.Level));
        Assert.Equal(20, Enumerable.Range(1, 20).Select(l => CastleTiers.For(l, layout.CastleTiers).ArtKey).Distinct().Count());
    }

    [Fact]
    public void Bad_tier_tables_are_rejected()
    {
        Assert.Throws<FormatException>(() => CastleTiers.Validate(new[] { new CastleTier(1, 2, "a", 1f) }));
        Assert.Throws<FormatException>(() => CastleTiers.Validate(new[] { new CastleTier(1, 1, "a", 1f), new CastleTier(2, 3, "b", 0.9f) }));
    }
}

public sealed class LevelBadgeTests
{
    [Fact]
    public void Farms_mills_and_forge_show_a_numbered_badge_and_keep_their_art()
    {
        var hook = Hook.Session(new ManualClock()).Hook!;
        foreach (var id in new[] { SurvIds.BasePlotFarm01, SurvIds.BasePlotFarm02, SurvIds.BasePlotLumber01, SurvIds.BasePlotLumber02, SurvIds.BasePlotForge01 })
        {
            var plot = hook.Base.Find(id)!;
            var v = BuildingVisual.For(plot);
            Assert.True(v.ShowBadge, id);
            Assert.Equal("1", v.BadgeText);
            Assert.Equal(HookArtKeys.ForPlot(id), v.ArtKey);
            Assert.Equal(1f, v.Scale);
            Assert.Equal(v.ArtKey, BuildingVisual.For(plot.Def, 5).ArtKey);
            Assert.Equal("5", BuildingVisual.For(plot.Def, 5).BadgeText);
        }
    }

    [Fact]
    public void Castle_has_no_badge_and_grows_with_level_instead()
    {
        var hook = Hook.Session(new ManualClock()).Hook!;
        var castle = hook.Base.Find(SurvIds.BasePlotHq01)!;
        var v1 = BuildingVisual.For(castle);
        Assert.False(v1.ShowBadge);
        Assert.Equal(1, v1.Tier);
        var v2 = BuildingVisual.For(castle.Def, 2);
        Assert.Equal(2, v2.Tier);
        Assert.True(v2.Scale > v1.Scale);
        Assert.NotEqual(v1.ArtKey, v2.ArtKey);
        var v20 = BuildingVisual.For(castle.Def, 20);
        Assert.Equal(20, v20.Tier);
        Assert.False(v20.ShowBadge);
    }

    [Fact]
    public void Badge_text_tracks_the_upgrade()
    {
        var clock = new ManualClock();
        var hook = Hook.Session(clock).Hook!;
        hook.Wallet.Add(SurvIds.ResSoft, 50000);
        hook.Wallet.Add(SurvIds.ResStone, 50000);
        hook.Wallet.Add(SurvIds.ResWood, 50000);
        Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
        clock.Advance(600);
        Assert.Equal(2, hook.FinishFree(SurvIds.BasePlotHq01));
        Assert.Equal(UpgradeBlock.None, hook.Base.CheckUpgrade(SurvIds.BasePlotForge01));
        Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotForge01));
        clock.Advance(1200);
        Assert.Equal(2, hook.FinishFree(SurvIds.BasePlotForge01));
        Assert.Equal("2", BuildingVisual.For(hook.Base.Find(SurvIds.BasePlotForge01)!).BadgeText);
    }
}

public sealed class VillageEconomyTests
{
    [Fact]
    public void Village_has_castle_two_farms_two_mills_and_a_forge()
    {
        var c = Hook.Catalog();
        Assert.Equal(SurvIds.BuildingWorkshop, c.Plots.Single(p => p.Id == SurvIds.BasePlotForge01).BuildingId);
        Assert.Null(c.Plots.Single(p => p.Id == SurvIds.BasePlotForge01).Produces);
        Assert.Equal(SurvIds.ResFood, c.Plots.Single(p => p.Id == SurvIds.BasePlotFarm02).Produces);
        Assert.Equal(SurvIds.ResWood, c.Plots.Single(p => p.Id == SurvIds.BasePlotLumber02).Produces);
        foreach (var plot in c.Plots)
        {
            Assert.Equal(Enumerable.Range(1, plot.Levels.Count), plot.Levels.Select(l => l.Level));
            foreach (var level in plot.Levels.Take(plot.Levels.Count - 1))
            {
                Assert.True(level.UpgradeSeconds > 0, plot.Id + " lv" + level.Level);
                Assert.NotEmpty(level.UpgradeCost);
            }
        }
    }

    [Fact]
    public void Every_plot_has_a_village_spot_and_new_buildings_wait_for_the_castle()
    {
        var layout = Layouts.Village();
        foreach (var id in SurvIds.BasePlots)
        {
            Assert.NotNull(layout.SpotFor(id));
        }

        Assert.NotEmpty(layout.EmptyPlots);
        Assert.All(layout.EmptyPlots, e => Assert.True(e.UnlockCastleLevel > 1));
        var hook = Hook.Session(new ManualClock()).Hook!;
        Assert.Equal(UpgradeBlock.NeedsCastle, hook.Base.CheckUpgrade(SurvIds.BasePlotForge01));
        Assert.Equal(UpgradeBlock.NeedsCastle, hook.Base.CheckUpgrade(SurvIds.BasePlotFarm02));
    }

    [Fact]
    public void Village_has_grass_and_plots_but_no_roads()
    {
        var json = File.ReadAllText(Path.Combine(TestPaths.PackDir, "map", "village_layout.json"));
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("roads", out _), "village_layout.json must not define roads");
        Assert.False(doc.RootElement.TryGetProperty("paths", out _), "village_layout.json must not define paths");
        Assert.DoesNotContain(typeof(VillageLayout).GetProperties(), p => p.Name is "Roads" or "Paths");
        var layout = Layouts.Village();
        Assert.Equal(6, layout.Plots.Count);
        Assert.NotEmpty(layout.EmptyPlots);
        Assert.DoesNotContain(layout.Props, p => p.Type.Contains("road") || p.Type.Contains("path"));
        Assert.True(TestPaths.LoadPack().TryArt(WorldArtKeys.VillageLawn, out _));
    }

    [Fact]
    public void Castle_can_climb_through_every_stage_with_resources()
    {
        var clock = new ManualClock();
        var hook = Hook.Session(clock).Hook!;
        foreach (var res in new[] { SurvIds.ResSoft, SurvIds.ResStone, SurvIds.ResWood })
        {
            hook.Wallet.Add(res, 50_000_000);
        }

        var tiers = new HashSet<int>();
        while (hook.Base.CheckUpgrade(SurvIds.BasePlotHq01) == UpgradeBlock.None)
        {
            Assert.True(hook.Base.TryStartUpgrade(SurvIds.BasePlotHq01));
            clock.Advance(400_000);
            Assert.True(hook.FinishFree(SurvIds.BasePlotHq01) > 0);
            tiers.Add(BuildingVisual.For(hook.Base.Find(SurvIds.BasePlotHq01)!).Tier);
        }

        Assert.Equal(20, hook.Base.HqLevel);
        Assert.Equal(UpgradeBlock.MaxLevel, hook.Base.CheckUpgrade(SurvIds.BasePlotHq01));
        Assert.Equal(Enumerable.Range(2, 19), tiers.Order());
    }

    [Fact]
    public void Castle_costs_and_timers_rise_every_level_while_other_buildings_keep_their_caps()
    {
        var c = Hook.Catalog();
        var hq = c.Plots.Single(p => p.Id == SurvIds.BasePlotHq01).Levels;
        Assert.Equal(20, hq.Count);
        for (var i = 1; i < hq.Count - 1; i++)
        {
            Assert.True(hq[i].UpgradeSeconds > hq[i - 1].UpgradeSeconds, "timer lv" + hq[i].Level);
            foreach (var (res, amount) in hq[i].UpgradeCost)
            {
                Assert.True(amount > hq[i - 1].UpgradeCost[res], res + " lv" + hq[i].Level);
            }
        }

        foreach (var id in new[] { SurvIds.BasePlotFarm01, SurvIds.BasePlotFarm02, SurvIds.BasePlotLumber01, SurvIds.BasePlotLumber02, SurvIds.BasePlotForge01 })
        {
            Assert.Equal(6, c.Plots.Single(p => p.Id == id).Levels.Count);
        }
    }
}

public sealed class MapCameraTests
{
    [Fact]
    public void Zoom_never_drops_below_cover_so_the_map_always_fills_the_phone()
    {
        var cam = new MapCamera(3240, 4320, 1080, 1920, 0.1f, 1.4f, 0.8f);
        Assert.Equal(1920f / 4320f, cam.CoverZoom, 4);
        Assert.Equal(cam.CoverZoom, cam.MinZoom, 4);
        cam.ZoomTo(0.01f);
        Assert.Equal(cam.MinZoom, cam.Zoom, 4);
        Assert.True(cam.VisibleWidth <= cam.WorldWidth + 0.01f && cam.VisibleHeight <= cam.WorldHeight + 0.01f);
        cam.ZoomTo(50f);
        Assert.Equal(1.4f, cam.Zoom, 4);
    }

    [Fact]
    public void Pan_is_clamped_to_the_map_edges()
    {
        var cam = new MapCamera(3240, 4320, 1080, 1920, 0.45f, 1.4f, 1f);
        cam.PanScreen(100000f, 100000f); // drag content far right/up -> camera to the bottom-left corner
        Assert.Equal(540f, cam.CenterX, 3);
        Assert.Equal(960f, cam.CenterY, 3);
        Assert.Equal(0f, cam.Left, 3);
        Assert.Equal(0f, cam.Bottom, 3);
        cam.PanScreen(-100000f, -100000f);
        Assert.Equal(3240f, cam.Right, 3);
        Assert.Equal(4320f, cam.Top, 3);
    }

    [Fact]
    public void Drag_moves_content_with_the_finger_and_respects_zoom()
    {
        var cam = new MapCamera(3240, 4320, 1080, 1920, 0.45f, 1.4f, 0.5f);
        var x = cam.CenterX;
        cam.PanScreen(100f, 0f);
        Assert.Equal(x - 200f, cam.CenterX, 3); // 100 screen units at zoom 0.5 = 200 map px
        Assert.Equal(-cam.CenterX * cam.Zoom, cam.ContentX, 3);
    }

    [Fact]
    public void Pinch_zoom_keeps_the_point_under_the_fingers_fixed()
    {
        var cam = new MapCamera(3240, 4320, 1080, 1920, 0.45f, 1.4f, 0.6f);
        var (wx, wy) = cam.ScreenToWorld(200f, -300f);
        cam.ZoomBy(1.5f, 200f, -300f);
        Assert.Equal(0.9f, cam.Zoom, 4);
        var (sx, sy) = cam.WorldToScreen(wx, wy);
        Assert.Equal(200f, sx, 2);
        Assert.Equal(-300f, sy, 2);
    }

    [Fact]
    public void Map_smaller_than_the_view_is_zoomed_to_cover_and_centered_on_the_filled_axis()
    {
        var cam = new MapCamera(500, 500, 1080, 1920, 0.5f, 1f, 1f);
        Assert.Equal(1920f / 500f, cam.Zoom, 3); // forced up to cover, even past max_zoom
        cam.PanScreen(3000f, 300f);
        Assert.Equal(250f, cam.CenterY, 3); // height exactly fills the view: locked to center
        Assert.Equal(cam.VisibleWidth * 0.5f, cam.CenterX, 3); // width still scrolls, clamped at the edge
        Assert.Equal(0f, cam.Left, 3);
    }

    [Fact]
    public void Keyboard_and_shift_drag_controls_pan_and_zoom()
    {
        var cam = new MapCamera(3240, 4320, 1080, 1920, 0.45f, 1.4f, 1f);
        var y = cam.CenterY;
        cam.KeyStep(0f, 1f, 0f, 0.1f); // W
        Assert.Equal(y + MapCamera.KeyPanPerSecond * 0.1f, cam.CenterY, 2);
        var z = cam.Zoom;
        cam.KeyStep(0f, 0f, -1f, 0.25f); // Q zooms out
        Assert.True(cam.Zoom < z);
        z = cam.Zoom;
        cam.ShiftDragZoom(MapCamera.ShiftDragPerDoubling); // drag up one doubling
        Assert.Equal(Math.Min(1.4f, z * 2f), cam.Zoom, 3);
    }

    [Fact]
    public void Viewport_resize_reclamps()
    {
        var cam = new MapCamera(2160, 2880, 1080, 1920, 0.67f, 1.6f, 0.7f);
        cam.SetViewport(1920, 1080); // landscape laptop window
        Assert.True(cam.Zoom >= cam.CoverZoom - 1e-4f);
        Assert.True(cam.Left >= -0.01f && cam.Right <= 2160.01f);
    }

    [Fact]
    public void Shipped_layout_zoom_ranges_are_sane_for_a_portrait_phone()
    {
        var world = Layouts.World();
        var village = Layouts.Village();
        var wc = new MapCamera(world.Width, world.Height, 1080, 1920, world.MinZoom, world.MaxZoom, world.StartZoom);
        var vc = new MapCamera(village.Width, village.Height, 1080, 1920, village.MinZoom, village.MaxZoom, village.StartZoom);
        Assert.Equal(world.StartZoom, wc.Zoom, 3);
        Assert.Equal(village.StartZoom, vc.Zoom, 3);
        Assert.True(world.Width > 1080 * 2 && world.Height > 1920 * 2, "world should scroll a few screens");
        Assert.True(wc.MaxZoom / wc.MinZoom >= 2.5f);
        Assert.True(vc.MaxZoom / vc.MinZoom >= 2f);
        Assert.True(vc.MinZoom > wc.MinZoom, "village is the zoomed-in view");
    }
}

public sealed class WorldSimTests
{
    [Fact]
    public void Shipped_world_has_many_villages_camps_resources_and_all_six_nodes()
    {
        var layout = Layouts.WorldWithNodes();
        Assert.NotNull(layout.Player);
        Assert.True(layout.Entities.Count(e => e.Kind is WorldEntityKind.RivalVillage or WorldEntityKind.AllyVillage) >= 8);
        Assert.Equal(WorldArtKeys.CampTypes.OrderBy(x => x), layout.Entities.Where(e => e.Kind == WorldEntityKind.EnemyCamp).Select(e => e.Type).Distinct().OrderBy(x => x));
        Assert.True(layout.Entities.Count(e => e.Kind == WorldEntityKind.EnemyCamp) >= 6);
        Assert.True(layout.Entities.Select(e => e.Level).Where(l => l > 0).Distinct().Count() >= 6, "camps of different levels");
        Assert.Equal(WorldArtKeys.ResourceTypes.OrderBy(x => x), layout.Entities.Where(e => e.Kind == WorldEntityKind.ResourceNode).Select(e => e.Type).Distinct().OrderBy(x => x));
        Assert.Equal(6, layout.Entities.Count(e => e.Kind == WorldEntityKind.Landmark));
        foreach (var e in layout.Entities)
        {
            Assert.InRange(e.X, 0f, layout.Width);
            Assert.InRange(e.Y, 0f, layout.Height);
        }

        foreach (var lm in layout.Entities.Where(e => e.Kind == WorldEntityKind.Landmark))
        {
            Assert.InRange(lm.X, layout.HeartX, layout.HeartX + layout.HeartWidth);
            Assert.InRange(lm.Y, layout.HeartY, layout.HeartY + layout.HeartHeight);
        }
    }

    [Fact]
    public void Mock_columns_are_mid_march_at_load_and_move_in_real_time()
    {
        var sim = new WorldSim(Layouts.WorldWithNodes(), 1000);
        var routes = Layouts.World().Routes.Count;
        Assert.Equal(routes, sim.Marches.Count);
        Assert.Contains(sim.Marches, m => !m.IsWaiting(1000) && m.Progress(1000) > 0f && m.Progress(1000) < 1f);
        var m0 = sim.Marches.First(m => !m.IsWaiting(1000) && m.Progress(1000) < 0.9f);
        var p0 = m0.Position(1000);
        sim.Tick(1001);
        var p1 = m0.Position(1001);
        Assert.NotEqual(p0, p1);
    }

    [Fact]
    public void Mock_columns_loop_out_and_back_forever()
    {
        var sim = new WorldSim(Layouts.WorldWithNodes(), 0);
        var count = sim.Marches.Count;
        for (var t = 1; t <= 600; t++)
        {
            sim.Tick(t);
            Assert.Equal(count, sim.Marches.Count);
        }

        Assert.Contains(sim.Marches, m => m.Leg == MarchLeg.Return);
        sim.Tick(600 + 3600); // a big debug skip resolves without piling up
        Assert.Equal(count, sim.Marches.Count);
    }

    [Fact]
    public void Player_march_goes_out_arrives_returns_and_frees_the_slot()
    {
        var sim = new WorldSim(Layouts.WorldWithNodes(), 0);
        var arrived = new List<string>();
        var returned = new List<string>();
        sim.Arrived += m => arrived.Add(m.TargetId);
        sim.Returned += m => returned.Add(m.TargetId);
        Assert.Equal(MarchSendResult.Ok, sim.SendPlayerMarch("camp.goblin_01", "attack", out var march));
        Assert.NotNull(march);
        Assert.Equal(sim.Player.X, march!.FromX);
        var target = sim.TryGet("camp.goblin_01", out var camp) ? camp : null;
        Assert.Equal(target!.X, march.ToX);
        var seconds = sim.MarchSeconds(sim.Player, target);
        Assert.InRange(seconds, sim.Layout.MinMarchSeconds, sim.Layout.MaxMarchSeconds);
        Assert.Equal(MarchSendResult.AlreadyMarching, sim.CanSend("camp.goblin_01"));

        sim.Tick(seconds * 0.5);
        var (hx, hy) = march.Position(seconds * 0.5);
        Assert.Equal((march.FromX + march.ToX) * 0.5f, hx, 1);
        Assert.Equal((march.FromY + march.ToY) * 0.5f, hy, 1);
        Assert.Empty(arrived);

        sim.Tick(seconds + 0.01);
        Assert.Equal(new[] { "camp.goblin_01" }, arrived);
        var back = sim.PlayerMarchTo("camp.goblin_01");
        Assert.NotNull(back);
        Assert.Equal(MarchLeg.Return, back!.Leg);
        Assert.Equal(march.Serial, back.Serial);
        Assert.Equal(MarchSendResult.AlreadyMarching, sim.CanSend("camp.goblin_01"));

        sim.Tick(seconds * 2 + 0.02);
        Assert.Equal(new[] { "camp.goblin_01" }, returned);
        Assert.Null(sim.PlayerMarchTo("camp.goblin_01"));
        Assert.Equal(MarchSendResult.Ok, sim.CanSend("camp.goblin_01"));
    }

    [Fact]
    public void March_queue_limits_and_own_village_or_unknown_targets_are_refused()
    {
        var sim = new WorldSim(Layouts.WorldWithNodes(), 0);
        Assert.Equal(MarchSendResult.OwnVillage, sim.SendPlayerMarch(sim.Player.Id, "scout", out _));
        Assert.Equal(MarchSendResult.UnknownTarget, sim.SendPlayerMarch("nope", "scout", out _));
        Assert.Equal(MarchSendResult.Ok, sim.SendPlayerMarch("camp.goblin_01", "attack", out _));
        Assert.Equal(MarchSendResult.Ok, sim.SendPlayerMarch("res.gold_01", "gather", out _));
        Assert.Equal(MarchSendResult.Ok, sim.SendPlayerMarch("village.rival_01", "scout", out _));
        Assert.Equal(MarchSendResult.QueueFull, sim.SendPlayerMarch("res.wood_01", "gather", out _));
        Assert.Equal(3, sim.PlayerMarchCount);
    }

    [Fact]
    public void Catalog_node_marches_can_use_the_loop_time_and_reach_the_landmark()
    {
        var sim = new WorldSim(Layouts.WorldWithNodes(), 50);
        Assert.Equal(MarchSendResult.Ok, sim.SendPlayerMarch(SurvIds.WorldNodeGather01, "node", out var m, seconds: 12));
        Assert.Equal(12, m!.Duration, 3);
        var got = new List<WorldMarch>();
        sim.Arrived += got.Add;
        sim.Tick(62);
        Assert.Single(got);
        Assert.Equal("node", got[0].Tag);
    }

    [Fact]
    public void Same_layout_and_clock_give_the_same_map()
    {
        var a = new WorldSim(Layouts.WorldWithNodes(), 123);
        var b = new WorldSim(Layouts.WorldWithNodes(), 123);
        a.Tick(200);
        b.Tick(200);
        Assert.Equal(a.Marches.Select(m => m.Position(200)), b.Marches.Select(m => m.Position(200)));
    }

    [Fact]
    public void Layout_rejects_missing_player_and_duplicate_ids()
    {
        const string head = "{\"world\":{\"width\":100,\"height\":100,\"min_zoom\":0.5,\"max_zoom\":2},\"heart\":{\"x\":0.5,\"y\":0.5,\"width\":10,\"height\":10},";
        Assert.Throws<FormatException>(() => WorldLayout.Parse(head + "\"villages\":[{\"id\":\"a\",\"name\":\"A\",\"kind\":\"rival\",\"x\":0.1,\"y\":0.1}]}"));
        Assert.Throws<FormatException>(() => WorldLayout.Parse(head + "\"villages\":[{\"id\":\"a\",\"name\":\"A\",\"kind\":\"player\",\"x\":0.1,\"y\":0.1},{\"id\":\"a\",\"name\":\"B\",\"x\":0.2,\"y\":0.2}]}"));
        Assert.Throws<FormatException>(() => WorldLayout.Parse(head + "\"villages\":[{\"id\":\"a\",\"name\":\"A\",\"kind\":\"player\",\"x\":1.5,\"y\":0.1}]}"));
    }
}

public sealed class WorldVillageArtTests
{
    [Fact]
    public void Every_world_and_village_key_is_bound_to_a_transparent_png()
    {
        var pack = TestPaths.LoadPack();
        var root = Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a");
        var keys = WorldArtKeys.Required.Concat(SurvIds.BasePlots.Select(HookArtKeys.ForPlot)).Concat(Layouts.Village().CastleTiers.Select(t => t.ArtKey));
        foreach (var key in keys.Distinct())
        {
            Assert.True(pack.TryArt(key, out var rel), key + " missing from pack.json art");
            var path = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), key + " -> " + rel + " not on disk");
            var (w, h, colorType) = Layouts.PngHeader(path);
            Assert.True(w >= 64 && h >= 64, key + " too small");
            Assert.True(colorType == 6, key + " must be RGBA");
            Assert.True(File.Exists(path + ".meta"), rel + " needs a Unity .meta");
        }
    }

    [Fact]
    public void Castle_stages_are_twenty_distinct_paintings()
    {
        var pack = TestPaths.LoadPack();
        var root = Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a");
        var paths = CastleTiers.Default.Select(t => pack.TryArt(t.ArtKey, out var rel) ? rel : "").ToList();
        Assert.Equal(20, paths.Distinct().Count());
        Assert.DoesNotContain("", paths);
        var hashes = paths.Select(rel => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(root, rel))))).ToList();
        Assert.Equal(20, hashes.Distinct().Count()); // different pixels, not just different file names
    }

    [Fact]
    public void World_and_village_strings_and_hud_slots_exist()
    {
        var pack = TestPaths.LoadPack();
        foreach (var key in new[] { PlayScreenToggle.LabelToWorld, PlayScreenToggle.LabelToVillage, "view.banner.world", "view.banner.village", "hook.plot." + SurvIds.BasePlotForge01 })
        {
            Assert.True(pack.Strings.ContainsKey(key), key);
        }

        foreach (var t in WorldArtKeys.CampTypes)
        {
            Assert.True(pack.Strings.ContainsKey("world.camp." + t), t);
        }

        foreach (var slot in new[] { "view_toggle", "map_card", "quest_tracker", "gem_shop", "nav" })
        {
            Assert.True(pack.Hud.ContainsKey(slot), slot);
        }
    }
}
