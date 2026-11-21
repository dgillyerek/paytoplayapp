using System;
using System.IO;
using Survival.Domain.Runner;
using Survival.Domain.View;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-20 (Pixel 7 Pro, 1440 x 3120): heroes, enemies and buttons keep their size on any phone; only the
/// visible world grows. The game is authored on the 1080 x 1920 (9:16) Game view.
/// </summary>
public sealed class AspectFitTests
{
    private const float Ref = 1080f / 1920f;
    private const float Pixel7Pro = 1440f / 3120f; // 19.5 : 9

    public static TheoryData<float> TallPhones => new() { 9f / 18f, 9f / 19.5f, 9f / 20f, Pixel7Pro, 9f / 21f };

    public static TheoryData<float> WideScreens => new() { Ref, 3f / 4f, 1f, 16f / 9f };

    [Theory]
    [InlineData(52f)]
    [InlineData(54f)]
    public void The_9_16_Game_view_is_framed_exactly_as_authored(float fov)
    {
        var f = AspectFit.Fit(fov, Ref, 1f);
        Assert.True(f.Symmetric);
        Assert.Equal(fov, f.VerticalDegrees, 4);
        Assert.Equal(fov, AspectFit.VerticalFovDegrees(fov, Ref), 4);
        Assert.Equal(1f, AspectFit.CanvasMatch(Ref));
        Assert.Equal(1f, AspectFit.FrameShare(1080f, 1920f));
        Assert.Equal(RunnerFraming.BottomGroundFromFocus(true), RunnerFraming.BottomGroundFromFocus(true, Ref), 5);
        Assert.Equal(RunnerFraming.BottomGroundFromFocus(true, Ref), RunnerFraming.BottomGroundFromFocus(true, 16f / 9f), 5);
    }

    [Theory]
    [MemberData(nameof(TallPhones))]
    public void A_taller_phone_keeps_the_9_16_width_of_view_so_units_keep_their_size(float aspect)
    {
        var authored = AspectFit.Fit(52f, Ref);
        var phone = AspectFit.Fit(52f, aspect, 1f);
        // Units' screen width is their angular width over the view's width: the same view width, the same size.
        Assert.Equal(authored.HorizontalDegrees, phone.HorizontalDegrees, 3);
        Assert.True(phone.VerticalDegrees > 52f, "taller screen shows more world: " + phone.VerticalDegrees);
        // Pixels per tan-unit across and down are equal (no squash): height span / width span == 1 / aspect.
        Assert.Equal(1d / aspect, (phone.Top - phone.Bottom) / (phone.Right - phone.Left), 4);
    }

    [Fact]
    public void The_Pixel_7_Pro_numbers()
    {
        // Today: 52 deg vertical on every screen -> 30.7 deg across on 9:16 but only 25.4 deg on 19.5:9, so heroes
        // and enemies were 21.9 % wider on the phone (and the HUD, height-matched, the same).
        Assert.Equal(30.68, AspectFit.HorizontalFovDegrees(52d, Ref), 2);
        Assert.Equal(25.37, AspectFit.HorizontalFovDegrees(52d, Pixel7Pro), 2);
        var phone = AspectFit.Fit(52f, Pixel7Pro, RunnerFraming.ExtraViewBelow);
        Assert.Equal(30.68, phone.HorizontalDegrees, 2);
        Assert.Equal(61.0, phone.VerticalDegrees, 0);
        // Runner: the top edge (title row, horizon) does not move; the extra is under the frame.
        Assert.Equal(Math.Tan(26d * Math.PI / 180d), phone.Top, 5);
        Assert.Equal(0.8207f, AspectFit.FrameShare(1440f, 3120f), 3);
        Assert.Equal(0f, AspectFit.CanvasMatch(Pixel7Pro));
    }

    [Theory]
    [MemberData(nameof(WideScreens))]
    public void The_9_16_view_and_anything_wider_keep_the_authored_vertical_view_and_height_match(float aspect)
    {
        var f = AspectFit.Fit(54f, aspect, 1f);
        Assert.True(f.Symmetric);
        Assert.Equal(54f, f.VerticalDegrees, 4);
        Assert.Equal(1f, AspectFit.CanvasMatch(aspect));
        Assert.Equal(1f, AspectFit.FrameShare(aspect * 1000f, 1000f));
        var (y0, y1) = AspectFit.PinY(0.937f, 0.985f, AspectFit.FrameShare(aspect * 1000f, 1000f));
        Assert.Equal(0.937f, y0);
        Assert.Equal(0.985f, y1);
    }

    [Fact]
    public void Taller_than_9_21_holds_the_9_21_view_instead_of_showing_past_the_frame()
    {
        var cap = AspectFit.Fit(52f, AspectFit.MinAspect, 1f);
        var taller = AspectFit.Fit(52f, 9f / 23f, 1f);
        Assert.Equal(cap.VerticalDegrees, taller.VerticalDegrees, 4);
        Assert.True(taller.HorizontalDegrees < cap.HorizontalDegrees);
        Assert.Equal(cap.Top, taller.Top, 6);
        Assert.Equal(cap.Bottom, taller.Bottom, 6);
    }

    [Fact]
    public void Centred_fit_splits_the_extra_height_evenly()
    {
        var f = AspectFit.Fit(54f, Pixel7Pro, 0.5f);
        Assert.True(f.Symmetric);
        Assert.Equal(f.VerticalDegrees, AspectFit.VerticalFovDegrees(54f, Pixel7Pro), 3);
    }

    [Fact]
    public void Runner_on_a_tall_phone_shows_more_road_under_the_squad_and_never_less()
    {
        foreach (var boss in new[] { false, true })
        {
            var authored = RunnerFraming.BottomGroundFromFocus(boss);
            var phone = RunnerFraming.BottomGroundFromFocus(boss, Pixel7Pro);
            var cap = RunnerFraming.BottomGroundFromFocus(boss, AspectFit.MinAspect);
            var past = RunnerFraming.BottomGroundFromFocus(boss, 9f / 24f);
            Assert.True(phone < authored, "more road behind on 19.5:9");
            Assert.InRange(authored - phone, 1.5f, 2.5f); // about two formation rows of road
            Assert.Equal(cap, past, 3); // clamped at 9:21
            // The bottom edge stays ahead of the camera, so the road (and grass) always fills it.
            Assert.True(cap > -(RunnerFraming.CameraBack + 1.4f));
        }
    }

    [Theory]
    [InlineData(0.937f, 0.985f)] // top row: hangs from the top
    [InlineData(0.10f, 0.15f)] // drag hint: stands on the bottom
    [InlineData(0.40f, 0.50f)] // start hint: stays centred
    [InlineData(0.56f, 0.625f)] // banner: stays centred
    public void Hud_boxes_keep_their_9_16_shape_on_a_taller_safe_area(float y0, float y1)
    {
        var share = AspectFit.FrameShare(1440f, 3120f);
        var (a, b) = AspectFit.PinY(y0, y1, share);
        Assert.Equal((y1 - y0) * share, b - a, 5);
        var c = (y0 + y1) * 0.5f;
        if (c >= 0.6f)
        {
            Assert.Equal(1f - ((1f - y1) * share), b, 5);
        }
        else if (c <= 0.4f)
        {
            Assert.Equal(y0 * share, a, 5);
        }
        else
        {
            Assert.Equal(0.5f + ((c - 0.5f) * share), (a + b) * 0.5f, 4);
        }
    }

    [Fact]
    public void A_sheet_taller_than_half_the_screen_grows_into_the_extra_height()
    {
        var (a, b) = AspectFit.PinY(0.1f, 0.9f, 0.8f);
        Assert.Equal(0.08f, a, 5);
        Assert.Equal(0.92f, b, 5);
        var (s0, s1) = AspectFit.PinY(0f, 1f, 0.8f);
        Assert.Equal(0f, s0);
        Assert.Equal(1f, s1);
    }

    [Fact]
    public void Safe_area_insets_map_into_the_screen()
    {
        // Pixel 7 Pro punch-hole: about 145 px of 3120 at the top.
        var top = 1f - (145f / 3120f);
        Assert.Equal(top, AspectFit.ToScreen(1f, 0f, top), 6);
        Assert.Equal(0f, AspectFit.ToScreen(0f, 0f, top), 6);
    }

    [Fact]
    public void Every_canvas_and_camera_uses_the_fit()
    {
        var unity = Path.Combine(Layouts.RepoRoot(), "Assets/Survival/Unity");
        var visuals = File.ReadAllText(Path.Combine(unity, "SurvivalVisuals.cs"));
        Assert.Contains("go.AddComponent<HudAspectFit>();", visuals);
        var runner = File.ReadAllText(Path.Combine(unity, "Runner/RunnerLevelView.cs"));
        Assert.Contains("CameraAspectFit.Attach(_cam, RunnerFraming.FieldOfViewDegrees, RunnerFraming.ExtraViewBelow);", runner);
        foreach (var file in new[] { "LockedBattleCamera.cs", "PortraitStage.cs" })
        {
            var src = File.ReadAllText(Path.Combine(unity, file));
            Assert.DoesNotContain("cam.aspect = PortraitGameView.Width / PortraitGameView.Height", src);
            Assert.Contains("AspectFit.VerticalFovDegrees(PortraitGameView.FovDegrees, cam.aspect)", src);
        }
    }
}
