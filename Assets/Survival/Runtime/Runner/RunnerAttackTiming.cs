#nullable enable
using System;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    /// <summary>
    /// A squad unit's battle attack take: its length at normal speed, and when in it the shot leaves (bow / crossbow
    /// / cannon release) or the blade lands. Read from the locked battle data, so the runner fires on the frame the
    /// battle does.
    /// </summary>
    public readonly struct RunnerAttackClip
    {
        public RunnerAttackClip(float clipSeconds, float releaseSeconds, string source)
        {
            ClipSeconds = clipSeconds;
            ReleaseSeconds = releaseSeconds;
            Source = source;
        }

        public float ClipSeconds { get; }
        public float ReleaseSeconds { get; }
        public string Source { get; }

        /// <summary>Release point as a fraction of the take (0 = first frame, 1 = last).</summary>
        public float ReleaseNormalized => ClipSeconds > 0f ? ReleaseSeconds / ClipSeconds : 0f;
    }

    /// <summary>
    /// One attack cycle as the runner plays it: the take sped up by <see cref="Speed"/> so it lasts exactly
    /// <see cref="Interval"/> (one shot or swing per take, no rest gap), the shot or hit at <see cref="Release"/>.
    /// </summary>
    public readonly struct RunnerAttackPlan
    {
        public RunnerAttackPlan(float speed, float interval, float release, bool capped)
        {
            Speed = speed;
            Interval = interval;
            Release = release;
            Capped = capped;
        }

        /// <summary>Playback rate of the attack take (1 = as authored).</summary>
        public float Speed { get; }

        /// <summary>Seconds from one attack to the next: the sped-up take's length.</summary>
        public float Interval { get; }

        /// <summary>Seconds into the cycle at which the shot spawns (or the swing lands).</summary>
        public float Release { get; }

        /// <summary>True when the unit's asked-for interval would have needed more than <see cref="RunnerAttackTiming.MaxSpeed"/>.</summary>
        public bool Capped { get; }

        /// <summary>The same plan with the attack-speed upgrade applied: shorter cycle, faster take, same release frame.</summary>
        public RunnerAttackPlan Faster(float rateMul)
        {
            var m = rateMul > 0.01f ? rateMul : 1f;
            return new RunnerAttackPlan(Speed * m, Interval / m, Release / m, Capped);
        }
    }

    public static class RunnerAttackTiming
    {
        /// <summary>Fastest the runner plays a battle take before it reads as a twitch (Derek 2026-10-21: cap ~2.5x).</summary>
        public const float MaxSpeed = 2.5f;

        private const float Fps = BlenderRigAttackSpec.FrameRate;

        /// <summary>The battle attack take of a roster name, or null when the runner has none (the unit then fires on its cycle start).</summary>
        public static RunnerAttackClip? ClipFor(string roster)
        {
            switch (roster)
            {
                case "Lyra":
                    return new RunnerAttackClip(LyraAttack.Spec.ClipSecondsFor, LyraAttack.ReleaseFrame / Fps, "LyraAttack.ReleaseFrame f" + LyraAttack.ReleaseFrame);
                case "Rowan":
                    return new RunnerAttackClip(RowanAttack.Spec.ClipSecondsFor, RowanAttack.ReleaseFrame / Fps, "RowanAttack.ReleaseFrame f" + RowanAttack.ReleaseFrame);
                case "Oakenshield":
                    return new RunnerAttackClip(OakenshieldAttack.Spec.ClipSecondsFor, OakenshieldAttack.ReleaseFrame / Fps, "OakenshieldAttack.ReleaseFrame f" + OakenshieldAttack.ReleaseFrame);
                case "Sir Aldric":
                    return new RunnerAttackClip(
                        SirAldricBattlePoses.AttackSeconds,
                        LockedBattleHits.SirAldricContactFrame / Fps,
                        "LockedBattleHits.SirAldricContactFrame f" + LockedBattleHits.SirAldricContactFrame);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Speed the take up so one take == the asked-for <paramref name="cycle"/>; past <see cref="MaxSpeed"/> the speed is
        /// capped and the interval stretches to the capped take's length.
        /// </summary>
        public static RunnerAttackPlan Plan(RunnerAttackClip? clip, float cycle)
        {
            if (clip == null || clip.Value.ClipSeconds <= 0f || cycle <= 0f)
            {
                return new RunnerAttackPlan(1f, Math.Max(cycle, 0.05f), 0f, false);
            }

            var c = clip.Value;
            var speed = c.ClipSeconds / cycle;
            var capped = speed > MaxSpeed + 1e-4f;
            if (capped)
            {
                speed = MaxSpeed;
            }

            return new RunnerAttackPlan(speed, c.ClipSeconds / speed, c.ReleaseSeconds / speed, capped);
        }

        public static RunnerAttackPlan PlanFor(RunnerUnitDef unit) => Plan(ClipFor(unit.Roster), unit.Cycle);
    }
}
