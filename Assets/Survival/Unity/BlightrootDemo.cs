using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Blightroot Game view 1080×1920. One dropdown of the exact Mixamo names.
    /// Orbit matches the Aldric scene: 1 / 2 / 3, Q/E or arrows, RMB drag.
    /// Yaw 0 is the front Play-cam. HOLD merge until Derek Game-view PASS.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class BlightrootDemo : MonoBehaviour
    {
        public const string DropdownObjectName = "BlightrootClipDropdown";

        private bool _booted;
        private BlightrootActor? _actor;
        private Dropdown? _dropdown;
        private Text? _phase;
        private float _orbitYaw;
        private float _orbitPitch;
        private InputActionMap? _orbitMap;

        private void Awake() => Boot();

        private void OnEnable()
        {
            Boot();
            BindOrbitActions();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _orbitMap?.Disable();
            _orbitMap?.Dispose();
            _orbitMap = null;
        }

        private void Start() => Boot();

        private void Update()
        {
            if (!_booted)
            {
                Boot();
            }

            TickOrbit();
            ApplyPlayCam(_orbitYaw, _orbitPitch);
            if (_phase == null || _actor == null)
            {
                return;
            }

            var name = _actor.ExactName ?? "—";
            _phase.text = name + "   ·   cam " + Mathf.RoundToInt(_orbitYaw) + "   ·   " + _actor.ClipLength.ToString("0.00") + "s   ·   " + BlightrootMotion.TakeName;
        }

        private void LateUpdate()
        {
            if (_booted)
            {
                ApplyPlayCam(_orbitYaw, _orbitPitch);
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            _ = context;
            _ = camera;
            if (_booted)
            {
                ApplyPlayCam(_orbitYaw, _orbitPitch);
            }
        }

        public bool PlayNamedClip(string exactName)
        {
            var actor = _actor != null ? _actor : FindFirstObjectByType<BlightrootActor>();
            if (actor == null)
            {
                return false;
            }

            var played = actor.PlayNamedClip(exactName);
            if (_dropdown != null && BlightrootMotion.TryGet(exactName, out _))
            {
                var clips = BlightrootMotion.Clips;
                for (var i = 0; i < clips.Length; i++)
                {
                    if (string.Equals(clips[i].ExactName, exactName, System.StringComparison.Ordinal))
                    {
                        _dropdown.SetValueWithoutNotify(i);
                        _dropdown.RefreshShownValue();
                        break;
                    }
                }
            }

            return played;
        }

        internal static void ApplyPlayCam() => ApplyPlayCam(0f, 0f);

        internal static void ApplyPlayCam(float orbitYawDegrees, float orbitPitchDegrees)
        {
            if (float.IsNaN(orbitYawDegrees) || float.IsInfinity(orbitYawDegrees))
            {
                orbitYawDegrees = 0f;
            }

            if (float.IsNaN(orbitPitchDegrees) || float.IsInfinity(orbitPitchDegrees))
            {
                orbitPitchDegrees = 0f;
            }

            BlightrootMotion.PlayCamOrbitEye(orbitYawDegrees, orbitPitchDegrees, out var x, out var y, out var z);
            var eye = new Vector3(x, y, z);
            var look = new Vector3(BlightrootMotion.PlayCamLookX, BlightrootMotion.PlayCamLookY, BlightrootMotion.PlayCamLookZ);
            var forward = look - eye;
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.back;
            }

            forward.Normalize();
            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.98f)
            {
                up = Vector3.forward;
            }

            var rot = Quaternion.LookRotation(forward, up);
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
            cam.fieldOfView = BlightrootMotion.PlayCamFovDegrees;
            cam.nearClipPlane = BlightrootMotion.PlayCamNear;
            cam.farClipPlane = BlightrootMotion.PlayCamFar;
            cam.aspect = BlightrootMotion.GameViewWidth / BlightrootMotion.GameViewHeight;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.06f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = eye;
            cam.transform.rotation = rot;
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10f, 10f, 110f, 40f), "1 Front"))
            {
                ApplyOrbitKey(KeyCode.Alpha1);
            }

            if (GUI.Button(new Rect(126f, 10f, 110f, 40f), "2 3/4"))
            {
                ApplyOrbitKey(KeyCode.Alpha2);
            }

            if (GUI.Button(new Rect(242f, 10f, 110f, 40f), "3 Rear"))
            {
                ApplyOrbitKey(KeyCode.Alpha3);
            }

            var ev = Event.current;
            if (ev == null)
            {
                return;
            }

            if (ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
            {
                ApplyOrbitKey(ev.keyCode);
                if (ev.keyCode == KeyCode.Q || ev.keyCode == KeyCode.LeftArrow || ev.keyCode == KeyCode.A)
                {
                    _orbitYaw -= 12f;
                    WrapOrbit();
                }
                else if (ev.keyCode == KeyCode.E || ev.keyCode == KeyCode.RightArrow || ev.keyCode == KeyCode.D)
                {
                    _orbitYaw += 12f;
                    WrapOrbit();
                }
            }

            var overButtons = ev.mousePosition.y <= 54f && ev.mousePosition.x <= 360f;
            if (!overButtons && ev.type == EventType.MouseDrag && ev.button <= 2)
            {
                _orbitYaw += ev.delta.x * 0.35f;
                _orbitPitch -= ev.delta.y * 0.35f;
                WrapOrbit();
            }

            if (ev.type == EventType.ScrollWheel)
            {
                _orbitYaw += ev.delta.y * 10f;
                WrapOrbit();
            }
        }

        private void TickOrbit()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.enabled)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                {
                    ApplyOrbitKey(KeyCode.Alpha1);
                }
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                {
                    ApplyOrbitKey(KeyCode.Alpha2);
                }
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                {
                    ApplyOrbitKey(KeyCode.Alpha3);
                }

                var yaw = 0f;
                if (kb.qKey.isPressed || kb.leftArrowKey.isPressed || kb.aKey.isPressed)
                {
                    yaw -= 1f;
                }

                if (kb.eKey.isPressed || kb.rightArrowKey.isPressed || kb.dKey.isPressed)
                {
                    yaw += 1f;
                }

                _orbitYaw += yaw * SirAldric3DMotion.PlayCamOrbitYawSpeed * Time.unscaledDeltaTime;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                ApplyOrbitKey(KeyCode.Alpha1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                ApplyOrbitKey(KeyCode.Alpha2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                ApplyOrbitKey(KeyCode.Alpha3);
            }

            var legacyYaw = 0f;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                legacyYaw -= 1f;
            }

            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                legacyYaw += 1f;
            }

            _orbitYaw += legacyYaw * SirAldric3DMotion.PlayCamOrbitYawSpeed * Time.unscaledDeltaTime;

            var mouse = Mouse.current;
            if (mouse != null && mouse.enabled && (mouse.rightButton.isPressed || mouse.middleButton.isPressed))
            {
                var d = mouse.delta.ReadValue();
                _orbitYaw += d.x * 0.18f;
                _orbitPitch -= d.y * 0.18f;
            }

            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                _orbitYaw += Input.GetAxis("Mouse X") * 140f;
                _orbitPitch -= Input.GetAxis("Mouse Y") * 80f;
            }

            WrapOrbit();
        }

        private void ApplyOrbitKey(KeyCode key)
        {
            if (key == KeyCode.Alpha1 || key == KeyCode.Keypad1)
            {
                _orbitYaw = 0f;
                _orbitPitch = 0f;
            }
            else if (key == KeyCode.Alpha2 || key == KeyCode.Keypad2)
            {
                _orbitYaw = BlightrootMotion.PlayCamThreeQuarterYawDegrees;
                _orbitPitch = 0f;
            }
            else if (key == KeyCode.Alpha3 || key == KeyCode.Keypad3)
            {
                _orbitYaw = BlightrootMotion.PlayCamOppositeYawDegrees;
                _orbitPitch = 0f;
            }
        }

        private void WrapOrbit()
        {
            if (float.IsNaN(_orbitYaw) || float.IsInfinity(_orbitYaw))
            {
                _orbitYaw = 0f;
            }

            if (float.IsNaN(_orbitPitch) || float.IsInfinity(_orbitPitch))
            {
                _orbitPitch = 0f;
            }

            if (_orbitYaw > 180f)
            {
                _orbitYaw -= 360f;
            }
            else if (_orbitYaw < -180f)
            {
                _orbitYaw += 360f;
            }

            _orbitPitch = Mathf.Clamp(
                _orbitPitch,
                SirAldric3DMotion.PlayCamOrbitPitchMin,
                SirAldric3DMotion.PlayCamOrbitPitchMax);
        }

        private void BindOrbitActions()
        {
            _orbitMap?.Dispose();
            var map = new InputActionMap("BlightrootOrbit");
            var front = map.AddAction("Front", InputActionType.Button);
            front.AddBinding("<Keyboard>/1");
            front.AddBinding("<Keyboard>/numpad1");
            front.performed += _ => ApplyOrbitKey(KeyCode.Alpha1);
            var threeQuarter = map.AddAction("ThreeQuarter", InputActionType.Button);
            threeQuarter.AddBinding("<Keyboard>/2");
            threeQuarter.AddBinding("<Keyboard>/numpad2");
            threeQuarter.performed += _ => ApplyOrbitKey(KeyCode.Alpha2);
            var rear = map.AddAction("Rear", InputActionType.Button);
            rear.AddBinding("<Keyboard>/3");
            rear.AddBinding("<Keyboard>/numpad3");
            rear.performed += _ => ApplyOrbitKey(KeyCode.Alpha3);
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

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            SurvivalVisuals.EnsureEventSystem();
            ApplyPlayCam();
            BuildStage();

            var actorGo = new GameObject("BlightrootActor");
            actorGo.transform.SetParent(transform, false);
            _actor = actorGo.AddComponent<BlightrootActor>();
            _actor.Build();

            var canvas = SurvivalVisuals.Canvas(transform, "BlightrootHud", 80);
            var title = SurvivalVisuals.Text(canvas, "Caption", "BLIGHTROOT", 30, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.06f, 0.952f);
            tr.anchorMax = new Vector2(0.94f, 0.992f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            var dropdown = SurvivalVisuals.Dropdown(canvas, DropdownObjectName, new Color(0.12f, 0.14f, 0.10f, 0.96f));
            var ddRt = dropdown.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0.04f, 0.858f);
            ddRt.anchorMax = new Vector2(0.96f, 0.946f);
            ddRt.offsetMin = Vector2.zero;
            ddRt.offsetMax = Vector2.zero;
            if (dropdown.captionText != null)
            {
                dropdown.captionText.fontSize = 32;
                dropdown.captionText.alignment = TextAnchor.MiddleLeft;
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.fontSize = 28;
            }

            if (dropdown.template != null)
            {
                dropdown.template.sizeDelta = new Vector2(0f, 1120f);
            }

            dropdown.ClearOptions();
            var options = new System.Collections.Generic.List<Dropdown.OptionData>();
            var clips = BlightrootMotion.Clips;
            for (var i = 0; i < clips.Length; i++)
            {
                options.Add(new Dropdown.OptionData(clips[i].ExactName));
            }

            dropdown.AddOptions(options);
            var start = IndexOf("mutant idle");
            dropdown.SetValueWithoutNotify(start);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(OnClipPicked);
            _dropdown = dropdown;

            _phase = SurvivalVisuals.Text(canvas, "Phase", "mutant idle", 22, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            var pr = _phase.rectTransform;
            pr.anchorMin = new Vector2(0.06f, 0.012f);
            pr.anchorMax = new Vector2(0.94f, 0.048f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            _booted = true;
            if (_actor.Built)
            {
                _actor.PlayNamedClip(clips[start].ExactName);
            }
        }

        private void OnClipPicked(int index)
        {
            var clips = BlightrootMotion.Clips;
            if (index < 0 || index >= clips.Length)
            {
                return;
            }

            PlayNamedClip(clips[index].ExactName);
        }

        private static int IndexOf(string exactName)
        {
            var clips = BlightrootMotion.Clips;
            for (var i = 0; i < clips.Length; i++)
            {
                if (string.Equals(clips[i].ExactName, exactName, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        private static void BuildStage()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(16f, 16f, 1f);
            ground.transform.position = new Vector3(0f, BlightrootMotion.GroundY, 0.4f);
            Object.Destroy(ground.GetComponent<Collider>());
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                var mat = new Material(shader);
                var color = new Color(0.11f, 0.13f, 0.09f, 1f);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                mat.color = color;
                renderer.sharedMaterial = mat;
            }
        }
    }
}
