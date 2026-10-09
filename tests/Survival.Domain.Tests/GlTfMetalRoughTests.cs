using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class GlTfMetalRoughTests
{
    [Fact]
    public void Metallic_comes_from_blue_and_smoothness_is_one_minus_green()
    {
        var (metallic, smoothness) = GlTfMetalRough.ToUrp(1f, 0.25f, 0.8f);
        Assert.Equal(0.8f, metallic, 5);
        Assert.Equal(0.75f, smoothness, 5);
    }

    [Fact]
    public void White_red_channel_does_not_make_the_body_chrome()
    {
        // Lyra's Meshy map averages R 254, G 121, B 29 (0..255). URP read R as metallic,
        // which made the whole body a near-mirror and rendered it dark.
        var (metallic, smoothness) = GlTfMetalRough.ToUrp(254f / 255f, 121f / 255f, 29f / 255f);
        Assert.InRange(metallic, 0.10f, 0.12f);
        Assert.InRange(smoothness, 0.52f, 0.54f);
    }

    [Fact]
    public void Values_are_clamped_to_unit_range()
    {
        var (metallic, smoothness) = GlTfMetalRough.ToUrp(0f, 1.5f, -0.2f);
        Assert.Equal(0f, metallic);
        Assert.Equal(0f, smoothness);
        (metallic, smoothness) = GlTfMetalRough.ToUrp(0f, -1f, 2f);
        Assert.Equal(1f, metallic);
        Assert.Equal(1f, smoothness);
    }

    [Fact]
    public void Pack_writes_metallic_to_rgb_and_smoothness_to_alpha()
    {
        var packed = GlTfMetalRough.PackRgba(new[]
        {
            1f, 0f, 0f, 1f,
            1f, 1f, 1f, 1f,
            0.5f, 0.4f, 0.6f, 0.2f,
        });

        Assert.Equal(new[] { 0f, 0f, 0f, 1f }, packed[0..4]);
        Assert.Equal(new[] { 1f, 1f, 1f, 0f }, packed[4..8]);
        Assert.Equal(0.6f, packed[8], 5);
        Assert.Equal(0.6f, packed[9], 5);
        Assert.Equal(0.6f, packed[10], 5);
        Assert.Equal(0.6f, packed[11], 5);
        Assert.Throws<ArgumentException>(() => GlTfMetalRough.PackRgba(new float[3]));
        Assert.Throws<ArgumentNullException>(() => GlTfMetalRough.PackRgba(null!));
    }

    [Fact]
    public void Fallback_is_a_plain_dielectric_and_packed_scales_are_one()
    {
        Assert.Equal(0f, GlTfMetalRough.FallbackMetallic);
        Assert.Equal(0.3f, GlTfMetalRough.FallbackSmoothness);
        Assert.Equal(1f, GlTfMetalRough.PackedMetallicScale);
        Assert.Equal(1f, GlTfMetalRough.PackedSmoothnessScale);
    }

    [Fact]
    public void Player_packs_the_metal_rough_map_instead_of_binding_it_raw()
    {
        var root = FindRepoRoot();
        var player = File.ReadAllText(Path.Combine(root, "Assets", "Survival", "Unity", "BlenderRigPlayer.cs"));
        Assert.Contains("GlTfMetalRough.ToUrp", player, StringComparison.Ordinal);
        Assert.Contains("linear: true, readable: true", player, StringComparison.Ordinal);
        Assert.Contains("_METALLICSPECGLOSSMAP", player, StringComparison.Ordinal);
        Assert.Contains("GlTfMetalRough.FallbackSmoothness", player, StringComparison.Ordinal);
        Assert.DoesNotContain("SetTexture(\"_MetallicGlossMap\", metal)", player, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
