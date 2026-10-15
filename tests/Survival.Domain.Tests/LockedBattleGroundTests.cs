using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class LockedBattleGroundTests
{
    [Fact]
    public void Ground_textures_sit_next_to_the_world_terrain_and_are_wired_into_the_battle()
    {
        var root = FindRepoRoot();
        var assets = Path.Combine(root, "Assets");
        Assert.True(File.Exists(Path.Combine(assets, LockedBattleGroundSpec.MapDir, LockedBattleGroundSpec.WorldTerrainFileName)));
        foreach (var rel in new[] { LockedBattleGroundSpec.GrassTileRel, LockedBattleGroundSpec.DirtTrackRel })
        {
            var path = Path.Combine(assets, rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), rel);
            Assert.True(File.Exists(path + ".meta"), rel);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray());
            var meta = File.ReadAllText(path + ".meta");
            Assert.Contains("wrapU: 0", meta, StringComparison.Ordinal);
            Assert.Contains("wrapV: 0", meta, StringComparison.Ordinal);
        }

        var demo = File.ReadAllText(Path.Combine(assets, "Survival", "Unity", "LockedBattleDemo.cs"));
        Assert.Contains("LockedBattleGround.Build(transform)", demo, StringComparison.Ordinal);
        Assert.Contains("LockedBattleGround.AddContactShadow(", demo, StringComparison.Ordinal);
        var camera = File.ReadAllText(Path.Combine(assets, "Survival", "Unity", "LockedBattleCamera.cs"));
        Assert.Contains("LockedBattleGround.Horizon", camera, StringComparison.Ordinal);
        var ground = File.ReadAllText(Path.Combine(assets, "Survival", "Unity", "LockedBattleGround.cs"));
        Assert.Contains("LockedBattleRoster.LaneX(lane)", ground, StringComparison.Ordinal);
        Assert.Contains("TextureWrapMode.Repeat", ground, StringComparison.Ordinal);
    }

    [Fact]
    public void Ground_covers_every_lane_fades_into_the_horizon_and_leaves_the_lanes_alone()
    {
        // Lanes unchanged by the ground.
        Assert.Equal(1.15f, LockedBattleRoster.LaneHalfSpacingMetres, 4);
        Assert.Equal(2.45f, LockedBattleRoster.OuterLaneHalfSpacingMetres, 4);

        // Plane and tracks cover every lane from start to start, with room to spare.
        Assert.True(LockedBattleGroundSpec.GroundSizeMetres / 2f > LockedBattleGroundSpec.FogEndMetres);
        Assert.True(LockedBattleGroundSpec.TrackLengthMetres > LockedBattleRoster.StartHalfGapMetres * 2f + 1f);
        Assert.True(LockedBattleGroundSpec.TrackWidthMetres < (LockedBattleRoster.OuterLaneHalfSpacingMetres - LockedBattleRoster.LaneHalfSpacingMetres));
        Assert.True(LockedBattleGroundSpec.FogStartMetres < LockedBattleGroundSpec.FogEndMetres);

        // Horizon is a light, green-leaning haze, not the old near-black clear colour.
        Assert.True(LockedBattleGroundSpec.HorizonG >= LockedBattleGroundSpec.HorizonR);
        Assert.True(LockedBattleGroundSpec.HorizonG > LockedBattleGroundSpec.HorizonB);
        Assert.True((LockedBattleGroundSpec.HorizonR + LockedBattleGroundSpec.HorizonG + LockedBattleGroundSpec.HorizonB) / 3f > 0.4f);

        Assert.InRange(LockedBattleGroundSpec.ContactShadowAlpha, 0.3f, 0.7f);
        Assert.True(LockedBattleGroundSpec.ContactShadowMinMetres < LockedBattleGroundSpec.ContactShadowMaxMetres);
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
