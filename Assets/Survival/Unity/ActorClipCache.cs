#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The editor-side clip lookups the actors do on a pose change (import-settings check, then
    /// AssetDatabase.LoadAllAssetsAtPath over the whole FBX), done once per clip per session instead of on every pose
    /// change. In the runner a pose change happens every few frames per rig (walk / attack / rest), and the import check
    /// reads ModelImporter settings (defaultClipAnimations parses the FBX takes), which is what stalls a frame.
    /// A clip destroyed by a reimport reads as null and is looked up again.
    /// </summary>
    public static class ActorClipCache
    {
        private static readonly Dictionary<string, AnimationClip> Clips = new Dictionary<string, AnimationClip>();
        private static readonly HashSet<string> Seen = new HashSet<string>();

        public static AnimationClip? Get(string key) =>
            Clips.TryGetValue(key, out var clip) && clip != null ? clip : null;

        public static AnimationClip? Put(string key, AnimationClip? clip)
        {
            if (clip != null)
            {
                Clips[key] = clip;
            }

            return clip;
        }

        /// <summary>True the first time <paramref name="key"/> is seen this session (one-time repairs and logs).</summary>
        public static bool First(string key) => Seen.Add(key);
    }
}
