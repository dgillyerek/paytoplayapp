#nullable enable
using UnityEngine.Scripting;

namespace Survival.Unity.Ota
{
    /// <summary>
    /// Values the Addressables profile reads at runtime. The OTA groups' load path is
    /// <c>https://{Survival.Unity.Ota.OtaHost.CdnHost}/content/[BuildTarget]/{Survival.Unity.Ota.OtaHost.ContentVersion}</c>
    /// (curly braces = evaluated on the phone, square brackets = at build time), so the host comes from
    /// Resources/Ota/ota_settings.json and the content folder from the remote config's content_version. Changing either
    /// needs no rebuild; rolling back is pointing content_version at an older folder. Set by <see cref="OtaBoot"/>
    /// before Addressables initializes; never read before that.
    /// </summary>
    [Preserve]
    public static class OtaHost
    {
        /// <summary>CDN host name (no scheme). ".invalid" (RFC 2606) until set, so a stray early load cannot reach anything.</summary>
        [Preserve]
        public static string CdnHost { get; internal set; } = "ota.invalid";

        /// <summary>Remote content folder (remote config content_version).</summary>
        [Preserve]
        public static string ContentVersion { get; internal set; } = "none";
    }
}
