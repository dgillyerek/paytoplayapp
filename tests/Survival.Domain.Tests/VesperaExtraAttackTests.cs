using Survival.Domain.Enemies;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Vespera attacks 2 and 3 (2026-10-18): Mixamo shadow lash and hex rain retargeted onto her no-cape rig, wind-ups 1.5x slower,
/// purple VFX. HOLD merge until Derek Game-view PASS.
/// </summary>
public sealed class VesperaExtraAttackTests
{
    [Fact]
    public void Shadow_lash_and_hex_rain_are_baked_mixamo_clips_on_her_no_cape_rig()
    {
        var root = ArcherExtraAttackTests.FindRepoRoot();
        var design = Path.Combine(root, VesperaMotion.DesignMixamoAttacksDir);
        var sums = File.ReadAllText(Path.Combine(design, "CHECKSUMS.md5"));
        foreach (var (file, md5, pose, last) in new[]
        {
            (VesperaMotion.Attack2FileName, VesperaMotion.Attack2Md5, VesperaMotion.Attack2PoseName, VesperaMotion.Attack2LastFrame),
            (VesperaMotion.Attack3FileName, VesperaMotion.Attack3Md5, VesperaMotion.Attack3PoseName, VesperaMotion.Attack3LastFrame)
        })
        {
            var pack = Path.Combine(root, "Assets", VesperaMotion.ThemePackDir, file);
            Assert.Equal(md5, ArcherExtraAttackTests.Md5(pack));
            Assert.Equal(md5, ArcherExtraAttackTests.Md5(Path.Combine(design, file)));
            Assert.Contains(md5 + "  " + file, sums, StringComparison.Ordinal);
            var meta = File.ReadAllText(pack + ".meta");
            Assert.Contains("name: \"" + pose + "\"", meta, StringComparison.Ordinal);
            Assert.Contains("takeName: Scene", meta, StringComparison.Ordinal);
            Assert.Contains("lastFrame: " + last, meta, StringComparison.Ordinal);
            Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
            Assert.Contains(md5, meta, StringComparison.Ordinal);
            var clip = VesperaMotion.Spec.ExtraClips.Single(c => c.PoseName == pose);
            Assert.Equal(file, clip.FileName);
            Assert.Equal("Scene", clip.TakeName);
            Assert.Equal(last, clip.LastFrame);
            Assert.Null(clip.BowShots);
        }

        Assert.Contains("40b9fefb92783e25b5ff2e37320bfa4a  VESPERA_mixamo_standing_1h_magic_attack_01.fbx", sums, StringComparison.Ordinal);
        Assert.Contains("254e0dd17d9dcb8d8d6049e0384493b7  VESPERA_mixamo_standing_2h_magic_area_attack_02.fbx", sums, StringComparison.Ordinal);
        Assert.Equal(150, VesperaMotion.WindupScalePercent);
        Assert.Equal(VesperaAttack.FileName, VesperaMotion.Spec.ExtraClips[0].FileName);
        Assert.Equal(36, VesperaMotion.Spec.ExtraClips[0].LastFrame);
    }

    [Fact]
    public void Rotation_is_orb_bolt_shadow_lash_hex_rain_with_purple_impacts()
    {
        var v = LockedBattleRoster.Vespera;
        var profile = LockedBattleHits.ProfileOf("Vespera");
        Assert.Equal(BattleSide.Villain, v.Side);
        Assert.Equal(3, LockedBattleRotation.Count(v));
        Assert.Equal(VesperaMotion.AttackPoseNames, new[] { 0, 1, 2 }.Select(i => LockedBattleRotation.AttackAt(v, i).PoseName).ToArray());
        Assert.Equal(VesperaMotion.Attack2ContactFrame, LockedBattleRotation.ContactFrame(v, profile, 1));
        Assert.Equal(VesperaMotion.Attack3ContactFrame, LockedBattleRotation.ContactFrame(v, profile, 2));
        Assert.Equal(BattleImpactKind.VillainShadowLash, LockedBattleRotation.Impact(v, profile, 1));
        Assert.Equal(BattleImpactKind.VillainHexRain, LockedBattleRotation.Impact(v, profile, 2));
        Assert.Equal(3, LockedBattleRotation.HitCount(v, profile, 2));
        Assert.True(LockedBattleRotation.AttackAt(v, 2).SpreadMetres > 0f);
        var hits = new List<(int, int)>();
        var dt = 1f / 120f;
        for (var t = 0f; t < LockedBattleRotation.RotationSeconds(v); t += dt)
        {
            if (LockedBattleRotation.MeleeHitCrossed(v, profile, t, t + dt, out var a, out var h))
            {
                hits.Add((a, h));
            }
        }

        Assert.Equal(new[] { (1, 0), (2, 0), (2, 1), (2, 2) }, hits);
    }
}
