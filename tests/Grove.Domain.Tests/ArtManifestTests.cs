using System.IO;
using Grove.Domain.Art;
using Grove.Domain.Merge;
using Grove.Domain.Orders;

namespace Grove.Domain.Tests;

public sealed class ArtManifestTests
{
    [Fact]
    public void Area1_manifest_lists_des001_and_des002_pack()
    {
        var manifest = ArtManifest.LoadDefault();
        Assert.True(manifest.Des001Ready);
        Assert.True(manifest.Des002Ready);
        Assert.Equal("Project Grove", manifest.Title);
        Assert.Equal(42, manifest.Assets.Count);
        Assert.True(manifest.TryGet("WF_T05_Bouquet", out var bouquet));
        Assert.Equal("Items/Wildflower/WF_T05_Bouquet.png", bouquet.Filename);
        Assert.Equal(100f, bouquet.PixelsPerUnit);
        Assert.Equal(0.5f, bouquet.PivotX);
        Assert.True(manifest.TryGet("Splash_Boot_ProjectGrove", out _));
        Assert.True(manifest.TryGet("Splash_AreaStart_FrontGarden", out _));
        Assert.True(manifest.TryGet("Splash_Milestone_FrontGardenRestored", out _));
        Assert.True(manifest.TryGet("MAYA_Portrait_Neutral", out _));
        Assert.True(manifest.TryGet("TL_T02_Stick", out _));
        Assert.True(manifest.TryGet("ENV_FG_Backdrop", out _));
        Assert.True(manifest.TryGet("ENV_FG_BoardSurface", out _));
        Assert.True(manifest.TryGet("ENV_FG_CellEmpty", out _));
        Assert.True(manifest.TryGet("ENV_FG_CellHighlight", out _));
        Assert.True(manifest.TryGet("UI_OrderDock_Panel", out _));
        Assert.True(manifest.TryGet("UI_GoalPill", out _));
        Assert.True(manifest.TryGet("UI_Teach_Ring", out _));
        Assert.True(manifest.TryGet("UI_Teach_Hand", out _));
        foreach (var stub in new[] { "UI_OrderDock_Panel", "UI_GoalPill", "UI_Teach_Ring", "UI_Teach_Hand" })
        {
            Assert.True(manifest.TryGet(stub, out var asset), stub);
            var path = Path.Combine(ArtManifest.ResolveDirectory(), asset.Filename.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), path);
        }
    }

    [Fact]
    public void Wildflower_t1_to_t5_and_maya_happy_are_production_pngs_not_tiny_stubs()
    {
        var manifest = ArtManifest.LoadDefault();
        var dir = ArtManifest.ResolveDirectory();
        // 9e84a1a refresh: Seed ~41KB (old stub ~2.8KB), Maya Happy ~348KB (old ~5KB).
        var minBytes = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["WF_T01_Seed"] = 20000,
            ["WF_T02_Sprout"] = 20000,
            ["WF_T03_Bud"] = 20000,
            ["WF_T04_Wildflower"] = 20000,
            ["WF_T05_Bouquet"] = 20000,
            ["MAYA_Portrait_Happy"] = 100000
        };
        foreach (var pair in minBytes)
        {
            Assert.True(manifest.TryGet(pair.Key, out var asset), pair.Key);
            var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
            var length = new FileInfo(path).Length;
            Assert.True(length >= pair.Value, $"{pair.Key} is {length} bytes — expected production pack, not the old stub.");
        }
    }

    [Fact]
    public void Board_composite_pngs_exist_so_play_mode_cannot_fall_back_to_a_bare_grid()
    {
        var manifest = ArtManifest.LoadDefault();
        var dir = ArtManifest.ResolveDirectory();
        // 7dc144a polish: BoardSurface ~1.6MB (old ~25KB), CellEmpty ~39KB (old ~0.6KB).
        var minBytes = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["ENV_FG_Backdrop"] = 100000,
            ["ENV_FG_BoardSurface"] = 100000,
            ["ENV_FG_CellEmpty"] = 10000,
            ["ENV_FG_CellHighlight"] = 10000,
            ["ENV_FG_GardenCrate_Idle"] = 100000
        };
        foreach (var pair in minBytes)
        {
            Assert.True(manifest.TryGet(pair.Key, out var asset), pair.Key);
            var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
            var length = new FileInfo(path).Length;
            Assert.True(length >= pair.Value, $"{pair.Key} is {length} bytes — expected Design board polish, not a stub that would show a bare grid.");
        }

        Assert.True(manifest.TryGet("ENV_FG_GardenCrate_Charged", out var charged));
        var chargedPath = Path.Combine(dir, charged.Filename.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(new FileInfo(chargedPath).Length < 20000, "Charged crate is still the tiny stub — Play must prefer Idle.");
    }

    [Fact]
    public void Teach_ring_and_hand_are_rgba_with_transparent_corners()
    {
        var manifest = ArtManifest.LoadDefault();
        var dir = ArtManifest.ResolveDirectory();
        foreach (var stub in new[] { "UI_Teach_Ring", "UI_Teach_Hand" })
        {
            Assert.True(manifest.TryGet(stub, out var asset), stub);
            var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
            PngInspect.Header(path, out var width, out var height, out var colorType);
            Assert.True(width >= 128 && height >= 128, stub);
            Assert.Equal(6, colorType);
            PngInspect.DecodeRgba(path, out _, out _, out var rgba);
            Assert.Equal(0, rgba[3]);
            var last = (width * height - 1) * 4;
            Assert.Equal(0, rgba[last + 3]);
            var opaque = 0;
            for (var i = 3; i < rgba.Length; i += 4)
            {
                if (rgba[i] > 32)
                {
                    opaque++;
                }
            }

            Assert.InRange(opaque, 200, width * height / 2);
        }
    }

    [Fact]
    public void Order_dock_panel_is_a_wide_inventory_bar_not_a_tall_overlay()
    {
        var manifest = ArtManifest.LoadDefault();
        var dir = ArtManifest.ResolveDirectory();
        Assert.True(manifest.TryGet("UI_OrderDock_Panel", out var asset));
        var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
        PngInspect.Header(path, out var width, out var height, out _);
        Assert.True(width > height * 2, $"dock panel {width}x{height} would smear over order cards if stretched to the full dock");
    }

    [Fact]
    public void Scripted_chain_items_map_to_manifest_stubs_with_pngs_on_disk()
    {
        var manifest = ArtManifest.LoadDefault();
        var dir = ArtManifest.ResolveDirectory();
        var ids = new[]
        {
            "wildflower_t1", "wildflower_t2", "wildflower_t3", "wildflower_t4", "wildflower_t5", "wildflower_t6",
            "herb_t1", "herb_t2", "herb_t3",
            "tool_t1", "tool_t2", "tool_t3"
        };
        foreach (var id in ids)
        {
            Assert.True(ArtManifest.TryStubForItem(id, out var stub), id);
            Assert.True(manifest.TryGet(stub, out var asset), stub);
            var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), path);
        }

        Assert.False(ArtManifest.TryStubForItem("twig_t1", out _));
        Assert.Equal("Stick", CatalogLoader.LoadDefault().Items.Require(GroveCatalog.ToolT2).DisplayName);
    }

    [Fact]
    public void Dev018_splash_sequence_is_boot_then_area_then_milestone()
    {
        var catalog = CatalogLoader.LoadDefault();
        var splash = new SplashStub(catalog.Copy);
        var boot = splash.TryConsumeBoot();
        Assert.NotNull(boot);
        Assert.Equal("Project Grove", boot!.Caption);
        Assert.Equal("Splash_Boot_ProjectGrove", boot.ArtStub);
        Assert.Null(splash.TryConsumeBoot());

        var area = splash.TryConsumeAreaStart();
        Assert.NotNull(area);
        Assert.Equal("Front Garden", area!.Caption);
        Assert.Equal("Splash_AreaStart_FrontGarden", area.ArtStub);

        Assert.Null(splash.TryConsumeMilestoneBeat(false));
        var milestone = splash.TryConsumeMilestoneBeat(true);
        Assert.NotNull(milestone);
        Assert.Equal("Front Garden Restored", milestone!.Caption);
        Assert.Equal("Splash_Milestone_FrontGardenRestored", milestone.ArtStub);
        Assert.Null(splash.TryConsumeMilestoneBeat(true));
    }
}

internal static class PngInspect
{
    public static void Header(string path, out int width, out int height, out int colorType)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 26 || bytes[0] != 0x89)
        {
            throw new InvalidDataException(path);
        }

        width = ReadBe32(bytes, 16);
        height = ReadBe32(bytes, 20);
        colorType = bytes[25];
    }

    public static void DecodeRgba(string path, out int width, out int height, out byte[] rgba)
    {
        var data = File.ReadAllBytes(path);
        Header(path, out width, out height, out var colorType);
        if (colorType != 6)
        {
            throw new InvalidDataException("Expected RGBA PNG: " + path);
        }

        var idat = new List<byte>();
        var pos = 8;
        while (pos + 12 <= data.Length)
        {
            var length = ReadBe32(data, pos);
            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            if (type == "IDAT")
            {
                for (var i = 0; i < length; i++)
                {
                    idat.Add(data[pos + 8 + i]);
                }
            }

            if (type == "IEND")
            {
                break;
            }

            pos += 12 + length;
        }

        var compressed = idat.ToArray();
        byte[] raw;
        using (var input = new MemoryStream(compressed))
        using (var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress))
        using (var output = new MemoryStream())
        {
            zlib.CopyTo(output);
            raw = output.ToArray();
        }

        var bpp = 4;
        var stride = width * bpp;
        rgba = new byte[width * height * 4];
        var prev = new byte[stride];
        var row = new byte[stride];
        var iRaw = 0;
        for (var y = 0; y < height; y++)
        {
            var filter = raw[iRaw++];
            Array.Copy(raw, iRaw, row, 0, stride);
            iRaw += stride;
            UndoFilter(filter, row, prev, bpp);
            Array.Copy(row, 0, rgba, y * stride, stride);
            Array.Copy(row, prev, stride);
        }
    }

    private static void UndoFilter(int filter, byte[] row, byte[] prev, int bpp)
    {
        for (var x = 0; x < row.Length; x++)
        {
            var left = x >= bpp ? row[x - bpp] : (byte)0;
            var up = prev[x];
            var upLeft = x >= bpp ? prev[x - bpp] : (byte)0;
            switch (filter)
            {
                case 1:
                    row[x] = (byte)(row[x] + left);
                    break;
                case 2:
                    row[x] = (byte)(row[x] + up);
                    break;
                case 3:
                    row[x] = (byte)(row[x] + ((left + up) / 2));
                    break;
                case 4:
                    row[x] = (byte)(row[x] + Paeth(left, up, upLeft));
                    break;
            }
        }
    }

    private static byte Paeth(byte a, byte b, byte c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc)
        {
            return a;
        }

        return pb <= pc ? b : c;
    }

    private static int ReadBe32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}
