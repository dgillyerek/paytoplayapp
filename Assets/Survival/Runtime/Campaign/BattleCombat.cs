#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Campaign
{
    public enum BattleOutcome
    {
        Ongoing = 0,
        Won = 1,
        Lost = 2
    }

    /// <summary>One fighter's hit points in a configured battle.</summary>
    public sealed class CombatUnit
    {
        public CombatUnit(int uid, LineupFighter fighter)
        {
            Uid = uid;
            Fighter = fighter;
            MaxHp = Math.Max(1, fighter.Hp);
            Hp = MaxHp;
        }

        public int Uid { get; }
        public LineupFighter Fighter { get; }
        public CampaignSide Side => Fighter.Side;
        public int MaxHp { get; }
        public int Hp { get; internal set; }
        public bool Alive => Hp > 0;
        public float Fraction => Hp / (float)MaxHp;
        public int DamageDealt { get; internal set; }
    }

    /// <summary>
    /// Hit points and the end of a configured BattleLocked fight. Every landed hit (melee contact, projectile, combo
    /// hit, volley arrow) deals the attacker's attack split across that attack's hits; heavy hits (stagger reactions)
    /// deal 35% more. A side loses when nobody on it stands; at the timeout the side with more HP left (by share) wins,
    /// a tie goes to the villains.
    /// </summary>
    public sealed class BattleCombat
    {
        public const float HeavyMultiplier = 1.35f;

        private readonly List<CombatUnit> _units = new();

        public BattleCombat(BattleLineup lineup)
        {
            Lineup = lineup;
            var uid = 0;
            foreach (var f in lineup.All)
            {
                _units.Add(new CombatUnit(uid++, f));
            }
        }

        public BattleLineup Lineup { get; }
        public IReadOnlyList<CombatUnit> Units => _units;
        public double Elapsed { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public bool TimedOut { get; private set; }
        public bool Retreated { get; private set; }

        /// <summary>(unit) went down.</summary>
        public event Action<CombatUnit>? Defeated;

        public CombatUnit? Find(CampaignSide side, string fighter)
        {
            foreach (var u in _units)
            {
                if (u.Side == side && string.Equals(u.Fighter.Fighter, fighter, StringComparison.Ordinal))
                {
                    return u;
                }
            }

            return null;
        }

        public static int DamageOf(int atk, int hitsInAttack, bool heavy)
        {
            var dmg = atk / (double)Math.Max(1, hitsInAttack);
            if (heavy)
            {
                dmg *= HeavyMultiplier;
            }

            return Math.Max(1, (int)Math.Round(dmg));
        }

        /// <summary>Applies one landed hit. Returns the damage dealt (0 once the battle is over or either side is down).</summary>
        public int Hit(CombatUnit attacker, CombatUnit target, int hitsInAttack, bool heavy)
        {
            if (Outcome != BattleOutcome.Ongoing || !attacker.Alive || !target.Alive || attacker.Side == target.Side)
            {
                return 0;
            }

            var dmg = Math.Min(target.Hp, DamageOf(attacker.Fighter.Atk, hitsInAttack, heavy));
            target.Hp -= dmg;
            attacker.DamageDealt += dmg;
            if (!target.Alive)
            {
                Defeated?.Invoke(target);
                Decide();
            }

            return dmg;
        }

        /// <summary>Nearest standing opponent from a position (x, z), ties to the same lane.</summary>
        public CombatUnit? NearestFoe(CombatUnit from, Func<CombatUnit, (float x, float z)> position)
        {
            var (fx, fz) = position(from);
            CombatUnit? best = null;
            var bestD = float.MaxValue;
            foreach (var u in _units)
            {
                if (u.Side == from.Side || !u.Alive)
                {
                    continue;
                }

                var (ux, uz) = position(u);
                var d = ((ux - fx) * (ux - fx)) + ((uz - fz) * (uz - fz));
                if (best == null || d < bestD - 1e-4f || (Math.Abs(d - bestD) <= 1e-4f && u.Fighter.Lane == from.Fighter.Lane))
                {
                    best = u;
                    bestD = d;
                }
            }

            return best;
        }

        public void Tick(double dt)
        {
            if (Outcome != BattleOutcome.Ongoing || dt <= 0)
            {
                return;
            }

            Elapsed += dt;
            if (Lineup.TimeoutSeconds > 0 && Elapsed >= Lineup.TimeoutSeconds)
            {
                TimedOut = true;
                Outcome = Share(CampaignSide.Hero) > Share(CampaignSide.Villain) ? BattleOutcome.Won : BattleOutcome.Lost;
            }
        }

        public void Retreat()
        {
            if (Outcome == BattleOutcome.Ongoing)
            {
                Retreated = true;
                Outcome = BattleOutcome.Lost;
            }
        }

        public int Standing(CampaignSide side)
        {
            var n = 0;
            foreach (var u in _units)
            {
                if (u.Side == side && u.Alive)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>Share of the side's total HP still standing, 0..1.</summary>
        public float Share(CampaignSide side)
        {
            var hp = 0;
            var max = 0;
            foreach (var u in _units)
            {
                if (u.Side == side)
                {
                    hp += u.Hp;
                    max += u.MaxHp;
                }
            }

            return max <= 0 ? 0f : hp / (float)max;
        }

        private void Decide()
        {
            if (Standing(CampaignSide.Villain) == 0)
            {
                Outcome = BattleOutcome.Won;
            }
            else if (Standing(CampaignSide.Hero) == 0)
            {
                Outcome = BattleOutcome.Lost;
            }
        }

        public BattleReport Result() => new BattleReport(
            Lineup.Stage.Id,
            Lineup.Stage.CampId,
            Outcome == BattleOutcome.Won,
            Elapsed,
            Standing(CampaignSide.Hero),
            Lineup.Enemies.Count - Standing(CampaignSide.Villain),
            TimedOut,
            Retreated);
    }

    public sealed record BattleReport(
        string StageId,
        string? CampId,
        bool Won,
        double Seconds,
        int HeroesStanding,
        int EnemiesDefeated,
        bool TimedOut = false,
        bool Retreated = false);

    /// <summary>
    /// Play → BattleLocked → Play: Fight puts the lineup here before the scene loads (BattleLocked boots in Awake), the
    /// battle reads it, and posts its result back. With nothing pending, BattleLocked plays its standalone demo.
    /// </summary>
    public static class BattleHandoff
    {
        public static BattleLineup? Pending { get; private set; }
        public static BattleReport? Result { get; private set; }

        public static event Action<BattleReport>? Completed;

        public static void Begin(BattleLineup lineup)
        {
            Pending = lineup ?? throw new ArgumentNullException(nameof(lineup));
            Result = null;
        }

        public static void Complete(BattleReport result)
        {
            Result = result;
            Pending = null;
            Completed?.Invoke(result);
        }

        public static BattleReport? TakeResult()
        {
            var r = Result;
            Result = null;
            return r;
        }

        public static void Reset()
        {
            Pending = null;
            Result = null;
            Completed = null;
        }
    }

    /// <summary>What a battle paid out.</summary>
    public sealed class RewardBundle
    {
        public Dictionary<string, int> Resources { get; } = new(StringComparer.Ordinal);
        public string ShardHero { get; set; } = "";
        public int Shards { get; set; }
        public bool FirstClear { get; set; }
        public bool Won { get; set; }

        /// <summary>Resource ids plus "shard.{hero id}" for the celebration rows.</summary>
        public Dictionary<string, int> AsRows()
        {
            var rows = new Dictionary<string, int>(Resources, StringComparer.Ordinal);
            if (Shards > 0 && ShardHero.Length > 0)
            {
                rows[ShardKey(ShardHero)] = Shards;
            }

            return rows;
        }

        public static string ShardKey(string heroId) => "shard." + heroId;
    }

    public static class BattleRewards
    {
        /// <summary>Win: the stage rewards, its hero shards and on the first clear its gems. Loss: a share of the gold only.</summary>
        public static RewardBundle For(StageDef stage, bool won, bool firstClear, double lossGoldShare)
        {
            var bundle = new RewardBundle { Won = won, FirstClear = won && firstClear };
            if (won)
            {
                foreach (var pair in stage.Rewards)
                {
                    if (pair.Value > 0)
                    {
                        bundle.Resources[pair.Key] = pair.Value;
                    }
                }

                if (firstClear && stage.FirstClearGems > 0)
                {
                    bundle.Resources.TryGetValue("res.hard", out var gems);
                    bundle.Resources["res.hard"] = gems + stage.FirstClearGems;
                }

                bundle.ShardHero = stage.ShardHero;
                bundle.Shards = stage.ShardHero.Length > 0 ? stage.Shards : 0;
            }
            else if (stage.Rewards.TryGetValue("res.soft", out var gold))
            {
                var share = (int)Math.Round(gold * Math.Max(0, lossGoldShare) / 10.0, MidpointRounding.AwayFromZero) * 10;
                if (share > 0)
                {
                    bundle.Resources["res.soft"] = share;
                }
            }

            return bundle;
        }
    }
}
