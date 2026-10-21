#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// Heroes + battles slice of the Play session: the hero roster with training, stage progress, battle bounties,
    /// rewards and the save file. Shares the HUD wallet with the village.
    /// </summary>
    public sealed class CampaignSession
    {
        private readonly HashSet<string> _cleared = new(StringComparer.Ordinal);

        public CampaignSession(CampaignCatalog catalog, Wallet wallet, IGameClock clock, RushRules? rush = null)
        {
            Catalog = catalog;
            Wallet = wallet;
            Clock = clock;
            Roster = new HeroRoster(catalog, wallet, clock, rush);
        }

        public CampaignCatalog Catalog { get; }
        public Wallet Wallet { get; }
        public IGameClock Clock { get; }
        public HeroRoster Roster { get; }
        public int Wins { get; private set; }
        public int Battles { get; private set; }
        public int BountyIndex { get; private set; }
        public IReadOnlyCollection<string> Cleared => _cleared;

        /// <summary>Highest runner level open (1-based; Level 1 is always open). Saved as "runner_unlocked".</summary>
        public int RunnerUnlocked { get; private set; } = 1;

        /// <summary>Open runner levels up to <paramref name="number"/> (never closes one).</summary>
        public void UnlockRunner(int number)
        {
            if (number > RunnerUnlocked)
            {
                RunnerUnlocked = number;
                Touch();
            }
        }

        internal void RestoreRunner(int unlocked) => RunnerUnlocked = Math.Max(1, unlocked);

        /// <summary>Anything the save file holds changed (training, levels, battles, bounties).</summary>
        public event Action? Changed;

        public void Touch() => Changed?.Invoke();

        public bool IsCleared(string stageId) => _cleared.Contains(stageId);

        /// <summary>The first stage is open; each next one opens when the one before is cleared. Camps are always open.</summary>
        public bool IsUnlocked(string stageId)
        {
            if (stageId.StartsWith("camp:", StringComparison.Ordinal))
            {
                return true;
            }

            var i = Catalog.StageIndex(stageId);
            return i == 0 || (i > 0 && IsCleared(Catalog.Stages[i - 1].Id));
        }

        /// <summary>First stage not yet cleared (the last one when all are).</summary>
        public StageDef? NextStage
        {
            get
            {
                foreach (var s in Catalog.Stages)
                {
                    if (!IsCleared(s.Id))
                    {
                        return s;
                    }
                }

                return Catalog.Stages.Count > 0 ? Catalog.Stages[Catalog.Stages.Count - 1] : null;
            }
        }

        public BattleSetup Setup(StageDef stage)
        {
            var setup = new BattleSetup(Catalog, Roster, stage, IsUnlocked(stage.Id));
            setup.AutoFill();
            return setup;
        }

        public int Tick()
        {
            var n = Roster.Tick();
            if (n > 0)
            {
                Changed?.Invoke();
            }

            return n;
        }

        /// <summary>Pays out a finished battle: wallet, shards, stage clear, win count. Call once per result.</summary>
        public RewardBundle ApplyResult(BattleReport result, StageDef stage)
        {
            var first = result.Won && !stage.Id.StartsWith("camp:", StringComparison.Ordinal) && !IsCleared(stage.Id);
            var bundle = BattleRewards.For(stage, result.Won, first, Catalog.LossGoldShare);
            Wallet.Grant(bundle.Resources);
            if (bundle.Shards > 0)
            {
                Roster.AddShards(bundle.ShardHero, bundle.Shards);
            }

            Battles++;
            if (result.Won)
            {
                Wins++;
                if (!stage.Id.StartsWith("camp:", StringComparison.Ordinal))
                {
                    _cleared.Add(stage.Id);
                }
            }

            Changed?.Invoke();
            return bundle;
        }

        public BountyDef? CurrentBounty => BountyIndex < Catalog.Bounties.Count ? Catalog.Bounties[BountyIndex] : null;

        public int BountyProgress(BountyDef b) => Math.Min(b.Count, b.Kind switch
        {
            "win" => Wins,
            "clear" => IsCleared(b.Target) ? 1 : 0,
            "hero_level" => Roster.TopLevel(),
            _ => 0
        });

        public bool BountyReady => CurrentBounty is { } b && BountyProgress(b) >= b.Count;

        public IReadOnlyDictionary<string, int>? ClaimBounty()
        {
            var b = CurrentBounty;
            if (b == null || BountyProgress(b) < b.Count)
            {
                return null;
            }

            Wallet.Grant(b.Rewards);
            BountyIndex++;
            Changed?.Invoke();
            return b.Rewards;
        }

        // ---------- save ----------

        public string Save() => CampaignSave.Write(this);

        /// <summary>Loads a save written by <see cref="Save"/>. Unknown heroes or stages are skipped; a broken file changes nothing.</summary>
        public bool Load(string json) => CampaignSave.Read(this, json);

        internal void RestoreCounters(int wins, int battles, int bountyIndex, IEnumerable<string> cleared)
        {
            Wins = Math.Max(0, wins);
            Battles = Math.Max(Wins, battles);
            BountyIndex = Math.Max(0, Math.Min(bountyIndex, Catalog.Bounties.Count));
            _cleared.Clear();
            foreach (var c in cleared)
            {
                if (Catalog.Stage(c) != null)
                {
                    _cleared.Add(c);
                }
            }
        }

        /// <summary>Resource ids the save keeps for the wallet.</summary>
        public static readonly string[] SavedResources =
        {
            SurvIds.ResSoft, SurvIds.ResHard, SurvIds.ResFood, SurvIds.ResWood, SurvIds.ResStone, SurvIds.ResSpeedupM
        };
    }
}
