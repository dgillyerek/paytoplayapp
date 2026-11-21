#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Survival.Domain.Serialization;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// runner_levels.json "progression": how many levels the runner has, the seed that lays out levels 2+, and how
    /// fast each step gets harder. Steps are per level after the first (Level n gets 1 + step * (n - 1)).
    /// </summary>
    public sealed record RunnerProgressionDef(
        int Count,
        int Seed,
        float LengthStep,
        float SpeedStep,
        float FoeHpStep,
        float BossHpStep,
        float BoxHpStep,
        float RewardStep,
        IReadOnlyList<string> Names)
    {
        internal static RunnerProgressionDef Read(JsonNode.Obj o)
        {
            var names = new List<string>();
            if (o.TryGet("names", out var nn) && nn is JsonNode.Arr arr)
            {
                foreach (var n in arr.Items)
                {
                    if (n is JsonNode.Str s)
                    {
                        names.Add(s.Value);
                    }
                }
            }

            return new RunnerProgressionDef(
                Math.Max(1, LayoutJson.OptInt(o, "count", 1)),
                LayoutJson.OptInt(o, "seed", 1),
                Math.Max(0f, LayoutJson.OptNum(o, "length_step", 12f)),
                Math.Max(0f, LayoutJson.OptNum(o, "speed_step", 0.02f)),
                Math.Max(0f, LayoutJson.OptNum(o, "foe_hp_step", 0.12f)),
                Math.Max(0f, LayoutJson.OptNum(o, "boss_hp_step", 0.2f)),
                Math.Max(0f, LayoutJson.OptNum(o, "box_hp_step", 0.1f)),
                Math.Max(0f, LayoutJson.OptNum(o, "reward_step", 0.3f)),
                names)
            {
                Ramp = o.TryGet("ramp", out var rn) && rn is JsonNode.Obj ro ? RunnerRampDef.Read(ro) : null,
                BossEvery = Math.Max(1, LayoutJson.OptInt(o, "boss_every", 1)),
                Tweaks = RunnerLevelTweak.ReadAll(o),
                Spread = o.TryGet("spread", out var sn) && sn is JsonNode.Obj so ? RunnerSpreadDef.Read(so) : null
            };
        }

        /// <summary>Per-level hand adjustments ("level_tweaks"), by level number; levels not listed use <see cref="RunnerLevelTweak.None"/>.</summary>
        public IReadOnlyDictionary<int, RunnerLevelTweak> Tweaks { get; init; } = new Dictionary<int, RunnerLevelTweak>();

        /// <summary>The hand adjustment for Level <paramref name="number"/> (all 1.0 when none is set).</summary>
        public RunnerLevelTweak Tweak(int number) => Tweaks.TryGetValue(number, out var tw) ? tw : RunnerLevelTweak.None;

        /// <summary>The late-game ramp (levels past <see cref="RunnerRampDef.From"/>), or null for the plain linear steps.</summary>
        public RunnerRampDef? Ramp { get; init; }

        /// <summary>How a generated level's waves are spread down the road (<see cref="RunnerSpreadDef"/>), or null to keep each beat's wave together.</summary>
        public RunnerSpreadDef? Spread { get; init; }

        /// <summary>
        /// A big boss stands at the end of every <see cref="BossEvery"/>th level only (Derek 2026-10-26: "there should only
        /// be a big boss every 5 levels or so"): Levels 5, 10, 15, ... The other levels end on a last wave and the finish line.
        /// </summary>
        public int BossEvery { get; init; } = 1;

        /// <summary>Whether Level <paramref name="number"/> ends on the boss.</summary>
        public bool IsBossLevel(int number) => BossEvery <= 1 || (number >= 1 && number % BossEvery == 0);

        /// <summary>
        /// Milestone bosses inside the existing boss cadence. <see cref="BossEvery"/> is 5, so Levels 5, 10, 15, ...
        /// already end on a boss. Every 10th of those (L10, L20, L30, L40, L50) is Vorgath; the others stay Ironhowl.
        /// </summary>
        public const int MilestoneEvery = 10;

        /// <summary>Whether Level <paramref name="number"/>'s boss is the milestone end boss (Vorgath).</summary>
        public bool IsMilestoneBossLevel(int number) => IsBossLevel(number) && number >= MilestoneEvery && number % MilestoneEvery == 0;

        /// <summary>The t (level - 1) the enemy and box counts use: it stops growing at the ramp's count cap.</summary>
        public int CountT(int number) => Math.Max(0, Math.Min(number, Ramp?.CountCap ?? int.MaxValue) - 1);
    }

    /// <summary>
    /// runner_levels.json "progression" "spread" (Derek 2026-11-20: "too much downtime ... enemies come in big clumps that
    /// die too easily, then a long lull with nothing happening"). Each beat's wave used to stand within a few metres (front
    /// line, second line, heavy, skirmishers), so the whole wave reached the squad in one or two seconds and the road was empty
    /// until the next beat. With a spread, every wave row before the boss guard / finale is cut into packs of at most
    /// <see cref="Pack"/> and the packs are laid out so they reach the squad at an even rhythm from the level's first enemy to
    /// its last (same order, same units, same lanes, same HP; each pack moved by <see cref="Jitter"/> of a step at most so it
    /// does not tick like a metronome). Packs only move later (into the lull after their clump, <see cref="Window"/> metres
    /// at most), never ahead of where they used to meet the squad, so a level's opening still lets the squad take its first chests. Arrival allows for the pack's walk speed: a wolf pack stands further up the road than
    /// an archer pack meant to arrive at the same moment. The set-piece moments (boss guard, finale) stay where they were.
    /// </summary>
    public sealed record RunnerSpreadDef(int Pack, float Jitter, int From, float Window)
    {
        /// <summary>
        /// Levels up to this one (0: none) use <see cref="EarlyPack"/> / <see cref="EarlyWindow"/> instead (Derek 2026-11-20:
        /// the opening levels clump and lull too, but must stay gentle for a first-time player): a milder spread that keeps
        /// a little of each beat together, so a new player still meets small groups with a breather between them.
        /// </summary>
        public int EarlyUntil { get; init; }

        public int EarlyPack { get; init; } = 2;

        public float EarlyWindow { get; init; } = 10f;

        /// <summary>
        /// Derek 2026-11-20 ("more enemies"): every stream wave carries this many times its enemies (at least; the extra ones
        /// are rows marked <see cref="RunnerRowDef.Extra"/>, spread into the stream like the rest). The closing set piece
        /// (boss guard, finale) keeps its size. 1 = off.
        /// </summary>
        public float Crowd { get; init; } = 1f;

        /// <summary>Every level's enemy HP is multiplied by this to pay for <see cref="Crowd"/> (bosses unchanged).</summary>
        public float CrowdHp { get; init; } = 1f;

        /// <summary>Every level's enemy damage is multiplied by this to pay for <see cref="Crowd"/>.</summary>
        public float CrowdDmg { get; init; } = 1f;

        /// <summary>
        /// Units whose rows never get <see cref="Crowd"/> copies (json "crowd.skip"); their share goes to the next eligible row.
        /// Archers: a ranged hit on a follower is clamped to a 2-hit kill whatever <see cref="CrowdDmg"/> says
        /// (<see cref="RunnerRules.RangedFoeHit"/>), so extra archers cost followers that no stat cut pays back.
        /// </summary>
        public IReadOnlyList<string> CrowdSkip { get; init; } = Array.Empty<string>();

        /// <summary>Outer lanes and side entries (json "spread.lanes"; null: off). See <see cref="RunnerLanesDef"/>.</summary>
        public RunnerLanesDef? Lanes { get; init; }

        /// <summary>The early window on Level 1 alone (the authored tutorial road, 0: <see cref="EarlyWindow"/>).</summary>
        public float FirstWindow { get; init; }

        internal static RunnerSpreadDef Read(JsonNode.Obj o)
        {
            var s = new RunnerSpreadDef(
                Math.Max(1, LayoutJson.OptInt(o, "pack", 3)),
                Math.Max(0f, Math.Min(0.45f, LayoutJson.OptNum(o, "jitter", 0.25f))),
                Math.Max(1, LayoutJson.OptInt(o, "from", 2)),
                Math.Max(1f, LayoutJson.OptNum(o, "window", RunnerProgression.BeatMetres)));
            if (o.TryGet("early", out var en) && en is JsonNode.Obj eo)
            {
                s = s with
                {
                    EarlyUntil = Math.Max(0, LayoutJson.OptInt(eo, "until", 0)),
                    EarlyPack = Math.Max(1, LayoutJson.OptInt(eo, "pack", 2)),
                    EarlyWindow = Math.Max(1f, LayoutJson.OptNum(eo, "window", 10f)),
                    FirstWindow = Math.Max(0f, LayoutJson.OptNum(eo, "first_window", 0f))
                };
            }

            if (o.TryGet("crowd", out var cn) && cn is JsonNode.Obj co)
            {
                s = s with
                {
                    Crowd = Math.Max(1f, LayoutJson.OptNum(co, "count", 1f)),
                    CrowdHp = Math.Max(0.1f, LayoutJson.OptNum(co, "foe_hp", 1f)),
                    CrowdDmg = Math.Max(0.1f, LayoutJson.OptNum(co, "foe_dmg", 1f))
                };
                if (co.TryGet("skip", out var sk) && sk is JsonNode.Arr sa)
                {
                    s = s with { CrowdSkip = sa.Items.OfType<JsonNode.Str>().Select(x => x.Value).ToArray() };
                }
            }

            if (o.TryGet("lanes", out var ln) && ln is JsonNode.Obj lo)
            {
                s = s with { Lanes = RunnerLanesDef.Read(lo) };
            }

            return s;
        }

        /// <summary>The spread Level <paramref name="number"/> uses (the early pack / window on Levels 1-<see cref="EarlyUntil"/>).</summary>
        public RunnerSpreadDef For(int number) => number <= EarlyUntil ? this with { Pack = EarlyPack, Window = number == 1 && FirstWindow > 0f ? FirstWindow : EarlyWindow } : this;

        /// <summary>Metres ahead of the hero a foe counts as in the fight (about the squad's reach); arrival is timed to this line.</summary>
        public const float FightLine = 18f;
    }

    /// <summary>
    /// runner_levels.json "progression" "spread" "lanes" (Derek 2026-11-20: "more enemies need to come from the outside of the
    /// lanes and stay there. a lot of times you can just stay in the center without moving. the same for powerups"; "the
    /// wolves for example move to the center too quickly"; "some enemies can spawn in from the side ... they should come in
    /// with enough time to kill"). After the stream is laid out, an even <see cref="Outer"/> share of its enemy packs move to
    /// an outer lane (<see cref="OuterX"/> m from the centre, left or right by a roll) and hold it (no drift toward the hero);
    /// <see cref="Side"/> of all stream packs (taken from the outer ones, <see cref="SideUnits"/> only) instead walk in from
    /// beyond the road edge onto that outer lane, fast enough that they are on the road at least <see cref="SideMinSeconds"/>
    /// before they reach the squad. An even <see cref="Crates"/> share of the power-up crates move to the outer lanes
    /// (<see cref="CrateX"/>). Enemies left inside drift toward the hero at <see cref="Seek"/> (per unit, else
    /// <see cref="RunnerRules.EnemySeek"/>). Its own random stream, so nothing else about a level's roll changes.
    /// </summary>
    public sealed record RunnerLanesDef(float Outer, float OuterX, float Side, float SideOut, float SideSpeed, float SideMinSeconds, float Crates, float CrateX)
    {
        public IReadOnlyList<string> SideUnits { get; init; } = Array.Empty<string>();

        public IReadOnlyDictionary<string, float> Seek { get; init; } = new Dictionary<string, float>();

        /// <summary>From this level on (1: every level).</summary>
        public int From { get; init; } = 1;

        /// <summary>Metres from the squad a foe attacks from (melee reach plus bodies), for the side-entry timing.</summary>
        public const float ContactMetres = 2.5f;

        internal static RunnerLanesDef Read(JsonNode.Obj o)
        {
            float Share(string k) => Math.Max(0f, Math.Min(1f, LayoutJson.OptNum(o, k, 0f)));
            var l = new RunnerLanesDef(
                Share("outer"),
                Math.Max(0f, LayoutJson.OptNum(o, "outer_x", 3f)),
                Share("side"),
                Math.Max(0.5f, LayoutJson.OptNum(o, "side_out", 2f)),
                Math.Max(0.5f, LayoutJson.OptNum(o, "side_speed", 2.5f)),
                Math.Max(0f, LayoutJson.OptNum(o, "side_min_s", 3f)),
                Share("crates"),
                Math.Max(0f, LayoutJson.OptNum(o, "crate_x", 3.2f)))
            {
                From = Math.Max(1, LayoutJson.OptInt(o, "from", 1))
            };
            if (o.TryGet("side_units", out var su) && su is JsonNode.Arr sa)
            {
                l = l with { SideUnits = sa.Items.OfType<JsonNode.Str>().Select(x => x.Value).ToArray() };
            }

            if (o.TryGet("seek", out var sk) && sk is JsonNode.Obj so)
            {
                var d = new Dictionary<string, float>();
                foreach (var kv in so.Fields)
                {
                    if (!kv.Key.StartsWith("_", StringComparison.Ordinal) && kv.Value is JsonNode.Num n)
                    {
                        d[kv.Key] = Math.Max(0f, (float)n.Value);
                    }
                }

                l = l with { Seek = d };
            }

            return l;
        }
    }

    /// <summary>
    /// runner_levels.json "progression" "level_tweaks": a small hand adjustment to one generated level, keyed by its number
    /// ("19": { "wave": 0.85, "foe_hp": 0.9, "barrier_hp": 0.7 }). Everything defaults to 1.0. The layout (where every row
    /// stands, which units, traps and barriers) stays the seed's; "wave" scales how many enemies each wave group has
    /// (never below one), "foe_hp" the level's enemy HP and "barrier_hp" its barriers' HP. "layout" (default 0) re-rolls
    /// where the level's beats start and which set piece each is (RunnerProgression.BeatStarts and SetPieces) without
    /// changing what the beats hold. Used only to smooth a level the seed made much harder or easier than its neighbours
    /// (RunnerBalanceTests.No_level_needs_much_more_than_the_one_before).
    /// </summary>
    public sealed record RunnerLevelTweak(float Wave = 1f, float FoeHp = 1f, float BarrierHp = 1f, int Layout = 0)
    {
        public static readonly RunnerLevelTweak None = new();

        internal static Dictionary<int, RunnerLevelTweak> ReadAll(JsonNode.Obj progression)
        {
            var all = new Dictionary<int, RunnerLevelTweak>();
            if (!progression.TryGet("level_tweaks", out var tn) || tn is not JsonNode.Obj t)
            {
                return all;
            }

            foreach (var key in t.Fields.Keys)
            {
                if (key.StartsWith("_", StringComparison.Ordinal) || !int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    continue;
                }

                if (t.TryGet(key, out var v) && v is JsonNode.Obj o)
                {
                    all[n] = new RunnerLevelTweak(
                        Math.Max(0.1f, LayoutJson.OptNum(o, "wave", 1f)),
                        Math.Max(0.1f, LayoutJson.OptNum(o, "foe_hp", 1f)),
                        Math.Max(0.1f, LayoutJson.OptNum(o, "barrier_hp", 1f)),
                        (int)Math.Round(LayoutJson.OptNum(o, "layout", 0f)));
                }
            }

            return all;
        }
    }

    /// <summary>
    /// runner_levels.json "progression" "ramp": how levels past <see cref="From"/> keep getting harder once the road stops
    /// getting longer. Each level past From multiplies enemy HP, enemy damage and boss HP by the growth; each level past
    /// <see cref="WallFrom"/> by the (steeper) wall growth instead. That wall is what a free player's trained heroes stop
    /// keeping up with (RunnerBalance; tests/Survival.Domain.Tests/RunnerBalanceTests.cs).
    /// </summary>
    public sealed record RunnerRampDef(
        int From,
        int WallFrom,
        int CountCap,
        float LengthCap,
        float SpeedCap,
        float FoeHpGrowth,
        float FoeDmgGrowth,
        float BossHpGrowth,
        float WallHpGrowth,
        float WallDmgGrowth,
        float WallBossGrowth,
        float WallStepHp = 1f,
        float WallStepDmg = 1f)
    {
        internal static RunnerRampDef Read(JsonNode.Obj o) => new(
            Math.Max(1, LayoutJson.OptInt(o, "from", 12)),
            Math.Max(1, LayoutJson.OptInt(o, "wall_from", 30)),
            Math.Max(1, LayoutJson.OptInt(o, "count_cap", 20)),
            Math.Max(10f, LayoutJson.OptNum(o, "length_cap", 330f)),
            Math.Max(0.5f, LayoutJson.OptNum(o, "speed_cap", 2.3f)),
            Math.Max(1f, LayoutJson.OptNum(o, "foe_hp_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "foe_dmg_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "boss_hp_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_hp_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_dmg_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_boss_growth", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_step_hp", 1f)),
            Math.Max(1f, LayoutJson.OptNum(o, "wall_step_dmg", 1f)));

        /// <summary>
        /// Product of the per-level growths from From + 1 to <paramref name="number"/> (1 up to From), times the one-time
        /// <paramref name="step"/> from the first wall level on.
        /// </summary>
        public float Mul(int number, float growth, float wallGrowth, float step = 1f)
        {
            var m = 1.0;
            for (var n = From + 1; n <= number; n++)
            {
                m *= n > WallFrom ? wallGrowth : growth;
                if (n == WallFrom + 1)
                {
                    m *= step;
                }
            }

            return (float)m;
        }

        public float FoeHpMul(int number) => Mul(number, FoeHpGrowth, WallHpGrowth, WallStepHp);

        public float FoeDmgMul(int number) => Mul(number, FoeDmgGrowth, WallDmgGrowth, WallStepDmg);

        public float BossHpMul(int number) => Mul(number, BossHpGrowth, WallBossGrowth, WallStepHp);
    }

    /// <summary>
    /// The shape of one beat of a generated level (Derek 2026-10-27: "are they all different? ... it seems like each item is
    /// in the same place"). March: box, then the wave. WaveFirst: the wave before its box. Ambush: a box in the middle,
    /// the wave from both edges. TrapCorridor: traps on both edges back to back, fodder down the open middle. Barricade:
    /// a barrier on one edge with the wave lined up across the open side. ArcherNest: archers behind a wall on one edge.
    /// CrateRun: the beat's boxes back to back down the middle, traps on the edges. FodderRush: the
    /// front wave in three small packs across the road.
    /// </summary>
    public enum RunnerSetPiece
    {
        March,
        WaveFirst,
        Ambush,
        TrapCorridor,
        Barricade,
        ArcherNest,
        CrateRun,
        FodderRush
    }

    /// <summary>
    /// Lays out runner levels from Level 1 and a seed (Level 1's own rows in the JSON were laid out by <see cref="Rows"/>
    /// with number 1, then left editable). Every beat holds a follower chest or a power-up crate (damage, fire rate,
    /// range, heal, or a small extra-follower chest), a mixed wave (a fodder front line, a different second line,
    /// sometimes a heavy anchor), on some beats skirmishers, and from the second beat on lane traps (spikes, then fire,
    /// then rolling logs) and barriers that get more common with the level. What a beat holds follows its number; where
    /// the beats stand (9-18 m apart) and how each is laid out (its <see cref="RunnerSetPiece"/>) come from the seed and
    /// the level number, so no two levels share a road. Vespera's casters join from Level 4, Ashwyrm from Level 7.
    /// Deterministic: the same seed and number always give the same level.
    /// </summary>
    public static class RunnerProgression
    {
        /// <summary>A level without a boss ends this many metres past its last row (the finale's captain): the finish gate.</summary>
        public const float FinishPad = 3f;

        /// <summary>The finale (no-boss levels) starts this far before where the boss would stand (the old archer line's spot).</summary>
        public const float FinaleBack = 7f;

        /// <summary>From this level on the finale has two captains.</summary>
        public const int FinaleSecondCaptainFrom = 20;

        /// <summary>Metres from one beat (boxes, then a wave) to the next.</summary>
        public const float BeatMetres = 13f;

        /// <summary>Most enemies standing side by side in one row; bigger groups form ranks behind.</summary>
        public const int MaxPerRank = 5;

        /// <summary>How far past the first follower chest the opening wave stands (later waves stand 8 m past theirs).</summary>
        public const float FirstWaveMetres = 12f;

        /// <summary>A second trap on one beat in four from <see cref="SecondTrapFrom"/>, on every other beat from <see cref="SecondTrapEveryOtherFrom"/>.</summary>
        public const int SecondTrapFrom = 13, SecondTrapEveryOtherFrom = 22;

        /// <summary>
        /// Breakable barriers (Derek 2026-10-26): from Level 2 on every third beat, every other beat from Level 15, every beat
        /// from Level 25. Most stand against the edge of the road away from the beat's box (dodge or shoot), leaving at least
        /// 2 m open; from Level 6 every fourth one is a
        /// full-width palisade gate or stone wall that has to be shot down (or walked through, at its damage). HP is a share
        /// of the beat's box HP (so it scales with the level); damage grows a step a level like the traps'.
        /// </summary>
        public const int BarrierFrom = 2, BarrierEveryOtherFrom = 15, BarrierEveryBeatFrom = 25, FullWidthBarrierFrom = 6, FullWidthEvery = 4;

        public const int PalisadeDamage = 92, PalisadeDamageStep = 7, StoneWallDamage = 110, StoneWallDamageStep = 9, BarricadeDamage = 130, BarricadeDamageStep = 9;

        /// <summary>
        /// Barrier HP as a share of the beat's box HP: a barricade breaks fastest, a stone wall slowest; a full-width one at
        /// <see cref="FullWidthHpShare"/>. About 2.75x the Oct 26 shares (0.15 / 0.22 / 0.3 / 0.1), so a barrier takes sustained
        /// fire to break (Derek 2026-10-27: "the destructible obstacles are too weak"); contact damage went up about 1.85x with it.
        /// </summary>
        public const float BarricadeHpShare = 0.41f, PalisadeHpShare = 0.6f, StoneWallHpShare = 0.83f, FullWidthHpShare = 0.28f;

        /// <summary>Metres past the beat's chest a barrier stands (behind the crate, before the skirmishers).</summary>
        public const float BarrierOffset = 3f;

        /// <summary>Least HP a side barrier / a full-width one has (was 40 / 60).</summary>
        public const int MinBarrierHp = 110, MinFullWidthHp = 165;

        /// <summary>
        /// Trap damage on Level 1 and per level after: about 1.85x the Oct 26 70 + 5, 96 + 7 and 52 + 5 (Derek 2026-10-27: "regular
        /// obstacles don't do enough damage"). A follower's share stays capped one short of its HP.
        /// </summary>
        public const int SpikeDamage = 130, SpikeDamageStep = 9, LogDamage = 176, LogDamageStep = 13, FireDamage = 96, FireDamageStep = 9;

        private static readonly string[] FollowerUnits = { "rowan", "lyra", "rowan", "oakenshield", "lyra", "oakenshield" };
        private static readonly string[] ShardHeroes = { "hero.lyra", "hero.rowan", "hero.oakenshield", "hero.sir_aldric" };
        private static readonly RunnerBoxKind[] PowerUps = { RunnerBoxKind.Weapon, RunnerBoxKind.Rate, RunnerBoxKind.Range, RunnerBoxKind.Followers, RunnerBoxKind.Heal, RunnerBoxKind.Weapon, RunnerBoxKind.Rate, RunnerBoxKind.Followers };

        /// <summary><paramref name="level"/> with every row <paramref name="metres"/> further up the road and the road that much longer
        /// (a negative <paramref name="metres"/> takes a run-up back off).</summary>
        public static RunnerLevelDef WithRunUp(RunnerLevelDef level, float metres)
        {
            if (metres == 0f)
            {
                return level;
            }

            var rows = new List<RunnerRowDef>(level.Rows.Count);
            foreach (var r in level.Rows)
            {
                rows.Add(r with { At = r.At + metres });
            }

            return level with { Length = level.Length + metres, Rows = rows };
        }

        public static RunnerLevelDef Generate(RunnerLevelDef first, RunnerProgressionDef p, int number, Func<string, bool> hasUnit, Func<string, float>? unitSpeed = null)
        {
            if (number < 2)
            {
                return first;
            }

            var t = number - 1;
            var ramp = p.Ramp;
            var length = (float)Math.Round(Math.Min(first.Length + (p.LengthStep * t), ramp?.LengthCap ?? float.MaxValue));
            var speed = Math.Min(first.Speed + (p.SpeedStep * t), Math.Max(first.Speed, ramp?.SpeedCap ?? float.MaxValue));
            var rows = Rows(p, number, length, first.HalfWidth, p.IsBossLevel(number) ? BossUnit(first, p.IsMilestoneBossLevel(number)) : null, hasUnit, speed, unitSpeed);
            if (!p.IsBossLevel(number))
            {
                // No boss: the finish gate stands just past the finale, not where the boss section would end.
                length = Math.Min(length, (float)Math.Ceiling(rows[rows.Count - 1].At + FinishPad));
            }

            var r = first.Rewards;
            var pay = 1f + (p.RewardStep * t);
            var rewards = new RunnerRewardDef(
                (int)Math.Round(r.WinGold * pay),
                (int)Math.Round(r.GoldPerKill * pay),
                (int)Math.Round(r.GoldPerBox * pay),
                (int)Math.Round(r.Shards * (1f + (0.5f * p.RewardStep * t))),
                ShardHeroes[(number - 1) % ShardHeroes.Length]);
            var slug = "road_" + number.ToString("00", CultureInfo.InvariantCulture);
            return new RunnerLevelDef(
                "runner." + slug,
                "runner.level." + slug,
                first.Hero,
                length,
                speed,
                first.HalfWidth,
                first.MaxFollowers,
                rows,
                rewards)
            {
                Number = number,
                DisplayName = number - 1 < p.Names.Count ? p.Names[number - 1] : "Road " + number.ToString(CultureInfo.InvariantCulture),
                FoeHpMul = first.FoeHpMul * (1f + (p.FoeHpStep * t)) * (ramp?.FoeHpMul(number) ?? 1f) * p.Tweak(number).FoeHp,
                BossHpMul = first.BossHpMul * (1f + (p.BossHpStep * t)) * (ramp?.BossHpMul(number) ?? 1f),
                FoeDmgMul = first.FoeDmgMul * (ramp?.FoeDmgMul(number) ?? 1f),
                GateCharge = first.GateCharge
            };
        }

        /// <summary>
        /// The spawn rows of level <paramref name="number"/> on a <paramref name="length"/> m road: the boss row last when
        /// <paramref name="bossUnit"/> is set, otherwise the road ends on the last wave and the finish line.
        /// </summary>
        public static List<RunnerRowDef> Rows(RunnerProgressionDef p, int number, float length, float halfWidth, string? bossUnit, Func<string, bool> hasUnit, float speed = 0f, Func<string, float>? unitSpeed = null)
        {
            var t = Math.Max(0, number - 1);
            // Enemy / box counts stop growing at the ramp's count cap (the road stops growing too); HP and damage carry on.
            var tc = p.CountT(number);
            // A hand tweak for this level (default 1.0): wave sizes and barrier HP only, so the layout stays the seed's.
            var tw = p.Tweak(number);
            int W(int count) => tw.Wave == 1f ? count : Math.Max(1, (int)Math.Round(count * tw.Wave));
            var rnd = new Random(unchecked(p.Seed + (number * 7919)));
            var bossAt = length - 8f;
            var boxMul = 1f + (p.BoxHpStep * t);
            var rows = new List<RunnerRowDef>();
            var pool = new List<string> { "nightfang", "bonequill", "nightfang", "blightroot", "bonequill" };
            if (number >= 4 && hasUnit("vespera"))
            {
                pool.Add("vespera");
            }

            if (number >= 7 && hasUnit("ashwyrm"))
            {
                pool.Add("ashwyrm");
            }

            // Traps: spikes from Level 1, fire from Level 2, rolling logs from Level 4. Every other beat on Levels 1-4,
            // every beat from Level 5, and a second trap on one beat in four from Level 13 and every other beat from Level 22
            // (Derek 2026-10-26: "the obstacles need to be more prevelant and do more damage": about 1.5-2x the old every
            // third / every other beat).
            var traps = new List<RunnerHazardKind> { RunnerHazardKind.Spikes };
            if (number >= 2)
            {
                traps.Add(RunnerHazardKind.Fire);
            }

            if (number >= 4)
            {
                traps.Add(RunnerHazardKind.Log);
            }

            var side = rnd.Next(2) == 0 ? -1f : 1f;
            var beats = Math.Max(1, (int)Math.Floor((bossAt - 16f - 12f) / BeatMetres) + 1);
            var count = BeatCount(length);
            // Where each beat starts (9-18 m apart, tight and breathing stretches) and which set piece it is: both from the
            // seed and the level number, so every level has its own road (Derek 2026-10-27: "it seems like each item is in the
            // same place"). What each beat holds (its boxes, wave sizes, traps and barriers, and their HP) still follows the
            // beat's number, so the counts and the balance climb level by level as before.
            var starts = BeatStarts(p, number, count);
            var pieces = SetPieces(p, number, count);
            var trap = 0;
            // Barriers draw from their own seed so adding them leaves the rest of the road as it was.
            var brnd = new Random(unchecked(p.Seed + (number * 104729) + 17));
            var barrier = 0;
            var crates = PlanCrates(p, number, count);
            var opener = count >= 3 ? OpeningFor(number) : new Opening(0f, (int)side, 2, false);
            for (var beat = 0; beat < count; beat++)
            {
                var z = starts[beat];
                var gap = beat + 1 < count ? starts[beat + 1] - z : BeatMetres;
                // In-beat distances stretch or squeeze with the gap to the next beat.
                var k = Math.Max(0.85f, Math.Min(1.15f, gap / BeatMetres));
                var piece = pieces[beat];
                // How far into the level this beat is (0 = the start, 1 = just before the boss): the waves grow along the
                // road as the squad grows from the boxes, so the extra difficulty of a later level lands late in it.
                var along = beats > 1 ? beat / (float)(beats - 1) : 1f;
                // Box and barrier HP follow the beat's number, not where it stands, so a stretched road is no tougher.
                var hpAt = (int)Math.Round(8.6f * (12f + (BeatMetres * beat)) * boxMul);
                var fx = side * (1.6f + ((float)rnd.NextDouble() * 0.6f));
                if (beat == 0)
                {
                    // The opening chest stands on the level's own side of the road (or in the middle).
                    fx = opener.Lane * Math.Abs(fx);
                }
                var ux = -side * (1.6f + ((float)rnd.NextDouble() * 0.6f));
                var wx = ((float)rnd.NextDouble() * 2.4f) - 1.2f;
                var laneRoll = rnd.NextDouble();
                var laneRoll2 = rnd.NextDouble();
                var phase = (float)Math.Round(rnd.NextDouble() * RunnerRules.SpikeCycle, 2);
                var phase2 = (float)Math.Round(rnd.NextDouble() * RunnerRules.SpikeCycle, 2);
                var centre = rnd.NextDouble() < 0.3;

                // The beat's boxes (one box a beat, not two: Derek 2026-10-23 "theres too many upgrades to enimies"): a
                // follower chest on the even beats, a power-up crate on the odd ones, plus a chest on beat 1 and the hero chest
                // on beat 3 (and beat 6). The waves keep growing, so later levels have more boxes but fewer per enemy.
                var boxes = BeatBoxes(beat, tc, t, boxMul, fx, crates, k);
                var hasBarrier = HasBarrier(number, beat);
                var trapEvery = number >= 5 ? 1 : 2;
                var nTraps = beat >= 1 && (beat - 1) % trapEvery == 0
                    ? 1 + (number >= SecondTrapFrom && (beat % 4 == 1 || (number >= SecondTrapEveryOtherFrom && beat % 2 == 1)) ? 1 : 0)
                    : 0;
                var skirm = beat % 2 == 1 && beat >= 3 ? (beat % 4 == 1 ? "nightfang" : "bonequill") : null;
                var skirmCount = W(2 + (int)Math.Round(tc * along * 0.2f));

                // The wave: a fodder front line, a different second line behind it, and on some beats a heavy anchor.
                var front = beat < 2 ? (beat == 0 ? "bonequill" : "nightfang") : pool[beat % pool.Count];
                if (front == "blightroot" || front == "ashwyrm")
                {
                    front = beat % 2 == 0 ? "nightfang" : "bonequill";
                }

                var back = beat == 0 ? "nightfang" : pool[((beat * 3) + 1) % pool.Count];
                if (back == front || back == "blightroot" || back == "ashwyrm")
                {
                    back = front == "bonequill" ? "nightfang" : "bonequill";
                }

                if (number >= 4 && beat >= beats / 2 && beat % 3 == 0 && hasUnit("vespera"))
                {
                    back = "vespera";
                }

                // The first two waves stay small: the squad is still the hero and a chest or two.
                var opening = beat < 2;
                var frontCount = W(opening ? 2 + beat : 3 + (beat % 2) + (int)Math.Round(tc * along * 0.3f));

                var backCount = beat > 0 ? W(opening ? 1 : 2 + (int)Math.Round(tc * along * 0.25f)) : 0;
                var hasHeavy = beat % 3 == 2 || (t >= 6 && beat % 3 == 0 && beat > 0);
                var heavy = number >= 7 && hasUnit("ashwyrm") && beat % 2 == 0 ? "ashwyrm" : OddHeavy(number, beat, hasUnit);
                var heavyCount = W(1 + (int)Math.Round(tc * along / 5f));

                // Where this beat's things stand: offsets from the beat start (scaled by k) and lanes, by set piece.
                float boxAt = 0f, barrierAt = 3f, skirmAt = 4f, frontAt = beat == 0 ? FirstWaveMetres : 8f, backAt = 10.5f, heavyAt = 12f;
                float trapAt = 5.5f, trap2At = 10f;
                float barrierSide = Math.Sign(beat % 2 == 1 ? ux : fx) == 0 ? 1f : -Math.Sign(beat % 2 == 1 ? ux : fx);
                float frontX = wx, backX = -wx * 0.5f, heavyX = -wx, skirmX = fx * 0.6f;
                var frontSplit = 1;
                // WaveFirst: where half the second line stands ahead of the box (null: the whole line behind the front).
                float? backAhead = null;
                float[]? trapLanes = null, trap2Lanes = null;
                var boxX = centre && !opening ? (float?)0f : null;
                var edge = halfWidth - 1.1f;
                switch (piece)
                {
                    case RunnerSetPiece.WaveFirst:
                        // The wave's small second line meets the squad before the box, the main line after it.
                        backAhead = 1f; boxAt = 3.5f; barrierAt = 6f; skirmAt = 7.5f; trapAt = 9f; frontAt = 10f; backAt = 11.5f; trap2At = 12f; heavyAt = 12.5f;
                        break;
                    case RunnerSetPiece.Ambush:
                        // The box in the middle of the road, the wave coming from both edges at once.
                        boxX = 0f; barrierAt = 3.5f; skirmAt = 5f; trapAt = 5.5f; frontAt = 8f; frontSplit = 2; backAt = 10.5f; trap2At = 11.5f; heavyAt = 12.5f;
                        frontX = 0f;
                        break;
                    case RunnerSetPiece.TrapCorridor:
                        // Traps on both edges back to back, the middle open, and a fodder line coming down that middle.
                        barrierAt = 4f; trapAt = hasBarrier ? 4.5f : 4f; trap2At = 5.5f; skirmAt = 2.5f; frontAt = 8.5f; frontX = 0f; backAt = 10.5f; heavyAt = 12f;
                        trapLanes = new[] { -barrierSide * edge };
                        trap2Lanes = new[] { hasBarrier ? -barrierSide * edge : barrierSide * edge };
                        if (!hasBarrier)
                        {
                            trapLanes = new[] { -barrierSide * edge };
                        }

                        break;
                    case RunnerSetPiece.Barricade:
                        // A barricade on one edge with the wave lined up across the open side of it: break through either.
                        boxX = 0f; trapAt = 3f; barrierAt = 6.5f; frontAt = 7.3f; frontX = -barrierSide * 1.8f; skirmAt = 9.5f; backAt = 10.5f; trap2At = 11f; heavyAt = 12.5f;
                        break;
                    case RunnerSetPiece.ArcherNest:
                        // Archers dug in right behind a wall on one edge, the rest of the wave on the other side.
                        trapAt = 3f; barrierAt = 5f; backAt = 6.2f; back = "bonequill"; backX = barrierSide * (halfWidth - 1.5f); frontAt = 8.5f; frontX = -barrierSide * 1.5f; skirmAt = 10f; trap2At = 11f; heavyAt = 12f;
                        break;
                    case RunnerSetPiece.CrateRun:
                        // The beat's boxes back to back down the middle, traps on the edges beside them.
                        boxX = 0f; skirmAt = 9f; trapAt = 3.5f; barrierAt = 5.5f; frontAt = 10f; backAt = 11.5f; trap2At = 7f; heavyAt = 13f;
                        trapLanes = new[] { edge, -edge };
                        trap2Lanes = new[] { -edge, edge };
                        break;
                    case RunnerSetPiece.FodderRush:
                        // A swarm in three small packs across the road, one behind the other.
                        barrierAt = 3f; trapAt = 5f; frontAt = 7f; frontSplit = 3; frontX = 0f; skirmAt = 4f; backAt = 11f; trap2At = 11.5f; heavyAt = 12.5f;
                        break;
                }

                if (beat == 0)
                {
                    // The level's opening moves its first box nearer or further, or behind the first wave (the waves and
                    // the beats stay where they were, so the fights do too).
                    boxAt = opener.WaveFirst ? WaveFirstFrontMetres + WaveFirstBoxMetres : opener.Shift;
                    if (opener.WaveFirst)
                    {
                        frontAt = WaveFirstFrontMetres;
                    }
                }
                else if (beat == 1 && opener.WaveFirst)
                {
                    // The second beat's boxes step back so they do not crowd the first box behind the wave.
                    boxAt += WaveFirstNextBoxMetres;
                }

                // A crate run stands its boxes back to back down the middle, 3 m apart.
                var boxZ = z + (boxAt * k);
                for (var i = 0; i < boxes.Count; i++)
                {
                    var bx = boxes[i];
                    var x = boxX ?? bx.X;
                    if (piece == RunnerSetPiece.CrateRun)
                    {
                        x = (i % 2 == 0 ? -0.6f : 0.6f) * side;
                        rows.Add(bx with { At = boxZ + (3f * i), X = x });
                    }
                    else if (boxX != null)
                    {
                        // A middle-of-the-road piece: the chest in the middle, a crate beside it (or alone in the middle).
                        rows.Add(bx with { At = boxZ + Math.Max(2f * i, bx.At), X = boxes.Count > 1 ? (i == 0 ? -0.9f : 0.9f) * side : x });
                    }
                    else
                    {
                        rows.Add(bx with { At = boxZ + bx.At });
                    }
                }

                if (skirm != null)
                {
                    Group(rows, z + (skirmAt * k), skirmX, skirm, skirmCount, 1.1f, halfWidth);
                }

                if (hasBarrier)
                {
                    AddBarrier(rows, z + (barrierAt * k), tw.BarrierHp == 1f ? hpAt : (int)Math.Round(hpAt * tw.BarrierHp), number, t, barrier, brnd, halfWidth, -(int)barrierSide);
                    barrier++;
                }

                if (nTraps >= 1)
                {
                    AddTrapAt(rows, traps[trap % traps.Count], z + (trapAt * k), halfWidth, t, phase, trapLanes, laneRoll);
                    trap++;
                }

                if (nTraps >= 2)
                {
                    AddTrapAt(rows, traps[trap % traps.Count], z + (trap2At * k), halfWidth, t, phase2, trap2Lanes, laneRoll2);
                    trap++;
                }

                var fz = z + (beat == 0 ? frontAt : frontAt * k);
                if (frontSplit == 1)
                {
                    Group(rows, fz, frontX, front, frontCount, 1.1f, halfWidth);
                }
                else
                {
                    var left = frontCount;
                    for (var i = 0; i < frontSplit && left > 0; i++)
                    {
                        var n = (left + (frontSplit - i - 1)) / (frontSplit - i);
                        left -= n;
                        var x = frontSplit == 2 ? (i == 0 ? -1f : 1f) * side * (halfWidth - 1.3f) : (i - 1) * side * (halfWidth - 1.6f);
                        Group(rows, fz + (frontSplit == 2 ? 0f : 1.6f * i), x, front, n, 1.1f, halfWidth);
                    }
                }

                if (backCount > 0 && backAhead is float ahead && backCount >= 2)
                {
                    var lead = backCount / 2;
                    Group(rows, z + (ahead * k), -backX, back, lead, 1.4f, halfWidth);
                    Group(rows, z + (backAt * k), backX, back, backCount - lead, 1.4f, halfWidth);
                }
                else if (backCount > 0)
                {
                    Group(rows, z + (backAt * k), backX, back, backCount, 1.4f, halfWidth);
                }

                if (hasHeavy)
                {
                    Group(rows, z + (heavyAt * k), heavyX, heavy, heavyCount, 3.2f, halfWidth);
                }

                side = -side;
            }

            // Spread the beats' waves into a steady stream (runner_levels.json "spread"), before the closing set piece goes in:
            // the boss guard and the finale stay the set pieces they are.
            if (p.Spread != null && number >= p.Spread.From && speed > 0f)
            {
                // The closing set piece's front: the boss guard's archer line, or the finale's fodder push.
                var (closingAt, closingUnit) = bossUnit != null ? (bossAt - 7f, "bonequill") : (bossAt - FinaleBack + 3f, "nightfang");
                SpreadWaves(rows, p.Spread.For(number), closingAt, closingUnit, speed, unitSpeed ?? (_ => 0f), new Random(unchecked(p.Seed + (number * 15485863) + 3)));
                ApplyLanes(rows, p.Spread.Lanes, number, halfWidth, speed, unitSpeed ?? (_ => 0f), new Random(unchecked(p.Seed + (number * 7919) + 17)));
            }

            if (bossUnit != null)
            {
                // The boss's guard: a line of archers, longer with the level, then the boss.
                Group(rows, bossAt - 7f, 0f, "bonequill", 3 + (tc / 2), 1.4f, halfWidth);
                rows.Add(new RunnerRowDef(bossAt, RunnerRowKind.Boss, 0f, bossUnit, 1, 0f, null));
            }
            else
            {
                Finale(rows, number, t, tc, bossAt - FinaleBack, traps, halfWidth, hasUnit, W);
            }

            KeepEscapeLanes(rows, halfWidth);
            ClearBoxes(rows, halfWidth);
            StandOffWalls(rows, halfWidth);
            rows.Sort((a, b) => a.At.CompareTo(b.At));
            return rows;
        }

        /// <summary>
        /// Spreads the hand-authored Level 1 the same way (when the progression's spread starts at 1): its enemy rows become a
        /// stream that ends one even step short of the finish line, the other rows stay where they are.
        /// </summary>
        public static RunnerLevelDef SpreadAuthored(RunnerLevelDef level, RunnerProgressionDef p, Func<string, float> unitSpeed)
        {
            if (p.Spread == null || p.Spread.From > level.Number || level.Speed <= 0f || level.Rows.Any(r => r.Kind == RunnerRowKind.Boss))
            {
                return level;
            }

            var rows = level.Rows.ToList();
            SpreadWaves(rows, p.Spread.For(level.Number), level.Length, "bonequill", level.Speed, unitSpeed, new Random(unchecked(p.Seed + (level.Number * 15485863) + 3)));
            ApplyLanes(rows, p.Spread.Lanes, level.Number, level.HalfWidth, level.Speed, unitSpeed, new Random(unchecked(p.Seed + (level.Number * 7919) + 17)));
            return level with { Rows = rows.OrderBy(r => r.At).ToList() };
        }

        /// <summary>
        /// Spreads every wave row of the road (the closing set piece is not in yet) into a steady stream that runs up to the
        /// closing set piece, whose front (<paramref name="closingUnit"/>) stands at <paramref name="closing"/>; see <see cref="RunnerSpreadDef"/>.
        /// </summary>
        private static void SpreadWaves(List<RunnerRowDef> rows, RunnerSpreadDef spread, float closing, string closingUnit, float speed, Func<string, float> unitSpeed, Random rnd)
        {
            // How much earlier than its row a pack walking at v meets the fight line, in road metres (squad walks at speed).
            float Lead(string unit) => RunnerSpreadDef.FightLine * speed / (speed + Math.Max(0f, unitSpeed(unit)));

            var packs = new List<(float Arrive, RunnerRowDef Row, int Count)>();
            void Split(RunnerRowDef r, int n)
            {
                var parts = (n + spread.Pack - 1) / spread.Pack;
                var left = n;
                for (var k = 0; k < parts; k++)
                {
                    var c = (left + (parts - k - 1)) / (parts - k);
                    left -= c;
                    packs.Add((r.At - Lead(r.Unit), r, c));
                }
            }

            // The crowd: each wave row grows to its share of ceil(stream x Crowd) enemies (a running total, so the level as
            // a whole carries at least Crowd times its enemies and the extra ones follow the waves they came with).
            var seen = 0;
            var made = 0;
            for (var i = rows.Count - 1; i >= 0; i--)
            {
                var r = rows[i];
                if (r.Kind != RunnerRowKind.Enemy)
                {
                    continue;
                }

                rows.RemoveAt(i);
                var n = Math.Max(1, r.Count);
                seen += n;
                var want = spread.Crowd > 1f ? (int)Math.Ceiling((seen * (double)spread.Crowd) - 1e-6) : seen;
                var extra = spread.CrowdSkip.Contains(r.Unit) ? 0 : Math.Max(0, want - made - n);
                made += n + extra;
                Split(r, n);
                if (extra > 0)
                {
                    Split(r with { Extra = true }, extra);
                }
            }

            if (packs.Count == 0)
            {
                return;
            }

            // Same encounter order as before (stable on ties: the order the beat put them in).
            var ordered = packs.Select((x, i) => (x, i)).OrderBy(q => q.x.Arrive).ThenByDescending(q => q.i).Select(q => q.x).ToList();
            var a0 = ordered[0].Arrive;
            var a1 = ordered[ordered.Count - 1].Arrive + spread.Window;

            // Smooth the arrivals forward over one window: each enemy's weight is spread evenly over the Window metres from
            // where it arrived, and the packs are laid at even shares of that smoothed road. A beat's clump fills the empty
            // road after it, so the action is steady, while no enemy reaches the squad sooner than before: the opening still
            // gives the squad its first chests before the bigger waves, and the level keeps its build-up (later beats still
            // bring more enemies per metre than early ones).
            const float cell = 0.1f;
            var cells = (int)Math.Ceiling((a1 - a0) / cell) + 1;
            var mass = new double[cells];
            foreach (var (arrive, _, c) in ordered)
            {
                var i0 = (int)((arrive - a0) / cell);
                var i1 = Math.Min(cells - 1, (int)((arrive + spread.Window - a0) / cell));
                for (var i = i0; i <= i1; i++)
                {
                    mass[i] += c / (double)(i1 - i0 + 1);
                }
            }

            var cum = new double[cells + 1];
            for (var i = 0; i < cells; i++)
            {
                cum[i + 1] = cum[i] + mass[i];
            }

            var total = cum[cells];
            float Place(double share)
            {
                var target = share * total;
                var j = Array.BinarySearch(cum, target);
                if (j < 0)
                {
                    j = ~j;
                }

                j = Math.Max(1, Math.Min(cells, j));
                var f = mass[j - 1] > 0 ? (target - cum[j - 1]) / mass[j - 1] : 0;
                return a0 + (float)((j - 1 + Math.Max(0, Math.Min(1, f))) * cell);
            }

            var done = 0.0;
            var at = new float[ordered.Count];
            for (var i = 0; i < ordered.Count; i++)
            {
                var c = ordered[i].Count;
                at[i] = Place((done + (c * 0.5)) / total);
                done += c;
            }

            // The level's first enemy still meets the squad exactly where it used to, so a road that opened on a wave still does
            // (the smoothing would otherwise slide it a few metres on, past the first pickup).
            at[0] = a0;

            // The stream ends one even step short of where the closing set piece meets the squad (the stream's tail is drawn
            // in to it, never pushed past it).
            var end = closing - Lead(closingUnit) - ((closing - a0) / (ordered.Count + 1));
            var first = at[0];
            var last = at[ordered.Count - 1];
            if (last > end && last > first && end > first)
            {
                for (var i = 0; i < ordered.Count; i++)
                {
                    at[i] = first + ((at[i] - first) * (end - first) / (last - first));
                }
            }

            for (var i = 0; i < ordered.Count; i++)
            {
                var (_, r, c) = ordered[i];
                var gapPrev = i > 0 ? at[i] - at[i - 1] : float.MaxValue;
                var gapNext = i + 1 < ordered.Count ? at[i + 1] - at[i] : float.MaxValue;
                var step = Math.Min(gapPrev, gapNext);
                var jitter = i > 0 && step < float.MaxValue ? (float)((rnd.NextDouble() * 2.0) - 1.0) * spread.Jitter * step : 0f;
                var z = (float)Math.Round(at[i] + jitter + Lead(r.Unit), 2);
                rows.Add(r with { At = z, Count = c, Spread = c > 1 ? r.Spread : 0f });
            }
        }

        /// <summary>
        /// Moves an even share of the stream's enemy packs and power-up crates to the outer lanes, and walks some of those packs
        /// in from beyond the road edge (see <see cref="RunnerLanesDef"/>). Only rows already in (the stream and the beats'
        /// boxes; the closing set piece goes in later). Rolls on <paramref name="rnd"/> alone.
        /// </summary>
        internal static void ApplyLanes(List<RunnerRowDef> rows, RunnerLanesDef? lanes, int number, float halfWidth, float speed, Func<string, float> unitSpeed, Random rnd)
        {
            if (lanes == null || number < lanes.From)
            {
                return;
            }

            // Every k-th pick of an even share, from a rolled offset: floor((k + 1) s + u) > floor(k s + u).
            static bool Pick(int k, double share, double u) => Math.Floor(((k + 1) * share) + u) > Math.Floor((k * share) + u);

            var foes = Enumerable.Range(0, rows.Count).Where(i => rows[i].Kind == RunnerRowKind.Enemy && !rows[i].Finale).OrderBy(i => rows[i].At).ThenBy(i => i).ToList();
            var u = rnd.NextDouble();
            var sideShare = lanes.Outer > 0f ? Math.Min(1.0, lanes.Side / (double)lanes.Outer) : 0.0;
            var us = rnd.NextDouble();
            var sideK = 0;
            var lastSign = rnd.Next(2) == 0 ? -1f : 1f;
            for (var k = 0; k < foes.Count; k++)
            {
                var i = foes[k];
                var r = rows[i];
                if (!Pick(k, lanes.Outer, u))
                {
                    if (lanes.Seek.TryGetValue(r.Unit, out var seek))
                    {
                        rows[i] = r with { Seek = seek };
                    }

                    continue;
                }

                // Mostly alternating sides, so the player has to cross the road; now and then the same side twice.
                var sign = rnd.NextDouble() < 0.7 ? -lastSign : lastSign;
                lastSign = sign;
                var span = r.Count > 1 ? (r.Count - 1) * r.Spread * 0.5f : 0f;
                var x = sign * Math.Max(0f, Math.Min(lanes.OuterX, halfWidth - 0.7f) - span);
                var row = r with { X = (float)Math.Round(x, 2), Seek = 0f };
                if (lanes.SideUnits.Contains(r.Unit))
                {
                    if (Pick(sideK++, sideShare, us))
                    {
                        // Walks in from SideOut beyond the edge; fast enough to be on the road SideMinSeconds before it can hit.
                        var closing = speed + Math.Max(0f, unitSpeed(r.Unit));
                        var gap = Math.Min(r.At, RunnerRules.SpawnAhead) - RunnerLanesDef.ContactMetres;
                        var total = closing > 0f ? gap / closing : 0f;
                        var room = total - lanes.SideMinSeconds;
                        var v = room > 0f ? Math.Max(lanes.SideSpeed, lanes.SideOut / room) : float.MaxValue;
                        if (v <= lanes.SideSpeed * 3f)
                        {
                            row = row with { EnterX = sign * (halfWidth + lanes.SideOut), EnterSpeed = (float)Math.Round(v, 2) };
                        }
                    }
                }

                rows[i] = row;
            }

            var crates = Enumerable.Range(0, rows.Count).Where(i => rows[i].Kind == RunnerRowKind.Box && rows[i].Box is { GivesUnits: false } && !rows[i].Finale).OrderBy(i => rows[i].At).ThenBy(i => i).ToList();
            var uc = rnd.NextDouble();
            var edge = Math.Min(lanes.CrateX, halfWidth - RunnerBox.Radius);
            for (var k = 0; k < crates.Count; k++)
            {
                var i = crates[k];
                var r = rows[i];
                if (!Pick(k, lanes.Crates, uc))
                {
                    continue;
                }

                // Out on its own side (it stays on the side it was rolled for, so it does not land on the chest it rode with).
                var sign = r.X < 0f ? -1f : r.X > 0f ? 1f : (rnd.Next(2) == 0 ? -1f : 1f);
                var x = sign * edge;
                if (!BoxBlocked(rows, r.At, x, halfWidth, r))
                {
                    rows[i] = r with { X = (float)Math.Round(x, 2) };
                }
            }
        }

        /// <summary>
        /// How a level opens (Derek 2026-10-27: "on the first level there is immediately one on the left followed shortly
        /// after by one on each left and right side of all the levels"). Every level used to open the same way: a chest on one
        /// edge at 12 m, then a chest and a crate side by side at 25 m. Now the first box stands 7-17 m in
        /// (<see cref="Shift"/> metres from the first beat), its chest on the left, the right or in the middle (<see cref="Lane"/>), the first crate beside
        /// the first chest, a few metres behind it, beside the second chest or behind that (<see cref="CrateMode"/> 0-3), and
        /// some levels meet a small wave before any box (<see cref="WaveFirst"/>). The same boxes with the same HP, so the
        /// balance holds.
        /// </summary>
        public readonly struct Opening
        {
            public readonly float Shift; public readonly int Lane, CrateMode; public readonly bool WaveFirst;
            public Opening(float shift, int lane, int crateMode, bool waveFirst) { Shift = shift; Lane = lane; CrateMode = crateMode; WaveFirst = waveFirst; }
        }

        private static readonly Opening[] Openings =
        {
            new(0f, -1, 0, false),
            new(-3f, 1, 1, false),
            new(5f, 0, 3, false),
            new(0f, -1, 1, true),
            new(0f, 1, 3, false),
            new(5f, -1, 0, false),
            new(-4f, 1, 2, false),
            new(0f, 1, 2, false),
            new(4f, 1, 2, false),
            new(2f, 1, 0, false),
            new(-5f, -1, 1, false),
            new(3f, -1, 2, false),
        };

        /// <summary>Level <paramref name="number"/>'s opening: the table stepped 5 at a time (so neighbours never share one),
        /// mirrored on every other pass through it.</summary>
        public static Opening OpeningFor(int number)
        {
            var o = Openings[(number * 5) % Openings.Length];
            return (number / Openings.Length) % 2 == 1 ? new Opening(o.Shift, -o.Lane, o.CrateMode, o.WaveFirst) : o;
        }

        /// <summary>On a wave-first opening, how far into the first beat the first wave stands (4 m nearer than usual), how
        /// far past it the first box stands, and how much further on the second beat's boxes step.</summary>
        public const float WaveFirstFrontMetres = 8f, WaveFirstBoxMetres = 1.5f, WaveFirstNextBoxMetres = 1.5f;

        /// <summary>How many beats a road of <paramref name="length"/> m has (the first at 12 m, nominally 13 m apart, all short of the boss section).</summary>
        public static int BeatCount(float length)
        {
            var n = 0;
            for (var z = 12f; z < length - 8f - 16f; z += BeatMetres)
            {
                n++;
            }

            return Math.Max(1, n);
        }

        /// <summary>
        /// Where each of a level's <paramref name="count"/> beats starts. The first three stand 13 m apart as always (the
        /// opening; its boxes move with the level's <see cref="Opening"/>); after that the road runs in stretches of two or three beats
        /// that are tight (9-11.5 m), even (12-14 m) or breathing (15-18 m), scaled so the last beat stands where it always
        /// did (the finale and the boss keep their places). From the seed and the level number only.
        /// </summary>
        public static float[] BeatStarts(RunnerProgressionDef p, int number, int count)
        {
            var z = new float[count];
            for (var i = 0; i < count; i++)
            {
                z[i] = 12f + (BeatMetres * i);
            }

            var free = count - 3;
            if (free < 1)
            {
                return z;
            }

            var rnd = new Random(unchecked(p.Seed + (number * 92821) + 11 + (p.Tweak(number).Layout * 7919)));
            var gaps = new float[free];
            var i0 = 0;
            var drift = 0f;
            while (i0 < free)
            {
                var run = 1 + rnd.Next(3);
                var mode = rnd.Next(3);
                for (var j = 0; j < run && i0 < free; j++, i0++)
                {
                    var u = (float)rnd.NextDouble();
                    var g = mode switch
                    {
                        0 => MinBeatGap + (u * 2.5f),
                        1 => 12f + (u * 2f),
                        _ => 15f + (u * (MaxBeatGap - 15f))
                    };
                    // The road never runs more than MaxDrift metres ahead of or behind the even 13 m beat, so a tight
                    // stretch is soon paid back by a breathing one and no part of a level packs in many more waves than before.
                    gaps[i0] = Math.Max(BeatMetres - MaxDrift - drift, Math.Min(BeatMetres + MaxDrift - drift, g));
                    drift += gaps[i0] - BeatMetres;
                }
            }

            // Fit: the same total road as 13 m beats, every gap within MinBeatGap..MaxBeatGap, on the half metre.
            var target = BeatMetres * free;
            for (var pass = 0; pass < 8; pass++)
            {
                var sum = 0f;
                foreach (var g in gaps)
                {
                    sum += g;
                }

                var shift = (target - sum) / free;
                for (var j = 0; j < free; j++)
                {
                    gaps[j] = Math.Max(MinBeatGap, Math.Min(MaxBeatGap, gaps[j] + shift));
                }
            }

            var total = 0f;
            for (var j = 0; j < free; j++)
            {
                gaps[j] = (float)Math.Round(gaps[j] * 2f) / 2f;
                total += gaps[j];
            }

            // Rounding leftovers, half a metre at a time, onto the gaps with room.
            for (var guard = 0; Math.Abs(total - target) > 0.01f && guard < 4 * free; guard++)
            {
                var j = guard % free;
                var step = total > target ? -0.5f : 0.5f;
                if (gaps[j] + step >= MinBeatGap && gaps[j] + step <= MaxBeatGap)
                {
                    gaps[j] += step;
                    total += step;
                }
            }

            for (var j = 0; j < free; j++)
            {
                z[j + 3] = z[j + 2] + gaps[j];
            }

            return z;
        }

        /// <summary>Gaps between beats after the opening (metres).</summary>
        public const float MinBeatGap = 9f, MaxBeatGap = 18f;

        /// <summary>
        /// How far (metres) the drawn gaps may run ahead of or behind the even 13 m spacing. Fitting them back to the level's
        /// length can push a beat a few metres further (at most 10.5 m on the shipped levels, never a whole beat).
        /// </summary>
        public const float MaxDrift = 6f;

        /// <summary>
        /// Which set piece each beat of Level <paramref name="number"/> is. The first two beats are always a plain
        /// <see cref="RunnerSetPiece.March"/> (the opening); beat 2 cycles through four gentle pieces by level number, so two
        /// levels in a row never open the same way and never share a sequence; the rest are drawn from the seed among the
        /// pieces that beat's contents allow, never the same piece twice running.
        /// </summary>
        public static RunnerSetPiece[] SetPieces(RunnerProgressionDef p, int number, int count)
        {
            var seq = new RunnerSetPiece[count];
            var rnd = new Random(unchecked(p.Seed + (number * 65537) + 3 + (p.Tweak(number).Layout * 6271)));
            var trapEvery = number >= 5 ? 1 : 2;
            int Traps(int beat) => beat >= 1 && (beat - 1) % trapEvery == 0
                ? 1 + (number >= SecondTrapFrom && (beat % 4 == 1 || (number >= SecondTrapEveryOtherFrom && beat % 2 == 1)) ? 1 : 0)
                : 0;
            var options = new List<(RunnerSetPiece Piece, int Weight)>();
            for (var beat = 0; beat < count; beat++)
            {
                if (beat < 2)
                {
                    seq[beat] = RunnerSetPiece.March;
                    continue;
                }

                if (beat == 2)
                {
                    seq[beat] = Beat2Cycle[number % Beat2Cycle.Length];
                    continue;
                }

                var prev = seq[beat - 1];
                var barrier = HasBarrier(number, beat);
                options.Clear();
                options.Add((RunnerSetPiece.March, 2));
                options.Add((RunnerSetPiece.WaveFirst, 1));
                options.Add((RunnerSetPiece.Ambush, 2));
                options.Add((RunnerSetPiece.FodderRush, 2));
                if (Traps(beat) >= 2 || (Traps(beat) >= 1 && barrier))
                {
                    options.Add((RunnerSetPiece.TrapCorridor, 3));
                }

                if (barrier)
                {
                    options.Add((RunnerSetPiece.Barricade, 2));
                    options.Add((RunnerSetPiece.ArcherNest, 2));
                }

                options.Add((RunnerSetPiece.CrateRun, 2));

                options.RemoveAll(o => o.Piece == prev);
                var total = 0;
                foreach (var o in options)
                {
                    total += o.Weight;
                }

                var roll = rnd.Next(total);
                foreach (var o in options)
                {
                    roll -= o.Weight;
                    if (roll < 0)
                    {
                        seq[beat] = o.Piece;
                        break;
                    }
                }
            }

            return seq;
        }

        private static readonly RunnerSetPiece[] Beat2Cycle = { RunnerSetPiece.Ambush, RunnerSetPiece.CrateRun, RunnerSetPiece.ArcherNest, RunnerSetPiece.FodderRush };

        /// <summary>One power-up crate: the beat it belongs to (its HP and kind come from there), the beat it rides with,
        /// how far into that beat it stands (metres, before the beat's stretch) and a roll for its lane.</summary>
        private readonly struct Crate
        {
            public Crate(int origin, RunnerBoxKind kind, int host, float dz, double lane)
            {
                Origin = origin;
                Kind = kind;
                Host = host;
                Dz = dz;
                Lane = lane;
            }

            public int Origin { get; }
            public RunnerBoxKind Kind { get; }
            public int Host { get; }
            public float Dz { get; }
            public double Lane { get; }
        }

        /// <summary>
        /// Where a level's power-up crates go (Derek 2026-10-27: "it stil feels like the power ups are in the same place
        /// every level"). A level still has one crate per odd beat with the same kinds as before (the same total value);
        /// the range, follower and heal crates come in the level's own order, and each crate rides with its own beat or
        /// the beat before (never later, so no power-up comes later than it used to), 0.5-4 m into it (odd and even
        /// levels apart), on the other edge from the level before where no chest takes it. The first may come with the opening beat (12 m). From its own seeds, so the rest of the road
        /// is as it was.
        /// </summary>
        private static List<Crate> PlanCrates(RunnerProgressionDef p, int number, int count)
        {
            var kinds = new List<RunnerBoxKind>();
            var origins = new List<int>();
            for (var b = 1; b < count; b += 2)
            {
                origins.Add(b);
                kinds.Add(PowerUps[(b / 2) % PowerUps.Length]);
            }

            // The order follows the level only, so a re-rolled layout keeps its kinds where the balance put them. The
            // damage and rate crates keep their slots (moving them swung the early levels by 20-30%); the range, follower
            // and heal crates in each round of the cycle swap among themselves.
            var orng = new Random(unchecked(p.Seed + (number * 130363) + 29));
            for (var start = 2; start < kinds.Count; start += PowerUps.Length)
            {
                var len = Math.Min(3, kinds.Count - start);
                for (var i = len - 1; i > 0; i--)
                {
                    var j = orng.Next(i + 1);
                    (kinds[start + i], kinds[start + j]) = (kinds[start + j], kinds[start + i]);
                }
            }

            var rnd = new Random(unchecked(p.Seed + (number * 130363) + 31 + (p.Tweak(number).Layout * 7919)));
            var plan = new List<Crate>();
            for (var i = 0; i < kinds.Count; i++)
            {
                var b = origins[i];
                var roll = rnd.NextDouble();
                var host = roll < 0.35 ? b - 1 : b;
                // The edge flips with the level number (in a fixed scramble down the road), so a crate never stands on
                // the same edge as the same crate on the level before or after, unless a chest takes that edge.
                var edge = (number + (CrateEdges >> (i % 8))) & 1;
                // Odd levels set their crates deeper into the beat than even ones (2.7-4 m against 0.5-1.5 m), so a crate
                // that rides with the same beat as on the level before still stands well over a metre away.
                var dz = (number % 2 == 1 ? 2.7f : 0.5f) + ((float)rnd.NextDouble() * (number % 2 == 1 ? 1.3f : 1f));
                if (i == 0 && count >= 3)
                {
                    // The first crate follows the level's opening: beside the first chest, a few metres behind it, beside
                    // the second chest or a few metres behind that one.
                    var mode = OpeningFor(number).CrateMode;
                    host = mode < 2 ? b - 1 : b;
                    dz = mode % 2 == 0 ? 0.4f : 3.5f;
                }

                plan.Add(new Crate(b, kinds[i], host, dz, (edge + rnd.NextDouble()) * 0.5));
            }

            return plan;
        }

        private static readonly float[] CrateLanes = { -2f, 2f };
        private const int CrateEdges = 0b1011_0010;

        /// <summary>The boxes beat <paramref name="beat"/> holds (x and z filled in by its set piece): its chest at the beat
        /// start and the power-up crates that ride with it, each at its own distance in and on a lane clear of the chest.</summary>
        private static List<RunnerRowDef> BeatBoxes(int beat, int tc, int t, float boxMul, float fx, List<Crate> crates, float k)
        {
            var boxes = new List<RunnerRowDef>();
            var hpAt = (int)Math.Round(8.6f * (12f + (BeatMetres * beat)) * boxMul);
            if (beat == 3 || beat == 6)
            {
                boxes.Add(Box(0f, fx, hpAt, RunnerBoxKind.Hero, beat == 3 ? "rowan" : "oakenshield", 1, 0f));
            }
            else if (beat % 2 == 0 || beat == 1)
            {
                boxes.Add(Box(0f, fx, hpAt, RunnerBoxKind.Followers, FollowerUnits[beat % FollowerUnits.Length], 2 + (beat % 3) + (tc / 3), 0f));
            }

            // Half as many crates as beats, so each damage / rate crate gives a little more than the old 0.2 / 0.15.
            foreach (var c in crates)
            {
                if (c.Host != beat)
                {
                    continue;
                }

                var lanes = CrateLanes.Where(l => boxes.Count == 0 || Math.Abs(l - fx) > 1.2f).ToArray();
                var ux = lanes[(int)(c.Lane * lanes.Length) % lanes.Length] + (((float)(c.Lane * 7 % 1) - 0.5f) * 0.4f);
                var dz = c.Dz * k;
                var upHp = (int)Math.Round((int)Math.Round(8.6f * (12f + (BeatMetres * c.Origin)) * boxMul) * 0.8f);
                var kind = c.Kind;
                boxes.Add(kind switch
                {
                    RunnerBoxKind.Weapon => Box(dz, ux, upHp, kind, "", 1, 0.3f + (0.02f * t)),
                    RunnerBoxKind.Rate => Box(dz, ux, upHp, kind, "", 1, 0.25f + (0.012f * t)),
                    RunnerBoxKind.Range => Box(dz, ux, upHp, kind, "", 1, 0.15f),
                    RunnerBoxKind.Heal => Box(dz, ux, (int)Math.Round(upHp * 0.7f), kind, "", 1, 0.35f),
                    _ => Box(dz, ux, (int)Math.Round(upHp * 0.6f), RunnerBoxKind.Followers, FollowerUnits[(c.Origin + 2) % FollowerUnits.Length], 1 + (tc / 4), 0f)
                });
            }

            return boxes;
        }

        /// <summary>How close (metres down the road) a box may stand to a trap or barrier in its lane.</summary>
        public const float BoxClearance = 1.5f;

        /// <summary>Whether a box at (z, x) stands in or right against a trap or a barrier.</summary>
        public static bool BoxBlocked(IReadOnlyList<RunnerRowDef> rows, float z, float x, float halfWidth, RunnerRowDef? skip = null)
        {
            foreach (var r in rows)
            {
                if (ReferenceEquals(r, skip) || Math.Abs(r.At - z) >= BoxClearance)
                {
                    continue;
                }

                var w = r.Hazard?.Width ?? r.Barrier?.Width ?? -1f;
                if (w >= 0f && Math.Abs(x - r.X) < (w * 0.5f) + 0.7f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A last check over the whole road: a box that ended up in or against a trap or barrier moves to another
        /// lane, else up to 3 m back or on down the road.</summary>
        private static void ClearBoxes(List<RunnerRowDef> rows, float halfWidth)
        {
            var edge = halfWidth - 0.8f;
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Kind != RunnerRowKind.Box || r.Finale || !BoxBlocked(rows, r.At, r.X, halfWidth, r))
                {
                    continue;
                }

                var fixedIt = false;
                foreach (var dz in new[] { 0f, -1.5f, 1.5f, -3f, 3f })
                {
                    foreach (var l in new[] { r.X, -2f, 0f, 2f, -edge, edge })
                    {
                        if (!BoxBlocked(rows, r.At + dz, l, halfWidth, r))
                        {
                            rows[i] = r with { At = r.At + dz, X = l };
                            fixedIt = true;
                            break;
                        }
                    }

                    if (fixedIt)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>Least open road (metres) a squad must have beside traps and side barriers that stand within <see cref="EscapeWindow"/> m of each other.</summary>
        public const float EscapeGap = 2f, EscapeWindow = 2.5f;

        /// <summary>
        /// The widest stretch of open road across the lane at z, counting the traps and side barriers (not the full-width
        /// gates, which are shot down) standing within <see cref="EscapeWindow"/> m of it, plus an optional extra one.
        /// </summary>
        public static float OpenRoad(IReadOnlyList<RunnerRowDef> rows, float z, float halfWidth, (float X, float Width)? extra = null, RunnerRowDef? skip = null)
        {
            var spans = new List<(float A, float B)>();
            foreach (var r in rows)
            {
                if (ReferenceEquals(r, skip) || Math.Abs(r.At - z) >= EscapeWindow)
                {
                    continue;
                }

                var w = r.Hazard?.Width ?? (r.Barrier != null && !r.Barrier.FullWidth(halfWidth) ? r.Barrier.Width : 0f);
                if (w > 0f)
                {
                    spans.Add((r.X - (w * 0.5f), r.X + (w * 0.5f)));
                }
            }

            if (extra is (float ex, float ew))
            {
                spans.Add((ex - (ew * 0.5f), ex + (ew * 0.5f)));
            }

            spans.Sort((a, b) => a.A.CompareTo(b.A));
            var best = 0f;
            var at = -halfWidth;
            foreach (var (a, b) in spans)
            {
                best = Math.Max(best, a - at);
                at = Math.Max(at, b);
            }

            return Math.Max(best, halfWidth - at);
        }

        private static RunnerHazardDef TrapDef(RunnerHazardKind kind, int t, float phase) => kind switch
        {
            RunnerHazardKind.Spikes => new RunnerHazardDef(kind, 2.0f, SpikeDamage + (SpikeDamageStep * t), phase),
            RunnerHazardKind.Log => new RunnerHazardDef(kind, 2.2f, LogDamage + (LogDamageStep * t), 0f),
            _ => new RunnerHazardDef(kind, 1.8f, FireDamage + (FireDamageStep * t), 0f)
        };

        /// <summary>
        /// A lane trap at z: on one of <paramref name="lanes"/> (the set piece's), else a side or the middle, never the
        /// lane of a box just before it when another will do, and always leaving <see cref="EscapeGap"/> m of open road
        /// beside the traps and barriers around it (moved on 2 m at a time when nothing fits).
        /// </summary>
        private static void AddTrapAt(List<RunnerRowDef> rows, RunnerHazardKind kind, float z, float halfWidth, int t, float phase, float[]? lanes, double roll)
        {
            var def = TrapDef(kind, t, phase);
            float Fit(float x) => Math.Max(-halfWidth + (def.Width * 0.5f), Math.Min(halfWidth - (def.Width * 0.5f), x));
            for (var tries = 0; tries < 6; tries++, z += 2f)
            {
                var ok = new List<float>();
                var clear = new List<float>();
                foreach (var set in new[] { lanes, DefaultLanes })
                {
                    if (set == null)
                    {
                        continue;
                    }

                    foreach (var l in set)
                    {
                        var x = Fit(l);
                        if (OpenRoad(rows, z, halfWidth, (x, def.Width)) < EscapeGap)
                        {
                            continue;
                        }

                        ok.Add(x);
                        var nearBox = false;
                        foreach (var r in rows)
                        {
                            nearBox |= r.Kind == RunnerRowKind.Box && r.At <= z && z - r.At < 6f && Math.Abs(r.X - x) < 1.2f;
                        }

                        if (!nearBox)
                        {
                            clear.Add(x);
                        }
                    }

                    if (ok.Count > 0)
                    {
                        break;
                    }
                }

                var pick = clear.Count > 0 ? clear : ok;
                if (pick.Count > 0)
                {
                    rows.Add(new RunnerRowDef(z, RunnerRowKind.Hazard, pick[(int)(roll * pick.Count) % pick.Count], "", 1, 0f, null) { Hazard = def });
                    return;
                }
            }

            rows.Add(new RunnerRowDef(z, RunnerRowKind.Hazard, 0f, "", 1, 0f, null) { Hazard = def });
        }

        private static readonly float[] DefaultLanes = { -2.2f, 0f, 2.2f };

        /// <summary>How far (metres) behind a full-width barrier the nearest enemy rank stands.</summary>
        public const float WallStandOff = 1.8f;

        /// <summary>
        /// Enemy ranks a set piece put right behind a barrier that spans the road would start the level pressed against it,
        /// unable to take a step; they stand <see cref="WallStandOff"/> m back instead, still walking from the frame they appear.
        /// </summary>
        private static void StandOffWalls(List<RunnerRowDef> rows, float halfWidth)
        {
            var walls = rows.Where(r => r.Kind == RunnerRowKind.Barrier && r.Barrier != null && r.Barrier.Width >= (2f * halfWidth) - 0.01f).Select(r => r.At).ToList();
            if (walls.Count == 0)
            {
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Kind != RunnerRowKind.Enemy || r.Finale)
                {
                    continue;
                }

                foreach (var w in walls)
                {
                    if (r.At >= w && r.At < w + WallStandOff)
                    {
                        rows[i] = r with { At = w + WallStandOff };
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// A last check over the whole road: a trap that ended up beside others (or a side barrier, or the finale's edge
        /// traps) with less than <see cref="EscapeGap"/> m of open road moves to another lane, else 2 m back down the road.
        /// </summary>
        private static void KeepEscapeLanes(List<RunnerRowDef> rows, float halfWidth)
        {
            rows.Sort((a, b) => a.At.CompareTo(b.At));
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Hazard == null || r.Finale || OpenRoad(rows, r.At, halfWidth) >= EscapeGap)
                {
                    continue;
                }

                var fixedIt = false;
                for (var back = 0f; back <= 8f && !fixedIt; back += 2f)
                {
                    foreach (var l in DefaultLanes)
                    {
                        var x = Math.Max(-halfWidth + (r.Hazard.Width * 0.5f), Math.Min(halfWidth - (r.Hazard.Width * 0.5f), l));
                        var z = r.At - back;
                        if (OpenRoad(rows, z, halfWidth, (x, r.Hazard.Width), r) >= EscapeGap)
                        {
                            rows[i] = r with { At = z, X = x };
                            fixedIt = true;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The closing set piece of a level without a boss, where the boss section used to stand (Derek 2026-10-27: "too
        /// much empty space at the end of the runner levels that don't have a larger enemy"): a trap gauntlet on both
        /// edges with the middle left open, a fast fodder line down that middle, the old archer line, and a captain (a
        /// heavy: Blightroot, Ashwyrm from Level 7; two of them from <see cref="FinaleSecondCaptainFrom"/>) at the back,
        /// just short of the finish gate (<see cref="FinishPad"/> m past the last rank).
        /// </summary>
        private static void Finale(List<RunnerRowDef> rows, int number, int t, int tc, float z, List<RunnerHazardKind> traps, float halfWidth, Func<string, bool> hasUnit, Func<int, int> w)
        {
            var edge = halfWidth - 1.1f;
            var from = rows.Count;
            AddFinaleTrap(rows, traps[number % traps.Count], z, -edge, halfWidth, t);
            AddFinaleTrap(rows, traps[(number + 1) % traps.Count], z + 1.5f, edge, halfWidth, t);
            Group(rows, z + 3f, 0f, "nightfang", w(3 + (tc / 3)), 1.1f, halfWidth);
            var captain = number >= 7 && hasUnit("ashwyrm") ? "ashwyrm" : "blightroot";
            Group(rows, z + 6f + (FinaleRanks(3 + (tc / 2)) * 1.5f) + 1f, 0f, captain, number >= FinaleSecondCaptainFrom ? 2 : 1, 3.2f, halfWidth);
            for (var i = from; i < rows.Count; i++)
            {
                rows[i] = rows[i] with { Finale = true };
            }

            // The archer line a boss level has as the boss's guard stands between the push and the captain.
            Group(rows, z + 6f, 0f, "bonequill", 3 + (tc / 2), 1.4f, halfWidth);
        }

        private static int FinaleRanks(int count) => (count + MaxPerRank - 1) / MaxPerRank;

        private static void AddFinaleTrap(List<RunnerRowDef> rows, RunnerHazardKind kind, float z, float x, float halfWidth, int t)
        {
            var def = kind switch
            {
                RunnerHazardKind.Spikes => new RunnerHazardDef(kind, 2.0f, SpikeDamage + (SpikeDamageStep * t), 0f),
                RunnerHazardKind.Log => new RunnerHazardDef(kind, 2.2f, LogDamage + (LogDamageStep * t), 0f),
                _ => new RunnerHazardDef(kind, 1.8f, FireDamage + (FireDamageStep * t), 0f)
            };
            rows.Add(new RunnerRowDef(z, RunnerRowKind.Hazard, Math.Max(-halfWidth + (def.Width * 0.5f), Math.Min(halfWidth - (def.Width * 0.5f), x)), "", 1, 0f, null) { Hazard = def });
        }

        /// <summary>A group of <paramref name="count"/> enemies around x: ranks of up to <see cref="MaxPerRank"/>, 1.5 m apart, fitted to the lane.</summary>
        private static void Group(List<RunnerRowDef> rows, float z, float x, string unit, int count, float spread, float halfWidth)
        {
            var rank = 0;
            while (count > 0)
            {
                var n = Math.Min(MaxPerRank, count);
                var s = n > 1 ? Math.Min(spread, ((2f * halfWidth) - 1.4f) / (n - 1)) : 0f;
                var span = s * (n - 1) * 0.5f;
                var cx = Math.Max(-halfWidth + 0.7f + span, Math.Min(halfWidth - 0.7f - span, x + (rank % 2 == 1 ? s * 0.5f : 0f)));
                rows.Add(new RunnerRowDef(z + (rank * 1.5f), RunnerRowKind.Enemy, (float)Math.Round(cx, 2), unit, n, (float)Math.Round(s, 2), null));
                count -= n;
                rank++;
            }
        }

        /// <summary>Whether beat <paramref name="beat"/> of Level <paramref name="number"/> has a breakable barrier.</summary>
        public static bool HasBarrier(int number, int beat) =>
            number >= BarrierFrom && beat >= 2 && (number >= BarrierEveryBeatFrom || (number >= BarrierEveryOtherFrom ? beat % 2 == 0 : beat % 3 == 2));

        private static void AddBarrier(List<RunnerRowDef> rows, float z, int hpAt, int number, int t, int index, Random rnd, float halfWidth, int boxSide)
        {
            var full = number >= FullWidthBarrierFrom && index % FullWidthEvery == FullWidthEvery - 1;
            RunnerBarrierDef def;
            var x = 0f;
            if (full)
            {
                var kind = (index / FullWidthEvery) % 2 == 0 ? RunnerBarrierKind.Palisade : RunnerBarrierKind.StoneWall;
                def = new RunnerBarrierDef(kind, 2f * halfWidth, Math.Max(MinFullWidthHp, (int)Math.Round(hpAt * FullWidthHpShare)), BarrierDamage(kind, t));
            }
            else
            {
                var kind = (RunnerBarrierKind)rnd.Next(3);
                var share = kind switch
                {
                    RunnerBarrierKind.StoneWall => StoneWallHpShare,
                    RunnerBarrierKind.Barricade => BarricadeHpShare,
                    _ => PalisadeHpShare
                };
                // Against the edge away from the beat's box, so it never hides the box (or the wave coming down the middle)
                // from the squad's fire; never the whole road: at least 2 m of open road stays beside it.
                var width = kind == RunnerBarrierKind.StoneWall ? 3.6f : 3.2f;
                x = -boxSide * (halfWidth - (width * 0.5f));
                def = new RunnerBarrierDef(kind, width, Math.Max(MinBarrierHp, (int)Math.Round(hpAt * share)), BarrierDamage(kind, t));
            }

            rows.Add(new RunnerRowDef(z, RunnerRowKind.Barrier, x, "", 1, 0f, null) { Barrier = def });
        }

        public static int BarrierDamage(RunnerBarrierKind kind, int t) => kind switch
        {
            RunnerBarrierKind.StoneWall => StoneWallDamage + (StoneWallDamageStep * t),
            RunnerBarrierKind.Barricade => BarricadeDamage + (BarricadeDamageStep * t),
            _ => PalisadeDamage + (PalisadeDamageStep * t)
        };

        /// <summary>First level whose odd-beat heavy anchor can be Malzareth (after Vorgath's first milestone).</summary>
        public const int MalzarethFrom = 11;

        /// <summary>
        /// The heavy anchor on a beat Ashwyrm doesn't take: Blightroot, or from <see cref="MalzarethFrom"/> Malzareth on
        /// one beat in four. Malzareth's runner unit has Blightroot's numbers, so the swap is a new face, not a harder wave.
        /// </summary>
        internal static string OddHeavy(int number, int beat, Func<string, bool> hasUnit) =>
            number >= MalzarethFrom && beat % 4 == 1 && hasUnit("malzareth") ? "malzareth" : "blightroot";

        /// <summary>
        /// The boss row's unit. Milestone levels (every 10th, which are already boss levels) are Vorgath. The other
        /// boss levels keep Level 1's boss, which is Ironhowl.
        /// </summary>
        private static string BossUnit(RunnerLevelDef first, bool milestone)
        {
            if (milestone)
            {
                return "vorgath";
            }

            foreach (var r in first.Rows)
            {
                if (r.Kind == RunnerRowKind.Boss)
                {
                    return r.Unit;
                }
            }

            return "ironhowl";
        }

        private static RunnerRowDef Box(float z, float x, int hp, RunnerBoxKind kind, string unit, int count, float amount) =>
            new RunnerRowDef(z, RunnerRowKind.Box, x, unit, 1, 0f, new RunnerBoxDef(Math.Max(100, hp), kind, unit, kind == RunnerBoxKind.Hero ? 1 : Math.Max(1, count), amount));
    }

    /// <summary>
    /// Which runner levels can be played. Levels are beaten in order and a beaten level can never be played again: only the
    /// next unbeaten level (the frontier) can be started, a loss there can be retried, a win moves the frontier on.
    /// <c>unlocked</c> is the frontier's number, saved as "runner_unlocked" in survival_save.json; levelCount + 1 means
    /// every level is beaten.
    /// </summary>
    public static class RunnerProgress
    {
        /// <summary>The frontier after <paramref name="result"/> (a win on the frontier moves it on; levelCount + 1 = all beaten).</summary>
        public static int AfterRun(int unlocked, RunnerResult result, int levelCount)
        {
            var open = Math.Max(1, unlocked);
            if (result.Won)
            {
                open = Math.Max(open, result.Number + 1);
            }

            return Math.Max(1, Math.Min(open, Math.Max(1, levelCount) + 1));
        }

        /// <summary>Reached: beaten or the frontier.</summary>
        public static bool IsOpen(int unlocked, int number) => number >= 1 && number <= Math.Max(1, unlocked);

        /// <summary>Beaten: below the frontier. It shows as completed and can never be started again.</summary>
        public static bool IsBeaten(int unlocked, int number) => number >= 1 && number < Math.Max(1, unlocked);

        /// <summary>Only the frontier (the next unbeaten level) can be started.</summary>
        public static bool CanStart(int unlocked, int number, int levelCount) =>
            number >= 1 && number <= levelCount && number == Math.Max(1, unlocked);

        /// <summary>Run <paramref name="number"/> again after a run of it ended: only after a loss (a win beat it for good).</summary>
        public static bool CanRetry(int unlocked, int number, bool won, int levelCount) => !won && CanStart(unlocked, number, levelCount);

        /// <summary>The level to open (the frontier), or null when every level is beaten.</summary>
        public static int? Frontier(int unlocked, int levelCount)
        {
            var f = Math.Max(1, unlocked);
            return f <= levelCount ? f : null;
        }

        public static bool AllBeaten(int unlocked, int levelCount) => Math.Max(1, unlocked) > levelCount;
    }
}
