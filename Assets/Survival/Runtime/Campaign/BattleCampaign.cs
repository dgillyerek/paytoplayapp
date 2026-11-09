#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Serialization;

namespace Survival.Domain.Campaign
{
    /// <summary>One campaign chapter (campaign.json "battle_campaign.chapters"): its name, the Castle level that opens it,
    /// the villain that guards its boss stages, the hero whose shards its bosses drop and its clear chest.</summary>
    public sealed record ChapterDef(
        int Number,
        string Name,
        int CastleLevel,
        string Boss,
        string ShardHero,
        IReadOnlyDictionary<string, int> Chest);

    /// <summary>
    /// The long battle campaign, generated from a handful of numbers (no hand-made data per stage). Stage n (1-based over
    /// the whole campaign, n = 1..N) fights villains at
    ///   level  = round(LevelStart + (LevelEnd - LevelStart) * t^LevelCurve), t = (n - 1) / (N - 1)
    ///   stars  = 1 + floor(t * (StarsEnd - 1))
    /// in 2 lanes for the first stages of chapter 1, then 3. Every <see cref="BossEvery"/>th stage is a boss: the chapter's
    /// boss villain in the middle lane at +BossLevels levels and +1 star. The chapter's villain pool grows by one villain a
    /// chapter in <see cref="VillainOrder"/> order, so Ashwyrm joins the regular mix once the order reaches him.
    /// Rewards grow by <see cref="RewardGrowth"/> a stage; a replay pays <see cref="ReplayShare"/> of the resources and no
    /// gems or shards.
    /// </summary>
    public sealed class BattleCampaignDef
    {
        public int StagesPerChapter { get; init; } = 10;
        public int BossEvery { get; init; } = 5;
        public int LevelStart { get; init; } = 8;
        public int LevelEnd { get; init; } = 50;
        public double LevelCurve { get; init; } = 1.0;
        public int StarsEnd { get; init; } = 5;
        public int BossLevels { get; init; } = 2;
        public int TwoLaneStages { get; init; } = 4;
        public IReadOnlyList<string> VillainOrder { get; init; } = Array.Empty<string>();
        public int StartPool { get; init; } = 3;
        public int GoldStart { get; init; } = 16000;
        public double RewardGrowth { get; init; } = 1.045;
        public double FoodShare { get; init; } = 0.4;
        public double WoodShare { get; init; } = 0.3;
        public double StoneShare { get; init; } = 0.3;
        public int FirstGems { get; init; } = 20;
        public int BossGems { get; init; } = 60;
        public int BossShards { get; init; } = 4;
        public int BossSpeedupMinutes { get; init; } = 15;
        public double ReplayShare { get; init; } = 0.25;
        public IReadOnlyList<ChapterDef> Chapters { get; init; } = Array.Empty<ChapterDef>();

        public int StageCount => Chapters.Count * StagesPerChapter;

        /// <summary>0..1 position of global stage <paramref name="n"/> (1-based) along the campaign.</summary>
        public double T(int n) => StageCount <= 1 ? 0 : Math.Max(0, Math.Min(1, (n - 1) / (double)(StageCount - 1)));

        public int FoeLevel(int n) => (int)Math.Round(LevelStart + (LevelEnd - LevelStart) * Math.Pow(T(n), LevelCurve));

        public int FoeStars(int n) => Math.Min(StarsEnd, 1 + (int)Math.Floor(T(n) * (StarsEnd - 1) + 1e-9));

        public bool IsBoss(int n) => BossEvery > 0 && n % BossEvery == 0;

        /// <summary>Campaign id of the milestone end boss (Vorgath). Not part of the regular villain pool.</summary>
        public const string VorgathId = "foe.vorgath";

        /// <summary>
        /// Chapter finales are already bosses (<see cref="BossEvery"/> is 5 and a chapter is 10 stages, so stage 10 is
        /// the named Boss, stage 5 an Elite). Vorgath takes that finale. Elites keep the chapter's own villain.
        /// </summary>
        public bool IsFinaleBoss(int n) => IsBoss(n) && StagesPerChapter > 0 && n % StagesPerChapter == 0;

        public int Lanes(int n) => n <= TwoLaneStages ? 2 : 3;

        public int Gold(int n) => Round(GoldStart * Math.Pow(RewardGrowth, n - 1) * (IsBoss(n) ? 2 : 1));

        /// <summary>The villains chapter <paramref name="chapter"/> (1-based) draws from.</summary>
        public IReadOnlyList<string> Pool(int chapter)
        {
            var count = Math.Max(1, Math.Min(VillainOrder.Count, StartPool + chapter - 1));
            var list = new List<string>();
            for (var i = 0; i < count; i++)
            {
                list.Add(VillainOrder[i]);
            }

            return list;
        }

        public static string StageId(int chapter, int stage) => "stage.c" + chapter.ToString("00") + "." + stage.ToString("00");

        /// <summary>Every campaign stage in order (chapter 1 stage 1 first).</summary>
        public List<StageDef> Generate()
        {
            var list = new List<StageDef>();
            for (var c = 0; c < Chapters.Count; c++)
            {
                var ch = Chapters[c];
                var pool = Pool(ch.Number);
                for (var s = 1; s <= StagesPerChapter; s++)
                {
                    var n = (c * StagesPerChapter) + s;
                    list.Add(Stage(ch, pool, n, s));
                }
            }

            return list;
        }

        private StageDef Stage(ChapterDef ch, IReadOnlyList<string> pool, int n, int s)
        {
            var level = FoeLevel(n);
            var stars = FoeStars(n);
            var boss = IsBoss(n);
            var finale = IsFinaleBoss(n);
            var bossId = finale ? VorgathId : ch.Boss;
            var lanes = Lanes(n);
            // Rotate through the chapter's pool (never the boss twice), topping up from the full villain order if the pool is short.
            var picks = new List<string>();
            var start = (n * 7) % Math.Max(1, pool.Count);
            for (var i = 0; picks.Count < lanes && i < pool.Count; i++)
            {
                var id = pool[(start + i) % pool.Count];
                if (!(boss && id == bossId))
                {
                    picks.Add(id);
                }
            }

            foreach (var id in VillainOrder)
            {
                if (picks.Count >= lanes)
                {
                    break;
                }

                if (!picks.Contains(id) && !(boss && id == bossId))
                {
                    picks.Add(id);
                }
            }

            if (boss)
            {
                picks.Insert(Math.Min(1, picks.Count), bossId);
            }

            lanes = Math.Min(lanes, picks.Count);
            // The chapter's villain still stands beside Vorgath on a finale, inside the lanes that actually fight.
            if (finale && ch.Boss.Length > 0 && ch.Boss != bossId)
            {
                var kept = false;
                for (var i = 0; i < lanes; i++)
                {
                    if (picks[i] == ch.Boss)
                    {
                        kept = true;
                        break;
                    }
                }

                if (!kept)
                {
                    for (var i = lanes - 1; i >= 0; i--)
                    {
                        if (picks[i] != bossId)
                        {
                            picks[i] = ch.Boss;
                            break;
                        }
                    }
                }
            }

            var enemies = new List<StageEnemyDef>();
            for (var i = 0; i < lanes; i++)
            {
                if (boss && picks[i] == bossId)
                {
                    enemies.Add(new StageEnemyDef(bossId, level + BossLevels, Math.Min(StarsEnd, stars + 1)));
                }
                else
                {
                    enemies.Add(new StageEnemyDef(picks[i], level, stars));
                }
            }

            var gold = Gold(n);
            var rewards = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["res.soft"] = gold,
                ["res.food"] = Round(gold * FoodShare),
                ["res.wood"] = Round(gold * WoodShare),
                ["res.stone"] = Round(gold * StoneShare)
            };
            if (boss && BossSpeedupMinutes > 0)
            {
                rewards["res.speedup_m"] = BossSpeedupMinutes;
            }

            var name = ch.Name + " " + s + (boss ? (s == StagesPerChapter ? " \u2014 Boss" : " \u2014 Elite") : "");
            return new StageDef(
                StageId(ch.Number, s),
                name,
                enemies,
                rewards,
                boss ? ch.ShardHero : "",
                boss ? BossShards : 0,
                FirstGems + (boss ? BossGems : 0),
                null,
                ch.Number,
                s,
                boss);
        }

        private static int Round(double v) => v >= 1000 ? (int)(Math.Round(v / 50.0) * 50) : (int)(Math.Round(v / 10.0) * 10);

        internal static BattleCampaignDef? Parse(JsonNode.Obj root)
        {
            if (!root.TryGet("battle_campaign", out var n) || n is not JsonNode.Obj o)
            {
                return null;
            }

            var order = new List<string>();
            foreach (var v in CampaignCatalog.Items(o, "villain_order"))
            {
                if (v is JsonNode.Str s)
                {
                    order.Add(s.Value);
                }
            }

            var chapters = new List<ChapterDef>();
            var i = 0;
            foreach (var c in CampaignCatalog.Items(o, "chapters"))
            {
                if (c is JsonNode.Obj co)
                {
                    i++;
                    chapters.Add(new ChapterDef(
                        i,
                        CampaignCatalog.Str(co, "name", "Chapter " + i),
                        Math.Max(1, CampaignCatalog.Int(co, "castle", 1)),
                        CampaignCatalog.Str(co, "boss", order.Count > 0 ? order[order.Count - 1] : ""),
                        CampaignCatalog.Str(co, "shard_hero", ""),
                        CampaignCatalog.Map(co, "chest")));
                }
            }

            var d = new BattleCampaignDef();
            return new BattleCampaignDef
            {
                StagesPerChapter = Math.Max(1, CampaignCatalog.Int(o, "stages_per_chapter", d.StagesPerChapter)),
                BossEvery = CampaignCatalog.Int(o, "boss_every", d.BossEvery),
                LevelStart = CampaignCatalog.Int(o, "level_start", d.LevelStart),
                LevelEnd = CampaignCatalog.Int(o, "level_end", d.LevelEnd),
                LevelCurve = CampaignCatalog.Num(o, "level_curve", d.LevelCurve),
                StarsEnd = CampaignCatalog.Int(o, "stars_end", d.StarsEnd),
                BossLevels = CampaignCatalog.Int(o, "boss_levels", d.BossLevels),
                TwoLaneStages = CampaignCatalog.Int(o, "two_lane_stages", d.TwoLaneStages),
                VillainOrder = order,
                StartPool = CampaignCatalog.Int(o, "start_pool", d.StartPool),
                GoldStart = CampaignCatalog.Int(o, "gold_start", d.GoldStart),
                RewardGrowth = CampaignCatalog.Num(o, "reward_growth", d.RewardGrowth),
                FoodShare = CampaignCatalog.Num(o, "food_share", d.FoodShare),
                WoodShare = CampaignCatalog.Num(o, "wood_share", d.WoodShare),
                StoneShare = CampaignCatalog.Num(o, "stone_share", d.StoneShare),
                FirstGems = CampaignCatalog.Int(o, "first_gems", d.FirstGems),
                BossGems = CampaignCatalog.Int(o, "boss_gems", d.BossGems),
                BossShards = CampaignCatalog.Int(o, "boss_shards", d.BossShards),
                BossSpeedupMinutes = CampaignCatalog.Int(o, "boss_speedup_m", d.BossSpeedupMinutes),
                ReplayShare = CampaignCatalog.Num(o, "replay_share", d.ReplayShare),
                Chapters = chapters
            };
        }
    }
}
