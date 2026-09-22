using System;
using Survival.Domain.Catalog;

namespace Survival.Domain.Energy
{
    public sealed class EnergySystem
    {
        public EnergySystem(EnergyConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Current = config.Cap;
        }

        public EnergyConfig Config { get; }
        public int Current { get; private set; }

        public bool TrySpend(string actionId)
        {
            if (!Config.ActionCosts.TryGetValue(actionId, out var cost))
            {
                return false;
            }

            if (Current < cost)
            {
                return false;
            }

            Current -= cost;
            return true;
        }
    }
}
