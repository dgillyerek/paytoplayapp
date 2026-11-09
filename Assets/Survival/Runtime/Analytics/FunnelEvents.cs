#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Analytics
{
    /// <summary>
    /// Soft-launch core funnel. Names are stable; props are allow-listed per event so a future vendor sink cannot
    /// accidentally ship ad IDs, device IDs, or free-text PII. Age gate only ever carries the band string.
    /// </summary>
    public static class FunnelEvents
    {
        public const string FirstOpen = "first_open";
        public const string AgeGateShown = "age_gate_shown";
        public const string AgeGateCompleted = "age_gate_completed";
        public const string TutorialStepStart = "tutorial_step_start";
        public const string TutorialStepComplete = "tutorial_step_complete";
        public const string QuestStepComplete = "quest_step_complete";
        public const string LevelUp = "level_up";
        public const string FirstBattle = "first_battle";
        public const string StoreView = "store_view";
        public const string IapAttempt = "iap_attempt";
        public const string IapSuccess = "iap_success";
        public const string IapFail = "iap_fail";
        public const string ReturnD1 = "return_d1";
        public const string ReturnD7 = "return_d7";

        public const string PropBand = "band";
        public const string PropStep = "step";
        public const string PropQuest = "quest";
        public const string PropChain = "chain";
        public const string PropLevel = "level";
        public const string PropSku = "sku";
        public const string PropResult = "result";
        public const string PropScreen = "screen";
        public const string PropReason = "reason";

        /// <summary>Hard-forbidden prop keys (COPPA / Play Families): never leave the device via analytics.</summary>
        public static readonly HashSet<string> ForbiddenProps = new(StringComparer.OrdinalIgnoreCase)
        {
            "ad_id", "adid", "advertising_id", "advertisingid", "aaid", "idfa", "idfv",
            "device_id", "deviceid", "android_id", "androidid", "imei", "imsi", "mac",
            "email", "name", "display_name", "displayname", "phone", "birth_date", "birthdate",
            "birthday", "user_id", "userid", "player_id", "playerid", "token", "ip", "ip_address",
        };

        private static readonly Dictionary<string, HashSet<string>> Schema = new(StringComparer.Ordinal)
        {
            [FirstOpen] = new(StringComparer.Ordinal),
            [AgeGateShown] = new(StringComparer.Ordinal),
            [AgeGateCompleted] = new(StringComparer.Ordinal) { PropBand },
            [TutorialStepStart] = new(StringComparer.Ordinal) { PropStep },
            [TutorialStepComplete] = new(StringComparer.Ordinal) { PropStep },
            [QuestStepComplete] = new(StringComparer.Ordinal) { PropChain, PropQuest, PropStep },
            [LevelUp] = new(StringComparer.Ordinal) { PropLevel },
            [FirstBattle] = new(StringComparer.Ordinal) { PropResult },
            [StoreView] = new(StringComparer.Ordinal) { PropScreen },
            [IapAttempt] = new(StringComparer.Ordinal) { PropSku },
            [IapSuccess] = new(StringComparer.Ordinal) { PropSku },
            [IapFail] = new(StringComparer.Ordinal) { PropSku, PropReason },
            [ReturnD1] = new(StringComparer.Ordinal),
            [ReturnD7] = new(StringComparer.Ordinal),
        };

        public static IReadOnlyCollection<string> AllNames => Schema.Keys;

        public static bool IsKnown(string eventName) => Schema.ContainsKey(eventName);

        /// <summary>
        /// Returns null when the event + props are safe to send; otherwise a short reason.
        /// Unknown event names fail. Extra props fail. Forbidden keys fail even if allow-listed by mistake.
        /// </summary>
        public static string? Validate(string eventName, IReadOnlyDictionary<string, string>? props)
        {
            if (string.IsNullOrWhiteSpace(eventName) || !Schema.TryGetValue(eventName, out var allowed))
            {
                return "unknown_event";
            }

            if (props != null)
            {
                foreach (var kv in props)
                {
                    if (ForbiddenProps.Contains(kv.Key))
                    {
                        return "forbidden_prop:" + kv.Key;
                    }

                    if (!allowed.Contains(kv.Key))
                    {
                        return "unexpected_prop:" + kv.Key;
                    }

                    if (kv.Value != null && kv.Value.Length > 64)
                    {
                        return "prop_too_long:" + kv.Key;
                    }
                }
            }

            if (eventName == AgeGateCompleted)
            {
                if (props == null || !props.TryGetValue(PropBand, out var band) || string.IsNullOrEmpty(band))
                {
                    return "band_required";
                }

                // Band only — never a birth date. Accept the IAP AgeBands strings + unknown.
                if (band is not ("unknown" or "under13" or "13plus"))
                {
                    return "bad_band";
                }
            }

            return null;
        }

        public static IReadOnlyDictionary<string, string> BandOnly(string band) =>
            new Dictionary<string, string>(StringComparer.Ordinal) { [PropBand] = band };

        public static IReadOnlyDictionary<string, string> Step(string stepId) =>
            new Dictionary<string, string>(StringComparer.Ordinal) { [PropStep] = stepId };

        public static IReadOnlyDictionary<string, string> Level(int level) =>
            new Dictionary<string, string>(StringComparer.Ordinal) { [PropLevel] = level.ToString() };

        public static IReadOnlyDictionary<string, string> Sku(string sku) =>
            new Dictionary<string, string>(StringComparer.Ordinal) { [PropSku] = sku };
    }
}
