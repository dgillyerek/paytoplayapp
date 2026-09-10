using Grove.Domain.Energy;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>Ticks the energy wallet so the HUD reflects offline/idle regen without a crate tap.</summary>
    public sealed class EnergyTicker : MonoBehaviour
    {
        public EnergyWallet? Wallet { get; set; }

        private void Update() => Wallet?.Tick();
    }
}
