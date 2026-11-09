#nullable enable
#if SURVIVAL_UNITY_IAP
using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Iap;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Survival.Unity.Iap
{
    /// <summary>
    /// Unity IAP 5 (com.unity.purchasing, Google Play Billing on Android, StoreKit 2 on iOS) behind
    /// <see cref="IPlatformStore"/>. It only moves orders between the store and <see cref="PurchaseService"/>: it never
    /// grants, and it confirms (consumes / acknowledges) an order only when PurchaseService says the server recorded it.
    /// </summary>
    public sealed class UnityIapStore : IPlatformStore
    {
        private StoreController? _store;
        private IReadOnlyList<IapProduct> _products = Array.Empty<IapProduct>();

        public IapStore Kind => Application.platform == RuntimePlatform.IPhonePlayer ? IapStore.AppStore : IapStore.GooglePlay;
        public bool IsReady { get; private set; }

        public event Action? Ready;
        public event Action<string>? Unavailable;
        public event Action<StoreTransaction>? PurchasePending;
        public event Action<string>? PurchaseDeferred;
        public event Action<string, string>? PurchaseFailed;

        public async void Connect(IReadOnlyList<IapProduct> products)
        {
            _products = products;
            try
            {
                var store = UnityIAPServices.StoreController();
                _store = store;

                // Subscribe before Connect so nothing that fires during connection is lost.
                store.OnStoreDisconnected += d =>
                {
                    IsReady = false;
                    Debug.LogWarning("[IAP] store disconnected: " + d.Message);
                    Unavailable?.Invoke("iap.disconnected");
                };
                store.OnProductsFetched += _ =>
                {
                    IsReady = true;
                    Ready?.Invoke();
                    store.FetchPurchases(); // re-delivers unconfirmed orders on OnPurchasePending
                };
                store.OnProductsFetchFailed += f =>
                {
                    Debug.LogWarning("[IAP] product fetch failed: " + f.FailureReason);
                    Unavailable?.Invoke("iap.products_unavailable");
                };
                store.OnPurchasePending += OnPending;
                store.OnPurchaseFailed += OnFailed;
                store.OnPurchaseDeferred += OnDeferred;
                store.OnPurchaseConfirmed += OnConfirmed;
                store.OnPurchasesFetched += OnFetched;
                store.OnPurchasesFetchFailed += f => Debug.LogWarning("[IAP] purchases fetch failed: " + f.Message);

                await store.Connect();
                store.FetchProducts(_products
                    .Select(p => new ProductDefinition(p.ProductId, p.Type == IapProductType.NonConsumable ? ProductType.NonConsumable : ProductType.Consumable))
                    .ToList());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                IsReady = false;
                Unavailable?.Invoke("iap.connect_failed");
            }
        }

        public void Purchase(string productId)
        {
            if (_store == null || !IsReady)
            {
                PurchaseFailed?.Invoke(productId, "iap.not_ready");
                return;
            }

            _store.PurchaseProduct(productId);
        }

        public void Confirm(StoreTransaction transaction)
        {
            // Confirmed orders re-sent for a server restore have no PendingOrder: nothing left to finish with the store.
            if (_store != null && transaction.Handle is PendingOrder order)
            {
                _store.ConfirmPurchase(order);
            }
        }

        public void Restore(Action<bool, string?> done)
        {
            if (_store == null)
            {
                done(false, "iap.not_ready");
                return;
            }

            _store.RestoreTransactions((ok, error) =>
            {
                _store.FetchPurchases();
                done(ok, error);
            });
        }

        public string? LocalizedPrice(string productId) => _store?.GetProductById(productId)?.metadata?.localizedPriceString;

        private void OnPending(PendingOrder order)
        {
            var tx = Transaction(order, order);
            if (tx != null)
            {
                PurchasePending?.Invoke(tx);
            }
        }

        /// <summary>
        /// Owned non-consumables the store already acknowledged: send each to the server again so a reinstall or a new
        /// device restores the unlock to this account (the server answers with entitlements only, no wallet grants).
        /// </summary>
        private void OnFetched(Orders orders)
        {
            foreach (var confirmed in orders.ConfirmedOrders)
            {
                var tx = Transaction(confirmed, null);
                if (tx != null && IsNonConsumable(tx.ProductId))
                {
                    PurchasePending?.Invoke(tx);
                }
            }

            foreach (var deferred in orders.DeferredOrders)
            {
                var id = ProductId(deferred);
                if (id != null)
                {
                    PurchaseDeferred?.Invoke(id);
                }
            }
        }

        private void OnFailed(FailedOrder order)
        {
            var reason = order.FailureReason switch
            {
                PurchaseFailureReason.UserCancelled => "iap.cancelled",
                PurchaseFailureReason.ExistingPurchasePending => "iap.pending",
                PurchaseFailureReason.ProductUnavailable => "iap.product_unavailable",
                PurchaseFailureReason.PurchasingUnavailable => "iap.not_ready",
                _ => "iap.store_error",
            };
            Debug.LogWarning("[IAP] purchase failed: " + order.FailureReason + " " + order.Details);
            PurchaseFailed?.Invoke(ProductId(order) ?? "", reason);
        }

        private void OnDeferred(DeferredOrder order)
        {
            var id = ProductId(order);
            if (id != null)
            {
                PurchaseDeferred?.Invoke(id);
            }
        }

        private static void OnConfirmed(Order order)
        {
            // A failed confirm leaves the order open; the store re-delivers it next launch and the applied-purchase
            // ledger stops a second grant.
            if (order is FailedOrder failed)
            {
                Debug.LogWarning("[IAP] confirm failed, will re-deliver: " + failed.FailureReason + " " + failed.Details);
            }
        }

        private bool IsNonConsumable(string productId)
        {
            foreach (var p in _products)
            {
                if (string.Equals(p.ProductId, productId, StringComparison.Ordinal))
                {
                    return p.Type == IapProductType.NonConsumable;
                }
            }

            return false;
        }

        private static string? ProductId(Order order) => order.CartOrdered?.Items().FirstOrDefault()?.Product?.definition?.id;

        private StoreTransaction? Transaction(Order order, object? handle)
        {
            var id = ProductId(order);
            if (id == null)
            {
                return null;
            }

            var info = order.Info;
            var google = info?.Google;
            var transactionId = info?.TransactionID ?? "";
            var token = Kind == IapStore.GooglePlay ? google?.PurchaseToken ?? transactionId : "";
            if (token.Length == 0 && transactionId.Length == 0)
            {
                return null;
            }

            return new StoreTransaction(Kind, id, transactionId, token, google?.OrderId, info?.Apple?.jwsRepresentation, handle);
        }
    }
}
#endif
