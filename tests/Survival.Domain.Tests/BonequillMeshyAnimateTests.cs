using Survival.Domain.Enemies;

namespace Survival.Domain.Tests;

public sealed class BonequillMeshyAnimateTests
{
    [Fact]
    public void Package_is_accurig_humanoid_walk_v2_with_atlas_and_real_normal()
    {
        Assert.Equal(180f, BonequillAnimateWalk.MixamoImportRearYawDegrees);
        Assert.Equal("Armature|Armature|Armature|Walking", BonequillAnimateWalk.WalkingTakeName);
        Assert.Equal("Bonequill_AccuRIG_Continuous_V2", BonequillAnimateWalk.MeshName);
        Assert.Contains("silhouette is continuous", BonequillAnimateWalk.SoftLeftover, StringComparison.Ordinal);
        Assert.Contains("bow character-RIGHT", BonequillAnimateWalk.SoftLeftover, StringComparison.Ordinal);
        Assert.Contains("quiver character-LEFT", BonequillAnimateWalk.SoftLeftover, StringComparison.Ordinal);
        Assert.Contains("CANCELLED", BonequillAnimateWalk.Authorship, StringComparison.Ordinal);
        Assert.Contains("No Design PASS", BonequillAnimateWalk.Authorship, StringComparison.Ordinal);

        var root = FindRepoRoot();
        var dir = Path.Combine(root, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "enemies", "3d");
        var fbx = Path.Combine(dir, "bonequill_meshy_animate_walk_v2.fbx");
        var atlas = Path.Combine(dir, "bonequill_meshy_animate_walk_v2_atlas.png");
        var normal = Path.Combine(dir, "bonequill_meshy_animate_walk_v2_normal.png");
        Assert.True(new FileInfo(fbx).Length > 20_000_000);
        Assert.True(new FileInfo(atlas).Length > 100_000);
        Assert.True(new FileInfo(normal).Length > 100_000);
        Assert.False(File.ReadAllBytes(atlas).SequenceEqual(File.ReadAllBytes(normal)));

        var bytes = File.ReadAllBytes(fbx);
        Assert.True(IndexOfAscii(bytes, "mixamorig") < 0);
        Assert.True(IndexOfAscii(bytes, "Bonequill_AccuRIG_BodyOnly") < 0);
        Assert.True(IndexOfAscii(bytes, "Bonequill_AccuRIG_Continuous_V2") >= 0);
        Assert.True(IndexOfAscii(bytes, "Armature|Armature|Armature|Walking") >= 0);
        Assert.True(IndexOfAscii(bytes, "Hips") >= 0);
        Assert.True(IndexOfAscii(bytes, "Spine02") >= 0);
        Assert.True(IndexOfAscii(bytes, "LeftFoot") >= 0);
        var png = false;
        for (var i = 0; i < bytes.Length - 3; i++)
        {
            if (bytes[i] == 0x89 && bytes[i + 1] == 0x50 && bytes[i + 2] == 0x4E && bytes[i + 3] == 0x47)
            {
                png = true;
                break;
            }
        }

        Assert.True(png);

        var meta = File.ReadAllText(fbx + ".meta");
        Assert.Contains("takeName: Armature|Armature|Armature|Walking", meta, StringComparison.Ordinal);
        Assert.Contains("name: Walking", meta, StringComparison.Ordinal);
        Assert.Contains("animationType: 3", meta, StringComparison.Ordinal);
        Assert.Contains("addHumanoidExtraRoot: 1", meta, StringComparison.Ordinal);
    }

    [Fact]
    public void Actor_is_walk_only_atlas_bind_no_path_a()
    {
        var root = FindRepoRoot();
        var actor = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BonequillMeshyAnimateActor.cs"));
        Assert.Contains("Path A weight-paint is CANCELLED", actor, StringComparison.Ordinal);
        Assert.Contains("SetTexture(\"_BaseMap\"", actor, StringComparison.Ordinal);
        Assert.Contains("SetTexture(\"_BumpMap\"", actor, StringComparison.Ordinal);
        Assert.Contains("RepairWalkingTake", actor, StringComparison.Ordinal);
        Assert.Contains("RearYawDegrees", actor, StringComparison.Ordinal);
        Assert.Contains("Quaternion.Euler(0f, RearYawDegrees, 0f)", actor, StringComparison.Ordinal);
        Assert.Contains("bonequill_meshy_animate_walk_v2", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectInput(1", actor, StringComparison.Ordinal);
        Assert.DoesNotContain("weight-paint remap", actor, StringComparison.Ordinal);

        var demo = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BonequillDemo.cs"));
        Assert.Contains("new Vector3(0f, 2.80f, -5.40f)", demo, StringComparison.Ordinal);
        Assert.Contains("new Vector3(0f, 0.90f, 0.50f)", demo, StringComparison.Ordinal);
        Assert.Contains("no Design PASS", demo, StringComparison.Ordinal);
        Assert.Contains("walk-only v2", demo, StringComparison.Ordinal);
        Assert.Contains("continuous body", demo, StringComparison.Ordinal);
    }

    [Fact]
    public void World_stills_and_walk_proof_are_on_disk()
    {
        var dir = Path.Combine(FindRepoRoot(), "Docs", "Survival", "previews", "bonequill_20260925");
        Assert.True(new FileInfo(Path.Combine(dir, "WIRE.md")).Length > 200);
        Assert.True(new FileInfo(Path.Combine(dir, "bonequill_v2_world_front.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "bonequill_v2_world_34_front.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "bonequill_v2_world_rear.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "bonequill_v2_world_rear_34.png")).Length > 50_000);
        Assert.True(new FileInfo(Path.Combine(dir, "bonequill_v2_walk_toward_top_rear.mp4")).Length > 100_000);
    }

    private static int IndexOfAscii(byte[] hay, string needle)
    {
        var n = System.Text.Encoding.ASCII.GetBytes(needle);
        var last = hay.Length - n.Length;
        for (var i = 0; i <= last; i++)
        {
            var ok = true;
            for (var j = 0; j < n.Length; j++)
            {
                if (hay[i + j] != n[j])
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                return i;
            }
        }

        return -1;
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
