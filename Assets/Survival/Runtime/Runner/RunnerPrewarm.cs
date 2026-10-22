#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Runner
{
    /// <summary>What kind of rig a prewarm entry is for.</summary>
    public enum RunnerPrewarmRole
    {
        Squad,
        Foe,
        Captive
    }

    /// <summary>One rig shelf to fill before the run: <see cref="Count"/> rigs of <see cref="Roster"/> at <see cref="Scale"/>.</summary>
    public readonly struct RunnerPrewarmEntry
    {
        public RunnerPrewarmEntry(string roster, float scale, int count, RunnerPrewarmRole role)
        {
            Roster = roster;
            Scale = scale;
            Count = count;
            Role = role;
        }

        public string Roster { get; }
        public float Scale { get; }
        public int Count { get; }
        public RunnerPrewarmRole Role { get; }

        public override string ToString() => Role + " " + Roster + " x" + Scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " = " + Count;
    }

    /// <summary>
    /// How many rigs of each kind a level needs on the road at once, so the view can build them all under the loading
    /// screen and never build one mid-run. <see cref="For"/> plays the level with two simple bots (one that chases boxes
    /// and enemies like a player, one that weaves), takes the most of each kind alive or still tumbling at any moment,
    /// and adds a margin (<see cref="Margin"/>); it never asks for more than <see cref="Bound"/>, the layout's hard upper
    /// bound (building that many would cost memory a phone does not have to spare: up to ~240 rigs on the late levels
    /// against ~50 ever on the road).
    /// <see cref="Bound"/>, from the level layout alone (no player behaviour assumed):
    /// <list type="bullet">
    /// <item>enemies: every enemy whose row lies in a sliding window of the road, from <see cref="RunnerRules.SpawnAhead"/>
    /// ahead of the squad back to the deepest squad's back row plus <see cref="RunnerRules.DropBehind"/>, the chase and
    /// the death tumble (an enemy that never dies, never moves and is walked past is on the road that whole time);</item>
    /// <item>squad: the hero plus, per unit, the followers its boxes offer up to the squad cap, plus the ones still
    /// tumbling after a death while their replacement walks in;</item>
    /// <item>captives: the follower and hero boxes in the window from <see cref="RunnerRules.SpawnAhead"/> ahead back to
    /// <see cref="RunnerRules.PassBehind"/>.</item>
    /// </list>
    /// </summary>
    public static class RunnerPrewarm
    {
        /// <summary>Seconds a dead rig stays on the road tumbling (the view's death age before it is gone), with margin.</summary>
        public const float DeathSeconds = 1.2f;

        /// <summary>Followers that can be mid-tumble while their replacements already walk.</summary>
        public const int DyingFollowerMargin = 3;

        /// <summary><see cref="Peaks"/> files the most shots in flight at once under this key.</summary>
        public const string ShotsKey = "Shots";

        /// <summary>The prewarm for one level: the bots' peak of each kind, plus <see cref="Margin"/>, capped by <see cref="Bound"/>.</summary>
        public static IReadOnlyList<RunnerPrewarmEntry> For(RunnerLevelDef level, RunnerCatalog catalog, float captiveScale) =>
            Plan(level, catalog, captiveScale, out _);

        /// <summary><see cref="For"/>, plus the shots to pool (the bots' most in flight at once, plus <see cref="Margin"/>).</summary>
        public static IReadOnlyList<RunnerPrewarmEntry> Plan(RunnerLevelDef level, RunnerCatalog catalog, float captiveScale, out int shots)
        {
            var bound = Bound(level, catalog, captiveScale);
            var seen = Peaks(level, catalog, captiveScale);
            var entries = new List<RunnerPrewarmEntry>(bound.Count);
            foreach (var b in bound)
            {
                var peak = seen.TryGetValue(KeyOf(b.Role, b.Roster, b.Scale), out var p) ? p : 0;
                entries.Add(new RunnerPrewarmEntry(b.Roster, b.Scale, Math.Min(b.Count, Margin(peak)), b.Role));
            }

            shots = Margin(seen.TryGetValue(ShotsKey, out var s) ? s : 0);
            return entries;
        }

        /// <summary>Per kind, the larger of two plans (this level and the next, so Next on the result screen builds nothing).</summary>
        public static List<RunnerPrewarmEntry> Union(IReadOnlyList<RunnerPrewarmEntry> a, IReadOnlyList<RunnerPrewarmEntry> b)
        {
            var merged = new List<RunnerPrewarmEntry>(a.Count + b.Count);
            foreach (var e in a)
            {
                merged.Add(e);
            }

            foreach (var e in b)
            {
                var found = -1;
                for (var i = 0; i < merged.Count; i++)
                {
                    var m = merged[i];
                    if (m.Role == e.Role && m.Roster == e.Roster && Math.Abs(m.Scale - e.Scale) < 1e-4f)
                    {
                        found = i;
                        break;
                    }
                }

                if (found < 0)
                {
                    merged.Add(e);
                }
                else if (e.Count > merged[found].Count)
                {
                    merged[found] = e;
                }
            }

            return merged;
        }

        /// <summary>Headroom over the bots' peak for a player who plays differently: a fifth more, plus one (a rare miss is built live and logged).</summary>
        public static int Margin(int peak) => peak + (int)Math.Ceiling(peak * 0.2) + 1;

        /// <summary>The layout's upper bound for each kind (see the class summary).</summary>
        public static IReadOnlyList<RunnerPrewarmEntry> Bound(RunnerLevelDef level, RunnerCatalog catalog, float captiveScale)
        {
            var entries = new List<RunnerPrewarmEntry>();
            var squad = new Dictionary<string, int>(StringComparer.Ordinal);
            var hero = catalog.Unit(level.Hero);
            Add(squad, hero.Roster, 1);

            // Followers per unit: what the boxes offer, capped by the squad size.
            var offered = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var r in level.Rows)
            {
                if (r.Kind == RunnerRowKind.Box && r.Box != null && r.Box.GivesUnits)
                {
                    Add(offered, catalog.Unit(r.Box.Unit).Roster, r.Box.Count);
                }
            }

            foreach (var pair in offered)
            {
                var alive = Math.Min(level.MaxFollowers, pair.Value);
                var dying = Math.Min(DyingFollowerMargin, pair.Value - alive);
                Add(squad, pair.Key, alive + dying);
            }

            foreach (var pair in squad)
            {
                entries.Add(new RunnerPrewarmEntry(pair.Key, 1f, pair.Value, RunnerPrewarmRole.Squad));
            }

            var depth = -RunnerFormation.Offset(Math.Max(0, level.MaxFollowers - 1)).Z;
            var behind = depth + RunnerRules.DropBehind + ((RunnerRules.ChaseSeconds + DeathSeconds) * level.Speed) + 1f;
            foreach (var (roster, scale, count) in WindowPeaks(level, catalog, behind, foes: true))
            {
                entries.Add(new RunnerPrewarmEntry(roster, scale, count, RunnerPrewarmRole.Foe));
            }

            foreach (var (roster, _, count) in WindowPeaks(level, catalog, RunnerRules.PassBehind + 1f, foes: false))
            {
                entries.Add(new RunnerPrewarmEntry(roster, captiveScale, count, RunnerPrewarmRole.Captive));
            }

            return entries;
        }

        /// <summary>The key <see cref="Peaks"/> files a kind under.</summary>
        public static string KeyOf(RunnerPrewarmRole role, string roster, float scale) =>
            role + "|" + roster + "|" + (int)Math.Round(scale * 1000f);

        /// <summary>
        /// Plays the level with each bot at 30 steps a second and returns, per <see cref="KeyOf"/>, the most rigs of that
        /// kind on the road at once: live squad members, enemies and follower boxes, plus the dead still tumbling
        /// (<see cref="DeathSeconds"/>).
        /// </summary>
        public static Dictionary<string, int> Peaks(RunnerLevelDef level, RunnerCatalog catalog, float captiveScale, float maxSeconds = 300f)
        {
            var peaks = new Dictionary<string, int>(StringComparer.Ordinal);
            Play(new RunnerRun(catalog, level), catalog, captiveScale, ChaseBot, maxSeconds, peaks);
            Play(new RunnerRun(catalog, level), catalog, captiveScale, WeaveBot, maxSeconds, peaks);
            return peaks;
        }

        private static void Play(RunnerRun run, RunnerCatalog catalog, float captiveScale, Func<RunnerRun, float> steer, float maxSeconds, Dictionary<string, int> peaks)
        {
            var keyOf = new Dictionary<int, string>();
            var dying = new List<(float Until, string Key)>();
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            while (!run.Over && run.Seconds < maxSeconds)
            {
                run.SteerTo(steer(run));
                var events = run.Step(1f / 30f);
                for (var i = 0; i < events.Count; i++)
                {
                    var e = events[i];
                    var gone = e.Kind == RunnerEventKind.FoeKilled || e.Kind == RunnerEventKind.FoePassed ? -1 - e.Id
                        : e.Kind == RunnerEventKind.FollowerLost ? e.Id : int.MinValue;
                    if (gone != int.MinValue && keyOf.TryGetValue(gone, out var k))
                    {
                        dying.Add((run.Seconds + DeathSeconds, k));
                    }
                }

                dying.RemoveAll(d => d.Until < run.Seconds);
                counts.Clear();
                foreach (var f in run.Foes)
                {
                    var k = KeyOf(RunnerPrewarmRole.Foe, f.Unit.Roster, f.Unit.Scale);
                    keyOf[-1 - f.Id] = k;
                    Add(counts, k, 1);
                }

                foreach (var m in run.Squad)
                {
                    var k = KeyOf(RunnerPrewarmRole.Squad, m.Unit.Roster, 1f);
                    keyOf[m.Id] = k;
                    Add(counts, k, 1);
                }

                foreach (var b in run.Boxes)
                {
                    if (b.Def.GivesUnits)
                    {
                        Add(counts, KeyOf(RunnerPrewarmRole.Captive, catalog.Unit(b.Def.Unit).Roster, captiveScale), 1);
                    }
                }

                foreach (var d in dying)
                {
                    Add(counts, d.Key, 1);
                }

                counts[ShotsKey] = run.Shots.Count;

                foreach (var pair in counts)
                {
                    if (!peaks.TryGetValue(pair.Key, out var have) || pair.Value > have)
                    {
                        peaks[pair.Key] = pair.Value;
                    }
                }
            }
        }

        /// <summary>Steers at the nearest box ahead within 22 m, else the nearest enemy ahead within 16 m.</summary>
        public static float ChaseBot(RunnerRun run)
        {
            RunnerBox? box = null;
            foreach (var b in run.Boxes)
            {
                if (b.Z > run.Hero.Z && b.Z - run.Hero.Z < 22f && (box == null || b.Z < box.Z))
                {
                    box = b;
                }
            }

            if (box != null)
            {
                return box.X;
            }

            RunnerFoe? foe = null;
            foreach (var f in run.Foes)
            {
                if (f.Hp > 0 && f.Z > run.Hero.Z - 0.5f && f.Z - run.Hero.Z < 16f && (foe == null || f.Z < foe.Z))
                {
                    foe = f;
                }
            }

            return foe?.X ?? run.Hero.X;
        }

        /// <summary>Sweeps the lane side to side.</summary>
        public static float WeaveBot(RunnerRun run) => (float)Math.Sin(run.Seconds * 0.9) * run.MaxSteer;

        /// <summary>Total rigs <see cref="For"/> asks for.</summary>
        public static int Total(IReadOnlyList<RunnerPrewarmEntry> entries)
        {
            var n = 0;
            foreach (var e in entries)
            {
                n += e.Count;
            }

            return n;
        }

        /// <summary>
        /// Per (roster, scale): the most enemies (or captives) whose rows lie within [d - behind, d + SpawnAhead] for any
        /// squad distance d. The count only changes where a row enters or leaves the window, so checking each row as
        /// the window's front edge and as its back edge finds the peak.
        /// </summary>
        private static List<(string Roster, float Scale, int Count)> WindowPeaks(RunnerLevelDef level, RunnerCatalog catalog, float behind, bool foes)
        {
            var items = new List<(float At, string Roster, float Scale, int Count)>();
            foreach (var r in level.Rows)
            {
                if (foes && (r.Kind == RunnerRowKind.Enemy || r.Kind == RunnerRowKind.Boss))
                {
                    var u = catalog.Unit(r.Unit);
                    items.Add((r.At, u.Roster, u.Scale, r.Kind == RunnerRowKind.Boss ? 1 : r.Count));
                }
                else if (!foes && r.Kind == RunnerRowKind.Box && r.Box != null && r.Box.GivesUnits)
                {
                    items.Add((r.At, catalog.Unit(r.Box.Unit).Roster, 1f, 1));
                }
            }

            var peaks = new List<(string Roster, float Scale, int Count)>();
            var window = behind + RunnerRules.SpawnAhead;
            foreach (var key in items)
            {
                var known = false;
                foreach (var p in peaks)
                {
                    if (p.Roster == key.Roster && Math.Abs(p.Scale - key.Scale) < 1e-4f)
                    {
                        known = true;
                        break;
                    }
                }

                if (known)
                {
                    continue;
                }

                var best = 0;
                foreach (var edge in items)
                {
                    // Window [edge.At, edge.At + window] (edge is the back-most row) and [edge.At - window, edge.At].
                    var fromBack = 0;
                    var fromFront = 0;
                    foreach (var it in items)
                    {
                        if (it.Roster != key.Roster || Math.Abs(it.Scale - key.Scale) >= 1e-4f)
                        {
                            continue;
                        }

                        if (it.At >= edge.At - 1e-4f && it.At <= edge.At + window + 1e-4f)
                        {
                            fromBack += it.Count;
                        }

                        if (it.At <= edge.At + 1e-4f && it.At >= edge.At - window - 1e-4f)
                        {
                            fromFront += it.Count;
                        }
                    }

                    best = Math.Max(best, Math.Max(fromBack, fromFront));
                }

                peaks.Add((key.Roster, key.Scale, best));
            }

            return peaks;
        }

        private static void Add(Dictionary<string, int> into, string key, int n) =>
            into[key] = (into.TryGetValue(key, out var have) ? have : 0) + n;
    }
}
