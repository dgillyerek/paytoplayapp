#nullable enable
using System;

namespace Survival.Domain.View
{
    /// <summary>The two Play screens: the zoomed-out kingdom map and the zoomed-in player village.</summary>
    public enum PlayScreen
    {
        Village,
        World
    }

    /// <summary>
    /// World / Village switch behind the Play HUD toggle button. Taps during the quick cross-fade are ignored,
    /// so a double tap cannot bounce the player back. <see cref="Fade"/> drives the incoming view's alpha.
    /// </summary>
    public sealed class PlayScreenToggle
    {
        public const string LabelToWorld = "view.toggle.world";
        public const string LabelToVillage = "view.toggle.village";

        public PlayScreenToggle(PlayScreen start = PlayScreen.Village, float transitionSeconds = 0.35f)
        {
            if (transitionSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(transitionSeconds));
            }

            Current = start;
            Previous = start;
            TransitionSeconds = transitionSeconds;
            Progress = 1f;
        }

        public PlayScreen Current { get; private set; }

        public PlayScreen Previous { get; private set; }

        public float TransitionSeconds { get; }

        /// <summary>0 right after a switch, 1 once the transition has settled.</summary>
        public float Progress { get; private set; }

        public bool InTransition => Progress < 1f;

        /// <summary>Smoothstep of <see cref="Progress"/>: alpha of the incoming view (outgoing is 1 - Fade).</summary>
        public float Fade => Progress * Progress * (3f - 2f * Progress);

        /// <summary>The screen the toggle button sends you to (its label shows the destination).</summary>
        public PlayScreen Destination => Other(Current);

        /// <summary>String key for the button label: "World" while in the village, "Village" on the map.</summary>
        public string LabelKey => Current == PlayScreen.Village ? LabelToWorld : LabelToVillage;

        /// <summary>(from, to) after each accepted switch.</summary>
        public event Action<PlayScreen, PlayScreen>? Changed;

        public static PlayScreen Other(PlayScreen screen) =>
            screen == PlayScreen.Village ? PlayScreen.World : PlayScreen.Village;

        public bool Toggle() => Set(Destination);

        /// <summary>Switch to <paramref name="screen"/>. False when already there or mid-transition.</summary>
        public bool Set(PlayScreen screen)
        {
            if (screen == Current || InTransition)
            {
                return false;
            }

            Previous = Current;
            Current = screen;
            Progress = 0f;
            Changed?.Invoke(Previous, Current);
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || !InTransition)
            {
                return;
            }

            Progress = Math.Min(1f, Progress + deltaSeconds / TransitionSeconds);
        }
    }
}
