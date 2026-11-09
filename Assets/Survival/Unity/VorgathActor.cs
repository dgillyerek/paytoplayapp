using Survival.Domain.Enemies;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Plays Vorgath's existing preview clips (idle, walk, slam, spit) on the preview rig. The spit clip also runs
    /// <see cref="VorgathSpitStream"/> from the mouth socket copied off the reference FBX. No art file is rewritten.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class VorgathActor : MonoBehaviour
    {
        private const string AssetRoot = "Assets/";

        private GameObject? _model;
        private AnimationClip? _idle;
        private AnimationClip? _walk;
        private AnimationClip? _slam;
        private AnimationClip? _spit;
        private string _pose = VorgathMotion.RestPoseName;
        private float _time;
        private bool _loop = true;
        private VorgathSpitStream? _stream;
        private Transform? _socket;
        private Vector3 _socketPos;
        private Quaternion _socketRot;
        private Vector3 _socketScale;
        private Bounds _bounds;

        /// <summary>Attack take rate. 1 in the battle. The runner raises the slam so frame 52 lands on the telegraph.</summary>
        public float AttackSpeed { get; set; } = 1f;

        public bool Built { get; private set; }
        public string Pose => _pose;
        public Bounds VisibleBounds => _bounds;

        public void Build()
        {
            var prefab = PlayerAssets.Load<GameObject>(AssetRoot + VorgathMotion.RigRestRel);
            if (prefab == null)
            {
                Debug.LogError("Vorgath rig missing. " + VorgathMotion.RigRestRel);
                return;
            }

            _model = Instantiate(prefab, transform);
            _model.name = "VorgathRig";
            foreach (var animator in _model.GetComponentsInChildren<Animator>(true))
            {
                Destroy(animator);
            }

            var atlas = PlayerAssets.Load<Texture2D>(AssetRoot + VorgathMotion.AtlasRel);
            ApplyAtlas(_model, atlas);
            _idle = LoadClip(AssetRoot + VorgathMotion.IdleRel);
            _walk = LoadClip(AssetRoot + VorgathMotion.WalkRel);
            _slam = LoadClip(AssetRoot + VorgathMotion.SlamRel);
            _spit = LoadClip(AssetRoot + VorgathMotion.SpitRel);
            _bounds = Measure(_model);
            AttachMouth();
            Sample(CurrentClip(), 0f);
            Built = _idle != null && _walk != null && _slam != null && _spit != null;
            if (!Built)
            {
                Debug.LogError("Vorgath clip missing. idle/walk/slam/spit must all load.");
            }
        }

        public bool PlayPose(string poseName)
        {
            if (_model == null)
            {
                return false;
            }

            var clip = ClipFor(poseName);
            if (clip == null)
            {
                Debug.LogWarning("Vorgath unknown pose. " + poseName);
                return false;
            }

            var loop = IsLoop(poseName);
            if (_pose == poseName && _loop == loop)
            {
                return true;
            }

            _pose = poseName;
            _loop = loop;
            _time = 0f;
            Sample(clip, 0f);
            return true;
        }

        private void Update()
        {
            var clip = CurrentClip();
            if (_model == null || clip == null)
            {
                return;
            }

            var rate = _loop ? 1f : Mathf.Max(0.05f, AttackSpeed);
            _time += Time.deltaTime * rate;
            var length = Mathf.Max(0.05f, clip.length);
            var sample = _loop ? Mathf.Repeat(_time, length) : Mathf.Min(_time, length);
            Sample(clip, sample);
            if (_socket != null)
            {
                _socket.localPosition = _socketPos;
                _socket.localRotation = _socketRot;
                _socket.localScale = _socketScale;
            }

            var spit = _pose == VorgathMotion.SpitPoseName;
            _stream?.ApplyClipTime(spit ? sample : -1f, Time.deltaTime);
        }

        private void Sample(AnimationClip? clip, float time)
        {
            if (_model != null && clip != null)
            {
                clip.SampleAnimation(_model, time);
            }
        }

        private AnimationClip? CurrentClip() => ClipFor(_pose);

        private AnimationClip? ClipFor(string poseName)
        {
            if (poseName == VorgathMotion.WalkPoseName)
            {
                return _walk;
            }

            if (poseName == VorgathMotion.SlamPoseName)
            {
                return _slam;
            }

            if (poseName == VorgathMotion.SpitPoseName)
            {
                return _spit;
            }

            if (poseName == VorgathMotion.RestPoseName || poseName == VorgathMotion.IdlePoseName)
            {
                return _idle;
            }

            return null;
        }

        private static bool IsLoop(string poseName) =>
            poseName == VorgathMotion.RestPoseName || poseName == VorgathMotion.IdlePoseName || poseName == VorgathMotion.WalkPoseName;

        private void AttachMouth()
        {
            if (_model == null)
            {
                return;
            }

            Transform? head = null;
            foreach (var t in _model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == VorgathMotion.SnoutBone)
                {
                    head = t;
                    break;
                }
            }

            if (head == null)
            {
                Debug.LogError("Vorgath spit: Bone.004 missing, mouth socket not created.");
                return;
            }

            var socket = new GameObject("VORGATH_mouth_socket").transform;
            socket.SetParent(head, false);
            if (!CopyMouthSocket(socket))
            {
                Destroy(socket.gameObject);
                return;
            }

            _socket = socket;
            _socketPos = socket.localPosition;
            _socketRot = socket.localRotation;
            _socketScale = socket.localScale;
            var prefab = PlayerAssets.Load<GameObject>(VorgathSpitStream.PrefabPath);
            var go = prefab != null ? Instantiate(prefab, socket) : new GameObject("VORGATH_spit_fire_stream");
            if (prefab == null)
            {
                go.transform.SetParent(socket, false);
            }

            go.name = "VORGATH_spit_fire_stream";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            _stream = go.GetComponent<VorgathSpitStream>() ?? go.AddComponent<VorgathSpitStream>();
            _stream.ApplyClipTime(-1f, 0f);
        }

        /// <summary>Copies the imported reference empty onto Bone.004. Never the Blender bone-local numbers.</summary>
        private static bool CopyMouthSocket(Transform dest)
        {
            var prefab = PlayerAssets.Load<GameObject>(AssetRoot + VorgathMotion.MouthSocketRefRel);
            if (prefab == null)
            {
                Debug.LogError("Vorgath spit: missing " + VorgathMotion.MouthSocketRefRel);
                return false;
            }

            var probe = Instantiate(prefab);
            probe.SetActive(false);
            Transform? bone = null;
            Transform? src = null;
            foreach (var t in probe.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == VorgathMotion.SnoutBone)
                {
                    bone = t;
                }

                if (t.name == "VORGATH_mouth_socket")
                {
                    src = t;
                }
            }

            if (src == null || bone == null)
            {
                Debug.LogError("Vorgath spit: reference FBX has no Bone.004 / VORGATH_mouth_socket.");
                Destroy(probe);
                return false;
            }

            if (src.parent == bone)
            {
                dest.localPosition = src.localPosition;
                dest.localRotation = src.localRotation;
                dest.localScale = src.localScale;
            }
            else
            {
                dest.localPosition = bone.InverseTransformPoint(src.position);
                dest.localRotation = Quaternion.Inverse(bone.rotation) * src.rotation;
                dest.localScale = Vector3.one;
            }

            Destroy(probe);
            return true;
        }

        private static AnimationClip? LoadClip(string path)
        {
            foreach (var o in PlayerAssets.LoadAll(path))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                {
                    return c;
                }
            }

            Debug.LogWarning("Vorgath clip missing. " + path);
            return null;
        }

        private static void ApplyAtlas(GameObject go, Texture2D? tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var mat = new Material(shader) { name = "Vorgath_atlas" };
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[Mathf.Max(1, rend.sharedMaterials.Length)];
                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }

                rend.sharedMaterials = mats;
                if (rend is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                }
            }
        }

        private static Bounds Measure(GameObject go)
        {
            var found = false;
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!found)
                {
                    bounds = r.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return found ? bounds : new Bounds(Vector3.up, Vector3.one);
        }
    }
}
