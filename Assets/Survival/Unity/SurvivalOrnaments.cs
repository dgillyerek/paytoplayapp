#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Survival.Domain.Theme;

namespace Survival.Unity
{
    /// <summary>
    /// Shared Theme A detail kit: filigree frames with corner flourishes, bevel/inner-shadow overlays, paper grain,
    /// folded-end ribbons, gem crests, star plates, shine sweeps, sparkles and the resource icons the theme pack lacks
    /// (hero tome, war banner, hourglass). Sprites are drawn procedurally once, packed into one small atlas texture
    /// (one draw-call batch), and tinted per use; painted theme art wins whenever a key exists.
    /// </summary>
    public static class Ornaments
    {
        public static readonly Color Gold = new Color(0.96f, 0.78f, 0.34f, 1f);
        public static readonly Color Bronze = new Color(0.72f, 0.50f, 0.30f, 1f);
        public static readonly Color Royal = new Color(0.16f, 0.24f, 0.52f, 1f);

        private const int Cell = 64;
        private const int Cols = 8;
        private static Texture2D? _atlas;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly string[] Order = { "frame", "bevel", "grain", "ribbon", "shine", "gem", "star", "tome", "banner", "hourglass", "plate", "crest", "sparkle", "coin", "wheat", "shard" };

        /// <summary>Rarity tint for frames and crests (common .. legendary).</summary>
        public static Color Rarity(string? rarity) => (rarity ?? "").ToLowerInvariant() switch
        {
            "common" => new Color(0.70f, 0.72f, 0.76f, 1f),
            "uncommon" => new Color(0.38f, 0.80f, 0.42f, 1f),
            "rare" => new Color(0.36f, 0.62f, 1f, 1f),
            "epic" => new Color(0.72f, 0.42f, 0.96f, 1f),
            "legendary" => new Color(1f, 0.70f, 0.20f, 1f),
            _ => new Color(0.36f, 0.62f, 1f, 1f),
        };

        public static Sprite Get(string name)
        {
            if (Sprites.TryGetValue(name, out var s))
            {
                return s;
            }

            Build();
            return Sprites[name];
        }

        /// <summary>Procedural icon for a resource with no painted art, or null.</summary>
        public static Sprite? IconFor(string resourceId) => resourceId switch
        {
            "res.hero_xp" => Get("tome"),
            "res.runner_boost" => Get("banner"),
            "res.speedup_m" => Get("hourglass"),
            "res.soft" => Get("coin"),
            "res.food" => Get("wheat"),
            "res.hard" => Get("gem"),
            _ => resourceId.StartsWith("shard.", StringComparison.Ordinal) ? Get("shard") : null,
        };

        private static Image Add(Transform parent, string name, string sprite, Color color, Image.Type type = Image.Type.Sliced)
        {
            var img = SurvivalVisuals.Image(parent, name, color, Get(sprite));
            img.type = type;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Ornate double-line filigree frame with corner flourishes, stretched over the parent.</summary>
        public static Image Frame(Transform parent, Color tint, float inset = 0f)
        {
            var img = Add(parent, "Filigree", "frame", tint);
            SurvivalVisuals.Stretch(img.rectTransform);
            img.rectTransform.offsetMin = new Vector2(-inset, -inset);
            img.rectTransform.offsetMax = new Vector2(inset, inset);
            return img;
        }

        /// <summary>Embossed bevel: top/left highlight, bottom/right inner shadow.</summary>
        public static Image Bevel(Transform parent, float strength = 1f)
        {
            var img = Add(parent, "Bevel", "bevel", new Color(1f, 1f, 1f, strength));
            SurvivalVisuals.Stretch(img.rectTransform);
            return img;
        }

        /// <summary>Tiled paper/leather grain so fills are never flat.</summary>
        public static Image Grain(Transform parent, Color tint)
        {
            var img = Add(parent, "Grain", "grain", tint, Image.Type.Tiled);
            SurvivalVisuals.Stretch(img.rectTransform);
            return img;
        }

        /// <summary>Banner ribbon with folded swallowtail ends and a label.</summary>
        public static Text Ribbon(Transform parent, string label, Color tint, out Image ribbon)
        {
            ribbon = Add(parent, "Ribbon", "ribbon", tint);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(ribbon.transform, "T", label, 16, TextAnchor.MiddleCenter, Color.white), 1.4f);
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            HookSkin.Fit(t, 9, 16);
            HookSkin.PadLabel(t.rectTransform, 0.20f, 0.16f);
            return t;
        }

        /// <summary>Faceted gem set in a gold crest, tinted by rarity.</summary>
        public static Image Crest(Transform parent, Color gem)
        {
            var crest = Add(parent, "Crest", "crest", Color.white, Image.Type.Simple);
            crest.preserveAspect = true;
            var g = Add(crest.transform, "Gem", "gem", gem, Image.Type.Simple);
            g.preserveAspect = true;
            Stretch(g.rectTransform, 0.28f, 0.26f, 0.72f, 0.74f);
            return crest;
        }

        /// <summary>Dark beveled plate behind stars or level text.</summary>
        public static Image Plate(Transform parent, string name)
        {
            var img = Add(parent, name, "plate", Color.white);
            img.rectTransform.SetAsFirstSibling();
            return img;
        }

        public static Image Star(Transform parent, Color tint) => Add(parent, "Star", "star", tint, Image.Type.Simple);

        public static Image Icon(Transform parent, string name, string sprite)
        {
            var img = Add(parent, name, sprite, Color.white, Image.Type.Simple);
            img.preserveAspect = true;
            return img;
        }

        /// <summary>Periodic diagonal light sweep across a button or claimable (clipped to it).</summary>
        public static void Shine(RectTransform target, float period = 2.8f)
        {
            var clip = new GameObject("ShineClip", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            clip.SetParent(target, false);
            SurvivalVisuals.Stretch(clip);
            var band = Add(clip, "Shine", "shine", new Color(1f, 1f, 1f, 0.55f), Image.Type.Simple);
            band.rectTransform.anchorMin = new Vector2(0f, -0.2f);
            band.rectTransform.anchorMax = new Vector2(0.35f, 1.2f);
            band.rectTransform.offsetMin = band.rectTransform.offsetMax = Vector2.zero;
            band.gameObject.AddComponent<ShineSweep>().Period = period;
        }

        /// <summary>A few twinkling sparkles scattered over a highlighted card (cheap: n small quads).</summary>
        public static void Sparkles(RectTransform target, int count, Color tint)
        {
            var key = SurvivalArt.Get(HookArtKeys.FxSparkle);
            var rng = new System.Random(target.name.GetHashCode());
            for (var i = 0; i < count; i++)
            {
                var img = key != null ? SurvivalVisuals.Image(target, "Sparkle" + i, tint, key) : Add(target, "Sparkle" + i, "sparkle", tint, Image.Type.Simple);
                img.raycastTarget = false;
                img.preserveAspect = true;
                var x = (float)rng.NextDouble();
                var y = (float)rng.NextDouble();
                Stretch(img.rectTransform, x - 0.025f, y - 0.09f, x + 0.025f, y + 0.09f);
                var tw = img.gameObject.AddComponent<Twinkle>();
                tw.Phase = (float)rng.NextDouble() * 6.28f;
                tw.Speed = 1.6f + (float)rng.NextDouble() * 1.6f;
            }
        }

        private static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ---------- procedural atlas ----------

        private static void Build()
        {
            if (_atlas != null)
            {
                return;
            }

            var rows = (Order.Length + Cols - 1) / Cols;
            _atlas = new Texture2D(Cols * Cell, rows * Cell, TextureFormat.RGBA32, false) { name = "ThemeA_Ornaments", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[_atlas.width * _atlas.height];
            for (var i = 0; i < Order.Length; i++)
            {
                var ox = (i % Cols) * Cell;
                var oy = (i / Cols) * Cell;
                for (var y = 0; y < Cell; y++)
                {
                    for (var x = 0; x < Cell; x++)
                    {
                        px[((oy + y) * _atlas.width) + ox + x] = Draw(Order[i], x + 0.5f, y + 0.5f);
                    }
                }
            }

            _atlas.SetPixels32(px);
            _atlas.Apply(false, true);
            for (var i = 0; i < Order.Length; i++)
            {
                var rect = new Rect((i % Cols) * Cell, (i / Cols) * Cell, Cell, Cell);
                var border = Order[i] switch
                {
                    "frame" => new Vector4(22, 22, 22, 22),
                    "bevel" => new Vector4(12, 12, 12, 12),
                    "plate" => new Vector4(14, 14, 14, 14),
                    "ribbon" => new Vector4(24, 8, 24, 8),
                    _ => Vector4.zero,
                };
                var sp = Sprite.Create(_atlas, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                sp.name = "ornament." + Order[i];
                Sprites[Order[i]] = sp;
            }
        }

        private static float Sat(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static float Band(float d, float a, float b) => Sat(Math.Min(d - a + 0.5f, b - d + 0.5f));
        private static Color32 C(float r, float g, float b, float a) => new Color32((byte)(Sat(r) * 255), (byte)(Sat(g) * 255), (byte)(Sat(b) * 255), (byte)(Sat(a) * 255));
        private static float Hash(int x, int y) { var h = (uint)((x * 374761393) + (y * 668265263)); h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffff) / 65535f; }
        private static float Circle(float x, float y, float cx, float cy, float r) => Sat(r - (float)Math.Sqrt(((x - cx) * (x - cx)) + ((y - cy) * (y - cy))) + 0.5f);

        /// <summary>Gold shading: brighter toward the top for an embossed metal look.</summary>
        private static Color32 Metal(float a, float y, float k = 1f) { var l = (0.75f + (0.35f * y / Cell)) * k; return C(l, l * 0.94f, l * 0.86f, a); }

        private static Color32 Draw(string what, float x, float y)
        {
            const float n = Cell;
            var e = Math.Min(Math.Min(x, y), Math.Min(n - x, n - y));
            switch (what)
            {
                case "frame":
                {
                    // Outer rim, dark gap, inner hairline, corner diamonds with curls; slice border 22.
                    var a = Math.Max(Band(e, 0.5f, 3.5f), Band(e, 6f, 7.5f) * 0.9f);
                    var cx = x < n / 2 ? x : n - x;
                    var cy = y < n / 2 ? y : n - y;
                    var dia = Sat(7.5f - (Math.Abs(cx - 10f) + Math.Abs(cy - 10f)));
                    var curl = Band((float)Math.Sqrt(((cx - 4f) * (cx - 4f)) + ((cy - 4f) * (cy - 4f))), 14f, 15.5f) * (cx < 20f && cy < 20f && (cx > 10f || cy > 10f) ? 1f : 0f);
                    a = Math.Max(a, Math.Max(dia, curl));
                    var shade = dia > 0.5f && Math.Abs(cx - 10f) + Math.Abs(cy - 10f) < 3f ? 1.25f : 1f;
                    var dark = Band(e, 3.5f, 6f) * 0.55f;
                    return a > 0.02f ? Metal(a, y, shade) : C(0.05f, 0.03f, 0.02f, dark);
                }
                case "bevel":
                {
                    var top = n - y < 12 ? Sat((12 - (n - y)) / 12f) : 0f;
                    var bottom = y < 12 ? Sat((12 - y) / 12f) : 0f;
                    var left = x < 8 ? Sat((8 - x) / 8f) * 0.5f : 0f;
                    var right = n - x < 8 ? Sat((8 - (n - x)) / 8f) * 0.5f : 0f;
                    var hi = Math.Max(top * 0.35f, left * 0.3f);
                    var lo = Math.Max(bottom * 0.45f, right * 0.35f);
                    return hi >= lo ? C(1f, 0.95f, 0.82f, hi) : C(0f, 0f, 0.05f, lo);
                }
                case "grain":
                {
                    var v = (Hash((int)x, (int)y) * 0.6f) + (Hash((int)x / 4, (int)y / 4) * 0.4f);
                    return v > 0.5f ? C(1f, 0.96f, 0.86f, (v - 0.5f) * 0.35f) : C(0f, 0f, 0f, (0.5f - v) * 0.4f);
                }
                case "ribbon":
                {
                    // Body rows 10..54; swallowtail notch on the outer 12px, darker fold 12..22.
                    var side = x < n / 2 ? x : n - x;
                    var body = y > 10 && y < 54;
                    var notch = side < 12 && Math.Abs(y - 32f) < (12 - side) * 1.6f;
                    if (!body || notch)
                    {
                        return C(0, 0, 0, 0);
                    }

                    var fold = side >= 12 && side < 22 ? 0.62f : 1f;
                    var hl = y > 46 ? 1.15f : y < 16 ? 0.8f : 1f;
                    var edge = y < 13 || y > 51 ? 0.7f : 1f;
                    var l = fold * hl * edge;
                    return C(l, l, l, 1f);
                }
                case "shine":
                {
                    var d = Math.Abs((x - (n / 2)) + ((y - (n / 2)) * 0.35f)) / (n / 2);
                    return C(1, 1, 1, Sat(1f - d) * Sat(1f - d));
                }
                case "gem":
                {
                    var d = (Math.Abs(x - 32f) * 1.1f) + Math.Abs(y - 32f);
                    if (d > 28f) return C(0, 0, 0, 0);
                    var facet = x < 32 ? (y > 32 ? 1.25f : 0.85f) : (y > 32 ? 1.05f : 0.65f);
                    var spark = Circle(x, y, 24f, 42f, 4f);
                    var l = facet + spark;
                    return C(l, l, l, Sat(28.5f - d));
                }
                case "star":
                {
                    var dx = x - 32f; var dy = y - 30f;
                    var ang = Math.Atan2(dy, dx) + (Math.PI / 2);
                    var r = Math.Sqrt((dx * dx) + (dy * dy));
                    var k = Math.Cos(5 * ang);
                    var lim = 14 + (14 * Math.Pow((k + 1) / 2, 3));
                    var a = Sat((float)(lim - r + 0.5));
                    var l = 0.85f + (0.3f * y / n);
                    return C(l, l, l, a);
                }
                case "plate":
                {
                    var a = Sat(e);
                    var rim = Band(e, 0.5f, 2.5f);
                    var hi = n - y < 10 ? 0.12f : 0f;
                    return rim > 0.3f ? Metal(rim, y, 0.85f) : C(0.10f + hi, 0.08f + hi, 0.14f + hi, a * 0.92f);
                }
                case "crest":
                {
                    // Heater shield in gold with dark inner field.
                    var w = y > 30 ? 26f : 26f * (float)Math.Sqrt(Math.Max(0, y - 4) / 26f);
                    var dx = Math.Abs(x - 32f);
                    if (y > 58 || y < 4 || dx > w) return C(0, 0, 0, 0);
                    var inner = dx < w - 5 && y > 9 && y < 53;
                    return inner ? C(0.10f, 0.12f, 0.28f, 1f) : Metal(1f, y, 1.05f);
                }
                case "tome":
                {
                    var cover = x > 12 && x < 54 && y > 8 && y < 56;
                    var pages = x > 8 && x <= 14 && y > 10 && y < 54;
                    if (pages) return C(0.95f, 0.90f, 0.76f, 1f);
                    if (!cover) return C(0, 0, 0, 0);
                    var gem = Circle(x, y, 33f, 32f, 6f);
                    var corner = (Math.Abs(x - 50f) < 4 || Math.Abs(x - 16f) < 3) && (Math.Abs(y - 52f) < 4 || Math.Abs(y - 12f) < 4);
                    if (gem > 0.5f) return C(0.4f, 0.8f, 1f, 1f);
                    if (corner || Band(Math.Min(Math.Min(x - 12, 54 - x), Math.Min(y - 8, 56 - y)), 3f, 4.5f) > 0.5f) return Metal(1f, y);
                    return C(0.42f, 0.14f, 0.16f + (0.1f * y / n), 1f);
                }
                case "banner":
                {
                    var pole = Math.Abs(x - 14f) < 2.5f && y > 4 && y < 60;
                    var knob = Circle(x, y, 14f, 59f, 4f);
                    if (pole || knob > 0.5f) return Metal(1f, y);
                    var flag = x > 16 && x < 54 && y > 14 && y < 56 && !(x > 40 && Math.Abs(y - 20f) < (x - 40) * 0.5f && y < 30);
                    if (!flag) return C(0, 0, 0, 0);
                    var emblem = Circle(x, y, 34f, 40f, 7f);
                    var trim = y > 52;
                    return emblem > 0.5f || trim ? Metal(1f, y) : C(0.18f, 0.30f, 0.72f, 1f);
                }
                case "hourglass":
                {
                    if (y > 54 && y < 60 || y > 4 && y < 10) return x > 12 && x < 52 ? Metal(1f, y) : C(0, 0, 0, 0);
                    var w = 4f + (Math.Abs(y - 32f) * 0.8f);
                    var dx = Math.Abs(x - 32f);
                    if (y <= 10 || y >= 54 || dx > w) return (x > 12 && x < 16 || x > 48 && x < 52) && y > 8 && y < 56 ? Metal(1f, y, 0.85f) : C(0, 0, 0, 0);
                    var sand = (y < 32 && y < 10 + (w * 0.9f)) || (y > 32 && y < 40 && dx < 4);
                    return sand ? C(0.95f, 0.76f, 0.36f, 1f) : C(0.75f, 0.88f, 1f, dx > w - 1.5f ? 0.9f : 0.35f);
                }
                case "coin":
                {
                    var a = Circle(x, y, 32f, 32f, 26f);
                    var rim = Band((float)Math.Sqrt(((x - 32) * (x - 32)) + ((y - 32) * (y - 32))), 20f, 26f);
                    var crown = Math.Abs(x - 32f) < 9 && y > 26 && y < 40 && !(y > 34 && Math.Abs(x - 32f) % 6 > 3);
                    return Metal(a, y, rim > 0.5f ? 1.1f : crown ? 1.25f : 0.9f);
                }
                case "wheat":
                {
                    var stalk = Math.Abs(x - 32f + ((y - 32f) * 0.1f)) < 1.6f && y > 6 && y < 44;
                    var a = 0f;
                    for (var k = 0; k < 5; k++)
                    {
                        var gy = 40f + (k * 4.5f);
                        a = Math.Max(a, Math.Max(Circle(x, y, 27f, gy, 4f), Circle(x, y, 37f, gy, 4f)));
                    }

                    a = Math.Max(a, Circle(x, y, 32f, 62f, 4f));
                    if (stalk) return C(0.70f, 0.55f, 0.22f, 1f);
                    return C(0.98f, 0.80f, 0.36f + (0.1f * y / n), a);
                }
                case "shard":
                {
                    var d = Math.Abs(x - 32f) * 2.2f + Math.Abs(y - 32f);
                    if (d > 30f) return C(0, 0, 0, 0);
                    var l = x < 32 ? 1.1f : 0.75f;
                    return C(0.72f * l, 0.42f * l, 0.96f * l, Sat(30.5f - d));
                }
                case "sparkle":
                {
                    var dx = Math.Abs(x - 32f); var dy = Math.Abs(y - 32f);
                    var a = Math.Max(Sat(1f - ((dx * dy) / 18f)) * Sat(1f - ((dx + dy) / 32f)), Circle(x, y, 32f, 32f, 5f));
                    return C(1, 1, 1, a);
                }
            }

            return C(0, 0, 0, 0);
        }
    }

    /// <summary>Moves a shine band across its clip every <see cref="Period"/> seconds (pauses between sweeps).</summary>
    public sealed class ShineSweep : MonoBehaviour
    {
        public float Period = 2.8f;
        private RectTransform? _rt;

        private void Awake() => _rt = (RectTransform)transform;

        private void Update()
        {
            if (_rt == null) return;
            var t = (Time.unscaledTime % Period) / Period;
            var x = Mathf.Lerp(-0.4f, 1.05f, Mathf.Clamp01(t / 0.35f));
            _rt.anchorMin = new Vector2(x, -0.2f);
            _rt.anchorMax = new Vector2(x + 0.35f, 1.2f);
        }
    }

    /// <summary>Scale + alpha pulse for sparkles.</summary>
    public sealed class Twinkle : MonoBehaviour
    {
        public float Phase;
        public float Speed = 2f;
        private Graphic? _g;

        private void Awake() => _g = GetComponent<Graphic>();

        private void Update()
        {
            var s = Mathf.Max(0f, Mathf.Sin((Time.unscaledTime * Speed) + Phase));
            transform.localScale = Vector3.one * (0.4f + (0.8f * s));
            if (_g != null)
            {
                var c = _g.color;
                c.a = s;
                _g.color = c;
            }
        }
    }
}
