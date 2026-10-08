using System.Security.Cryptography;
using System.Text;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Survival.Domain.View;

namespace Survival.Domain.Tests;

/// <summary>Rowan Meshy compare (Design meshy_nocape_20261008). HOLD merge until Derek Game-view PASS.</summary>
public sealed class RowanMeshyCompareTests
{
    private const double FbxTicksPerSecond = 46186158000d;

    [Fact]
    public void Meshy_fbx_md5s_match_design_and_themepack()
    {
        var root = FindRepoRoot();
        var design = Path.Combine(root, RowanMeshyMotion.DesignDir);
        var pack = Path.Combine(root, "Assets", RowanMeshyMotion.ThemePackDir);
        Assert.Equal("5cb9e286fd4aa63d55c192533e98697d", RowanMeshyMotion.RestMd5);
        Assert.Equal("d7680dc0f5848a78078bfb1387058fb5", RowanMeshyMotion.WalkMd5);
        Assert.Equal("e74bf8ae1f3d73dfe6813318a3e7641f", RowanMeshyMotion.AttackMd5);
        foreach (var clip in RowanMeshyMotion.Spec.Clips)
        {
            Assert.Equal(clip.FileMd5, Md5(Path.Combine(design, clip.FileName)));
            Assert.Equal(clip.FileMd5, Md5(Path.Combine(pack, clip.FileName)));
            Assert.True(File.Exists(Path.Combine(pack, clip.FileName + ".meta")), clip.FileName);
        }

        // The ROWAN_meshy_*.fbx files are the Meshy zip exports renamed, byte for byte.
        var zip = "Meshy_AI_Azure_Ranger_biped";
        Assert.Equal(RowanMeshyMotion.RestMd5, Md5(Path.Combine(design, "idle_zip", zip, zip + "_Animation_Idle_02_withSkin.fbx")));
        Assert.Equal(RowanMeshyMotion.WalkMd5, Md5(Path.Combine(design, "walk_zip", zip, zip + "_Animation_Walking_withSkin.fbx")));
        Assert.Equal(RowanMeshyMotion.AttackMd5, Md5(Path.Combine(design, "attack_zip", zip, zip + "_Animation_Archery_Shot_withSkin.fbx")));
    }

    [Fact]
    public void Meshy_textures_match_every_zip_copy_and_themepack()
    {
        var root = FindRepoRoot();
        var design = Path.Combine(root, RowanMeshyMotion.DesignDir);
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
            Assert.True(File.Exists(Path.Combine(tex, file + ".meta")), file);
            foreach (var z in new[] { "idle_zip", "walk_zip", "attack_zip" })
            {
                Assert.Equal(md5, Md5(Path.Combine(design, z, "Meshy_AI_Azure_Ranger_biped", file)));
            }
        }
    }

    [Fact]
    public void Design_folder_is_committed_file_for_file()
    {
        var design = Path.Combine(FindRepoRoot(), RowanMeshyMotion.DesignDir);
        var files = Directory.GetFiles(design, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(design, f).Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(27, files.Length);
        foreach (var expected in new[]
        {
            "ROWAN_meshy_rest.fbx", "ROWAN_meshy_walk.fbx", "ROWAN_meshy_attack.fbx",
            "meshy_attack_preview.png", "meshy_walk_preview.png",
            "_zips/attack_Archery_Shot.zip", "_zips/idle_Idle1.zip", "_zips/rigged_character_glb.zip", "_zips/walk_Walking.zip",
            "glb_zip/Meshy_AI_Azure_Ranger_biped/Meshy_AI_Azure_Ranger_biped_Animation_Archery_Shot_withSkin.glb",
            "glb_zip/Meshy_AI_Azure_Ranger_biped/Meshy_AI_Azure_Ranger_biped_Animation_Running_withSkin.glb",
            "glb_zip/Meshy_AI_Azure_Ranger_biped/Meshy_AI_Azure_Ranger_biped_Animation_Walking_withSkin.glb",
        })
        {
            Assert.Contains(expected, files);
        }
    }

    [Fact]
    public void Fbx_headers_say_24_fps_cm_units_and_take_lengths_match_the_spec()
    {
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        foreach (var clip in RowanMeshyMotion.Spec.Clips)
        {
            var bytes = File.ReadAllBytes(Path.Combine(pack, clip.FileName));
            Assert.Equal(24d, ReadP70Double(bytes, "CustomFrameRate"));
            Assert.Equal(1d, ReadP70Double(bytes, "UnitScaleFactor"));
            Assert.Equal(RowanMeshyMotion.FileFrameRate, clip.FrameRate);
            Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(clip.TakeName + "\0\u0001AnimStack")), clip.TakeName);
            var stop = ReadAllP70Long(bytes, "LocalStop").Max();
            Assert.Equal(clip.LastFrame, (int)Math.Round(stop / FbxTicksPerSecond * clip.FrameRate));
            Assert.Equal(clip.Seconds, (float)(stop / FbxTicksPerSecond), 3);
        }

        Assert.Equal(45, RowanMeshyMotion.RestLastFrame);
        Assert.Equal(25, RowanMeshyMotion.WalkLastFrame);
        Assert.Equal(120, RowanMeshyMotion.AttackLastFrame);
    }

    [Fact]
    public void Bones_bind_by_the_fbx_bone_names_and_every_export_shares_the_skeleton()
    {
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        var spec = RowanMeshyMotion.Spec;
        Assert.Equal(RowanMeshyMotion.BoneCount, spec.BoneNames.Length);
        Assert.Equal(spec.BoneNames.Length, spec.BoneNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(spec.BoneRoot, spec.BoneNames);
        foreach (var bone in MixamoHumanoidBones.Names)
        {
            Assert.Contains(bone, spec.BoneNames);
        }

        var limbTail = new byte[] { (byte)'S', 8, 0, 0, 0 }.Concat(Encoding.ASCII.GetBytes("LimbNode")).ToArray();
        foreach (var clip in spec.Clips)
        {
            var bytes = File.ReadAllBytes(Path.Combine(pack, clip.FileName));
            Assert.Equal(spec.BoneNames.Length, Count(bytes, Encoding.ASCII.GetBytes("\0\u0001Model").Concat(limbTail).ToArray()));
            foreach (var bone in spec.BoneNames)
            {
                Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(bone + "\0\u0001Model").Concat(limbTail).ToArray()), clip.FileName + " " + bone);
            }

            Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(spec.ArmatureName + "\0\u0001Model")), spec.ArmatureName);
            Assert.True(Contains(bytes, Encoding.ASCII.GetBytes(spec.BodyMeshName + "\0\u0001Model")), spec.BodyMeshName);
        }
    }

    [Fact]
    public void Stray_icosphere_in_the_meshy_export_is_hidden_by_name()
    {
        var pack = Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir);
        var bytes = File.ReadAllBytes(Path.Combine(pack, RowanMeshyMotion.RestFileName));
        Assert.True(Contains(bytes, Encoding.ASCII.GetBytes("Icosphere\0\u0001Model")));
        Assert.True(RowanMeshyMotion.Spec.IsHiddenMesh("Icosphere"));
        Assert.False(RowanMeshyMotion.Spec.IsHiddenMesh(RowanMeshyMotion.BodyMeshName));
        var player = ReadUnity("MeshyRigPlayer.cs");
        Assert.Contains("HideStrayMeshes(_instance)", player, StringComparison.Ordinal);
        Assert.Contains("t.gameObject.SetActive(false)", player, StringComparison.Ordinal);
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
    public void Rear_battle_camera_and_attack_aimed_up_the_screen()
    {
        Assert.Equal(PortraitGameView.RearYawDegrees, RowanMeshyMotion.CameraYawDegrees);
        Assert.InRange(RowanMeshyMotion.CameraPitchDegrees, PortraitGameView.PitchMin, -1f);

        // Full draw heads -96.8° (Rowan's left) in Unity; the attack yaw turns that heading to +Z.
        var heading = -96.8f + RowanMeshyMotion.AttackYawDegrees;
        Assert.InRange(heading, -2f, 2f);
        Assert.InRange(RowanMeshyMotion.AttackReleaseFrame, 82, RowanMeshyMotion.AttackLastFrame);

        var demo = ReadUnity("RowanMeshyCompareDemo.cs");
        Assert.Contains("StartYawDegrees => RowanMeshyMotion.CameraYawDegrees", demo, StringComparison.Ordinal);
        Assert.Contains("StartPitchDegrees => RowanMeshyMotion.CameraPitchDegrees", demo, StringComparison.Ordinal);
        var baseDemo = ReadUnity("BlenderRigDemo.cs");
        Assert.Contains("protected virtual float StartYawDegrees => 0f;", baseDemo, StringComparison.Ordinal);
        Assert.Contains("stage.SetView(StartYawDegrees, StartPitchDegrees)", baseDemo, StringComparison.Ordinal);
        var player = ReadUnity("MeshyRigPlayer.cs");
        Assert.Contains("SetYaw(attack ? _spec.AttackYawDegrees : 0f)", player, StringComparison.Ordinal);
    }

    [Fact]
    public void Player_is_in_place_generic_and_honours_the_cm_file_scale()
    {
        var player = ReadUnity("MeshyRigPlayer.cs");
        Assert.Contains("_animator.applyRootMotion = false", player, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", player, StringComparison.Ordinal);
        Assert.Contains("importer.useFileScale = true", player, StringComparison.Ordinal);
        Assert.Contains("importer.bakeAxisConversion = false", player, StringComparison.Ordinal);
        Assert.Contains("CheckBones(_instance)", player, StringComparison.Ordinal);
        Assert.Contains("_spec.AttackClipSeconds(_time", player, StringComparison.Ordinal);
        foreach (var clip in RowanMeshyMotion.Spec.Clips)
        {
            var meta = File.ReadAllText(Path.Combine(FindRepoRoot(), "Assets", RowanMeshyMotion.ThemePackDir, clip.FileName + ".meta"));
            Assert.Contains("useFileScale: 1", meta, StringComparison.Ordinal);
            Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
        }
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
