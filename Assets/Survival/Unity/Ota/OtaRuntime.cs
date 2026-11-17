#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Ota;

namespace Survival.Unity.Ota
{
    /// <summary>
    /// What the OTA boot settled on for this run: the remote config in force (fresh, cached or baked), whether its balance
    /// patches passed the free-pace guard, and whether downloaded content is overlaid. Gameplay code reads feature flags,
    /// event schedules and store offer presentation here; until <see cref="OtaBoot"/> runs it serves the baked config.
    /// </summary>
    public static class OtaRuntime
    {
        public static RemoteConfig? Config { get; internal set; }

        public static RemoteConfigSource Source { get; internal set; } = RemoteConfigSource.Baked;

        public static FreePaceVerdict BalanceVerdict { get; internal set; } = FreePaceVerdict.NoPatches;

        /// <summary>The content_version whose bundles are overlaid this run ("" = only content baked into the build).</summary>
        public static string ActiveContentVersion { get; internal set; } = "";

        /// <summary>Remote feature flag, or <paramref name="fallback"/> when no config sets it.</summary>
        public static bool Flag(string name, bool fallback = false) => Config?.Flag(name, fallback) ?? fallback;

        /// <summary>Remote schedule for a shipped event id, or null when the remote config does not schedule it (baked rules apply).</summary>
        public static RemoteEventSchedule? EventSchedule(string eventId)
        {
            if (Config == null)
            {
                return null;
            }

            foreach (var e in Config.Events)
            {
                if (string.Equals(e.Id, eventId, StringComparison.Ordinal))
                {
                    return e;
                }
            }

            return null;
        }

        /// <summary>Store offers the remote config turns on right now, in their remote sort order.</summary>
        public static IReadOnlyList<RemoteStoreOffer> LiveOffers(DateTime nowUtc)
        {
            var list = new List<RemoteStoreOffer>();
            if (Config == null)
            {
                return list;
            }

            foreach (var o in Config.StoreOffers)
            {
                if (o.IsLive(nowUtc))
                {
                    list.Add(o);
                }
            }

            list.Sort((a, b) => a.Sort.CompareTo(b.Sort));
            return list;
        }

        /// <summary>The remote config's entry for a shipped offer id, or null (baked presentation applies).</summary>
        public static RemoteStoreOffer? Offer(string offerId)
        {
            if (Config == null)
            {
                return null;
            }

            foreach (var o in Config.StoreOffers)
            {
                if (string.Equals(o.Offer, offerId, StringComparison.Ordinal))
                {
                    return o;
                }
            }

            return null;
        }
    }
}
