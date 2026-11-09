#nullable enable
using System;

namespace Survival.Domain.Kids
{
    /// <summary>Features that collect personal info from the player (COPPA "personal information" incl. persistent ids).</summary>
    public enum ConsentPurpose
    {
        /// <summary>Alliance / world sign-in: device-derived id, display name, chat, power on the shared map.</summary>
        OnlineSignIn,

        /// <summary>Cloud save synced by account (soft-launch item 3).</summary>
        CloudSave,
    }

    public enum ConsentStatus
    {
        /// <summary>No verified consent on record. The default until Derek picks a consent method.</summary>
        NeedsConsent,

        /// <summary>A request is out (e.g. email-plus sent, waiting for the parent).</summary>
        Pending,

        Granted,
        Denied,
    }

    /// <summary>
    /// Verifiable parental consent (16 CFR 312.5), pluggable because the method is an open Derek decision (card check,
    /// ID check, knowledge-based, email-plus where allowed, or a VPC vendor such as a kids-consent service). The
    /// provider owns the parent-facing flow and how long consent lasts; the game only asks for a status.
    /// </summary>
    public interface IParentalConsentProvider
    {
        /// <summary>Short id for logs / the PR checklist ("none", "email_plus", vendor name).</summary>
        string Method { get; }

        ConsentStatus Status(ConsentPurpose purpose);

        /// <summary>Starts (or resumes) the parent flow. done gets the status afterwards (often Pending).</summary>
        void Request(ConsentPurpose purpose, Action<ConsentStatus> done);

        /// <summary>Parent revoked consent (or the account was deleted): forget it.</summary>
        void Revoke();
    }

    /// <summary>The default provider: no consent method chosen, so consent can never be granted.</summary>
    public sealed class NoConsentProvider : IParentalConsentProvider
    {
        public static readonly NoConsentProvider Instance = new();

        public string Method => "none";

        public ConsentStatus Status(ConsentPurpose purpose) => ConsentStatus.NeedsConsent;

        public void Request(ConsentPurpose purpose, Action<ConsentStatus> done) => done(ConsentStatus.NeedsConsent);

        public void Revoke()
        {
        }
    }

    public enum ConsentDecision
    {
        Allowed,
        NeedsConsent,
        Blocked,
    }

    /// <summary>
    /// Ask before any sign-in or cloud save. 13+: per adult rules (allowed by default). Child / unknown age: per
    /// child.online_sign_in / child.cloud_save; with consent_required only a Granted provider status lets it through.
    /// The game keeps working offline in every other case (local guild, on-device save).
    /// </summary>
    public static class ConsentGate
    {
        public static ConsentDecision Decide(ChildSafetyPolicy policy, IParentalConsentProvider provider, ConsentPurpose purpose)
        {
            var mode = purpose == ConsentPurpose.CloudSave ? policy.Rules.CloudSave : policy.Rules.OnlineSignIn;
            switch (mode)
            {
                case ConsentMode.Allowed:
                    return ConsentDecision.Allowed;
                case ConsentMode.Blocked:
                    return ConsentDecision.Blocked;
                default:
                    return provider.Status(purpose) == ConsentStatus.Granted ? ConsentDecision.Allowed : ConsentDecision.NeedsConsent;
            }
        }

        public static bool Allows(ChildSafetyPolicy policy, IParentalConsentProvider provider, ConsentPurpose purpose) =>
            Decide(policy, provider, purpose) == ConsentDecision.Allowed;
    }
}
