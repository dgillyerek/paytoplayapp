#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    public enum RunnerOutcome
    {
        Running,
        Won,
        Lost
    }

    public enum RunnerEventKind
    {
        FoeSpawned,
        BoxSpawned,
        Fired,
        Hit,
        FoeKilled,
        FoePassed,
        BoxBroken,
        BoxMissed,
        FollowerJoined,
        FollowerLost,
        HeroHit,
        Upgrade,
        BossEngaged,

        /// <summary>A lane trap appeared (<see cref="RunnerEvent.Unit"/> = spikes / log / fire).</summary>
        HazardSpawned,

        /// <summary>
        /// A trap caught a squad member (<see cref="RunnerEvent.Id"/> = the trap, Other = the member, Amount = hero damage or
        /// 0 for a follower). <see cref="RunnerEvent.OnBox"/> is true for a side hit (<see cref="RunnerRules.SideHitMul"/>).
        /// </summary>
        HazardHit,
        Won,
        Lost,

        /// <summary>A squad member starts one attack take (<see cref="RunnerEvent.Amount"/> = playback speed); its <see cref="Fired"/> follows at the take's release.</summary>
        AttackStarted,

        /// <summary>The boss raises his arm: the slam lands in <see cref="RunnerEvent.Amount"/> seconds on the marked road in front of him.</summary>
        BossWindup,

        /// <summary>The slam lands (<see cref="RunnerEvent.Amount"/> = squad members caught, the hero's hit shows as <see cref="HeroHit"/>).</summary>
        BossSlam,

        /// <summary>
        /// The boss rears up for the quake: it lands in <see cref="RunnerEvent.Amount"/> seconds on all the road except the safe band centred on
        /// <see cref="RunnerEvent.X"/> (the scene marks the rest red, shows DODGE! and the safe band).
        /// </summary>
        BossQuakeWindup,

        /// <summary>The quake lands (<see cref="RunnerEvent.Amount"/> = squad members caught, the hero's hit shows as <see cref="HeroHit"/>; X = the safe band's centre).</summary>
        BossQuake,

        /// <summary>A follower took a hit it survives (<see cref="RunnerEvent.Id"/> = the follower, Other = the foe, Amount = damage); one it does not is <see cref="FollowerLost"/>.</summary>
        FollowerHit,

        /// <summary>A breakable barrier appeared (<see cref="RunnerEvent.Unit"/> = palisade / stone_wall / barricade, Amount = its HP).</summary>
        BarrierSpawned,

        /// <summary>A squad shot chipped a barrier (<see cref="RunnerEvent.Id"/> = the barrier, Other = the shooter, Amount = damage).</summary>
        BarrierHit,

        /// <summary>
        /// A barrier came apart (<see cref="RunnerEvent.Unit"/> = its kind). Shot down: Other = the shooter, Amount 0, Big false.
        /// Walked into: Other 0, Amount = squad members it hurt, Big true (their hits follow as HeroHit / FollowerHit / FollowerLost).
        /// The scene's break sound hook keys on this event.
        /// </summary>
        BarrierBroken,

        /// <summary>
        /// The boss bends to pick up a boulder (X/Z = his spot, Amount = seconds to the release): the scene plays his pickup and
        /// throw, the rock appearing in his hands at <see cref="RunnerRules.BossRockGrab"/>. No landing mark (Derek 2026-11-03).
        /// </summary>
        BossRockThrown,

        /// <summary>The boulder lands (X/Z = the spot, Amount = squad members caught; their hits follow as HeroHit / FollowerHit / FollowerLost).</summary>
        BossRockLanded,

        /// <summary>The boulder leaves his hands (X/Z = where it lands, aimed now; Amount = seconds of flight).</summary>
        BossRockReleased,

        /// <summary>
        /// One swing of the boss's slam lands (X/Z = where: the hero when it struck him, else the aimed road; Amount = squad
        /// members it caught; Big = the hero was struck). Every swing of a multi-swing move fires one, on its own contact frame.
        /// </summary>
        BossSwingHit
    }

    /// <summary>What the boss is winding up.</summary>
    public enum RunnerBossMove
    {
        None,
        Slam,
        Quake
    }

    public sealed record RunnerResult(
        string LevelId,
        bool Won,
        int Kills,
        int BoxesBroken,
        int BoxesMissed,
        int FollowersPeak,
        int FollowersLeft,
        float Distance,
        float Seconds,
        int Number = 1);
}
