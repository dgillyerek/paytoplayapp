#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    /// <summary>The hero (<see cref="IsHero"/>) or a follower.</summary>
    public sealed class RunnerMember
    {
        internal RunnerMember(int id, RunnerUnitDef unit, bool hero, float x, float z, RunnerUnitBoost? boost = null)
        {
            Id = id;
            Unit = unit;
            IsHero = hero;
            X = x;
            Z = z;
            PrevX = x;
            PrevZ = z;
            var b = boost ?? RunnerUnitBoost.None;
            Hp = MaxHp = Math.Max(1, (int)Math.Round((hero ? unit.Hp : RunnerRules.FollowerHp) * b.HpMul));
            Power = b.DamageMul;
            Plan = RunnerAttackTiming.PlanFor(unit);
        }

        /// <summary>The lead's unit HP, or <see cref="RunnerRules.FollowerHp"/> for a follower, times the hero's trained HP boost.</summary>
        public int MaxHp { get; }

        /// <summary>Damage multiplier from the hero's trained level and stars (1 untrained); see <see cref="RunnerLoadout"/>.</summary>
        public float Power { get; }
        public float HpFraction => MaxHp > 0 ? Math.Max(0f, Hp / (float)MaxHp) : 0f;

        public int Id { get; }
        public RunnerUnitDef Unit { get; }
        public bool IsHero { get; }

        /// <summary>The unit's attack take timing before upgrades (speed, interval, release).</summary>
        public RunnerAttackPlan Plan { get; }
        public float X { get; internal set; }
        public float Z { get; internal set; }

        /// <summary>Sideways velocity (m/s, + = right) from the steering spring; the scene leans the body with it.</summary>
        public float VX { get; internal set; }

        /// <summary>Where it stood at the start of this step (tells a side hit from a head-on one).</summary>
        internal float PrevX { get; set; }
        internal float PrevZ { get; set; }
        public int Hp { get; internal set; }
        public int Slot { get; internal set; } = -1;
        public float Cooldown { get; internal set; }

        /// <summary>Id of the foe or box this member last attacked (0 = none), and how long ago.</summary>
        public int TargetId { get; internal set; }
        public float SinceAttack { get; internal set; } = 99f;

        /// <summary>True from the start of an attack take to its end (the scene plays the take, then walks again).</summary>
        public bool Attacking => CycleClock >= 0f;

        /// <summary>Seconds into the current attack take, or -1 between takes.</summary>
        public float CycleClock { get; internal set; } = -1f;

        /// <summary>The current take's timing (the plan with the attack-speed upgrade in force when it started).</summary>
        public RunnerAttackPlan Cycle { get; internal set; }

        /// <summary>Whether the current take has already let its shot (or swing) go.</summary>
        public bool Released { get; internal set; }

        /// <summary>Attack takes started and shots / swings released so far.</summary>
        public int CyclesStarted { get; internal set; }
        public int Releases { get; internal set; }
    }
}
