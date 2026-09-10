namespace Grove.Domain.Energy
{
    public sealed record EnergyConfig(
        int Cap,
        int RegenSecondsPerPoint,
        int ProduceCost,
        int DigCost,
        bool FtueTopUpToCap)
    {
        public static EnergyConfig Default { get; } = new(
            Cap: 100,
            RegenSecondsPerPoint: 120,
            ProduceCost: 1,
            DigCost: 1,
            FtueTopUpToCap: true);
    }

    public enum EnergySpendReason
    {
        Produce,
        Dig
    }
}
