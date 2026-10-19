namespace Survival.Domain.Tests;

/// <summary>
/// Derek's Game-view note 2026-10-18 on #70: "some characters are floating, mostly the villains". Every fighter's lowest
/// body point is planted on the ground each frame (rest, walk, every attack) by moving only the actor host, never the rig
/// bones or the clips.
/// </summary>
public sealed class LockedBattleGroundingTests
{
    private static string Unity(string file) =>
        File.ReadAllText(Path.Combine(ArcherExtraAttackTests.FindRepoRoot(), "Assets", "Survival", "Unity", file));

    [Fact]
    public void Every_fighter_is_planted_on_the_ground_by_the_host_only()
    {
        var demo = Unity("LockedBattleDemo.cs");
        Assert.Contains("f.Ground = new LockedBattleGrounding(host.transform);", demo, StringComparison.Ordinal);
        Assert.Contains("f.Ground?.Plant(f.Slot.position.y);", demo, StringComparison.Ordinal);
        Assert.Contains("private void LateUpdate()", demo, StringComparison.Ordinal);
        var ground = Unity("LockedBattleGrounding.cs");
        Assert.Contains("r.BakeMesh(baked, true);", ground, StringComparison.Ordinal);
        Assert.Contains("_host.position += new Vector3(0f, lift, 0f);", ground, StringComparison.Ordinal);
        // body skins only: props and capes never plant the body; no import changes
        Assert.Contains("\"cape\"", ground, StringComparison.Ordinal);
        Assert.DoesNotContain("ModelImporter", ground, StringComparison.Ordinal);
        Assert.DoesNotContain("localRotation", ground, StringComparison.Ordinal);
        Assert.DoesNotContain("Animator", ground, StringComparison.Ordinal);
    }
}
