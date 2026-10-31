using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Taps;
using Xunit;

public class WanderingMerchantTests
{
    private const double T0 = 1_800_000_000;

    private static (WanderingMerchant m, double[] now) Make(int seed = 5)
    {
        var now = new[] { T0 };
        return (new WanderingMerchant(MerchantConfig.Default, () => now[0], seed), now);
    }

    private static Wallet Rich() => new Wallet(new Dictionary<string, int>
    {
        [SurvIds.ResSoft] = 100000, [SurvIds.ResWood] = 100000, [SurvIds.ResStone] = 100000, [SurvIds.ResFood] = 100000, [SurvIds.ResHard] = 1000,
    });

    [Fact]
    public void FirstVisit_IsNow_ForOneHour_WithThreeOffers()
    {
        var (m, now) = Make();
        Assert.True(m.IsHere);
        Assert.Equal(3, m.Offers.Count);
        Assert.Equal(MerchantDealKinds.Sku, m.Offers[2].Deal.Kind);
        now[0] += 3599;
        Assert.True(m.IsHere);
        now[0] += 1;
        Assert.False(m.IsHere);
    }

    [Fact]
    public void NextVisit_TwoToFiveHoursAfterLeaving_Deterministic()
    {
        for (var seed = 0; seed < 30; seed++)
        {
            var (a, na) = Make(seed);
            var (b, nb) = Make(seed);
            na[0] += 3600;
            nb[0] += 3600;
            Assert.True(a.Tick());
            b.Tick();
            Assert.Equal(a.ArrivesAt, b.ArrivesAt);
            Assert.InRange(a.ArrivesAt - (T0 + 3600), 2 * 3600, 5 * 3600);
            Assert.Equal(a.Offers.Select(o => o.Deal.Id), b.Offers.Select(o => o.Deal.Id));
            Assert.False(a.IsHere);
        }
    }

    [Fact]
    public void FreeGift_ShowsUpOnRoughlyAThirdOfVisits_AtMostOne()
    {
        var (m, now) = Make(1);
        var visits = 0;
        var free = 0;
        for (var i = 0; i < 400; i++)
        {
            now[0] = m.LeavesAt;
            m.Tick();
            visits++;
            var n = m.Offers.Count(o => o.IsFree);
            Assert.InRange(n, 0, 1);
            free += n;
        }

        Assert.InRange(free / (double)visits, 0.25, 0.45);
    }

    [Fact]
    public void Buy_SpendsOnce_AndGrantsEconomyOnly()
    {
        var (m, _) = Make();
        var w = Rich();
        var before = w.Get(SurvIds.ResHard) + w.Get(SurvIds.ResSoft) + w.Get(SurvIds.ResWood) + w.Get(SurvIds.ResFood) + w.Get(SurvIds.ResStone);
        Assert.Equal(MerchantBuy.Ok, m.TryBuy(0, w, out var g));
        Assert.NotNull(g);
        Assert.Equal(MerchantBuy.AlreadyBought, m.TryBuy(0, w, out _));
        Assert.Equal(MerchantBuy.UseStore, m.TryBuy(2, w, out _));
        Assert.True(m.MarkBought(2));
        Assert.False(m.MarkBought(2));
        var allowed = new HashSet<string> { SurvIds.ResSoft, SurvIds.ResWood, SurvIds.ResStone, SurvIds.ResFood, SurvIds.ResSpeedupM, SurvIds.ResHard };
        Assert.All(MerchantConfig.Default.Deals.SelectMany(d => d.Grants.Keys), k => Assert.Contains(k, allowed));
    }

    [Fact]
    public void Buy_FailsWhenAwayOrBroke()
    {
        var (m, now) = Make();
        var broke = new Wallet();
        var paid = m.Offers.Select((o, i) => (o, i)).First(x => !x.o.IsFree && x.o.Deal.Kind != MerchantDealKinds.Sku).i;
        Assert.Equal(MerchantBuy.CannotAfford, m.TryBuy(paid, broke, out _));
        Assert.False(m.Offers[paid].Bought);
        now[0] += 3600;
        Assert.Equal(MerchantBuy.NotHere, m.TryBuy(0, Rich(), out _));
    }

    [Fact]
    public void Offline_CatchesUpOnMissedVisits()
    {
        var (m, now) = Make();
        now[0] += 3 * 24 * 3600;
        m.Tick();
        Assert.True(m.LeavesAt > now[0]);
        Assert.Equal(3600, m.LeavesAt - m.ArrivesAt);
    }

    [Fact]
    public void SaveRoundTrip()
    {
        var (m, now) = Make();
        m.TryBuy(1, Rich(), out _);
        var copy = new WanderingMerchant(MerchantConfig.Default, () => now[0], 77);
        Assert.True(copy.Apply(m.Capture()));
        Assert.Equal(m.ArrivesAt, copy.ArrivesAt);
        Assert.Equal(m.LeavesAt, copy.LeavesAt);
        Assert.Equal(m.Offers.Select(o => (o.Deal.Id, o.Bought)), copy.Offers.Select(o => (o.Deal.Id, o.Bought)));
        Assert.False(copy.Apply("[]"));
    }
}
