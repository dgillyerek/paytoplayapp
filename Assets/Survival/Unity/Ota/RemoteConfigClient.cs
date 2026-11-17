#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Survival.Unity.Ota
{
    /// <summary>
    /// Fetches the remote config with one plain HTTPS GET and keeps the last good copy on the device.
    /// Kid safety (COPPA / Google Play Families): no query string, no custom headers, no device, install, advertising or
    /// account identifiers; the URL is the same for every phone. Nothing is ever sent but the request line.
    /// </summary>
    public static class RemoteConfigClient
    {
        private const string CacheFolder = "ota";
        private const string CacheFile = "remote_config.json";

        public static string CachePath => Path.Combine(Application.persistentDataPath, CacheFolder, CacheFile);

        /// <summary>GET <paramref name="url"/>; <paramref name="done"/> gets the body, or null on any error, timeout or oversize body.</summary>
        public static IEnumerator Fetch(string url, double timeoutSeconds, int maxBytes, Action<string?> done)
        {
            if (!url.StartsWith("https://", StringComparison.Ordinal))
            {
                Debug.LogWarning("OTA: remote config URL is not https, skipped: " + url);
                done(null);
                yield break;
            }

            using var req = UnityWebRequest.Get(url);
            req.timeout = Math.Max(1, (int)Math.Ceiling(timeoutSeconds));
            req.redirectLimit = 2;
            var started = Time.realtimeSinceStartup;
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (Time.realtimeSinceStartup - started > timeoutSeconds + 0.5f)
                {
                    req.Abort();
                    Debug.LogWarning("OTA: remote config timed out after " + timeoutSeconds + " s; using the cached or baked copy.");
                    done(null);
                    yield break;
                }

                yield return null;
            }

            if (req.result != UnityWebRequest.Result.Success || req.responseCode != 200)
            {
                Debug.LogWarning("OTA: remote config fetch failed (" + req.responseCode + " " + req.error + "); using the cached or baked copy.");
                done(null);
                yield break;
            }

            var bytes = req.downloadHandler.data;
            if (bytes == null || bytes.Length == 0 || bytes.Length > maxBytes)
            {
                Debug.LogWarning("OTA: remote config empty or larger than " + maxBytes + " bytes; ignored.");
                done(null);
                yield break;
            }

            done(Encoding.UTF8.GetString(bytes));
        }

        public static string? ReadCache()
        {
            try
            {
                return File.Exists(CachePath) ? File.ReadAllText(CachePath, Encoding.UTF8) : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("OTA: could not read the cached remote config: " + ex.Message);
                return null;
            }
        }

        /// <summary>Atomic replace (write a temp file, then move) so a crash mid-write never leaves a half copy.</summary>
        public static void WriteCache(string json)
        {
            try
            {
                var dir = Path.GetDirectoryName(CachePath)!;
                Directory.CreateDirectory(dir);
                var tmp = CachePath + ".tmp";
                File.WriteAllText(tmp, json, new UTF8Encoding(false));
                if (File.Exists(CachePath))
                {
                    File.Delete(CachePath);
                }

                File.Move(tmp, CachePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("OTA: could not save the remote config: " + ex.Message);
            }
        }
    }
}
