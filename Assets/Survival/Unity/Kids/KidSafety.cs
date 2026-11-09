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

        public static ChildSafetyConfig Config => _config ?? Load(AppFlavorConfig.FantasyKingdomA);

        public static AgeGateStore Store => _store ??= new AgeGateStore(new PlayerPrefsKidStorage());

        /// <summary>Rebuilt on each read: cheap, and always reflects the latest answer.</summary>
        public static ChildSafetyPolicy Policy => new ChildSafetyPolicy(Config, Store.Load());

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
            Changed?.Invoke();
        }
    }
}
