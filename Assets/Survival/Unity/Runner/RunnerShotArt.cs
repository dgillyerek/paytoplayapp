#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The runner's projectile art, generated once at load (no new asset files, nothing to import): a real arrow mesh
    /// (thin wooden shaft, steel bodkin head, three vanes of fletching and a nock) and a thorn spike as procedural
    /// meshes split into per-part submeshes, plus two small textures for the magic bolts: a soft radial glow (halo,
    /// muzzle flash) and a wake strip that is white-hot at the head and fades out along its length. Materials are
    /// shared and cached per colour (URP Unlit, additive where it glows), so the SRP batcher folds every shot of a look
    /// into a few draw calls; no lights, no particle systems, no allocation once warm.
    /// </summary>
    internal static class RunnerShotArt
    {
        private static Mesh? _arrow;
        private static Mesh? _thorn;
        private static Mesh? _quad;
        private static Texture2D? _glowTex;
        private static Texture2D? _wakeTex;
        private static readonly Dictionary<Color, Material> Glows = new Dictionary<Color, Material>();
        private static readonly Dictionary<Color, Material> Wakes = new Dictionary<Color, Material>();

        /// <summary>Arrow along +Z, 0.75 long, tip at +0.375. Submeshes: 0 shaft, 1 head, 2 fletching, 3 nock.</summary>
        internal static Mesh Arrow => _arrow != null ? _arrow : _arrow = BuildArrow();

        /// <summary>Oakenshield's thrown thorn: a curved, tapering spike along +Z. Submeshes: 0 body, 1 tip.</summary>
        internal static Mesh Thorn => _thorn != null ? _thorn : _thorn = BuildThorn();

        /// <summary>A unit quad facing -Z (turned to the camera for halos).</summary>
        internal static Mesh Quad => _quad != null ? _quad : _quad = BuildQuad();

        internal static Material Glow(Color c) => Cached(Glows, c, () => Make(GlowTex(), c, 3010));

        internal static Material Wake(Color c) => Cached(Wakes, c, () => Make(WakeTex(), c, 3009));

        private static Material Cached(Dictionary<Color, Material> cache, Color c, System.Func<Material> make)
        {
            if (cache.TryGetValue(c, out var m) && m != null)
            {
                return m;
            }

            m = make();
            cache[c] = m;
            return m;
        }

        // ---------- meshes ----------

        private sealed class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<List<int>> Subs = new List<List<int>>();

            public List<int> Sub(int i)
            {
                while (Subs.Count <= i)
                {
                    Subs.Add(new List<int>());
                }

                return Subs[i];
            }

            public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, bool twoSided = false)
            {
                var n = Vector3.Cross(b - a, c - a).normalized;
                var t = Sub(sub);
                var i = V.Count;
                V.Add(a); V.Add(b); V.Add(c);
                N.Add(n); N.Add(n); N.Add(n);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                if (twoSided)
                {
                    i = V.Count;
                    V.Add(a); V.Add(c); V.Add(b);
                    N.Add(-n); N.Add(-n); N.Add(-n);
                    t.Add(i); t.Add(i + 1); t.Add(i + 2);
                }
            }

            /// <summary>A tapered n-sided tube from z0 (radius r0) to z1 (radius r1), capped; r = 0 gives a point.</summary>
            public void Tube(int sub, int sides, float z0, float r0, float z1, float r1, float bend = 0f)
            {
                for (var s = 0; s < sides; s++)
                {
                    var a0 = s * Mathf.PI * 2f / sides;
                    var a1 = (s + 1) * Mathf.PI * 2f / sides;
                    var p00 = new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, z0);
                    var p01 = new Vector3(Mathf.Cos(a1) * r0, Mathf.Sin(a1) * r0, z0);
                    var p10 = new Vector3(Mathf.Cos(a0) * r1, (Mathf.Sin(a0) * r1) + bend, z1);
                    var p11 = new Vector3(Mathf.Cos(a1) * r1, (Mathf.Sin(a1) * r1) + bend, z1);
                    Tri(sub, p00, p10, p11);
                    if (r0 > 0f)
                    {
                        Tri(sub, p00, p11, p01);
                        Tri(sub, new Vector3(0f, 0f, z0), p01, p00);
                    }

                    if (r1 > 0f)
                    {
                        Tri(sub, new Vector3(0f, bend, z1), p10, p11);
                    }
                }
            }

            public Mesh Done(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(V);
                m.SetNormals(N);
                m.subMeshCount = Subs.Count;
                for (var i = 0; i < Subs.Count; i++)
                {
                    m.SetTriangles(Subs[i], i);
                }

                m.RecalculateBounds();
                m.UploadMeshData(true);
                return m;
            }
        }

        private static Mesh BuildArrow()
        {
            var b = new Builder();
            const float tail = -0.375f;
            const float headBase = 0.27f;
            // Shaft: a 6-sided 1 cm-ish dowel from the nock to the head socket.
            b.Tube(0, 6, tail + 0.02f, 0.011f, headBase, 0.011f);
            // Head: a socket then a four-faced bodkin point, wider than the shaft so it reads at distance.
            b.Tube(1, 4, headBase - 0.02f, 0.016f, headBase + 0.015f, 0.03f);
            b.Tube(1, 4, headBase + 0.015f, 0.03f, 0.375f, 0f);
            // Fletching: three swept vanes at 120 degrees near the tail.
            for (var k = 0; k < 3; k++)
            {
                var rot = Quaternion.AngleAxis(90f + (k * 120f), Vector3.forward);
                var root0 = rot * new Vector3(0f, 0.011f, tail + 0.04f);
                var root1 = rot * new Vector3(0f, 0.011f, tail + 0.17f);
                var tip0 = rot * new Vector3(0f, 0.045f, tail + 0.03f);
                var tip1 = rot * new Vector3(0f, 0.04f, tail + 0.09f);
                b.Tri(2, root0, tip0, tip1, true);
                b.Tri(2, root0, tip1, root1, true);
            }

            // Nock: a short darker collar at the very end.
            b.Tube(3, 6, tail, 0.014f, tail + 0.025f, 0.014f);
            return b.Done("RunnerArrow");
        }

        private static Mesh BuildThorn()
        {
            var b = new Builder();
            b.Tube(0, 6, -0.22f, 0.035f, 0.05f, 0.03f);
            b.Tube(0, 6, 0.05f, 0.03f, 0.16f, 0.016f, 0.01f);
            b.Tube(1, 6, 0.16f, 0.016f, 0.26f, 0f, 0.025f);
            return b.Done("RunnerThorn");
        }

        private static Mesh BuildQuad()
        {
            var m = new Mesh { name = "RunnerShotQuad" };
            m.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            m.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            m.UploadMeshData(true);
            return m;
        }

        // ---------- textures ----------

        /// <summary>Soft radial glow: a hot pinpoint core and a long gaussian-ish falloff.</summary>
        private static Texture2D GlowTex()
        {
            if (_glowTex != null)
            {
                return _glowTex;
            }

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "ShotGlow", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = ((x + 0.5f) / n * 2f) - 1f;
                    var dy = ((y + 0.5f) / n * 2f) - 1f;
                    var r = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var halo = Mathf.Clamp01(1f - r);
                    halo *= halo;
                    var core = Mathf.Exp(-r * r * 40f);
                    var a = Mathf.Clamp01((halo * 0.75f) + core);
                    var w = (byte)(255f * Mathf.Clamp01(0.75f + (core * 0.25f)));
                    px[(y * n) + x] = new Color32(w, w, w, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _glowTex = tex;
        }

        /// <summary>Trail strip (TrailRenderer Stretch: u 0 at the head, 1 at the tail; v across): bright centre line
        /// fading across, and out along the length.</summary>
        private static Texture2D WakeTex()
        {
            if (_wakeTex != null)
            {
                return _wakeTex;
            }

            const int w = 64;
            const int h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "ShotWake", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                var across = 1f - Mathf.Abs(((y + 0.5f) / h * 2f) - 1f);
                for (var x = 0; x < w; x++)
                {
                    var along = 1f - ((x + 0.5f) / w);
                    var a = Mathf.Pow(along, 1.6f) * Mathf.SmoothStep(0f, 1f, across);
                    px[(y * w) + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _wakeTex = tex;
        }

        private static Material Make(Texture2D tex, Color tint, int queue)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = "Shot_" + tex.name };
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
            }

            m.mainTexture = tex;
            if (m.HasProperty("_BaseColor"))
            {
                m.SetColor("_BaseColor", tint);
            }

            m.color = tint;
            LockedBattleGround.MakeTransparent(m);
            // Additive: overlapping bolts brighten instead of muddying.
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.renderQueue = queue;
            return m;
        }
    }
}
