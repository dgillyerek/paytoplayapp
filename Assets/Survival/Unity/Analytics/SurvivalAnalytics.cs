#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Analytics;
using Survival.Domain.Iap;
using Survival.Domain.Kids;
using Survival.Unity.Kids;
using UnityEngine;

namespace Survival.Unity.Analytics
{
    /// <summary>
    /// App entry for funnel events + crashes. Always goes through <see cref="KidGatedAnalytics"/> /
    /// <see cref="KidGatedCrashReporter"/> (under-13 and unknown age are hard-off). Vendor SDK packages are
    /// not referenced yet — plug one in via <see cref="KidSafety.SetVendorSink"/> after Derek decides.
    /// </summary>
    public static class SurvivalAnalytics
    {
        private const string PrefsFirstOpenAt = "survival.analytics.first_open_at_unix";
        private const string PrefsReturnD1 = "survival.analytics.return_d1_sent";
        private const string PrefsReturnD7 = "survival.analytics.return_d7_sent";

        private static IAnalytics? _analytics;
        private static ICrashReporter? _crash;

        public static IAnalytics Analytics => _analytics ??= BuildAnalytics();
        public static ICrashReporter Crash => _crash ??= BuildCrash();

        public static void ResetForTests()
        {
            _analytics = null;
            _crash = null;
        }

        /// <summary>Call once after boot (and after age-gate may have been answered). Emits first_open / d1 / d7 markers.</summary>
        public static void OnSessionStart(double nowUnixSeconds)
        {
            // first_open: only once per install, local prefs (no device id).
            if (!PlayerPrefs.HasKey(PrefsFirstOpenAt))
            {
                PlayerPrefs.SetString(PrefsFirstOpenAt, ((long)nowUnixSeconds).ToString());
                PlayerPrefs.Save();
                Analytics.Track(FunnelEvents.FirstOpen);
            }
            else if (long.TryParse(PlayerPrefs.GetString(PrefsFirstOpenAt, "0"), out var first))
            {
                var days = (nowUnixSeconds - first) / 86400.0;
                if (days >= 1 && PlayerPrefs.GetInt(PrefsReturnD1, 0) == 0)
                {
                    PlayerPrefs.SetInt(PrefsReturnD1, 1);
                    PlayerPrefs.Save();
                    Analytics.Track(FunnelEvents.ReturnD1);
                }

                if (days >= 7 && PlayerPrefs.GetInt(PrefsReturnD7, 0) == 0)
                {
                    PlayerPrefs.SetInt(PrefsReturnD7, 1);
                    PlayerPrefs.Save();
                    Analytics.Track(FunnelEvents.ReturnD7);
                }
            }
        }

        public static void AgeGateShown() => Analytics.Track(FunnelEvents.AgeGateShown);

        public static void AgeGateCompleted(AgeBand band) =>
            Analytics.Track(FunnelEvents.AgeGateCompleted, FunnelEvents.BandOnly(AgeBands.Id(band)));

        public static void TutorialStepStart(string stepId) =>
            Analytics.Track(FunnelEvents.TutorialStepStart, FunnelEvents.Step(stepId));

        public static void TutorialStepComplete(string stepId) =>
            Analytics.Track(FunnelEvents.TutorialStepComplete, FunnelEvents.Step(stepId));

        public static void QuestStepComplete(string chainId, string questId, int stepIndex) =>
            Analytics.Track(FunnelEvents.QuestStepComplete, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [FunnelEvents.PropChain] = chainId,
                [FunnelEvents.PropQuest] = questId,
                [FunnelEvents.PropStep] = stepIndex.ToString(),
            });

        public static void LevelUp(int level) => Analytics.Track(FunnelEvents.LevelUp, FunnelEvents.Level(level));

        public static void FirstBattle(string result) =>
            Analytics.Track(FunnelEvents.FirstBattle, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [FunnelEvents.PropResult] = result,
            });

        public static void StoreView(string screen) =>
            Analytics.Track(FunnelEvents.StoreView, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [FunnelEvents.PropScreen] = screen,
            });

        public static void IapAttempt(string sku) => Analytics.Track(FunnelEvents.IapAttempt, FunnelEvents.Sku(sku));
        public static void IapSuccess(string sku) => Analytics.Track(FunnelEvents.IapSuccess, FunnelEvents.Sku(sku));

        public static void IapFail(string sku, string reason) =>
            Analytics.Track(FunnelEvents.IapFail, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [FunnelEvents.PropSku] = sku,
                [FunnelEvents.PropReason] = reason,
            });

        public static void ReportCrash(Exception error) => Crash.Report(error);

        private static IAnalytics BuildAnalytics()
        {
            // Vendor sink stays NoOp until SetVendorSink; KidGated still hard-blocks under13/unknown.
            var bridge = new TelemetrySinkAnalytics(KidSafety.Telemetry);
            return new KidGatedAnalytics(bridge, () => KidSafety.AgeBand, () => KidSafety.Policy.Analytics);
        }

        private static ICrashReporter BuildCrash()
        {
            var bridge = new TelemetrySinkAnalytics(KidSafety.Telemetry);
            return new KidGatedCrashReporter(bridge, () => KidSafety.AgeBand, () => KidSafety.Policy.CrashReporting);
        }
    }
}
