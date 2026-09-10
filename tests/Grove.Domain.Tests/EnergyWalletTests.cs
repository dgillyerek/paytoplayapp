using Grove.Domain.Energy;
using Grove.Domain.Merge;

namespace Grove.Domain.Tests;

public sealed class EnergyWalletTests
{
    [Fact]
    public void Starts_at_cap_and_will_not_go_negative()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var wallet = new EnergyWallet(EnergyConfig.Default, clock);
        Assert.Equal(100, wallet.Current);

        for (var i = 0; i < 100; i++)
        {
            Assert.True(wallet.TrySpendProduce());
        }

        Assert.Equal(0, wallet.Current);
        Assert.True(wallet.IsEmpty);
        Assert.False(wallet.TrySpendProduce());
        Assert.Equal(0, wallet.Current);
        Assert.False(wallet.TrySpendDig());
        Assert.Equal(0, wallet.Current);
    }

    [Fact]
    public void Regen_is_one_point_per_two_minutes_with_remainder()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var wallet = new EnergyWallet(EnergyConfig.Default, clock, startingEnergy: 50);
        clock.Advance(TimeSpan.FromMinutes(3));
        Assert.Equal(51, wallet.Current);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(52, wallet.Current);
    }

    [Fact]
    public void Offline_regen_caps_and_does_not_overshoot()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var wallet = new EnergyWallet(EnergyConfig.Default, clock, startingEnergy: 0);
        clock.Advance(TimeSpan.FromHours(10));
        Assert.Equal(100, wallet.Current);
        clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(100, wallet.Current);
    }

    [Fact]
    public void Ftue_top_up_fills_once()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var wallet = new EnergyWallet(EnergyConfig.Default, clock, startingEnergy: 10);
        Assert.True(wallet.TryGrantFtueTopUp());
        Assert.Equal(100, wallet.Current);
        Assert.False(wallet.TryGrantFtueTopUp());
        Assert.Equal(100, wallet.Current);
    }

    [Fact]
    public void Empty_listener_fires_on_failed_spend()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-10T00:00:00Z"));
        var listener = new RecordingListener();
        var wallet = new EnergyWallet(EnergyConfig.Default, clock, listener, startingEnergy: 1);
        Assert.True(wallet.TrySpendProduce());
        Assert.False(wallet.TrySpendProduce());
        Assert.True(listener.EmptyCount >= 1);
    }

    [Fact]
    public void Production_energy_json_matches_defaults()
    {
        var catalog = CatalogLoader.LoadDefault();
        Assert.Equal(100, catalog.Energy.Cap);
        Assert.Equal(120, catalog.Energy.RegenSecondsPerPoint);
        Assert.Equal(1, catalog.Energy.ProduceCost);
        Assert.Equal(1, catalog.Energy.DigCost);
    }

    private sealed class RecordingListener : IEnergyListener
    {
        public int EmptyCount { get; private set; }

        public void OnEnergyChanged(int current, int cap)
        {
        }

        public void OnEnergyEmpty() => EmptyCount++;
    }
}
