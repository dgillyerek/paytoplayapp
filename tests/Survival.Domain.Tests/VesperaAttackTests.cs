using System.Security.Cryptography;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Vespera Theme A attack add-on (Design 2026-10-07). HOLD merge until Derek Game-view PASS.</summary>
public sealed class VesperaAttackTests
{
    [Fact]
    public void Vespera_attack_is_a_new_scene_take_after_the_existing_clips()
    {
        var poses = VesperaMotion.PoseNames;
        Assert.Equal(5, poses.Length);
        Assert.Equal("rest", poses[0]);
        Assert.Equal("walk", poses[1]);
        Assert.Equal("attack", poses[2]);
        Assert.Equal(VesperaMotion.PoseNames, VesperaMotion.Spec.PoseNames);
        var clip = VesperaMotion.Spec.ExtraClips[0];
        Assert.Equal("attack", clip.PoseName);
        Assert.Equal("VESPERA_blenderig_attack.fbx", clip.FileName);
        Assert.Equal("Scene", clip.TakeName);
        Assert.Equal(36, clip.LastFrame);
        Assert.Equal(30f, clip.FrameRate);
        Assert.Equal(1.2f, clip.Seconds, 3);
        Assert.Equal(36, VesperaAttack.Spec.ClipLastFrame);
        Assert.Equal("VESPERA_blenderig_attack.fbx", VesperaAttack.FileName);
        Assert.Equal("VESPERA_blenderig_attack.fbx", VesperaAttack.Spec.FileName);
        Assert.False(VesperaAttack.Spec.InPlace);
    }

    [Fact]
    public void Vespera_attack_fbx_props_and_design_sot_match_design_md5()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", VesperaMotion.ThemePackDir);
        var design = Path.Combine(root, VesperaAttack.DesignDir.Replace('/', Path.DirectorySeparatorChar));
        // ThemePack attack is the Design no-cape pack (blender_rig_nocape_20261009); attack_20261007 keeps the original.
        Assert.Equal("3ba1e081c79de8cbf65fc5f0d4749edb", VesperaAttack.FileMd5);
        Assert.Equal("3ba1e081c79de8cbf65fc5f0d4749edb", Md5(Path.Combine(pack, VesperaAttack.FileName)));
        Assert.Equal("082b741eae52d9144a612566380766bc", VesperaAttack.DesignFileMd5);
        Assert.Equal("082b741eae52d9144a612566380766bc", Md5(Path.Combine(design, VesperaAttack.FileName)));

        Assert.Equal(VesperaAttack.PropFileNames.Length, VesperaAttack.PropFileMd5s.Length);
        for (var i = 0; i < VesperaAttack.PropFileNames.Length; i++)
        {
            Assert.Equal(VesperaAttack.PropFileMd5s[i], Md5(Path.Combine(pack, VesperaAttack.PropFileNames[i])));
        }

        foreach (var file in new[] { "NOTE.md", "CHECKSUMS.md5", Path.Combine("work", "attack_meta.json") })
        {
            Assert.True(File.Exists(Path.Combine(design, file)), file);
        }

        foreach (var line in File.ReadAllLines(Path.Combine(design, "CHECKSUMS.md5")))
        {
            var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var name = parts[1].TrimStart('*').Trim();
            var local = Path.Combine(design, name.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(local))
            {
                Assert.Equal(parts[0], Md5(local));
            }
        }

        // Rest/walk are now the Design no-cape pack (blender_rig_nocape_20261009).
        Assert.Equal("d46729fb1f36700957dd1ee0f08cb55f", Md5(Path.Combine(pack, "VESPERA_blenderig.fbx")));
        Assert.Equal("fe139c5fbaa4146b4bc3eadc03265d72", Md5(Path.Combine(pack, "VESPERA_blenderig_walk.fbx")));
    }

    [Fact]
    public void Vespera_attack_tracks_attach_to_real_bones_and_fly_character_forward()
    {
        var spec = VesperaAttack.Spec;
        Assert.Equal(spec.CalibrationBones.Length * 3, spec.CalibrationRest.Length);
        Assert.True(spec.CalibrationBones.Length >= 3);
        foreach (var bone in spec.CalibrationBones)
        {
            Assert.Contains(bone, VesperaMotion.BoneNames);
        }

        Assert.NotEmpty(spec.Tracks);
        foreach (var track in spec.Tracks)
        {
            Assert.Equal(VesperaAttack.LastFrame + 1, track.Frames.Length);
            Assert.True(track.FirstVisibleFrame >= 0, track.ObjectName);
            if (track.Kind == AttackTrackKind.Held)
            {
                Assert.Contains(track.Bone, VesperaMotion.BoneNames);
                Assert.True(track.HasMesh, track.ObjectName);
            }

            if (track.Kind == AttackTrackKind.Projectile)
            {
                Assert.Equal(spec.ReleaseFrame, track.ReleaseFrame);
                Assert.Equal(spec.ReleaseFrame, track.FirstVisibleFrame);
                Assert.True(track.Speed > 0f, track.ObjectName);
                var a = track.Frames[track.FirstVisibleFrame];
                var b = track.Frames[track.LastVisibleFrame];
                Assert.True(b.Z > a.Z + 0.5f, track.ObjectName + " must travel character-forward (+Z, top of screen in the rear camera).");
                for (var f = track.FirstVisibleFrame + 1; f <= track.LastVisibleFrame; f++)
                {
                    var p = track.Frames[f - 1];
                    var q = track.Frames[f];
                    Assert.True(q.Z >= p.Z, track.ObjectName + " never moves toward the camera.");
                    var step = (float)Math.Sqrt(((q.X - p.X) * (q.X - p.X)) + ((q.Y - p.Y) * (q.Y - p.Y)) + ((q.Z - p.Z) * (q.Z - p.Z)));
                    Assert.InRange(step, (track.Speed / 30f) - 0.01f, (track.Speed / 30f) + 0.01f);
                }
            }

            if (track.Kind == AttackTrackKind.Fixed)
            {
                Assert.True(track.LastVisibleFrame > track.FirstVisibleFrame, track.ObjectName);
                Assert.NotEqual(AttackVfxShape.None, track.Shape);
            }

            if (!track.HasMesh)
            {
                Assert.NotEqual(AttackVfxShape.None, track.Shape);
                Assert.Equal(3, track.CoreRgb.Length);
            }
        }
    }

    [Fact]
    public void Vespera_orb_is_conjured_between_the_hands_then_launched_as_the_bolt()
    {
        var spec = VesperaAttack.Spec;
        Assert.Equal(27, VesperaAttack.ReleaseFrame);
        Assert.Equal(23, VesperaAttack.OrbFullFrame);
        Assert.Equal(new[] { "VESPERA_orb.fbx" }, VesperaAttack.PropFileNames);
        var orb = spec.Find("VESPERA_orb")!;
        var bolt = spec.Find("VESPERA_shadowbolt_vfx")!;
        Assert.Equal(AttackTrackKind.Fixed, orb.Kind);
        Assert.Equal("VESPERA_orb.fbx", orb.PropFileName);
        Assert.Equal("VESPERA_orb.fbx", bolt.PropFileName);
        Assert.Equal(AttackVfxShape.Orb, orb.Shape);
        Assert.Equal(AttackVfxShape.Orb, bolt.Shape);

        // Hidden at rest (f0) and after the release, visible from the appear frame to the frame before release.
        Assert.Equal(VesperaAttack.OrbAppearFrame, orb.FirstVisibleFrame);
        Assert.Equal(VesperaAttack.ReleaseFrame - 1, orb.LastVisibleFrame);
        Assert.False(orb.Frames[0].Visible);
        Assert.False(bolt.Frames[0].Visible);

        // Grows while the hands circle, full size by the end of the circle.
        for (var f = orb.FirstVisibleFrame + 1; f <= VesperaAttack.OrbFullFrame; f++)
        {
            Assert.True(orb.Frames[f].Scale >= orb.Frames[f - 1].Scale, "orb grows f" + f);
        }

        Assert.True(orb.Frames[orb.FirstVisibleFrame].Scale < 0.3f);
        Assert.Equal(1f, orb.Frames[VesperaAttack.OrbFullFrame].Scale, 2);

        // Rave conjure: the orb stays between the hands (it does not swing round with them) and spins with the contrary sweeps.
        for (var f = 6; f <= 22; f++)
        {
            Assert.True(Math.Abs(orb.Frames[f].X - orb.Frames[5].X) < 0.05f, "orb stays centred f" + f);
            Assert.True(Math.Abs(orb.Frames[f].Y - orb.Frames[5].Y) < 0.05f, "orb stays centred f" + f);
        }

        // The bolt starts where the orb is at the release (continuous hand-off, within 3 cm of the last orb frame).
        var last = orb.Frames[VesperaAttack.ReleaseFrame - 1];
        var start = bolt.Frames[VesperaAttack.ReleaseFrame];
        var gap = Math.Sqrt(((start.X - last.X) * (start.X - last.X)) + ((start.Y - last.Y) * (start.Y - last.Y)) + ((start.Z - last.Z) * (start.Z - last.Z)));
        Assert.True(gap < 0.03, "orb to bolt hand-off " + gap);
    }

    [Fact]
    public void Vespera_attack_plays_once_then_rests_then_repeats()
    {
        // Vespera's take is f0-36 (1.2 s); everyone else keeps the shared f0-30 (1 s) cycle.
        var spec = VesperaAttack.Spec;
        var f0 = spec.CycleFrameFor(0f, out var in0, out var c0);
        Assert.Equal(0f, f0);
        Assert.True(in0);
        Assert.Equal(0, c0);
        Assert.Equal(15f, spec.CycleFrameFor(0.5f, out _, out _), 3);
        Assert.Equal(33f, spec.CycleFrameFor(1.1f, out var inLate, out _), 3);
        Assert.True(inLate);
        Assert.Equal(36f, spec.CycleFrameFor(1.5f, out var inGap, out _));
        Assert.False(inGap);
        Assert.Equal(0f, spec.CycleFrameFor(1.7f, out _, out var c1), 3);
        Assert.Equal(1, c1);
        Assert.Equal(1.2f, spec.ClipSecondsFor, 3);

        // Shared default is unchanged for the other characters.
        Assert.Equal(30f, BlenderRigAttackSpec.CycleFrame(1.2f, out var sharedGap, out _));
        Assert.False(sharedGap);
        Assert.Equal(0f, BlenderRigAttackSpec.CycleFrame(BlenderRigAttackSpec.CycleSeconds, out _, out var s1), 3);
        Assert.Equal(1, s1);
    }

    [Fact]
    public void Vespera_attack_is_wired_in_the_actor_and_has_no_demo_menu()
    {
        var root = FindRepoRoot();
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.DoesNotContain("Survival/Vespera Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "VesperaActor.cs"));
        Assert.Contains("VesperaAttack.Spec", actor, StringComparison.Ordinal);
        var driver = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigAttackDriver.cs"));
        Assert.Contains("ParticleSystem", driver, StringComparison.Ordinal);
        Assert.Contains("TrailRenderer", driver, StringComparison.Ordinal);
        Assert.Contains("applyRootMotion = false", File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs")), StringComparison.Ordinal);
    }

    private static string Md5(string path)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string FindRepoRoot()
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
