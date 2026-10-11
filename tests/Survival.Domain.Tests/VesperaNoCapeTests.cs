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
    public void Vespera_uses_derek_16_bone_rig_as_generic_with_reweighted_skin_and_welded_seams()
    {
        Assert.Equal(16, VesperaNoCape.RigBoneCount);
        Assert.Equal(VesperaNoCape.RigBoneCount, VesperaMotion.BoneNames.Length);
        Assert.Equal(16, VesperaMotion.Spec.BoneCount);
        Assert.Equal(BlenderRigAvatar.CustomGeneric, VesperaMotion.Spec.Avatar);
        Assert.False(VesperaMotion.Spec.PreferHumanoid);
        Assert.Equal("mixamorig:Spine1", VesperaMotion.BoneRoot);
        foreach (var gone in VesperaNoCape.DeletedBones)
        {
            Assert.DoesNotContain(gone, VesperaMotion.BoneNames);
        }

        foreach (var bone in VesperaAttack.Spec.CalibrationBones)
        {
            Assert.Contains(bone, VesperaMotion.BoneNames);
        }

        var design = Path.Combine(FindRepoRoot(), VesperaNoCape.DesignDir.Replace('/', Path.DirectorySeparatorChar));
        var work = Path.Combine(FindRepoRoot(), VesperaNoCape.HandWorkDir.Replace('/', Path.DirectorySeparatorChar));
        using var ex = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "export.json")));
        var exported = ex.RootElement.GetProperty("bones").EnumerateArray().Select(b => b.GetString()).ToArray();
        Assert.Equal(VesperaMotion.BoneNames, exported);
        Assert.True(ex.RootElement.GetProperty("max_influences").GetInt32() <= 4);

        using var qc = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "qc_fbx.json")));
        foreach (var kind in new[] { "rest", "walk", "attack" })
        {
            var k = qc.RootElement.GetProperty(kind);
            var bones = k.GetProperty("bones").EnumerateArray().Select(b => b.GetString()).ToArray();
            Assert.Equal(VesperaMotion.BoneNames, bones);
            Assert.True(k.GetProperty("max_infl").GetInt32() <= 4, kind);
        }

        using var st = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "stretch.json")));
        Assert.Equal(0, st.RootElement.GetProperty("VESPERA_nocape_rest").GetProperty("stretch_gt_1.25x").GetInt32());
        Assert.Equal(0, st.RootElement.GetProperty("VESPERA_nocape_walk").GetProperty("grow_gt_2cm").GetInt32());
        Assert.True(st.RootElement.GetProperty("VESPERA_nocape_walk").GetProperty("max_grow_cm").GetDouble() <= 2.0);

        // 507fb9a arm split seams re-welded: no coincident duplicate surface vertices left (flipped lining twins excluded).
        using var weld = JsonDocument.Parse(File.ReadAllText(Path.Combine(design, "work", "reweight_20261010", "weld.json")));
        Assert.True(weld.RootElement.GetProperty("seam_pairs_before").GetInt32() > 500);
        Assert.Equal(0, weld.RootElement.GetProperty("coincident_left").GetInt32());

        // Weights live only on Derek's 16 bones (no deleted-bone groups), <=4 per vertex, none unweighted.
        using var w = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "weights.json")));
        Assert.Equal(0, w.RootElement.GetProperty("unweighted").GetInt32());
        Assert.True(w.RootElement.GetProperty("max_influences").GetInt32() <= 4);
        var dominant = w.RootElement.GetProperty("dominant").EnumerateObject().Select(p => "mixamorig:" + p.Name).ToArray();
        Assert.Equal(VesperaMotion.BoneNames, dominant);
        foreach (var kind in new[] { "rest", "walk", "attack" })
        {
            Assert.Equal(0, qc.RootElement.GetProperty(kind).GetProperty("unweighted").GetInt32());
        }
    }

    [Fact]
    public void Vespera_right_hand_is_free_of_the_skirt_and_the_attack_goes_around_her()
    {
        var work = Path.Combine(FindRepoRoot(), VesperaNoCape.HandWorkDir.Replace('/', Path.DirectorySeparatorChar));

        // Cut: the faces joining the right forearm/hand to the skirt/belt/hip were deleted, and the hand is closed.
        using var cut = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "cut.json")));
        Assert.True(cut.RootElement.GetProperty("faces_deleted").GetInt32() > 0);
        Assert.True(cut.RootElement.GetProperty("holes_filled").GetArrayLength() > 0);

        // Weights either side of the cut, closed hand, no duplicate surface verts, still <=4 and none unweighted.
        using var qc = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "hand_qc.json")));
        var q = qc.RootElement;
        Assert.Equal(0.0, q.GetProperty("hand_piece_max_nonarm_weight").GetDouble());
        Assert.Equal(0.0, q.GetProperty("hand_forearm_max_upperarm_weight").GetDouble());
        Assert.Equal(0.0, q.GetProperty("body_near_hand_max_rightarm_weight").GetDouble());
        Assert.Equal(0, q.GetProperty("hand_lowforearm_open_edges").GetInt32());
        Assert.Equal(0, q.GetProperty("coincident_surface_pairs").GetInt32());
        Assert.True(q.GetProperty("max_influences").GetInt32() <= 4);
        Assert.Equal(0, q.GetProperty("unweighted").GetInt32());

        // Attack: hand + forearm never intersect the body (hair excluded) between the bind-pose ends,
        // and the hand + lower forearm keep >= 3 cm from f2 to f29.
        using var clr = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "clear_attack.json")));
        var frames = clr.RootElement.EnumerateArray().ToArray();
        Assert.Equal(31, frames.Length);
        for (var f = 1; f <= 29; f++)
        {
            Assert.Equal(0, frames[f].GetProperty("body_tri_overlaps").GetInt32());
            if (f >= 2)
            {
                Assert.True(frames[f].GetProperty("min_body_hand_lowforearm_cm").GetDouble() >= 3.0, "attack f" + f);
            }
        }

        // Release in front of her right side (Unity +X is her right), not across the chest.
        var bolt = VesperaAttack.Spec.Tracks[0].Frames[VesperaAttack.ReleaseFrame];
        Assert.True(bolt.X > 0.1f, "bolt spawns on her right side");

        // Stretch: walk 0 edges over 2 cm, attack far better than f41ef77 (206 over 2 cm, max 7.4 cm).
        using var st = JsonDocument.Parse(File.ReadAllText(Path.Combine(work, "stretch.json")));
        Assert.Equal(0, st.RootElement.GetProperty("VESPERA_nocape_walk").GetProperty("grow_gt_2cm").GetInt32());
        Assert.True(st.RootElement.GetProperty("VESPERA_nocape_attack").GetProperty("grow_gt_2cm").GetInt32() <= 20);
        Assert.True(st.RootElement.GetProperty("VESPERA_nocape_attack").GetProperty("max_grow_cm").GetDouble() < 5.0);
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
            Assert.False(BlenderRigImportMeta.NeedsReimport(text, false, clip, pose, "Scene", 30), meta + " would reimport on Play");
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
