using System.Collections.Generic;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.TrophyHall;
using Xunit;

namespace Survival.Domain.Tests
{
    public class TrophyHallTests
    {
        private static (TrophyHall.TrophyHall h, Dictionary<string, int> p, Wallet w) Make()
        {
            var p = new Dictionary<string, int>();
            var h = new TrophyHall.TrophyHall(k => p.TryGetValue(k, out var v) ? v : 0);
            return (h, p, new Wallet());
        }

        [Fact]
        public void NothingReadyAtStart()
        {
            var (h, _, w) = Make();
            Assert.Equal(0, h.ReadyCount);
            Assert.Null(h.Claim("build_3", w));
        }

        [Fact]
        public void ReachingGoalMakesItClaimableOnce()
        {
            var (h, p, w) = Make();
            p[TrophyKinds.BuildingUpgrades] = 3;
            Assert.Equal(1, h.ReadyCount);
            Assert.NotNull(h.Claim("build_3", w));
            Assert.Equal(5000, w.Get(SurvIds.ResSoft));
            Assert.Null(h.Claim("build_3", w));
            Assert.Equal(0, h.ReadyCount);
        }

        [Fact]
        public void RowAdvancesToNextTier()
        {
            var (h, p, w) = Make();
            p[TrophyKinds.HeroStars] = 3;
            Assert.Equal(2, h.ReadyCount);
            Assert.Equal("stars_2", h.NextOf(TrophyKinds.HeroStars)!.Id);
            h.Claim("stars_2", w);
            Assert.Equal("stars_3", h.NextOf(TrophyKinds.HeroStars)!.Id);
            Assert.Equal((1, 3), h.TiersOf(TrophyKinds.HeroStars));
        }

        [Fact]
        public void ProgressIsClampedToTarget()
        {
            var (h, p, _) = Make();
            p[TrophyKinds.BattlesWon] = 999;
            Assert.Equal(5, h.Current(h.NextOf(TrophyKinds.BattlesWon)!));
        }

        [Fact]
        public void ClaimsSurviveSaveLoad()
        {
            var (h, p, w) = Make();
            p[TrophyKinds.RunnerCleared] = 10;
            h.Claim("runner_3", w);
            h.Claim("runner_10", w);
            var (h2, p2, _) = Make();
            Assert.True(h2.Load(h.Save()));
            Assert.True(h2.IsClaimed("runner_10"));
            p2[TrophyKinds.RunnerCleared] = 10;
            Assert.Equal(0, h2.ReadyCount);
        }

        [Fact]
        public void EveryKindHasAscendingTargets()
        {
            var last = new Dictionary<string, int>();
            foreach (var t in TrophyHall.TrophyHall.DefaultTrophies)
            {
                Assert.True(t.Target > (last.TryGetValue(t.Kind, out var v) ? v : 0), t.Id);
                last[t.Kind] = t.Target;
            }
        }
    }
}
