using System.Text;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.View;

namespace Survival.Domain.Tests;

public sealed class ThemeARosterTests
{
    [Fact]
    public void Portrait_view_is_1080_by_1920()
    {
        Assert.Equal(1080f, PortraitGameView.Width);
        Assert.Equal(1920f, PortraitGameView.Height);
        Assert.Equal(54f, PortraitGameView.FovDegrees);
        Assert.Equal(0.25f, PortraitGameView.Near);
        Assert.True(PortraitGameView.Far >= 4000f);
        var body = PortraitGameView.FrameDistance(1.65f, 1.90f, 0.77f);
        var larger = PortraitGameView.FrameDistance(16.5f, 19.0f, 7.7f);
        Assert.True(body > 1f);
        Assert.True(larger > body);
        PortraitGameView.OrbitEye(0f, 1f, 0f, 10f, 0f, 0f, out var x, out var y, out var z);
        Assert.Equal(0f, x, 3);
        Assert.True(z > 9f);
        Assert.True(y > 1f);
    }

    [Fact]
    public void Emberfang_uses_the_55_design_bones_and_scene_flap()
    {
        Assert.Equal(55, EmberfangMotion.BoneCount);
        Assert.Equal(55, EmberfangMotion.BoneNames.Length);
        Assert.Equal(EmberfangMotion.BoneNames.Length, EmberfangMotion.BoneNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("root", EmberfangMotion.BoneNames[0]);
        Assert.Contains("spine_01", EmberfangMotion.BoneNames);
        Assert.Contains("wing_root.L", EmberfangMotion.BoneNames);
        Assert.Equal("rest", EmberfangMotion.PoseNames[0]);
        Assert.Equal("wing flap", EmberfangMotion.PoseNames[1]);
        Assert.Equal("Scene", EmberfangMotion.FlapTakeName);
        Assert.NotEqual("mixamo.com", EmberfangMotion.FlapTakeName);
        Assert.Equal(1f, EmberfangMotion.FlapSeconds);
        Assert.Equal(30f, EmberfangMotion.FlapFrameRate);
        Assert.Equal(30, EmberfangMotion.FlapLastFrame);
        Assert.Equal("EMBERFANG_rig.fbx", EmberfangMotion.RejectedMixamoFileName);
        Assert.DoesNotContain("mixamorig", File.ReadAllText(MotionPath("Heroes", "EmberfangMotion.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Ironhowl_is_a_tpose_with_no_clip_list()
    {
        Assert.Equal("IRONHOWL_rig.fbx", IronhowlMotion.BodyFileName);
        Assert.Equal("Ironhowl_meshy_look.jpg", IronhowlMotion.LookReferenceFileName);
        Assert.Equal("mixamorig:Hips", IronhowlMotion.BoneRoot);
        var source = File.ReadAllText(MotionPath("Enemies", "IronhowlMotion.cs"));
        Assert.DoesNotContain("ExactName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Clips", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Rig_files_match_the_handoff_and_skip_the_mixamo_dragon()
    {
        var root = FindRepoRoot();
        var iron = Path.Combine(root, "Assets", IronhowlMotion.BodyThemePackRel);
        var rest = Path.Combine(root, "Assets", EmberfangMotion.RestThemePackRel);
        var flap = Path.Combine(root, "Assets", EmberfangMotion.FlapThemePackRel);
        var rejected = Path.Combine(root, "Assets", EmberfangMotion.ThemePackDir, EmberfangMotion.RejectedMixamoFileName);
        Assert.True(File.Exists(iron));
        Assert.True(File.Exists(rest));
        Assert.True(File.Exists(flap));
        Assert.False(File.Exists(rejected));
        Assert.True(ContainsAscii(iron, "mixamorig:Hips"));
        Assert.True(ContainsAscii(rest, "wing_root.L"));
        Assert.True(ContainsAscii(rest, "spine_01"));
        Assert.False(ContainsAscii(rest, "mixamorig"));
        Assert.True(ContainsAscii(flap, "wing_root.L"));
        Assert.True(ContainsAscii(flap, "Scene"));
        Assert.False(ContainsAscii(flap, "mixamorig"));

        var ironMeta = File.ReadAllText(iron + ".meta");
        Assert.Contains("clipAnimations: []", ironMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", ironMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", ironMeta, StringComparison.Ordinal);
        Assert.Contains("useFileScale: 0", ironMeta, StringComparison.Ordinal);

        var flapMeta = File.ReadAllText(flap + ".meta");
        Assert.Contains("name: \"wing flap\"", flapMeta, StringComparison.Ordinal);
        Assert.Contains("takeName: Scene", flapMeta, StringComparison.Ordinal);
        Assert.Contains("lastFrame: 30", flapMeta, StringComparison.Ordinal);
        Assert.Contains("mirror: 0", flapMeta, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion: 0", flapMeta, StringComparison.Ordinal);
        Assert.Contains("animationType: 2", flapMeta, StringComparison.Ordinal);
    }

    [Fact]
    public void Actors_do_not_invent_textures_or_rewrite_the_rig()
    {
        var root = FindRepoRoot();
        var iron = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "IronhowlActor.cs"));
        var fang = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "EmberfangActor.cs"));
        Assert.DoesNotContain("Ironhowl_meshy_look", iron, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation =", iron, StringComparison.Ordinal);
        Assert.DoesNotContain("localScale =", iron, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion = true", iron, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion = false", iron, StringComparison.Ordinal);
        Assert.Contains("No clips", iron, StringComparison.Ordinal);
        Assert.DoesNotContain("EMBERFANG_dragonrig_sheet", fang, StringComparison.Ordinal);
        Assert.DoesNotContain("mixamorig", fang, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation =", fang, StringComparison.Ordinal);
        Assert.DoesNotContain("localScale =", fang, StringComparison.Ordinal);
        Assert.DoesNotContain("bakeAxisConversion = true", fang, StringComparison.Ordinal);
        Assert.Contains("bakeAxisConversion = false", fang, StringComparison.Ordinal);
        Assert.Contains("RejectedMixamoFileName", fang, StringComparison.Ordinal);
        Assert.Contains("PlayPose", fang, StringComparison.Ordinal);
    }

    [Fact]
    public void Working_roster_scenes_are_listed_and_the_held_names_are_absent()
    {
        var root = FindRepoRoot();
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        var build = File.ReadAllText(Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset"));
        foreach (var scene in new[] { "SirAldric.unity", "Blightroot.unity", "Ironhowl.unity", "Emberfang.unity", "Vespera.unity" })
        {
            Assert.True(File.Exists(Path.Combine(root, "Assets", "Survival", "Scenes", scene)));
            Assert.Contains(scene, editor, StringComparison.Ordinal);
            Assert.Contains(scene, build, StringComparison.Ordinal);
        }

        var ironScene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Ironhowl.unity"));
        var fangScene = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Scenes", "Emberfang.unity"));
        Assert.Contains("IronhowlRoot", ironScene, StringComparison.Ordinal);
        Assert.Contains("7c1e9a24b6d84f0a9e3c5b71d2a84f06", ironScene, StringComparison.Ordinal);
        Assert.Contains("EmberfangRoot", fangScene, StringComparison.Ordinal);
        Assert.Contains("8d2f0b35c7e94a1b0f4d6c82e3b95a17", fangScene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", ironScene, StringComparison.Ordinal);
        Assert.Contains("field of view: 54", fangScene, StringComparison.Ordinal);

        var ironDemo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "IronhowlDemo.cs"));
        var fangDemo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "EmberfangDemo.cs"));
        Assert.Contains("IRONHOWL", ironDemo, StringComparison.Ordinal);
        Assert.Contains("T-pose · no clips", ironDemo, StringComparison.Ordinal);
        Assert.DoesNotContain("Dropdown", ironDemo, StringComparison.Ordinal);
        Assert.Contains("EmberfangClipDropdown", fangDemo, StringComparison.Ordinal);
        Assert.Contains("RestPoseName", fangDemo, StringComparison.Ordinal);
        Assert.Contains("SetValueWithoutNotify(0)", fangDemo, StringComparison.Ordinal);

        var excluded = new[]
        {
            "Rowan", "Lyra", "Stormcrest", "Oakenshield", "Ashwyrm", "Nightfang", "Bonequill"
        };
        var scenes = Path.Combine(root, "Assets", "Survival", "Scenes");
        var pack = Path.Combine(root, "Assets", "ThemePack");
        foreach (var name in excluded)
        {
            Assert.Empty(Directory.GetFiles(scenes, "*" + name + "*", SearchOption.AllDirectories));
            Assert.Empty(Directory.GetFiles(pack, "*" + name + "*", SearchOption.AllDirectories));
        }
    }

    private static string MotionPath(string folder, string file)
    {
        return Path.Combine(FindRepoRoot(), "Assets", "Survival", "Runtime", folder, file);
    }

    private static bool ContainsAscii(string path, string needle)
    {
        var bytes = File.ReadAllBytes(path);
        var n = Encoding.ASCII.GetBytes(needle);
        for (var i = 0; i <= bytes.Length - n.Length; i++)
        {
            var hit = true;
            for (var j = 0; j < n.Length; j++)
            {
                if (bytes[i + j] != n[j])
                {
                    hit = false;
                    break;
                }
            }

            if (hit)
            {
                return true;
            }
        }

        return false;
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
