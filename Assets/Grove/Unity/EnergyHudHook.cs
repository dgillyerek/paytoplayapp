using Grove.Domain.Energy;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>2D HUD stub for an empty energy bar. Wire a prompt GameObject; domain calls this on empty/failed spend.</summary>
    public sealed class EnergyHudHook : MonoBehaviour, IEnergyListener
    {
        [SerializeField] private GameObject emptyPrompt = null!;

        public int LastCurrent { get; private set; } = -1;

        public int LastCap { get; private set; } = -1;

        public int EmptyCount { get; private set; }

        public void OnEnergyChanged(int current, int cap)
        {
            LastCurrent = current;
            LastCap = cap;
            if (emptyPrompt != null && current > 0)
            {
                emptyPrompt.SetActive(false);
            }
        }

        public void OnEnergyEmpty()
        {
            EmptyCount++;
            if (emptyPrompt != null)
            {
                emptyPrompt.SetActive(true);
            }
        }
    }
}
