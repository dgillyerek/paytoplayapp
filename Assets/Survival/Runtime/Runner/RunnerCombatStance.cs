#nullable enable
using System;

namespace Survival.Domain.Runner
{
    /// <summary>What a runner squad member's body shows: its world walk, or its attack / weapon aim.</summary>
    public enum RunnerStance
    {
        Walk,
        Attack,
    }

    /// <summary>
    /// Walk or attack for one squad member in the runner (Derek 2026-10-29: "if there is no enemies or chests in striking
    /// range the characters go into their standard walking scene modes"). In range (an attack take running, which the sim
    /// only starts with a foe, crate or barrier inside the member's own reach) means Attack at once; out of range it holds
    /// Attack for <see cref="HoldSeconds"/> so a target at the reach edge does not flicker it, then drops to Walk.
    /// <see cref="Weight"/> eases 0 (walk clip only) to 1 (full aim) over <see cref="BlendSeconds"/>, so nothing pops.
    /// </summary>
    public sealed class RunnerCombatStance
    {
        /// <summary>Seconds out of range before the stance drops back to Walk (the hysteresis).</summary>
        public const float HoldSeconds = 0.6f;

        /// <summary>Seconds of the crossfade between the walk and the attack pose (either way).</summary>
        public const float BlendSeconds = 0.25f;

        private float _outOfRange = HoldSeconds;

        public RunnerStance Stance { get; private set; } = RunnerStance.Walk;

        /// <summary>0 = the plain walk, 1 = the full attack pose.</summary>
        public float Weight { get; private set; }

        /// <summary>The pure rule without hysteresis: in range attacks, else walks.</summary>
        public static RunnerStance For(bool targetInRange) => targetInRange ? RunnerStance.Attack : RunnerStance.Walk;

        public RunnerStance Step(bool targetInRange, float dt)
        {
            dt = Math.Max(0f, dt);
            _outOfRange = targetInRange ? 0f : _outOfRange + dt;
            Stance = targetInRange || _outOfRange < HoldSeconds ? RunnerStance.Attack : RunnerStance.Walk;
            var goal = Stance == RunnerStance.Attack ? 1f : 0f;
            var step = dt / BlendSeconds;
            Weight = Weight < goal ? Math.Min(goal, Weight + step) : Math.Max(goal, Weight - step);
            return Stance;
        }

        /// <summary>Back to walking with no aim (a pooled rig reused, a death).</summary>
        public void Reset()
        {
            _outOfRange = HoldSeconds;
            Stance = RunnerStance.Walk;
            Weight = 0f;
        }
    }
}
