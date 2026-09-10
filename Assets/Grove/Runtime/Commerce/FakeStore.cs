using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grove.Domain.Commerce
{
    /// <summary>Dev / editor store. Always succeeds for known product ids; never talks to a real store.</summary>
    public sealed class FakeStore : IPurchaseService
    {
        private readonly List<StoreProduct> _products;
        private readonly List<PurchaseResult.Success> _history = new();

        public FakeStore(IEnumerable<StoreProduct>? products = null)
        {
            _products = (products ?? DefaultProducts()).ToList();
        }

        public IReadOnlyList<StoreProduct> Products => _products;

        public IReadOnlyList<PurchaseResult.Success> History => _history;

        public Task<PurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_products.All(p => p.ProductId != productId))
            {
                PurchaseResult failed = new PurchaseResult.Failed(productId, "unknown-product");
                return Task.FromResult(failed);
            }

            var success = new PurchaseResult.Success(productId, Guid.NewGuid().ToString("N"));
            _history.Add(success);
            return Task.FromResult<PurchaseResult>(success);
        }

        public static IEnumerable<StoreProduct> DefaultProducts()
        {
            return new[]
            {
                new StoreProduct("com.grove.remove_ads", "Remove Ads", "USD 2.99"),
                new StoreProduct("com.grove.starter_pack", "Starter Pack", "USD 4.99")
            };
        }
    }
}
