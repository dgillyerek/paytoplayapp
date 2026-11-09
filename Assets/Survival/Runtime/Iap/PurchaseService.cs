#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Store;

namespace Survival.Domain.Iap
{
    public enum IapStore
    {
        GooglePlay,
        AppStore
    }

    /// <summary>
    /// A paid transaction the platform store handed over for fulfilment (new, re-delivered after a crash, or approved
    /// after Ask-to-Buy / a Play pending payment). Handle is the platform's own order object; only the adapter reads it.
    /// </summary>
    public sealed record StoreTransaction(
        IapStore Store,
        string ProductId,
        string TransactionId,
        string PurchaseToken,
        string? OrderId = null,
        string? SignedTransaction = null,
        object? Handle = null)
    {
        /// <summary>Stable key for this purchase on this device: one Play purchase token or one App Store transaction.</summary>
        public string Key => (Store == IapStore.GooglePlay ? "gp:" : "as:") + (PurchaseToken.Length > 0 ? PurchaseToken : TransactionId);
    }

    /// <summary>
    /// The platform store (Google Play Billing today, StoreKit 2 later) behind one shape. The Unity IAP adapter in
    /// Survival.Iap.Unity implements it; tests use a fake. Nothing here talks to the backend.
    /// </summary>
    public interface IPlatformStore
    {
        IapStore Kind { get; }
        bool IsReady { get; }

        /// <summary>Connected and products fetched.</summary>
        event Action? Ready;

        /// <summary>Could not connect or fetch products (reason code).</summary>
        event Action<string>? Unavailable;

        /// <summary>Paid and waiting to be fulfilled, then confirmed with <see cref="Confirm"/>.</summary>
        event Action<StoreTransaction>? PurchasePending;

        /// <summary>Waiting on payment or approval (Ask to Buy, Play "pending" cash payment). Nothing is paid yet.</summary>
        event Action<string>? PurchaseDeferred;

        /// <summary>Purchase did not go through: product id and a reason code (iap.cancelled, iap.store_error, ...).</summary>
        event Action<string, string>? PurchaseFailed;

        void Connect(IReadOnlyList<IapProduct> products);

        void Purchase(string productId);

        /// <summary>Finish the transaction with the store: consumes a consumable, acknowledges a non-consumable.</summary>
        void Confirm(StoreTransaction transaction);

        /// <summary>Ask the store to re-deliver owned purchases (they arrive on <see cref="PurchasePending"/> or are re-verified).</summary>
        void Restore(Action<bool, string?> done);

        /// <summary>The store's localized price string, or null before products are fetched.</summary>
        string? LocalizedPrice(string productId);
    }

    public enum VerifyStatus
    {
        /// <summary>Receipt valid and recorded; Grants / Entitlements are the server's.</summary>
        Granted,

        /// <summary>Store says the payment is still pending. Do not confirm; it re-delivers when it completes.</summary>
        Pending,

        /// <summary>Receipt invalid, cancelled, refunded, or owned by another account. Never grant.</summary>
        Rejected,

        /// <summary>Could not reach the server (offline, 5xx, not signed in). Keep the transaction open and retry.</summary>
        Unavailable
    }

    public sealed record VerifyResult(
        VerifyStatus Status,
        IReadOnlyDictionary<string, int> Grants,
        IReadOnlyList<string> Entitlements,
        string? Error = null)
    {
        public static VerifyResult Fail(VerifyStatus status, string error) =>
            new(status, PurchaseOutcome.NoGrants, Array.Empty<string>(), error);
    }

    /// <summary>The backend's receipt check (Backend/alliance POST /v1/iap/validate).</summary>
    public interface IReceiptVerifier
    {
        void Verify(StoreTransaction transaction, Action<VerifyResult> done);
    }

    /// <summary>Device-local ledger of purchase keys whose grants were already credited, so a re-delivery never double-grants.</summary>
    public interface IAppliedPurchases
    {
        bool Contains(string key);

        void Add(string key);
    }

    public sealed class MemoryAppliedPurchases : IAppliedPurchases
    {
        private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

        public bool Contains(string key) => _keys.Contains(key);

        public void Add(string key) => _keys.Add(key);
    }

    /// <summary>
    /// Real-money purchases through a platform store with server-side receipt validation.
    /// Flow: store pays → <see cref="IReceiptVerifier"/> validates with the backend → the buy callback (or
    /// <see cref="Unsolicited"/>) gets the server's grants → the key is recorded in the local ledger → only then is the
    /// transaction confirmed (consumed) with the store. Pending / deferred / rejected / offline never confirm and never
    /// grant. The client never decides what a purchase is worth.
    /// </summary>
    public sealed class PurchaseService : ISkuPurchaseService
    {
        private readonly IPlatformStore _store;
        private readonly IReceiptVerifier _verifier;
        private readonly IAppliedPurchases _applied;
        private readonly IPurchaseGate _gate;
        private readonly Dictionary<string, IapProduct> _byProductId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<PurchaseOutcome>> _waiting = new(StringComparer.Ordinal);
        private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StoreTransaction> _retry = new(StringComparer.Ordinal);
        private readonly HashSet<string> _entitlements = new(StringComparer.Ordinal);
        private bool _initialized;

        /// <param name="gate">Age / consent check before each new purchase. Null = <see cref="AgeBandPurchaseGate.Default"/> (blocks).</param>
        public PurchaseService(IPlatformStore store, IReceiptVerifier verifier, IAppliedPurchases applied, IEnumerable<IapProduct> products, IPurchaseGate? gate = null)
        {
            _gate = gate ?? AgeBandPurchaseGate.Default();
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
            _applied = applied ?? throw new ArgumentNullException(nameof(applied));
            foreach (var p in products)
            {
                _byProductId[p.ProductId] = p;
            }
        }

        public bool IsSandbox => false;
        public bool IsReady => _store.IsReady;
        public IReadOnlyCollection<IapProduct> Products => _byProductId.Values;

        /// <summary>Entitlements the server has returned this session (track / crusade unlocks, temp builder).</summary>
        public IReadOnlyCollection<string> Entitlements => _entitlements;

        /// <summary>Transactions still open because the server could not be reached; <see cref="RetryUnverified"/> resends them.</summary>
        public int UnverifiedCount => _retry.Count;

        /// <summary>A verified purchase with no buy call waiting for it. Credit outcome.Grants (StoreSystem.ApplyServerGrant).</summary>
        public event Action<PurchaseOutcome>? Unsolicited;

        /// <summary>A purchase went to "waiting for payment / approval" (product id).</summary>
        public event Action<string>? Deferred;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _store.PurchasePending += OnPending;
            _store.PurchaseDeferred += OnDeferred;
            _store.PurchaseFailed += OnFailed;
            _store.Connect(new List<IapProduct>(_byProductId.Values));
        }

        public string? LocalizedPrice(string productId) => _store.LocalizedPrice(productId);

        public void Purchase(string productId, Action<PurchaseOutcome> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (!_byProductId.ContainsKey(productId))
            {
                done(Fail(productId, "store.unknown_offer"));
                return;
            }

            if (!_initialized || !_store.IsReady)
            {
                done(Fail(productId, "iap.not_ready"));
                return;
            }

            if (_waiting.ContainsKey(productId))
            {
                done(Fail(productId, "iap.busy"));
                return;
            }

            _waiting[productId] = done;
            _gate.Check(productId, (allowed, reason) =>
            {
                if (!allowed)
                {
                    _waiting.Remove(productId);
                    done(Fail(productId, reason ?? "iap.blocked"));
                    return;
                }

                _store.Purchase(productId);
            });
        }

        /// <summary>Re-deliver owned purchases from the store; each one is validated by the server like a new purchase.</summary>
        public void Restore(Action<bool, string?> done) => _store.Restore(done);

        /// <summary>Resend transactions that could not be validated (call when the network comes back).</summary>
        public void RetryUnverified()
        {
            var open = new List<StoreTransaction>(_retry.Values);
            _retry.Clear();
            foreach (var tx in open)
            {
                OnPending(tx);
            }
        }

        private void OnPending(StoreTransaction tx)
        {
            if (!_inFlight.Add(tx.Key))
            {
                return; // the store re-delivered a transaction we are already validating
            }

            _verifier.Verify(tx, result =>
            {
                _inFlight.Remove(tx.Key);
                OnVerified(tx, result);
            });
        }

        private void OnVerified(StoreTransaction tx, VerifyResult result)
        {
            switch (result.Status)
            {
                case VerifyStatus.Granted:
                {
                    _retry.Remove(tx.Key);
                    foreach (var e in result.Entitlements)
                    {
                        _entitlements.Add(e);
                    }

                    var fresh = !_applied.Contains(tx.Key);
                    var outcome = new PurchaseOutcome(
                        true,
                        tx.ProductId,
                        tx.Key,
                        null,
                        fresh ? result.Grants : PurchaseOutcome.NoGrants,
                        result.Entitlements,
                        Verified: true);
                    try
                    {
                        Deliver(tx.ProductId, outcome, unsolicitedIfNoneWaiting: true);
                    }
                    finally
                    {
                        if (fresh)
                        {
                            _applied.Add(tx.Key);
                        }

                        // Consume / acknowledge only now: the server has recorded it and the grant is credited.
                        _store.Confirm(tx);
                    }

                    break;
                }

                case VerifyStatus.Pending:
                    Deliver(tx.ProductId, Fail(tx.ProductId, "iap.pending"), unsolicitedIfNoneWaiting: false);
                    break;

                case VerifyStatus.Rejected:
                    Deliver(tx.ProductId, Fail(tx.ProductId, result.Error ?? "iap.rejected"), unsolicitedIfNoneWaiting: false);
                    break;

                default:
                    _retry[tx.Key] = tx;
                    Deliver(tx.ProductId, Fail(tx.ProductId, result.Error ?? "iap.verify_unavailable"), unsolicitedIfNoneWaiting: false);
                    break;
            }
        }

        private void OnDeferred(string productId)
        {
            Deferred?.Invoke(productId);
            Deliver(productId, Fail(productId, "iap.deferred"), unsolicitedIfNoneWaiting: false);
        }

        private void OnFailed(string productId, string reason) =>
            Deliver(productId, Fail(productId, string.IsNullOrEmpty(reason) ? "iap.store_error" : reason), unsolicitedIfNoneWaiting: false);

        private void Deliver(string productId, PurchaseOutcome outcome, bool unsolicitedIfNoneWaiting)
        {
            if (_waiting.TryGetValue(productId, out var done))
            {
                _waiting.Remove(productId);
                done(outcome);
                return;
            }

            if (unsolicitedIfNoneWaiting)
            {
                Unsolicited?.Invoke(outcome);
            }
        }

        private static PurchaseOutcome Fail(string productId, string error) => new(false, productId, "", error);
    }
}
