#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Base;
using Survival.Domain.Catalog;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Energy;
using Survival.Domain.Gift;
using Survival.Domain.Ids;
using Survival.Domain.Quest;
using Survival.Domain.Store;

namespace Survival.Domain.Session
{
    /// <summary>
    /// The "hook" slice runtime: one wallet, one clock, Castle/Farm/Lumber Mill production + upgrades,
    /// starter quests, free gifts and the sandbox Gem Shop. Views only call into this.
    /// </summary>
    public sealed class HookSession
    {
        public HookSession(
            HookCatalog catalog,
            Wallet wallet,
            IGameClock clock,
            IReadOnlyDictionary<string, ProductDef> products,
            ISkuPurchaseService purchases,
            EnergySystem? energy = null,
            int giftSeed = 0)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Energy = energy;
            Wallet.Grant(catalog.StartingWallet);
            Timers = new TimerQueue(clock, catalog.Rush, catalog.BuildSlots);
            Base = new BaseEconomy(catalog.Plots, clock, wallet, Timers);
            Build = new VillageBuild(Base, catalog.Buildables);
            Quests = new QuestSystem(catalog.StarterChain, Base.LevelOf);
            Gifts = new FreeGiftScheduler(catalog.Gifts, clock, giftSeed);
            Store = new StoreSystem(catalog.Stores, products, purchases, wallet);

            Base.Collected += (plot, _, amount) =>
                Quests.Report(SurvIds.QuestObjectiveCollect, plot.Def.BuildingId, 1);
            Base.LevelReached += (plot, level) => Quests.ReportLevel(plot.Def.BuildingId, level);
        }

        public HookCatalog Catalog { get; }
        public Wallet Wallet { get; }
        public IGameClock Clock { get; }
        public EnergySystem? Energy { get; }
        public TimerQueue Timers { get; }
        public BaseEconomy Base { get; }

        /// <summary>Village Build (new Farms / Lumber Mills / Quarries / Forges on free spots).</summary>
        public VillageBuild Build { get; }
        public QuestSystem Quests { get; }
        public FreeGiftScheduler Gifts { get; }
        public StoreSystem Store { get; }

        /// <summary>Builds / upgrades whose timer ran out complete on their own (see <see cref="BaseEconomy.CompleteDueUpgrades"/>).</summary>
        public void Tick()
        {
            Base.CompleteDueUpgrades();
            Base.Tick();
            Gifts.Tick();
        }

        /// <summary>World march resolved on a node (gather quarry counts for quest step 2).</summary>
        public void ReportMarch(string nodeId, string nodeType)
        {
            if (string.Equals(nodeType, SurvIds.WorldNodeTypeGather, StringComparison.Ordinal))
            {
                Quests.Report(SurvIds.QuestObjectiveGather, nodeId, 1);
            }
        }

        /// <summary>Claim the pending free gift into the wallet (energy goes to the energy meter).</summary>
        public PendingGift? ClaimFreeGift()
        {
            var gift = Gifts.Claim();
            if (gift == null)
            {
                return null;
            }

            if (string.Equals(gift.Resource, SurvIds.ResEnergy, StringComparison.Ordinal) && Energy != null)
            {
                Energy.Grant(gift.Amount);
            }
            else
            {
                Wallet.Add(gift.Resource, gift.Amount);
            }

            return gift;
        }

        public bool FreeAvailable(string storeId) =>
            Gifts.HasPending && string.Equals(Gifts.Config.StoreId, storeId, StringComparison.Ordinal);

        /// <summary>Finish the plot's upgrade timer for free (inside the window) and level up.</summary>
        public int FinishFree(string plotId)
        {
            var timerId = BaseEconomy.UpgradeTimerId(plotId);
            if (!Timers.IsDone(timerId) && !Timers.TryFinishFree(timerId))
            {
                return 0;
            }

            return Base.TryFinishUpgrade(plotId);
        }

        /// <summary>How long a hired temporary builder stays (12 hours of real time).</summary>
        public const double TempBuilderSeconds = 12 * 3600;

        /// <summary>
        /// Offer the $1.99 temporary builder: every builder is busy (the tap was blocked only by the queue) and no
        /// temporary builder is on the job right now. Once it leaves it can be hired again.
        /// </summary>
        public bool TempBuilderOffered(string plotId) =>
            !Timers.ExtraSlotActive && Base.CheckUpgrade(plotId) == UpgradeBlock.QueueBusy;

        /// <summary>The same offer for a new building (Build) blocked only by busy builders.</summary>
        public bool TempBuilderOfferedForBuild(string type, string? spotId = null) =>
            !Timers.ExtraSlotActive && Build.Check(type, spotId) == BuildBlock.QueueBusy;

        /// <summary>The temporary builder's store row (price label, product id), or null when the theme pack lacks it.</summary>
        public ProductDef? TempBuilderProduct => Store.ProductFor(SurvIds.SkuTempBuilder);

        /// <summary>
        /// Buy the temporary builder through the (sandbox) purchase service: on success one extra builder for
        /// <see cref="TempBuilderSeconds"/>. Refused while one is still hired.
        /// </summary>
        public void HireTempBuilder(Action<PurchaseOutcome> done)
        {
            var product = TempBuilderProduct;
            if (product == null)
            {
                done(new PurchaseOutcome(false, "", "", "store.unknown_offer"));
                return;
            }

            if (Timers.ExtraSlotActive)
            {
                done(new PurchaseOutcome(false, product.ProductId, "", "store.already_active"));
                return;
            }

            Store.Purchases.Purchase(product.ProductId, outcome =>
            {
                if (outcome.Ok)
                {
                    Timers.ExtraSlotUntil = Clock.Now + TempBuilderSeconds;
                }

                done(outcome);
            });
        }

        /// <summary>Hire the temporary builder, then start the upgrade that asked for it. Ok only when both happened.</summary>
        public void HireTempBuilderAndUpgrade(string plotId, Action<bool> done) =>
            HireTempBuilder(o => done(o.Ok && Base.TryStartUpgrade(plotId)));

        /// <summary>Hire the temporary builder, then place the building that asked for it. Ok only when both happened.</summary>
        public void HireTempBuilderAndBuild(string type, string? spotId, Action<BasePlot?> done) =>
            HireTempBuilder(o => done(o.Ok ? Build.TryBuild(type, spotId) : null));

        /// <summary>Gem rush the plot's upgrade and level up. Returns the new level, or 0.</summary>
        public int Rush(string plotId, out int gemsSpent)
        {
            gemsSpent = 0;
            var timerId = BaseEconomy.UpgradeTimerId(plotId);
            if (!Timers.IsDone(timerId) && !Timers.TryRush(timerId, Wallet, out gemsSpent))
            {
                return 0;
            }

            return Base.TryFinishUpgrade(plotId);
        }
    }
}
