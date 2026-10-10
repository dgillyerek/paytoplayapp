using System.Security.Cryptography;
using System.Text.Json;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Vespera no-cape pack (Design blender_rig_nocape_20261009). HOLD merge until Derek Game-view PASS.</summary>
public sealed class VesperaNoCapeTests
{
    [Fact]
    public void Vespera_themepack_rest_walk_attack_are_the_nocape_fbxs()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", VesperaMotion.ThemePackDir);
        var design = Path.Combine(root, VesperaNoCape.DesignDir.Replace('/', Path.DirectorySeparatorChar));
        var pairs = new[]
        {
            (VesperaMotion.RestFileName, VesperaNoCape.RestFileName, VesperaNoCape.RestMd5),
            (VesperaMotion.WalkFileName, VesperaNoCape.WalkFileName, VesperaNoCape.WalkMd5),
            (VesperaMotion.AttackFileName, VesperaNoCape.AttackFileName, VesperaNoCape.AttackMd5)
        };
        foreach (var (packName, designName, md5) in pairs)
        {
            Assert.Equal(md5, Md5(Path.Combine(pack, packName)));
            Assert.Equal(md5, Md5(Path.Combine(design, designName)));
            Assert.True(File.Exists(Path.Combine(pack, packName + ".meta")), packName + ".meta keeps its GUID");
        }

        Assert.Equal(VesperaNoCape.AttackMd5, VesperaAttack.FileMd5);
        Assert.Equal(VesperaNoCape.BlendMd5, Md5(Path.Combine(design, VesperaNoCape.BlendFileName)));
        Assert.NotEqual(VesperaNoCape.ClothSplitRestMd5, VesperaNoCape.RestMd5);
        Assert.NotEqual(VesperaNoCape.ClothSplitWalkMd5, VesperaNoCape.WalkMd5);
        Assert.NotEqual(VesperaNoCape.ClothSplitAttackMd5, VesperaNoCape.AttackMd5);

        var verified = 0;
        foreach (var line in File.ReadAllLines(Path.Combine(design, "CHECKSUMS.md5")))
        {
            var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || parts[0].Length != 32)
            {
                continue;
            }

            var local = Path.Combine(design, parts[1].TrimStart('*').Trim().Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(local), local);
            Assert.Equal(parts[0], Md5(local));
            verified++;
        }

        Assert.True(verified >= 5, "CHECKSUMS.md5 verified " + verified);
    }

    [Fact]
    public void Vespera_nocape_rig_is_22_body_bones_with_clean_weights()
    {
        Assert.Equal(22, VesperaNoCape.RigBoneCount);
        Assert.Equal(VesperaNoCape.RigBoneCount, VesperaMotion.BoneNames.Length);
        Assert.Equal(MixamoHumanoidBones.Names, VesperaMotion.BoneNames);
        foreach (var bone in VesperaMotion.BoneNames)
        {
            Assert.StartsWith("mixamorig:", bone, StringComparison.Ordinal);
        }

        var work = Path.Combine(FindRepoRoot(), VesperaNoCape.DesignDir.Replace('/', Path.DirectorySeparatorChar), "work");
        using var fin = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "finalize.json")));
        Assert.Equal(4, fin.RootElement.GetProperty("max_influences").GetInt32());
        Assert.Equal(0, fin.RootElement.GetProperty("unweighted").GetInt32());

        using var qc = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "qc_fbx.json")));
        foreach (var kind in new[] { "rest", "walk", "attack" })
        {
            var k = qc.RootElement.GetProperty(kind);
            var bones = k.GetProperty("bones").EnumerateArray().Select(b => b.GetString()).ToArray();
            Assert.Equal(VesperaMotion.BoneNames, bones);
            Assert.True(k.GetProperty("max_infl").GetInt32() <= 4, kind);
            Assert.Equal(0, k.GetProperty("unweighted").GetInt32());
            Assert.DoesNotContain(bones, b => b!.Contains("cape", StringComparison.OrdinalIgnoreCase) || b!.Contains("skirt", StringComparison.OrdinalIgnoreCase));
        }

        using var st = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "stretch.json")));
        var rest = st.RootElement.GetProperty("VESPERA_nocape_rest");
        Assert.Equal(0, rest.GetProperty("stretch_gt_1.25x").GetInt32());
        var walk = st.RootElement.GetProperty("VESPERA_nocape_walk");
        Assert.Equal(0, walk.GetProperty("grow_gt_5cm").GetInt32());
        Assert.True(walk.GetProperty("max_grow_cm").GetDouble() < 3.0);
        var attack = st.RootElement.GetProperty("VESPERA_nocape_attack");
        Assert.True(attack.GetProperty("max_grow_cm").GetDouble() < 10.0);
    }

    [Fact]
    public void Vespera_actor_has_no_cloth_springs_and_keeps_the_demo_menu()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "VesperaActor.cs"));
        Assert.DoesNotContain("ClothSplit", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("ClothSpring", actor, StringComparison.Ordinal);
        Assert.Contains("VesperaAttack.Spec", actor, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "Assets", "Survival", "Runtime", "Enemies", "VesperaClothSplit.cs")));
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Vespera Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        foreach (var meta in new[] { "VESPERA_blenderig.fbx.meta", "VESPERA_blenderig_walk.fbx.meta", "VESPERA_blenderig_attack.fbx.meta" })
        {
            var text = File.ReadAllText(Path.Combine(root, "Assets", VesperaMotion.ThemePackDir, meta));
            var clip = meta != "VESPERA_blenderig.fbx.meta";
            var pose = meta.Contains("walk") ? "walk" : "attack";
            Assert.False(BlenderRigImportMeta.NeedsReimport(text, true, clip, pose, "Scene", 30), meta + " would reimport on Play");
        }
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
