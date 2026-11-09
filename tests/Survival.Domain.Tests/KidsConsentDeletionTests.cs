using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

public sealed class KidsConsentDeletionTests
{
    private sealed class FixedConsent : IParentalConsentProvider
    {
        public FixedConsent(ConsentStatus s) => S = s;

        public ConsentStatus S { get; private set; }

        public string Method => "test";

        public ConsentStatus Status(ConsentPurpose purpose) => S;

        public void Request(ConsentPurpose purpose, Action<ConsentStatus> done) => done(S);

        public void Revoke() => S = ConsentStatus.NeedsConsent;
    }

    [Theory]
    [InlineData(ConsentPurpose.OnlineSignIn)]
    [InlineData(ConsentPurpose.CloudSave)]
    public void Default_provider_keeps_children_and_unknown_age_offline(ConsentPurpose purpose)
    {
        var c = KidsFiles.Shipped();
        Assert.Equal(ConsentDecision.NeedsConsent, ConsentGate.Decide(KidsFiles.Policy(c, "under13"), NoConsentProvider.Instance, purpose));
        Assert.Equal(ConsentDecision.NeedsConsent, ConsentGate.Decide(KidsFiles.Policy(c, null), NoConsentProvider.Instance, purpose));
        Assert.Equal(ConsentDecision.Allowed, ConsentGate.Decide(KidsFiles.Policy(c, "13plus"), NoConsentProvider.Instance, purpose));
    }

    [Fact]
    public void Default_provider_never_grants()
    {
        ConsentStatus? got = null;
        NoConsentProvider.Instance.Request(ConsentPurpose.OnlineSignIn, s => got = s);
        Assert.Equal(ConsentStatus.NeedsConsent, got);
        Assert.Equal("none", NoConsentProvider.Instance.Method);
    }

    [Theory]
    [InlineData(ConsentStatus.Granted, ConsentDecision.Allowed)]
    [InlineData(ConsentStatus.Pending, ConsentDecision.NeedsConsent)]
    [InlineData(ConsentStatus.Denied, ConsentDecision.NeedsConsent)]
    public void Only_granted_consent_lets_a_child_sign_in(ConsentStatus status, ConsentDecision expected)
    {
        var p = KidsFiles.Policy(KidsFiles.Shipped(), "under13");
        Assert.Equal(expected, ConsentGate.Decide(p, new FixedConsent(status), ConsentPurpose.OnlineSignIn));
    }

    [Fact]
    public void Blocked_mode_ignores_consent()
    {
        var json = File.ReadAllText(KidsFiles.ConfigPath).Replace("\"online_sign_in\": \"consent_required\"", "\"online_sign_in\": \"blocked\"");
        var p = KidsFiles.Policy(ChildSafetyConfig.Parse(json), "under13");
        Assert.Equal(ConsentDecision.Blocked, ConsentGate.Decide(p, new FixedConsent(ConsentStatus.Granted), ConsentPurpose.OnlineSignIn));
    }

    [Fact]
    public void Revoked_consent_closes_the_gate_again()
    {
        var p = KidsFiles.Policy(KidsFiles.Shipped(), "under13");
        var consent = new FixedConsent(ConsentStatus.Granted);
        Assert.True(ConsentGate.Allows(p, consent, ConsentPurpose.CloudSave));
        consent.Revoke();
        Assert.False(ConsentGate.Allows(p, consent, ConsentPurpose.CloudSave));
    }

    [Fact]
    public void Shipped_config_has_a_web_deletion_url()
    {
        var url = KidsFiles.Shipped().WebDeletionUrl;
        Assert.StartsWith("https://", url);
    }

    [Fact]
    public void Deletion_without_an_account_wipes_the_device_only()
    {
        var wiped = 0;
        var server = 0;
        DeletionResult? r = null;
        new AccountDeletion(() => false, d => { server++; d(true); }, () => wiped++).Run(x => r = x);
        Assert.Equal(DeletionResult.DeletedLocalOnly, r);
        Assert.Equal(1, wiped);
        Assert.Equal(0, server);
    }

    [Fact]
    public void Deletion_goes_server_first_then_device()
    {
        var order = new List<string>();
        DeletionResult? r = null;
        new AccountDeletion(() => true, d => { order.Add("server"); d(true); }, () => order.Add("device")).Run(x => r = x);
        Assert.Equal(DeletionResult.Deleted, r);
        Assert.Equal(new[] { "server", "device" }, order);
    }

    [Fact]
    public void Server_failure_keeps_the_device_data_for_a_retry()
    {
        var wiped = false;
        DeletionResult? r = null;
        new AccountDeletion(() => true, d => d(false), () => wiped = true).Run(x => r = x);
        Assert.Equal(DeletionResult.ServerFailed, r);
        Assert.False(wiped);
    }

    [Fact]
    public void Clearing_the_age_record_asks_the_gate_again()
    {
        var storage = new MemoryKidStorage();
        var store = new AgeGateStore(storage);
        var c = KidsFiles.Shipped();
        store.MarkShown();
        Assert.True(store.Save(c.Band("13plus")!));
        Assert.False(store.Load().NeedsGate);
        store.Clear();
        Assert.True(store.Load().NeedsGate);
    }
}
