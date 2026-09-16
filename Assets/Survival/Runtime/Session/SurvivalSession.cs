using System.Collections.Generic;
using Survival.Domain.Alliance;
using Survival.Domain.Base;
using Survival.Domain.Battles;
using Survival.Domain.Catalog;
using Survival.Domain.Energy;
using Survival.Domain.Events;
using Survival.Domain.Flavor;
using Survival.Domain.Ftue;
using Survival.Domain.Heroes;
using Survival.Domain.Iap;
using Survival.Domain.Meta;
using Survival.Domain.Research;
using Survival.Domain.Theme;
using Survival.Domain.World;

namespace Survival.Domain.Session
{
    /// <summary>Shared survival_core runtime: one session, baked flavor + ThemePack.</summary>
    public sealed class SurvivalSession
    {
        public SurvivalSession(AppFlavorConfig flavor, ThemePackBinder pack, SurvivalCatalog catalog)
        {
            Flavor = flavor;
            Pack = pack;
            Catalog = catalog;
            flavor.Validate();
            Base = new BaseSystem(catalog);
            Research = new ResearchSystem(catalog);
            Heroes = new HeroSystem(catalog);
            Alliance = new AllianceSystem(catalog);
            World = new WorldMap(catalog, pack);
            Battles = new BattleSystem(catalog);
            Energy = new EnergySystem(catalog.Energy);
            Events = new EventSystem(catalog);
            Iap = new IapSystem(catalog);
            Ftue = new FtueSystem(catalog);
            Meta = new MetaSystem();
            Chips = new ChipWallet(new Dictionary<string, int>
            {
                [ChipWallet.SoftChip] = ParseChip(pack, "hud.chip.gold_amount", 245600),
                [ChipWallet.StoneChip] = ParseChip(pack, "hud.chip.stone_amount", 96700),
                ["res_wood"] = ParseChip(pack, "hud.chip.wood_amount", 178300),
                ["res_food"] = ParseChip(pack, "hud.chip.food_amount", 64200)
            });
        }

        public AppFlavorConfig Flavor { get; }
        public ThemePackBinder Pack { get; }
        public SurvivalCatalog Catalog { get; }
        public BaseSystem Base { get; }
        public ResearchSystem Research { get; }
        public HeroSystem Heroes { get; }
        public AllianceSystem Alliance { get; }
        public WorldMap World { get; }
        public BattleSystem Battles { get; }
        public EnergySystem Energy { get; }
        public EventSystem Events { get; }
        public IapSystem Iap { get; }
        public FtueSystem Ftue { get; }
        public MetaSystem Meta { get; }
        public ChipWallet Chips { get; }

        private static int ParseChip(ThemePackBinder pack, string key, int fallback)
        {
            var raw = pack.StringOr(key, "");
            return int.TryParse(raw, out var n) ? n : fallback;
        }
    }
}
