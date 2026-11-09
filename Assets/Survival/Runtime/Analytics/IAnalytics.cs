#nullable enable
using System.Collections.Generic;

namespace Survival.Domain.Analytics
{
    /// <summary>
    /// Vendor-agnostic analytics. Soft-launch default is <see cref="NoOpAnalytics"/>; a vendor adapter plugs in
    /// after Derek picks one (see Docs/analytics-vendors.md). Never send ad IDs, device IDs, or free-text PII.
    /// </summary>
    public interface IAnalytics
    {
        void Track(string eventName, IReadOnlyDictionary<string, string>? props = null);
    }

    public sealed class NoOpAnalytics : IAnalytics
    {
        public static readonly NoOpAnalytics Instance = new();

        public void Track(string eventName, IReadOnlyDictionary<string, string>? props = null)
        {
        }
    }
}
