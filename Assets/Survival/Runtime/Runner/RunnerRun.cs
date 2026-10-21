#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Runner
{
    /// <summary>Tuning shared by every level (metres, seconds).</summary>
    public static class RunnerRules
    {
        /// <summary>Rows spawn this far ahead of the squad, so they are in view before they matter.</summary>
        public const float SpawnAhead = 30f;

        /// <summary>Enemies start walking at the squad once it is this close.</summary>
        public const float Aggro = 15f;

        /// <summary>Half width of the strip in front of each unit that it shoots into (plus the target's radius).</summary>
        public const float AimHalfWidth = 1.5f;

        /// <summary>Formation spacing between followers.</summary>
        public const float Spacing = 1.15f;

        /// <summary>The hero slides toward the steer point at most this fast.</summary>
        public const float SteerSpeed = 11f;

        /// <summary>Enemies drift sideways toward the hero this fast while they walk in.</summary>
        public const float EnemySeek = 0.55f;

        /// <summary>Enemies and boxes this far behind the hero are left behind (no kill, no box).</summary>
        public const float PassBehind = 1.2f;

        /// <summary>A hit at least this big (or any boss, box break, follower loss or hero hit) shakes the screen.</summary>
        public const int BigHit = 60;

        /// <summary>Shots land once they are this close to the target.</summary>
        public const float ShotHitRadius = 0.35f;

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
        Won,
        Lost
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
        internal RunnerMember(int id, RunnerUnitDef unit, bool hero, float x, float z)
        {
            Id = id;
            Unit = unit;
            IsHero = hero;
            X = x;
            Z = z;
            Hp = unit.Hp;
        }

        public int Id { get; }
        public RunnerUnitDef Unit { get; }
        public bool IsHero { get; }
        public float X { get; internal set; }
        public float Z { get; internal set; }
        public int Hp { get; internal set; }
        public int Slot { get; internal set; } = -1;
        public float Cooldown { get; internal set; }

        /// <summary>Id of the foe or box this member last attacked (0 = none), and how long ago.</summary>
        public int TargetId { get; internal set; }
        public float SinceAttack { get; internal set; } = 99f;
        public bool Attacking => SinceAttack < Unit.Cycle + 0.25f;
    }

    public sealed class RunnerFoe
    {
        internal RunnerFoe(int id, RunnerUnitDef unit, float x, float z, bool boss)
        {
            Id = id;
            Unit = unit;
            X = x;
            Z = z;
            Boss = boss;
            Hp = MaxHp = unit.Hp;
        }

        public int Id { get; }
        public RunnerUnitDef Unit { get; }
        public float X { get; internal set; }
        public float Z { get; internal set; }
        public bool Boss { get; }
        public int Hp { get; internal set; }
        public int MaxHp { get; }
        public float Cooldown { get; internal set; }
        public bool Walking { get; internal set; }
        public bool Engaged { get; internal set; }
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

    /// <summary>A shot in flight. It homes on its target; it fizzles if the target is gone first.</summary>
    public sealed class RunnerShot
    {
        internal RunnerShot(int id, int shooter, int target, bool onBox, bool byFoe, float x, float z, int damage, float speed)
        {
            Id = id;
            Shooter = shooter;
            Target = target;
            OnBox = onBox;
            ByFoe = byFoe;
            X = x;
            Z = z;
            Damage = damage;
            Speed = speed;
        }

        public int Id { get; }
        public int Shooter { get; }
        public int Target { get; }
        public bool OnBox { get; }
        public bool ByFoe { get; }
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
        float Seconds);

    /// <summary>
    /// One run of a runner level ("runner army"): the squad auto-walks down the lane at the level's pace and every unit
    /// auto-attacks what is in front of it (ranged shots or melee swings); the player only steers left / right. Enemy
    /// waves walk in; one in contact holds the squad up, and every enemy hit kills a follower first, the hero only once
    /// no followers are left. Bonus boxes have HP and must be shot down to be claimed: followers, a new hero, or
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
        private readonly List<RunnerEvent> _events = new();
        private int _nextRow;
        private int _nextId = 1;
        private float _steer;

        public RunnerRun(RunnerCatalog catalog, RunnerLevelDef level)
        {
            _catalog = catalog;
            Level = level;
            Hero = new RunnerMember(_nextId++, catalog.Unit(level.Hero), true, 0f, 0f);
            _squad.Add(Hero);
            SpawnRows();
        }

        public RunnerLevelDef Level { get; }
        public RunnerMember Hero { get; }

        /// <summary>Hero first, then followers in formation order.</summary>
        public IReadOnlyList<RunnerMember> Squad => _squad;
        public IReadOnlyList<RunnerFoe> Foes => _foes;
        public IReadOnlyList<RunnerBox> Boxes => _boxes;
        public IReadOnlyList<RunnerShot> Shots => _shots;

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

        public int Kills { get; private set; }
        public int BoxesBroken { get; private set; }
        public int BoxesMissed { get; private set; }

        /// <summary>True while a foe in contact (or the boss) holds the squad up.</summary>
        public bool Blocked { get; private set; }
        public RunnerOutcome Outcome { get; private set; }
        public bool Over => Outcome != RunnerOutcome.Running;

        public RunnerResult Result =>
            new RunnerResult(Level.Id, Outcome == RunnerOutcome.Won, Kills, BoxesBroken, BoxesMissed, FollowersPeak, Followers, Distance, Seconds);

        public float MaxSteer => Math.Max(0.5f, Level.HalfWidth - Hero.Unit.Radius);

        /// <summary>Steer the hero to lane position <paramref name="x"/> (metres from the centre line, clamped to the lane).</summary>
        public void SteerTo(float x) => _steer = Clamp(x, -MaxSteer, MaxSteer);

        public void SteerBy(float dx) => SteerTo(_steer + dx);

        public RunnerMember? Member(int id) => _squad.Find(m => m.Id == id);
        public RunnerFoe? Foe(int id) => _foes.Find(f => f.Id == id);
        public RunnerBox? Box(int id) => _boxes.Find(b => b.Id == id);

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
            DropPassed();
            CheckEnd();
        }

        // ---------- movement ----------

        private void MoveSquad(float dt)
        {
            Blocked = false;
            foreach (var f in _foes)
            {
                if (f.Engaged && (f.Boss || f.Z >= Hero.Z - 0.3f))
                {
                    Blocked = true;
                    break;
                }
            }

            if (!Blocked)
            {
                Distance = Math.Min(Level.Length, Distance + (Level.Speed * dt));
            }

            var maxMove = RunnerRules.SteerSpeed * dt;
            Hero.X += Clamp(_steer - Hero.X, -maxMove, maxMove);
            Hero.Z = Distance;
            for (var i = 1; i < _squad.Count; i++)
            {
                var m = _squad[i];
                var (sx, sz) = RunnerFormation.Offset(m.Slot);
                var row = RunnerFormation.RowOf(m.Slot);
                // Back rows lag more, so the formation bends as the hero steers.
                var lag = 1f - (float)Math.Exp(-dt * (9f / (1f + (0.35f * row))));
                var tx = Clamp(Hero.X + sx, -Level.HalfWidth + m.Unit.Radius, Level.HalfWidth - m.Unit.Radius);
                m.X += (tx - m.X) * lag;
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

                    default:
                    {
                        var unit = _catalog.Unit(row.Unit);
                        var boss = row.Kind == RunnerRowKind.Boss;
                        for (var i = 0; i < row.Count; i++)
                        {
                            var x = Clamp(row.XOf(i), -Level.HalfWidth + unit.Radius, Level.HalfWidth - unit.Radius);
                            var foe = new RunnerFoe(_nextId++, unit, x, row.At, boss);
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
                if (f.Boss)
                {
                    // The boss holds the whole road: no going round it.
                    var gap = f.Z - Hero.Z;
                    f.Engaged = gap <= reach;
                    if (f.Engaged && !wasEngaged)
                    {
                        Emit(RunnerEventKind.BossEngaged, f.Id, Hero.Id, f.X, f.Z, f.Hp, true);
                    }
                }
                else
                {
                    // Contact is body to body: a ranged foe shoots from range but only holds the squad up once it is touched.
                    f.Engaged = near != null && dist <= ContactReach(f) + (near?.Unit.Radius ?? 0f);
                }

                f.Walking = false;
                var holding = f.Unit.Ranged && near != null && dist <= reach * 0.8f;
                if (f.Engaged || holding || f.Unit.Speed <= 0f || near == null)
                {
                    continue;
                }

                if (f.Z - Hero.Z > RunnerRules.Aggro)
                {
                    continue;
                }

                f.Walking = true;
                // Walk in down the lane and drift toward the hero's line.
                f.Z -= f.Unit.Speed * dt;
                var seek = RunnerRules.EnemySeek * dt;
                f.X += Clamp(Hero.X - f.X, -seek, seek);
                f.X = Clamp(f.X, -Level.HalfWidth + f.Unit.Radius, Level.HalfWidth - f.Unit.Radius);
            }
        }

        private static float ContactReach(RunnerFoe f) => (f.Unit.Ranged ? 0.6f : f.Unit.Range) + f.Unit.Radius;

        // ---------- attacks ----------

        private void SquadAttacks(float dt)
        {
            foreach (var m in _squad)
            {
                m.SinceAttack += dt;
                m.Cooldown -= dt;
                if (m.Cooldown > 0f)
                {
                    continue;
                }

                if (!FindTarget(m, out var targetId, out var onBox, out var tx, out var tz))
                {
                    m.Cooldown = 0f;
                    continue;
                }

                m.Cooldown += m.Unit.Cycle / RateMul;
                if (m.Cooldown < 0f)
                {
                    m.Cooldown = 0f;
                }

                m.TargetId = targetId;
                m.SinceAttack = 0f;
                var damage = (int)Math.Round(m.Unit.Damage * DamageMul);
                if (m.Unit.Ranged)
                {
                    var shot = new RunnerShot(_nextId++, m.Id, targetId, onBox, false, m.X, m.Z + 0.4f, damage, m.Unit.ShotSpeed);
                    _shots.Add(shot);
                    Emit(RunnerEventKind.Fired, m.Id, targetId, tx, tz, shot.Id, false, onBox, m.Unit.Id);
                }
                else
                {
                    Emit(RunnerEventKind.Fired, m.Id, targetId, tx, tz, 0, false, onBox, m.Unit.Id);
                    Damage(targetId, onBox, damage, m.Id);
                }
            }
        }

        /// <summary>Nearest enemy in front (in range, inside the aim strip) first, else the nearest box.</summary>
        private bool FindTarget(RunnerMember m, out int id, out bool onBox, out float x, out float z)
        {
            id = 0;
            onBox = false;
            x = z = 0f;
            var best = float.MaxValue;
            foreach (var f in _foes)
            {
                // The boss fills the road, so everyone can reach it from any lane position.
                var x0 = f.Boss ? m.X : f.X;
                if (f.Hp <= 0 || !InStrike(m, x0, f.Z, f.Unit.Radius, out var d) || d >= best)
                {
                    continue;
                }

                best = d;
                id = f.Id;
                x = f.X;
                z = f.Z;
            }

            if (id != 0)
            {
                return true;
            }

            foreach (var b in _boxes)
            {
                if (b.Hp <= 0 || !InStrike(m, b.X, b.Z, RunnerBox.Radius, out var d) || d >= best)
                {
                    continue;
                }

                best = d;
                id = b.Id;
                x = b.X;
                z = b.Z;
                onBox = true;
            }

            return id != 0;
        }

        private static bool InStrike(RunnerMember m, float x, float z, float radius, out float distance)
        {
            var dx = x - m.X;
            var dz = z - m.Z;
            distance = (float)Math.Sqrt((dx * dx) + (dz * dz));
            if (m.Unit.Ranged)
            {
                return dz > -0.5f && Math.Abs(dx) <= RunnerRules.AimHalfWidth + radius && distance <= m.Unit.Range + radius;
            }

            return dz > -1f && distance <= m.Unit.Range + radius + m.Unit.Radius;
        }

        private void FoesAttack(float dt)
        {
            foreach (var f in _foes)
            {
                f.SinceAttack += dt;
                if (f.Hp <= 0)
                {
                    continue;
                }

                f.Cooldown -= dt;
                var target = NearestMember(f.X, f.Z, out var dist);
                if (target == null)
                {
                    continue;
                }

                var inRange = f.Boss
                    ? f.Engaged
                    : f.Unit.Ranged
                        ? dist <= f.Unit.Range + f.Unit.Radius + target.Unit.Radius && Math.Abs(target.X - f.X) <= RunnerRules.AimHalfWidth + target.Unit.Radius
                        : f.Engaged;
                if (!inRange || target.Z > f.Z + 1.5f)
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
                Emit(RunnerEventKind.Fired, f.Id, target.Id, target.X, target.Z, 0, false, false, f.Unit.Id);
                if (f.Unit.Ranged)
                {
                    _shots.Add(new RunnerShot(_nextId++, f.Id, target.Id, false, true, f.X, f.Z - 0.4f, f.Unit.Damage, f.Unit.ShotSpeed));
                }
                else
                {
                    StrikeSquad(f, f.Unit.Damage);
                }
            }
        }

        /// <summary>An enemy hit: kills the follower nearest the enemy; with no followers left it hurts the hero.</summary>
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
                _squad.Remove(victim);
                Emit(RunnerEventKind.FollowerLost, victim.Id, f.Id, victim.X, victim.Z, Followers, true, false, victim.Unit.Id);
                Reslot();
                return;
            }

            Hero.Hp = Math.Max(0, Hero.Hp - damage);
            Emit(RunnerEventKind.HeroHit, Hero.Id, f.Id, Hero.X, Hero.Z, damage, true, false, Hero.Unit.Id);
            if (Hero.Hp <= 0)
            {
                Outcome = RunnerOutcome.Lost;
                Emit(RunnerEventKind.Lost, Hero.Id, f.Id, Hero.X, Hero.Z, 0, true);
            }
        }

        private void MoveShots(float dt)
        {
            for (var i = _shots.Count - 1; i >= 0; i--)
            {
                var s = _shots[i];
                float tx, tz;
                if (s.ByFoe)
                {
                    var m = Member(s.Target) ?? NearestMember(s.X, s.Z, out _);
                    if (m == null)
                    {
                        _shots.RemoveAt(i);
                        continue;
                    }

                    tx = m.X;
                    tz = m.Z;
                }
                else if (s.OnBox)
                {
                    var b = Box(s.Target);
                    if (b == null || b.Hp <= 0)
                    {
                        _shots.RemoveAt(i);
                        continue;
                    }

                    tx = b.X;
                    tz = b.Z;
                }
                else
                {
                    var f = Foe(s.Target);
                    if (f == null || f.Hp <= 0)
                    {
                        _shots.RemoveAt(i);
                        continue;
                    }

                    tx = f.X;
                    tz = f.Z;
                }

                var dx = tx - s.X;
                var dz = tz - s.Z;
                var d = (float)Math.Sqrt((dx * dx) + (dz * dz));
                var step = s.Speed * dt;
                if (d <= step + RunnerRules.ShotHitRadius)
                {
                    _shots.RemoveAt(i);
                    s.X = tx;
                    s.Z = tz;
                    if (s.ByFoe)
                    {
                        var shooter = Foe(s.Shooter);
                        if (shooter != null)
                        {
                            StrikeSquad(shooter, s.Damage);
                        }
                    }
                    else
                    {
                        Damage(s.Target, s.OnBox, s.Damage, s.Shooter);
                    }

                    if (Over)
                    {
                        return;
                    }

                    continue;
                }

                s.X += dx / d * step;
                s.Z += dz / d * step;
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
            }

            var unit = _catalog.Unit(b.Def.Unit);
            for (var i = 0; i < b.Def.Count && Followers < Level.MaxFollowers; i++)
            {
                var m = new RunnerMember(_nextId++, unit, false, b.X + ((i - ((b.Def.Count - 1) * 0.5f)) * 0.6f), b.Z);
                m.Cooldown = unit.Cycle * (0.2f + (0.13f * (_squad.Count % 6)));
                _squad.Add(m);
                Reslot();
                Emit(RunnerEventKind.FollowerJoined, m.Id, b.Id, m.X, m.Z, Followers, false, true, unit.Id);
            }

            FollowersPeak = Math.Max(FollowersPeak, Followers);
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
                if (!f.Boss && f.Z < Hero.Z - RunnerRules.PassBehind - SquadDepth && !f.Engaged)
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
