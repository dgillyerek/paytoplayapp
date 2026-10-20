#nullable enable
using System;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Scroll / zoom rig for a map viewport (World and Village views). Sits on the full-screen viewport Image
    /// behind the HUD and drives <see cref="MapCamera"/>; the content RectTransform (pivot 0,0 on the viewport
    /// center) is scaled by zoom and offset by -center * zoom.
    /// Phone: drag to pan (with a short fling), two-finger pinch to zoom.
    /// Laptop: drag / right-drag to pan, wheel to zoom at the pointer, Shift + drag to zoom,
    /// WASD / arrows to pan, Q / E or - / + to zoom. Works with the Input System and the legacy Input Manager.
    /// Drags cancel the click on whatever button they started on, so panning never taps a building.
    /// </summary>
    public sealed class SurvivalMapRig : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        private const float FlingDecay = 6f;
        private const float FlingMin = 12f;

        private RectTransform? _viewport;
        private RectTransform? _content;
        private Canvas? _canvas;
        private Vector2 _velocity;
        private bool _dragging;
        private float _pinchDistance;
        private Vector2 _pinchMid;
        private bool _pinching;
        private Vector2? _focusTarget;
        private float _focusZoom;
        private float _lastZoom = -1f;

        public MapCamera? Camera { get; private set; }

        /// <summary>Only the visible view listens to keys and pinches.</summary>
        public bool InputEnabled { get; set; } = true;

        /// <summary>Raised after the camera moved or zoomed (labels re-scale on zoom).</summary>
        public event Action<MapCamera>? Moved;

        /// <summary>A tap on empty map (no drag): closes info cards.</summary>
        public event Action? EmptyTap;

        public void Init(RectTransform viewport, RectTransform content, MapCamera camera)
        {
            _viewport = viewport;
            _content = content;
            Camera = camera;
            _canvas = viewport.GetComponentInParent<Canvas>();
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = Vector2.zero;
            content.sizeDelta = new Vector2(camera.WorldWidth, camera.WorldHeight);
            SyncViewport();
            Apply();
        }

        /// <summary>Glide the camera to a map point (and optionally a zoom) over a few frames.</summary>
        public void FocusOn(float worldX, float worldY, float zoom = -1f)
        {
            _focusTarget = new Vector2(worldX, worldY);
            _focusZoom = zoom;
            _velocity = Vector2.zero;
        }

        public void JumpTo(float worldX, float worldY)
        {
            _focusTarget = null;
            Camera?.CenterOn(worldX, worldY);
            Apply();
        }

        // ---------- pointer ----------

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _velocity = Vector2.zero;
            _focusTarget = null;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Camera == null || _pinching || TouchCount() >= 2)
            {
                return;
            }

            var d = eventData.delta / Scale;
            if (ShiftHeld())
            {
                Camera.ShiftDragZoom(d.y);
                _velocity = Vector2.zero;
            }
            else
            {
                Camera.PanScreen(d.x, d.y);
                var dt = Mathf.Max(Time.unscaledDeltaTime, 1f / 240f);
                _velocity = Vector2.Lerp(_velocity, d / dt, 0.5f);
            }

            Apply();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            if (ShiftHeld() || _velocity.magnitude < FlingMin * 10f)
            {
                _velocity = Vector2.zero;
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (Camera == null || !InputEnabled)
            {
                return;
            }

            var y = eventData.scrollDelta.y;
            if (Mathf.Approximately(y, 0f))
            {
                return;
            }

            // Legacy reports notches (+-1); some Input System versions report 120 per notch.
            var notches = Mathf.Abs(y) > 10f ? y / 120f : y;
            var pivot = LocalFromScreen(eventData.position);
            Camera.ZoomBy(Mathf.Pow(MapCamera.WheelStep, Mathf.Clamp(notches, -4f, 4f)), pivot.x, pivot.y);
            _focusTarget = null;
            Apply();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
            {
                EmptyTap?.Invoke();
            }
        }

        // ---------- per frame ----------

        private void Update()
        {
            if (Camera == null || _content == null)
            {
                return;
            }

            SyncViewport();
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (InputEnabled)
            {
                Pinch();
                Keys(dt);
            }

            if (_focusTarget is Vector2 target)
            {
                var k = 1f - Mathf.Exp(-10f * dt);
                var cx = Mathf.Lerp(Camera.CenterX, target.x, k);
                var cy = Mathf.Lerp(Camera.CenterY, target.y, k);
                if (_focusZoom > 0f)
                {
                    Camera.ZoomTo(Mathf.Lerp(Camera.Zoom, _focusZoom, k));
                }

                Camera.CenterOn(cx, cy);
                if (Mathf.Abs(cx - target.x) < 1f && Mathf.Abs(cy - target.y) < 1f)
                {
                    _focusTarget = null;
                }
            }
            else if (!_dragging && _velocity.sqrMagnitude > FlingMin * FlingMin)
            {
                Camera.PanScreen(_velocity.x * dt, _velocity.y * dt);
                _velocity *= Mathf.Exp(-FlingDecay * dt);
            }

            Apply();
        }

        private void Keys(float dt)
        {
            var x = 0f;
            var y = 0f;
            var z = 0f;
            var kb = Keyboard.current;
            if (kb != null && kb.enabled)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.eKey.isPressed || kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) z += 1f;
                if (kb.qKey.isPressed || kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) z -= 1f;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (x == 0f && y == 0f && z == 0f)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
                if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) z += 1f;
                if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) z -= 1f;
            }
#endif
            x = Mathf.Clamp(x, -1f, 1f);
            y = Mathf.Clamp(y, -1f, 1f);
            z = Mathf.Clamp(z, -1f, 1f);
            if (x != 0f || y != 0f || z != 0f)
            {
                _focusTarget = null;
                _velocity = Vector2.zero;
                Camera!.KeyStep(x, y, z, dt);
            }
        }

        private void Pinch()
        {
            if (!TryTwoTouches(out var a, out var b))
            {
                _pinching = false;
                return;
            }

            var dist = Vector2.Distance(a, b);
            var mid = (a + b) * 0.5f;
            if (!_pinching)
            {
                _pinching = true;
                _pinchDistance = dist;
                _pinchMid = mid;
                _velocity = Vector2.zero;
                _focusTarget = null;
                return;
            }

            if (_pinchDistance > 1f && dist > 1f)
            {
                var pivot = LocalFromScreen(mid);
                Camera!.ZoomBy(dist / _pinchDistance, pivot.x, pivot.y);
                var pan = (mid - _pinchMid) / Scale;
                Camera.PanScreen(pan.x, pan.y);
            }

            _pinchDistance = dist;
            _pinchMid = mid;
        }

        private static int TouchCount()
        {
            var ts = Touchscreen.current;
            if (ts != null)
            {
                var n = 0;
                foreach (var t in ts.touches)
                {
                    if (t.press.isPressed)
                    {
                        n++;
                    }
                }

                if (n > 0)
                {
                    return n;
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.touchCount;
#else
            return 0;
#endif
        }

        private static bool TryTwoTouches(out Vector2 a, out Vector2 b)
        {
            a = b = Vector2.zero;
            var ts = Touchscreen.current;
            if (ts != null)
            {
                var found = 0;
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed)
                    {
                        continue;
                    }

                    if (found == 0)
                    {
                        a = t.position.ReadValue();
                    }
                    else
                    {
                        b = t.position.ReadValue();
                    }

                    if (++found == 2)
                    {
                        return true;
                    }
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount >= 2)
            {
                a = Input.GetTouch(0).position;
                b = Input.GetTouch(1).position;
                return true;
            }
#endif
            return false;
        }

        private static bool ShiftHeld()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.enabled && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
            {
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }

        // ---------- helpers ----------

        private float Scale => _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;

        /// <summary>Screen point -> canvas units relative to the viewport center (y up).</summary>
        private Vector2 LocalFromScreen(Vector2 screen)
        {
            if (_viewport == null)
            {
                return Vector2.zero;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, screen, null, out var local);
            var r = _viewport.rect;
            return local - r.center;
        }

        private void SyncViewport()
        {
            if (_viewport == null || Camera == null)
            {
                return;
            }

            var r = _viewport.rect;
            if (r.width > 1f && r.height > 1f && (!Mathf.Approximately(r.width, Camera.ViewWidth) || !Mathf.Approximately(r.height, Camera.ViewHeight)))
            {
                Camera.SetViewport(r.width, r.height);
            }
        }

        private void Apply()
        {
            if (_content == null || Camera == null)
            {
                return;
            }

            _content.localScale = new Vector3(Camera.Zoom, Camera.Zoom, 1f);
            _content.anchoredPosition = new Vector2(Camera.ContentX, Camera.ContentY);
            if (!Mathf.Approximately(_lastZoom, Camera.Zoom) || _dragging || _focusTarget != null || _velocity.sqrMagnitude > 0f)
            {
                _lastZoom = Camera.Zoom;
                Moved?.Invoke(Camera);
            }
        }

        /// <summary>Full-screen transparent raycast target for a viewport (catches drags on empty map).</summary>
        public static Image ViewportGraphic(Transform parent, string name)
        {
            var img = SurvivalVisuals.Image(parent, name, new Color(1f, 1f, 1f, 0f));
            img.raycastTarget = true;
            SurvivalVisuals.Stretch(img.rectTransform);
            return img;
        }
    }
}
