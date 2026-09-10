using Grove.Domain.Producer;

namespace Grove.Domain.Tests;

public sealed class ScriptedRandom : IRandomSource
{
    private readonly int[] _values;
    private int _index;

    public ScriptedRandom(params int[] values) => _values = values;

    public static ScriptedRandom Always(int value) => new(value);

    public int Next(int maxExclusive)
    {
        var v = _values[_index % _values.Length];
        _index++;
        if (v < 0 || v >= maxExclusive)
        {
            return 0;
        }

        return v;
    }
}
