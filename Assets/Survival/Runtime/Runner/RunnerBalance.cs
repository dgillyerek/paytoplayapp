#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Survival.Domain.Campaign;
using Survival.Domain.Catalog;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;
using Survival.Domain.Store;

namespace Survival.Domain.Runner
{
    /// <summary>runner_balance.json: the modelling assumptions (pace, village income, what each spend profile buys from the power-pack ladder).</summary>
    public sealed record RunnerBalanceDef
    {
        public const string FileName = "runner_balance.json";

        public int SessionsPerDay { get; init; } = 3;
        public int NewLevelsPerSession { get; init; } = 1;
        public int ReplaysPerSession { get; init; } = 1;
        public int MaxDays { get; init; } = 1095;
        public int Farms { get; init; } = 3;
        public int FarmLevel { get; init; } = 4;
        public int CollectionsPerDay { get; init; } = 4;
        public int GiftsPerDay { get; init; } = 4;
        public double HeroGoldShare { get; init; } = 0.75;
        public double HeroFoodShare { get; init; } = 1.0;
        public double FoodValue { get; init; } = 2.0;

        /// <summary>Stop the timeline once every level is cleared (false: run all max_days, for power-over-time charts).</summary>
        public bool StopWhenDone { get; init; } = true;
        public IReadOnlyList<RunnerSpendProfile> Profiles { get; init; } = Array.Empty<RunnerSpendProfile>();

        public RunnerSpendProfile Profile(string id) => Profiles.First(p => p.Id == id);

        public static RunnerBalanceDef Parse(string json)
        {
            var root = LayoutJson.Root(json, FileName);
            JsonNode.Obj O(string k) => root.TryGet(k, out var n) && n is JsonNode.Obj o ? o : new JsonNode.Obj();
            var pace = O("pace");
            var village = O("village");
            var greedy = O("greedy");
            var profiles = LayoutJson.Objs(root, "profiles").Select(p => new RunnerSpendProfile(
                LayoutJson.Str(p, "id"),
                LayoutJson.Objs(p, "buys").Select(b => new RunnerBuy(
                    LayoutJson.Str(b, "offer"),
                    LayoutJson.OptBool(b, "once", false),
                    Math.Max(0, LayoutJson.OptInt(b, "per_week", 0)),
                    Math.Max(0, LayoutJson.OptInt(b, "when_stuck_from", 0)))).ToList())).ToList();
            return new RunnerBalanceDef
            {
                SessionsPerDay = Math.Max(1, LayoutJson.OptInt(pace, "sessions_per_day", 3)),
                NewLevelsPerSession = Math.Max(1, LayoutJson.OptInt(pace, "new_levels_per_session", 1)),
                ReplaysPerSession = Math.Max(0, LayoutJson.OptInt(pace, "replays_per_session", 1)),
                MaxDays = Math.Max(1, LayoutJson.OptInt(pace, "max_days", 1095)),
                Farms = Math.Max(0, LayoutJson.OptInt(village, "farms", 3)),
                FarmLevel = Math.Max(1, LayoutJson.OptInt(village, "farm_level", 4)),
                CollectionsPerDay = Math.Max(0, LayoutJson.OptInt(village, "collections_per_day", 4)),
                GiftsPerDay = Math.Max(0, LayoutJson.OptInt(village, "gifts_per_day", 4)),
                HeroGoldShare = Math.Max(0f, Math.Min(1f, LayoutJson.OptNum(village, "hero_gold_share", 0.75f))),
                HeroFoodShare = Math.Max(0f, Math.Min(1f, LayoutJson.OptNum(village, "hero_food_share", 1f))),
                FoodValue = Math.Max(0f, LayoutJson.OptNum(greedy, "food_value", 2f)),
                Profiles = profiles
            };
        }
    }

    /// <summary>
    /// One purchase rule of a spend profile, on a power-pack offer: once (session 1), <paramref name="PerWeek"/> times a week
    /// (spread over the week), or once when the player first fails a level at or past <paramref name="WhenStuckFrom"/>.
    /// </summary>
    public sealed record RunnerBuy(string OfferId, bool Once, int PerWeek, int WhenStuckFrom);

    /// <summary>A spend profile: the power packs it buys (none = free).</summary>
    public sealed record RunnerSpendProfile(string Id, IReadOnlyList<RunnerBuy> Buys)
    {
        public bool Pays => Buys.Count > 0;

        /// <summary>USD a week from the recurring buys (one-time and stuck buys not counted).</summary>
        public double UsdPerWeek(PowerPackCatalog packs) => Buys.Sum(b => b.PerWeek * (packs.Pack(b.OfferId)?.Usd ?? 0));
    }

    /// <summary>
    /// The real game files the economy reads: campaign.json (heroes, level curve, stages, bounties), runner_levels.json,
    /// base_economy.json (farm output, starting gems, the rush rate), gifts.json, quests.json, and the power-pack ladder
    /// (stores.json store.power_packs + power_packs.json).
    /// </summary>
    public sealed class RunnerBalanceSources
    {
        public RunnerBalanceSources(CampaignCatalog campaign, RunnerCatalog runner, string baseEconomyJson, string giftsJson, string questsJson, string? storesJson = null, string? powerPacksJson = null)
        {
            Campaign = campaign;
            Runner = runner;
            if (storesJson != null && powerPacksJson != null)
            {
                Packs = PowerPackCatalog.Parse(powerPacksJson);
                var store = HookCatalogLoader.ParseStores(storesJson).FirstOrDefault(s => s.Id == Packs.StoreId);
                if (store != null)
                {
                    PackRows = Packs.Rows(store).ToDictionary(r => r.Offer.Id, StringComparer.Ordinal);
                }
            }

            var eco = LayoutJson.Root(baseEconomyJson, "base_economy.json");
            if (eco.TryGet("starting_wallet", out var sw) && sw is JsonNode.Obj wallet)
            {
                StartGems = LayoutJson.OptInt(wallet, SurvIds.ResHard, 0);
            }

            if (eco.TryGet("rush", out var rn) && rn is JsonNode.Obj rush)
            {
                FreeFinishSeconds = LayoutJson.OptInt(rush, "free_finish_seconds", 300);
                GemsPerMinute = Math.Max(0.01f, LayoutJson.OptNum(rush, "gems_per_minute", 1f));
            }

            foreach (var plot in LayoutJson.Objs(eco, "plots"))
            {
                if (LayoutJson.OptStr(plot, "id", "").Contains("farm"))
                {
                    foreach (var l in LayoutJson.Objs(plot, "levels"))
                    {
                        FarmLevels.Add((LayoutJson.OptInt(l, "per_hour", 0), LayoutJson.OptInt(l, "capacity", 0)));
                    }

                    break;
                }
            }

            var gifts = LayoutJson.Root(giftsJson, "gifts.json");
            DailyGiftCap = LayoutJson.OptInt(gifts, "daily_cap", 6);
            var pool = LayoutJson.Objs(gifts, "pool").ToList();
            var weight = pool.Sum(p => LayoutJson.OptNum(p, "weight", 0f));
            foreach (var p in pool)
            {
                var share = weight > 0 ? LayoutJson.OptNum(p, "weight", 0f) / weight : 0f;
                var mean = (LayoutJson.OptNum(p, "min", 0f) + LayoutJson.OptNum(p, "max", 0f)) * 0.5;
                var res = LayoutJson.OptStr(p, "resource", "");
                GiftMean.TryGetValue(res, out var have);
                GiftMean[res] = have + (share * mean);
            }

            var quests = LayoutJson.Root(questsJson, "quests.json");
            if (quests.TryGet("chain", out var cn) && cn is JsonNode.Obj chain)
            {
                foreach (var step in LayoutJson.Objs(chain, "steps"))
                {
                    if (step.TryGet("rewards", out var rw) && rw is JsonNode.Obj r)
                    {
                        foreach (var f in r.Fields)
                        {
                            if (f.Value is JsonNode.Num n)
                            {
                                QuestRewards.TryGetValue(f.Key, out var have);
                                QuestRewards[f.Key] = have + (int)Math.Round(n.Value);
                            }
                        }
                    }
                }
            }
        }

        public CampaignCatalog Campaign { get; }
        public RunnerCatalog Runner { get; }
        public int StartGems { get; }
        public int FreeFinishSeconds { get; } = 300;
        public float GemsPerMinute { get; } = 1f;
        public List<(int PerHour, int Capacity)> FarmLevels { get; } = new();
        public int DailyGiftCap { get; }
        public Dictionary<string, double> GiftMean { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> QuestRewards { get; } = new(StringComparer.Ordinal);

        /// <summary>The power-pack ladder (null: no packs, every profile plays free).</summary>
        public PowerPackCatalog? Packs { get; }
        public IReadOnlyDictionary<string, PowerPackRow> PackRows { get; } = new Dictionary<string, PowerPackRow>();

        /// <summary>Campaign heroes whose runner units the squad uses (lyra, rowan, oakenshield), in a stable order.</summary>
        public IReadOnlyList<string> SquadHeroes =>
            Runner.Units.Values.Where(u => u.HeroId.Length > 0 && Campaign.Hero(u.HeroId) != null).Select(u => u.HeroId).Distinct().OrderBy(h => h, StringComparer.Ordinal).ToList();

        public double FoodPerDay(RunnerBalanceDef d)
        {
            if (FarmLevels.Count == 0)
            {
                return 0;
            }

            var (perHour, cap) = FarmLevels[Math.Max(0, Math.Min(FarmLevels.Count - 1, d.FarmLevel - 1))];
            return d.Farms * Math.Min(perHour * 24.0, cap * (double)d.CollectionsPerDay);
        }
    }

    /// <summary>Who the squad of a level is made of, at full strength: the lead plus the follower chests in road order, up to the cap.</summary>
    public sealed record RunnerSquadShape(string Lead, IReadOnlyDictionary<string, int> Followers, float WeaponTotal, float RateTotal)
    {
        public static RunnerSquadShape Of(RunnerCatalog c, RunnerLevelDef level)
        {
            var cap = Math.Min(level.MaxFollowers, RunnerRules.MaxFollowers);
            var f = new Dictionary<string, int>(StringComparer.Ordinal);
            var n = 0;
            float weapon = 0f, rate = 0f;
            foreach (var r in level.Rows)
            {
                var b = r.Box;
                if (b == null)
                {
                    continue;
                }

                if (b.Kind == RunnerBoxKind.Weapon)
                {
                    weapon += b.Amount;
                }
                else if (b.Kind == RunnerBoxKind.Rate)
                {
                    rate += b.Amount;
                }
                else if (b.GivesUnits && n < cap)
                {
                    var take = Math.Min(cap - n, b.Count);
                    f.TryGetValue(b.Unit, out var have);
                    f[b.Unit] = have + take;
                    n += take;
                }
            }

            return new RunnerSquadShape(level.Hero, f, weapon, rate);
        }
    }

    /// <summary>One squad's numbers on one level (see <see cref="RunnerBalance.Strength"/>).</summary>
    public sealed record RunnerStrength(double DamageMul, double HpMul, double SquadDps, double SquadHp, double TimeToKill, double TimeToOverrun)
    {
        /// <summary>The uniform stat multiplier this loadout is worth: sqrt(damage share x HP share).</summary>
        public double Scale => Math.Sqrt(DamageMul * HpMul);

        /// <summary>Lanchester-style power vs an untrained squad: damage x HP.</summary>
        public double Power => DamageMul * HpMul;

        /// <summary>Time to be overrun / time to kill, per wave.</summary>
        public double PressureRatio => TimeToKill > 0 ? TimeToOverrun / TimeToKill : 0;
    }

    /// <summary>
    /// The runner's power math. A squad's strength on a level is the damage share and HP share its trained heroes add to
    /// the level's full squad (lead + follower chests): damage weighted by each unit's DPS, HP by each body's HP. The
    /// analytic pressure compares, per wave, the time the squad needs to kill the wave (wave HP / squad DPS at the end of the
    /// road) with the time the wave needs to overrun it (squad HP / wave DPS); the sim then says what actually wins.
    /// </summary>
    public static class RunnerBalance
    {
        public static RunnerStrength Strength(RunnerCatalog c, RunnerLevelDef level, RunnerLoadout loadout)
        {
            var shape = RunnerSquadShape.Of(c, level);
            double dps0 = 0, dps = 0, hp0 = 0, hp = 0;
            void Add(string unitId, int count, bool lead)
            {
                var u = c.Unit(unitId);
                var b = loadout.For(unitId);
                var d = u.Cycle > 0 ? u.Damage / u.Cycle : 0;
                var h = lead ? u.Hp : RunnerRules.FollowerHp;
                dps0 += count * d;
                dps += count * d * b.DamageMul;
                hp0 += count * h;
                hp += count * h * b.HpMul;
            }

            Add(shape.Lead, 1, true);
            foreach (var f in shape.Followers)
            {
                Add(f.Key, f.Value, false);
            }

            var squadDps = dps * (1 + shape.WeaponTotal) * (1 + shape.RateTotal);
            double waveHp = 0, waveDps = 0;
            var waves = 0;
            var last = float.MinValue;
            foreach (var r in level.Rows)
            {
                if (r.Kind != RunnerRowKind.Enemy && r.Kind != RunnerRowKind.Boss)
                {
                    continue;
                }

                // Rows within one beat are one wave.
                if (r.At - last > RunnerProgression.BeatMetres * 0.6f)
                {
                    waves++;
                }

                last = r.At;
                var u = c.Unit(r.Unit);
                var boss = r.Kind == RunnerRowKind.Boss;
                waveHp += r.Count * u.Hp * (boss ? level.BossHpMul : level.FoeHpMul);
                waveDps += r.Count * (u.Cycle > 0 ? u.Damage * level.FoeDmgMul / u.Cycle : 0);
            }

            waves = Math.Max(1, waves);
            var ttk = squadDps > 0 ? waveHp / waves / squadDps : double.PositiveInfinity;
            var tto = waveDps > 0 ? hp / (waveDps / waves) : double.PositiveInfinity;
            return new RunnerStrength(dps0 > 0 ? dps / dps0 : 1, hp0 > 0 ? hp / hp0 : 1, squadDps, hp, ttk, tto);
        }

        /// <summary>A loadout that multiplies every squad unit's damage and HP by <paramref name="scale"/>.</summary>
        public static RunnerLoadout Uniform(RunnerCatalog c, double scale) =>
            new(c.Units.Values.Where(u => u.HeroId.Length > 0).ToDictionary(u => u.Id, _ => new RunnerUnitBoost((float)scale, (float)scale)));

        /// <summary><paramref name="loadout"/> with every boost multiplied by <paramref name="k"/>.</summary>
        public static RunnerLoadout Scaled(RunnerCatalog c, RunnerLoadout loadout, double k) =>
            new(c.Units.Values.Where(u => u.HeroId.Length > 0).ToDictionary(u => u.Id, u =>
            {
                var b = loadout.For(u.Id);
                return new RunnerUnitBoost((float)(b.DamageMul * k), (float)(b.HpMul * k));
            }));

        /// <summary>Gold of a clean win of <paramref name="level"/>: every enemy and box, plus the win gold (the real reward formula).</summary>
        public static int WinGold(RunnerLevelDef level)
        {
            var b = RunnerRewards.For(level, new RunnerResult(level.Id, true, level.EnemyCount + 1, level.BoxCount, 0, 0, 0, level.Length, 0f, level.Number));
            return b.Resources.TryGetValue(SurvIds.ResSoft, out var g) ? g : 0;
        }
    }

    /// <summary>One hero's state in the model.</summary>
    public sealed class ModelHero
    {
        public ModelHero(string id) => Id = id;

        public string Id { get; }
        public bool Owned { get; set; }
        public int Level { get; set; } = 1;
        public int Stars { get; set; } = 1;
        public int Shards { get; set; }

        /// <summary>Seconds left on the running level-up (0 = not training).</summary>
        public double Training { get; set; }

        public ModelHero Copy() => new(Id) { Owned = Owned, Level = Level, Stars = Stars, Shards = Shards, Training = Training };
    }

    /// <summary>The model player at one moment.</summary>
    public sealed record RunnerModelSnapshot(
        int Session,
        int Cleared,
        IReadOnlyDictionary<string, (int Level, int Stars)> Heroes,
        double Gold,
        double Food,
        double Gems,
        double GoldEarned,
        double FoodEarned,
        double UsdSpent,
        int Banners = 0);

    /// <summary>What one profile's timeline says about one level.</summary>
    public sealed record RunnerLevelOutcome(
        int Number,
        RunnerModelSnapshot AtPace,
        double PowerRatio,
        int? ClearedSession,
        double? ClearedDay,
        RunnerModelSnapshot? AtClear);

    /// <summary>
    /// The deterministic player model: a session-by-session timeline from a fresh save. Each session: free income (farms,
    /// gifts, one-time campaign / bounty / quest rewards in the first days, the profile's power packs), trainings tick on two
    /// slots, star-ups and unlocks from shards, Hero Tomes on the best hero, greedy level-ups (best squad power per cost,
    /// gold + food), rush with gems / speed-ups, then one attempt at the next runner level (cleared if the squad's power
    /// ratio, x the War Banner boost while charges are left, is at least 1; a first clear spends a charge) and the replays.
    /// A "stuck" buy fires on the first failed attempt at or past its level; the player then keeps attempting in that
    /// session until a level fails (the burst).
    /// </summary>
    public sealed class RunnerEconomy
    {
        private readonly RunnerBalanceSources _src;
        private readonly RunnerBalanceDef _def;
        private readonly RunnerSpendProfile _profile;
        private readonly Func<int, double> _requiredScale;
        private readonly Dictionary<string, ModelHero> _heroes = new(StringComparer.Ordinal);
        private readonly List<RunnerUnitDef> _squadUnits;
        private double _gold, _food, _gems, _speedupMin, _goldEarned, _foodEarned, _usd;
        private int _cleared, _tomes, _banners;
        private bool _burst;
        private readonly HashSet<RunnerBuy> _stuckDone = new();

        /// <param name="requiredScale">The uniform stat scale level n needs (from the sim: the least boost a dodging player wins with).</param>
        public RunnerEconomy(RunnerBalanceSources src, RunnerBalanceDef def, RunnerSpendProfile profile, Func<int, double> requiredScale)
        {
            _src = src;
            _def = def;
            _profile = profile;
            _requiredScale = requiredScale;
            foreach (var id in src.SquadHeroes)
            {
                var h = src.Campaign.Hero(id)!;
                _heroes[id] = new ModelHero(id) { Owned = h.StartOwned, Level = Math.Max(1, h.StartLevel), Stars = Math.Max(1, h.StartStars) };
            }

            _squadUnits = src.Runner.Units.Values.Where(u => u.HeroId.Length > 0).ToList();
        }

        public LevelCurve Curve => _src.Campaign.Curve;

        /// <summary>The state at the end of every day of the last <see cref="Run"/> (index 0 = end of day 1).</summary>
        public List<RunnerModelSnapshot> Daily { get; } = new();

        /// <summary>Every power pack bought in the last <see cref="Run"/>: (session, offer, level the player was on).</summary>
        public List<(int Session, string OfferId, int Level)> Purchases { get; } = new();

        /// <summary>The stuck buys of the last <see cref="Run"/>: the level that failed, the session, and how many new levels the burst cleared.</summary>
        public List<(int Level, int Session, int Burst)> StuckBuys { get; } = new();

        public int Tomes => _tomes;
        public int Banners => _banners;

        /// <summary>The squad now, boosted while a War Banner charge is held.</summary>
        public RunnerLoadout Loadout() => Loadout(_heroes, _banners);

        public RunnerLoadout Loadout(IReadOnlyDictionary<string, ModelHero> heroes) =>
            RunnerLoadout.From(_src.Runner, _src.Campaign, id => heroes.TryGetValue(id, out var h) ? (h.Level, h.Stars) : null);

        public RunnerLoadout Loadout(IReadOnlyDictionary<string, ModelHero> heroes, int banners)
        {
            var lo = Loadout(heroes);
            return banners > 0 && _src.Packs != null ? lo.Boosted(_src.Runner, _src.Packs.BoostDamageMul, _src.Packs.BoostHpMul) : lo;
        }

        /// <summary>The loadout a snapshot fights with (its heroes, boosted if it held a banner).</summary>
        public RunnerLoadout LoadoutOf(RunnerModelSnapshot s) =>
            Loadout(s.Heroes.ToDictionary(h => h.Key, h => new ModelHero(h.Key) { Owned = true, Level = h.Value.Level, Stars = h.Value.Stars }), s.Banners);

        public double Ratio(int number) => Ratio(number, Loadout());

        public double Ratio(int number, RunnerLoadout loadout)
        {
            var level = _src.Runner.ByNumber(number);
            var need = _requiredScale(number);
            var s = RunnerBalance.Strength(_src.Runner, level, loadout).Scale;
            return need > 0 ? (s * s) / (need * need) : double.PositiveInfinity;
        }

        /// <summary>Runs the timeline; per level: the state at the nominal pace (session n), its power ratio, and when the level was first cleared.</summary>
        public List<RunnerLevelOutcome> Run()
        {
            var count = _src.Runner.Levels.Count;
            var atPace = new RunnerModelSnapshot?[count + 1];
            var cleared = new (int Session, RunnerModelSnapshot Snap)?[count + 1];
            var sessions = _def.MaxDays * _def.SessionsPerDay;
            var stages = _src.Campaign.Stages.Where(s => s.CampId == null).ToList();
            var bounties = _src.Campaign.Bounties;
            _gems = _src.StartGems;
            for (var k = 1; k <= sessions; k++)
            {
                Income(k, stages, bounties);
                Spend();
                if (k <= count)
                {
                    atPace[k] = Snap(k);
                }

                var burstFrom = -1;
                for (var a = 0; (a < _def.NewLevelsPerSession || _burst) && _cleared < count; a++)
                {
                    var next = _cleared + 1;
                    if (Ratio(next) < 1.0)
                    {
                        if (!StuckBuy(k, next))
                        {
                            break;
                        }

                        burstFrom = _cleared;
                        _burst = true;
                        Spend();
                        if (Ratio(next) < 1.0)
                        {
                            break;
                        }
                    }

                    _cleared = next;
                    if (_banners > 0)
                    {
                        _banners--;
                    }

                    Win(next);
                    cleared[next] = (k, Snap(k));
                }

                if (burstFrom >= 0)
                {
                    StuckBuys.Add((burstFrom + 1, k, _cleared - burstFrom));
                }

                _burst = false;

                for (var r = 0; r < _def.ReplaysPerSession && _cleared > 0; r++)
                {
                    Win(_cleared);
                }

                Spend();
                if (k % _def.SessionsPerDay == 0)
                {
                    Daily.Add(Snap(k));
                }

                if (_cleared >= count && k >= count && _def.StopWhenDone)
                {
                    break;
                }
            }

            var outcomes = new List<RunnerLevelOutcome>();
            for (var n = 1; n <= count; n++)
            {
                var snap = atPace[n]!;
                var lo = LoadoutOf(snap);
                var c = cleared[n];
                outcomes.Add(new RunnerLevelOutcome(n, snap, Ratio(n, lo), c?.Session, c == null ? null : (c.Value.Session - 1) / (double)_def.SessionsPerDay, c?.Snap));
            }

            return outcomes;
        }

        private RunnerModelSnapshot Snap(int session) => new(
            session,
            _cleared,
            _heroes.ToDictionary(h => h.Key, h => (h.Value.Level, h.Value.Stars)),
            _gold,
            _food,
            _gems,
            _goldEarned,
            _foodEarned,
            _usd,
            _banners);

        private void AddGold(double g)
        {
            _gold += g * _def.HeroGoldShare;
            _goldEarned += g;
        }

        private void AddFood(double f)
        {
            _food += f * _def.HeroFoodShare;
            _foodEarned += f;
        }

        private void AddShards(string heroId, int shards)
        {
            if (_heroes.TryGetValue(heroId, out var h))
            {
                h.Shards += shards;
            }
        }

        private void Grant(IReadOnlyDictionary<string, int> rewards)
        {
            foreach (var r in rewards)
            {
                switch (r.Key)
                {
                    case SurvIds.ResSoft: AddGold(r.Value); break;
                    case SurvIds.ResFood: AddFood(r.Value); break;
                    case SurvIds.ResHard: _gems += r.Value; break;
                    case SurvIds.ResSpeedupM: _speedupMin += r.Value; break;
                }
            }
        }

        /// <summary>A power pack: its gold and food go straight to heroes (bought for them), gems, speed-ups, tomes, banners.</summary>
        private void Buy(int session, string offerId)
        {
            if (!_src.PackRows.TryGetValue(offerId, out var row))
            {
                throw new InvalidOperationException("Profile " + _profile.Id + " buys unknown power pack " + offerId);
            }

            _usd += row.Pack.Usd;
            Purchases.Add((session, offerId, _cleared + 1));
            foreach (var g in row.Grants)
            {
                switch (g.Key)
                {
                    case SurvIds.ResSoft: _gold += g.Value; break;
                    case SurvIds.ResFood: _food += g.Value; break;
                    case SurvIds.ResHard: _gems += g.Value; break;
                    case SurvIds.ResSpeedupM: _speedupMin += g.Value; break;
                    case SurvIds.ResHeroXp: _tomes += g.Value; break;
                    case SurvIds.ResRunnerBoost: _banners += g.Value; break;
                }
            }
        }

        private void ScheduledBuys(int k)
        {
            var week = 7 * _def.SessionsPerDay;
            var offset = (k - 1) % week;
            foreach (var b in _profile.Buys)
            {
                if (b.Once && k == 1)
                {
                    Buy(k, b.OfferId);
                }

                for (var i = 0; i < b.PerWeek; i++)
                {
                    if (offset == (int)Math.Floor(i * week / (double)b.PerWeek))
                    {
                        Buy(k, b.OfferId);
                    }
                }
            }
        }

        private bool StuckBuy(int k, int level)
        {
            foreach (var b in _profile.Buys)
            {
                if (b.WhenStuckFrom > 0 && level >= b.WhenStuckFrom && _stuckDone.Add(b))
                {
                    Buy(k, b.OfferId);
                    return true;
                }
            }

            return false;
        }

        private void Income(int k, IReadOnlyList<StageDef> stages, IReadOnlyList<BountyDef> bounties)
        {
            var spd = _def.SessionsPerDay;
            var day = (k - 1) / spd;
            var firstOfDay = (k - 1) % spd == 0;
            if (k == 1)
            {
                Grant(_src.QuestRewards);
            }

            // One campaign stage (first clear) and one bounty a day, from day 1.
            if (firstOfDay && day < stages.Count)
            {
                var s = stages[day];
                Grant(s.Rewards);
                _gems += s.FirstClearGems;
                AddShards(s.ShardHero, s.Shards);
            }

            if (firstOfDay && day < bounties.Count)
            {
                Grant(bounties[day].Rewards);
            }

            // The profile's power packs (one-time on session 1, weekly ones spread over the week).
            ScheduledBuys(k);

            var gifts = Math.Min(_def.GiftsPerDay, _src.DailyGiftCap) / (double)spd;
            foreach (var g in _src.GiftMean)
            {
                switch (g.Key)
                {
                    case SurvIds.ResSoft: AddGold(g.Value * gifts); break;
                    case SurvIds.ResHard: _gems += g.Value * gifts; break;
                    case SurvIds.ResSpeedupM: _speedupMin += g.Value * gifts; break;
                }
            }

            AddFood(_src.FoodPerDay(_def) / spd);

            // Training runs between sessions.
            var dt = 86400.0 / spd;
            foreach (var h in _heroes.Values)
            {
                if (h.Training > 0)
                {
                    h.Training = Math.Max(0, h.Training - dt);
                    if (h.Training <= 0)
                    {
                        h.Level++;
                    }
                }
            }
        }

        private void Win(int number)
        {
            var level = _src.Runner.ByNumber(number);
            AddGold(RunnerBalance.WinGold(level));
            AddShards(level.Rewards.ShardHero, level.Rewards.Shards);
        }

        private int TrainingCount => _heroes.Values.Count(h => h.Training > 0);

        private void Spend()
        {
            for (var pass = 0; pass < 8; pass++)
            {
                var changed = StarsAndUnlocks();
                changed |= ApplyTomes();
                changed |= LevelUps();
                changed |= Rush();
                if (!changed)
                {
                    return;
                }
            }
        }

        private bool StarsAndUnlocks()
        {
            var changed = false;
            foreach (var h in _heroes.Values)
            {
                var def = _src.Campaign.Hero(h.Id)!;
                if (!h.Owned)
                {
                    if (def.UnlockShards > 0 && h.Shards >= def.UnlockShards)
                    {
                        h.Shards -= def.UnlockShards;
                        h.Owned = true;
                        changed = true;
                    }
                    else if (def.UnlockGems > 0 && _gems >= def.UnlockGems)
                    {
                        _gems -= def.UnlockGems;
                        h.Owned = true;
                        changed = true;
                    }

                    continue;
                }

                while (Curve.ShardsToStar(h.Stars) > 0 && h.Shards >= Curve.ShardsToStar(h.Stars))
                {
                    h.Shards -= Curve.ShardsToStar(h.Stars);
                    h.Stars++;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>The squad power the model optimises: damage x HP share of the next level's full squad.</summary>
        private double PowerWith(string heroId, int level)
        {
            var copy = _heroes.ToDictionary(h => h.Key, h => h.Value.Copy());
            copy[heroId].Level = level;
            var number = Math.Max(1, Math.Min(_src.Runner.Levels.Count, _cleared + 1));
            return RunnerBalance.Strength(_src.Runner, _src.Runner.ByNumber(number), Loadout(copy)).Power;
        }

        /// <summary>Hero Tomes: one level each on the hero it adds the most squad power to, up to the star cap (counting training).</summary>
        private bool ApplyTomes()
        {
            var changed = false;
            while (_tomes > 0)
            {
                var number = Math.Max(1, Math.Min(_src.Runner.Levels.Count, _cleared + 1));
                var now = RunnerBalance.Strength(_src.Runner, _src.Runner.ByNumber(number), Loadout(_heroes)).Power;
                ModelHero? best = null;
                var bestGain = double.NegativeInfinity;
                foreach (var h in _heroes.Values)
                {
                    if (!h.Owned || h.Level + (h.Training > 0 ? 1 : 0) >= Curve.LevelCap(h.Stars))
                    {
                        continue;
                    }

                    var gain = PowerWith(h.Id, h.Level + 1) - now;
                    if (gain > bestGain)
                    {
                        best = h;
                        bestGain = gain;
                    }
                }

                if (best == null)
                {
                    return changed;
                }

                best.Level++;
                _tomes--;
                changed = true;
            }

            return changed;
        }

        private bool LevelUps()
        {
            var changed = false;
            while (TrainingCount < _src.Campaign.TrainingSlots)
            {
                var number = Math.Max(1, Math.Min(_src.Runner.Levels.Count, _cleared + 1));
                var now = RunnerBalance.Strength(_src.Runner, _src.Runner.ByNumber(number), Loadout(_heroes)).Power;
                ModelHero? best = null;
                var bestValue = 0.0;
                foreach (var h in _heroes.Values)
                {
                    if (!h.Owned || h.Training > 0 || h.Level >= Curve.LevelCap(h.Stars))
                    {
                        continue;
                    }

                    var cost = Curve.GoldFor(h.Level) + (_def.FoodValue * Curve.FoodFor(h.Level));
                    var value = (PowerWith(h.Id, h.Level + 1) - now) / Math.Max(1, cost);
                    if (best == null || value > bestValue)
                    {
                        best = h;
                        bestValue = value;
                    }
                }

                if (best == null)
                {
                    return changed;
                }

                var gold = Curve.GoldFor(best.Level);
                var food = Curve.FoodFor(best.Level);
                if (_gold < gold || _food < food)
                {
                    return changed;
                }

                _gold -= gold;
                _food -= food;
                best.Training = Curve.SecondsFor(best.Level);
                changed = true;
                FinishCheap(best);
            }

            return changed;
        }

        /// <summary>Free finish (the last few minutes) and speed-up minutes.</summary>
        private void FinishCheap(ModelHero h)
        {
            var left = h.Training - _src.FreeFinishSeconds;
            if (left > 0 && _speedupMin * 60 >= left)
            {
                _speedupMin -= Math.Ceiling(left / 60.0);
                left = 0;
            }

            if (left <= 0)
            {
                h.Training = 0;
                h.Level++;
            }
        }

        /// <summary>Gems on what is still training (1 gem a minute past the free finish), whatever the profile: a free player's gems too.</summary>
        private bool Rush()
        {
            var changed = false;
            foreach (var h in _heroes.Values.Where(x => x.Training > 0).OrderBy(x => x.Training))
            {
                var minutes = Math.Ceiling(Math.Max(0, h.Training - _src.FreeFinishSeconds - (_speedupMin * 60)) / 60.0);
                var cost = Math.Max(1, Math.Ceiling(minutes * _src.GemsPerMinute));
                if (_gems >= cost)
                {
                    _gems -= cost;
                    _speedupMin = Math.Max(0, _speedupMin - Math.Ceiling(Math.Max(0, h.Training - _src.FreeFinishSeconds) / 60.0));
                    h.Training = 0;
                    h.Level++;
                    changed = true;
                }
            }

            return changed;
        }
    }
}
