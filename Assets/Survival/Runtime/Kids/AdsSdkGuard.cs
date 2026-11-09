#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Kids
{
    /// <summary>
    /// The only switch that can turn an ads SDK on. Soft launch ships no ads (ads.enabled = false). If ads ever ship in this
    /// mixed-audience Families app, the exact SDK and version must be on ads.families_self_certified, copied by hand from
    /// Google's Families Self-Certified Ads SDK list (ads.source_url) with the date checked in ads.verified_on.
    /// Applies to every player, not only children, because the age answer is self-declared.
    /// </summary>
    public static class AdsSdkGuard
    {
        /// <summary>Package / artifact names of ads SDKs we know of; any of them in the build must be on the allowlist.</summary>
        public static readonly IReadOnlyList<string> KnownAdsSdks = new[]
        {
            "com.unity.ads",
            "com.unity.services.levelplay",
            "com.google.ads.mobile",
            "com.google.android.gms:play-services-ads",
            "com.applovin",
            "com.ironsource",
            "com.chartboost",
            "com.inmobi",
            "com.vungle",
            "com.liftoff",
            "com.kidoz",
            "tv.superawesome",
            "com.facebook.audience",
            "com.mintegral",
            "com.bytedance.sdk.openadsdk",
        };

        public static bool CanEnable(ChildSafetyConfig config, string sdk, string version, out string reason)
        {
            if (!config.Ads.Enabled)
            {
                reason = "ads.disabled";
                return false;
            }

            if (string.IsNullOrEmpty(config.Ads.VerifiedOn))
            {
                reason = "ads.allowlist_unverified";
                return false;
            }

            foreach (var c in config.Ads.FamiliesSelfCertified)
            {
                if (string.Equals(c.Sdk, sdk, StringComparison.Ordinal) && string.Equals(c.Version, version, StringComparison.Ordinal))
                {
                    reason = "";
                    return true;
                }
            }

            reason = "ads.not_families_self_certified";
            return false;
        }

        /// <summary>True when a package name belongs to a known ads SDK.</summary>
        public static bool IsAdsSdk(string packageName)
        {
            foreach (var k in KnownAdsSdks)
            {
                if (packageName.StartsWith(k, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Ads SDK packages present in a dependency list that are not allowlisted (empty = OK to ship).</summary>
        public static IReadOnlyList<string> Unapproved(ChildSafetyConfig config, IEnumerable<KeyValuePair<string, string>> packages)
        {
            var bad = new List<string>();
            foreach (var p in packages)
            {
                if (IsAdsSdk(p.Key) && !CanEnable(config, p.Key, p.Value, out _))
                {
                    bad.Add(p.Key + "@" + p.Value);
                }
            }

            return bad;
        }
    }
}
