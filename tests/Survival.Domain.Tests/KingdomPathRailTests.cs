using Survival.Domain.View;
using Xunit;

namespace Survival.Domain.Tests
{
    public class KingdomPathRailTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void AlwaysOnRail(int n) => Assert.True(RailActionable.KingdomPath(n));

        [Fact]
        public void FreeBadgeOnlyWithClaimable()
        {
            Assert.False(RailActionable.KingdomPathFree(0));
            Assert.True(RailActionable.KingdomPathFree(1));
        }
    }
}
