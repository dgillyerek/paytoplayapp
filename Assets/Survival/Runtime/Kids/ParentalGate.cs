#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Iap;
using Survival.Domain.Store;

namespace Survival.Domain.Kids
{
    /// <summary>
    /// A grown-up check before a real-money purchase in child mode (Google Play Families "parental gate"): a multiplication
    /// a young child is unlikely to solve, typed on a keypad. Not verifiable parental consent; that is a
    /// separate, pluggable hook for sign-in and cloud save.
    /// </summary>
    public sealed record ParentalGateChallenge(int A, int B)
    {
        public int Answer => A * B;

        /// <summary>Two factors 6..12, never a square of the same number twice in a row being trivial (6x6 allowed).</summary>
        public static ParentalGateChallenge Create(Random rng) => new(rng.Next(6, 13), rng.Next(6, 13));
    }

    public enum ParentalGateResult
    {
        Passed,
        Wrong,
        LockedOut,
    }

    /// <summary>
    /// One gate attempt: a fresh question after every wrong answer, and a lockout after too many misses so guessing does
    /// not work. Lockout survives closing the gate (the session is shared for the app run).
    /// </summary>
    public sealed class ParentalGateSession
    {
        private readonly Random _rng;
        private readonly int _maxFailures;
        private readonly TimeSpan _lockout;
        private readonly Func<DateTime> _now;
        private int _failures;
        private DateTime _lockedUntil = DateTime.MinValue;

        public ParentalGateSession(int maxFailures, int lockoutSeconds, Func<DateTime>? now = null, Random? rng = null)
        {
            _maxFailures = Math.Max(1, maxFailures);
            _lockout = TimeSpan.FromSeconds(Math.Max(0, lockoutSeconds));
            _now = now ?? (() => DateTime.UtcNow);
            _rng = rng ?? new Random();
            Challenge = ParentalGateChallenge.Create(_rng);
        }

        public ParentalGateChallenge Challenge { get; private set; }

        public bool IsLockedOut => _now() < _lockedUntil;

        public TimeSpan LockoutLeft => IsLockedOut ? _lockedUntil - _now() : TimeSpan.Zero;

        public ParentalGateResult Submit(int? answer)
        {
            if (IsLockedOut)
            {
                return ParentalGateResult.LockedOut;
            }

            if (answer == Challenge.Answer)
            {
                _failures = 0;
                Challenge = ParentalGateChallenge.Create(_rng);
                return ParentalGateResult.Passed;
            }

            _failures++;
            Challenge = ParentalGateChallenge.Create(_rng);
            if (_failures >= _maxFailures)
            {
                _failures = 0;
                _lockedUntil = _now() + _lockout;
                return ParentalGateResult.LockedOut;
            }

            return ParentalGateResult.Wrong;
        }
    }

    /// <summary>The on-screen gate. done(true) only when a grown-up answered; closing it is done(false).</summary>
    public interface IParentalGate
    {
        void Ask(string purpose, Action<bool> done);
    }

    /// <summary>Counts child purchases per UTC day for the optional child.iap_max_per_day cap. Storage-backed.</summary>
    public sealed class ChildPurchaseCounter
    {
        private const string Key = "ks.iapday";
        private readonly IKidSafetyStorage _storage;
        private readonly Func<DateTime> _now;

        public ChildPurchaseCounter(IKidSafetyStorage storage, Func<DateTime>? now = null)
        {
            _storage = storage;
            _now = now ?? (() => DateTime.UtcNow);
        }

        public int Today
        {
            get
            {
                var v = _storage.Get(Key);
                var parts = v?.Split(':');
                return parts != null && parts.Length == 2 && parts[0] == Day() && int.TryParse(parts[1], out var n) ? n : 0;
            }
        }

        public void Add()
        {
            _storage.Set(Key, Day() + ":" + (Today + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            _storage.Save();
        }

        private string Day() => _now().ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Plugs into the IAP branch's AgeBandPurchaseGate (Survival.Domain.Iap.IParentalConsent, PR #201), which asks it for
    /// every under-13 purchase. Reads child.iap from the policy at ask time: blocked declines, parental_gate runs the
    /// on-screen gate (and the daily cap if set), allowed lets it through. Unknown age never reaches here (#201 blocks it).
    /// </summary>
    public sealed class ParentalGateConsent : IParentalConsent
    {
        private readonly Func<ChildSafetyPolicy> _policy;
        private readonly IParentalGate _gate;
        private readonly ChildPurchaseCounter? _counter;

        public ParentalGateConsent(Func<ChildSafetyPolicy> policy, IParentalGate gate, ChildPurchaseCounter? counter = null)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _counter = counter;
        }

        /// <summary>Last decline reason (kids.iap_blocked, kids.iap_daily_cap, kids.gate_failed), for the UI.</summary>
        public string? LastReason { get; private set; }

        public void Request(string productId, Action<bool> granted)
        {
            var p = _policy();
            LastReason = null;
            switch (p.Iap)
            {
                case IapMode.Blocked:
                    LastReason = "kids.iap_blocked";
                    granted(false);
                    return;
                case IapMode.Allowed:
                    granted(true);
                    return;
            }

            if (_counter != null && p.Rules.IapMaxPerDay is { } cap && _counter.Today >= cap)
            {
                LastReason = "kids.iap_daily_cap";
                granted(false);
                return;
            }

            _gate.Ask("iap:" + productId, ok =>
            {
                if (ok)
                {
                    _counter?.Add();
                }
                else
                {
                    LastReason = "kids.gate_failed";
                }

                granted(ok);
            });
        }
    }

    /// <summary>What child mode hides so nothing pressures a child to buy (Families Ads &amp; Monetization).</summary>
    public static class NoPressure
    {
        /// <summary>"BEST VALUE" / "POPULAR" / "ONE TIME" style sales tags.</summary>
        public static bool ShowSalesTag(ChildSafetyPolicy p, string? tag) => !p.NoPressure && !string.IsNullOrEmpty(tag);

        /// <summary>Real-money deals inside a timed visit (Wandering Merchant) or a pop-up while blocked (temp builder).</summary>
        public static bool ShowTimedOrPromptedSku(ChildSafetyPolicy p) => !p.NoPressure;

        /// <summary>The "leaves in" countdown next to a timed deal list that includes a real-money deal.</summary>
        public static bool ShowOfferCountdown(ChildSafetyPolicy p) => !p.NoPressure;

        public static IReadOnlyList<string> Hidden(ChildSafetyPolicy p) => p.NoPressure
            ? new[] { "sales_tags", "timed_sku_offers", "prompted_sku_offers", "offer_countdowns" }
            : Array.Empty<string>();
    }

    /// <summary>
    /// Runs an <see cref="IPurchaseGate"/> in front of any purchase service. The live service from #201 already gates
    /// itself; this is for the sandbox service (Editor / Development builds) so child mode behaves the same in review.
    /// </summary>
    public sealed class GatedPurchaseService : ISkuPurchaseService
    {
        private readonly ISkuPurchaseService _inner;
        private readonly IPurchaseGate _gate;

        public GatedPurchaseService(ISkuPurchaseService inner, IPurchaseGate gate)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }

        public bool IsSandbox => _inner.IsSandbox;

        public void Purchase(string productId, Action<PurchaseOutcome> done) =>
            _gate.Check(productId, (ok, reason) =>
            {
                if (ok)
                {
                    _inner.Purchase(productId, done);
                }
                else
                {
                    done(new PurchaseOutcome(false, productId, "", reason ?? "iap.gated"));
                }
            });
    }
}
