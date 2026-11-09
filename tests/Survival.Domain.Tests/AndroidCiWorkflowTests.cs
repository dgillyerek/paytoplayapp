using System.Text.RegularExpressions;

namespace Survival.Domain.Tests;

/// <summary>
/// The GitHub Actions Android build (.github/workflows/android-build.yml, staged at ci/github-workflows/) calls an Editor method by name and passes the
/// signing key and version code; these guard the parts a rename would silently break.
/// </summary>
public sealed class AndroidCiWorkflowTests
{
    private static readonly string Root = FindRepoRoot();
    private static readonly string Live = Path.Combine(Root, ".github", "workflows", "android-build.yml");

    // Staged copy: the bot's GitHub login cannot push .github/workflows (no "workflow" scope), so the workflow waits here
    // until it is copied into .github/workflows/ (see ci/github-workflows/README.md).
    private static readonly string Staged = Path.Combine(Root, "ci", "github-workflows", "android-build.yml");

    private static string Workflow => File.ReadAllText(File.Exists(Live) ? Live : Staged);

    [Fact]
    public void Workflow_exists_and_a_staged_copy_matches_the_live_one()
    {
        Assert.True(File.Exists(Live) || File.Exists(Staged), "no android-build.yml in .github/workflows or ci/github-workflows");
        if (File.Exists(Live) && File.Exists(Staged))
        {
            Assert.Equal(File.ReadAllText(Live).ReplaceLineEndings(), File.ReadAllText(Staged).ReplaceLineEndings());
        }
    }

    [Fact]
    public void Workflow_build_method_exists_as_public_static_editor_method()
    {
        var m = Regex.Match(Workflow, @"buildMethod:\s*(\S+)");
        Assert.True(m.Success, "workflow has no buildMethod");
        var parts = m.Groups[1].Value.Split('.');
        var method = parts[^1];
        var type = parts[^2];
        var ns = string.Join('.', parts[..^2]);
        var file = Path.Combine(Root, "Assets", "Survival", "Editor", type + ".cs");
        Assert.True(File.Exists(file), file);
        var src = File.ReadAllText(file);
        Assert.Contains("namespace " + ns, src, StringComparison.Ordinal);
        Assert.Matches(@"public static class " + type + @"\b", src);
        Assert.Matches(@"public static void " + method + @"\(\)", src);
        Assert.Contains("EditorApplication.Exit(", src, StringComparison.Ordinal);
        Assert.Contains("PlayerBake.Bake(", src, StringComparison.Ordinal);
        Assert.Contains("PlayerBake.Verify()", src, StringComparison.Ordinal);
        Assert.True(File.Exists(file + ".meta"));
    }

    [Fact]
    public void Workflow_builds_a_signed_release_aab_with_an_increasing_version_code()
    {
        var wf = Workflow;
        Assert.Contains("targetPlatform: Android", wf, StringComparison.Ordinal);
        Assert.Contains("androidExportType: ${{ env.FORMAT == 'aab' && 'androidAppBundle' || 'androidPackage' }}", wf, StringComparison.Ordinal);
        Assert.Contains("FORMAT: ${{ github.event_name == 'pull_request' && 'apk' || inputs.format || 'aab' }}", wf, StringComparison.Ordinal);
        Assert.Contains("androidTargetSdkVersion: AndroidApiLevel36", wf, StringComparison.Ordinal);
        Assert.Contains("androidVersionCode: ${{ steps.meta.outputs.version_code }}", wf, StringComparison.Ordinal);
        Assert.Contains("version_code=$((VERSION_CODE_OFFSET + RUN_NUMBER))", wf, StringComparison.Ordinal);
        Assert.Contains("RUN_NUMBER: ${{ github.run_number }}", wf, StringComparison.Ordinal);
        Assert.Contains("-requireKeystore", wf, StringComparison.Ordinal);
        Assert.Contains("-flavor ${{ env.FLAVOR }}", wf, StringComparison.Ordinal);
        Assert.Contains("FLAVOR: fantasy_kingdom_a", wf, StringComparison.Ordinal);
        Assert.Contains("path: build/Android/${{ steps.commit.outputs.name }}.aab", wf, StringComparison.Ordinal);
        foreach (var secret in new[] { "ANDROID_KEYSTORE_BASE64", "ANDROID_KEYSTORE_PASS", "ANDROID_KEYALIAS_NAME", "ANDROID_KEYALIAS_PASS", "UNITY_EMAIL", "UNITY_PASSWORD", "UNITY_LICENSE", "UNITY_SERIAL" })
        {
            Assert.Contains("secrets." + secret, wf, StringComparison.Ordinal);
        }

        Assert.Contains("android-latest", wf, StringComparison.Ordinal);
        Assert.Contains("paytoplayapp-latest.apk", wf, StringComparison.Ordinal);
    }

    [Fact]
    public void Families_app_removes_the_advertising_id_permission_and_ships_no_ad_sdk()
    {
        var src = File.ReadAllText(Path.Combine(Root, "Assets", "Survival", "Editor", "AndroidFamiliesManifest.cs"));
        Assert.Contains("IPostGenerateGradleAndroidProject", src, StringComparison.Ordinal);
        Assert.Contains("\"com.google.android.gms.permission.AD_ID\"", src, StringComparison.Ordinal);
        Assert.Contains("entry.SetAttribute(\"node\", ToolsNs, \"remove\")", src, StringComparison.Ordinal);
        Assert.Contains("\"launcher\", \"src\", \"main\", \"AndroidManifest.xml\"", src, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Root, "Assets", "Survival", "Editor", "AndroidFamiliesManifest.cs.meta")));

        var wf = Workflow;
        Assert.Contains("bundletool.jar dump manifest", wf, StringComparison.Ordinal);
        Assert.Contains("if grep -q 'com.google.android.gms.permission.AD_ID' merged-manifest.xml; then", wf, StringComparison.Ordinal);

        // No ad or ad-ID SDKs: packages, Unity Ads/Analytics service toggles, and any committed manifest asking for AD_ID.
        var packages = File.ReadAllText(Path.Combine(Root, "Packages", "manifest.json"));
        foreach (var banned in new[] { "com.unity.ads", "com.unity.services.analytics", "com.unity.analytics", "com.unity.purchasing.ads",
                                       "com.google.ads", "play-services-ads", "com.unity.services.mediation", "com.unity.services.levelplay", "ironsource", "applovin" })
        {
            Assert.DoesNotContain(banned, packages, StringComparison.OrdinalIgnoreCase);
        }

        var connect = File.ReadAllText(Path.Combine(Root, "ProjectSettings", "UnityConnectSettings.asset"));
        Assert.Matches(@"UnityAdsSettings:\n    m_Enabled: 0\n", connect);
        Assert.Matches(@"UnityAnalyticsSettings:\n    m_Enabled: 0\n", connect);
        var plugins = Path.Combine(Root, "Assets", "Plugins");
        if (Directory.Exists(plugins))
        {
            foreach (var f in Directory.GetFiles(plugins, "*.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".aar", StringComparison.OrdinalIgnoreCase) ||
                                     f.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".androidlib", StringComparison.OrdinalIgnoreCase)))
            {
                Assert.DoesNotContain("play-services-ads", Path.GetFileName(f), StringComparison.OrdinalIgnoreCase);
                if (f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    var xml = File.ReadAllText(f);
                    Assert.False(xml.Contains("permission.AD_ID", StringComparison.Ordinal) && !xml.Contains("tools:node=\"remove\"", StringComparison.Ordinal), f);
                }
            }
        }
    }

    [Fact]
    public void Ci_build_applies_the_flavor_and_builds_il2cpp_arm64_for_the_play_target_api()
    {
        var ci = File.ReadAllText(Path.Combine(Root, "Assets", "Survival", "Editor", "CiAndroidBuild.cs"));
        Assert.Contains("FlavorBuildSettings.TryApplyFlavor(flavor)", ci, StringComparison.Ordinal);
        Assert.Contains("ScriptingImplementation.IL2CPP", ci, StringComparison.Ordinal);
        Assert.Contains("AndroidArchitecture.ARM64", ci, StringComparison.Ordinal);
        Assert.Contains("EditorUserBuildSettings.buildAppBundle = appBundle", ci, StringComparison.Ordinal);
        Assert.Contains("\"androidAppBundle\"", ci, StringComparison.Ordinal);
        var flavor = File.ReadAllText(Path.Combine(Root, "Assets", "Survival", "Editor", "FlavorBuildSettings.cs"));
        Assert.Contains("PlayTargetSdk = AndroidSdkVersions.AndroidApiLevel36", flavor, StringComparison.Ordinal);
        Assert.Contains("PlayerSettings.Android.targetSdkVersion = PlayTargetSdk", flavor, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectSettings", "ProjectVersion.txt")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
