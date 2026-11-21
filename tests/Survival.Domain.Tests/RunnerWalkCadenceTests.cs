using Survival.Domain.Roster;
using Survival.Domain.Runner;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-11-19 (phone, test-apk-20261119): the wolf (Nightfang) "stays in its idle pose with its feet not moving while
/// it comes at me". The sim walks it in at 4.05 m/s and the view played its 0.63 m/s trot at 1x, so it glided. The runner now
/// plays each foe's walk loop at the sim's ground speed over the clip's own, within a band. The cap is 2x
/// (Derek 2026-11-20: the wolf's legs at 3x were "a little fast" and the front paws shuffled).
/// </summary>
public sealed class RunnerWalkCadenceTests
{
    private static RunnerUnitDef Unit(string id) => Runner.Catalog().Units[id];

    [Fact]
    public void Nightfang_trots_at_the_fastest_cadence_instead_of_gliding_at_1x()
    {
        var wolf = Unit("nightfang");
        Assert.True(LockedBattleRoster.TryFind(wolf.Roster, out var fighter));
        var clip = RunnerWalkCadence.ClipGroundSpeed(fighter, wolf.Scale);
        Assert.InRange(clip, 0.6f, 0.66f);
        Assert.True(wolf.Speed / clip > 6f, "the sim moves the wolf over 6x its trot's ground speed");
        Assert.Equal(RunnerWalkCadence.MaxRate, RunnerWalkCadence.ForFoe(wolf));
    }

    [Fact]
    public void Every_shipped_walking_foe_plants_its_feet_except_where_its_clip_is_too_slow_for_the_band()
    {
        var clamped = new List<string>();
        foreach (var u in Runner.Catalog().Units.Values)
        {
            if (u.Speed <= 0f || !LockedBattleRoster.TryFind(u.Roster, out var fighter))
            {
                continue;
            }

            var clip = RunnerWalkCadence.ClipGroundSpeed(fighter, u.Scale);
            var rate = RunnerWalkCadence.ForFoe(u);
            Assert.InRange(rate, RunnerWalkCadence.MinRate, RunnerWalkCadence.MaxRate);
            if (clip <= 0f)
            {
                // A root-carrying walk (Blightroot): the stride hold pins it; its rate is left alone.
                Assert.Equal(1f, rate);
                continue;
            }

            var shown = clip * rate;
            if (System.Math.Abs(shown - u.Speed) > 1e-3f)
            {
                clamped.Add(u.Roster);
            }
        }

        // Nightfang needs a run / gallop clip (Design) to plant at 4.05 m/s; everything else steps at its sim speed.
        Assert.Equal(new[] { FighterNames.Nightfang }, clamped);
    }

    [Fact]
    public void A_chasing_foe_steps_at_the_squads_pace_when_that_is_slower()
    {
        var wolf = Unit("nightfang");
        Assert.True(LockedBattleRoster.TryFind(wolf.Roster, out var fighter));
        var clip = RunnerWalkCadence.ClipGroundSpeed(fighter, wolf.Scale);
        Assert.Equal(1.7f, RunnerWalkCadence.FoeGroundSpeed(wolf.Speed, chasing: true, levelSpeed: 1.7f));
        Assert.Equal(wolf.Speed, RunnerWalkCadence.FoeGroundSpeed(wolf.Speed, chasing: false, levelSpeed: 1.7f));
        // 1.7 / 0.63 is ~2.7x, past the 2x readable cap on this trot, so a chase is held at MaxRate too.
        Assert.Equal(RunnerWalkCadence.MaxRate, RunnerWalkCadence.ForFoe(wolf, chasing: true, levelSpeed: 1.7f));
    }

    [Fact]
    public void Unknown_or_still_speeds_leave_the_loop_at_1x_and_the_band_holds()
    {
        Assert.Equal(1f, RunnerWalkCadence.Rate(0f, 0.6f));
        Assert.Equal(1f, RunnerWalkCadence.Rate(2f, 0f));
        Assert.Equal(RunnerWalkCadence.MinRate, RunnerWalkCadence.Rate(0.1f, 2f));
        Assert.Equal(RunnerWalkCadence.MaxRate, RunnerWalkCadence.Rate(20f, 1f));
        Assert.Equal(1f, RunnerWalkCadence.ForFoe(Unit("ironhowl")));
        Assert.Equal(1f, RunnerWalkCadence.ForFoe(Unit("vorgath")));
    }

    [Fact]
    public void The_runner_view_drives_the_walk_rate_of_every_foe_actor_that_has_a_walk_loop()
    {
        var root = Layouts.RepoRoot();
        var unity = Path.Combine(root, "Assets", "Survival", "Unity");
        var view = File.ReadAllText(Path.Combine(unity, "Runner", "RunnerLevelView.cs"));
        Assert.Contains("rig.SetWalkRate(RunnerWalkCadence.Rate(", view);
        var rig = File.ReadAllText(Path.Combine(unity, "Runner", "RunnerRig.cs"));
        // The shared Blender-rig actor (Nightfang, Vespera, Ashwyrm), Bonequill and the new-char actor (Malzareth).
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(rig, @"rig\._walkRate = v => a\.WalkRate = v;").Count);
        var player = File.ReadAllText(Path.Combine(unity, "BlenderRigPlayer.cs"));
        Assert.Contains("(walking ? WalkRate : 1f)", player);
        Assert.Contains("(_baseIsWalk ? WalkRate : 1f)", player);
    }
}
