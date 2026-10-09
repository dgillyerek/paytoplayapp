using System.Security.Cryptography;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Ashwyrm Theme A attack add-on (Design 2026-10-07). Attack is Derek Game-view PASS. Wing flap and walk still await Game-view.</summary>
public sealed class AshwyrmAttackTests
{
    [Fact]
    public void Ashwyrm_attack_is_a_new_scene_take_after_the_existing_clips()
    {
        var poses = AshwyrmMotion.PoseNames;
        Assert.Equal(4, poses.Length);
        Assert.Equal("rest", poses[0]);
        Assert.Equal("wing flap", poses[1]);
        Assert.Equal("walk", poses[2]);
        Assert.Equal("attack", poses[3]);
        Assert.Equal(AshwyrmMotion.PoseNames, AshwyrmMotion.Spec.PoseNames);
        var clip = AshwyrmMotion.Spec.ExtraClips[AshwyrmMotion.Spec.ExtraClips.Length - 1];
        Assert.Equal("attack", clip.PoseName);
        Assert.Equal("ASHWYRM_blenderig_attack.fbx", clip.FileName);
        Assert.Equal("Scene", clip.TakeName);
        Assert.Equal(30, clip.LastFrame);
        Assert.Equal(30f, clip.FrameRate);
        Assert.Equal(1f, clip.Seconds);
        Assert.Equal("ASHWYRM_blenderig_attack.fbx", AshwyrmAttack.FileName);
        Assert.Equal("ASHWYRM_blenderig_attack.fbx", AshwyrmAttack.Spec.FileName);
        Assert.False(AshwyrmAttack.Spec.InPlace);
    }

    [Fact]
    public void Ashwyrm_attack_fbx_props_and_design_sot_match_design_md5()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", AshwyrmMotion.ThemePackDir);
        var design = Path.Combine(root, AshwyrmAttack.DesignDir.Replace('/', Path.DirectorySeparatorChar));
        Assert.Equal("9dc8b9a2d2083290b6a369129a6469b3", AshwyrmAttack.FileMd5);
        Assert.Equal("9dc8b9a2d2083290b6a369129a6469b3", Md5(Path.Combine(pack, AshwyrmAttack.FileName)));
        Assert.Equal("9dc8b9a2d2083290b6a369129a6469b3", Md5(Path.Combine(design, AshwyrmAttack.FileName)));

        Assert.Equal(AshwyrmAttack.PropFileNames.Length, AshwyrmAttack.PropFileMd5s.Length);
        for (var i = 0; i < AshwyrmAttack.PropFileNames.Length; i++)
        {
            Assert.Equal(AshwyrmAttack.PropFileMd5s[i], Md5(Path.Combine(pack, AshwyrmAttack.PropFileNames[i])));
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

        // Rest, attack, and walk stay byte-identical. Flap is the swept-back lowered shoulder beat re-export.
        Assert.Equal("f1f9f937f2961ce185eb2d942d91d595", Md5(Path.Combine(pack, "ASHWYRM_blenderig.fbx")));
        Assert.Equal("490e574a13f505202af9c468a9f38a0e", Md5(Path.Combine(pack, "ASHWYRM_blenderig_walk.fbx")));
        Assert.Equal("6f2b3924678d0fa6e9191cad096726c3", Md5(Path.Combine(pack, "ASHWYRM_blenderig_wingflap.fbx")));
    }

    [Fact]
    public void Ashwyrm_attack_tracks_attach_to_real_bones_and_fly_character_forward()
    {
        var spec = AshwyrmAttack.Spec;
        Assert.Equal(spec.CalibrationBones.Length * 3, spec.CalibrationRest.Length);
        Assert.True(spec.CalibrationBones.Length >= 3);
        foreach (var bone in spec.CalibrationBones)
        {
            Assert.Contains(bone, AshwyrmMotion.BoneNames);
        }

        Assert.NotEmpty(spec.Tracks);
        foreach (var track in spec.Tracks)
        {
            Assert.Equal(31, track.Frames.Length);
            Assert.True(track.FirstVisibleFrame >= 0, track.ObjectName);
            if (track.Kind == AttackTrackKind.Held)
            {
                Assert.Contains(track.Bone, AshwyrmMotion.BoneNames);
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
    public void Ashwyrm_attack_plays_once_then_rests_then_repeats()
    {
        var f0 = BlenderRigAttackSpec.CycleFrame(0f, out var in0, out var c0);
        Assert.Equal(0f, f0);
        Assert.True(in0);
        Assert.Equal(0, c0);
        Assert.Equal(15f, BlenderRigAttackSpec.CycleFrame(0.5f, out _, out _), 3);
        Assert.Equal(30f, BlenderRigAttackSpec.CycleFrame(1.2f, out var inGap, out _));
        Assert.False(inGap);
        Assert.Equal(0f, BlenderRigAttackSpec.CycleFrame(BlenderRigAttackSpec.CycleSeconds, out _, out var c1), 3);
        Assert.Equal(1, c1);
    }

    [Fact]
    public void Ashwyrm_attack_is_wired_in_the_demo_and_menu()
    {
        var root = FindRepoRoot();
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Ashwyrm Demo (rest + wing flap + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "AshwyrmActor.cs"));
        Assert.Contains("AshwyrmAttack.Spec", actor, StringComparison.Ordinal);
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
