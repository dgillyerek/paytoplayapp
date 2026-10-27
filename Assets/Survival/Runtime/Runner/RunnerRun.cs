#nullable enable
using System;
using System.Collections.Generic;

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
        BarrierBroken
    }

    /// <summary>What the boss is winding up.</summary>
    public enum RunnerBossMove
    {
        None,
        Slam,
        Quake
    }

    /// <summary>
    /// What happened in one step, for the scene to show. <see cref="Id"/> is the foe / box / member it is about;
    /// <see cref="Other"/> the attacker or target; <see cref="Amount"/> damage, HP left, count or bonus; <see cref="Big"/> shakes the screen.
    /// </summary>
    public readonly struct RunnerEvent
    {
        public RunnerEvent(RunnerEventKind kind, int id, int other, float x, float z, float amount, bool big, bool onBox = false, string unit = "")
        {
            Kind = kind;
            Id = id;
            Other = other;
            X = x;
            Z = z;
            Amount = amount;
            Big = big;
            OnBox = onBox;
            Unit = unit;
        }

        public RunnerEventKind Kind { get; }
        public int Id { get; }
        public int Other { get; }
        public float X { get; }
        public float Z { get; }
        public float Amount { get; }
        public bool Big { get; }
        public bool OnBox { get; }
        public string Unit { get; }

        public override string ToString() => Kind + " #" + Id + (Other != 0 ? " by/at #" + Other : "") + " " + Amount;
    }

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

    public sealed class RunnerFoe
    {
        internal RunnerFoe(int id, RunnerUnitDef unit, float x, float z, bool boss, float hpMul = 1f, float damageMul = 1f)
        {
            Id = id;
            Unit = unit;
            X = x;
            LaneX = x;
            Z = z;
            Boss = boss;
            Hp = MaxHp = Math.Max(1, (int)Math.Round(unit.Hp * (hpMul > 0f ? hpMul : 1f)));
            Damage = (int)Math.Round(unit.Damage * (damageMul > 0f ? damageMul : 1f));
            // Walking from the moment it spawns (even the rows already in view when the run starts); the boss holds.
            Walking = !boss;
        }

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

        /// <summary>Centre (x) of the current or last quake's safe band.</summary>
        public float QuakeSafeX { get; internal set; }

        public float SinceAttack { get; internal set; } = 99f;
        public bool Attacking => SinceAttack < Unit.Cycle + 0.25f;
        public float HpFraction => MaxHp > 0 ? Math.Max(0f, Hp / (float)MaxHp) : 0f;
    }

    public sealed class RunnerBox
    {
        internal RunnerBox(int id, RunnerBoxDef def, float x, float z)
        {
            Id = id;
            Def = def;
            X = x;
            Z = z;
            Hp = def.Hp;
        }

        public const float Radius = 0.8f;

        public int Id { get; }
        public RunnerBoxDef Def { get; }
        public float X { get; }
        public float Z { get; }
        public int Hp { get; internal set; }
        public int MaxHp => Def.Hp;
        public float HpFraction => MaxHp > 0 ? Math.Max(0f, Hp / (float)MaxHp) : 0f;
    }

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

    /// <summary>
    /// A breakable barrier in play: the squad's shots chip it down (it stops them, like a box), and walking into it while it
    /// stands hurts whoever it touches and smashes it. It never moves; enemies walk round it (or wait behind one that spans
    /// the road), and their shots fly over it.
    /// </summary>
    public sealed class RunnerBarrier
    {
        internal RunnerBarrier(int id, RunnerBarrierDef def, float x, float z)
        {
            Id = id;
            Def = def;
            X = x;
            Z = z;
            Hp = def.Hp;
        }

        public int Id { get; }
        public RunnerBarrierDef Def { get; }
        public RunnerBarrierKind Kind => Def.Kind;
        public float X { get; }
        public float Z { get; }
        public int Hp { get; internal set; }
        public int MaxHp => Def.Hp;
        public float HpFraction => MaxHp > 0 ? Math.Max(0f, Hp / (float)MaxHp) : 0f;
        public float HalfWidth => Def.Width * 0.5f;
        public float HalfDepth => RunnerRules.BarrierHalfDepth;

        public bool Touches(float x, float z, float radius) =>
            Math.Abs(x - X) <= HalfWidth + (radius * RunnerRules.HazardBodyShare)
            && Math.Abs(z - Z) <= HalfDepth + (radius * RunnerRules.HazardBodyShare);

        /// <summary>True when a member now touching it came in from the side (see <see cref="RunnerObstacle.SideEntry"/>).</summary>
        public bool SideEntry(float prevX, float prevZ, float radius) =>
            RunnerObstacle.SideEntry(X, Z, HalfWidth, HalfDepth, prevX, prevZ, radius * RunnerRules.HazardBodyShare);
    }

    /// <summary>
    /// A shot in flight, straight down the road (squad shots +z, foe shots -z) from where it was fired; no homing.
    /// It hits the first body it passes through and despawns after <see cref="Range"/> metres.
    /// </summary>
    public sealed class RunnerShot
    {
        internal RunnerShot(int id, int shooter, bool byFoe, float x, float z, int damage, float speed, float range, float direction = 0f, float aimX = 0f)
        {
            Direction = direction != 0f ? Math.Sign(direction) : byFoe ? -1f : 1f;
            // Heading: straight along the road (squad shots, aimX 0), or a foe's aimed line (aimX, Direction) normalised.
            var len = (float)Math.Sqrt((aimX * aimX) + 1f);
            DX = aimX / len;
            DZ = Direction / len;
            Id = id;
            Shooter = shooter;
            ByFoe = byFoe;
            X = x;
            Z = z;
            Damage = damage;
            Speed = speed;
            Range = range;
        }

        public int Id { get; }
        public int Shooter { get; }
        public bool ByFoe { get; }

        /// <summary>+1 down the road (squad shots), -1 back up it (foe shots at a squad still ahead of them); a foe
        /// the squad has walked past shoots after it (+1).</summary>
        public float Direction { get; }

        /// <summary>Unit heading on the road (x across, z along). Squad shots: (0, 1), straight ahead. Foe shots: at
        /// the target as it stood when the shot was fired (plus the road's scroll), diagonals across lanes included.</summary>
        public float DX { get; }
        public float DZ { get; }
        public float Range { get; }
        public float Travelled { get; internal set; }
        public float X { get; internal set; }
        public float Z { get; internal set; }
        public int Damage { get; }
        public float Speed { get; }
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

    /// <summary>
    /// One run of a runner level ("runner army"): the squad auto-walks down the lane at the level's pace and every unit
    /// auto-attacks straight ahead (ranged shots fly down its own lane, melee swings hit what stands in front of it);
    /// the player only steers left / right, which is also the aim. The squad never stops: enemy waves walk in, and what
    /// it does not kill it walks through while they keep swinging (melee in reach, ahead or already passed, ranged foes
    /// shooting either way down the road, bodies touching a caster hurting) and chase it briefly, then they drop behind.
    /// An enemy hit lands on a follower (<see cref="RunnerRules.FollowerHp"/>) first, the hero only once no followers are left. Only the boss stops the
    /// squad: it halts at his arena line and fights (still steering) until he falls or the squad is wiped. Bonus boxes have HP and must be shot down to be claimed: followers, a new hero, or
    /// weapon / attack-speed upgrades. Reaching the end (or killing the boss, when the level has one) wins; losing the
    /// hero loses. Plain C#, deterministic, stepped by <see cref="Step"/>.
    /// </summary>
    public sealed class RunnerRun
    {
        private readonly RunnerCatalog _catalog;
        private readonly List<RunnerMember> _squad = new();
        private readonly List<RunnerFoe> _foes = new();
        private readonly List<RunnerBox> _boxes = new();
        private readonly List<RunnerShot> _shots = new();
        private readonly List<RunnerHazard> _hazards = new();
        private readonly List<RunnerBarrier> _barriers = new();
        private readonly List<RunnerEvent> _events = new();
        private int _nextRow;
        private int _nextId = 1;
        private float _steer;

        public RunnerRun(RunnerCatalog catalog, RunnerLevelDef level)
            : this(catalog, level, RunnerLoadout.None)
        {
        }

        /// <summary>A run whose squad carries the player's trained heroes (<paramref name="loadout"/>: damage / HP per runner unit).</summary>
        public RunnerRun(RunnerCatalog catalog, RunnerLevelDef level, RunnerLoadout loadout)
        {
            _catalog = catalog;
            Level = level;
            Loadout = loadout ?? RunnerLoadout.None;
            Hero = new RunnerMember(_nextId++, catalog.Unit(level.Hero), true, 0f, 0f, Loadout.For(level.Hero));
            ArenaLine = level.Length;
            foreach (var r in level.Rows)
            {
                if (r.Kind == RunnerRowKind.Boss)
                {
                    var boss = catalog.Unit(r.Unit);
                    ArenaLine = r.At - (RunnerRules.BossArenaShare * (boss.Range + boss.Radius + Hero.Unit.Radius));
                }
            }

            _squad.Add(Hero);
            SpawnRows();
        }

        public RunnerLevelDef Level { get; }
        public RunnerLoadout Loadout { get; }
        public RunnerMember Hero { get; }

        /// <summary>Hero first, then followers in formation order.</summary>
        public IReadOnlyList<RunnerMember> Squad => _squad;
        public IReadOnlyList<RunnerFoe> Foes => _foes;
        public IReadOnlyList<RunnerBox> Boxes => _boxes;
        public IReadOnlyList<RunnerShot> Shots => _shots;
        public IReadOnlyList<RunnerHazard> Hazards => _hazards;
        public IReadOnlyList<RunnerBarrier> Barriers => _barriers;

        public int Followers => _squad.Count - 1;
        public int FollowersPeak { get; private set; }

        /// <summary>Metres walked; the hero stands at Z = Distance.</summary>
        public float Distance { get; private set; }
        public float Seconds { get; private set; }
        public float Progress => Math.Min(1f, Distance / Level.Length);
        public float SteerTarget => _steer;

        /// <summary>Squad-wide damage and attack speed multipliers from upgrade boxes.</summary>
        public float DamageMul { get; private set; } = 1f;
        public float RateMul { get; private set; } = 1f;

        /// <summary>Squad-wide range multiplier for ranged shots (range crates).</summary>
        public float RangeMul { get; private set; } = 1f;

        /// <summary>Followers knocked out and hero hits taken from lane traps.</summary>
        public int HazardHits { get; private set; }

        /// <summary>Barriers shot down, barriers the squad walked into (and the squad members those hurt).</summary>
        public int BarriersBroken { get; private set; }
        public int BarriersSmashed { get; private set; }
        public int BarrierHits { get; private set; }

        /// <summary>Trap and barrier hits taken from the side (<see cref="RunnerRules.SideHitMul"/> damage).</summary>
        public int SideHits { get; private set; }

        /// <summary>How far a member's attack reaches now: ranged shots scale with <see cref="RangeMul"/>, melee does not.</summary>
        public float ReachOf(RunnerMember m) => m.Unit.Ranged ? m.Unit.Range * RangeMul : m.Unit.Range;

        public int Kills { get; private set; }
        public int BoxesBroken { get; private set; }
        public int BoxesMissed { get; private set; }

        /// <summary>True once the squad has reached the boss.</summary>
        public bool BossFight { get; private set; }

        /// <summary>True while the squad stands at the boss's arena line (only the boss ever stops it).</summary>
        public bool Halted { get; private set; }

        /// <summary>
        /// Where the squad halts for the boss: <see cref="RunnerRules.BossArenaShare"/> of the boss's slam reach in front of
        /// his row (the level's length without a boss).
        /// </summary>
        public float ArenaLine { get; }

        /// <summary>How far the squad can walk: the arena line on a boss level, else the level's length.</summary>
        public float RoadEnd => Math.Min(Level.Length, ArenaLine);

        /// <summary>The level's enemy HP multiplier (progressive levels toughen the same units).</summary>
        public float FoeHpMul => Level.FoeHpMul;
        public RunnerOutcome Outcome { get; private set; }
        public bool Over => Outcome != RunnerOutcome.Running;

        public RunnerResult Result =>
            new RunnerResult(Level.Id, Outcome == RunnerOutcome.Won, Kills, BoxesBroken, BoxesMissed, FollowersPeak, Followers, Distance, Seconds, Level.Number);

        public float MaxSteer => Math.Max(0.5f, Level.HalfWidth - Hero.Unit.Radius);

        /// <summary>Steer the hero to lane position <paramref name="x"/> (metres from the centre line, clamped to the lane).</summary>
        public void SteerTo(float x) => _steer = Clamp(x, -MaxSteer, MaxSteer);

        public void SteerBy(float dx) => SteerTo(_steer + dx);

        // Plain loops, not List.Find(lambda): the scene looks these up for every health bar every frame, and a capturing
        // lambda allocates each call (garbage the collector then stalls a frame to sweep).
        public RunnerMember? Member(int id)
        {
            for (var i = 0; i < _squad.Count; i++)
            {
                if (_squad[i].Id == id)
                {
                    return _squad[i];
                }
            }

            return null;
        }

        public RunnerFoe? Foe(int id)
        {
            for (var i = 0; i < _foes.Count; i++)
            {
                if (_foes[i].Id == id)
                {
                    return _foes[i];
                }
            }

            return null;
        }

        public RunnerBox? Box(int id)
        {
            for (var i = 0; i < _boxes.Count; i++)
            {
                if (_boxes[i].Id == id)
                {
                    return _boxes[i];
                }
            }

            return null;
        }

        public RunnerBarrier? Barrier(int id)
        {
            for (var i = 0; i < _barriers.Count; i++)
            {
                if (_barriers[i].Id == id)
                {
                    return _barriers[i];
                }
            }

            return null;
        }

        public RunnerHazard? Hazard(int id)
        {
            for (var i = 0; i < _hazards.Count; i++)
            {
                if (_hazards[i].Id == id)
                {
                    return _hazards[i];
                }
            }

            return null;
        }

        /// <summary>Advance by <paramref name="dt"/> seconds; returns what happened (valid until the next call).</summary>
        public IReadOnlyList<RunnerEvent> Step(float dt)
        {
            _events.Clear();
            while (dt > 1e-6f && !Over)
            {
                var h = Math.Min(dt, RunnerRules.MaxStep);
                Tick(h);
                dt -= h;
            }

            return _events;
        }

        private void Tick(float dt)
        {
            Seconds += dt;
            MoveSquad(dt);
            SpawnRows();
            MoveFoes(dt);
            SquadAttacks(dt);
            FoesAttack(dt);
            MoveShots(dt);
            StepHazards(dt);
            StepBarriers();
            DropPassed();
            CheckEnd();
        }

        // ---------- movement ----------

        private void MoveSquad(float dt)
        {
            // Never held up by the waves: the squad walks through whatever it has not killed (and pays for it in hits).
            // Only the boss stops it, at the arena line in front of him.
            foreach (var p in _squad)
            {
                p.PrevX = p.X;
                p.PrevZ = p.Z;
            }

            Distance = Math.Min(RoadEnd, Distance + (Level.Speed * dt));
            Halted = Level.HasBoss && Distance >= ArenaLine - 1e-4f;

            var vx = Hero.VX;
            Hero.X = RunnerSteering.SmoothDamp(Hero.X, _steer, ref vx, RunnerRules.SteerSmoothSeconds, RunnerRules.SteerSpeed, dt);
            Hero.VX = vx;
            Hero.Z = Distance;
            for (var i = 1; i < _squad.Count; i++)
            {
                var m = _squad[i];
                var (sx, sz) = RunnerFormation.Offset(m.Slot);
                var row = RunnerFormation.RowOf(m.Slot);
                // Each follower springs toward its slot; back rows ease a bit slower, so the formation bends as the
                // hero steers and settles without snapping.
                var tx = Clamp(Hero.X + sx, -Level.HalfWidth + m.Unit.Radius, Level.HalfWidth - m.Unit.Radius);
                var fvx = m.VX;
                m.X = RunnerSteering.SmoothDamp(m.X, tx, ref fvx, RunnerRules.FollowSmoothSeconds + (RunnerRules.FollowSmoothPerRow * row), RunnerRules.FollowMaxSpeed, dt);
                m.VX = fvx;
                // Joiners run in from their box faster than the walk, then hold their slot exactly.
                var tz = Distance + sz;
                var run = (Level.Speed + 4f) * dt;
                m.Z += Clamp(tz - m.Z, -run, run);
            }
        }

        private void SpawnRows()
        {
            while (_nextRow < Level.Rows.Count && Level.Rows[_nextRow].At - Distance <= RunnerRules.SpawnAhead)
            {
                var row = Level.Rows[_nextRow++];
                switch (row.Kind)
                {
                    case RunnerRowKind.Box:
                    {
                        var box = new RunnerBox(_nextId++, row.Box!, Clamp(row.X, -Level.HalfWidth + RunnerBox.Radius, Level.HalfWidth - RunnerBox.Radius), row.At);
                        _boxes.Add(box);
                        Emit(RunnerEventKind.BoxSpawned, box.Id, 0, box.X, box.Z, box.Hp, false, true, box.Def.Unit);
                        break;
                    }

                    case RunnerRowKind.Hazard:
                    {
                        var hd = row.Hazard!;
                        var half = Math.Min(hd.Width * 0.5f, Level.HalfWidth);
                        var hz = new RunnerHazard(_nextId++, hd, Clamp(row.X, -Level.HalfWidth + half, Level.HalfWidth - half), row.At) { PrevZ = row.At };
                        _hazards.Add(hz);
                        Emit(RunnerEventKind.HazardSpawned, hz.Id, 0, hz.X, hz.Z, hd.Width, false, false, RunnerHazardDef.Name(hd.Kind));
                        break;
                    }

                    case RunnerRowKind.Barrier:
                    {
                        var bd = row.Barrier!;
                        var half = Math.Min(bd.Width * 0.5f, Level.HalfWidth);
                        var bar = new RunnerBarrier(_nextId++, bd, Clamp(row.X, -Level.HalfWidth + half, Level.HalfWidth - half), row.At);
                        _barriers.Add(bar);
                        Emit(RunnerEventKind.BarrierSpawned, bar.Id, 0, bar.X, bar.Z, bar.Hp, false, false, RunnerBarrierDef.Name(bd.Kind));
                        break;
                    }

                    default:
                    {
                        var unit = _catalog.Unit(row.Unit);
                        var boss = row.Kind == RunnerRowKind.Boss;
                        for (var i = 0; i < row.Count; i++)
                        {
                            var x = Clamp(row.XOf(i), -Level.HalfWidth + unit.Radius, Level.HalfWidth - unit.Radius);
                            var foe = new RunnerFoe(_nextId++, unit, x, row.At, boss, boss ? Level.BossHpMul : Level.FoeHpMul, Level.FoeDmgMul);
                            // Stagger the first swing so a pack does not attack in one beat.
                            foe.Cooldown = unit.Cycle * (0.35f + (0.2f * (i % 3)));
                            _foes.Add(foe);
                            Emit(RunnerEventKind.FoeSpawned, foe.Id, 0, foe.X, foe.Z, foe.Hp, false, false, unit.Id);
                        }

                        break;
                    }
                }
            }
        }

        private void MoveFoes(float dt)
        {
            foreach (var f in _foes)
            {
                var near = NearestMember(f.X, f.Z, out var dist);
                var reach = f.Unit.Range + f.Unit.Radius + (near?.Unit.Radius ?? 0f);
                var wasEngaged = f.Engaged;
                f.Walking = false;
                if (f.Boss)
                {
                    // He holds his ground; the squad halts at the arena line inside his slam reach and they fight it out.
                    var keep = f.Unit.Range + f.Unit.Radius + Hero.Unit.Radius;
                    f.Engaged = f.Z - Hero.Z <= keep + 0.05f;
                    if (f.Engaged && !wasEngaged)
                    {
                        BossFight = true;
                        Emit(RunnerEventKind.BossEngaged, f.Id, Hero.Id, f.X, f.Z, f.Hp, true);
                    }

                    continue;
                }

                // Contact is body to body: a ranged foe shoots from range, and anything the squad walks into hits back.
                f.Engaged = near != null && dist <= ContactReach(f) + near.Unit.Radius;
                if (!f.Passed && f.Z < Hero.Z - 0.3f)
                {
                    f.Passed = true;
                }

                if (f.Passed)
                {
                    // Once the hero is past, it has a moment to turn, chase and hit the rows going by; then it gives up.
                    f.ChaseLeft -= dt;
                }

                if (f.Unit.Speed <= 0f || near == null || f.Engaged)
                {
                    continue;
                }

                if (f.Passed)
                {
                    // Turns and chases the squad that walked past it for a moment, then gives up and drops behind.
                    if (f.ChaseLeft <= 0f || f.Unit.Ranged)
                    {
                        continue;
                    }

                    WalkFoe(f, 1f, Math.Min(f.Unit.Speed, Level.Speed) * dt, near.X, dt);
                    continue;
                }

                // A ranged foe stops once the squad is inside its range and shoots; everything else keeps coming.
                if (f.Unit.Ranged && dist <= reach * 0.8f)
                {
                    continue;
                }

                // Walks in down the lane from the moment it spawns; once close it also drifts toward the hero's line.
                WalkFoe(f, -1f, f.Unit.Speed * dt, f.Z - Hero.Z <= RunnerRules.SeekRange ? Hero.X : f.LaneX, dt);
            }
        }

        /// <summary>
        /// One walking step for a foe going <paramref name="dir"/> along the road (-1 toward the squad, +1 chasing it), at
        /// its usual pace, drifting toward <paramref name="seekX"/> (Derek 2026-10-27: "the enemies should avoid the
        /// obstacles and not run over them"). A trap or standing barrier within <see cref="RunnerRules.EnemyAvoidLook"/>
        /// in its path makes it side-step toward the nearer open end instead; it never steps onto a footprint, so if the
        /// way round is not clear yet (or the barrier spans the road) it waits behind it, and walks on once it is clear
        /// (or the squad smashes / shoots the barrier down). One it is somehow standing on (a log rolled into it) it
        /// steps off sideways.
        /// </summary>
        private void WalkFoe(RunnerFoe f, float dir, float step, float seekX, float dt)
        {
            // Its body counts the way a squad member's does against a trap (half its radius); it plans with its whole
            // radius plus a margin so it goes round with room to spare, and squeezes through on the body alone when the
            // roomy line is shut (two obstacles close together).
            var body = f.Unit.Radius * RunnerRules.HazardBodyShare;
            var plan = f.Unit.Radius + RunnerRules.EnemyAvoidMargin;
            var minX = -Level.HalfWidth + f.Unit.Radius;
            var maxX = Level.HalfWidth - f.Unit.Radius;
            var side = RunnerRules.EnemyAvoidSpeed * dt;
            f.Walking = true;
            f.Avoiding = false;
            f.Blocked = false;
            f.BlockedBy = 0;
            var oldX = f.X;

            // The line it would hold with the road clear: the usual slow drift toward its target.
            f.LaneX = Clamp(f.LaneX + Clamp(seekX - f.LaneX, -RunnerRules.EnemySeek * dt, RunnerRules.EnemySeek * dt), minX, maxX);

            if (ObstacleUnder(f.X, f.Z, body, out var ox, out var ohw))
            {
                // Standing on one (it appeared there, or a log rolled into it): steps off to the nearer open side.
                var to = OpenSide(f.X, f.Z, ox, ohw, body, minX, maxX, f.X) ?? (f.X < ox ? minX : maxX);
                f.X = Clamp(f.X + Clamp(to - f.X, -side, side), minX, maxX);
                f.Avoiding = true;
            }
            else if (ObstacleAhead(f.X, f.Z, dir, plan, out var ax, out var az, out var ahw))
            {
                // Side-steps to the open end (or holds its line if the barrier spans the road, and waits behind it).
                var open = OpenSide(f.X, az, ax, ahw, plan, minX, maxX, seekX) ?? OpenSide(f.X, az, ax, ahw, body, minX, maxX, seekX);
                f.Avoiding = open != null;
                f.AvoidX = open ?? f.X;
                var nx = Clamp(f.X + Clamp(f.AvoidX - f.X, -side, side), minX, maxX);
                if (!ObstacleUnder(nx, f.Z, body, out _, out _))
                {
                    f.X = nx;
                }
            }
            else
            {
                // Back onto its own line (it keeps its place in the pack rather than bunching at the end of the obstacle it
                // went round), unless that would steer it into the path of one coming up. With nothing gone round this is
                // just the usual slow drift toward its target.
                var nx = Clamp(f.X + Clamp(f.LaneX - f.X, -side, side), minX, maxX);
                if (!ObstacleUnder(nx, f.Z, body, out _, out _) && !ObstacleAhead(nx, f.Z, dir, plan, out _, out _, out _))
                {
                    f.X = nx;
                }
            }

            // Then along the road at its usual pace, unless that step would put its body on a footprint: it waits.
            var nz = f.Z + (dir * step);
            if (!ObstacleUnder(f.X, nz, body, out _, out _) || ObstacleUnder(f.X, f.Z, body, out _, out _))
            {
                f.Z = nz;
            }
            else
            {
                f.Blocked = true;
                f.BlockedBy = BarrierUnder(f.X, nz, body)?.Id ?? 0;
                f.Walking = Math.Abs(f.X - oldX) > 1e-4f;
            }
        }

        /// <summary>The standing barrier a body of half-size <paramref name="r"/> at (x, z) would stand on, if any.</summary>
        private RunnerBarrier? BarrierUnder(float x, float z, float r)
        {
            foreach (var b in _barriers)
            {
                if (b.Hp > 0 && RunnerObstacle.Overlaps(b.X, b.Z, b.HalfWidth, b.HalfDepth, x, z, r))
                {
                    return b;
                }
            }

            return null;
        }

        /// <summary>The obstacle footprint (a trap, or a barrier still standing) a body of half-size <paramref name="r"/> at (x, z) would stand on.</summary>
        private bool ObstacleUnder(float x, float z, float r, out float ox, out float halfWidth)
        {
            foreach (var h in _hazards)
            {
                if (RunnerObstacle.Overlaps(h.X, h.Z, h.HalfWidth, h.HalfDepth, x, z, r))
                {
                    ox = h.X;
                    halfWidth = h.HalfWidth;
                    return true;
                }
            }

            foreach (var b in _barriers)
            {
                if (b.Hp > 0 && RunnerObstacle.Overlaps(b.X, b.Z, b.HalfWidth, b.HalfDepth, x, z, r))
                {
                    ox = b.X;
                    halfWidth = b.HalfWidth;
                    return true;
                }
            }

            ox = 0f;
            halfWidth = 0f;
            return false;
        }

        /// <summary>The nearest obstacle in the lane at <paramref name="x"/> within <see cref="RunnerRules.EnemyAvoidLook"/> ahead of a body at <paramref name="z"/> going <paramref name="dir"/>.</summary>
        private bool ObstacleAhead(float x, float z, float dir, float r, out float ox, out float oz, out float halfWidth)
        {
            var best = float.MaxValue;
            ox = oz = halfWidth = 0f;
            foreach (var h in _hazards)
            {
                if (InPath(x, z, dir, r, h.X, h.Z, h.HalfWidth, h.HalfDepth, ref best))
                {
                    (ox, oz, halfWidth) = (h.X, h.Z, h.HalfWidth);
                }
            }

            foreach (var b in _barriers)
            {
                if (b.Hp > 0 && InPath(x, z, dir, r, b.X, b.Z, b.HalfWidth, b.HalfDepth, ref best))
                {
                    (ox, oz, halfWidth) = (b.X, b.Z, b.HalfWidth);
                }
            }

            return best < float.MaxValue;
        }

        /// <summary>True (and <paramref name="best"/> lowered to its gap) when this footprint is the nearest one in the lane ahead so far.</summary>
        private static bool InPath(float x, float z, float dir, float r, float ox, float oz, float hw, float hd, ref float best)
        {
            // Anything whose middle is still ahead counts, even one it is already level with (it is beside it, squeezing past).
            var gap = (dir * (oz - z)) - hd - r;
            if (dir * (oz - z) <= 0f || gap > RunnerRules.EnemyAvoidLook || gap >= best || Math.Abs(x - ox) >= hw + r)
            {
                return false;
            }

            best = gap;
            return true;
        }

        /// <summary>
        /// The lane just past the left or right end of an obstacle that a body of half-size <paramref name="r"/> fits in at
        /// the obstacle's row (on the road and off every other footprint there), the one nearer <paramref name="from"/>
        /// (ties toward <paramref name="prefer"/>); null when neither end is open (a barrier that spans the road).
        /// </summary>
        private float? OpenSide(float from, float atZ, float ox, float halfWidth, float r, float minX, float maxX, float prefer)
        {
            var left = ox - halfWidth - r - 0.01f;
            var right = ox + halfWidth + r + 0.01f;
            var leftOk = left >= minX && !ObstacleUnder(left, atZ, r, out _, out _);
            var rightOk = right <= maxX && !ObstacleUnder(right, atZ, r, out _, out _);
            if (leftOk && rightOk)
            {
                var dl = Math.Abs(from - left);
                var dr = Math.Abs(from - right);
                return Math.Abs(dl - dr) < 0.05f ? (Math.Abs(prefer - left) <= Math.Abs(prefer - right) ? left : right) : dl < dr ? left : right;
            }

            return leftOk ? left : rightOk ? right : null;
        }

        private static float ContactReach(RunnerFoe f) => (f.Unit.Ranged ? 0.6f : f.Unit.Range) + f.Unit.Radius;

        // ---------- attacks ----------

        private void SquadAttacks(float dt)
        {
            // By index over the members there at the start of the step: a swing that breaks a box adds followers.
            var count = _squad.Count;
            for (var i = 0; i < count && i < _squad.Count; i++)
            {
                var m = _squad[i];
                m.SinceAttack += dt;
                if (m.CycleClock >= 0f)
                {
                    m.CycleClock += dt;
                }
                else
                {
                    m.Cooldown -= dt;
                }

                // One attack take = one shot or swing. The take runs to its end; the next starts right then if anything
                // is still in range (no rest gap, so the cadence is the take's length), otherwise the member walks on.
                for (var guard = 0; guard < 4; guard++)
                {
                    if (m.CycleClock >= 0f && !m.Released && m.CycleClock >= m.Cycle.Release)
                    {
                        Release(m);
                    }

                    if (m.CycleClock >= 0f && m.CycleClock < m.Cycle.Interval)
                    {
                        break;
                    }

                    // From idle the take counts as started at the top of this step (dt in), so its release lands on the
                    // same sim time whatever the frame rate; back to back, the next take carries the last one's overrun.
                    var carry = m.CycleClock >= 0f ? m.CycleClock - m.Cycle.Interval : dt;
                    m.CycleClock = -1f;
                    if (m.Cooldown > 0f || !(m.Unit.Ranged ? AnythingAhead(m) : MeleeTarget(m, out _)))
                    {
                        m.Cooldown = Math.Max(m.Cooldown, 0f);
                        break;
                    }

                    m.Cycle = m.Plan.Faster(RateMul);
                    m.CycleClock = Math.Min(carry, dt);
                    m.Released = false;
                    m.Cooldown = 0f;
                    m.CyclesStarted++;
                    Emit(RunnerEventKind.AttackStarted, m.Id, 0, m.X, m.Z, m.Cycle.Speed, false, false, m.Unit.Id);
                }
            }
        }

        /// <summary>The take's release frame: the shot leaves the shooter's lane, or the swing lands on what is in front now.</summary>
        private void Release(RunnerMember m)
        {
            m.Released = true;
            m.Releases++;
            m.SinceAttack = 0f;
            var damage = (int)Math.Round(m.Unit.Damage * m.Power * DamageMul);
            if (m.Unit.Ranged)
            {
                m.TargetId = 0;
                var reach = ReachOf(m);
                var shot = new RunnerShot(_nextId++, m.Id, false, m.X, m.Z + 0.4f, damage, m.Unit.ShotSpeed, reach + RunnerRules.ShotOvershoot);
                _shots.Add(shot);
                Emit(RunnerEventKind.Fired, m.Id, 0, m.X, m.Z + reach, shot.Id, false, false, m.Unit.Id);
                return;
            }

            if (!MeleeTarget(m, out var melee))
            {
                // Whatever it swung at died or slipped out of reach mid-take: the blade whiffs.
                Emit(RunnerEventKind.Fired, m.Id, 0, m.X, m.Z + m.Unit.Range, 0, false, false, m.Unit.Id);
                return;
            }

            m.TargetId = melee.Id;
            Emit(RunnerEventKind.Fired, m.Id, melee.Id, melee.X, melee.Z, 0, false, melee.OnBox, m.Unit.Id);
            Damage(melee.Id, melee.OnBox, damage, m.Id);
        }

        /// <summary>The longest reach any ranged squad member has right now (range crates included); 0 with none.</summary>
        public float SquadReach
        {
            get
            {
                var r = 0f;
                foreach (var m in _squad)
                {
                    if (m.Unit.Ranged)
                    {
                        r = Math.Max(r, ReachOf(m));
                    }
                }

                return r;
            }
        }

        /// <summary>
        /// How far a foe opens fire from. A ranged enemy shoots from its own range or the squad's longest reach, whichever
        /// is further, so it is never outranged (Derek 2026-10-26: "they don't start firing from as far away as the heroes
        /// can"). Melee and the boss keep their own reach.
        /// </summary>
        public float FireRangeOf(RunnerFoe f) => f.Unit.Ranged && !f.Boss ? Math.Max(f.Unit.Range, SquadReach) : f.Unit.Range;

        /// <summary>
        /// True when a ranged squad member could shoot this foe right now (the same test as <see cref="AnythingAhead"/>):
        /// a foe a hero can hit can shoot back, even when its nearest target is a lane or two over.
        /// </summary>
        private bool InSquadReach(RunnerFoe f)
        {
            foreach (var m in _squad)
            {
                if (m.Unit.Ranged && f.Z > m.Z - 0.5f && f.Z - m.Z <= ReachOf(m) + f.Unit.Radius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Any live foe or box ahead within the unit's range, at any lane position.</summary>
        private bool AnythingAhead(RunnerMember m)
        {
            foreach (var f in _foes)
            {
                if (f.Hp > 0 && f.Z > m.Z - 0.5f && f.Z - m.Z <= ReachOf(m) + f.Unit.Radius)
                {
                    return true;
                }
            }

            foreach (var b in _boxes)
            {
                if (b.Hp > 0 && b.Z > m.Z - 0.5f && b.Z - m.Z <= ReachOf(m) + RunnerBox.Radius)
                {
                    return true;
                }
            }

            foreach (var b in _barriers)
            {
                if (b.Hp > 0 && b.Z > m.Z - 0.5f && b.Z - m.Z <= ReachOf(m) + b.HalfDepth)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The nearest foe (then box) straight in front of a melee unit, within its reach.</summary>
        private bool MeleeTarget(RunnerMember m, out (int Id, bool OnBox, float X, float Z) hit)
        {
            hit = default;
            var best = float.MaxValue;
            foreach (var f in _foes)
            {
                var dz = f.Z - m.Z;
                var dx = Math.Abs(f.X - m.X);
                if (f.Hp <= 0 || dz <= -1f || dz > m.Unit.Range + f.Unit.Radius + m.Unit.Radius
                    || dx > RunnerRules.MeleeHalfWidth + f.Unit.Radius || dz >= best)
                {
                    continue;
                }

                best = dz;
                hit = (f.Id, false, f.X, f.Z);
            }

            if (hit.Id != 0)
            {
                return true;
            }

            foreach (var b in _boxes)
            {
                var dz = b.Z - m.Z;
                if (b.Hp <= 0 || dz <= -1f || dz > m.Unit.Range + RunnerBox.Radius + m.Unit.Radius
                    || Math.Abs(b.X - m.X) > RunnerRules.MeleeHalfWidth + RunnerBox.Radius || dz >= best)
                {
                    continue;
                }

                best = dz;
                hit = (b.Id, true, b.X, b.Z);
            }

            return hit.Id != 0;
        }

        private void FoesAttack(float dt)
        {
            for (var i = 0; i < _foes.Count && !Over; i++)
            {
                var f = _foes[i];
                f.SinceAttack += dt;
                if (f.Hp <= 0)
                {
                    continue;
                }

                f.Cooldown -= dt;
                var target = NearestMember(f.X, f.Z, out var dist);
                if (target == null || (f.Passed && f.ChaseLeft <= 0f))
                {
                    // Gave up on the squad that walked past: it drops behind and falls off the road.
                    continue;
                }

                if (f.Boss)
                {
                    BossAttack(f, dt);
                    continue;
                }

                // Bodies overlapping: the squad pushing through takes contact hits, whatever the enemy's own attack is.
                if (f.Unit.Ranged && dist <= f.Unit.Radius + target.Unit.Radius)
                {
                    f.ContactClock += dt * RunnerRules.ContactHitsPerSecond;
                    if (f.ContactClock >= 1f)
                    {
                        f.ContactClock -= 1f;
                        StrikeMember(f, target, Math.Max(1, (int)Math.Round(f.Damage * RunnerRules.ContactDamageShare)));
                        if (Over)
                        {
                            return;
                        }

                        target = NearestMember(f.X, f.Z, out dist);
                        if (target == null)
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    f.ContactClock = Math.Min(f.ContactClock, 0.5f);
                }

                // Melee: whoever is in reach, ahead or already past. Ranged: the nearest member in range, in any lane,
                // either way down the road (a foe the squad has walked past shoots after it): enemies aim, heroes fire straight.
                // Held up behind a barrier: it batters the barrier down at its own attack rate rather than firing over it
                // (unless a squad member is right on it), so a pack never sits safe behind a wall shooting.
                if (f.BlockedBy != 0 && dist > f.Unit.Radius + target.Unit.Radius + f.Unit.Range && Barrier(f.BlockedBy) is { Hp: > 0 } wall)
                {
                    if (f.Cooldown <= 0f)
                    {
                        f.Cooldown = f.Unit.Cycle;
                        f.SinceAttack = 0f;
                        f.TargetId = wall.Id;
                        Emit(RunnerEventKind.Fired, f.Id, wall.Id, wall.X, wall.Z, 0, false, false, f.Unit.Id);
                        DamageBarrier(wall, f.Damage, f.Id);
                    }

                    continue;
                }

                var inRange = dist <= FireRangeOf(f) + f.Unit.Radius + target.Unit.Radius || (f.Unit.Ranged && InSquadReach(f));
                if (!inRange)
                {
                    f.Cooldown = Math.Max(f.Cooldown, 0f);
                    continue;
                }

                if (f.Cooldown > 0f)
                {
                    continue;
                }

                f.Cooldown = f.Unit.Cycle;
                f.SinceAttack = 0f;
                f.TargetId = target.Id;
                Emit(RunnerEventKind.Fired, f.Id, target.Id, target.X, target.Z, 0, false, false, f.Unit.Id);
                if (f.Unit.Ranged)
                {
                    var dir = target.Z >= f.Z ? 1f : -1f;
                    var (ax, az) = FoeAim(f.X, f.Z + (0.4f * dir), target.X, target.Z, f.Unit.ShotSpeed, Level.Speed);
                    // It flies far enough to cover the longer fire range and the led aim point, so it never fizzles short.
                    var flight = Math.Max(FireRangeOf(f), (float)Math.Sqrt((ax * ax) + (az * az))) + RunnerRules.ShotOvershoot + 2f;
                    _shots.Add(new RunnerShot(_nextId++, f.Id, true, f.X, f.Z + (0.4f * dir), f.Damage, f.Unit.ShotSpeed, flight, az, Math.Abs(az) > 1e-4f ? ax / Math.Abs(az) : 0f));
                }
                else
                {
                    StrikeSquad(f, f.Damage);
                }
            }
        }

        /// <summary>
        /// The boss's attack is a telegraphed slam, never a free hit: once the squad is at his arena line and his cooldown
        /// is up he raises his arm (<see cref="RunnerEventKind.BossWindup"/>, the scene marks the road and plays his swing),
        /// and <see cref="RunnerRules.BossWindup"/> seconds later the slam lands on everyone standing in the marked area
        /// (<see cref="SlamArea"/>): each follower in it falls, the hero takes the boss's damage. Steering out of the
        /// marked road in time dodges it. A squad nowhere near him is not slammed.
        /// </summary>
        private void BossAttack(RunnerFoe f, float dt)
        {
            if (f.Windup > 0f)
            {
                f.Windup -= dt;
                if (f.Windup <= 0f)
                {
                    f.Windup = 0f;
                    var quake = f.Move == RunnerBossMove.Quake;
                    f.Move = RunnerBossMove.None;
                    f.Cooldown = f.Unit.Cycle + (quake ? RunnerRules.BossQuakeRecovery : 0f);
                    if (quake)
                    {
                        Quake(f);
                    }
                    else
                    {
                        Slam(f);
                    }
                }

                return;
            }

            if (!f.Engaged || f.Cooldown > 0f)
            {
                f.Cooldown = Math.Max(f.Cooldown, 0f);
                return;
            }

            f.SinceAttack = 0f;
            f.TargetId = Hero.Id;
            if (f.Actions % RunnerRules.BossQuakeEvery == RunnerRules.BossQuakeSlot)
            {
                // The quake: a long telegraph, the whole road marked except a safe band on the side away from the hero.
                f.Move = RunnerBossMove.Quake;
                f.Windup = f.WindupTotal = RunnerRules.BossQuakeWindup;
                f.QuakeSafeX = QuakeSafeCentre(Hero.X, f.Quakes, Level.HalfWidth);
                f.Quakes++;
                f.Actions++;
                Emit(RunnerEventKind.BossQuakeWindup, f.Id, Hero.Id, f.QuakeSafeX, f.Z, f.Windup, true, false, f.Unit.Id);
                return;
            }

            f.Move = RunnerBossMove.Slam;
            f.Windup = f.WindupTotal = RunnerRules.BossWindup;
            f.Actions++;
            Emit(RunnerEventKind.BossWindup, f.Id, Hero.Id, f.X, f.Z, f.Windup, false, false, f.Unit.Id);
            Emit(RunnerEventKind.Fired, f.Id, Hero.Id, Hero.X, Hero.Z, 0, false, false, f.Unit.Id);
        }

        /// <summary>
        /// Where the quake's safe band is centred: on the side of the road away from the lead, so the squad has to cross to it
        /// (a lead near the middle alternates sides). The band is <see cref="RunnerRules.BossQuakeSafeHalf"/> each side of it.
        /// </summary>
        public static float QuakeSafeCentre(float heroX, int quakesSoFar, float roadHalfWidth)
        {
            var side = heroX > 0.4f ? -1f : heroX < -0.4f ? 1f : (quakesSoFar % 2 == 0 ? -1f : 1f);
            return side * Math.Min(RunnerRules.BossQuakeSafeCentre, roadHalfWidth * 0.6f);
        }

        /// <summary>The road a quake lands on: z back from the boss's front (all of the road's width except <see cref="QuakeSafe"/>).</summary>
        public static (float MinZ, float MaxZ) QuakeArea(RunnerFoe boss) => (boss.Z - RunnerRules.BossQuakeDepth, boss.Z);

        /// <summary>The quake's safe band (x range) for the quake the boss is winding up or last landed.</summary>
        public static (float MinX, float MaxX) QuakeSafe(RunnerFoe boss) =>
            (boss.QuakeSafeX - RunnerRules.BossQuakeSafeHalf, boss.QuakeSafeX + RunnerRules.BossQuakeSafeHalf);

        /// <summary>True when a body at (<paramref name="x"/>, <paramref name="z"/>) of <paramref name="radius"/> would be caught by the boss's quake (same body tolerance as the slam).</summary>
        public static bool QuakeCatches(RunnerFoe boss, float x, float z, float radius)
        {
            var r = radius * 0.5f;
            var (minZ, maxZ) = QuakeArea(boss);
            if (z < minZ - r || z > maxZ + r)
            {
                return false;
            }

            var (minX, maxX) = QuakeSafe(boss);
            return x - r < minX || x + r > maxX;
        }

        private void Quake(RunnerFoe f)
        {
            var caught = 0;
            var heroHit = false;
            var victims = new List<RunnerMember>();
            foreach (var m in _squad)
            {
                if (!QuakeCatches(f, m.X, m.Z, m.Unit.Radius))
                {
                    continue;
                }

                if (m.IsHero)
                {
                    heroHit = true;
                }
                else
                {
                    victims.Add(m);
                }
            }

            foreach (var v in victims)
            {
                HurtFollower(f, v, f.Damage);
                caught++;
            }

            if (heroHit)
            {
                caught++;
            }

            Emit(RunnerEventKind.BossQuake, f.Id, Hero.Id, f.QuakeSafeX, f.Z - (RunnerRules.BossQuakeDepth * 0.5f), caught, true, false, f.Unit.Id);
            if (heroHit)
            {
                HurtHero(f, (int)Math.Round(f.Damage * RunnerRules.BossQuakeDamage));
            }
        }

        /// <summary>The road a boss's slam lands on: across his body (plus a margin) and <see cref="RunnerRules.BossSlamDepth"/> back from his front.</summary>
        public static (float MinX, float MaxX, float MinZ, float MaxZ) SlamArea(RunnerFoe boss)
        {
            var half = boss.Unit.Radius + RunnerRules.BossSlamExtra;
            return (boss.X - half, boss.X + half, boss.Z - RunnerRules.BossSlamDepth, boss.Z);
        }

        private void Slam(RunnerFoe f)
        {
            var (minX, maxX, minZ, maxZ) = SlamArea(f);
            var caught = 0;
            var heroHit = false;
            var victims = new List<RunnerMember>();
            foreach (var m in _squad)
            {
                var r = m.Unit.Radius * 0.5f;
                if (m.X >= minX - r && m.X <= maxX + r && m.Z >= minZ - r && m.Z <= maxZ + r)
                {
                    if (m.IsHero)
                    {
                        heroHit = true;
                    }
                    else
                    {
                        victims.Add(m);
                    }
                }
            }

            foreach (var v in victims)
            {
                HurtFollower(f, v, f.Damage);
                caught++;
            }

            if (heroHit)
            {
                caught++;
            }

            Emit(RunnerEventKind.BossSlam, f.Id, Hero.Id, f.X, f.Z - (RunnerRules.BossSlamDepth * 0.5f), caught, true, false, f.Unit.Id);
            if (heroHit)
            {
                HurtHero(f, f.Damage);
            }
        }

        /// <summary>A contact hit on one member: the damage comes off its HP (a follower at 0 falls).</summary>
        private void StrikeMember(RunnerFoe f, RunnerMember m, int damage)
        {
            if (m.IsHero)
            {
                HurtHero(f, damage);
                return;
            }

            HurtFollower(f, m, damage);
        }

        private void HurtFollower(RunnerFoe f, RunnerMember victim, int damage) => HurtFollower(f.Id, victim, damage);

        /// <summary>Every hit on a follower (contact, shots, traps, the boss) comes off its HP; it falls only at 0.</summary>
        private void HurtFollower(int source, RunnerMember victim, int damage)
        {
            victim.Hp = Math.Max(0, victim.Hp - damage);
            if (victim.Hp <= 0)
            {
                _squad.Remove(victim);
                Emit(RunnerEventKind.FollowerLost, victim.Id, source, victim.X, victim.Z, Followers, true, false, victim.Unit.Id);
                Reslot();
                return;
            }

            Emit(RunnerEventKind.FollowerHit, victim.Id, source, victim.X, victim.Z, damage, true, false, victim.Unit.Id);
        }

        /// <summary>A follower's share of a trap's hero damage: one trap takes a good bite of its HP, a second one finishes it.</summary>
        public static int FollowerTrapDamage(int heroDamage) =>
            Math.Max(1, Math.Min(RunnerRules.FollowerHp - 1, (int)Math.Round(heroDamage * RunnerRules.FollowerTrapShare)));

        /// <summary>
        /// The follower's bite, <see cref="RunnerRules.SideHitMul"/> times over from the side, still never all of a fresh
        /// follower's HP in one go (spikes 40: 16 head-on, 24 from the side, so the next knock of any kind drops it).
        /// </summary>
        public static int FollowerTrapDamage(int heroDamage, bool side) =>
            side ? Math.Max(1, Math.Min(RunnerRules.FollowerHp - 1, (int)Math.Round(heroDamage * RunnerRules.FollowerTrapShare * RunnerRules.SideHitMul))) : FollowerTrapDamage(heroDamage);

        /// <summary>A trap's or barrier's hero damage, <see cref="RunnerRules.SideHitMul"/> times over from the side.</summary>
        public static int SideDamage(int damage, bool side) => side ? (int)Math.Round(damage * RunnerRules.SideHitMul) : damage;

        private void HurtHero(RunnerFoe f, int damage)
        {
            Hero.Hp = Math.Max(0, Hero.Hp - damage);
            Emit(RunnerEventKind.HeroHit, Hero.Id, f.Id, Hero.X, Hero.Z, damage, true, false, Hero.Unit.Id);
            if (Hero.Hp <= 0 && !Over)
            {
                Outcome = RunnerOutcome.Lost;
                Emit(RunnerEventKind.Lost, Hero.Id, f.Id, Hero.X, Hero.Z, 0, true);
            }
        }

        /// <summary>An enemy hit: hurts the follower nearest the enemy; with no followers left it hurts the hero.</summary>
        private void StrikeSquad(RunnerFoe f, int damage)
        {
            RunnerMember? victim = null;
            var best = float.MaxValue;
            for (var i = 1; i < _squad.Count; i++)
            {
                var m = _squad[i];
                var d = Sq(m.X - f.X) + Sq(m.Z - f.Z);
                if (d < best)
                {
                    best = d;
                    victim = m;
                }
            }

            if (victim != null)
            {
                HurtFollower(f, victim, damage);
                return;
            }

            HurtHero(f, damage);
        }

        private void MoveShots(float dt)
        {
            for (var i = _shots.Count - 1; i >= 0; i--)
            {
                var s = _shots[i];
                var step = s.Speed * dt;
                var from = s.Z;
                var to = s.Z + (s.DZ * step);
                var toX = s.X + (s.DX * step);
                var lo = Math.Min(from, to);
                var hi = Math.Max(from, to);
                var hit = false;
                if (s.ByFoe)
                {
                    // Along its aimed line (diagonal across lanes too): the first squad member whose body the segment
                    // flown this frame passes through takes it (as a hit on the squad: a follower first, the hero only
                    // once none are left).
                    RunnerMember? struck = null;
                    var bestT = float.MaxValue;
                    foreach (var m in _squad)
                    {
                        var r = m.Unit.Radius + RunnerRules.ShotHalfWidth;
                        var t = Math.Max(0f, Math.Min(step, ((m.X - s.X) * s.DX) + ((m.Z - s.Z) * s.DZ)));
                        var px = s.X + (s.DX * t);
                        var pz = s.Z + (s.DZ * t);
                        if (Sq(m.X - px) + Sq(m.Z - pz) <= r * r && t < bestT)
                        {
                            bestT = t;
                            struck = m;
                        }
                    }

                    if (struck != null)
                    {
                        hit = true;
                        _shots.RemoveAt(i);
                        s.X += s.DX * bestT;
                        s.Z += s.DZ * bestT;
                        var shooter = Foe(s.Shooter);
                        if (shooter != null)
                        {
                            StrikeSquad(shooter, s.Damage);
                        }
                    }
                }
                else
                {
                    // Straight down the road: the nearest foe or box whose body it passes through (the boss too: only a
                    // shot whose lane overlaps his body, radius included, hurts him).
                    var bestZ = float.MaxValue;
                    var id = 0;
                    var onBox = false;
                    foreach (var f in _foes)
                    {
                        var r = f.Unit.Radius + RunnerRules.ShotHalfWidth;
                        if (f.Hp > 0 && Math.Abs(f.X - s.X) <= r && f.Z >= lo - r && f.Z <= hi + r && f.Z < bestZ)
                        {
                            bestZ = f.Z;
                            id = f.Id;
                            onBox = false;
                        }
                    }

                    foreach (var b in _boxes)
                    {
                        var r = RunnerBox.Radius + RunnerRules.ShotHalfWidth;
                        if (b.Hp > 0 && Math.Abs(b.X - s.X) <= r && b.Z >= lo - r && b.Z <= hi + r && b.Z < bestZ)
                        {
                            bestZ = b.Z;
                            id = b.Id;
                            onBox = true;
                        }
                    }

                    // A barrier stops the shot too (the stakes, the stones): whatever is nearest along the lane takes it.
                    RunnerBarrier? wall = null;
                    foreach (var b in _barriers)
                    {
                        var r = b.HalfDepth + RunnerRules.ShotHalfWidth;
                        if (b.Hp > 0 && Math.Abs(b.X - s.X) <= b.HalfWidth + RunnerRules.ShotHalfWidth && b.Z >= lo - r && b.Z <= hi + r && b.Z < bestZ)
                        {
                            bestZ = b.Z;
                            wall = b;
                        }
                    }

                    if (wall != null)
                    {
                        hit = true;
                        _shots.RemoveAt(i);
                        s.Z = bestZ;
                        DamageBarrier(wall, s.Damage, s.Shooter);
                    }
                    else if (id != 0)
                    {
                        hit = true;
                        _shots.RemoveAt(i);
                        s.Z = bestZ;
                        Damage(id, onBox, s.Damage, s.Shooter);
                    }
                }

                if (hit)
                {
                    if (Over)
                    {
                        return;
                    }

                    continue;
                }

                s.Z = to;
                s.X = toX;
                s.Travelled += step;
                if (s.Travelled >= s.Range)
                {
                    _shots.RemoveAt(i);
                }
            }
        }

        private void Damage(int targetId, bool onBox, int damage, int by)
        {
            if (onBox)
            {
                var b = Box(targetId);
                if (b == null || b.Hp <= 0)
                {
                    return;
                }

                b.Hp = Math.Max(0, b.Hp - damage);
                Emit(RunnerEventKind.Hit, b.Id, by, b.X, b.Z, damage, damage >= RunnerRules.BigHit, true);
                if (b.Hp == 0)
                {
                    _boxes.Remove(b);
                    BoxesBroken++;
                    Emit(RunnerEventKind.BoxBroken, b.Id, by, b.X, b.Z, b.Def.Count, true, true, b.Def.Unit);
                    Claim(b);
                }

                return;
            }

            var f = Foe(targetId);
            if (f == null || f.Hp <= 0)
            {
                return;
            }

            f.Hp = Math.Max(0, f.Hp - damage);
            Emit(RunnerEventKind.Hit, f.Id, by, f.X, f.Z, damage, f.Boss || damage >= RunnerRules.BigHit);
            if (f.Hp == 0)
            {
                _foes.Remove(f);
                Kills++;
                Emit(RunnerEventKind.FoeKilled, f.Id, by, f.X, f.Z, Kills, f.Boss, false, f.Unit.Id);
            }
        }

        private void DamageBarrier(RunnerBarrier b, int damage, int by)
        {
            if (b.Hp <= 0)
            {
                return;
            }

            b.Hp = Math.Max(0, b.Hp - damage);
            Emit(RunnerEventKind.BarrierHit, b.Id, by, b.X, b.Z, damage, false, false, RunnerBarrierDef.Name(b.Kind));
            if (b.Hp == 0)
            {
                _barriers.Remove(b);
                BarriersBroken++;
                Emit(RunnerEventKind.BarrierBroken, b.Id, by, b.X, b.Z, 0, false, false, RunnerBarrierDef.Name(b.Kind));
            }
        }

        /// <summary>
        /// A standing barrier the squad walks into: everyone it touches right then takes its damage (the hero all of it, a
        /// follower a trap's share, <see cref="FollowerTrapDamage(int, bool)"/>; double from the side), and the impact smashes
        /// it, so the rows behind walk on.
        /// </summary>
        private void StepBarriers()
        {
            for (var i = _barriers.Count - 1; i >= 0 && !Over; i--)
            {
                var b = _barriers[i];
                if (b.Z < Hero.Z - SquadDepth - RunnerRules.DropBehind)
                {
                    _barriers.RemoveAt(i);
                    continue;
                }

                _touching.Clear();
                foreach (var m in _squad)
                {
                    if (b.Touches(m.X, m.Z, m.Unit.Radius))
                    {
                        _touching.Add(m);
                    }
                }

                if (_touching.Count == 0)
                {
                    continue;
                }

                _barriers.RemoveAt(i);
                BarriersSmashed++;
                BarrierHits += _touching.Count;
                var anySide = false;
                foreach (var m in _touching)
                {
                    anySide |= b.SideEntry(m.PrevX, m.PrevZ, m.Unit.Radius);
                }

                // OnBox flags a side crash (someone ran into its end sideways) so the scene can call it out.
                Emit(RunnerEventKind.BarrierBroken, b.Id, 0, b.X, b.Z, _touching.Count, true, anySide, RunnerBarrierDef.Name(b.Kind));
                foreach (var m in _touching)
                {
                    if (Over)
                    {
                        break;
                    }

                    var side = b.SideEntry(m.PrevX, m.PrevZ, m.Unit.Radius);
                    SideHits += side ? 1 : 0;
                    if (m.IsHero)
                    {
                        var dmg = SideDamage(b.Def.Damage, side);
                        Hero.Hp = Math.Max(0, Hero.Hp - dmg);
                        Emit(RunnerEventKind.HeroHit, Hero.Id, b.Id, Hero.X, Hero.Z, dmg, true, false, Hero.Unit.Id);
                        if (Hero.Hp <= 0)
                        {
                            Outcome = RunnerOutcome.Lost;
                            Emit(RunnerEventKind.Lost, Hero.Id, b.Id, Hero.X, Hero.Z, 0, true);
                        }

                        continue;
                    }

                    HurtFollower(b.Id, m, FollowerTrapDamage(b.Def.Damage, side));
                }
            }
        }

        private readonly List<RunnerMember> _touching = new();

        /// <summary>A broken box pays out: units join at the box and run into formation, or the squad is upgraded.</summary>
        private void Claim(RunnerBox b)
        {
            switch (b.Def.Kind)
            {
                case RunnerBoxKind.Weapon:
                    DamageMul += b.Def.Amount;
                    Emit(RunnerEventKind.Upgrade, b.Id, 0, b.X, b.Z, DamageMul, true, true, "weapon");
                    return;
                case RunnerBoxKind.Rate:
                    RateMul += b.Def.Amount;
                    Emit(RunnerEventKind.Upgrade, b.Id, 0, b.X, b.Z, RateMul, true, true, "rate");
                    return;
                case RunnerBoxKind.Range:
                    RangeMul += b.Def.Amount;
                    Emit(RunnerEventKind.Upgrade, b.Id, 0, b.X, b.Z, RangeMul, true, true, "range");
                    return;
                case RunnerBoxKind.Heal:
                {
                    var heal = Math.Max(1, (int)Math.Round(Hero.MaxHp * b.Def.Amount));
                    Hero.Hp = Math.Min(Hero.MaxHp, Hero.Hp + heal);
                    Emit(RunnerEventKind.Upgrade, b.Id, 0, b.X, b.Z, Hero.Hp, true, true, "heal");
                    return;
                }
            }

            var unit = _catalog.Unit(b.Def.Unit);
            var room = Math.Max(0, Math.Min(Level.MaxFollowers, RunnerRules.MaxFollowers) - Followers);
            var spare = Math.Max(0, b.Def.Count - room);
            for (var i = 0; i < b.Def.Count && Followers < Math.Min(Level.MaxFollowers, RunnerRules.MaxFollowers); i++)
            {
                // Joiners step out at the box's side of the road level with the squad (not out among the enemies
                // beyond it, now that the squad never stops) and run into their slot.
                var m = new RunnerMember(_nextId++, unit, false, b.X + ((i - ((b.Def.Count - 1) * 0.5f)) * 0.6f), Math.Min(b.Z, Hero.Z), Loadout.For(unit.Id));
                m.Cooldown = unit.Cycle * (0.2f + (0.13f * (_squad.Count % 6)));
                _squad.Add(m);
                Reslot();
                Emit(RunnerEventKind.FollowerJoined, m.Id, b.Id, m.X, m.Z, Followers, false, true, unit.Id);
            }

            FollowersPeak = Math.Max(FollowersPeak, Followers);
            if (spare > 0)
            {
                // The squad is full: the units that would not fit are paid as a damage boost, and the chest heals the hero.
                DamageMul += RunnerRules.FullSquadDamage * spare;
                var heal = Math.Max(1, (int)Math.Round(Hero.MaxHp * RunnerRules.FullSquadHeal));
                Hero.Hp = Math.Min(Hero.MaxHp, Hero.Hp + heal);
                Emit(RunnerEventKind.Upgrade, b.Id, 0, b.X, b.Z, DamageMul, true, true, "squad_full");
            }
        }

        private void Reslot()
        {
            for (var i = 1; i < _squad.Count; i++)
            {
                _squad[i].Slot = i - 1;
            }
        }

        // ---------- clean-up and end ----------

        private void DropPassed()
        {
            for (var i = _foes.Count - 1; i >= 0; i--)
            {
                var f = _foes[i];
                if (!f.Boss && f.Z < Hero.Z - SquadDepth - RunnerRules.DropBehind)
                {
                    _foes.RemoveAt(i);
                    Emit(RunnerEventKind.FoePassed, f.Id, 0, f.X, f.Z, 0, false, false, f.Unit.Id);
                }
            }

            for (var i = _boxes.Count - 1; i >= 0; i--)
            {
                var b = _boxes[i];
                if (b.Z < Hero.Z - RunnerRules.PassBehind)
                {
                    _boxes.RemoveAt(i);
                    BoxesMissed++;
                    Emit(RunnerEventKind.BoxMissed, b.Id, 0, b.X, b.Z, b.Hp, false, true, b.Def.Unit);
                }
            }
        }

        // ---------- lane traps ----------

        private void StepHazards(float dt)
        {
            for (var i = _hazards.Count - 1; i >= 0 && !Over; i--)
            {
                var h = _hazards[i];
                var was = h.State;
                h.PrevZ = h.Z;
                h.StateClock += dt;
                switch (h.Kind)
                {
                    case RunnerHazardKind.Spikes:
                    {
                        // On the run's clock (deterministic, staggered by the row's phase): down, rattle, up.
                        var t = (Seconds + h.Def.Phase) % RunnerRules.SpikeCycle;
                        h.State = t < RunnerRules.SpikeDown ? RunnerHazardState.Idle
                            : t < RunnerRules.SpikeDown + RunnerRules.SpikeWarn ? RunnerHazardState.Warning
                            : RunnerHazardState.Active;
                        break;
                    }

                    case RunnerHazardKind.Fire:
                        if (h.StateClock >= RunnerRules.FireFlare)
                        {
                            h.StateClock = 0f;
                            h.Hit.Clear();
                            h.Caught = 0;
                        }

                        break;

                    case RunnerHazardKind.Log:
                        if (h.State == RunnerHazardState.Idle && h.Z - Hero.Z <= RunnerRules.LogTrigger + h.Def.Phase)
                        {
                            h.State = RunnerHazardState.Warning;
                        }
                        else if (h.State == RunnerHazardState.Warning && h.StateClock >= RunnerRules.LogWarn)
                        {
                            h.State = RunnerHazardState.Active;
                        }
                        else if (h.State == RunnerHazardState.Active)
                        {
                            h.Z -= RunnerRules.LogSpeed * dt;
                        }

                        break;
                }

                if (h.State != was)
                {
                    h.StateClock = 0f;
                    if (h.State == RunnerHazardState.Active)
                    {
                        // A new activation: everyone can be caught again (once).
                        h.Hit.Clear();
                        h.Caught = 0;
                    }
                }

                if (h.Hurts)
                {
                    Catch(h);
                }

                if (h.Z < Hero.Z - SquadDepth - RunnerRules.DropBehind)
                {
                    _hazards.RemoveAt(i);
                }
            }
        }

        /// <summary>Whoever the trap touches (once each per activation): a follower is knocked out, the hero takes the damage.</summary>
        private void Catch(RunnerHazard h)
        {
            for (var i = _squad.Count - 1; i >= 0 && !Over; i--)
            {
                if (i >= _squad.Count)
                {
                    continue;
                }

                var m = _squad[i];
                if (h.Caught >= h.CatchCap)
                {
                    return;
                }

                if (h.Hit.Contains(m.Id) || !h.Touches(m.X, m.Z, m.Unit.Radius))
                {
                    continue;
                }

                h.Hit.Add(m.Id);
                h.Caught++;
                HazardHits++;
                var side = h.SideEntry(m.PrevX, m.PrevZ, m.Unit.Radius);
                SideHits += side ? 1 : 0;
                if (m.IsHero)
                {
                    var dmg = SideDamage(h.Def.Damage, side);
                    Hero.Hp = Math.Max(0, Hero.Hp - dmg);
                    Emit(RunnerEventKind.HazardHit, h.Id, m.Id, m.X, m.Z, dmg, true, side, RunnerHazardDef.Name(h.Kind));
                    Emit(RunnerEventKind.HeroHit, Hero.Id, h.Id, Hero.X, Hero.Z, dmg, true, false, Hero.Unit.Id);
                    if (Hero.Hp <= 0 && !Over)
                    {
                        Outcome = RunnerOutcome.Lost;
                        Emit(RunnerEventKind.Lost, Hero.Id, h.Id, Hero.X, Hero.Z, 0, true);
                    }

                    continue;
                }

                var bite = FollowerTrapDamage(h.Def.Damage, side);
                Emit(RunnerEventKind.HazardHit, h.Id, m.Id, m.X, m.Z, 0, false, side, RunnerHazardDef.Name(h.Kind));
                HurtFollower(h.Id, m, bite);
            }
        }

        private float SquadDepth => -RunnerFormation.Offset(Math.Max(0, Followers - 1)).Z;

        private void CheckEnd()
        {
            if (Over)
            {
                return;
            }

            var won = Level.HasBoss
                ? _nextRow >= Level.Rows.Count && !_foes.Exists(f => f.Boss)
                : Distance >= Level.Length - 1e-3f;
            if (won)
            {
                Outcome = RunnerOutcome.Won;
                Emit(RunnerEventKind.Won, Hero.Id, 0, Hero.X, Hero.Z, Kills, true);
            }
        }

        // ---------- helpers ----------

        /// <summary>
        /// A foe's aimed heading (unnormalised x, z) from (x, z) at a target standing at (tx, tz): straight at it, led by
        /// <see cref="RunnerRules.FoeShotLead"/> of the road scroll for the shot's flight time. Never its steering:
        /// aimed once at fire time and never homing, so moving out of the line dodges it.
        /// </summary>
        public static (float X, float Z) FoeAim(float x, float z, float tx, float tz, float shotSpeed, float roadSpeed)
        {
            var flight = (float)Math.Sqrt(Sq(tx - x) + Sq(tz - z)) / Math.Max(0.1f, shotSpeed);
            var lz = tz + (roadSpeed * flight * RunnerRules.FoeShotLead);
            var dx = tx - x;
            var dz = lz - z;
            if (Math.Abs(dz) < 1e-3f)
            {
                dz = tz >= z ? 1e-3f : -1e-3f;
            }

            return (dx, dz);
        }

        private RunnerMember? NearestMember(float x, float z, out float distance)
        {
            RunnerMember? best = null;
            var bestSq = float.MaxValue;
            foreach (var m in _squad)
            {
                var d = Sq(m.X - x) + Sq(m.Z - z);
                if (d < bestSq)
                {
                    bestSq = d;
                    best = m;
                }
            }

            distance = best != null ? (float)Math.Sqrt(bestSq) : float.MaxValue;
            return best;
        }

        private void Emit(RunnerEventKind kind, int id, int other, float x, float z, float amount, bool big, bool onBox = false, string unit = "") =>
            _events.Add(new RunnerEvent(kind, id, other, x, z, amount, big, onBox, unit));

        private static float Sq(float v) => v * v;

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }

    /// <summary>Follower slots behind the hero: row r (1, 2, 3 ...) holds r + 1 units, centred, <see cref="RunnerRules.Spacing"/> apart.</summary>
    /// <summary>Critically damped spring used for steering (the same maths as Unity's Mathf.SmoothDamp).</summary>
    public static class RunnerSteering
    {
        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float maxSpeed, float dt)
        {
            if (dt <= 0f)
            {
                return current;
            }

            smoothTime = Math.Max(0.0001f, smoothTime);
            var omega = 2f / smoothTime;
            var x = omega * dt;
            var exp = 1f / (1f + x + (0.48f * x * x) + (0.235f * x * x * x));
            var change = current - target;
            var original = target;
            var maxChange = maxSpeed * smoothTime;
            change = change < -maxChange ? -maxChange : change > maxChange ? maxChange : change;
            target = current - change;
            var temp = (velocity + (omega * change)) * dt;
            velocity = (velocity - (omega * temp)) * exp;
            var output = target + ((change + temp) * exp);
            if ((original - current > 0f) == (output > original))
            {
                output = original;
                velocity = (output - original) / dt;
            }

            return output;
        }
    }

    /// <summary>
    /// Where the road camera frames the squad. It rides halfway between the lead and the back row, pushed one row
    /// up the road (Derek 2026-10-23: "the heros can be pushed back a row. now that we limited it to 10 there's an
    /// empty row in the back"): with the squad capped at 10 the back row is row 3, and the bottom of the shot used to
    /// leave a row and a half of empty road behind it. Only the camera moves; the sim (and so the engage distance,
    /// shot range, the arena line and the quake lanes) is unchanged.
    /// </summary>
    public static class RunnerFraming
    {
        public const float CameraHeight = 7.6f;
        public const float CameraBack = 10.0f;
        public const float CameraLookAhead = 6.5f;
        public const float CameraLookHeight = 0.9f;
        public const float FieldOfViewDegrees = 52f;

        /// <summary>One formation row (see <see cref="RunnerFormation.Offset"/>).</summary>
        public const float RowMetres = RunnerRules.Spacing * 0.95f;

        /// <summary>Formation rows behind the lead in a full squad (<see cref="RunnerRules.MaxFollowers"/> followers).</summary>
        public static int FullRows => RunnerFormation.RowOf(RunnerRules.MaxFollowers - 1);

        /// <summary>
        /// Where the back row sits from the focus (metres, - = below/behind): the same spot whatever the squad's size.
        /// It is the full squad's framing (halfway between lead and back row, one row up the road), so ten heroes fill
        /// the shot exactly as before; a smaller squad keeps its rearmost row on that bottom spot and fills forward
        /// from it (Derek 2026-10-23: "the 2 in the back sit all the way down with the lead in front").
        /// </summary>
        public static float BackRowFromFocus => (-FullRows * RowMetres * 0.5f) - RowMetres;

        /// <summary>The focus's place along the road with the rearmost member at <paramref name="backZ"/> (bottom-anchored).</summary>
        public static float FocusZ(float backZ) => backZ - BackRowFromFocus;

        /// <summary>The rearmost member's place along the road (the lead's when it walks alone).</summary>
        public static float BackZ(IReadOnlyList<RunnerMember> squad, float leadZ)
        {
            var back = leadZ;
            for (var i = 0; i < squad.Count; i++)
            {
                back = Math.Min(back, squad[i].Z);
            }

            return back;
        }

        /// <summary>The back row's offset from the lead once a squad of <paramref name="members"/> has settled into its slots.</summary>
        public static float SettledBackOffset(int members)
        {
            var back = 0f;
            for (var slot = 0; slot < members - 1; slot++)
            {
                back = Math.Min(back, RunnerFormation.Offset(slot).Z);
            }

            return back;
        }

        /// <summary>Road place (metres from the focus, - = behind) where the bottom edge of the shot meets the ground.</summary>
        public static float BottomGroundFromFocus(bool boss)
        {
            var height = CameraHeight + (boss ? 0.8f : 0f);
            var back = CameraBack + (boss ? 1.4f : 0f);
            var lookHeight = CameraLookHeight + (boss ? 0.6f : 0f);
            var pitch = Math.Atan2(height - lookHeight, back + CameraLookAhead);
            var down = pitch + (FieldOfViewDegrees * 0.5 * Math.PI / 180.0);
            return (float)(-back + (height / Math.Tan(down)));
        }
    }

    public static class RunnerFormation
    {
        public static int RowOf(int slot)
        {
            var row = 1;
            var start = 0;
            while (slot >= start + row + 1)
            {
                start += row + 1;
                row++;
            }

            return row;
        }

        /// <summary>(x, z) offset from the hero; z is negative (behind).</summary>
        public static (float X, float Z) Offset(int slot)
        {
            if (slot < 0)
            {
                return (0f, 0f);
            }

            var row = 1;
            var start = 0;
            while (slot >= start + row + 1)
            {
                start += row + 1;
                row++;
            }

            var k = slot - start;
            var s = RunnerRules.Spacing;
            return ((k - (row * 0.5f)) * s, -row * s * 0.95f);
        }
    }
}
