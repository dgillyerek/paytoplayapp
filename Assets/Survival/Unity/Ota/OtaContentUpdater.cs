#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Survival.Domain.Ota;
using UnityEngine;
#if SURVIVAL_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Survival.Unity.Ota
{
    /// <summary>Outcome of the content step.</summary>
    public enum OtaContentOutcome
    {
        /// <summary>No remote content wanted (host unset, content_version "", content_api mismatch, flag off).</summary>
        Skipped,

        /// <summary>The remote catalog's content is all on the device (downloaded now or earlier) and overlaid.</summary>
        Overlaid,

        /// <summary>Offline, timed out or failed: the content baked into the build is used.</summary>
        Bundled
    }

    /// <summary>
    /// Addressables side of over-the-air content: check the remote catalog, switch to a newer one, size and download every
    /// bundle the "ota" label needs (progress on the loading screen), then load the four OTA registries
    /// (<see cref="OtaContentGroups"/>) and hand them to <see cref="PlayerAssets.SetOverlays"/>. Any failure or timeout
    /// leaves PlayerAssets on the registry baked into the build, so the game always starts.
    /// Bundles hold assets and data only (models, clips, textures, JSON as TextAssets); no code is ever downloaded or run.
    /// </summary>
    public static class OtaContentUpdater
    {
        public static IEnumerator Run(OtaSettings settings, Action<OtaBootStep, float, long, long> progress, Action<OtaContentOutcome> done)
        {
#if SURVIVAL_ADDRESSABLES
            var outcome = OtaContentOutcome.Bundled;
            var timeout = settings.CatalogTimeoutSeconds;

            // 1. Initialize (local + cached catalogs only: "Only update catalogs manually" is on).
            progress(OtaBootStep.Catalog, 0f, 0, 0);
            var init = Addressables.InitializeAsync(false);
            yield return Wait(init, timeout, "initialize");
            if (!Succeeded(init))
            {
                done(outcome);
                yield break;
            }

            // 2. Newer remote catalog? Switch to it (old bundles the new catalog no longer lists are cleaned from the cache).
            var check = Addressables.CheckForCatalogUpdates(false);
            yield return Wait(check, timeout, "catalog check");
            List<string>? changed = Succeeded(check) ? check.Result : null;
            ReleaseIfDone(check);
            progress(OtaBootStep.Catalog, 0.5f, 0, 0);
            if (changed != null && changed.Count > 0)
            {
                var update = Addressables.UpdateCatalogs(true, changed, false);
                yield return Wait(update, timeout, "catalog update");
                var ok = Succeeded(update);
                ReleaseIfDone(update);
                if (!ok)
                {
                    Debug.LogWarning("OTA: catalog update failed; using the cached catalog if its content is all on the device.");
                }
            }

            progress(OtaBootStep.Catalog, 1f, 0, 0);

            // 3. How much is missing from the cache?
            var size = Addressables.GetDownloadSizeAsync((object)OtaContentGroups.Label);
            yield return Wait(size, timeout, "download size");
            if (!Succeeded(size))
            {
                ReleaseIfDone(size);
                done(outcome);
                yield break;
            }

            var bytes = size.Result;
            Addressables.Release(size);

            // 4. Download with a stall timeout (slow networks are fine as long as bytes keep arriving).
            if (bytes > 0)
            {
                var dl = Addressables.DownloadDependenciesAsync((object)OtaContentGroups.Label, false);
                long last = -1;
                var lastMove = Time.realtimeSinceStartup;
                while (!dl.IsDone)
                {
                    var s = dl.GetDownloadStatus();
                    if (s.DownloadedBytes != last)
                    {
                        last = s.DownloadedBytes;
                        lastMove = Time.realtimeSinceStartup;
                    }

                    progress(OtaBootStep.Download, bytes > 0 ? (float)s.DownloadedBytes / bytes : s.Percent, s.DownloadedBytes, bytes);
                    if (Time.realtimeSinceStartup - lastMove > settings.DownloadStallSeconds)
                    {
                        Debug.LogWarning("OTA: download stalled for " + settings.DownloadStallSeconds + " s; playing on bundled content (it resumes next launch).");
                        done(OtaContentOutcome.Bundled);
                        yield break;
                    }

                    yield return null;
                }

                var downloaded = dl.Status == AsyncOperationStatus.Succeeded;
                Addressables.Release(dl);
                if (!downloaded)
                {
                    Debug.LogWarning("OTA: download failed; playing on bundled content.");
                    done(OtaContentOutcome.Bundled);
                    yield break;
                }
            }

            progress(OtaBootStep.Download, 1f, bytes, bytes);

            // 5. Load the OTA registries (all from the device cache now) and overlay them.
            progress(OtaBootStep.Load, 0f, bytes, bytes);
            var load = Addressables.LoadAssetsAsync<PlayerAssetRegistry>((object)OtaContentGroups.Label, null);
            yield return Wait(load, Math.Max(timeout, 30), "load registries");
            if (!Succeeded(load) || load.Result == null || load.Result.Count == 0)
            {
                ReleaseIfDone(load);
                done(OtaContentOutcome.Bundled);
                yield break;
            }

            // The handle stays alive for the whole run: releasing it would unload the overlaid assets.
            var overlays = new List<PlayerAssetRegistry>(load.Result);
            PlayerAssets.SetOverlays(overlays);
            progress(OtaBootStep.Load, 1f, bytes, bytes);
            Debug.Log("OTA: overlaid " + overlays.Count + " content registries (" + bytes + " bytes downloaded this launch).");
            done(OtaContentOutcome.Overlaid);
#else
            Debug.LogWarning("OTA: com.unity.addressables is not installed; using bundled content.");
            progress(OtaBootStep.Load, 1f, 0, 0);
            done(OtaContentOutcome.Bundled);
            yield break;
#endif
        }

#if SURVIVAL_ADDRESSABLES
        /// <summary>Waits for <paramref name="op"/> up to <paramref name="seconds"/>; a late operation is left to finish unobserved.</summary>
        private static IEnumerator Wait(AsyncOperationHandle op, double seconds, string what)
        {
            var started = Time.realtimeSinceStartup;
            while (op.IsValid() && !op.IsDone)
            {
                if (Time.realtimeSinceStartup - started > seconds)
                {
                    Debug.LogWarning("OTA: " + what + " timed out after " + seconds + " s.");
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator Wait<T>(AsyncOperationHandle<T> op, double seconds, string what) => Wait((AsyncOperationHandle)op, seconds, what);

        private static bool Succeeded(AsyncOperationHandle op) => op.IsValid() && op.IsDone && op.Status == AsyncOperationStatus.Succeeded;

        private static bool Succeeded<T>(AsyncOperationHandle<T> op) => Succeeded((AsyncOperationHandle)op);

        private static void ReleaseIfDone(AsyncOperationHandle op)
        {
            if (op.IsValid() && op.IsDone)
            {
                Addressables.Release(op);
            }
        }

        private static void ReleaseIfDone<T>(AsyncOperationHandle<T> op) => ReleaseIfDone((AsyncOperationHandle)op);
#endif
    }
}
