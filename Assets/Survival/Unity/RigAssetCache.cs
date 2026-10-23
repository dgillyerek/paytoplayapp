#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Survival.Unity
{
    /// <summary>
    /// What every rig of one kind shares, read once per session instead of once per rig. Building a rig used to repeat,
    /// for each copy: the import-settings check (parses the .meta), AssetDatabase.LoadAllAssetsAtPath over the whole FBX
    /// (twice, for the avatar), the texture import checks, and the glTF metallic-roughness repack (GetPixels, a
    /// per-pixel convert, SetPixels, Apply with mips on a full-size texture). Ten copies of one hero did that ten times.
    /// Now the first copy does it and the rest reuse the result: meshes, clips (<see cref="ActorClipCache"/>), avatars and
    /// the packed textures are shared between copies, only the instance (hierarchy, playable graph, materials) is per rig.
    /// A cached object destroyed by a reimport reads as null and is rebuilt.
    /// </summary>
    internal static class RigAssetCache
    {
        private static readonly Dictionary<string, Object[]> Assets = new Dictionary<string, Object[]>();
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> Done = new HashSet<string>();

        /// <summary>Lookups served from the cache / worked out the slow way (logged with the prewarm line).</summary>
        public static int Hits { get; private set; }

        public static int Misses { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Assets.Clear();
            Textures.Clear();
            Done.Clear();
            Hits = Misses = 0;
        }

        /// <summary>True the first time <paramref name="key"/> is asked this session (one-time import checks).</summary>
        public static bool First(string key) => Done.Add(key);

        /// <summary><see cref="PlayerAssets.LoadAll"/> once per path per session.</summary>
        public static Object[] All(string path)
        {
            if (Assets.TryGetValue(path, out var have) && Alive(have))
            {
                Hits++;
                return have;
            }

            Misses++;
            var loaded = PlayerAssets.LoadAll(path);
            if (loaded.Length > 0)
            {
                Assets[path] = loaded;
            }

            return loaded;
        }

        /// <summary>A texture one rig kind builds (a loaded map, a packed metallic map): <paramref name="make"/> runs once per key.</summary>
        public static Texture2D? Texture(string key, Func<Texture2D?> make)
        {
            if (Textures.TryGetValue(key, out var have) && have != null)
            {
                Hits++;
                return have;
            }

            Misses++;
            var made = make();
            if (made != null)
            {
                Textures[key] = made;
            }

            return made;
        }

        private static bool Alive(Object[] all)
        {
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
