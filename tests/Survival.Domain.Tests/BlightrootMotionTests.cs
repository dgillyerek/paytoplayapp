using System.Security.Cryptography;
using System.Text;
using Survival.Domain.Enemies;

namespace Survival.Domain.Tests;

public sealed class BlightrootMotionTests
{
    private static readonly string[] ExactOrder =
    {
        "mutant right turn 90",
        "mutant jumping",
        "mutant jumping (2)",
        "mutant walking",
        "mutant swiping",
        "mutant run",
        "mutant dying",
        "mutant idle",
        "mutant breathing idle",
        "mutant right turn 45",
        "mutant idle (2)",
        "mutant right turn 45 (2)",
        "mutant left turn 45",
        "left turn 45",
        "mutant flexing muscles",
        "jump attack",
        "mutant jump attack",
        "mutant punch",
        "mutant roaring",
    };

    [Fact]
    public void Dropdown_order_is_the_exact_mixamo_names()
    {
        Assert.Equal(BlightrootMotion.ClipCount, ExactOrder.Length);
        Assert.Equal(ExactOrder.Length, BlightrootMotion.Clips.Length);
        Assert.Equal("mixamo.com", BlightrootMotion.TakeName);
        Assert.Equal("Scene", BlightrootMotion.RejectedEmptyTakeName);
        Assert.Equal("mixamorig:Hips", BlightrootMotion.BoneRoot);
        Assert.False(BlightrootMotion.Mirror);
        for (var i = 0; i < ExactOrder.Length; i++)
        {
            Assert.Equal(ExactOrder[i], BlightrootMotion.Clips[i].ExactName);
            Assert.True(BlightrootMotion.TryGet(ExactOrder[i], out var clip));
            Assert.Equal(ExactOrder[i], clip.ExactName);
            Assert.False(BlightrootMotion.TryGet(ExactOrder[i].ToUpperInvariant(), out _));
            Assert.False(BlightrootMotion.TryGet(clip.FileName, out _));
            Assert.True(clip.LastFrame > 1f);
            Assert.NotEqual(clip.ExactName, Path.GetFileNameWithoutExtension(clip.FileName));
        }

        Assert.True(BlightrootMotion.TryGet("left turn 45", out var plain));
        Assert.True(BlightrootMotion.TryGet("mutant left turn 45", out var mutant));
        Assert.NotEqual(plain.FileName, mutant.FileName);
        Assert.True(BlightrootMotion.TryGet("mutant idle", out var idle));
        Assert.True(BlightrootMotion.TryGet("mutant idle (2)", out var idle2));
        Assert.NotEqual(idle.FileName, idle2.FileName);
        Assert.True(BlightrootMotion.IsEmptyClip(0f));
        Assert.False(BlightrootMotion.IsEmptyClip(0.5f));
    }

    [Fact]
    public void Game_view_1080x1920_fits_every_clip_including_jumps()
    {
        Assert.Equal(1080f, BlightrootMotion.GameViewWidth);
        Assert.Equal(1920f, BlightrootMotion.GameViewHeight);
        Assert.True(BlightrootMotion.PlayCamZ > 8f);
        Assert.True(
            BlightrootMotion.MeshMaxY - BlightrootMotion.MeshMinY
            > BlightrootMotion.MeshMaxX - BlightrootMotion.MeshMinX);
        var bodyHeight = BlightrootMotion.MeshMaxY - BlightrootMotion.MeshMinY;
        var bodyWidth = BlightrootMotion.MeshMaxX - BlightrootMotion.MeshMinX;
        var bodyDepth = BlightrootMotion.MeshMaxZ - BlightrootMotion.MeshMinZ;
        Assert.InRange(bodyHeight, 1.89d, 1.92d);
        Assert.InRange(bodyWidth, 1.64d, 1.67d);
        Assert.InRange(bodyDepth, 0.75d, 0.79d);
        Assert.True(BlightrootMotion.TryGet("mutant idle", out var planted));
        var idleFoot = BlightrootMotion.MeshMinY + (planted.HipYMin - BlightrootMotion.BindHipY);
        Assert.InRange(idleFoot, BlightrootMotion.GroundY - 0.02d, BlightrootMotion.GroundY + 0.02d);
        Assert.True(BlightrootMotion.GameViewFitsEveryClip());
        Assert.True(BlightrootMotion.TryGet("jump attack", out var jump));
        Assert.True(BlightrootMotion.GameViewFitsClip(jump));
        Assert.True(BlightrootMotion.TryGet("mutant jump attack", out var mutantJump));
        Assert.True(BlightrootMotion.GameViewFitsClip(mutantJump));
        Assert.True(BlightrootMotion.TryGet("mutant dying", out var dying));
        Assert.True(BlightrootMotion.GameViewFitsClip(dying));
        BlightrootMotion.BodyAabb(jump, out _, out _, out _, out _, out var jumpTop, out _);
        BlightrootMotion.BodyAabb(idleOr("mutant idle"), out _, out _, out _, out _, out var idleTop, out _);
        Assert.True(jumpTop > idleTop + 0.8d);
    }

    [Fact]
    public void Files_match_catalog_md5_and_mixamo_take()
    {
        var root = FindRepoRoot();
        var body = Path.Combine(root, "Assets", BlightrootMotion.BodyThemePackRel);
        Assert.True(File.Exists(body));
        Assert.Equal(BlightrootMotion.BodyMd5, Md5(body));
        AssertTake(body);

        foreach (var clip in BlightrootMotion.Clips)
        {
            var path = Path.Combine(root, "Assets", clip.ThemePackRel);
            Assert.True(File.Exists(path), path);
            Assert.Equal(clip.Md5, Md5(path));
            AssertTake(path);
            var meta = File.ReadAllText(path + ".meta");
            Assert.Contains("takeName: mixamo.com", meta, StringComparison.Ordinal);
            Assert.Contains("mirror: 0", meta, StringComparison.Ordinal);
            Assert.Contains("useFileScale: 0", meta, StringComparison.Ordinal);
            Assert.Contains("animationType: 2", meta, StringComparison.Ordinal);
            Assert.Contains("optimizeBones: 0", meta, StringComparison.Ordinal);
            Assert.Contains("bakeAxisConversion: 0", meta, StringComparison.Ordinal);
            Assert.DoesNotContain("bakeAxisConversion: 1", meta, StringComparison.Ordinal);
            Assert.Contains("name: \"" + clip.ExactName + "\"", meta, StringComparison.Ordinal);
            Assert.Contains("lastFrame: " + clip.LastFrame.ToString("0"), meta, StringComparison.Ordinal);
            Assert.DoesNotContain("takeName: Scene", meta, StringComparison.Ordinal);
            Assert.DoesNotContain("mirror: 1", meta, StringComparison.Ordinal);
        }

        var bodyMeta = File.ReadAllText(body + ".meta");
        Assert.Contains("takeName: mixamo.com", bodyMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", bodyMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", bodyMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", bodyMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion: 1", bodyMeta, StringComparison.Ordinal);
        Assert.DoesNotContain("takeName: Scene", bodyMeta, StringComparison.Ordinal);
    }

    [Fact]
    public void Actor_plays_named_clips_on_the_skinned_body_and_rejects_empty_takes()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlightrootActor.cs"));
        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlightrootDemo.cs"));
        Assert.Contains("BlightrootDualWeaponCombo", actor, StringComparison.Ordinal);
        Assert.Contains("ThemePackBody", actor, StringComparison.Ordinal);
        Assert.Contains("PlayNamedClip", actor, StringComparison.Ordinal);
        Assert.Contains("clip.empty", actor, StringComparison.Ordinal);
        Assert.Contains("BlightrootMotion.TakeName", actor, StringComparison.Ordinal);
        Assert.Contains("RejectedEmptyTakeName", actor, StringComparison.Ordinal);
        Assert.Contains("ModelImporterAnimationType.Generic", actor, StringComparison.Ordinal);
        Assert.Contains("useFileScale = false", actor, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion = false", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion = true", actor, StringComparison.Ordinal);
        Assert.Contains("Keep the imported root rotation", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation = Quaternion.identity", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation = Quaternion.Euler", actor, StringComparison.Ordinal);
        Assert.Contains("mirror = false", actor, StringComparison.Ordinal);
        Assert.Contains("localScale = Vector3.one", actor, StringComparison.Ordinal);
        Assert.Contains("localScale = Vector3.one * factor", actor, StringComparison.Ordinal);
        Assert.Contains("BodyHeightMinMeters = 0.5f", actor, StringComparison.Ordinal);
        Assert.Contains("BodyHeightMaxMeters = 5f", actor, StringComparison.Ordinal);
        Assert.Contains("BodyHeightTargetMeters = 1.80f", actor, StringComparison.Ordinal);
        var aldric = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "SirAldricMeshyAnimateActor.cs"));
        Assert.Contains("BodyHeightMinMeters = 0.5f", aldric, StringComparison.Ordinal);
        Assert.Contains("BodyHeightMaxMeters = 5f", aldric, StringComparison.Ordinal);
        Assert.Contains("BodyHeightTargetMeters = 1.80f", aldric, StringComparison.Ordinal);
        Assert.DoesNotContain("new Vector3(", actor, StringComparison.Ordinal);
        Assert.Contains("keepOriginalPositionY = true", actor, StringComparison.Ordinal);
        Assert.Contains("keepOriginalPositionXZ = true", actor, StringComparison.Ordinal);
        Assert.Contains("isHuman", actor, StringComparison.Ordinal);
        Assert.Contains("SetSpeed(1d)", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimationType.Humanoid", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("mirror = true", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("SetSpeed(-", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Scene\"", actor, StringComparison.Ordinal);

        Assert.Contains("BlightrootClipDropdown", demo, StringComparison.Ordinal);
        Assert.Contains("BlightrootMotion.Clips", demo, StringComparison.Ordinal);
        Assert.Contains("ExactName", demo, StringComparison.Ordinal);
        Assert.Contains("ApplyPlayCam", demo, StringComparison.Ordinal);
        Assert.Contains("PlayCamFovDegrees", demo, StringComparison.Ordinal);
        Assert.Contains("BlightrootMotion.GroundY", demo, StringComparison.Ordinal);
        Assert.DoesNotContain("BtnClip_", demo, StringComparison.Ordinal);

        var scene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Blightroot.unity"));
        Assert.Contains("guid: e7c3a1b24d5f4e8a9c0b6d2f11a8e5c4", scene, StringComparison.Ordinal);
        Assert.Contains("BlightrootRoot", scene, StringComparison.Ordinal);
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Blightroot.unity", editor, StringComparison.Ordinal);
    }

    private static BlightrootMotion.Clip idleOr(string exact)
    {
        Assert.True(BlightrootMotion.TryGet(exact, out var clip));
        return clip;
    }

    private static void AssertTake(string path)
    {
        var text = Encoding.Latin1.GetString(File.ReadAllBytes(path));
        Assert.Contains("mixamo.com", text, StringComparison.Ordinal);
        Assert.Contains("AnimationStack", text, StringComparison.Ordinal);
        Assert.Contains("mixamorig:Hips", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Armature", text, StringComparison.Ordinal);
    }

    private static string Md5(string path)
    {
        var hash = MD5.HashData(File.ReadAllBytes(path));
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            chars[i * 2] = "0123456789abcdef"[hash[i] >> 4];
            chars[i * 2 + 1] = "0123456789abcdef"[hash[i] & 0xf];
        }

        return new string(chars);
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
