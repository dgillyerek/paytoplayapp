#nullable enable
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Survival.Unity
{
    /// <summary>
    /// Dev-only frame-time logger for the runner (the Editor and Development builds; every call compiles away in a
    /// release build). The view calls <see cref="Frame"/> once at the top of each Update; anything the runner does
    /// between two calls is counted, and when the gap between them (the real frame time) passes
    /// <see cref="ThresholdMs"/> it logs one line, prefixed <see cref="Prefix"/>, naming the frame's counts and the
    /// milliseconds the runner's own code took, so a freeze names its cause:
    /// <code>[RunnerHitch] 412 ms frame at L3 31.2s (loading=no) | built 2 rig(s) live: Nightfang, Bonequill | reused 3 | spawned foes 4 boxes 1 | shots 6 fx 3 floaters 2 poses 14 kills 1 | events 9 | gc0 +1 | own ms sim 0.2 actors 380.1 shots 0.4 hud 0.3 hazards 0.1 late 0.2 cam 0.1 finish 0.0 | live foes 18 squad 24 shots 60 | boss quake-telegraph | renderers active 210/260 particle systems playing 0/0 | timeScale 1 | last transitions boss quake telegraph (1.2s ago) -> ...</code>
    /// Heavy synchronous stretches (<see cref="Heavy"/>) log "begin"/"end" lines of their own, so a freeze names where it stopped even though the frame's line only arrives after it ends.
    /// If "own ms" is small next to the frame time, the stall was outside the runner's scripts (rendering, a shader
    /// compile, an asset import or the GC). A pool miss (a rig built mid-run because the prewarm was too small) logs
    /// on its own line too, and <see cref="Summary"/> prints the run's worst frame and how many passed 50 / 100 ms.
    /// </summary>
    public static class RunnerHitchLog
    {
        public const string Prefix = "[RunnerHitch]";
        public const float ThresholdMs = 100f;
        public const float BudgetMs = 50f;

        public enum Counter
        {
            RigBuilt,
            RigBuiltPrewarm,
            RigReused,
            FoeSpawned,
            BoxSpawned,
            HazardSpawned,
            FollowerJoined,
            Shot,
            Fx,
            Floater,
            Pose,
            Kill,
            Event,
            Bar,
            Count
        }

        public enum Section
        {
            Sim,
            Actors,
            Shots,
            Hud,
            Hazards,
            Late,
            Camera,
            Finish,
            Count
        }

        /// <summary>
        /// A synchronous heavy stretch of the runner's own code (the result-screen build, a loading-screen swap, a level
        /// change). Logs a line when it starts (so a freeze still leaves a trail) and one when it ends with its
        /// milliseconds. Use as <c>using (RunnerHitchLog.Heavy("result screen"))</c>; compiles to nothing in a release build.
        /// </summary>
        public static HeavyScope Heavy(string name) => new HeavyScope(name);

        public readonly struct HeavyScope : System.IDisposable
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            private readonly string _name;
            private readonly long _start;

            public HeavyScope(string name)
            {
                _name = name;
                _start = Stopwatch.GetTimestamp();
                Transition("begin " + name);
                Debug.Log(Prefix + " begin " + name + " at " + Where() + " (loading=" + (_loading ? "yes" : "no") + ")");
            }

            public void Dispose()
            {
                var ms = (Stopwatch.GetTimestamp() - _start) * 1000.0 / Stopwatch.Frequency;
                Transition("end " + _name);
                var line = Prefix + " end " + _name + ": " + ms.ToString("0.0") + " ms";
                if (ms > BudgetMs)
                {
                    Debug.LogWarning(line);
                }
                else
                {
                    Debug.Log(line);
                }
            }
#else
            public HeavyScope(string name)
            {
            }

            public void Dispose()
            {
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly int[] Counts = new int[(int)Counter.Count];
        private static readonly long[] Ticks = new long[(int)Section.Count];
        private static readonly long[] Started = new long[(int)Section.Count];
        private static readonly StringBuilder Built = new StringBuilder(64);
        private static readonly StringBuilder Line = new StringBuilder(256);
        private static double _last = -1;
        private static int _gc;
        private static int _whereLevel;
        private static float _whereSeconds;
        private static bool _loading;
        private static int _frames;
        private static int _over50;
        private static int _over100;
        private static float _worst;
        private static int _misses;
        private static readonly string[] Recent = new string[3];
        private static readonly float[] RecentAt = new float[3];
        private static int _recentN;
        private static System.Func<string>? _describe;
#endif

        /// <summary>
        /// The view's one-line account of the scene right now (boss state, and so on), read only when a hitch is logged.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Describe(System.Func<string>? describe)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _describe = describe;
#endif
        }

        /// <summary>A runner state change (run start, boss telegraph, win, result shown, ...). The last three ride on every hitch line.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Transition(string what)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var i = _recentN % Recent.Length;
            Recent[i] = what;
            RecentAt[i] = Time.realtimeSinceStartup;
            _recentN++;
#endif
        }

        /// <summary>Count one thing that happened this frame.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Note(Counter c, int n = 1)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Counts[(int)c] += n;
#endif
        }

        /// <summary>A rig was built: under the loading screen (<paramref name="prewarm"/>) or live, a pool miss that is logged at once.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void NoteRigBuilt(string roster, bool prewarm)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (prewarm || _loading)
            {
                Counts[(int)Counter.RigBuiltPrewarm]++;
                return;
            }

            Counts[(int)Counter.RigBuilt]++;
            _misses++;
            if (Built.Length < 200)
            {
                Built.Append(Built.Length > 0 ? ", " : "").Append(roster);
            }

            Debug.LogWarning(Prefix + " pool miss at " + Where() + ": built a " + roster + " rig live (the level's prewarm was too small for this run)");
#endif
        }

        /// <summary>Start timing one of the runner's own sections (pair with <see cref="End"/>).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Begin(Section s)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Started[(int)s] = Stopwatch.GetTimestamp();
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void End(Section s)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Ticks[(int)s] += Stopwatch.GetTimestamp() - Started[(int)s];
#endif
        }

        /// <summary>Loading-screen frames are logged too but tagged loading=yes and left out of the run summary.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Loading(bool loading)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _loading = loading;
#endif
        }

        /// <summary>A new run starts: clear the summary.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Reset()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _frames = _over50 = _over100 = _misses = 0;
            _worst = 0f;
            _last = -1;
            Transition("run start");
            Clear();
#endif
        }

        /// <summary>
        /// Call once at the top of the view's Update. Closes the frame that just ended: if it ran over
        /// <see cref="ThresholdMs"/>, logs what the runner did in it. <paramref name="level"/> and <paramref name="seconds"/>
        /// say where in the run it was; the live counts say how busy the road was.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Frame(int level, float seconds, int foes, int squad, int shots)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var now = Time.realtimeSinceStartupAsDouble;
            var ms = _last < 0 ? 0f : (float)((now - _last) * 1000.0);
            _last = now;
            var gc = System.GC.CollectionCount(0);
            var gcDelta = gc - _gc;
            _gc = gc;
            if (!_loading && ms > 0f)
            {
                _frames++;
                _worst = Mathf.Max(_worst, ms);
                _over50 += ms > BudgetMs ? 1 : 0;
                _over100 += ms > ThresholdMs ? 1 : 0;
            }

            if (ms > ThresholdMs)
            {
                var f = 1000.0 / Stopwatch.Frequency;
                Line.Clear();
                Line.Append(Prefix).Append(' ').Append((int)ms).Append(" ms frame at ").Append(Where())
                    .Append(" (loading=").Append(_loading ? "yes" : "no").Append(')');
                if (Counts[(int)Counter.RigBuilt] > 0)
                {
                    Line.Append(" | built ").Append(Counts[(int)Counter.RigBuilt]).Append(" rig(s) live: ").Append(Built);
                }

                if (Counts[(int)Counter.RigBuiltPrewarm] > 0)
                {
                    Line.Append(" | prewarmed ").Append(Counts[(int)Counter.RigBuiltPrewarm]).Append(" rig(s)");
                }

                Line.Append(" | reused ").Append(Counts[(int)Counter.RigReused])
                    .Append(" | spawned foes ").Append(Counts[(int)Counter.FoeSpawned])
                    .Append(" boxes ").Append(Counts[(int)Counter.BoxSpawned])
                    .Append(" traps ").Append(Counts[(int)Counter.HazardSpawned])
                    .Append(" joined ").Append(Counts[(int)Counter.FollowerJoined])
                    .Append(" | shots ").Append(Counts[(int)Counter.Shot])
                    .Append(" fx ").Append(Counts[(int)Counter.Fx])
                    .Append(" floaters ").Append(Counts[(int)Counter.Floater])
                    .Append(" bars ").Append(Counts[(int)Counter.Bar])
                    .Append(" poses ").Append(Counts[(int)Counter.Pose])
                    .Append(" kills ").Append(Counts[(int)Counter.Kill])
                    .Append(" | events ").Append(Counts[(int)Counter.Event])
                    .Append(" | gc0 +").Append(gcDelta)
                    .Append(" | own ms sim ").Append((Ticks[(int)Section.Sim] * f).ToString("0.0"))
                    .Append(" actors ").Append((Ticks[(int)Section.Actors] * f).ToString("0.0"))
                    .Append(" shots ").Append((Ticks[(int)Section.Shots] * f).ToString("0.0"))
                    .Append(" hud ").Append((Ticks[(int)Section.Hud] * f).ToString("0.0"))
                    .Append(" hazards ").Append((Ticks[(int)Section.Hazards] * f).ToString("0.0"))
                    .Append(" late ").Append((Ticks[(int)Section.Late] * f).ToString("0.0"))
                    .Append(" cam ").Append((Ticks[(int)Section.Camera] * f).ToString("0.0"))
                    .Append(" finish ").Append((Ticks[(int)Section.Finish] * f).ToString("0.0"))
                    .Append(" | live foes ").Append(foes).Append(" squad ").Append(squad).Append(" shots ").Append(shots);
                AppendScene();
                Debug.LogWarning(Line.ToString());
            }

            _whereLevel = level;
            _whereSeconds = seconds;
            Clear();
#endif
        }

        /// <summary>Logs the run's frame-time summary (call when a run ends).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Summary(int level, string outcome, int foesPeak, int squadPeak, int shotsPeak)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(Prefix + " summary L" + level + " " + outcome + ": " + _frames + " frames, worst " + (int)_worst + " ms, over "
                + (int)BudgetMs + " ms: " + _over50 + ", over " + (int)ThresholdMs + " ms: " + _over100 + ", pool misses: " + _misses
                + " | peak live foes " + foesPeak + " squad " + squadPeak + " shots " + shotsPeak);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Formatted only when a line is logged, so a smooth frame allocates nothing here.
        private static string Where() => "L" + _whereLevel + " " + _whereSeconds.ToString("0.0") + "s";

        // Only on a logged hitch (never on a smooth frame): what is on screen and how the runner got here.
        private static void AppendScene()
        {
            var renderers = 0;
            var enabled = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                renderers++;
                enabled += r.enabled && r.gameObject.activeInHierarchy ? 1 : 0;
            }

            var particles = 0;
            var playing = 0;
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                particles++;
                playing += ps.isPlaying ? 1 : 0;
            }

            Line.Append(" | boss ").Append(_describe != null ? _describe() : "?")
                .Append(" | renderers active ").Append(enabled).Append('/').Append(renderers)
                .Append(" particle systems playing ").Append(playing).Append('/').Append(particles)
                .Append(" | timeScale ").Append(Time.timeScale.ToString("0.##"))
                .Append(" | last transitions ");
            var n = System.Math.Min(_recentN, Recent.Length);
            if (n == 0)
            {
                Line.Append("none");
            }

            var now = Time.realtimeSinceStartup;
            for (var k = n; k > 0; k--)
            {
                var i = (_recentN - k) % Recent.Length;
                Line.Append(k < n ? " -> " : "").Append(Recent[i]).Append(" (").Append((now - RecentAt[i]).ToString("0.0")).Append("s ago)");
            }
        }

        private static void Clear()
        {
            System.Array.Clear(Counts, 0, Counts.Length);
            System.Array.Clear(Ticks, 0, Ticks.Length);
            Built.Clear();
        }
#endif
    }
}
