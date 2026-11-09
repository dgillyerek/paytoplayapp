#nullable enable
using System;

namespace Survival.Domain.Analytics
{
    /// <summary>
    /// Vendor-agnostic crash reporting. Soft-launch default is <see cref="NoOpCrashReporter"/>.
    /// Under-13 / unknown age must stay off (see <see cref="KidGatedCrashReporter"/>).
    /// </summary>
    public interface ICrashReporter
    {
        void Report(Exception error);
    }

    public sealed class NoOpCrashReporter : ICrashReporter
    {
        public static readonly NoOpCrashReporter Instance = new();

        public void Report(Exception error)
        {
        }
    }
}
