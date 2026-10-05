using Survival.Domain.Enemies;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Stands the Ironhowl Mixamo T-pose. No clips. No texture is invented.
    /// The look JPEG is a Design reference and is not bound.
    /// Do not bake axis conversion, do not scale one axis, do not retarget,
    /// and do not write the imported root rotation.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class IronhowlActor : MonoBehaviour
    {
        private GameObject? _instance;
        private bool _ready;

        public bool Built => _ready;
        public Bounds VisibleBounds { get; private set; }

        public void Build()
        {
            BuildLights();
#if UNITY_EDITOR
            var path = "Assets/" + IronhowlMotion.BodyThemePackRel;
            EnsureImport(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("Ironhowl rig missing. " + path);
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.name = "IronhowlRig";
            var animator = _instance.GetComponent<Animator>() ?? _instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            if (animator.avatar != null && animator.avatar.isHuman)
            {
                Debug.LogWarning("Ironhowl: ignoring a human avatar so the Mixamo T-pose is not retargeted.");
                animator.avatar = null;
            }

            EnableLit(_instance);
            VisibleBounds = Encapsulate(_instance);
            _ready = true;
            Debug.Log("Ironhowl T-pose. No clips. bounds " + VisibleBounds.size);
#else
            Debug.LogError("Ironhowl plays in the Editor Game view.");
#endif
        }

#if UNITY_EDITOR
        private static void EnsureImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                return;
            }

            var dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }

            if (importer.useFileScale)
            {
                importer.useFileScale = false;
                dirty = true;
            }

            if (Mathf.Abs(importer.globalScale - 1f) > 0.001f)
            {
                importer.globalScale = 1f;
                dirty = true;
            }

            if (importer.optimizeBones)
            {
                importer.optimizeBones = false;
                dirty = true;
            }

            if (importer.bakeAxisConversion)
            {
                importer.bakeAxisConversion = false;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
#endif

        private static void EnableLit(GameObject root)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var color = new Color(0.45f, 0.44f, 0.46f, 1f);
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                rend.enabled = true;
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                mat.color = color;
                rend.sharedMaterial = mat;
            }
        }

        private static Bounds Encapsulate(GameObject root)
        {
            var seeded = false;
            var bounds = new Bounds(root.transform.position, Vector3.one);
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!seeded)
                {
                    bounds = rend.bounds;
                    seeded = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }

            return bounds;
        }

        private static void BuildLights()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.36f, 0.34f, 1f);
            var key = new GameObject("IronhowlKeyLight");
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f, 1f);
            light.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(42f, 170f, 0f);
            var fillGo = new GameObject("IronhowlFillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.color = new Color(0.72f, 0.7f, 0.84f, 1f);
            fillGo.transform.rotation = Quaternion.Euler(18f, -20f, 0f);
        }
    }
}
