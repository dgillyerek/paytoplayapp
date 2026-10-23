#nullable enable
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The fire patch's art (Update 7), generated once at load from a few small formulas, so there are no new files
    /// and nothing to import: a soft ground-glow decal, a scorched ground decal with an irregular burnt edge, a thin
    /// hot rim ring, a flame tongue (yellow-white at the root, orange through the middle, red at the tip, fading out)
    /// and a narrower hot-core tongue. Four 64x64 / 32x64 textures (~45 KB) and six shared materials serve every patch
    /// on the road: a patch is a few dozen quads on those, the SRP batcher folds them into about six draw calls, and
    /// the animation only moves transforms (no allocation, no material instances, no lights).
    /// </summary>
    internal static class RunnerFireArt
    {
        private const int Disc = 64;
        private const int TongueW = 32;
        private const int TongueH = 64;

        private static Texture2D? _glow;
        private static Texture2D? _scorch;
        private static Texture2D? _rim;
        private static Texture2D? _tongue;
        private static Texture2D? _core;

        private static Material? _scorchMat;
        private static Material? _glowMat;
        private static Material? _rimMat;
        private static Material? _tongueMat;
        private static Material? _coreMat;
        private static Material? _emberMat;

        /// <summary>Soft burnt ground under and around the flames (alpha blended, dark).</summary>
        internal static Material Scorch => _scorchMat != null ? _scorchMat : _scorchMat = Make(ScorchTex(), Color.white, false, 2995);

        /// <summary>Warm light spilled on the ground (additive orange).</summary>
        internal static Material Glow => _glowMat != null ? _glowMat : _glowMat = Make(GlowTex(), new Color(1f, 0.5f, 0.12f, 0.8f), true, 3001);

        /// <summary>The hot edge of the damage footprint (additive).</summary>
        internal static Material Rim => _rimMat != null ? _rimMat : _rimMat = Make(RimTex(), new Color(1f, 0.55f, 0.15f, 0.9f), true, 3002);

        /// <summary>Flame tongues: orange-to-red gradient baked in the texture (additive).</summary>
        internal static Material Tongue => _tongueMat != null ? _tongueMat : _tongueMat = Make(TongueTex(), new Color(1f, 1f, 1f, 0.85f), true, 3003);

        /// <summary>The hot core inside each tongue (additive, yellow-white).</summary>
        internal static Material Core => _coreMat != null ? _coreMat : _coreMat = Make(CoreTex(), new Color(1f, 1f, 1f, 0.95f), true, 3004);

        /// <summary>Rising embers (additive, bright).</summary>
        internal static Material Ember => _emberMat != null ? _emberMat : _emberMat = Make(GlowTex(), new Color(1f, 0.82f, 0.35f, 1f), true, 3005);

        /// <summary>Builds every texture and material now (the prewarm calls it under the loading screen).</summary>
        internal static void Warm()
        {
            _ = Scorch;
            _ = Glow;
            _ = Rim;
            _ = Tongue;
            _ = Core;
            _ = Ember;
        }

        /// <summary>Smooth 0..1 ramp.</summary>
        internal static float Smooth(float a, float b, float x)
        {
            var t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - (2f * t));
        }

        /// <summary>A cheap closed-form wobble of an angle, in -1..1 (breaks the ring edge up).</summary>
        internal static float Wobble(float angle) =>
            (0.5f * Mathf.Sin((3f * angle) + 1.3f)) + (0.3f * Mathf.Sin((7f * angle) + 0.4f)) + (0.2f * Mathf.Sin((13f * angle) + 2.1f));

        /// <summary>The flame colour from root (0) to tip (1): yellow-white, orange, red.</summary>
        internal static Color Ramp(float y)
        {
            if (y < 0.25f)
            {
                return Color.Lerp(new Color(1f, 0.97f, 0.7f, 1f), new Color(1f, 0.62f, 0.14f, 1f), y / 0.25f);
            }

            if (y < 0.6f)
            {
                return Color.Lerp(new Color(1f, 0.62f, 0.14f, 1f), new Color(0.95f, 0.3f, 0.06f, 1f), (y - 0.25f) / 0.35f);
            }

            return Color.Lerp(new Color(0.95f, 0.3f, 0.06f, 1f), new Color(0.7f, 0.08f, 0.02f, 1f), (y - 0.6f) / 0.4f);
        }

        private static Texture2D GlowTex()
        {
            if (_glow != null)
            {
                return _glow;
            }

            var px = new Color32[Disc * Disc];
            for (var y = 0; y < Disc; y++)
            {
                for (var x = 0; x < Disc; x++)
                {
                    var u = (((x + 0.5f) / Disc) * 2f) - 1f;
                    var v = (((y + 0.5f) / Disc) * 2f) - 1f;
                    var a = Mathf.Clamp01(1f - Mathf.Sqrt((u * u) + (v * v)));
                    a *= a;
                    px[(y * Disc) + x] = new Color32(255, (byte)(215 + (a * 40f)), (byte)(120 + (a * 120f)), (byte)(a * 255f));
                }
            }

            return _glow = Bake("FireGlow", Disc, Disc, px);
        }

        private static Texture2D ScorchTex()
        {
            if (_scorch != null)
            {
                return _scorch;
            }

            var px = new Color32[Disc * Disc];
            for (var y = 0; y < Disc; y++)
            {
                for (var x = 0; x < Disc; x++)
                {
                    var u = (((x + 0.5f) / Disc) * 2f) - 1f;
                    var v = (((y + 0.5f) / Disc) * 2f) - 1f;
                    var r = Mathf.Sqrt((u * u) + (v * v));
                    r += 0.05f * Wobble(Mathf.Atan2(v, u));
                    // Dark filled middle, a darker burnt rim just inside the edge, then a soft fade into the grass.
                    var body = 1f - Smooth(0.72f, 0.98f, r);
                    var rim = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.07f, 2f));
                    var a = Mathf.Clamp01((0.55f * body) + (0.35f * rim * body));
                    px[(y * Disc) + x] = new Color32(16, 9, 5, (byte)(a * 255f));
                }
            }

            return _scorch = Bake("FireScorch", Disc, Disc, px);
        }

        private static Texture2D RimTex()
        {
            if (_rim != null)
            {
                return _rim;
            }

            var px = new Color32[Disc * Disc];
            for (var y = 0; y < Disc; y++)
            {
                for (var x = 0; x < Disc; x++)
                {
                    var u = (((x + 0.5f) / Disc) * 2f) - 1f;
                    var v = (((y + 0.5f) / Disc) * 2f) - 1f;
                    var r = Mathf.Sqrt((u * u) + (v * v)) + (0.03f * Wobble(Mathf.Atan2(v, u) * 2f));
                    var a = Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.07f, 2f)) * (1f - Smooth(0.96f, 1f, r));
                    px[(y * Disc) + x] = new Color32(255, (byte)(150 + (a * 60f)), 60, (byte)(a * 255f));
                }
            }

            return _rim = Bake("FireRim", Disc, Disc, px);
        }

        private static Texture2D TongueTex() => _tongue != null ? _tongue : _tongue = BakeTongue(1f, 1f, false, "FireTongue");

        private static Texture2D CoreTex() => _core != null ? _core : _core = BakeTongue(0.5f, 0.7f, true, "FireCore");

        /// <summary>A teardrop tongue, root at the bottom row, leaning right as it rises.</summary>
        private static Texture2D BakeTongue(float widthScale, float heightScale, bool core, string name)
        {
            var px = new Color32[TongueW * TongueH];
            for (var row = 0; row < TongueH; row++)
            {
                var y = (row + 0.5f) / TongueH;
                var t = Mathf.Clamp01(y / heightScale);
                var half = widthScale * 0.92f * Mathf.Pow(Mathf.Max(0f, 1f - t), 0.55f) * (1f - (0.35f * t));
                var lean = 0.22f * t * t;
                for (var col = 0; col < TongueW; col++)
                {
                    var x = ((((col + 0.5f) / TongueW) * 2f) - 1f) - lean;
                    var edge = Mathf.Clamp01((half - Mathf.Abs(x)) / ((0.45f * half) + 0.03f));
                    var a = edge * (y < heightScale ? 1f : 0f);
                    a *= 0.35f + (0.65f * (1f - (t * t)));
                    var c = core ? Color.Lerp(new Color(1f, 0.98f, 0.82f, 1f), new Color(1f, 0.78f, 0.28f, 1f), t) : Ramp(t);
                    px[(row * TongueW) + col] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));
                }
            }

            return Bake(name, TongueW, TongueH, px);
        }

        private static Texture2D Bake(string name, int w, int h, Color32[] pixels)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Material Make(Texture2D tex, Color tint, bool additive, int queue)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = "Fire_" + tex.name };
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
            if (additive)
            {
                // Premultiplied-style glow: colour * alpha added to what is behind, so overlapping flames brighten.
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            }

            m.renderQueue = queue;
            return m;
        }
    }
}
