using Survival.Domain.Heroes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Rowan A-pose rig (2026-10-14): rest, walk and bow attack on the 20-bone body (imported Generic, exact Blender keys), the single
    /// bow on LeftHand in every clip (idle prop standing in for the attack track), and the separate coat-tails FBX. The coat is
    /// instantiated under the actor, coat_Hips is reparented onto the body Hips (world pose kept: same rest matrix), and ClothSpringRig
    /// springs its 7 chains with leg/hips/arm colliders in LateUpdate. Coat is never baked. Rejects ROWAN_rig.fbx.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class RowanActor : MonoBehaviour, IBlenderRigPlayback
    {
        private BlenderRigPlayer? _player;
        private ClothSpringRig? _cloth;
        private GameObject? _coat;

        public bool Built => _player != null && _player.Built;
        public string Pose => _player != null ? _player.Pose : RowanMotion.RestPoseName;
        public float ClipLength => _player != null ? _player.ClipLength : 0f;
        public Bounds VisibleBounds => _player != null ? _player.VisibleBounds : new Bounds(Vector3.zero, Vector3.one);
        public ClothSpringRig? Cloth => _cloth;
        public GameObject? Coat => _coat;

        public void Build()
        {
            _player = new BlenderRigPlayer(RowanMotion.Spec, transform, RowanAttack.Spec, RowanAttack.IdleProps, restAtAttackStart: true);
            _player.Build();
            if (_player.Built)
            {
                BuildCoat();
            }
        }

        public bool PlayPose(string poseName)
        {
            var ok = _player != null && _player.PlayPose(poseName);
            _cloth?.ResetToRest();
            return ok;
        }

        private void BuildCoat()
        {
#if UNITY_EDITOR
            var dir = "Assets/" + RowanMotion.ThemePackDir + "/";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dir + RowanCoat.FileName);
            if (prefab == null)
            {
                Debug.LogError("Rowan coat missing. " + dir + RowanCoat.FileName);
                return;
            }

            // Bind pose: body is in rest right after Build, so coat and body bones coincide.
            _coat = Instantiate(prefab, transform);
            _coat.name = "RowanCoat";
            var coatAnimator = _coat.GetComponent<Animator>();
            if (coatAnimator != null)
            {
                Destroy(coatAnimator);
            }

            var all = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < RowanCoat.Anchors.GetLength(0); i++)
            {
                var anchor = Find(all, RowanCoat.Anchors[i, 0]);
                var body = Find(all, RowanCoat.Anchors[i, 1]);
                if (anchor == null || body == null)
                {
                    Debug.LogError("Rowan coat anchor missing: " + RowanCoat.Anchors[i, 0] + " -> " + RowanCoat.Anchors[i, 1]);
                    continue;
                }

                anchor.SetParent(body, worldPositionStays: true);
            }

            BindCoatMaterial(dir);
            _cloth = gameObject.AddComponent<ClothSpringRig>();
            if (!_cloth.Bind(RowanCoat.Spec, transform))
            {
                Debug.LogWarning("Rowan coat springs did not bind fully. Missing: " + string.Join(", ", _cloth.MissingBones));
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
        private void BindCoatMaterial(string dir)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null || _coat == null)
            {
                return;
            }

            var tex = dir + RowanCoat.TextureFolder + "/";
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + RowanCoat.BaseColorFile);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + RowanCoat.NormalFile);
            var mat = new Material(shader) { name = "ROWAN_coat" };
            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTexture("_MainTex", albedo);
                mat.EnableKeyword("_BASEMAP");
            }
            else
            {
                Debug.LogError("Rowan coat basecolor missing. " + tex + RowanCoat.BaseColorFile);
            }

            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            SetIf(mat, "_BaseColor", Color.white);
            SetIf(mat, "_Metallic", 0f);
            SetIf(mat, "_Smoothness", 0.2f);
            SetIf(mat, "_Cull", 0f);       // two-sided: the hem shows its inside
            mat.doubleSidedGI = true;
            foreach (var rend in _coat.GetComponentsInChildren<Renderer>(true))
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
