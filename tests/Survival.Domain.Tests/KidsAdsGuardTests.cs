using System.Text.RegularExpressions;
using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

public sealed class KidsAdsGuardTests
{
    private const string Listed = "{\"ads\":{\"enabled\":true,\"verified_on\":\"2026-11-09\",\"families_self_certified\":[{\"sdk\":\"com.unity.ads\",\"version\":\"4.12.0\"}]}}";

    [Fact]
    public void Shipped_config_allows_no_ads_sdk_at_all()
    {
        var c = KidsFiles.Shipped();
        Assert.False(AdsSdkGuard.CanEnable(c, "com.unity.ads", "4.12.0", out var reason));
        Assert.Equal("ads.disabled", reason);
    }

    [Fact]
    public void Only_the_exact_listed_version_passes()
    {
        var c = ChildSafetyConfig.Parse(Listed);
        Assert.True(AdsSdkGuard.CanEnable(c, "com.unity.ads", "4.12.0", out _));
        Assert.False(AdsSdkGuard.CanEnable(c, "com.unity.ads", "4.13.0", out var r1));
        Assert.Equal("ads.not_families_self_certified", r1);
        Assert.False(AdsSdkGuard.CanEnable(c, "com.applovin.mediation", "1.0.0", out _));
    }

    [Fact]
    public void An_allowlist_nobody_dated_is_not_trusted()
    {
        var c = ChildSafetyConfig.Parse(Listed.Replace("\"2026-11-09\"", "null"));
        Assert.False(AdsSdkGuard.CanEnable(c, "com.unity.ads", "4.12.0", out var reason));
        Assert.Equal("ads.allowlist_unverified", reason);
    }

    [Fact]
    public void The_Unity_package_manifest_has_no_unapproved_ads_sdk()
    {
        var manifest = File.ReadAllText(Path.Combine(KidsFiles.RepoRoot(), "Packages", "manifest.json"));
        var packages = Regex.Matches(manifest, "\"([a-z0-9_.\\-:]+)\"\\s*:\\s*\"([^\"]+)\"")
            .Select(m => new KeyValuePair<string, string>(m.Groups[1].Value, m.Groups[2].Value));
        Assert.Empty(AdsSdkGuard.Unapproved(KidsFiles.Shipped(), packages));
    }

    [Fact]
    public void Unapproved_flags_ads_packages_and_ignores_others()
    {
        var bad = AdsSdkGuard.Unapproved(KidsFiles.Shipped(), new[]
        {
            new KeyValuePair<string, string>("com.unity.ugui", "2.0.0"),
            new KeyValuePair<string, string>("com.unity.ads", "4.12.0"),
        });
        Assert.Equal(new[] { "com.unity.ads@4.12.0" }, bad);
    }
}
