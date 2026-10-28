#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Ids;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// A won run pays gold per kill and per box broken, the win gold and the hero shards. A lost run pays nothing at all
    /// (Derek 2026-10-27: "i shouldn't get runner rewards when I die"): an empty bundle, no level opened, no banner spent.
    /// </summary>
    public static class RunnerRewards
    {
        public static RewardBundle For(RunnerLevelDef level, RunnerResult result)
        {
            var bundle = new RewardBundle { Won = result.Won, RunnerNumber = Math.Max(1, result.Number) };
            if (!result.Won)
            {
                return bundle;
            }

            var r = level.Rewards;
            var gold = (result.Kills * r.GoldPerKill) + (result.BoxesBroken * r.GoldPerBox) + r.WinGold;
            if (gold > 0)
            {
                bundle.Resources[SurvIds.ResSoft] = gold;
            }

            if (r.Shards > 0 && r.ShardHero.Length > 0)
            {
                bundle.ShardHero = r.ShardHero;
                bundle.Shards = r.Shards;
            }

            return bundle;
        }

        /// <summary>Pay a bundle into the wallet and the hero roster (the same path a Battle Setup fight pays through).</summary>
        public static void Grant(RewardBundle bundle, CampaignSession campaign)
        {
            if (!bundle.Won)
            {
                // A lost run: nothing to pay, nothing saved.
                return;
            }

            campaign.Wallet.Grant(bundle.Resources);
            if (bundle.Won && bundle.RunnerNumber > 0)
            {
                // A War Banner charge is spent only on a new level cleared (the run was boosted while charges were left).
                if (bundle.RunnerNumber >= campaign.RunnerUnlocked && campaign.Wallet.Get(SurvIds.ResRunnerBoost) > 0)
                {
                    campaign.Wallet.TrySpend(SurvIds.ResRunnerBoost, 1);
                }

                campaign.UnlockRunner(bundle.RunnerNumber + 1);
            }

            if (bundle.Shards > 0 && bundle.ShardHero.Length > 0)
            {
                campaign.Roster.AddShards(bundle.ShardHero, bundle.Shards);
            }

            campaign.Touch();
        }

        /// <summary>Several runs (Retry) paid together: resources and shards summed per hero.</summary>
        public static Dictionary<string, int> Rows(IEnumerable<RewardBundle> bundles)
        {
            var rows = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var b in bundles)
            {
                foreach (var pair in b.AsRows())
                {
                    rows.TryGetValue(pair.Key, out var have);
                    rows[pair.Key] = have + pair.Value;
                }
            }

            return rows;
        }
    }

    /// <summary>
    /// Play → RunnerLevel round trip (the scene loads additively; Play stays loaded). <see cref="Begin"/> names the level;
    /// every finished run (Retry included) is banked with <see cref="Bank"/> and paid once when the scene hands back with
    /// <see cref="Finish"/>. Standalone (menu entry), nothing is pending and the scene pays the save file itself.
    /// </summary>
    public static class RunnerHandoff
    {
        private static readonly List<(RunnerResult Result, RewardBundle Bundle)> Banked = new();

        public static string? PendingLevel { get; private set; }

        /// <summary>Highest runner level open when Play opened the runner (the scene's level list and Next button).</summary>
        public static int Unlocked { get; private set; } = 1;
        public static bool FromPlay => PendingLevel != null;
        public static bool Done { get; private set; }

        public static IReadOnlyList<(RunnerResult Result, RewardBundle Bundle)> Runs => Banked;

        public static event Action? Finished;

        public static void Begin(string levelId, int unlocked = 1)
        {
            PendingLevel = levelId ?? throw new ArgumentNullException(nameof(levelId));
            Unlocked = Math.Max(1, unlocked);
            Banked.Clear();
            Done = false;
        }

        public static void Bank(RunnerResult result, RewardBundle bundle)
        {
            Banked.Add((result, bundle));
            if (result.Won)
            {
                Unlocked = Math.Max(Unlocked, result.Number + 1);
            }
        }

        public static void Finish()
        {
            Done = true;
            Finished?.Invoke();
        }

        /// <summary>The banked runs, cleared; Play pays them.</summary>
        public static List<(RunnerResult Result, RewardBundle Bundle)> Take()
        {
            var runs = new List<(RunnerResult, RewardBundle)>(Banked);
            Banked.Clear();
            PendingLevel = null;
            Done = false;
            return runs;
        }

        public static void Reset()
        {
            Banked.Clear();
            Unlocked = 1;
            PendingLevel = null;
            Done = false;
            Finished = null;
        }
    }
}
