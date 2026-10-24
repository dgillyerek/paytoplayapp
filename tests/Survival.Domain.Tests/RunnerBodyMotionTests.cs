using System.Collections.Generic;
using System.IO;
using Survival.Domain.Campaign;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 2026-10-23: heroes stepped forward and back while firing; Blightroot bounced back almost off screen when hit;
/// the Heroes screen banner said "HEROES" on the Foes tab.
/// </summary>
public class RunnerBodyMotionTests
{
    private const string Hips = "Armature/mixamorig:Hips";

    [Theory]
    [InlineData("Armature/mixamorig:Hips/mixamorig:Spine", true)]
    [InlineData("Armature/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm", true)]
    [InlineData("Armature/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:Neck/mixamorig:Head", true)]
    [InlineData("Armature/Weapon", true)]
    [InlineData("Armature/mixamorig:Hips", false)]
    [InlineData("Armature", false)]
    [InlineData("", false)]
    [InlineData("Armature/mixamorig:Hips/mixamorig:LeftUpLeg/mixamorig:LeftLeg/mixamorig:LeftFoot", false)]
    [InlineData("Armature/mixamorig:Hips/mixamorig:RightUpLeg", false)]
    public void Upper_body_mask_takes_the_spine_down_and_leaves_hips_and_legs_on_the_walk(string path, bool upper)
    {
        Assert.Equal(upper, RunnerUpperBodyAttack.IsUpperBody(path, Hips, "mixamorig:Spine"));
    }

    [Fact]
    public void Shooters_hips_stay_on_the_in_place_walk_so_their_forward_offset_stays_within_a_few_cm()
    {
        // Attack takes move the hips 0.40 (Lyra), 0.12 (Rowan) and 0.17 m (Oakenshield) forward and back. Layered, the
        // hips are never in the take's mask, so they follow the walk, which is in place (no root stride).
        foreach (var name in RunnerUpperBodyAttack.Shooters)
        {
            var f = LockedBattleRoster.All.Single(x => x.Name == name);
            Assert.False(f.WalkCarriesRoot, name);
            Assert.InRange(f.WalkRootStrideMetres, 0f, 0.03f);
            Assert.False(RunnerUpperBodyAttack.IsUpperBody(Hips, Hips, "mixamorig:Spine"));
            Assert.True(RunnerUpperBodyAttack.Applies(name));
        }

        Assert.False(RunnerUpperBodyAttack.Applies("Blightroot"));
        Assert.False(RunnerUpperBodyAttack.Applies("Sir Aldric"));
    }

    [Fact]
    public void Crossfade_eases_in_and_out_over_about_a_tenth_of_a_second()
    {
        Assert.InRange(RunnerUpperBodyAttack.CrossfadeSeconds, 0.08f, 0.12f);
        Assert.Equal(0f, RunnerUpperBodyAttack.FadeIn(0f));
        Assert.Equal(1f, RunnerUpperBodyAttack.FadeIn(RunnerUpperBodyAttack.CrossfadeSeconds));
        Assert.Equal(0.5f, RunnerUpperBodyAttack.FadeIn(RunnerUpperBodyAttack.CrossfadeSeconds / 2f), 3);
        Assert.Equal(0f, RunnerUpperBodyAttack.FadeOut(1f, RunnerUpperBodyAttack.CrossfadeSeconds));
        Assert.Equal(0.8f, RunnerUpperBodyAttack.FadeOut(0.8f, 0f), 4);

        // A restart: the old take fades out as the new one fades in, so the attack layer never drops to the walk.
        for (var t = 0f; t <= RunnerUpperBodyAttack.CrossfadeSeconds; t += 0.005f)
        {
            var newer = RunnerUpperBodyAttack.FadeIn(t);
            var older = RunnerUpperBodyAttack.FadeOut(1f, t);
            Assert.InRange(1f - ((1f - newer) * (1f - older)), 0.74f, 1.0001f);
        }
    }

    [Theory]
    [InlineData("Lyra")]
    [InlineData("Rowan")]
    [InlineData("Oakenshield")]
    public void Release_frame_is_unchanged_by_the_layering(string name)
    {
        // The layered take runs on the same clock (seconds x speed through CycleFrameFor) as the full-body one: the
        // sim's release time lands on the battle's release frame.
        var spec = name switch { "Lyra" => LyraAttack.Spec, "Rowan" => RowanAttack.Spec, _ => OakenshieldAttack.Spec };
        var release = name switch { "Lyra" => LyraAttack.ReleaseFrame, "Rowan" => RowanAttack.ReleaseFrame, _ => OakenshieldAttack.ReleaseFrame };
        var clip = RunnerAttackTiming.ClipFor(name)!.Value;
        foreach (var cycle in new[] { 0.3f, 0.6f, 1.2f })
        {
            var plan = RunnerAttackTiming.Plan(clip, cycle);
            var frame = spec.CycleFrameFor(plan.Release * plan.Speed, out var inClip, out var take);
            Assert.True(inClip);
            Assert.Equal(0, take);
            Assert.Equal(release, frame, 2);
        }
    }

    [Fact]
    public void A_hit_nudges_every_enemy_a_few_cm_at_most()
    {
        foreach (var f in LockedBattleRoster.All)
        {
            var profile = LockedBattleHits.ProfileOf(f.Name);
            foreach (var reaction in new[] { LockedBattleReactions.Flinch, LockedBattleReactions.HeadSnap, LockedBattleReactions.Stagger, LockedBattleReactions.GustLean })
            {
                for (var t = 0f; t < reaction.Seconds; t += 0.01f)
                {
                    // The rig's step at full punch along the worst direction, then the cap.
                    var step = reaction.RootScale * reaction.Weight(t) * profile.RecoilMetres;
                    var x = step * 0.6f;
                    var z = step * 0.8f;
                    var k = RunnerHitNudge.Scale(x, z);
                    var shown = System.MathF.Sqrt((x * x) + (z * z)) * k;
                    Assert.True(shown <= RunnerHitNudge.MaxMetres + 1e-5f, f.Name + " " + reaction.Kind + " " + shown);
                }
            }
        }

        Assert.True(RunnerHitNudge.MaxMetres < 0.1f);
    }

    [Fact]
    public void Blightroot_stride_is_held_so_his_body_never_snaps_back_on_a_loop_wrap()
    {
        var blight = LockedBattleRoster.All.Single(x => x.Name == "Blightroot");
        Assert.True(blight.WalkCarriesRoot);
        Assert.True(RunnerStrideHold.Applies(blight));
        Assert.False(RunnerStrideHold.Applies(LockedBattleRoster.Ironhowl));
        foreach (var f in LockedBattleRoster.All)
        {
            Assert.Equal(f.WalkCarriesRoot && f.Name != "Ironhowl", RunnerStrideHold.Applies(f));
        }

        // His walk carries the hips the stride forward per loop, then wraps to 0: unheld that is a jump back of the
        // whole stride (x 1.9 scale on the road); held, the body shows at its rest place every frame.
        var scale = 1.9f;
        var previous = RunnerStrideHold.Shown(0f, 0f, true);
        for (var frame = 0; frame <= 86; frame++)
        {
            var bone = blight.WalkRootStrideMetres * scale * ((frame % 43) / 43f);
            var shown = RunnerStrideHold.Shown(0f, bone, true);
            Assert.InRange(previous - shown, -0.1f, 0.1f);
            previous = shown;
        }

        Assert.True(blight.WalkRootStrideMetres * scale - RunnerStrideHold.Shown(0f, 0f, false) > 1f);
    }

    [Fact]
    public void A_hit_never_moves_an_enemy_back_down_the_road_in_any_level()
    {
        var c = Runner.Catalog();
        foreach (var level in c.Levels)
        {
            var r = new RunnerRun(c, level);
            var before = new Dictionary<int, (float Z, bool Passed)>();
            var hits = 0;
            while (!r.Over && r.Seconds < 600f)
            {
                before.Clear();
                foreach (var f in r.Foes)
                {
                    before[f.Id] = (f.Z, f.Passed);
                }

                r.SteerTo(RunnerBossQuakeTests.DodgingPlayer(r));
                var hitIds = new HashSet<int>();
                foreach (var e in r.Step(1f / 30f))
                {
                    if (e.Kind == RunnerEventKind.Hit && !e.OnBox)
                    {
                        hitIds.Add(e.Id);
                    }
                }

                foreach (var f in r.Foes)
                {
                    if (!hitIds.Contains(f.Id) || !before.TryGetValue(f.Id, out var was) || was.Passed || f.Passed)
                    {
                        continue;
                    }

                    hits++;
                    // Foes walk toward the squad (-Z); back is +Z.
                    Assert.True(f.Z - was.Z <= 0.1f, "L" + level.Number + " foe #" + f.Id + " moved back " + (f.Z - was.Z));
                }
            }

            Assert.True(hits > 0, "L" + level.Number + " had no hits to check");
        }
    }

    [Fact]
    public void Heroes_screen_banner_follows_the_selected_tab()
    {
        Assert.Equal("campaign.heroes.title", HeroesScreen.TitleKey(false));
        Assert.Equal("campaign.foes.title", HeroesScreen.TitleKey(true));
        Assert.Equal("HEROES", HeroesScreen.TitleFallback(false));
        Assert.Equal("FOES", HeroesScreen.TitleFallback(true));
        Assert.True(HeroesScreen.TabHighlighted(false, false));
        Assert.False(HeroesScreen.TabHighlighted(true, false));
        Assert.True(HeroesScreen.TabHighlighted(true, true));
        Assert.False(HeroesScreen.TabHighlighted(false, true));

        var root = Layouts.RepoRoot();
        var strings = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "strings", "en.json"))).RootElement;
        Assert.Equal("HEROES", strings.GetProperty(HeroesScreen.TitleKey(false)).GetString());
        Assert.Equal("FOES", strings.GetProperty(HeroesScreen.TitleKey(true)).GetString());

        // The screen is rebuilt on open and on every tab tap (both tabs call OpenHeroes), and its banner reads the tab.
        var view = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SurvivalCampaignView.cs"));
        Assert.Contains("OpenScreen(HeroesScreenName, S(HeroesScreen.TitleKey(_foesTab), HeroesScreen.TitleFallback(_foesTab)))", view);
        Assert.DoesNotContain("OpenScreen(HeroesScreenName, S(\"campaign.heroes.title\"", view);
        Assert.Contains("_foesTab = false; _selected = \"\"; OpenHeroes();", view);
        Assert.Contains("_foesTab = true; _selected = \"\"; OpenHeroes();", view);
        Assert.Contains("HeroesScreen.TabHighlighted(false, _foesTab)", view);
        Assert.Contains("HeroesScreen.TabHighlighted(true, _foesTab)", view);
    }
}
