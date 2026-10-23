#nullable enable
using Survival.Domain.Runner;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The handshake between the Play-side trip (<see cref="SurvivalRunnerEntry"/>, which owns the loading screen) and the
    /// runner scene's own boot (<see cref="RunnerLevelView"/>, which now builds its pools over many frames).
    /// The trip arms one <see cref="RunnerBootProgress"/> before it loads the scene; the view claims it when it starts,
    /// updates it as it builds, and finishes it when the run is live. The trip polls it to draw the bar, the percentage and
    /// the step name, and only reveals the runner when it is finished (or a failsafe time runs out).
    /// </summary>
    internal static class RunnerBoot
    {
        private static RunnerBootProgress? _armed;

        /// <summary>The boot the trip is watching, if any.</summary>
        public static RunnerBootProgress? Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _armed = null;
            Current = null;
        }

        /// <summary>The trip is about to load the runner scene: start a progress for its boot to claim.</summary>
        public static RunnerBootProgress Arm()
        {
            _armed = new RunnerBootProgress();
            Current = _armed;
            return _armed;
        }

        /// <summary>The view's boot starts (or a level switch): take the armed progress, else make a fresh one.</summary>
        public static RunnerBootProgress Begin()
        {
            var p = _armed ?? new RunnerBootProgress();
            _armed = null;
            Current = p;
            return p;
        }

        /// <summary>"Mustering heroes and foes 12/35 - 47%": the line above the loading bar.</summary>
        public static string Status(RunnerBootProgress p) => p.Label() + "  " + p.Percent + "%";
    }
}
