#nullable enable
namespace Survival.Domain.Ota
{
    public enum UpdateRequirement
    {
        /// <summary>Installed version meets the minimum (or no minimum for this platform): play.</summary>
        None,

        /// <summary>Installed version is below min_supported_version: block play and prompt a store update.</summary>
        Required
    }

    /// <summary>
    /// The minimum-version check. Content and data travel over the air; C# code only ships through the stores. When a
    /// content or server change needs new code, raise min_supported_version and older installs are sent to the store.
    /// </summary>
    public static class MinVersionGate
    {
        /// <param name="platform">"android" or "ios"; any other platform (Editor, desktop) is never blocked.</param>
        /// <param name="installed">Application.version.</param>
        public static UpdateRequirement Decide(RemoteConfig? config, string platform, string installed)
        {
            if (config == null || !config.MinSupportedVersion.TryGetValue(platform, out var min))
            {
                return UpdateRequirement.None;
            }

            if (!AppVersion.TryParse(installed, out var have))
            {
                // An unparseable installed version cannot be compared; do not lock the player out on our own bug.
                return UpdateRequirement.None;
            }

            return have.CompareTo(min) < 0 ? UpdateRequirement.Required : UpdateRequirement.None;
        }

        /// <summary>The store page for <paramref name="platform"/>, or <paramref name="fallback"/>.</summary>
        public static string StoreLink(RemoteConfig? config, string platform, string fallback) =>
            config != null && config.StoreLinks.TryGetValue(platform, out var link) ? link : fallback;
    }
}
