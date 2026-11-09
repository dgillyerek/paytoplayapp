using System.Text.Json;
using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

public sealed class KidsSettingsTests
{
    private sealed class Fixed : IParentalConsentProvider
    {
        public Fixed(ConsentStatus s, string method = "vendor") { S = s; Method = method; }
        public ConsentStatus S { get; }
        public string Method { get; }
        public ConsentStatus Status(ConsentPurpose purpose) => S;
        public void Request(ConsentPurpose purpose, Action<ConsentStatus> done) => done(S);
        public void Revoke() { }
    }

    private static ChildSafetyPolicy Policy(string? band, ChatMode? choice = null) =>
        new(KidsFiles.Shipped(), band == null ? new AgeGateState(AgeGateStatus.Fresh, null) : new AgeGateState(AgeGateStatus.Answered, band), choice);

    [Fact]
    public void A_child_can_choose_off_or_presets_but_never_free()
    {
        var s = new PrivacySettings(Policy("under13"), NoConsentProvider.Instance);
        Assert.Equal(new[] { ChatMode.Off, ChatMode.Presets }, s.ChatChoices);
        Assert.Equal(ChatMode.Off, s.Chat);
        Assert.False(s.CanChoose(ChatMode.Free));
        Assert.Equal(ChatMode.Presets, Policy("under13", ChatMode.Free).Chat);
        Assert.Equal(ChatMode.Presets, Policy(null, ChatMode.Free).Chat);
    }

    [Fact]
    public void Adults_get_all_three_and_their_choice_sticks()
    {
        var s = new PrivacySettings(Policy("13plus", ChatMode.Presets), NoConsentProvider.Instance);
        Assert.Equal(new[] { ChatMode.Off, ChatMode.Presets, ChatMode.Free }, s.ChatChoices);
        Assert.Equal(ChatMode.Presets, s.Chat);
        Assert.False(s.ChangeNeedsParent(ChatMode.Free));
        Assert.False(s.DeleteNeedsParent);
        Assert.False(s.LinkNeedsParent);
    }

    [Fact]
    public void Turning_a_childs_chat_on_needs_a_grown_up_turning_it_off_does_not()
    {
        var off = new PrivacySettings(Policy("under13"), NoConsentProvider.Instance);
        Assert.True(off.ChangeNeedsParent(ChatMode.Presets));
        var on = new PrivacySettings(Policy("under13", ChatMode.Presets), NoConsentProvider.Instance);
        Assert.False(on.ChangeNeedsParent(ChatMode.Off));
        Assert.True(on.DeleteNeedsParent);
        Assert.True(on.LinkNeedsParent);
    }

    [Fact]
    public void Chat_max_off_removes_presets_for_children()
    {
        var json = File.ReadAllText(KidsFiles.ConfigPath).Replace("\"chat_max\": \"presets\"", "\"chat_max\": \"off\"");
        var c = ChildSafetyConfig.Parse(json);
        var p = new ChildSafetyPolicy(c, new AgeGateState(AgeGateStatus.Answered, "under13"), ChatMode.Presets);
        Assert.Equal(ChatMode.Off, p.Chat);
        Assert.Equal(new[] { ChatMode.Off }, new PrivacySettings(p, NoConsentProvider.Instance).ChatChoices);
        var free = ChildSafetyConfig.Parse(File.ReadAllText(KidsFiles.ConfigPath).Replace("\"chat_max\": \"presets\"", "\"chat_max\": \"free\""));
        Assert.Equal(ChatMode.Presets, free.ChildChatMax);
    }

    [Fact]
    public void Chat_preference_round_trips_and_ignores_junk()
    {
        var storage = new MemoryKidStorage();
        var pref = new ChatPreference(storage);
        Assert.Null(pref.Load());
        pref.Save(ChatMode.Presets);
        Assert.Equal(ChatMode.Presets, pref.Load());
        storage.Values[ChatPreference.Key] = "everything";
        Assert.Null(pref.Load());
        pref.Clear();
        Assert.False(storage.Values.ContainsKey(ChatPreference.Key));
    }

    [Theory]
    [InlineData("13plus", ConsentStatus.NeedsConsent, ConsentView.NotNeeded)]
    [InlineData("under13", ConsentStatus.NeedsConsent, ConsentView.Needed)]
    [InlineData("under13", ConsentStatus.Pending, ConsentView.Pending)]
    [InlineData("under13", ConsentStatus.Granted, ConsentView.Granted)]
    [InlineData("under13", ConsentStatus.Denied, ConsentView.Denied)]
    [InlineData(null, ConsentStatus.NeedsConsent, ConsentView.Needed)]
    public void Consent_status_row(string? band, ConsentStatus status, ConsentView expected) =>
        Assert.Equal(expected, new PrivacySettings(Policy(band), new Fixed(status)).Consent);

    [Fact]
    public void Ask_a_grown_up_shows_only_once_a_consent_method_is_plugged_in()
    {
        Assert.False(new PrivacySettings(Policy("under13"), NoConsentProvider.Instance).CanRequestConsent);
        Assert.True(new PrivacySettings(Policy("under13"), new Fixed(ConsentStatus.NeedsConsent)).CanRequestConsent);
        Assert.False(new PrivacySettings(Policy("under13"), new Fixed(ConsentStatus.Granted)).CanRequestConsent);
        Assert.False(new PrivacySettings(Policy("13plus"), new Fixed(ConsentStatus.NeedsConsent)).CanRequestConsent);
    }

    [Fact]
    public void Age_row_is_read_only_band_text()
    {
        Assert.Equal("under13", new PrivacySettings(Policy("under13"), NoConsentProvider.Instance).AgeBandKey);
        Assert.Equal("13plus", new PrivacySettings(Policy("13plus"), NoConsentProvider.Instance).AgeBandKey);
        Assert.Equal("unknown", new PrivacySettings(Policy(null), NoConsentProvider.Instance).AgeBandKey);
    }

    [Fact]
    public void Shipped_config_has_privacy_and_terms_links()
    {
        var c = KidsFiles.Shipped();
        Assert.StartsWith("https://", c.PrivacyPolicyUrl);
        Assert.StartsWith("https://", c.TermsUrl);
        Assert.Equal("", ChildSafetyConfig.Default.PrivacyPolicyUrl);
    }

    [Fact]
    public void Hud_settings_gear_has_its_own_slot_in_the_top_bar()
    {
        var path = Path.Combine(KidsFiles.RepoRoot(), "Assets", "ThemePack", "fantasy_kingdom_a", "ui", "hud_layout.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var hud = doc.RootElement;
        Assert.True(hud.TryGetProperty("settings", out var g));
        static float V(JsonElement e, string k) => (float)e.GetProperty(k).GetDouble();
        foreach (var other in new[] { "player_name", "player_power", "res_soft" })
        {
            var o = hud.GetProperty(other);
            var overlap = V(g, "x_min") < V(o, "x_max") && V(o, "x_min") < V(g, "x_max") && V(g, "y_top_min") < V(o, "y_top_max") && V(o, "y_top_min") < V(g, "y_top_max");
            Assert.False(overlap, "settings overlaps " + other);
        }
    }
}
