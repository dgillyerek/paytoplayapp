using System.Security.Cryptography;
using System.Text.Json;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

/// <summary>Oakenshield rigid-leaves pack (Design rigidleaves_20261010). HOLD merge until Derek Game-view PASS.</summary>
public sealed class OakenshieldRigidLeavesTests
{
    [Fact]
    public void Oakenshield_themepack_rest_walk_attack_are_the_rigidleaves_fbxs()
    {
        var root = FindRepoRoot();
        var pack = Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir);
        var design = Path.Combine(root, OakenshieldRigidLeaves.DesignDir.Replace('/', Path.DirectorySeparatorChar));
        var pairs = new[]
        {
            (OakenshieldMotion.RestFileName, OakenshieldRigidLeaves.RestFileName, OakenshieldRigidLeaves.RestMd5),
            (OakenshieldMotion.WalkFileName, OakenshieldRigidLeaves.WalkFileName, OakenshieldRigidLeaves.WalkMd5),
            (OakenshieldMotion.AttackFileName, OakenshieldRigidLeaves.AttackFileName, OakenshieldRigidLeaves.AttackMd5)
        };
        foreach (var (packName, designName, md5) in pairs)
        {
            Assert.Equal(md5, Md5(Path.Combine(pack, packName)));
            Assert.Equal(md5, Md5(Path.Combine(design, designName)));
            Assert.True(File.Exists(Path.Combine(pack, packName + ".meta")), packName + ".meta keeps its GUID");
        }

        Assert.Equal(OakenshieldRigidLeaves.AttackMd5, OakenshieldAttack.FileMd5);
        Assert.Equal(OakenshieldRigidLeaves.BlendMd5, Md5(Path.Combine(design, OakenshieldRigidLeaves.BlendFileName)));
        Assert.NotEqual(OakenshieldRigidLeaves.ClothSplitRestMd5, OakenshieldRigidLeaves.RestMd5);
        Assert.NotEqual(OakenshieldRigidLeaves.ClothSplitWalkMd5, OakenshieldRigidLeaves.WalkMd5);
        Assert.NotEqual(OakenshieldRigidLeaves.ClothSplitAttackMd5, OakenshieldRigidLeaves.AttackMd5);

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
    public void Oakenshield_rigidleaves_rig_is_22_body_bones_with_clean_weights_and_no_stretch()
    {
        Assert.Equal(22, OakenshieldRigidLeaves.RigBoneCount);
        Assert.Equal(OakenshieldRigidLeaves.RigBoneCount, OakenshieldMotion.BoneNames.Length);
        Assert.Equal(MixamoHumanoidBones.Names, OakenshieldMotion.BoneNames);
        foreach (var bone in OakenshieldMotion.BoneNames)
        {
            Assert.StartsWith("mixamorig:", bone, StringComparison.Ordinal);
        }

        var work = Path.Combine(FindRepoRoot(), OakenshieldRigidLeaves.DesignDir.Replace('/', Path.DirectorySeparatorChar), "work");
        using var fin = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "finalize.json")));
        Assert.Equal(4, fin.RootElement.GetProperty("max_influences").GetInt32());
        Assert.Equal(0, fin.RootElement.GetProperty("unweighted").GetInt32());

        using var qc = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "qc_fbx.json")));
        foreach (var kind in new[] { "rest", "walk", "attack" })
        {
            var k = qc.RootElement.GetProperty(kind);
            var bones = k.GetProperty("bones").EnumerateArray().Select(b => b.GetString()).ToArray();
            Assert.Equal(OakenshieldMotion.BoneNames, bones);
            Assert.True(k.GetProperty("max_infl").GetInt32() <= 4, kind);
            Assert.Equal(0, k.GetProperty("unweighted").GetInt32());
            Assert.DoesNotContain(bones, b => b!.Contains("leaf_", StringComparison.OrdinalIgnoreCase) || b!.Contains("vine_", StringComparison.OrdinalIgnoreCase));
        }

        using var st = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "stretch.json")));
        var rest = st.RootElement.GetProperty("OAKENSHIELD_rest:OAKENSHIELD_body");
        Assert.Equal(0, rest.GetProperty("grow_gt_2cm").GetInt32());
        var walk = st.RootElement.GetProperty("OAKENSHIELD_walk:OAKENSHIELD_body");
        Assert.Equal(0, walk.GetProperty("grow_gt_5cm").GetInt32());
        Assert.True(walk.GetProperty("grow_gt_2cm").GetInt32() < 200);
        Assert.True(walk.GetProperty("max_grow_cm").GetDouble() < 5.0);
        var attack = st.RootElement.GetProperty("OAKENSHIELD_attack:OAKENSHIELD_body");
        Assert.True(attack.GetProperty("grow_gt_5cm").GetInt32() < 50);
        Assert.True(attack.GetProperty("max_grow_cm").GetDouble() < 10.0);

        // loose rigid pieces stay on the surface they sit on (no floating leaves)
        using var gap = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "gap.json")));
        foreach (var act in new[] { "OAKENSHIELD_walk", "OAKENSHIELD_attack" })
        {
            Assert.True(gap.RootElement.GetProperty(act).GetProperty("max_gap_open_cm").GetDouble() < 1.0, act);
        }

        using var rig = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "rig.json")));
        Assert.Equal(0, rig.RootElement.GetProperty("leg_verts_with_torso_or_arm_weight").GetInt32());
        Assert.Equal(0, rig.RootElement.GetProperty("torso_verts_with_forearm_or_hand_weight").GetInt32());
        Assert.True(rig.RootElement.GetProperty("rigid_pieces").GetInt32() > 100);
    }

    [Fact]
    public void Oakenshield_actor_has_no_cloth_springs_and_keeps_the_demo_menu()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "OakenshieldActor.cs"));
        Assert.DoesNotContain("ClothSplit", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("ClothSpring", actor, StringComparison.Ordinal);
        Assert.Contains("OakenshieldAttack.Spec", actor, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "Assets", "Survival", "Runtime", "Heroes", "OakenshieldClothSplit.cs")));
        var editor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("Survival/Oakenshield Demo (rest + walk + attack, Game view 1080x1920)", editor, StringComparison.Ordinal);
        foreach (var meta in new[] { "OAKENSHIELD_blenderig.fbx.meta", "OAKENSHIELD_blenderig_walk.fbx.meta", "OAKENSHIELD_blenderig_attack.fbx.meta" })
        {
            var text = File.ReadAllText(Path.Combine(root, "Assets", OakenshieldMotion.ThemePackDir, meta));
            var clip = meta != "OAKENSHIELD_blenderig.fbx.meta";
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
