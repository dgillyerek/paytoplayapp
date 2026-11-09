using Survival.Domain.Iap;
using Survival.Domain.Kids;
using Survival.Domain.Store;

namespace Survival.Domain.Tests;

public sealed class KidsChatIapTests
{
    private static ChildSafetyPolicy Child(string json = "{}") => KidsFiles.Policy(ChildSafetyConfig.Parse(json), "under13");

    private static ChildSafetyPolicy Adult => KidsFiles.Policy(KidsFiles.Shipped(), "13plus");

    private sealed class FakeGate : IParentalGate
    {
        public bool Answer { get; set; }
        public int Asked { get; private set; }

        public void Ask(string purpose, Action<bool> done)
        {
            Asked++;
            done(Answer);
        }
    }

    // ---- parental gate ----

    [Fact]
    public void The_gate_passes_on_the_right_product_and_rotates_the_question_on_a_miss()
    {
        var s = new ParentalGateSession(3, 60, () => DateTime.UnixEpoch, new Random(1));
        var first = s.Challenge;
        Assert.InRange(first.A, 6, 12);
        Assert.InRange(first.B, 6, 12);
        Assert.Equal(ParentalGateResult.Wrong, s.Submit(first.Answer + 1));
        Assert.Equal(ParentalGateResult.Passed, s.Submit(s.Challenge.Answer));
        Assert.Equal(ParentalGateResult.Wrong, s.Submit(null));
    }

    [Fact]
    public void Too_many_misses_lock_the_gate_until_the_lockout_ends()
    {
        var now = DateTime.UnixEpoch;
        var s = new ParentalGateSession(3, 60, () => now, new Random(2));
        s.Submit(-1);
        s.Submit(-1);
        Assert.Equal(ParentalGateResult.LockedOut, s.Submit(-1));
        Assert.Equal(ParentalGateResult.LockedOut, s.Submit(s.Challenge.Answer));
        now = now.AddSeconds(61);
        Assert.Equal(ParentalGateResult.Passed, s.Submit(s.Challenge.Answer));
    }

    // ---- purchase consent (plugs into #201 AgeBandPurchaseGate) ----

    private static bool? Ask(IPurchaseGate gate)
    {
        bool? result = null;
        gate.Check("com.paytoplay.fantasykingdoma.gems_m", (ok, _) => result = ok);
        return result;
    }

    [Fact]
    public void Under13_purchase_needs_the_grown_up_by_default()
    {
        var fake = new FakeGate { Answer = false };
        var consent = new ParentalGateConsent(() => Child(), fake);
        var gate = new AgeBandPurchaseGate(() => AgeBand.Under13, consent);
        Assert.False(Ask(gate));
        Assert.Equal("kids.gate_failed", consent.LastReason);
        fake.Answer = true;
        Assert.True(Ask(gate));
        Assert.Equal(2, fake.Asked);
    }

    [Fact]
    public void Blocked_child_iap_never_shows_the_gate()
    {
        var fake = new FakeGate { Answer = true };
        var consent = new ParentalGateConsent(() => Child("{\"child\":{\"iap\":\"blocked\"}}"), fake);
        Assert.False(Ask(new AgeBandPurchaseGate(() => AgeBand.Under13, consent)));
        Assert.Equal(0, fake.Asked);
        Assert.Equal("kids.iap_blocked", consent.LastReason);
    }

    [Fact]
    public void The_daily_cap_stops_purchases_after_the_limit_and_resets_next_day()
    {
        var now = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);
        var counter = new ChildPurchaseCounter(new MemoryKidStorage(), () => now);
        var fake = new FakeGate { Answer = true };
        var gate = new AgeBandPurchaseGate(() => AgeBand.Under13, new ParentalGateConsent(() => Child("{\"child\":{\"iap_max_per_day\":1}}"), fake, counter));
        Assert.True(Ask(gate));
        Assert.False(Ask(gate));
        now = now.AddDays(1);
        Assert.True(Ask(gate));
    }

    [Fact]
    public void Unknown_age_is_blocked_by_201_and_13plus_skips_the_gate()
    {
        var fake = new FakeGate { Answer = true };
        var consent = new ParentalGateConsent(() => Child(), fake);
        Assert.False(Ask(new AgeBandPurchaseGate(() => AgeBand.Unknown, consent)));
        Assert.True(Ask(new AgeBandPurchaseGate(() => AgeBand.ThirteenPlus, consent)));
        Assert.Equal(0, fake.Asked);
    }

    [Fact]
    public void The_sandbox_service_is_gated_too_and_charges_nothing_when_declined()
    {
        var sandbox = new SandboxPurchaseService();
        var svc = new GatedPurchaseService(sandbox, new AgeBandPurchaseGate(() => AgeBand.Under13, new ParentalGateConsent(() => Child(), new FakeGate { Answer = false })));
        PurchaseOutcome? outcome = null;
        svc.Purchase("p", o => outcome = o);
        Assert.False(outcome!.Ok);
        Assert.Equal("iap.consent_required", outcome.Error);
        Assert.Empty(sandbox.History);
        Assert.True(svc.IsSandbox);
    }

    // ---- no pressure ----

    [Fact]
    public void Child_mode_hides_sales_tags_timed_or_prompted_sku_offers_and_countdowns()
    {
        var c = Child();
        Assert.False(NoPressure.ShowSalesTag(c, "best_value"));
        Assert.False(NoPressure.ShowTimedOrPromptedSku(c));
        Assert.False(NoPressure.ShowOfferCountdown(c));
        Assert.Equal(4, NoPressure.Hidden(c).Count);
        Assert.True(NoPressure.ShowSalesTag(Adult, "best_value"));
        Assert.False(NoPressure.ShowSalesTag(Adult, null));
        Assert.True(NoPressure.ShowTimedOrPromptedSku(Adult));
    }

    // ---- chat ----

    [Fact]
    public void Presets_round_trip_and_reject_anything_else()
    {
        var body = ChatSafety.EncodePreset("help_please");
        Assert.Equal("[[p:help_please]]", body);
        Assert.True(ChatSafety.TryPreset(body, out var id));
        Assert.Equal("help_please", id);
        foreach (var bad in new[] { "hello", "[[p:hello]] 555-1234", "[[p:Hello]]", "[[p:a.b]]" })
        {
            Assert.False(ChatSafety.TryPreset(bad, out _));
        }

        Assert.Throws<ArgumentException>(() => ChatSafety.EncodePreset("https://x"));
    }

    [Fact]
    public void Shipped_presets_all_encode()
    {
        foreach (var p in KidsFiles.Shipped().ChatPresets)
        {
            Assert.True(ChatSafety.TryPreset(ChatSafety.EncodePreset(p.Id), out _));
            Assert.False(string.IsNullOrWhiteSpace(p.Text));
        }
    }

    [Fact]
    public void Child_chat_is_off_by_default_presets_show_only_presets_and_blocked_players_vanish()
    {
        var blocked = new HashSet<long> { 7 };
        string? Text(string id) => id == "hello" ? "Hello!" : null;

        var off = Child();
        Assert.False(ChatSafety.CanRead(off));
        Assert.Null(ChatSafety.Visible(off, 1, "[[p:hello]]", blocked, Text));

        var presets = Child("{\"child\":{\"chat\":\"presets\"}}");
        Assert.True(ChatSafety.CanSendPresets(presets));
        Assert.False(ChatSafety.CanSendFreeText(presets));
        Assert.Equal("Hello!", ChatSafety.Visible(presets, 1, "[[p:hello]]", blocked, Text));
        Assert.Null(ChatSafety.Visible(presets, 1, "my school is", blocked, Text));
        Assert.Null(ChatSafety.Visible(presets, 1, "[[p:unknown]]", blocked, Text));
        Assert.Null(ChatSafety.Visible(presets, 7, "[[p:hello]]", blocked, Text));

        Assert.Equal("hi", ChatSafety.Visible(Adult, 1, "hi", blocked, Text));
        Assert.Null(ChatSafety.Visible(Adult, 7, "hi", blocked, Text));
    }

    [Fact]
    public void Restricted_players_get_a_generated_name_in_the_server_safe_shape()
    {
        Assert.False(ChatSafety.CanTypeName(Child("{\"child\":{\"chat\":\"presets\"}}")));
        Assert.True(ChatSafety.CanTypeName(Adult));
        foreach (var seed in new[] { "a", "0123456789abcdef", Guid.NewGuid().ToString("N") })
        {
            var name = ChatSafety.SafeName(seed);
            Assert.True(ChatSafety.IsSafeName(name), name);
            Assert.Equal(name, ChatSafety.SafeName(seed));
        }

        Assert.False(ChatSafety.IsSafeName("Sam Smith 2014"));
    }

    [Fact]
    public void Children_cannot_found_an_alliance_with_a_typed_name()
    {
        Assert.False(Child().CanCreateAlliance);
        Assert.True(Adult.CanCreateAlliance);
    }
}
