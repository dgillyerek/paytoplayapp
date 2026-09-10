using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grove.Domain.Commerce
{
    public sealed record StoreProduct(string ProductId, string DisplayName, string PriceLabel);

    public abstract record PurchaseResult
    {
        private PurchaseResult()
        {
        }

        public sealed record Success(string ProductId, string TransactionId) : PurchaseResult;

        public sealed record Failed(string ProductId, string Reason) : PurchaseResult;
    }

    public interface IPurchaseService
    {
        IReadOnlyList<StoreProduct> Products { get; }

        Task<PurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default);
    }
}
