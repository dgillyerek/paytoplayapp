using Survival.Domain.Ids;
using Survival.Domain.View;
using Survival.Domain.World;
using Xunit;

public class BalanceBarTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(950, "950")]
    [InlineData(9999, "9,999")]
    [InlineData(12500, "12.5K")]
    [InlineData(250000, "250K")]
    [InlineData(999999, "999.9K")]
    [InlineData(1234567, "1.2M")]
    [InlineData(20000000, "20M")]
    public void AbbreviatesBigNumbers(int amount, string expected) => Assert.Equal(expected, ChipWallet.FormatAbbrev(amount));

    [Theory]
    [InlineData(45, "45m")]
    [InlineData(90, "1.5h")]
    [InlineData(60 * 24 * 3, "3d")]
    public void SpeedupsReadAsTime(int minutes, string expected) => Assert.Equal(expected, BalanceBarIds.Format(SurvIds.ResSpeedupM, minutes));

    [Fact]
    public void HeroScreenShowsWhatLevelUpAndRushSpend()
    {
        Assert.Contains(SurvIds.ResSoft, BalanceBarIds.Heroes);
        Assert.Contains(SurvIds.ResFood, BalanceBarIds.Heroes);
        Assert.Contains(SurvIds.ResHard, BalanceBarIds.Heroes);
        Assert.Contains(SurvIds.ResSpeedupM, BalanceBarIds.Heroes);
        Assert.Contains(SurvIds.ResHeroXp, BalanceBarIds.Heroes);
    }
}
