using Survival.Domain.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Survival.Unity
{
    /// <summary>
    /// Portrait 1080×1920 free world-view camera for the locked battle demo. Same lens as
    /// PortraitStage (54° FOV, near 0.25, far 4000, horizon clear colour). Starts on the
    /// three-quarter framing that fits the whole battlefield box, then the mouse drives it:
    /// left- or right-drag orbits around the battle centre, the scroll wheel zooms, middle-drag
    /// pans across the ground. Pitch stays above the ground and the zoom is clamped.
    /// Laptop controls (no middle button, Derek 2026-10-18): Shift + left-drag pans like middle-drag; WASD or the arrow keys
    /// slide the view across the ground relative to where the camera faces (W/Up away from the camera, D/Right to its right);
    /// E, + or keypad + zooms in and Q, - or keypad - zooms out. Pans stay within <see cref="PanMarginMetres"/> of the field.
    /// Input: the project runs both backends (activeInputHandler 2); the Input System mouse is
    /// read first and the legacy Input Manager only when the Input System reports nothing.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class LockedBattleCamera : MonoBehaviour
    {
        /// <summary>Default framing: the old three-quarter view.</summary>
        public const float DefaultYawDegrees = 135f;
        public const float DefaultPitchDegrees = 30f;

        public const float MinPitchDegrees = 5f;
        public const float MaxPitchDegrees = 85f;
        public const float MinDistanceMetres = 2.5f;

        /// <summary>Farthest zoom as a multiple of the default fit distance.</summary>
        public const float MaxDistanceFactor = 2.5f;

        /// <summary>Lowest the eye may go above the ground.</summary>
        public const float MinEyeHeightMetres = 0.3f;

        /// <summary>How far the pan may take the orbit centre beyond the battlefield box.</summary>
        public const float PanMarginMetres = 3f;

        /// <summary>Degrees of orbit per screen pixel dragged.</summary>
        public const float OrbitDegreesPerPixel = 0.25f;

        /// <summary>Zoom factor per scroll notch.</summary>
        public const float ZoomPerNotch = 0.9f;

        /// <summary>Keyboard pan speed as a fraction of the zoom distance per second, clamped to the range below.</summary>
        public const float KeyPanDistancePerSecond = 0.6f;
        public const float KeyPanMinMetresPerSecond = 2f;
        public const float KeyPanMaxMetresPerSecond = 12f;

        /// <summary>Keyboard zoom in scroll notches per second.</summary>
        public const float KeyZoomNotchesPerSecond = 5f;

        /// <summary>How long one hit shake lasts.</summary>
        public const float ShakeSeconds = 0.18f;

        private Bounds _field = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(6f, 2f, 8f));
        private Vector3 _pivot;
        private float _yaw = DefaultYawDegrees;
        private float _pitch = DefaultPitchDegrees;
        private float _distance = 10f;
        private float _maxDistance = 25f;
        private bool _orbiting;
        private bool _panning;
        private bool _shiftPanning;
        private Vector3 _eye;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _shakeOffset;
        private float _shakeMetres;
        private float _shakeAge = 1f;

        public void SetField(Bounds field)
        {
            _field = field;
            ResetView();
        }

        /// <summary>Back to the default three-quarter framing of the whole field.</summary>
        public void ResetView()
        {
            _yaw = DefaultYawDegrees;
            _pitch = DefaultPitchDegrees;
            _pivot = _field.center;
            _distance = FitDistance(_yaw, _pitch);
            _maxDistance = Mathf.Max(MinDistanceMetres * 2f, _distance * MaxDistanceFactor);
            Solve();
        }

        private void Update()
        {
            ReadPointer(out var orbitDrag, out var panDrag, out var notches);
            if (orbitDrag.sqrMagnitude > 0f)
            {
                _yaw += orbitDrag.x * OrbitDegreesPerPixel;
                _pitch -= orbitDrag.y * OrbitDegreesPerPixel;
            }

            if (panDrag.sqrMagnitude > 0f)
            {
                Pan(panDrag);
            }

            ReadKeys(out var keyMove, out var keyZoom);
            if (keyMove.sqrMagnitude > 0f)
            {
                var speed = Mathf.Clamp(_distance * KeyPanDistancePerSecond, KeyPanMinMetresPerSecond, KeyPanMaxMetresPerSecond);
                Slide(keyMove.normalized * (speed * Time.unscaledDeltaTime));
            }

            notches += keyZoom * KeyZoomNotchesPerSecond * Time.unscaledDeltaTime;
            if (Mathf.Abs(notches) > 1e-4f)
            {
                _distance *= Mathf.Pow(ZoomPerNotch, notches);
            }

            Solve();
        }

        /// <summary>
        /// Mouse this frame: drag in pixels for orbit (left or right button) and pan (middle),
        /// and scroll in notches (positive zooms in). A drag that starts over the HUD is ignored.
        /// </summary>
        private void ReadPointer(out Vector2 orbitDrag, out Vector2 panDrag, out float notches)
        {
            orbitDrag = Vector2.zero;
            panDrag = Vector2.zero;
            notches = 0f;
            var heard = false;
            var mouse = Mouse.current;
            if (mouse != null && mouse.enabled)
            {
                var keyboard = Keyboard.current;
                var shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
                var leftHeld = mouse.leftButton.isPressed;
                var rightHeld = mouse.rightButton.isPressed;
                var middleHeld = mouse.middleButton.isPressed;
                var pressed = mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                              || mouse.middleButton.wasPressedThisFrame;
                Track(leftHeld, rightHeld, middleHeld, shift, pressed);
                var delta = mouse.delta.ReadValue();
                var wheel = mouse.scroll.ReadValue().y;
                heard = leftHeld || rightHeld || middleHeld || Mathf.Abs(wheel) > 1e-4f;
                if (_orbiting)
                {
                    orbitDrag = delta;
                }
                else if (_panning)
                {
                    panDrag = delta;
                }

                if (!OverHud())
                {
                    // Input System scroll is about 120 per wheel notch on Windows, 1 on macOS.
                    notches = Mathf.Abs(wheel) >= 20f ? wheel / 120f : wheel;
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!heard)
            {
                var legacyShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var pressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                Track(Input.GetMouseButton(0), Input.GetMouseButton(1), Input.GetMouseButton(2), legacyShift, pressed);
                var pixels = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 20f;
                if (_orbiting)
                {
                    orbitDrag = pixels;
                }
                else if (_panning)
                {
                    panDrag = pixels;
                }

                if (!OverHud())
                {
                    notches = Input.mouseScrollDelta.y;
                }
            }
#else
            _ = heard;
#endif
        }

        /// <summary>
        /// Starts a drag on a fresh press away from the HUD; ends it on release. Left or right orbits, middle pans, and a
        /// left press with Shift held pans (laptops without a middle button); that pan lasts while left stays down.
        /// </summary>
        private void Track(bool leftHeld, bool rightHeld, bool middleHeld, bool shift, bool pressed)
        {
            if (pressed && !OverHud())
            {
                _shiftPanning = shift && leftHeld && !rightHeld;
                _orbiting = !_shiftPanning && (leftHeld || rightHeld);
                _panning = _shiftPanning || (!_orbiting && middleHeld);
            }

            if (!(leftHeld || rightHeld))
            {
                _orbiting = false;
            }

            if (!(middleHeld || (_shiftPanning && leftHeld)))
            {
                _panning = false;
                _shiftPanning = false;
            }
        }

        /// <summary>
        /// Keyboard this frame: ground move (x right, y away from the camera, -1..1) from WASD or the arrow keys, and zoom
        /// (+1 in, -1 out) from E/+ and Q/-. Ignored while a HUD control has keyboard focus.
        /// </summary>
        private static void ReadKeys(out Vector2 move, out float zoom)
        {
            move = Vector2.zero;
            zoom = 0f;
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null
                && events.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() != null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.enabled)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    move.y += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    move.y -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    move.x += 1f;
                }

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    move.x -= 1f;
                }

                if (keyboard.eKey.isPressed || keyboard.equalsKey.isPressed || keyboard.numpadPlusKey.isPressed)
                {
                    zoom += 1f;
                }

                if (keyboard.qKey.isPressed || keyboard.minusKey.isPressed || keyboard.numpadMinusKey.isPressed)
                {
                    zoom -= 1f;
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (move == Vector2.zero && Mathf.Abs(zoom) < 1e-4f)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                {
                    move.y += 1f;
                }

                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                {
                    move.y -= 1f;
                }

                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                {
                    move.x += 1f;
                }

                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                {
                    move.x -= 1f;
                }

                if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
                {
                    zoom += 1f;
                }

                if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
                {
                    zoom -= 1f;
                }
            }
#endif
        }

        private static bool OverHud()
        {
            var events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

        /// <summary>Slides the orbit centre across the ground plane, a screen pixel to a world pixel at the centre's depth.</summary>
        private void Pan(Vector2 pixels)
        {
            var screenHeight = Mathf.Max(Screen.height, 1);
            var metresPerPixel = 2f * _distance * Mathf.Tan(PortraitGameView.FovDegrees * 0.5f * Mathf.Deg2Rad) / screenHeight;
            Slide(-pixels * metresPerPixel);
        }

        /// <summary>Moves the orbit centre on the ground: x metres to the camera's right, y metres away from the camera (Solve clamps it to the field margin).</summary>
        private void Slide(Vector2 metres)
        {
            var flatRight = _rotation * Vector3.right;
            flatRight.y = 0f;
            var flatForward = _rotation * Vector3.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 1e-6f)
            {
                flatForward = _rotation * Vector3.up;
                flatForward.y = 0f;
            }

            _pivot += (flatRight.normalized * metres.x) + (flatForward.normalized * metres.y);
        }

        /// <summary>Small screen-plane shake for a big hit. Overlapping hits keep the stronger one.</summary>
        public void Shake(float metres)
        {
            if (metres <= 0f)
            {
                return;
            }

            var left = _shakeAge < ShakeSeconds ? _shakeMetres * (1f - (_shakeAge / ShakeSeconds)) : 0f;
            _shakeMetres = Mathf.Max(metres, left);
            _shakeAge = 0f;
        }

        private void LateUpdate()
        {
            _shakeAge += Time.unscaledDeltaTime;
            if (_shakeAge < ShakeSeconds && _shakeMetres > 0f)
            {
                var k = 1f - (_shakeAge / ShakeSeconds);
                var t = Time.unscaledTime * 55f;
                var x = (Mathf.PerlinNoise(t, 0.37f) * 2f) - 1f;
                var y = (Mathf.PerlinNoise(0.71f, t) * 2f) - 1f;
                _shakeOffset = ((_rotation * Vector3.right * x) + (_rotation * Vector3.up * y)) * (_shakeMetres * k * k);
            }
            else
            {
                _shakeOffset = Vector3.zero;
            }

            Apply();
        }

        private static Vector3 ToEye(float yaw, float pitch)
        {
            var yr = yaw * Mathf.Deg2Rad;
            var pr = pitch * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(pr) * Mathf.Sin(yr), Mathf.Sin(pr), Mathf.Cos(pr) * Mathf.Cos(yr));
        }

        /// <summary>Distance from the field centre that fits the whole battlefield box in the portrait frame.</summary>
        private float FitDistance(float yaw, float pitch)
        {
            var forward = -ToEye(yaw, pitch);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var up = Vector3.Cross(forward, right).normalized;
            var tanV = Mathf.Tan(PortraitGameView.FovDegrees * 0.5f * Mathf.Deg2Rad);
            var tanH = tanV * (PortraitGameView.Width / PortraitGameView.Height);
            var look = _field.center;
            var need = 2f;
            var min = _field.min;
            var max = _field.max;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                var rel = corner - look;
                var r = Mathf.Abs(Vector3.Dot(rel, right));
                var u = Mathf.Abs(Vector3.Dot(rel, up));
                var w = Vector3.Dot(rel, forward);
                need = Mathf.Max(need, w + (r / tanH));
                need = Mathf.Max(need, w + (u / tanV));
            }

            return need * 1.06f;
        }

        /// <summary>Clamps the orbit, then places the eye. Never below the ground, zoom kept in range.</summary>
        private void Solve()
        {
            if (float.IsNaN(_yaw) || float.IsInfinity(_yaw))
            {
                _yaw = DefaultYawDegrees;
            }

            if (float.IsNaN(_pitch) || float.IsInfinity(_pitch))
            {
                _pitch = DefaultPitchDegrees;
            }

            _yaw = Mathf.Repeat(_yaw, 360f);
            _pitch = Mathf.Clamp(_pitch, MinPitchDegrees, MaxPitchDegrees);
            _distance = Mathf.Clamp(float.IsNaN(_distance) ? _maxDistance * 0.5f : _distance, MinDistanceMetres, _maxDistance);
            var lo = _field.min - new Vector3(PanMarginMetres, 0f, PanMarginMetres);
            var hi = _field.max + new Vector3(PanMarginMetres, 0f, PanMarginMetres);
            _pivot = new Vector3(Mathf.Clamp(_pivot.x, lo.x, hi.x), _field.center.y, Mathf.Clamp(_pivot.z, lo.z, hi.z));
            var toEye = ToEye(_yaw, _pitch);
            _eye = _pivot + (toEye * _distance);
            if (_eye.y < MinEyeHeightMetres)
            {
                _eye.y = MinEyeHeightMetres;
            }

            _rotation = Quaternion.LookRotation(_pivot - _eye, Vector3.up);
            Apply();
        }

        private void Apply()
        {
            var n = Camera.allCamerasCount;
            if (n <= 0)
            {
                Place(Camera.main);
                return;
            }

            var cams = new Camera[n];
            Camera.GetAllCameras(cams);
            for (var i = 0; i < n; i++)
            {
                Place(cams[i]);
            }
        }

        private void Place(Camera? cam)
        {
            if (cam == null)
            {
                return;
            }

            cam.orthographic = false;
            cam.usePhysicalProperties = false;
            cam.fieldOfView = PortraitGameView.FovDegrees;
            cam.nearClipPlane = PortraitGameView.Near;
            cam.farClipPlane = PortraitGameView.Far;
            cam.aspect = PortraitGameView.Width / PortraitGameView.Height;
            cam.backgroundColor = LockedBattleGround.Horizon;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.SetPositionAndRotation(_eye + _shakeOffset, _rotation);
        }
    }
}
