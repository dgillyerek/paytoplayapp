using Survival.Domain.Analytics;
using Survival.Domain.Iap;
using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

/// <summary>The audience switch (child_safety.json audience_mode): 13+ soft launch by default, Families kept intact.</summary>
public sealed class KidsAudienceModeTests
{
    private static readonly ChildSafetyConfig Teen = KidsFiles.Shipped();
    private static readonly ChildSafetyConfig Fam = KidsFiles.Families(KidsFiles.Shipped());

    private static ChildSafetyPolicy P(ChildSafetyConfig c, AgeGateStatus s, string? band = null) => new(c, new AgeGateState(s, band));

    private sealed class CountingConsent : IParentalConsent
    {
        public int Asked;

        public void Request(string productId, Action<bool> granted)
        {
            Asked++;
            granted(true);
        }
    }

    private sealed class GrantedProvider : IParentalConsentProvider
    {
        public string Method => "test";

        public ConsentStatus Status(ConsentPurpose purpose) => ConsentStatus.Granted;

        public void Request(ConsentPurpose purpose, Action<ConsentStatus> done) => done(ConsentStatus.Granted);

        public void Revoke()
        {
        }
    }

    private sealed class Rec : IAnalytics
    {
        public List<string> Events { get; } = new();

        public bool Enabled => true;

        public void Track(string eventName, IReadOnlyDictionary<string, string>? props = null) => Events.Add(eventName);
    }

    [Fact]
    public void Shipped_and_fallback_configs_are_13_plus_and_families_parses()
    {
        Assert.Equal(AudienceMode.TeenPlus, Teen.Audience);
        Assert.Equal(AudienceMode.TeenPlus, ChildSafetyConfig.Default.Audience);
        Assert.Equal(AudienceMode.TeenPlus, ChildSafetyConfig.Parse("{}").Audience);
        Assert.Equal(AudienceMode.Families, ChildSafetyConfig.Parse("{\"audience_mode\":\"families\"}").Audience);
        Assert.Equal(AudienceMode.TeenPlus, ChildSafetyConfig.Parse("{\"audience_mode\":\"teen_plus\"}").Audience);
        Assert.Throws<FormatException>(() => ChildSafetyConfig.Parse("{\"audience_mode\":\"kids\"}"));
    }

    [Fact]
    public void Teen_plus_13_plus_plays_buys_chats_freely_and_can_sign_in()
    {
        var p = P(Teen, AgeGateStatus.Answered, "13plus");
        Assert.False(p.IsBlocked);
        Assert.False(p.IsChild);
        Assert.Equal(ChatMode.Free, p.Chat);
        Assert.Equal(IapMode.Allowed, p.Iap);
        Assert.Equal(AgeBand.ThirteenPlus, p.AgeBand);
        Assert.True(ConsentGate.Allows(p, NoConsentProvider.Instance, ConsentPurpose.OnlineSignIn));
        Assert.True(ConsentGate.Allows(p, NoConsentProvider.Instance, ConsentPurpose.CloudSave));
        Assert.True(p.CanCreateAlliance);
        Assert.False(p.NoPressure);
        Assert.Equal(TelemetryMode.Full, p.Rules.Analytics);
        Assert.Equal(TelemetryMode.Off, p.Analytics); // no vendor picked yet: nothing is sent
    }

    [Theory]
    [InlineData(AgeGateStatus.Answered, "under13")]
    [InlineData(AgeGateStatus.Answered, "a_band_removed_from_config")]
    [InlineData(AgeGateStatus.Tampered, null)]
    public void Teen_plus_blocks_under_13_and_tampered_answers_and_shuts_every_data_path(AgeGateStatus status, string? band)
    {
        var p = P(Teen, status, band);
        Assert.True(p.IsBlocked);
        Assert.Equal(ChatMode.Off, p.Chat);
        Assert.Equal(ChatMode.Off, p.ChatMax);
        Assert.Equal(IapMode.Blocked, p.Iap);
        Assert.Equal(TelemetryMode.Off, p.Rules.Analytics);
        Assert.Equal(TelemetryMode.Off, p.Rules.CrashReporting);
        Assert.False(p.CanCreateAlliance);
        // Even a granted consent provider cannot open sign-in or cloud save in 13+ mode.
        Assert.Equal(ConsentDecision.Blocked, ConsentGate.Decide(p, new GrantedProvider(), ConsentPurpose.OnlineSignIn));
        Assert.Equal(ConsentDecision.Blocked, ConsentGate.Decide(p, new GrantedProvider(), ConsentPurpose.CloudSave));
    }

    [Fact]
    public void Teen_plus_before_the_answer_is_not_blocked_but_nothing_goes_online()
    {
        var p = P(Teen, AgeGateStatus.Fresh);
        Assert.False(p.IsBlocked);
        Assert.True(p.IsChild);
        Assert.False(p.ConsentFlowEnabled);
        Assert.Equal(ConsentDecision.Blocked, ConsentGate.Decide(p, new GrantedProvider(), ConsentPurpose.OnlineSignIn));
        var s = new PrivacySettings(p, new GrantedProvider());
        Assert.Equal(ConsentView.Unavailable, s.Consent);
        Assert.False(s.CanRequestConsent);
        Assert.False(s.ShowConsent);
    }

    [Fact]
    public void Families_keeps_child_mode_and_the_consent_flow()
    {
        var p = P(Fam, AgeGateStatus.Answered, "under13");
        Assert.False(p.IsBlocked);
        Assert.True(p.IsChild);
        Assert.True(p.ConsentFlowEnabled);
        Assert.Equal(ConsentDecision.NeedsConsent, ConsentGate.Decide(p, NoConsentProvider.Instance, ConsentPurpose.OnlineSignIn));
        Assert.Equal(ConsentDecision.Allowed, ConsentGate.Decide(p, new GrantedProvider(), ConsentPurpose.OnlineSignIn));
        Assert.True(new PrivacySettings(p, NoConsentProvider.Instance).ShowConsent);
        Assert.False(new PrivacySettings(p, NoConsentProvider.Instance).CanCorrectAge);
        Assert.False(P(Fam, AgeGateStatus.Tampered).IsBlocked);
        Assert.Equal(IapMode.ParentalGate, p.Iap);
    }

    [Fact]
    public void Purchase_gate_13_plus_buys_in_both_modes_and_under_13_is_declined_without_consent_in_teen_plus()
    {
        foreach (var flow in new[] { true, false })
        {
            string? reason = "x";
            var ok = false;
            new AgeBandPurchaseGate(() => AgeBand.ThirteenPlus, new CountingConsent(), consentFlow: () => flow).Check("gems_small", (a, r) => (ok, reason) = (a, r));
            Assert.True(ok);
            Assert.Null(reason);
        }

        var consent = new CountingConsent();
        string? why = null;
        var allowed = true;
        new AgeBandPurchaseGate(() => AgeBand.Under13, consent, consentFlow: () => false).Check("gems_small", (a, r) => (allowed, why) = (a, r));
        Assert.False(allowed);
        Assert.Equal("iap.audience_13plus", why);
        Assert.Equal(0, consent.Asked);

        new AgeBandPurchaseGate(() => AgeBand.Under13, consent).Check("gems_small", (a, r) => (allowed, why) = (a, r));
        Assert.True(allowed);
        Assert.Equal(1, consent.Asked); // Families default: the consent step runs
    }

    [Fact]
    public void The_policy_band_for_a_verified_13_plus_player_is_not_unknown()
    {
        var store = new AgeGateStore(new MemoryKidStorage());
        store.MarkShown();
        Assert.True(store.Save(Teen.BandForAge(20)));
        var p = new ChildSafetyPolicy(Teen, store.Load());
        Assert.Equal(AgeBand.ThirteenPlus, p.AgeBand);
        var ok = false;
        new AgeBandPurchaseGate(() => p.AgeBand, consentFlow: () => p.ConsentFlowEnabled).Check("gems_small", (a, _) => ok = a);
        Assert.True(ok);
    }

    [Fact]
    public void Age_can_be_lowered_to_under_13_but_never_raised_and_the_answer_cannot_be_retried()
    {
        var store = new AgeGateStore(new MemoryKidStorage());
        Assert.True(store.Save(Teen.BandForAge(15)));
        Assert.False(store.Save(Teen.BandForAge(10))); // a second Save is refused: no retry in or across sessions
        Assert.False(store.Lower(Teen.BandForAge(30), Teen)); // not a child band
        Assert.True(store.Lower(Teen.BandForAge(10), Teen));
        Assert.True(P(Teen, store.Load().Status, store.Load().BandId).IsBlocked);
        Assert.False(store.Lower(Teen.BandForAge(9), Teen)); // already a child
        Assert.False(store.Save(Teen.BandForAge(30)));
        Assert.Equal("under13", store.Load().BandId);
    }

    [Fact]
    public void Under_13_block_deletes_server_then_device_and_keeps_only_the_block_record()
    {
        var log = new List<string>();
        AgeBandDef? kept = null;
        AudienceBlockResult? result = null;
        var deletion = new AccountDeletion(() => true, done => { log.Add("server"); done(true); }, () => log.Add("wipe"));
        AudienceBlock.Enforce(P(Teen, AgeGateStatus.Answered, "under13"), deletion, b => { log.Add("keep"); kept = b; }, r => result = r);
        Assert.Equal(AudienceBlockResult.Deleted, result);
        Assert.Equal(new[] { "server", "wipe", "keep" }, log);
        Assert.True(kept!.Child);
    }

    [Fact]
    public void Under_13_block_wipes_nothing_when_the_server_fails_and_never_runs_outside_a_block()
    {
        var log = new List<string>();
        AudienceBlockResult? result = null;
        var failing = new AccountDeletion(() => true, done => done(false), () => log.Add("wipe"));
        AudienceBlock.Enforce(P(Teen, AgeGateStatus.Answered, "under13"), failing, _ => log.Add("keep"), r => result = r);
        Assert.Equal(AudienceBlockResult.ServerFailed, result);
        Assert.Empty(log);

        var localOnly = new AccountDeletion(() => false, done => done(false), () => log.Add("wipe"));
        AudienceBlock.Enforce(P(Teen, AgeGateStatus.Answered, "under13"), localOnly, _ => log.Add("keep"), r => result = r);
        Assert.Equal(AudienceBlockResult.Deleted, result);
        Assert.Equal(new[] { "wipe", "keep" }, log);

        log.Clear();
        AudienceBlock.Enforce(P(Teen, AgeGateStatus.Tampered), localOnly, _ => log.Add("keep"), r => result = r);
        Assert.Equal(AudienceBlockResult.BlockedOnly, result);
        AudienceBlock.Enforce(P(Fam, AgeGateStatus.Answered, "under13"), localOnly, _ => log.Add("keep"), r => result = r);
        Assert.Equal(AudienceBlockResult.NotBlocked, result);
        AudienceBlock.Enforce(P(Teen, AgeGateStatus.Answered, "13plus"), localOnly, _ => log.Add("keep"), r => result = r);
        Assert.Equal(AudienceBlockResult.NotBlocked, result);
        Assert.Empty(log);
    }

    [Fact]
    public void Analytics_runs_for_13_plus_once_a_vendor_is_set_and_never_for_blocked_or_unknown()
    {
        var withVendor = Teen with { AnalyticsVendor = "some_vendor", CrashVendor = "some_vendor" };
        var adult = P(withVendor, AgeGateStatus.Answered, "13plus");
        var rec = new Rec();
        new KidGatedAnalytics(rec, () => adult.AgeBand, () => adult.Analytics).Track(FunnelEvents.FirstOpen);
        Assert.Equal(new[] { FunnelEvents.FirstOpen }, rec.Events);

        foreach (var p in new[] { P(withVendor, AgeGateStatus.Answered, "under13"), P(withVendor, AgeGateStatus.Fresh), P(KidsFiles.Families(withVendor), AgeGateStatus.Answered, "under13") })
        {
            var r = new Rec();
            var a = new KidGatedAnalytics(r, () => p.AgeBand, () => p.Analytics);
            a.Track(FunnelEvents.FirstOpen);
            Assert.False(a.Enabled);
            Assert.Empty(r.Events);
        }

        // Shipped: vendor "none", so even 13+ sends nothing.
        var shipped = P(Teen, AgeGateStatus.Answered, "13plus");
        Assert.False(new KidGatedAnalytics(new Rec(), () => shipped.AgeBand, () => shipped.Analytics).Enabled);
    }

    [Fact]
    public void Teen_plus_settings_offer_an_age_correction_and_hide_consent()
    {
        var s = new PrivacySettings(P(Teen, AgeGateStatus.Answered, "13plus"), NoConsentProvider.Instance);
        Assert.True(s.CanCorrectAge);
        Assert.False(s.ShowConsent);
        Assert.Equal(new[] { ChatMode.Off, ChatMode.Presets, ChatMode.Free }, s.ChatChoices);
        Assert.False(s.DeleteNeedsParent);
        Assert.False(s.LinkNeedsParent);
    }
}
