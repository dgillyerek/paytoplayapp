#nullable enable
using System;

namespace Survival.Domain.Clock
{
    /// <summary>Seconds-based game clock. Every timer, producer and gift reads time through this.</summary>
    public interface IGameClock
    {
        double Now { get; }

        /// <summary>Debug time skip (adds to every later reading). Never used by shipping flows.</summary>
        void Skip(double seconds);
    }

    /// <summary>Wall clock (UTC epoch seconds) plus a debug skip offset.</summary>
    public sealed class WallClock : IGameClock
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public double Offset { get; private set; }

        public double Now => (DateTime.UtcNow - Epoch).TotalSeconds + Offset;

        public void Skip(double seconds)
        {
            if (seconds > 0)
            {
                Offset += seconds;
            }
        }
    }

    /// <summary>Hand-driven clock for tests and deterministic playback.</summary>
    public sealed class ManualClock : IGameClock
    {
        public ManualClock(double start = 0) => Now = start;

        public double Now { get; set; }

        public void Skip(double seconds)
        {
            if (seconds > 0)
            {
                Now += seconds;
            }
        }

        public void Advance(double seconds) => Skip(seconds);
    }
}
