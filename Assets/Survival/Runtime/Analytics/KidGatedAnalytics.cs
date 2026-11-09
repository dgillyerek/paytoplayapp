#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Iap;
using Survival.Domain.Kids;

namespace Survival.Domain.Analytics
{
    /// <summary>
    /// Hard gate: under-13 and unknown age never emit (Play Families / COPPA). Thirteen-plus still respects
    /// <see cref="ChildSafetyPolicy.Analytics"/> (Off / Limited / Full) and schema validation.
    /// </summary>
    public sealed class KidGatedAnalytics : IAnalytics
    {
        private readonly IAnalytics _inner;
        private readonly Func<AgeBand> _band;
        private readonly Func<TelemetryMode> _mode;

        public KidGatedAnalytics(IAnalytics inner, Func<AgeBand> band, Func<TelemetryMode>? mode = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _band = band ?? throw new ArgumentNullException(nameof(band));
            _mode = mode ?? (() => TelemetryMode.Full);
        }

        public bool Enabled
        {
            get
            {
                var band = _band();
                if (band is AgeBand.Under13 or AgeBand.Unknown)
                {
                    return false;
                }

                return _mode() != TelemetryMode.Off;
            }
        }

        public void Track(string eventName, IReadOnlyDictionary<string, string>? props = null)
        {
            if (!Enabled)
            {
                return;
            }

            if (FunnelEvents.Validate(eventName, props) != null)
            {
                return;
            }

            if (_mode() == TelemetryMode.Limited)
            {
                props = StripLimited(props);
            }

            _inner.Track(eventName, props);
        }

        private static IReadOnlyDictionary<string, string>? StripLimited(IReadOnlyDictionary<string, string>? props)
        {
            if (props == null)
            {
                return null;
            }

            var kept = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in props)
            {
                if (PolicyTelemetry.LimitedProps.Contains(kv.Key))
                {
                    kept[kv.Key] = kv.Value;
                }
            }

            return kept;
        }
    }

    /// <summary>Hard gate for crashes: under-13 / unknown never report; Limited redacts to type name only.</summary>
    public sealed class KidGatedCrashReporter : ICrashReporter
    {
        private readonly ICrashReporter _inner;
        private readonly Func<AgeBand> _band;
        private readonly Func<TelemetryMode> _mode;

        public KidGatedCrashReporter(ICrashReporter inner, Func<AgeBand> band, Func<TelemetryMode>? mode = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _band = band ?? throw new ArgumentNullException(nameof(band));
            _mode = mode ?? (() => TelemetryMode.Full);
        }

        public bool Enabled
        {
            get
            {
                var band = _band();
                if (band is AgeBand.Under13 or AgeBand.Unknown)
                {
                    return false;
                }

                return _mode() != TelemetryMode.Off;
            }
        }

        public void Report(Exception error)
        {
            if (!Enabled || error == null)
            {
                return;
            }

            if (_mode() == TelemetryMode.Limited)
            {
                _inner.Report(new RedactedCrash(error.GetType().FullName ?? "Exception"));
                return;
            }

            _inner.Report(error);
        }
    }

    /// <summary>Adapts domain analytics onto the kids <see cref="ITelemetrySink"/> used by PolicyTelemetry.</summary>
    public sealed class TelemetrySinkAnalytics : IAnalytics, ICrashReporter
    {
        private readonly ITelemetrySink _sink;

        public TelemetrySinkAnalytics(ITelemetrySink sink) =>
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));

        public void Track(string eventName, IReadOnlyDictionary<string, string>? props = null) =>
            _sink.Event(eventName, props);

        public void Report(Exception error) => _sink.Crash(error);
    }

    /// <summary>Test double that records every accepted call.</summary>
    public sealed class RecordingAnalytics : IAnalytics, ICrashReporter
    {
        public List<(string Name, IReadOnlyDictionary<string, string>? Props)> Events { get; } = new();
        public List<Exception> Crashes { get; } = new();

        public void Track(string eventName, IReadOnlyDictionary<string, string>? props = null) =>
            Events.Add((eventName, props));

        public void Report(Exception error) => Crashes.Add(error);
    }
}
