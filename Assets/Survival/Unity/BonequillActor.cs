using Survival.Domain.Enemies;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Bonequill fresh rig (2026-10-13): rest, walk and bow attack on the 20-bone body (imported Generic, exact Blender keys), the bone
    /// bow on LeftHand in every clip (idle prop standing in for the attack track), and the separate cape FBX. The cape is instantiated
    /// under the actor, its anchors are reparented onto the body Spine2/shoulders (world pose kept: same rest matrices), and
    /// ClothSpringRig springs its 7 chains with leg colliders in LateUpdate. Cape is never baked. Rejects BONEQUILL_rig.fbx.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class BonequillActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;
        private ClothSpringRig? _cloth;
        private GameObject? _cape;

        public bool Built => _player != null && _player.Built;

        /// <summary>Attack effects driver (arrow flight), for the locked battle's hit blocker.</summary>
        public BlenderRigAttackDriver? AttackDriver => _player?.AttackDriver;
        public string Pose => _player != null ? _player.Pose : BonequillMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);
        public ClothSpringRig? Cloth => _cloth;
        public GameObject? Cape => _cape;

        public void Build()
        {
            _player = new BlenderRigPlayer(BonequillMotion.Spec, transform, BonequillAttack.Spec, BonequillAttack.IdleProps, restAtAttackStart: true);
            _player.Build();
            if (_player.Built)
            {
                BuildCape();
            }
        }

        public bool PlayPose(string poseName)
        {
            var ok = _player != null && _player.PlayPose(poseName);
            _cloth?.ResetToRest();
            return ok;
        }

        private void BuildCape()
        {
#if UNITY_EDITOR
            var dir = "Assets/" + BonequillMotion.ThemePackDir + "/";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dir + BonequillCape.FileName);
            if (prefab == null)
            {
                Debug.LogError("Bonequill cape missing. " + dir + BonequillCape.FileName);
                return;
            }

            // Bind pose: body is in rest right after Build, so cape and body bones coincide.
            _cape = Instantiate(prefab, transform);
            _cape.name = "BonequillCape";
            var capeAnimator = _cape.GetComponent<Animator>();
            if (capeAnimator != null)
            {
                Destroy(capeAnimator);
            }

            var all = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < BonequillCape.Anchors.GetLength(0); i++)
            {
                var anchor = Find(all, BonequillCape.Anchors[i, 0]);
                var body = Find(all, BonequillCape.Anchors[i, 1]);
                if (anchor == null || body == null)
                {
                    Debug.LogError("Bonequill cape anchor missing: " + BonequillCape.Anchors[i, 0] + " -> " + BonequillCape.Anchors[i, 1]);
                    continue;
                }

                anchor.SetParent(body, worldPositionStays: true);
            }

            BindCapeMaterial(dir);
            _cloth = gameObject.AddComponent<ClothSpringRig>();
            if (!_cloth.Bind(BonequillCape.Spec, transform))
            {
                Debug.LogWarning("Bonequill cape springs did not bind fully. Missing: " + string.Join(", ", _cloth.MissingBones));
            }
#endif
        }

        private static Transform? Find(Transform[] all, string name)
        {
            foreach (var t in all)
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void BindCapeMaterial(string dir)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null || _cape == null)
            {
                return;
            }

            var tex = dir + BonequillCape.TextureFolder + "/";
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + BonequillCape.BaseColorFile);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + BonequillCape.NormalFile);
            var mat = new Material(shader) { name = "BONEQUILL_cape" };
            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTexture("_MainTex", albedo);
                mat.EnableKeyword("_BASEMAP");
            }
            else
            {
                Debug.LogError("Bonequill cape basecolor missing. " + tex + BonequillCape.BaseColorFile);
            }

            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            SetIf(mat, "_BaseColor", Color.white);
            SetIf(mat, "_Metallic", 0f);
            SetIf(mat, "_Smoothness", 0.2f);
            SetIf(mat, "_Cull", 0f);       // two-sided: the hood and hem show their inside
            mat.doubleSidedGI = true;
            foreach (var rend in _cape.GetComponentsInChildren<Renderer>(true))
            {
                if (rend is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                }

                var mats = new Material[Mathf.Max(1, rend.sharedMaterials.Length)];
                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }

                rend.sharedMaterials = mats;
            }
        }

        private static void SetIf(Material mat, string name, float value)
        {
            if (mat.HasProperty(name))
            {
                mat.SetFloat(name, value);
            }
        }

        private static void SetIf(Material mat, string name, Color value)
        {
            if (mat.HasProperty(name))
            {
                mat.SetColor(name, value);
            }
        }
#endif

        private void Update()
        {
            _player?.Tick();
        }

        private void OnDestroy()
        {
            _player?.Dispose();
        }
    }
}
