using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Survival.Domain.View;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-20 (Pixel 7 Pro): the round gem Shop button hid the world map's Reports button. No two HUD buttons
/// may overlap on any portrait phone from 9:16 to 9:20, with or without a notch / gesture-bar safe area, after
/// <see cref="AspectFit"/> places each 9:16 slot on the screen (what HudAspectFit does at run time).
/// </summary>
public sealed class HudSlotOverlapTests
{
    /// <summary>Layers that hold other slots, not buttons: the top bar strip, the nav strip, the map, the map card.</summary>
    private static readonly HashSet<string> Containers = new() { "top_bar", "nav", "map_area", "map_card" };

    /// <summary>The gems chip's "+" sits on the chip's right end by design (one control, two tap targets that both open the shop).</summary>
    private static readonly (string, string)[] Attached = { ("res_hard", "gem_plus") };

    private readonly record struct Slot(string Name, double X0, double Y0, double X1, double Y1);

    private static List<Slot> Slots()
    {
        var path = Path.Combine(Layouts.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "ui", "hud_layout.json");
        var hud = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var list = new List<Slot>();
        foreach (var s in hud.EnumerateObject())
        {
            if (s.Value.ValueKind != JsonValueKind.Object || Containers.Contains(s.Name))
            {
                continue;
            }

            double V(string k) => s.Value.GetProperty(k).GetDouble();
            // y_top is measured down from the top; anchors are up from the bottom.
            list.Add(new Slot(s.Name, V("x_min"), 1d - V("y_top_max"), V("x_max"), 1d - V("y_top_min")));
        }

        return list;
    }

    public static TheoryData<float, float, float> Screens => new()
    {
        // width, height, safe top inset px (0 = no cutout; 145 ~ Pixel 7 Pro punch-hole status strip), bottom 0
        { 1080f, 1920f, 0f }, // 9:16 PC Game view
        { 1080f, 2160f, 0f }, // 9:18
        { 1440f, 3120f, 0f }, // 9:19.5 Pixel 7 Pro, no inset
        { 1440f, 3120f, 145f }, // Pixel 7 Pro with the punch-hole inset
        { 1080f, 2400f, 120f }, // 9:20
    };

    [Theory]
    [MemberData(nameof(Screens))]
    public void No_two_hud_buttons_overlap_on_any_portrait_phone(float width, float height, float topInset)
    {
        var safeH = height - topInset;
        var share = AspectFit.FrameShare(width, safeH);
        var safeTop = 1f - (topInset / height);
        var placed = Slots().Select(s =>
        {
            var (y0, y1) = AspectFit.PinY((float)s.Y0, (float)s.Y1, share);
            return new Slot(
                s.Name,
                s.X0 * width,
                AspectFit.ToScreen(y0, 0f, safeTop) * height,
                s.X1 * width,
                AspectFit.ToScreen(y1, 0f, safeTop) * height);
        }).ToList();
        var clashes = new List<string>();
        for (var i = 0; i < placed.Count; i++)
        {
            for (var j = i + 1; j < placed.Count; j++)
            {
                var a = placed[i];
                var b = placed[j];
                if (Attached.Contains((a.Name, b.Name)) || Attached.Contains((b.Name, a.Name)))
                {
                    continue;
                }

                if (a.X0 < b.X1 - 0.5 && b.X0 < a.X1 - 0.5 && a.Y0 < b.Y1 - 0.5 && b.Y0 < a.Y1 - 0.5)
                {
                    clashes.Add(a.Name + " x " + b.Name);
                }
            }
        }

        Assert.True(clashes.Count == 0, string.Join(", ", clashes));
        // Everything stays on the screen and out of the cutout strip.
        foreach (var s in placed)
        {
            Assert.InRange(s.Y1, 0d, (safeTop * height) + 0.01);
            Assert.True(s.Y0 >= -0.01, s.Name);
        }
    }

    [Fact]
    public void Reports_button_has_its_own_slot_clear_of_the_gem_shop()
    {
        var slots = Slots().ToDictionary(s => s.Name);
        Assert.True(slots.ContainsKey("world_reports"), "hud_layout.json needs a world_reports slot");
        var shop = slots["gem_shop"];
        var rep = slots["world_reports"];
        Assert.False(rep.X0 < shop.X1 && shop.X0 < rep.X1 && rep.Y0 < shop.Y1 && shop.Y0 < rep.Y1);
        // The old hard-coded rect sat under the Shop disc.
        Assert.True(0.78 < shop.X1 && shop.X0 < 0.97 && 0.80 < shop.Y1 && shop.Y0 < 0.86, "the old rect overlapped the shop");
        var src = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets", "Survival", "Unity", "SurvivalWorldView.Online.cs"));
        Assert.Contains("_session.Pack.Hud.TryGetValue(ReportsSlot, out var slot)", src);
        Assert.DoesNotContain("0.78f, 0.80f, 0.97f, 0.86f", src);
    }
}
