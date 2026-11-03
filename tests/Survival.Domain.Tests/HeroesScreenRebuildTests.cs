using Survival.Domain.Campaign;
using Xunit;

namespace Survival.Domain.Tests
{
    public class HeroesScreenRebuildTests
    {
        [Fact]
        public void ClosedScreenBuildsShell() => Assert.Equal(HeroesRebuild.Shell, HeroesScreen.Plan(false, false, false));

        [Fact]
        public void HeroTapOnlySwapsDetail() => Assert.Equal(HeroesRebuild.DetailOnly, HeroesScreen.Plan(true, false, false));

        [Fact]
        public void FoeTapOnlySwapsDetail() => Assert.Equal(HeroesRebuild.DetailOnly, HeroesScreen.Plan(true, true, true));

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void TabSwitchRedrawsBodyNotShell(bool shown, bool now) => Assert.Equal(HeroesRebuild.Body, HeroesScreen.Plan(true, shown, now));
    }
}
