using System;
using System.Collections.Generic;

namespace Grove.Domain.Producer
{
    public sealed record WeightedOutput(string ItemId, int Weight, bool NeverConsecutive);

    public sealed record ProducerDefinition(
        string Id,
        int MaxCharges,
        int RechargeSeconds,
        int EnergyPerTap,
        IReadOnlyList<WeightedOutput> Outputs);
}
