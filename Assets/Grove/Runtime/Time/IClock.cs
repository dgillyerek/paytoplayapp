using System;

namespace Grove.Domain.Time
{
    /// <summary>Injectable clock so energy regen and crate recharge stay accurate in tests and while backgrounded.</summary>
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
