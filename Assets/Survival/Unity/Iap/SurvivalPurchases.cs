#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Iap;
using Survival.Domain.Store;
using UnityEngine;

namespace Survival.Unity.Iap
{
    /// <summary>
    /// Picks the purchase service for this build.
    /// Live (Unity IAP + server validation): Android player builds that are not Development Builds, or any build with
    /// the SURVIVAL_REAL_IAP scripting define. Sandbox (free, nothing charged): the Editor, Development Builds, iOS until
    /// the App Store validator is live, or any build with SURVIVAL_SANDBOX_IAP. A live build that cannot start billing
    /// gets <see cref="DisabledPurchaseService"/>, never the sandbox, so a release build can never hand out free packs.
    /// </summary>
    public static class SurvivalPurchases
    {
        private const string AgeBandPref = "account.age_band";

        /// <summary>
        /// COPPA / Play Families parental-consent step for under-13 accounts. STUB until Product sends the requirements:
        /// the default declines, so under-13 accounts cannot buy. Replace with the real flow when it exists.
        /// </summary>
        public static IParentalConsent ParentalConsent { get; set; } = new NoParentalConsent();

        /// <summary>
        /// Unknown age may buy only when Product's spec says so. It has not, so this stays false: with no age screen yet,
        /// live purchases are blocked until the player's band is set to 13plus (or under13 with consent).
        /// </summary>
        public static bool AllowUnknownAge { get; set; }

        /// <summary>
        /// The age band from the first-launch neutral age gate (Kids.KidSafety, signed on-device record). That answer is the
        /// only source: the server copy and the plain "account.age_band" pref are written from it but never read back, so
        /// neither can lift a child to 13plus. Unanswered, edited or removed answers read as Unknown (blocked).
        /// </summary>
        public static AgeBand CurrentAgeBand => Kids.KidSafety.AgeBand;

        /// <summary>Hook for the (future) age screen: saves the band on the device and on the account. Only the band, never a birth date.</summary>
        public static void SetAgeBand(AgeBand band)
        {
            PlayerPrefs.SetString(AgeBandPref, AgeBands.Id(band));
            PlayerPrefs.Save();
            SurvivalCampaignView.AllianceNet?.SetAgeBand(AgeBands.Id(band));
        }

        /// <summary>The live service when this build uses one (for Restore buttons and the localized price).</summary>
        public static PurchaseService? Live { get; private set; }

#if SURVIVAL_UNITY_IAP && !SURVIVAL_SANDBOX_IAP && ((UNITY_ANDROID && !UNITY_EDITOR && !DEVELOPMENT_BUILD) || SURVIVAL_REAL_IAP)
        public static readonly bool UsesLiveStore = true;
#else
        public static readonly bool UsesLiveStore = false;
#endif

        public static ISkuPurchaseService Create(MonoBehaviour runner, string packRoot, IReadOnlyDictionary<string, ProductDef> products)
        {
            // Read the reward table in every build so the player bake always carries it.
            var rewardsPath = Path.Combine(packRoot, "store", IapRewardCatalog.FileName);
            IapRewardCatalog? rewards = null;
            try
            {
                rewards = PlayerAssets.FileExists(rewardsPath) ? IapRewardCatalog.Parse(PlayerAssets.ReadAllText(rewardsPath)) : null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (!UsesLiveStore)
            {
                return new SandboxPurchaseService();
            }

#if SURVIVAL_UNITY_IAP
            try
            {
                if (rewards == null)
                {
                    return new DisabledPurchaseService("iap.no_reward_table");
                }

                var api = PlayerPrefs.GetString("alliance.api", SurvivalCampaignView.DefaultAllianceApi);
                if (string.IsNullOrWhiteSpace(api))
                {
                    api = SurvivalCampaignView.DefaultAllianceApi;
                }

                // Same account as the alliance client: reuse its bearer token, never sign in a second time.
                var verifier = new ServerReceiptVerifier(new UnityAllianceTransport(runner, api), () => SurvivalCampaignView.AllianceNet?.SessionToken);
                var live = new PurchaseService(new UnityIapStore(), verifier, new PlayerPrefsAppliedPurchases(), rewards.ProductsFor(products.Values),
                    new AgeBandPurchaseGate(() => CurrentAgeBand, new DeferredConsent(), AllowUnknownAge));
                live.Initialize();
                runner.StartCoroutine(RetryUnverified(live));
                Live = live;
                return live;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return new DisabledPurchaseService("iap.start_failed");
            }
#else
            return new DisabledPurchaseService("iap.package_missing");
#endif
        }

        /// <summary>Re-sends purchases the server could not validate yet (offline, or before the alliance sign-in finished).</summary>
        private static IEnumerator RetryUnverified(PurchaseService live)
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(15f);
                if (live.UnverifiedCount > 0 && !string.IsNullOrEmpty(SurvivalCampaignView.AllianceNet?.SessionToken))
                {
                    live.RetryUnverified();
                }
            }
        }
    }

    /// <summary>Reads <see cref="SurvivalPurchases.ParentalConsent"/> at ask time, so the real flow can be plugged in later.</summary>
    internal sealed class DeferredConsent : IParentalConsent
    {
        public void Request(string productId, Action<bool> granted) => SurvivalPurchases.ParentalConsent.Request(productId, granted);
    }

    /// <summary>Live build whose billing could not start: every purchase fails, nothing is granted.</summary>
    public sealed class DisabledPurchaseService : ISkuPurchaseService
    {
        private readonly string _reason;

        public DisabledPurchaseService(string reason) => _reason = reason;

        public bool IsSandbox => false;

        public void Purchase(string productId, Action<PurchaseOutcome> done) => done(new PurchaseOutcome(false, productId, "", _reason));
    }

    /// <summary>Applied-purchase ledger in PlayerPrefs (last 200 keys, newest last).</summary>
    public sealed class PlayerPrefsAppliedPurchases : IAppliedPurchases
    {
        private const string Pref = "iap.applied";
        private const int Max = 200;
        private readonly List<string> _keys = new();

        public PlayerPrefsAppliedPurchases()
        {
            foreach (var k in PlayerPrefs.GetString(Pref, "").Split('\n'))
            {
                if (k.Length > 0)
                {
                    _keys.Add(k);
                }
            }
        }

        public bool Contains(string key) => _keys.Contains(Hash(key));

        public void Add(string key)
        {
            var h = Hash(key);
            if (_keys.Contains(h))
            {
                return;
            }

            _keys.Add(h);
            if (_keys.Count > Max)
            {
                _keys.RemoveRange(0, _keys.Count - Max);
            }

            PlayerPrefs.SetString(Pref, string.Join("\n", _keys));
            PlayerPrefs.Save();
        }

        /// <summary>Purchase tokens are long; keep a short stable digest instead.</summary>
        private static string Hash(string key)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
            return BitConverter.ToString(bytes, 0, 12).Replace("-", "");
        }
    }
}
