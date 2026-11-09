#nullable enable
using System;
using System.IO;
using Survival.Domain.Flavor;
using Survival.Domain.Iap;
using Survival.Domain.Kids;
using UnityEngine;

namespace Survival.Unity.Kids
{
    /// <summary>PlayerPrefs behind the kid-safety storage shape.</summary>
    public sealed class PlayerPrefsKidStorage : IKidSafetyStorage
    {
        public string? Get(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        public void Set(string key, string value) => PlayerPrefs.SetString(key, value);

        public void Delete(string key) => PlayerPrefs.DeleteKey(key);

        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// The app-wide child-safety state: pack config (kids/child_safety.json), the stored age answer, and the policy built from
    /// them. Everything that collects data, shows chat, sells, or nudges asks <see cref="Policy"/> first. Until the age gate
    /// is answered the policy is child mode.
    /// </summary>
    public static class KidSafety
    {
        private static ChildSafetyConfig? _config;
        private static AgeGateStore? _store;
        private static ITelemetrySink? _telemetry;
        private static ChatPreference? _chat;
        private static IParentalConsentProvider _consent = NoConsentProvider.Instance;

        public static ChildSafetyConfig Config => _config ?? Load(AppFlavorConfig.FantasyKingdomA);

        public static AgeGateStore Store => _store ??= new AgeGateStore(new PlayerPrefsKidStorage());

        /// <summary>Rebuilt on each read: cheap, and always reflects the latest answer.</summary>
        public static ChildSafetyPolicy Policy => new ChildSafetyPolicy(Config, Store.Load(), Chat.Load());

        /// <summary>The Settings chat choice; the policy clamps it (a child never gets free chat).</summary>
        public static ChatPreference Chat => _chat ??= new ChatPreference(new PlayerPrefsKidStorage());

        /// <summary>Settings: save the chat choice and tell listeners (the guild view and the server mirror).</summary>
        public static void SetChat(ChatMode mode)
        {
            Chat.Save(mode);
            Changed?.Invoke();
        }

        public static bool IsChild => Policy.IsChild;

        /// <summary>For the IAP branch's AgeBandPurchaseGate / SurvivalPurchases.CurrentAgeBand.</summary>
        public static AgeBand AgeBand => Policy.AgeBand;

        public static bool NeedsAgeGate => Store.Load().NeedsGate;

        /// <summary>Raised after the age answer is saved (or cleared by account deletion).</summary>
        public static event Action? Changed;

        /// <summary>
        /// The only telemetry door. The analytics / crash work replaces the vendor with <see cref="SetVendorSink"/>; the policy
        /// wrapper still drops or strips per audience, and with telemetry.*_vendor = "none" nothing is ever sent.
        /// </summary>
        public static ITelemetrySink Telemetry => _telemetry ??= new PolicyTelemetry(NoOpTelemetrySink.Instance, () => Policy);

        public static void SetVendorSink(ITelemetrySink vendor) => _telemetry = new PolicyTelemetry(vendor, () => Policy);

        /// <summary>
        /// Verifiable parental consent hook. Default: <see cref="NoConsentProvider"/> (never granted), so with
        /// child.online_sign_in / child.cloud_save = consent_required an under-13 / unknown-age player stays offline.
        /// The chosen VPC method or vendor plugs in here.
        /// </summary>
        public static IParentalConsentProvider Consent
        {
            get => _consent;
            set => _consent = value ?? NoConsentProvider.Instance;
        }

        /// <summary>Ask before alliance / world sign-in.</summary>
        public static bool AllowsOnlineSignIn => ConsentGate.Allows(Policy, _consent, ConsentPurpose.OnlineSignIn);

        /// <summary>Ask before any cloud save upload or download (the cloud-save work calls this).</summary>
        public static bool AllowsCloudSave => ConsentGate.Allows(Policy, _consent, ConsentPurpose.CloudSave);

        public static ChildSafetyConfig Load(AppFlavorConfig flavor)
        {
            var path = Path.Combine(SurvivalArt.ResolvePackRoot(flavor), "kids", ChildSafetyConfig.FileName);
            try
            {
                _config = PlayerAssets.FileExists(path) ? ChildSafetyConfig.Parse(PlayerAssets.ReadAllText(path)) : ChildSafetyConfig.Default;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _config = ChildSafetyConfig.Default;
            }

            return _config;
        }

        /// <summary>
        /// Plugs the parental gate into the IAP branch's purchase gate (#201 AgeBandPurchaseGate asks it for every under-13
        /// purchase): child.iap decides blocked / parental_gate / allowed, plus the optional daily cap.
        /// </summary>
        public static void InstallPurchaseConsent()
        {
            if (Iap.SurvivalPurchases.ParentalConsent is not ParentalGateConsent)
            {
                Iap.SurvivalPurchases.ParentalConsent = new ParentalGateConsent(() => Policy, new ParentalGateScreen.Gate(), new ChildPurchaseCounter(new PlayerPrefsKidStorage()));
            }
        }

        public static void SaveAnswer(AgeBandDef band)
        {
            if (Store.Save(band))
            {
                // Mirror the band to the account (players.age_band, IAP #201) once signed in; never a birth date.
                Iap.SurvivalPurchases.SetAgeBand(AgeBand);
                Changed?.Invoke();
            }
        }

        /// <summary>Account deletion only: forget the age answer so the next launch asks again.</summary>
        public static void ClearForAccountDeletion()
        {
            Store.Clear();
            Chat.Clear();
            _consent.Revoke();
            Changed?.Invoke();
        }
    }
}
