#nullable enable
using Survival.Domain.Campaign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Cover-fits this rect to its parent (same rule as AspectRatioFitter EnvelopeParent) and
    /// shifts it so a <see cref="PortraitFocus"/> point sits as close as possible to the window
    /// centre, without leaving gaps. Drives anchors, size and position; the parent should clip.
    /// </summary>
    public sealed class PortraitCoverFit : UIBehaviour, ILayoutSelfController
    {
        public float Aspect = 1f;
        public float FocusX = PortraitFocus.DefaultX;
        public float FocusY = PortraitFocus.DefaultY;

        private DrivenRectTransformTracker _tracker;
        private bool _applying;

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        protected override void OnDisable()
        {
            _tracker.Clear();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Apply();
        }

        public void SetLayoutHorizontal() => Apply();

        public void SetLayoutVertical() => Apply();

        public void Apply()
        {
            if (_applying || !isActiveAndEnabled)
            {
                return;
            }

            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var win = parent.rect;
            if (win.width <= 0f || win.height <= 0f)
            {
                return;
            }

            _applying = true;
            try
            {
                PortraitFocus.CoverSize(win.width, win.height, Aspect > 0f ? Aspect : 1f, out var artW, out var artH);
                PortraitFocus.CoverShift(win.width, win.height, artW, artH, FocusX, FocusY, out var sx, out var sy);
                _tracker.Clear();
                _tracker.Add(this, rt, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta | DrivenTransformProperties.Pivot);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(artW, artH);
                rt.anchoredPosition = new Vector2(sx, sy);
            }
            finally
            {
                _applying = false;
            }
        }
    }
}
