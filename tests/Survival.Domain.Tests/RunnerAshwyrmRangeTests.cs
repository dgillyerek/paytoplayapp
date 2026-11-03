using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-02: "the dragon needs a longer attack range on the runner". Ashwyrm breathes fire, so he stops and
/// strikes from 3.6 m of reach (double the old 1.8 m claw reach) instead of walking right into the squad. He stays a
/// melee-style foe (the hit lands on his release frame, no dodgeable shot), so his damage is unchanged.
/// </summary>
public sealed class RunnerAshwyrmRangeTests
{
    [Fact]
    public void Ashwyrm_has_double_his_old_reach_and_stays_a_close_attacker()
    {
        var u = Runner.Catalog().Unit("ashwyrm");
        Assert.Equal(3.6f, u.Range, 3);
        Assert.False(u.Ranged);
    }

    [Fact]
    public void Ashwyrm_breathes_on_the_squad_from_well_out_instead_of_walking_into_it()
    {
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerCatalog.FileName)))!;
        json["units"]!["ashwyrm"]!["hp"] = 1000000;
        json.AsObject().Remove("progression");
        var level = json["levels"]![0]!;
        level["length"] = 200;
        level["rows"] = new JsonArray { JsonNode.Parse(@"{ ""at"": 45, ""kind"": ""enemy"", ""x"": 0, ""unit"": ""ashwyrm"" }") };
        var c = RunnerCatalog.Parse(json.ToJsonString());
        var run = new RunnerRun(c, c.First);
        var gaps = new List<float>();
        while (!run.Over && run.Seconds < 40f)
        {
            run.SteerTo(0f);
            foreach (var e in run.Step(1f / 30f))
            {
                var f = run.Foe(e.Id);
                if (e.Kind == RunnerEventKind.Fired && f != null && f.Unit.Id == "ashwyrm")
                {
                    gaps.Add(f.Z - run.Hero.Z);
                }
            }
        }

        Assert.True(gaps.Count >= 2, "breathed " + gaps.Count);
        // Old reach put him ~3.2 m out (1.8 + both bodies); now he opens up past 4.5 m.
        Assert.True(gaps[0] >= 4.5f, "first breath from " + gaps[0]);
        Assert.True(run.Hero.Hp < run.Hero.MaxHp);
    }
}
