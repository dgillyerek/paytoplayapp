#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// Per-portrait cover-fit focus in image space (x left-to-right, y top-to-bottom, 0..1).
    /// Default (0.5, 0.5) is the centre of the PNG. Used by the Heroes/Foes framed portrait so a
    /// face that sits off-centre in a square painting stays in the narrower detail box.
    /// Nightfang's eyes were measured on the 512×512 PNG (glowing purple irises at (65, 262) and
    /// (155, 262)); the focus is their midpoint.
    /// </summary>
    public static class PortraitFocus
    {
        public const float DefaultX = 0.5f;
        public const float DefaultY = 0.5f;

        /// <summary>Nightfang eye midpoint: front (65, 262), rear (155, 262), mid (110, 262) on the 512 PNG.</summary>
        public const float NightfangX = 0.215f;

        public const float NightfangY = 0.512f;

        public const string NightfangKey = "theme_a.hero.portrait.nightfang";

        static readonly Dictionary<string, (float X, float Y)> Points = new Dictionary<string, (float X, float Y)>(StringComparer.OrdinalIgnoreCase)
        {
            { NightfangKey, (NightfangX, NightfangY) },
            { "nightfang", (NightfangX, NightfangY) },
        };

        /// <summary>Focus for a pack portrait key or a short name (e.g. "nightfang"). Unknown keys are the centre.</summary>
        public static void Of(string? key, out float x, out float y) => Of(key, null, out x, out y);

        /// <summary>Looks up <paramref name="key"/>, then <paramref name="alt"/> (a hero id like foe.nightfang).</summary>
        public static void Of(string? key, string? alt, out float x, out float y)
        {
            if (Try(key, out x, out y) || Try(alt, out x, out y))
            {
                return;
            }

            x = DefaultX;
            y = DefaultY;
        }

        static bool Try(string? key, out float x, out float y)
        {
            x = DefaultX;
            y = DefaultY;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (Points.TryGetValue(key, out var p))
            {
                x = p.X;
                y = p.Y;
                return true;
            }

            var dot = key.LastIndexOf('.');
            if (dot >= 0 && dot < key.Length - 1 && Points.TryGetValue(key.Substring(dot + 1), out p))
            {
                x = p.X;
                y = p.Y;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Size of an art rect with width/height <paramref name="artAspect"/> that cover-fits a window
        /// without squashing (the same rule as AspectRatioFitter EnvelopeParent).
        /// </summary>
        public static void CoverSize(float windowW, float windowH, float artAspect, out float artW, out float artH)
        {
            var aspect = artAspect > 0f ? artAspect : 1f;
            var windowAspect = windowH > 0f ? windowW / windowH : 1f;
            if (windowAspect < aspect)
            {
                artH = Math.Max(0f, windowH);
                artW = artH * aspect;
            }
            else
            {
                artW = Math.Max(0f, windowW);
                artH = aspect > 0f ? artW / aspect : Math.Max(0f, windowH);
            }
        }

        /// <summary>
        /// Source rectangle in image space (x, y from the top-left, 0..1) shown by a cover-fit window
        /// focused at (<paramref name="focusX"/>, <paramref name="focusY"/>). Always inside 0..1, and
        /// large enough that the window is filled (no letterbox gaps).
        /// </summary>
        public static void CoverVisible(float windowW, float windowH, float artAspect, float focusX, float focusY, out float x, out float y, out float w, out float h)
        {
            CoverSize(windowW, windowH, artAspect, out var artW, out var artH);
            w = artW > 0f ? Clamp01(windowW / artW) : 1f;
            h = artH > 0f ? Clamp01(windowH / artH) : 1f;
            var fx = Clamp01(focusX);
            var fy = Clamp01(focusY);
            x = Clamp(fx - (w * 0.5f), 0f, 1f - w);
            y = Clamp(fy - (h * 0.5f), 0f, 1f - h);
        }

        /// <summary>
        /// Anchored-position shift (parent and art pivots at 0.5, Unity y-up) that puts the image-space
        /// focus as close as possible to the window centre, clamped so the art still covers the window.
        /// A positive X moves the art right, which brings a left-of-centre face into the box.
        /// </summary>
        public static void CoverShift(float windowW, float windowH, float artW, float artH, float focusX, float focusY, out float shiftX, out float shiftY)
        {
            var fx = Clamp01(focusX);
            var fy = Clamp01(focusY);
            var desiredX = (0.5f - fx) * artW;
            var desiredY = (fy - 0.5f) * artH;
            var maxX = Math.Max(0f, (artW - windowW) * 0.5f);
            var maxY = Math.Max(0f, (artH - windowH) * 0.5f);
            shiftX = Clamp(desiredX, -maxX, maxX);
            shiftY = Clamp(desiredY, -maxY, maxY);
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
