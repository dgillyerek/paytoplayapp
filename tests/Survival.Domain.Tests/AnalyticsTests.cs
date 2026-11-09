#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Analytics;
using Survival.Domain.Iap;
using Survival.Domain.Kids;
using Xunit;

namespace Survival.Domain.Tests
{
    public class AnalyticsTests
    {
        [Fact]
        public void Funnel_schema_accepts_core_events_and_rejects_pii_props()
        {
            Assert.Null(FunnelEvents.Validate(FunnelEvents.FirstOpen, null));
            Assert.Null(FunnelEvents.Validate(FunnelEvents.AgeGateCompleted, FunnelEvents.BandOnly("under13")));
            Assert.Equal("bad_band", FunnelEvents.Validate(FunnelEvents.AgeGateCompleted, FunnelEvents.BandOnly("2013-01-01")));
            Assert.Equal("band_required", FunnelEvents.Validate(FunnelEvents.AgeGateCompleted, new Dictionary<string, string>()));
            Assert.StartsWith("forbidden_prop:", FunnelEvents.Validate(FunnelEvents.LevelUp, new Dictionary<string, string>
            {
                ["level"] = "3",
                ["device_id"] = "abc",
            })!);
            Assert.Equal("unknown_event", FunnelEvents.Validate("behavioral_profile", null));
            Assert.Equal("unexpected_prop:sku", FunnelEvents.Validate(FunnelEvents.LevelUp, FunnelEvents.Sku("gems")));
        }

        [Fact]
        public void All_funnel_names_are_registered()
        {
            var expected = new[]
            {
                FunnelEvents.FirstOpen, FunnelEvents.AgeGateShown, FunnelEvents.AgeGateCompleted,
                FunnelEvents.TutorialStepStart, FunnelEvents.TutorialStepComplete, FunnelEvents.QuestStepComplete,
                FunnelEvents.LevelUp, FunnelEvents.FirstBattle, FunnelEvents.StoreView,
                FunnelEvents.IapAttempt, FunnelEvents.IapSuccess, FunnelEvents.IapFail,
                FunnelEvents.ReturnD1, FunnelEvents.ReturnD7,
            };
            foreach (var name in expected)
            {
                Assert.True(FunnelEvents.IsKnown(name), name);
            }

            Assert.Equal(expected.Length, FunnelEvents.AllNames.Count);
        }

        [Theory]
        [InlineData(AgeBand.Under13)]
        [InlineData(AgeBand.Unknown)]
        public void Kid_gate_drops_everything_for_under13_and_unknown(AgeBand band)
        {
            var rec = new RecordingAnalytics();
            var analytics = new KidGatedAnalytics(rec, () => band, () => TelemetryMode.Full);
            var crash = new KidGatedCrashReporter(rec, () => band, () => TelemetryMode.Full);

            Assert.False(analytics.Enabled);
            analytics.Track(FunnelEvents.FirstOpen);
            crash.Report(new InvalidOperationException("boom"));
            Assert.Empty(rec.Events);
            Assert.Empty(rec.Crashes);
        }

        [Fact]
        public void Kid_gate_forwards_safe_events_for_thirteen_plus()
        {
            var rec = new RecordingAnalytics();
            var analytics = new KidGatedAnalytics(rec, () => AgeBand.ThirteenPlus, () => TelemetryMode.Full);
            Assert.True(analytics.Enabled);

            analytics.Track(FunnelEvents.AgeGateCompleted, FunnelEvents.BandOnly("13plus"));
            analytics.Track(FunnelEvents.LevelUp, new Dictionary<string, string> { ["level"] = "5", ["ad_id"] = "nope" });
            Assert.Single(rec.Events);
            Assert.Equal(FunnelEvents.AgeGateCompleted, rec.Events[0].Name);
            Assert.Equal("13plus", rec.Events[0].Props![FunnelEvents.PropBand]);
        }

        [Fact]
        public void Kid_gate_respects_off_mode_even_for_adults()
        {
            var rec = new RecordingAnalytics();
            var analytics = new KidGatedAnalytics(rec, () => AgeBand.ThirteenPlus, () => TelemetryMode.Off);
            analytics.Track(FunnelEvents.FirstOpen);
            Assert.Empty(rec.Events);
        }

        [Fact]
        public void Limited_mode_strips_non_limited_props_for_adults()
        {
            var rec = new RecordingAnalytics();
            var analytics = new KidGatedAnalytics(rec, () => AgeBand.ThirteenPlus, () => TelemetryMode.Limited);
            analytics.Track(FunnelEvents.QuestStepComplete, new Dictionary<string, string>
            {
                [FunnelEvents.PropChain] = "quest.chain.starter",
                [FunnelEvents.PropQuest] = "q1",
                [FunnelEvents.PropStep] = "2",
            });
            Assert.Single(rec.Events);
            Assert.True(rec.Events[0].Props!.ContainsKey(FunnelEvents.PropStep));
            Assert.False(rec.Events[0].Props!.ContainsKey(FunnelEvents.PropChain));
            Assert.False(rec.Events[0].Props!.ContainsKey(FunnelEvents.PropQuest));
        }

        [Fact]
        public void Crash_limited_redacts_to_type_name()
        {
            var rec = new RecordingAnalytics();
            var crash = new KidGatedCrashReporter(rec, () => AgeBand.ThirteenPlus, () => TelemetryMode.Limited);
            crash.Report(new InvalidOperationException("secret path /Users/kid"));
            Assert.Single(rec.Crashes);
            Assert.IsType<RedactedCrash>(rec.Crashes[0]);
            Assert.DoesNotContain("secret", rec.Crashes[0].Message);
        }

        [Fact]
        public void NoOp_sinks_are_safe_defaults()
        {
            NoOpAnalytics.Instance.Track(FunnelEvents.FirstOpen);
            NoOpCrashReporter.Instance.Report(new Exception("x"));
        }
    }
}
