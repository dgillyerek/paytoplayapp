using Survival.Domain.Catalog;
using Survival.Domain.Flavor;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.World;

namespace Survival.Domain.Tests;

public sealed class WorldMarchLoopTests
{
    [Fact]
    public void Catalog_gather_node_has_slice_yield_and_march()
    {
        var catalog = CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir);
        var gather = Assert.Single(catalog.WorldNodes, n => n.Id == SurvIds.WorldNodeGather01);
        Assert.Equal(SurvIds.WorldNodeTypeGather, gather.NodeTypeId);
        Assert.Equal(8400, gather.Available);
        Assert.Equal(840, gather.YieldPerAction);
        Assert.Equal(132, gather.MarchSeconds);
        Assert.Equal("2m 12s", WorldMarchLoop.FormatClock(132));
        Assert.Equal(132, WorldMarchLoop.RemainingSeconds(132, 0f));
        Assert.Equal(132, WorldMarchLoop.RemainingSeconds(132, 0.9f));
        Assert.Equal(131, WorldMarchLoop.RemainingSeconds(132, 1f));
        Assert.Equal(0, WorldMarchLoop.RemainingSeconds(132, 132f));
        Assert.False(WorldMarchLoop.TimedLegComplete(132, 1.45f));
        Assert.True(WorldMarchLoop.TimedLegComplete(132, 132f));
    }

    [Fact]
    public void Theme_pack_gather_pin_uses_map_quarry_and_inspect_copy()
    {
        var pack = TestPaths.LoadPack();
        var pin = Assert.Single(pack.Pins, p => p.NodeId == SurvIds.WorldNodeGather01);
        Assert.Equal(SurvIds.WorldNodeTypeGather, pin.NodeTypeId);
        Assert.Equal(SurvIds.ThemeANodeGatherQuarryMap, pin.ContentKey);
        Assert.InRange(pin.PinX, 0.48f, 0.482f);
        Assert.InRange(pin.PinYFromTop, 0.283f, 0.285f);
        Assert.InRange(pin.SpriteSize, 0.128f, 0.13f);
        Assert.Equal("Stone Quarry", pack.StringOr("inspect.gather.title", ""));
        Assert.Equal("Available", pack.StringOr("inspect.gather.available", ""));
        Assert.Equal("Stone", pack.StringOr("inspect.gather.resource", ""));
        Assert.Equal("March + mine", pack.StringOr("inspect.gather.march", ""));
        Assert.Equal("Mine", pack.StringOr("inspect.gather.action", ""));
        Assert.True(pack.TryArt(SurvIds.ThemeANodeGatherQuarryMap, out var mapArt));
        Assert.Contains("02_gather_quarry_map", mapArt, StringComparison.Ordinal);
        Assert.Equal(0f, Assert.Single(pack.Pins, p => p.NodeId == SurvIds.WorldNodeHome01).SpriteSize);
    }

    [Fact]
    public void Mine_spends_gather_energy_then_arrive_credits_stone_chip()
    {
        var session = new SurvivalSession(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        var loop = session.World.Gather;
        Assert.NotNull(loop);
        var energyBefore = session.Energy.Current;
        Assert.True(loop!.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out var begin));
        Assert.True(begin.Ok);
        Assert.Equal(SurvIds.WorldNodeGather01, begin.NodeId);
        Assert.Equal(SurvIds.WorldActionMarch, begin.MarchActionId);
        Assert.Equal(SurvIds.WorldActionGather, begin.ResolveActionId);
        Assert.Equal(SurvIds.EnergyActionGather, begin.EnergyActionId);
        Assert.Equal(ChipWallet.StoneChip, begin.ChipKey);
        Assert.Equal(840, begin.Yield);
        Assert.Equal(4, energyBefore - session.Energy.Current);
        Assert.True(loop.MarchPending);
        Assert.False(loop.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out _));

        var arrive = loop.CompleteArrive();
        Assert.True(arrive.Ok);
        Assert.Equal(7560, arrive.AvailableAfter);
        Assert.False(loop.MarchPending);
        Assert.Equal(96700, session.Chips.Get(ChipWallet.StoneChip));
        Assert.Equal(97540, session.Chips.Add(ChipWallet.StoneChip, arrive.Yield));
        Assert.Equal("97.5K", ChipWallet.FormatCompact(97540));
        Assert.Equal("8,400", ChipWallet.FormatGrouped(8400));
    }

    [Fact]
    public void Mine_fails_without_energy()
    {
        var session = new SurvivalSession(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        while (session.Energy.TrySpend(SurvIds.EnergyActionGather))
        {
        }

        Assert.False(session.World.Gather!.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out var result));
        Assert.False(result.Ok);
        Assert.Equal(MarchResolveResult.Fail, session.World.Gather.CompleteArrive());
    }

    [Fact]
    public void Catalog_fight_node_has_threat_attempts_and_march()
    {
        var catalog = CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir);
        var fight = Assert.Single(catalog.WorldNodes, n => n.Id == SurvIds.WorldNodeFight01);
        Assert.Equal(SurvIds.WorldNodeTypeFight, fight.NodeTypeId);
        Assert.Equal(3, fight.Available);
        Assert.Equal(1200, fight.YieldPerAction);
        Assert.Equal(108, fight.MarchSeconds);
        Assert.Equal("1m 48s", WorldMarchLoop.FormatClock(108));
    }

    [Fact]
    public void Theme_pack_fight_pin_uses_dark_keep_and_inspect_copy()
    {
        var pack = TestPaths.LoadPack();
        var pin = Assert.Single(pack.Pins, p => p.NodeId == SurvIds.WorldNodeFight01);
        Assert.Equal(SurvIds.WorldNodeTypeFight, pin.NodeTypeId);
        Assert.Equal(SurvIds.ThemeANodeFightDarkKeep, pin.ContentKey);
        Assert.InRange(pin.SpriteSize, 0.18f, 0.22f);
        Assert.Equal("Dark Keep", pack.StringOr("inspect.fight.title", ""));
        Assert.Equal("Threat", pack.StringOr("inspect.fight.threat", ""));
        Assert.Equal("Darkness", pack.StringOr("inspect.fight.enemy", ""));
        Assert.Equal("March + battle", pack.StringOr("inspect.fight.march", ""));
        Assert.Equal("Attack", pack.StringOr("inspect.fight.action", ""));
        Assert.Equal("Gold", pack.StringOr("inspect.fight.resource", ""));
        Assert.True(pack.TryArt(SurvIds.ThemeANodeFightDarkKeep, out var art));
        Assert.Contains("05_fight_dark_keep", art, StringComparison.Ordinal);
    }

    [Fact]
    public void Attack_spends_battle_energy_then_arrive_credits_gold_and_wins()
    {
        var session = new SurvivalSession(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        var loop = session.World.Fight;
        Assert.NotNull(loop);
        Assert.True(session.World.TryGetMarch(SurvIds.WorldNodeFight01, out var byId));
        Assert.Same(loop, byId);
        var energyBefore = session.Energy.Current;
        Assert.True(loop!.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out var begin));
        Assert.Equal(SurvIds.WorldNodeFight01, begin.NodeId);
        Assert.Equal(SurvIds.WorldActionMarch, begin.MarchActionId);
        Assert.Equal(SurvIds.BattleActionStart, begin.ResolveActionId);
        Assert.Equal(SurvIds.EnergyActionBattle, begin.EnergyActionId);
        Assert.Equal(ChipWallet.SoftChip, begin.ChipKey);
        Assert.Equal(1200, begin.Yield);
        Assert.Equal(10, energyBefore - session.Energy.Current);
        Assert.False(loop.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out _));

        var arrive = loop.CompleteArrive();
        Assert.True(arrive.Ok);
        Assert.Equal(2, arrive.AvailableAfter);
        Assert.Equal(SurvIds.BattleResultWin, session.Battles.StubResolvePve());
        Assert.Equal(245600, session.Chips.Get(ChipWallet.SoftChip));
        Assert.Equal(246800, session.Chips.Add(ChipWallet.SoftChip, arrive.Yield));
        Assert.Equal("246.8K", ChipWallet.FormatCompact(246800));
    }

    [Fact]
    public void Attack_fails_without_energy()
    {
        var session = new SurvivalSession(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        while (session.Energy.TrySpend(SurvIds.EnergyActionBattle))
        {
        }

        Assert.False(session.World.Fight!.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out var result));
        Assert.False(result.Ok);
        Assert.Equal(MarchResolveResult.Fail, session.World.Fight.CompleteArrive());
    }

    [Fact]
    public void Catalog_explore_node_has_relics_and_scout_march()
    {
        var catalog = CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir);
        var explore = Assert.Single(catalog.WorldNodes, n => n.Id == SurvIds.WorldNodeExplore01);
        Assert.Equal(SurvIds.WorldNodeTypeExplore, explore.NodeTypeId);
        Assert.Equal(5, explore.Available);
        Assert.Equal(600, explore.YieldPerAction);
        Assert.Equal(84, explore.MarchSeconds);
        Assert.Equal("1m 24s", WorldMarchLoop.FormatClock(84));
    }

    [Fact]
    public void Theme_pack_explore_pin_uses_ruins_and_inspect_copy()
    {
        var pack = TestPaths.LoadPack();
        var pin = Assert.Single(pack.Pins, p => p.NodeId == SurvIds.WorldNodeExplore01);
        Assert.Equal(SurvIds.WorldNodeTypeExplore, pin.NodeTypeId);
        Assert.Equal(SurvIds.ThemeANodeExploreRuins, pin.ContentKey);
        Assert.InRange(pin.SpriteSize, 0.16f, 0.20f);
        Assert.Equal("Ruins", pack.StringOr("inspect.explore.title", ""));
        Assert.Equal("Relics", pack.StringOr("inspect.explore.relics", ""));
        Assert.Equal("March + scout", pack.StringOr("inspect.explore.march", ""));
        Assert.Equal("Explore", pack.StringOr("inspect.explore.action", ""));
        Assert.Equal("Gold", pack.StringOr("inspect.explore.resource", ""));
        Assert.True(pack.TryArt(SurvIds.ThemeANodeExploreRuins, out var art));
        Assert.Contains("06_explore_ruins", art, StringComparison.Ordinal);
    }

    [Fact]
    public void Scout_spends_march_energy_then_arrive_credits_gold()
    {
        var session = new SurvivalSession(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        var loop = session.World.Explore;
        Assert.NotNull(loop);
        Assert.True(session.World.TryGetMarch(SurvIds.WorldNodeExplore01, out var byId));
        Assert.Same(loop, byId);
        var energyBefore = session.Energy.Current;
        Assert.True(loop!.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out var begin));
        Assert.Equal(SurvIds.WorldNodeExplore01, begin.NodeId);
        Assert.Equal(SurvIds.WorldActionMarch, begin.MarchActionId);
        Assert.Equal(SurvIds.WorldActionScout, begin.ResolveActionId);
        Assert.Equal(SurvIds.EnergyActionMarch, begin.EnergyActionId);
        Assert.Equal(ChipWallet.SoftChip, begin.ChipKey);
        Assert.Equal(600, begin.Yield);
        Assert.Equal(5, energyBefore - session.Energy.Current);
        Assert.False(loop.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out _));

        var arrive = loop.CompleteArrive();
        Assert.True(arrive.Ok);
        Assert.Equal(4, arrive.AvailableAfter);
        Assert.Equal(246200, session.Chips.Add(ChipWallet.SoftChip, arrive.Yield));
        Assert.Equal("246.2K", ChipWallet.FormatCompact(246200));
    }

    [Fact]
    public void Scout_fails_without_energy()
    {
        var session = new SurvivalSession(
            AppFlavorConfig.FantasyKingdomA,
            TestPaths.LoadPack(),
            CatalogLoader.LoadFromDirectory(TestPaths.CatalogDir));
        while (session.Energy.TrySpend(SurvIds.EnergyActionMarch))
        {
        }

        Assert.False(session.World.Explore!.TryBegin(session.Energy, SurvIds.WorldNodeHome01, out var result));
        Assert.False(result.Ok);
        Assert.Equal(MarchResolveResult.Fail, session.World.Explore.CompleteArrive());
    }
}
