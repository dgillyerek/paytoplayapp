#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Serialization;

namespace Survival.Domain.Kids
{
    /// <summary>Analytics / crash reporting level. Off = nothing leaves the device; Limited = event names only, no ids.</summary>
    public enum TelemetryMode
    {
        Off,
        Limited,
        Full,
    }

    /// <summary>Guild chat level. Presets = only fixed phrases from the theme pack (no free text, links or personal info).</summary>
    public enum ChatMode
    {
        Off,
        Presets,
        Free,
    }

    /// <summary>Real-money purchases. ParentalGate = an adult must pass the parental gate before each purchase.</summary>
    public enum IapMode
    {
        Allowed,
        ParentalGate,
        Blocked,
    }

    /// <summary>Features that collect personal info (sign-in, cloud save). ConsentRequired = verifiable parental consent first.</summary>
    public enum ConsentMode
    {
        Allowed,
        ConsentRequired,
        Blocked,
    }

    /// <summary>One age band from the pack. MaxAge is inclusive; null = no upper bound. Child bands get the child rules.</summary>
    public sealed record AgeBandDef(string Id, int? MaxAge, bool Child);

    /// <summary>What one audience (child or 13+) may do.</summary>
    public sealed record AudienceRules(
        bool AdvertisingId,
        bool Personalization,
        TelemetryMode Analytics,
        TelemetryMode CrashReporting,
        ChatMode Chat,
        IapMode Iap,
        int? IapMaxPerDay,
        ConsentMode OnlineSignIn,
        ConsentMode CloudSave,
        bool NoPressure,
        bool CanCreateAlliance);

    /// <summary>One ads SDK version Google lists as Families Self-Certified. Checked by hand against <see cref="AdsConfig.SourceUrl"/>.</summary>
    public sealed record CertifiedAdsSdk(string Sdk, string Version);

    public sealed record AdsConfig(bool Enabled, IReadOnlyList<CertifiedAdsSdk> FamiliesSelfCertified, string SourceUrl, string? VerifiedOn);

    public sealed record ChatPreset(string Id, string Text);

    /// <summary>
    /// Theme pack child-safety settings (<c>kids/child_safety.json</c>): age bands, what child and 13+ players may do, the ads
    /// allowlist, telemetry vendor, chat presets and the account-deletion links. Every open Derek decision is a value here.
    /// </summary>
    public sealed record ChildSafetyConfig(
        int SchemaVersion,
        IReadOnlyList<AgeBandDef> AgeBands,
        int OldestBirthYearSpan,
        AudienceRules ChildRules,
        AudienceRules AdultRules,
        AdsConfig Ads,
        string AnalyticsVendor,
        string CrashVendor,
        IReadOnlyList<ChatPreset> ChatPresets,
        string WebDeletionUrl,
        string SupportEmail,
        int ParentalGateMaxFailures,
        int ParentalGateLockoutSeconds)
    {
        public const string FileName = "child_safety.json";
        public const string VendorNone = "none";

        /// <summary>
        /// The safe defaults used when the pack file is missing or unreadable: under-13 / 13+, child chat off, child IAP behind
        /// the parental gate, child sign-in and cloud save need parental consent, no ads, no analytics or crash vendor.
        /// </summary>
        public static ChildSafetyConfig Default { get; } = new(
            1,
            new[] { new AgeBandDef("under13", 12, true), new AgeBandDef("13plus", null, false) },
            100,
            new AudienceRules(false, false, TelemetryMode.Off, TelemetryMode.Off, ChatMode.Off, IapMode.ParentalGate, null, ConsentMode.ConsentRequired, ConsentMode.ConsentRequired, true, false),
            new AudienceRules(false, false, TelemetryMode.Off, TelemetryMode.Off, ChatMode.Free, IapMode.Allowed, null, ConsentMode.Allowed, ConsentMode.Allowed, false, true),
            new AdsConfig(false, Array.Empty<CertifiedAdsSdk>(), "https://support.google.com/googleplay/android-developer/answer/12955712", null),
            VendorNone,
            VendorNone,
            Array.Empty<ChatPreset>(),
            "",
            "",
            3,
            60);

        public AgeBandDef? Band(string? id)
        {
            foreach (var b in AgeBands)
            {
                if (string.Equals(b.Id, id, StringComparison.Ordinal))
                {
                    return b;
                }
            }

            return null;
        }

        /// <summary>The band for an age in whole years (bands are matched in file order by MaxAge).</summary>
        public AgeBandDef BandForAge(int age)
        {
            foreach (var b in AgeBands)
            {
                if (b.MaxAge == null || age <= b.MaxAge.Value)
                {
                    return b;
                }
            }

            return AgeBands[AgeBands.Count - 1];
        }

        public static ChildSafetyConfig Parse(string json)
        {
            if (JsonRead.Parse(json) is not JsonNode.Obj root)
            {
                throw new FormatException("child_safety.json must be an object.");
            }

            var d = Default;
            var bands = new List<AgeBandDef>();
            if (root.TryGet("age_bands", out var bn) && bn is JsonNode.Arr ba)
            {
                foreach (var i in ba.Items)
                {
                    if (i is JsonNode.Obj o)
                    {
                        var id = Str(o, "id", "");
                        if (id.Length == 0)
                        {
                            throw new FormatException("age band without id");
                        }

                        bands.Add(new AgeBandDef(id, o.TryGet("max_age", out var m) && m is JsonNode.Num mn ? mn.AsInt() : null, Bool(o, "child", true)));
                    }
                }
            }

            if (bands.Count == 0)
            {
                bands.AddRange(d.AgeBands);
            }

            ValidateBands(bands);

            var ads = d.Ads;
            if (root.TryGet("ads", out var an) && an is JsonNode.Obj ao)
            {
                var list = new List<CertifiedAdsSdk>();
                if (ao.TryGet("families_self_certified", out var ln) && ln is JsonNode.Arr la)
                {
                    foreach (var i in la.Items)
                    {
                        if (i is JsonNode.Obj o)
                        {
                            list.Add(new CertifiedAdsSdk(Str(o, "sdk", ""), Str(o, "version", "")));
                        }
                    }
                }

                ads = new AdsConfig(Bool(ao, "enabled", false), list, Str(ao, "source_url", d.Ads.SourceUrl), ao.TryGet("verified_on", out var v) && v is JsonNode.Str vs ? vs.Value : null);
            }

            var presets = new List<ChatPreset>();
            if (root.TryGet("chat_presets", out var pn) && pn is JsonNode.Arr pa)
            {
                foreach (var i in pa.Items)
                {
                    if (i is JsonNode.Obj o)
                    {
                        presets.Add(new ChatPreset(Str(o, "id", ""), Str(o, "text", "")));
                    }
                }
            }

            var telemetry = Obj(root, "telemetry");
            var account = Obj(root, "account");
            var gate = Obj(root, "parental_gate");
            return new ChildSafetyConfig(
                Int(root, "schema_version", 1),
                bands,
                Int(Obj(root, "age_gate"), "oldest_birth_year_span", d.OldestBirthYearSpan),
                Rules(Obj(root, "child"), d.ChildRules),
                Rules(Obj(root, "adult"), d.AdultRules),
                ads,
                Str(telemetry, "analytics_vendor", VendorNone),
                Str(telemetry, "crash_vendor", VendorNone),
                presets,
                Str(account, "web_deletion_url", ""),
                Str(account, "support_email", ""),
                Int(gate, "max_failures", d.ParentalGateMaxFailures),
                Int(gate, "lockout_seconds", d.ParentalGateLockoutSeconds));
        }

        private static void ValidateBands(List<AgeBandDef> bands)
        {
            int? last = null;
            for (var i = 0; i < bands.Count; i++)
            {
                var max = bands[i].MaxAge;
                if (max == null && i != bands.Count - 1)
                {
                    throw new FormatException("only the last age band may be open-ended");
                }

                if (max != null && last != null && max <= last)
                {
                    throw new FormatException("age bands must be in rising max_age order");
                }

                last = max;
            }

            if (bands[bands.Count - 1].MaxAge != null)
            {
                throw new FormatException("the last age band must be open-ended (no max_age)");
            }
        }

        private static AudienceRules Rules(JsonNode.Obj? o, AudienceRules d) => o == null
            ? d
            : new AudienceRules(
                Bool(o, "advertising_id", d.AdvertisingId),
                Bool(o, "personalization", d.Personalization),
                Enum(o, "analytics", d.Analytics),
                Enum(o, "crash_reporting", d.CrashReporting),
                Enum(o, "chat", d.Chat),
                Enum(o, "iap", d.Iap),
                o.TryGet("iap_max_per_day", out var n) && n is JsonNode.Num nn ? nn.AsInt() : d.IapMaxPerDay,
                Enum(o, "online_sign_in", d.OnlineSignIn),
                Enum(o, "cloud_save", d.CloudSave),
                Bool(o, "no_pressure", d.NoPressure),
                Bool(o, "can_create_alliance", d.CanCreateAlliance));

        private static T Enum<T>(JsonNode.Obj o, string key, T fallback)
            where T : struct, System.Enum
        {
            if (!o.TryGet(key, out var n) || n is not JsonNode.Str s)
            {
                return fallback;
            }

            var name = s.Value.Replace("_", "");
            if (!System.Enum.TryParse<T>(name, true, out var v))
            {
                throw new FormatException($"{key}: unknown value '{s.Value}'");
            }

            return v;
        }

        private static JsonNode.Obj? Obj(JsonNode.Obj? o, string key) => o != null && o.TryGet(key, out var n) ? n as JsonNode.Obj : null;

        private static string Str(JsonNode.Obj? o, string key, string fallback) => o != null && o.TryGet(key, out var n) && n is JsonNode.Str s ? s.Value : fallback;

        private static bool Bool(JsonNode.Obj o, string key, bool fallback) => o.TryGet(key, out var n) && n is JsonNode.Bool b ? b.Value : fallback;

        private static int Int(JsonNode.Obj? o, string key, int fallback) => o != null && o.TryGet(key, out var n) && n is JsonNode.Num num ? num.AsInt() : fallback;

        internal static string Inv(int v) => v.ToString(CultureInfo.InvariantCulture);
    }
}
