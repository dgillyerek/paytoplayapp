using System.Collections.Generic;
using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Survival.Unity
{
    /// <summary>
    /// Runtime 3D Aldric proxy: skinned blockout + Animator PlayableGraph clips.
    /// Painted mid-poly replaces this mesh later; clips / camera stay.
    /// </summary>
    public sealed class SirAldric3DActor : MonoBehaviour
    {
        public const float CharacterHeight = 1.86f;

        private Animator? _animator;
        private PlayableGraph _graph;
        private bool _graphReady;
        private Transform? _root;
        private readonly Dictionary<string, Transform> _bones = new();

        public bool Built => _graphReady;

        public void Build()
        {
            BuildRig();
            BuildSkinnedMesh();
            BuildLights();
            _animator = gameObject.AddComponent<Animator>();
            var clip = BuildLoopClip();
            _graph = PlayableGraph.Create("SirAldric3D");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
            var output = AnimationPlayableOutput.Create(_graph, "Aldric", _animator);
            var playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetDuration(clip.length);
            output.SetSourcePlayable(playable);
            _graph.Play();
            _graphReady = true;
        }

        public string PhaseLabel(float timeSeconds)
        {
            var pose = SirAldric3DMotion.Evaluate(timeSeconds);
            if (!pose.Attacking)
            {
                return "WALK  ·  toward TOP";
            }

            return pose.StrikeTowardTop ? "ATTACK  ·  strike TOP" : "ATTACK  ·  draw / recover";
        }

        private void OnDestroy()
        {
            if (_graphReady && _graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private void BuildRig()
        {
            _root = Bone("Root", transform, Vector3.zero);
            var hips = Bone("Hips", _root, new Vector3(0f, 0.96f, 0f));
            var spine = Bone("Spine", hips, new Vector3(0f, 0.12f, 0f));
            var chest = Bone("Chest", spine, new Vector3(0f, 0.18f, 0f));
            var neck = Bone("Neck", chest, new Vector3(0f, 0.20f, 0f));
            Bone("Head", neck, new Vector3(0f, 0.10f, 0f));
            var armL = Bone("Arm_L", chest, new Vector3(-0.22f, 0.10f, 0f));
            var foreL = Bone("Fore_L", armL, new Vector3(0f, -0.28f, 0f));
            Bone("Hand_L", foreL, new Vector3(0f, -0.24f, 0f));
            var armR = Bone("Arm_R", chest, new Vector3(0.22f, 0.10f, 0f));
            var foreR = Bone("Fore_R", armR, new Vector3(0f, -0.28f, 0f));
            var handR = Bone("Hand_R", foreR, new Vector3(0f, -0.24f, 0f));
            Bone("Sword", handR, new Vector3(0.02f, -0.08f, 0.02f));
            var upL = Bone("UpLeg_L", hips, new Vector3(-0.11f, -0.04f, 0f));
            var loL = Bone("Leg_L", upL, new Vector3(0f, -0.42f, 0f));
            Bone("Foot_L", loL, new Vector3(0f, -0.40f, 0.05f));
            var upR = Bone("UpLeg_R", hips, new Vector3(0.11f, -0.04f, 0f));
            var loR = Bone("Leg_R", upR, new Vector3(0f, -0.42f, 0f));
            Bone("Foot_R", loR, new Vector3(0f, -0.40f, 0.05f));
            var scabbard = Bone("Scabbard", hips, new Vector3(0.20f, -0.04f, -0.02f));
            scabbard.localRotation = Quaternion.Euler(18f, 0f, 22f);
        }

        private Transform Bone(string name, Transform parent, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = local;
            t.localRotation = Quaternion.identity;
            _bones[name] = t;
            return t;
        }

        private void BuildSkinnedMesh()
        {
            var silver = new Color(0.73f, 0.76f, 0.80f);
            var gold = new Color(0.83f, 0.69f, 0.32f);
            var blue = new Color(0.16f, 0.30f, 0.58f);
            var brown = new Color(0.36f, 0.22f, 0.13f);
            var dark = new Color(0.18f, 0.20f, 0.22f);

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var cols = new List<Color>();
            var weights = new List<BoneWeight>();
            var tris = new List<int>();
            var boneList = new List<Transform>();
            var names = new[]
            {
                "Root", "Hips", "Spine", "Chest", "Neck", "Head",
                "Arm_L", "Fore_L", "Hand_L", "Arm_R", "Fore_R", "Hand_R", "Sword",
                "UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R", "Scabbard"
            };
            foreach (var n in names)
            {
                boneList.Add(_bones[n]);
            }

            var index = new Dictionary<string, int>();
            for (var i = 0; i < names.Length; i++)
            {
                index[names[i]] = i;
            }

            void Box(string bone, Vector3 center, Vector3 size, Color color)
            {
                AddBox(verts, norms, cols, weights, tris, index[bone], center, size, color);
            }

            Box("Head", new Vector3(0f, 0.14f, 0.02f), new Vector3(0.24f, 0.28f, 0.26f), silver);
            Box("Head", new Vector3(0f, 0.30f, 0f), new Vector3(0.04f, 0.10f, 0.04f), gold);
            Box("Head", new Vector3(0f, 0.10f, 0.14f), new Vector3(0.16f, 0.08f, 0.04f), dark);
            Box("Neck", new Vector3(0f, 0.02f, 0f), new Vector3(0.16f, 0.18f, 0.16f), silver);
            Box("Chest", new Vector3(0f, 0.02f, 0f), new Vector3(0.40f, 0.36f, 0.22f), silver);
            Box("Chest", new Vector3(0f, -0.02f, -0.12f), new Vector3(0.28f, 0.28f, 0.04f), blue);
            Box("Chest", new Vector3(0f, 0.02f, -0.135f), new Vector3(0.10f, 0.14f, 0.02f), gold);
            Box("Chest", new Vector3(-0.22f, 0.12f, 0f), new Vector3(0.16f, 0.14f, 0.16f), silver);
            Box("Chest", new Vector3(0.22f, 0.12f, 0f), new Vector3(0.16f, 0.14f, 0.16f), silver);
            Box("Chest", new Vector3(-0.22f, 0.18f, 0f), new Vector3(0.10f, 0.04f, 0.10f), gold);
            Box("Chest", new Vector3(0.22f, 0.18f, 0f), new Vector3(0.10f, 0.04f, 0.10f), gold);
            Box("Spine", new Vector3(0f, 0.02f, 0f), new Vector3(0.30f, 0.16f, 0.18f), silver);
            Box("Hips", new Vector3(0f, -0.06f, 0f), new Vector3(0.36f, 0.22f, 0.20f), blue);
            Box("Hips", new Vector3(0f, -0.16f, 0f), new Vector3(0.38f, 0.05f, 0.18f), gold);
            Box("Arm_L", new Vector3(0f, -0.14f, 0f), new Vector3(0.12f, 0.30f, 0.12f), silver);
            Box("Fore_L", new Vector3(0f, -0.12f, 0f), new Vector3(0.10f, 0.26f, 0.10f), silver);
            Box("Hand_L", new Vector3(0f, -0.04f, 0f), new Vector3(0.10f, 0.10f, 0.10f), silver);
            Box("Arm_R", new Vector3(0f, -0.14f, 0f), new Vector3(0.12f, 0.30f, 0.12f), silver);
            Box("Fore_R", new Vector3(0f, -0.12f, 0f), new Vector3(0.10f, 0.26f, 0.10f), silver);
            Box("Hand_R", new Vector3(0f, -0.04f, 0f), new Vector3(0.10f, 0.10f, 0.10f), silver);
            Box("Sword", new Vector3(0.01f, -0.28f, 0.02f), new Vector3(0.035f, 0.62f, 0.045f), silver);
            Box("Sword", new Vector3(0.01f, 0.04f, 0.02f), new Vector3(0.12f, 0.04f, 0.08f), gold);
            Box("UpLeg_L", new Vector3(0f, -0.20f, 0f), new Vector3(0.15f, 0.44f, 0.16f), silver);
            Box("Leg_L", new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.42f, 0.15f), silver);
            Box("Foot_L", new Vector3(0f, -0.02f, 0.06f), new Vector3(0.13f, 0.08f, 0.24f), silver);
            Box("Foot_L", new Vector3(0f, 0.02f, 0.04f), new Vector3(0.10f, 0.03f, 0.10f), gold);
            Box("UpLeg_R", new Vector3(0f, -0.20f, 0f), new Vector3(0.15f, 0.44f, 0.16f), silver);
            Box("Leg_R", new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.42f, 0.15f), silver);
            Box("Foot_R", new Vector3(0f, -0.02f, 0.06f), new Vector3(0.13f, 0.08f, 0.24f), silver);
            Box("Foot_R", new Vector3(0f, 0.02f, 0.04f), new Vector3(0.10f, 0.03f, 0.10f), gold);
            Box("Scabbard", new Vector3(0.02f, -0.22f, 0f), new Vector3(0.055f, 0.52f, 0.055f), brown);
            Box("Scabbard", new Vector3(0.02f, 0.06f, 0f), new Vector3(0.08f, 0.05f, 0.08f), gold);

            var mesh = new Mesh { name = "SirAldricProxy" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.boneWeights = weights.ToArray();
            var bind = new Matrix4x4[boneList.Count];
            for (var i = 0; i < boneList.Count; i++)
            {
                bind[i] = boneList[i].worldToLocalMatrix * transform.localToWorldMatrix;
            }

            mesh.bindposes = bind;
            mesh.RecalculateBounds();

            var smr = gameObject.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.bones = boneList.ToArray();
            smr.rootBone = _bones["Hips"];
            smr.material = MakeLit(Color.white);
            smr.quality = SkinQuality.Bone1;
        }

        private static void AddBox(
            List<Vector3> verts,
            List<Vector3> norms,
            List<Color> cols,
            List<BoneWeight> weights,
            List<int> tris,
            int bone,
            Vector3 center,
            Vector3 size,
            Color color)
        {
            var e = size * 0.5f;
            var corners = new[]
            {
                center + new Vector3(-e.x, -e.y, -e.z),
                center + new Vector3(e.x, -e.y, -e.z),
                center + new Vector3(e.x, e.y, -e.z),
                center + new Vector3(-e.x, e.y, -e.z),
                center + new Vector3(-e.x, -e.y, e.z),
                center + new Vector3(e.x, -e.y, e.z),
                center + new Vector3(e.x, e.y, e.z),
                center + new Vector3(-e.x, e.y, e.z)
            };
            var faces = new[]
            {
                (0, 1, 2, 3, new Vector3(0, 0, -1)),
                (5, 4, 7, 6, new Vector3(0, 0, 1)),
                (4, 0, 3, 7, new Vector3(-1, 0, 0)),
                (1, 5, 6, 2, new Vector3(1, 0, 0)),
                (3, 2, 6, 7, new Vector3(0, 1, 0)),
                (4, 5, 1, 0, new Vector3(0, -1, 0))
            };
            var bw = new BoneWeight { boneIndex0 = bone, weight0 = 1f };
            foreach (var (a, b, c, d, n) in faces)
            {
                var i0 = verts.Count;
                verts.Add(corners[a]);
                verts.Add(corners[b]);
                verts.Add(corners[c]);
                verts.Add(corners[d]);
                for (var i = 0; i < 4; i++)
                {
                    norms.Add(n);
                    cols.Add(color);
                    weights.Add(bw);
                }

                tris.Add(i0);
                tris.Add(i0 + 1);
                tris.Add(i0 + 2);
                tris.Add(i0);
                tris.Add(i0 + 2);
                tris.Add(i0 + 3);
            }
        }

        private static Material MakeLit(Color tint)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = tint };
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", tint);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", tint);
            }

            return mat;
        }

        private void BuildLights()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var sun = new GameObject("AldricKeyLight");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(42f, -20f, 0f);
        }

        private AnimationClip BuildLoopClip()
        {
            var clip = new AnimationClip
            {
                name = "Aldric_WalkAttackLoop",
                legacy = false,
                frameRate = 30f,
                wrapMode = WrapMode.Loop
            };
            var dt = 1f / 30f;
            var n = Mathf.CeilToInt(SirAldric3DMotion.LoopSeconds / dt);
            var paths = new (string path, System.Func<SirAldric3DMotion.Pose, SirAldric3DMotion.Euler> sel)[]
            {
                ("Root/Hips", p => p.Hips),
                ("Root/Hips/Spine", p => p.Spine),
                ("Root/Hips/Spine/Chest", p => p.Chest),
                ("Root/Hips/Spine/Chest/Neck/Head", p => p.Head),
                ("Root/Hips/Spine/Chest/Arm_L", p => p.ArmL),
                ("Root/Hips/Spine/Chest/Arm_L/Fore_L", p => p.ForeL),
                ("Root/Hips/Spine/Chest/Arm_R", p => p.ArmR),
                ("Root/Hips/Spine/Chest/Arm_R/Fore_R", p => p.ForeR),
                ("Root/Hips/Spine/Chest/Arm_R/Fore_R/Hand_R", p => p.HandR),
                ("Root/Hips/Spine/Chest/Arm_R/Fore_R/Hand_R/Sword", p => p.Sword),
                ("Root/Hips/UpLeg_L", p => p.UpLegL),
                ("Root/Hips/UpLeg_L/Leg_L", p => p.LegL),
                ("Root/Hips/UpLeg_L/Leg_L/Foot_L", p => p.FootL),
                ("Root/Hips/UpLeg_R", p => p.UpLegR),
                ("Root/Hips/UpLeg_R/Leg_R", p => p.LegR),
                ("Root/Hips/UpLeg_R/Leg_R/Foot_R", p => p.FootR)
            };

            foreach (var (path, sel) in paths)
            {
                var cx = new AnimationCurve();
                var cy = new AnimationCurve();
                var cz = new AnimationCurve();
                var cw = new AnimationCurve();
                for (var i = 0; i <= n; i++)
                {
                    var t = Mathf.Min(i * dt, SirAldric3DMotion.LoopSeconds);
                    var e = sel(SirAldric3DMotion.Evaluate(t));
                    var q = Quaternion.Euler(e.X, e.Y, e.Z);
                    cx.AddKey(new Keyframe(t, q.x, 0f, 0f));
                    cy.AddKey(new Keyframe(t, q.y, 0f, 0f));
                    cz.AddKey(new Keyframe(t, q.z, 0f, 0f));
                    cw.AddKey(new Keyframe(t, q.w, 0f, 0f));
                }

                clip.SetCurve(path, typeof(Transform), "localRotation.x", cx);
                clip.SetCurve(path, typeof(Transform), "localRotation.y", cy);
                clip.SetCurve(path, typeof(Transform), "localRotation.z", cz);
                clip.SetCurve(path, typeof(Transform), "localRotation.w", cw);
            }

            var py = new AnimationCurve();
            var pz = new AnimationCurve();
            for (var i = 0; i <= n; i++)
            {
                var t = Mathf.Min(i * dt, SirAldric3DMotion.LoopSeconds);
                var p = SirAldric3DMotion.Evaluate(t);
                py.AddKey(new Keyframe(t, p.RootY, 0f, 0f));
                pz.AddKey(new Keyframe(t, p.RootZ, 0f, 0f));
            }

            clip.SetCurve("Root", typeof(Transform), "localPosition.y", py);
            clip.SetCurve("Root", typeof(Transform), "localPosition.z", pz);
            clip.EnsureQuaternionContinuity();
            return clip;
        }
    }
}
