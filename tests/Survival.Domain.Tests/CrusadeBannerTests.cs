using Survival.Domain.Clock;
using Survival.Domain.Crusade;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Store;
using Xunit;

namespace Survival.Domain.Tests
{
    public class CrusadeBannerTests
    {
        private static readonly double MidSeason = CrusadeBanner.SeasonEpoch + (10 * CrusadeBanner.SeasonSeconds) + 3600;

        private static (CrusadeBanner b, ManualClock c, Wallet w) Make()
        {
            var c = new ManualClock(MidSeason);
            return (new CrusadeBanner(c), c, new Wallet());
        }

        [Fact]
        public void XpOpensTiers()
        {
            var (b, _, _) = Make();
            Assert.Equal(0, b.TierReached);
            b.AddXp(CrusadeXpSource.RunnerLevel, 3); // 360
            Assert.Equal(1, b.TierReached);
            Assert.Equal(110, b.XpIntoTier);
            Assert.Equal(1, b.ReadyCount);
        }

        [Fact]
        public void FreeClaimOnceAndPremiumNeedsBanner()
        {
            var (b, _, w) = Make();
            b.AddXp(CrusadeXpSource.Upgrade, 5);
            Assert.NotNull(b.Claim(1, false, w));
            Assert.Null(b.Claim(1, false, w));
            Assert.Null(b.Claim(1, true, w));
            b.UnlockPremium();
            Assert.Equal(1, b.ReadyCount);
            var got = b.Claim(1, true, w);
            Assert.NotNull(got);
            Assert.True(w.Get(SurvIds.ResHard) >= 22);
        }

        [Fact]
        public void CannotClaimUnreachedTier()
        {
            var (b, _, w) = Make();
            b.UnlockPremium();
            Assert.Null(b.Claim(2, true, w));
        }

        [Fact]
        public void XpCapsAtLastTier()
        {
            var (b, _, _) = Make();
            b.AddXp(CrusadeXpSource.RunnerLevel, 10000);
            Assert.Equal(CrusadeBanner.TierCount, b.TierReached);
            Assert.Equal(CrusadeBanner.TierCount * 2 - CrusadeBanner.TierCount, b.ReadyCount);
        }

        [Fact]
        public void NewSeasonWipesProgressAndBanner()
        {
            var (b, c, w) = Make();
            b.AddXp(CrusadeXpSource.Quest, 10);
            b.UnlockPremium();
            b.Claim(1, false, w);
            var s = b.Season;
            Assert.True(b.SecondsLeft > 0 && b.SecondsLeft <= CrusadeBanner.SeasonSeconds);
            c.Advance(b.SecondsLeft + 1);
            Assert.True(b.RollSeason());
            Assert.Equal(s + 1, b.Season);
            Assert.Equal(0, b.Xp);
            Assert.False(b.HasPremium);
            Assert.False(b.IsClaimed(1, false));
        }

        [Fact]
        public void SaveLoadRoundTripsAndRollsStaleSeason()
        {
            var (b, c, w) = Make();
            b.AddXp(CrusadeXpSource.RunnerLevel, 5);
            b.UnlockPremium();
            b.Claim(2, true, w);
            b.RunnerSeen = 7;
            var b2 = new CrusadeBanner(c);
            Assert.True(b2.Load(b.Save()));
            Assert.Equal(b.Xp, b2.Xp);
            Assert.True(b2.HasPremium);
            Assert.True(b2.IsClaimed(2, true));
            Assert.Equal(7, b2.RunnerSeen);
            c.Advance(CrusadeBanner.SeasonSeconds);
            var b3 = new CrusadeBanner(c);
            Assert.True(b3.Load(b.Save()));
            Assert.Equal(0, b3.Xp);
            Assert.False(b3.HasPremium);
        }

        [Fact]
        public void SandboxPurchaseGoesThroughStore()
        {
            var (b, _, w) = Make();
            var sandbox = new SandboxPurchaseService();
            var store = CrusadeBanner.MakeStore(null, sandbox, w);
            Assert.Equal("$9.99", store.ProductFor(CrusadeBanner.Sku)!.PriceLabel);
            var ok = false;
            store.BuySku(CrusadeBanner.OfferId, o => ok = o.Ok);
            Assert.True(ok);
            Assert.Single(sandbox.History);
            sandbox.FailNext = true;
            store.BuySku(CrusadeBanner.OfferId, o => ok = o.Ok);
            Assert.False(ok);
        }
    }
}
