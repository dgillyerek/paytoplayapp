using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Survival.Domain.Flavor;
using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Survival.Unity
{
    /// <summary>
    /// Runtime 3D Aldric: Path 2 Meshy look (e5b132f paint PASS) + hang skin (FMT v4).
    /// Derek STOP: remesh / capsule-arm Game-view is not the Design gate look.
    /// Animator PlayableGraph walk/attack toward TOP. Motion SoT is Evaluate()
    /// (5916447 gait HOLD — do not edit keys). Editor-less bind is
    /// sir_aldric_meshy.mesh.txt. DCC: sir_aldric_path2_clean.fbx.
    /// Loft midpoly remains as archive. Do not regress bone names.
    /// Play hub PNG stays locked. Walk-with-look is NOT claimed.
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
            Bone("Sword", handR, new Vector3(0.02f, -0.08f, 0.06f));
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
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var tris = new List<int>();
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

            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < names.Length; i++)
            {
                index[names[i]] = i;
            }

            LoadMidPoly(verts, norms, uvs, weights, tris, index);
            // Character-right Scabbard is in the Blender mesh.txt (bone Scabbard).
            // No Cape mesh — short royal-blue surcoat is chest/hips. 02 turnaround is ref only.

            var mesh = new Mesh { name = "SirAldricBlender" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 1;
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
            smr.quality = SkinQuality.Bone4;
            smr.sharedMaterial = MakeAtlas(TryLoadAtlas());
        }

        private void LoadMidPoly(
            List<Vector3> verts,
            List<Vector3> norms,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            List<int> tris,
            Dictionary<string, int> index)
        {
            var path = ResolveHero3D("sir_aldric_meshy.mesh.txt")
                       ?? ResolveHero3D("sir_aldric_midpoly.mesh.txt");
            if (path == null || !File.Exists(path))
            {
                throw new FileNotFoundException("Sir Aldric Meshy mesh.txt missing under ThemePack art/heroes/3d/");
            }

            var bone = "Hips";
            var pending = new List<int>();
            foreach (var raw in File.ReadLines(path))
            {
                if (raw.Length == 0 || raw[0] == '#')
                {
                    continue;
                }

                if (raw.StartsWith("BONE ", StringComparison.Ordinal))
                {
                    bone = raw.Substring(5).Trim();
                    continue;
                }

                if (raw.StartsWith("V ", StringComparison.Ordinal))
                {
                    var p = raw.Substring(2).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var ic = CultureInfo.InvariantCulture;
                    verts.Add(new Vector3(float.Parse(p[0], ic), float.Parse(p[1], ic), float.Parse(p[2], ic)));
                    norms.Add(new Vector3(float.Parse(p[3], ic), float.Parse(p[4], ic), float.Parse(p[5], ic)));
                    uvs.Add(new Vector2(float.Parse(p[6], ic), float.Parse(p[7], ic)));
                    weights.Add(ParseBoneWeight(p, bone, index));
                    pending.Add(verts.Count - 1);
                    continue;
                }

                if (raw == "T" && pending.Count >= 3)
                {
                    var i0 = pending[pending.Count - 3];
                    var i1 = pending[pending.Count - 2];
                    var i2 = pending[pending.Count - 1];
                    tris.Add(i0);
                    tris.Add(i1);
                    tris.Add(i2);
                }
            }
        }

        private static BoneWeight ParseBoneWeight(
            string[] p,
            string fallbackBone,
            Dictionary<string, int> index)
        {
            var ic = CultureInfo.InvariantCulture;
            var names = new string[4];
            var ws = new float[4];
            var n = 0;
            for (var k = 8; k < p.Length && n < 4; k++)
            {
                var pair = p[k];
                var colon = pair.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                var name = pair.Substring(0, colon);
                if (!index.TryGetValue(name, out _))
                {
                    continue;
                }

                names[n] = name;
                ws[n] = float.Parse(pair.Substring(colon + 1), ic);
                n++;
            }

            if (n == 0)
            {
                var bi = index.TryGetValue(fallbackBone, out var b) ? b : index["Hips"];
                return new BoneWeight { boneIndex0 = bi, weight0 = 1f };
            }

            var sum = 0f;
            for (var i = 0; i < n; i++)
            {
                sum += ws[i];
            }

            if (sum <= 1e-6f)
            {
                sum = 1f;
            }

            var bw = new BoneWeight();
            if (n > 0)
            {
                bw.boneIndex0 = index[names[0]];
                bw.weight0 = ws[0] / sum;
            }

            if (n > 1)
            {
                bw.boneIndex1 = index[names[1]];
                bw.weight1 = ws[1] / sum;
            }

            if (n > 2)
            {
                bw.boneIndex2 = index[names[2]];
                bw.weight2 = ws[2] / sum;
            }

            if (n > 3)
            {
                bw.boneIndex3 = index[names[3]];
                bw.weight3 = ws[3] / sum;
            }

            return bw;
        }

        private static string? ResolveHero3D(string fileName)
        {
            var packRoot = SurvivalArt.ResolvePackRoot(AppFlavorConfig.FantasyKingdomA);
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            foreach (var path in new[]
                     {
                         Path.Combine(packRoot, "art", "heroes", "3d", fileName),
                         Path.Combine(repo, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "heroes", "3d", fileName)
                     })
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static Texture2D? TryLoadAtlas()
        {
            try
            {
                var path = ResolveHero3D("sir_aldric_meshy_atlas.png")
                           ?? ResolveHero3D("sir_aldric_atlas.png");
                if (path == null)
                {
                    return null;
                }

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    return tex;
                }
            }
            catch (IOException)
            {
            }

            return null;
        }

        private static Material MakeAtlas(Texture2D? tex)
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
