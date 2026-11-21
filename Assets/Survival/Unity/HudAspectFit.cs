#nullable enable
using System.Collections.Generic;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Keeps a screen-space HUD the same shape on any portrait phone (Derek 2026-11-20, Pixel 7 Pro). Every Survival
    /// canvas is authored on the 1080 x 1920 Game view in 0..1 anchor boxes. On a taller screen this component:
    /// <list type="bullet">
    /// <item>matches the CanvasScaler to the width, so text, icons and pixel-sized panels are the same share of the
    /// screen's width as on 9:16 (they used to follow the height and grow 22 % wider on 19.5:9);</item>
    /// <item>re-anchors each boxed element into the safe area (notch, gesture bar) at its 9:16 shape: top boxes hang from
    /// the top, bottom boxes stand on the bottom, mid-screen ones stay centred, and the extra height opens in the middle,
    /// where the world shows.</item>
    /// </list>
    /// Full-screen layers (backgrounds, the drag pad, map viewports, dimmers) still fill the whole screen; the walk
    /// descends through them to the boxes they hold. Point-anchored elements (code-placed markers) are left alone.
    /// At 9:16 with no cutout (the PC Game view) every mapping is the identity.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class HudAspectFit : MonoBehaviour
    {
        /// <summary>How deep the walk follows full-screen layers.</summary>
        private const int MaxDepth = 4;

        private const float Edge = 0.001f;

        private struct Fitted
        {
            public Vector2 AuthoredMin, AuthoredMax, AppliedMin, AppliedMax;
        }

        private readonly Dictionary<RectTransform, Fitted> _fitted = new Dictionary<RectTransform, Fitted>();
        private readonly List<RectTransform> _gone = new List<RectTransform>();
        private CanvasScaler? _scaler;
        private RectTransform? _root;
        private Vector2Int _screen;
        private Rect _safe;
        private float _share = 1f;
        private Rect _safeN = new Rect(0f, 0f, 1f, 1f);
        private bool _remap;

        /// <summary>The authored 9:16 frame's height in this canvas's units (the canvas height at 9:16 or wider).</summary>
        public static float FrameHeight(RectTransform canvas)
        {
            var r = canvas.rect;
            return r.height * AspectFit.FrameShare(r.width, r.height);
        }

        private void OnEnable()
        {
            _screen = Vector2Int.zero;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            _scaler ??= GetComponent<CanvasScaler>();
            _root ??= (RectTransform)transform;
            var screen = new Vector2Int(Screen.width, Screen.height);
            var safe = Screen.safeArea;
            var changed = screen != _screen || safe != _safe;
            if (changed)
            {
                _screen = screen;
                _safe = safe;
                var w = Mathf.Max(1f, screen.x);
                var h = Mathf.Max(1f, screen.y);
                if (_scaler != null)
                {
                    _scaler.matchWidthOrHeight = AspectFit.CanvasMatch(w / h);
                }

                _safeN = safe.width > 0f && safe.height > 0f
                    ? new Rect(safe.xMin / w, safe.yMin / h, safe.width / w, safe.height / h)
                    : new Rect(0f, 0f, 1f, 1f);
                _share = AspectFit.FrameShare(safe.width > 0f ? safe.width : w, safe.height > 0f ? safe.height : h);
                _remap = _share < 1f || _safeN.xMin > Edge || _safeN.yMin > Edge || _safeN.xMax < 1f - Edge || _safeN.yMax < 1f - Edge;
            }

            if (!_remap && _fitted.Count == 0)
            {
                // The PC Game view at 9:16 (or wider) with no cutout: nothing to do.
                return;
            }

            Walk(_root, 0, changed);
            Sweep();
        }

        private void Walk(RectTransform parent, int depth, bool changed)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                if (!(parent.GetChild(i) is RectTransform rt) || rt.GetComponent<HudFitIgnore>() != null)
                {
                    continue;
                }

                if (_fitted.TryGetValue(rt, out var f))
                {
                    // Code re-anchored it since the last fit: that is the new authored box.
                    if (rt.anchorMin != f.AppliedMin || rt.anchorMax != f.AppliedMax)
                    {
                        f.AuthoredMin = rt.anchorMin;
                        f.AuthoredMax = rt.anchorMax;
                        Fit(rt, ref f);
                    }
                    else if (changed)
                    {
                        Fit(rt, ref f);
                    }

                    continue;
                }

                var min = rt.anchorMin;
                var max = rt.anchorMax;
                if (FullScreen(min, max))
                {
                    if (depth < MaxDepth)
                    {
                        Walk(rt, depth + 1, changed);
                    }

                    continue;
                }

                if (!Boxed(min, max) || !_remap)
                {
                    continue;
                }

                f = new Fitted { AuthoredMin = min, AuthoredMax = max };
                Fit(rt, ref f);
            }
        }

        private void Fit(RectTransform rt, ref Fitted f)
        {
            var (y0, y1) = AspectFit.PinY(f.AuthoredMin.y, f.AuthoredMax.y, _share);
            var min = new Vector2(
                AspectFit.ToScreen(f.AuthoredMin.x, _safeN.xMin, _safeN.xMax),
                AspectFit.ToScreen(y0, _safeN.yMin, _safeN.yMax));
            var max = new Vector2(
                AspectFit.ToScreen(f.AuthoredMax.x, _safeN.xMin, _safeN.xMax),
                AspectFit.ToScreen(y1, _safeN.yMin, _safeN.yMax));
            rt.anchorMin = min;
            rt.anchorMax = max;
            f.AppliedMin = min;
            f.AppliedMax = max;
            _fitted[rt] = f;
        }

        private void Sweep()
        {
            _gone.Clear();
            foreach (var pair in _fitted)
            {
                if (pair.Key == null)
                {
                    _gone.Add(pair.Key!);
                }
            }

            foreach (var rt in _gone)
            {
                _fitted.Remove(rt);
            }
        }

        private static bool FullScreen(Vector2 min, Vector2 max) =>
            min.x <= Edge && min.y <= Edge && max.x >= 1f - Edge && max.y >= 1f - Edge;

        private static bool Boxed(Vector2 min, Vector2 max) => max.x - min.x > Edge && max.y - min.y > Edge;
    }
}
