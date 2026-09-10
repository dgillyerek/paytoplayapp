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
        Assert.Equal(38, manifest.Assets.Count);
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
    }

    [Fact]
    public void Board_composite_pngs_exist_so_play_mode_cannot_fall_back_to_a_bare_grid()
    {
        var manifest = ArtManifest.LoadDefault();
        var dir = ArtManifest.ResolveDirectory();
        var stubs = new[] { "ENV_FG_Backdrop", "ENV_FG_BoardSurface", "ENV_FG_CellEmpty" };
        foreach (var stub in stubs)
        {
            Assert.True(manifest.TryGet(stub, out var asset), stub);
            var path = Path.Combine(dir, asset.Filename.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), path);
        }
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
