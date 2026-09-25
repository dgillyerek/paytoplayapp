using System;
using System.IO;
using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Aldric World proof actor: Meshy Animate FBX humanoid + Walking clip AS-IS.
    /// Path A weight-paint is CANCELLED. Do not remap sheath / Hand_R / ghost legs.
    /// World LIGHT + MATERIAL punch. Binds Meshy albedo from the FBX pack
    /// (Humanoid import often drops texture links). Design PASS not claimed.
    /// </summary>
    public sealed class SirAldricMeshyAnimateActor : MonoBehaviour
    {
        public const string ThemePackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx";
        public const string ThemePackAttackFbx = "ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx";
        public const string ClipHint = "Walking";
        public const string AttackClipHint = "Attack";
        /// <summary>
        /// Yaw so imported Mixamo forward (−Z, face to Play cam) becomes world +Z.
        /// Camera SoT: SirAldricDemo (0, 2.80, −5.40) LookAt (0, 0.90, 0.50) → view +Z.
        /// SirAldric3DMotion: march / character forward = +Z = screen TOP; rear view = back to camera.
        /// </summary>
        public const float RearYawDegrees = SirAldric3DMotion.MixamoImportRearYawDegrees;

        private Animator? _animator;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _walkPlayable;
        private AnimationClipPlayable _attackPlayable;
        private float _walkLength;
        private float _attackLength;
        private bool _hasAttack;
        private bool _graphReady;
        private GameObject? _instance;

        public bool Built => _graphReady;

        /// <summary>True only when Design has dropped an Attack clip at the convention path (or a named take on the walk FBX). Never a hand-baked fake.</summary>
        public bool HasAttackClip => _hasAttack;

        public void Build()
        {
            BuildLights();
            var prefab = LoadFbxPrefab();
            if (prefab == null)
            {
                Debug.LogError(
                    "Meshy Animate FBX not imported yet. Open the project in Unity so " +
                    ThemePackFbx + " Humanoid-imports, then Play SirAldric.");
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "SirAldricMeshyAnimate";
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

            var walk = LoadWalkingClip();
            if (walk == null)
            {
                Debug.LogError("Meshy Animate FBX has no Walking clip after Humanoid import.");
                return;
            }

            walk.wrapMode = WrapMode.Loop;
            _walkLength = walk.length;
            var attack = LoadAttackClip();
            _hasAttack = attack != null && attack.length > 0.05f;
            if (_hasAttack)
            {
                attack!.wrapMode = WrapMode.Once;
                _attackLength = attack.length;
            }

            _graph = PlayableGraph.Create("SirAldricMeshyAnimate");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "Aldric", _animator);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            _walkPlayable = AnimationClipPlayable.Create(_graph, walk);
            _mixer.ConnectInput(0, _walkPlayable, 0, 1f);
            if (_hasAttack)
            {
                _attackPlayable = AnimationClipPlayable.Create(_graph, attack);
                _mixer.ConnectInput(1, _attackPlayable, 0, 0f);
            }

            output.SetSourcePlayable(_mixer);
            _graph.Play();
            _graphReady = true;
            SampleLoop(0f);
        }

        private void Update()
        {
            if (!_graphReady || _walkLength <= 0f)
            {
                return;
            }

            SampleLoop(Time.unscaledTime);
        }

        private void SampleLoop(float timeSeconds)
        {
            if (!_graph.IsValid())
            {
                return;
            }

            var walkBlock = _walkLength * SirAldric3DMotion.WalkCyclesBeforeAttack;
            var loop = walkBlock + (_hasAttack ? _attackLength : 0f);
            if (loop <= 0f)
            {
                return;
            }

            var t = timeSeconds % loop;
            const float fade = 0.12f;
            if (!_hasAttack || t < walkBlock)
            {
                _mixer.SetInputWeight(0, 1f);
                if (_hasAttack)
                {
                    _mixer.SetInputWeight(1, 0f);
                }

                _walkPlayable.SetTime(t % _walkLength);
            }
            else
            {
                var at = t - walkBlock;
                var wAttack = at < fade ? at / fade : 1f;
                if (at > _attackLength - fade && _attackLength > fade)
                {
                    wAttack = Mathf.Clamp01((_attackLength - at) / fade);
                }

                _mixer.SetInputWeight(0, 1f - wAttack);
                _mixer.SetInputWeight(1, wAttack);
                _walkPlayable.SetTime(walkBlock % _walkLength);
                _attackPlayable.SetTime(Mathf.Clamp(at, 0f, _attackLength));
            }

            _graph.Evaluate();
        }

        public string PhaseLabel(float timeSeconds)
        {
            if (_hasAttack && _walkLength > 0f)
            {
                var walkBlock = _walkLength * SirAldric3DMotion.WalkCyclesBeforeAttack;
                var loop = walkBlock + _attackLength;
                if (loop > 0f && (timeSeconds % loop) >= walkBlock)
                {
                    return "ATTACK  ·  toward TOP  ·  Meshy Animate";
                }
            }

            return "WALK  ·  toward TOP  ·  Meshy Animate";
        }

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
            // Verified (not guessed):
            // - Play cam: SirAldricDemo sets (0, 2.80, −5.40) LookAt (0, 0.90, 0.50) → view +Z.
            // - SirAldric3DMotion: character forward / march = world +Z = screen TOP; enemy +Z.
            // - Chevrons / EnemyTop sit at +Z. WorldMarchLoop is a node timer, not a heading.
            // - Humanoid Mixamo on this FBX instantiates face-to-camera (−Z) = frontal FAIL.
            // Yaw 180° so transform.forward = +Z: back to camera, walk toward TOP.
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.Euler(0f, RearYawDegrees, 0f);
        }

        private static GameObject? LoadFbxPrefab()
        {
#if UNITY_EDITOR
            var data = Application.dataPath;
            var rel = "Assets/" + ThemePackFbx.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            if (go != null)
            {
                return go;
            }

            var abs = Path.Combine(Directory.GetParent(data)!.FullName, rel);
            if (File.Exists(abs))
            {
                AssetDatabase.ImportAsset(rel);
                return AssetDatabase.LoadAssetAtPath<GameObject>(rel);
            }
#endif
            return Resources.Load<GameObject>("sir_aldric_meshy_animate_walk");
        }

        private static string FbxAssetPath => "Assets/" + ThemePackFbx.Replace('\\', '/');

        private static AnimationClip? LoadAttackClip()
        {
#if UNITY_EDITOR
            foreach (var rel in AttackFbxAssetPaths())
            {
                if (AssetImporter.GetAtPath(rel) == null && !File.Exists(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, rel)))
                {
                    continue;
                }

                var clip = PickNamedClip(rel, AttackClipHint, "Slash", "Strike", "Punch");
                if (clip != null)
                {
                    return clip;
                }
            }

            return PickNamedClip(FbxAssetPath, AttackClipHint, "Slash", "Strike", "Punch");
#else
            return null;
#endif
        }

        private static string[] AttackFbxAssetPaths()
        {
            return new[]
            {
                "Assets/" + ThemePackAttackFbx.Replace('\\', '/'),
                "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx",
                "Assets/Survival/Art/sir_aldric_meshy_animate_attack.fbx",
            };
        }

        private static AnimationClip? LoadWalkingClip()
        {
#if UNITY_EDITOR
            var clip = PickWalkingClip(FbxAssetPath);
            if (clip != null)
            {
                return clip;
            }

            // clipAnimations.takeName must match the FBX stack. A short name like
            // "Walking" drops every clip on Humanoid import (mesh stays, clip list empty).
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

                // Walking.001 is a 2-frame stub. Keep the longer cycle.
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

        private static AnimationClip? PickNamedClip(string rel, params string[] hints)
        {
#if UNITY_EDITOR
            AnimationClip? best = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(rel))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                var hit = false;
                foreach (var hint in hints)
                {
                    if (clip.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hit = true;
                        break;
                    }
                }

                if (!hit)
                {
                    continue;
                }

                if (best == null || clip.length > best.length)
                {
                    best = clip;
                }
            }

            return best;
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
        private static Texture2D? _cachedMetallic;
        private static Texture2D? _cachedRoughness;

        private static void PunchMaterials(GameObject root)
        {
            var albedo = LoadMeshyAlbedo();
            var metallic = LoadMeshyPackedMap(grayIndex: 0);
            var roughness = LoadMeshyPackedMap(grayIndex: 1);
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = rend.sharedMaterials;
                if (shared == null || shared.Length == 0)
                {
                    var lone = NewLit();
                    BindMapsAndPunch(lone, albedo, metallic, roughness);
                    rend.material = lone;
                    continue;
                }

                var copies = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++)
                {
                    var src = shared[i] != null ? shared[i] : NewLit();
                    var mat = new Material(src);
                    BindMapsAndPunch(mat, albedo, metallic, roughness);
                    copies[i] = mat;
                }

                rend.materials = copies;
            }
        }

        private static void BindMapsAndPunch(
            Material mat,
            Texture2D? albedo,
            Texture2D? metallic,
            Texture2D? roughness)
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

            if (metallic != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
            }

            if (roughness != null && mat.HasProperty("_SpecGlossMap"))
            {
                mat.SetTexture("_SpecGlossMap", roughness);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor");
                if (c.maxColorComponent < 0.08f)
                {
                    c = Color.white;
                }

                mat.SetColor("_BaseColor", new Color(
                    Mathf.Clamp01(c.r * 1.12f),
                    Mathf.Clamp01(c.g * 1.10f),
                    Mathf.Clamp01(c.b * 1.16f),
                    c.a));
            }

            var hasMetMap = mat.HasProperty("_MetallicGlossMap")
                && mat.GetTexture("_MetallicGlossMap") != null;
            if (mat.HasProperty("_Metallic") && hasMetMap)
            {
                mat.SetFloat("_Metallic", Mathf.Max(mat.GetFloat("_Metallic"), 0.55f));
            }

            if (mat.HasProperty("_Smoothness"))
            {
                var floor = hasMetMap ? 0.68f : 0.52f;
                mat.SetFloat("_Smoothness", Mathf.Max(mat.GetFloat("_Smoothness"), floor));
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

        private static Texture2D? LoadMeshyAlbedo()
        {
            if (_cachedAlbedo != null)
            {
                return _cachedAlbedo;
            }

#if UNITY_EDITOR
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(FbxAssetPath))
            {
                if (obj is not Texture2D tex || tex.width < 256)
                {
                    continue;
                }

                var n = tex.name.ToLowerInvariant();
                if (n.Contains("texture_0")
                    && !n.Contains("metallic")
                    && !n.Contains("rough")
                    && !n.Contains("normal"))
                {
                    _cachedAlbedo = tex;
                    return tex;
                }
            }
#endif
            _cachedAlbedo = ExtractFbxPng(color: true, grayIndex: -1)
                            ?? LoadSiblingPng("sir_aldric_meshy_atlas.png");
            return _cachedAlbedo;
        }

        private static Texture2D? LoadMeshyPackedMap(int grayIndex)
        {
            var cache = grayIndex == 0 ? _cachedMetallic : _cachedRoughness;
            if (cache != null)
            {
                return cache;
            }

            var extracted = ExtractFbxPng(color: false, grayIndex: grayIndex);
            if (grayIndex == 0)
            {
                _cachedMetallic = extracted ?? LoadSiblingPng("sir_aldric_meshy_atlas_metallic.png");
                return _cachedMetallic;
            }

            _cachedRoughness = extracted ?? LoadSiblingPng("sir_aldric_meshy_atlas_roughness.png");
            return _cachedRoughness;
        }

        private static Texture2D? LoadSiblingPng(string fileName)
        {
            var path = ResolveHero3D(fileName);
            if (path == null)
            {
                return null;
            }

#if UNITY_EDITOR
            var rel = "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/" + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
            if (asset != null)
            {
                return asset;
            }
#endif
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

        private static string? ResolveHero3D(string fileName)
        {
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            foreach (var path in new[]
                     {
                         Path.Combine(data, ThemePackFbx.Replace("sir_aldric_meshy_animate_walk.fbx", fileName)),
                         Path.Combine(repo, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", fileName),
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
            foreach (var path in new[]
                     {
                         Path.Combine(data, rel),
                         Path.Combine(repo, "Assets", rel),
                     })
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static Texture2D? ExtractFbxPng(bool color, int grayIndex)
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
                var graySeen = 0;
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
                        var isGray = tex.format == TextureFormat.Alpha8
                                     || tex.format == TextureFormat.R8
                                     || LooksGrayscale(tex);
                        var isNormal = LooksLikeNormalMap(tex);
                        if (color && !isGray && !isNormal)
                        {
                            tex.wrapMode = TextureWrapMode.Clamp;
                            tex.filterMode = FilterMode.Bilinear;
                            tex.name = "meshy_animate_albedo";
                            return tex;
                        }

                        if (!color && isGray)
                        {
                            if (graySeen == grayIndex)
                            {
                                tex.wrapMode = TextureWrapMode.Clamp;
                                tex.filterMode = FilterMode.Bilinear;
                                tex.name = grayIndex == 0 ? "meshy_animate_metallic" : "meshy_animate_roughness";
                                return tex;
                            }

                            graySeen++;
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

        private static bool LooksGrayscale(Texture2D tex)
        {
            var px = tex.GetPixels(tex.width / 4, tex.height / 4, 1, 1);
            if (px.Length == 0)
            {
                return false;
            }

            var c = px[0];
            return Mathf.Abs(c.r - c.g) < 0.02f && Mathf.Abs(c.g - c.b) < 0.02f;
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
            // Flat grey World + dim key was the mute Game-view. Punch in place
            // even if a leftover light already exists.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.29f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.13f, 0.11f);
            RenderSettings.ambientIntensity = 1.25f;
            RenderSettings.reflectionIntensity = 1.15f;

            var key = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (key == null)
            {
                var sun = new GameObject("AldricKeyLight");
                key = sun.AddComponent<Light>();
            }

            key.type = LightType.Directional;
            key.color = new Color(1f, 0.96f, 0.88f);
            key.intensity = 1.85f;
            key.transform.rotation = Quaternion.Euler(52f, -16f, 0f);

            if (UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length < 2)
            {
                var fillGo = new GameObject("AldricFillLight");
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = new Color(0.82f, 0.88f, 1f);
                fill.intensity = 0.55f;
                fillGo.transform.rotation = Quaternion.Euler(70f, 35f, 0f);
            }
        }
    }
}
