#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;

namespace Survival.Domain.Campaign
{
    /// <summary>HP / attack / power of a character at a level and star rank.</summary>
    public readonly struct HeroStats
    {
        public HeroStats(int hp, int atk, int power)
        {
            Hp = hp;
            Atk = atk;
            Power = power;
        }

        public int Hp { get; }
        public int Atk { get; }
        public int Power { get; }

        public static HeroStats Of(CampaignHeroDef def, LevelCurve curve, int level, int stars)
        {
            var star = 1.0 + (curve.StarStatBonus * Math.Max(0, stars - 1));
            var hp = (int)Math.Round((def.BaseHp + (def.HpPerLevel * Math.Max(0, level - 1))) * star);
            var atk = (int)Math.Round((def.BaseAtk + (def.AtkPerLevel * Math.Max(0, level - 1))) * star);
            return new HeroStats(hp, atk, PowerOf(curve, hp, atk));
        }

        public static int PowerOf(LevelCurve curve, int hp, int atk) => (int)Math.Round((hp * curve.PowerHp) + (atk * curve.PowerAtk));
    }

    /// <summary>One hero's saved progress.</summary>
    public sealed class HeroProgress
    {
        public HeroProgress(string id) => Id = id;

        public string Id { get; }
        public bool Owned { get; set; }
        public int Level { get; set; } = 1;
        public int Stars { get; set; } = 1;
        public int Shards { get; set; }
    }

    /// <summary>A running level-up. Absolute clock times, so it survives a save and reload.</summary>
    public sealed class TrainingTimer
    {
        public TrainingTimer(string heroId, double startedAt, double durationSeconds)
        {
            HeroId = heroId;
            StartedAt = startedAt;
            DurationSeconds = Math.Max(0, durationSeconds);
        }

        public string HeroId { get; }
        public double StartedAt { get; }
        public double DurationSeconds { get; private set; }
        public double EndsAt => StartedAt + DurationSeconds;

        public double Remaining(double now) => Math.Max(0, EndsAt - now);

        public double Progress(double now) => DurationSeconds <= 0 ? 1 : Math.Min(1, Math.Max(0, (now - StartedAt) / DurationSeconds));

        internal void EndAt(double now) => DurationSeconds = Math.Max(0, now - StartedAt);
    }

    public enum LevelUpCheck
    {
        Ok = 0,
        Unknown,
        NotOwned,
        Training,
        AtStarCap,
        MaxLevel,
        NoSlot,
        CantAfford
    }

    public enum UnlockCheck
    {
        Ok = 0,
        Unknown,
        AlreadyOwned,
        NotForSale,
        CantAfford
    }

    /// <summary>
    /// Heroes the player owns and trains: level up for gold + food with a timer (free to finish in the last
    /// minutes, gems to rush, speed-ups to shorten), star up with shards (raises the level cap and every stat),
    /// unlock locked heroes with shards or gems. Free play with pay to go faster, same rules as the village.
    /// </summary>
    public sealed class HeroRoster
    {
        private readonly Dictionary<string, HeroProgress> _progress = new(StringComparer.Ordinal);
        private readonly List<TrainingTimer> _training = new();

        public HeroRoster(CampaignCatalog catalog, Wallet wallet, IGameClock clock, RushRules? rush = null)
        {
            Catalog = catalog;
            Wallet = wallet;
            Clock = clock;
            Rush = rush ?? new RushRules();
            foreach (var def in catalog.OnSide(CampaignSide.Hero))
            {
                _progress[def.Id] = new HeroProgress(def.Id) { Owned = def.StartOwned, Level = def.StartLevel, Stars = def.StartStars };
            }
        }

        public CampaignCatalog Catalog { get; }
        public Wallet Wallet { get; }
        public IGameClock Clock { get; }
        public RushRules Rush { get; }
        public IReadOnlyList<TrainingTimer> Training => _training;
        public IEnumerable<HeroProgress> All => _progress.Values;

        /// <summary>A hero finished training: (hero id, new level).</summary>
        public event Action<string, int>? LevelledUp;

        public event Action<string, int>? StarredUp;

        public event Action<string>? Unlocked;

        public HeroProgress? Get(string heroId) => _progress.TryGetValue(heroId, out var p) ? p : null;

        public bool IsOwned(string heroId) => Get(heroId)?.Owned == true;

        public IEnumerable<string> OwnedIds()
        {
            foreach (var def in Catalog.OnSide(CampaignSide.Hero))
            {
                if (IsOwned(def.Id))
                {
                    yield return def.Id;
                }
            }
        }

        public HeroStats Stats(string heroId)
        {
            var def = Catalog.Hero(heroId);
            var p = Get(heroId);
            return def == null ? default : HeroStats.Of(def, Catalog.Curve, p?.Level ?? 1, p?.Stars ?? 1);
        }

        /// <summary>Stats one level up (for the "HP 1,150 → 1,285" preview).</summary>
        public HeroStats NextLevelStats(string heroId)
        {
            var def = Catalog.Hero(heroId);
            var p = Get(heroId);
            return def == null ? default : HeroStats.Of(def, Catalog.Curve, (p?.Level ?? 1) + 1, p?.Stars ?? 1);
        }

        public HeroStats NextStarStats(string heroId)
        {
            var def = Catalog.Hero(heroId);
            var p = Get(heroId);
            return def == null ? default : HeroStats.Of(def, Catalog.Curve, p?.Level ?? 1, (p?.Stars ?? 1) + 1);
        }

        public int LevelCap(string heroId) => Catalog.Curve.LevelCap(Get(heroId)?.Stars ?? 1);

        public IReadOnlyDictionary<string, int> LevelUpCost(string heroId)
        {
            var level = Get(heroId)?.Level ?? 1;
            return new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [SurvIds.ResSoft] = Catalog.Curve.GoldFor(level),
                [SurvIds.ResFood] = Catalog.Curve.FoodFor(level)
            };
        }

        public int LevelUpSeconds(string heroId) => Catalog.Curve.SecondsFor(Get(heroId)?.Level ?? 1);

        public TrainingTimer? TrainingOf(string heroId)
        {
            foreach (var t in _training)
            {
                if (string.Equals(t.HeroId, heroId, StringComparison.Ordinal))
                {
                    return t;
                }
            }

            return null;
        }

        public bool HasFreeSlot => _training.Count < Catalog.TrainingSlots;

        public LevelUpCheck CanLevelUp(string heroId)
        {
            var p = Get(heroId);
            if (p == null)
            {
                return LevelUpCheck.Unknown;
            }

            if (!p.Owned)
            {
                return LevelUpCheck.NotOwned;
            }

            if (TrainingOf(heroId) != null)
            {
                return LevelUpCheck.Training;
            }

            if (p.Level >= Catalog.Curve.LevelCap(Catalog.Curve.MaxStars))
            {
                return LevelUpCheck.MaxLevel;
            }

            if (p.Level >= LevelCap(heroId))
            {
                return LevelUpCheck.AtStarCap;
            }

            if (!HasFreeSlot)
            {
                return LevelUpCheck.NoSlot;
            }

            return Wallet.CanAfford(LevelUpCost(heroId)) ? LevelUpCheck.Ok : LevelUpCheck.CantAfford;
        }

        /// <summary>Pays gold + food and starts the training timer.</summary>
        public LevelUpCheck TryStartLevelUp(string heroId)
        {
            var check = CanLevelUp(heroId);
            if (check != LevelUpCheck.Ok)
            {
                return check;
            }

            Wallet.TrySpend(LevelUpCost(heroId));
            _training.Add(new TrainingTimer(heroId, Clock.Now, LevelUpSeconds(heroId)));
            return LevelUpCheck.Ok;
        }

        /// <summary>
        /// A Hero Tome (res.hero_xp, from power packs): one level now, no gold, food, timer or slot. Up to the star cap,
        /// counting a level in training (that training still adds its own level when it ends).
        /// </summary>
        public LevelUpCheck CanUseTome(string heroId)
        {
            var p = Get(heroId);
            if (p == null)
            {
                return LevelUpCheck.Unknown;
            }

            if (!p.Owned)
            {
                return LevelUpCheck.NotOwned;
            }

            var level = p.Level + (TrainingOf(heroId) != null ? 1 : 0);
            if (level >= Catalog.Curve.LevelCap(Catalog.Curve.MaxStars))
            {
                return LevelUpCheck.MaxLevel;
            }

            if (level >= LevelCap(heroId))
            {
                return LevelUpCheck.AtStarCap;
            }

            return Wallet.Get(SurvIds.ResHeroXp) > 0 ? LevelUpCheck.Ok : LevelUpCheck.CantAfford;
        }

        /// <summary>Spends one Hero Tome for one level now (see <see cref="CanUseTome"/>).</summary>
        public LevelUpCheck TryUseTome(string heroId)
        {
            var check = CanUseTome(heroId);
            if (check != LevelUpCheck.Ok || !Wallet.TrySpend(SurvIds.ResHeroXp, 1))
            {
                return check == LevelUpCheck.Ok ? LevelUpCheck.CantAfford : check;
            }

            var p = Get(heroId)!;
            p.Level++;
            LevelledUp?.Invoke(p.Id, p.Level);
            return LevelUpCheck.Ok;
        }

        public double Remaining(string heroId) => TrainingOf(heroId)?.Remaining(Clock.Now) ?? 0;

        public bool CanFinishFree(string heroId)
        {
            var t = TrainingOf(heroId);
            return t != null && t.Remaining(Clock.Now) > 0 && Rush.IsFree(t.Remaining(Clock.Now));
        }

        public int RushCost(string heroId)
        {
            var t = TrainingOf(heroId);
            return t == null ? 0 : Rush.GemCost(t.Remaining(Clock.Now));
        }

        /// <summary>Inside the free window ends the timer for 0 gems; otherwise spends <see cref="RushCost"/> gems. Levels the hero up now.</summary>
        public bool TryRush(string heroId, out int gemsSpent)
        {
            gemsSpent = 0;
            var t = TrainingOf(heroId);
            if (t == null)
            {
                return false;
            }

            var remaining = t.Remaining(Clock.Now);
            if (remaining > 0)
            {
                var cost = Rush.GemCost(remaining);
                if (cost > 0 && !Wallet.TrySpend(SurvIds.ResHard, cost))
                {
                    return false;
                }

                gemsSpent = cost;
                t.EndAt(Clock.Now);
            }

            Tick();
            return true;
        }

        public bool TryFinishFree(string heroId)
        {
            if (!CanFinishFree(heroId))
            {
                return false;
            }

            return TryRush(heroId, out _);
        }

        /// <summary>Heroes whose training is inside the free-finish window (the Village FREE hero badge), in queue order.</summary>
        public IReadOnlyList<string> FreeFinishable()
        {
            var ids = new List<string>();
            foreach (var t in _training)
            {
                if (CanFinishFree(t.HeroId))
                {
                    ids.Add(t.HeroId);
                }
            }

            return ids;
        }

        /// <summary>Free-finish every eligible training (the FREE hero badge tap). Returns (hero, new level) for each.</summary>
        public IReadOnlyList<(string HeroId, int Level)> FinishAllFree()
        {
            var done = new List<(string, int)>();
            foreach (var id in FreeFinishable())
            {
                if (TryFinishFree(id))
                {
                    done.Add((id, Get(id)!.Level));
                }
            }

            return done;
        }

        /// <summary>Burns speed-up minutes (res.speedup_m) on a training timer; returns minutes used.</summary>
        public int ApplySpeedups(string heroId)
        {
            var t = TrainingOf(heroId);
            if (t == null || t.Remaining(Clock.Now) <= 0)
            {
                return 0;
            }

            var need = (int)Math.Ceiling((t.Remaining(Clock.Now) / 60.0) - 1e-9);
            var used = Math.Min(need, Wallet.Get(SurvIds.ResSpeedupM));
            if (used <= 0)
            {
                return 0;
            }

            Wallet.TrySpend(SurvIds.ResSpeedupM, used);
            var left = t.Remaining(Clock.Now) - (used * 60.0);
            t.EndAt(Clock.Now + Math.Max(0, left));
            Tick();
            return used;
        }

        /// <summary>Finishes every timer that has run out. Returns how many heroes levelled up.</summary>
        public int Tick()
        {
            var done = 0;
            for (var i = _training.Count - 1; i >= 0; i--)
            {
                var t = _training[i];
                if (t.Remaining(Clock.Now) > 0)
                {
                    continue;
                }

                _training.RemoveAt(i);
                var p = Get(t.HeroId);
                if (p == null)
                {
                    continue;
                }

                p.Level++;
                done++;
                LevelledUp?.Invoke(p.Id, p.Level);
            }

            return done;
        }

        public int ShardsToStar(string heroId) => Catalog.Curve.ShardsToStar(Get(heroId)?.Stars ?? 1);

        public bool CanStarUp(string heroId)
        {
            var p = Get(heroId);
            var need = ShardsToStar(heroId);
            return p != null && p.Owned && need > 0 && p.Shards >= need;
        }

        public bool TryStarUp(string heroId)
        {
            if (!CanStarUp(heroId))
            {
                return false;
            }

            var p = Get(heroId)!;
            p.Shards -= ShardsToStar(heroId);
            p.Stars++;
            StarredUp?.Invoke(p.Id, p.Stars);
            return true;
        }

        public void AddShards(string heroId, int amount)
        {
            var p = Get(heroId);
            if (p != null && amount > 0)
            {
                p.Shards += amount;
            }
        }

        public UnlockCheck CanUnlockWithGems(string heroId)
        {
            var def = Catalog.Hero(heroId);
            var p = Get(heroId);
            if (def == null || p == null)
            {
                return UnlockCheck.Unknown;
            }

            if (p.Owned)
            {
                return UnlockCheck.AlreadyOwned;
            }

            if (def.UnlockGems <= 0)
            {
                return UnlockCheck.NotForSale;
            }

            return Wallet.CanAfford(SurvIds.ResHard, def.UnlockGems) ? UnlockCheck.Ok : UnlockCheck.CantAfford;
        }

        public UnlockCheck CanUnlockWithShards(string heroId)
        {
            var def = Catalog.Hero(heroId);
            var p = Get(heroId);
            if (def == null || p == null)
            {
                return UnlockCheck.Unknown;
            }

            if (p.Owned)
            {
                return UnlockCheck.AlreadyOwned;
            }

            if (def.UnlockShards <= 0)
            {
                return UnlockCheck.NotForSale;
            }

            return p.Shards >= def.UnlockShards ? UnlockCheck.Ok : UnlockCheck.CantAfford;
        }

        public UnlockCheck TryUnlockWithGems(string heroId)
        {
            var check = CanUnlockWithGems(heroId);
            if (check != UnlockCheck.Ok)
            {
                return check;
            }

            Wallet.TrySpend(SurvIds.ResHard, Catalog.Hero(heroId)!.UnlockGems);
            Own(heroId);
            return UnlockCheck.Ok;
        }

        public UnlockCheck TryUnlockWithShards(string heroId)
        {
            var check = CanUnlockWithShards(heroId);
            if (check != UnlockCheck.Ok)
            {
                return check;
            }

            Get(heroId)!.Shards -= Catalog.Hero(heroId)!.UnlockShards;
            Own(heroId);
            return UnlockCheck.Ok;
        }

        private void Own(string heroId)
        {
            Get(heroId)!.Owned = true;
            Unlocked?.Invoke(heroId);
        }

        /// <summary>Highest level among owned heroes (bounty "hero_level").</summary>
        public int TopLevel()
        {
            var top = 0;
            foreach (var p in _progress.Values)
            {
                if (p.Owned && p.Level > top)
                {
                    top = p.Level;
                }
            }

            return top;
        }

        internal void Restore(HeroProgress saved)
        {
            var p = Get(saved.Id);
            if (p == null)
            {
                return;
            }

            p.Owned = saved.Owned || p.Owned;
            p.Level = Math.Max(1, Math.Min(saved.Level, Catalog.Curve.LevelCap(Catalog.Curve.MaxStars)));
            p.Stars = Math.Max(1, Math.Min(saved.Stars, Catalog.Curve.MaxStars));
            p.Shards = Math.Max(0, saved.Shards);
        }

        internal void RestoreTraining(TrainingTimer timer)
        {
            if (Get(timer.HeroId) != null && TrainingOf(timer.HeroId) == null && _training.Count < Catalog.TrainingSlots)
            {
                _training.Add(timer);
            }
        }
    }
}
