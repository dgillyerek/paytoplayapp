using System.Collections.Generic;
using System.IO;
using System.Linq;
using Survival.Domain.Alliance;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Xunit;

public class AllianceBagTests
{
    [Fact]
    public void SimulatedGuildIsStableLedAndSortedByPower()
    {
        var a = AllianceOverview.Simulated();
        var b = AllianceOverview.Simulated();
        Assert.Equal(a.Members.Select(m => m.Name), b.Members.Select(m => m.Name));
        Assert.Equal(10, a.Members.Count);
        Assert.False(string.IsNullOrWhiteSpace(a.GuildName));
        Assert.Contains(a.Members, m => m.Role == "leader");
        Assert.Equal(a.Members.OrderByDescending(m => m.Power).Select(m => m.Power), a.Members.Select(m => m.Power));
    }

    [Fact]
    public void BagListsWalletCountsOwnedFirst()
    {
        var w = new Wallet(new Dictionary<string, int> { [SurvIds.ResHeroXp] = 3, [SurvIds.ResSpeedupM] = 45, [SurvIds.ResSoft] = 999 });
        var items = BagContents.From(w);
        Assert.Equal(SurvIds.ResSpeedupM, items[0].ResourceId);
        Assert.Equal(45, items[0].Count);
        Assert.Equal(3, items[1].Count);
        Assert.DoesNotContain(items, i => i.ResourceId == SurvIds.ResSoft);
        Assert.All(items.Skip(2), i => Assert.Equal(0, i.Count));
    }

    [Fact]
    public void UseButtonOnlyWhereAUseExists()
    {
        Assert.True(BagContents.CanUse(new BagItem(SurvIds.ResSpeedupM, 5, BagUse.SpeedUpTimer), timerRunning: true));
        Assert.False(BagContents.CanUse(new BagItem(SurvIds.ResSpeedupM, 5, BagUse.SpeedUpTimer), timerRunning: false));
        Assert.False(BagContents.CanUse(new BagItem(SurvIds.ResSpeedupM, 0, BagUse.SpeedUpTimer), timerRunning: true));
        Assert.True(BagContents.CanUse(new BagItem(SurvIds.ResHeroXp, 1, BagUse.OpenHeroes), false));
        Assert.Equal(BagUse.OpenRoad, BagContents.UseOf(SurvIds.ResRunnerBoost));
        Assert.Equal(BagUse.OpenRoad, BagContents.UseOf(SurvIds.ResEnergy));
        Assert.Equal(BagUse.None, BagContents.UseOf(SurvIds.ResAllianceGift));
        Assert.True(BagContents.CanUse(new BagItem(SurvIds.ResEnergy, 3, BagUse.OpenRoad), false));
    }

    [Fact]
    public void BottomBarAllianceAndBagAreWired()
    {
        var root = RepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "Assets/Survival/Unity/SurvivalCampaignView.AllianceBag.cs"));
        Assert.Contains("play.AllianceTapped +=", src);
        Assert.Contains("play.BagTapped += OpenBag", src);
        Assert.Contains("WireAllianceBag(play);", File.ReadAllText(Path.Combine(root, "Assets/Survival/Unity/SurvivalCampaignView.cs")));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null && !(Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repo root not found.");
    }
}
