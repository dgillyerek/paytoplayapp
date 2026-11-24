namespace Survival.Domain.Tests;

/// <summary>
/// The Heroes/Foes framed portrait must clip painted art inside the gold trim. Nightfang's
/// ears sit on the top edge of a square 512 PNG, so a mask that never registered would let
/// his head paint over the box on the Foes detail pane.
/// </summary>
public sealed class PortraitFitTests
{
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
        Assert.Contains("AspectRatioFitter.AspectMode.EnvelopeParent", body, StringComparison.Ordinal);
        Assert.Contains("art.maskable = true", body, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(RectMask2D))", body, StringComparison.Ordinal);
    }
}
