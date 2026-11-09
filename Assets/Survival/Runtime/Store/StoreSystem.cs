#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Economy;

namespace Survival.Domain.Store
{
    public static class OfferKinds
    {
        /// <summary>Real-money SKU through <see cref="ISkuPurchaseService"/>.</summary>
        public const string Sku = "sku";

        /// <summary>Paid with in-game currency (Price).</summary>
        public const string Currency = "currency";

        /// <summary>The free-gift slot, fed by the gift scheduler.</summary>
        public const string Free = "free";
    }

    public sealed record OfferDef(
        string Id,
        string Kind,
        string? Sku,
        IReadOnlyDictionary<string, int> Price,
        IReadOnlyDictionary<string, int> Grants,
        string? Tag);

    public sealed record StoreDef(string Id, IReadOnlyList<OfferDef> Offers);

    /// <summary>Theme-pack product row: sku.* → store product id + display price.</summary>
    public sealed record ProductDef(string Sku, string ProductId, string PriceLabel);

    /// <summary>
    /// Result of one real-money purchase. With a live store (<see cref="ISkuPurchaseService.IsSandbox"/> false) Ok is only
    /// ever true once the backend has validated the receipt (Verified), and Grants / Entitlements are exactly what the
    /// server's reply said; the client never grants from its own tables. Grants is empty when this purchase was already
    /// applied on this device (a re-delivered transaction).
    /// </summary>
    public sealed record PurchaseOutcome(
        bool Ok,
        string ProductId,
        string Receipt,
        string? Error,
        IReadOnlyDictionary<string, int>? Grants = null,
        IReadOnlyList<string>? Entitlements = null,
        bool Verified = false)
    {
        public static readonly IReadOnlyDictionary<string, int> NoGrants = new Dictionary<string, int>();

        /// <summary>
        /// What the wallet should receive for this outcome: the server's grants when verified, the local offer grants
        /// only for the sandbox service (dev builds, nothing charged), otherwise nothing.
        /// </summary>
        public IReadOnlyDictionary<string, int> WalletGrants(IReadOnlyDictionary<string, int> sandboxFallback, bool sandbox) =>
            !Ok ? NoGrants : Verified ? Grants ?? NoGrants : sandbox ? sandboxFallback : NoGrants;

        /// <summary>True when the outcome may unlock something: sandbox success, or a server-verified success.</summary>
        public bool Unlocks(bool sandbox) => Ok && (Verified || sandbox);
    }

    public interface ISkuPurchaseService
    {
        bool IsSandbox { get; }

        void Purchase(string productId, Action<PurchaseOutcome> done);
    }

    /// <summary>Sandbox only: every purchase succeeds immediately, nothing is charged.</summary>
    public sealed class SandboxPurchaseService : ISkuPurchaseService
    {
        private readonly List<string> _history = new();

        public bool IsSandbox => true;

        /// <summary>Set to make the next purchase fail (tests / cancel path).</summary>
        public bool FailNext { get; set; }

        public IReadOnlyList<string> History => _history;

        public void Purchase(string productId, Action<PurchaseOutcome> done)
        {
            if (FailNext)
            {
                FailNext = false;
                done(new PurchaseOutcome(false, productId, "", "sandbox.cancelled"));
                return;
            }

            _history.Add(productId);
            done(new PurchaseOutcome(true, productId, "sandbox-" + _history.Count, null));
        }
    }

    public sealed class StoreSystem
    {
        private readonly Dictionary<string, StoreDef> _stores = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ProductDef> _products;

        public StoreSystem(
            IReadOnlyList<StoreDef> stores,
            IReadOnlyDictionary<string, ProductDef> products,
            ISkuPurchaseService purchases,
            Wallet wallet)
        {
            foreach (var s in stores)
            {
                _stores[s.Id] = s;
            }

            _products = new Dictionary<string, ProductDef>(products, StringComparer.Ordinal);
            Purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public ISkuPurchaseService Purchases { get; }
        public Wallet Wallet { get; }
        public IEnumerable<StoreDef> Stores => _stores.Values;

        public StoreDef? Find(string storeId) => _stores.TryGetValue(storeId, out var s) ? s : null;

        public OfferDef? FindOffer(string offerId)
        {
            foreach (var s in _stores.Values)
            {
                foreach (var o in s.Offers)
                {
                    if (string.Equals(o.Id, offerId, StringComparison.Ordinal))
                    {
                        return o;
                    }
                }
            }

            return null;
        }

        public ProductDef? ProductFor(string? sku) =>
            sku != null && _products.TryGetValue(sku, out var p) ? p : null;

        public bool CanAfford(OfferDef offer) =>
            !string.Equals(offer.Kind, OfferKinds.Currency, StringComparison.Ordinal) || Wallet.CanAfford(offer.Price);

        /// <summary>Buy a currency-priced offer (e.g. 12 gems → 15 min speed-ups).</summary>
        public bool TryBuyWithCurrency(string offerId)
        {
            var offer = FindOffer(offerId);
            if (offer == null || !string.Equals(offer.Kind, OfferKinds.Currency, StringComparison.Ordinal))
            {
                return false;
            }

            if (!Wallet.TrySpend(offer.Price))
            {
                return false;
            }

            Wallet.Grant(offer.Grants);
            return true;
        }

        /// <summary>Buy a SKU offer through the purchase service; grants on success.</summary>
        public void BuySku(string offerId, Action<PurchaseOutcome> done)
        {
            var offer = FindOffer(offerId);
            var product = offer == null ? null : ProductFor(offer.Sku);
            if (offer == null || product == null || !string.Equals(offer.Kind, OfferKinds.Sku, StringComparison.Ordinal))
            {
                done(new PurchaseOutcome(false, product?.ProductId ?? "", "", "store.unknown_offer"));
                return;
            }

            Purchases.Purchase(product.ProductId, outcome =>
            {
                // Live store: only the server's grants (outcome.Grants). Sandbox: the local offer, as before.
                var sandbox = Purchases.IsSandbox;
                if (outcome.Ok && !outcome.Unlocks(sandbox))
                {
                    done(outcome with { Ok = false, Error = outcome.Error ?? "iap.unverified" });
                    return;
                }

                Wallet.Grant(outcome.WalletGrants(offer.Grants, sandbox));
                done(outcome);
            });
        }

        /// <summary>
        /// A server-verified purchase that arrived with no buy call waiting (an approved Ask-to-Buy / Play pending
        /// payment, or a transaction re-delivered after a crash). Credits the server's grants. Returns false when unverified.
        /// </summary>
        public bool ApplyServerGrant(PurchaseOutcome outcome)
        {
            if (!outcome.Ok || !outcome.Verified)
            {
                return false;
            }

            Wallet.Grant(outcome.Grants ?? PurchaseOutcome.NoGrants);
            return true;
        }
    }
}
