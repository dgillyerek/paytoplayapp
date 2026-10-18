using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using Xunit;

namespace Survival.Domain.Tests;

public sealed class LockedBattleRotationTests
{
    private static readonly string[] Creatures = { "Emberfang", "Stormcrest", "Nightfang", "Ashwyrm" };

    private static LockedBattleFighter Fighter(string name)
    {
        foreach (var f in LockedBattleRoster.All)
        {
            if (f.Name == name)
            {
                return f;
            }
        }

        throw new InvalidOperationException(name);
    }

    [Fact]
    public void Every_fighter_has_one_to_three_attacks_and_attack_one_is_the_locked_attack()
    {
        foreach (var f in LockedBattleRoster.All)
        {
            var n = LockedBattleRotation.Count(f);
            Assert.InRange(n, 1, LockedBattleRotation.MaxAttacks);
            var first = LockedBattleRotation.AttackAt(f, 0);
            Assert.Equal(f.AttackPoseName, first.PoseName);
            Assert.Equal(f.AttackSeconds, first.Seconds, 4);
            Assert.Equal(f.AttackLabel, first.Label);
            Assert.Equal(LockedBattleAttack.ProfileContactFrame, first.MeleeContactFrame);
            Assert.True(first.ReachShift.IsNone, f.Name);
            Assert.False(first.InPlace, f.Name);
        }
    }

    [Fact]
    public void Creatures_are_on_hold_with_their_single_attack()
    {
        foreach (var name in Creatures)
        {
            Assert.Equal(1, LockedBattleRotation.Count(Fighter(name)));
        }
    }

    [Fact]
    public void Single_attack_fighters_keep_the_old_attack_plus_rest_loop()
    {
        foreach (var f in LockedBattleRoster.All)
        {
            if (LockedBattleRotation.Count(f) != 1)
            {
                continue;
            }

            Assert.Equal(f.AttackCycleSeconds, LockedBattleRotation.RotationSeconds(f), 4);
            for (var t = 0f; t < f.AttackCycleSeconds * 3f; t += 0.05f)
            {
                var old = (t % f.AttackCycleSeconds) < f.AttackSeconds;
                Assert.Equal(old, LockedBattleRotation.InAttackClip(f, t));
                Assert.True(LockedBattleRotation.Locate(f, t, out var index, out _));
                Assert.Equal(0, index);
            }

            // Out-of-range indices wrap onto the one attack.
            Assert.Equal(f.AttackPoseName, LockedBattleRotation.AttackAt(f, 5).PoseName);
        }
    }

    [Fact]
    public void Blightroot_rotates_swipe_punch_jump_smash_from_clips_already_in_the_repo()
    {
        var b = LockedBattleRoster.Blightroot;
        Assert.Equal(3, LockedBattleRotation.Count(b));
        Assert.Equal(BlightrootBattlePoses.Attack, LockedBattleRotation.AttackAt(b, 0).PoseName);
        Assert.Equal(BlightrootBattlePoses.Attack2, LockedBattleRotation.AttackAt(b, 1).PoseName);
        Assert.Equal(BlightrootBattlePoses.Attack3, LockedBattleRotation.AttackAt(b, 2).PoseName);
        Assert.Equal("mutant swiping", BlightrootBattlePoses.ClipFor(BlightrootBattlePoses.Attack));
        Assert.Equal("mutant punch", BlightrootBattlePoses.ClipFor(BlightrootBattlePoses.Attack2));
        Assert.Equal("mutant jump attack", BlightrootBattlePoses.ClipFor(BlightrootBattlePoses.Attack3));

        // The clips are catalogued creature-pack takes, non-looping, and the attack lengths match
        // their import frame ranges.
        foreach (var (pose, last) in new[] { (BlightrootBattlePoses.Attack2, BlightrootBattlePoses.Attack2LastFrame), (BlightrootBattlePoses.Attack3, BlightrootBattlePoses.Attack3LastFrame) })
        {
            Assert.True(BlightrootMotion.TryGet(BlightrootBattlePoses.ClipFor(pose), out var clip));
            Assert.False(clip.Loop);
            Assert.Equal(last, clip.LastFrame, 3);
            Assert.True(BlightrootBattlePoses.IsAttack(pose));
        }

        Assert.Equal(BlightrootBattlePoses.Attack2LastFrame / 30f, LockedBattleRotation.AttackAt(b, 1).Seconds, 4);
        Assert.Equal(BlightrootBattlePoses.Attack3LastFrame / 30f, LockedBattleRotation.AttackAt(b, 2).Seconds, 4);
        Assert.True(LockedBattleRotation.AttackAt(b, 2).InPlace);
        Assert.False(BlightrootBattlePoses.IsAttack(BlightrootBattlePoses.Walk));
    }

    [Fact]
    public void Blightroot_lands_each_attack_once_on_its_own_contact_frame_in_order()
    {
        var b = LockedBattleRoster.Blightroot;
        var profile = LockedBattleHits.Blightroot;
        var dt = 1f / 120f;
        var rotation = LockedBattleRotation.RotationSeconds(b);
        var hits = new List<(float t, int index)>();
        for (var t = 0f; t < rotation * 2f; t += dt)
        {
            if (LockedBattleRotation.MeleeHitCrossed(b, profile, t, t + dt, out var index))
            {
                hits.Add((t + dt, index));
                Assert.True(LockedBattleRotation.InAttackClip(b, t + dt));
                Assert.True(LockedBattleRotation.Locate(b, t + dt, out var playing, out var into));
                Assert.Equal(index, playing);
                var contact = LockedBattleRotation.ContactFrame(b, profile, index) / 30f;
                Assert.InRange(into, contact - 1e-3f, contact + dt + 1e-3f);
            }
        }

        Assert.Equal(6, hits.Count);
        for (var i = 0; i < hits.Count; i++)
        {
            Assert.Equal(i % 3, hits[i].index);
        }

        Assert.Equal(LockedBattleHits.BlightrootContactFrame, LockedBattleRotation.ContactFrame(b, profile, 0));
        Assert.Equal(BlightrootBattlePoses.Attack2ContactFrame, LockedBattleRotation.ContactFrame(b, profile, 1));
        Assert.Equal(BlightrootBattlePoses.Attack3ContactFrame, LockedBattleRotation.ContactFrame(b, profile, 2));
        Assert.Equal("mixamorig:LeftHand", LockedBattleRotation.StrikeBone(b, profile, 0));
        Assert.Equal("mixamorig:RightHand", LockedBattleRotation.StrikeBone(b, profile, 1));
        Assert.Equal(profile.ShakeMetres, LockedBattleRotation.ShakeMetres(b, profile, 0), 4);

        // The legacy single-cycle API now follows the rotation as well.
        var legacy = 0;
        for (var t = 0f; t < rotation; t += dt)
        {
            if (LockedBattleHits.MeleeHitCrossed(b, profile, t, t + dt))
            {
                legacy++;
            }
        }

        Assert.Equal(3, legacy);
    }

    [Fact]
    public void Rest_gap_follows_every_attack_and_recoil_respects_the_rotation()
    {
        var b = LockedBattleRoster.Blightroot;
        for (var i = 0; i < 3; i++)
        {
            var start = LockedBattleRotation.OffsetSeconds(b, i);
            var seconds = LockedBattleRotation.AttackAt(b, i).Seconds;
            Assert.True(LockedBattleRotation.InAttackClip(b, start + 0.01f));
            Assert.True(LockedBattleRotation.InAttackClip(b, start + seconds - 0.01f));
            Assert.False(LockedBattleRotation.InAttackClip(b, start + seconds + 0.01f));
            Assert.False(LockedBattleRotation.InAttackClip(b, start + seconds + BlenderRigAttackSpec.RestGapSeconds - 0.01f));
            Assert.False(LockedBattleHits.RecoilAllowed(b, true, start + 0.01f));
            Assert.True(LockedBattleHits.RecoilAllowed(b, true, start + seconds + 0.01f));
        }

        Assert.False(LockedBattleRotation.InAttackClip(b, -0.1f));
    }

    [Fact]
    public void Reach_slides_ease_in_and_back_to_zero_inside_each_clip()
    {
        var b = LockedBattleRoster.Blightroot;
        for (var i = 1; i < 3; i++)
        {
            var a = LockedBattleRotation.AttackAt(b, i);
            var shift = a.ReachShift;
            Assert.False(shift.IsNone);
            Assert.InRange(shift.Metres, 0.1f, 0.6f);
            Assert.Equal(0f, shift.At(0f));
            Assert.Equal(shift.Metres, shift.At(a.MeleeContactFrame), 4);
            Assert.Equal(0f, shift.At(a.Seconds * 30f));
            Assert.True(shift.EndFrame <= a.Seconds * 30f);
            var previous = 0f;
            for (var f = shift.StartFrame; f <= shift.FullFrame; f += 0.5f)
            {
                var v = shift.At(f);
                Assert.True(v >= previous - 1e-5f);
                previous = v;
            }
        }
    }

    [Fact]
    public void Battle_demo_plays_the_rotation_pose_and_lands_per_attack_hits()
    {
        var root = FindRepoRoot();
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "LockedBattleDemo.cs"));
        Assert.Contains("LockedBattleRotation.MeleeHitCrossed(f.Data, f.Profile", demo, StringComparison.Ordinal);
        Assert.Contains("LockedBattleRotation.AttackAt(f.Data, index).PoseName", demo, StringComparison.Ordinal);
        Assert.Contains("LockedBattleRotation.Count(f.Data) > 1", demo, StringComparison.Ordinal);
        Assert.Contains("f.BaseLocal + f.AttackOffset", demo, StringComparison.Ordinal);
        var blight = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlightrootActor.cs"));
        Assert.Contains("BlightrootBattlePoses.IsAttack(pose)", blight, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Survival")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("repo root");
    }
}
