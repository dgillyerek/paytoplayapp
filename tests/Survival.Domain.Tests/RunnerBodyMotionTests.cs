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

    }

    // Blightroot's walk as Unity measured it on Derek's laptop: 1.339 host units of hips travel per 44-frame loop
    // (0.016 sideways), hips about 1 host unit up; on the road the host is scaled 1.9 / 2.135 x 1.9.
    private const float BlightScale = 1.9f / 2.135f * 1.9f;
    private static readonly System.Numerics.Vector3 BlightHipsRest = new(0f, 1f, 0f);

    private static System.Numerics.Vector3 BlightHips(int frame)
    {
        var u = (frame % 44) / 44f;
        return new System.Numerics.Vector3(-0.016f * u, 1f + (0.03f * (float)System.Math.Sin(u * 4 * System.Math.PI)), 1.339f * u);
    }

    /// <summary>RunnerRig.Late's host turn: the hit lean (FromToRotation up to up + tilt x tan(recoil)), else the death tip.</summary>
    private static System.Numerics.Quaternion HostTurn(float leanWeight, float deathT)
    {
        // The hit comes from the squad in front, so "away" is the slot's -Z.
        var away = new System.Numerics.Vector3(0f, 0f, -1f);
        if (deathT >= 0f)
        {
            var fall = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(System.Numerics.Vector3.UnitY, away));
            var s = System.Math.Min(1f, deathT * 1.6f);
            var deg = -80f * (s * s * (3f - (2f * s)));
            return System.Numerics.Quaternion.CreateFromAxisAngle(fall, deg * (float)System.Math.PI / 180f);
        }

        var tilt = away * (LockedBattleReactions.Stagger.RootLeanScale * leanWeight);
        var to = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.UnitY + (tilt * (float)System.Math.Tan(2.5 * System.Math.PI / 180)));
        var axis = System.Numerics.Vector3.Cross(System.Numerics.Vector3.UnitY, to);
        if (axis.LengthSquared() < 1e-10f)
        {
            return System.Numerics.Quaternion.Identity;
        }

        return System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.Normalize(axis), (float)System.Math.Acos(System.Math.Clamp(to.Y, -1f, 1f)));
    }

    [Fact]
    public void Blightroot_rendered_body_never_travels_back_when_shot_while_walking_attacking_or_dying()
    {
        var scale = new System.Numerics.Vector3(BlightScale);
        var baseLocal = new System.Numerics.Vector3(0f, 0.2f, 0f);
        // The capped hit nudge, pointing back down the road.
        var nudge = new System.Numerics.Vector3(0f, 0f, -RunnerHitNudge.MaxMetres);
        var worstNew = 0f;
        var oldBack = 0f;
        var previousOld = 0f;
        for (var frame = 0; frame < 160; frame++)
        {
            // A shot lands every 9 frames (Stagger lean, the heaviest); the killing shot at frame 100 starts the fall.
            var hitAge = (frame % 9) / 30f;
            var w = LockedBattleReactions.Stagger.Weight(hitAge);
            var deathT = frame >= 100 ? System.Math.Min(1f, (frame - 100) / 30f / 0.9f) : -1f;
            var turn = HostTurn(w, deathT);
            var sink = deathT >= 0f ? 1.9f * 1.9f * 0.9f * deathT * deathT : 0f;
            var hips = BlightHips(frame);
            var host = RunnerRootPlacement.HostPosition(baseLocal, nudge * w, sink, turn, scale, BlightHipsRest, hips, true);
            var shown = RunnerRootPlacement.Rendered(host, turn, scale, hips);
            worstNew = System.Math.Min(worstNew, shown.Z);
            Assert.InRange(shown.X, -0.05f, 0.05f);

            // The old placement (8362e21): the hold subtracted along the road, the turn about the feet.
            var oldHost = baseLocal + (nudge * w) + new System.Numerics.Vector3(0f, -sink, 0f) - new System.Numerics.Vector3(0f, 0f, hips.Z * BlightScale);
            var oldZ = RunnerRootPlacement.Rendered(oldHost, turn, scale, hips).Z;
            if (frame > 100)
            {
                // Each frame the body slid back down the road (it snaps forward again on the loop wrap).
                oldBack += System.Math.Max(0f, previousOld - oldZ);
            }

            previousOld = oldZ;
        }

        Assert.True(worstNew >= -RunnerHitNudge.MaxMetres - 0.005f, "Blightroot's hips showed " + worstNew + " m back");
        // The old placement saws the falling body back down the road by the walk's whole stride per loop.
        Assert.True(oldBack > 2f, "the old placement should reproduce the bounce, slid back " + oldBack);
    }

    [Fact]
    public void Every_rig_turns_about_its_pivot_so_a_hit_or_a_fall_never_carries_its_body_back()
    {
        foreach (var height in new[] { 0.6f, 1.9f, 3.6f })
        {
            var pivot = new System.Numerics.Vector3(0f, height * 0.5f, 0f);
            var scale = System.Numerics.Vector3.One;
            for (var frame = 0; frame < 60; frame++)
            {
                var deathT = frame >= 30 ? (frame - 30) / 27f : -1f;
                var turn = HostTurn(1f, System.Math.Min(1f, deathT));
                var host = RunnerRootPlacement.HostPosition(System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero, 0f, turn, scale, pivot, pivot, false);
                var shown = RunnerRootPlacement.Rendered(host, turn, scale, pivot);
                Assert.InRange(shown.Z, -0.001f, 0.001f);
                Assert.InRange(shown.X, -0.001f, 0.001f);
            }
        }

        // At rest the host is where it always was.
        var still = RunnerRootPlacement.HostPosition(new System.Numerics.Vector3(0f, 0.3f, 0f), System.Numerics.Vector3.Zero, 0f, System.Numerics.Quaternion.Identity, new System.Numerics.Vector3(1.7f), BlightHipsRest, BlightHipsRest, true);
        Assert.True(System.Numerics.Vector3.Distance(new System.Numerics.Vector3(0f, 0.3f, 0f), still) < 1e-5f);
    }

    [Fact]
    public void Runner_rig_places_its_host_through_the_shared_root_placement()
    {
        var rig = File.ReadAllText(Path.Combine(Layouts.RepoRoot(), "Assets/Survival/Unity/Runner/RunnerRig.cs"));
        Assert.Contains("RunnerRootPlacement.HostPosition(", rig);
        Assert.Contains("PlaceHost(nudge, Height * 0.9f * t * t);", rig);
        Assert.Contains("PlaceHost(nudge, 0f);", rig);
        Assert.DoesNotContain("- hold", rig);
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

    [Fact]
    public void A_full_squad_of_ten_sits_one_row_lower_with_its_back_row_still_in_shot()
    {
        // Nine followers fill rows 1 to 3; there is no fourth row any more.
        var backRow = 0;
        for (var slot = 0; slot < RunnerRules.MaxFollowers; slot++)
        {
            backRow = System.Math.Max(backRow, RunnerFormation.RowOf(slot));
        }

        Assert.Equal(3, backRow);
        var backZ = RunnerFormation.Offset(RunnerRules.MaxFollowers - 1).Z;
        Assert.Equal(-3f * RunnerFraming.RowMetres, backZ, 3);
        var focus = RunnerFraming.FocusZ(0f, backZ);
        foreach (var boss in new[] { false, true })
        {
            var bottom = RunnerFraming.BottomGroundFromFocus(boss);
            var spare = (backZ - focus) - bottom;
            // The back row's feet stay in shot, with less than a row of empty road behind them.
            Assert.InRange(spare, 0.2f, RunnerFraming.RowMetres);
            // The old framing (halfway between the lead and the back row) left more than a whole row empty.
            var oldSpare = (backZ - ((0f + backZ) * 0.5f)) - bottom;
            Assert.True(oldSpare > RunnerFraming.RowMetres, "old spare " + oldSpare);
        }

        // The lead stays in the top half's reach: ahead of the focus by half a row.
        Assert.InRange(0f - focus, 0f, RunnerFraming.RowMetres);
    }
}
