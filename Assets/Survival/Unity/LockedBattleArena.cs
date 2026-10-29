using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Roster;
using UnityEngine;
using UnityEngine.Rendering;

namespace Survival.Unity
{
    /// <summary>
    /// The configured battle's ground (Derek 2026-10-28: "the ground is very basic, lets make it more interesting"): a
    /// Theme A courtyard field. One ground mesh, flat in the arena and rolling into low hills beyond
    /// (<see cref="BattleArena.Height"/>), with one texture baked at load from the ThemePack grass and dirt tiles: worn
    /// dirt where the fight happens, a broken flagstone ring at its edge, grass patches beyond. Around it, one combined
    /// mesh of props from <see cref="BattleArena.Props"/> (ruined pillars, a broken wall, banners, rocks, bushes, broad
    /// trees and pines), all coloured from a small palette atlas. Two draw calls, no extra lights, no prop shadows; the
    /// props all stand outside the arena so none hides a fighter.
    /// </summary>
    public static class LockedBattleArena
    {
        public const int TextureSize = 1024;
        public const int GridCells = 96;

        /// <summary>Ground half-size beyond the prop ring (the fog takes the edge).</summary>
        public const float ApronMetres = 14f;

        public static void Build(Transform parent, float arenaRadius)
        {
            var half = arenaRadius + BattleArena.PropRingMetres + ApronMetres;
            var root = new GameObject("BattleArena").transform;
            root.SetParent(parent, false);

            var ground = new GameObject("ArenaGround", typeof(MeshFilter), typeof(MeshRenderer));
            ground.transform.SetParent(root, false);
            ground.GetComponent<MeshFilter>().sharedMesh = GroundMesh(half, arenaRadius);
            var gr = ground.GetComponent<MeshRenderer>();
            gr.sharedMaterial = LockedBattleGround.Lit(BakeGround(half, arenaRadius), new Color(0.39f, 0.41f, 0.20f, 1f), false);
            gr.shadowCastingMode = ShadowCastingMode.Off;

            var props = new GameObject("ArenaProps", typeof(MeshFilter), typeof(MeshRenderer));
            props.transform.SetParent(root, false);
            props.GetComponent<MeshFilter>().sharedMesh = PropsMesh(arenaRadius);
            var pr = props.GetComponent<MeshRenderer>();
            pr.sharedMaterial = LockedBattleGround.Lit(PaletteTexture(), Color.grey, false);
            pr.shadowCastingMode = ShadowCastingMode.Off;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = LockedBattleGround.Horizon;
            RenderSettings.fogStartDistance = LockedBattleGroundSpec.FogStartMetres + 6f;
            RenderSettings.fogEndDistance = LockedBattleGroundSpec.FogEndMetres + 12f;
        }

        private static Mesh GroundMesh(float half, float arenaRadius)
        {
            var n = GridCells;
            var verts = new Vector3[(n + 1) * (n + 1)];
            var uvs = new Vector2[verts.Length];
            for (var j = 0; j <= n; j++)
            {
                for (var i = 0; i <= n; i++)
                {
                    var u = i / (float)n;
                    var v = j / (float)n;
                    var x = Mathf.Lerp(-half, half, u);
                    var z = Mathf.Lerp(-half, half, v);
                    verts[(j * (n + 1)) + i] = new Vector3(x, BattleArena.Height(x, z, arenaRadius), z);
                    uvs[(j * (n + 1)) + i] = new Vector2(u, v);
                }
            }

            var tris = new int[n * n * 6];
            var t = 0;
            for (var j = 0; j < n; j++)
            {
                for (var i = 0; i < n; i++)
                {
                    var a = (j * (n + 1)) + i;
                    tris[t++] = a;
                    tris[t++] = a + n + 1;
                    tris[t++] = a + 1;
                    tris[t++] = a + 1;
                    tris[t++] = a + n + 1;
                    tris[t++] = a + n + 2;
                }
            }

            var mesh = new Mesh { name = "BattleArenaGround", vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The ground colour map: ThemePack grass and dirt at their real tile size, mixed by the arena masks.</summary>
        private static Texture2D BakeGround(float half, float arenaRadius)
        {
            var grassTex = LockedBattleGround.Load(LockedBattleGroundSpec.GrassTileRel);
            var dirtTex = LockedBattleGround.Load(LockedBattleGroundSpec.DirtTrackRel);
            var grass = grassTex != null ? grassTex.GetPixels32() : null;
            var dirt = dirtTex != null ? dirtTex.GetPixels32() : null;
            var size = TextureSize;
            var px = new Color32[size * size];
            var metresPerPx = 2f * half / size;
            var fallbackGrass = new Color(0.39f, 0.41f, 0.20f);
            var fallbackDirt = new Color(0.58f, 0.52f, 0.38f);
            for (var y = 0; y < size; y++)
            {
                var z = -half + ((y + 0.5f) * metresPerPx);
                for (var x = 0; x < size; x++)
                {
                    var wx = -half + ((x + 0.5f) * metresPerPx);
                    var g = Sample(grass, grassTex, wx, z, LockedBattleGroundSpec.GrassTileMetres, fallbackGrass);
                    // Lighter and darker grass by large-scale noise, and a touch darker in the hollows.
                    var tone = 0.85f + (0.3f * BattleArena.Noise(wx * 0.07f, z * 0.07f, 3));
                    g *= tone;
                    var d = Sample(dirt, dirtTex, wx * 0.5f, z, LockedBattleGroundSpec.TrackRepeatMetres, fallbackDirt);
                    // Painted grass: long brush strokes, warm sunlit patches, cooler hollows.
                    var stroke = BattleArena.Noise((wx * 2.6f) + (z * 0.9f), z * 0.7f, 19);
                    g *= 0.9f + (0.2f * stroke);
                    g = Color.Lerp(g, new Color(0.60f, 0.62f, 0.27f), 0.28f * BattleArena.Noise(wx * 0.18f, z * 0.18f, 29));
                    var dm = BattleArena.Dirt(wx, z, arenaRadius);
                    var c = Color.Lerp(g, d, dm);
                    c = Speckle(c, wx, z, dm);
                    var s = BattleArena.Stone(wx, z, arenaRadius);
                    if (s > 0.01f)
                    {
                        c = Color.Lerp(c, Flagstone(wx, z), s);
                    }

                    c.a = 1f;
                    px[(y * size) + x] = c;
                }
            }

            if (grassTex != null)
            {
                Object.Destroy(grassTex);
            }

            if (dirtTex != null)
            {
                Object.Destroy(dirtTex);
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "battle_arena_ground", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>Small painted details: wildflowers and tufts in the grass, pebbles with a lit side in the dirt.</summary>
        private static Color Speckle(Color c, float x, float z, float dirt)
        {
            const float cell = 0.3f;
            var cx = Mathf.FloorToInt(x / cell);
            var cz = Mathf.FloorToInt(z / cell);
            var h = BattleArena.Hash(cx, cz, 83);
            var ox = (cx + 0.25f + (0.5f * BattleArena.Hash(cx, cz, 84))) * cell;
            var oz = (cz + 0.25f + (0.5f * BattleArena.Hash(cx, cz, 85))) * cell;
            var d = new Vector2(x - ox, z - oz).magnitude;
            if (dirt > 0.45f && h < 0.16f && d < 0.05f + (0.05f * h / 0.16f))
            {
                var pebble = Color.Lerp(new Color(0.56f, 0.52f, 0.45f), new Color(0.44f, 0.42f, 0.40f), BattleArena.Hash(cx, cz, 86));
                return (z - oz) > 0f ? pebble * 1.15f : pebble * 0.85f;
            }

            if (dirt < 0.3f && h > 0.985f && d < 0.05f)
            {
                return BattleArena.Hash(cx, cz, 87) > 0.5f ? new Color(0.95f, 0.92f, 0.78f) : new Color(0.93f, 0.78f, 0.28f);
            }

            if (dirt < 0.35f && h > 0.8f && h < 0.9f && d < 0.09f)
            {
                return c * (0.78f + (0.6f * Mathf.Clamp01((z - oz + 0.09f) / 0.18f)));
            }

            return c;
        }

        private static Color Sample(Color32[]? pixels, Texture2D? tex, float x, float z, float tileMetres, Color fallback)
        {
            if (pixels == null || tex == null)
            {
                return fallback;
            }

            var u = Mathf.Repeat(x / tileMetres, 1f);
            var v = Mathf.Repeat(z / tileMetres, 1f);
            var i = Mathf.Min(tex.width - 1, (int)(u * tex.width));
            var j = Mathf.Min(tex.height - 1, (int)(v * tex.height));
            return pixels[(j * tex.width) + i];
        }

        /// <summary>
        /// Old courtyard paving in the Theme A sandstone: 0.7 m stones in staggered rows, each its own warm shade, a lit
        /// bevel on the sun side and a shadowed one opposite, chipped corners, hairline cracks and moss in the joints.
        /// </summary>
        private static Color Flagstone(float x, float z)
        {
            const float stone = 0.7f;
            var row = Mathf.FloorToInt(z / stone);
            var sx = (x / stone) + ((row & 1) * 0.5f);
            var col = Mathf.FloorToInt(sx);
            var fx = sx - col;
            var fz = (z / stone) - row;
            var h = BattleArena.Hash(col, row, 9);
            var edge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fz, 1f - fz));
            var corner = Mathf.Min(Mathf.Min(fx, 1f - fx) + Mathf.Min(fz, 1f - fz), 1f);
            var mossy = BattleArena.Noise(x * 1.7f, z * 1.7f, 61);
            if (edge < 0.05f || corner < 0.09f + (0.08f * h))
            {
                // Grout, or a chipped-away corner: dark earth, green where moss has crept in.
                return Color.Lerp(new Color(0.24f, 0.22f, 0.17f), new Color(0.27f, 0.36f, 0.16f), Mathf.Clamp01((mossy - 0.4f) * 3f));
            }

            var baseC = Color.Lerp(new Color(0.62f, 0.55f, 0.43f), new Color(0.50f, 0.47f, 0.42f), h) * (0.9f + (0.18f * BattleArena.Noise(x * 6f, z * 6f, 13)));
            if (edge < 0.12f)
            {
                baseC *= (fz > 0.5f || fx < 0.5f) ? 1.14f : 0.8f;
            }

            // A hairline crack across some stones.
            if (h > 0.7f && Mathf.Abs(fz - 0.5f - ((fx - 0.5f) * (h - 0.85f) * 4f)) < 0.018f)
            {
                baseC *= 0.62f;
            }

            if (mossy > 0.62f && edge < 0.2f)
            {
                baseC = Color.Lerp(baseC, new Color(0.33f, 0.43f, 0.18f), 0.55f);
            }

            return baseC;
        }

        // Theme A colours (the castle town and loading art: warm sandstone, yellow-green grass, red and royal-blue cloth, gold).
        private static readonly Color[] Palette =
        {
            new Color(0.33f, 0.22f, 0.14f), // 0 bark
            new Color(0.20f, 0.36f, 0.14f), // 1 leaf deep
            new Color(0.32f, 0.50f, 0.18f), // 2 leaf mid
            new Color(0.50f, 0.64f, 0.24f), // 3 leaf sunlit
            new Color(0.14f, 0.30f, 0.20f), // 4 pine
            new Color(0.66f, 0.58f, 0.44f), // 5 sandstone
            new Color(0.50f, 0.44f, 0.35f), // 6 sandstone shadow
            new Color(0.48f, 0.47f, 0.44f), // 7 grey rock
            new Color(0.34f, 0.44f, 0.17f), // 8 moss
            new Color(0.66f, 0.14f, 0.13f), // 9 banner red
            new Color(0.17f, 0.27f, 0.62f), // 10 royal blue
            new Color(0.88f, 0.68f, 0.26f), // 11 gold
            new Color(0.45f, 0.31f, 0.18f), // 12 pole wood
            new Color(0.95f, 0.90f, 0.70f), // 13 flowers
            new Color(0.78f, 0.20f, 0.22f), // 14 berries
            new Color(0.34f, 0.31f, 0.27f), // 15 rubble
        };

        private const int Tones = 4;
        private const int Moss = 8;

        /// <summary>Each colour in four painted tones, dark at the base to warm and light at the top (hand-shaded look).</summary>
        private static Texture2D PaletteTexture()
        {
            var tex = new Texture2D(Palette.Length, Tones, TextureFormat.RGBA32, false) { name = "battle_arena_palette", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[Palette.Length * Tones];
            for (var r = 0; r < Tones; r++)
            {
                var k = 0.68f + (0.16f * r);
                for (var c = 0; c < Palette.Length; c++)
                {
                    var col = Palette[c] * k;
                    // Light tones lean warm, dark tones lean cool, the way the theme's painted art shades.
                    col += new Color(0.03f, 0.015f, -0.02f) * (r - 1.5f);
                    col.a = 1f;
                    px[(r * Palette.Length) + c] = col;
                }
            }

            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        private sealed class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<int> T = new List<int>();

            /// <summary>
            /// Add one part. Tone runs from dark at the part's base to light at its top, with noise so no two parts
            /// match; <paramref name="jitter"/> pushes vertices in and out (lumpy rock, leaf clusters, chipped stone);
            /// <paramref name="moss"/> paints upward faces moss green in patches.
            /// </summary>
            public void Add(Mesh src, Matrix4x4 m, int colour, float jitter = 0f, int seed = 0, bool moss = false, float toneBias = 0f)
            {
                var start = V.Count;
                var verts = src.vertices;
                var norms = src.normals;
                var b = src.bounds;
                var span = Mathf.Max(1e-4f, b.size.y);
                for (var i = 0; i < verts.Length; i++)
                {
                    var p = verts[i];
                    if (jitter > 0f)
                    {
                        var k = 1f + (jitter * ((BattleArena.Noise((p.x * 3f) + seed, (p.y * 3f) + (p.z * 2f), seed) * 2f) - 1f));
                        p *= k;
                    }

                    var w = m.MultiplyPoint3x4(p);
                    var wn = m.MultiplyVector(norms[i]).normalized;
                    var up = (verts[i].y - b.min.y) / span;
                    var t = (0.75f * up) + (0.35f * BattleArena.Noise(w.x * 1.9f, (w.z * 1.9f) + w.y, seed + 3)) + (0.2f * wn.y) + toneBias - 0.1f;
                    var row = Mathf.Clamp((int)(t * Tones), 0, Tones - 1);
                    var c = colour;
                    if (moss && wn.y > 0.45f && BattleArena.Noise(w.x * 1.3f, w.z * 1.3f, seed + 17) > 0.42f)
                    {
                        c = Moss;
                    }

                    V.Add(w);
                    N.Add(wn);
                    Uv.Add(new Vector2((c + 0.5f) / Palette.Length, (row + 0.5f) / Tones));
                }

                foreach (var tri in src.triangles)
                {
                    T.Add(start + tri);
                }
            }

            public Mesh Build()
            {
                var mesh = new Mesh { name = "BattleArenaProps", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(V);
                mesh.SetNormals(N);
                mesh.SetUVs(0, Uv);
                mesh.SetTriangles(T, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Mesh Finish(string name, List<Vector3> v, List<int> t, List<Vector3>? n = null)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            if (n != null)
            {
                mesh.SetNormals(n);
            }
            else
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Low-poly sphere (once-subdivided icosahedron, 80 tris) radius 0.5: leaf clusters, rocks, bushes.</summary>
        private static Mesh Ico()
        {
            var g = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var v = new List<Vector3>
            {
                new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0),
                new Vector3(0, -1, g), new Vector3(0, 1, g), new Vector3(0, -1, -g), new Vector3(0, 1, -g),
                new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1),
            };
            int[] f =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var t = new List<int>();
            var mid = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                var key = ((long)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
                if (!mid.TryGetValue(key, out var i))
                {
                    i = v.Count;
                    v.Add((v[a] + v[b]) * 0.5f);
                    mid[key] = i;
                }

                return i;
            }

            for (var i = 0; i < f.Length; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                t.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            var n = new List<Vector3>();
            for (var i = 0; i < v.Count; i++)
            {
                n.Add(v[i].normalized);
                v[i] = v[i].normalized * 0.5f;
            }

            return Finish("Ico", v, t, n);
        }

        /// <summary>Tapered prism, y -1..1, bottom radius 0.5 and top radius 0.5*<paramref name="taper"/>, capped. Trunks, poles, column drums.</summary>
        private static Mesh Prism(int sides, float taper, bool smooth)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (var i = 0; i < sides; i++)
            {
                var a0 = i * Mathf.PI * 2f / sides;
                var a1 = (i + 1) * Mathf.PI * 2f / sides;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                var b = v.Count;
                v.Add((d0 * 0.5f) + Vector3.down);
                v.Add((d0 * 0.5f * taper) + Vector3.up);
                v.Add((d1 * 0.5f * taper) + Vector3.up);
                v.Add((d1 * 0.5f) + Vector3.down);
                var flat = (d0 + d1).normalized;
                n.Add(smooth ? d0 : flat);
                n.Add(smooth ? d0 : flat);
                n.Add(smooth ? d1 : flat);
                n.Add(smooth ? d1 : flat);
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            var top = v.Count;
            v.Add(Vector3.up);
            n.Add(Vector3.up);
            for (var i = 0; i < sides; i++)
            {
                var a = i * Mathf.PI * 2f / sides;
                v.Add((new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.5f * taper) + Vector3.up);
                n.Add(Vector3.up);
            }

            for (var i = 0; i < sides; i++)
            {
                t.AddRange(new[] { top, top + 1 + ((i + 1) % sides), top + 1 + i });
            }

            return Finish("Prism", v, t, n);
        }

        /// <summary>Cone, base radius 0.5 at y 0, tip at y 1, with a slightly drooping skirt: pine tiers, finials.</summary>
        private static Mesh Cone(int segments)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            for (var i = 0; i < segments; i++)
            {
                var a0 = i * Mathf.PI * 2f / segments;
                var a1 = (i + 1) * Mathf.PI * 2f / segments;
                var b = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f));
                v.Add(new Vector3(0f, 1f, 0f));
                v.Add(new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f));
                t.AddRange(new[] { b, b + 1, b + 2 });
                // Underside so tiers read from the low camera.
                var c = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f));
                v.Add(new Vector3(0f, 0.25f, 0f));
                v.Add(new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f));
                t.AddRange(new[] { c, c + 2, c + 1 });
            }

            return Finish("Cone", v, t);
        }

        /// <summary>Bevelled block (cube with chamfered edges, 44 tris), -0.5..0.5: masonry, column caps, plinths.</summary>
        private static Mesh Bevel(float chamfer)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            var c = 0.5f - chamfer;
            // Each face is an inset quad; the chamfers are quads joining neighbouring faces; corners are triangles.
            var axes = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var a in axes)
            {
                var u = Mathf.Abs(a.y) > 0.5f ? Vector3.right : Vector3.up;
                var w = Vector3.Cross(a, u);
                var b = v.Count;
                v.Add((a * 0.5f) + (u * c) + (w * c));
                v.Add((a * 0.5f) + (u * c) - (w * c));
                v.Add((a * 0.5f) - (u * c) - (w * c));
                v.Add((a * 0.5f) - (u * c) + (w * c));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            // Chamfer strips for the 12 edges.
            for (var i = 0; i < 6; i++)
            {
                for (var j = i + 1; j < 6; j++)
                {
                    var a = axes[i];
                    var bAxis = axes[j];
                    if (Mathf.Abs(Vector3.Dot(a, bAxis)) > 0.5f)
                    {
                        continue;
                    }

                    var along = Vector3.Cross(a, bAxis);
                    var b = v.Count;
                    v.Add((a * 0.5f) + (bAxis * c) + (along * c));
                    v.Add((a * 0.5f) + (bAxis * c) - (along * c));
                    v.Add((bAxis * 0.5f) + (a * c) - (along * c));
                    v.Add((bAxis * 0.5f) + (a * c) + (along * c));
                    t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                }
            }

            // Corner triangles.
            foreach (var sx in new[] { -1f, 1f })
            {
                foreach (var sy in new[] { -1f, 1f })
                {
                    foreach (var sz in new[] { -1f, 1f })
                    {
                        var b = v.Count;
                        v.Add(new Vector3(sx * 0.5f, sy * c, sz * c));
                        v.Add(new Vector3(sx * c, sy * 0.5f, sz * c));
                        v.Add(new Vector3(sx * c, sy * c, sz * 0.5f));
                        t.AddRange(new[] { b, b + 1, b + 2 });
                    }
                }
            }

            // Wind every triangle to face outward (the block is convex and centred).
            for (var i = 0; i < t.Count; i += 3)
            {
                var p0 = v[t[i]];
                var p1 = v[t[i + 1]];
                var p2 = v[t[i + 2]];
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), p0 + p1 + p2) < 0f)
                {
                    (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
                }
            }

            // Flat shading: one vertex per triangle corner.
            var fv = new List<Vector3>();
            var ft = new List<int>();
            for (var i = 0; i < t.Count; i++)
            {
                fv.Add(v[t[i]]);
                ft.Add(i);
            }

            return Finish("Bevel", fv, ft);
        }

        /// <summary>
        /// Banner cloth hanging from y 0 down to -1, x 0..1: folds across it, a swallowtail hem, both sides
        /// (point filtered palette so the tone rows read as light and shadow on the folds).
        /// </summary>
        private static Mesh Cloth()
        {
            const int cols = 8;
            const int rows = 4;
            var v = new List<Vector3>();
            var t = new List<int>();
            for (var r = 0; r <= rows; r++)
            {
                for (var c = 0; c <= cols; c++)
                {
                    var x = (float)c / cols;
                    var hem = 1f - (0.28f * (1f - Mathf.Abs((x * 2f) - 1f)));
                    var y = -hem * r / rows;
                    var fold = Mathf.Sin(x * Mathf.PI * 3f) * 0.05f * (0.4f + ((float)r / rows));
                    v.Add(new Vector3(x, y, fold + (0.06f * x * r / rows)));
                }
            }

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var i = (r * (cols + 1)) + c;
                    t.AddRange(new[] { i, i + 1, i + cols + 1, i + 1, i + cols + 2, i + cols + 1 });
                }
            }

            var front = Finish("ClothFront", v, t);
            var n = new List<Vector3>(front.normals);
            var count = v.Count;
            for (var i = 0; i < count; i++)
            {
                v.Add(v[i]);
                n.Add(-n[i]);
            }

            var back = new List<int>(t);
            for (var i = 0; i < back.Count; i += 3)
            {
                t.Add(back[i] + count);
                t.Add(back[i + 2] + count);
                t.Add(back[i + 1] + count);
            }

            return Finish("Cloth", v, t, n);
        }

        private static Matrix4x4 P(Vector3 at, Vector3 size, Vector3 euler = default) => Matrix4x4.TRS(at, Quaternion.Euler(euler), size);

        private static Mesh PropsMesh(float arenaRadius)
        {
            var ico = Ico();
            var cube = Bevel(0.09f);
            var trunk = Prism(7, 0.62f, true);
            var drum = Prism(10, 1f, false);
            var pole = Prism(6, 0.8f, true);
            var cone = Cone(10);
            var cloth = Cloth();
            var b = new Builder();
            var index = 0;
            foreach (var p in BattleArena.Props(arenaRadius))
            {
                index++;
                var y = BattleArena.Height(p.X, p.Z, arenaRadius);
                var at = Matrix4x4.TRS(new Vector3(p.X, y, p.Z), Quaternion.Euler(0f, p.YawDegrees, 0f), Vector3.one * p.Scale);
                var h = BattleArena.Hash(index, 0, 31);
                float H(int k) => BattleArena.Hash(index, k, 71);
                switch (p.Kind)
                {
                    case BattleArena.PropKind.Tree:
                        // Tapered trunk with root flares and two limbs, crowned by lumpy leaf clusters in three greens.
                        b.Add(trunk, at * P(new Vector3(0f, 0.95f, 0f), new Vector3(0.42f, 0.95f, 0.42f), new Vector3(0f, 0f, (h - 0.5f) * 6f)), 0, 0.08f, index);
                        for (var k = 0; k < 3; k++)
                        {
                            b.Add(trunk, at * P(Quaternion.Euler(0f, (k * 120f) + (h * 60f), 0f) * new Vector3(0.18f, 0.12f, 0f), new Vector3(0.16f, 0.22f, 0.16f), new Vector3(0f, (k * 120f) + (h * 60f), 55f)), 0, 0f, index, false, -0.3f);
                        }

                        b.Add(trunk, at * P(new Vector3(0.35f, 1.75f, 0f), new Vector3(0.16f, 0.5f, 0.16f), new Vector3(0f, h * 90f, -38f)), 0);
                        b.Add(trunk, at * P(new Vector3(-0.3f, 1.65f, 0.15f), new Vector3(0.14f, 0.45f, 0.14f), new Vector3(0f, h * 90f, 35f)), 0);
                        var clusters = 6 + (int)(h * 3f);
                        for (var k = 0; k < clusters; k++)
                        {
                            var a = (k * 137.5f) + (H(k) * 40f);
                            var r = k == 0 ? 0f : 0.55f + (0.45f * H(k + 9));
                            var cy = k == 0 ? 2.55f : 1.95f + (0.9f * H(k + 3));
                            var s = k == 0 ? 2.0f : 1.05f + (0.55f * H(k + 5));
                            var off = Quaternion.Euler(0f, a, 0f) * new Vector3(r, cy, 0f);
                            // Higher clusters catch the sun: lighter green.
                            var green = cy > 2.5f ? 3 : (H(k + 7) > 0.5f ? 2 : 1);
                            b.Add(ico, at * P(off, new Vector3(s, s * 0.85f, s)), green, 0.16f, index * 7 + k);
                        }

                        break;
                    case BattleArena.PropKind.Pine:
                        b.Add(trunk, at * P(new Vector3(0f, 0.7f, 0f), new Vector3(0.28f, 0.7f, 0.28f)), 0, 0.06f, index);
                        for (var k = 0; k < 4; k++)
                        {
                            var w = 2.2f - (k * 0.45f);
                            b.Add(cone, at * P(new Vector3(0f, 0.75f + (k * 0.85f), 0f), new Vector3(w, 1.35f, w), new Vector3(0f, (k * 23f) + (h * 40f), (H(k) - 0.5f) * 6f)), 4, 0.1f, index * 5 + k, false, (k * 0.12f) - 0.1f);
                        }

                        break;
                    case BattleArena.PropKind.Bush:
                        for (var k = 0; k < 4; k++)
                        {
                            var off = Quaternion.Euler(0f, k * 95f, 0f) * new Vector3(k == 0 ? 0f : 0.42f, 0.32f + (0.08f * H(k)), 0f);
                            var s = k == 0 ? 1.15f : 0.7f + (0.25f * H(k + 2));
                            b.Add(ico, at * P(off, new Vector3(s, s * 0.78f, s)), k == 0 ? 2 : (H(k + 4) > 0.5f ? 1 : 3), 0.18f, index * 3 + k);
                        }

                        // A few flowers or berries dotting the top.
                        var dots = h > 0.5f ? 13 : 14;
                        for (var k = 0; k < 5; k++)
                        {
                            var off = Quaternion.Euler(0f, (k * 72f) + (h * 50f), 0f) * new Vector3(0.38f + (0.2f * H(k + 11)), 0.62f + (0.15f * H(k + 13)), 0f);
                            b.Add(ico, at * P(off, Vector3.one * 0.12f), dots, 0f, 0, false, 0.4f);
                        }

                        break;
                    case BattleArena.PropKind.Rock:
                        b.Add(ico, at * P(new Vector3(0f, 0.18f, 0f), new Vector3(1.55f, 0.95f, 1.25f), new Vector3(h * 40f, 0f, h * 25f)), h > 0.5f ? 7 : 6, 0.28f, index, true);
                        b.Add(ico, at * P(new Vector3(0.75f, 0.08f, 0.35f), new Vector3(0.6f, 0.4f, 0.5f), new Vector3(0f, h * 90f, 20f)), 7, 0.3f, index + 101, true);
                        b.Add(ico, at * P(new Vector3(-0.55f, 0.04f, -0.5f), new Vector3(0.35f, 0.22f, 0.3f)), 15, 0.3f, index + 202);
                        break;
                    case BattleArena.PropKind.Pillar:
                        var tall = h > 0.4f ? 2.6f : 1.1f + (h * 1.5f);
                        var lean = h > 0.4f ? 0f : 4f;
                        // Stepped plinth, a column of drums with a torus-like moulding at the foot, a bevelled capital.
                        b.Add(cube, at * P(new Vector3(0f, 0.12f, 0f), new Vector3(1.05f, 0.24f, 1.05f)), 6, 0.04f, index, true);
                        b.Add(cube, at * P(new Vector3(0f, 0.34f, 0f), new Vector3(0.85f, 0.2f, 0.85f)), 5, 0.03f, index + 1, true);
                        b.Add(drum, at * P(new Vector3(0f, 0.5f, 0f), new Vector3(0.74f, 0.07f, 0.74f)), 5);
                        var drums = Mathf.Max(1, Mathf.RoundToInt(tall / 0.65f));
                        var dh = tall / drums;
                        for (var k = 0; k < drums; k++)
                        {
                            var top = k == drums - 1 && h <= 0.4f;
                            b.Add(drum, at * P(new Vector3(lean * 0.004f * k, 0.57f + (dh * (k + 0.5f)), 0f), new Vector3(0.6f - (0.015f * k), dh * 0.48f, 0.6f - (0.015f * k)), new Vector3(0f, H(k) * 36f, lean)), H(k + 20) > 0.6f ? 6 : 5, top ? 0.25f : 0.03f, index * 11 + k, true);
                        }

                        if (h > 0.4f)
                        {
                            b.Add(drum, at * P(new Vector3(0f, 0.57f + tall + 0.05f, 0f), new Vector3(0.72f, 0.06f, 0.72f)), 5);
                            b.Add(cube, at * P(new Vector3(0f, 0.57f + tall + 0.2f, 0f), new Vector3(0.92f, 0.22f, 0.92f), new Vector3(0f, 0f, 0f)), 5, 0.03f, index, true);
                        }
                        else
                        {
                            // The fallen top lies in pieces at the foot.
                            b.Add(drum, at * P(new Vector3(0.9f, 0.28f, 0.3f), new Vector3(0.56f, 0.3f, 0.56f), new Vector3(85f, h * 180f, 0f)), 6, 0.08f, index + 3, true);
                            b.Add(cube, at * P(new Vector3(-0.7f, 0.12f, 0.6f), new Vector3(0.4f, 0.24f, 0.35f), new Vector3(10f, h * 120f, 8f)), 15, 0.1f, index + 4);
                        }

                        break;
                    case BattleArena.PropKind.BrokenWall:
                        // Courses of individual bevelled blocks, broken down to a jagged top, rubble at the foot.
                        for (var k = 0; k < 4; k++)
                        {
                            var column = 1 + (int)(BattleArena.Hash(index, k, 57) * 4f);
                            for (var c = 0; c < column; c++)
                            {
                                var stagger = (c & 1) * 0.2f;
                                var hb = BattleArena.Hash(index * 13 + k, c, 59);
                                var block = new Vector3(0.78f - (0.06f * hb), 0.4f, 0.55f);
                                var pos = new Vector3(-1.2f + (k * 0.8f) + stagger - 0.1f, 0.2f + (c * 0.41f), (hb - 0.5f) * 0.05f);
                                b.Add(cube, at * P(pos, block, new Vector3(0f, (hb - 0.5f) * 5f, (hb - 0.5f) * 3f)), hb > 0.65f ? 6 : 5, 0.04f, index * 31 + (k * 7) + c, true);
                            }
                        }

                        for (var k = 0; k < 4; k++)
                        {
                            var off = new Vector3(-1.4f + (H(k) * 2.8f), 0.08f, 0.55f + (0.35f * H(k + 1)));
                            b.Add(cube, at * P(off, new Vector3(0.36f, 0.22f, 0.3f) * (0.7f + (0.5f * H(k + 2))), new Vector3(H(k + 3) * 30f, H(k + 4) * 180f, H(k + 5) * 20f)), k == 0 ? 15 : 6, 0.12f, index + (k * 5));
                        }

                        break;
                    case BattleArena.PropKind.Banner:
                        // Stone footing, wood pole with a crossbar, folded swallowtail cloth with gold trim and a gold finial.
                        var cloth1 = h > 0.35f ? 9 : 10;
                        b.Add(cube, at * P(new Vector3(0f, 0.12f, 0f), new Vector3(0.5f, 0.24f, 0.5f), new Vector3(0f, 20f, 0f)), 5, 0.06f, index, true);
                        b.Add(pole, at * P(new Vector3(0f, 1.6f, 0f), new Vector3(0.11f, 1.6f, 0.11f)), 12);
                        b.Add(pole, at * P(new Vector3(0.38f, 2.85f, 0f), new Vector3(0.07f, 0.45f, 0.07f), new Vector3(0f, 0f, 90f)), 12);
                        b.Add(cloth, at * P(new Vector3(0.0f, 2.82f, 0f), new Vector3(0.78f, 1.35f, 1f)), cloth1, 0f, index, false, 0.15f);
                        b.Add(cube, at * P(new Vector3(0.39f, 2.78f, 0.01f), new Vector3(0.8f, 0.07f, 0.05f)), 11, 0f, 0, false, 0.3f);
                        b.Add(cube, at * P(new Vector3(0.39f, 2.3f, 0.06f), new Vector3(0.22f, 0.22f, 0.04f), new Vector3(0f, 0f, 45f)), 11, 0f, 0, false, 0.4f);
                        b.Add(drum, at * P(new Vector3(0f, 3.24f, 0f), new Vector3(0.16f, 0.03f, 0.16f)), 11);
                        b.Add(cone, at * P(new Vector3(0f, 3.26f, 0f), new Vector3(0.16f, 0.28f, 0.16f)), 11, 0f, 0, false, 0.4f);
                        break;
                }
            }

            return b.Build();
        }
    }
}
