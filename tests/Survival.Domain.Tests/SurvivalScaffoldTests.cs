using Survival.Domain.Catalog;
using Survival.Domain.Flavor;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Theme;

namespace Survival.Domain.Tests;

public sealed class StableIdTests
{
    [Fact]
    public void Dictionary_json_matches_surv_p0_constants()
    {
        var json = File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CatalogLoader.DictionaryFileName));
        CatalogLoader.AssertDictionaryMatches(json);
    }

    [Fact]
    public void World_node_types_cover_home_gather_build_guild_fight_explore()
    {
        Assert.Equal(6, SurvIds.WorldNodeTypes.Count);
        Assert.Contains(SurvIds.WorldNodeTypeHome, SurvIds.WorldNodeTypes);
        Assert.Contains(SurvIds.WorldNodeTypeGather, SurvIds.WorldNodeTypes);
        Assert.Contains(SurvIds.WorldNodeTypeBuild, SurvIds.WorldNodeTypes);
        Assert.Contains(SurvIds.WorldNodeTypeGuild, SurvIds.WorldNodeTypes);
        Assert.Contains(SurvIds.WorldNodeTypeFight, SurvIds.WorldNodeTypes);
        Assert.Contains(SurvIds.WorldNodeTypeExplore, SurvIds.WorldNodeTypes);
        Assert.Equal(SurvIds.WorldNodeTypeHome, WorldNodeType.Home.ToId());
        Assert.Equal(WorldNodeType.Fight, SurvEnums.ParseWorldNodeType(SurvIds.WorldNodeTypeFight));
    }

    [Fact]
    public void Forbidden_grove_merge_ids_are_rejected()
    {
        Assert.True(SurvIds.IsForbidden("grove.board"));
        Assert.True(SurvIds.IsForbidden("merge.tile"));
        Assert.True(SurvIds.IsForbidden("maya.portrait"));
        Assert.True(SurvIds.IsForbidden("runtime_theme_switch"));
        Assert.False(SurvIds.IsForbidden(SurvIds.BuildingHq));
    }

    [Fact]
    public void Systems_api_is_survival_core_v1()
    {
        Assert.Equal("survival_core_v1", SurvIds.SystemsApi);
        Assert.Equal("theme.id.fantasy_kingdom_a", SurvIds.ThemeIdFantasyKingdomA);
        Assert.Equal("flavor.id.fantasy_kingdom_a", SurvIds.FlavorIdFantasyKingdomA);
    }
}

public sealed class CatalogAndPackTests
{
    [Fact]
    public void Catalog_loads_six_world_nodes_and_core_tables()
    {
        var catalog = CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir);
        Assert.Equal(SurvIds.SystemsApi, catalog.SystemsApi);
        Assert.Equal(11, catalog.Modules.Count);
        Assert.Equal(9, catalog.Buildings.Count);
        Assert.Equal(6, catalog.WorldNodeTypes.Count);
        Assert.Equal(6, catalog.WorldNodes.Count);
        Assert.Equal(SurvIds.WorldNodeHome01, catalog.WorldNodes[0].Id);
        Assert.Equal(SurvIds.HeroSlot01, catalog.Heroes[0].SlotId);
        Assert.Equal(SurvIds.SkuStarterPack, catalog.Skus[^2].Id);
        Assert.Equal(SurvIds.EnergyMeterMain, catalog.Energy.MeterId);
    }

    [Fact]
    public void Theme_pack_binder_uses_surv_p0_content_keys_and_pins()
    {
        var pack = TestPaths.LoadPack();
        Assert.Equal(SurvIds.ThemeIdFantasyKingdomA, pack.ThemeId);
        Assert.Equal(SurvIds.SystemsApi, pack.SystemsApi);
        Assert.Equal("Guild", pack.StringOr(SurvIds.ThemeAAllianceLabel, ""));
        Assert.True(pack.TryArt(SurvIds.ThemeANodeHomeCottage, out var cottage));
        Assert.Contains("01_home_cottage", cottage, StringComparison.Ordinal);
        Assert.Equal(6, pack.Pins.Count);
        Assert.Contains(pack.Pins, p => p.NodeId == SurvIds.WorldNodeFight01 && p.NodeTypeId == SurvIds.WorldNodeTypeFight);
    }

    [Fact]
    public void Session_boots_flavor_pack_and_world_map()
    {
        var session = new SurvivalSession(AppFlavorConfig.FantasyKingdomA, TestPaths.LoadPack(), CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        Assert.Equal("ThemePack/fantasy_kingdom_a", session.Flavor.PackPath);
        Assert.Equal(6, session.World.Nodes.Count);
        Assert.True(session.World.Select(SurvIds.WorldNodeGather01));
        Assert.Equal(SurvIds.WorldNodeGather01, session.World.SelectedNodeId);
        Assert.True(session.Energy.TrySpend(SurvIds.EnergyActionMarch));
        Assert.Equal(SurvIds.FtueStepBoot, session.Ftue.CurrentStepId);
        Assert.Contains(session.Iap.Skus, s => s.Id == SurvIds.SkuPassSeason);
    }

    [Fact]
    public void Flavor_json_points_at_baked_fantasy_kingdom_a_pack()
    {
        var json = File.ReadAllText(Path.Combine(TestPaths.FlavorDir, "flavor.json"));
        Assert.Contains("flavor.id.fantasy_kingdom_a", json);
        Assert.Contains("theme.id.fantasy_kingdom_a", json);
        Assert.Contains("ThemePack/fantasy_kingdom_a", json);
        Assert.Contains("survival_core_v1", json);
        AppFlavorConfig.FantasyKingdomA.Validate();
    }
}

internal static class TestPaths
{
    public static string CatalogDir => Path.Combine(AppContext.BaseDirectory, "Data");

    public static string PackDir => Path.Combine(AppContext.BaseDirectory, "Pack");

    public static string FlavorDir => Path.Combine(AppContext.BaseDirectory, "Flavor");

    public static ThemePackBinder LoadPack() =>
        ThemePackBinder.Parse(
            File.ReadAllText(Path.Combine(PackDir, "pack.json")),
            File.ReadAllText(Path.Combine(PackDir, "strings", "en.json")),
            File.ReadAllText(Path.Combine(PackDir, "map", "node_pins.json")),
            File.ReadAllText(Path.Combine(PackDir, "ui", "hud_layout.json")));
}
