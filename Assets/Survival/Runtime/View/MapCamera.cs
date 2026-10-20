#nullable enable
using System;

namespace Survival.Domain.View
{
    /// <summary>
    /// Pan / zoom math for a scrollable 2D map (World and Village views). World units are content pixels with the
    /// origin bottom-left and y up; the viewport is the on-screen map area in canvas units. The camera keeps the
    /// view inside the map: zoom is clamped to [<see cref="MinZoom"/>, <see cref="MaxZoom"/>] where the floor is
    /// never below "cover" (the map always fills the viewport), and the center is clamped so no edge shows.
    /// Screen offsets are relative to the viewport center, y up.
    /// </summary>
    public sealed class MapCamera
    {
        /// <summary>Keyboard pan speed in screen units per second (WASD / arrows).</summary>
        public const float KeyPanPerSecond = 900f;

        /// <summary>Keyboard zoom rate per second (Q / E, + / -): factor = exp(rate * dt).</summary>
        public const float KeyZoomPerSecond = 1.6f;

        /// <summary>Mouse wheel: zoom factor per wheel notch.</summary>
        public const float WheelStep = 1.12f;

        /// <summary>Shift + drag: vertical drag of this many screen units doubles / halves the zoom.</summary>
        public const float ShiftDragPerDoubling = 360f;

        private readonly float _minZoomConfig;

        public MapCamera(float worldWidth, float worldHeight, float viewWidth, float viewHeight, float minZoom, float maxZoom, float startZoom)
        {
            if (worldWidth <= 0f || worldHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(worldWidth), "World size must be positive.");
            }

            if (minZoom <= 0f || maxZoom < minZoom)
            {
                throw new ArgumentOutOfRangeException(nameof(minZoom), "Need 0 < min_zoom <= max_zoom.");
            }

            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            _minZoomConfig = minZoom;
            ConfigMaxZoom = maxZoom;
            CenterX = worldWidth * 0.5f;
            CenterY = worldHeight * 0.5f;
            Zoom = startZoom;
            SetViewport(viewWidth, viewHeight);
        }

        public float WorldWidth { get; }
        public float WorldHeight { get; }
        public float ViewWidth { get; private set; }
        public float ViewHeight { get; private set; }

        /// <summary>Content scale: 1 = one map pixel per canvas unit.</summary>
        public float Zoom { get; private set; }

        public float CenterX { get; private set; }
        public float CenterY { get; private set; }

        public float ConfigMaxZoom { get; }

        /// <summary>Smallest zoom that still covers the viewport (no void around the map).</summary>
        public float CoverZoom => Math.Max(ViewWidth / WorldWidth, ViewHeight / WorldHeight);

        public float MinZoom => Math.Max(_minZoomConfig, CoverZoom);

        public float MaxZoom => Math.Max(ConfigMaxZoom, MinZoom);

        /// <summary>Visible map rect (world units).</summary>
        public float VisibleWidth => ViewWidth / Zoom;
        public float VisibleHeight => ViewHeight / Zoom;
        public float Left => CenterX - VisibleWidth * 0.5f;
        public float Right => CenterX + VisibleWidth * 0.5f;
        public float Bottom => CenterY - VisibleHeight * 0.5f;
        public float Top => CenterY + VisibleHeight * 0.5f;

        /// <summary>Content anchoredPosition when its pivot (0,0) sits on the viewport center: -center * zoom.</summary>
        public float ContentX => -CenterX * Zoom;
        public float ContentY => -CenterY * Zoom;

        public void SetViewport(float viewWidth, float viewHeight)
        {
            ViewWidth = Math.Max(1f, viewWidth);
            ViewHeight = Math.Max(1f, viewHeight);
            Zoom = ClampZoom(Zoom);
            Clamp();
        }

        public float ClampZoom(float zoom) => Math.Min(MaxZoom, Math.Max(MinZoom, float.IsNaN(zoom) ? MinZoom : zoom));

        /// <summary>Drag: content follows the finger, so the camera moves the other way.</summary>
        public void PanScreen(float dx, float dy)
        {
            CenterX -= dx / Zoom;
            CenterY -= dy / Zoom;
            Clamp();
        }

        public void PanWorld(float dx, float dy)
        {
            CenterX += dx;
            CenterY += dy;
            Clamp();
        }

        public void CenterOn(float worldX, float worldY)
        {
            CenterX = worldX;
            CenterY = worldY;
            Clamp();
        }

        public void ZoomTo(float zoom)
        {
            Zoom = ClampZoom(zoom);
            Clamp();
        }

        /// <summary>Zoom by <paramref name="factor"/> keeping the map point under the screen pivot fixed (pinch / wheel).</summary>
        public void ZoomBy(float factor, float pivotScreenX = 0f, float pivotScreenY = 0f)
        {
            if (factor <= 0f || float.IsNaN(factor))
            {
                return;
            }

            var wx = CenterX + pivotScreenX / Zoom;
            var wy = CenterY + pivotScreenY / Zoom;
            Zoom = ClampZoom(Zoom * factor);
            CenterX = wx - pivotScreenX / Zoom;
            CenterY = wy - pivotScreenY / Zoom;
            Clamp();
        }

        /// <summary>Shift + drag zoom: drag up zooms in.</summary>
        public void ShiftDragZoom(float dy) => ZoomBy((float)Math.Pow(2.0, dy / ShiftDragPerDoubling));

        /// <summary>Keyboard step: axes in -1..1 (WASD / arrows), zoomAxis +1 = in (E / +), -1 = out (Q / -).</summary>
        public void KeyStep(float panX, float panY, float zoomAxis, float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (zoomAxis != 0f)
            {
                ZoomBy((float)Math.Exp(zoomAxis * KeyZoomPerSecond * deltaSeconds));
            }

            if (panX != 0f || panY != 0f)
            {
                // Keys move the camera (the view), so W shows more of the north.
                PanWorld(panX * KeyPanPerSecond * deltaSeconds / Zoom, panY * KeyPanPerSecond * deltaSeconds / Zoom);
            }
        }

        public (float X, float Y) ScreenToWorld(float screenX, float screenY) =>
            (CenterX + screenX / Zoom, CenterY + screenY / Zoom);

        public (float X, float Y) WorldToScreen(float worldX, float worldY) =>
            ((worldX - CenterX) * Zoom, (worldY - CenterY) * Zoom);

        public bool IsVisible(float worldX, float worldY, float margin = 0f) =>
            worldX >= Left - margin && worldX <= Right + margin && worldY >= Bottom - margin && worldY <= Top + margin;

        private void Clamp()
        {
            CenterX = ClampAxis(CenterX, ViewWidth / Zoom, WorldWidth);
            CenterY = ClampAxis(CenterY, ViewHeight / Zoom, WorldHeight);
        }

        private static float ClampAxis(float center, float visible, float world)
        {
            if (float.IsNaN(center))
            {
                return world * 0.5f;
            }

            if (visible >= world)
            {
                return world * 0.5f;
            }

            var half = visible * 0.5f;
            return Math.Min(world - half, Math.Max(half, center));
        }
    }
}
