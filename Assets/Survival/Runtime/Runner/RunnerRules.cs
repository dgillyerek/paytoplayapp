#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
    /// <summary>Tuning shared by every level (metres, seconds).</summary>
    public static class RunnerRules
    {
        /// <summary>Rows spawn this far ahead of the squad, so they are in view before they matter.</summary>
        public const float SpawnAhead = 38f;

        /// <summary>
        /// Enemies walk at the squad from the moment they spawn (no aggro wait); within this distance of the hero they also
        /// drift sideways toward his line, so a lane layout still reads as lanes while it is far off.
        /// </summary>
        public const float SeekRange = 15f;

        /// <summary>
        /// Share of the road's own scroll a foe's aimed shot leads its target by (the squad walks on at the level speed
        /// whatever the player does). Steering is never led: a shot is aimed once, when fired, and flies straight, so a
        /// sidestep dodges it (Derek 2026-10-23: "the enimies should target the hero's ... unlike the heroes who just fire
        /// straight").
        /// </summary>
        public const float FoeShotLead = 1f;

        /// <summary>
        /// Half width of a squad shot: shots fly straight down the road from the shooter's lane position and hit the
        /// first foe or box whose body (radius) they pass through. Steering is the aim.
        /// </summary>
        public const float ShotHalfWidth = 0.25f;

        /// <summary>Shots fly this far past the shooter's range before they despawn.</summary>
        public const float ShotOvershoot = 1.5f;

        /// <summary>Half width of the strip straight in front of a melee unit that its swing reaches (plus the target's radius).</summary>
        public const float MeleeHalfWidth = 0.7f;

        /// <summary>
        /// Most squad members on the road at once, the lead hero included (a phone-friendly cap: the rigs, shots and
        /// formation rows all stay small). A follower or hero box broken with the squad full pays out
        /// <see cref="FullSquadDamage"/> and <see cref="FullSquadHeal"/> instead.
        /// </summary>
        public const int SquadCap = 10;

        /// <summary>Most followers behind the lead hero (<see cref="SquadCap"/> minus the hero).</summary>
        public const int MaxFollowers = SquadCap - 1;

        /// <summary>
        /// A follower's HP (Derek 2026-10-23: "a little more health ... maybe like 25"; it was 1, so any hit killed one).
        /// Small enemies' shots and swings take two hits, the big ones still one; a trap still knocks a follower out and
        /// the boss's slam and quake still kill every follower caught in the marked road. The lead keeps the unit's HP.
        /// </summary>
        public const int FollowerHp = 25;

        /// <summary>
        /// A ranged enemy's shot (Bonequill, Vespera) always kills a follower in exactly 2 hits (Derek 2026-10-29: "make it so
        /// ranged attacks kill secondary units in 2 hits. they can be dodged"): the level-scaled damage clamped to
        /// [ceil(FollowerHp / 2), FollowerHp - 1]. The hero takes the same hit.
        /// </summary>
        public static int RangedFoeHit(int scaled) => Math.Clamp(scaled, (FollowerHp + 1) / 2, FollowerHp - 1);

        /// <summary>
        /// A crowd enemy's hit on a follower: <paramref name="hit"/> is what the hit would be without the crowd (ranged ones
        /// already clamped by <see cref="RangedFoeHit"/>); it is capped at <see cref="FollowerHp"/> and times
        /// <paramref name="cut"/>, at least 1. At cut 0.6 a 1-hit melee kill takes 2 hits and a 2-hit archer kill 2-4.
        /// cut 1 (no crowd) returns <paramref name="hit"/> unchanged.
        /// </summary>
        public static int CrowdFollowerHit(int hit, float cut) =>
            cut >= 1f ? hit : Math.Max(1, (int)Math.Round(Math.Min(hit, FollowerHp) * (double)cut));

        /// <summary>Share of a trap's hero damage a follower takes (spikes 40 -> 16 of its 25 HP).</summary>
        public const float FollowerTrapShare = 0.4f;

        /// <summary>Squad damage bonus for each unit a box would have added with the squad already full.</summary>
        public const float FullSquadDamage = 0.03f;

        /// <summary>Share of the hero's max HP a unit box heals when the squad is already full.</summary>
        public const float FullSquadHeal = 0.2f;

        /// <summary>The boss's slam: seconds from the raised arm (the ground marker appears) to the impact.</summary>
        public const float BossWindup = 0.9f;

        /// <summary>The slam lands on this much road in front of the boss (metres back from his front), across his body plus <see cref="BossSlamExtra"/> each side.</summary>
        public const float BossSlamDepth = 5.0f;
        public const float BossSlamExtra = 0.5f;

        /// <summary>After a slam or quake lands the boss keeps facing the locked danger zone this long (his follow-through), then turns back to the squad.</summary>
        public const float BossAimHoldAfter = 0.8f;

        /// <summary>
        /// The boss's signature attack, the quake (Update 8): seconds from the rear-up (the red road and the DODGE! cue appear)
        /// to the impact. Long enough to read and to steer across the whole road (<see cref="BossQuakeSteerSeconds"/> is the worst case).
        /// </summary>
        public const float BossQuakeWindup = 1.8f;

        /// <summary>Every this many boss actions one is the quake (the rest are slams): slam, QUAKE, slam, slam, QUAKE ... about every 12 s.</summary>
        public const int BossQuakeEvery = 3;

        /// <summary>Which action of each <see cref="BossQuakeEvery"/> is the quake (0 = first): the 2nd, so the first one lands about 5 s into the fight.</summary>
        public const int BossQuakeSlot = 1;

        /// <summary>The quake lands on all of the road this far back from the boss's front (metres); only the safe band is spared.</summary>
        public const float BossQuakeDepth = 9.0f;

        /// <summary>The safe band: centred this far off the middle of the road on the side away from the hero, <see cref="BossQuakeSafeHalf"/> each side of that.</summary>
        public const float BossQuakeSafeCentre = 2.4f;
        public const float BossQuakeSafeHalf = 2.1f;

        /// <summary>The hero's hit from the quake, in boss hits (a follower in the marked area just falls).</summary>
        public const float BossQuakeDamage = 2.5f;

        /// <summary>Extra pause after a quake before the boss's next action, on top of his cycle.</summary>
        public const float BossQuakeRecovery = 1.0f;

        /// <summary>The longest the squad needs to get the lead and every follower into the safe band from anywhere on the road (see the tests).</summary>
        public const float BossQuakeSteerSeconds = 1.2f;

        /// <summary>
        /// Ironhowl's opener (Derek 2026-11-03: he "takes a while to start attack"): from the moment he is on the road until
        /// the squad reaches his arena line he tears up boulders and throws them at the squad. Derek's review (same day): he
        /// must really pick the rock up, throw every 3 s, the rock flies a touch slower, and there is no landing ring: the
        /// squad reads the pickup and the throw, and steers off the lane the rock flies down.
        /// One throw, from <see cref="RunnerEventKind.BossRockThrown"/>: his ground pound (attack4) bends him down, his fists reach
        /// the road at <see cref="BossRockGrab"/> and rip the rock out; he rises with it, and from <see cref="BossRockWindupAt"/>
        /// his overhead smash (attack2) swings it up and forward, letting go at <see cref="BossRockRelease"/>
        /// (<see cref="RunnerEventKind.BossRockReleased"/>, aim locked then); it lands <see cref="BossRockFlight"/> later.
        /// </summary>
        public const float BossRockGrab = Enemies.IronhowlMotion.Attack4ContactFrame / Enemies.IronhowlMotion.FrameRate;

        /// <summary>Seconds into the throw the overhead-smash swing starts (frame 40 of the ground pound: he is most of the way back up).</summary>
        public const float BossRockWindupAt = 40f / Enemies.IronhowlMotion.FrameRate;

        /// <summary>Frame of the overhead smash the rock leaves his hands: arms coming over and forward, just before the smash's contact (28).</summary>
        public const int BossRockReleaseFrame = 24;

        /// <summary>Seconds from the pickup's start to the release.</summary>
        public const float BossRockRelease = BossRockWindupAt + (BossRockReleaseFrame / Enemies.IronhowlMotion.FrameRate);

        /// <summary>Seconds of flight from his hands to the road: 18% longer than the first version's 0.53 s (Derek: "just slightly slower").</summary>
        public const float BossRockFlight = 0.63f;

        /// <summary>Seconds from one pickup to the next (Derek: 3 s). A throw plays out in <see cref="BossRockRelease"/> + flight = 2.8 s.</summary>
        public const float BossRockEvery = 3.0f;

        /// <summary>Seconds after he appears before the first pickup.</summary>
        public const float BossRockFirst = 0.3f;

        /// <summary>Radius (metres) of road a boulder lands on.</summary>
        public const float BossRockRadius = 1.4f;

        /// <summary>A boulder's hit on each squad member it lands on, in boss hits (a quarter of his melee: chip damage that punishes standing still).</summary>
        public const float BossRockDamage = 0.25f;

        /// <summary>Formation spacing between followers.</summary>
        public const float Spacing = 1.15f;

        /// <summary>The hero slides toward the steer point at most this fast (m/s).</summary>
        public const float SteerSpeed = 11f;

        /// <summary>
        /// Steering is a critically damped spring (SmoothDamp) toward the steer point: it eases in and settles without
        /// overshoot. This is its smooth time (s): about how long it takes to close most of a gap.
        /// </summary>
        public const float SteerSmoothSeconds = 0.10f;

        /// <summary>Followers' spring smooth time: the front row a touch behind the hero, each row back a bit more.</summary>
        public const float FollowSmoothSeconds = 0.12f;
        public const float FollowSmoothPerRow = 0.03f;

        /// <summary>Followers may move sideways a little faster than the hero, so they can catch up to their slot.</summary>
        public const float FollowMaxSpeed = 13f;

        /// <summary>Enemies drift sideways toward the hero this fast while they walk in.</summary>
        public const float EnemySeek = 0.55f;

        /// <summary>
        /// On a level without a boss, once the road ahead is clear the squad runs for the finish gate and covers what is
        /// left in at most this long (s) (Derek 2026-10-27: no long empty walk at the end).
        /// </summary>
        public const float GateChargeSeconds = 1.2f;

        /// <summary>
        /// On a level without a boss, the finish moves up to this many metres past where the squad stands when the road
        /// clears (last enemy down, nothing left ahead), so the level ends shortly after the last enemy (Derek 2026-10-30).
        /// </summary>
        public const float FinishTail = 4f;

        /// <summary>
        /// A barrier never falls to fewer than this many of the squad's hardest hits (Derek 2026-10-30: "the destructible
        /// barriers need to take more than one hit to break... like the chests"): at spawn its HP is raised to this times
        /// the strongest hit in the squad then, so trained or starred heroes and weapon crates can't one-shot it.
        /// </summary>
        public const int BarrierMinHits = 6;

        /// <summary>The floor applies through this level; from L31 the barriers keep their tuned HP so the locked free pace past the wall is untouched.</summary>
        public const int BarrierMinHitsThrough = 30;

        /// <summary>Boxes this far behind the hero are left behind (no box).</summary>
        public const float PassBehind = 1.2f;

        /// <summary>
        /// Enemies this far behind the squad's back row fall off the road (no kill). The squad never stops: what it does
        /// not kill it walks through, taking hits on the way, and leaves behind.
        /// </summary>
        public const float DropBehind = 4f;

        /// <summary>
        /// A ranged enemy's body overlapping a squad body strikes it this often while they overlap (a melee enemy's
        /// contact damage is its own swing, every cycle, at whoever is in reach as the squad pushes past).
        /// </summary>
        public const float ContactHitsPerSecond = 1f;

        /// <summary>Share of an enemy's attack damage a contact hit does to a squad member.</summary>
        public const float ContactDamageShare = 0.5f;

        /// <summary>
        /// An enemy the squad walks past keeps fighting (turns, chases, hits the rows going by) for this long after the
        /// hero is past it, then gives up and drops behind.
        /// </summary>
        public const float ChaseSeconds = 1.5f;

        /// <summary>An enemy take started on the approach waits this long past its release frame for the squad to come into reach, then lapses.</summary>
        public const float FoeTakeGraceSeconds = 0.6f;

        /// <summary>
        /// The boss fight is a stand: the squad walks up to an arena line this share of the boss's slam reach in front of
        /// him (so his slam lands on the front row) and halts there to fight, still steering left / right. He holds his
        /// ground. There is no time limit: the run is won when he falls and lost only when the squad is wiped.
        /// </summary>
        public const float BossArenaShare = 0.9f;

        /// <summary>A hit at least this big (or any boss, box break, follower loss or hero hit) shakes the screen.</summary>
        public const int BigHit = 60;

        /// <summary>Spike strip cycle (s): down, then a rattle warning, then up (hurting); staggered by the row's phase.</summary>
        public const float SpikeDown = 1.3f;
        public const float SpikeWarn = 0.7f;
        public const float SpikeUp = 1.0f;
        public const float SpikeCycle = SpikeDown + SpikeWarn + SpikeUp;

        /// <summary>A log starts wobbling (its warning) once the hero is this close, then rolls down its lane at <see cref="LogSpeed"/>.</summary>
        public const float LogTrigger = 17f;
        public const float LogWarn = 1.1f;
        public const float LogSpeed = 2.6f;

        /// <summary>Footprint depth (half, metres along the road) of each trap.</summary>
        public const float SpikeHalfDepth = 0.55f;
        public const float FireHalfDepth = 0.75f;
        public const float LogHalfDepth = 0.4f;

        /// <summary>Share of a member's body radius that has to overlap a trap for it to count (forgiving edges).</summary>
        public const float HazardBodyShare = 0.5f;

        /// <summary>Most squad members one activation catches (spikes snap shut on the first, a log flattens two); the rest get through.</summary>
        public const int SpikeCatch = 1;
        public const int LogCatch = 2;

        /// <summary>A fire patch burns one member at a time, re-flaring every this many seconds.</summary>
        public const float FireFlare = 1.2f;

        /// <summary>Half a breakable barrier's depth along the road (a gate, a wall, a stake barricade are all thin).</summary>
        public const float BarrierHalfDepth = 0.35f;

        /// <summary>
        /// A trap or barrier a squad member runs into from the side (it was already level with it along the road and
        /// steered sideways into it) hits this many times harder than one walked into head-on (Derek 2026-10-27: "the
        /// obstacles should do more damage when heroes side run into them"). A side hit on a follower is doubled too, but still
        /// capped one short of a fresh follower's HP (see <see cref="RunnerRun.FollowerTrapDamage(int, bool)"/>).
        /// </summary>
        public const float SideHitMul = 2f;

        /// <summary>
        /// Enemies steer round traps and standing barriers instead of walking through them (Derek 2026-10-27: "the enemies
        /// should avoid the obstacles and not run over them"). They look this far ahead (metres from their front)...
        /// </summary>
        public const float EnemyAvoidLook = 5f;

        /// <summary>...side-step toward the nearer open side this fast (m/s), keeping this much clearance past the edge...</summary>
        public const float EnemyAvoidSpeed = 3f;
        public const float EnemyAvoidMargin = 0.15f;

        /// <summary>Longest internal step; bigger frame deltas are split so results do not depend on frame rate.</summary>
        public const float MaxStep = 1f / 30f;
    }
}
