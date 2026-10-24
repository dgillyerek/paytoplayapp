#nullable enable
using System;

namespace Survival.Domain.Runner
{
    /// <summary>Where the runner's boot is, for the loading screen's status line.</summary>
    public enum RunnerBootStep
    {
        Scene,
        Plan,
        Rigs,
        Shots,
        Effects,
        Traps,
        Shaders,
        Start,
        Done
    }

    /// <summary>What the boot loop does next.</summary>
    public enum RunnerBootVerdict
    {
        /// <summary>More to build, time left.</summary>
        Work,

        /// <summary>Everything is built but the screen has not been up <see cref="RunnerBootProgress.MinSeconds"/> yet.</summary>
        Hold,

        /// <summary>Built (or given up on) and shown long enough: start the run.</summary>
        Go,

        /// <summary>Out of time with work left: stop building, start the run, build the rest live (logged).</summary>
        TimedOut
    }

    /// <summary>
    /// Real progress of the runner's boot, so the loading screen's bar, percentage and step text move with the work
    /// instead of sitting still. The boot registers what it will do (<see cref="Expect"/>, in work units that stand for
    /// roughly equal time: a rig is 1, a shot 0.03, an effect sprite 0.01), reports each piece (<see cref="Did"/>), and
    /// the loading screen reads <see cref="Fraction"/>, <see cref="Percent"/> and <see cref="Label"/>. The policy
    /// (<see cref="FrameBudgetMs"/> of work per frame, <see cref="MinSeconds"/> on screen at least,
    /// <see cref="TimeoutSeconds"/> at most) lives here so it is tested.
    /// </summary>
    public sealed class RunnerBootProgress
    {
        /// <summary>Build for this long, then yield a frame so the bar and the percentage can draw (one rig may overshoot it).</summary>
        public const double FrameBudgetMs = 10.0;

        /// <summary>The boot never finishes sooner than this after it starts (no flash of a loading screen).</summary>
        public const double MinSeconds = 0.8;

        /// <summary>Stop building and start the run after this long, leaving room before the screen's 20 s self-clear.</summary>
        public const double TimeoutSeconds = 14.0;

        /// <summary>The share of the bar the scene load takes; the boot fills the rest.</summary>
        public const float SceneShare = 0.1f;

        public const double ShotUnits = 0.03;
        public const double FxUnits = 0.01;
        public const double TrapUnits = 0.5;
        public const double ShaderUnits = 2.0;
        public const double PlanUnits = 0.3;
        public const double StartUnits = 0.5;

        private double _total;
        private double _done;
        private int _stepTotal;
        private int _stepDone;

        public RunnerBootStep Step { get; private set; } = RunnerBootStep.Scene;

        public bool Finished { get; private set; }

        public bool TimedOut { get; private set; }

        /// <summary>Work done over work expected, 0..1; stays under 1 until <see cref="Finish"/>.</summary>
        public float Fraction => Finished ? 1f : _total <= 0.0 ? 0f : (float)Math.Min(0.99, _done / _total);

        public int Percent => (int)Math.Round(Fraction * 100f);

        /// <summary>Items done / expected in the current step (rigs built so far, say).</summary>
        public int StepDone => _stepDone;

        public int StepTotal => _stepTotal;

        /// <summary>Add expected work. Call before the work it covers, so the bar's end is known early.</summary>
        public void Expect(double units) => _total += Math.Max(0.0, units);

        /// <summary>Start a step of <paramref name="count"/> items.</summary>
        public void Begin(RunnerBootStep step, int count)
        {
            Step = step;
            _stepTotal = count;
            _stepDone = 0;
        }

        /// <summary>One or more items of the current step are done, worth <paramref name="units"/> in total.</summary>
        public void Did(double units, int items = 1)
        {
            _done += Math.Max(0.0, units);
            _stepDone += items;
        }

        public void Finish()
        {
            Step = RunnerBootStep.Done;
            Finished = true;
        }

        public void MarkTimedOut() => TimedOut = true;

        /// <summary>English status line: "Mustering the squad 12/35".</summary>
        public string Label()
        {
            var counted = _stepTotal > 1 ? " " + _stepDone + "/" + _stepTotal : "";
            return Step switch
            {
                RunnerBootStep.Scene => "Loading the road",
                RunnerBootStep.Plan => "Scouting the level",
                RunnerBootStep.Rigs => "Mustering heroes and foes" + counted,
                RunnerBootStep.Shots => "Fletching arrows" + counted,
                RunnerBootStep.Effects => "Gathering sparks",
                RunnerBootStep.Traps => "Setting traps" + counted,
                RunnerBootStep.Shaders => "Warming the lights",
                RunnerBootStep.Start => "Ready",
                _ => "Ready"
            };
        }

        /// <summary>The loading screen's bar: the scene load first, then the boot.</summary>
        public static float Bar(float sceneProgress, float bootFraction)
        {
            var scene = Math.Max(0f, Math.Min(1f, sceneProgress));
            var boot = Math.Max(0f, Math.Min(1f, bootFraction));
            return (scene * SceneShare) + (boot * (1f - SceneShare));
        }

        /// <summary>Whether to keep building this frame: true while under <see cref="FrameBudgetMs"/> since the frame's work began.</summary>
        public static bool HasFrameBudget(double msSinceFrameWorkStarted) => msSinceFrameWorkStarted < FrameBudgetMs;

        /// <summary>What the boot loop does at <paramref name="elapsedSeconds"/> since it started.</summary>
        public static RunnerBootVerdict Decide(double elapsedSeconds, bool workDone)
        {
            if (!workDone)
            {
                return elapsedSeconds >= TimeoutSeconds ? RunnerBootVerdict.TimedOut : RunnerBootVerdict.Work;
            }

            return elapsedSeconds >= MinSeconds ? RunnerBootVerdict.Go : RunnerBootVerdict.Hold;
        }
    }
}
