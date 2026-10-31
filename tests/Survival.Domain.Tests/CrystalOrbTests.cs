using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Taps;
using Xunit;

public class CrystalOrbTests
{
    private const double Day0 = 1_800_000_000; // fixed epoch seconds

    private static (CrystalOrb orb, double[] now) Make(int seed = 7)
    {
        var now = new[] { Day0 };
        return (new CrystalOrb(OrbConfig.Default, () => now[0], seed), now);
    }

    private static Wallet Gems(int n) => new Wallet(new Dictionary<string, int> { [SurvIds.ResHard] = n });

    [Fact]
    public void NewOrb_IsGlowing_AndRevealGivesTaskInRange()
    {
        var (orb, _) = Make();
        Assert.Equal(OrbState.Glowing, orb.State);
        Assert.True(orb.Reveal());
        Assert.Equal(OrbState.Active, orb.State);
        var def = OrbConfig.Default.Tasks.Single(t => t.Kind == orb.Task!.Kind);
        Assert.InRange(orb.Task!.Target, def.Min, def.Max);
        Assert.False(orb.Reveal());
    }

    [Fact]
    public void SameSeed_SameTasksAndChest()
    {
        var (a, _) = Make(42);
        var (b, _) = Make(42);
        a.Reveal();
        b.Reveal();
        Assert.Equal(a.Task!.Kind, b.Task!.Kind);
        Assert.Equal(a.Task.Target, b.Task.Target);
        a.Report(a.Task.Kind, 999);
        b.Report(b.Task.Kind, 999);
        Assert.Equal(a.Claim(), b.Claim());
    }

    [Fact]
    public void Report_OnlyCountsMatchingKind_AndCompletes()
    {
        var (orb, _) = Make();
        orb.Reveal();
        var kind = orb.Task!.Kind;
        var other = OrbConfig.Default.Tasks.First(t => t.Kind != kind).Kind;
        Assert.False(orb.Report(other, 100));
        Assert.Equal(0, orb.Task.Progress);
        Assert.True(orb.Report(kind, orb.Task.Target));
        Assert.Equal(OrbState.Complete, orb.State);
    }

    [Fact]
    public void Claim_PaysEconomyOnly_AndStartsCooldown()
    {
        var allowed = new HashSet<string> { SurvIds.ResSoft, SurvIds.ResWood, SurvIds.ResStone, SurvIds.ResFood, SurvIds.ResSpeedupM, SurvIds.ResHard };
        for (var seed = 0; seed < 50; seed++)
        {
            var (orb, now) = Make(seed);
            orb.Reveal();
            Assert.Null(orb.Claim());
            orb.Report(orb.Task!.Kind, 1000);
            var chest = orb.Claim()!;
            Assert.NotEmpty(chest);
            Assert.All(chest, kv => Assert.Contains(kv.Key, allowed));
            Assert.All(chest, kv => Assert.True(kv.Value > 0));
            Assert.Equal(OrbState.Resting, orb.State);
            now[0] += OrbConfig.Default.TaskEverySeconds - 1;
            Assert.Equal(OrbState.Resting, orb.State);
            now[0] += 1;
            Assert.Equal(OrbState.Glowing, orb.State);
        }
    }

    [Fact]
    public void Rerolls_FreeTwiceADay_ThenGems_AndResetNextDay()
    {
        var (orb, now) = Make();
        orb.Reveal();
        var w = Gems(20);
        var first = orb.Task!.Kind;
        Assert.Equal(OrbSpend.Ok, orb.Reroll(w));
        Assert.NotEqual(first, orb.Task!.Kind);
        Assert.Equal(OrbSpend.Ok, orb.Reroll(w));
        Assert.Equal(20, w.Get(SurvIds.ResHard));
        Assert.Equal(OrbConfig.Default.RerollGems, orb.RerollCost);
        Assert.Equal(OrbSpend.Ok, orb.Reroll(w));
        Assert.Equal(5, w.Get(SurvIds.ResHard));
        Assert.Equal(OrbSpend.NotEnoughGems, orb.Reroll(w));
        now[0] += 86400;
        Assert.Equal(2, orb.FreeRerollsLeft);
    }

    [Fact]
    public void Hasten_CostsGems_OnlyWhileResting()
    {
        var (orb, _) = Make();
        var w = Gems(100);
        Assert.Equal(OrbSpend.NotNeeded, orb.Hasten(w));
        orb.Reveal();
        orb.Report(orb.Task!.Kind, 1000);
        orb.Claim();
        Assert.Equal(OrbSpend.NotEnoughGems, orb.Hasten(Gems(1)));
        Assert.Equal(OrbSpend.Ok, orb.Hasten(w));
        Assert.Equal(100 - OrbConfig.Default.HastenGems, w.Get(SurvIds.ResHard));
        Assert.Equal(OrbState.Glowing, orb.State);
    }

    [Fact]
    public void SaveRoundTrip_KeepsTaskProgressTimerAndRerolls()
    {
        var (orb, now) = Make();
        orb.Reveal();
        orb.Reroll(Gems(0));
        orb.Report(orb.Task!.Kind, 1);
        var json = orb.Capture();
        var copy = new CrystalOrb(OrbConfig.Default, () => now[0], 1);
        Assert.True(copy.Apply(json));
        Assert.Equal(orb.Task.Kind, copy.Task!.Kind);
        Assert.Equal(orb.Task.Target, copy.Task.Target);
        Assert.Equal(orb.Task.Progress, copy.Task.Progress);
        Assert.Equal(1, copy.RerollsToday);
        Assert.False(copy.Apply("not json"));
        Assert.False(copy.Apply(""));
    }

    [Fact]
    public void ActivityFeed_ForwardsReports()
    {
        ActivityFeed.ResetForTests();
        var got = new List<(string, int)>();
        ActivityFeed.Reported += (k, n) => got.Add((k, n));
        ActivityFeed.Report(ActivityKinds.Harvest, 2);
        Assert.Equal(new[] { (ActivityKinds.Harvest, 2) }, got);
        ActivityFeed.ResetForTests();
    }
}
