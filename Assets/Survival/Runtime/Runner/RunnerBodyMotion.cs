#nullable enable
using System;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// How far a hit may move a runner rig's body (Derek 2026-10-23: "when blightroot gets hit he bounces back almost
    /// off screen"). The sim never pushes a foe back; the rig shows the battle's recoil step only as a small flinch,
    /// capped here for every enemy whatever its recoil profile, reaction or scale.
    /// </summary>
    public static class RunnerHitNudge
    {
        /// <summary>Most a hit moves a runner rig's body sideways or back, in metres.</summary>
        public const float MaxMetres = 0.05f;

        /// <summary>Factor that brings a ground nudge (<paramref name="x"/>, <paramref name="z"/>) to at most <see cref="MaxMetres"/>.</summary>
        public static float Scale(float x, float z)
        {
            var length = (float)Math.Sqrt((x * x) + (z * z));
            return length > MaxMetres ? MaxMetres / length : 1f;
        }
    }

    /// <summary>
    /// Rigs whose walk clip carries the body forward (Blightroot's mutant walk: 1.45 m of hips travel per loop, 1.9x
    /// scale on the road) are held in place by the runner, the way the battle pins its stride: the sim moves the slot,
    /// so the hips' own forward travel is cancelled every frame instead of drifting ahead and snapping back on each
    /// loop wrap and each walk / attack / rest change.
    /// </summary>
    public static class RunnerStrideHold
    {
        /// <summary>
        /// True for a rig the runner holds. Ironhowl is left out: he never walks in the runner (the boss holds at the
        /// arena line), and his slam's lunge is the readable, lane-aimed big attack.
        /// </summary>
        public static bool Applies(LockedBattleFighter fighter) =>
            fighter.WalkCarriesRoot && !string.Equals(fighter.Name, LockedBattleRoster.Ironhowl.Name, StringComparison.Ordinal);

        /// <summary>
        /// Where the body shows along the road (metres, + = the way it faces) when the stride bone has travelled
        /// <paramref name="boneForward"/> from its rest place <paramref name="restForward"/> and the hold is on.
        /// </summary>
        public static float Shown(float restForward, float boneForward, bool hold) =>
            hold ? restForward : boneForward;
    }

    /// <summary>
    /// The runner's squad shooters (Lyra, Rowan, Oakenshield) keep walking while they fire (Derek 2026-10-23: "it
    /// looks weird when the heros are stepping foward and backwards while firing"): their attack takes move the
    /// hips 0.12 to 0.40 m forward and back, and played full-body over a sliding slot they read as steps. The take
    /// plays on the upper body only (spine and everything under it, plus any side branch such as a weapon bone);
    /// the hips and legs stay on the walk loop. Each take crossfades in and out over <see cref="CrossfadeSeconds"/>;
    /// its timing (and so its release frame) is untouched.
    /// </summary>
    public static class RunnerUpperBodyAttack
    {
        public const float CrossfadeSeconds = 0.1f;

        /// <summary>Roster names that play the layered attack in the runner.</summary>
        public static readonly string[] Shooters = { "Lyra", "Rowan", "Oakenshield" };

        public static bool Applies(string rosterName) => Array.IndexOf(Shooters, rosterName) >= 0;

        /// <summary>Smoothstep of a 0..1 progress.</summary>
        public static float Ease(float u)
        {
            if (u <= 0f)
            {
                return 0f;
            }

            if (u >= 1f)
            {
                return 1f;
            }

            return u * u * (3f - (2f * u));
        }

        /// <summary>Weight of a take <paramref name="age"/> seconds after it started.</summary>
        public static float FadeIn(float age) => Ease(age / CrossfadeSeconds);

        /// <summary>Weight of a layer that was at <paramref name="from"/> when it began fading out <paramref name="age"/> seconds ago.</summary>
        public static float FadeOut(float from, float age) => from * (1f - Ease(age / CrossfadeSeconds));

        /// <summary>
        /// True when the transform at <paramref name="path"/> (relative to the animator, '/'-separated) takes the attack.
        /// The hips, their ancestors and every hips branch other than the spine (the legs) stay on the walk; the spine
        /// and everything under it take the attack, as does a side branch beside the hips (a weapon bone).
        /// </summary>
        public static bool IsUpperBody(string path, string hipsPath, string spineName)
        {
            if (path.Length == 0 || hipsPath.Length == 0)
            {
                return false;
            }

            if (string.Equals(path, hipsPath, StringComparison.Ordinal)
                || hipsPath.StartsWith(path + "/", StringComparison.Ordinal))
            {
                return false;
            }

            if (!path.StartsWith(hipsPath + "/", StringComparison.Ordinal))
            {
                return true;
            }

            var below = path.Substring(hipsPath.Length + 1).Split('/');
            return string.Equals(below[0], spineName, StringComparison.Ordinal);
        }
    }
}
