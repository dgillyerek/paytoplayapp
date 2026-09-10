using Grove.Domain.Energy;
using Grove.Domain.Merge;
using Grove.Domain.Producer;

namespace Grove.Domain.Tests;

public sealed class GardenCrateTests
{
    [Fact]
    public void Prototype_crate_is_generous_and_mostly_wildflower()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.NotNull(catalog.GardenCrate);
        Assert.True(catalog.GardenCrate!.MaxCharges >= 20);
        Assert.Equal("wildflower_t1", catalog.GardenCrate.Outputs[0].ItemId);
        Assert.True(catalog.GardenCrate.Outputs[0].Weight >= 50);
    }

    [Fact]
    public void First_tap_is_free_then_energy_is_spent()
    {
        var catalog = CatalogLoader.LoadDefault();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var energy = new EnergyWallet(catalog.Energy, clock, startingEnergy: 5);
        var crate = new GardenCrate(catalog.GardenCrate!, ScriptedRandom.Always(0));
        Assert.IsType<SpitResult.Ok>(crate.TrySpit(energy, clock));
        Assert.Equal(5, energy.Current);
        Assert.IsType<SpitResult.Ok>(crate.TrySpit(energy, clock));
        Assert.Equal(4, energy.Current);
    }

    [Fact]
    public void Empty_energy_does_not_consume_a_charge()
    {
        var catalog = CatalogLoader.LoadDefault();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var energy = new EnergyWallet(catalog.Energy, clock, startingEnergy: 0);
        var crate = new GardenCrate(catalog.GardenCrate!, ScriptedRandom.Always(0), ftueFreeTapRemaining: false);
        var before = crate.Charges(clock);
        var result = crate.TrySpit(energy, clock);
        Assert.Equal("no-energy", Assert.IsType<SpitResult.Failed>(result).Reason);
        Assert.Equal(before, crate.Charges(clock));
    }

    [Fact]
    public void Recharge_restores_a_charge_after_interval()
    {
        var catalog = CatalogLoader.LoadDefault();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var energy = new EnergyWallet(catalog.Energy, clock);
        var crate = new GardenCrate(catalog.GardenCrate!, ScriptedRandom.Always(0), startingCharges: 1);
        Assert.IsType<SpitResult.Ok>(crate.TrySpit(energy, clock));
        Assert.Equal(0, crate.Charges(clock));
        Assert.Equal("no-charges", Assert.IsType<SpitResult.Failed>(crate.TrySpit(energy, clock)).Reason);
        clock.Advance(TimeSpan.FromSeconds(catalog.GardenCrate!.RechargeSeconds));
        Assert.Equal(1, crate.Charges(clock));
    }
}
