#nullable enable
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace Survival.Editor
{
    /// <summary>
    /// Theme A is for an audience that includes children under 13 (COPPA, Google Play Families policy), so the app must
    /// not declare the advertising-ID permission. The project has no ad or ad-ID SDK, but a future dependency
    /// (play-services-ads-identifier, Firebase, an analytics SDK) can add <c>com.google.android.gms.permission.AD_ID</c>
    /// through manifest merging. This writes <c>&lt;uses-permission android:name="...AD_ID" tools:node="remove"/&gt;</c>
    /// into the generated Gradle project's launcher (app) manifest, the highest-priority manifest in the merge, and the
    /// unityLibrary manifest, so the merged manifest never carries it. The CI workflow re-checks the built .aab.
    /// </summary>
    public sealed class AndroidFamiliesManifest : IPostGenerateGradleAndroidProject
    {
        public const string AdIdPermission = "com.google.android.gms.permission.AD_ID";
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";
        private const string ToolsNs = "http://schemas.android.com/tools";

        public int callbackOrder => 1000;

        /// <param name="path">The generated unityLibrary module folder; the launcher module sits beside it.</param>
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            foreach (var manifest in new[]
                     {
                         Path.Combine(path, "..", "launcher", "src", "main", "AndroidManifest.xml"),
                         Path.Combine(path, "src", "main", "AndroidManifest.xml")
                     })
            {
                if (File.Exists(manifest))
                {
                    RemovePermission(manifest, AdIdPermission);
                    Debug.Log("Families: " + AdIdPermission + " marked tools:node=\"remove\" in " + Path.GetFullPath(manifest));
                }
                else
                {
                    Debug.LogWarning("Families: no manifest at " + Path.GetFullPath(manifest));
                }
            }
        }

        /// <summary>Adds (or turns into) a tools:node="remove" uses-permission entry for <paramref name="permission"/>.</summary>
        public static void RemovePermission(string manifestPath, string permission)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(manifestPath);
            var root = doc.DocumentElement!;
            if (!root.HasAttribute("xmlns:tools"))
            {
                root.SetAttribute("xmlns:tools", ToolsNs);
            }

            XmlElement? entry = null;
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is XmlElement e && e.Name == "uses-permission" && e.GetAttribute("name", AndroidNs) == permission)
                {
                    entry = e;
                    break;
                }
            }

            if (entry == null)
            {
                entry = doc.CreateElement("uses-permission");
                entry.SetAttribute("name", AndroidNs, permission);
                root.InsertBefore(entry, root.FirstChild);
            }

            entry.SetAttribute("node", ToolsNs, "remove");
            doc.Save(manifestPath);
        }
    }
}
