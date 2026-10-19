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

    public sealed record PurchaseOutcome(bool Ok, string ProductId, string Receipt, string? Error);

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
                if (outcome.Ok)
                {
                    Wallet.Grant(offer.Grants);
                }

                done(outcome);
            });
        }
    }
}
