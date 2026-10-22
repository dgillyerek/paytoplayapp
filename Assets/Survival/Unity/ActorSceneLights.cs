#nullable enable
namespace Survival.Unity
{
    /// <summary>
    /// The demo actors each light their own preview scene (a key + fill directional and the ambient) when they build. A
    /// scene that owns its lighting and builds many actors (the runner) sets <see cref="Suppressed"/> first: otherwise
    /// every rig adds two more directional lights (one with soft shadows) that never go away, and the ambient flips with
    /// whichever rig was built last.
    /// </summary>
    public static class ActorSceneLights
    {
        public static bool Suppressed { get; set; }
    }
}
