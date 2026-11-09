using Survival.Domain.Catalog;

namespace Survival.Domain.Iap
{
    /// <summary>SKU class table only. Live billing is <see cref="PurchaseService"/> (Unity IAP + server validation, Backend/alliance /v1/iap/*).</summary>
    public sealed class IapSystem
    {
        public IapSystem(SurvivalCatalog catalog) => Skus = catalog.Skus;

        public System.Collections.Generic.IReadOnlyList<SkuDef> Skus { get; }
    }
}
