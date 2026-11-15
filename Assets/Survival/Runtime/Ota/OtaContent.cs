#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Survival.Domain.Serialization;

namespace Survival.Domain.Ota
{
    /// <summary>The four remote Addressables groups over-the-air content ships in.</summary>
    public enum OtaContentGroup
    {
        /// <summary>Hero / villain models, meshes, avatars, materials (Assets/ArtSource, roster rigs).</summary>
        Characters,

        /// <summary>Animation clips and controllers.</summary>
        Animations,

        /// <summary>Textures, sprites, VFX, audio and any other art not in the theme pack.</summary>
        Art,

        /// <summary>Everything under Assets/ThemePack/fantasy_kingdom_a (pack.json, strings, map, UI, store art) plus the catalog JSON.</summary>
        ThemePack
    }

    public static class OtaContentGroups
    {
        /// <summary>Addressables label every OTA registry carries (OtaBoot loads them all by it).</summary>
        public const string Label = "ota";

        public static readonly IReadOnlyList<OtaContentGroup> All = new[]
        {
            OtaContentGroup.Characters, OtaContentGroup.Animations, OtaContentGroup.Art, OtaContentGroup.ThemePack
        };

        /// <summary>Addressables group name ("OTA-Characters").</summary>
        public static string GroupName(OtaContentGroup g) => "OTA-" + g;

        /// <summary>Address of the group's registry asset ("ota/characters").</summary>
        public static string Address(OtaContentGroup g) => "ota/" + g.ToString().ToLowerInvariant();

        /// <summary>Which group a baked "Assets/..." path ships in. Deterministic so a content build is reproducible.</summary>
        public static OtaContentGroup Classify(string assetPath, bool rawFile)
        {
            var p = (assetPath ?? "").Replace('\\', '/');
            var lower = p.ToLowerInvariant();
            if (lower.StartsWith("assets/themepack/", StringComparison.Ordinal) || lower.StartsWith("assets/appflavor/", StringComparison.Ordinal) ||
                lower.StartsWith("assets/survival/resources/survival/", StringComparison.Ordinal))
            {
                return OtaContentGroup.ThemePack;
            }

            if (lower.EndsWith(".anim", StringComparison.Ordinal) || lower.EndsWith(".controller", StringComparison.Ordinal) ||
                lower.EndsWith(".overridecontroller", StringComparison.Ordinal) || lower.Contains("/anims/") || lower.Contains("/animations/") ||
                lower.Contains("/clips/"))
            {
                return OtaContentGroup.Animations;
            }

            if (!rawFile && (lower.EndsWith(".fbx", StringComparison.Ordinal) || lower.EndsWith(".glb", StringComparison.Ordinal) ||
                             lower.EndsWith(".gltf", StringComparison.Ordinal) || lower.EndsWith(".prefab", StringComparison.Ordinal) ||
                             lower.EndsWith(".mat", StringComparison.Ordinal) || lower.EndsWith(".mesh", StringComparison.Ordinal) ||
                             lower.Contains("/characters/") || lower.Contains("/rigs/")))
            {
                return OtaContentGroup.Characters;
            }

            return OtaContentGroup.Art;
        }
    }

    /// <summary>
    /// Build-baked OTA settings (Resources/Ota/ota_settings.json). An empty cdn_host turns every network step off and the
    /// game runs on baked content and the baked remote config only.
    /// </summary>
    public sealed class OtaSettings
    {
        private static readonly Regex HostPattern = new Regex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)+(:[0-9]{2,5})?$", RegexOptions.CultureInvariant);

        public string CdnHost { get; init; } = "";

        /// <summary>Path of the remote config on the host, "config/{platform}/remote_config.json".</summary>
        public string ConfigPath { get; init; } = "config/{platform}/remote_config.json";

        public int ContentApi { get; init; } = 1;

        public double ConfigTimeoutSeconds { get; init; } = 5;

        public double CatalogTimeoutSeconds { get; init; } = 8;

        /// <summary>A download with no progress for this long is abandoned (bundled / cached content is used).</summary>
        public double DownloadStallSeconds { get; init; } = 20;

        public bool Enabled => CdnHost.Length > 0;

        public string ConfigUrl(string platform) => "https://" + CdnHost + "/" + ConfigPath.Replace("{platform}", platform);

        public static OtaSettings Parse(string json)
        {
            var o = JsonRead.Parse(json) as JsonNode.Obj ?? throw new FormatException("ota_settings.json must be an object.");
            string S(string k, string d) => o.TryGet(k, out var n) && n is JsonNode.Str s ? s.Value.Trim() : d;
            double D(string k, double d) => o.TryGet(k, out var n) && n is JsonNode.Num x && x.Value > 0 ? x.Value : d;
            var host = S("cdn_host", "").ToLowerInvariant();
            if (host.StartsWith("https://", StringComparison.Ordinal))
            {
                host = host.Substring(8);
            }

            host = host.TrimEnd('/');
            if (host.Length > 0 && !HostPattern.IsMatch(host))
            {
                throw new FormatException("ota_settings.json cdn_host '" + host + "' is not a host name (no scheme, no path).");
            }

            var path = S("config_path", "config/{platform}/remote_config.json").TrimStart('/');
            if (path.Contains("..") || path.Contains("?") || path.Contains("#"))
            {
                throw new FormatException("ota_settings.json config_path must be a plain path.");
            }

            return new OtaSettings
            {
                CdnHost = host,
                ConfigPath = path,
                ContentApi = (int)D("content_api", 1),
                ConfigTimeoutSeconds = D("config_timeout_seconds", 5),
                CatalogTimeoutSeconds = D("catalog_timeout_seconds", 8),
                DownloadStallSeconds = D("download_stall_seconds", 20)
            };
        }
    }

    /// <summary>Boot steps of the OTA check, for the loading screen's status line.</summary>
    public enum OtaBootStep
    {
        Config,
        Catalog,
        Download,
        Load,
        Done
    }

    /// <summary>
    /// Loading-bar policy for the OTA step: config and catalog checks are small fixed slices, the download fills most of
    /// the bar by bytes, loading the registries finishes it.
    /// </summary>
    public static class OtaBootProgress
    {
        public const float ConfigShare = 0.05f;
        public const float CatalogShare = 0.10f;
        public const float DownloadShare = 0.80f;

        public static float Bar(OtaBootStep step, float stepFraction)
        {
            var f = Math.Max(0f, Math.Min(1f, stepFraction));
            return step switch
            {
                OtaBootStep.Config => f * ConfigShare,
                OtaBootStep.Catalog => ConfigShare + (f * CatalogShare),
                OtaBootStep.Download => ConfigShare + CatalogShare + (f * DownloadShare),
                OtaBootStep.Load => ConfigShare + CatalogShare + DownloadShare + (f * (1f - ConfigShare - CatalogShare - DownloadShare)),
                _ => 1f
            };
        }

        /// <summary>"Downloading new content 3.2 / 12.5 MB".</summary>
        public static string Label(OtaBootStep step, long downloadedBytes, long totalBytes)
        {
            return step switch
            {
                OtaBootStep.Config => "Checking for updates",
                OtaBootStep.Catalog => "Checking for new content",
                OtaBootStep.Download => "Downloading new content " + Mb(downloadedBytes) + " / " + Mb(totalBytes) + " MB",
                OtaBootStep.Load => "Unpacking new content",
                _ => "Ready"
            };

            static string Mb(long b) => (Math.Max(0, b) / (1024.0 * 1024.0)).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Whether this launch should touch Addressables at all.</summary>
        public static bool WantsRemoteContent(OtaSettings settings, RemoteConfig config) =>
            settings.Enabled && config.ContentVersion.Length > 0 && config.ContentApi == settings.ContentApi && config.Flag("ota_content", true);
    }
}
