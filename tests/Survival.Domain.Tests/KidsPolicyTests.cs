using Survival.Domain.Iap;
using Survival.Domain.Kids;

namespace Survival.Domain.Tests;

public sealed class KidsPolicyTests
{
    [Fact]
    public void Shipped_config_parses_with_the_safe_defaults_for_open_Derek_decisions()
    {
        var c = KidsFiles.Shipped();
        Assert.Equal(new[] { "under13", "13plus" }, c.AgeBands.Select(b => b.Id));
        Assert.Equal(ChatMode.Off, c.ChildRules.Chat);
        Assert.Equal(IapMode.ParentalGate, c.ChildRules.Iap);
        Assert.Equal(TelemetryMode.Off, c.ChildRules.Analytics);
        Assert.Equal(TelemetryMode.Off, c.ChildRules.CrashReporting);
        Assert.Equal(ConsentMode.ConsentRequired, c.ChildRules.OnlineSignIn);
        Assert.Equal(ConsentMode.ConsentRequired, c.ChildRules.CloudSave);
        Assert.True(c.ChildRules.NoPressure);
        Assert.False(c.Ads.Enabled);
        Assert.Empty(c.Ads.FamiliesSelfCertified);
        Assert.Equal(ChildSafetyConfig.VendorNone, c.AnalyticsVendor);
        Assert.Equal(ChildSafetyConfig.VendorNone, c.CrashVendor);
        Assert.NotEmpty(c.ChatPresets);
        Assert.StartsWith("https://", c.WebDeletionUrl);
    }

    [Fact]
    public void Shipped_config_matches_the_built_in_fallback_rules()
    {
        var c = KidsFiles.Shipped();
        Assert.Equal(ChildSafetyConfig.Default.ChildRules, c.ChildRules);
        Assert.Equal(ChildSafetyConfig.Default.AdultRules, c.AdultRules);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("under13")]
    [InlineData("a_band_removed_from_config")]
    public void Unknown_unanswered_or_child_bands_get_child_rules(string? band)
    {
        var p = KidsFiles.Policy(KidsFiles.Shipped(), band);
        Assert.True(p.IsChild);
        Assert.False(p.AdvertisingIdAllowed);
        Assert.False(p.PersonalizationAllowed);
        Assert.Equal(ChatMode.Off, p.Chat);
        Assert.Equal(IapMode.ParentalGate, p.Iap);
        Assert.True(p.NoPressure);
        Assert.Equal(TelemetryMode.Off, p.Analytics);
    }

    [Fact]
    public void Thirteen_plus_gets_adult_rules()
    {
        var p = KidsFiles.Policy(KidsFiles.Shipped(), "13plus");
        Assert.False(p.IsChild);
        Assert.Equal(ChatMode.Free, p.Chat);
        Assert.Equal(IapMode.Allowed, p.Iap);
        Assert.False(p.NoPressure);
        Assert.Equal(AgeBand.ThirteenPlus, p.AgeBand);
    }

    [Fact]
    public void Age_band_maps_to_the_IAP_branch_enum()
    {
        var c = KidsFiles.Shipped();
        Assert.Equal(AgeBand.Unknown, KidsFiles.Policy(c, null).AgeBand);
        Assert.Equal(AgeBand.Under13, KidsFiles.Policy(c, "under13").AgeBand);
    }

    [Fact]
    public void Config_cannot_lift_the_child_floors()
    {
        var json = "{\"child\":{\"advertising_id\":true,\"personalization\":true,\"chat\":\"free\"}}";
        var p = KidsFiles.Policy(ChildSafetyConfig.Parse(json), "under13");
        Assert.False(p.AdvertisingIdAllowed);
        Assert.False(p.PersonalizationAllowed);
        Assert.Equal(ChatMode.Presets, p.Chat);
    }

    [Fact]
    public void No_vendor_means_no_telemetry_for_anyone_even_if_rules_allow_it()
    {
        var json = "{\"adult\":{\"analytics\":\"full\",\"crash_reporting\":\"full\"},\"telemetry\":{\"analytics_vendor\":\"none\",\"crash_vendor\":\"none\"}}";
        var p = KidsFiles.Policy(ChildSafetyConfig.Parse(json), "13plus");
        Assert.Equal(TelemetryMode.Off, p.Analytics);
        Assert.Equal(TelemetryMode.Off, p.CrashReporting);
    }

    [Fact]
    public void Unknown_enum_values_fail_loudly()
    {
        Assert.Throws<FormatException>(() => ChildSafetyConfig.Parse("{\"child\":{\"chat\":\"sometimes\"}}"));
    }

    private sealed class RecordingSink : ITelemetrySink
    {
        public List<(string Name, IReadOnlyDictionary<string, string>? Props)> Events { get; } = new();
        public List<Exception> Crashes { get; } = new();

        public void Event(string name, IReadOnlyDictionary<string, string>? props) => Events.Add((name, props));

        public void Crash(Exception error) => Crashes.Add(error);
    }

    private static ChildSafetyConfig WithVendor(string childMode) => ChildSafetyConfig.Parse(
        "{\"child\":{\"analytics\":\"" + childMode + "\",\"crash_reporting\":\"" + childMode + "\"},\"adult\":{\"analytics\":\"full\",\"crash_reporting\":\"full\"},\"telemetry\":{\"analytics_vendor\":\"test\",\"crash_vendor\":\"test\"}}");

    [Fact]
    public void Default_telemetry_is_a_no_op()
    {
        var sink = new RecordingSink();
        var t = new PolicyTelemetry(sink, () => KidsFiles.Policy(KidsFiles.Shipped(), "13plus"));
        t.Event("ftue_step", new Dictionary<string, string> { ["step"] = "1" });
        t.Crash(new InvalidOperationException("x"));
        Assert.Empty(sink.Events);
        Assert.Empty(sink.Crashes);
    }

    [Fact]
    public void Child_off_sends_nothing_and_limited_strips_ids_and_messages()
    {
        var sink = new RecordingSink();
        new PolicyTelemetry(sink, () => KidsFiles.Policy(WithVendor("off"), "under13")).Event("e", null);
        Assert.Empty(sink.Events);

        var limited = new PolicyTelemetry(sink, () => KidsFiles.Policy(WithVendor("limited"), "under13"));
        limited.Event("level_up", new Dictionary<string, string> { ["level"] = "5", ["player_id"] = "123", ["name"] = "Sam" });
        limited.Crash(new InvalidOperationException("typed by Sam"));
        var props = Assert.Single(sink.Events).Props!;
        Assert.Equal(new[] { "level" }, props.Keys);
        var crash = Assert.IsType<RedactedCrash>(Assert.Single(sink.Crashes));
        Assert.DoesNotContain("Sam", crash.Message);
    }

    [Fact]
    public void Telemetry_follows_the_age_answer_mid_session()
    {
        var sink = new RecordingSink();
        var c = WithVendor("off");
        string? band = null;
        var t = new PolicyTelemetry(sink, () => KidsFiles.Policy(c, band));
        t.Event("before", null);
        band = "13plus";
        t.Event("after", null);
        Assert.Equal("after", Assert.Single(sink.Events).Name);
    }
}
