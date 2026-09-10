using Grove.Domain.Commerce;

namespace Grove.Domain.Tests;

public sealed class FakeStoreTests
{
    [Fact]
    public async Task Known_product_succeeds_and_is_recorded()
    {
        var store = new FakeStore();
        var result = await store.PurchaseAsync("com.grove.remove_ads");
        var success = Assert.IsType<PurchaseResult.Success>(result);
        Assert.Equal("com.grove.remove_ads", success.ProductId);
        Assert.False(string.IsNullOrWhiteSpace(success.TransactionId));
        Assert.Single(store.History);
    }

    [Fact]
    public async Task Unknown_product_fails()
    {
        var store = new FakeStore();
        var result = await store.PurchaseAsync("com.grove.nope");
        var failed = Assert.IsType<PurchaseResult.Failed>(result);
        Assert.Equal("unknown-product", failed.Reason);
        Assert.Empty(store.History);
    }

    [Fact]
    public void Exposes_stub_catalog()
    {
        Assert.Equal(2, new FakeStore().Products.Count);
    }
}
