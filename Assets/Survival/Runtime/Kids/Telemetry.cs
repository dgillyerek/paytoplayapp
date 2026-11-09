#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Kids
{
    /// <summary>
    /// Where analytics events and crash reports go. The analytics / crash work plugs a vendor sink in here; until a vendor
    /// is chosen (child_safety.json telemetry.*_vendor = "none") everything goes to <see cref="NoOpTelemetrySink"/>.
    /// </summary>
    public interface ITelemetrySink
    {
        void Event(string name, IReadOnlyDictionary<string, string>? props);

        void Crash(Exception error);
    }

    public sealed class NoOpTelemetrySink : ITelemetrySink
    {
        public static readonly NoOpTelemetrySink Instance = new();

        public void Event(string name, IReadOnlyDictionary<string, string>? props)
        {
        }

        public void Crash(Exception error)
        {
        }
    }

    /// <summary>
    /// The one door to the vendor sink. Re-reads the policy on every call (the age answer can arrive mid-session).
    /// Off drops everything. Limited sends only event names whose props are on <see cref="LimitedProps"/> and crash
    /// type names without messages or stacks (they can carry typed text). Full passes through.
    /// </summary>
    public sealed class PolicyTelemetry : ITelemetrySink
    {
        /// <summary>Props allowed in Limited mode: coarse game state only, never ids or free text.</summary>
        public static IReadOnlyCollection<string> LimitedProps => Limited;

        private static readonly HashSet<string> Limited = new HashSet<string>(StringComparer.Ordinal) { "step", "level", "result", "screen" };

        private readonly ITelemetrySink _vendor;
        private readonly Func<ChildSafetyPolicy> _policy;

        public PolicyTelemetry(ITelemetrySink vendor, Func<ChildSafetyPolicy> policy)
        {
            _vendor = vendor ?? throw new ArgumentNullException(nameof(vendor));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public void Event(string name, IReadOnlyDictionary<string, string>? props)
        {
            switch (_policy().Analytics)
            {
                case TelemetryMode.Full:
                    _vendor.Event(name, props);
                    return;
                case TelemetryMode.Limited:
                    _vendor.Event(name, Strip(props));
                    return;
            }
        }

        public void Crash(Exception error)
        {
            switch (_policy().CrashReporting)
            {
                case TelemetryMode.Full:
                    _vendor.Crash(error);
                    return;
                case TelemetryMode.Limited:
                    _vendor.Crash(new RedactedCrash(error.GetType().FullName ?? "Exception"));
                    return;
            }
        }

        private static IReadOnlyDictionary<string, string>? Strip(IReadOnlyDictionary<string, string>? props)
        {
            if (props == null)
            {
                return null;
            }

            var kept = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in props)
            {
                if (Limited.Contains(kv.Key))
                {
                    kept[kv.Key] = kv.Value;
                }
            }

            return kept;
        }
    }

    /// <summary>A crash with only its exception type: no message, no stack.</summary>
    public sealed class RedactedCrash : Exception
    {
        public RedactedCrash(string type)
            : base(type) => OriginalType = type;

        public string OriginalType { get; }
    }
}
