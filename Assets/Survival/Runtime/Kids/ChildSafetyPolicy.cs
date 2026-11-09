#nullable enable
using Survival.Domain.Iap;

namespace Survival.Domain.Kids
{
    /// <summary>
    /// What this player may do right now, from the pack config and the stored age answer. Unknown, unanswered, tampered or
    /// unrecognised bands are child mode (a Families app assumes a child until told otherwise). Some rules are floors the
    /// config cannot lift for children: no advertising id, no personalization, no free-text chat.
    /// </summary>
    public sealed class ChildSafetyPolicy
    {
        /// <param name="chatChoice">The player's (or, for a child, the parent's) Settings choice; clamped to <see cref="ChatMax"/>.</param>
        public ChildSafetyPolicy(ChildSafetyConfig config, AgeGateState state, ChatMode? chatChoice = null)
        {
            Config = config;
            State = state;
            var band = state.Status == AgeGateStatus.Answered ? config.Band(state.BandId) : null;
            IsChild = band == null || band.Child;
            BandId = band?.Id;
            var r = IsChild ? config.ChildRules : config.AdultRules;
            Rules = IsChild
                ? r with
                {
                    AdvertisingId = false,
                    Personalization = false,
                    Chat = r.Chat == ChatMode.Free ? ChatMode.Presets : r.Chat,
                }
                : r;
            ChatMax = IsChild ? (config.ChildChatMax == ChatMode.Free ? ChatMode.Presets : config.ChildChatMax) : ChatMode.Free;
            var chat = chatChoice ?? Rules.Chat;
            Rules = Rules with { Chat = chat > ChatMax ? ChatMax : chat };
        }

        /// <summary>The most chat this player can have: 13+ free; child per child.chat_max (presets by default, never free).</summary>
        public ChatMode ChatMax { get; }

        public ChildSafetyConfig Config { get; }
        public AgeGateState State { get; }

        /// <summary>Child rules apply (under-13 band, or age not known yet).</summary>
        public bool IsChild { get; }

        /// <summary>The answered band id, or null when unknown.</summary>
        public string? BandId { get; }

        /// <summary>The rules in force, after the child floors.</summary>
        public AudienceRules Rules { get; }

        /// <summary>The band in the IAP branch's shape (Survival.Domain.Iap.AgeBand) for its purchase gate.</summary>
        public AgeBand AgeBand => BandId == null ? AgeBand.Unknown : IsChild ? AgeBand.Under13 : AgeBand.ThirteenPlus;

        /// <summary>Analytics level after the vendor check: with no vendor chosen nothing is sent for anyone.</summary>
        public TelemetryMode Analytics => Config.AnalyticsVendor == ChildSafetyConfig.VendorNone ? TelemetryMode.Off : Rules.Analytics;

        public TelemetryMode CrashReporting => Config.CrashVendor == ChildSafetyConfig.VendorNone ? TelemetryMode.Off : Rules.CrashReporting;

        public bool AdvertisingIdAllowed => Rules.AdvertisingId;

        public bool PersonalizationAllowed => Rules.Personalization;

        public ChatMode Chat => Rules.Chat;

        public IapMode Iap => Rules.Iap;

        public bool NoPressure => Rules.NoPressure;

        public bool CanCreateAlliance => Rules.CanCreateAlliance;
    }
}
