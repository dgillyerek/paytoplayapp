using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Taps;
using Xunit;

public class WishingWellTests
{
    private const double T0 = 1_800_000_000; // mid-day UTC

    private static (WishingWell w, double[] now) Make(int seed = 9)
    {
        var now = new[] { T0 };
        return (new WishingWell(WellConfig.Default, () => now[0], seed), now);
    }

    private static Wallet Purse(int gold, int gems) => new Wallet(new Dictionary<string, int> { [SurvIds.ResSoft] = gold, [SurvIds.ResHard] = gems });

    [Fact]
    public void PriceLadder_FreeThenGoldThenGemsThenSoldOut()
    {
        var (well, _) = Make();
        var w = Purse(100000, 1000);
        Assert.Equal(WellCostKind.Free, well.NextCost.Kind);
        Assert.NotNull(well.Toss(w));
        Assert.Equal(100000, w.Get(SurvIds.ResSoft)); // free toss costs nothing; the view grants rewards
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(WellCostKind.Gold, well.NextCost.Kind);
            Assert.Equal(5000, well.NextCost.Amount);
            Assert.NotNull(well.Toss(w));
        }

        Assert.Equal(85000, w.Get(SurvIds.ResSoft));
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(WellCostKind.Gems, well.NextCost.Kind);
            Assert.NotNull(well.Toss(w));
        }

        Assert.Equal(800, w.Get(SurvIds.ResHard));
        Assert.Equal(WellCostKind.SoldOut, well.NextCost.Kind);
        Assert.Null(well.Toss(w));
    }

    [Fact]
    public void Broke_DoesNotCountAToss()
    {
        var (well, _) = Make();
        well.Toss(Purse(0, 0));
        Assert.Null(well.Toss(Purse(4999, 0)));
        Assert.Equal(1, well.TossesToday);
    }

    [Fact]
    public void FreeTossReturns_AtUtcMidnight()
    {
        var (well, now) = Make();
        well.Toss(Purse(0, 0));
        Assert.False(well.FreeAvailable);
        now[0] += well.SecondsUntilReset - 1;
        Assert.False(well.FreeAvailable);
        now[0] += 1;
        Assert.True(well.FreeAvailable);
    }

    [Fact]
    public void Rewards_AreEconomyOnly_DeterministicAndJackpotIsRare()
    {
        var allowed = new HashSet<string> { SurvIds.ResSoft, SurvIds.ResWood, SurvIds.ResStone, SurvIds.ResFood, SurvIds.ResSpeedupM, SurvIds.ResHard };
        var jackpots = 0;
        const int days = 2000;
        var (a, na) = Make(123);
        var (b, nb) = Make(123);
        for (var d = 0; d < days; d++)
        {
            na[0] += 86400;
            nb[0] += 86400;
            var ra = a.Toss(Purse(0, 0))!;
            var rb = b.Toss(Purse(0, 0))!;
            Assert.Equal(ra.Rewards, rb.Rewards);
            Assert.All(ra.Rewards.Keys, k => Assert.Contains(k, allowed));
            if (ra.Jackpot)
            {
                jackpots++;
            }
        }

        Assert.InRange(jackpots / (double)days, 0.01, 0.06);
    }

    [Fact]
    public void SaveRoundTrip_KeepsTodaysTosses()
    {
        var (well, now) = Make();
        well.Toss(Purse(0, 0));
        well.Toss(Purse(5000, 0));
        var copy = new WishingWell(WellConfig.Default, () => now[0], 1);
        Assert.True(copy.Apply(well.Capture()));
        Assert.Equal(2, copy.TossesToday);
        Assert.Equal(WellCostKind.Gold, copy.NextCost.Kind);
        now[0] += 86400;
        Assert.True(copy.FreeAvailable);
        Assert.False(copy.Apply("nope"));
    }
}
