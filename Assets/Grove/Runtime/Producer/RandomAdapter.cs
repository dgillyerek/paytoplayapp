using System;

namespace Grove.Domain.Producer
{
    public sealed class RandomAdapter : IRandomSource
    {
        private readonly Random _random;

        public RandomAdapter(int? seed = null)
        {
            _random = seed is int s ? new Random(s) : new Random();
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "maxExclusive must be > 0.");
            }

            return _random.Next(maxExclusive);
        }
    }
}
