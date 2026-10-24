#nullable enable
using System.Collections.Generic;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Lane traps (Update 4), drawn from shaded primitives in the painted kingdom palette (iron, oak, ember) so they
    /// sit with the ground art: a spike plate whose iron cones rattle and glow before they shoot up, an oak log that
    /// wobbles with a warning stripe down its lane before it rolls at the squad, and a fire patch: a scorched
    /// ground decal with a burnt edge, a warm ground glow, a hot rim marking the damage footprint, layered flame
    /// tongues (yellow-white core, orange-to-red body) that flicker, sink low just before the next flare and leap on it,
    /// and embers rising off it (<see cref="RunnerFireArt"/>: a few quads on six shared materials, no lights, no allocation per frame). The run owns every state and position; this only shows them.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        private static readonly Color IronDark = new Color(0.22f, 0.21f, 0.24f, 1f);
        private static readonly Color Iron = new Color(0.55f, 0.56f, 0.6f, 1f);
        private static readonly Color Bark = new Color(0.38f, 0.24f, 0.13f, 1f);
        private static readonly Color Heartwood = new Color(0.82f, 0.64f, 0.4f, 1f);
        private static readonly Color Warn = new Color(1f, 0.38f, 0.12f, 0.55f);
        private static readonly Color Scorch = new Color(0.1f, 0.06f, 0.04f, 0.75f);
        private static readonly Color Flame = new Color(1f, 0.55f, 0.12f, 0.85f);
        private static readonly Color FlameCore = new Color(1f, 0.9f, 0.45f, 0.9f);

        private const int FireEmbers = 8;

        private static Mesh? _cone;

        private readonly Dictionary<int, HazardView> _hazards = new Dictionary<int, HazardView>();

        private sealed class HazardView
        {
            public Transform Root = null!;
            public RunnerHazardKind Kind;
            public readonly List<Transform> Parts = new List<Transform>();
            public Renderer? Glow;
            public Transform? Body;
            public readonly List<Transform> Cores = new List<Transform>();
            public readonly List<Transform> Embers = new List<Transform>();
            public Transform? Heat;
            public Transform? Rim;
            public float Seed;
            public RunnerHazardDef Def = null!;
            public RunnerHazardState Last = RunnerHazardState.Idle;
        }

        private void SyncHazards()
        {
            if (_run == null || _actors == null)
            {
                return;
            }

            var hazards = _run.Hazards;
            for (var i = 0; i < hazards.Count; i++)
            {
                var h = hazards[i];
                if (!_hazards.ContainsKey(h.Id))
                {
                    _hazards[h.Id] = TakeHazard(h);
                }
            }

            if (_hazards.Count == hazards.Count)
            {
                return;
            }

            _scratchIds.Clear();
            _scratchIds.AddRange(_hazards.Keys);
            foreach (var id in _scratchIds)
            {
                if (_run.Hazard(id) == null)
                {
                    var v = _hazards[id];
                    _hazards.Remove(id);
                    ReleaseHazard(v);
                }
            }
        }

        private static int _hazardsBuilt;

        /// <summary>A trap of <paramref name="def"/>'s kind and width (pooled: <see cref="TakeHazard"/> places it).</summary>
        private HazardView BuildHazard(RunnerHazardDef def, Transform parent)
        {
            var n = ++_hazardsBuilt;
            var root = new GameObject("Hazard_" + RunnerHazardDef.Name(def.Kind) + "_" + n).transform;
            root.SetParent(parent, false);
            var v = new HazardView { Root = root, Kind = def.Kind, Def = def, Seed = (n * 0.618f) % 1f };
            var h = (Kind: def.Kind, HalfDepth: RunnerHazard.HalfDepthOf(def.Kind));
            var w = def.Width;
            switch (h.Kind)
            {
                case RunnerHazardKind.Spikes:
                {
                    // Warning glow first (under the plate) so the plate edge reads against it.
                    v.Glow = Part(root, PrimitiveType.Quad, "Glow", new Vector3(0f, 0.015f, 0f), new Vector3(w + 0.5f, h.HalfDepth * 2f + 0.5f, 1f), Unlit(Warn), Quaternion.Euler(90f, 0f, 0f)).GetComponent<Renderer>();
                    Part(root, PrimitiveType.Cube, "Plate", new Vector3(0f, 0.04f, 0f), new Vector3(w, 0.08f, h.HalfDepth * 2f), LitMat(IronDark), Quaternion.identity);
                    var cols = Mathf.Max(3, Mathf.RoundToInt(w / 0.36f));
                    for (var r = 0; r < 3; r++)
                    {
                        for (var c = 0; c < cols; c++)
                        {
                            var x = ((c + 0.5f) / cols - 0.5f) * (w - 0.2f) + (r % 2 == 1 ? 0.09f : 0f);
                            var z = (r - 1) * h.HalfDepth * 0.62f;
                            var spike = new GameObject("Spike").transform;
                            spike.SetParent(root, false);
                            spike.localPosition = new Vector3(x, 0.08f, z);
                            var cone = spike.gameObject.AddComponent<MeshFilter>();
                            cone.sharedMesh = Cone();
                            spike.gameObject.AddComponent<MeshRenderer>().sharedMaterial = LitMat(Iron);
                            spike.localScale = new Vector3(0.16f, 0.05f, 0.16f);
                            v.Parts.Add(spike);
                        }
                    }

                    break;
                }

                case RunnerHazardKind.Log:
                {
                    // The stripe runs from the log back toward the squad: where it will roll.
                    var stripe = Part(root, PrimitiveType.Quad, "Lane", new Vector3(0f, 0.02f, -7f), new Vector3(w + 0.3f, 14f, 1f), Unlit(Warn), Quaternion.Euler(90f, 0f, 0f));
                    v.Glow = stripe.GetComponent<Renderer>();
                    v.Glow.enabled = false;
                    var body = new GameObject("Log").transform;
                    body.SetParent(root, false);
                    body.localPosition = new Vector3(0f, 0.36f, 0f);
                    var log = Part(body, PrimitiveType.Cylinder, "Bark", Vector3.zero, new Vector3(0.72f, w * 0.5f, 0.72f), LitMat(Bark), Quaternion.Euler(0f, 0f, 90f));
                    Part(body, PrimitiveType.Cylinder, "RingL", new Vector3(-w * 0.5f, 0f, 0f), new Vector3(0.6f, 0.012f, 0.6f), LitMat(Heartwood), Quaternion.Euler(0f, 0f, 90f));
                    Part(body, PrimitiveType.Cylinder, "RingR", new Vector3(w * 0.5f, 0f, 0f), new Vector3(0.6f, 0.012f, 0.6f), LitMat(Heartwood), Quaternion.Euler(0f, 0f, 90f));
                    // A knot so the roll reads.
                    Part(body, PrimitiveType.Sphere, "Knot", new Vector3(w * 0.15f, 0.33f, 0f), new Vector3(0.22f, 0.1f, 0.22f), LitMat(Heartwood * 0.8f), Quaternion.identity);
                    v.Body = body;
                    v.Parts.Add(log.transform);
                    LockedBattleGround.AddContactShadow(root, w, 0.6f);
                    break;
                }

                default:
                {
                    RunnerFireArt.Warm();
                    var depth = h.HalfDepth * 2f;
                    // Ground, bottom to top: burnt scorch, warm glow, hot rim at the footprint's edge.
                    v.Glow = Part(root, PrimitiveType.Quad, "Scorch", new Vector3(0f, 0.011f, 0f), new Vector3(w + 1.1f, depth + 1.1f, 1f), RunnerFireArt.Scorch, Quaternion.Euler(90f, 0f, 0f)).GetComponent<Renderer>();
                    v.Heat = Part(root, PrimitiveType.Quad, "Glow", new Vector3(0f, 0.016f, 0f), new Vector3(w + 1.5f, depth + 1.5f, 1f), RunnerFireArt.Glow, Quaternion.Euler(90f, 0f, 0f)).transform;
                    v.Rim = Part(root, PrimitiveType.Quad, "Rim", new Vector3(0f, 0.02f, 0f), new Vector3(w + 0.35f, depth + 0.35f, 1f), RunnerFireArt.Rim, Quaternion.Euler(90f, 0f, 0f)).transform;

                    // Flames: an outer tongue (orange to red) with a hot core in front of it, spread over the footprint.
                    var tongues = Mathf.Clamp(Mathf.RoundToInt(w / 0.42f), 6, 12);
                    for (var i = 0; i < tongues; i++)
                    {
                        var a = (i * 2.39996f) + (v.Seed * 6.28f);
                        var rad = Mathf.Sqrt((i + 0.5f) / tongues);
                        var x = Mathf.Cos(a) * rad * w * 0.44f;
                        var z = Mathf.Sin(a) * rad * h.HalfDepth * 0.8f;
                        var yaw = Mathf.Sin(i * 1.9f) * 14f;
                        v.Parts.Add(Part(root, PrimitiveType.Quad, "Flame", new Vector3(x, 0.4f, z), new Vector3(0.85f, 0.9f, 1f), RunnerFireArt.Tongue, Quaternion.Euler(0f, yaw, 0f)).transform);
                        v.Cores.Add(Part(root, PrimitiveType.Quad, "Core", new Vector3(x, 0.3f, z - 0.02f), new Vector3(0.55f, 0.6f, 1f), RunnerFireArt.Core, Quaternion.Euler(0f, yaw, 0f)).transform);
                    }

                    // Embers: small bright dots that rise, drift and shrink away, looping on their own phase.
                    for (var i = 0; i < FireEmbers; i++)
                    {
                        var x = (((i * 0.6180339f) % 1f) - 0.5f) * w * 0.8f;
                        var z = ((((i * 0.7548777f) + v.Seed) % 1f) - 0.5f) * h.HalfDepth * 1.4f;
                        v.Embers.Add(Part(root, PrimitiveType.Quad, "Ember", new Vector3(x, 0.4f, z), new Vector3(0.1f, 0.1f, 1f), RunnerFireArt.Ember, Quaternion.identity).transform);
                    }

                    break;
                }
            }

            return v;
        }

        private void AnimateHazards(float dt)
        {
            if (_run == null)
            {
                return;
            }

            RunnerHitchLog.Begin(RunnerHitchLog.Section.Hazards);
            var time = Time.time;
            foreach (var pair in _hazards)
            {
                var h = _run.Hazard(pair.Key);
                var v = pair.Value;
                if (h == null || v.Root == null)
                {
                    continue;
                }

                v.Root.position = new Vector3(h.X, 0f, h.Z);
                switch (h.Kind)
                {
                    case RunnerHazardKind.Spikes:
                    {
                        // Down: tips just showing. Warning: rattle + a pulsing ember glow. Up: snapped full height.
                        float height;
                        var jitter = 0f;
                        if (h.State == RunnerHazardState.Active)
                        {
                            height = Mathf.Lerp(0.2f, 0.62f, Mathf.Clamp01(h.StateClock / 0.08f));
                        }
                        else if (h.State == RunnerHazardState.Warning)
                        {
                            height = 0.1f;
                            jitter = Mathf.Sin(time * 70f) * 0.025f;
                        }
                        else
                        {
                            height = Mathf.MoveTowards(v.Parts.Count > 0 ? v.Parts[0].localScale.y : 0.05f, 0.05f, dt * 1.2f);
                        }

                        foreach (var s in v.Parts)
                        {
                            s.localScale = new Vector3(0.16f, height, 0.16f);
                            var p = s.localPosition;
                            s.localPosition = new Vector3(p.x, 0.08f + jitter, p.z);
                        }

                        if (v.Glow != null)
                        {
                            var pulse = h.State == RunnerHazardState.Warning ? 0.55f + (0.45f * Mathf.Sin(time * 18f)) : h.State == RunnerHazardState.Active ? 0.9f : 0.18f;
                            v.Glow.sharedMaterial = Unlit(new Color(Warn.r, Warn.g, Warn.b, Mathf.Round(pulse * 10f) / 10f * Warn.a));
                        }

                        break;
                    }

                    case RunnerHazardKind.Log:
                    {
                        if (v.Glow != null)
                        {
                            v.Glow.enabled = h.State != RunnerHazardState.Idle;
                            if (v.Glow.enabled)
                            {
                                var pulse = h.State == RunnerHazardState.Warning ? 0.5f + (0.5f * Mathf.Sin(time * 14f)) : 0.6f;
                                v.Glow.sharedMaterial = Unlit(new Color(Warn.r, Warn.g, Warn.b, Mathf.Round(pulse * 10f) / 10f * Warn.a));
                            }
                        }

                        if (v.Body != null)
                        {
                            if (h.State == RunnerHazardState.Warning)
                            {
                                // Rocks back and forth on its seat, harder as the roll nears.
                                var k = Mathf.Clamp01(h.StateClock / RunnerRules.LogWarn);
                                v.Body.localRotation = Quaternion.Euler(Mathf.Sin(time * 16f) * (6f + (14f * k)), 0f, 0f);
                            }
                            else if (h.State == RunnerHazardState.Active)
                            {
                                // Rolling toward -z: spin about x by distance over radius.
                                var rolled = h.StartZ - h.Z;
                                v.Body.localRotation = Quaternion.Euler(-rolled / 0.36f * Mathf.Rad2Deg, 0f, 0f);
                            }
                        }

                        break;
                    }

                    default:
                    {
                        AnimateFire(v, h, dt, time);
                        break;
                    }
                }

                v.Last = h.State;
            }

            RunnerHitchLog.End(RunnerHitchLog.Section.Hazards);
        }

        /// <summary>
        /// One fire patch for one frame. The patch burns one member at a time and re-flares every <see cref="RunnerRules.FireFlare"/>
        /// seconds (the run owns that clock: <see cref="RunnerHazard.StateClock"/>). The look follows it: the flames leap at the flare,
        /// settle into a flicker, and sink low over the last third of the beat, so a squad can read when the next burn is coming; the rim
        /// quickens and the ground glow swells with it. Transforms only: no allocation, no materials touched.
        /// </summary>
        private void AnimateFire(HazardView v, RunnerHazard h, float dt, float time)
        {
            var beat = Mathf.Clamp01(h.StateClock / RunnerRules.FireFlare);
            var flare = 1f + (0.65f * Mathf.Exp(-h.StateClock * 6f));
            var wind = beat > 0.65f ? Mathf.Lerp(1f, 0.6f, (beat - 0.65f) / 0.35f) : 1f;
            for (var i = 0; i < v.Parts.Count; i++)
            {
                var f = v.Parts[i];
                var n = Mathf.PerlinNoise((time * 6f) + (i * 1.7f), v.Seed * 9f);
                var tall = (0.55f + (0.6f * n)) * flare * wind;
                var wide = (0.8f + (0.2f * n)) * (1.15f - (0.15f * flare));
                var sway = Mathf.Sin((time * 3.1f) + (i * 1.3f)) * 9f;
                var yaw = Mathf.Sin(i * 1.9f) * 14f;
                var rot = Quaternion.Euler(0f, yaw, sway);
                f.localScale = new Vector3(wide, tall * 1.1f, 1f);
                f.localRotation = rot;
                var p = f.localPosition;
                f.localPosition = new Vector3(p.x, tall * 0.55f, p.z);

                var core = v.Cores[i];
                core.localScale = new Vector3(wide * 0.62f, tall * 0.8f, 1f);
                core.localRotation = rot;
                var q = core.localPosition;
                core.localPosition = new Vector3(q.x, tall * 0.4f, q.z);
            }

            if (v.Heat != null)
            {
                var swell = 1f + (0.1f * flare) + (0.04f * Mathf.Sin(time * 9f));
                v.Heat.localScale = new Vector3((v.Def.Width + 1.5f) * swell, (RunnerHazard.HalfDepthOf(v.Kind) * 2f + 1.5f) * swell, 1f);
            }

            if (v.Rim != null)
            {
                var quick = 1f + (0.035f * Mathf.Sin(time * (8f + (14f * beat))));
                v.Rim.localScale = new Vector3((v.Def.Width + 0.35f) * quick, (RunnerHazard.HalfDepthOf(v.Kind) * 2f + 0.35f) * quick, 1f);
            }

            for (var i = 0; i < v.Embers.Count; i++)
            {
                var e = v.Embers[i];
                var life = Mathf.Repeat((time * (0.32f + (0.07f * (i % 3)))) + (i * 0.37f) + v.Seed, 1f);
                var p = e.localPosition;
                var size = 0.13f * (1f - life) * (0.7f + (0.3f * Mathf.Sin(i * 2.1f + 1f)));
                e.localScale = new Vector3(size, size, 1f);
                e.localPosition = new Vector3(p.x + (Mathf.Sin((time * 2.2f) + i) * dt * 0.25f), 0.25f + (life * 1.7f), p.z);
            }

            if (h.StateClock < dt)
            {
                Sparks(v.Root.position + (Vector3.up * 0.6f), Flame, 3, 0.8f);
            }
        }

        /// <summary>A trap caught someone: a flash on the spot and the call-out.</summary>
        private void HazardStruck(RunnerEvent e, Vector3 at)
        {
            var color = e.Unit == "fire" ? Flame : e.Unit == "log" ? Heartwood : Iron;
            Sparks(at + (Vector3.up * 0.4f), color, 10, 1.4f);
            AddFx(HookArtKeys.FxBurstRing, at + (Vector3.up * 0.1f), 1.4f, 0.35f, new Color(1f, 0.45f, 0.2f, 0.9f), Vector3.zero, 1.4f, 0f);
            if (!e.Big)
            {
                // A follower caught: FollowerHit (next event) shows the damage off its HP bar, or FollowerLost drops it at 0.
                return;
            }

            Floater(at + (Vector3.up * 2.6f), e.Unit switch
            {
                "fire" => S("runner.hazard.fire", "Burned!"),
                "log" => S("runner.hazard.log", "Flattened!"),
                _ => S("runner.hazard.spikes", "Spiked!"),
            }, new Color(1f, 0.5f, 0.25f, 1f), 38);
        }

        private static Color RewardColor(RunnerBoxKind kind, Color fallback) => kind switch
        {
            RunnerBoxKind.Rate => new Color(0.55f, 0.9f, 1f, 1f),
            RunnerBoxKind.Range => new Color(0.6f, 1f, 0.55f, 1f),
            RunnerBoxKind.Heal => new Color(1f, 0.62f, 0.7f, 1f),
            _ => fallback
        };

        /// <summary>The crate art a range / heal box borrows, retinted so the four power-ups read apart.</summary>
        private static Color CrateTint(RunnerBoxKind kind) => kind switch
        {
            RunnerBoxKind.Range => new Color(0.72f, 1f, 0.7f, 1f),
            RunnerBoxKind.Heal => new Color(1f, 0.74f, 0.8f, 1f),
            _ => Color.white
        };

        private static readonly Dictionary<Color, Material> Lits = new Dictionary<Color, Material>();

        private static Material LitMat(Color c)
        {
            if (!Lits.TryGetValue(c, out var m) || m == null)
            {
                m = LockedBattleGround.Lit(null, c, false);
                Lits[c] = m;
            }

            return m;
        }

        private static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 at, Vector3 scale, Material mat, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A unit cone (base radius 0.5 at y 0, tip at y 1), shared by every spike.</summary>
        private static Mesh Cone()
        {
            if (_cone != null)
            {
                return _cone;
            }

            const int sides = 10;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (var i = 0; i < sides; i++)
            {
                var a0 = i * Mathf.PI * 2f / sides;
                var a1 = (i + 1) * Mathf.PI * 2f / sides;
                var b = verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f));
                verts.Add(new Vector3(0f, 1f, 0f));
                verts.Add(new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f));
                tris.Add(b);
                tris.Add(b + 1);
                tris.Add(b + 2);
            }

            _cone = new Mesh { name = "RunnerSpike" };
            _cone.SetVertices(verts);
            _cone.SetTriangles(tris, 0);
            _cone.RecalculateNormals();
            _cone.RecalculateBounds();
            return _cone;
        }
    }
}
