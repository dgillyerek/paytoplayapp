#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Campaign
{
    /// <summary>One character in a configured battle, with its stats already scaled by level and stars.</summary>
    public sealed class LineupFighter
    {
        public LineupFighter(CampaignHeroDef def, int lane, int level, int stars, HeroStats stats)
        {
            CharacterId = def.Id;
            Fighter = def.Fighter;
            Side = def.Side;
            Lane = lane;
            Level = level;
            Stars = stars;
            Hp = stats.Hp;
            Atk = stats.Atk;
            Power = stats.Power;
            PortraitKey = def.PortraitKey;
        }

        public string CharacterId { get; }

        /// <summary>BattleLocked roster name ("Sir Aldric").</summary>
        public string Fighter { get; }

        public CampaignSide Side { get; }
        public int Lane { get; }
        public int Level { get; }
        public int Stars { get; }
        public int Hp { get; }
        public int Atk { get; }
        public int Power { get; }
        public string PortraitKey { get; }
    }

    /// <summary>What Fight hands to BattleLocked: lanes of heroes vs villains, both sides' stats, the stage.</summary>
    public sealed class BattleLineup
    {
        public BattleLineup(StageDef stage, IReadOnlyList<LineupFighter> heroes, IReadOnlyList<LineupFighter> enemies, double timeoutSeconds)
        {
            Stage = stage;
            Heroes = heroes;
            Enemies = enemies;
            TimeoutSeconds = timeoutSeconds;
            var lanes = 0;
            foreach (var f in heroes)
            {
                lanes = Math.Max(lanes, f.Lane + 1);
            }

            foreach (var f in enemies)
            {
                lanes = Math.Max(lanes, f.Lane + 1);
            }

            LaneCount = lanes;
        }

        public StageDef Stage { get; }
        public IReadOnlyList<LineupFighter> Heroes { get; }
        public IReadOnlyList<LineupFighter> Enemies { get; }
        public int LaneCount { get; }
        public double TimeoutSeconds { get; }

        public IEnumerable<LineupFighter> All
        {
            get
            {
                foreach (var h in Heroes)
                {
                    yield return h;
                }

                foreach (var e in Enemies)
                {
                    yield return e;
                }
            }
        }

        public int HeroPower => Sum(Heroes);
        public int EnemyPower => Sum(Enemies);

        private static int Sum(IReadOnlyList<LineupFighter> list)
        {
            var p = 0;
            foreach (var f in list)
            {
                p += f.Power;
            }

            return p;
        }
    }

    public enum BattleOdds
    {
        Easy = 0,
        Even = 1,
        Hard = 2,
        VeryHard = 3
    }

    public enum SetupCheck
    {
        Ok = 0,
        NoHeroes,
        StageLocked
    }

    /// <summary>
    /// Battle Setup: the stage's villains stand in their lanes; the player puts owned heroes in lanes (one hero per lane,
    /// each hero once) and sees both sides' power before Fight.
    /// </summary>
    public sealed class BattleSetup
    {
        public const int MaxLanes = 3;

        private readonly string?[] _lanes;

        public BattleSetup(CampaignCatalog catalog, HeroRoster roster, StageDef stage, bool unlocked = true)
        {
            Catalog = catalog;
            Roster = roster;
            Stage = stage;
            Unlocked = unlocked;
            LaneCount = Math.Max(1, Math.Min(MaxLanes, stage.Enemies.Count));
            _lanes = new string?[LaneCount];
        }

        public CampaignCatalog Catalog { get; }
        public HeroRoster Roster { get; }
        public StageDef Stage { get; }
        public bool Unlocked { get; }
        public int LaneCount { get; }

        public event Action? Changed;

        public string? HeroAt(int lane) => lane >= 0 && lane < LaneCount ? _lanes[lane] : null;

        public StageEnemyDef? EnemyAt(int lane) => lane >= 0 && lane < LaneCount && lane < Stage.Enemies.Count ? Stage.Enemies[lane] : null;

        public int LaneOf(string heroId)
        {
            for (var i = 0; i < LaneCount; i++)
            {
                if (string.Equals(_lanes[i], heroId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        public int AssignedCount
        {
            get
            {
                var n = 0;
                foreach (var h in _lanes)
                {
                    if (h != null)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Puts an owned hero in a lane; it leaves any lane it was in, and replaces whoever stood there.</summary>
        public bool Assign(int lane, string heroId)
        {
            if (lane < 0 || lane >= LaneCount || !Roster.IsOwned(heroId))
            {
                return false;
            }

            var from = LaneOf(heroId);
            if (from >= 0)
            {
                _lanes[from] = null;
            }

            _lanes[lane] = heroId;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Tap a hero in the pool: off if it is in a lane, else into the first empty lane (or replace the last lane).</summary>
        public int Toggle(string heroId)
        {
            var at = LaneOf(heroId);
            if (at >= 0)
            {
                Clear(at);
                return -1;
            }

            for (var i = 0; i < LaneCount; i++)
            {
                if (_lanes[i] == null)
                {
                    return Assign(i, heroId) ? i : -1;
                }
            }

            return Assign(LaneCount - 1, heroId) ? LaneCount - 1 : -1;
        }

        public void Clear(int lane)
        {
            if (lane >= 0 && lane < LaneCount && _lanes[lane] != null)
            {
                _lanes[lane] = null;
                Changed?.Invoke();
            }
        }

        /// <summary>Strongest owned heroes against the strongest villains, lane by lane.</summary>
        public void AutoFill()
        {
            var heroes = new List<string>(Roster.OwnedIds());
            heroes.Sort((a, b) => Roster.Stats(b).Power.CompareTo(Roster.Stats(a).Power));
            var lanes = new List<int>();
            for (var i = 0; i < LaneCount; i++)
            {
                lanes.Add(i);
                _lanes[i] = null;
            }

            lanes.Sort((a, b) => EnemyStats(b).Power.CompareTo(EnemyStats(a).Power));
            for (var i = 0; i < lanes.Count && i < heroes.Count; i++)
            {
                _lanes[lanes[i]] = heroes[i];
            }

            Changed?.Invoke();
        }

        public HeroStats EnemyStats(int lane)
        {
            var e = EnemyAt(lane);
            var def = e != null ? Catalog.Hero(e.HeroId) : null;
            return def == null || e == null ? default : HeroStats.Of(def, Catalog.Curve, e.Level, e.Stars);
        }

        public int HeroPower
        {
            get
            {
                var p = 0;
                foreach (var h in _lanes)
                {
                    if (h != null)
                    {
                        p += Roster.Stats(h).Power;
                    }
                }

                return p;
            }
        }

        public int EnemyPower
        {
            get
            {
                var p = 0;
                for (var i = 0; i < LaneCount; i++)
                {
                    p += EnemyStats(i).Power;
                }

                return p;
            }
        }

        public BattleOdds Odds => OddsOf(HeroPower, EnemyPower);

        public static BattleOdds OddsOf(int heroPower, int enemyPower)
        {
            if (heroPower <= 0)
            {
                return BattleOdds.VeryHard;
            }

            var ratio = heroPower / (double)Math.Max(1, enemyPower);
            return ratio >= 1.25 ? BattleOdds.Easy : ratio >= 0.9 ? BattleOdds.Even : ratio >= 0.65 ? BattleOdds.Hard : BattleOdds.VeryHard;
        }

        public SetupCheck CanFight => !Unlocked ? SetupCheck.StageLocked : AssignedCount == 0 ? SetupCheck.NoHeroes : SetupCheck.Ok;

        public BattleLineup Build()
        {
            var heroes = new List<LineupFighter>();
            var enemies = new List<LineupFighter>();
            for (var i = 0; i < LaneCount; i++)
            {
                var h = _lanes[i];
                var hd = h != null ? Catalog.Hero(h) : null;
                if (hd != null)
                {
                    var p = Roster.Get(hd.Id)!;
                    heroes.Add(new LineupFighter(hd, i, p.Level, p.Stars, Roster.Stats(hd.Id)));
                }

                var e = EnemyAt(i);
                var ed = e != null ? Catalog.Hero(e.HeroId) : null;
                if (e != null && ed != null)
                {
                    enemies.Add(new LineupFighter(ed, i, e.Level, e.Stars, HeroStats.Of(ed, Catalog.Curve, e.Level, e.Stars)));
                }
            }

            return new BattleLineup(Stage, heroes, enemies, Catalog.BattleTimeoutSeconds);
        }
    }

    /// <summary>Lane centres across the field for a configured battle: lanes as wide as their widest body, centred on x = 0.</summary>
    public static class BattleLaneLayout
    {
        public const float BaseLaneMetres = 2.6f;

        public static float LaneWidth(float largestBattleScale) => BaseLaneMetres * Math.Max(1f, largestBattleScale) * (largestBattleScale > 1.01f ? 0.85f : 1f);

        public static float[] Centres(IReadOnlyList<float> widths)
        {
            var total = 0f;
            foreach (var w in widths)
            {
                total += w;
            }

            var centres = new float[widths.Count];
            var x = -total * 0.5f;
            for (var i = 0; i < widths.Count; i++)
            {
                centres[i] = x + (widths[i] * 0.5f);
                x += widths[i];
            }

            return centres;
        }
    }
}
