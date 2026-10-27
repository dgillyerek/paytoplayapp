#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Breakable barriers (Derek 2026-10-26: "some type of gate or stone fence. something time period appropriate"): a
    /// palisade gate of sharpened oak stakes, a dry-stone wall, and a spiked barricade (a log frame bristling with iron
    /// points). PLACEHOLDER ART: shaded primitives in the trap palette so they read on the road; Design should replace
    /// them with final models. Each one carries an always-on HP bar; squad shots chip it (a puff of splinters or grit) and
    /// it bursts into debris when it falls or when the squad walks into it. The run owns every state; this only shows it.
    /// </summary>
    public sealed partial class RunnerLevelView
    {
        /// <summary>
        /// Sound hook names (the audio itself arrives with the game-audio branch). <see cref="BarrierSound"/> fires with one
        /// of these and the world position so an audio player can subscribe without this view knowing about it.
        /// </summary>
        public const string BarrierBreakSound = "runner.barrier.break";
        public const string BarrierSmashSound = "runner.barrier.smash";
        public const string BarrierHitSound = "runner.barrier.hit";

        /// <summary>Raised with a sound hook name above and where it happened.</summary>
        public static event Action<string, Vector3>? BarrierSound;

        private static readonly Color Stone = new Color(0.6f, 0.58f, 0.54f, 1f);
        private static readonly Color StoneDark = new Color(0.42f, 0.4f, 0.38f, 1f);
        private static readonly Color Oak = new Color(0.5f, 0.33f, 0.18f, 1f);
        private static readonly Color Rope = new Color(0.72f, 0.6f, 0.38f, 1f);

        private const float DebrisLife = 1.1f;

        private readonly Dictionary<int, BarrierView> _barrierViews = new Dictionary<int, BarrierView>();
        private readonly List<Debris> _debris = new List<Debris>();

        private sealed class BarrierView
        {
            public Transform Root = null!;
            public RunnerBarrierKind Kind;
            public float Width;
            public float Height;
            public float Wobble;
        }

        private sealed class Debris
        {
            public Transform T = null!;
            public Vector3 V;
            public Vector3 Spin;
            public float Age;
        }

        private void SyncBarriers()
        {
            if (_run == null || _actors == null)
            {
                return;
            }

            var barriers = _run.Barriers;
            for (var i = 0; i < barriers.Count; i++)
            {
                var b = barriers[i];
                if (!_barrierViews.ContainsKey(b.Id))
                {
                    var v = BuildBarrier(b.Def, _actors);
                    v.Root.localPosition = new Vector3(b.X, 0f, b.Z);
                    _barrierViews[b.Id] = v;
                    AddBar(b.Id, false, v.Height, false);
                    if (_bars.TryGetValue(b.Id, out var bar))
                    {
                        bar.Barrier = true;
                        bar.Group.alpha = 1f;
                    }
                }
            }

            if (_barrierViews.Count == barriers.Count)
            {
                return;
            }

            _scratchIds.Clear();
            _scratchIds.AddRange(_barrierViews.Keys);
            foreach (var id in _scratchIds)
            {
                if (_run.Barrier(id) == null)
                {
                    var v = _barrierViews[id];
                    _barrierViews.Remove(id);
                    RemoveBar(id);
                    if (v.Root != null)
                    {
                        Destroy(v.Root.gameObject);
                    }
                }
            }
        }

        /// <summary>Placeholder primitives for <paramref name="def"/>'s kind, <see cref="RunnerBarrierDef.Width"/> wide.</summary>
        private BarrierView BuildBarrier(RunnerBarrierDef def, Transform parent)
        {
            var root = new GameObject("Barrier_" + RunnerBarrierDef.Name(def.Kind)).transform;
            root.SetParent(parent, false);
            var w = def.Width;
            var d = RunnerRules.BarrierHalfDepth * 2f;
            var v = new BarrierView { Root = root, Kind = def.Kind, Width = w };
            switch (def.Kind)
            {
                case RunnerBarrierKind.StoneWall:
                {
                    // Two staggered courses of rough blocks with a capstone row.
                    v.Height = 1.3f;
                    var blocks = Mathf.Max(2, Mathf.RoundToInt(w / 0.8f));
                    for (var course = 0; course < 3; course++)
                    {
                        var n = blocks + (course % 2);
                        var bw = w / n;
                        for (var i = 0; i < n; i++)
                        {
                            var x = ((i + 0.5f) * bw) - (w * 0.5f);
                            var tint = ((i + course) % 3 == 0) ? StoneDark : Stone;
                            Part(root, PrimitiveType.Cube, "Block", new Vector3(x, 0.22f + (course * 0.42f), 0f), new Vector3(bw * 0.94f, 0.4f, d * (course == 2 ? 0.8f : 1f)), LitMat(tint), Quaternion.Euler(0f, (i % 2 == 0 ? 2f : -3f), 0f));
                        }
                    }

                    break;
                }

                case RunnerBarrierKind.Barricade:
                {
                    // A log frame on trestles with iron points angled at the squad.
                    v.Height = 1.2f;
                    Part(root, PrimitiveType.Cylinder, "Beam", new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, w * 0.5f, 0.3f), LitMat(Bark), Quaternion.Euler(0f, 0f, 90f));
                    for (var x = -w * 0.5f + 0.25f; x <= (w * 0.5f) - 0.2f; x += Mathf.Max(0.9f, w / 4f))
                    {
                        Part(root, PrimitiveType.Cylinder, "LegA", new Vector3(x, 0.4f, 0f), new Vector3(0.14f, 0.5f, 0.14f), LitMat(Bark), Quaternion.Euler(30f, 0f, 0f));
                        Part(root, PrimitiveType.Cylinder, "LegB", new Vector3(x, 0.4f, 0f), new Vector3(0.14f, 0.5f, 0.14f), LitMat(Bark), Quaternion.Euler(-30f, 0f, 0f));
                    }

                    var spikes = Mathf.Max(3, Mathf.RoundToInt(w / 0.45f));
                    for (var i = 0; i < spikes; i++)
                    {
                        var x = ((i + 0.5f) / spikes - 0.5f) * (w - 0.2f);
                        var tip = Part(root, PrimitiveType.Cylinder, "Point", new Vector3(x, 0.7f + (i % 2 * 0.15f), -0.3f), new Vector3(0.07f, 0.4f, 0.07f), LitMat(Iron), Quaternion.Euler(-55f, 0f, 0f));
                        tip.name = "Spike";
                    }

                    break;
                }

                default:
                {
                    // Palisade: sharpened oak stakes lashed to two cross rails.
                    v.Height = 1.7f;
                    var stakes = Mathf.Max(3, Mathf.RoundToInt(w / 0.32f));
                    for (var i = 0; i < stakes; i++)
                    {
                        var x = ((i + 0.5f) / stakes - 0.5f) * w;
                        var h = 1.35f + ((i * 37 % 5) * 0.05f);
                        Part(root, PrimitiveType.Cylinder, "Stake", new Vector3(x, h * 0.5f, 0f), new Vector3(0.26f, h * 0.5f, 0.26f), LitMat(i % 2 == 0 ? Bark : Oak), Quaternion.identity);
                        Part(root, PrimitiveType.Cube, "Point", new Vector3(x, h + 0.08f, 0f), new Vector3(0.16f, 0.16f, 0.16f), LitMat(Heartwood), Quaternion.Euler(45f, 0f, 45f));
                    }

                    Part(root, PrimitiveType.Cube, "RailLow", new Vector3(0f, 0.45f, -0.16f), new Vector3(w, 0.12f, 0.1f), LitMat(Oak), Quaternion.identity);
                    Part(root, PrimitiveType.Cube, "RailHigh", new Vector3(0f, 1.05f, -0.16f), new Vector3(w, 0.12f, 0.1f), LitMat(Rope), Quaternion.identity);
                    break;
                }
            }

            LockedBattleGround.AddContactShadow(root, w, 0.7f);
            return v;
        }

        private static Color BarrierColor(string kind) => kind switch
        {
            "stone_wall" => Stone,
            "barricade" => Bark,
            _ => Oak
        };

        /// <summary>A squad shot chipped one: a puff of splinters or grit and a little shudder.</summary>
        private void BarrierStruck(RunnerEvent e)
        {
            var at = new Vector3(e.X, 0.8f, e.Z - RunnerRules.BarrierHalfDepth);
            Sparks(at, BarrierColor(e.Unit), 4, 0.6f);
            if (_barrierViews.TryGetValue(e.Id, out var v))
            {
                v.Wobble = 0.12f;
            }

            BarrierSound?.Invoke(BarrierHitSound, at);
        }

        /// <summary>It fell to shots (or the squad walked into it, <see cref="RunnerEvent.Big"/>): debris, dust and a shake.</summary>
        private void BarrierBroke(RunnerEvent e)
        {
            var at = new Vector3(e.X, 0.6f, e.Z);
            var color = BarrierColor(e.Unit);
            var width = _barrierViews.TryGetValue(e.Id, out var v) ? v.Width : 3f;
            AddFx(HookArtKeys.FxBurstRing, at, 1.2f + (width * 0.35f), 0.45f, new Color(0.85f, 0.75f, 0.6f, 0.85f), Vector3.zero, 1.5f, 0f);
            Sparks(at, color, 10, 1.6f);
            SpawnDebris(at, width, color);
            Shake(e.Big ? 0.3f : 0.15f);
            if (e.Big)
            {
                Floater(at + (Vector3.up * 2.2f), S("runner.barrier.smashed", "Crashed through!"), new Color(1f, 0.5f, 0.25f, 1f), 34);
            }

            BarrierSound?.Invoke(e.Big ? BarrierSmashSound : BarrierBreakSound, at);
        }

        private void SpawnDebris(Vector3 at, float width, Color color)
        {
            if (_actors == null)
            {
                return;
            }

            var n = Mathf.Clamp(Mathf.RoundToInt(width * 2f), 4, 12);
            for (var i = 0; i < n; i++)
            {
                var x = ((i + 0.5f) / n - 0.5f) * width;
                var chunk = Part(_actors, PrimitiveType.Cube, "Debris", at + new Vector3(x, 0f, 0f), Vector3.one * UnityEngine.Random.Range(0.14f, 0.3f), LitMat(color), UnityEngine.Random.rotation).transform;
                _debris.Add(new Debris
                {
                    T = chunk,
                    V = new Vector3(UnityEngine.Random.Range(-2f, 2f) + (x * 0.6f), UnityEngine.Random.Range(3f, 6f), UnityEngine.Random.Range(1f, 4f)),
                    Spin = UnityEngine.Random.insideUnitSphere * 540f,
                });
            }
        }

        private void AnimateBarriers(float dt)
        {
            foreach (var v in _barrierViews.Values)
            {
                if (v.Wobble <= 0f || v.Root == null)
                {
                    continue;
                }

                v.Wobble = Mathf.Max(0f, v.Wobble - dt);
                v.Root.localRotation = Quaternion.Euler(Mathf.Sin(v.Wobble * 90f) * v.Wobble * 25f, 0f, 0f);
            }

            for (var i = _debris.Count - 1; i >= 0; i--)
            {
                var c = _debris[i];
                c.Age += dt;
                if (c.Age >= DebrisLife || c.T == null)
                {
                    if (c.T != null)
                    {
                        Destroy(c.T.gameObject);
                    }

                    _debris.RemoveAt(i);
                    continue;
                }

                c.V += Physics.gravity * dt;
                var p = c.T.localPosition + (c.V * dt);
                if (p.y < 0.08f)
                {
                    p.y = 0.08f;
                    c.V = new Vector3(c.V.x * 0.5f, -c.V.y * 0.3f, c.V.z * 0.5f);
                }

                c.T.localPosition = p;
                c.T.localRotation *= Quaternion.Euler(c.Spin * dt);
                c.T.localScale = Vector3.one * Mathf.Lerp(c.T.localScale.x, 0f, c.Age / DebrisLife * 0.2f);
            }
        }

        /// <summary>The road is torn down (its objects go with <c>_actors</c>): forget the views and debris.</summary>
        private void ClearBarriers()
        {
            _barrierViews.Clear();
            _debris.Clear();
        }
    }
}
