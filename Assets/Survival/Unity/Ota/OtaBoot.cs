#nullable enable
using System;
using System.Collections;
using Survival.Domain.Ota;
using UnityEngine;

namespace Survival.Unity.Ota
{
    /// <summary>What the OTA boot decided; <see cref="SurvivalBootstrap"/> acts on it.</summary>
    public sealed class OtaBootResult
    {
        public RemoteConfig Config { get; set; } = null!;

        public RemoteConfigSource Source { get; set; }

        public UpdateRequirement Update { get; set; }

        public string StoreUrl { get; set; } = "";

        public OtaContentOutcome Content { get; set; }

        public FreePaceVerdict Balance { get; set; }

        /// <summary>True when anything PlayerAssets serves changed (overlays or balance patches): reload pack and art.</summary>
        public bool ContentChanged => Content == OtaContentOutcome.Overlaid || Balance == FreePaceVerdict.Accepted;
    }

    /// <summary>
    /// Over-the-air step of the splash, before the session loads:
    /// 1. remote config: GET from the CDN (cdn_host in Resources/Ota/ota_settings.json), else the last good copy, else the
    ///    copy baked into the build; validated by <see cref="RemoteConfigParser"/>;
    /// 2. minimum version: below min_supported_version for this platform, stop and prompt a store update;
    /// 3. balance patches: applied to catalog reads only when <see cref="FreePaceGuard"/> accepts them (locked free pace);
    /// 4. content: Addressables remote catalog + bundle download with progress (<see cref="OtaContentUpdater"/>).
    /// Every step falls back (cached / bundled) on error or timeout; an unset host skips all network and runs on baked data.
    /// Only content and data are downloaded, never code.
    /// </summary>
    public static class OtaBoot
    {
        public const string SettingsResource = "Ota/ota_settings";
        public const string BakedConfigResource = "Ota/remote_config.default";

        public static IEnumerator Run(string appId, Action<float, string> progress, Action<OtaBootResult> done)
        {
            OtaSettings settings;
            string baked;
            try
            {
                var s = Resources.Load<TextAsset>(SettingsResource);
                settings = s != null ? OtaSettings.Parse(s.text) : new OtaSettings();
                var b = Resources.Load<TextAsset>(BakedConfigResource);
                baked = b != null ? b.text : throw new InvalidOperationException("Resources/" + BakedConfigResource + ".json is missing.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                done(new OtaBootResult { Config = new RemoteConfig { AppId = appId }, Source = RemoteConfigSource.Baked, Content = OtaContentOutcome.Skipped });
                yield break;
            }

            // 1. Remote config.
            progress(OtaBootProgress.Bar(OtaBootStep.Config, 0f), OtaBootProgress.Label(OtaBootStep.Config, 0, 0));
            string? fresh = null;
            if (settings.Enabled && !string.IsNullOrEmpty(InAppUpdate.Platform))
            {
                yield return RemoteConfigClient.Fetch(settings.ConfigUrl(InAppUpdate.Platform), settings.ConfigTimeoutSeconds, RemoteConfigParser.MaxBytes, body => fresh = body);
            }
            else if (settings.Enabled)
            {
                // Editor / desktop: android's config, so the Editor sees what phones see.
                yield return RemoteConfigClient.Fetch(settings.ConfigUrl("android"), settings.ConfigTimeoutSeconds, RemoteConfigParser.MaxBytes, body => fresh = body);
            }

            RemoteConfigChoice choice;
            try
            {
                choice = RemoteConfigSelector.Pick(fresh, RemoteConfigClient.ReadCache(), baked, appId);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                done(new OtaBootResult { Config = new RemoteConfig { AppId = appId }, Source = RemoteConfigSource.Baked, Content = OtaContentOutcome.Skipped });
                yield break;
            }

            foreach (var note in choice.Notes)
            {
                Debug.LogWarning("OTA: " + note);
            }

            if (choice.SaveFresh && fresh != null)
            {
                RemoteConfigClient.WriteCache(fresh);
            }

            progress(OtaBootProgress.Bar(OtaBootStep.Config, 1f), OtaBootProgress.Label(OtaBootStep.Config, 0, 0));

            // 2. Minimum version.
            var config = choice.Config;
            var platform = InAppUpdate.Platform;
            var update = MinVersionGate.Decide(config, platform, Application.version);
            var storeUrl = MinVersionGate.StoreLink(config, platform, platform == "android" ? "https://play.google.com/store/apps/details?id=" + appId : "");
            if (update == UpdateRequirement.Required)
            {
                Publish(config, choice.Source, FreePaceVerdict.NoPatches, "");
                done(new OtaBootResult { Config = config, Source = choice.Source, Update = update, StoreUrl = storeUrl, Content = OtaContentOutcome.Skipped });
                yield break;
            }

            // 3. Balance patches, guarded by the locked free pace.
            var balance = RemoteBalance.For(config, out var verdict, out var reason);
            if (verdict == FreePaceVerdict.Rejected)
            {
                Debug.LogWarning("OTA: remote balance rejected, baked balance kept: " + reason);
                config = config.WithoutBalance();
            }

            PlayerAssets.TextFilter = balance.PatchCount > 0 ? balance.Apply : (Func<string, string, string>?)null;

            // 4. Content.
            var content = OtaContentOutcome.Skipped;
            if (OtaBootProgress.WantsRemoteContent(settings, config))
            {
                OtaHost.CdnHost = settings.CdnHost;
                OtaHost.ContentVersion = config.ContentVersion;
                yield return OtaContentUpdater.Run(
                    settings,
                    (step, f, got, total) => progress(OtaBootProgress.Bar(step, f), OtaBootProgress.Label(step, got, total)),
                    outcome => content = outcome);
            }

            Publish(config, choice.Source, verdict, content == OtaContentOutcome.Overlaid ? config.ContentVersion : "");
            progress(1f, OtaBootProgress.Label(OtaBootStep.Done, 0, 0));
            Debug.Log("OTA: config v" + config.ConfigVersion + " (" + choice.Source + "), content " + content +
                      (content == OtaContentOutcome.Overlaid ? " " + config.ContentVersion : "") + ", balance " + verdict + ".");
            done(new OtaBootResult { Config = config, Source = choice.Source, Update = update, StoreUrl = storeUrl, Content = content, Balance = verdict });
        }

        private static void Publish(RemoteConfig config, RemoteConfigSource source, FreePaceVerdict verdict, string contentVersion)
        {
            OtaRuntime.Config = config;
            OtaRuntime.Source = source;
            OtaRuntime.BalanceVerdict = verdict;
            OtaRuntime.ActiveContentVersion = contentVersion;
        }
    }
}
