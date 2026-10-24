using System.IO;
using Survival.Domain.Roster;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// World-map ground for the locked battle (see <see cref="LockedBattleGroundSpec"/>): tiled map
    /// grass, a worn dirt track down each lane, soft contact shadows under the fighters, and a hazy
    /// horizon with distance fog. Textures load straight from the ThemePack PNGs, so Play does not
    /// wait on an import. If a texture is missing, it falls back to the map grass colour.
    /// </summary>
    public static class LockedBattleGround
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Texture2D? s_shadow;

        public static Color Horizon => new Color(LockedBattleGroundSpec.HorizonR, LockedBattleGroundSpec.HorizonG, LockedBattleGroundSpec.HorizonB, 1f);

        public static void Build(Transform parent)
        {
            var grassTex = Load(LockedBattleGroundSpec.GrassTileRel);
            var grass = Quad(parent, "Ground", LockedBattleGroundSpec.GroundSizeMetres, LockedBattleGroundSpec.GroundSizeMetres, 0f, 0f, 0f);
            var tiles = LockedBattleGroundSpec.GroundSizeMetres / LockedBattleGroundSpec.GrassTileMetres;
            var grassMat = Lit(grassTex, new Color(0.39f, 0.41f, 0.20f, 1f), false);
            Tile(grassMat, tiles, tiles);
            grass.sharedMaterial = grassMat;

            var dirtTex = Load(LockedBattleGroundSpec.DirtTrackRel);
            var dirtMat = Lit(dirtTex, new Color(0.58f, 0.52f, 0.38f, 0.8f), true);
            Tile(dirtMat, 1f, LockedBattleGroundSpec.TrackLengthMetres / LockedBattleGroundSpec.TrackRepeatMetres);
            for (var lane = 0; lane < LockedBattleRoster.LaneCount; lane++)
            {
                var track = Quad(parent, "LaneTrack" + lane, LockedBattleGroundSpec.TrackWidthMetres, LockedBattleGroundSpec.TrackLengthMetres, LockedBattleRoster.LaneX(lane), 0.003f, 0f);
                track.sharedMaterial = dirtMat;
                track.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Horizon;
            RenderSettings.fogStartDistance = LockedBattleGroundSpec.FogStartMetres;
            RenderSettings.fogEndDistance = LockedBattleGroundSpec.FogEndMetres;
        }

        /// <summary>Soft dark blob on the ground under a fighter. Child of the slot, so it follows the walk but not the flinch.</summary>
        public static void AddContactShadow(Transform slot, float footprintWidth, float footprintDepth, float battleScale = 1f)
        {
            // The cap grows with a battle-scaled body (the 2x dragons), so the blob stays the same share of it.
            var max = LockedBattleGroundSpec.ContactShadowMaxMetres * Mathf.Max(battleScale, 1f);
            var w = Mathf.Clamp(footprintWidth * LockedBattleGroundSpec.ContactShadowFootprintScale, LockedBattleGroundSpec.ContactShadowMinMetres, max);
            var d = Mathf.Clamp(footprintDepth * LockedBattleGroundSpec.ContactShadowFootprintScale, LockedBattleGroundSpec.ContactShadowMinMetres, max);
            var r = Quad(slot, "ContactShadow", w, d, 0f, 0.006f, 0f);
            r.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            var mat = new Material(shader);
            MakeTransparent(mat);
            var tex = ShadowTexture();
            var color = new Color(0.04f, 0.05f, 0.02f, LockedBattleGroundSpec.ContactShadowAlpha);
            if (mat.HasProperty(BaseMapId))
            {
                mat.SetTexture(BaseMapId, tex);
            }

            if (mat.HasProperty(BaseColorId))
            {
                mat.SetColor(BaseColorId, color);
            }

            mat.mainTexture = tex;
            mat.color = color;
            r.sharedMaterial = mat;
        }

        internal static MeshRenderer Quad(Transform parent, string name, float width, float depth, float x, float y, float z)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(width, depth, 1f);
            go.transform.localPosition = new Vector3(x, y, z);
            Object.Destroy(go.GetComponent<Collider>());
            return go.GetComponent<MeshRenderer>();
        }

        internal static Material Lit(Texture2D? tex, Color fallback, bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            var tint = tex != null ? Color.white : fallback;
            if (tex != null)
            {
                if (mat.HasProperty(BaseMapId))
                {
                    mat.SetTexture(BaseMapId, tex);
                }

                mat.mainTexture = tex;
            }

            if (mat.HasProperty(BaseColorId))
            {
                mat.SetColor(BaseColorId, tint);
            }

            mat.color = tint;
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.05f);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", 0f);
            }

            if (transparent)
            {
                MakeTransparent(mat);
            }

            return mat;
        }

        internal static void Tile(Material mat, float u, float v)
        {
            var scale = new Vector2(u, v);
            if (mat.HasProperty(BaseMapId))
            {
                mat.SetTextureScale(BaseMapId, scale);
            }

            mat.mainTextureScale = scale;
        }

        /// <summary>URP alpha blend, no depth write, drawn after the ground.</summary>
        internal static void MakeTransparent(Material mat)
        {
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
            }

            if (mat.HasProperty("_Blend"))
            {
                mat.SetFloat("_Blend", 0f);
            }

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10;
        }

        internal static Texture2D? Load(string themePackRel)
        {
            var path = Path.Combine(Application.dataPath, themePackRel.Replace('/', Path.DirectorySeparatorChar));
            if (!PlayerAssets.FileExists(path))
            {
                Debug.LogWarning("Locked battle ground: missing " + themePackRel + ", using the map grass colour.");
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!tex.LoadImage(PlayerAssets.ReadAllBytes(path)))
            {
                return null;
            }

            tex.name = Path.GetFileNameWithoutExtension(path);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            tex.Apply(true, false);
            return tex;
        }

        private static Texture2D ShadowTexture()
        {
            if (s_shadow != null)
            {
                return s_shadow;
            }

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color[n * n];
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = ((x + 0.5f) / n * 2f) - 1f;
                    var dy = ((y + 0.5f) / n * 2f) - 1f;
                    var d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var a = Mathf.Clamp01(1f - d);
                    pixels[(y * n) + x] = new Color(1f, 1f, 1f, a * a * (3f - (2f * a)));
                }
            }

            tex.SetPixels(pixels);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            tex.name = "locked_battle_contact_shadow";
            s_shadow = tex;
            return tex;
        }
    }
}
