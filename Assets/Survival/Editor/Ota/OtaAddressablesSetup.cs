#nullable enable
#if SURVIVAL_ADDRESSABLES
using System;
using System.Collections.Generic;
using Survival.Domain.Ota;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Survival.Editor.Ota
{
    /// <summary>
    /// Idempotent Addressables setup for over-the-air content (Docs/OTA.md). Creates or repairs:
    /// <list type="bullet">
    /// <item>the four remote groups OTA-Characters / OTA-Animations / OTA-Art / OTA-ThemePack (packed together per group,
    /// LZ4, hashed bundle names, CRC checked, cached on the device, not static, so a full content build replaces them);</item>
    /// <item>profile values Remote.BuildPath = ServerData/[BuildTarget] and Remote.LoadPath =
    /// https://{Survival.Unity.Ota.OtaHost.CdnHost}/content/[BuildTarget]/{Survival.Unity.Ota.OtaHost.ContentVersion}
    /// (curly = evaluated on the phone, so the host and content version are swappable without a rebuild);</item>
    /// <item>a remote catalog, "only update catalogs manually" (OtaBoot checks at boot), a fixed player version "ota" so
    /// catalog file names stay stable across content builds, and an 8 s catalog request timeout.</item>
    /// </list>
    /// Run from Survival/OTA/Set Up Addressables once and commit Assets/AddressableAssetsData; CI runs it on every build.
    /// </summary>
    public static class OtaAddressablesSetup
    {
        public const string SettingsFolder = "Assets/AddressableAssetsData";
        public const string RemoteBuildPathValue = "ServerData/[BuildTarget]";
        public const string RemoteLoadPathValue = "https://{Survival.Unity.Ota.OtaHost.CdnHost}/content/[BuildTarget]/{Survival.Unity.Ota.OtaHost.ContentVersion}";
        public const string PlayerVersion = "ota";

        [MenuItem("Survival/OTA/Set Up Addressables")]
        public static void SetUpMenu()
        {
            var settings = Ensure();
            EditorUtility.DisplayDialog("Survival OTA", "Addressables set up: " + settings.groups.Count + " groups, remote catalog at\n" + RemoteLoadPathValue, "OK");
        }

        public static AddressableAssetSettings Ensure()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                settings = AddressableAssetSettings.Create(SettingsFolder, "AddressableAssetSettings", true, true);
                AddressableAssetSettingsDefaultObject.Settings = settings;
            }

            var profiles = settings.profileSettings;
            var names = profiles.GetVariableNames();
            if (!names.Contains(AddressableAssetSettings.kRemoteBuildPath))
            {
                profiles.CreateValue(AddressableAssetSettings.kRemoteBuildPath, RemoteBuildPathValue);
            }

            if (!names.Contains(AddressableAssetSettings.kRemoteLoadPath))
            {
                profiles.CreateValue(AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPathValue);
            }

            foreach (var profileName in profiles.GetAllProfileNames())
            {
                var id = profiles.GetProfileId(profileName);
                profiles.SetValue(id, AddressableAssetSettings.kRemoteBuildPath, RemoteBuildPathValue);
                profiles.SetValue(id, AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPathValue);
            }

            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            settings.DisableCatalogUpdateOnStartup = true;
            settings.OverridePlayerVersion = PlayerVersion;
            settings.CatalogRequestsTimeout = 8;
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

            AddressableAssetGroup? themePack = null;
            foreach (var g in OtaContentGroups.All)
            {
                var group = EnsureGroup(settings, OtaContentGroups.GroupName(g));
                if (g == OtaContentGroup.ThemePack)
                {
                    themePack = group;
                }
            }

            // Unity's built-in shader bundle follows the default group: keep it remote with the rest.
            if (themePack != null)
            {
                settings.DefaultGroup = themePack;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string name)
        {
            var group = settings.FindGroup(name) ??
                        settings.CreateGroup(name, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var bundled = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            bundled.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bundled.UseAssetBundleCache = true;
            bundled.UseAssetBundleCrc = true;
            bundled.IncludeInBuild = true;
            var update = group.GetSchema<ContentUpdateGroupSchema>() ?? group.AddSchema<ContentUpdateGroupSchema>();
            update.StaticContent = false;
            EditorUtility.SetDirty(group);
            return group;
        }
    }
}
#endif
