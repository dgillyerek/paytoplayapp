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
        /// <summary>Hero / villain models, meshes, avatars, materials (ThemePack 3d folders and ArtSource rigs).</summary>
        Characters,

        /// <summary>Animation clips, controllers and override controllers (including paths under anim folders).</summary>
        Animations,

        /// <summary>Textures, sprites, VFX, audio and other non-config art, including images under ThemePack.</summary>
        Art,

        /// <summary>Config and data only: raw JSON/text and catalogs under ThemePack, AppFlavor and Resources/Survival.</summary>
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

        /// <summary>
        /// Which group a baked "Assets/..." path ships in. Deterministic so a content build is reproducible.
        /// Type wins over the ThemePack / AppFlavor folder prefix, so a character or texture update does not
        /// re-download the whole pack.
        /// </summary>
        public static OtaContentGroup Classify(string assetPath, bool rawFile)
        {
            var lower = (assetPath ?? "").Replace('\\', '/').ToLowerInvariant();
            if (IsAnimation(lower))
            {
                return OtaContentGroup.Animations;
            }

            if (!rawFile && IsModel(lower))
            {
                return OtaContentGroup.Characters;
            }

            if (IsImage(lower))
            {
                return OtaContentGroup.Art;
            }

            if (IsConfigData(lower))
            {
                return OtaContentGroup.ThemePack;
            }

            return OtaContentGroup.Art;
        }

        /// <summary>
        /// Whether this group's Addressables packing should split by <see cref="PackLabel"/> so one character's
        /// bundle can update without re-downloading the rest. ThemePack is small config and stays packed together.
        /// </summary>
        public static bool PacksByLabel(OtaContentGroup g) => g != OtaContentGroup.ThemePack;

        /// <summary>
        /// Addressables label used with PackTogetherByLabel. One character's models or textures share a label
        /// (char-lyra, art-store, shared, …). Never returns the registry label <see cref="Label"/>.
        /// </summary>
        public static string PackLabel(string assetPath)
        {
            var lower = (assetPath ?? "").Replace('\\', '/').ToLowerInvariant();
            if (TryFolderAfter(lower, "/art/heroes/3d/", out var hero) && !IsSharedFolder(hero))
            {
                return "char-" + SanitizeLabel(hero);
            }

            if (TryFolderAfter(lower, "/art/enemies/3d/", out var enemy) && !IsSharedFolder(enemy))
            {
                return "char-" + SanitizeLabel(enemy);
            }

            if (TryFolderAfter(lower, "/art/preview_new_chars/", out var preview) && !IsSharedFolder(preview))
            {
                return "char-" + SanitizeLabel(preview);
            }

            if (TryFolderAfter(lower, "/characters/", out var character) && !IsSharedFolder(character))
            {
                return "char-" + SanitizeLabel(character);
            }

            if (TryFolderAfter(lower, "/rigs/", out var rig) && !IsSharedFolder(rig))
            {
                return "char-" + SanitizeLabel(rig);
            }

            if (TryFolderAfter(lower, "/art/", out var area) && area.Length > 0)
            {
                return "art-" + SanitizeLabel(area);
            }

            return "shared";
        }

        private static bool IsAnimation(string lower) =>
            EndsWith(lower, ".anim") || EndsWith(lower, ".controller") || EndsWith(lower, ".overridecontroller") ||
            lower.Contains("/anims/") || lower.Contains("/animations/") || lower.Contains("/clips/") || lower.Contains("/anim/");

        private static bool IsModel(string lower) =>
            EndsWith(lower, ".fbx") || EndsWith(lower, ".glb") || EndsWith(lower, ".gltf") || EndsWith(lower, ".prefab") ||
            EndsWith(lower, ".mat") || EndsWith(lower, ".mesh") || lower.Contains("/characters/") || lower.Contains("/rigs/");

        private static bool IsImage(string lower) =>
            EndsWith(lower, ".png") || EndsWith(lower, ".jpg") || EndsWith(lower, ".jpeg") || EndsWith(lower, ".tga") ||
            EndsWith(lower, ".tif") || EndsWith(lower, ".tiff") || EndsWith(lower, ".psd") || EndsWith(lower, ".psb") ||
            EndsWith(lower, ".gif") || EndsWith(lower, ".exr") || EndsWith(lower, ".hdr") || EndsWith(lower, ".bmp") ||
            EndsWith(lower, ".webp") || EndsWith(lower, ".spriteatlas");

        private static bool IsConfigData(string lower) =>
            EndsWith(lower, ".json") || EndsWith(lower, ".txt") || EndsWith(lower, ".bytes") || EndsWith(lower, ".csv") ||
            EndsWith(lower, ".xml") || EndsWith(lower, ".yaml") || EndsWith(lower, ".yml");

        private static bool EndsWith(string lower, string ext) => lower.EndsWith(ext, StringComparison.Ordinal);

        private static bool TryFolderAfter(string lowerPath, string marker, out string folder)
        {
            folder = "";
            var startMarker = lowerPath.IndexOf(marker, StringComparison.Ordinal);
            if (startMarker < 0)
            {
                return false;
            }

            var start = startMarker + marker.Length;
            if (start >= lowerPath.Length)
            {
                return false;
            }

            var end = lowerPath.IndexOf('/', start);
            if (end < 0)
            {
                end = lowerPath.Length;
            }

            if (end <= start)
            {
                return false;
            }

            folder = lowerPath.Substring(start, end - start);
            return folder.Length > 0 && folder.IndexOf('.') < 0;
        }

        private static bool IsSharedFolder(string name) =>
            name == "materials" || name == "textures" || name == "shared" || name == "common" ||
            name == "anims" || name == "animations" || name == "clips" || name == "mesh" || name == "meshes";

        private static string SanitizeLabel(string name)
        {
            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-' || c == '_')
                {
                    continue;
                }

                chars[i] = '-';
            }

            return new string(chars);
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
