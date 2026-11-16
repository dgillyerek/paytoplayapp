#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    public enum RunnerHazardState
    {
        /// <summary>Harmless for now (spikes down, a log waiting at the top of its lane).</summary>
        Idle,

        /// <summary>About to hurt: spikes rattling, a log wobbling. The scene shows it plainly.</summary>
        Warning,

        /// <summary>Hurts what it touches (spikes up, a log rolling, fire always).</summary>
        Active
    }

    /// <summary>A lane trap in play. Cannot be shot; the squad steers round it.</summary>
    public sealed class RunnerHazard
    {
        private readonly HashSet<int> _hit = new();

        internal RunnerHazard(int id, RunnerHazardDef def, float x, float z)
        {
            Id = id;
            Def = def;
            X = x;
            Z = z;
            StartZ = z;
            State = def.Kind == RunnerHazardKind.Fire ? RunnerHazardState.Active : RunnerHazardState.Idle;
        }

        public int Id { get; }
        public RunnerHazardDef Def { get; }
        public RunnerHazardKind Kind => Def.Kind;
        public float X { get; }
        public float Z { get; internal set; }
        public float StartZ { get; }

        /// <summary>Where it was at the start of this step (a log rolls).</summary>
        internal float PrevZ { get; set; }
        public float HalfWidth => Def.Width * 0.5f;

        public float HalfDepth => HalfDepthOf(Def.Kind);

        /// <summary>Half the trap's depth along the road for <paramref name="kind"/> (the view builds pooled traps from it).</summary>
        public static float HalfDepthOf(RunnerHazardKind kind) => kind switch
        {
            RunnerHazardKind.Spikes => RunnerRules.SpikeHalfDepth,
            RunnerHazardKind.Log => RunnerRules.LogHalfDepth,
            _ => RunnerRules.FireHalfDepth
        };

        public RunnerHazardState State { get; internal set; }

        /// <summary>Seconds in the current state (the scene times the rattle / wobble / spike pop on it).</summary>
        public float StateClock { get; internal set; }

        /// <summary>Squad members this activation has already caught (each once per activation).</summary>
        internal HashSet<int> Hit => _hit;

        /// <summary>Members caught this activation, and the most one activation catches.</summary>
        internal int Caught { get; set; }

        internal int CatchCap => Def.Kind == RunnerHazardKind.Log ? RunnerRules.LogCatch : RunnerRules.SpikeCatch;

        public bool Hurts => State == RunnerHazardState.Active;

        public bool Touches(float x, float z, float radius) =>
            Math.Abs(x - X) <= HalfWidth + (radius * RunnerRules.HazardBodyShare)
            && Math.Abs(z - Z) <= HalfDepth + (radius * RunnerRules.HazardBodyShare);

        /// <summary>True when a member now touching it came in from the side (see <see cref="RunnerObstacle.SideEntry"/>).</summary>
        public bool SideEntry(float prevX, float prevZ, float radius) =>
            RunnerObstacle.SideEntry(X, PrevZ, HalfWidth, HalfDepth, prevX, prevZ, radius * RunnerRules.HazardBodyShare);
    }

    /// <summary>Footprint geometry shared by traps and barriers.</summary>
    public static class RunnerObstacle
    {
        /// <summary>
        /// A side hit: at the start of the step the body was already level with the obstacle along the road (its depth
        /// overlapped) but clear of it sideways, so it got there by steering into it, not by walking into its face.
        /// </summary>
        public static bool SideEntry(float x, float z, float halfWidth, float halfDepth, float prevX, float prevZ, float pad) =>
            Math.Abs(prevZ - z) <= halfDepth + pad && Math.Abs(prevX - x) > halfWidth + pad;

        /// <summary>True when a body of <paramref name="radius"/> at (<paramref name="px"/>, <paramref name="pz"/>) would stand on the footprint.</summary>
        public static bool Overlaps(float x, float z, float halfWidth, float halfDepth, float px, float pz, float radius) =>
            Math.Abs(px - x) < halfWidth + radius && Math.Abs(pz - z) < halfDepth + radius;
    }
}
