using System.Security.Cryptography;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>
/// Archer attacks 2 and 3 (2026-10-18): Mixamo Pro Longbow clips retargeted onto the approved Blender rigs, the bow only rolling
/// in the locked grip, arrows landing through explicit hit frames after a flight streak. HOLD merge until Derek Game-view PASS.
/// </summary>
public sealed class ArcherExtraAttackTests
{
    [Fact]
    public void Rowan_double_shot_and_volley_are_baked_mixamo_clips_on_his_rig()
    {
        Assert.Equal(new[] { "rest", "walk", "attack", "attack2", "attack3" }, RowanMotion.PoseNames);
        AssertExtraClip(RowanMotion.Spec, RowanMotion.ThemePackDir, RowanMotion.DesignMixamoAttacksDir, RowanMotion.Attack2PoseName,
            RowanMotion.Attack2FileName, RowanMotion.Attack2Md5, RowanMotion.Attack2LastFrame, RowanMotion.Attack2BowShots);
        AssertExtraClip(RowanMotion.Spec, RowanMotion.ThemePackDir, RowanMotion.DesignMixamoAttacksDir, RowanMotion.Attack3PoseName,
            RowanMotion.Attack3FileName, RowanMotion.Attack3Md5, RowanMotion.Attack3LastFrame, RowanMotion.Attack3BowShots);
        var sums = File.ReadAllText(Path.Combine(FindRepoRoot(), RowanMotion.DesignMixamoAttacksDir, "CHECKSUMS.md5"));
        Assert.Contains("7c18cbd67933dae68ebaee6b2931630b  ROWAN_mixamo_standing_draw_arrow.fbx", sums, StringComparison.Ordinal);
        Assert.Contains("ecc0ca95c858ee1083294ba57614d994  ROWAN_mixamo_standing_aim_recoil.fbx", sums, StringComparison.Ordinal);
        Assert.Contains("6bf0a7e22a2a3dffe5e8d7707bf11ca4  ROWAN_mixamo_standing_aim_overdraw.fbx", sums, StringComparison.Ordinal);

        // The approved rest, walk and attack are untouched.
        Assert.Equal("f50ab0b2ed1560515c8242fca9f87cb6", RowanAttack.FileMd5);
        Assert.Equal(RowanAttack.FileName, RowanMotion.Spec.ExtraClips[0].FileName);
    }

    [Fact]
    public void Rowan_bow_only_rolls_in_the_grip_per_shot_and_the_arrow_leaves_at_each_release()
    {
        var grip = RowanAttack.BowGrip;
        foreach (var shots in new[] { RowanMotion.Attack2BowShots, RowanMotion.Attack3BowShots })
        {
            Assert.Equal("ROWAN_arrow", shots.ArrowTrack);
            Assert.True(shots.ArrowRest[0] > 0.024f, "arrow rest beside the riser on his left");
            Assert.Equal(grip.RollRestDeg, grip.RollDeg(shots, 0f));
            foreach (var s in shots.Shots)
            {
                Assert.True(s.ArrowInHand <= s.Nock && s.Nock < s.Release);
                Assert.True(s.RollA < s.RollB && s.RollB <= s.Release && s.Release < s.RollC && s.RollC < s.RollE);
                Assert.Equal(grip.RollDrawDeg, grip.RollDeg(shots, s.Release));
                Assert.True(shots.Nocked(s.Release - 1));
                Assert.False(shots.ArrowHeld(s.Release));
                Assert.Equal(grip.RollRestDeg, grip.RollDeg(shots, s.RollE));
            }
        }

        Assert.Equal(new[] { 44, 80 }, RowanMotion.Attack2BowShots.Shots.Select(s => s.Release).ToArray());
        Assert.Equal(RowanMotion.Attack3ReleaseFrame, RowanMotion.Attack3BowShots.Shots.Single().Release);
        Assert.Equal(grip.RollRestDeg, grip.RollDeg(RowanMotion.Attack2BowShots, RowanMotion.Attack2LastFrame));
        Assert.Equal(grip.RollRestDeg, grip.RollDeg(RowanMotion.Attack3BowShots, RowanMotion.Attack3LastFrame));
    }

    [Fact]
    public void Rowan_rotates_bow_shot_double_shot_volley_with_flight_streaks_before_each_hit()
    {
        var rowan = LockedBattleRoster.Rowan;
        var profile = LockedBattleHits.ProfileOf("Rowan");
        Assert.True(profile.Projectile);
        Assert.Equal(3, LockedBattleRotation.Count(rowan));
        Assert.Equal(RowanMotion.AttackPoseNames, new[] { 0, 1, 2 }.Select(i => LockedBattleRotation.AttackAt(rowan, i).PoseName).ToArray());
        Assert.Equal(-1, LockedBattleRotation.ContactFrame(rowan, profile, 0));
        Assert.Equal(BattleImpactKind.RangerArrow, LockedBattleRotation.Impact(rowan, profile, 1));
        Assert.Equal(BattleImpactKind.RangerArrow, LockedBattleRotation.Impact(rowan, profile, 2));
        Assert.Equal(2, LockedBattleRotation.HitCount(rowan, profile, 1));
        Assert.Equal(3, LockedBattleRotation.HitCount(rowan, profile, 2));
        Assert.True(LockedBattleRotation.AttackAt(rowan, 2).SpreadMetres > 0f);
        Assert.True(LockedBattleRotation.AttackAt(rowan, 2).FlightLoftMetres > 0f);

        var hits = new List<(int, int)>();
        var loosed = new List<(int, int, float)>();
        var dt = 1f / 120f;
        for (var t = 0f; t < LockedBattleRotation.RotationSeconds(rowan); t += dt)
        {
            if (LockedBattleRotation.MeleeHitCrossed(rowan, profile, t, t + dt, out var a, out var h))
            {
                hits.Add((a, h));
            }

            if (LockedBattleRotation.ShotLoosed(rowan, profile, t, t + dt, out var la, out var lh))
            {
                loosed.Add((la, lh, (t + dt - LockedBattleRotation.OffsetSeconds(rowan, la)) * 30f));
            }
        }

        Assert.Equal(new[] { (1, 0), (1, 1), (2, 0), (2, 1), (2, 2) }, hits);
        Assert.Equal(new[] { (1, 0), (1, 1), (2, 0), (2, 1), (2, 2) }, loosed.Select(x => (x.Item1, x.Item2)).ToArray());

        // Each streak starts at (or just after) the clip's release, never before the arrow left the string.
        Assert.InRange(loosed[0].Item3, 44f, 45f);
        Assert.InRange(loosed[1].Item3, 80f, 81f);
        Assert.InRange(loosed[2].Item3, 84f, 85f);
    }

    [Fact]
    public void Bonequill_rapid_quiver_shot_and_bone_volley_are_baked_mixamo_clips_on_his_rig()
    {
        Assert.Equal(new[] { "rest", "walk", "attack", "attack2", "attack3" }, BonequillMotion.PoseNames);
        AssertExtraClip(BonequillMotion.Spec, BonequillMotion.ThemePackDir, BonequillMotion.DesignMixamoAttacksDir, BonequillMotion.Attack2PoseName,
            BonequillMotion.Attack2FileName, BonequillMotion.Attack2Md5, BonequillMotion.Attack2LastFrame, BonequillMotion.Attack2BowShots);
        AssertExtraClip(BonequillMotion.Spec, BonequillMotion.ThemePackDir, BonequillMotion.DesignMixamoAttacksDir, BonequillMotion.Attack3PoseName,
            BonequillMotion.Attack3FileName, BonequillMotion.Attack3Md5, BonequillMotion.Attack3LastFrame, BonequillMotion.Attack3BowShots);
        var sums = File.ReadAllText(Path.Combine(FindRepoRoot(), BonequillMotion.DesignMixamoAttacksDir, "CHECKSUMS.md5"));
        Assert.Contains("67c1411ced096fa3f00bccea26647f2c  BONEQUILL_mixamo_standing_draw_arrow.fbx", sums, StringComparison.Ordinal);
        Assert.Contains("061909498f1487dd6fa9218297326633  BONEQUILL_mixamo_standing_aim_overdraw.fbx", sums, StringComparison.Ordinal);
        Assert.Equal("b3fd5c3208bc3f2992a1293fcd77ef0c", BonequillAttack.FileMd5);

        // Quiver draw, then the arrow nocked on his left side of the riser with the drawn string on its nock, and the bow only rolling in the grip.
        var grip = BonequillAttack.BowGrip;
        Assert.True(grip.HasDrawnString);
        foreach (var shots in new[] { BonequillMotion.Attack2BowShots, BonequillMotion.Attack3BowShots })
        {
            Assert.Equal(grip.ArrowTrack, shots.ArrowTrack);
            Assert.Equal(grip.ArrowRest, shots.ArrowRest);
            foreach (var s in shots.Shots)
            {
                Assert.True(s.ArrowInHand < s.Nock && s.Nock < s.Release);
                Assert.Equal(grip.RollDrawDeg, grip.RollDeg(shots, s.Release));
                Assert.Equal(grip.RollRestDeg, grip.RollDeg(shots, s.RollE));
            }
        }
    }

    [Fact]
    public void Bonequill_rotates_bone_arrow_rapid_shot_bone_volley_with_purple_flight_streaks()
    {
        var bq = LockedBattleRoster.Bonequill;
        var profile = LockedBattleHits.ProfileOf("Bonequill");
        Assert.Equal(3, LockedBattleRotation.Count(bq));
        Assert.Equal(BonequillMotion.AttackPoseNames, new[] { 0, 1, 2 }.Select(i => LockedBattleRotation.AttackAt(bq, i).PoseName).ToArray());
        Assert.Equal(BattleImpactKind.BoneShard, LockedBattleRotation.Impact(bq, profile, 1));
        Assert.Equal(BattleImpactKind.BoneShard, LockedBattleRotation.Impact(bq, profile, 2));
        var hits = new List<(int, int)>();
        var loosed = new List<float>();
        var dt = 1f / 120f;
        for (var t = 0f; t < LockedBattleRotation.RotationSeconds(bq); t += dt)
        {
            if (LockedBattleRotation.MeleeHitCrossed(bq, profile, t, t + dt, out var a, out var h))
            {
                hits.Add((a, h));
            }

            if (LockedBattleRotation.ShotLoosed(bq, profile, t, t + dt, out var la, out _))
            {
                loosed.Add((t + dt - LockedBattleRotation.OffsetSeconds(bq, la)) * 30f);
            }
        }

        Assert.Equal(new[] { (1, 0), (2, 0), (2, 1), (2, 2) }, hits);
        Assert.InRange(loosed[0], BonequillMotion.Attack2ReleaseFrame, BonequillMotion.Attack2ReleaseFrame + 1f);
        Assert.InRange(loosed[1], BonequillMotion.Attack3ReleaseFrame, BonequillMotion.Attack3ReleaseFrame + 1f);
    }

    internal static void AssertExtraClip(BlenderRigSpec spec, string themePackDir, string designDir, string pose, string file, string md5, int lastFrame, BlenderBowShots shots)
    {
        var root = FindRepoRoot();
        var clip = spec.ExtraClips.Single(c => c.PoseName == pose);
        Assert.Equal(file, clip.FileName);
        Assert.Equal("Scene", clip.TakeName);
        Assert.Equal(lastFrame, clip.LastFrame);
        Assert.Same(shots, clip.BowShots);
        foreach (var s in shots.Shots)
        {
            Assert.InRange(s.RollE, 1, lastFrame);
        }

        var pack = Path.Combine(root, "Assets", themePackDir, file);
        Assert.Equal(md5, Md5(pack));
        Assert.Equal(md5, Md5(Path.Combine(root, designDir, file)));
        Assert.Contains(md5 + "  " + file, File.ReadAllText(Path.Combine(root, designDir, "CHECKSUMS.md5")), StringComparison.Ordinal);
        var meta = File.ReadAllText(pack + ".meta");
        Assert.Contains("name: \"" + pose + "\"", meta, StringComparison.Ordinal);
        Assert.Contains("takeName: Scene", meta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: " + lastFrame, meta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
        Assert.Contains(md5, meta, StringComparison.Ordinal);
    }

    internal static string Md5(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    internal static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
