#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Roster;

namespace Survival.Domain.Runner
{
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

    /// <summary>
    /// A breakable barrier in play: the squad's shots chip it down (it stops them, like a box), and walking into it while it
    /// stands hurts whoever it touches and smashes it. It never moves; enemies walk round it (or wait behind one that spans
    /// the road), and their shots fly over it.
    /// </summary>
    public sealed class RunnerBarrier
    {
        internal RunnerBarrier(int id, RunnerBarrierDef def, float x, float z, int hp = 0)
        {
            Id = id;
            Def = def;
            X = x;
            Z = z;
            Hp = MaxHp = Math.Max(def.Hp, hp);
        }

        public int Id { get; }
        public RunnerBarrierDef Def { get; }
        public RunnerBarrierKind Kind => Def.Kind;
        public float X { get; }
        public float Z { get; }
        public int Hp { get; internal set; }
        public int MaxHp { get; }
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
}
