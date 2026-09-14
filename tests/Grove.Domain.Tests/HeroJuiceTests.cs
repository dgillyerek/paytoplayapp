using System;
using System.IO;
using Grove.Domain.Board;
using Grove.Domain.Energy;
using Grove.Domain.Juice;
using Grove.Domain.Merge;
using Grove.Domain.Producer;

namespace Grove.Domain.Tests;

public sealed class HeroJuiceTests
{
    [Fact]
    public void Style_bible_windows_are_premium_casual_not_instant()
    {
        Assert.True(HeroJuice.InCrateTapWindow(HeroJuice.CrateTapSeconds));
        Assert.True(HeroJuice.InCrateTapWindow(HeroJuice.CrateSpitSeconds));
        Assert.True(HeroJuice.InCrateTapWindow(HeroJuice.ChargePipTickSeconds));
        Assert.True(HeroJuice.InMergePopWindow(HeroJuice.MergePopSeconds));
        Assert.True(HeroJuice.InMergePopWindow(HeroJuice.SparkleSeconds));
        Assert.True(HeroJuice.InDeliverWindow(HeroJuice.DeliverSeconds));
        Assert.True(HeroJuice.MayaFlashSeconds <= HeroJuice.DeliverMaxSeconds);
        Assert.True(HeroJuice.TierUpMaxSeconds <= 0.2001f);
        Assert.InRange(HeroJuice.CrateTapSeconds, 0.080f, 0.120f);
        Assert.InRange(HeroJuice.MergePopSeconds, 0.120f, 0.180f);
        Assert.InRange(HeroJuice.DeliverSeconds, 0.200f, 0.350f);
        Assert.True(HeroJuice.CrateTapSeconds > 0.05f, "crate must not be an instant swap");
        Assert.True(HeroJuice.MergePopSeconds > 0.10f, "merge must not be an instant swap");
        Assert.True(HeroJuice.DeliverSeconds > 0.18f, "deliver must not be an instant swap");
    }

    [Fact]
    public void Easing_is_not_default_linear_lerp()
    {
        Assert.True(HeroJuice.EaseOutCubic(0.5f) > 0.70f);
        Assert.NotEqual(0.5f, HeroJuice.EaseOutCubic(0.5f), 2);
        Assert.NotEqual(0.5f, HeroJuice.DeliverFlyT(0.5f), 2);
        Assert.True(HeroJuice.DeliverFlyT(0.5f) > 0.5f);
        Assert.True(HeroJuice.EaseOutBack(0.85f) > 1.02f);
        Assert.InRange(HeroJuice.EaseOutBack(0f), -0.001f, 0.001f);
        Assert.InRange(HeroJuice.EaseOutBack(1f), 0.999f, 1.001f);
        Assert.InRange(HeroJuice.EaseOutCubic(0f), -0.001f, 0.001f);
        Assert.InRange(HeroJuice.EaseOutCubic(1f), 0.999f, 1.001f);
    }

    [Fact]
    public void Crate_squash_is_soft_and_non_uniform()
    {
        HeroJuice.SquashScale(0f, out var sx0, out var sy0);
        HeroJuice.SquashScale(0.45f, out var sx, out var sy);
        HeroJuice.SquashScale(1f, out var sx1, out var sy1);
        Assert.InRange(sx0, 0.99f, 1.01f);
        Assert.InRange(sy0, 0.99f, 1.01f);
        Assert.True(sx > 1.05f);
        Assert.True(sy < 0.95f);
        Assert.True(sx > sy);
        Assert.InRange(sx1, 0.99f, 1.01f);
        Assert.InRange(sy1, 0.99f, 1.01f);
        Assert.InRange(HeroJuice.PipTickScale(0.5f), 1.15f, 1.40f);
        Assert.InRange(HeroJuice.PipTickScale(0f), 0.99f, 1.01f);
        Assert.InRange(HeroJuice.PipTickScale(1f), 0.99f, 1.01f);
    }

    [Fact]
    public void Spit_arc_is_not_a_straight_line()
    {
        HeroJuice.ArcPoint(0.5f, 0f, 0f, 4f, 0f, 2f, out var x, out var y);
        Assert.InRange(x, 1.2f, 3.2f);
        Assert.True(y > 0.35f, "arc must loft above the chord");
        HeroJuice.ArcPoint(0f, 0f, 0f, 4f, 0f, 2f, out var x0, out var y0);
        HeroJuice.ArcPoint(1f, 0f, 0f, 4f, 0f, 2f, out var x1, out var y1);
        Assert.InRange(x0, -0.02f, 0.02f);
        Assert.InRange(y0, -0.02f, 0.02f);
        Assert.InRange(x1, 3.98f, 4.02f);
        Assert.InRange(y1, -0.02f, 0.02f);
        Assert.True(HeroJuice.FlyerScale(0f) < 0.80f);
        Assert.True(HeroJuice.FlyerScale(1f) >= 0.98f);
    }

    [Fact]
    public void Merge_pop_overshoots_and_stays_readable()
    {
        var start = HeroJuice.MergePopScale(0f);
        var mid = HeroJuice.MergePopScale(0.48f);
        var end = HeroJuice.MergePopScale(1f);
        Assert.InRange(start, 0.68f, 0.85f);
        Assert.True(mid > 1.10f, "scale overshoot required");
        Assert.InRange(end, 0.98f, 1.02f);
        Assert.True(start > 0.65f, "result stays readable — never scale from 0");
        Assert.Equal(6, HeroJuice.SparkleQuadCount);
        HeroJuice.SparkleQuad(0f, out var s0, out var a0);
        HeroJuice.SparkleQuad(1f, out var s1, out var a1);
        Assert.True(s0 < s1);
        Assert.True(a0 > 0.7f);
        Assert.True(a1 < 0.05f);
        Assert.True(HeroJuice.SparkleR > HeroJuice.SparkleG);
        Assert.True(HeroJuice.SparkleG > HeroJuice.SparkleB);
        Assert.True(HeroJuice.SparkleG > 0.75f);
    }

    [Fact]
    public void Deliver_card_settle_and_reward_fly_are_not_linear()
    {
        var start = HeroJuice.CardSettleScale(0f);
        var mid = HeroJuice.CardSettleScale(0.45f);
        var end = HeroJuice.CardSettleScale(1f);
        Assert.True(start < 0.98f);
        Assert.True(mid > 1.02f);
        Assert.InRange(end, 0.99f, 1.01f);
        Assert.True(HeroJuice.CardSettleOffsetY(0.4f, 20f) > 4f);
        Assert.InRange(HeroJuice.CardSettleOffsetY(0f, 20f), -0.05f, 0.05f);
        Assert.InRange(HeroJuice.CardSettleOffsetY(1f, 20f), -0.05f, 0.05f);
        Assert.True(HeroJuice.DeliverFlyT(0.4f) > 0.4f + 0.15f);
    }

    [Fact]
    public void Crate_tap_service_reports_spit_cell_for_arc()
    {
        var catalog = CatalogLoader.LoadDefault();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var energy = new EnergyWallet(catalog.Energy, clock);
        var crate = new GardenCrate(catalog.GardenCrate!, ScriptedRandom.Always(0));
        var board = new BoardGrid();
        board.Place(new GridPos(0, 0), new PieceStack(GroveCatalog.WildflowerT1, 1));
        var tap = new CrateTapService(crate, board, energy, clock);

        var spit = tap.TryTap();
        var ok = Assert.IsType<SpitResult.Ok>(spit);
        Assert.Equal(GroveCatalog.WildflowerT1, ok.Item);
        Assert.True(ok.At.HasValue);
        Assert.Equal(new GridPos(0, 1), ok.At!.Value);
        Assert.True(board.TryGet(ok.At.Value, out var stack));
        Assert.Equal(ok.Item, stack.Id);
    }

    [Fact]
    public void Play_wires_crate_merge_deliver_juice_not_linear_swap()
    {
        var hud = ReadUnity("PrototypeHud.cs");
        var board = ReadUnity("BoardView.cs");
        var drag = ReadUnity("DragMergeController.cs");
        var fx = ReadUnity("HeroJuiceFx.cs");

        Assert.Contains("CrateTapJuice", hud, StringComparison.Ordinal);
        Assert.Contains("PlaySpitArc", hud, StringComparison.Ordinal);
        Assert.Contains("HeroJuice.CrateTapSeconds", hud, StringComparison.Ordinal);
        Assert.Contains("TickScale", hud, StringComparison.Ordinal);
        Assert.Contains("CardSettle", hud, StringComparison.Ordinal);
        Assert.Contains("RewardFly", hud, StringComparison.Ordinal);
        Assert.Contains("FlashImage", hud, StringComparison.Ordinal);
        Assert.Contains("HeroJuice.DeliverSeconds", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("CrateLabel", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Charges", hud, StringComparison.Ordinal);

        Assert.Contains("PlayMergePop", board, StringComparison.Ordinal);
        Assert.Contains("WorldSparkle", board, StringComparison.Ordinal);
        Assert.Contains("HeroJuice.MergePopSeconds", board, StringComparison.Ordinal);
        Assert.Contains("StubMergeSparkle", board, StringComparison.Ordinal);

        Assert.Contains("PlayMergePop", drag, StringComparison.Ordinal);
        Assert.Contains("HeroJuice.EaseOutCubic", drag, StringComparison.Ordinal);
        Assert.DoesNotContain("var fly = 0.08f", drag, StringComparison.Ordinal);

        Assert.Contains("SquashUi", fx, StringComparison.Ordinal);
        Assert.Contains("WorldArc", fx, StringComparison.Ordinal);
        Assert.Contains("SparkleQuadCount", fx, StringComparison.Ordinal);
        Assert.DoesNotContain("ParticleSystem", fx, StringComparison.Ordinal);
    }

    private static string ReadUnity(string file)
    {
        var art = Grove.Domain.Art.ArtManifest.ResolveDirectory();
        var root = Path.GetFullPath(Path.Combine(art, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(root, "Assets", "Grove", "Unity", file));
    }
}
