#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Roster;
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
        Rate,

        /// <summary>Range upgrade: the squad's shots fly further (+Amount, 0.15 = +15%).</summary>
        Range,

        /// <summary>Healing: the hero gets back Amount of its max HP (0.35 = 35%).</summary>
        Heal
    }

    public enum RunnerRowKind
    {
        Enemy,
        Box,
        Boss,

        /// <summary>A trap in the lane (spikes, a rolling log, a fire patch): steer round it; it cannot be shot.</summary>
        Hazard,

        /// <summary>A breakable obstacle (palisade gate, stone wall, spiked barricade): shoot it down or steer round it; walking into it hurts.</summary>
        Barrier
    }

    /// <summary>
    /// Breakable road obstacles (Derek 2026-10-26: "some obstacles that can get destroyed but do damage if you don't avoid
    /// them. maybe some type of gate or stone fence. something time period appropriate"). See <see cref="RunnerBarrierDef"/>.
    /// </summary>
    public enum RunnerBarrierKind
    {
        /// <summary>A wooden palisade gate: sharpened oak stakes on cross beams. Middling HP and bite.</summary>
        Palisade,

        /// <summary>A dry-stone field wall: the toughest to shoot down, a hard knock to run into.</summary>
        StoneWall,

        /// <summary>A cheval de frise: crossed sharpened stakes on a log. Quick to break, nasty to walk into.</summary>
        Barricade
    }

    /// <summary>
    /// A breakable obstacle: <see cref="Width"/> metres across the lane (centred on the row's x), <see cref="Hp"/> that the
    /// squad's shots chip down, and <see cref="Damage"/> to every squad member it touches when the squad walks into it while
    /// it still stands (the hero takes all of it, a follower a trap's share); that contact smashes it. A barrier as wide as
    /// the road cannot be steered round: it has to be shot down (or walked through, at that price).
    /// </summary>
    public sealed record RunnerBarrierDef(RunnerBarrierKind Kind, float Width, int Hp, int Damage)
    {
        public static string Name(RunnerBarrierKind kind) => kind switch
        {
            RunnerBarrierKind.StoneWall => "stone_wall",
            RunnerBarrierKind.Barricade => "barricade",
            _ => "palisade"
        };

        public static RunnerBarrierKind Parse(string name) => name switch
        {
            "palisade" => RunnerBarrierKind.Palisade,
            "stone_wall" => RunnerBarrierKind.StoneWall,
            "barricade" => RunnerBarrierKind.Barricade,
            _ => throw new FormatException("Runner barrier '" + name + "' is not palisade / stone_wall / barricade.")
        };

        /// <summary>True when it spans a road <paramref name="halfWidth"/> either side of the centre line (no way round).</summary>
        public bool FullWidth(float halfWidth) => Width >= (2f * halfWidth) - 0.05f;
    }

    /// <summary>Lane traps. Each one shows a warning before it can hurt (see <see cref="RunnerHazardDef"/>).</summary>
    public enum RunnerHazardKind
    {
        /// <summary>A spike strip: it cycles down, then a rattle warning, then the spikes stand up (only then does it hurt).</summary>
        Spikes,

        /// <summary>A log at the top of the lane: it wobbles as a warning, then rolls back down its lane at the squad.</summary>
        Log,

        /// <summary>A fire patch: ringed in scorch marks, burns whatever walks through it, always on.</summary>
        Fire
    }

    /// <summary>
    /// A lane trap: <see cref="Width"/> metres across the lane (centred on the row's x), <see cref="Damage"/> to the hero
    /// on contact (a follower it touches is knocked out). Spikes and logs use <see cref="Phase"/> to stagger their cycle.
    /// </summary>
    public sealed record RunnerHazardDef(RunnerHazardKind Kind, float Width, int Damage, float Phase = 0f)
    {
        public static string Name(RunnerHazardKind kind) => kind switch
        {
            RunnerHazardKind.Spikes => "spikes",
            RunnerHazardKind.Log => "log",
            _ => "fire"
        };
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
        float Radius)
    {
        /// <summary>
        /// Runner body scale on top of the battle size (1 = battle size). The JSON radius is the scale-1 body; <see cref="Radius"/>
        /// is already multiplied by this, so the hit body, lane clamp and bars grow with the model.
        /// </summary>
        public float Scale { get; init; } = 1f;
    }

    public sealed record RunnerBoxDef(int Hp, RunnerBoxKind Kind, string Unit, int Count, float Amount)
    {
        public bool GivesUnits => Kind == RunnerBoxKind.Followers || Kind == RunnerBoxKind.Hero;

        /// <summary>Painted art: the hero chest for followers and heroes, the sword crate and the lightning crate for upgrades.</summary>
        public string ArtKey => Kind switch
        {
            RunnerBoxKind.Weapon => RunnerArtKeys.CrateWeapon,
            RunnerBoxKind.Rate => RunnerArtKeys.CrateSpeed,
            RunnerBoxKind.Range => RunnerArtKeys.CrateRange,
            RunnerBoxKind.Heal => RunnerArtKeys.CrateHeal,
            _ => RunnerArtKeys.ChestHero
        };
    }

    /// <summary>
    /// One spawn row: at <see cref="At"/> metres down the lane, <see cref="Count"/> enemies of <see cref="Unit"/> spread
    /// <see cref="Spread"/> metres apart around <see cref="X"/>, one box, or the boss.
    /// </summary>
    public sealed record RunnerRowDef(float At, RunnerRowKind Kind, float X, string Unit, int Count, float Spread, RunnerBoxDef? Box)
    {
        /// <summary>The trap, on a <see cref="RunnerRowKind.Hazard"/> row.</summary>
        public RunnerHazardDef? Hazard { get; init; }

        /// <summary>The obstacle, on a <see cref="RunnerRowKind.Barrier"/> row.</summary>
        public RunnerBarrierDef? Barrier { get; init; }

        /// <summary>
        /// Part of a no-boss level's closing set piece (its trap gauntlet, fodder push and captain; see
        /// RunnerProgression's Finale), beyond the archer line a boss level has as its guard.
        /// </summary>
        public bool Finale { get; init; }

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
        /// <summary>1-based place in the level sequence (Level 1 = Thornwood Road).</summary>
        public int Number { get; init; } = 1;

        /// <summary>Shown when the pack has no string for <see cref="Title"/>.</summary>
        public string DisplayName { get; init; } = "";

        /// <summary>Every enemy's HP on this level is its unit HP times this (progressive levels toughen the same units).</summary>
        public float FoeHpMul { get; init; } = 1f;

        /// <summary>The boss's HP is its unit HP times this.</summary>
        public float BossHpMul { get; init; } = 1f;

        /// <summary>Every enemy's (and the boss's) hit is its unit damage times this; 1 up to Level 12, then the ramp (see <see cref="RunnerProgression"/>).</summary>
        public float FoeDmgMul { get; init; } = 1f;

        /// <summary>
        /// Whether the squad runs for the finish once the road ahead is clear on a level without a boss (json
        /// "gate_charge", off by default; the campaign's first level turns it on and the generated levels copy it).
        /// </summary>
        public bool GateCharge { get; init; }

        /// <summary>Boss HP on this level (0 without a boss).</summary>
        public int BossHp(RunnerCatalog catalog)
        {
            foreach (var r in Rows)
            {
                if (r.Kind == RunnerRowKind.Boss)
                {
                    return Math.Max(1, (int)Math.Round(catalog.Unit(r.Unit).Hp * BossHpMul));
                }
            }

            return 0;
        }

        /// <summary>Sum of every non-boss enemy's HP on this level.</summary>
        public int FoeHpTotal(RunnerCatalog catalog)
        {
            var hp = 0;
            foreach (var r in Rows)
            {
                if (r.Kind == RunnerRowKind.Enemy)
                {
                    hp += r.Count * Math.Max(1, (int)Math.Round(catalog.Unit(r.Unit).Hp * FoeHpMul));
                }
            }

            return hp;
        }

        public int BoxHpTotal
        {
            get
            {
                var hp = 0;
                foreach (var r in Rows)
                {
                    hp += r.Box?.Hp ?? 0;
                }

                return hp;
            }
        }

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

        public int HazardCount
        {
            get
            {
                var n = 0;
                foreach (var r in Rows)
                {
                    n += r.Kind == RunnerRowKind.Hazard ? 1 : 0;
                }

                return n;
            }
        }

        public int BarrierCount
        {
            get
            {
                var n = 0;
                foreach (var r in Rows)
                {
                    n += r.Kind == RunnerRowKind.Barrier ? 1 : 0;
                }

                return n;
            }
        }

        /// <summary>Barriers as wide as the road (shoot them down or walk through them).</summary>
        public int FullWidthBarrierCount
        {
            get
            {
                var n = 0;
                foreach (var r in Rows)
                {
                    n += r.Barrier != null && r.Barrier.FullWidth(HalfWidth) ? 1 : 0;
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

        /// <summary>Range and heal crates reuse the painted crates, tinted in the scene (the pack has no third crate yet).</summary>
        public const string CrateRange = CrateWeapon;
        public const string CrateHeal = CrateSpeed;
        public const string EventIcon = "theme_a.runner.icon";
        public const string PortraitPrefix = "theme_a.hero.portrait.";

        /// <summary>Painted portrait key of a roster unit ("Sir Aldric" → theme_a.hero.portrait.sir_aldric).</summary>
        public static string Portrait(string roster) =>
            PortraitPrefix + roster.Trim().ToLowerInvariant().Replace(' ', '_');

        public static IReadOnlyList<string> Required { get; } = new[] { CrateWeapon, CrateSpeed, EventIcon };
    }

    /// <summary>
    /// Who may be on the runner's squad. The runner fires straight down the road, so only heroes with a ranged attack
    /// belong: a fighter whose every attack is a melee swing (Sir Aldric) is not a start hero, a follower box, a hero
    /// chest or a captive. Read from the locked battle data (an attack with no melee contact frame, or a flight streak,
    /// is a shot), so a roster change there flows through.
    /// </summary>
    public static class RunnerRoster
    {
        /// <summary>The order a stand-in hero is picked in when a requested one is not allowed.</summary>
        public static readonly string[] Fallbacks = { "lyra", "rowan", "oakenshield" };

        /// <summary>True when at least one of the roster fighter's attacks is a projectile (an unknown name is judged by its unit flag alone).</summary>
        public static bool IsRangedFighter(string roster)
        {
            var found = false;
            var fighter = LockedBattleRoster.All.Count > 0 ? LockedBattleRoster.All[0] : default;
            foreach (var f in LockedBattleRoster.All)
            {
                if (string.Equals(f.Name, roster, StringComparison.OrdinalIgnoreCase))
                {
                    fighter = f;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return true;
            }

            var profile = LockedBattleHits.ProfileOf(fighter.Name);
            for (var i = 0; i < LockedBattleRotation.Count(fighter); i++)
            {
                if (LockedBattleRotation.ContactFrame(fighter, profile, i) < 0 || LockedBattleRotation.AttackAt(fighter, i).FlightFrames > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A unit that may stand on the squad: flagged ranged, and a fighter that really shoots.</summary>
        public static bool IsPlayable(RunnerUnitDef unit) => unit.Ranged && IsRangedFighter(unit.Roster);

        /// <summary>
        /// The hero a run starts with: <paramref name="preferredId"/> (a saved choice, say) when the catalog has it and
        /// it may be on the squad, else the first of <see cref="Fallbacks"/> (lyra, rowan, oakenshield) it has, else any
        /// ranged hero. A stale selection naming a melee hero therefore never reaches the road.
        /// </summary>
        public static RunnerUnitDef? PlayableHero(RunnerCatalog catalog, string? preferredId)
        {
            if (!string.IsNullOrEmpty(preferredId) && catalog.Units.TryGetValue(preferredId!, out var wanted) && !string.IsNullOrEmpty(wanted.HeroId) && IsPlayable(wanted))
            {
                return wanted;
            }

            foreach (var id in Fallbacks)
            {
                if (catalog.Units.TryGetValue(id, out var u) && IsPlayable(u))
                {
                    return u;
                }
            }

            foreach (var u in catalog.Units.Values)
            {
                if (!string.IsNullOrEmpty(u.HeroId) && IsPlayable(u))
                {
                    return u;
                }
            }

            return null;
        }
    }

    /// <summary>runner_levels.json: the unit stats and the levels (spawn rows by distance). More levels are more JSON.</summary>
    public sealed class RunnerCatalog
    {
        public const string FileName = "runner_levels.json";

        private readonly Dictionary<string, RunnerUnitDef> _units;
        private readonly List<RunnerLevelDef> _levels;

        private readonly bool _allowMeleeHeroes;

        /// <param name="allowMeleeHeroes">
        /// Off by default: the runner never fields a hero whose attacks are all melee (the start hero, a follower or
        /// hero box, a captive), see <see cref="RunnerRoster"/>. Tests of the melee squad rules switch it on.
        /// </param>
        public RunnerCatalog(IEnumerable<RunnerUnitDef> units, IEnumerable<RunnerLevelDef> levels, bool allowMeleeHeroes = false)
        {
            _allowMeleeHeroes = allowMeleeHeroes;
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

        /// <summary>The level generator's settings, when the file has a "progression" block.</summary>
        public RunnerProgressionDef? Progression { get; private set; }

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

        /// <summary>Level <paramref name="number"/> (1-based), clamped into the sequence.</summary>
        public RunnerLevelDef ByNumber(int number) => _levels[Math.Max(0, Math.Min(_levels.Count - 1, number - 1))];

        /// <summary>The level after <paramref name="level"/>, or null after the last.</summary>
        public RunnerLevelDef? Next(RunnerLevelDef level) => level.Number < _levels.Count ? _levels[level.Number] : null;

        private RunnerUnitDef SquadUnit(string id, string where)
        {
            var u = Unit(id);
            if (!_allowMeleeHeroes && !RunnerRoster.IsPlayable(u))
            {
                throw new FormatException(where + ": '" + id + "' (" + u.Roster + ") is melee-only; the runner squad is ranged heroes only.");
            }

            return u;
        }

        private void Validate(RunnerLevelDef level)
        {
            SquadUnit(level.Hero, level.Id + " hero");
            foreach (var row in level.Rows)
            {
                if (row.At < 0f || row.At > level.Length)
                {
                    throw new FormatException(level.Id + ": row at " + row.At + " m is outside the 0.." + level.Length + " m lane.");
                }

                if (row.Kind == RunnerRowKind.Hazard)
                {
                    if (row.Hazard == null || row.Hazard.Width <= 0f)
                    {
                        throw new FormatException(level.Id + ": hazard at " + row.At + " m needs a width > 0.");
                    }
                }
                else if (row.Kind == RunnerRowKind.Barrier)
                {
                    if (row.Barrier == null || row.Barrier.Width <= 0f || row.Barrier.Hp <= 0)
                    {
                        throw new FormatException(level.Id + ": barrier at " + row.At + " m needs a width > 0 and hp > 0.");
                    }
                }
                else if (row.Kind == RunnerRowKind.Box)
                {
                    if (row.Box == null || row.Box.Hp <= 0)
                    {
                        throw new FormatException(level.Id + ": box at " + row.At + " m needs hp > 0.");
                    }

                    if (row.Box.GivesUnits)
                    {
                        SquadUnit(row.Box.Unit, level.Id + ": box at " + row.At + " m");
                    }
                }
                else
                {
                    Unit(row.Unit);
                }
            }
        }

        public static RunnerCatalog Parse(string json) => Parse(json, false);

        internal static RunnerCatalog Parse(string json, bool allowMeleeHeroes)
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
                    var scale = Math.Max(0.2f, LayoutJson.OptNum(u, "scale", 1f));
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
                        Math.Max(0.1f, LayoutJson.OptNum(u, "radius", 0.45f)) * scale) { Scale = scale });
                }
            }

            var levels = new List<RunnerLevelDef>();
            foreach (var l in LayoutJson.Objs(root, "levels"))
            {
                var number = levels.Count + 1;
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
                    Math.Min(RunnerRules.MaxFollowers, Math.Max(0, LayoutJson.OptInt(l, "max_followers", RunnerRules.MaxFollowers))),
                    rows,
                    new RunnerRewardDef(
                        LayoutJson.OptInt(rw, "win_gold", 0),
                        LayoutJson.OptInt(rw, "gold_per_kill", 0),
                        LayoutJson.OptInt(rw, "gold_per_box", 0),
                        LayoutJson.OptInt(rw, "shards", 0),
                        LayoutJson.OptStr(rw, "shard_hero", "")))
                {
                    Number = number,
                    DisplayName = LayoutJson.OptStr(l, "name", LayoutJson.Str(l, "id")),
                    FoeHpMul = Math.Max(0.1f, LayoutJson.OptNum(l, "foe_hp_mul", 1f)),
                    BossHpMul = Math.Max(0.1f, LayoutJson.OptNum(l, "boss_hp_mul", 1f)),
                    FoeDmgMul = Math.Max(0.1f, LayoutJson.OptNum(l, "foe_dmg_mul", 1f)),
                    GateCharge = LayoutJson.OptBool(l, "gate_charge", false)
                });
            }

            // The authored levels come first (Level 1 = Thornwood Road); "progression" generates the rest from a seed.
            RunnerProgressionDef? progression = null;
            if (root.TryGet("progression", out var pn) && pn is JsonNode.Obj po && levels.Count > 0)
            {
                var prog = RunnerProgressionDef.Read(po);
                progression = prog;
                var have = new HashSet<string>(StringComparer.Ordinal);
                foreach (var u in units)
                {
                    have.Add(u.Id);
                }

                for (var n = levels.Count + 1; n <= prog.Count; n++)
                {
                    levels.Add(RunnerProgression.Generate(levels[0], prog, n, have.Contains));
                }
            }

            return new RunnerCatalog(units, levels, allowMeleeHeroes) { Progression = progression };
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
                        "range" => RunnerBoxKind.Range,
                        "heal" => RunnerBoxKind.Heal,
                        _ => throw new FormatException("Runner box reward '" + give + "' is not followers / hero / weapon / rate / range / heal.")
                    };
                    var box = new RunnerBoxDef(
                        LayoutJson.OptInt(r, "hp", 0),
                        kind,
                        LayoutJson.OptStr(r, "unit", ""),
                        kind == RunnerBoxKind.Hero ? 1 : Math.Max(1, LayoutJson.OptInt(r, "count", 1)),
                        LayoutJson.OptNum(r, "amount", 0.25f));
                    return new RunnerRowDef(at, RunnerRowKind.Box, x, box.Unit, 1, 0f, box);
                }

                case "hazard":
                {
                    var type = LayoutJson.OptStr(r, "hazard", "spikes");
                    var hk = type switch
                    {
                        "spikes" => RunnerHazardKind.Spikes,
                        "log" => RunnerHazardKind.Log,
                        "fire" => RunnerHazardKind.Fire,
                        _ => throw new FormatException("Runner hazard '" + type + "' is not spikes / log / fire.")
                    };
                    var hz = new RunnerHazardDef(hk, Math.Max(0.4f, LayoutJson.OptNum(r, "width", 1.8f)), Math.Max(0, LayoutJson.OptInt(r, "damage", 40)), LayoutJson.OptNum(r, "phase", 0f));
                    return new RunnerRowDef(at, RunnerRowKind.Hazard, x, "", 1, 0f, null) { Hazard = hz };
                }

                case "barrier":
                {
                    var bk = RunnerBarrierDef.Parse(LayoutJson.OptStr(r, "barrier", "palisade"));
                    var bd = new RunnerBarrierDef(bk, Math.Max(0.4f, LayoutJson.OptNum(r, "width", 3f)), Math.Max(1, LayoutJson.OptInt(r, "hp", 100)), Math.Max(0, LayoutJson.OptInt(r, "damage", 40)));
                    return new RunnerRowDef(at, RunnerRowKind.Barrier, x, "", 1, 0f, null) { Barrier = bd };
                }

                default:
                    throw new FormatException("Runner row kind '" + kindText + "' at " + at.ToString(CultureInfo.InvariantCulture) + " m is not enemy / box / boss / hazard / barrier.");
            }
        }
    }
}
