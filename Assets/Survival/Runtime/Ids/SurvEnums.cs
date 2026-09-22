namespace Survival.Domain.Ids
{
    public enum SysModule
    {
        Base,
        Research,
        Heroes,
        Alliance,
        World,
        Battles,
        Energy,
        Events,
        Iap,
        Ftue,
        Meta
    }

    public enum WorldNodeType
    {
        Home,
        Gather,
        Build,
        Guild,
        Fight,
        Explore
    }

    public enum HeroArchetype
    {
        Tank,
        Dps,
        Support,
        Ranged
    }

    public enum HeroRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum BattleMode
    {
        Pve,
        Pvp
    }

    public enum BattleResult
    {
        Win,
        Lose,
        Flee
    }

    public enum FtueStep
    {
        Boot,
        PlaceHq,
        FirstBuild,
        FirstMarch,
        FirstBattle,
        AllianceIntro,
        Complete
    }

    public static class SurvEnums
    {
        public static string ToId(this SysModule value) => value switch
        {
            SysModule.Base => SurvIds.SysBase,
            SysModule.Research => SurvIds.SysResearch,
            SysModule.Heroes => SurvIds.SysHeroes,
            SysModule.Alliance => SurvIds.SysAlliance,
            SysModule.World => SurvIds.SysWorld,
            SysModule.Battles => SurvIds.SysBattles,
            SysModule.Energy => SurvIds.SysEnergy,
            SysModule.Events => SurvIds.SysEvents,
            SysModule.Iap => SurvIds.SysIap,
            SysModule.Ftue => SurvIds.SysFtue,
            SysModule.Meta => SurvIds.SysMeta,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static string ToId(this WorldNodeType value) => value switch
        {
            WorldNodeType.Home => SurvIds.WorldNodeTypeHome,
            WorldNodeType.Gather => SurvIds.WorldNodeTypeGather,
            WorldNodeType.Build => SurvIds.WorldNodeTypeBuild,
            WorldNodeType.Guild => SurvIds.WorldNodeTypeGuild,
            WorldNodeType.Fight => SurvIds.WorldNodeTypeFight,
            WorldNodeType.Explore => SurvIds.WorldNodeTypeExplore,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static WorldNodeType ParseWorldNodeType(string id) => id switch
        {
            SurvIds.WorldNodeTypeHome => WorldNodeType.Home,
            SurvIds.WorldNodeTypeGather => WorldNodeType.Gather,
            SurvIds.WorldNodeTypeBuild => WorldNodeType.Build,
            SurvIds.WorldNodeTypeGuild => WorldNodeType.Guild,
            SurvIds.WorldNodeTypeFight => WorldNodeType.Fight,
            SurvIds.WorldNodeTypeExplore => WorldNodeType.Explore,
            _ => throw new System.ArgumentOutOfRangeException(nameof(id), id, "Unknown world.node_type.*")
        };

        public static string ToId(this HeroArchetype value) => value switch
        {
            HeroArchetype.Tank => SurvIds.HeroArchetypeTank,
            HeroArchetype.Dps => SurvIds.HeroArchetypeDps,
            HeroArchetype.Support => SurvIds.HeroArchetypeSupport,
            HeroArchetype.Ranged => SurvIds.HeroArchetypeRanged,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static string ToId(this HeroRarity value) => value switch
        {
            HeroRarity.Common => SurvIds.HeroRarityCommon,
            HeroRarity.Rare => SurvIds.HeroRarityRare,
            HeroRarity.Epic => SurvIds.HeroRarityEpic,
            HeroRarity.Legendary => SurvIds.HeroRarityLegendary,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static string ToId(this BattleMode value) => value switch
        {
            BattleMode.Pve => SurvIds.BattleModePve,
            BattleMode.Pvp => SurvIds.BattleModePvp,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static string ToId(this BattleResult value) => value switch
        {
            BattleResult.Win => SurvIds.BattleResultWin,
            BattleResult.Lose => SurvIds.BattleResultLose,
            BattleResult.Flee => SurvIds.BattleResultFlee,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static string ToId(this FtueStep value) => value switch
        {
            FtueStep.Boot => SurvIds.FtueStepBoot,
            FtueStep.PlaceHq => SurvIds.FtueStepPlaceHq,
            FtueStep.FirstBuild => SurvIds.FtueStepFirstBuild,
            FtueStep.FirstMarch => SurvIds.FtueStepFirstMarch,
            FtueStep.FirstBattle => SurvIds.FtueStepFirstBattle,
            FtueStep.AllianceIntro => SurvIds.FtueStepAllianceIntro,
            FtueStep.Complete => SurvIds.FtueFlagComplete,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}
