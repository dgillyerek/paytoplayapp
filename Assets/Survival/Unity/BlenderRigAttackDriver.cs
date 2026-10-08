using System.Collections.Generic;
using Survival.Domain.Roster;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Survival.Unity
{
    /// <summary>
    /// Drives the Design Theme A attack add-on props and effects next to a Blender rig.
    /// Held props ride their attach bone with the attack_meta offset. Projectiles spawn at the
    /// release frame at the Design spawn point and fly character-forward (+Z, top of the screen
    /// in the rear camera) at the Design speed, then despawn at the end of the clip. Design VFX
    /// meshes are look reference only, so effects are procedural URP particles, trails, and lines
    /// in the looksheet colours. Design space is mapped onto the imported rig from rest bone
    /// heads, so FBX unit scale and axis import settings do not matter.
    /// </summary>
    public sealed class BlenderRigAttackDriver
    {
        private readonly BlenderRigAttackSpec _spec;
        private readonly string _themePackDir;
        private readonly Transform _actor;
        private readonly List<TrackView> _views = new List<TrackView>();
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
        private Material? _additive;
        private Texture2D? _dot;
        private float _scale = 1f;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _offset;
        private bool _calibrated;
        private bool _built;
        private int _cycle = -1;
        private int _lightningStep = -1;

        public BlenderRigAttackDriver(BlenderRigAttackSpec spec, string themePackDir, Transform actor)
        {
            _spec = spec;
            _themePackDir = themePackDir;
            _actor = actor;
        }

        /// <summary>Unity units per Design metre on this import.</summary>
        public float Scale => _scale;

        public bool Calibrated => _calibrated;

        /// <summary>Call right after the rig is instantiated, before any clip evaluates (bind = Design rest).</summary>
        public void Calibrate(GameObject rig)
        {
            _bones.Clear();
            foreach (var t in rig.GetComponentsInChildren<Transform>(true))
            {
                if (!_bones.ContainsKey(t.name))
                {
                    _bones.Add(t.name, t);
                }
            }

            var design = new List<Vector3>();
            var unity = new List<Vector3>();
            var names = _spec.CalibrationBones;
            for (var i = 0; i < names.Length; i++)
            {
                if (!_bones.TryGetValue(names[i], out var bone))
                {
                    Debug.LogWarning(_spec.Character + " attack calibration bone missing. " + names[i]);
                    continue;
                }

                design.Add(new Vector3(
                    _spec.CalibrationRest[(i * 3) + 0],
                    _spec.CalibrationRest[(i * 3) + 1],
                    _spec.CalibrationRest[(i * 3) + 2]));
                unity.Add(_actor.InverseTransformPoint(bone.position));
            }

            if (design.Count < 3)
            {
                Debug.LogError(_spec.Character + " attack calibration needs 3 bones. Props use Design metres as-is.");
                _scale = 1f;
                _rotation = Quaternion.identity;
                _offset = Vector3.zero;
                _calibrated = false;
                return;
            }

            var cd = Vector3.zero;
            var cu = Vector3.zero;
            for (var i = 0; i < design.Count; i++)
            {
                cd += design[i];
                cu += unity[i];
            }

            cd /= design.Count;
            cu /= design.Count;
            var sd = 0f;
            var su = 0f;
            for (var i = 0; i < design.Count; i++)
            {
                sd += (design[i] - cd).sqrMagnitude;
                su += (unity[i] - cu).sqrMagnitude;
            }

            _scale = sd > 1e-10f ? Mathf.Sqrt(su / sd) : 1f;
            var lateralD = design.Count >= 4 ? design[3] - design[2] : design[2] - design[0];
            var lateralU = unity.Count >= 4 ? unity[3] - unity[2] : unity[2] - unity[0];
            var qd = Frame(design[1] - design[0], lateralD);
            var qu = Frame(unity[1] - unity[0], lateralU);
            _rotation = qu * Quaternion.Inverse(qd);
            _offset = cu - (_scale * (_rotation * cd));
            _calibrated = true;
            var worst = 0f;
            for (var i = 0; i < design.Count; i++)
            {
                worst = Mathf.Max(worst, (MapLocal(design[i]) - unity[i]).magnitude);
            }

            Debug.Log(
                _spec.Character + " attack calibration: " + _scale.ToString("0.####") + " units per Design metre, rotation " +
                _rotation.eulerAngles.ToString("F1") + ", worst rest bone residual " +
                (worst / Mathf.Max(_scale, 1e-6f) * 100f).ToString("0.0") + " cm.");
        }

        /// <summary>Builds props and effects on first use, then binds held props at their ref frames.</summary>
        public void Activate(System.Action<float> sampleFrame)
        {
            if (!_built)
            {
                Build();
            }

            foreach (var view in _views)
            {
                if (view.Track.Kind != AttackTrackKind.Held || view.Bone == null)
                {
                    continue;
                }

                sampleFrame(view.Track.RefFrame);
                var key = view.Track.Frames[Mathf.Clamp(view.Track.RefFrame, 0, view.Track.Frames.Length - 1)];
                var worldPos = WorldPoint(key);
                var worldRot = WorldRotation(key);
                view.BoneLocalPos = view.Bone.InverseTransformPoint(worldPos);
                view.BoneLocalRot = Quaternion.Inverse(view.Bone.rotation) * worldRot;
                view.BoneBound = true;
            }

            sampleFrame(BlenderRigAttackSpec.FirstFrame);
            _cycle = -1;
            foreach (var view in _views)
            {
                view.Root.SetActive(true);
                SetShown(view, false, true);
            }
        }

        /// <summary>Call after the rig evaluated <paramref name="frame"/>.</summary>
        public void Tick(float frame, bool inClip, int cycle)
        {
            if (!_built)
            {
                return;
            }

            var newCycle = cycle != _cycle;
            _cycle = cycle;
            var step = Mathf.FloorToInt(frame / 2f);
            var reroll = step != _lightningStep;
            _lightningStep = step;
            foreach (var view in _views)
            {
                var track = view.Track;
                bool visible;
                if (track.Kind == AttackTrackKind.Held)
                {
                    visible = track.VisibleAt(frame);
                }
                else
                {
                    visible = inClip && frame < BlenderRigAttackSpec.LastFrame && track.VisibleAt(frame);
                }

                if (newCycle && view.Shown && track.Kind != AttackTrackKind.Held)
                {
                    SetShown(view, false, true);
                }

                if (!visible)
                {
                    if (view.Shown)
                    {
                        SetShown(view, false, false);
                    }

                    continue;
                }

                var key = track.Sample(frame);
                Place(view, key);
                if (!view.Shown)
                {
                    SetShown(view, true, true);
                }

                if (track.Shape == AttackVfxShape.Lightning && reroll)
                {
                    RollLightning(view);
                }

                if (track.Shape == AttackVfxShape.Slash)
                {
                    SweepSlash(view, frame);
                }
            }
        }

        /// <summary>Hides every prop and effect (another pose was picked).</summary>
        public void Deactivate()
        {
            foreach (var view in _views)
            {
                SetShown(view, false, true);
                view.Root.SetActive(false);
            }

            _cycle = -1;
        }

        public void Dispose()
        {
            foreach (var view in _views)
            {
                if (view.Root != null)
                {
                    Object.Destroy(view.Root);
                }
            }

            _views.Clear();
            if (_additive != null)
            {
                Object.Destroy(_additive);
            }

            if (_dot != null)
            {
                Object.Destroy(_dot);
            }

            _built = false;
        }

        private Vector3 MapLocal(Vector3 design)
        {
            return _offset + (_scale * (_rotation * design));
        }

        private Vector3 WorldPoint(AttackFrame key)
        {
            return _actor.TransformPoint(MapLocal(new Vector3(key.X, key.Y, key.Z)));
        }

        private Quaternion WorldRotation(AttackFrame key)
        {
            var q = new Quaternion(key.Qx, key.Qy, key.Qz, key.Qw);
            return _actor.rotation * _rotation * q;
        }

        private static Quaternion Frame(Vector3 primary, Vector3 lateral)
        {
            if (primary.sqrMagnitude < 1e-12f)
            {
                primary = Vector3.up;
            }

            var p = primary.normalized;
            var up = Vector3.ProjectOnPlane(lateral, p);
            if (up.sqrMagnitude < 1e-12f)
            {
                up = Vector3.ProjectOnPlane(Vector3.right, p);
            }

            return Quaternion.LookRotation(p, up.normalized);
        }

        private void Place(TrackView view, AttackFrame key)
        {
            var t = view.Root.transform;
            if (view.Track.Kind == AttackTrackKind.Held && view.BoneBound && view.Bone != null)
            {
                t.SetPositionAndRotation(view.Bone.TransformPoint(view.BoneLocalPos), view.Bone.rotation * view.BoneLocalRot);
            }
            else
            {
                t.SetPositionAndRotation(WorldPoint(key), WorldRotation(key));
            }

            var s = Mathf.Max(key.Scale, 0.001f);
            t.localScale = new Vector3(s, s, s);
        }

        private void SetShown(TrackView view, bool shown, bool clear)
        {
            view.Shown = shown;
            if (view.Mesh != null)
            {
                view.Mesh.SetActive(shown);
            }

            foreach (var ps in view.Particles)
            {
                if (shown)
                {
                    if (clear)
                    {
                        ps.Clear(true);
                    }

                    ps.Play(true);
                }
                else
                {
                    ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
                }
            }

            foreach (var trail in view.Trails)
            {
                if (shown && clear)
                {
                    trail.Clear();
                }

                trail.emitting = shown;
                if (!shown && clear)
                {
                    trail.Clear();
                }
            }

            foreach (var line in view.Lines)
            {
                line.enabled = shown;
            }
        }

        private void Build()
        {
            _built = true;
            _additive = MakeAdditive();
            foreach (var track in _spec.Tracks)
            {
                var root = new GameObject(_spec.Character + "Attack_" + track.ObjectName);
                root.transform.SetParent(_actor, false);
                var view = new TrackView(track, root);
                if (track.Kind == AttackTrackKind.Held && track.Bone.Length > 0)
                {
                    if (_bones.TryGetValue(track.Bone, out var bone))
                    {
                        view.Bone = bone;
                    }
                    else
                    {
                        Debug.LogError(_spec.Character + " attack attach bone missing. " + track.Bone);
                    }
                }

                if (track.HasMesh)
                {
                    view.Mesh = LoadProp(track, root);
                }

                BuildEffect(view);
                _views.Add(view);
                SetShown(view, false, true);
                root.SetActive(false);
            }
        }

        private GameObject? LoadProp(AttackTrack track, GameObject root)
        {
#if UNITY_EDITOR
            var path = "Assets/" + _themePackDir + "/" + track.PropFileName;
            EnsurePropImport(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError(_spec.Character + " attack prop missing. " + path);
                return null;
            }

            var holder = new GameObject("Prop");
            var instance = Object.Instantiate(prefab, holder.transform);
            instance.name = track.ObjectName;
            foreach (var anim in instance.GetComponentsInChildren<Animator>(true))
            {
                Object.Destroy(anim);
            }

            var measured = Measure(holder.transform, instance);
            var expected = Mathf.Max(track.PropExtent[0], Mathf.Max(track.PropExtent[1], track.PropExtent[2]));
            var got = Mathf.Max(measured.x, Mathf.Max(measured.y, measured.z));
            var k = expected > 1e-6f && got > 1e-6f ? got / expected : 1f;
            if (LongAxis(measured) != LongAxis(new Vector3(track.PropExtent[0], track.PropExtent[1], track.PropExtent[2])))
            {
                Debug.LogWarning(
                    _spec.Character + " attack prop " + track.ObjectName + " long axis differs from Design. measured " +
                    measured.ToString("F3") + " Design metres " + track.PropExtent[0].ToString("F3") + "," +
                    track.PropExtent[1].ToString("F3") + "," + track.PropExtent[2].ToString("F3"));
            }

            var baseScale = _scale / k;
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = new Vector3(baseScale, baseScale, baseScale);
            FixPropMaterials(instance);
            return holder;
#else
            return null;
#endif
        }

        private static int LongAxis(Vector3 v)
        {
            if (v.x >= v.y && v.x >= v.z)
            {
                return 0;
            }

            return v.y >= v.z ? 1 : 2;
        }

        private static Vector3 Measure(Transform space, GameObject instance)
        {
            var seeded = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var m = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var b = filter.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!seeded)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        seeded = true;
                    }
                    else
                    {
                        bounds.Encapsulate(p);
                    }
                }
            }

            return bounds.size;
        }

        private static void FixPropMaterials(GameObject root)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                rend.shadowCastingMode = ShadowCastingMode.On;
                var shared = rend.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    if (lit == null)
                    {
                        continue;
                    }

                    var color = Color.white;
                    if (mat != null)
                    {
                        if (mat.HasProperty("_BaseColor"))
                        {
                            color = mat.GetColor("_BaseColor");
                        }
                        else if (mat.HasProperty("_Color"))
                        {
                            color = mat.GetColor("_Color");
                        }
                    }

                    if (mat == null || mat.shader == null || mat.shader.name.IndexOf("Universal", System.StringComparison.Ordinal) < 0)
                    {
                        var next = new Material(lit);
                        next.name = mat != null ? mat.name : "AttackProp";
                        if (next.HasProperty("_BaseColor"))
                        {
                            next.SetColor("_BaseColor", color);
                        }

                        next.color = color;
                        mat = next;
                        shared[i] = mat;
                    }

                    var lower = mat.name.ToLowerInvariant();
                    if (lower.Contains("crystal") || lower.Contains("rune") || lower.Contains("spiral") || lower.Contains("band"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", color * 1.4f);
                        }
                    }
                }

                rend.sharedMaterials = shared;
            }
        }

#if UNITY_EDITOR
        private void EnsurePropImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError(_spec.Character + " attack prop importer missing. " + path);
                return;
            }

            var dirty = false;
            if (importer.animationType != ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.None;
                dirty = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                dirty = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
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

        private void BuildEffect(TrackView view)
        {
            var track = view.Track;
            var s = _scale;
            var core = Rgb(track.CoreRgb, 1f);
            var glow = Rgb(track.GlowRgb, 0.85f);
            var edge = Rgb(track.EdgeRgb, 0.5f);
            var size = Mathf.Max(track.SizeMetres, 0.02f) * s;
            var length = Mathf.Max(track.LengthMetres, 0.05f) * s;
            var particles = new List<ParticleSystem>();
            var trails = new List<TrailRenderer>();
            var lines = new List<LineRenderer>();
            switch (track.Shape)
            {
                case AttackVfxShape.Orb:
                    particles.Add(AddParticles(view.Root, "Core", false, 70f, 0.12f, 0f, size * 0.55f, size * 0.85f, core, glow, edge, ParticleSystemShapeType.Sphere, size * 0.05f, 0f, 0.7f));
                    particles.Add(AddParticles(view.Root, "Embers", true, 90f, 0.35f, 0.35f * s, size * 0.08f, size * 0.22f, glow, edge, edge, ParticleSystemShapeType.Sphere, size * 0.35f, 0f, 0.3f));
                    trails.Add(AddTrail(view.Root, "Trail", 0.18f, size * 0.6f, glow, edge));
                    break;
                case AttackVfxShape.Trail:
                    trails.Add(AddTrail(view.Root, "Trail", 0.14f, Mathf.Max(size, 0.035f * s), core, glow));
                    particles.Add(AddParticles(view.Root, "Sparks", true, 45f, 0.25f, 0.2f * s, 0.02f * s, 0.05f * s, core, glow, edge, ParticleSystemShapeType.Sphere, 0.02f * s, 0f, 0.2f));
                    break;
                case AttackVfxShape.Flare:
                    var cone = AddParticles(view.Root, "Breath", true, 160f, 0.22f, length / 0.22f, size * 0.25f, size * 0.5f, core, glow, edge, ParticleSystemShapeType.Cone, size * 0.08f, 16f, 1.8f);
                    particles.Add(cone);
                    particles.Add(AddParticles(view.Root, "MouthGlow", false, 50f, 0.1f, 0f, size * 0.45f, size * 0.7f, core, glow, edge, ParticleSystemShapeType.Sphere, size * 0.02f, 0f, 0.8f));
                    break;
                case AttackVfxShape.Lightning:
                    lines.Add(AddLine(view.Root, "BoltGlow", 12, 0.12f * s, glow, edge));
                    lines.Add(AddLine(view.Root, "BoltCore", 12, 0.035f * s, core, glow));
                    particles.Add(AddParticles(view.Root, "Sparks", true, 80f, 0.3f, 0.8f * s, 0.02f * s, 0.06f * s, Rgb(track.EdgeRgb, 1f), glow, glow, ParticleSystemShapeType.Sphere, 0.08f * s, 0f, 0.2f));
                    view.LineLength = length;
                    break;
                case AttackVfxShape.Slash:
                    var tips = new List<Transform>();
                    for (var i = 0; i < 3; i++)
                    {
                        var tip = new GameObject("Claw" + i);
                        tip.transform.SetParent(view.Root.transform, false);
                        trails.Add(AddTrail(tip, "Trail", 0.16f, 0.035f * s, core, glow));
                        tips.Add(tip.transform);
                    }

                    view.SlashTips = tips.ToArray();
                    view.SlashWidth = Mathf.Max(track.PropExtent[0], 0.2f) * s;
                    view.SlashHeight = Mathf.Max(track.PropExtent[1], 0.1f) * s;
                    particles.Add(AddParticles(view.Root, "Sparks", true, 60f, 0.25f, 0.6f * s, 0.02f * s, 0.05f * s, core, glow, edge, ParticleSystemShapeType.Sphere, view.SlashWidth * 0.3f, 0f, 0.2f));
                    break;
            }

            view.Particles = particles.ToArray();
            view.Trails = trails.ToArray();
            view.Lines = lines.ToArray();
        }

        private void RollLightning(TrackView view)
        {
            var count = 12;
            var positions = new Vector3[count];
            var jitter = view.LineLength * 0.035f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1);
                var j = i == 0 || i == count - 1 ? 0f : jitter;
                positions[i] = new Vector3(Random.Range(-j, j), Random.Range(-j, j), t * view.LineLength);
            }

            foreach (var line in view.Lines)
            {
                line.positionCount = count;
                line.SetPositions(positions);
            }
        }

        private void SweepSlash(TrackView view, float frame)
        {
            if (view.SlashTips.Length == 0)
            {
                return;
            }

            var first = view.Track.FirstVisibleFrame;
            var last = view.Track.LastVisibleFrame;
            var span = Mathf.Max(1f, last - first);
            var p = Mathf.Clamp01((frame - first) / (span * 0.75f));
            var w = view.SlashWidth;
            var h = view.SlashHeight;
            for (var i = 0; i < view.SlashTips.Length; i++)
            {
                var lane = (i - 1) * h * 0.22f;
                var x = Mathf.Lerp(w * 0.5f, -w * 0.5f, p);
                var y = (h * 0.35f * Mathf.Cos(p * Mathf.PI)) + lane;
                var z = Mathf.Sin(p * Mathf.PI) * h * 0.25f;
                view.SlashTips[i].localPosition = new Vector3(x, y, z);
            }
        }

        private ParticleSystem AddParticles(
            GameObject host,
            string name,
            bool worldSpace,
            float rate,
            float lifetime,
            float speed,
            float sizeMin,
            float sizeMax,
            Color a,
            Color b,
            Color c,
            ParticleSystemShapeType shapeType,
            float radius,
            float angle,
            float sizeEnd)
        {
            var go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = Color.white;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.radius = Mathf.Max(radius, 0.0001f);
            shape.angle = angle;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(a, b, c));
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, sizeEnd));
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = _additive;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return ps;
        }

        private TrailRenderer AddTrail(GameObject host, string name, float time, float width, Color a, Color b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = time;
            trail.widthMultiplier = width;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.colorGradient = Fade(a, b, b);
            trail.sharedMaterial = _additive;
            trail.minVertexDistance = Mathf.Max(0.005f, width * 0.15f);
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            return trail;
        }

        private LineRenderer AddLine(GameObject host, string name, int count, float width, Color a, Color b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = count;
            line.widthMultiplier = width;
            line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 1f) });
            line.colorGradient = g;
            line.sharedMaterial = _additive;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private static Gradient Fade(Color a, Color b, Color c)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.45f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(a.a, 0f), new GradientAlphaKey(b.a * 0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static Color Rgb(float[] rgb, float alpha)
        {
            if (rgb == null || rgb.Length < 3)
            {
                return new Color(1f, 1f, 1f, alpha);
            }

            return new Color(rgb[0], rgb[1], rgb[2], alpha);
        }

        private Material MakeAdditive()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.name = _spec.Character + "AttackFx";
            _dot = SoftDot();
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", _dot);
            }

            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", _dot);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            SetFloat(mat, "_Surface", 1f);
            SetFloat(mat, "_Blend", 2f);
            SetFloat(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(mat, "_DstBlend", (float)BlendMode.One);
            SetFloat(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_DstBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_ZWrite", 0f);
            SetFloat(mat, "_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        private static void SetFloat(Material mat, string name, float value)
        {
            if (mat.HasProperty(name))
            {
                mat.SetFloat(name, value);
            }
        }

        private static Texture2D SoftDot()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = "AttackSoftDot";
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[n * n];
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = ((x + 0.5f) / n * 2f) - 1f;
                    var dy = ((y + 0.5f) / n * 2f) - 1f;
                    var r = Mathf.Clamp01(Mathf.Sqrt((dx * dx) + (dy * dy)));
                    var a = (1f - r) * (1f - r);
                    pixels[(y * n) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private sealed class TrackView
        {
            public TrackView(AttackTrack track, GameObject root)
            {
                Track = track;
                Root = root;
            }

            public AttackTrack Track { get; }
            public GameObject Root { get; }
            public GameObject? Mesh { get; set; }
            public Transform? Bone { get; set; }
            public Vector3 BoneLocalPos { get; set; }
            public Quaternion BoneLocalRot { get; set; } = Quaternion.identity;
            public bool BoneBound { get; set; }
            public bool Shown { get; set; }
            public ParticleSystem[] Particles { get; set; } = System.Array.Empty<ParticleSystem>();
            public TrailRenderer[] Trails { get; set; } = System.Array.Empty<TrailRenderer>();
            public LineRenderer[] Lines { get; set; } = System.Array.Empty<LineRenderer>();
            public Transform[] SlashTips { get; set; } = System.Array.Empty<Transform>();
            public float LineLength { get; set; }
            public float SlashWidth { get; set; }
            public float SlashHeight { get; set; }
        }
    }
}
