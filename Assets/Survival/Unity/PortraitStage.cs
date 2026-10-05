using Survival.Domain.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Survival.Unity
{
    /// <summary>
    /// Portrait 1080×1920 orbit shared by Ironhowl and Emberfang.
    /// Frames whatever bounds the skinned body reports. Does not scale the body.
    /// </summary>
    public sealed class PortraitStage : MonoBehaviour
    {
        private float _yaw;
        private float _pitch;
        private float _distance = 8f;
        private Vector3 _look = new Vector3(0f, 1f, 0f);
        private InputActionMap? _orbitMap;

        public void SetBounds(Bounds bounds)
        {
            _look = bounds.center;
            _distance = PortraitGameView.FrameDistance(bounds.size.x, bounds.size.y, bounds.size.z);
            Apply();
        }

        private void OnEnable()
        {
            BindOrbit();
            RenderPipelineHook.Install(Apply);
        }

        private void OnDisable()
        {
            RenderPipelineHook.Remove(Apply);
            _orbitMap?.Disable();
            _orbitMap?.Dispose();
            _orbitMap = null;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                {
                    _yaw = 0f;
                    _pitch = 0f;
                }
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                {
                    _yaw = PortraitGameView.ThreeQuarterYawDegrees;
                    _pitch = 0f;
                }
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                {
                    _yaw = PortraitGameView.RearYawDegrees;
                    _pitch = 0f;
                }
            }

            var yaw = _orbitMap?.FindAction("Yaw");
            if (yaw != null)
            {
                _yaw += yaw.ReadValue<float>() * 90f * Time.unscaledDeltaTime;
            }

            Wrap();
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            PortraitGameView.OrbitEye(_look.x, _look.y, _look.z, _distance, _yaw, _pitch, out var x, out var y, out var z);
            var eye = new Vector3(x, y, z);
            var forward = _look - eye;
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.back;
            }

            var rot = Quaternion.LookRotation(forward.normalized, Vector3.up);
            var n = Camera.allCamerasCount;
            if (n <= 0)
            {
                Place(Camera.main, eye, rot);
                return;
            }

            var cams = new Camera[n];
            Camera.GetAllCameras(cams);
            for (var i = 0; i < n; i++)
            {
                Place(cams[i], eye, rot);
            }
        }

        private static void Place(Camera? cam, Vector3 eye, Quaternion rot)
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
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.06f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = eye;
            cam.transform.rotation = rot;
        }

        public void BuildGround(float groundY, float span)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var size = Mathf.Max(8f, span);
            ground.transform.localScale = new Vector3(size, size, 1f);
            ground.transform.position = new Vector3(0f, groundY, 0f);
            Object.Destroy(ground.GetComponent<Collider>());
            var renderer = ground.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            var color = new Color(0.11f, 0.13f, 0.09f, 1f);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10f, 10f, 110f, 40f), "1 Front"))
            {
                _yaw = 0f;
                _pitch = 0f;
            }

            if (GUI.Button(new Rect(126f, 10f, 110f, 40f), "2 3/4"))
            {
                _yaw = PortraitGameView.ThreeQuarterYawDegrees;
                _pitch = 0f;
            }

            if (GUI.Button(new Rect(242f, 10f, 110f, 40f), "3 Rear"))
            {
                _yaw = PortraitGameView.RearYawDegrees;
                _pitch = 0f;
            }

            var ev = Event.current;
            if (ev == null)
            {
                return;
            }

            if (ev.type == EventType.KeyDown)
            {
                if (ev.keyCode == KeyCode.Alpha1 || ev.keyCode == KeyCode.Keypad1)
                {
                    _yaw = 0f;
                    _pitch = 0f;
                }
                else if (ev.keyCode == KeyCode.Alpha2 || ev.keyCode == KeyCode.Keypad2)
                {
                    _yaw = PortraitGameView.ThreeQuarterYawDegrees;
                    _pitch = 0f;
                }
                else if (ev.keyCode == KeyCode.Alpha3 || ev.keyCode == KeyCode.Keypad3)
                {
                    _yaw = PortraitGameView.RearYawDegrees;
                    _pitch = 0f;
                }
            }

            var overButtons = ev.mousePosition.y <= 54f && ev.mousePosition.x <= 360f;
            if (!overButtons && ev.type == EventType.MouseDrag && ev.button <= 2)
            {
                _yaw += ev.delta.x * 0.35f;
                _pitch -= ev.delta.y * 0.35f;
            }

            Wrap();
        }

        private void Wrap()
        {
            if (_yaw > 180f)
            {
                _yaw -= 360f;
            }
            else if (_yaw < -180f)
            {
                _yaw += 360f;
            }

            _pitch = Mathf.Clamp(_pitch, PortraitGameView.PitchMin, PortraitGameView.PitchMax);
        }

        private void BindOrbit()
        {
            _orbitMap?.Dispose();
            var map = new InputActionMap("PortraitOrbit");
            var yaw = map.AddAction("Yaw", InputActionType.Value);
            yaw.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q")
                .With("Positive", "<Keyboard>/e");
            yaw.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            yaw.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            map.Enable();
            _orbitMap = map;
        }
    }

    /// <summary>Applies the portrait camera at the start of each render.</summary>
    internal static class RenderPipelineHook
    {
        private static System.Action? _apply;

        public static void Install(System.Action apply)
        {
            _apply = apply;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBegin;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBegin;
        }

        public static void Remove(System.Action apply)
        {
            if (_apply == apply)
            {
                _apply = null;
            }

            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBegin;
        }

        private static void OnBegin(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            _ = context;
            _ = camera;
            _apply?.Invoke();
        }
    }
}
