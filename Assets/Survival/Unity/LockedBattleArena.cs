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

        private static readonly Color[] Palette =
        {
            new Color(0.36f, 0.25f, 0.16f), // 0 bark
            new Color(0.24f, 0.42f, 0.18f), // 1 leaf
            new Color(0.32f, 0.50f, 0.22f), // 2 leaf light
            new Color(0.16f, 0.31f, 0.20f), // 3 pine
            new Color(0.55f, 0.53f, 0.49f), // 4 stone
            new Color(0.44f, 0.42f, 0.40f), // 5 stone dark
            new Color(0.62f, 0.15f, 0.14f), // 6 banner red
            new Color(0.83f, 0.66f, 0.27f), // 7 gold
        };

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
                    var c = Color.Lerp(g, d, BattleArena.Dirt(wx, z, arenaRadius));
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

        /// <summary>Old courtyard paving: 0.7 m stones in staggered rows, each its own shade, dark grout between.</summary>
        private static Color Flagstone(float x, float z)
        {
            const float stone = 0.7f;
            var row = Mathf.FloorToInt(z / stone);
            var sx = (x / stone) + ((row & 1) * 0.5f);
            var col = Mathf.FloorToInt(sx);
            var fx = sx - col;
            var fz = (z / stone) - row;
            var edge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fz, 1f - fz));
            var shade = 0.82f + (0.3f * BattleArena.Hash(col, row, 9));
            var baseC = new Color(0.56f, 0.54f, 0.50f) * shade;
            return edge < 0.06f ? new Color(0.26f, 0.25f, 0.22f) : baseC;
        }

        private static Texture2D PaletteTexture()
        {
            var tex = new Texture2D(Palette.Length, 1, TextureFormat.RGBA32, false) { name = "battle_arena_palette", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(Palette);
            tex.Apply(false, true);
            return tex;
        }

        private sealed class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<int> T = new List<int>();

            public void Add(Mesh src, Matrix4x4 m, int colour, float jitter = 0f, int seed = 0)
            {
                var start = V.Count;
                var uv = new Vector2((colour + 0.5f) / Palette.Length, 0.5f);
                var verts = src.vertices;
                var norms = src.normals;
                for (var i = 0; i < verts.Length; i++)
                {
                    var p = verts[i];
                    if (jitter > 0f)
                    {
                        // Lumpy rocks: push each vertex in or out by a hash of its direction.
                        var k = 1f + (jitter * ((BattleArena.Noise((p.x * 3f) + seed, (p.y * 3f) + (p.z * 2f), seed) * 2f) - 1f));
                        p *= k;
                    }

                    V.Add(m.MultiplyPoint3x4(p));
                    N.Add(m.MultiplyVector(norms[i]).normalized);
                    Uv.Add(uv);
                }

                foreach (var t in src.triangles)
                {
                    T.Add(start + t);
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

        private static Mesh Primitive(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return mesh;
        }

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
                t.Add(b);
                t.Add(b + 1);
                t.Add(b + 2);
            }

            var mesh = new Mesh { name = "Cone" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh PropsMesh(float arenaRadius)
        {
            var cube = Primitive(PrimitiveType.Cube);
            var cyl = Primitive(PrimitiveType.Cylinder);
            var sphere = Primitive(PrimitiveType.Sphere);
            var cone = Cone(9);
            var b = new Builder();
            var index = 0;
            foreach (var p in BattleArena.Props(arenaRadius))
            {
                index++;
                var y = BattleArena.Height(p.X, p.Z, arenaRadius);
                var at = Matrix4x4.TRS(new Vector3(p.X, y, p.Z), Quaternion.Euler(0f, p.YawDegrees, 0f), Vector3.one * p.Scale);
                var h = BattleArena.Hash(index, 0, 31);
                switch (p.Kind)
                {
                    case BattleArena.PropKind.Tree:
                        b.Add(cyl, at * Matrix4x4.TRS(new Vector3(0f, 0.9f, 0f), Quaternion.identity, new Vector3(0.32f, 0.9f, 0.32f)), 0);
                        b.Add(sphere, at * Matrix4x4.TRS(new Vector3(0f, 2.4f, 0f), Quaternion.identity, new Vector3(2.4f, 2f, 2.4f)), h > 0.5f ? 1 : 2);
                        b.Add(sphere, at * Matrix4x4.TRS(new Vector3(0.6f, 2.0f, 0.3f), Quaternion.identity, new Vector3(1.6f, 1.4f, 1.6f)), h > 0.5f ? 2 : 1);
                        b.Add(sphere, at * Matrix4x4.TRS(new Vector3(-0.5f, 2.1f, -0.4f), Quaternion.identity, new Vector3(1.5f, 1.3f, 1.5f)), 1);
                        break;
                    case BattleArena.PropKind.Pine:
                        b.Add(cyl, at * Matrix4x4.TRS(new Vector3(0f, 0.6f, 0f), Quaternion.identity, new Vector3(0.25f, 0.6f, 0.25f)), 0);
                        b.Add(cone, at * Matrix4x4.TRS(new Vector3(0f, 0.8f, 0f), Quaternion.identity, new Vector3(2.0f, 1.8f, 2.0f)), 3);
                        b.Add(cone, at * Matrix4x4.TRS(new Vector3(0f, 1.8f, 0f), Quaternion.identity, new Vector3(1.6f, 1.6f, 1.6f)), 3);
                        b.Add(cone, at * Matrix4x4.TRS(new Vector3(0f, 2.8f, 0f), Quaternion.identity, new Vector3(1.1f, 1.4f, 1.1f)), 3);
                        break;
                    case BattleArena.PropKind.Bush:
                        b.Add(sphere, at * Matrix4x4.TRS(new Vector3(0f, 0.3f, 0f), Quaternion.identity, new Vector3(1.2f, 0.8f, 1.1f)), 1);
                        b.Add(sphere, at * Matrix4x4.TRS(new Vector3(0.45f, 0.25f, 0.2f), Quaternion.identity, new Vector3(0.8f, 0.6f, 0.8f)), 2);
                        break;
                    case BattleArena.PropKind.Rock:
                        b.Add(sphere, at * Matrix4x4.TRS(new Vector3(0f, 0.15f, 0f), Quaternion.Euler(h * 40f, 0f, h * 25f), new Vector3(1.5f, 0.9f, 1.2f)), h > 0.5f ? 4 : 5, 0.25f, index);
                        break;
                    case BattleArena.PropKind.Pillar:
                        var tall = h > 0.4f ? 2.6f : 1.1f + (h * 1.5f);
                        b.Add(cube, at * Matrix4x4.TRS(new Vector3(0f, 0.15f, 0f), Quaternion.identity, new Vector3(0.9f, 0.3f, 0.9f)), 5);
                        b.Add(cyl, at * Matrix4x4.TRS(new Vector3(0f, 0.3f + (tall * 0.5f), 0f), Quaternion.Euler(0f, 0f, h > 0.4f ? 0f : 4f), new Vector3(0.6f, tall * 0.5f, 0.6f)), 4);
                        if (h > 0.4f)
                        {
                            b.Add(cube, at * Matrix4x4.TRS(new Vector3(0f, 0.3f + tall + 0.12f, 0f), Quaternion.identity, new Vector3(0.85f, 0.24f, 0.85f)), 5);
                        }

                        break;
                    case BattleArena.PropKind.BrokenWall:
                        for (var k = 0; k < 4; k++)
                        {
                            var hk = BattleArena.Hash(index, k, 57);
                            var height = 0.6f + (hk * 1.6f);
                            b.Add(cube, at * Matrix4x4.TRS(new Vector3(-1.2f + (k * 0.8f), height * 0.5f, 0f), Quaternion.Euler(0f, 0f, (hk - 0.5f) * 6f), new Vector3(0.82f, height, 0.5f)), (k & 1) == 0 ? 4 : 5);
                        }

                        break;
                    case BattleArena.PropKind.Banner:
                        b.Add(cyl, at * Matrix4x4.TRS(new Vector3(0f, 1.5f, 0f), Quaternion.identity, new Vector3(0.08f, 1.5f, 0.08f)), 0);
                        b.Add(cube, at * Matrix4x4.TRS(new Vector3(0.36f, 2.3f, 0f), Quaternion.identity, new Vector3(0.66f, 1.1f, 0.03f)), 6);
                        b.Add(sphere, at * Matrix4x4.TRS(new Vector3(0f, 3.05f, 0f), Quaternion.identity, Vector3.one * 0.14f), 7);
                        break;
                }
            }

            return b.Build();
        }
    }
}
