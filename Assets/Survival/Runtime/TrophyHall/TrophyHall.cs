#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;

namespace Survival.Domain.TrophyHall
{
    /// <summary>What a trophy counts. The view supplies the live number for each kind (see <see cref="TrophyHall.Progress"/>).</summary>
    public static class TrophyKinds
    {
        /// <summary>Total building levels gained above level 1, summed over every plot.</summary>
        public const string BuildingUpgrades = "building_upgrades";

        /// <summary>Most stars on any one hero.</summary>
        public const string HeroStars = "hero_stars";

        /// <summary>Highest runner level beaten.</summary>
        public const string RunnerCleared = "runner_cleared";

        /// <summary>Heroes owned.</summary>
        public const string HeroesOwned = "heroes_owned";

        /// <summary>Battles won.</summary>
        public const string BattlesWon = "battles_won";
    }

    public sealed class TrophyDef
    {
        public TrophyDef(string id, string kind, int target, IReadOnlyDictionary<string, int> reward)
        {
            Id = id;
            Kind = kind;
            Target = target;
            Reward = reward;
        }

        public string Id { get; }
        public string Kind { get; }
        public int Target { get; }
        public IReadOnlyDictionary<string, int> Reward { get; }
    }

    /// <summary>
    /// Trophy Hall achievements. Progress is read live from the game (buildings, heroes, runner, battles), so nothing
    /// has to be tracked twice; only which trophies were claimed is saved (trophy_hall.json). One-time rewards, free.
    /// </summary>
    public sealed class TrophyHall
    {
        public const string FileName = "trophy_hall.json";

        public static readonly IReadOnlyList<TrophyDef> DefaultTrophies = new[]
        {
            T("build_3", TrophyKinds.BuildingUpgrades, 3, (SurvIds.ResSoft, 5000)),
            T("build_10", TrophyKinds.BuildingUpgrades, 10, (SurvIds.ResHard, 30), (SurvIds.ResSpeedupM, 30)),
            T("build_25", TrophyKinds.BuildingUpgrades, 25, (SurvIds.ResHard, 80), (SurvIds.ResSpeedupM, 120)),
            T("stars_2", TrophyKinds.HeroStars, 2, (SurvIds.ResHeroXp, 1), (SurvIds.ResSoft, 5000)),
            T("stars_3", TrophyKinds.HeroStars, 3, (SurvIds.ResHard, 50), (SurvIds.ResHeroXp, 2)),
            T("stars_5", TrophyKinds.HeroStars, 5, (SurvIds.ResHard, 150), (SurvIds.ResHeroXp, 4)),
            T("runner_3", TrophyKinds.RunnerCleared, 3, (SurvIds.ResSoft, 8000)),
            T("runner_10", TrophyKinds.RunnerCleared, 10, (SurvIds.ResHard, 40), (SurvIds.ResSoft, 20000)),
            T("runner_20", TrophyKinds.RunnerCleared, 20, (SurvIds.ResHard, 100), (SurvIds.ResSpeedupM, 60)),
            T("heroes_3", TrophyKinds.HeroesOwned, 3, (SurvIds.ResSoft, 6000), (SurvIds.ResFood, 5000)),
            T("heroes_5", TrophyKinds.HeroesOwned, 5, (SurvIds.ResHard, 40)),
            T("heroes_8", TrophyKinds.HeroesOwned, 8, (SurvIds.ResHard, 120), (SurvIds.ResHeroXp, 3)),
            T("wins_5", TrophyKinds.BattlesWon, 5, (SurvIds.ResSoft, 5000)),
            T("wins_25", TrophyKinds.BattlesWon, 25, (SurvIds.ResHard, 40), (SurvIds.ResSpeedupM, 30)),
            T("wins_100", TrophyKinds.BattlesWon, 100, (SurvIds.ResHard, 120), (SurvIds.ResHeroXp, 3)),
        };

        private readonly HashSet<string> _claimed = new(StringComparer.Ordinal);

        public TrophyHall(Func<string, int> progress, IReadOnlyList<TrophyDef>? trophies = null)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Trophies = trophies ?? DefaultTrophies;
        }

        /// <summary>Live count for a <see cref="TrophyKinds"/> value.</summary>
        public Func<string, int> Progress { get; }

        public IReadOnlyList<TrophyDef> Trophies { get; }

        public event Action? Changed;

        public bool IsClaimed(string id) => _claimed.Contains(id);

        public int Current(TrophyDef t) => Math.Max(0, Math.Min(t.Target, Progress(t.Kind)));

        public bool IsReady(TrophyDef t) => !IsClaimed(t.Id) && Current(t) >= t.Target;

        /// <summary>How many trophies are waiting to be claimed (the hub badge).</summary>
        public int ReadyCount
        {
            get
            {
                var n = 0;
                foreach (var t in Trophies)
                {
                    if (IsReady(t))
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public int ClaimedCount => _claimed.Count;

        /// <summary>The goal kinds in display order (each kind is one row with tiers I, II, III).</summary>
        public List<string> Kinds()
        {
            var kinds = new List<string>();
            foreach (var t in Trophies)
            {
                if (!kinds.Contains(t.Kind))
                {
                    kinds.Add(t.Kind);
                }
            }

            return kinds;
        }

        /// <summary>The tier a row shows: the first unclaimed trophy of this kind, or null when every tier is claimed.</summary>
        public TrophyDef? NextOf(string kind)
        {
            foreach (var t in Trophies)
            {
                if (t.Kind == kind && !IsClaimed(t.Id))
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>(claimed tiers, total tiers) for a kind.</summary>
        public (int Done, int Total) TiersOf(string kind)
        {
            int done = 0, total = 0;
            foreach (var t in Trophies)
            {
                if (t.Kind == kind)
                {
                    total++;
                    if (IsClaimed(t.Id))
                    {
                        done++;
                    }
                }
            }

            return (done, total);
        }

        /// <summary>Grants the reward once. Null when the goal isn't met yet or was already claimed.</summary>
        public IReadOnlyDictionary<string, int>? Claim(string id, Wallet wallet)
        {
            foreach (var t in Trophies)
            {
                if (t.Id != id)
                {
                    continue;
                }

                if (!IsReady(t))
                {
                    return null;
                }

                _claimed.Add(id);
                wallet.Grant(t.Reward);

                Changed?.Invoke();
                return t.Reward;
            }

            return null;
        }

        /// <summary>Order for the list: ready first, then in progress, claimed last (stable within each group).</summary>
        public List<TrophyDef> Sorted()
        {
            var list = new List<TrophyDef>(Trophies);
            var rank = new Dictionary<string, int>();
            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                rank[t.Id] = ((IsReady(t) ? 0 : IsClaimed(t.Id) ? 2 : 1) * 1000) + i;
            }

            list.Sort((a, b) => rank[a.Id].CompareTo(rank[b.Id]));
            return list;
        }

        public string Save()
        {
            var w = new JsonWriter();
            w.Begin();
            w.Field("version", 1);
            w.Key("claimed");
            w.BeginArray();
            var ids = new List<string>(_claimed);
            ids.Sort(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                w.Value(id);
            }

            w.EndArray();
            w.End();
            return w.ToString();
        }

        public bool Load(string json)
        {
            try
            {
                if (JsonRead.Parse(json) is not JsonNode.Obj o)
                {
                    return false;
                }

                _claimed.Clear();
                foreach (var n in CampaignCatalog.Items(o, "claimed"))
                {
                    if (n is JsonNode.Str s)
                    {
                        _claimed.Add(s.Value);
                    }
                }

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static TrophyDef T(string id, string kind, int target, params (string Id, int N)[] reward)
        {
            var d = new Dictionary<string, int>();
            foreach (var (rid, n) in reward)
            {
                d[rid] = n;
            }

            return new TrophyDef(id, kind, target, d);
        }
    }
}
