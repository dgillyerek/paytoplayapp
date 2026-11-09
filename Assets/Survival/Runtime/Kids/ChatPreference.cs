#nullable enable
using System.Collections.Generic;

namespace Survival.Domain.Kids
{
    /// <summary>
    /// The Settings chat choice (off / presets / free), stored on device. <see cref="ChildSafetyPolicy"/> clamps it, so an
    /// edited value can never give a child free chat, and the server clamps again (safety.mjs).
    /// </summary>
    public sealed class ChatPreference
    {
        public const string Key = "ks.chat";

        private readonly IKidSafetyStorage _storage;

        public ChatPreference(IKidSafetyStorage storage) => _storage = storage;

        public ChatMode? Load() => ChatSafety.ParseChatMode(_storage.Get(Key));

        public void Save(ChatMode mode)
        {
            _storage.Set(Key, ChatSafety.ChatModeId(mode));
            _storage.Save();
        }

        public void Clear()
        {
            _storage.Delete(Key);
            _storage.Save();
        }
    }

    public enum ConsentView
    {
        /// <summary>13+, or the feature is allowed without consent.</summary>
        NotNeeded,
        Needed,
        Pending,
        Granted,
        Denied,

        /// <summary>Config blocks the feature for children; consent cannot unlock it.</summary>
        Unavailable,
    }

    /// <summary>What the Settings "Privacy and account" section shows and allows, kept out of the Unity view so it is testable.</summary>
    public sealed class PrivacySettings
    {
        public PrivacySettings(ChildSafetyPolicy policy, IParentalConsentProvider consent)
        {
            Policy = policy;
            AgeBandKey = policy.BandId == null ? "unknown" : policy.BandId;
            Chat = policy.Chat;
            var choices = new List<ChatMode> { ChatMode.Off };
            if (policy.ChatMax >= ChatMode.Presets)
            {
                choices.Add(ChatMode.Presets);
            }

            if (policy.ChatMax >= ChatMode.Free)
            {
                choices.Add(ChatMode.Free);
            }

            ChatChoices = choices;
            Consent = policy.Rules.OnlineSignIn switch
            {
                ConsentMode.Allowed => ConsentView.NotNeeded,
                ConsentMode.Blocked => ConsentView.Unavailable,
                _ => consent.Status(ConsentPurpose.OnlineSignIn) switch
                {
                    ConsentStatus.Granted => ConsentView.Granted,
                    ConsentStatus.Pending => ConsentView.Pending,
                    ConsentStatus.Denied => ConsentView.Denied,
                    _ => ConsentView.Needed,
                },
            };
            CanRequestConsent = policy.Rules.OnlineSignIn == ConsentMode.ConsentRequired && Consent != ConsentView.Granted
                && consent.Method != NoConsentProvider.Instance.Method;
        }

        public ChildSafetyPolicy Policy { get; }

        /// <summary>Band id for the read-only row ("under13", "13plus", or "unknown" before the gate / after tampering).</summary>
        public string AgeBandKey { get; }

        public ChatMode Chat { get; }

        public IReadOnlyList<ChatMode> ChatChoices { get; }

        public ConsentView Consent { get; }

        /// <summary>A consent method is plugged in and consent is still missing.</summary>
        public bool CanRequestConsent { get; }

        /// <summary>Child mode: turning chat on (more chat than now) needs the parental gate; turning it down never does.</summary>
        public bool ChangeNeedsParent(ChatMode to) => Policy.IsChild && to > Chat;

        public bool CanChoose(ChatMode to) => to <= Policy.ChatMax;

        /// <summary>Opening a link out of the app: parental gate in child mode (Families policy).</summary>
        public bool LinkNeedsParent => Policy.IsChild;

        /// <summary>Delete account: parental gate in child mode, a confirm step for 13+.</summary>
        public bool DeleteNeedsParent => Policy.IsChild;
    }
}
