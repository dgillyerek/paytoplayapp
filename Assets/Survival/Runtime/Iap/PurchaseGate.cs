#nullable enable
using System;

namespace Survival.Domain.Iap
{
    /// <summary>Age band on the account (no birth date is stored, only the band). Default is Unknown.</summary>
    public enum AgeBand
    {
        Unknown,
        Under13,
        ThirteenPlus
    }

    public static class AgeBands
    {
        public const string Unknown = "unknown";
        public const string Under13 = "under13";
        public const string ThirteenPlus = "13plus";

        public static string Id(AgeBand band) => band switch
        {
            AgeBand.Under13 => Under13,
            AgeBand.ThirteenPlus => ThirteenPlus,
            _ => Unknown,
        };

        public static AgeBand Parse(string? id) => id switch
        {
            Under13 => AgeBand.Under13,
            ThirteenPlus => AgeBand.ThirteenPlus,
            _ => AgeBand.Unknown,
        };
    }

    /// <summary>
    /// Asked before every new purchase is sent to the store (COPPA / Google Play Families). It can allow, block, or run a
    /// parental-consent step first. Re-delivered or restored purchases are already paid, so they are never gated.
    /// done(allowed, reason): reason is an error code when not allowed (iap.age_unknown, iap.consent_required, ...).
    /// </summary>
    public interface IPurchaseGate
    {
        void Check(string productId, Action<bool, string?> done);
    }

    /// <summary>
    /// Parental consent step for under-13 accounts. STUB: Product will send the exact requirements (verifiable consent
    /// method, what the parent sees, how long consent lasts). Until then <see cref="NoParentalConsent"/> declines.
    /// </summary>
    public interface IParentalConsent
    {
        void Request(string productId, Action<bool> granted);
    }

    /// <summary>The consent stub: always declines, so under-13 accounts cannot buy until the real flow ships.</summary>
    public sealed class NoParentalConsent : IParentalConsent
    {
        public void Request(string productId, Action<bool> granted) => granted(false);
    }

    /// <summary>
    /// The age-band gate: 13plus buys; under13 needs parental consent first; unknown is blocked unless
    /// <c>allowUnknown</c> is set. Blocked-by-default for unknown is deliberate: Product has not yet said an unknown age
    /// may buy, and a Families app must assume a child until told otherwise.
    /// </summary>
    public sealed class AgeBandPurchaseGate : IPurchaseGate
    {
        private readonly Func<AgeBand> _band;
        private readonly IParentalConsent _consent;
        private readonly bool _allowUnknown;

        public AgeBandPurchaseGate(Func<AgeBand> band, IParentalConsent? consent = null, bool allowUnknown = false)
        {
            _band = band ?? throw new ArgumentNullException(nameof(band));
            _consent = consent ?? new NoParentalConsent();
            _allowUnknown = allowUnknown;
        }

        /// <summary>The safe default: unknown age, no consent flow → every purchase blocked.</summary>
        public static AgeBandPurchaseGate Default() => new(() => AgeBand.Unknown);

        public void Check(string productId, Action<bool, string?> done)
        {
            switch (_band())
            {
                case AgeBand.ThirteenPlus:
                    done(true, null);
                    return;
                case AgeBand.Under13:
                    _consent.Request(productId, ok => done(ok, ok ? null : "iap.consent_required"));
                    return;
                default:
                    done(_allowUnknown, _allowUnknown ? null : "iap.age_unknown");
                    return;
            }
        }
    }

    /// <summary>Lets every purchase through (tests, and non-Families flavors if Product ever ships one).</summary>
    public sealed class AllowAllPurchaseGate : IPurchaseGate
    {
        public static readonly AllowAllPurchaseGate Instance = new();

        public void Check(string productId, Action<bool, string?> done) => done(true, null);
    }
}
