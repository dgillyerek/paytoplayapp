using Survival.Domain.Campaign;

namespace Survival.Domain.Tests;

/// <summary>
/// The Heroes/Foes framed portrait cover-fits and clips painted art inside the gold trim, and
/// honours a per-portrait focus so an off-centre face (Nightfang) is not cropped out of the
/// narrower detail box.
/// </summary>
public sealed class PortraitFitTests
{
    /// <summary>
    /// Foes detail pane portrait window after the 6px trim and 3px window inset, at the 1080×1920
    /// reference: panel 0.96×0.92 of the canvas, detail 0.95×0.335 of the panel, portrait
    /// 0.32×0.735 of the detail.
    /// </summary>
    public const float DetailWindowW = 297.1872f;

    public const float DetailWindowH = 416.93184f;

    /// <summary>Roster-card portrait window at the same reference (card 0.30×0.195 of the panel, art 1×0.78 of the card).</summary>
    public const float GridWindowW = 293.04f;

    public const float GridWindowH = 250.66944f;

    [Fact]
    public void Framed_portrait_parents_art_under_a_canvas_mask()
    {
        var src = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets", "Survival", "Unity", "SurvivalCampaignView.cs"));
        var start = src.IndexOf("internal RectTransform Portrait(", StringComparison.Ordinal);
        Assert.True(start >= 0, "Portrait widget is missing");
        var end = src.IndexOf("private sealed class StarRow", start, StringComparison.Ordinal);
        Assert.True(end > start, "could not bound the Portrait widget");
        var body = src.Substring(start, end - start);
        Assert.Contains("new GameObject(\"Window\", typeof(RectTransform))", body, StringComparison.Ordinal);
        Assert.Contains("window.SetParent(trim.transform, false)", body, StringComparison.Ordinal);
        Assert.Contains("AddComponent<RectMask2D>()", body, StringComparison.Ordinal);
        Assert.Contains("AddComponent<Mask>()", body, StringComparison.Ordinal);
        Assert.Contains("showMaskGraphic = false", body, StringComparison.Ordinal);
        var parentAt = body.IndexOf("window.SetParent(trim.transform, false)", StringComparison.Ordinal);
        var rectMaskAt = body.IndexOf("AddComponent<RectMask2D>()", StringComparison.Ordinal);
        var maskAt = body.IndexOf("AddComponent<Mask>()", StringComparison.Ordinal);
        var artAt = body.IndexOf("SurvivalVisuals.Image(window, \"Art\"", StringComparison.Ordinal);
        Assert.True(parentAt >= 0 && parentAt < rectMaskAt && rectMaskAt < maskAt, "mask must be added after the window is parented to the canvas");
        Assert.True(artAt > maskAt, "art must be created as a child of the already-masked window");
        Assert.Contains("AddComponent<PortraitCoverFit>()", body, StringComparison.Ordinal);
        Assert.Contains("PortraitFocus.Of(def.PortraitKey, def.Id, out fit.FocusX, out fit.FocusY)", body, StringComparison.Ordinal);
        Assert.Contains("art.maskable = true", body, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(RectMask2D))", body, StringComparison.Ordinal);
        Assert.DoesNotContain("AspectRatioFitter", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_portraits_focus_on_the_centre()
    {
        PortraitFocus.Of("theme_a.hero.portrait.ironhowl", out var x, out var y);
        Assert.Equal(PortraitFocus.DefaultX, x);
        Assert.Equal(PortraitFocus.DefaultY, y);
        PortraitFocus.Of(null, "hero.lyra", out x, out y);
        Assert.Equal(0.5f, x);
        Assert.Equal(0.5f, y);
    }

    [Fact]
    public void Nightfang_focus_is_the_midpoint_of_his_eyes()
    {
        PortraitFocus.Of(PortraitFocus.NightfangKey, out var x, out var y);
        Assert.Equal(PortraitFocus.NightfangX, x);
        Assert.Equal(PortraitFocus.NightfangY, y);
        PortraitFocus.Of("foe.nightfang", out x, out y);
        Assert.Equal(PortraitFocus.NightfangX, x);
        Assert.Equal(PortraitFocus.NightfangY, y);
        PortraitFocus.Of("nightfang", out x, out y);
        Assert.Equal(0.215f, x);
        Assert.Equal(0.512f, y);
        // Eyes were measured at (65, 262) and (155, 262) on the 512 PNG; mid (110, 262).
        Assert.InRange(x, 110f / 512f - 0.002f, 110f / 512f + 0.002f);
        Assert.InRange(y, 262f / 512f - 0.002f, 262f / 512f + 0.002f);
    }

    [Fact]
    public void Nightfang_detail_crop_starts_at_the_left_edge_so_his_snout_stays_in_the_box()
    {
        PortraitFocus.CoverVisible(DetailWindowW, DetailWindowH, 1f, PortraitFocus.NightfangX, PortraitFocus.NightfangY, out var x, out var y, out var w, out var h);
        Assert.Equal(0f, x, 4);
        Assert.Equal(0f, y, 4);
        Assert.True(h > 0.99f, "the tall detail box should show the full height of a square portrait, got " + h);
        Assert.True(w < 0.85f && w > 0.65f, "the detail box should crop the left/right of a square, got " + w);
        // Eyes at 0.215 sit inside, and as far right as a no-gap cover-fit allows (clamped).
        Assert.InRange(PortraitFocus.NightfangX, x, x + w);
        // A centre crop would have started around (1-w)/2 ≈ 0.14 and cut the snout (~x 0.04–0.10).
        PortraitFocus.CoverVisible(DetailWindowW, DetailWindowH, 1f, 0.5f, 0.5f, out var cx, out _, out _, out _);
        Assert.True(cx > 0.10f, "centre crop should have inset past Nightfang's snout, got " + cx);
        Assert.True(x < cx, "focus crop should shift left of the centre crop");
    }

    [Fact]
    public void Cover_shift_clamps_so_the_art_always_fills_the_window()
    {
        PortraitFocus.CoverSize(DetailWindowW, DetailWindowH, 1f, out var artW, out var artH);
        Assert.Equal(DetailWindowH, artW, 3);
        Assert.Equal(DetailWindowH, artH, 3);
        var maxX = (artW - DetailWindowW) * 0.5f;
        PortraitFocus.CoverShift(DetailWindowW, DetailWindowH, artW, artH, PortraitFocus.NightfangX, PortraitFocus.NightfangY, out var sx, out var sy);
        Assert.Equal(maxX, sx, 3);
        Assert.Equal(0f, sy, 3);
        PortraitFocus.CoverShift(DetailWindowW, DetailWindowH, artW, artH, 0.5f, 0.5f, out sx, out sy);
        Assert.Equal(0f, sx, 3);
        Assert.Equal(0f, sy, 3);
        // A focus past the edge still fills: shift equals the clamp, never more than the overflow.
        PortraitFocus.CoverShift(DetailWindowW, DetailWindowH, artW, artH, 0f, 0f, out sx, out sy);
        Assert.Equal(maxX, sx, 3);
        Assert.Equal(0f, sy, 3);
    }

    [Fact]
    public void Grid_tile_shows_the_full_width_so_Nightfang_focus_does_not_change_the_horizontal_crop()
    {
        PortraitFocus.CoverVisible(GridWindowW, GridWindowH, 1f, PortraitFocus.NightfangX, PortraitFocus.NightfangY, out var x, out var y, out var w, out var h);
        PortraitFocus.CoverVisible(GridWindowW, GridWindowH, 1f, 0.5f, 0.5f, out var cx, out var cy, out var cw, out var ch);
        Assert.Equal(0f, x, 4);
        Assert.Equal(0f, cx, 4);
        Assert.True(w > 0.99f, "the wide grid tile should show the full width, got " + w);
        Assert.Equal(cw, w, 4);
        Assert.Equal(ch, h, 4);
        // Vertical centre vs eyes at 0.512 is a tiny shift, still inside the tile.
        Assert.InRange(PortraitFocus.NightfangY, y, y + h);
        Assert.InRange(0.5f, cy, cy + ch);
    }
}
