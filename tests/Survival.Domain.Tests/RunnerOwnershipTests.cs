using System.Collections.Generic;
using System.IO;
using System.Linq;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Campaign;
using Survival.Domain.Economy;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Derek 2026-10-27: "the runner characters shouldn't be available until they've been purchased in the main level".</summary>
public class RunnerOwnershipTests
{
    private const string Oak = "oakenshield";

    private static CampaignSession FreshSave()
    {
        var catalog = CampaignCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, CampaignCatalog.FileName)));
        return new CampaignSession(catalog, new Wallet(), new ManualClock(1000));
    }

    /// <summary>Plays the whole level steering into every hero chest; returns every unit that ever stood in the squad.</summary>
    private static HashSet<string> Fielded(RunnerRun run)
    {
        var seen = new HashSet<string> { run.Hero.Unit.Id };
        for (var i = 0; i < 30 * 600 && !run.Over; i++)
        {
            var next = run.Level.Rows.FirstOrDefault(r => r.Box is { GivesUnits: true } && r.At > run.Distance);
            run.SteerTo(next?.X ?? 0f);
            run.Step(1f / 30f);
            foreach (var m in run.Squad)
            {
                seen.Add(m.Unit.Id);
            }
        }

        return seen;
    }

    private static IEnumerable<RunnerLevelDef> OakLevels(RunnerCatalog c) =>
        c.Levels.Where(l => l.Rows.Any(r => r.Box is { GivesUnits: true } b && b.Unit == Oak));

    [Fact]
    public void A_fresh_save_fields_only_its_owned_starters_and_never_an_unbought_hero()
    {
        var c = Survival.Domain.Tests.Runner.Catalog();
        var campaign = FreshSave();
        Assert.False(campaign.Roster.IsOwned("hero.oakenshield"));
        var loadout = RunnerLoadout.From(c, campaign.Roster);

        Assert.False(loadout.Fields(c.Unit(Oak)));
        Assert.Contains("lyra", RunnerOwnership.FieldedUnits(c, loadout));
        Assert.True(OakLevels(c).Any(), "the shipped levels hand out Oakenshield somewhere");
        foreach (var level in c.Levels)
        {
            var run = new RunnerRun(c, level, loadout);
            // A valid starter leads every level, and no chest names a hero the save has not bought.
            Assert.True(loadout.Fields(run.Hero.Unit));
            Assert.DoesNotContain(run.Level.Rows, r => r.Box is { GivesUnits: true } b && !loadout.Fields(c.Unit(b.Unit)));
            Assert.Equal(level.Rows.Count, run.Level.Rows.Count);
        }

        foreach (var level in OakLevels(c).Take(3))
        {
            Assert.DoesNotContain(Oak, Fielded(new RunnerRun(c, level, loadout)));
        }
    }

    [Fact]
    public void Recruiting_a_hero_in_the_kingdom_makes_it_available_in_the_runner()
    {
        var c = Survival.Domain.Tests.Runner.Catalog();
        var campaign = FreshSave();
        campaign.Wallet.Add(SurvIds.ResHard, 10000);
        Assert.Equal(UnlockCheck.Ok, campaign.Roster.TryUnlockWithGems("hero.oakenshield"));

        // Same save round trip the runner scene reads.
        var reloaded = FreshSave();
        Assert.True(reloaded.Load(campaign.Save()));
        var loadout = RunnerLoadout.From(c, reloaded.Roster);
        Assert.True(loadout.Fields(c.Unit(Oak)));

        var level = OakLevels(c).First();
        var run = new RunnerRun(c, level, loadout);
        Assert.Same(level, run.Level);
        Assert.Contains(Oak, Fielded(run));
    }

    [Fact]
    public void Without_a_save_gate_levels_play_as_shipped()
    {
        var c = Survival.Domain.Tests.Runner.Catalog();
        foreach (var level in OakLevels(c).Take(2))
        {
            Assert.Same(level, new RunnerRun(c, level, RunnerLoadout.None).Level);
        }
    }
}
