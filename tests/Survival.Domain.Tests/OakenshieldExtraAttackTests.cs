using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Oakenshield attack 3 (2026-10-18): Mixamo root snare retargeted onto her approved Blender rig, wired as the second entry of
/// her rotation with a hero-blue root burst. Mixamo "Stabbing" was rejected for attack 2. HOLD merge until Derek Game-view PASS.
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
    public void Rotation_is_spear_throw_then_root_snare_landing_once_on_its_frame()
    {
        var oak = LockedBattleRoster.Oakenshield;
        var profile = LockedBattleHits.ProfileOf("Oakenshield");
        Assert.Equal(2, LockedBattleRotation.Count(oak));
        Assert.Equal(OakenshieldMotion.AttackPoseNames, new[] { 0, 1 }.Select(i => LockedBattleRotation.AttackAt(oak, i).PoseName).ToArray());
        Assert.Equal("thorn spear throw", LockedBattleRotation.AttackAt(oak, 0).Label);
        Assert.Equal(OakenshieldMotion.Attack3ContactFrame, LockedBattleRotation.ContactFrame(oak, profile, 1));
        Assert.Equal(BattleImpactKind.HeroRootSnare, LockedBattleRotation.Impact(oak, profile, 1));
        var hits = new List<int>();
        var dt = 1f / 120f;
        for (var t = 0f; t < LockedBattleRotation.RotationSeconds(oak); t += dt)
        {
            if (LockedBattleRotation.MeleeHitCrossed(oak, profile, t, t + dt, out var index))
            {
                hits.Add(index);
            }
        }

        Assert.Equal(new[] { 1 }, hits);
    }
}
