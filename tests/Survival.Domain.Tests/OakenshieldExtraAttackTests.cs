using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Oakenshield attacks 2 and 3 (2026-10-18): Mixamo Bayonet Stab (the thorn spear turning in her grip into a two-hand line,
/// LeafEarth at the reach) and the Mixamo root snare (hero-blue root burst), retargeted onto her approved Blender rig.
/// Mixamo "Stabbing" was rejected for attack 2. HOLD merge until Derek Game-view PASS.
/// </summary>
public sealed class OakenshieldExtraAttackTests
{
    [Fact]
    public void Root_snare_is_a_baked_mixamo_clip_on_her_rig_and_the_approved_attack_is_untouched()
    {
        var root = ArcherExtraAttackTests.FindRepoRoot();
        var design = Path.Combine(root, OakenshieldMotion.DesignMixamoAttacksDir);
        var pack = Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir, OakenshieldMotion.Attack3FileName);
        var md5 = OakenshieldMotion.Attack3Md5;
        Assert.Equal(md5, ArcherExtraAttackTests.Md5(pack));
        Assert.Equal(md5, ArcherExtraAttackTests.Md5(Path.Combine(design, OakenshieldMotion.Attack3FileName)));
        var sums = File.ReadAllText(Path.Combine(design, "CHECKSUMS.md5"));
        Assert.Contains(md5 + "  " + OakenshieldMotion.Attack3FileName, sums, StringComparison.Ordinal);
        Assert.Contains("1236c2b5c5acf4821167c7ceba07df5e  OAKENSHIELD_mixamo_standing_2h_magic_area_attack_02.fbx", sums, StringComparison.Ordinal);
        var meta = File.ReadAllText(pack + ".meta");
        Assert.Contains("name: \"attack3\"", meta, StringComparison.Ordinal);
        Assert.Contains("takeName: Scene", meta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: " + OakenshieldMotion.Attack3LastFrame, meta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
        Assert.Contains(md5, meta, StringComparison.Ordinal);

        var clip = OakenshieldMotion.Spec.ExtraClips.Single(c => c.PoseName == OakenshieldMotion.Attack3PoseName);
        Assert.Equal(OakenshieldMotion.Attack3FileName, clip.FileName);
        Assert.Equal("Scene", clip.TakeName);
        Assert.Equal(OakenshieldMotion.Attack3LastFrame, clip.LastFrame);
        Assert.Null(clip.BowShots);
        Assert.Equal(OakenshieldAttack.FileName, OakenshieldMotion.Spec.ExtraClips[0].FileName);
        Assert.Equal(OakenshieldMotion.AttackMd5, ArcherExtraAttackTests.Md5(Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir, OakenshieldAttack.FileName)));
    }

    [Fact]
    public void Rotation_is_spear_throw_thrust_then_root_snare_each_thrust_and_snare_landing_once_on_its_frame()
    {
        var oak = LockedBattleRoster.Oakenshield;
        var profile = LockedBattleHits.ProfileOf("Oakenshield");
        Assert.Equal(3, LockedBattleRotation.Count(oak));
        Assert.Equal(OakenshieldMotion.AttackPoseNames, new[] { 0, 1, 2 }.Select(i => LockedBattleRotation.AttackAt(oak, i).PoseName).ToArray());
        Assert.Equal("thorn spear throw", LockedBattleRotation.AttackAt(oak, 0).Label);
        Assert.Equal(OakenshieldMotion.Attack2ContactFrame, LockedBattleRotation.ContactFrame(oak, profile, 1));
        Assert.Equal(BattleImpactKind.LeafEarth, LockedBattleRotation.Impact(oak, profile, 1));
        Assert.Equal("mixamorig:RightHand", LockedBattleRotation.StrikeBone(oak, profile, 1));
        Assert.Equal(OakenshieldMotion.Attack3ContactFrame, LockedBattleRotation.ContactFrame(oak, profile, 2));
        Assert.Equal(BattleImpactKind.HeroRootSnare, LockedBattleRotation.Impact(oak, profile, 2));
        var slide = LockedBattleRotation.AttackAt(oak, 1).ReachShift;
        Assert.InRange(slide.Metres, 0.3f, 0.7f);
        Assert.InRange(OakenshieldMotion.Attack2ContactFrame, slide.FullFrame, slide.HoldFrame);
        var hits = new List<int>();
        var dt = 1f / 120f;
        for (var t = 0f; t < LockedBattleRotation.RotationSeconds(oak); t += dt)
        {
            if (LockedBattleRotation.MeleeHitCrossed(oak, profile, t, t + dt, out var index))
            {
                hits.Add(index);
            }
        }

        Assert.Equal(new[] { 1, 2 }, hits);
    }

    [Fact]
    public void Spear_thrust_is_a_baked_mixamo_clip_and_the_spear_turns_in_her_grip_into_the_two_hand_line_at_the_reach()
    {
        var root = ArcherExtraAttackTests.FindRepoRoot();
        var design = Path.Combine(root, OakenshieldMotion.DesignMixamoAttacksDir);
        var pack = Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir, OakenshieldMotion.Attack2FileName);
        var md5 = OakenshieldMotion.Attack2Md5;
        Assert.Equal(md5, ArcherExtraAttackTests.Md5(pack));
        Assert.Equal(md5, ArcherExtraAttackTests.Md5(Path.Combine(design, OakenshieldMotion.Attack2FileName)));
        var sums = File.ReadAllText(Path.Combine(design, "CHECKSUMS.md5"));
        Assert.Contains(md5 + "  " + OakenshieldMotion.Attack2FileName, sums, StringComparison.Ordinal);
        Assert.Contains("e1650b09bea4373caec04faa0fb2e0fb  OAKENSHIELD_mixamo_bayonet_stab.fbx", sums, StringComparison.Ordinal);
        var meta = File.ReadAllText(pack + ".meta");
        Assert.Contains("name: \"attack2\"", meta, StringComparison.Ordinal);
        Assert.Contains("takeName: Scene", meta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: " + OakenshieldMotion.Attack2LastFrame, meta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
        Assert.Contains(md5, meta, StringComparison.Ordinal);

        var clip = OakenshieldMotion.Spec.ExtraClips.Single(c => c.PoseName == OakenshieldMotion.Attack2PoseName);
        Assert.Equal(OakenshieldMotion.Attack2FileName, clip.FileName);
        Assert.Null(clip.BowShots);
        var spear = clip.HeldRealign!;
        Assert.Same(OakenshieldMotion.Attack2Spear, spear);
        Assert.Contains(OakenshieldAttack.Spec.Tracks, t => t.ObjectName == spear.TrackName && t.Kind == AttackTrackKind.Held && t.Bone == "mixamorig:RightHand");
        Assert.Equal("mixamorig:LeftHand", spear.FrontBone);
        Assert.Equal(0f, spear.Weight(spear.Keys[0]));
        Assert.Equal(1f, spear.Weight(OakenshieldMotion.Attack2ContactFrame));
        Assert.Equal(0f, spear.Weight(spear.Keys[3]));
        Assert.True(spear.Keys[1] <= OakenshieldMotion.Attack2ContactFrame && OakenshieldMotion.Attack2ContactFrame <= spear.Keys[2]);
        Assert.InRange(spear.Weight(25f), 0.1f, 0.9f);
        Assert.Equal(1f, spear.Scale(OakenshieldMotion.Attack2ContactFrame));
        Assert.Equal(BlenderHeldRealign.HiddenScale, spear.Scale(0f));
        Assert.False(spear.Visible(OakenshieldMotion.Attack2LastFrame));
        Assert.All(OakenshieldMotion.Spec.ExtraClips.Where(c => c.PoseName != OakenshieldMotion.Attack2PoseName), c => Assert.Null(c.HeldRealign));

        // Blender QC of the bake (work/qc_attack2_spear_thrust_spear.json): no body polygons crossed by the spear outside the hands
        // on any frame, the point within 3 deg of the target and the shaft through the front palm at the reach.
        var qc = File.ReadAllText(Path.Combine(design, "work", "qc_attack2_spear_thrust_spear.json"));
        Assert.DoesNotMatch("\"overlap_not_hands\": [1-9]", qc);
    }
}
