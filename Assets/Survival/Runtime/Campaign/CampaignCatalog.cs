#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Serialization;

namespace Survival.Domain.Campaign
{
    /// <summary>Which side of the battle a campaign character stands on (mirrors the BattleLocked roster sides).</summary>
    public enum CampaignSide
    {
        Hero = 0,
        Villain = 1
    }

    /// <summary>
    /// One battle character (campaign.json "heroes"). <see cref="Fighter"/> is the BattleLocked roster name, so the
    /// fight spawns that character's locked rig, clips, three-attack rotation and hit reactions.
    /// </summary>
    public sealed record CampaignHeroDef(
        string Id,
        string Fighter,
        CampaignSide Side,
        string Rarity,
        string Role,
        int BaseHp,
        int BaseAtk,
        int HpPerLevel,
        int AtkPerLevel,
        bool StartOwned,
        int StartLevel,
        int StartStars,
        int UnlockGems,
        int UnlockShards,
        string PortraitKey)
    {
        public bool Purchasable => Side == CampaignSide.Hero && (UnlockGems > 0 || UnlockShards > 0);
    }

    public sealed record StageEnemyDef(string HeroId, int Level, int Stars = 1);

    /// <summary>A battle the player can set up: up to three lanes of villains, rewards and first-clear gems.</summary>
    public sealed record StageDef(
        string Id,
        string Name,
        IReadOnlyList<StageEnemyDef> Enemies,
        IReadOnlyDictionary<string, int> Rewards,
        string ShardHero,
        int Shards,
        int FirstClearGems,
        string? CampId = null);

    public sealed record CampEncounterDef(string Type, IReadOnlyList<string> Enemies, string ShardHero, int Shards);

    /// <summary>Battle bounty: "win" (count wins), "clear" (clear target stage), "hero_level" (any hero reaches count).</summary>
    public sealed record BountyDef(string Id, string Kind, string Target, int Count, IReadOnlyDictionary<string, int> Rewards);

    /// <summary>Level-up costs and timers, star caps and shard costs.</summary>
    public sealed class LevelCurve
    {
        public int GoldBase { get; init; } = 2000;
        public double GoldGrowth { get; init; } = 1.45;
        public int FoodBase { get; init; } = 800;
        public double FoodGrowth { get; init; } = 1.42;
        public int SecondsBase { get; init; } = 30;
        public double SecondsGrowth { get; init; } = 1.6;
        public IReadOnlyList<int> StarLevelCaps { get; init; } = new[] { 10, 20, 30, 40, 50 };
        public IReadOnlyList<int> ShardsPerStar { get; init; } = new[] { 10, 20, 40, 80 };
        public double StarStatBonus { get; init; } = 0.15;
        public double PowerHp { get; init; } = 0.5;
        public double PowerAtk { get; init; } = 3.0;

        public int MaxStars => StarLevelCaps.Count;

        /// <summary>Highest level a hero with <paramref name="stars"/> stars can train to.</summary>
        public int LevelCap(int stars) => StarLevelCaps[Math.Max(0, Math.Min(StarLevelCaps.Count - 1, stars - 1))];

        /// <summary>Shards to go from <paramref name="stars"/> to stars + 1, or 0 at max stars.</summary>
        public int ShardsToStar(int stars) => stars >= 1 && stars - 1 < ShardsPerStar.Count ? ShardsPerStar[stars - 1] : 0;

        /// <summary>Gold to train from <paramref name="level"/> to level + 1.</summary>
        public int GoldFor(int level) => RoundCost(GoldBase * Math.Pow(GoldGrowth, Math.Max(0, level - 1)));

        public int FoodFor(int level) => RoundCost(FoodBase * Math.Pow(FoodGrowth, Math.Max(0, level - 1)));

        /// <summary>Training time from <paramref name="level"/> to level + 1, whole seconds.</summary>
        public int SecondsFor(int level) => (int)Math.Round(SecondsBase * Math.Pow(SecondsGrowth, Math.Max(0, level - 1)));

        private static int RoundCost(double v) => v >= 1000 ? (int)(Math.Round(v / 50.0) * 50) : (int)(Math.Round(v / 10.0) * 10);
    }

    /// <summary>campaign.json: heroes and villains, level curve, stages, camp encounters, bounties.</summary>
    public sealed class CampaignCatalog
    {
        public const string FileName = "campaign.json";

        private readonly Dictionary<string, CampaignHeroDef> _byId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StageDef> _stages = new(StringComparer.Ordinal);

        public CampaignCatalog(
            IReadOnlyList<CampaignHeroDef> heroes,
            IReadOnlyList<StageDef> stages,
            IReadOnlyDictionary<string, CampEncounterDef> camps,
            IReadOnlyList<BountyDef> bounties,
            LevelCurve curve,
            int trainingSlots = 2,
            double lossGoldShare = 0.25,
            double battleTimeoutSeconds = 120)
        {
            Heroes = heroes;
            Stages = stages;
            Camps = camps;
            Bounties = bounties;
            Curve = curve;
            TrainingSlots = Math.Max(1, trainingSlots);
            LossGoldShare = lossGoldShare;
            BattleTimeoutSeconds = battleTimeoutSeconds;
            foreach (var h in heroes)
            {
                _byId[h.Id] = h;
            }

            foreach (var s in stages)
            {
                _stages[s.Id] = s;
            }
        }

        public IReadOnlyList<CampaignHeroDef> Heroes { get; }
        public IReadOnlyList<StageDef> Stages { get; }
        public IReadOnlyDictionary<string, CampEncounterDef> Camps { get; }
        public IReadOnlyList<BountyDef> Bounties { get; }
        public LevelCurve Curve { get; }
        public int TrainingSlots { get; }
        public double LossGoldShare { get; }
        public double BattleTimeoutSeconds { get; }

        public IEnumerable<CampaignHeroDef> OnSide(CampaignSide side)
        {
            foreach (var h in Heroes)
            {
                if (h.Side == side)
                {
                    yield return h;
                }
            }
        }

        public CampaignHeroDef? Hero(string id) => _byId.TryGetValue(id, out var h) ? h : null;

        public CampaignHeroDef? ByFighter(string fighterName)
        {
            foreach (var h in Heroes)
            {
                if (string.Equals(h.Fighter, fighterName, StringComparison.Ordinal))
                {
                    return h;
                }
            }

            return null;
        }

        public StageDef? Stage(string id) => _stages.TryGetValue(id, out var s) ? s : null;

        public int StageIndex(string id)
        {
            for (var i = 0; i < Stages.Count; i++)
            {
                if (string.Equals(Stages[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// World map enemy camp as a battle: the camp type's villains (at most three lanes, fewer at low camp
        /// levels) at a level from the camp level, its loot as gold, and the type's shards.
        /// </summary>
        public StageDef CampStage(string campId, string type, int campLevel, int loot, string name)
        {
            Camps.TryGetValue(type, out var enc);
            var pool = enc?.Enemies ?? new[] { "foe.nightfang" };
            var lanes = Math.Max(1, Math.Min(pool.Count, campLevel <= 3 ? 1 : campLevel <= 6 ? 2 : 3));
            var level = Math.Max(1, (int)Math.Round(campLevel * 0.6));
            var enemies = new List<StageEnemyDef>();
            for (var i = 0; i < lanes; i++)
            {
                enemies.Add(new StageEnemyDef(pool[i], level));
            }

            var rewards = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["res.soft"] = Math.Max(0, loot),
                ["res.food"] = Math.Max(0, loot / 3)
            };
            return new StageDef("camp:" + campId, name, enemies, rewards, enc?.ShardHero ?? "", enc?.Shards ?? 0, 0, campId);
        }

        public static CampaignCatalog Parse(string json)
        {
            if (JsonRead.Parse(json) is not JsonNode.Obj root)
            {
                throw new FormatException("campaign.json: root must be an object.");
            }

            var curve = new LevelCurve();
            if (root.TryGet("level_curve", out var lc) && lc is JsonNode.Obj c)
            {
                curve = new LevelCurve
                {
                    GoldBase = Int(c, "gold_base", curve.GoldBase),
                    GoldGrowth = Num(c, "gold_growth", curve.GoldGrowth),
                    FoodBase = Int(c, "food_base", curve.FoodBase),
                    FoodGrowth = Num(c, "food_growth", curve.FoodGrowth),
                    SecondsBase = Int(c, "seconds_base", curve.SecondsBase),
                    SecondsGrowth = Num(c, "seconds_growth", curve.SecondsGrowth),
                    StarLevelCaps = Ints(c, "star_level_caps") ?? curve.StarLevelCaps,
                    ShardsPerStar = Ints(c, "shards_per_star") ?? curve.ShardsPerStar,
                    StarStatBonus = Num(c, "star_stat_bonus", curve.StarStatBonus),
                    PowerHp = Num(c, "power_hp", curve.PowerHp),
                    PowerAtk = Num(c, "power_atk", curve.PowerAtk)
                };
            }

            var heroes = new List<CampaignHeroDef>();
            foreach (var n in Items(root, "heroes"))
            {
                if (n is not JsonNode.Obj h)
                {
                    continue;
                }

                heroes.Add(new CampaignHeroDef(
                    Str(h, "id"),
                    Str(h, "fighter"),
                    Str(h, "side") == "villain" ? CampaignSide.Villain : CampaignSide.Hero,
                    Str(h, "rarity", "rare"),
                    Str(h, "role", ""),
                    Int(h, "hp", 800),
                    Int(h, "atk", 80),
                    Int(h, "hp_per_level", 90),
                    Int(h, "atk_per_level", 10),
                    Bool(h, "owned"),
                    Math.Max(1, Int(h, "level", 1)),
                    Math.Max(1, Int(h, "stars", 1)),
                    Int(h, "gems", 0),
                    Int(h, "shards", 0),
                    Str(h, "portrait", "")));
            }

            var stages = new List<StageDef>();
            foreach (var n in Items(root, "stages"))
            {
                if (n is not JsonNode.Obj s)
                {
                    continue;
                }

                var enemies = new List<StageEnemyDef>();
                foreach (var e in Items(s, "enemies"))
                {
                    if (e is JsonNode.Obj eo)
                    {
                        enemies.Add(new StageEnemyDef(Str(eo, "id"), Math.Max(1, Int(eo, "level", 1)), Math.Max(1, Int(eo, "stars", 1))));
                    }
                }

                stages.Add(new StageDef(Str(s, "id"), Str(s, "name", Str(s, "id")), enemies, Map(s, "rewards"), Str(s, "shard_hero", ""), Int(s, "shards", 0), Int(s, "first_gems", 0)));
            }

            var camps = new Dictionary<string, CampEncounterDef>(StringComparer.Ordinal);
            if (root.TryGet("camps", out var cn) && cn is JsonNode.Obj co)
            {
                foreach (var pair in co.Fields)
                {
                    if (pair.Value is not JsonNode.Obj camp)
                    {
                        continue;
                    }

                    var list = new List<string>();
                    foreach (var e in Items(camp, "enemies"))
                    {
                        if (e is JsonNode.Str es)
                        {
                            list.Add(es.Value);
                        }
                    }

                    camps[pair.Key] = new CampEncounterDef(pair.Key, list, Str(camp, "shard_hero", ""), Int(camp, "shards", 0));
                }
            }

            var bounties = new List<BountyDef>();
            foreach (var n in Items(root, "bounties"))
            {
                if (n is JsonNode.Obj b)
                {
                    bounties.Add(new BountyDef(Str(b, "id"), Str(b, "kind", "win"), Str(b, "target", ""), Math.Max(1, Int(b, "count", 1)), Map(b, "rewards")));
                }
            }

            return new CampaignCatalog(
                heroes,
                stages,
                camps,
                bounties,
                curve,
                Int(root, "training_slots", 2),
                Num(root, "loss_gold_share", 0.25),
                Num(root, "battle_timeout_seconds", 120));
        }

        internal static IEnumerable<JsonNode> Items(JsonNode.Obj o, string key) =>
            o.TryGet(key, out var n) && n is JsonNode.Arr a ? a.Items : (IEnumerable<JsonNode>)Array.Empty<JsonNode>();

        internal static string Str(JsonNode.Obj o, string key, string? fallback = null)
        {
            if (o.TryGet(key, out var n) && n is JsonNode.Str s)
            {
                return s.Value;
            }

            return fallback ?? throw new FormatException("campaign.json: missing string '" + key + "'.");
        }

        internal static int Int(JsonNode.Obj o, string key, int fallback) =>
            o.TryGet(key, out var n) && n is JsonNode.Num v ? (int)Math.Round(v.Value) : fallback;

        internal static double Num(JsonNode.Obj o, string key, double fallback) =>
            o.TryGet(key, out var n) && n is JsonNode.Num v ? v.Value : fallback;

        internal static bool Bool(JsonNode.Obj o, string key) => o.TryGet(key, out var n) && n is JsonNode.Bool b && b.Value;

        internal static IReadOnlyList<int>? Ints(JsonNode.Obj o, string key)
        {
            if (!o.TryGet(key, out var n) || n is not JsonNode.Arr a)
            {
                return null;
            }

            var list = new List<int>();
            foreach (var i in a.Items)
            {
                if (i is JsonNode.Num v)
                {
                    list.Add((int)Math.Round(v.Value));
                }
            }

            return list;
        }

        internal static IReadOnlyDictionary<string, int> Map(JsonNode.Obj o, string key)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            if (o.TryGet(key, out var n) && n is JsonNode.Obj m)
            {
                foreach (var pair in m.Fields)
                {
                    if (pair.Value is JsonNode.Num v)
                    {
                        map[pair.Key] = (int)Math.Round(v.Value);
                    }
                }
            }

            return map;
        }
    }
}
