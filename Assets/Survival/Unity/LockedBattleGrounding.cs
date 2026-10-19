using System.Collections.Generic;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Keeps a fighter's lowest body point on the ground every frame (rest, walk and every attack),
    /// without touching the rig bones or the clips: it only moves the actor host up or down.
    ///
    /// Why: the battle used to plant each body from <c>Renderer.bounds</c>. With
    /// <c>updateWhenOffscreen</c> on (every locked actor turns it on) a skinned renderer's bounds are
    /// Unity's per-bone boxes rotated into the pose and merged, so the box's floor sits below the
    /// real sole (most for steep or heeled feet, Vespera's boots), and lifting the body by that
    /// floor leaves it hovering. Poses that are lower or higher than the measured one (Sir Aldric's
    /// crouched rest, a Mixamo rig whose bind pose is centred on the origin) moved it further.
    ///
    /// How: the first time a body skin is drawn, one <c>BakeMesh</c> of the pose on screen gives the
    /// real vertex positions. A sample of them (every vertex in the lowest band of the body plus a
    /// thin spread over the rest) is pinned to its nearest bone. Each LateUpdate, after the clips
    /// and the battle have posed the body, the lowest pinned point is found and the host is moved
    /// so that point sits on the slot's ground (y = 0).
    /// </summary>
    public sealed class LockedBattleGrounding
    {
        /// <summary>Body skins only. Held props, capes and effects never plant the body.</summary>
        private static readonly string[] PropWords =
        {
            "sword", "shield", "spear", "bow", "staff", "weapon", "arrow", "quiver", "orb", "cape", "ico", "effect", "trail"
        };

        /// <summary>Vertices in the lowest share of the body height that are all kept.</summary>
        public const float LowBand = 0.22f;

        public const int MaxLowPoints = 2400;
        public const int MaxSpreadPoints = 900;

        private readonly Transform _host;
        private readonly Dictionary<SkinnedMeshRenderer, Skin?> _skins = new Dictionary<SkinnedMeshRenderer, Skin?>();
        private SkinnedMeshRenderer[] _renderers;
        private bool _warned;

        public LockedBattleGrounding(Transform host)
        {
            _host = host;
            _renderers = FindBodySkins(host);
        }

        /// <summary>Lift (world metres) applied by the last <see cref="Plant"/>.</summary>
        public float LastLift { get; private set; }

        /// <summary>True once at least one body skin has been sampled.</summary>
        public bool Ready { get; private set; }

        public static bool IsBodySkin(Renderer r) => IsBodySkinName(r.name);

        /// <summary>
        /// False when a word of the name (split on anything but letters) starts with a prop word:
        /// "SirAldric_PILOT_sword" and "Icosphere" are props, "OAKENSHIELD_body" is a body.
        /// </summary>
        public static bool IsBodySkinName(string name)
        {
            foreach (var word in System.Text.RegularExpressions.Regex.Split(name, "[^A-Za-z]+"))
            {
                if (word.Length == 0)
                {
                    continue;
                }

                foreach (var w in PropWords)
                {
                    if (word.StartsWith(w, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Moves the host so the lowest visible body point sits on <paramref name="groundY"/>.
        /// Returns false (host untouched) when no body skin could be sampled.
        /// </summary>
        public bool Plant(float groundY)
        {
            if (!TryLowest(out var lowest))
            {
                LastLift = 0f;
                return false;
            }

            var lift = groundY - lowest;
            if (Mathf.Abs(lift) > 1e-5f)
            {
                _host.position += new Vector3(0f, lift, 0f);
            }

            LastLift = lift;
            return true;
        }

        /// <summary>Lowest world y of the visible body skins in the pose on screen now.</summary>
        public bool TryLowest(out float lowest)
        {
            lowest = float.PositiveInfinity;
            var any = false;
            for (var i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!_skins.TryGetValue(r, out var skin))
                {
                    skin = Skin.Sample(r);
                    _skins[r] = skin;
                    if (skin == null && !_warned)
                    {
                        _warned = true;
                        Debug.LogWarning("Locked battle grounding: could not sample " + r.name + " under " + _host.name + ".");
                    }
                }

                if (skin == null)
                {
                    continue;
                }

                var y = skin.Lowest();
                if (y < lowest)
                {
                    lowest = y;
                }

                any = true;
            }

            if (any)
            {
                Ready = true;
            }

            return any && !float.IsInfinity(lowest) && !float.IsNaN(lowest);
        }

        /// <summary>Picks up body skins an actor turned on after spawn (pose swaps).</summary>
        public void Refresh()
        {
            _renderers = FindBodySkins(_host);
        }

        private static SkinnedMeshRenderer[] FindBodySkins(Transform host)
        {
            var list = new List<SkinnedMeshRenderer>();
            foreach (var r in host.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.sharedMesh != null && IsBodySkin(r))
                {
                    list.Add(r);
                }
            }

            return list.ToArray();
        }

        /// <summary>Sampled vertices of one skin, each pinned to its nearest bone.</summary>
        private sealed class Skin
        {
            private readonly Transform[] _bones;
            private readonly Vector3[] _local;

            private Skin(Transform[] bones, Vector3[] local)
            {
                _bones = bones;
                _local = local;
            }

            public float Lowest()
            {
                var lowest = float.PositiveInfinity;
                for (var i = 0; i < _local.Length; i++)
                {
                    var b = _bones[i];
                    if (b == null)
                    {
                        continue;
                    }

                    var y = b.TransformPoint(_local[i]).y;
                    if (y < lowest)
                    {
                        lowest = y;
                    }
                }

                return lowest;
            }

            public static Skin? Sample(SkinnedMeshRenderer r)
            {
                Vector3[] verts;
                var baked = new Mesh();
                try
                {
                    r.BakeMesh(baked, true);
                    verts = baked.vertices;
                }
                catch (System.Exception)
                {
                    verts = System.Array.Empty<Vector3>();
                }
                finally
                {
                    Object.Destroy(baked);
                }

                if (verts == null || verts.Length < 3)
                {
                    return null;
                }

                var space = r.transform;
                var world = new Vector3[verts.Length];
                var lo = float.PositiveInfinity;
                var hi = float.NegativeInfinity;
                for (var i = 0; i < verts.Length; i++)
                {
                    var w = space.TransformPoint(verts[i]);
                    world[i] = w;
                    lo = Mathf.Min(lo, w.y);
                    hi = Mathf.Max(hi, w.y);
                }

                var band = lo + ((hi - lo) * LowBand);
                var low = new List<int>();
                for (var i = 0; i < world.Length; i++)
                {
                    if (world[i].y <= band)
                    {
                        low.Add(i);
                    }
                }

                var picks = new List<int>();
                var lowStep = Mathf.Max(1, Mathf.CeilToInt(low.Count / (float)MaxLowPoints));
                for (var i = 0; i < low.Count; i += lowStep)
                {
                    picks.Add(low[i]);
                }

                var spreadStep = Mathf.Max(1, world.Length / MaxSpreadPoints);
                for (var i = 0; i < world.Length; i += spreadStep)
                {
                    picks.Add(i);
                }

                var segments = Segments(r);
                var bones = new Transform[picks.Count];
                var local = new Vector3[picks.Count];
                for (var k = 0; k < picks.Count; k++)
                {
                    var p = world[picks[k]];
                    var bone = Nearest(segments, p) ?? space;
                    bones[k] = bone;
                    local[k] = bone.InverseTransformPoint(p);
                }

                return new Skin(bones, local);
            }

            /// <summary>Each bone as a segment to each of its child bones (a point when it has none).</summary>
            private static List<(Transform bone, Vector3 a, Vector3 b)> Segments(SkinnedMeshRenderer r)
            {
                var result = new List<(Transform, Vector3, Vector3)>();
                var bones = r.bones;
                if (bones == null)
                {
                    return result;
                }

                var set = new HashSet<Transform>();
                foreach (var b in bones)
                {
                    if (b != null)
                    {
                        set.Add(b);
                    }
                }

                foreach (var b in set)
                {
                    var head = b.position;
                    var hasChild = false;
                    for (var c = 0; c < b.childCount; c++)
                    {
                        var child = b.GetChild(c);
                        if (set.Contains(child))
                        {
                            result.Add((b, head, child.position));
                            hasChild = true;
                        }
                    }

                    if (!hasChild)
                    {
                        result.Add((b, head, head));
                    }
                }

                return result;
            }

            private static Transform? Nearest(List<(Transform bone, Vector3 a, Vector3 b)> segments, Vector3 p)
            {
                Transform? best = null;
                var bestDistance = float.PositiveInfinity;
                foreach (var (bone, a, b) in segments)
                {
                    var ab = b - a;
                    var len = ab.sqrMagnitude;
                    var t = len > 1e-12f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len) : 0f;
                    var d = (a + (ab * t) - p).sqrMagnitude;
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = bone;
                    }
                }

                return best;
            }
        }
    }
}
