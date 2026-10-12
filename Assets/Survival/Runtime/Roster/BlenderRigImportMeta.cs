using System;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// Decides whether a Design Blender rig .meta already has the import settings
    /// <c>BlenderRigPlayer.EnsureImport</c> would write. The on-disk meta is what Unity
    /// imports. ModelImporter getters can disagree with that file and still round-trip
    /// to the same bytes, so trusting the getters calls SaveAndReimport on every Play.
    /// </summary>
    public static class BlenderRigImportMeta
    {
        public static bool NeedsReimport(
            string meta,
            bool humanoid,
            bool clip,
            string poseName,
            string takeName,
            int lastFrame)
        {
            if (string.IsNullOrEmpty(meta))
            {
                return true;
            }

            if (!HasInt(meta, "animationType", humanoid ? 3 : 2))
            {
                return true;
            }

            if (!HasInt(meta, "avatarSetup", 1))
            {
                return true;
            }

            if (!HasInt(meta, "autoGenerateAvatarMappingIfUnspecified", humanoid ? 1 : 0))
            {
                return true;
            }

            if (!HasInt(meta, "materialImportMode", 2))
            {
                return true;
            }

            if (!HasInt(meta, "useFileScale", 0))
            {
                return true;
            }

            if (!HasFloat(meta, "globalScale", 1f))
            {
                return true;
            }

            if (!HasInt(meta, "optimizeBones", 0))
            {
                return true;
            }

            if (!HasInt(meta, "bakeAxisConversion", 0))
            {
                return true;
            }

            if (!HasInt(meta, "importAnimation", 1))
            {
                return true;
            }

            if (!clip)
            {
                return false;
            }

            return !ClipPinned(meta, poseName, takeName, lastFrame);
        }

        private static bool ClipPinned(string meta, string poseName, string takeName, int lastFrame)
        {
            if (HasLine(meta, "clipAnimations: []"))
            {
                return false;
            }

            if (!HasLine(meta, "name: \"" + poseName + "\""))
            {
                return false;
            }

            if (!HasLine(meta, "takeName: " + takeName))
            {
                return false;
            }

            if (!HasInt(meta, "lastFrame", lastFrame))
            {
                return false;
            }

            if (!HasInt(meta, "loopTime", 1))
            {
                return false;
            }

            return HasInt(meta, "mirror", 0);
        }

        private static bool HasLine(string meta, string expected)
        {
            foreach (var raw in meta.Split('\n'))
            {
                if (string.Equals(raw.Trim(), expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasInt(string meta, string key, int value)
        {
            var found = false;
            foreach (var raw in meta.Split('\n'))
            {
                if (!TryValue(raw, key, out var text))
                {
                    continue;
                }

                found = true;
                if (!int.TryParse(text, out var parsed) || parsed != value)
                {
                    return false;
                }
            }

            return found;
        }

        private static bool HasFloat(string meta, string key, float value)
        {
            var found = false;
            foreach (var raw in meta.Split('\n'))
            {
                if (!TryValue(raw, key, out var text))
                {
                    continue;
                }

                found = true;
                if (!float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                    || Math.Abs(parsed - value) > 0.001f)
                {
                    return false;
                }
            }

            return found;
        }

        private static bool TryValue(string raw, string key, out string value)
        {
            var line = raw.Trim();
            var prefix = key + ":";
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = string.Empty;
                return false;
            }

            value = line.Substring(prefix.Length).Trim();
            return value.Length > 0;
        }
    }
}
