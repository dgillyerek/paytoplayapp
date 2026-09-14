using System.Security.Cryptography;
using System.Text;
using Grove.Domain.Art;
using Grove.Domain.Layout;
using Grove.Domain.Merge;

namespace Grove.Domain.Tests;

/// <summary>
/// QA DES-006 AC (match exactly) vs Derek phone Play FAIL SoT 2026-09-11.
/// Cream 7×5 plates, Maya “A little bigge…” clip, STA/RTER wrap, Wildflo/wer wrap,
/// icons overlapping copy.
/// </summary>
public sealed class Des006AcceptanceTests
{
    public const string CellEmptySha256 = "c78fabe8497c90933d89331cab4778b94b97dee79d951bc55f1a433bbd6521f7";
    public const int CellEmptyBytes = 65949;
    public const int CellEmptySize = 256;

    [Fact]
    public void Ac1_maya_bubble_fully_inside_top_right_hud_no_clip()
    {
        Assert.True(PlayLayout.TopBarBand.Contains(PlayLayout.Maya));
        Assert.True(PlayLayout.TopBarBand.Contains(PlayLayout.MayaBubble));
        Assert.True(PlayLayout.ScreenSafe.Contains(PlayLayout.MayaBubble));
        Assert.True(PlayLayout.ScreenSafe.Contains(PlayLayout.Maya));
        Assert.True(PlayLayout.MayaBubble.XMin >= 0.50f);
        Assert.True(PlayLayout.MayaBubble.XMax <= PlayLayout.Maya.XMin + 0.0001f);
        Assert.False(PlayLayout.MayaBubble.Overlaps(PlayLayout.Maya));
        Assert.False(PlayLayout.MayaBubble.Overlaps(PlayLayout.EnergyLabel));
        Assert.False(PlayLayout.MayaBubble.Overlaps(PlayLayout.CoinLabel));
        Assert.False(PlayLayout.MayaBubble.Overlaps(PlayLayout.Goal));
        Assert.True(PlayLayout.Maya.XMin - PlayLayout.MayaBubble.XMax >= PlayLayout.MinGapNormX - 0.0001f);

        var inner = PlayLayout.InsetPx(
            PlayLayout.MayaBubble,
            PlayLayout.MayaBubblePadXPx,
            PlayLayout.MayaBubblePadYPx);
        var width = PlayLayout.WidthPx(inner);
        var height = PlayLayout.HeightPx(inner);
        Assert.True(
            PlayCopy.FitsWrapped(
                "A little bigger — keep stacking matches.",
                width,
                height,
                PlayLayout.TypeTeach));
        var wrapped = PlayCopy.WrapWords(
            "A little bigger — keep stacking matches.",
            width,
            PlayLayout.TypeTeach);
        Assert.DoesNotContain(wrapped, line => line.Contains("bigge") && !line.Contains("bigger"));
        Assert.DoesNotContain(wrapped, line => line.Contains("\n"));
    }

    [Fact]
    public void Ac2_starter_and_item_names_single_line_ellipsis_never_wrap()
    {
        var starterInner = PlayLayout.WidthPx(PlayLayout.Store) - 2f * PlayLayout.StarterPadXPx;
        Assert.Equal("STARTER", PlayCopy.Ellipsize("STARTER", starterInner, PlayLayout.TypeButton));
        Assert.True(PlayCopy.TokenFits("STARTER", PlayLayout.TypeButton, starterInner));
        Assert.Equal(1, PlayCopy.WrapWords("STARTER", starterInner, PlayLayout.TypeButton).Count);
        Assert.DoesNotContain("\n", PlayCopy.Ellipsize("STARTER", 40f, PlayLayout.TypeButton));
        Assert.Contains("…", PlayCopy.Ellipsize("STARTER", 40f, PlayLayout.TypeButton));

        var catalog = CatalogLoader.LoadDefault();
        foreach (var active in new[] { true, false })
        {
            var card = PlayLayout.MapLocal(PlayLayout.OrderTray, PlayLayout.OrderCardLocal(active ? 0 : 2, 3));
            var body = PlayLayout.MapLocal(card, PlayLayout.OrderCardBody(active));
            var width = PlayLayout.WidthPx(body);
            var type = active ? PlayLayout.TypeOrderActive : PlayLayout.TypeOrderBody;
            foreach (var token in new[] { "STARTER", "Wildflower", "Herb", "Pot", "Bud" })
            {
                var fit = PlayCopy.Ellipsize(token, width, type);
                Assert.DoesNotContain("\n", fit);
                Assert.Equal(1, PlayCopy.WrapWords(fit, width, type).Count);
            }

            Assert.Equal("Wildflower", PlayCopy.Ellipsize("Wildflower", width, type));
            var phrase = PlayCopy.Ellipsize("0/1 Wildflower", width, type);
            Assert.DoesNotContain("\n", phrase);
            var squeezed = PlayCopy.Ellipsize("0/1 Wildflower", 80f, type);
            Assert.DoesNotContain("\n", squeezed);
            Assert.True(squeezed.IndexOf('…') >= 0 || squeezed == "0/1 Wildflower");
        }

        foreach (var order in catalog.Orders)
        {
            foreach (var req in order.Requirements)
            {
                if (!catalog.Items.TryGet(req.Item, out var def))
                {
                    continue;
                }

                foreach (var token in def.DisplayName.Split(' '))
                {
                    var card = PlayLayout.MapLocal(PlayLayout.OrderTray, PlayLayout.OrderCardLocal(0, 3));
                    var width = PlayLayout.WidthPx(PlayLayout.MapLocal(card, PlayLayout.OrderCardBody(true)));
                    Assert.True(PlayCopy.TokenFits(token, PlayLayout.TypeOrderBody, width), token);
                    Assert.Equal(1, PlayCopy.WrapWords(token, width, PlayLayout.TypeOrderBody).Count);
                }
            }
        }
    }

    [Fact]
    public void Ac3_order_cards_gutters_icon_scale_and_12px_inset()
    {
        Assert.True(PlayLayout.MinDockGutterDp >= 12f);
        Assert.Equal(0.70f, PlayLayout.OrderIconMaxOfInner);
        Assert.Equal(12f, PlayLayout.OrderIconInsetPx);

        var trayW = PlayLayout.OrderTray.XMax - PlayLayout.OrderTray.XMin;
        var a = PlayLayout.OrderCardLocal(0, 3);
        var b = PlayLayout.OrderCardLocal(1, 3);
        var c = PlayLayout.OrderCardLocal(2, 3);
        var gutterPx = PlayLayout.MinDockGutterDp;
        Assert.True(a.XMin * trayW * PlayLayout.ReferenceWidth >= gutterPx - 0.05f);
        Assert.True((b.XMin - a.XMax) * trayW * PlayLayout.ReferenceWidth >= gutterPx - 0.05f);
        Assert.True((c.XMin - b.XMax) * trayW * PlayLayout.ReferenceWidth >= gutterPx - 0.05f);

        var innerW = 1f - 2f * PlayLayout.OrderCardInnerPad;
        foreach (var active in new[] { true, false })
        {
            var icons = PlayLayout.OrderCardIcons(active);
            Assert.True(
                icons.XMax - icons.XMin <= innerW * PlayLayout.OrderIconMaxOfInner + 0.0001f,
                "icon cluster wider than 70% of card inner");
            Assert.False(icons.Overlaps(PlayLayout.OrderCardBody(active)));

            for (var i = 0; i < 3; i++)
            {
                var card = PlayLayout.MapLocal(PlayLayout.OrderTray, PlayLayout.OrderCardLocal(i, 3));
                AssertInsetFromCard(card, PlayLayout.MapLocal(card, icons), checkBottom: false);
                AssertInsetFromCard(card, PlayLayout.MapLocal(card, PlayLayout.OrderCardBody(active)), checkBottom: false);
                for (var r = 0; r < 2; r++)
                {
                    var req = PlayLayout.MapLocal(PlayLayout.MapLocal(card, icons), PlayLayout.OrderCardReqIcon(r, 2));
                    AssertInsetFromCard(card, req, checkBottom: false);
                }
            }
        }
    }

    [Fact]
    public void Ac4_board_bottom_dock_top_and_16dp_gap()
    {
        Assert.InRange(PlayLayout.DesignBoardBottom, 0.68f, 0.70f);
        Assert.InRange(PlayLayout.DesignDockTop, 0.715f, 0.725f);
        Assert.Equal(0.72f, PlayLayout.DesignDockTop);
        var gapPx = (PlayLayout.DesignDockTop - PlayLayout.DesignBoardBottom) * PlayLayout.ReferenceHeight;
        Assert.True(gapPx >= PlayLayout.MinHudGapDp - 0.01f, gapPx.ToString("0.0"));
        Assert.True(PlayLayout.BoardBand.YMin - PlayLayout.DockBand.YMax >= PlayLayout.MinGapNormY - 0.0001f);
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.BoardBand));
        Assert.False(PlayLayout.OrderTray.Overlaps(PlayLayout.BoardSafeRect));
        Assert.True(PlayLayout.BoardBand.Contains(PlayLayout.BoardSafeRect));
    }

    [Fact]
    public void Ac5_cellempty_is_abcf378_true_alpha_well_not_cream_plate()
    {
        var manifest = ArtManifest.LoadDefault();
        var dir = ArtManifest.ResolveDirectory();
        Assert.True(manifest.TryGet("ENV_FG_CellEmpty", out var asset));
        var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(CellEmptyBytes, bytes.Length);
        Assert.Equal(CellEmptySha256, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

        PngInspect.Header(path, out var width, out var height, out var colorType);
        Assert.Equal(CellEmptySize, width);
        Assert.Equal(CellEmptySize, height);
        Assert.Equal(6, colorType);

        PngInspect.DecodeRgba(path, out width, out height, out var rgba);
        Assert.True(StudioPlatePunch.CornersTransparent(rgba, width, height), "cream outer plate would have opaque corners");
        StudioPlatePunch.PunchMagentaKey(rgba, width, height);
        Assert.True(
            StudioPlatePunch.LooksLikeTrueAlphaWell(rgba, width, height),
            "DES-006 CellEmpty @ abcf378 must be a true-alpha well, not a cream plate");
        Assert.InRange(PlayLayout.CellWellFill, 0.84f, 0.96f);
        Assert.True(PlayLayout.CellWellFill < 1f);
    }

    [Fact]
    public void Ban_moved_toast_and_energy_charges_wrap()
    {
        var energyW = PlayLayout.WidthPx(PlayLayout.EnergyLabel);
        Assert.True(energyW >= 220f, energyW.ToString("0.0"));
        Assert.Equal("100/100", PlayCopy.Ellipsize("100/100", energyW, PlayLayout.TypeHud));
        Assert.True(PlayCopy.TokenFits("100/100", PlayLayout.TypeHud, energyW));
        Assert.Equal(1, PlayCopy.WrapWords("100/100", energyW, PlayLayout.TypeHud).Count);

        var hud = ReadRepoFile("Assets", "Grove", "Unity", "PrototypeHud.cs");
        Assert.DoesNotContain("LastFeedback", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Moved", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("Moved.", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("CrateLabel", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Charges", hud, StringComparison.Ordinal);
        Assert.Contains("oneLine: true", hud, StringComparison.Ordinal);
        Assert.Contains("PlayCopy.Ellipsize", hud, StringComparison.Ordinal);
        Assert.Contains("\"STARTER\"", hud, StringComparison.Ordinal);

        foreach (var rect in PlayLayout.OccludingHud())
        {
            Assert.False(
                Nearly(rect, PlayLayout.CrateLabel),
                "Charges crate label must stay off Play HUD");
        }
    }

    private static void AssertInsetFromCard(NormRect card, NormRect child, bool checkBottom)
    {
        var left = (child.XMin - card.XMin) * PlayLayout.ReferenceWidth;
        var right = (card.XMax - child.XMax) * PlayLayout.ReferenceWidth;
        var top = (card.YMax - child.YMax) * PlayLayout.ReferenceHeight;
        Assert.True(left >= PlayLayout.OrderIconInsetPx - 0.05f, "left inset " + left.ToString("0.0"));
        Assert.True(right >= PlayLayout.OrderIconInsetPx - 0.05f, "right inset " + right.ToString("0.0"));
        Assert.True(top >= PlayLayout.OrderIconInsetPx - 0.05f, "top inset " + top.ToString("0.0"));
        if (checkBottom)
        {
            var bottom = (child.YMin - card.YMin) * PlayLayout.ReferenceHeight;
            Assert.True(bottom >= PlayLayout.OrderIconInsetPx - 0.05f, "bottom inset " + bottom.ToString("0.0"));
        }
    }

    private static bool Nearly(NormRect a, NormRect b) =>
        Math.Abs(a.XMin - b.XMin) < 0.0001f
        && Math.Abs(a.YMin - b.YMin) < 0.0001f
        && Math.Abs(a.XMax - b.XMax) < 0.0001f
        && Math.Abs(a.YMax - b.YMax) < 0.0001f;

    private static string ReadRepoFile(params string[] parts)
    {
        var art = ArtManifest.ResolveDirectory();
        var root = Path.GetFullPath(Path.Combine(art, "..", "..", "..", ".."));
        var path = Path.Combine(root, Path.Combine(parts));
        return File.ReadAllText(path, Encoding.UTF8);
    }
}
