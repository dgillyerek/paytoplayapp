using Survival.Domain.Campaign;
using Survival.Domain.Economy;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.DailyLogin;
using Xunit;

namespace Survival.Domain.Tests
{
    public class DailyLoginTests
    {
        private const long Day = 86400;

        private static (DailyLogin.DailyLogin d, ManualClock c, Wallet w) Make(long start = 1_800_000_000)
        {
            var c = new ManualClock(start);
            return (new DailyLogin.DailyLogin(c, 0), c, new Wallet());
        }

        [Fact]
        public void FirstLaunchCanClaimDayOne()
        {
            var (d, _, w) = Make();
            Assert.True(d.CanClaim);
            Assert.Equal(1, d.NextDay);
            var got = d.Claim(w);
            Assert.NotNull(got);
            Assert.Equal(5000, w.Get(SurvIds.ResSoft));
            Assert.False(d.CanClaim);
            Assert.Null(d.Claim(w));
        }

        [Fact]
        public void OneClaimPerDayThenNextDayUnlocks()
        {
            var (d, c, w) = Make();
            d.Claim(w);
            c.Advance(Day);
            Assert.True(d.CanClaim);
            Assert.Equal(2, d.NextDay);
        }

        [Fact]
        public void MissedDaysContinueInsteadOfReset()
        {
            var (d, c, w) = Make();
            d.Claim(w);
            c.Advance(Day * 5);
            Assert.Equal(2, d.NextDay);
        }

        [Fact]
        public void WeekWrapsAfterBigChest()
        {
            var (d, c, w) = Make();
            for (var i = 0; i < 7; i++) { Assert.NotNull(d.Claim(w)); c.Advance(Day); }
            Assert.Equal(1, d.Weeks);
            Assert.Equal(1, d.NextDay);
            Assert.True(w.Get(SurvIds.ResHard) >= 160);
        }

        [Fact]
        public void SaveLoadRoundTrips()
        {
            var (d, c, w) = Make();
            d.Claim(w);
            var d2 = new DailyLogin.DailyLogin(c, 0);
            Assert.True(d2.Load(d.Save()));
            Assert.False(d2.CanClaim);
            Assert.Equal(2, d2.NextDay);
            Assert.True(d2.SecondsUntilNext > 0 && d2.SecondsUntilNext <= Day);
        }
    }
}
