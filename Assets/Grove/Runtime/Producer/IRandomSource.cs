namespace Grove.Domain.Producer
{
    /// <summary>Integer RNG used by weighted spit tables. Tests inject a scripted source.</summary>
    public interface IRandomSource
    {
        /// <summary>Returns a value in <c>[0, maxExclusive)</c>.</summary>
        int Next(int maxExclusive);
    }
}
