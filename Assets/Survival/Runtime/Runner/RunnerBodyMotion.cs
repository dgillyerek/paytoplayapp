#nullable enable
using System;
using System.Numerics;
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
    }

    /// <summary>
    /// Where a runner rig's host sits in its slot (Derek 2026-10-23: "blightroot when shot still bounces way back").
    /// The body turns (hit lean, death tip) about its pivot: the rest place of the stride bone (Blightroot's hips) or,
    /// for a rig without one, its mid height. The pivot stays where the sim puts the slot, give or take the hit
    /// nudge and the death sink, and a held rig's clip travel is cancelled whatever the host's turn (only its bob is
    /// kept, straight up). (The old hold was subtracted along the road while the host was tipped
    /// 80 degrees in its death fall: the walk's forward travel had turned upward, so the hold pushed the body up to
    /// 2.3 m back, sawing on every loop. The old lean also pivoted at the feet, swinging the hips back.)
    /// All vectors are in the slot's frame except <paramref name="pivotRest"/> and <paramref name="bone"/>, which are
    /// host-local (before the host's scale).
    /// </summary>
    public static class RunnerRootPlacement
    {
        public static Vector3 HostPosition(
            Vector3 baseLocal,
            Vector3 nudge,
            float sink,
            Quaternion rotation,
            Vector3 scale,
            Vector3 pivotRest,
            Vector3 bone,
            bool hold)
        {
            if (!hold)
            {
                bone = pivotRest;
            }

            // The held bone shows at its rest place; only its own rise and fall (the walk's bob) is kept, straight up.
            var bob = (bone.Y - pivotRest.Y) * scale.Y;
            return baseLocal + nudge + new Vector3(0f, bob - sink, 0f) + (pivotRest * scale) - Vector3.Transform(bone * scale, rotation);
        }

        /// <summary>Where host-local point <paramref name="point"/> shows in the slot.</summary>
        public static Vector3 Rendered(Vector3 hostPosition, Quaternion rotation, Vector3 scale, Vector3 point) =>
            hostPosition + Vector3.Transform(point * scale, rotation);
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
