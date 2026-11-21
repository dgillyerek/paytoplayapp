#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    public sealed class RunnerFoe
    {
        internal RunnerFoe(int id, RunnerUnitDef unit, float x, float z, bool boss, float hpMul = 1f, float damageMul = 1f, float crowdCut = 1f)
        {
            Id = id;
            Unit = unit;
            X = x;
            LaneX = x;
            Z = z;
            Boss = boss;
            Hp = MaxHp = Math.Max(1, (int)Math.Round(unit.Hp * (hpMul > 0f ? hpMul : 1f)));
            Damage = (int)Math.Round(unit.Damage * (damageMul > 0f ? damageMul : 1f));
            if (unit.Ranged && !boss)
            {
                Damage = RunnerRules.RangedFoeHit(Damage);
            }

            FollowerDamage = Damage;
            if (crowdCut < 1f && !boss)
            {
                var plain = (int)Math.Round(unit.Damage * (damageMul > 0f ? damageMul : 1f) / crowdCut);
                FollowerDamage = RunnerRules.CrowdFollowerHit(unit.Ranged ? RunnerRules.RangedFoeHit(plain) : plain, crowdCut);
            }
            // Walking from the moment it spawns (even the rows already in view when the run starts); the boss holds.
            Walking = !boss;
            Plan = RunnerAttackTiming.FoePlanFor(unit);
        }

        /// <summary>Its hit on a follower: <see cref="Damage"/>, or on a crowd level <see cref="RunnerRules.CrowdFollowerHit"/>.</summary>
        public int FollowerDamage { get; }

        /// <summary>Its attack take: the shot or hit leaves <see cref="RunnerAttackPlan.Release"/> into it, one take per fire interval.</summary>
        public RunnerAttackPlan Plan { get; }

        /// <summary>Mid attack take: started (AttackStarted), shot or hit not released yet (Fired at the release frame).</summary>
        public bool InTake { get; internal set; }

        /// <summary>Seconds into the current take.</summary>
        public float TakeClock { get; internal set; }

        /// <summary>The current take's playback speed and its release time (normally <see cref="Plan"/>'s; see AttackStarted).</summary>
        public float TakeSpeed { get; internal set; } = 1f;

        /// <summary>Seconds since its last take started (a take's own clip time is this times <see cref="TakeSpeed"/>).</summary>
        public float TakeAge { get; internal set; } = 99f;
        public float TakeRelease { get; internal set; }

        /// <summary>The barrier the current take batters (0 = the take is at the squad).</summary>
        internal int TakeWall { get; set; }

        /// <summary>Last tick's distance short of attack reach (&lt;= 0 in reach), to see a take's release coming into reach.</summary>
        internal float LastSlack { get; set; } = float.NaN;

        public int Id { get; }
        public RunnerUnitDef Unit { get; }
        public float X { get; internal set; }
        public float Z { get; internal set; }
        public bool Boss { get; }
        public int Hp { get; internal set; }
        public int MaxHp { get; }

        /// <summary>One hit: the unit's damage times the level's <see cref="RunnerLevelDef.FoeDmgMul"/>.</summary>
        public int Damage { get; }
        public float Cooldown { get; internal set; }
        public bool Walking { get; internal set; }

        /// <summary>Side-stepping round a trap or barrier in its path, toward <see cref="AvoidX"/>.</summary>
        public bool Avoiding { get; internal set; }
        public float AvoidX { get; internal set; }

        /// <summary>The line it would walk with nothing in the way (its spawn lane, drifting toward its target); it steps back onto it once past an obstacle.</summary>
        internal float LaneX { get; set; }

        /// <summary>How fast (m/s) it drifts sideways toward the hero once close (RunnerRowDef.Seek; 0 holds its lane).</summary>
        public float Seek { get; internal set; } = RunnerRules.EnemySeek;

        /// <summary>Walking in from beyond the road edge (a side entry) toward <see cref="LaneX"/>, at <see cref="EnterSpeed"/> m/s sideways.</summary>
        public bool Entering { get; internal set; }

        /// <inheritdoc cref="Entering"/>
        public float EnterSpeed { get; internal set; }

        /// <summary>Held up behind a trap or barrier it cannot get round yet (or one that spans the road).</summary>
        public bool Blocked { get; internal set; }

        /// <summary>The barrier it is held up behind (0 for none or a trap): with no squad member in reach it batters this.</summary>
        public int BlockedBy { get; internal set; }

        /// <summary>Body to body with a squad member (or, for the boss, in slam range of the front row).</summary>
        public bool Engaged { get; internal set; }

        /// <summary>The squad has walked past it (its front is behind the hero); it chases for <see cref="RunnerRules.ChaseSeconds"/>.</summary>
        public bool Passed { get; internal set; }
        public float ChaseLeft { get; internal set; } = RunnerRules.ChaseSeconds;

        /// <summary>Member it last attacked (0 = none).</summary>
        public int TargetId { get; internal set; }
        public float ContactClock { get; internal set; }

        /// <summary>Seconds until the boss's raised slam lands (0 = not winding up).</summary>
        public float Windup { get; internal set; }

        /// <summary>What the boss is winding up (<see cref="RunnerBossMove.None"/> when he is not).</summary>
        public RunnerBossMove Move { get; internal set; }

        /// <summary>Seconds the current windup was given (the scene's fill is <c>1 - Windup / WindupTotal</c>).</summary>
        public float WindupTotal { get; internal set; }

        /// <summary>Boss actions started so far (slams and quakes): every <see cref="RunnerRules.BossQuakeEvery"/>th is a quake.</summary>
        public int Actions { get; internal set; }

        /// <summary>Quakes started so far.</summary>
        public int Quakes { get; internal set; }

        /// <summary>Slams started so far (picks each slam's move from his rotation).</summary>
        public int Slams { get; internal set; }

        /// <summary>Rotation index of the current or last slam's move (<see cref="LockedBattleRotation.AttackAt"/>); the scene plays that clip.</summary>
        public int SlamSwing { get; internal set; } = -1;

        /// <summary>Next swing of the current slam still to land (0 = none pending; the first lands at the windup's end).</summary>
        internal int SlamNextHit { get; set; }

        /// <summary>Seconds until <see cref="SlamNextHit"/> lands.</summary>
        internal float SlamHitClock { get; set; }

        /// <summary>True between a multi-swing slam's first swing and its last (more claws still coming).</summary>
        public bool SlamSwinging => SlamNextHit > 0;

        /// <summary>Seconds until the boulder he lifted lands (0 = none in his hands or in the air).</summary>
        public float RockLeft { get; internal set; }

        /// <summary>Where the current boulder lands, locked at the release (<see cref="RunnerEventKind.BossRockReleased"/>).</summary>
        public float RockX { get; internal set; }

        /// <inheritdoc cref="RockX"/>
        public float RockZ { get; internal set; }

        /// <summary>Seconds until the next lift.</summary>
        public float RockClock { get; internal set; } = RunnerRules.BossRockFirst;

        /// <summary>True while the current boulder is still in his hands (picked up, not yet thrown).</summary>
        public bool RockHeld => RockLeft > RunnerRules.BossRockFlight;

        /// <summary>Boulders thrown so far.</summary>
        public int Rocks { get; internal set; }

        /// <summary>Centre (x) of the current or last quake's safe band.</summary>
        public float QuakeSafeX { get; internal set; }

        /// <summary>
        /// The ground point the boss's telegraphed attack is aimed at, locked when the warning starts (Derek 2026-10-27: he
        /// kept turning to follow the hero, so he seemed to swing at a hero who had already dodged). It sits inside the marked
        /// danger zone and does not move while he winds up or strikes; the scene faces him at it, not at the hero.
        /// </summary>
        public float AimX { get; internal set; }

        /// <inheritdoc cref="AimX"/>
        public float AimZ { get; internal set; }

        /// <summary>Seconds he keeps facing <see cref="AimX"/>/<see cref="AimZ"/>: the whole windup plus <see cref="RunnerRules.BossAimHoldAfter"/> after the impact.</summary>
        public float AimHold { get; internal set; }

        /// <summary>True while a telegraphed attack's aim is locked (windup, strike and its follow-through).</summary>
        public bool AimLocked => Boss && (Windup > 0f || AimHold > 0f);

        public float SinceAttack { get; internal set; } = 99f;
        public bool Attacking => InTake || SinceAttack < Unit.Cycle + 0.25f;
        public float HpFraction => MaxHp > 0 ? Math.Max(0f, Hp / (float)MaxHp) : 0f;
    }
}
