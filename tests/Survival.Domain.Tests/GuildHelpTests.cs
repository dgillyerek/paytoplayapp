using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Guild;
using Survival.Domain.Ids;
using Xunit;

namespace Survival.Domain.Tests
{
    public class GuildHelpTests
    {
        private const double Start = 1_800_000_000;

        private static (GuildHelp g, TimerQueue q, ManualClock c, Wallet w) Make()
        {
            var c = new ManualClock(Start);
            var q = new TimerQueue(c, new RushRules(), 2);
            return (new GuildHelp(new SimulatedGuild(), q), q, c, new Wallet());
        }

        [Fact]
        public void SimulatedGuildAlwaysHasOpenRequests()
        {
            var (g, _, _, _) = Make();
            Assert.Equal(SimulatedGuild.OpenSlots, g.Requests().Count);
            Assert.Equal(SimulatedGuild.OpenSlots, g.Helpable);
        }

        [Fact]
        public void HelpingGivesGoldOncePerRequest()
        {
            var (g, _, _, w) = Make();
            var id = g.Requests()[0].Id;
            Assert.Equal(GuildHelp.GoldPerHelp, g.Help(id, w));
            Assert.Equal(-1, g.Help(id, w));
            Assert.Equal(GuildHelp.GoldPerHelp, w.Get(SurvIds.ResSoft));
            Assert.Equal(SimulatedGuild.OpenSlots - 1, g.Helpable);
        }

        [Fact]
        public void HelpAllHelpsEveryOpenRequestAndRewardsAreCappedPerDay()
        {
            var (g, _, c, w) = Make();
            var (n, gold) = g.HelpAll(w);
            Assert.Equal(SimulatedGuild.OpenSlots, n);
            Assert.Equal(n * GuildHelp.GoldPerHelp, gold);
            Assert.Equal(0, g.Helpable);

            var total = gold;
            for (var i = 0; i < 40; i++)
            {
                c.Now += SimulatedGuild.PostEvery;
                total += g.HelpAll(w).Gold;
            }

            Assert.True(total <= GuildHelp.RewardedHelpsPerDay * GuildHelp.GoldPerHelp * 2);
            Assert.Equal(total, w.Get(SurvIds.ResSoft));
        }

        [Fact]
        public void AskedTimerIsShortenedAsHelpsArriveUpToTheCap()
        {
            var (g, q, c, _) = Make();
            Assert.True(q.TryStart("base.timer.farm", "farm", 10 * 3600, out _));
            Assert.True(g.Ask("base.timer.farm"));
            Assert.False(g.Ask("base.timer.farm"));
            var before = q.Remaining("base.timer.farm");

            c.Now += SimulatedGuild.IncomingEvery * 3;
            Assert.Equal(3, g.Tick());
            Assert.Equal(before - (SimulatedGuild.IncomingEvery * 3) - (3 * 360), q.Remaining("base.timer.farm"), 3);

            c.Now += 3600;
            Assert.Equal(GuildHelp.MaxIncoming - 3, g.Tick());
            Assert.Equal(GuildHelp.MaxIncoming, g.Received("base.timer.farm"));
            Assert.Equal(0, g.Tick());
        }

        [Fact]
        public void ShortTimersGetAtLeastAMinutePerHelpAndFinishedAsksAreDropped()
        {
            var (g, q, c, _) = Make();
            q.TryStart("t", "x", 200, out _);
            g.Ask("t");
            c.Now += SimulatedGuild.IncomingEvery;
            g.Tick();
            Assert.Equal(200 - 90 - 60, q.Remaining("t"), 3);
            c.Now += 3600;
            g.Tick();
            Assert.False(g.Asked("t"));
        }

        [Fact]
        public void SaveAndLoadKeepHelpedRequestsAsksAndTheDailyCount()
        {
            var (g, q, c, w) = Make();
            q.TryStart("t", "x", 36000, out _);
            g.Ask("t");
            var id = g.Requests()[0].Id;
            g.Help(id, w);
            c.Now += SimulatedGuild.IncomingEvery * 2;
            g.Tick();

            var g2 = new GuildHelp(new SimulatedGuild(), q);
            Assert.True(g2.Load(g.Save()));
            Assert.True(g2.HelpedByMe(id));
            Assert.True(g2.Asked("t"));
            Assert.Equal(2, g2.Received("t"));
            Assert.Equal(GuildHelp.RewardedHelpsPerDay - 1, g2.RewardsLeftToday);
            Assert.False(g2.Load("nope"));
        }
    }
}
