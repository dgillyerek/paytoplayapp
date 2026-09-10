using Grove.Domain.Time;

namespace Grove.Domain.Tests;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset start) => UtcNow = start;

    public DateTimeOffset UtcNow { get; set; }

    public void Advance(TimeSpan span) => UtcNow += span;
}
