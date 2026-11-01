using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-26: "the attacks from the enimies in the runner stage move too slowly once fired. make them about 25%
/// faster. Also they don't start firing from as far away as the heroes can." Ranged enemies' shots fly 25% faster, and a
/// ranged enemy opens fire as soon as it is inside the squad's reach (range crates included), with shots that fly far
/// enough to land.
/// </summary>
public sealed class RunnerEnemyRangeTests
{
    private static readonly string[] RangedFoes = { "bonequill", "vespera" };

    private static RunnerRun Arena(string foe, string? rangeCrate = null)
    {
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerCatalog.FileName)))!;
        json["units"]![foe]!["hp"] = 1000000;
        json.AsObject().Remove("progression");
        var level = json["levels"]![0]!;
        level["length"] = 200;
        var rows = new JsonArray
        {
            // A Rowan follower: the squad's longest base reach (21 m), past both ranged enemies' own 18-19 m.
            JsonNode.Parse(@"{ ""at"": 3, ""kind"": ""box"", ""x"": 0, ""hp"": 1, ""reward"": ""followers"", ""unit"": ""rowan"", ""count"": 1 }"),
        };
        if (rangeCrate != null)
        {
            rows.Add(JsonNode.Parse(@"{ ""at"": 6, ""kind"": ""box"", ""x"": 0, ""hp"": 1, ""reward"": ""range"", ""amount"": " + rangeCrate + " }"));
        }

        rows.Add(JsonNode.Parse(@"{ ""at"": 60, ""kind"": ""enemy"", ""x"": 0, ""unit"": """ + foe + @""" }"));
        level["rows"] = rows;
        var c = RunnerCatalog.Parse(json.ToJsonString());
        return new RunnerRun(c, c.First);
    }

    /// <summary>Plays until the foe's first shot; returns how far ahead of the hero it stood and the squad's reach then.</summary>
    private static (float Dz, float Reach, RunnerRun Run) FirstShot(string foe, string? rangeCrate = null)
    {
        var run = Arena(foe, rangeCrate);
        while (!run.Over && run.Seconds < 40f)
        {
            run.SteerTo(0f);
            foreach (var e in run.Step(1f / 60f))
            {
                var f = run.Foe(e.Id);
                if (e.Kind == RunnerEventKind.Fired && f != null && f.Unit.Id == foe)
                {
                    return (f.Z - run.Hero.Z, run.SquadReach, run);
                }
            }
        }

        Assert.Fail(foe + " never fired");
        return default;
    }

    [Fact]
    public void Ranged_enemy_shots_fly_25_percent_faster()
    {
        // Was 12 and 11 m/s.
        Assert.Equal(15f, Runner.Catalog().Unit("bonequill").ShotSpeed, 3);
        Assert.Equal(13.75f, Runner.Catalog().Unit("vespera").ShotSpeed, 3);
        foreach (var u in Runner.Catalog().Units.Values.Where(u => u.Ranged && u.Id is not ("lyra" or "rowan" or "oakenshield")))
        {
            Assert.Contains(u.Id, RangedFoes);
        }
    }

    [Fact]
    public void A_ranged_enemy_opens_fire_as_soon_as_it_is_inside_the_squads_reach()
    {
        foreach (var foe in RangedFoes)
        {
            var unit = Runner.Catalog().Unit(foe);
            var (dz, reach, _) = FirstShot(foe);
            Assert.Equal(21f, reach, 3);
            // Further out than its own range ever let it shoot, and within a step or two of crossing the squad's reach.
            Assert.True(dz > unit.Range + unit.Radius + 0.5f, foe + " first shot from " + dz);
            Assert.True(dz >= reach + unit.Radius - 0.5f, foe + " first shot from " + dz + ", squad reach " + reach);
        }
    }

    [Fact]
    public void A_range_crate_does_not_let_the_squad_outrange_them()
    {
        foreach (var foe in RangedFoes)
        {
            var (plain, _, _) = FirstShot(foe);
            var (crate, reach, run) = FirstShot(foe, "0.3");
            Assert.True(run.RangeMul > 1.29f, "range crate taken: " + run.RangeMul);
            Assert.True(crate > plain + 4f, foe + " first shot from " + crate + " with the crate, " + plain + " without");
            Assert.True(crate >= reach - 0.5f, foe + " first shot from " + crate + ", squad reach " + reach);
        }
    }

    [Fact]
    public void Shots_from_the_longer_range_still_reach_the_squad()
    {
        foreach (var foe in RangedFoes)
        {
            var run = Arena(foe, "0.3");
            var seen = new HashSet<RunnerShot>();
            var landed = 0;
            var hp = run.Hero.Hp;
            while (!run.Over && run.Seconds < 40f)
            {
                run.SteerTo(0f);
                run.Step(1f / 60f);
                foreach (var s in run.Shots.Where(s => s.ByFoe && seen.Add(s)))
                {
                    // Long enough for the whole fire range plus the overshoot.
                    Assert.True(s.Range >= run.SquadReach + RunnerRules.ShotOvershoot, foe + " shot range " + s.Range + " vs reach " + run.SquadReach);
                }

                if (run.Hero.Hp < hp)
                {
                    landed++;
                    hp = run.Hero.Hp;
                }
            }

            Assert.True(seen.Count >= 2, foe + " fired " + seen.Count);
            Assert.True(landed >= 1, foe + "'s long shots never landed");
        }
    }
}
