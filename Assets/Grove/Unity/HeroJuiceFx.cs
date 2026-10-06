using System.Collections;
using Grove.Domain.Juice;
using UnityEngine;
using UnityEngine.UI;

namespace Grove.Unity
{
    /// <summary>
    /// Play-mode hero-loop VFX: simple quads / UI tweens sampled from <see cref="HeroJuice"/>.
    /// No mesh trails. Design still owns visual PASS.
    /// </summary>
    internal static class HeroJuiceFx
    {
        public static Color SparkleTint =>
            new Color(HeroJuice.SparkleR, HeroJuice.SparkleG, HeroJuice.SparkleB, HeroJuice.SparkleA);

        public static Vector3 OverlayToWorld(RectTransform ui, Camera cam)
        {
            var corners = new Vector3[4];
            ui.GetWorldCorners(corners);
            var screen = (corners[0] + corners[2]) * 0.5f;
            var z = cam != null ? -cam.transform.position.z : 10f;
            var world = cam != null
                ? cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z))
                : screen;
            world.z = 0f;
            return world;
        }

        public static IEnumerator SquashUi(Transform target, float seconds)
        {
            if (target == null)
            {
                yield break;
            }

            var original = target.localScale;
            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration && target != null)
            {
                t += Time.unscaledDeltaTime;
                HeroJuice.SquashScale(t / duration, out var sx, out var sy);
                target.localScale = new Vector3(original.x * sx, original.y * sy, original.z);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = original;
            }
        }

        public static IEnumerator TickScale(Transform target, float seconds)
        {
            if (target == null)
            {
                yield break;
            }

            var original = target.localScale;
            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration && target != null)
            {
                t += Time.unscaledDeltaTime;
                var k = HeroJuice.PipTickScale(t / duration);
                target.localScale = original * k;
                yield return null;
            }

            if (target != null)
            {
                target.localScale = original;
            }
        }

        public static IEnumerator MergePop(Transform target, float seconds)
        {
            if (target == null)
            {
                yield break;
            }

            var original = target.localScale;
            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration && target != null)
            {
                t += Time.unscaledDeltaTime;
                var k = HeroJuice.MergePopScale(t / duration);
                target.localScale = original * k;
                yield return null;
            }

            if (target != null)
            {
                target.localScale = original;
            }
        }

        public static IEnumerator CardSettle(RectTransform card, float seconds, float bouncePx)
        {
            if (card == null)
            {
                yield break;
            }

            var scale = card.localScale;
            var pos = card.anchoredPosition;
            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration && card != null)
            {
                t += Time.unscaledDeltaTime;
                var u = t / duration;
                var k = HeroJuice.CardSettleScale(u);
                card.localScale = scale * k;
                card.anchoredPosition = pos + new Vector2(0f, HeroJuice.CardSettleOffsetY(u, bouncePx));
                yield return null;
            }

            if (card != null)
            {
                card.localScale = scale;
                card.anchoredPosition = pos;
            }
        }

        public static IEnumerator FlashImage(Image image, Color flash, float seconds)
        {
            if (image == null)
            {
                yield break;
            }

            var original = image.color;
            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration && image != null)
            {
                t += Time.unscaledDeltaTime;
                var u = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
                image.color = Color.Lerp(original, flash, u * 0.55f);
                yield return null;
            }

            if (image != null)
            {
                image.color = original;
            }
        }

        public static IEnumerator WorldArc(
            Transform flyer,
            Vector3 from,
            Vector3 to,
            float loft,
            float seconds)
        {
            if (flyer == null)
            {
                yield break;
            }

            var restScale = flyer.localScale;
            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration && flyer != null)
            {
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / duration);
                HeroJuice.ArcPoint(u, from.x, from.y, to.x, to.y, loft, out var x, out var y);
                flyer.position = new Vector3(x, y, from.z);
                var k = HeroJuice.FlyerScale(u);
                flyer.localScale = restScale * k;
                yield return null;
            }

            if (flyer != null)
            {
                flyer.position = new Vector3(to.x, to.y, from.z);
                flyer.localScale = restScale;
            }
        }

        public static IEnumerator UiArc(RectTransform flyer, Vector3 from, Vector3 to, float loftPx, float seconds)
        {
            if (flyer == null)
            {
                yield break;
            }

            var rest = flyer.localScale;
            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration && flyer != null)
            {
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / duration);
                HeroJuice.ArcPoint(u, from.x, from.y, to.x, to.y, loftPx, out var x, out var y);
                flyer.position = new Vector3(x, y, from.z);
                flyer.localScale = rest * HeroJuice.FlyerScale(u);
                var img = flyer.GetComponent<Image>();
                if (img != null)
                {
                    var c = img.color;
                    c.a = Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(t / duration));
                    img.color = c;
                }

                yield return null;
            }
        }

        public static IEnumerator WorldSparkle(
            Transform parent,
            Vector3 center,
            Sprite? sparkle,
            Sprite fallback,
            float cellSize,
            float seconds)
        {
            var sprite = sparkle != null ? sparkle : fallback;
            if (sprite == null || parent == null)
            {
                yield break;
            }

            var n = HeroJuice.SparkleQuadCount;
            var quads = new Transform[n];
            var bases = new Vector3[n];
            var drifts = new Vector3[n];
            var rest = new Vector3[n];
            for (var i = 0; i < n; i++)
            {
                var ang = (i / (float)n) * Mathf.PI * 2f + 0.31f;
                var radial = cellSize * (i == 0 ? 0.02f : 0.18f + (i % 3) * 0.05f);
                var pos = center + new Vector3(Mathf.Cos(ang) * radial, Mathf.Sin(ang) * radial, -0.12f);
                var size = i == 0 ? cellSize * 0.95f : cellSize * 0.28f;
                var go = GroveVisuals.SpriteObject(
                    "MergeSparkle" + i,
                    parent,
                    pos,
                    size,
                    i == 0 ? Color.white : SparkleTint,
                    i == 0 ? sprite : fallback,
                    16);
                quads[i] = go.transform;
                bases[i] = pos;
                drifts[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * (cellSize * 0.42f);
                rest[i] = go.transform.localScale;
            }

            var t = 0f;
            var duration = Mathf.Max(0.01f, seconds);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / duration);
                HeroJuice.SparkleQuad(u, out var scale, out var alpha);
                for (var i = 0; i < n; i++)
                {
                    if (quads[i] == null)
                    {
                        continue;
                    }

                    quads[i].position = bases[i] + drifts[i] * HeroJuice.EaseOutCubic(u);
                    quads[i].localScale = rest[i] * scale;
                    var sr = quads[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        var c = i == 0 ? Color.white : SparkleTint;
                        c.a = alpha;
                        sr.color = c;
                    }
                }

                yield return null;
            }

            for (var i = 0; i < n; i++)
            {
                if (quads[i] != null)
                {
                    UnityEngine.Object.Destroy(quads[i].gameObject);
                }
            }
        }
    }
}
