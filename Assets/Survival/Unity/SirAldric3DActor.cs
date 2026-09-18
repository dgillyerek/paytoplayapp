using System.Collections.Generic;
using System.IO;
using Survival.Domain.Flavor;
using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Survival.Unity
{
    /// <summary>
    /// Runtime 3D Aldric: capsule-sculpted skinned mesh + rear look-target albedo,
    /// Animator PlayableGraph walk/attack toward TOP. Not cubes / not PNG warp.
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
            Bone("Cape", chest, new Vector3(0f, 0.08f, -0.12f));
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
            var uvs = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var bodyTris = new List<int>();
            var trimTris = new List<int>();
            var boneList = new List<Transform>();
            var names = new[]
            {
                "Root", "Hips", "Spine", "Chest", "Neck", "Head",
                "Arm_L", "Fore_L", "Hand_L", "Arm_R", "Fore_R", "Hand_R", "Sword",
                "UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R", "Scabbard", "Cape"
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

            void BodyCap(string bone, Vector3 a, Vector3 b, float r)
            {
                AddCapsule(verts, norms, cols, uvs, weights, bodyTris, _bones[bone], index[bone], a, b, r, Color.white, true);
            }

            void TrimCap(string bone, Vector3 a, Vector3 b, float r, Color color)
            {
                AddCapsule(verts, norms, cols, uvs, weights, trimTris, _bones[bone], index[bone], a, b, r, color, false);
            }

            void TrimSph(string bone, Vector3 c, float r, Color color)
            {
                AddSphere(verts, norms, cols, uvs, weights, trimTris, _bones[bone], index[bone], c, r, color, false);
            }

            BodyCap("Head", new Vector3(0f, 0.02f, 0.02f), new Vector3(0f, 0.26f, 0.02f), 0.13f);
            TrimSph("Head", new Vector3(0f, 0.30f, 0f), 0.035f, gold);
            TrimCap("Head", new Vector3(-0.07f, 0.12f, 0.12f), new Vector3(0.07f, 0.12f, 0.12f), 0.03f, dark);
            BodyCap("Neck", new Vector3(0f, -0.04f, 0f), new Vector3(0f, 0.10f, 0f), 0.07f);
            BodyCap("Chest", new Vector3(0f, -0.12f, 0f), new Vector3(0f, 0.16f, 0f), 0.20f);
            TrimSph("Chest", new Vector3(-0.22f, 0.12f, 0f), 0.10f, silver);
            TrimSph("Chest", new Vector3(0.22f, 0.12f, 0f), 0.10f, silver);
            TrimSph("Chest", new Vector3(-0.22f, 0.18f, 0f), 0.045f, gold);
            TrimSph("Chest", new Vector3(0.22f, 0.18f, 0f), 0.045f, gold);
            BodyCap("Spine", new Vector3(0f, -0.04f, 0f), new Vector3(0f, 0.10f, 0f), 0.16f);
            BodyCap("Hips", new Vector3(0f, -0.16f, 0f), new Vector3(0f, 0.06f, 0f), 0.18f);
            TrimCap("Hips", new Vector3(-0.16f, -0.18f, 0f), new Vector3(0.16f, -0.18f, 0f), 0.03f, gold);
            BodyCap("Arm_L", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.26f, 0f), 0.065f);
            BodyCap("Fore_L", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.22f, 0f), 0.055f);
            BodyCap("Hand_L", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.08f, 0f), 0.05f);
            BodyCap("Arm_R", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.26f, 0f), 0.065f);
            BodyCap("Fore_R", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.22f, 0f), 0.055f);
            BodyCap("Hand_R", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.08f, 0f), 0.05f);
            TrimCap("Sword", new Vector3(0.01f, 0.02f, 0.02f), new Vector3(0.01f, -0.58f, 0.04f), 0.022f, silver);
            TrimCap("Sword", new Vector3(-0.06f, 0.04f, 0.02f), new Vector3(0.08f, 0.04f, 0.02f), 0.018f, gold);
            BodyCap("UpLeg_L", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.40f, 0f), 0.085f);
            BodyCap("Leg_L", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.36f, 0f), 0.07f);
            BodyCap("Foot_L", new Vector3(0f, -0.02f, -0.02f), new Vector3(0f, -0.02f, 0.16f), 0.055f);
            TrimSph("Foot_L", new Vector3(0f, 0.02f, 0.04f), 0.03f, gold);
            BodyCap("UpLeg_R", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.40f, 0f), 0.085f);
            BodyCap("Leg_R", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.36f, 0f), 0.07f);
            BodyCap("Foot_R", new Vector3(0f, -0.02f, -0.02f), new Vector3(0f, -0.02f, 0.16f), 0.055f);
            TrimSph("Foot_R", new Vector3(0f, 0.02f, 0.04f), 0.03f, gold);
            TrimCap("Scabbard", new Vector3(0.02f, 0.08f, 0f), new Vector3(0.02f, -0.48f, 0f), 0.032f, brown);
            TrimSph("Scabbard", new Vector3(0.02f, 0.10f, 0f), 0.04f, gold);
            BodyCap("Cape", new Vector3(-0.16f, 0.04f, 0f), new Vector3(0.16f, -0.42f, -0.06f), 0.08f);

            var mesh = new Mesh { name = "SirAldricSculpt" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(bodyTris, 0);
            mesh.SetTriangles(trimTris, 1);
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
            smr.quality = SkinQuality.Bone1;
            var tex = TryLoadRearAlbedo();
            smr.sharedMaterials = new[]
            {
                MakeBody(tex),
                MakeTrim()
            };
        }

        private static void AddCapsule(
            List<Vector3> verts,
            List<Vector3> norms,
            List<Color> cols,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            List<int> tris,
            Transform bone,
            int boneIndex,
            Vector3 a,
            Vector3 b,
            float radius,
            Color color,
            bool bodyTex)
        {
            var axis = b - a;
            var height = axis.magnitude;
            if (height < 1e-5f)
            {
                AddSphere(verts, norms, cols, uvs, weights, tris, bone, boneIndex, a, radius, color, bodyTex);
                return;
            }

            var nY = axis / height;
            var nX = Vector3.Cross(Mathf.Abs(nY.y) < 0.9f ? Vector3.up : Vector3.right, nY).normalized;
            var nZ = Vector3.Cross(nY, nX);
            const int rings = 6;
            const int segs = 8;
            var bw = new BoneWeight { boneIndex0 = boneIndex, weight0 = 1f };
            var ring0 = verts.Count;
            for (var i = 0; i <= rings; i++)
            {
                var t = i / (float)rings;
                var p = Vector3.Lerp(a, b, t);
                for (var s = 0; s < segs; s++)
                {
                    var ang = s / (float)segs * Mathf.PI * 2f;
                    var radial = (Mathf.Cos(ang) * nX) + (Mathf.Sin(ang) * nZ);
                    var local = p + radial * radius;
                    verts.Add(local);
                    norms.Add(radial);
                    cols.Add(color);
                    uvs.Add(bodyTex ? UvOf(bone.TransformPoint(local)) : new Vector2(0.5f, 0.5f));
                    weights.Add(bw);
                }
            }

            for (var i = 0; i < rings; i++)
            {
                for (var s = 0; s < segs; s++)
                {
                    var s1 = (s + 1) % segs;
                    var i0 = ring0 + i * segs + s;
                    var i1 = ring0 + i * segs + s1;
                    var i2 = ring0 + (i + 1) * segs + s;
                    var i3 = ring0 + (i + 1) * segs + s1;
                    tris.Add(i0);
                    tris.Add(i2);
                    tris.Add(i1);
                    tris.Add(i1);
                    tris.Add(i2);
                    tris.Add(i3);
                }
            }

            AddSphere(verts, norms, cols, uvs, weights, tris, bone, boneIndex, a, radius, color, bodyTex);
            AddSphere(verts, norms, cols, uvs, weights, tris, bone, boneIndex, b, radius, color, bodyTex);
        }

        private static void AddSphere(
            List<Vector3> verts,
            List<Vector3> norms,
            List<Color> cols,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            List<int> tris,
            Transform bone,
            int boneIndex,
            Vector3 center,
            float radius,
            Color color,
            bool bodyTex)
        {
            const int slices = 6;
            const int stacks = 5;
            var bw = new BoneWeight { boneIndex0 = boneIndex, weight0 = 1f };
            var start = verts.Count;
            for (var y = 0; y <= stacks; y++)
            {
                var v = y / (float)stacks;
                var phi = v * Mathf.PI;
                for (var x = 0; x <= slices; x++)
                {
                    var u = x / (float)slices;
                    var th = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    var local = center + n * radius;
                    verts.Add(local);
                    norms.Add(n);
                    cols.Add(color);
                    uvs.Add(bodyTex ? UvOf(bone.TransformPoint(local)) : new Vector2(0.5f, 0.5f));
                    weights.Add(bw);
                }
            }

            for (var y = 0; y < stacks; y++)
            {
                for (var x = 0; x < slices; x++)
                {
                    var i0 = start + y * (slices + 1) + x;
                    var i1 = i0 + 1;
                    var i2 = i0 + slices + 1;
                    var i3 = i2 + 1;
                    tris.Add(i0);
                    tris.Add(i2);
                    tris.Add(i1);
                    tris.Add(i1);
                    tris.Add(i2);
                    tris.Add(i3);
                }
            }
        }

        private static Vector2 UvOf(Vector3 bindWorld)
        {
            var u = Mathf.InverseLerp(-0.32f, 0.32f, bindWorld.x);
            var v = Mathf.InverseLerp(0.00f, 1.86f, bindWorld.y);
            return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        }

        private static Texture2D? TryLoadRearAlbedo()
        {
            try
            {
                foreach (var path in SirAldricDemo.ResolveMasterPaths(AppFlavorConfig.FantasyKingdomA))
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(File.ReadAllBytes(path)))
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;
                        return tex;
                    }
                }
            }
            catch (IOException)
            {
            }

            return null;
        }

        private static Material MakeBody(Texture2D? tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", tex);
                }

                if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", tex);
                }
            }

            var white = Color.white;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", white);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", white);
            }

            return mat;
        }

        private static Material MakeTrim()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", Color.white);
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
            sun.transform.rotation = Quaternion.Euler(48f, -18f, 0f);
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
