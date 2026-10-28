#nullable enable
using System;
using Survival.Domain.Enemies;
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
        public RunnerAttackClip(float clipSeconds, float releaseSeconds, string source, bool fixedSpeed = false)
        {
            ClipSeconds = clipSeconds;
            ReleaseSeconds = releaseSeconds;
            Source = source;
            FixedSpeed = fixedSpeed;
        }

        /// <summary>
        /// True when the runner rig cannot change this take's playback rate (Blightroot's named battle clip): it plays as
        /// authored and the hit lands on its contact frame at speed 1.
        /// </summary>
        public bool FixedSpeed { get; }

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
                case "Vespera":
                    return new RunnerAttackClip(VesperaAttack.Spec.ClipSecondsFor, VesperaAttack.ReleaseFrame / Fps, "VesperaAttack.ReleaseFrame f" + VesperaAttack.ReleaseFrame);
                case "Bonequill":
                    return new RunnerAttackClip(BonequillAttack.Spec.ClipSecondsFor, BonequillAttack.ReleaseFrame / Fps, "BonequillAttack.ReleaseFrame f" + BonequillAttack.ReleaseFrame);
                case "Nightfang":
                    return new RunnerAttackClip(NightfangAttack.Spec.ClipSecondsFor, NightfangAttack.ReleaseFrame / Fps, "NightfangAttack.ReleaseFrame f" + NightfangAttack.ReleaseFrame);
                case "Ashwyrm":
                    return new RunnerAttackClip(AshwyrmAttack.Spec.ClipSecondsFor, AshwyrmAttack.ReleaseFrame / Fps, "AshwyrmAttack.ReleaseFrame f" + AshwyrmAttack.ReleaseFrame);
                case "Blightroot":
                    return new RunnerAttackClip(
                        BlightrootBattlePoses.AttackSeconds,
                        LockedBattleHits.BlightrootContactFrame / Fps,
                        "LockedBattleHits.BlightrootContactFrame f" + LockedBattleHits.BlightrootContactFrame,
                        fixedSpeed: true);
                default:
                    return null;
            }
        }

        /// <summary>
        /// An enemy's attack cycle (Derek 2026-10-27: Vespera's bolt left before her hands pushed it out). The fire
        /// interval stays the unit's own <see cref="RunnerUnitDef.Cycle"/> (same damage per second); only the phase moves:
        /// the take starts <see cref="RunnerAttackPlan.Release"/> before the shot, which leaves on the take's release frame.
        /// The take is played at the rate that makes take + the rig's built-in rest beat
        /// (<see cref="BlenderRigAttackSpec.RestGapSeconds"/>) exactly one fire interval, so the self-cycling rig and the sim's
        /// restarts land together and never drift. No clip: speed 1, release 0 (fires on the cycle start, as before).
        /// </summary>
        public static RunnerAttackPlan FoePlan(RunnerAttackClip? clip, float cycle)
        {
            var interval = Math.Max(cycle, 0.05f);
            if (clip == null || clip.Value.ClipSeconds <= 0f)
            {
                return new RunnerAttackPlan(1f, interval, 0f, false);
            }

            var c = clip.Value;
            if (c.FixedSpeed)
            {
                return new RunnerAttackPlan(1f, interval, Math.Min(c.ReleaseSeconds, interval * 0.95f), false);
            }

            var speed = (c.ClipSeconds + BlenderRigAttackSpec.RestGapSeconds) / interval;
            var capped = speed > MaxSpeed + 1e-4f;
            speed = Math.Min(Math.Max(speed, MinFoeSpeed), MaxSpeed);
            return new RunnerAttackPlan(speed, interval, Math.Min(c.ReleaseSeconds / speed, interval * 0.95f), capped);
        }

        public static RunnerAttackPlan FoePlanFor(RunnerUnitDef unit) => FoePlan(ClipFor(unit.Roster), unit.Cycle);

        /// <summary>Slowest an enemy take plays (a very slow fire interval rests longer rather than crawling).</summary>
        public const float MinFoeSpeed = 0.5f;

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
