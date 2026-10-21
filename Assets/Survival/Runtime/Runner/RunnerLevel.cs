#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Serialization;

namespace Survival.Domain.Runner
{
    /// <summary>What a bonus box gives once it is shot or hit down to 0 HP. Nothing in the lane is a free pickup.</summary>
    public enum RunnerBoxKind
    {
        /// <summary>Copies of a hero unit join the squad.</summary>
        Followers,

        /// <summary>A new hero joins the squad (one unit, celebrated by name).</summary>
        Hero,

        /// <summary>Weapon upgrade: the whole squad hits harder (+Amount, 0.25 = +25%).</summary>
        Weapon,

        /// <summary>Attack speed upgrade: the whole squad attacks faster (+Amount, 0.25 = +25%).</summary>
        Rate
    }

    public enum RunnerRowKind
    {
        Enemy,
        Box,
        Boss
    }

    /// <summary>
    /// A runner unit: a locked battle rig (<see cref="Roster"/> is the <c>LockedBattleRoster</c> name, whose model, materials and
    /// walk / attack clips the scene reuses) with runner stats. Distances are metres, times seconds.
    /// </summary>
    public sealed record RunnerUnitDef(
        string Id,
        string Roster,
        string HeroId,
        int Hp,
        int Damage,
        float Cycle,
        float Range,
        bool Ranged,
        float ShotSpeed,
        float Speed,
        float Radius);

    public sealed record RunnerBoxDef(int Hp, RunnerBoxKind Kind, string Unit, int Count, float Amount)
    {
        public bool GivesUnits => Kind == RunnerBoxKind.Followers || Kind == RunnerBoxKind.Hero;

        /// <summary>Painted art: the hero chest for followers and heroes, the sword crate and the lightning crate for upgrades.</summary>
        public string ArtKey => Kind switch
        {
            RunnerBoxKind.Weapon => RunnerArtKeys.CrateWeapon,
            RunnerBoxKind.Rate => RunnerArtKeys.CrateSpeed,
            _ => RunnerArtKeys.ChestHero
        };
    }

    /// <summary>
    /// One spawn row: at <see cref="At"/> metres down the lane, <see cref="Count"/> enemies of <see cref="Unit"/> spread
    /// <see cref="Spread"/> metres apart around <see cref="X"/>, one box, or the boss.
    /// </summary>
    public sealed record RunnerRowDef(float At, RunnerRowKind Kind, float X, string Unit, int Count, float Spread, RunnerBoxDef? Box)
    {
        public float XOf(int index) => X + ((index - ((Count - 1) * 0.5f)) * Spread);
    }

    public sealed record RunnerRewardDef(int WinGold, int GoldPerKill, int GoldPerBox, int Shards, string ShardHero);

    public sealed record RunnerLevelDef(
        string Id,
        string Title,
        string Hero,
        float Length,
        float Speed,
        float HalfWidth,
        int MaxFollowers,
        IReadOnlyList<RunnerRowDef> Rows,
        RunnerRewardDef Rewards)
    {
        public bool HasBoss
        {
            get
            {
                foreach (var r in Rows)
                {
                    if (r.Kind == RunnerRowKind.Boss)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int EnemyCount
        {
            get
            {
                var n = 0;
                foreach (var r in Rows)
                {
                    n += r.Kind == RunnerRowKind.Enemy ? r.Count : r.Kind == RunnerRowKind.Boss ? 1 : 0;
                }

                return n;
            }
        }

        public int BoxCount
        {
            get
            {
                var n = 0;
                foreach (var r in Rows)
                {
                    n += r.Kind == RunnerRowKind.Box ? 1 : 0;
                }

                return n;
            }
        }
    }

    public static class RunnerArtKeys
    {
        public const string ChestHero = "theme_a.hook.icon.chest_closed";
        public const string ChestOpen = "theme_a.hook.icon.chest_open";
        public const string CrateWeapon = "theme_a.runner.crate_weapon";
        public const string CrateSpeed = "theme_a.runner.crate_speed";
        public const string EventIcon = "theme_a.runner.icon";
        public const string PortraitPrefix = "theme_a.hero.portrait.";

        /// <summary>Painted portrait key of a roster unit ("Sir Aldric" → theme_a.hero.portrait.sir_aldric).</summary>
        public static string Portrait(string roster) =>
            PortraitPrefix + roster.Trim().ToLowerInvariant().Replace(' ', '_');

        public static IReadOnlyList<string> Required { get; } = new[] { CrateWeapon, CrateSpeed, EventIcon };
    }

    /// <summary>runner_levels.json: the unit stats and the levels (spawn rows by distance). More levels are more JSON.</summary>
    public sealed class RunnerCatalog
    {
        public const string FileName = "runner_levels.json";

        private readonly Dictionary<string, RunnerUnitDef> _units;
        private readonly List<RunnerLevelDef> _levels;

        public RunnerCatalog(IEnumerable<RunnerUnitDef> units, IEnumerable<RunnerLevelDef> levels)
        {
            _units = new Dictionary<string, RunnerUnitDef>(StringComparer.Ordinal);
            foreach (var u in units)
            {
                _units[u.Id] = u;
            }

            _levels = new List<RunnerLevelDef>(levels);
            foreach (var level in _levels)
            {
                Validate(level);
            }
        }

        public IReadOnlyDictionary<string, RunnerUnitDef> Units => _units;
        public IReadOnlyList<RunnerLevelDef> Levels => _levels;

        public RunnerUnitDef Unit(string id) =>
            _units.TryGetValue(id, out var u) ? u : throw new KeyNotFoundException("Runner unit '" + id + "' is not in " + FileName + ".");

        public RunnerLevelDef? Level(string? id)
        {
            foreach (var l in _levels)
            {
                if (string.Equals(l.Id, id, StringComparison.Ordinal))
                {
                    return l;
                }
            }

            return null;
        }

        public RunnerLevelDef First => _levels.Count > 0 ? _levels[0] : throw new InvalidOperationException(FileName + " has no levels.");

        private void Validate(RunnerLevelDef level)
        {
            Unit(level.Hero);
            foreach (var row in level.Rows)
            {
                if (row.At < 0f || row.At > level.Length)
                {
                    throw new FormatException(level.Id + ": row at " + row.At + " m is outside the 0.." + level.Length + " m lane.");
                }

                if (row.Kind == RunnerRowKind.Box)
                {
                    if (row.Box == null || row.Box.Hp <= 0)
                    {
                        throw new FormatException(level.Id + ": box at " + row.At + " m needs hp > 0.");
                    }

                    if (row.Box.GivesUnits)
                    {
                        Unit(row.Box.Unit);
                    }
                }
                else
                {
                    Unit(row.Unit);
                }
            }
        }

        public static RunnerCatalog Parse(string json)
        {
            var root = LayoutJson.Root(json, FileName);
            var units = new List<RunnerUnitDef>();
            if (root.TryGet("units", out var un) && un is JsonNode.Obj unitsObj)
            {
                foreach (var pair in unitsObj.Fields)
                {
                    if (pair.Value is not JsonNode.Obj u)
                    {
                        continue;
                    }

                    var ranged = LayoutJson.OptBool(u, "ranged", false);
                    units.Add(new RunnerUnitDef(
                        pair.Key,
                        LayoutJson.Str(u, "roster"),
                        LayoutJson.OptStr(u, "hero", ""),
                        Math.Max(1, LayoutJson.OptInt(u, "hp", 1)),
                        Math.Max(0, LayoutJson.OptInt(u, "damage", 10)),
                        Math.Max(0.1f, LayoutJson.OptNum(u, "cycle", 1f)),
                        Math.Max(0.5f, LayoutJson.OptNum(u, "range", ranged ? 10f : 2f)),
                        ranged,
                        LayoutJson.OptNum(u, "shot_speed", 16f),
                        Math.Max(0f, LayoutJson.OptNum(u, "speed", 0f)),
                        Math.Max(0.1f, LayoutJson.OptNum(u, "radius", 0.45f))));
                }
            }

            var levels = new List<RunnerLevelDef>();
            foreach (var l in LayoutJson.Objs(root, "levels"))
            {
                var rows = new List<RunnerRowDef>();
                foreach (var r in LayoutJson.Objs(l, "rows"))
                {
                    rows.Add(ReadRow(r));
                }

                rows.Sort((a, b) => a.At.CompareTo(b.At));
                var rw = l.TryGet("rewards", out var rn) && rn is JsonNode.Obj ro ? ro : new JsonNode.Obj();
                levels.Add(new RunnerLevelDef(
                    LayoutJson.Str(l, "id"),
                    LayoutJson.OptStr(l, "title", LayoutJson.Str(l, "id")),
                    LayoutJson.Str(l, "hero"),
                    Math.Max(10f, LayoutJson.Num(l, "length")),
                    Math.Max(0.2f, LayoutJson.OptNum(l, "speed", 1.6f)),
                    Math.Max(1.5f, LayoutJson.OptNum(l, "half_width", 4f)),
                    Math.Max(0, LayoutJson.OptInt(l, "max_followers", 24)),
                    rows,
                    new RunnerRewardDef(
                        LayoutJson.OptInt(rw, "win_gold", 0),
                        LayoutJson.OptInt(rw, "gold_per_kill", 0),
                        LayoutJson.OptInt(rw, "gold_per_box", 0),
                        LayoutJson.OptInt(rw, "shards", 0),
                        LayoutJson.OptStr(rw, "shard_hero", ""))));
            }

            return new RunnerCatalog(units, levels);
        }

        private static RunnerRowDef ReadRow(JsonNode.Obj r)
        {
            var at = LayoutJson.Num(r, "at");
            var kindText = LayoutJson.Str(r, "kind");
            var x = LayoutJson.OptNum(r, "x", 0f);
            switch (kindText)
            {
                case "enemy":
                    return new RunnerRowDef(at, RunnerRowKind.Enemy, x, LayoutJson.Str(r, "unit"), Math.Max(1, LayoutJson.OptInt(r, "count", 1)), LayoutJson.OptNum(r, "spread", 1.4f), null);
                case "boss":
                    return new RunnerRowDef(at, RunnerRowKind.Boss, x, LayoutJson.Str(r, "unit"), 1, 0f, null);
                case "box":
                {
                    var give = LayoutJson.Str(r, "reward");
                    var kind = give switch
                    {
                        "followers" => RunnerBoxKind.Followers,
                        "hero" => RunnerBoxKind.Hero,
                        "weapon" => RunnerBoxKind.Weapon,
                        "rate" => RunnerBoxKind.Rate,
                        _ => throw new FormatException("Runner box reward '" + give + "' is not followers / hero / weapon / rate.")
                    };
                    var box = new RunnerBoxDef(
                        LayoutJson.OptInt(r, "hp", 0),
                        kind,
                        LayoutJson.OptStr(r, "unit", ""),
                        kind == RunnerBoxKind.Hero ? 1 : Math.Max(1, LayoutJson.OptInt(r, "count", 1)),
                        LayoutJson.OptNum(r, "amount", 0.25f));
                    return new RunnerRowDef(at, RunnerRowKind.Box, x, box.Unit, 1, 0f, box);
                }

                default:
                    throw new FormatException("Runner row kind '" + kindText + "' at " + at.ToString(CultureInfo.InvariantCulture) + " m is not enemy / box / boss.");
            }
        }
    }
}
