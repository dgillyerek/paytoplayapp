using System.Text.RegularExpressions;
using Survival.Domain.Ota;
using Survival.Domain.Serialization;

namespace Survival.Domain.Tests;

/// <summary>Over-the-air content updates (Docs/OTA.md): remote config schema, min-version gate, config choice, balance guard, content groups.</summary>
public sealed class OtaTests
{
    private const string AppId = "com.brightmoatstudios.kingdomdefense";

    internal static string OtaDir => Path.Combine(AppContext.BaseDirectory, "Ota");

    internal static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Survival")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new DirectoryNotFoundException("repo root");
        }
    }

    internal static string Baked => File.ReadAllText(Path.Combine(OtaDir, "remote_config.default.json"));

    private static string With(string replaceFrom, string replaceTo)
    {
        var s = Baked;
        Assert.Contains(replaceFrom, s);
        return s.Replace(replaceFrom, replaceTo);
    }

    [Fact]
    public void The_baked_default_config_is_valid_and_ships_no_remote_content_or_balance()
    {
        var r = RemoteConfigParser.Parse(Baked, AppId);
        Assert.True(r.Ok, string.Join("\n", r.Errors));
        var c = r.Config!;
        Assert.Equal("", c.ContentVersion);
        Assert.False(c.HasBalancePatches);
        Assert.Equal(FreePaceVerdict.NoPatches, FreePaceGuard.Check(c, out _));
        Assert.Equal("0.1.0", c.MinSupportedVersion["android"].Text);
        Assert.StartsWith("https://play.google.com/", c.StoreLinks["android"]);
        Assert.StartsWith("https://apps.apple.com/", c.StoreLinks["ios"]);
    }

    [Fact]
    public void The_publishable_config_in_the_repo_is_valid()
    {
        var r = RemoteConfigParser.Parse(File.ReadAllText(Path.Combine(RepoRoot, "ota", "remote_config.json")), AppId);
        Assert.True(r.Ok, string.Join("\n", r.Errors));
        Assert.NotEqual(FreePaceVerdict.Rejected, FreePaceGuard.Check(r.Config!, out var why));
    }

    [Fact]
    public void The_baked_settings_point_at_the_spaces_cdn_and_keep_bundled_content_until_a_version_is_published()
    {
        var s = OtaSettings.Parse(File.ReadAllText(Path.Combine(OtaDir, "ota_settings.json")));
        Assert.True(s.Enabled);
        Assert.Equal("ota.brightmoatstudios.com", s.CdnHost);
        Assert.Equal("https://ota.brightmoatstudios.com/config/android/remote_config.json", s.ConfigUrl("android"));
        Assert.False(OtaBootProgress.WantsRemoteContent(s, RemoteConfigParser.Parse(Baked, AppId).Config!));
        var on = OtaSettings.Parse("{\"cdn_host\": \"https://ota.example.com/\"}");
        Assert.Equal("ota.example.com", on.CdnHost);
        Assert.Equal("https://ota.example.com/config/android/remote_config.json", on.ConfigUrl("android"));
        Assert.Throws<FormatException>(() => OtaSettings.Parse("{\"cdn_host\": \"ota.example.com/evil?x=1\"}"));
    }

    [Theory]
    [InlineData("\"config_version\": 1,", "\"config_version\": 1, \"surprise\": true,", "unknown key")]
    [InlineData("\"schema_version\": 1,", "\"schema_version\": 2,", "schema_version")]
    [InlineData("\"app_id\": \"com.brightmoatstudios.kingdomdefense\"", "\"app_id\": \"com.other.app\"", "app_id")]
    [InlineData("\"content_version\": \"\"", "\"content_version\": \"../../etc\"", "content_version")]
    [InlineData("\"content_version\": \"\"", "\"content_version\": \"a/b\"", "content_version")]
    [InlineData("https://play.google.com/store/apps/details?id=com.brightmoatstudios.kingdomdefense", "http://play.google.com/x", "https")]
    [InlineData("\"android\": \"0.1.0\"", "\"android\": \"latest\"", "min_supported_version")]
    [InlineData("\"patches\": {}", "\"patches\": { \"runner_balance.json\": { \"pace\": { \"sessions_per_day\": 9 } } }", "not a patchable")]
    [InlineData("\"patches\": {}", "\"patches\": { \"heroes.json\": {} }", "not a patchable")]
    [InlineData("\"events\": []", "\"events\": [ { \"id\": \"event.x\", \"starts_at\": \"2026-12-01\" } ]", "UTC")]
    [InlineData("\"store_offers\": []", "\"store_offers\": [ { \"offer\": \"offer.power_t3\", \"price\": 0 } ]", "unknown key")]
    [InlineData("\"ota_content\": true", "\"ota_content\": \"yes\"", "true or false")]
    public void An_invalid_document_is_rejected_whole(string from, string to, string error)
    {
        var r = RemoteConfigParser.Parse(With(from, to), AppId);
        Assert.False(r.Ok);
        Assert.Null(r.Config);
        Assert.Contains(r.Errors, e => e.Contains(error, StringComparison.Ordinal));
    }

    [Fact]
    public void Not_json_oversize_and_empty_documents_are_rejected()
    {
        Assert.False(RemoteConfigParser.Parse("{nope", AppId).Ok);
        Assert.False(RemoteConfigParser.Parse("", AppId).Ok);
        Assert.False(RemoteConfigParser.Parse(new string(' ', RemoteConfigParser.MaxBytes + 1), AppId).Ok);
    }

    [Fact]
    public void Events_offers_and_flags_parse_with_windows()
    {
        var json = With("\"events\": []", "\"events\": [ { \"id\": \"event.harvest\", \"enabled\": true, \"starts_at\": \"2026-12-01T00:00:00Z\", \"ends_at\": \"2026-12-08T00:00:00Z\" } ]")
            .Replace("\"store_offers\": []", "\"store_offers\": [ { \"offer\": \"offer.power_t3\", \"badge\": \"New\", \"sort\": 2 }, { \"offer\": \"offer.power_starter\", \"enabled\": false } ]")
            .Replace("\"ota_content\": true", "\"ota_content\": true, \"new_hud\": false");
        var r = RemoteConfigParser.Parse(json, AppId);
        Assert.True(r.Ok, string.Join("\n", r.Errors));
        var c = r.Config!;
        var e = Assert.Single(c.Events);
        Assert.True(e.IsLive(new DateTime(2026, 12, 3, 0, 0, 0, DateTimeKind.Utc)));
        Assert.False(e.IsLive(new DateTime(2026, 12, 8, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(2, c.StoreOffers.Count);
        Assert.False(c.StoreOffers[1].IsLive(DateTime.UtcNow));
        Assert.False(c.Flag("new_hud", true));
        Assert.True(c.Flag("missing", true));
    }

    [Theory]
    [InlineData("0.1.0", "0.1.0", 0)]
    [InlineData("0.1.9", "0.1.10", -1)]
    [InlineData("1.0", "0.9.99", 1)]
    [InlineData("0.1", "0.1.0", 0)]
    [InlineData("0.1.42-beta", "0.1.42", 0)]
    public void App_versions_compare_numerically(string a, string b, int sign)
    {
        Assert.Equal(sign, Math.Sign(AppVersion.Parse(a).CompareTo(AppVersion.Parse(b))));
    }

    [Fact]
    public void Below_min_supported_version_requires_a_store_update_per_platform()
    {
        var c = RemoteConfigParser.Parse(With("\"android\": \"0.1.0\"", "\"android\": \"0.1.50\""), AppId).Config!;
        Assert.Equal(UpdateRequirement.Required, MinVersionGate.Decide(c, "android", "0.1.49"));
        Assert.Equal(UpdateRequirement.None, MinVersionGate.Decide(c, "android", "0.1.50"));
        Assert.Equal(UpdateRequirement.None, MinVersionGate.Decide(c, "android", "0.2.1"));
        Assert.Equal(UpdateRequirement.None, MinVersionGate.Decide(c, "ios", "0.1.0"));
        Assert.Equal(UpdateRequirement.None, MinVersionGate.Decide(c, "", "0.0.1"));
        Assert.Equal(UpdateRequirement.None, MinVersionGate.Decide(c, "android", "garbage"));
        Assert.Equal("https://apps.apple.com/app/id0000000000", MinVersionGate.StoreLink(c, "ios", ""));
    }

    [Fact]
    public void Fresh_beats_cached_beats_baked_and_a_stale_edge_cannot_roll_back()
    {
        var v5 = With("\"config_version\": 1", "\"config_version\": 5");
        var v3 = With("\"config_version\": 1", "\"config_version\": 3");

        var fresh = RemoteConfigSelector.Pick(v5, v3, Baked, AppId);
        Assert.Equal(RemoteConfigSource.Fresh, fresh.Source);
        Assert.True(fresh.SaveFresh);

        var stale = RemoteConfigSelector.Pick(v3, v5, Baked, AppId);
        Assert.Equal(RemoteConfigSource.Cached, stale.Source);
        Assert.Equal(5, stale.Config.ConfigVersion);

        var broken = RemoteConfigSelector.Pick("{\"schema\": 1}", v3, Baked, AppId);
        Assert.Equal(RemoteConfigSource.Cached, broken.Source);
        Assert.NotEmpty(broken.Notes);

        var offline = RemoteConfigSelector.Pick(null, null, Baked, AppId);
        Assert.Equal(RemoteConfigSource.Baked, offline.Source);
        Assert.False(offline.SaveFresh);
    }

    [Fact]
    public void Merge_patch_follows_rfc_7386()
    {
        var target = JsonRead.Parse("{\"a\":\"b\",\"c\":{\"d\":\"e\",\"f\":\"g\"},\"list\":[1,2]}");
        var patch = JsonRead.Parse("{\"a\":\"z\",\"c\":{\"f\":null},\"list\":[3],\"n\":1.5}");
        Assert.Equal("{\"a\":\"z\",\"c\":{\"d\":\"e\"},\"list\":[3],\"n\":1.5}", JsonPatch.Canonical(JsonPatch.MergePatch(target, patch)));
    }

    [Fact]
    public void The_balance_hash_ignores_key_order_and_whitespace_but_not_values()
    {
        var a = JsonRead.Parse("{\"x\": {\"b\": 2, \"a\": 0.1}}");
        var b = JsonRead.Parse("{\n  \"x\":{\"a\":0.10,\n\"b\":2.0}}");
        var c = JsonRead.Parse("{\"x\": {\"b\": 2, \"a\": 0.11}}");
        Assert.Equal(JsonPatch.Sha256Hex(a), JsonPatch.Sha256Hex(b));
        Assert.NotEqual(JsonPatch.Sha256Hex(a), JsonPatch.Sha256Hex(c));
    }

    internal static string ConfigWithPatch(string patchesJson, string? checkJson) =>
        With("\"patches\": {},\n    \"free_pace_check\": null", "\"patches\": " + patchesJson + ",\n    \"free_pace_check\": " + (checkJson ?? "null"));

    internal static string CheckJson(string sha, int wall, IReadOnlyDictionary<int, double> days) =>
        "{ \"balance_sha256\": \"" + sha + "\", \"wall\": " + wall + ", \"cleared_day\": { " +
        string.Join(", ", days.Select(d => "\"" + d.Key + "\": " + d.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + " } }";

    private const string HarmlessPatch = "{ \"gifts.json\": { \"_doc\": \"remote note\" } }";

    private static string ShaOf(string patchesJson) =>
        FreePaceGuard.HashPatches(RemoteConfigParser.Parse(ConfigWithPatch(patchesJson, null), AppId).Config!);

    [Fact]
    public void Balance_patches_without_a_publish_time_pace_check_are_rejected()
    {
        var c = RemoteConfigParser.Parse(ConfigWithPatch(HarmlessPatch, null), AppId).Config!;
        Assert.Equal(FreePaceVerdict.Rejected, FreePaceGuard.Check(c, out var why));
        Assert.Contains("free_pace_check", why);
        Assert.Equal(0, RemoteBalance.For(c, out _, out _).PatchCount);
        Assert.False(c.WithoutBalance().HasBalancePatches);
    }

    [Fact]
    public void Balance_patches_with_a_matching_in_tolerance_check_are_accepted()
    {
        var sha = ShaOf(HarmlessPatch);
        var c = RemoteConfigParser.Parse(ConfigWithPatch(HarmlessPatch, CheckJson(sha, FreePaceLock.Wall, FreePaceLock.ClearedDay)), AppId).Config!;
        Assert.Equal(FreePaceVerdict.Accepted, FreePaceGuard.Check(c, out var why));
        Assert.Equal("", why);
        Assert.Equal(1, RemoteBalance.For(c, out _, out _).PatchCount);
    }

    [Fact]
    public void A_check_made_for_other_patches_is_rejected()
    {
        var sha = ShaOf(HarmlessPatch);
        var edited = "{ \"gifts.json\": { \"daily_cap\": 60 } }";
        var c = RemoteConfigParser.Parse(ConfigWithPatch(edited, CheckJson(sha, FreePaceLock.Wall, FreePaceLock.ClearedDay)), AppId).Config!;
        Assert.Equal(FreePaceVerdict.Rejected, FreePaceGuard.Check(c, out var why));
        Assert.Contains("different balance patches", why);
    }

    [Theory]
    [InlineData(32, 31, 13.0)]
    [InlineData(31, 31, 11.0)]
    [InlineData(31, 50, 700.0)]
    [InlineData(31, 10, 3.5)]
    public void A_check_that_moved_the_free_pace_is_rejected(int wall, int level, double day)
    {
        var days = FreePaceLock.ClearedDay.ToDictionary(d => d.Key, d => d.Value);
        days[level] = day;
        var c = RemoteConfigParser.Parse(ConfigWithPatch(HarmlessPatch, CheckJson(ShaOf(HarmlessPatch), wall, days)), AppId).Config!;
        Assert.Equal(FreePaceVerdict.Rejected, FreePaceGuard.Check(c, out var why));
        Assert.Contains("locked free pace", why);
    }

    [Fact]
    public void The_lock_matches_the_free_pace_test_targets()
    {
        Assert.Equal(FreePaceTarget.Wall, FreePaceLock.Wall);
        Assert.Equal(FreePaceTarget.Tolerance, FreePaceLock.Tolerance);
        Assert.Equal(FreePaceTarget.ClearedDay.OrderBy(k => k.Key), FreePaceLock.ClearedDay.OrderBy(k => k.Key));
    }

    [Fact]
    public void Accepted_patches_rewrite_only_their_catalog_file()
    {
        var patch = "{ \"gifts.json\": { \"daily_cap\": 5 } }";
        var c = RemoteConfigParser.Parse(ConfigWithPatch(patch, CheckJson(ShaOf(patch), FreePaceLock.Wall, FreePaceLock.ClearedDay)), AppId).Config!;
        var overlay = RemoteBalance.For(c, out _, out _);
        var gifts = File.ReadAllText(Path.Combine(TestPaths.CatalogDir, "gifts.json"));
        var patched = overlay.Apply("Assets/Survival/Resources/Survival/gifts.json", gifts);
        var node = (JsonNode.Obj)JsonRead.Parse(patched);
        Assert.Equal(5, ((JsonNode.Num)node.Fields["daily_cap"]).AsInt());
        Assert.True(node.Fields.ContainsKey("pool"));
        Assert.Equal(gifts, overlay.Apply("Assets/ThemePack/fantasy_kingdom_a/store/gifts.json", gifts));
        Assert.Equal("{}", overlay.Apply("Assets/Survival/Resources/Survival/quests.json", "{}"));
    }

    [Theory]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/pack.json", true, OtaContentGroup.ThemePack)]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/store/flavor_icon.png", false, OtaContentGroup.Art)]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/store/flavor_icon.png", true, OtaContentGroup.Art)]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/lyra/LYRA_blenderig.fbx", false, OtaContentGroup.Characters)]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/lyra/lyra_attack.anim", false, OtaContentGroup.Animations)]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/lyra/lyra_basecolor.png", false, OtaContentGroup.Art)]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/strings/en.json", true, OtaContentGroup.ThemePack)]
    [InlineData("Assets/Survival/Resources/Survival/runner_levels.json", true, OtaContentGroup.ThemePack)]
    [InlineData("Assets/ArtSource/Characters/Lyra/lyra_attack.anim", false, OtaContentGroup.Animations)]
    [InlineData("Assets/Survival/Unity/Anims/walk.controller", false, OtaContentGroup.Animations)]
    [InlineData("Assets/ArtSource/Rowan/rowan.fbx", false, OtaContentGroup.Characters)]
    [InlineData("Assets/ArtSource/fx/spark.png", false, OtaContentGroup.Art)]
    [InlineData("Assets/ArtSource/Rowan/rowan.fbx", true, OtaContentGroup.Art)]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/audio/ui_click.ogg", false, OtaContentGroup.Art)]
    public void Baked_paths_land_in_a_fixed_content_group(string path, bool raw, OtaContentGroup group)
    {
        Assert.Equal(group, OtaContentGroups.Classify(path, raw));
    }

    [Theory]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/lyra/LYRA_blenderig.fbx", "char-lyra")]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ashwyrm/ashwyrm_rest.fbx", "char-ashwyrm")]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/kaelen/kaelen.png", "char-kaelen")]
    [InlineData("Assets/ArtSource/Characters/Lyra/lyra_attack.anim", "char-lyra")]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/lite_sword_shield/sword_and_shield_idle.fbx", "char-siraldric")]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture.png", "char-siraldric")]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/store/flavor_icon.png", "art-store")]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/Materials/shared.mat", "art-heroes")]
    [InlineData("Assets/ArtSource/Rowan/rowan.fbx", "shared")]
    [InlineData("Assets/ThemePack/fantasy_kingdom_a/pack.json", "shared")]
    public void Pack_labels_group_one_character_or_art_area(string path, string label)
    {
        Assert.Equal(label, OtaContentGroups.PackLabel(path));
        Assert.NotEqual(OtaContentGroups.Label, label);
        Assert.True(OtaContentGroups.PacksByLabel(OtaContentGroup.Characters));
        Assert.True(OtaContentGroups.PacksByLabel(OtaContentGroup.Art));
        Assert.False(OtaContentGroups.PacksByLabel(OtaContentGroup.ThemePack));
    }

    [Fact]
    public void Theme_pack_models_and_images_are_not_dumped_into_the_theme_pack_group()
    {
        var root = Path.Combine(RepoRoot, "Assets", "ThemePack");
        Assert.True(Directory.Exists(root), root);
        var misplaced = new List<string>();
        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rel = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            if (!rel.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                rel = "Assets/" + rel.Replace('\\', '/');
            }

            var lower = rel.ToLowerInvariant();
            var raw = lower.EndsWith(".json") || lower.EndsWith(".txt") || lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg");
            var group = OtaContentGroups.Classify(rel, raw);
            var wrong = (lower.EndsWith(".fbx") || lower.EndsWith(".prefab") || lower.EndsWith(".mat") || lower.EndsWith(".mesh")) && group != OtaContentGroup.Characters
                        || (lower.EndsWith(".anim") || lower.EndsWith(".controller") || lower.EndsWith(".overridecontroller")) && group != OtaContentGroup.Animations
                        || (lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg")) && group != OtaContentGroup.Art
                        || (lower.EndsWith(".json") || lower.EndsWith(".txt")) && group != OtaContentGroup.ThemePack;
            if (wrong)
            {
                misplaced.Add(rel + " -> " + group);
            }
        }

        Assert.True(misplaced.Count == 0, "ThemePack files must follow type grouping:\n" + string.Join("\n", misplaced.Take(20)));
    }

    [Fact]
    public void The_boot_bar_only_moves_forward_and_ends_full()
    {
        var last = -1f;
        foreach (var step in new[] { OtaBootStep.Config, OtaBootStep.Catalog, OtaBootStep.Download, OtaBootStep.Load })
        {
            foreach (var f in new[] { 0f, 0.5f, 1f })
            {
                var bar = OtaBootProgress.Bar(step, f);
                Assert.True(bar >= last, step + " " + f);
                last = bar;
            }
        }

        Assert.Equal(1f, OtaBootProgress.Bar(OtaBootStep.Load, 1f), 3);
        Assert.Equal("Downloading new content 1.0 / 2.0 MB", OtaBootProgress.Label(OtaBootStep.Download, 1024 * 1024, 2 * 1024 * 1024));
    }

    private static IEnumerable<string> OtaSources() =>
        new[] { "Assets/Survival/Runtime/Ota", "Assets/Survival/Unity/Ota", "Assets/Survival/Editor/Ota" }
            .SelectMany(d => Directory.GetFiles(Path.Combine(RepoRoot, d), "*.cs"));

    [Fact]
    public void Ota_downloads_data_only_and_sends_no_identifiers()
    {
        var forbidden = new Regex(
            "deviceUniqueIdentifier|AdvertisingIdentifier|advertisingId|AD_ID|SetRequestHeader|Assembly\\.Load|AppDomain|HybridCLR|XLua|SLua|MoonSharp|ILRuntime|Reflection\\.Emit|System\\.Reflection|PlayerPrefs|Analytics|Firebase|deviceModel|SystemInfo\\.",
            RegexOptions.CultureInvariant);
        var hits = new List<string>();
        foreach (var file in OtaSources())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var code = lines[i].TrimStart();
                if (code.StartsWith("///") || code.StartsWith("//"))
                {
                    continue;
                }

                if (forbidden.IsMatch(lines[i]))
                {
                    hits.Add(Path.GetFileName(file) + ":" + (i + 1) + ": " + code);
                }
            }
        }

        Assert.True(hits.Count == 0, "OTA must stay content/data only with no identifiers or tracking:\n" + string.Join("\n", hits));
    }

    [Fact]
    public void Addressables_is_in_the_manifest_and_the_publish_pipeline_is_staged()
    {
        var manifest = File.ReadAllText(Path.Combine(RepoRoot, "Packages", "manifest.json"));
        Assert.Contains("\"com.unity.addressables\": \"2.10.3\"", manifest);
        var live = Path.Combine(RepoRoot, ".github", "workflows", "ota-content.yml");
        var wf = File.ReadAllText(File.Exists(live) ? live : Path.Combine(RepoRoot, "ci", "github-workflows", "ota-content.yml"));
        foreach (var secret in new[] { "OTA_S3_ACCESS_KEY_ID", "OTA_S3_SECRET_ACCESS_KEY", "OTA_S3_BUCKET", "OTA_S3_ENDPOINT" })
        {
            Assert.Contains("secrets." + secret, wf);
        }

        Assert.DoesNotContain("OTA_R2_", wf);
        Assert.Contains("OTA_S3_REGION: nyc3", wf);

        Assert.Contains("Survival.Editor.Ota.OtaContentBuild.BuildFromCommandLine", wf);
        Assert.True(File.Exists(Path.Combine(RepoRoot, "scripts", "ota", "publish_content.sh")));
        Assert.True(File.Exists(Path.Combine(RepoRoot, "Docs", "OTA.md")));
    }
}
