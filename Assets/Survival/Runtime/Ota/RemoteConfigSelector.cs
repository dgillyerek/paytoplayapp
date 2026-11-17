#nullable enable
using System.Collections.Generic;

namespace Survival.Domain.Ota
{
    public enum RemoteConfigSource
    {
        /// <summary>The build's baked default (Resources/Ota/remote_config.default.json).</summary>
        Baked,

        /// <summary>The last good copy saved on the device.</summary>
        Cached,

        /// <summary>Fetched from the host this launch.</summary>
        Fresh
    }

    /// <summary>The config to use this launch and why the others were not.</summary>
    public sealed class RemoteConfigChoice
    {
        public RemoteConfig Config { get; init; } = null!;

        public RemoteConfigSource Source { get; init; }

        /// <summary>True when the fresh document should be written as the new last-good copy.</summary>
        public bool SaveFresh { get; init; }

        public IReadOnlyList<string> Notes { get; init; } = new List<string>();
    }

    /// <summary>
    /// Fresh beats cached beats baked, each only when it validates. A fresh document with a lower config_version than the
    /// cached one is ignored (a stale CDN edge must not roll a phone back; roll back by publishing a higher config_version).
    /// </summary>
    public static class RemoteConfigSelector
    {
        public static RemoteConfigChoice Pick(string? freshJson, string? cachedJson, string bakedJson, string appId)
        {
            var notes = new List<string>();
            var baked = RemoteConfigParser.Parse(bakedJson, appId);
            if (!baked.Ok)
            {
                throw new System.InvalidOperationException("Baked remote config is invalid: " + string.Join("; ", baked.Errors));
            }

            RemoteConfig? cached = null;
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var c = RemoteConfigParser.Parse(cachedJson, appId);
                if (c.Ok)
                {
                    cached = c.Config;
                }
                else
                {
                    notes.Add("cached config rejected: " + string.Join("; ", c.Errors));
                }
            }

            if (!string.IsNullOrEmpty(freshJson))
            {
                var f = RemoteConfigParser.Parse(freshJson, appId);
                if (!f.Ok)
                {
                    notes.Add("fresh config rejected: " + string.Join("; ", f.Errors));
                }
                else if (cached != null && f.Config!.ConfigVersion < cached.ConfigVersion)
                {
                    notes.Add("fresh config_version " + f.Config.ConfigVersion + " is older than cached " + cached.ConfigVersion + "; kept cached");
                }
                else
                {
                    return new RemoteConfigChoice { Config = f.Config!, Source = RemoteConfigSource.Fresh, SaveFresh = true, Notes = notes };
                }
            }

            if (cached != null)
            {
                return new RemoteConfigChoice { Config = cached, Source = RemoteConfigSource.Cached, Notes = notes };
            }

            return new RemoteConfigChoice { Config = baked.Config!, Source = RemoteConfigSource.Baked, Notes = notes };
        }
    }
}
