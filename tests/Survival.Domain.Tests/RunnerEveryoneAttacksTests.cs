using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-23: "a lot of enimies aren't even firing". Ranged enemies used to have a 7-8 m reach against a squad
/// that kills from ~19 m, so most died before they were ever in range; they also only aimed at the nearest member,
/// who could stand outside their firing strip. Plus follower HP (every hit goes through the 25) and the
/// bottom-anchored formation.
/// </summary>
public sealed class RunnerEveryoneAttacksTests
{
    private static readonly string[] RangedFoes = { "bonequill", "vespera" };
    private static readonly string[] MeleeFoes = { "nightfang", "blightroot", "ashwyrm" };

    /// <summary>The shipped units on a bare road: an optional follower box, then one <paramref name="foe"/> the squad cannot kill.</summary>
    private static RunnerRun Arena(string foe, int followers = 0)
    {
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.CatalogDir, RunnerCatalog.FileName)))!;
        json["units"]![foe]!["hp"] = 1000000;
        json.AsObject().Remove("progression");
        var level = json["levels"]![0]!;
        level["length"] = 200;
        var rows = new JsonArray();
        if (followers > 0)
        {
            rows.Add(JsonNode.Parse(@"{ ""at"": 4, ""kind"": ""box"", ""hp"": 1, ""reward"": ""followers"", ""unit"": ""rowan"", ""count"": " + followers + " }"));
        }

        rows.Add(JsonNode.Parse(@"{ ""at"": 45, ""kind"": ""enemy"", ""x"": 0, ""unit"": """ + foe + @""" }"));
        level["rows"] = rows;
        var c = RunnerCatalog.Parse(json.ToJsonString());
        return new RunnerRun(c, c.First);
    }

    [Fact]
    public void Each_ranged_enemy_shoots_the_squad_from_range_and_the_shot_lands()
    {
        foreach (var foe in RangedFoes)
        {
            var run = Arena(foe);
            var shots = new List<float>();
            while (!run.Over && run.Seconds < 40f)
            {
                run.SteerTo(0f);
                foreach (var e in run.Step(1f / 30f))
                {
                    var f = run.Foe(e.Id);
                    if (e.Kind == RunnerEventKind.Fired && f != null && f.Unit.Id == foe)
                    {
                        shots.Add(f.Z - run.Hero.Z);
                    }
                }
            }

            Assert.True(shots.Count >= 2, foe + " fired " + shots.Count);
            // It opens fire from well out (the squad kills from ~19 m; the old 7-8 m reach was never reached).
            Assert.True(shots.Max() >= 12f, foe + " first shot from " + shots.Max());
            Assert.True(run.Hero.Hp < run.Hero.MaxHp, foe + "'s shots reach the hero");
            Assert.True(Runner.Catalog().Unit(foe).Range >= 17f, foe + " range " + Runner.Catalog().Unit(foe).Range);
        }
    }

    [Fact]
    public void Each_melee_enemy_strikes_on_contact()
    {
        foreach (var foe in MeleeFoes)
        {
            var run = Arena(foe);
            var events = Runner.Play(run, _ => 0f, 40f);
            var id = events.First(e => e.Kind == RunnerEventKind.FoeSpawned && e.Unit == foe).Id;
            var swings = events.Where(e => e.Kind == RunnerEventKind.Fired && e.Id == id).ToList();
            Assert.True(swings.Count >= 2, foe + " swung " + swings.Count);
            Assert.True(run.Hero.Hp < run.Hero.MaxHp, foe + " hurts the hero it reaches");
        }
    }

    [Fact]
    public void Ranged_enemies_on_the_shipped_levels_get_their_shots_off_and_every_squad_member_fires()
    {
        var c = Runner.Catalog();
        var spawned = RangedFoes.ToDictionary(f => f, _ => 0);
        var shooters = RangedFoes.ToDictionary(f => f, _ => 0);
        foreach (var level in c.Levels)
        {
            var run = new RunnerRun(c, level);
            var unitOf = new Dictionary<int, string>();
            var fired = new HashSet<int>();
            var joined = new Dictionary<int, float>();
            var lost = new Dictionary<int, float>();
            var levelSpawned = new HashSet<string>();
            var levelFired = new HashSet<string>();
            while (!run.Over && run.Seconds < 600f)
            {
                run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
                foreach (var e in run.Step(1f / 30f))
                {
                    switch (e.Kind)
                    {
                        case RunnerEventKind.FoeSpawned:
                            unitOf[e.Id] = e.Unit;
                            if (spawned.ContainsKey(e.Unit))
                            {
                                spawned[e.Unit]++;
                                levelSpawned.Add(e.Unit);
                            }

                            break;
                        case RunnerEventKind.Fired:
                            if (fired.Add(e.Id) && unitOf.TryGetValue(e.Id, out var u) && shooters.ContainsKey(u))
                            {
                                shooters[u]++;
                                levelFired.Add(u);
                            }

                            break;
                        case RunnerEventKind.FollowerJoined:
                            joined[e.Id] = run.Seconds;
                            break;
                        case RunnerEventKind.FollowerLost:
                            lost[e.Id] = run.Seconds;
                            break;
                    }
                }
            }

            Assert.Equal(RunnerOutcome.Won, run.Outcome);
            // Every ranged type that walks onto a road shoots on it.
            Assert.Subset(levelFired, levelSpawned);
            // The lead and every follower who walked with the squad for 2 s fired.
            Assert.Contains(run.Hero.Id, fired);
            foreach (var (id, at) in joined)
            {
                var gone = lost.TryGetValue(id, out var l) ? l : run.Seconds;
                if (gone - at >= 2f)
                {
                    Assert.True(fired.Contains(id), level.Id + ": follower " + id + " never fired");
                }
            }
        }

        foreach (var foe in RangedFoes)
        {
            Assert.True(spawned[foe] > 0, foe + " is on the roads");
            // Before: ~0% of Bonequills (0/25 on L1) and ~25% of Vesperas ever shot. Now about half to two thirds.
            Assert.True(shooters[foe] >= spawned[foe] * 0.4f, foe + " " + shooters[foe] + "/" + spawned[foe]);
        }
    }

    /// <summary>One ranged foe the squad cannot kill on a bare road, the lead steered to <paramref name="leadX"/>.</summary>
    private static (RunnerRun Run, List<RunnerShot> FoeShots, List<RunnerShot> SquadShots, List<(float X, float Z, float Ox, float Oz)> Targets) Shoot(string foe, float leadX, int followers = 0)
    {
        var run = Arena(foe, followers);
        var foeShots = new List<RunnerShot>();
        var squadShots = new List<RunnerShot>();
        var targets = new List<(float, float, float, float)>();
        var seen = new HashSet<int>();
        while (!run.Over && run.Seconds < 30f)
        {
            run.SteerTo(leadX);
            var fired = new Queue<(float X, float Z)>();
            foreach (var e in run.Step(1f / 30f))
            {
                // The Fired event carries the target's place at fire time.
                if (e.Kind == RunnerEventKind.Fired && run.Foe(e.Id) != null)
                {
                    fired.Enqueue((e.X, e.Z));
                }
            }

            foreach (var s in run.Shots)
            {
                if (seen.Add(s.Id))
                {
                    (s.ByFoe ? foeShots : squadShots).Add(s);
                    if (s.ByFoe && fired.Count > 0)
                    {
                        // Where it left the bow: back along its heading by what it has flown so far.
                        var (tx, tz) = fired.Dequeue();
                        targets.Add((tx, tz, s.X - (s.DX * s.Travelled), s.Z - (s.DZ * s.Travelled)));
                    }
                }
            }
        }

        return (run, foeShots, squadShots, targets);
    }

    [Fact]
    public void Enemy_shots_are_aimed_at_the_hero_even_a_lane_over_and_squad_shots_still_fly_straight()
    {
        foreach (var foe in RangedFoes)
        {
            // The foe walks down the middle (x 0); the lead holds 3 m to one side, well outside the old 1.5 m strip.
            var (run, foeShots, squadShots, targets) = Shoot(foe, 3f);
            Assert.True(foeShots.Count >= 2, foe + " fired " + foeShots.Count + " at a hero a lane over");
            Assert.Equal(foeShots.Count, targets.Count);
            for (var k = 0; k < foeShots.Count; k++)
            {
                var s = foeShots[k];
                var (tx, tz, fx, fz) = targets[k];
                var (ax, az) = RunnerRun.FoeAim(fx, fz, tx, tz, s.Speed, run.Level.Speed);
                var len = (float)System.Math.Sqrt((ax * ax) + (az * az));
                // Heading = the aim at fire time: straight at the target's x, diagonal across the road.
                Assert.Equal(ax / len, s.DX, 2);
                Assert.Equal(az / len, s.DZ, 2);
                Assert.True(System.Math.Sign(s.DX) == System.Math.Sign(tx - fx) && System.Math.Abs(s.DX) > 0.05f, foe + " shot dx " + s.DX + " target dx " + (tx - fx));
                Assert.Equal(1f, (s.DX * s.DX) + (s.DZ * s.DZ), 3);
            }

            // ... and they land on him.
            Assert.True(run.Hero.Hp < run.Hero.MaxHp, foe + " hits a hero a lane over");
            // Heroes are unchanged: dead straight down the road.
            Assert.NotEmpty(squadShots);
            Assert.All(squadShots, s => { Assert.Equal(0f, s.DX); Assert.Equal(1f, s.DZ); });
        }
    }

    [Fact]
    public void An_aimed_shot_is_not_homing_a_sidestep_after_it_leaves_dodges_it()
    {
        var run = Arena("bonequill");
        RunnerShot? shot = null;
        var hitsBefore = 0;
        while (!run.Over && run.Seconds < 30f && shot == null)
        {
            run.SteerTo(0f);
            run.Step(1f / 30f);
            shot = run.Shots.FirstOrDefault(s => s.ByFoe);
        }

        Assert.NotNull(shot);
        var dx = shot!.DX;
        var dz = shot.DZ;
        hitsBefore = run.Hero.MaxHp - run.Hero.Hp;
        // Step well out of its line at once: the shot keeps its heading and misses.
        for (var k = 0; k < 90 && run.Shots.Contains(shot); k++)
        {
            run.SteerTo(run.Level.HalfWidth);
            run.Step(1f / 30f);
            Assert.Equal(dx, shot.DX);
            Assert.Equal(dz, shot.DZ);
        }

        Assert.Equal(hitsBefore, run.Hero.MaxHp - run.Hero.Hp);
    }

    [Fact]
    public void Ranged_enemies_pick_the_nearest_member_in_range_in_any_lane()
    {
        var src = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets/Survival/Runtime/Runner/RunnerRun.cs"));
        Assert.DoesNotContain("NearestInStrip", src);
        Assert.DoesNotContain("AimHalfWidth", src);
    }

    [Fact]
    public void Every_follower_a_box_gives_joins_with_25_hp_whatever_its_unit()
    {
        var c = Runner.Catalog();
        var units = new HashSet<string>();
        foreach (var level in c.Levels)
        {
            var run = new RunnerRun(c, level);
            while (!run.Over && run.Seconds < 600f)
            {
                run.SteerTo(RunnerBossQuakeTests.DodgingPlayer(run));
                var batch = run.Step(1f / 30f).ToList();
                foreach (var e in batch)
                {
                    if (e.Kind != RunnerEventKind.FollowerJoined)
                    {
                        continue;
                    }

                    var m = run.Member(e.Id);
                    Assert.NotNull(m);
                    Assert.False(m!.IsHero);
                    Assert.Equal(RunnerRules.FollowerHp, m.MaxHp);
                    // Full 25, less anything that hit it later in the same frame it joined.
                    Assert.Equal(RunnerRules.FollowerHp, m.Hp + (int)batch.Where(h => h.Kind == RunnerEventKind.FollowerHit && h.Id == m.Id).Sum(h => h.Amount));
                    units.Add(m.Unit.Id);
                }
            }
        }

        // The shipped boxes hand out Rowan, Lyra and Oakenshield followers; a prize Lyra is 25 too, not her 420.
        Assert.Contains("rowan", units);
        Assert.Contains("lyra", units);
        Assert.Contains("oakenshield", units);
    }

    [Fact]
    public void An_enemy_shot_takes_hp_off_a_follower_instead_of_knocking_it_out()
    {
        var run = Arena("bonequill", followers: 3);
        var events = Runner.Play(run, _ => 0f, 40f);
        var hits = events.Where(e => e.Kind == RunnerEventKind.FollowerHit).ToList();
        Assert.NotEmpty(hits);
        // 15 off 25: the first arrow leaves a follower standing on 10.
        var first = hits[0];
        Assert.Equal(15f, first.Amount);
        Assert.True(events.IndexOf(first) < events.FindIndex(e => e.Kind == RunnerEventKind.FollowerLost && e.Id == first.Id)
            || !events.Any(e => e.Kind == RunnerEventKind.FollowerLost && e.Id == first.Id));
    }

    [Fact]
    public void A_melee_contact_hit_takes_hp_off_a_follower()
    {
        var run = Arena("nightfang", followers: 3);
        var events = Runner.Play(run, _ => 0f, 40f);
        var hit = events.First(e => e.Kind == RunnerEventKind.FollowerHit);
        Assert.Equal(20f, hit.Amount);
    }

    [Fact]
    public void Traps_and_the_boss_slam_and_quake_go_through_a_followers_hp()
    {
        Assert.Equal(4, RunnerRun.FollowerTrapDamage(10));
        Assert.Equal(RunnerRules.FollowerHp - 1, RunnerRun.FollowerTrapDamage(200));
        var src = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets/Survival/Runtime/Runner/RunnerRun.cs"));
        Assert.DoesNotContain("KillFollower", src);
        Assert.Contains("HurtFollower(h.Id, m, bite);", src);
    }

    [Fact]
    public void Squad_bars_are_always_shown_while_alive()
    {
        Assert.Equal(1f, RunnerSideBar.SquadAlpha(1));
        Assert.Equal(1f, RunnerSideBar.SquadAlpha(RunnerRules.FollowerHp));
        Assert.Equal(0f, RunnerSideBar.SquadAlpha(0));
        Assert.True(RunnerSideBar.WidthFor(true) > RunnerSideBar.WidthFor(false));
        var hud = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets/Survival/Unity/Runner/RunnerLevelView.Hud.cs"));
        Assert.Contains("bar.Group.alpha = RunnerSideBar.SquadAlpha(hp);", hud);
    }

    [Fact]
    public void The_back_row_always_sits_on_the_bottom_anchor_and_the_lead_moves_up_a_row_at_each_row_boundary()
    {
        var anchor = RunnerFraming.BackRowFromFocus;
        float? lastLead = null;
        var lastRows = 0;
        for (var n = 1; n <= RunnerRules.SquadCap; n++)
        {
            var back = RunnerFraming.SettledBackOffset(n);
            var focus = RunnerFraming.FocusZ(back);
            Assert.Equal(anchor, back - focus, 4);
            var lead = 0f - focus;
            var rows = n == 1 ? 0 : RunnerFormation.RowOf(n - 2);
            Assert.Equal(anchor + (rows * RunnerFraming.RowMetres), lead, 3);
            if (lastLead is { } prev)
            {
                var boundary = n == 2 || n == 4 || n == 7;
                Assert.Equal(boundary ? RunnerFraming.RowMetres : 0f, lead - prev, 3);
                Assert.Equal(boundary ? lastRows + 1 : lastRows, rows);
            }

            lastLead = lead;
            lastRows = rows;
        }

        // A full ten is framed as before: the back row's feet in shot with under a row spare.
        foreach (var boss in new[] { false, true })
        {
            Assert.InRange(anchor - RunnerFraming.BottomGroundFromFocus(boss), 0.2f, RunnerFraming.RowMetres);
        }

        // The view reads the members' actual places (joiners slide back into slot) and eases the camera: no snap.
        var view = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets/Survival/Unity/Runner/RunnerLevelView.cs"));
        Assert.Contains("var back = RunnerFraming.BackZ(_run.Squad, _run.Distance);", view);
        Assert.Contains("RunnerFraming.FocusZ(back)", view);
        Assert.Contains("_camFocus = Vector3.Lerp(_camFocus, Focus(), 1f - Mathf.Exp(-dt * 6f));", view);
    }
}
