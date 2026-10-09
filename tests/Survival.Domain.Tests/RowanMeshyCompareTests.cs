using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Survival.Domain.View;

namespace Survival.Domain.Tests;

/// <summary>
/// Rowan Meshy compare on Design's no-bow pack (meshy_nocape_nobow_20261008): metre body exports, the bow held
/// on RightHand at rest/walk, baked bow + arrow in the attack. HOLD merge until Derek Game-view PASS.
/// </summary>
public sealed class RowanMeshyCompareTests
{
    private const double FbxTicksPerSecond = 46186158000d;

    private static readonly string[] BodyFiles =
    {
        RowanMeshyMotion.RestFileName, RowanMeshyMotion.WalkFileName, RowanMeshyMotion.AttackFileName,
    };

    [Fact]
    public void Nobow_md5s_match_design_checksums_and_themepack()
    {
        var root = FindRepoRoot();
        var design = Path.Combine(root, RowanMeshyMotion.DesignDir);
        var pack = Path.Combine(root, "Assets", RowanMeshyMotion.ThemePackDir);
        var expected = new (string File, string Md5)[]
        {
            (RowanMeshyMotion.RestFileName, "7868c8337afdcad0c03a2fa4684f3798"),
            (RowanMeshyMotion.WalkFileName, "d0a8262872c65b19288cf6f54f36a769"),
            (RowanMeshyMotion.AttackFileName, "ed5e50e663304431cab3c57de105609e"),
            (RowanMeshyMotion.BowAttackFileName, "12240e87b97897940285cd9886308a91"),
            (RowanMeshyMotion.ArrowAttackFileName, "4d99e038dd431d493df6d961f41c096d"),
            (RowanMeshyMotion.BowPropFileName, "698cf4720af7c8b8b3142c1c57fb7e12"),
            (RowanMeshyMotion.ArrowPropFileName, "958692923580766df6187103c3e8f7bd"),
        };
        Assert.Equal(RowanMeshyMotion.SourceRestMd5, expected[0].Md5);
        Assert.Equal(RowanMeshyMotion.SourceWalkMd5, expected[1].Md5);
        Assert.Equal(RowanMeshyMotion.SourceAttackMd5, expected[2].Md5);
        Assert.Equal(RowanMeshyMotion.BowAttackMd5, expected[3].Md5);
        Assert.Equal(RowanMeshyMotion.ArrowAttackMd5, expected[4].Md5);
        Assert.Equal(RowanMeshyMotion.BowPropMd5, expected[5].Md5);
        Assert.Equal(RowanMeshyMotion.ArrowPropMd5, expected[6].Md5);
        Assert.Equal("d3c32103af78de52c27e24920e275072", RowanMeshyMotion.BowMetaMd5);
        Assert.Equal(RowanMeshyMotion.BowMetaMd5, Md5(Path.Combine(design, RowanMeshyMotion.BowMetaFileName)));
        foreach (var (file, md5) in expected)
        {
            Assert.Equal(md5, Md5(Path.Combine(design, file)));
            Assert.Equal(ShippedMd5(file, md5), Md5(Path.Combine(pack, file)));
            Assert.True(File.Exists(Path.Combine(pack, file + ".meta")), file);
        }

        foreach (var clip in RowanMeshyMotion.Spec.Clips)
        {
            Assert.Equal(clip.FileMd5, Md5(Path.Combine(pack, clip.FileName)));
        }

        // Every line of Design's CHECKSUMS.md5 holds against the committed folder.
        var lines = File.ReadAllLines(Path.Combine(design, "CHECKSUMS.md5")).Where(l => l.Trim().Length > 0).ToArray();
        Assert.Equal(17, lines.Length);
        foreach (var line in lines)
        {
            var md5 = line.Substring(0, 32);
            var file = line.Substring(32).TrimStart(' ', '*');
            Assert.Equal(md5, Md5(Path.Combine(design, file)));
        }
    }

    /// <summary>The three body FBXs ship skin-fixed; every other file ships exactly as Design delivered it.</summary>
    private static string ShippedMd5(string file, string designMd5) => file switch
    {
        RowanMeshyMotion.RestFileName => RowanMeshyMotion.RestMd5,
        RowanMeshyMotion.WalkFileName => RowanMeshyMotion.WalkMd5,
        RowanMeshyMotion.AttackFileName => RowanMeshyMotion.AttackMd5,
        _ => designMd5,
    };

    [Fact]
    public void Skin_fix_ships_only_the_three_bodies_and_is_rebuildable_from_design()
    {
        var root = FindRepoRoot();
        var fix = Path.Combine(root, RowanMeshyMotion.SkinFixDir);
        Assert.NotEqual(RowanMeshyMotion.SourceRestMd5, RowanMeshyMotion.RestMd5);
        Assert.NotEqual(RowanMeshyMotion.SourceWalkMd5, RowanMeshyMotion.WalkMd5);
        Assert.NotEqual(RowanMeshyMotion.SourceAttackMd5, RowanMeshyMotion.AttackMd5);
        Assert.Equal(63, RowanMeshyMotion.SourceTriangles - RowanMeshyMotion.Triangles);
        foreach (var f in new[]
        {
            "README.md", "CHECKSUMS.md5", "rowan_meshy_nobow_skinfix_contact_sheet.png",
            "tools/build.sh", "tools/port_nobow.py", "tools/reweight.py", "tools/seg.py", "tools/fbxbin.py",
            "tools/stretch.py", "tools/dump_bind_bones.py", "tools/dump_bind_weights.py",
            "tools/render_nb.py", "tools/contact_sheet_nb.py",
        })
        {
            Assert.True(File.Exists(Path.Combine(fix, f)), f);
        }

        // CHECKSUMS.md5 pins the shipped bodies (what build.sh reproduces) and the contact sheet.
        var lines = File.ReadAllLines(Path.Combine(fix, "CHECKSUMS.md5")).Where(l => l.Trim().Length > 0).ToArray();
        var pack = Path.Combine(root, "Assets", RowanMeshyMotion.ThemePackDir);
        foreach (var (file, md5) in new[]
        {
            (RowanMeshyMotion.RestFileName, RowanMeshyMotion.RestMd5),
            (RowanMeshyMotion.WalkFileName, RowanMeshyMotion.WalkMd5),
            (RowanMeshyMotion.AttackFileName, RowanMeshyMotion.AttackMd5),
        })
        {
            Assert.Contains(md5 + "  " + file, lines);
            Assert.Equal(md5, Md5(Path.Combine(pack, file)));
        }

        var sheet = lines.Single(l => l.EndsWith("rowan_meshy_nobow_skinfix_contact_sheet.png", StringComparison.Ordinal));
        Assert.Equal(sheet.Substring(0, 32), Md5(Path.Combine(fix, "rowan_meshy_nobow_skinfix_contact_sheet.png")));
    }

    [Fact]
    public void Old_fused_bow_exports_are_gone_from_the_themepack_but_the_superseded_design_folder_stays()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", RowanMeshyMotion.ThemePackDir);
        foreach (var old in new[] { "ROWAN_meshy_rest.fbx", "ROWAN_meshy_walk.fbx", "ROWAN_meshy_attack.fbx" })
        {
            Assert.False(File.Exists(Path.Combine(pack, old)), old);
            Assert.False(File.Exists(Path.Combine(pack, old + ".meta")), old);
        }

        var superseded = Path.Combine(root, RowanMeshyMotion.SupersededDesignDir);
        Assert.Equal(27, Directory.GetFiles(superseded, "*", SearchOption.AllDirectories).Length);
        Assert.Equal("5cb9e286fd4aa63d55c192533e98697d", Md5(Path.Combine(superseded, "ROWAN_meshy_rest.fbx")));
        Assert.NotEqual(RowanMeshyMotion.SupersededDesignDir, RowanMeshyMotion.DesignDir);
    }

    [Fact]
    public void Meshy_textures_match_design_and_themepack()
    {
        var root = FindRepoRoot();
        var design = Path.Combine(root, RowanMeshyMotion.DesignDir, "textures");
        var tex = Path.Combine(root, "Assets", RowanMeshyMotion.ThemePackDir, RowanMeshyMotion.TextureFolder);
        var maps = new (string File, string Md5)[]
        {
            (RowanMeshyMotion.BaseColorFile, RowanMeshyMotion.BaseColorMd5),
            (RowanMeshyMotion.NormalFile, RowanMeshyMotion.NormalMd5),
            (RowanMeshyMotion.MetallicFile, RowanMeshyMotion.MetallicMd5),
            (RowanMeshyMotion.RoughnessFile, RowanMeshyMotion.RoughnessMd5),
        };
        foreach (var (file, md5) in maps)
        {
            Assert.Equal(md5, Md5(Path.Combine(tex, file)));
            Assert.Equal(md5, Md5(Path.Combine(design, file)));
            Assert.True(File.Exists(Path.Combine(tex, file + ".meta")), file);
        }
    }

    [Fact]
    public void Design_folder_is_committed_file_for_file()
    {
        var design = Path.Combine(FindRepoRoot(), RowanMeshyMotion.DesignDir);
        var files = Directory.GetFiles(design, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(design, f).Replace('\\', '/'))
            .ToArray();
        Assert.Equal(30, files.Length);
        foreach (var expected in new[]
        {
            "CHECKSUMS.md5", "NOTE.md", "ROWAN_meshy_bow_meta.json", "ROWAN_meshy_nocape_nobow.blend",
            "ROWAN_meshy_nobow_rest.fbx", "ROWAN_meshy_nobow_walk.fbx", "ROWAN_meshy_nobow_attack.fbx",
            "ROWAN_meshy_bow_attack.fbx", "ROWAN_meshy_arrow_attack.fbx",
            "ROWAN_meshy_nobow_attack_sheet.jpg", "ROWAN_meshy_nobow_walk_sheet.jpg", "ROWAN_meshy_nobow_walk_attack_preview.mp4",
            "props/ROWAN_bow_meshy.fbx", "props/ROWAN_arrow_blue_fletch_meshy.fbx",
        })
        {
            Assert.Contains(expected, files);
        }

        Assert.Equal(12, files.Count(f => f.StartsWith("work/", StringComparison.Ordinal)));
    }

    [Fact]
    public void Fbx_headers_say_24_fps_metres_and_scene_take_lengths_match_the_spec()
    {
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        foreach (var clip in RowanMeshyMotion.Spec.Clips)
        {
            var bytes = File.ReadAllBytes(Path.Combine(pack, clip.FileName));
            Assert.Equal(24d, ReadP70Double(bytes, "CustomFrameRate"));
            Assert.Equal(100d, ReadP70Double(bytes, "UnitScaleFactor"));
            Assert.Equal(RowanMeshyMotion.FileFrameRate, clip.FrameRate);
            Assert.Equal(RowanMeshyMotion.TakeName, clip.TakeName);
            Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(clip.TakeName + "\0\u0001AnimStack")), clip.FileName);
            var stop = ReadAllP70Long(bytes, "LocalStop").Max();
            Assert.Equal(clip.LastFrame, (int)Math.Round(stop / FbxTicksPerSecond * clip.FrameRate));
            Assert.Equal(clip.Seconds, (float)(stop / FbxTicksPerSecond), 3);
        }

        foreach (var baked in RowanMeshyMotion.Spec.AttackBakedProps)
        {
            var bytes = File.ReadAllBytes(Path.Combine(pack, baked.FileName));
            Assert.Equal(24d, ReadP70Double(bytes, "CustomFrameRate"));
            Assert.Equal(100d, ReadP70Double(bytes, "UnitScaleFactor"));
            Assert.Equal(RowanMeshyMotion.TakeName, baked.TakeName);
            Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(baked.TakeName + "\0\u0001AnimStack")), baked.FileName);
            var stop = ReadAllP70Long(bytes, "LocalStop").Max();
            Assert.Equal(baked.LastFrame, (int)Math.Round(stop / FbxTicksPerSecond * 24d));
            Assert.Equal(RowanMeshyMotion.AttackLastFrame, baked.LastFrame);
        }

        Assert.Equal(45, RowanMeshyMotion.RestLastFrame);
        Assert.Equal(25, RowanMeshyMotion.WalkLastFrame);
        Assert.Equal(120, RowanMeshyMotion.AttackLastFrame);
    }

    [Fact]
    public void Bones_bind_by_the_same_88_names_and_every_export_shares_the_skeleton()
    {
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        var spec = RowanMeshyMotion.Spec;
        Assert.Equal(88, RowanMeshyMotion.BoneCount);
        Assert.Equal(RowanMeshyMotion.BoneCount, spec.BoneNames.Length);
        Assert.Equal(spec.BoneNames.Length, spec.BoneNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(spec.BoneRoot, spec.BoneNames);
        Assert.Contains(RowanMeshyMotion.RightHand, spec.BoneNames);
        Assert.Contains(RowanMeshyMotion.LeftHand, spec.BoneNames);
        foreach (var bone in MixamoHumanoidBones.Names)
        {
            Assert.Contains(bone, spec.BoneNames);
        }

        var limbTail = new byte[] { (byte)'S', 8, 0, 0, 0 }.Concat(Encoding.ASCII.GetBytes("LimbNode")).ToArray();
        foreach (var file in BodyFiles)
        {
            var bytes = File.ReadAllBytes(Path.Combine(pack, file));
            Assert.Equal(spec.BoneNames.Length, Count(bytes, Encoding.ASCII.GetBytes("\0\u0001Model").Concat(limbTail).ToArray()));
            foreach (var bone in spec.BoneNames)
            {
                Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(bone + "\0\u0001Model").Concat(limbTail).ToArray()), file + " " + bone);
            }

            Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(spec.ArmatureName + "\0\u0001Model")), spec.ArmatureName);
            Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(spec.BodyMeshName + "\0\u0001Model")), spec.BodyMeshName);
        }
    }

    [Fact]
    public void No_bow_in_the_body_and_no_stray_sphere_so_nothing_is_hidden()
    {
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        foreach (var file in BodyFiles)
        {
            var bytes = File.ReadAllBytes(Path.Combine(pack, file));
            Assert.False(Contains(bytes, Encoding.ASCII.GetBytes("Icosphere")), file);
            Assert.False(Contains(bytes, Encoding.ASCII.GetBytes("ROWAN_bow")), file);
        }

        Assert.Empty(RowanMeshyMotion.Spec.HiddenMeshNames);
        Assert.False(RowanMeshyMotion.Spec.IsHiddenMesh("Icosphere"));
        Assert.False(RowanMeshyMotion.Spec.IsHiddenMesh(RowanMeshyMotion.BodyMeshName));
    }

    [Fact]
    public void Baked_attack_files_carry_the_bow_rig_and_the_arrow()
    {
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        var bow = File.ReadAllBytes(Path.Combine(pack, RowanMeshyMotion.BowAttackFileName));
        var limbTail = new byte[] { (byte)'S', 8, 0, 0, 0 }.Concat(Encoding.ASCII.GetBytes("LimbNode")).ToArray();
        foreach (var bone in new[] { "bow_grip", "bow_nock" })
        {
            Assert.True(Contains(bow, Encoding.ASCII.GetBytes(bone + "\0\u0001Model").Concat(limbTail).ToArray()), bone);
        }

        Assert.True(Contains(bow, Encoding.ASCII.GetBytes("ROWAN_bow_rig\0\u0001Model")));
        Assert.True(Contains(bow, Encoding.ASCII.GetBytes("ROWAN_bow\0\u0001Model")));
        var arrow = File.ReadAllBytes(Path.Combine(pack, RowanMeshyMotion.ArrowAttackFileName));
        Assert.True(Contains(arrow, Encoding.ASCII.GetBytes("ROWAN_arrow_blue_fletch\0\u0001Model")));
        Assert.Equal(new[] { "ROWAN_bow_attack", "ROWAN_arrow_attack" }, RowanMeshyMotion.Spec.AttackBakedProps.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Bow_meta_matches_the_spec_hands_release_and_frame_counts()
    {
        var path = Path.Combine(FindRepoRoot(), RowanMeshyMotion.DesignDir, RowanMeshyMotion.BowMetaFileName);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var meta = doc.RootElement;
        Assert.Equal(24, meta.GetProperty("fps").GetInt32());
        var clips = meta.GetProperty("clips");
        Assert.Equal(RowanMeshyMotion.RestFileName, clips.GetProperty("rest").GetProperty("fbx").GetString());
        Assert.Equal(RowanMeshyMotion.WalkFileName, clips.GetProperty("walk").GetProperty("fbx").GetString());
        Assert.Equal(RowanMeshyMotion.AttackFileName, clips.GetProperty("attack").GetProperty("fbx").GetString());
        Assert.Equal(RowanMeshyMotion.RestLastFrame, clips.GetProperty("rest").GetProperty("frames")[1].GetInt32());
        Assert.Equal(RowanMeshyMotion.WalkLastFrame, clips.GetProperty("walk").GetProperty("frames")[1].GetInt32());
        Assert.Equal(RowanMeshyMotion.AttackLastFrame, clips.GetProperty("attack").GetProperty("frames")[1].GetInt32());

        var bow = meta.GetProperty("bow");
        Assert.Equal(RowanMeshyMotion.BowAttackFileName, bow.GetProperty("baked_fbx").GetString());
        Assert.Equal(RowanMeshyMotion.BowPropFileName, bow.GetProperty("static_fbx").GetString());
        var hands = bow.GetProperty("hand_bones");
        Assert.Equal(RowanMeshyMotion.RightHand, hands.GetProperty("rest").GetString());
        Assert.Equal(RowanMeshyMotion.RightHand, hands.GetProperty("walk").GetString());
        Assert.StartsWith(RowanMeshyMotion.LeftHand, hands.GetProperty("attack").GetString(), StringComparison.Ordinal);
        Assert.Equal(4, bow.GetProperty("offset_in_RightHand_rest_walk").GetArrayLength());
        Assert.Equal(4, bow.GetProperty("offset_in_LeftHand_attack").GetArrayLength());

        var arrow = meta.GetProperty("arrow");
        Assert.StartsWith(RowanMeshyMotion.ArrowAttackFileName, arrow.GetProperty("baked_fbx").GetString(), StringComparison.Ordinal);
        Assert.Equal(RowanMeshyMotion.ArrowPropFileName, arrow.GetProperty("static_fbx").GetString());
        Assert.Equal(RowanMeshyMotion.AttackReleaseFrame, arrow.GetProperty("release_frame").GetInt32());
        Assert.Equal(RowanMeshyMotion.AttackReleaseFrame, RowanMeshyMotion.Spec.AttackReleaseFrame);
        Assert.Equal(RowanMeshyMotion.ArrowSpeedMetresPerSecond, (float)arrow.GetProperty("speed_mps").GetDouble(), 3);

        // Aim is fixed in the export: the shot now heads straight up-screen with no Unity turn.
        var aim = meta.GetProperty("aim_fix");
        Assert.InRange(aim.GetProperty("shot_yaw_after_deg").GetDouble(), -0.5, 0.5);
        Assert.InRange(aim.GetProperty("rotation_about_world_up_deg").GetDouble(), -100, -98);
    }

    [Fact]
    public void Idle_bow_rides_right_hand_and_the_fallback_rides_left_hand()
    {
        var spec = RowanMeshyMotion.Spec;
        var idle = Assert.Single(spec.IdleProps);
        Assert.Equal(RowanMeshyMotion.RightHand, idle.Bone);
        Assert.Equal(RowanMeshyMotion.BowPropFileName, idle.FileName);
        Assert.Equal(MeshyRigSpec.RestPoseName, idle.BindPose);
        Assert.Equal(0, idle.BindFrame);
        AssertPose(idle.CharacterPose, 0.85f, 1.1f);

        var fallback = Assert.Single(spec.AttackFallbackProps);
        Assert.Equal(RowanMeshyMotion.LeftHand, fallback.Bone);
        Assert.Equal(RowanMeshyMotion.BowPropFileName, fallback.FileName);
        Assert.Equal(MeshyRigSpec.AttackPoseName, fallback.BindPose);
        Assert.Equal(RowanMeshyMotion.FallbackBindFrame, fallback.BindFrame);
        Assert.InRange(fallback.BindFrame, 45, RowanMeshyMotion.AttackReleaseFrame - 1);
        AssertPose(fallback.CharacterPose, 1.3f, 1.55f);
        Assert.False(spec.UseFileScale);
    }

    [Fact]
    public void Player_hides_idle_bow_for_baked_attack_props_with_no_projectile_code()
    {
        var player = ReadUnity("MeshyRigPlayer.cs");
        var attack = player.IndexOf("if (attack)", StringComparison.Ordinal);
        Assert.True(attack > 0);
        var hide = player.IndexOf("ShowIdleProps(false)", attack, StringComparison.Ordinal);
        var show = player.IndexOf("ShowAttackProps(true)", attack, StringComparison.Ordinal);
        Assert.True(hide > 0 && show > hide);
        Assert.Contains("SampleAttackProps(t)", player, StringComparison.Ordinal);
        Assert.Contains("BuildIdleProps()", player, StringComparison.Ordinal);
        Assert.Contains("prop.transform.SetParent(bone, true)", player, StringComparison.Ordinal);
        Assert.Contains("Object.Instantiate(prefab, _instance.transform, false)", player, StringComparison.Ordinal);
        Assert.Contains("_spec.AttackFallbackProps", player, StringComparison.Ordinal);
        Assert.Contains("PinTake(importer, spec.Name, spec.TakeName", player, StringComparison.Ordinal);
        foreach (var banned in new[] { "Rigidbody", "velocity", "ArrowSpeed", "Projectile", "AddForce" })
        {
            Assert.DoesNotContain(banned, player, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Dropdown_is_rest_walk_attack_like_the_other_demos()
    {
        var spec = RowanMeshyMotion.Spec;
        Assert.Equal(new[] { "rest", "walk", "attack" }, spec.PoseNames);
        Assert.Equal(spec.PoseNames, RowanMeshyMotion.DemoSpec.PoseNames);
        Assert.Equal("RowanMeshyCompareClipDropdown", RowanMeshyMotion.DemoSpec.DropdownObjectName);
        Assert.Equal(spec.DropdownObjectName, RowanMeshyMotion.DemoSpec.DropdownObjectName);
        Assert.Equal(RowanMeshyMotion.RestFileName, RowanMeshyMotion.DemoSpec.RestFileName);
        Assert.Equal(RowanMeshyMotion.ThemePackDir, RowanMeshyMotion.DemoSpec.ThemePackDir);
        Assert.DoesNotContain("rowan_remesh_male", RowanMeshyMotion.ThemePackDir, StringComparison.Ordinal);
        Assert.EndsWith("/rowan_meshy", RowanMeshyMotion.ThemePackDir, StringComparison.Ordinal);
    }

    [Fact]
    public void Attack_plays_once_holds_rest_half_a_second_then_repeats()
    {
        var spec = RowanMeshyMotion.Spec;
        Assert.Equal(0.5f, MeshyRigSpec.RestGapSeconds);
        Assert.Equal(BlenderRigAttackSpec.RestGapSeconds, MeshyRigSpec.RestGapSeconds);
        Assert.Equal(5f, spec.Attack.Seconds, 4);
        Assert.Equal(5.5f, spec.AttackCycleSeconds, 4);

        Assert.Equal(0f, spec.AttackClipSeconds(0f, out var inClip, out var cycle));
        Assert.True(inClip);
        Assert.Equal(0, cycle);
        Assert.Equal(3.9f, spec.AttackClipSeconds(3.9f, out inClip, out _), 4);
        Assert.True(inClip);
        Assert.Equal(5f, spec.AttackClipSeconds(5.3f, out inClip, out cycle), 4);
        Assert.False(inClip);
        Assert.Equal(0, cycle);
        Assert.Equal(0.25f, spec.AttackClipSeconds(5.75f, out inClip, out cycle), 3);
        Assert.True(inClip);
        Assert.Equal(1, cycle);
        Assert.Equal(0f, spec.AttackClipSeconds(-1f, out _, out _));
    }

    [Fact]
    public void Rear_battle_camera_and_no_unity_attack_turn()
    {
        Assert.Equal(PortraitGameView.RearYawDegrees, RowanMeshyMotion.CameraYawDegrees);
        Assert.InRange(RowanMeshyMotion.CameraPitchDegrees, PortraitGameView.PitchMin, -1f);
        Assert.InRange(RowanMeshyMotion.AttackReleaseFrame, 45, RowanMeshyMotion.AttackLastFrame);

        var demo = ReadUnity("RowanMeshyCompareDemo.cs");
        Assert.Contains("StartYawDegrees => RowanMeshyMotion.CameraYawDegrees", demo, StringComparison.Ordinal);
        Assert.Contains("StartPitchDegrees => RowanMeshyMotion.CameraPitchDegrees", demo, StringComparison.Ordinal);
        var baseDemo = ReadUnity("BlenderRigDemo.cs");
        Assert.Contains("protected virtual float StartYawDegrees => 0f;", baseDemo, StringComparison.Ordinal);
        Assert.Contains("stage.SetView(StartYawDegrees, StartPitchDegrees)", baseDemo, StringComparison.Ordinal);
        var player = ReadUnity("MeshyRigPlayer.cs");
        Assert.DoesNotContain("SetYaw", player, StringComparison.Ordinal);
        Assert.DoesNotContain("AttackYaw", player, StringComparison.Ordinal);
        Assert.Null(typeof(MeshyRigSpec).GetProperty("AttackYawDegrees"));
        Assert.Null(typeof(RowanMeshyMotion).GetField("AttackYawDegrees"));
    }

    [Fact]
    public void Player_is_in_place_generic_and_file_scale_follows_the_metre_spec()
    {
        var player = ReadUnity("MeshyRigPlayer.cs");
        Assert.Contains("_animator.applyRootMotion = false", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", player, StringComparison.Ordinal);
        Assert.Contains("importer.useFileScale = _spec.UseFileScale", player, StringComparison.Ordinal);
        Assert.DoesNotContain("importer.useFileScale = true", player, StringComparison.Ordinal);
        Assert.Contains("importer.bakeAxisConversion = false", player, StringComparison.Ordinal);
        Assert.Contains("CheckBones(_instance)", player, StringComparison.Ordinal);
        Assert.Contains("_spec.AttackClipSeconds(_time", player, StringComparison.Ordinal);
        Assert.False(RowanMeshyMotion.UseFileScale);
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        foreach (var file in BodyFiles.Concat(new[] { RowanMeshyMotion.BowAttackFileName, RowanMeshyMotion.ArrowAttackFileName }))
        {
            var meta = File.ReadAllText(Path.Combine(pack, file + ".meta"));
            Assert.Contains("useFileScale: 0", meta, StringComparison.Ordinal);
            Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
        }

        foreach (var file in new[] { RowanMeshyMotion.BowPropFileName, RowanMeshyMotion.ArrowPropFileName })
        {
            var meta = File.ReadAllText(Path.Combine(pack, file + ".meta"));
            Assert.Contains("useFileScale: 0", meta, StringComparison.Ordinal);
            Assert.Contains("animationType: 0", meta, StringComparison.Ordinal);
        }

        Assert.True(File.Exists(Path.Combine(pack, "props.meta")));
        var guids = Directory.GetFiles(pack, "*.meta", SearchOption.AllDirectories)
            .Select(f => File.ReadAllLines(f).First(l => l.StartsWith("guid: ", StringComparison.Ordinal)))
            .ToArray();
        Assert.Equal(guids.Length, guids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Separate_metallic_and_roughness_pack_into_urp_metallic_r_and_smoothness_a()
    {
        // Rowan's Meshy maps average metallic 3/255 and roughness 142/255.
        var (metallic, smoothness) = GlTfMetalRough.FromSeparate(3f / 255f, 142f / 255f);
        Assert.InRange(metallic, 0.011f, 0.012f);
        Assert.InRange(smoothness, 0.44f, 0.45f);

        GlTfMetalRough.PackSeparateTexel(3, 142, out var rgb, out var alpha);
        Assert.Equal(3, rgb);
        Assert.Equal(113, alpha);

        var packed = GlTfMetalRough.PackSeparateRgba32(
            new byte[] { 0, 0, 0, 255, 255, 255, 255, 255, 40, 1, 2, 255 },
            new byte[] { 255, 255, 255, 255, 0, 0, 0, 255, 100, 7, 9, 255 });
        Assert.Equal(new byte[] { 0, 0, 0, 0, 255, 255, 255, 255, 40, 40, 40, 155 }, packed);
        Assert.Throws<ArgumentException>(() => GlTfMetalRough.PackSeparateRgba32(new byte[4], new byte[8]));
        Assert.Throws<ArgumentException>(() => GlTfMetalRough.PackSeparateRgba32(new byte[3], new byte[3]));
        Assert.Throws<ArgumentNullException>(() => GlTfMetalRough.PackSeparateRgba32(null!, new byte[4]));
        Assert.Throws<ArgumentNullException>(() => GlTfMetalRough.PackSeparateRgba32(new byte[4], null!));
    }

    [Fact]
    public void Player_packs_separate_maps_and_never_binds_them_raw()
    {
        var player = ReadUnity("MeshyRigPlayer.cs");
        Assert.Contains("GlTfMetalRough.PackSeparateTexel(metalPx[i].r, roughPx[i].r", player, StringComparison.Ordinal);
        Assert.Contains("srgb: false, readable: true", player, StringComparison.Ordinal);
        Assert.Contains("mat.SetTexture(\"_MetallicGlossMap\", packed)", player, StringComparison.Ordinal);
        Assert.Contains("_METALLICSPECGLOSSMAP", player, StringComparison.Ordinal);
        Assert.Contains("GlTfMetalRough.FallbackSmoothness", player, StringComparison.Ordinal);
        Assert.Contains("_SmoothnessTextureChannel\", 0f", player, StringComparison.Ordinal);
        Assert.DoesNotContain("SetTexture(\"_MetallicGlossMap\", metallic)", player, StringComparison.Ordinal);
        Assert.DoesNotContain("SetTexture(\"_MetallicGlossMap\", roughness)", player, StringComparison.Ordinal);
    }

    [Fact]
    public void Separate_scene_and_menu_open_the_compare_demo()
    {
        var root = FindRepoRoot();
        Assert.Equal("Survival/Rowan Meshy Compare (rest + walk + attack, Game view 1080x1920)", RowanMeshyMotion.MenuItem);
        var menu = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("[MenuItem(\"" + RowanMeshyMotion.MenuItem + "\")]", menu, StringComparison.Ordinal);
        Assert.Contains("\"" + RowanMeshyMotion.ScenePath + "\"", menu, StringComparison.Ordinal);

        var scene = File.ReadAllText(Path.Combine(root, RowanMeshyMotion.ScenePath));
        var demoMeta = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "RowanMeshyCompareDemo.cs.meta"));
        var guid = demoMeta.Split('\n').First(l => l.StartsWith("guid: ", StringComparison.Ordinal)).Substring(6).Trim();
        Assert.Contains("guid: " + guid + ", type: 3", scene, StringComparison.Ordinal);
        Assert.Contains("m_Name: RowanMeshyCompareRoot", scene, StringComparison.Ordinal);
        Assert.DoesNotContain("Stormcrest", scene, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, RowanMeshyMotion.ScenePath + ".meta")));
        Assert.Equal(RowanMeshyMotion.Name + ".unity", Path.GetFileName(RowanMeshyMotion.ScenePath));
    }

    private static void AssertPose(float[] pose, float minY, float maxY)
    {
        Assert.Equal(7, pose.Length);
        Assert.InRange(pose[1], minY, maxY);
        Assert.InRange(MathF.Abs(pose[0]), 0f, 0.6f);
        Assert.InRange(MathF.Abs(pose[2]), 0f, 0.9f);
        var norm = MathF.Sqrt(pose[3] * pose[3] + pose[4] * pose[4] + pose[5] * pose[5] + pose[6] * pose[6]);
        Assert.InRange(norm, 0.999f, 1.001f);
    }

    private static string ReadUnity(string file) =>
        File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", "Survival", "Unity", file));

    private static double ReadP70Double(byte[] bytes, string name)
    {
        var at = IndexOf(bytes, PropName(name), 0);
        Assert.True(at >= 0, name);
        var o = at + PropName(name).Length;
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal((byte)'S', bytes[o]);
            o += 5 + BitConverter.ToInt32(bytes, o + 1);
        }

        Assert.Equal((byte)'D', bytes[o]);
        return BitConverter.ToDouble(bytes, o + 1);
    }

    private static IEnumerable<long> ReadAllP70Long(byte[] bytes, string name)
    {
        var key = PropName(name);
        var at = IndexOf(bytes, key, 0);
        while (at >= 0)
        {
            var o = at + key.Length;
            for (var i = 0; i < 3; i++)
            {
                o += 5 + BitConverter.ToInt32(bytes, o + 1);
            }

            if (bytes[o] == (byte)'L')
            {
                yield return BitConverter.ToInt64(bytes, o + 1);
            }

            at = IndexOf(bytes, key, at + 1);
        }
    }

    private static byte[] PropName(string name)
    {
        var s = Encoding.ASCII.GetBytes(name);
        return new byte[] { (byte)'S' }.Concat(BitConverter.GetBytes(s.Length)).Concat(s).ToArray();
    }

    private static bool Contains(byte[] hay, byte[] needle) => IndexOf(hay, needle, 0) >= 0;

    private static int Count(byte[] hay, byte[] needle)
    {
        var n = 0;
        var at = IndexOf(hay, needle, 0);
        while (at >= 0)
        {
            n++;
            at = IndexOf(hay, needle, at + 1);
        }

        return n;
    }

    private static int IndexOf(byte[] hay, byte[] needle, int start)
    {
        var span = hay.AsSpan(start);
        var i = span.IndexOf(needle);
        return i < 0 ? -1 : start + i;
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
