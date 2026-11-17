#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Serialization;

namespace Survival.Domain.Ota
{
    /// <summary>
    /// The locked free (no-spend, no power-up) pace, Derek 2026-10-27: "No matter what changes we make we always want to
    /// keep the pace the same without power ups." tests/Survival.Domain.Tests/RunnerFreePaceTests.cs holds the shipped
    /// data to it; <see cref="FreePaceGuard"/> holds remote balance overrides to it.
    /// </summary>
    public static class FreePaceLock
    {
        /// <summary>The level the free profile first fails on schedule.</summary>
        public const int Wall = 31;

        /// <summary>Allowed relative drift of a checkpoint day (3%), never tighter than one session.</summary>
        public const double Tolerance = 0.03;

        /// <summary>One play session in days (runner_balance.json pace.sessions_per_day = 3; not remotely patchable).</summary>
        public const double OneSessionDays = 1.0 / 3.0 + 1e-6;

        /// <summary>Day the free profile first clears each checkpoint level.</summary>
        public static readonly IReadOnlyDictionary<int, double> ClearedDay = new Dictionary<int, double>
        {
            [10] = 3.0,
            [20] = 6.33,
            [31] = 13.0,
            [34] = 48.67,
            [36] = 149.0,
            [40] = 150.33,
            [44] = 341.0,
            [46] = 488.67,
            [50] = 828.67,
        };

        /// <summary>Allowed absolute drift (days) for a checkpoint locked at <paramref name="lockedDay"/>.</summary>
        public static double Allowed(double lockedDay, double oneSessionDays = OneSessionDays) =>
            Math.Max(lockedDay * Tolerance, oneSessionDays);

        /// <summary>Every checkpoint that is off: empty when <paramref name="wall"/> and <paramref name="clearedDay"/> match the lock.</summary>
        public static List<string> Drift(int wall, Func<int, double?> clearedDay, double oneSessionDays = OneSessionDays)
        {
            var off = new List<string>();
            if (wall != Wall)
            {
                off.Add("wall L" + wall + ", locked L" + Wall);
            }

            foreach (var kv in ClearedDay)
            {
                var day = clearedDay(kv.Key);
                var allowed = Allowed(kv.Value, oneSessionDays);
                if (day == null || Math.Abs(day.Value - kv.Value) > allowed)
                {
                    off.Add("L" + kv.Key + ": day " + (day?.ToString("0.00") ?? "never") + ", locked " + kv.Value.ToString("0.00") + " +-" + allowed.ToString("0.00"));
                }
            }

            return off;
        }
    }

    public enum FreePaceVerdict
    {
        /// <summary>No balance patches: baked balance, nothing to check.</summary>
        NoPatches,

        /// <summary>Patches carry a matching, in-tolerance publish-time check: apply them.</summary>
        Accepted,

        /// <summary>Patches without a check, with a check for different patches, or with a check that moved the pace: ignore them.</summary>
        Rejected
    }

    /// <summary>
    /// A phone cannot run the free-pace sim (thousands of runner sims) at boot, so remote balance is checked at publish
    /// time: scripts/ota/check_remote_balance.sh runs the real sim (RemoteBalancePaceTests) with the patches applied and
    /// stamps the result plus the SHA-256 of the canonical patches into balance.free_pace_check. The phone applies the
    /// patches only when that hash matches the patches it received and the stamped wall/days are within the lock, so an
    /// unchecked, edited, or pace-moving override is rejected and the baked balance stays.
    /// </summary>
    public static class FreePaceGuard
    {
        public static FreePaceVerdict Check(RemoteConfig config, out string reason)
        {
            if (!config.HasBalancePatches)
            {
                reason = "";
                return FreePaceVerdict.NoPatches;
            }

            var check = config.FreePaceCheck;
            if (check == null)
            {
                reason = "balance patches have no free_pace_check (run scripts/ota/check_remote_balance.sh before publishing)";
                return FreePaceVerdict.Rejected;
            }

            var sha = HashPatches(config);
            if (!string.Equals(sha, check.BalanceSha256, StringComparison.Ordinal))
            {
                reason = "free_pace_check was made for different balance patches (sha " + check.BalanceSha256 + ", patches " + sha + ")";
                return FreePaceVerdict.Rejected;
            }

            var off = FreePaceLock.Drift(check.Wall, level => check.ClearedDay.TryGetValue(level, out var d) ? d : (double?)null);
            if (off.Count > 0)
            {
                reason = "balance patches move the locked free pace: " + string.Join("; ", off);
                return FreePaceVerdict.Rejected;
            }

            reason = "";
            return FreePaceVerdict.Accepted;
        }

        /// <summary>SHA-256 of the canonical balance.patches object, the value free_pace_check.balance_sha256 must carry.</summary>
        public static string HashPatches(RemoteConfig config) => JsonPatch.Sha256Hex(config.BalancePatchesNode);
    }

    /// <summary>
    /// Applies accepted remote balance patches to baked catalog files as they are read. Only the files the free-pace model
    /// reads can be patched (so the publish-time check covers everything a patch can change); runner_balance.json (the
    /// model's own assumptions) and every other file are not patchable.
    /// </summary>
    public sealed class RemoteBalance
    {
        private static readonly HashSet<string> Patchable = new HashSet<string>(StringComparer.Ordinal)
        {
            "runner_levels.json",
            "campaign.json",
            "base_economy.json",
            "gifts.json",
            "quests.json",
            "stores.json",
            "power_packs.json",
        };

        public static IReadOnlyCollection<string> PatchableFiles => Patchable;

        public static bool IsPatchable(string fileName) => Patchable.Contains(fileName);

        private readonly IReadOnlyDictionary<string, JsonNode> _patches;

        private RemoteBalance(IReadOnlyDictionary<string, JsonNode> patches) => _patches = patches;

        public static RemoteBalance None { get; } = new RemoteBalance(new Dictionary<string, JsonNode>());

        public int PatchCount => _patches.Count;

        /// <summary>The overlay for <paramref name="config"/>: its patches when the guard accepts them, else none.</summary>
        public static RemoteBalance For(RemoteConfig config, out FreePaceVerdict verdict, out string reason)
        {
            verdict = FreePaceGuard.Check(config, out reason);
            return verdict == FreePaceVerdict.Accepted ? new RemoteBalance(config.BalancePatches) : None;
        }

        /// <summary>Test / publish-tool entry: patches applied without the guard (the sim decides).</summary>
        internal static RemoteBalance Unchecked(RemoteConfig config) => new RemoteBalance(config.BalancePatches);

        /// <summary>
        /// <paramref name="text"/> with the patch for <paramref name="path"/>'s file name applied, or unchanged. Only files
        /// under a "Survival" catalog folder are touched (the Theme pack's own store/products.json is never patched).
        /// </summary>
        public string Apply(string path, string text)
        {
            if (_patches.Count == 0 || string.IsNullOrEmpty(path))
            {
                return text;
            }

            var p = path.Replace('\\', '/');
            var slash = p.LastIndexOf('/');
            var name = slash >= 0 ? p.Substring(slash + 1) : p;
            if (!_patches.TryGetValue(name, out var patch))
            {
                return text;
            }

            var dir = slash >= 0 ? p.Substring(0, slash) : "";
            if (slash >= 0 && !dir.EndsWith("/Survival", StringComparison.Ordinal) && !dir.EndsWith("Data", StringComparison.Ordinal))
            {
                return text;
            }

            return JsonPatch.Write(JsonPatch.MergePatch(JsonRead.Parse(text), patch));
        }
    }
}
