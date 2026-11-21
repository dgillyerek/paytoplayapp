#nullable enable
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>Marks a RectTransform that <see cref="HudAspectFit"/> must leave alone (it places itself, e.g. by the safe area).</summary>
    [DisallowMultipleComponent]
    public sealed class HudFitIgnore : MonoBehaviour
    {
    }
}
