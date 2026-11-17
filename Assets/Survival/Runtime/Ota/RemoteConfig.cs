#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Survival.Domain.Serialization;

namespace Survival.Domain.Ota
{
    /// <summary>One event's remote schedule. Only schedules events the build already ships (events.json); rewards stay baked.</summary>
    public sealed record RemoteEventSchedule(string Id, bool Enabled, DateTime? StartsAtUtc, DateTime? EndsAtUtc)
    {
        public bool IsLive(DateTime nowUtc) =>
            Enabled && (StartsAtUtc == null || nowUtc >= StartsAtUtc) && (EndsAtUtc == null || nowUtc < EndsAtUtc);
    }

    /// <summary>
    /// One store offer's remote presentation (on/off, badge, order, window). Price and contents stay baked and
    /// server-validated (store/products.json, Play Console); remote config cannot change what a purchase grants.
    /// </summary>
    public sealed record RemoteStoreOffer(string Offer, bool Enabled, string Badge, int Sort, DateTime? StartsAtUtc, DateTime? EndsAtUtc)
    {
        public bool IsLive(DateTime nowUtc) =>
            Enabled && (StartsAtUtc == null || nowUtc >= StartsAtUtc) && (EndsAtUtc == null || nowUtc < EndsAtUtc);
    }

    /// <summary>The publish-time free-pace check result the phone verifies before it applies balance patches.</summary>
    public sealed record FreePaceCheck(string BalanceSha256, int Wall, IReadOnlyDictionary<int, double> ClearedDay);

    /// <summary>
    /// The over-the-air remote config (schema "paytoplay.remote_config" v1): content version to load, per-platform minimum
    /// app versions, store links, feature flags, event schedules, store offer presentation and balance patches. Data only:
    /// nothing in it is code. Parsed and fully validated by <see cref="RemoteConfigParser"/>; an invalid document is
    /// rejected whole and the last-good (cached) or baked copy is used instead.
    /// </summary>
    public sealed class RemoteConfig
    {
        public const string SchemaId = "paytoplay.remote_config";

        /// <summary>The newest schema this build understands. A higher schema_version is rejected (use the cached copy).</summary>
        public const int SupportedSchemaVersion = 1;

        public int SchemaVersion { get; init; }

        /// <summary>Monotonic publish counter. The client never moves to a lower one than it has cached (stale CDN edge).</summary>
        public int ConfigVersion { get; init; }

        public string AppId { get; init; } = "";

        /// <summary>Remote content folder to load ("" = use only the content baked into the build).</summary>
        public string ContentVersion { get; init; } = "";

        /// <summary>Content format level. Content is only loaded when this equals the build's baked content_api.</summary>
        public int ContentApi { get; init; } = 1;

        /// <summary>Platform ("android", "ios") to minimum supported app version.</summary>
        public IReadOnlyDictionary<string, AppVersion> MinSupportedVersion { get; init; } = new Dictionary<string, AppVersion>();

        /// <summary>Platform to store page (https only).</summary>
        public IReadOnlyDictionary<string, string> StoreLinks { get; init; } = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, bool> FeatureFlags { get; init; } = new Dictionary<string, bool>();

        public IReadOnlyList<RemoteEventSchedule> Events { get; init; } = Array.Empty<RemoteEventSchedule>();

        public IReadOnlyList<RemoteStoreOffer> StoreOffers { get; init; } = Array.Empty<RemoteStoreOffer>();

        /// <summary>Catalog file name (e.g. runner_levels.json) to RFC 7386 merge patch. Empty = baked balance.</summary>
        internal IReadOnlyDictionary<string, JsonNode> BalancePatches { get; init; } = new Dictionary<string, JsonNode>();

        /// <summary>The balance section's "patches" object, as parsed (hashed for <see cref="FreePaceGuard"/>).</summary>
        internal JsonNode BalancePatchesNode { get; init; } = new JsonNode.Obj();

        public FreePaceCheck? FreePaceCheck { get; init; }

        public bool HasBalancePatches => BalancePatches.Count > 0;

        /// <summary>Remote flag, or <paramref name="fallback"/> when the config does not set it.</summary>
        public bool Flag(string name, bool fallback = false) => FeatureFlags.TryGetValue(name, out var v) ? v : fallback;

        /// <summary>The same config without balance patches (used when <see cref="FreePaceGuard"/> rejects them).</summary>
        public RemoteConfig WithoutBalance() => new RemoteConfig
        {
            SchemaVersion = SchemaVersion,
            ConfigVersion = ConfigVersion,
            AppId = AppId,
            ContentVersion = ContentVersion,
            ContentApi = ContentApi,
            MinSupportedVersion = MinSupportedVersion,
            StoreLinks = StoreLinks,
            FeatureFlags = FeatureFlags,
            Events = Events,
            StoreOffers = StoreOffers,
            BalancePatches = new Dictionary<string, JsonNode>(),
            BalancePatchesNode = new JsonNode.Obj(),
            FreePaceCheck = null
        };
    }

    /// <summary>Outcome of parsing + validating a remote config document.</summary>
    public sealed class RemoteConfigResult
    {
        public RemoteConfig? Config { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

        public bool Ok => Config != null && Errors.Count == 0;
    }

    /// <summary>Strict schema validation of the remote config. Unknown keys (other than "_"-prefixed comments) are errors.</summary>
    public static class RemoteConfigParser
    {
        /// <summary>Documents above this size are refused before parsing.</summary>
        public const int MaxBytes = 256 * 1024;

        public static readonly IReadOnlyCollection<string> Platforms = new[] { "android", "ios" };

        private static readonly Regex ContentVersionPattern = new Regex("^[A-Za-z0-9._-]{0,64}$", RegexOptions.CultureInvariant);
        private static readonly Regex IdPattern = new Regex("^[a-z0-9_.-]{1,96}$", RegexOptions.CultureInvariant);
        private static readonly Regex Sha256Pattern = new Regex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant);

        private static readonly HashSet<string> TopKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "schema", "schema_version", "config_version", "app_id", "content_version", "content_api", "min_supported_version",
            "store_links", "feature_flags", "events", "store_offers", "balance"
        };

        public static RemoteConfigResult Parse(string? json, string expectedAppId)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return Fail("empty document");
            }

            if (json!.Length > MaxBytes)
            {
                return Fail("document larger than " + MaxBytes + " bytes");
            }

            JsonNode root;
            try
            {
                root = JsonRead.Parse(json);
            }
            catch (FormatException ex)
            {
                return Fail("not JSON: " + ex.Message);
            }

            if (!(root is JsonNode.Obj o))
            {
                return Fail("root must be an object");
            }

            foreach (var key in o.Fields.Keys)
            {
                if (!TopKeys.Contains(key) && !key.StartsWith("_", StringComparison.Ordinal))
                {
                    errors.Add("unknown key '" + key + "'");
                }
            }

            if (Str(o, "schema", errors, required: true) != RemoteConfig.SchemaId)
            {
                errors.Add("schema must be '" + RemoteConfig.SchemaId + "'");
            }

            var schemaVersion = Int(o, "schema_version", errors, required: true);
            if (schemaVersion < 1 || schemaVersion > RemoteConfig.SupportedSchemaVersion)
            {
                errors.Add("schema_version " + schemaVersion + " not supported (this build reads 1.." + RemoteConfig.SupportedSchemaVersion + ")");
            }

            var configVersion = Int(o, "config_version", errors, required: true);
            if (configVersion < 1)
            {
                errors.Add("config_version must be >= 1");
            }

            var appId = Str(o, "app_id", errors, required: true);
            if (!string.Equals(appId, expectedAppId, StringComparison.Ordinal))
            {
                errors.Add("app_id '" + appId + "' is not this app (" + expectedAppId + ")");
            }

            var contentVersion = Str(o, "content_version", errors, required: false);
            if (!ContentVersionPattern.IsMatch(contentVersion) || contentVersion.Contains(".."))
            {
                errors.Add("content_version must be 0-64 of [A-Za-z0-9._-] (it becomes a URL path segment)");
            }

            var contentApi = o.Fields.ContainsKey("content_api") ? Int(o, "content_api", errors, required: true) : 1;
            if (contentApi < 1)
            {
                errors.Add("content_api must be >= 1");
            }

            var minVersions = new Dictionary<string, AppVersion>(StringComparer.Ordinal);
            foreach (var kv in Map(o, "min_supported_version", errors))
            {
                if (!IsPlatform(kv.Key, errors, "min_supported_version"))
                {
                    continue;
                }

                if (kv.Value is JsonNode.Str s && AppVersion.TryParse(s.Value, out var v))
                {
                    minVersions[kv.Key] = v;
                }
                else
                {
                    errors.Add("min_supported_version." + kv.Key + " must be a dotted version string");
                }
            }

            var links = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in Map(o, "store_links", errors))
            {
                if (!IsPlatform(kv.Key, errors, "store_links"))
                {
                    continue;
                }

                if (kv.Value is JsonNode.Str s && Uri.TryCreate(s.Value, UriKind.Absolute, out var uri) && uri.Scheme == "https")
                {
                    links[kv.Key] = s.Value;
                }
                else
                {
                    errors.Add("store_links." + kv.Key + " must be an https URL");
                }
            }

            var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var kv in Map(o, "feature_flags", errors))
            {
                if (kv.Key.StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IdPattern.IsMatch(kv.Key))
                {
                    errors.Add("feature_flags key '" + kv.Key + "' must match " + IdPattern);
                }
                else if (kv.Value is JsonNode.Bool b)
                {
                    flags[kv.Key] = b.Value;
                }
                else
                {
                    errors.Add("feature_flags." + kv.Key + " must be true or false");
                }
            }

            var events = new List<RemoteEventSchedule>();
            foreach (var (item, path) in List(o, "events", errors))
            {
                var id = Str(item, "id", errors, required: true, path);
                if (!IdPattern.IsMatch(id))
                {
                    errors.Add(path + ".id must match " + IdPattern);
                }

                OnlyKeys(item, path, errors, "id", "enabled", "starts_at", "ends_at");
                var (start, end) = Window(item, path, errors);
                events.Add(new RemoteEventSchedule(id, Bool(item, "enabled", errors, true, path), start, end));
            }

            var offers = new List<RemoteStoreOffer>();
            foreach (var (item, path) in List(o, "store_offers", errors))
            {
                var id = Str(item, "offer", errors, required: true, path);
                if (!IdPattern.IsMatch(id))
                {
                    errors.Add(path + ".offer must match " + IdPattern);
                }

                OnlyKeys(item, path, errors, "offer", "enabled", "badge", "sort", "starts_at", "ends_at");
                var badge = Str(item, "badge", errors, required: false, path);
                if (badge.Length > 32)
                {
                    errors.Add(path + ".badge longer than 32 characters");
                }

                var sort = item.Fields.ContainsKey("sort") ? Int(item, "sort", errors, required: true, path) : 0;
                var (start, end) = Window(item, path, errors);
                offers.Add(new RemoteStoreOffer(id, Bool(item, "enabled", errors, true, path), badge, sort, start, end));
            }

            var patches = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
            JsonNode patchesNode = new JsonNode.Obj();
            FreePaceCheck? check = null;
            if (o.TryGet("balance", out var balanceNode))
            {
                if (balanceNode is JsonNode.Obj balance)
                {
                    OnlyKeys(balance, "balance", errors, "patches", "free_pace_check");
                    if (balance.TryGet("patches", out var pn))
                    {
                        if (pn is JsonNode.Obj po)
                        {
                            patchesNode = po;
                            foreach (var kv in po.Fields)
                            {
                                if (!RemoteBalance.IsPatchable(kv.Key))
                                {
                                    errors.Add("balance.patches." + kv.Key + " is not a patchable balance file (" + string.Join(", ", RemoteBalance.PatchableFiles) + ")");
                                }
                                else if (!(kv.Value is JsonNode.Obj))
                                {
                                    errors.Add("balance.patches." + kv.Key + " must be a JSON merge-patch object");
                                }
                                else
                                {
                                    patches[kv.Key] = kv.Value;
                                }
                            }
                        }
                        else
                        {
                            errors.Add("balance.patches must be an object");
                        }
                    }

                    if (balance.TryGet("free_pace_check", out var cn) && !(cn is JsonNode.Null))
                    {
                        check = ParseCheck(cn, errors);
                    }
                }
                else
                {
                    errors.Add("balance must be an object");
                }
            }

            if (errors.Count > 0)
            {
                return new RemoteConfigResult { Errors = errors };
            }

            return new RemoteConfigResult
            {
                Config = new RemoteConfig
                {
                    SchemaVersion = schemaVersion,
                    ConfigVersion = configVersion,
                    AppId = appId,
                    ContentVersion = contentVersion,
                    ContentApi = contentApi,
                    MinSupportedVersion = minVersions,
                    StoreLinks = links,
                    FeatureFlags = flags,
                    Events = events,
                    StoreOffers = offers,
                    BalancePatches = patches,
                    BalancePatchesNode = patchesNode,
                    FreePaceCheck = check
                }
            };

            static RemoteConfigResult Fail(string e) => new RemoteConfigResult { Errors = new[] { e } };
        }

        private static FreePaceCheck? ParseCheck(JsonNode node, List<string> errors)
        {
            if (!(node is JsonNode.Obj c))
            {
                errors.Add("balance.free_pace_check must be an object");
                return null;
            }

            const string path = "balance.free_pace_check";
            OnlyKeys(c, path, errors, "balance_sha256", "wall", "cleared_day", "checked_at", "checked_by");
            var sha = Str(c, "balance_sha256", errors, required: true, path);
            if (!Sha256Pattern.IsMatch(sha))
            {
                errors.Add(path + ".balance_sha256 must be 64 lower-case hex characters");
            }

            var wall = Int(c, "wall", errors, required: true, path);
            var days = new Dictionary<int, double>();
            if (c.TryGet("cleared_day", out var dn) && dn is JsonNode.Obj d)
            {
                foreach (var kv in d.Fields)
                {
                    if (int.TryParse(kv.Key, NumberStyles.None, CultureInfo.InvariantCulture, out var level) && kv.Value is JsonNode.Num n && n.Value >= 0)
                    {
                        days[level] = n.Value;
                    }
                    else
                    {
                        errors.Add(path + ".cleared_day." + kv.Key + " must be level -> day (number)");
                    }
                }
            }
            else
            {
                errors.Add(path + ".cleared_day must be an object");
            }

            return new FreePaceCheck(sha, wall, days);
        }

        private static bool IsPlatform(string key, List<string> errors, string section)
        {
            if (key.StartsWith("_", StringComparison.Ordinal))
            {
                return false;
            }

            foreach (var p in Platforms)
            {
                if (p == key)
                {
                    return true;
                }
            }

            errors.Add(section + "." + key + ": platform must be one of " + string.Join(", ", Platforms));
            return false;
        }

        private static (DateTime? Start, DateTime? End) Window(JsonNode.Obj item, string path, List<string> errors)
        {
            var start = Date(item, "starts_at", path, errors);
            var end = Date(item, "ends_at", path, errors);
            if (start != null && end != null && end <= start)
            {
                errors.Add(path + ": ends_at must be after starts_at");
            }

            return (start, end);
        }

        private static DateTime? Date(JsonNode.Obj item, string key, string path, List<string> errors)
        {
            if (!item.TryGet(key, out var n) || n is JsonNode.Null)
            {
                return null;
            }

            if (n is JsonNode.Str s && s.Value.EndsWith("Z", StringComparison.Ordinal) &&
                DateTime.TryParse(s.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d))
            {
                return DateTime.SpecifyKind(d, DateTimeKind.Utc);
            }

            errors.Add(path + "." + key + " must be an ISO-8601 UTC time ending in Z");
            return null;
        }

        private static void OnlyKeys(JsonNode.Obj o, string path, List<string> errors, params string[] allowed)
        {
            foreach (var key in o.Fields.Keys)
            {
                if (Array.IndexOf(allowed, key) < 0 && !key.StartsWith("_", StringComparison.Ordinal))
                {
                    errors.Add(path + ": unknown key '" + key + "'");
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, JsonNode>> Map(JsonNode.Obj o, string key, List<string> errors)
        {
            if (!o.TryGet(key, out var n) || n is JsonNode.Null)
            {
                return Array.Empty<KeyValuePair<string, JsonNode>>();
            }

            if (n is JsonNode.Obj m)
            {
                return m.Fields;
            }

            errors.Add(key + " must be an object");
            return Array.Empty<KeyValuePair<string, JsonNode>>();
        }

        private static IEnumerable<(JsonNode.Obj Item, string Path)> List(JsonNode.Obj o, string key, List<string> errors)
        {
            var result = new List<(JsonNode.Obj, string)>();
            if (!o.TryGet(key, out var n) || n is JsonNode.Null)
            {
                return result;
            }

            if (!(n is JsonNode.Arr a))
            {
                errors.Add(key + " must be an array");
                return result;
            }

            for (var i = 0; i < a.Items.Count; i++)
            {
                if (a.Items[i] is JsonNode.Obj item)
                {
                    result.Add((item, key + "[" + i + "]"));
                }
                else
                {
                    errors.Add(key + "[" + i + "] must be an object");
                }
            }

            return result;
        }

        private static string Str(JsonNode.Obj o, string key, List<string> errors, bool required, string path = "")
        {
            var name = path.Length > 0 ? path + "." + key : key;
            if (!o.TryGet(key, out var n) || n is JsonNode.Null)
            {
                if (required)
                {
                    errors.Add(name + " is required");
                }

                return "";
            }

            if (n is JsonNode.Str s)
            {
                return s.Value;
            }

            errors.Add(name + " must be a string");
            return "";
        }

        private static int Int(JsonNode.Obj o, string key, List<string> errors, bool required, string path = "")
        {
            var name = path.Length > 0 ? path + "." + key : key;
            if (!o.TryGet(key, out var n))
            {
                if (required)
                {
                    errors.Add(name + " is required");
                }

                return 0;
            }

            if (n is JsonNode.Num num && Math.Abs(num.Value - Math.Round(num.Value)) < 1e-9 && Math.Abs(num.Value) < int.MaxValue)
            {
                return (int)Math.Round(num.Value);
            }

            errors.Add(name + " must be an integer");
            return 0;
        }

        private static bool Bool(JsonNode.Obj o, string key, List<string> errors, bool fallback, string path)
        {
            if (!o.TryGet(key, out var n))
            {
                return fallback;
            }

            if (n is JsonNode.Bool b)
            {
                return b.Value;
            }

            errors.Add(path + "." + key + " must be true or false");
            return fallback;
        }
    }
}
