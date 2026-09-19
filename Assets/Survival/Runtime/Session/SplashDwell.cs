using System;

namespace Survival.Domain.Session
{
    /// <summary>
    /// Splash presentation gate: player always sees <see cref="MinSeconds"/> after splash start
    /// once content is ready (wait out the remainder; if load runs long, advance when ready).
    /// </summary>
    public static class SplashDwell
    {
        public const float MinSeconds = 5f;
        public const float DotStepSeconds = 0.4f;

        public static bool CanAdvance(float elapsedSeconds, bool contentReady) =>
            contentReady && elapsedSeconds + 0.0001f >= MinSeconds;

        public static float WaitRemaining(float elapsedSeconds, bool contentReady)
        {
            if (!contentReady)
            {
                return MinSeconds;
            }

            var left = MinSeconds - elapsedSeconds;
            return left > 0f ? left : 0f;
        }

        public static string FormatLoading(string stem, float elapsedSeconds, float stepSeconds = DotStepSeconds)
        {
            if (string.IsNullOrEmpty(stem))
            {
                stem = "Loading";
            }

            if (stepSeconds <= 0f)
            {
                stepSeconds = DotStepSeconds;
            }

            var n = ((int)Math.Floor(elapsedSeconds / stepSeconds) % 3) + 1;
            return stem + new string('.', n);
        }
    }
}
