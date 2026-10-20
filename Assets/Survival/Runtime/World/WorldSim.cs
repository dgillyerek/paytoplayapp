#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.World
{
    public enum MarchLeg
    {
        Outbound,
        Return
    }

    public enum MarchSendResult
    {
        Ok,
        UnknownTarget,
        OwnVillage,
        AlreadyMarching,
        QueueFull
    }

    /// <summary>One troop column on the map: straight line from -> to over [Start, Start + Duration] (clock seconds).</summary>
    public sealed class WorldMarch
    {
        internal WorldMarch(int serial, string ownerId, string targetId, bool isPlayer, MarchLeg leg, string tag, float fromX, float fromY, float toX, float toY, double start, double duration)
        {
            Serial = serial;
            OwnerId = ownerId;
            TargetId = targetId;
            IsPlayer = isPlayer;
            Leg = leg;
            Tag = tag;
            FromX = fromX;
            FromY = fromY;
            ToX = toX;
            ToY = toY;
            Start = start;
            Duration = Math.Max(0.01, duration);
        }

        /// <summary>Stable per-column id: out and back legs of one trip share it.</summary>
        public int Serial { get; }
        public string Id => "march." + Serial;
        public string OwnerId { get; }
        public string TargetId { get; }
        public bool IsPlayer { get; }
        public MarchLeg Leg { get; }

        /// <summary>Why it marches: attack / gather / scout / node (catalog loop) / patrol (mock).</summary>
        public string Tag { get; }

        public float FromX { get; }
        public float FromY { get; }
        public float ToX { get; }
        public float ToY { get; }
        public double Start { get; }
        public double Duration { get; }
        public double Arrival => Start + Duration;

        /// <summary>Mock columns rest at home between trips; hidden while waiting.</summary>
        public bool IsWaiting(double now) => now < Start;

        public float Progress(double now) => (float)Math.Min(1.0, Math.Max(0.0, (now - Start) / Duration));

        public double Remaining(double now) => Math.Max(0.0, Arrival - Math.Max(now, Start));

        public (float X, float Y) Position(double now)
        {
            var t = Progress(now);
            return (FromX + (ToX - FromX) * t, FromY + (ToY - FromY) * t);
        }

        /// <summary>Marker art faces right; flip it when walking west.</summary>
        public bool FacingLeft => ToX < FromX;
    }

    /// <summary>
    /// Real-time mock of the kingdom map: rival / ally columns loop between their village and scripted targets,
    /// and the player sends columns from their village (one per target, up to the march queue). Arrivals are
    /// processed in time order inside <see cref="Tick"/>, so a debug clock skip still resolves everything.
    /// No randomness: the same layout and clock always give the same map.
    /// </summary>
    public sealed class WorldSim
    {
        /// <summary>Mock columns rest this long at home before the next trip.</summary>
        public const double RestSeconds = 4.0;

        private const int MaxStepsPerTick = 4096;

        private readonly Dictionary<string, WorldEntity> _byId = new(StringComparer.Ordinal);
        private readonly List<WorldMarch> _marches = new();
        private int _serial;

        public WorldSim(WorldLayout layout, double now)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            foreach (var e in layout.Entities)
            {
                _byId[e.Id] = e;
            }

            Player = layout.Player ?? throw new ArgumentException("Layout has no player village.", nameof(layout));
            Now = now;
            foreach (var route in layout.Routes)
            {
                if (!_byId.TryGetValue(route.OwnerId, out var owner) || !_byId.TryGetValue(route.TargetId, out var target))
                {
                    continue;
                }

                // Start columns part-way so the map is already busy on first look.
                var seconds = MarchSeconds(owner, target);
                var offset = route.OffsetSeconds % (seconds * 2.0 + RestSeconds);
                _marches.Add(new WorldMarch(++_serial, owner.Id, target.Id, false, MarchLeg.Outbound, "patrol", owner.X, owner.Y, target.X, target.Y, now - offset, seconds));
            }

            Tick(now);
        }

        public WorldLayout Layout { get; }
        public WorldEntity Player { get; }
        public double Now { get; private set; }

        public IReadOnlyList<WorldMarch> Marches => _marches;

        public IEnumerable<WorldEntity> Entities => _byId.Values;

        /// <summary>A player column reached its target (resolve rewards here); the return leg has been queued.</summary>
        public event Action<WorldMarch>? Arrived;

        /// <summary>A player column is home again (its queue slot is free).</summary>
        public event Action<WorldMarch>? Returned;

        public bool TryGet(string id, out WorldEntity entity) => _byId.TryGetValue(id, out entity!);

        public int PlayerMarchCount
        {
            get
            {
                var n = 0;
                foreach (var m in _marches)
                {
                    if (m.IsPlayer)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public double MarchSeconds(WorldEntity from, WorldEntity to)
        {
            var speed = Math.Max(1f, Layout.MarchSpeed);
            var s = from.DistanceTo(to) / speed;
            return Math.Min(Layout.MaxMarchSeconds, Math.Max(Layout.MinMarchSeconds, s));
        }

        public MarchSendResult CanSend(string targetId)
        {
            if (!_byId.TryGetValue(targetId, out var target))
            {
                return MarchSendResult.UnknownTarget;
            }

            if (target.Kind == WorldEntityKind.PlayerVillage)
            {
                return MarchSendResult.OwnVillage;
            }

            foreach (var m in _marches)
            {
                if (m.IsPlayer && string.Equals(m.TargetId, targetId, StringComparison.Ordinal))
                {
                    return MarchSendResult.AlreadyMarching;
                }
            }

            return PlayerMarchCount >= Math.Max(1, Layout.MaxPlayerMarches) ? MarchSendResult.QueueFull : MarchSendResult.Ok;
        }

        /// <summary>Send a player column from the player's village. <paramref name="seconds"/> overrides the distance time (catalog loops).</summary>
        public MarchSendResult SendPlayerMarch(string targetId, string tag, out WorldMarch? march, double? seconds = null)
        {
            march = null;
            var check = CanSend(targetId);
            if (check != MarchSendResult.Ok)
            {
                return check;
            }

            var target = _byId[targetId];
            var duration = seconds ?? MarchSeconds(Player, target);
            march = new WorldMarch(++_serial, Player.Id, target.Id, true, MarchLeg.Outbound, tag, Player.X, Player.Y, target.X, target.Y, Now, duration);
            _marches.Add(march);
            return MarchSendResult.Ok;
        }

        /// <summary>The player's column heading to (or back from) <paramref name="targetId"/>, if any.</summary>
        public WorldMarch? PlayerMarchTo(string targetId)
        {
            foreach (var m in _marches)
            {
                if (m.IsPlayer && string.Equals(m.TargetId, targetId, StringComparison.Ordinal))
                {
                    return m;
                }
            }

            return null;
        }

        public void Tick(double now)
        {
            if (now < Now)
            {
                return;
            }

            Now = now;
            for (var step = 0; step < MaxStepsPerTick; step++)
            {
                var next = EarliestArrived(now);
                if (next == null)
                {
                    return;
                }

                _marches.Remove(next);
                Land(next);
            }
        }

        private WorldMarch? EarliestArrived(double now)
        {
            WorldMarch? best = null;
            foreach (var m in _marches)
            {
                if (m.Arrival <= now && (best == null || m.Arrival < best.Arrival))
                {
                    best = m;
                }
            }

            return best;
        }

        private void Land(WorldMarch m)
        {
            if (m.Leg == MarchLeg.Outbound)
            {
                var back = new WorldMarch(m.Serial, m.OwnerId, m.TargetId, m.IsPlayer, MarchLeg.Return, m.Tag, m.ToX, m.ToY, m.FromX, m.FromY, m.Arrival, m.IsPlayer ? Math.Min(m.Duration, Layout.MaxMarchSeconds) : m.Duration);
                _marches.Add(back);
                if (m.IsPlayer)
                {
                    Arrived?.Invoke(m);
                }

                return;
            }

            if (m.IsPlayer)
            {
                Returned?.Invoke(m);
                return;
            }

            // Mock column: rest, then march the same route again.
            _marches.Add(new WorldMarch(m.Serial, m.OwnerId, m.TargetId, false, MarchLeg.Outbound, m.Tag, m.ToX, m.ToY, m.FromX, m.FromY, m.Arrival + RestSeconds, m.Duration));
        }
    }
}
