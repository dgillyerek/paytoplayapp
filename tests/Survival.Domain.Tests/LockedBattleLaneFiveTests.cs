using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Lane 5 (Derek 2026-10-15): Sir Aldric vs Blightroot.</summary>
public sealed class LockedBattleLaneFiveTests
{
    [Fact]
    public void Both_fighters_have_body_boxes_and_melee_hits_that_land_inside_their_attack_clip()
    {
        var aldric = LockedBattleHits.ProfileOf("Sir Aldric");
        var blight = LockedBattleHits.ProfileOf("Blightroot");
        Assert.Equal(BattleImpactKind.SwordSteel, aldric.Impact);
        Assert.Equal(BattleImpactKind.BlightRot, blight.Impact);
        Assert.False(aldric.Projectile);
        Assert.False(blight.Projectile);
        Assert.Equal("mixamorig:RightHand", aldric.StrikeBone);
        Assert.Equal("mixamorig:LeftHand", blight.StrikeBone);

        // Boxes sit inside the measured meshes: Aldric 1.80 m plate, Blightroot 1.90 m.
        Assert.InRange(aldric.Box.Height, 1.6f, LockedBattleRoster.SirAldric.DesignHeightMetres);
        Assert.InRange(blight.Box.Height, 1.7f, LockedBattleRoster.Blightroot.DesignHeightMetres);
        Assert.True(aldric.Box.HalfWidth > 0.2f && aldric.Box.Front > 0.1f);
        Assert.True(blight.Box.HalfWidth > 0.3f && blight.Box.Front > 0.2f);

        // Contact frames fall inside the moving part of each attack.
        Assert.True(aldric.MeleeContactSeconds < LockedBattleRoster.SirAldric.AttackSeconds);
        Assert.True(blight.MeleeContactSeconds < LockedBattleRoster.Blightroot.AttackSeconds);
        Assert.Equal(16, LockedBattleHits.SirAldricContactFrame);
        Assert.Equal(42, LockedBattleHits.BlightrootContactFrame);

        // One hit per attack cycle, never in the rest gap. Sir Aldric still loops his one attack;
        // Blightroot's rotation is covered in LockedBattleRotationTests.
        foreach (var (fighter, profile) in new[] { (LockedBattleRoster.SirAldric, aldric) })
        {
            var hits = 0;
            var dt = 1f / 120f;
            for (var t = 0f; t < fighter.AttackCycleSeconds * 3f; t += dt)
            {
                if (LockedBattleHits.MeleeHitCrossed(fighter, profile, t, t + dt))
                {
                    hits++;
                    var inCycle = (t + dt) % fighter.AttackCycleSeconds;
                    Assert.True(LockedBattleRoster.InAttackClip(fighter, t + dt), fighter.Name);
                    Assert.InRange(inCycle, profile.MeleeContactSeconds - 0.02f, profile.MeleeContactSeconds + dt + 0.02f);
                }
            }

            Assert.Equal(3, hits);
        }
    }

    [Fact]
    public void Strikes_stop_on_the_defenders_body_box_at_attack_range()
    {
        // Aldric stops 1.4 m from Blightroot. On the contact frame his blade tip is 0.94 m ahead
        // (measured on the Lite clip with the sword in his right hand), which meets Blightroot's
        // front face; the burst lands on that face, never past it.
        var range = LockedBattleRoster.SirAldric.AttackRangeMetres;
        var box = LockedBattleHits.Blightroot.Box;
        const float tipReach = 0.94f;
        var bladeTipZ = range - tipReach; // in Blightroot's frame, in front of it
        Assert.InRange(bladeTipZ, box.Front - 0.1f, box.Front + 0.1f);
        Assert.True(LockedBattleHits.ProjectileSegmentHits(box, 0f, 1.2f, range, 0f, 1.2f, box.Front - 0.05f, out var t));
        Assert.InRange(range + ((box.Front - 0.05f - range) * t), box.Front - 1e-3f, box.Front + 1e-3f);

        // Blightroot's claw lunges 1.36 m ahead: from its 1.6 m stop (or closer, once Aldric has
        // walked in to 1.4 m) it reaches Aldric's front face.
        var claw = LockedBattleRoster.Blightroot.AttackRangeMetres - 1.36f;
        Assert.True(LockedBattleRoster.SirAldric.AttackRangeMetres - 1.36f <= LockedBattleHits.SirAldric.Box.Front);
        Assert.True(claw <= LockedBattleHits.SirAldric.Box.Front);
        Assert.True(LockedBattleHits.SirAldric.Box.Contains(0f, 1.0f, claw));

        // A hand that stops short still puts the burst on the box surface (closest point), not in the air.
        box.ClosestPoint(0f, 1.2f, 1.5f, out var cx, out var cy, out var cz);
        Assert.Equal(box.Front, cz, 4);
        Assert.Equal(1.2f, cy, 4);
        Assert.Equal(0f, cx, 4);
    }

    [Fact]
    public void Each_fighter_has_its_own_impact_burst_in_its_colours_matching_the_existing_style()
    {
        var root = FindRepoRoot();
        var impact = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleImpact.cs"));
        Assert.Contains("case BattleImpactKind.SwordSteel:", impact, StringComparison.Ordinal);
        Assert.Contains("case BattleImpactKind.BlightRot:", impact, StringComparison.Ordinal);
        Assert.Contains("private void BuildSwordSteel(", impact, StringComparison.Ordinal);
        Assert.Contains("private void BuildBlightRot(", impact, StringComparison.Ordinal);

        // #1b3060 is (27, 48, 96).
        Assert.Equal(27f / 255f, LockedBattleHits.SirAldricBlueRgb[0], 4);
        Assert.Equal(48f / 255f, LockedBattleHits.SirAldricBlueRgb[1], 4);
        Assert.Equal(96f / 255f, LockedBattleHits.SirAldricBlueRgb[2], 4);
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleDemo.cs"));
        Assert.Contains("LockedBattleHits.SirAldricBlueRgb", demo, StringComparison.Ordinal);
        Assert.Contains("case \"Blightroot\":", demo, StringComparison.Ordinal);
        Assert.Contains("LockedBattleImpact.Spawn(LockedBattleRotation.Impact(attacker.Data, attacker.Profile, attackIndex)", demo, StringComparison.Ordinal);

        // Every impact kind is distinct per fighter.
        var kinds = LockedBattleRoster.All.Select(f => LockedBattleHits.ProfileOf(f.Name).Impact).ToArray();
        Assert.Equal(kinds.Length, kinds.Distinct().Count());
    }

    [Fact]
    public void Cloth_rule_no_loose_cloth_on_either_lane_five_fighter()
    {
        var root = FindRepoRoot();
        var aldric = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        // The soft cape exists as a leftover helper but is never attached: tight plate only.
        Assert.Contains("private static void AttachSoftCape(GameObject root)", aldric, StringComparison.Ordinal);
        Assert.DoesNotContain("AttachSoftCape(_", aldric, StringComparison.Ordinal);
        Assert.DoesNotContain("AttachSoftCape(root)", aldric, StringComparison.Ordinal);
        Assert.DoesNotContain(" Cloth>", aldric, StringComparison.Ordinal);
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleDemo.cs"));
        Assert.DoesNotContain("Cloth", demo, StringComparison.Ordinal);
        var blight = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlightrootActor.cs"));
        Assert.DoesNotContain("Cloth", blight, StringComparison.Ordinal);
    }

    [Fact]
    public void Lane_five_actors_play_battle_poses_without_touching_the_battle_host_transform()
    {
        var root = FindRepoRoot();
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleDemo.cs"));
        Assert.Contains("actor.PlayBattlePose(SirAldricBattlePoses.Walk)", demo, StringComparison.Ordinal);
        Assert.Contains("f.Bind(actor.PlayBattlePose, () => actor.BattlePose", demo, StringComparison.Ordinal);
        Assert.Contains("actor.FaceWalkForward()", demo, StringComparison.Ordinal);
        Assert.Contains("DestroyNamed(\"AldricKeyLight\")", demo, StringComparison.Ordinal);
        Assert.Contains("RenderSettings.reflectionIntensity = reflections", demo, StringComparison.Ordinal);

        var aldric = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        Assert.Contains("public bool PlayBattlePose(string pose)", aldric, StringComparison.Ordinal);
        Assert.Contains("!_battleWalkOnly", aldric, StringComparison.Ordinal);
        var blight = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlightrootActor.cs"));
        Assert.Contains("public bool PlayBattlePose(string pose)", blight, StringComparison.Ordinal);
        Assert.Contains("public float FaceWalkForward()", blight, StringComparison.Ordinal);

        Assert.Equal("mutant idle", BlightrootBattlePoses.ClipFor("rest"));
        Assert.Equal("mutant walking", BlightrootBattlePoses.ClipFor("walk"));
        Assert.Equal("mutant swiping", BlightrootBattlePoses.ClipFor("attack"));
        Assert.Equal("mutant idle", BlightrootBattlePoses.ClipFor("anything"));

        var ground = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleGround.cs"));
        Assert.Contains("lane < LockedBattleRoster.LaneCount", ground, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("repo root");
    }
}
