using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Survival.Domain.Alliance;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Iap;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.Taps;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Platform store double: records calls, the test decides what the store does.</summary>
internal sealed class FakePlatformStore : IPlatformStore
{
    public IapStore Kind => IapStore.GooglePlay;
    public bool IsReady { get; set; }
    public List<string> Log { get; } = new();
    public List<StoreTransaction> Confirmed { get; } = new();
    public IReadOnlyList<IapProduct> Products { get; private set; } = Array.Empty<IapProduct>();
    private int _n;

    public event Action? Ready;
    public event Action<string>? Unavailable;
    public event Action<StoreTransaction>? PurchasePending;
    public event Action<string>? PurchaseDeferred;
    public event Action<string, string>? PurchaseFailed;

    public void Connect(IReadOnlyList<IapProduct> products)
    {
        Products = products;
        IsReady = true;
        Ready?.Invoke();
    }

    public void Purchase(string productId) => Log.Add("buy " + productId);

    public void Confirm(StoreTransaction t)
    {
        Log.Add("confirm " + t.PurchaseToken);
        Confirmed.Add(t);
    }

    public void Restore(Action<bool, string?> done) => done(true, null);

    public string? LocalizedPrice(string productId) => "US$ 4.99";

    public StoreTransaction Pay(string productId, string? token = null)
    {
        var t = new StoreTransaction(IapStore.GooglePlay, productId, "tx" + (++_n), token ?? "tok" + _n, "GPA." + _n);
        PurchasePending?.Invoke(t);
        return t;
    }

    public void Defer(string productId) => PurchaseDeferred?.Invoke(productId);
    public void Fail(string productId, string reason) => PurchaseFailed?.Invoke(productId, reason);
    public void Drop() => Unavailable?.Invoke("iap.disconnected");
}

/// <summary>Server double: answers are queued per call, or held so the test can prove nothing happens before the server replies.</summary>
internal sealed class FakeVerifier : IReceiptVerifier
{
    public List<StoreTransaction> Seen { get; } = new();
    public Queue<VerifyResult> Answers { get; } = new();
    public List<Action<VerifyResult>> Held { get; } = new();
    public bool Hold { get; set; }

    public void Verify(StoreTransaction t, Action<VerifyResult> done)
    {
        Seen.Add(t);
        if (Hold)
        {
            Held.Add(done);
            return;
        }

        done(Answers.Count > 0 ? Answers.Dequeue() : Granted(("res.hard", 1)));
    }

    public static VerifyResult Granted(params (string id, int n)[] grants) =>
        new(VerifyStatus.Granted, grants.ToDictionary(g => g.id, g => g.n), Array.Empty<string>());
}

public sealed class IapPurchaseServiceTests
{
    private const string GemsM = "com.paytoplay.fantasykingdoma.hard_m";
    private const string Track = "com.paytoplay.fantasykingdoma.track_royal";

    private static (PurchaseService Svc, FakePlatformStore Store, FakeVerifier Server, MemoryAppliedPurchases Ledger) Make()
    {
        var store = new FakePlatformStore();
        var server = new FakeVerifier();
        var ledger = new MemoryAppliedPurchases();
        var products = IapRewardCatalogTests.Rewards().ProductsFor(Hook.Products().Values);
        var svc = new PurchaseService(store, server, ledger, products, AllowAllPurchaseGate.Instance);
        svc.Initialize();
        return (svc, store, server, ledger);
    }

    [Fact]
    public void Initializes_the_store_with_every_theme_pack_product_and_its_type()
    {
        var (_, store, _, _) = Make();
        Assert.Equal(Hook.Products().Count, store.Products.Count);
        Assert.Equal(IapProductType.Consumable, store.Products.Single(p => p.ProductId == GemsM).Type);
        Assert.Equal(IapProductType.NonConsumable, store.Products.Single(p => p.ProductId == Track).Type);
        Assert.All(store.Products, p => Assert.StartsWith("com.paytoplay.fantasykingdoma.", p.ProductId));
    }

    [Fact]
    public void Purchase_grants_exactly_the_server_reply_and_consumes_only_after_it()
    {
        var (svc, store, server, _) = Make();
        server.Hold = true;
        PurchaseOutcome? got = null;
        svc.Purchase(GemsM, o => got = o);
        store.Pay(GemsM);

        Assert.Null(got);
        Assert.Empty(store.Confirmed); // nothing consumed before the server confirms

        server.Held.Single()(FakeVerifier.Granted(("res.hard", 500)));
        Assert.NotNull(got);
        Assert.True(got!.Ok);
        Assert.True(got.Verified);
        Assert.Equal(500, got.Grants![SurvIds.ResHard]);
        Assert.Single(store.Confirmed);
        Assert.Equal(new[] { "buy " + GemsM, "confirm tok1" }, store.Log);
    }

    [Theory]
    [InlineData(VerifyStatus.Rejected, "iap.invalid_token")]
    [InlineData(VerifyStatus.Pending, "iap.pending")]
    [InlineData(VerifyStatus.Unavailable, "iap.offline")]
    public void Rejected_pending_or_offline_never_grant_and_never_consume(VerifyStatus status, string error)
    {
        var (svc, store, server, ledger) = Make();
        server.Answers.Enqueue(VerifyResult.Fail(status, error));
        PurchaseOutcome? got = null;
        svc.Purchase(GemsM, o => got = o);
        var t = store.Pay(GemsM);

        Assert.False(got!.Ok);
        Assert.Equal(error, got.Error);
        Assert.Null(got.Grants);
        Assert.Empty(store.Confirmed);
        Assert.False(ledger.Contains(t.Key));
    }

    [Fact]
    public void Offline_purchase_is_retried_and_granted_once_the_server_answers()
    {
        var (svc, store, server, _) = Make();
        server.Answers.Enqueue(VerifyResult.Fail(VerifyStatus.Unavailable, "iap.offline"));
        svc.Purchase(GemsM, _ => { });
        store.Pay(GemsM);
        Assert.Equal(1, svc.UnverifiedCount);

        PurchaseOutcome? late = null;
        svc.Unsolicited += o => late = o;
        server.Answers.Enqueue(FakeVerifier.Granted(("res.hard", 500)));
        svc.RetryUnverified();

        Assert.Equal(0, svc.UnverifiedCount);
        Assert.True(late!.Verified);
        Assert.Equal(500, late.Grants![SurvIds.ResHard]);
        Assert.Single(store.Confirmed);
    }

    [Fact]
    public void Deferred_and_cancelled_purchases_report_and_grant_nothing()
    {
        var (svc, store, server, _) = Make();
        string? deferredEvent = null;
        svc.Deferred += id => deferredEvent = id;
        PurchaseOutcome? a = null, b = null;
        svc.Purchase(GemsM, o => a = o);
        store.Defer(GemsM);
        svc.Purchase(GemsM, o => b = o);
        store.Fail(GemsM, "iap.cancelled");

        Assert.Equal("iap.deferred", a!.Error);
        Assert.Equal(GemsM, deferredEvent);
        Assert.Equal("iap.cancelled", b!.Error);
        Assert.Empty(server.Seen);
        Assert.Empty(store.Confirmed);
    }

    [Fact]
    public void A_redelivered_transaction_is_confirmed_but_never_granted_twice()
    {
        var (svc, store, _, _) = Make();
        var outcomes = new List<PurchaseOutcome>();
        svc.Unsolicited += outcomes.Add;
        store.Pay(GemsM, "same");
        store.Pay(GemsM, "same"); // the store re-delivers after a crash before confirm landed

        Assert.Equal(2, outcomes.Count);
        Assert.NotEmpty(outcomes[0].Grants!);
        Assert.Empty(outcomes[1].Grants!);
        Assert.Equal(2, store.Confirmed.Count);
    }

    [Fact]
    public void A_duplicate_delivery_while_the_server_is_checking_is_sent_once()
    {
        var (svc, store, server, _) = Make();
        server.Hold = true;
        store.Pay(GemsM, "dup");
        store.Pay(GemsM, "dup");
        Assert.Single(server.Seen);
    }

    [Fact]
    public void Approved_ask_to_buy_arrives_as_unsolicited_with_server_grants()
    {
        var (svc, store, server, _) = Make();
        PurchaseOutcome? late = null;
        svc.Unsolicited += o => late = o;
        server.Answers.Enqueue(new VerifyResult(VerifyStatus.Granted, new Dictionary<string, int>(), new[] { "ent.track_royal" }));
        store.Pay(Track);
        Assert.True(late!.Ok);
        Assert.Contains("ent.track_royal", late.Entitlements!);
        Assert.Contains("ent.track_royal", svc.Entitlements);
    }

    [Fact]
    public void Unknown_product_and_store_not_ready_fail_fast()
    {
        var (svc, store, _, _) = Make();
        PurchaseOutcome? a = null, b = null;
        svc.Purchase("com.other.x", o => a = o);
        store.IsReady = false;
        svc.Purchase(GemsM, o => b = o);
        Assert.Equal("store.unknown_offer", a!.Error);
        Assert.Equal("iap.not_ready", b!.Error);
        Assert.DoesNotContain(store.Log, l => l.StartsWith("buy", StringComparison.Ordinal));
        Assert.False(svc.IsSandbox);
    }

    [Fact]
    public void A_second_buy_of_the_same_product_while_one_is_open_is_refused()
    {
        var (svc, _, _, _) = Make();
        PurchaseOutcome? second = null;
        svc.Purchase(GemsM, _ => { });
        svc.Purchase(GemsM, o => second = o);
        Assert.Equal("iap.busy", second!.Error);
    }
}

public sealed class IapStoreGrantTests
{
    private const string GemsM = "com.paytoplay.fantasykingdoma.hard_m";

    /// <summary>A live service that says Ok with whatever the test wants (verified or not).</summary>
    private sealed class ScriptedLive : ISkuPurchaseService
    {
        public PurchaseOutcome? Next { get; set; }
        public bool IsSandbox => false;
        public void Purchase(string productId, Action<PurchaseOutcome> done) => done(Next! with { ProductId = productId });
    }

    private static StoreSystem Store(ISkuPurchaseService pay, Wallet wallet) => new(Hook.Catalog().Stores, Hook.Products(), pay, wallet);

    [Fact]
    public void Live_store_credits_the_server_grants_not_the_local_offer()
    {
        var wallet = new Wallet();
        var pay = new ScriptedLive { Next = new PurchaseOutcome(true, "", "k", null, new Dictionary<string, int> { [SurvIds.ResHard] = 7 }, null, Verified: true) };
        PurchaseOutcome? got = null;
        Store(pay, wallet).BuySku("offer.gems_m", o => got = o);
        Assert.True(got!.Ok);
        Assert.Equal(7, wallet.Get(SurvIds.ResHard)); // not the 500 in stores.json: the server decides
    }

    [Fact]
    public void Live_store_never_grants_an_unverified_success()
    {
        var wallet = new Wallet();
        var pay = new ScriptedLive { Next = new PurchaseOutcome(true, "", "k", null) };
        PurchaseOutcome? got = null;
        Store(pay, wallet).BuySku("offer.gems_m", o => got = o);
        Assert.False(got!.Ok);
        Assert.Equal("iap.unverified", got.Error);
        Assert.Equal(0, wallet.Get(SurvIds.ResHard));
    }

    [Fact]
    public void Sandbox_store_still_grants_the_local_offer()
    {
        var wallet = new Wallet();
        Store(new SandboxPurchaseService(), wallet).BuySku("offer.gems_m", _ => { });
        Assert.Equal(500, wallet.Get(SurvIds.ResHard));
    }

    [Fact]
    public void End_to_end_live_purchase_credits_wallet_once_from_server_reply()
    {
        var store = new FakePlatformStore();
        var server = new FakeVerifier();
        server.Answers.Enqueue(FakeVerifier.Granted((SurvIds.ResHard, 500)));
        var svc = new PurchaseService(store, server, new MemoryAppliedPurchases(), IapRewardCatalogTests.Rewards().ProductsFor(Hook.Products().Values), AllowAllPurchaseGate.Instance);
        svc.Initialize();
        var wallet = new Wallet();
        var shop = Store(svc, wallet);
        PurchaseOutcome? got = null;
        shop.BuySku("offer.gems_m", o => got = o);
        store.Pay(GemsM);
        Assert.True(got!.Ok);
        Assert.Equal(500, wallet.Get(SurvIds.ResHard));
        Assert.Single(store.Confirmed);
    }

    [Fact]
    public void Temp_builder_is_hired_only_on_a_verified_live_purchase()
    {
        var clock = new ManualClock();
        var rejected = new ScriptedLive { Next = new PurchaseOutcome(false, "", "", "iap.rejected") };
        var hook = new HookSession(Hook.Catalog(), new Wallet(), clock, Hook.Products(), rejected);
        hook.HireTempBuilder(_ => { });
        Assert.False(hook.Timers.ExtraSlotActive);

        var unverified = new ScriptedLive { Next = new PurchaseOutcome(true, "", "", null) };
        hook = new HookSession(Hook.Catalog(), new Wallet(), clock, Hook.Products(), unverified);
        PurchaseOutcome? got = null;
        hook.HireTempBuilder(o => got = o);
        Assert.False(hook.Timers.ExtraSlotActive);
        Assert.False(got!.Ok);

        var ok = new ScriptedLive { Next = new PurchaseOutcome(true, "", "", null, PurchaseOutcome.NoGrants, new[] { "ent.temp_builder" }, Verified: true) };
        hook = new HookSession(Hook.Catalog(), new Wallet(), clock, Hook.Products(), ok);
        hook.HireTempBuilder(_ => { });
        Assert.True(hook.Timers.ExtraSlotActive);
    }

    [Fact]
    public void Unsolicited_purchase_credits_server_grants_and_hires_the_builder()
    {
        var clock = new ManualClock();
        var wallet = new Wallet();
        var hook = new HookSession(Hook.Catalog(), wallet, clock, Hook.Products(), new ScriptedLive());
        var start = wallet.Get(SurvIds.ResHard);
        Assert.False(hook.ApplyUnsolicitedPurchase(new PurchaseOutcome(true, "x", "", null, new Dictionary<string, int> { [SurvIds.ResHard] = 9 })));
        Assert.Equal(start, wallet.Get(SurvIds.ResHard));

        Assert.True(hook.ApplyUnsolicitedPurchase(new PurchaseOutcome(true, "com.paytoplay.fantasykingdoma.power_t1", "", null, new Dictionary<string, int> { [SurvIds.ResHard] = 50 }, null, true)));
        Assert.Equal(start + 50, wallet.Get(SurvIds.ResHard));
        Assert.False(hook.Timers.ExtraSlotActive);

        Assert.True(hook.ApplyUnsolicitedPurchase(new PurchaseOutcome(true, hook.TempBuilderProduct!.ProductId, "", null, PurchaseOutcome.NoGrants, null, true)));
        Assert.True(hook.Timers.ExtraSlotActive);
    }
}

/// <summary>COPPA / Play Families: purchases are gated by the account's age band before they reach the store.</summary>
public sealed class IapPurchaseGateTests
{
    private const string GemsM = "com.paytoplay.fantasykingdoma.hard_m";

    private sealed class Consent : IParentalConsent
    {
        public bool Answer;
        public int Asked;

        public void Request(string productId, Action<bool> granted)
        {
            Asked++;
            granted(Answer);
        }
    }

    private static (PurchaseService Svc, FakePlatformStore Store) Make(IPurchaseGate? gate)
    {
        var store = new FakePlatformStore();
        var svc = new PurchaseService(store, new FakeVerifier(), new MemoryAppliedPurchases(), IapRewardCatalogTests.Rewards().ProductsFor(Hook.Products().Values), gate);
        svc.Initialize();
        return (svc, store);
    }

    private static string? Buy(PurchaseService svc)
    {
        string? error = "pending-store";
        svc.Purchase(GemsM, o => error = o.Error);
        return error;
    }

    [Fact]
    public void With_no_gate_given_every_purchase_is_blocked_because_the_age_is_unknown()
    {
        var (svc, store) = Make(null);
        Assert.Equal("iap.age_unknown", Buy(svc));
        Assert.Empty(store.Log);
    }

    [Fact]
    public void Thirteen_plus_buys_and_unknown_is_blocked_unless_explicitly_allowed()
    {
        var (adult, adultStore) = Make(new AgeBandPurchaseGate(() => AgeBand.ThirteenPlus));
        Assert.Equal("pending-store", Buy(adult)); // reached the store, waiting on payment
        Assert.Single(adultStore.Log);

        var (unknown, unknownStore) = Make(new AgeBandPurchaseGate(() => AgeBand.Unknown));
        Assert.Equal("iap.age_unknown", Buy(unknown));
        Assert.Empty(unknownStore.Log);

        var (allowed, allowedStore) = Make(new AgeBandPurchaseGate(() => AgeBand.Unknown, allowUnknown: true));
        Buy(allowed);
        Assert.Single(allowedStore.Log);
    }

    [Fact]
    public void Under_13_needs_parental_consent_and_the_stub_declines()
    {
        var (stub, stubStore) = Make(new AgeBandPurchaseGate(() => AgeBand.Under13));
        Assert.Equal("iap.consent_required", Buy(stub));
        Assert.Empty(stubStore.Log);

        var consent = new Consent { Answer = true };
        var (ok, okStore) = Make(new AgeBandPurchaseGate(() => AgeBand.Under13, consent));
        Buy(ok);
        Assert.Equal(1, consent.Asked);
        Assert.Single(okStore.Log);
    }

    [Fact]
    public void A_blocked_buy_does_not_leave_the_product_busy()
    {
        var band = AgeBand.Unknown;
        var (svc, store) = Make(new AgeBandPurchaseGate(() => band));
        Assert.Equal("iap.age_unknown", Buy(svc));
        band = AgeBand.ThirteenPlus;
        Assert.Equal("pending-store", Buy(svc));
        Assert.Single(store.Log);
    }

    [Fact]
    public void Already_paid_redeliveries_are_never_gated()
    {
        var (svc, store) = Make(new AgeBandPurchaseGate(() => AgeBand.Under13));
        PurchaseOutcome? late = null;
        svc.Unsolicited += o => late = o;
        store.Pay(GemsM);
        Assert.True(late!.Ok);
    }

    [Fact]
    public void Age_band_ids_round_trip_and_default_to_unknown()
    {
        foreach (var b in new[] { AgeBand.Unknown, AgeBand.Under13, AgeBand.ThirteenPlus })
        {
            Assert.Equal(b, AgeBands.Parse(AgeBands.Id(b)));
        }

        Assert.Equal(AgeBand.Unknown, AgeBands.Parse(null));
        Assert.Equal(AgeBand.Unknown, AgeBands.Parse("12"));
        Assert.Equal("13plus", AgeBands.Id(AgeBand.ThirteenPlus));
    }
}

public sealed class IapServerVerifierTests
{
    private sealed class Http : IAllianceTransport
    {
        public (string Method, string Path, string? Body, string? Token)? Last;
        public int Status = 200;
        public string Reply = "{}";

        public void Send(string method, string path, string? jsonBody, string? token, Action<int, string> done)
        {
            Last = (method, path, jsonBody, token);
            done(Status, Reply);
        }
    }

    private static readonly StoreTransaction Tx = new(IapStore.GooglePlay, "com.paytoplay.fantasykingdoma.hard_m", "t1", "tok\"1", "GPA.1");

    [Fact]
    public void Posts_the_store_token_with_the_shared_bearer_token()
    {
        var http = new Http { Reply = "{\"status\":\"granted\",\"grants\":{\"res.hard\":500},\"entitlements\":[]}" };
        VerifyResult? r = null;
        new ServerReceiptVerifier(http, () => "bearer").Verify(Tx, x => r = x);
        Assert.Equal(("POST", "/v1/iap/validate"), (http.Last!.Value.Method, http.Last.Value.Path));
        Assert.Equal("bearer", http.Last.Value.Token);
        var body = System.Text.Json.JsonDocument.Parse(http.Last.Value.Body!).RootElement;
        Assert.Equal("google_play", body.GetProperty("store").GetString());
        Assert.Equal("tok\"1", body.GetProperty("purchaseToken").GetString());
        Assert.Equal(VerifyStatus.Granted, r!.Status);
        Assert.Equal(500, r.Grants[SurvIds.ResHard]);
    }

    [Fact]
    public void Not_signed_in_is_retry_later_without_calling_the_server()
    {
        var http = new Http();
        VerifyResult? r = null;
        new ServerReceiptVerifier(http, () => null).Verify(Tx, x => r = x);
        Assert.Null(http.Last);
        Assert.Equal(VerifyStatus.Unavailable, r!.Status);
    }

    [Theory]
    [InlineData(200, "{\"status\":\"pending\"}", VerifyStatus.Pending)]
    [InlineData(200, "{\"status\":\"rejected\",\"reason\":\"invalid_token\"}", VerifyStatus.Rejected)]
    [InlineData(409, "{\"error\":\"owned_by_other_account\"}", VerifyStatus.Rejected)]
    [InlineData(400, "{\"error\":\"unknown_product\"}", VerifyStatus.Rejected)]
    [InlineData(0, "", VerifyStatus.Unavailable)]
    [InlineData(401, "{\"error\":\"unauthorized\"}", VerifyStatus.Unavailable)]
    [InlineData(429, "{\"error\":\"rate_limited\"}", VerifyStatus.Unavailable)]
    [InlineData(501, "{\"error\":\"app_store_not_enabled\"}", VerifyStatus.Unavailable)]
    [InlineData(503, "{\"error\":\"verify_unavailable\"}", VerifyStatus.Unavailable)]
    [InlineData(200, "not json", VerifyStatus.Unavailable)]
    public void Maps_server_replies(int status, string body, VerifyStatus expected)
    {
        var r = ServerReceiptVerifier.Map(status, body);
        Assert.Equal(expected, r.Status);
        Assert.Empty(r.Grants);
    }
}

public sealed class IapRewardCatalogTests
{
    internal static IapRewardCatalog Rewards() =>
        IapRewardCatalog.Parse(File.ReadAllText(Path.Combine(TestPaths.PackDir, "store", IapRewardCatalog.FileName)));

    [Fact]
    public void Every_product_has_exactly_one_reward_row()
    {
        var rewards = Rewards();
        var products = Hook.Products();
        Assert.Equal(products.Keys.OrderBy(k => k), rewards.Rewards.Select(r => r.Sku).OrderBy(k => k));
        Assert.Equal("com.paytoplay.fantasykingdoma", rewards.PackageName);
        Assert.All(products.Values, p => Assert.StartsWith(rewards.PackageName + ".", p.ProductId));
    }

    [Fact]
    public void Server_grants_equal_the_game_offers_for_each_sku()
    {
        var rewards = Rewards();
        var checkedSkus = new HashSet<string>();
        foreach (var offer in Hook.Catalog().Stores.SelectMany(s => s.Offers).Where(o => o.Kind == OfferKinds.Sku && o.Sku != null))
        {
            var r = rewards.Find(offer.Sku!);
            Assert.NotNull(r);
            Assert.Equal(offer.Grants.OrderBy(kv => kv.Key), r!.Grants.OrderBy(kv => kv.Key));
            checkedSkus.Add(offer.Sku!);
        }

        var bundle = MerchantConfig.Default.Deals.Single(d => d.Kind == MerchantDealKinds.Sku);
        Assert.Equal(bundle.Grants.OrderBy(kv => kv.Key), rewards.Find(MerchantConfig.BundleSku)!.Grants.OrderBy(kv => kv.Key));
        Assert.Contains(SurvIds.SkuPowerT1, checkedSkus);
    }

    [Fact]
    public void Unlock_products_grant_no_wallet_resources_and_carry_an_entitlement()
    {
        var rewards = Rewards();
        foreach (var sku in new[] { "sku.track_premium", "sku.track_royal", "sku.track_royal_upgrade", "sku.pass_season", "sku.temp_builder" })
        {
            var r = rewards.Find(sku)!;
            Assert.Empty(r.Grants);
            Assert.NotEmpty(r.Entitlements);
        }

        Assert.All(new[] { "sku.track_premium", "sku.track_royal", "sku.track_royal_upgrade" }, s => Assert.Equal(IapProductType.NonConsumable, rewards.Find(s)!.Type));
        Assert.Equal(IapProductType.Consumable, rewards.Find("sku.pass_season")!.Type); // a new banner each 28-day season
    }

    [Fact]
    public void Prices_are_marked_as_placeholders_pending_Derek()
    {
        var json = File.ReadAllText(Path.Combine(TestPaths.PackDir, "store", "products.json"));
        Assert.Contains("PLACEHOLDER", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_unknown_type_and_duplicate_sku()
    {
        Assert.Throws<FormatException>(() => IapRewardCatalog.Parse("{\"rewards\":[{\"sku\":\"sku.a\",\"type\":\"rental\"}]}"));
        Assert.Throws<FormatException>(() => IapRewardCatalog.Parse("{\"rewards\":[{\"sku\":\"sku.a\",\"type\":\"consumable\"},{\"sku\":\"sku.a\",\"type\":\"consumable\"}]}"));
    }
}
