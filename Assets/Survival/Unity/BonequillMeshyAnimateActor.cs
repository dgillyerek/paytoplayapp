using System;
using System.IO;
using Survival.Domain.Enemies;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Bonequill World proof actor: Meshy Animate FBX humanoid + Walking clip AS-IS.
    /// Path A weight-paint is CANCELLED. Atlas albedo + normal bound so Lit is not white.
    /// Walk-only. Soft leftover: body-only (no bow/quiver). Design PASS not claimed.
    /// </summary>
    public sealed class BonequillMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = BonequillAnimateWalk.ThemePackFbx;
        public const string ClipHint = BonequillAnimateWalk.ClipHint;
        public const float RearYawDegrees = BonequillAnimateWalk.MixamoImportRearYawDegrees;

        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private float _clipLength;
        private bool _graphReady;
        private GameObject? _instance;

        public bool Built => _graphReady;

        public void Build()
        {
            BuildLights();
            var prefab = LoadFbxPrefab();
            if (prefab == null)
            {
                Debug.LogError(
                    "Bonequill Meshy Animate FBX not imported yet. Open the project in Unity so " +
                    ThemePackFbx + " Humanoid-imports, then Play Bonequill.");
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "BonequillMeshyAnimateWalk";
            HideJunk(_instance);
            FaceWorldTop(_instance);
            PunchMaterials(_instance);

            _animator = _instance.GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = _instance.AddComponent<Animator>();
            }

            if (_animator.avatar == null)
            {
                _animator.avatar = LoadHumanoidAvatar();
            }

            var clip = LoadWalkingClip();
            if (clip == null)
            {
                Debug.LogError("Bonequill Meshy Animate FBX has no Walking clip after Humanoid import.");
                return;
            }

            clip.wrapMode = WrapMode.Loop;
            _clipLength = clip.length;
            _graph = PlayableGraph.Create("BonequillMeshyAnimate");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "Bonequill", _animator);
            _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            output.SetSourcePlayable(_clipPlayable);
            _graph.Play();
            _graphReady = true;
            SampleWalk(0f);
        }

        private void Update()
        {
            if (!_graphReady || _clipLength <= 0f)
            {
                return;
            }

            SampleWalk(Time.unscaledTime % _clipLength);
        }

        private void SampleWalk(float time)
        {
            if (!_graph.IsValid())
            {
                return;
            }

            _clipPlayable.SetTime(time);
            _graph.Evaluate();
        }

        public string PhaseLabel(float timeSeconds) => "WALK  ·  toward TOP  ·  Meshy Animate";

        private void OnDestroy()
        {
            if (_graphReady && _graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private static void HideJunk(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        private static void FaceWorldTop(GameObject root)
        {
            // AccuRIG Mixamo import instantiates face-to-camera (−Z) = frontal.
            // Yaw 180 so transform.forward faces World TOP (+Z); back to Play cam.
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.Euler(0f, RearYawDegrees, 0f);
        }

        private static GameObject? LoadFbxPrefab()
        {
#if UNITY_EDITOR
            var rel = "Assets/" + ThemePackFbx.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            if (go != null)
            {
                return go;
            }

            var data = Application.dataPath;
            var abs = Path.Combine(Directory.GetParent(data)!.FullName, rel);
            if (File.Exists(abs))
            {
                AssetDatabase.ImportAsset(rel);
                return AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            }
#endif
            return Resources.Load<GameObject>("bonequill_meshy_animate_walk");
        }

        private static string FbxAssetPath => "Assets/" + ThemePackFbx.Replace('\\', '/');

        private static AnimationClip? LoadWalkingClip()
        {
#if UNITY_EDITOR
            var clip = PickWalkingClip(FbxAssetPath);
            if (clip != null)
            {
                return clip;
            }

            if (RepairWalkingTake(FbxAssetPath))
            {
                return PickWalkingClip(FbxAssetPath);
            }

            return null;
#else
            return null;
#endif
        }

        private static Avatar? LoadHumanoidAvatar()
        {
#if UNITY_EDITOR
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(FbxAssetPath))
            {
                if (obj is Avatar avatar)
                {
                    return avatar;
                }
            }
#endif
            return null;
        }

        private static AnimationClip? PickWalkingClip(string rel)
        {
#if UNITY_EDITOR
            AnimationClip? exact = null;
            AnimationClip? walk = null;
            AnimationClip? longest = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                if (longest == null || clip.length > longest.length)
                {
                    longest = clip;
                }

                var isWalk = clip.name.IndexOf(ClipHint, StringComparison.OrdinalIgnoreCase) >= 0
                    || clip.name.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isWalk)
                {
                    continue;
                }

                if (string.Equals(clip.name, ClipHint, StringComparison.OrdinalIgnoreCase))
                {
                    if (exact == null || clip.length > exact.length)
                    {
                        exact = clip;
                    }

                    continue;
                }

                if (walk == null || clip.length > walk.length)
                {
                    walk = clip;
                }
            }

            return exact ?? walk ?? longest;
#else
            return null;
#endif
        }

        private static bool RepairWalkingTake(string rel)
        {
#if UNITY_EDITOR
            if (AssetImporter.GetAtPath(rel) is not ModelImporter importer || !importer.importAnimation)
            {
                return false;
            }

            var defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                return false;
            }

            ModelImporterClipAnimation? best = null;
            var bestSpan = -1f;
            foreach (var candidate in defaults)
            {
                var label = (candidate.takeName ?? "") + "\n" + (candidate.name ?? "");
                if (label.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var span = candidate.lastFrame - candidate.firstFrame;
                if (best == null || span > bestSpan)
                {
                    best = candidate;
                    bestSpan = span;
                }
            }

            if (best == null || bestSpan <= 0f)
            {
                return false;
            }

            best.name = ClipHint;
            best.loopTime = true;
            best.loop = true;
            best.keepOriginalOrientation = true;
            best.keepOriginalPositionY = true;
            best.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { best };
            importer.SaveAndReimport();
            return true;
#else
            return false;
#endif
        }

        private static Texture2D? _cachedAlbedo;
        private static Texture2D? _cachedNormal;

        private static void PunchMaterials(GameObject root)
        {
            var albedo = LoadAlbedo();
            var normal = LoadNormal();
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = rend.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    var lone = NewLit();
                    BindMapsAndPunch(lone, albedo, normal);
                    rend.material = lone;
                    continue;
                }

                var copies = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++)
                {
                    var src = shared[i] != null ? shared[i] : NewLit();
                    var mat = new Material(src);
                    BindMapsAndPunch(mat, albedo, normal);
                    copies[i] = mat;
                }

                rend.materials = copies;
            }
        }

        private static void BindMapsAndPunch(Material mat, Texture2D? albedo, Texture2D? normal)
        {
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
            }

            if (normal != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normal);
                    if (mat.HasProperty("_BumpScale"))
                    {
                        mat.SetFloat("_BumpScale", 1f);
                    }
                }

                if (mat.HasProperty("_NormalMap"))
                {
                    mat.SetTexture("_NormalMap", normal);
                }
            }

            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor");
                if (c.maxColorComponent < 0.08f)
                {
                    c = Color.white;
                }

                mat.SetColor("_BaseColor", new Color(
                    Mathf.Clamp01(c.r * 1.08f),
                    Mathf.Clamp01(c.g * 1.06f),
                    Mathf.Clamp01(c.b * 1.04f),
                    c.a));
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", Mathf.Max(mat.GetFloat("_Smoothness"), 0.42f));
            }
        }

        private static Material NewLit()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Sprites/Default");
            return new Material(shader);
        }

        private static Texture2D? LoadAlbedo()
        {
            if (_cachedAlbedo != null)
            {
                return _cachedAlbedo;
            }

#if UNITY_EDITOR
            var sibling = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + BonequillAnimateWalk.ThemePackAtlas);
            if (sibling != null)
            {
                _cachedAlbedo = sibling;
                return sibling;
            }
#endif
            _cachedAlbedo = LoadSiblingPng("bonequill_meshy_animate_walk_atlas.png")
                            ?? ExtractFbxPng(color: true);
            return _cachedAlbedo;
        }

        private static Texture2D? LoadNormal()
        {
            if (_cachedNormal != null)
            {
                return _cachedNormal;
            }

#if UNITY_EDITOR
            var sibling = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + BonequillAnimateWalk.ThemePackNormal);
            if (sibling != null)
            {
                _cachedNormal = sibling;
                return sibling;
            }
#endif
            _cachedNormal = LoadSiblingPng("bonequill_meshy_animate_walk_normal.png")
                           ?? ExtractFbxPng(color: false);
            return _cachedNormal;
        }

        private static Texture2D? LoadSiblingPng(string fileName)
        {
            var path = ResolveEnemy3D(fileName);
            if (path == null)
            {
                return null;
            }

            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.name = Path.GetFileNameWithoutExtension(fileName);
                    return tex;
                }
            }
            catch (IOException)
            {
            }

            return null;
        }

        private static string? ResolveEnemy3D(string fileName)
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            foreach (var path in new[]
                     {
                         Path.Combine(data, "ThemePack", "fantasy_kingdom_a", "art", "enemies", "3d", fileName),
                         Path.Combine(repo, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "enemies", "3d", fileName),
                     })
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static string? ResolveFbxPath()
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            var rel = ThemePackFbx.Replace('\\', '/');
            foreach (var path in new[] { Path.Combine(data, rel), Path.Combine(repo, "Assets", rel) })
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static Texture2D? ExtractFbxPng(bool color)
        {
            var fbx = ResolveFbxPath();
            if (fbx == null)
            {
                return null;
            }

            try
            {
                var bytes = File.ReadAllBytes(fbx);
                var sig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                var iend = new byte[] { 0x49, 0x45, 0x4E, 0x44 };
                var start = 0;
                while (start < bytes.Length)
                {
                    var i = IndexOf(bytes, sig, start);
                    if (i < 0)
                    {
                        break;
                    }

                    var j = IndexOf(bytes, iend, i + 8);
                    if (j < 0)
                    {
                        break;
                    }

                    var end = Math.Min(bytes.Length, j + 8);
                    var png = new byte[end - i];
                    Buffer.BlockCopy(bytes, i, png, 0, png.Length);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(png) && tex.width >= 256)
                    {
                        var isNormal = LooksLikeNormalMap(tex);
                        if (color && !isNormal)
                        {
                            tex.wrapMode = TextureWrapMode.Clamp;
                            tex.filterMode = FilterMode.Bilinear;
                            tex.name = "bonequill_meshy_albedo";
                            return tex;
                        }

                        if (!color && isNormal)
                        {
                            tex.wrapMode = TextureWrapMode.Clamp;
                            tex.filterMode = FilterMode.Bilinear;
                            tex.name = "bonequill_meshy_normal";
                            return tex;
                        }
                    }

                    start = i + 8;
                }
            }
            catch (IOException)
            {
            }

            return null;
        }

        private static bool LooksLikeNormalMap(Texture2D tex)
        {
            var px = tex.GetPixels(tex.width / 2, tex.height / 2, 8, 8);
            if (px.Length == 0)
            {
                return false;
            }

            var r = 0f;
            var g = 0f;
            var b = 0f;
            foreach (var c in px)
            {
                r += c.r;
                g += c.g;
                b += c.b;
            }

            var n = px.Length;
            r /= n;
            g /= n;
            b /= n;
            return b > 0.75f && r > 0.35f && r < 0.65f && g > 0.35f && g < 0.65f;
        }

        private static int IndexOf(byte[] hay, byte[] needle, int start)
        {
            var last = hay.Length - needle.Length;
            for (var i = start; i <= last; i++)
            {
                var ok = true;
                for (var j = 0; j < needle.Length; j++)
                {
                    if (hay[i + j] != needle[j])
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.29f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.13f, 0.11f);
            RenderSettings.ambientIntensity = 1.25f;
            RenderSettings.reflectionIntensity = 1.15f;

            var key = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (key == null)
            {
                var sun = new GameObject("BonequillKeyLight");
                key = sun.AddComponent<Light>();
            }

            key.type = LightType.Directional;
            key.color = new Color(1f, 0.96f, 0.88f);
            key.intensity = 1.85f;
            key.transform.rotation = Quaternion.Euler(52f, -16f, 0f);

            if (UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length < 2)
            {
                var fillGo = new GameObject("BonequillFillLight");
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = new Color(0.82f, 0.88f, 1f);
                fill.intensity = 0.55f;
                fillGo.transform.rotation = Quaternion.Euler(70f, 35f, 0f);
            }
        }
    }
}
