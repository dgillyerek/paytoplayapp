using System;
using System.Collections.Generic;

namespace Survival.Domain.Ids
{
    /// <summary>
    /// SURV-P0 stable IDs. Logic and data tables must use these strings only.
    /// Display names live in the baked ThemePack, never here.
    /// </summary>
    public static class SurvIds
    {
        public const string SystemsApi = "survival_core_v1";
        public const string SurvivalCoreApi = "survival_core.api";
        public const string PackSystemsApi = "pack.systems_api";
        public const string PackPath = "pack.path";

        public const string ThemeIdFantasyKingdomA = "theme.id.fantasy_kingdom_a";
        public const string FlavorIdFantasyKingdomA = "flavor.id.fantasy_kingdom_a";
        public const string FlavorBundleId = "flavor.bundle_id";
        public const string FlavorAppName = "flavor.app_name";
        public const string FlavorIcon = "flavor.icon";
        public const string FlavorSplash = "flavor.splash";
        public const string FlavorThemeId = "flavor.theme_id";

        public const string SysBase = "sys.base";
        public const string SysResearch = "sys.research";
        public const string SysHeroes = "sys.heroes";
        public const string SysAlliance = "sys.alliance";
        public const string SysWorld = "sys.world";
        public const string SysBattles = "sys.battles";
        public const string SysEnergy = "sys.energy";
        public const string SysEvents = "sys.events";
        public const string SysIap = "sys.iap";
        public const string SysFtue = "sys.ftue";
        public const string SysMeta = "sys.meta";

        public const string ResEnergy = "res.energy";
        public const string ResSoft = "res.soft";
        public const string ResHard = "res.hard";
        public const string ResSpeedupM = "res.speedup_m";
        public const string ResHeroXp = "res.hero_xp";
        public const string ResAllianceGift = "res.alliance_gift";
        public const string ResEventScore = "res.event_score";

        public const string BuildingHq = "building.hq";
        public const string BuildingBarracks = "building.barracks";
        public const string BuildingEconomy = "building.economy";
        public const string BuildingResearch = "building.research";
        public const string BuildingHeroHall = "building.hero_hall";
        public const string BuildingWall = "building.wall";
        public const string BuildingAlliance = "building.alliance";
        public const string BuildingWarehouse = "building.warehouse";
        public const string BuildingWorkshop = "building.workshop";

        public const string BaseQueueBuild = "base.queue.build";
        public const string BaseQueueUpgrade = "base.queue.upgrade";
        public const string BaseActionPlace = "base.action.place";
        public const string BaseActionUpgrade = "base.action.upgrade";
        public const string BaseActionCollect = "base.action.collect";

        public const string ResearchOffense1 = "research.offense_1";
        public const string ResearchDefense1 = "research.defense_1";
        public const string ResearchEconomy1 = "research.economy_1";
        public const string ResearchMarch1 = "research.march_1";
        public const string ResearchHero1 = "research.hero_1";

        public const string HeroSlot01 = "hero.slot_01";
        public const string HeroArchetypeTank = "hero.archetype.tank";
        public const string HeroArchetypeDps = "hero.archetype.dps";
        public const string HeroArchetypeSupport = "hero.archetype.support";
        public const string HeroArchetypeRanged = "hero.archetype.ranged";
        public const string HeroRarityCommon = "hero.rarity.common";
        public const string HeroRarityRare = "hero.rarity.rare";
        public const string HeroRarityEpic = "hero.rarity.epic";
        public const string HeroRarityLegendary = "hero.rarity.legendary";
        public const string HeroStatAtk = "hero.stat.atk";
        public const string HeroStatHp = "hero.stat.hp";
        public const string HeroStatDef = "hero.stat.def";
        public const string HeroStatPower = "hero.stat.power";

        public const string AllianceOrg = "alliance.org";
        public const string AllianceRankLeader = "alliance.rank.leader";
        public const string AllianceRankOfficer = "alliance.rank.officer";
        public const string AllianceRankMember = "alliance.rank.member";
        public const string AllianceActionCreate = "alliance.action.create";
        public const string AllianceActionJoin = "alliance.action.join";
        public const string AllianceActionLeave = "alliance.action.leave";
        public const string AllianceActionKick = "alliance.action.kick";
        public const string AllianceChat = "alliance.chat";
        public const string AllianceGift = "alliance.gift";
        public const string AllianceWar = "alliance.war";

        public const string WorldNodeTypeHome = "world.node_type.home";
        public const string WorldNodeTypeGather = "world.node_type.gather";
        public const string WorldNodeTypeBuild = "world.node_type.build";
        public const string WorldNodeTypeGuild = "world.node_type.guild";
        public const string WorldNodeTypeFight = "world.node_type.fight";
        public const string WorldNodeTypeExplore = "world.node_type.explore";
        public const string WorldNodeHome01 = "world.node.home_01";
        public const string WorldNodeGather01 = "world.node.gather_01";
        public const string WorldNodeBuild01 = "world.node.build_01";
        public const string WorldNodeGuild01 = "world.node.guild_01";
        public const string WorldNodeFight01 = "world.node.fight_01";
        public const string WorldNodeExplore01 = "world.node.explore_01";
        public const string WorldMapCampaign = "world.map.campaign";
        public const string WorldActionMarch = "world.action.march";
        public const string WorldActionScout = "world.action.scout";
        public const string WorldActionGather = "world.action.gather";
        public const string WorldFog = "world.fog";

        public const string BattleModePve = "battle.mode.pve";
        public const string BattleModePvp = "battle.mode.pvp";
        public const string BattleFormationFront = "battle.formation.front";
        public const string BattleFormationMid = "battle.formation.mid";
        public const string BattleFormationBack = "battle.formation.back";
        public const string BattleResultWin = "battle.result.win";
        public const string BattleResultLose = "battle.result.lose";
        public const string BattleResultFlee = "battle.result.flee";
        public const string BattleActionStart = "battle.action.start";
        public const string BattleActionAuto = "battle.action.auto";
        public const string BattleActionSkip = "battle.action.skip";

        public const string EnergyMeterMain = "energy.meter.main";
        public const string EnergyActionMarch = "energy.action.march";
        public const string EnergyActionBattle = "energy.action.battle";
        public const string EnergyActionGather = "energy.action.gather";
        public const string EnergyActionBuildRush = "energy.action.build_rush";

        public const string EventTypeSiege = "event.type.siege";
        public const string EventTypeGuildCrusade = "event.type.guild_crusade";
        public const string EventTypeBanner = "event.type.banner";

        public const string SkuEnergyS = "sku.energy_s";
        public const string SkuEnergyM = "sku.energy_m";
        public const string SkuEnergyL = "sku.energy_l";
        public const string SkuHardS = "sku.hard_s";
        public const string SkuHardM = "sku.hard_m";
        public const string SkuHardL = "sku.hard_l";
        public const string SkuHardXl = "sku.hard_xl";
        public const string SkuSpeedupBundle = "sku.speedup_bundle";
        public const string SkuStarterPack = "sku.starter_pack";
        public const string SkuPassSeason = "sku.pass_season";

        public const string FtueStepBoot = "ftue.step.boot";
        public const string FtueStepPlaceHq = "ftue.step.place_hq";
        public const string FtueStepFirstBuild = "ftue.step.first_build";
        public const string FtueStepFirstMarch = "ftue.step.first_march";
        public const string FtueStepFirstBattle = "ftue.step.first_battle";
        public const string FtueStepAllianceIntro = "ftue.step.alliance_intro";
        public const string FtueFlagComplete = "ftue.flag.complete";

        public const string ThemeABuildingHqKeep = "theme_a.building.hq.keep";
        public const string ThemeAHeroKnight01 = "theme_a.hero.knight_01";
        public const string ThemeAThreatDarkness01 = "theme_a.threat.darkness_01";
        public const string ThemeAWorldChipDarkKeep = "theme_a.world.chip.dark_keep";
        public const string ThemeAStoreIcon = "theme_a.store.icon";
        public const string ThemeAAllianceLabel = "theme_a.alliance.label";
        public const string ThemeAMapLabel = "theme_a.map.label";
        public const string ThemeANodeHomeCottage = "theme_a.node.home_cottage";
        public const string ThemeANodeGatherQuarry = "theme_a.node.gather_quarry";
        public const string ThemeANodeGatherQuarryMap = "theme_a.node.gather_quarry_map";
        public const string ThemeANodeBuildOutpost = "theme_a.node.build_outpost";
        public const string ThemeANodeGuildFort = "theme_a.node.guild_fort";
        public const string ThemeANodeFightDarkKeep = "theme_a.node.fight_dark_keep";
        public const string ThemeANodeExploreRuins = "theme_a.node.explore_ruins";

        public static readonly IReadOnlyList<string> Modules = new[]
        {
            SysBase, SysResearch, SysHeroes, SysAlliance, SysWorld, SysBattles,
            SysEnergy, SysEvents, SysIap, SysFtue, SysMeta
        };

        public static readonly IReadOnlyList<string> Resources = new[]
        {
            ResEnergy, ResSoft, ResHard, ResSpeedupM, ResHeroXp, ResAllianceGift, ResEventScore
        };

        public static readonly IReadOnlyList<string> Buildings = new[]
        {
            BuildingHq, BuildingBarracks, BuildingEconomy, BuildingResearch, BuildingHeroHall,
            BuildingWall, BuildingAlliance, BuildingWarehouse, BuildingWorkshop
        };

        public static readonly IReadOnlyList<string> WorldNodeTypes = new[]
        {
            WorldNodeTypeHome, WorldNodeTypeGather, WorldNodeTypeBuild,
            WorldNodeTypeGuild, WorldNodeTypeFight, WorldNodeTypeExplore
        };

        public static readonly IReadOnlyList<string> WorldNodeInstances = new[]
        {
            WorldNodeHome01, WorldNodeGather01, WorldNodeBuild01,
            WorldNodeGuild01, WorldNodeFight01, WorldNodeExplore01
        };

        public static readonly IReadOnlyDictionary<string, string> WorldNodeInstanceTypes =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WorldNodeHome01] = WorldNodeTypeHome,
                [WorldNodeGather01] = WorldNodeTypeGather,
                [WorldNodeBuild01] = WorldNodeTypeBuild,
                [WorldNodeGuild01] = WorldNodeTypeGuild,
                [WorldNodeFight01] = WorldNodeTypeFight,
                [WorldNodeExplore01] = WorldNodeTypeExplore
            };

        public static readonly IReadOnlyDictionary<string, string> WorldNodeTypeThemeAKeys =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WorldNodeTypeHome] = ThemeANodeHomeCottage,
                [WorldNodeTypeGather] = ThemeANodeGatherQuarry,
                [WorldNodeTypeBuild] = ThemeANodeBuildOutpost,
                [WorldNodeTypeGuild] = ThemeANodeGuildFort,
                [WorldNodeTypeFight] = ThemeANodeFightDarkKeep,
                [WorldNodeTypeExplore] = ThemeANodeExploreRuins
            };

        public static readonly IReadOnlyList<string> ForbiddenPrefixes = new[]
        {
            "grove.", "merge.", "maya."
        };

        public static readonly IReadOnlyList<string> ForbiddenIds = new[]
        {
            "runtime_theme_switch", "player_selectable_skin"
        };

        public static bool IsForbidden(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return true;
            }

            for (var i = 0; i < ForbiddenIds.Count; i++)
            {
                if (string.Equals(id, ForbiddenIds[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            for (var i = 0; i < ForbiddenPrefixes.Count; i++)
            {
                if (id.StartsWith(ForbiddenPrefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
