#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Ironhowl's boulder, built in code (Derek 2026-11-03: "a lot more detail to the rock"; no new art). A twice-subdivided
    /// icosphere pushed out by layered noise, cut flat by a few chipped planes, scored by crack grooves, flat-shaded so the
    /// facets catch the light, and painted with a generated stone texture (grain, darker cracks, pale chip edges, moss on
    /// top) plus a normal map from the same height field. Deterministic per seed.
    /// </summary>
    public static class BossRockMesh
    {
        private const int TexSize = 128;

        public static Mesh Build(int seed, float radius, bool chips = true)
        {
            var rng = new System.Random(seed);
            var (verts, tris) = Icosphere(chips ? 2 : 1);
            var off = new Vector3((float)rng.NextDouble() * 100f, (float)rng.NextDouble() * 100f, (float)rng.NextDouble() * 100f);
            var planes = new List<(Vector3 N, float D)>();
            for (var i = 0; chips && i < 5; i++)
            {
                planes.Add((Random3(rng).normalized, 0.72f + ((float)rng.NextDouble() * 0.14f)));
            }

            var cracks = new List<Vector3>();
            for (var i = 0; chips && i < 3; i++)
            {
                cracks.Add(Random3(rng).normalized);
            }

            var stretch = new Vector3(1f + ((float)rng.NextDouble() * 0.25f), 0.78f + ((float)rng.NextDouble() * 0.12f), 0.9f + ((float)rng.NextDouble() * 0.2f));
            for (var i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                var r = 1f + (0.22f * (Fbm((v * 1.6f) + off, 4) - 0.5f)) + (0.06f * (Fbm((v * 5f) + off, 2) - 0.5f));
                foreach (var c in cracks)
                {
                    // A groove along a great circle, a few centimetres deep and fading out along its length.
                    var d = Mathf.Abs(Vector3.Dot(v, c));
                    if (d < 0.07f)
                    {
                        r -= 0.07f * (1f - (d / 0.07f)) * Fbm((v * 3f) + off + c, 1);
                    }
                }

                var p = Vector3.Scale(v * r, stretch);
                foreach (var (n, depth) in planes)
                {
                    var along = Vector3.Dot(p, n);
                    if (along > depth)
                    {
                        p -= n * (along - depth);
                    }
                }

                verts[i] = p * radius;
            }

            // Flat shading: three unshared vertices per face, UVs from a box projection on the face normal.
            var mv = new Vector3[tris.Count];
            var uv = new Vector2[tris.Count];
            var idx = new int[tris.Count];
            for (var t = 0; t < tris.Count; t += 3)
            {
                var a = verts[tris[t]];
                var b = verts[tris[t + 1]];
                var c = verts[tris[t + 2]];
                var n = Vector3.Cross(b - a, c - a).normalized;
                for (var k = 0; k < 3; k++)
                {
                    var p = verts[tris[t + k]] / radius;
                    mv[t + k] = verts[tris[t + k]];
                    uv[t + k] = Mathf.Abs(n.y) > 0.6f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                    uv[t + k] = (uv[t + k] * 0.5f) + new Vector2(0.5f, 0.5f);
                    idx[t + k] = t + k;
                }
            }

            var mesh = new Mesh { name = "BossRock" + seed, vertices = mv, uv = uv, triangles = idx };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The stone material: generated albedo (grain, cracks, moss) and its normal map, lit.</summary>
        public static Material Material(Material fallback)
        {
            var albedo = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, true) { name = "BossRockAlbedo", wrapMode = TextureWrapMode.Repeat };
            var normal = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, true, true) { name = "BossRockNormal", wrapMode = TextureWrapMode.Repeat };
            var h = new float[TexSize * TexSize];
            for (var y = 0; y < TexSize; y++)
            {
                for (var x = 0; x < TexSize; x++)
                {
                    var p = new Vector3(x / 16f, y / 16f, 3.3f);
                    var grain = Fbm(p, 5);
                    var vein = Mathf.Abs(Fbm((p * 0.5f) + new Vector3(9f, 1f, 0f), 3) - 0.5f);
                    var crack = Mathf.Clamp01(1f - (vein / 0.035f));
                    h[(y * TexSize) + x] = grain - (crack * 0.6f);
                    var stone = Color.Lerp(new Color(0.33f, 0.31f, 0.29f), new Color(0.58f, 0.55f, 0.5f), grain);
                    stone = Color.Lerp(stone, new Color(0.12f, 0.11f, 0.1f), crack);
                    var moss = Mathf.Clamp01((Fbm((p * 0.35f) + new Vector3(4f, 7f, 2f), 3) - 0.58f) * 6f) * (y / (float)TexSize);
                    stone = Color.Lerp(stone, new Color(0.27f, 0.36f, 0.17f), moss * 0.85f);
                    var speck = Fbm(p * 6f, 1) > 0.8f ? 0.12f : 0f;
                    albedo.SetPixel(x, y, stone + new Color(speck, speck, speck, 0f));
                }
            }

            for (var y = 0; y < TexSize; y++)
            {
                for (var x = 0; x < TexSize; x++)
                {
                    float H(int i, int j) => h[(((j + TexSize) % TexSize) * TexSize) + ((i + TexSize) % TexSize)];
                    var dx = (H(x + 1, y) - H(x - 1, y)) * 3f;
                    var dy = (H(x, y + 1) - H(x, y - 1)) * 3f;
                    var n = new Vector3(-dx, -dy, 1f).normalized;
                    normal.SetPixel(x, y, new Color((n.x * 0.5f) + 0.5f, (n.y * 0.5f) + 0.5f, (n.z * 0.5f) + 0.5f, 1f));
                }
            }

            albedo.Apply(true);
            normal.Apply(true);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = shader != null ? new Material(shader) : new Material(fallback);
            m.name = "BossRockStone";
            m.color = Color.white;
            m.mainTexture = albedo;
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", albedo);
                m.SetColor("_BaseColor", Color.white);
            }

            if (m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1.2f);
                m.EnableKeyword("_NORMALMAP");
            }

            if (m.HasProperty("_Smoothness"))
            {
                m.SetFloat("_Smoothness", 0.12f);
            }

            return m;
        }

        private static Vector3 Random3(System.Random r) => new Vector3((float)r.NextDouble() - 0.5f, (float)r.NextDouble() - 0.5f, (float)r.NextDouble() - 0.5f);

        private static float Fbm(Vector3 p, int octaves)
        {
            var sum = 0f;
            var amp = 0.5f;
            var norm = 0f;
            for (var i = 0; i < octaves; i++)
            {
                var a = Mathf.PerlinNoise(p.x + p.z, p.y);
                var b = Mathf.PerlinNoise(p.y + 31.7f, p.z - p.x + 17.3f);
                sum += amp * (a + b) * 0.5f;
                norm += amp;
                amp *= 0.5f;
                p *= 2.03f;
            }

            return sum / norm;
        }

        private static (List<Vector3> Verts, List<int> Tris) Icosphere(int subdivisions)
        {
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0), new(0, -1, t), new(0, 1, t),
                new(0, -1, -t), new(0, 1, -t), new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1)
            };
            for (var i = 0; i < verts.Count; i++)
            {
                verts[i] = verts[i].normalized;
            }

            var tris = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            for (var s = 0; s < subdivisions; s++)
            {
                var cache = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (!cache.TryGetValue(key, out var m))
                    {
                        m = verts.Count;
                        verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                        cache[key] = m;
                    }

                    return m;
                }

                var next = new List<int>(tris.Count * 4);
                for (var i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }

                tris = next;
            }

            return (verts, tris);
        }
    }
}
