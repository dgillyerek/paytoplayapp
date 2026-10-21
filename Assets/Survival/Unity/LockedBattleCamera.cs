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
    /// read, and the legacy Input Manager only when the Input System has no mouse or keyboard at all
    /// (reading both every idle frame read the same scroll and motion twice).
    /// Steady view (Derek 2026-10-18, "the screen is jumping around"): the camera never follows a
    /// fighter. Input moves a target orbit and the view eases to it (<see cref="SmoothPerSecond"/>), so a
    /// laptop touchpad's burst of scroll, a hitch frame or a fresh click glides instead of jumping. Scroll is
    /// read as notches per device (Windows 120 a notch, touchpads a few units an event) and capped per frame,
    /// the press frame of a drag is skipped, and the frame step is capped. Hit shake is scaled down to a
    /// barely-there tremor (<see cref="ShakeScale"/>, at most <see cref="MaxShakeMetres"/>, <see cref="ShakeSeconds"/>).
    /// With no input the view holds still. Leftover demo cameras from other open scenes are switched off.
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
        public const float ShakeSeconds = 0.12f;

        /// <summary>Share of an attack's shake the camera plays; all twelve fighters hit, so it stays a tremor.</summary>
        public const float ShakeScale = 0.25f;

        /// <summary>Largest shake offset in metres, whatever the attack asks for.</summary>
        public const float MaxShakeMetres = 0.012f;

        /// <summary>Shake noise rate (Hz). Slow enough to read as a nudge, not a buzz.</summary>
        public const float ShakeHz = 18f;

        /// <summary>How fast the view eases to the target orbit (per second; about 95% in 0.2 s).</summary>
        public const float SmoothPerSecond = 15f;

        /// <summary>Longest frame step the camera integrates (a hitch does not throw the view).</summary>
        public const float MaxStepSeconds = 0.05f;

        /// <summary>Largest drag a single frame may apply (pixels), so a lost-focus or warp delta cannot spin the view.</summary>
        public const float MaxDragPixelsPerFrame = 120f;

        /// <summary>Most zoom notches a single frame may apply.</summary>
        public const float MaxNotchesPerFrame = 2f;

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
        private Vector3 _viewPivot;
        private float _viewYaw = DefaultYawDegrees;
        private float _viewPitch = DefaultPitchDegrees;
        private float _viewDistance = 10f;
        private bool _viewSet;
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
            _viewSet = false;
            Solve(0f);
        }

        private void OnEnable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            SilenceOtherCameraDrivers();
        }

        private void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        }

        /// <summary>Last word before each camera renders, so no other script's placement shows for a frame.</summary>
        private void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera cam)
        {
            _ = context;
            Place(cam);
        }

        /// <summary>
        /// Demo stages from other scenes left open in the Editor place the same cameras every frame and
        /// before each render; two drivers on one camera make the view jump between them.
        /// </summary>
        private static void SilenceOtherCameraDrivers()
        {
            foreach (var stage in Object.FindObjectsByType<PortraitStage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (stage != null && stage.enabled)
                {
                    Debug.LogWarning("Locked battle camera: switching off leftover PortraitStage on " + stage.name + ".");
                    stage.enabled = false;
                }
            }
        }

        private void Update()
        {
            var dt = Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
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
                Slide(keyMove.normalized * (speed * dt));
            }

            notches = Mathf.Clamp(notches, -MaxNotchesPerFrame, MaxNotchesPerFrame);
            notches += keyZoom * KeyZoomNotchesPerSecond * dt;
            if (Mathf.Abs(notches) > 1e-4f)
            {
                _distance *= Mathf.Pow(ZoomPerNotch, notches);
            }

            Solve(dt);
        }

        /// <summary>
        /// Scroll in notches. Windows wheels report 120 a notch; laptop touchpads report a few units per
        /// event, many events a second (those were read as whole notches and leapt the zoom); macOS and
        /// normalised wheels report about 1.
        /// </summary>
        public static float ScrollNotches(float wheel)
        {
            var a = Mathf.Abs(wheel);
            if (a >= 60f)
            {
                return wheel / 120f;
            }

            if (a > 1.5f)
            {
                return wheel / 40f;
            }

            return wheel;
        }

        private static Vector2 ClampDrag(Vector2 pixels)
        {
            return Vector2.ClampMagnitude(pixels, MaxDragPixelsPerFrame);
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
                // The press frame's motion happened before the drag began; using it snapped the orbit.
                var delta = pressed ? Vector2.zero : ClampDrag(mouse.delta.ReadValue());
                var wheel = mouse.scroll.ReadValue().y;
                heard = true;
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
                    notches = ScrollNotches(wheel);
                }
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!heard)
            {
                var legacyShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var pressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                Track(Input.GetMouseButton(0), Input.GetMouseButton(1), Input.GetMouseButton(2), legacyShift, pressed);
                var pixels = pressed ? Vector2.zero : ClampDrag(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 20f);
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
                    notches = ScrollNotches(Input.mouseScrollDelta.y);
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
            if (keyboard == null || !keyboard.enabled)
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
            _shakeMetres = Mathf.Min(Mathf.Max(metres * ShakeScale, left), MaxShakeMetres);
            _shakeAge = 0f;
        }

        private void LateUpdate()
        {
            _shakeAge += Time.unscaledDeltaTime;
            if (_shakeAge < ShakeSeconds && _shakeMetres > 0f)
            {
                var k = 1f - (_shakeAge / ShakeSeconds);
                var t = Time.unscaledTime * ShakeHz;
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

        /// <summary>
        /// Clamps the target orbit, eases the view toward it over <paramref name="dt"/> seconds (0 snaps),
        /// then places the eye. Never below the ground, zoom kept in range.
        /// </summary>
        private void Solve(float dt)
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
            if (!_viewSet || dt <= 0f)
            {
                _viewYaw = _yaw;
                _viewPitch = _pitch;
                _viewDistance = _distance;
                _viewPivot = _pivot;
                _viewSet = true;
            }
            else
            {
                var k = 1f - Mathf.Exp(-SmoothPerSecond * dt);
                _viewYaw = Mathf.Repeat(Mathf.LerpAngle(_viewYaw, _yaw, k), 360f);
                _viewPitch = Mathf.Lerp(_viewPitch, _pitch, k);
                _viewDistance = Mathf.Lerp(_viewDistance, _distance, k);
                _viewPivot = Vector3.Lerp(_viewPivot, _pivot, k);
            }

            var toEye = ToEye(_viewYaw, _viewPitch);
            _eye = _viewPivot + (toEye * _viewDistance);
            if (_eye.y < MinEyeHeightMetres)
            {
                _eye.y = MinEyeHeightMetres;
            }

            _rotation = Quaternion.LookRotation(_viewPivot - _eye, Vector3.up);
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
            // The loading screen's floor camera (it only clears the screen) is not a battle view.
            if (cam == null || BattleLoadingScreen.IsLoadingCamera(cam))
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
