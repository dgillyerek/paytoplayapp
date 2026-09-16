using Survival.Domain.Catalog;
using Survival.Domain.Flavor;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.World;

namespace Survival.Domain.Tests;

public sealed class GatherLoopTests
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
        Assert.Equal("2m 12s", GatherLoop.FormatClock(132));
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
        Assert.True(loop!.TryBeginMine(session.Energy, SurvIds.WorldNodeHome01, out var begin));
        Assert.True(begin.Ok);
        Assert.Equal(SurvIds.WorldNodeGather01, begin.NodeId);
        Assert.Equal(SurvIds.WorldActionMarch, begin.MarchActionId);
        Assert.Equal(SurvIds.WorldActionGather, begin.GatherActionId);
        Assert.Equal(SurvIds.EnergyActionGather, begin.EnergyActionId);
        Assert.Equal(840, begin.Yield);
        Assert.Equal(4, energyBefore - session.Energy.Current);
        Assert.True(loop.MarchPending);
        Assert.False(loop.TryBeginMine(session.Energy, SurvIds.WorldNodeHome01, out _));

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

        Assert.False(session.World.Gather!.TryBeginMine(session.Energy, SurvIds.WorldNodeHome01, out var result));
        Assert.False(result.Ok);
        Assert.Equal(GatherMineResult.Fail, session.World.Gather.CompleteArrive());
    }
}
