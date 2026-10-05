using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Binds Design loose paint on URP Lit. Metallic and roughness stay split on disk.
    /// URP Lit has no roughness slot: they pack once into _MetallicGlossMap
    /// (R metallic, A smoothness). Smoothness is 1 - roughness. The packed ORM
    /// is not assigned, and the split maps are not also assigned beside that pack.
    /// </summary>
    public static class RosterPaint
    {
        public const string LitShader = "Universal Render Pipeline/Lit";

        private static readonly Dictionary<string, Texture2D> PackedByKey = new();

        public static void Bind(
            GameObject root,
            string themePackDir,
            string baseColorFile,
            string normalFile,
            string metallicFile,
            string roughnessFile,
            string label,
            bool preferEmbeddedBaseAndNormal)
        {
#if UNITY_EDITOR
            var shader = Shader.Find(LitShader) ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError(label + " paint shader missing.");
                return;
            }

            var looseAlbedo = LoadMap(themePackDir, baseColorFile, asNormal: false, linear: false, readable: false);
            var looseNormal = LoadMap(themePackDir, normalFile, asNormal: true, linear: true, readable: false);
            var metallic = LoadMap(themePackDir, metallicFile, asNormal: false, linear: true, readable: true);
            var roughness = LoadMap(themePackDir, roughnessFile, asNormal: false, linear: true, readable: true);
            var packed = PackMetallicSmoothness(metallic, roughness, metallicFile);
            if (looseAlbedo == null)
            {
                Debug.LogError(label + " albedo missing. " + baseColorFile);
            }

            if (packed == null)
            {
                Debug.LogError(label + " metallic/roughness pack missing. " + metallicFile + " " + roughnessFile);
            }

            var painted = 0;
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                rend.enabled = true;
                if (rend is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                }

                var slots = rend.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    var made = MakeMaterial(shader, null, looseAlbedo, looseNormal, packed, preferEmbeddedBaseAndNormal, out var baseSource, out var normalSource);
                    rend.sharedMaterial = made;
                    LogSlot(label, shader, made, baseSource, normalSource, looseAlbedo, looseNormal, packed);
                    painted++;
                    continue;
                }

                for (var i = 0; i < slots.Length; i++)
                {
                    slots[i] = MakeMaterial(
                        shader,
                        slots[i],
                        looseAlbedo,
                        looseNormal,
                        packed,
                        preferEmbeddedBaseAndNormal,
                        out var baseSource,
                        out var normalSource);
                    LogSlot(label, shader, slots[i], baseSource, normalSource, looseAlbedo, looseNormal, packed);
                    painted++;
                }

                rend.sharedMaterials = slots;
            }

            if (painted == 0)
            {
                Debug.LogError(label + " paint found no renderer.");
            }
#else
            _ = root;
            _ = themePackDir;
            _ = baseColorFile;
            _ = normalFile;
            _ = metallicFile;
            _ = roughnessFile;
            _ = preferEmbeddedBaseAndNormal;
            Debug.LogError(label + " paint binds in the Editor Game view.");
#endif
        }

#if UNITY_EDITOR
        private static Material MakeMaterial(
            Shader shader,
            Material? existing,
            Texture2D? looseAlbedo,
            Texture2D? looseNormal,
            Texture2D? packed,
            bool preferEmbeddedBaseAndNormal,
            out string baseSource,
            out string normalSource)
        {
            var mat = new Material(shader);
            if (existing != null)
            {
                mat.name = existing.name;
            }

            Texture? albedo = null;
            Texture? normal = null;
            baseSource = "loose";
            normalSource = "loose";
            if (preferEmbeddedBaseAndNormal && existing != null && !ShaderMissing(existing))
            {
                var embeddedAlbedo = EmbeddedAlbedo(existing);
                var embeddedNormal = EmbeddedNormal(existing);
                if (embeddedAlbedo != null)
                {
                    albedo = embeddedAlbedo;
                    baseSource = "embedded";
                }

                if (embeddedNormal != null)
                {
                    normal = embeddedNormal;
                    normalSource = "embedded";
                }
            }

            if (albedo == null)
            {
                albedo = looseAlbedo;
                baseSource = looseAlbedo != null ? "loose" : "missing";
            }

            if (normal == null)
            {
                normal = looseNormal;
                normalSource = looseNormal != null ? "loose" : "missing";
            }

            if (albedo != null)
            {
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", albedo);
                }

                if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", albedo);
                }

                mat.EnableKeyword("_BASEMAP");
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", Color.white);
                }

                mat.color = Color.white;
            }

            if (mat.HasProperty("_WorkflowMode"))
            {
                mat.SetFloat("_WorkflowMode", 1f);
            }

            mat.DisableKeyword("_SPECULAR_SETUP");
            if (packed != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", packed);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.EnableKeyword("_METALLICGLOSSMAP");
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", 1f);
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 1f);
                }

                if (mat.HasProperty("_SmoothnessTextureChannel"))
                {
                    mat.SetFloat("_SmoothnessTextureChannel", 0f);
                }

                mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            }

            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
                if (mat.HasProperty("_BumpScale"))
                {
                    mat.SetFloat("_BumpScale", 1f);
                }
            }

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 0f);
            }

            return mat;
        }

        private static void LogSlot(
            string label,
            Shader shader,
            Material mat,
            string baseSource,
            string normalSource,
            Texture? looseAlbedo,
            Texture? looseNormal,
            Texture? packed)
        {
            var albedo = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            albedo ??= mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            var size = albedo != null ? albedo.width + "x" + albedo.height : "missing";
            Debug.Log(
                label + " paint shader=" + shader.name +
                " material=" + mat.name +
                " base=" + baseSource + " " + size +
                " normal=" + normalSource + " " + (looseNormal != null || normalSource == "embedded" ? "bound" : "missing") +
                " metallicSmoothness=" + (packed != null ? "packed" : "missing") +
                " smoothness=1-roughness" +
                " looseAlbedo=" + (looseAlbedo != null ? looseAlbedo.name : "missing"));
        }

        private static Texture2D? LoadMap(string themePackDir, string fileName, bool asNormal, bool linear, bool readable)
        {
            var path = "Assets/" + themePackDir.Replace('\\', '/') + "/" + fileName;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer == null)
            {
                Debug.LogError("Roster paint map missing. " + path);
                return null;
            }

            var dirty = false;
            var wantType = asNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != wantType)
            {
                importer.textureType = wantType;
                dirty = true;
            }

            if (!asNormal && importer.sRGBTexture != !linear)
            {
                importer.sRGBTexture = !linear;
                dirty = true;
            }

            if (readable && !importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// URP Lit reads R as metallic and A as smoothness. Design roughness is higher = rougher,
        /// so smoothness = 1f - roughness. Split maps only; no ORM composite is sampled.
        /// </summary>
        private static Texture2D? PackMetallicSmoothness(Texture2D? metallic, Texture2D? roughnessMap, string metallicFile)
        {
            if (metallic == null || roughnessMap == null)
            {
                return null;
            }

            if (metallic.width != roughnessMap.width || metallic.height != roughnessMap.height)
            {
                Debug.LogError(
                    "Roster paint metallic/roughness size mismatch. " +
                    metallic.width + "x" + metallic.height + " vs " +
                    roughnessMap.width + "x" + roughnessMap.height);
                return null;
            }

            var key = metallicFile + "|" + metallic.width + "x" + metallic.height;
            if (PackedByKey.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                var metalPx = metallic.GetPixels();
                var roughPx = roughnessMap.GetPixels();
                var packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, true, true);
                var output = new Color[metalPx.Length];
                for (var i = 0; i < metalPx.Length; i++)
                {
                    var metal = metalPx[i].r;
                    var roughness = roughPx[i].r;
                    var smoothness = 1f - roughness;
                    output[i] = new Color(metal, metal, metal, smoothness);
                }

                packed.SetPixels(output);
                packed.Apply(true, false);
                packed.wrapMode = TextureWrapMode.Repeat;
                packed.filterMode = FilterMode.Bilinear;
                packed.name = metallic.name + "_metallicSmoothness";
                PackedByKey[key] = packed;
                return packed;
            }
            catch (Exception ex)
            {
                Debug.LogError("Roster paint metallic-smoothness pack failed. " + ex.Message);
                return null;
            }
        }

        private static Texture? EmbeddedAlbedo(Material mat)
        {
            if (mat.HasProperty("_BaseMap") && Usable(mat.GetTexture("_BaseMap")))
            {
                return mat.GetTexture("_BaseMap");
            }

            if (mat.HasProperty("_MainTex") && Usable(mat.GetTexture("_MainTex")))
            {
                return mat.GetTexture("_MainTex");
            }

            return Usable(mat.mainTexture) ? mat.mainTexture : null;
        }

        private static Texture? EmbeddedNormal(Material mat)
        {
            if (mat.HasProperty("_BumpMap") && Usable(mat.GetTexture("_BumpMap")))
            {
                return mat.GetTexture("_BumpMap");
            }

            if (mat.HasProperty("_NormalMap") && Usable(mat.GetTexture("_NormalMap")))
            {
                return mat.GetTexture("_NormalMap");
            }

            return null;
        }

        private static bool Usable(Texture? tex)
        {
            if (tex == null || tex.width <= 4 || tex.height <= 4)
            {
                return false;
            }

            var name = tex.name ?? string.Empty;
            if (name.Length == 0)
            {
                return false;
            }

            if (name.IndexOf("Default", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("unity_builtin", StringComparison.OrdinalIgnoreCase) >= 0
                || string.Equals(name, "UnityWhite", StringComparison.Ordinal)
                || string.Equals(name, "UnityNormalMap", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private static bool ShaderMissing(Material mat)
        {
            return mat.shader == null || mat.shader.name.IndexOf("InternalError", StringComparison.Ordinal) >= 0;
        }
#endif
    }
}
