#nullable enable
using System.IO;
using System.Text.RegularExpressions;
using Survival.Unity.Ota;
using UnityEditor.Android;
using UnityEngine;

namespace Survival.Editor.Ota
{
    /// <summary>
    /// Adds Google Play In-App Updates (<see cref="InAppUpdate.AppUpdateDependency"/>) to the generated Gradle project's
    /// unityLibrary module, for the minimum-version prompt's immediate update flow. No Unity package or EDM4U is needed:
    /// Survival.Unity.Ota.InAppUpdate calls the Java API through JNI. The library declares no permissions and no AD_ID;
    /// <see cref="AndroidFamiliesManifest"/> (which runs after this) and the CI bundle check still guard AD_ID.
    /// </summary>
    public sealed class AndroidInAppUpdateGradle : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 900;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var gradle = Path.Combine(path, "build.gradle");
            if (!File.Exists(gradle))
            {
                Debug.LogWarning("OTA: no unityLibrary build.gradle at " + gradle + "; Play in-app updates fall back to the store link.");
                return;
            }

            var text = File.ReadAllText(gradle);
            var updated = AddDependency(text, InAppUpdate.AppUpdateDependency);
            if (updated != text)
            {
                File.WriteAllText(gradle, updated);
                Debug.Log("OTA: added " + InAppUpdate.AppUpdateDependency + " to " + Path.GetFullPath(gradle));
            }
        }

        /// <summary>Adds <c>implementation '&lt;dep&gt;'</c> to the first top-level dependencies block (idempotent).</summary>
        public static string AddDependency(string gradle, string dependency)
        {
            var name = dependency.Substring(0, dependency.LastIndexOf(':'));
            if (gradle.Contains(name))
            {
                return gradle;
            }

            var m = Regex.Match(gradle, @"(?m)^dependencies\s*\{");
            if (!m.Success)
            {
                return gradle + "\ndependencies {\n    implementation '" + dependency + "'\n}\n";
            }

            var at = m.Index + m.Length;
            return gradle.Substring(0, at) + "\n    implementation '" + dependency + "'" + gradle.Substring(at);
        }
    }
}
