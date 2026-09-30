using System.IO;
using Survival.Domain.Flavor;
using Survival.Domain.Heroes;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// 1080×1920 Game-view demo: PILOT Mixamo holefixed + Walk + Slash.
    /// Path A / ClipSword HOLD. RH sword prop. Design / Derek PASS not claimed.
    /// Play-cam defaults to the rear SoT; 1/2/3 and Q/E or RMB orbit around the knight.
    /// Input System + IMGUI (Keyboard.current is deaf until Game view owns focus).
    /// Walk RH grip; Design-owned DIAG Inward Slash (YouTube iQ1s3nN1330 SoT). HOLD.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class SirAldricDemo : MonoBehaviour
    {
        public const string DropFileName = "SIR_ALDRIC_REAR_MASTER_LOCKED.png";
        public const string DropFileName512 = "SIR_ALDRIC_REAR_MASTER_LOCKED_512.png";

        private bool _booted;
        private SirAldricMeshyAnimateActor? _actor;
        private Text? _phase;
        private float _orbitYaw;
        private float _orbitPitch;
        private InputActionMap? _orbitMap;
        public static SirAldricDemo? Live;

        private void Awake() => Boot();

        private void OnEnable()
        {
            Live = this;
            Boot();
            BindOrbitActions();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _orbitMap?.Disable();
            if (Live == this)
            {
                Live = null;
            }
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
            if (_actor == null || !_actor.Built)
            {
                return;
            }

            var t = Time.unscaledTime;
            if (_phase != null)
            {
                var kb = Keyboard.current;
                var kbMark = kb != null && kb.enabled ? "kb" : "no-kb";
                _phase.text = _actor.PhaseLabel(t) + "  ·  cam " + Mathf.RoundToInt(_orbitYaw) + "  ·  " + kbMark;
            }
        }

        private void OnGUI()
        {
            // IMGUI lives on the Game view. Derek 7187204: Keyboard.current never
            // yawed because Editor play routes keys to the focused window (default
            // PointersAndKeyboardsRespectGameViewFocus). Buttons + drag always work.
            if (GUI.Button(new Rect(10f, 10f, 96f, 40f), "1 Rear"))
            {
                ApplyOrbitKey(KeyCode.Alpha1);
            }

            if (GUI.Button(new Rect(112f, 10f, 96f, 40f), "2 3/4"))
            {
                ApplyOrbitKey(KeyCode.Alpha2);
            }

            if (GUI.Button(new Rect(214f, 10f, 96f, 40f), "3 Front"))
            {
                ApplyOrbitKey(KeyCode.Alpha3);
            }

            var ev = Event.current;
            if (ev == null)
            {
                return;
            }

            if (ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None) // skip ev.keyCode == KeyCode.None
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

            var overButtons = ev.mousePosition.y <= 54f && ev.mousePosition.x <= 320f;
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

        /// <summary>
        /// Default rear Play-cam SoT. yaw/pitch 0 keeps SirAldric3DMotion rear eye.
        /// </summary>
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

            SirAldric3DMotion.PlayCamOrbitEye(orbitYawDegrees, orbitPitchDegrees, out var x, out var y, out var z);
            var eye = new Vector3(x, y, z);
            var look = new Vector3(
                SirAldric3DMotion.PlayCamLookX,
                SirAldric3DMotion.PlayCamLookY,
                SirAldric3DMotion.PlayCamLookZ);
            var forward = look - eye;
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.999f)
            {
                up = Vector3.back;
            }

            var rot = Quaternion.Normalize(Quaternion.LookRotation(forward, up));
            var n = Camera.allCamerasCount;
            if (n <= 0)
            {
                PlaceOneCam(Camera.main, eye, rot);
                return;
            }

            var cams = new Camera[n];
            Camera.GetAllCameras(cams);
            for (var i = 0; i < n; i++)
            {
                PlaceOneCam(cams[i], eye, rot);
            }
        }

        private static void PlaceOneCam(Camera? cam, Vector3 eye, Quaternion rot)
        {
            if (cam == null)
            {
                return;
            }

            cam.orthographic = false;
            cam.usePhysicalProperties = false;
            cam.fieldOfView = SirAldric3DMotion.PlayCamFovDegrees;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 40f;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.07f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = eye;
            cam.transform.rotation = rot;
        }

        public void ForcePreset(int slot)
        {
            if (slot == 2)
            {
                ApplyOrbitKey(KeyCode.Alpha2);
            }
            else if (slot == 3)
            {
                ApplyOrbitKey(KeyCode.Alpha3);
            }
            else
            {
                ApplyOrbitKey(KeyCode.Alpha1);
            }

            ApplyPlayCam(_orbitYaw, _orbitPitch);
        }

        public void AddOrbit(float yaw, float pitch)
        {
            _orbitYaw += yaw;
            _orbitPitch += pitch;
            WrapOrbit();
            ApplyPlayCam(_orbitYaw, _orbitPitch);
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
                _orbitYaw = SirAldric3DMotion.PlayCamThreeQuarterYawDegrees();
                _orbitPitch = 0f;
            }
            else if (key == KeyCode.Alpha3 || key == KeyCode.Keypad3)
            {
                _orbitYaw = 180f;
                _orbitPitch = 0f;
            }
        }

        private void TickOrbit()
        {
            // Poll every backend. Derek 7187204: Keyboard.current != null skipped
            // legacy GetKey, then Editor focus left Keyboard.current deaf.
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

            if (_orbitMap != null)
            {
                var yawAction = _orbitMap.FindAction("Yaw");
                if (yawAction != null)
                {
                    _orbitYaw += yawAction.ReadValue<float>() * SirAldric3DMotion.PlayCamOrbitYawSpeed * Time.unscaledDeltaTime;
                }
            }

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
            var map = new InputActionMap("SirAldricOrbit");
            var rear = map.AddAction("Rear", InputActionType.Button);
            rear.AddBinding("<Keyboard>/1");
            rear.AddBinding("<Keyboard>/numpad1");
            rear.performed += _ => ForcePreset(1);
            var tq = map.AddAction("ThreeQuarter", InputActionType.Button);
            tq.AddBinding("<Keyboard>/2");
            tq.AddBinding("<Keyboard>/numpad2");
            tq.performed += _ => ForcePreset(2);
            var front = map.AddAction("Front", InputActionType.Button);
            front.AddBinding("<Keyboard>/3");
            front.AddBinding("<Keyboard>/numpad3");
            front.performed += _ => ForcePreset(3);
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

        private static void EnsurePlayInput()
        {
            try
            {
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("PILOT orbit Input System settings: " + ex.Message);
            }

            SurvivalVisuals.EnsureEventSystem();
            var es = EventSystem.current != null
                ? EventSystem.current
                : Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                return;
            }

            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void BuildOrbitHud(RectTransform canvas)
        {
            var pad = SurvivalVisuals.Image(canvas, "OrbitPad", new Color(1f, 1f, 1f, 0.01f));
            pad.raycastTarget = true;
            SurvivalVisuals.Stretch(pad.rectTransform);
            pad.rectTransform.offsetMin = new Vector2(0f, 90f);
            pad.rectTransform.offsetMax = new Vector2(0f, -120f);
            pad.gameObject.AddComponent<SirAldricOrbitPad>();

            AddOrbitButton(canvas, "BtnRear", "1  REAR", 0.08f, 0.36f, 1);
            AddOrbitButton(canvas, "BtnTq", "2  3/4", 0.38f, 0.62f, 2);
            AddOrbitButton(canvas, "BtnFront", "3  FRONT", 0.64f, 0.92f, 3);
        }

        private void AddOrbitButton(RectTransform canvas, string name, string label, float x0, float x1, int slot)
        {
            var btn = SurvivalVisuals.Button(canvas, name, new Color(0.16f, 0.14f, 0.10f, 0.92f));
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, 0.075f);
            rt.anchorMax = new Vector2(x1, 0.145f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = SurvivalVisuals.Text(btn.transform, "Label", label, 22, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            SurvivalVisuals.Stretch(text.rectTransform);
            var captured = slot;
            btn.onClick.AddListener(() => ForcePreset(captured));
        }

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            SurvivalVisuals.EnsurePlayCamera();
            EnsurePlayInput();
            _orbitYaw = 0f;
            _orbitPitch = 0f;
            ApplyPlayCam();
            ThemePackBinder? pack = null;
            try
            {
                var flavor = AppFlavorConfig.FantasyKingdomA;
                pack = LoadPack(flavor);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(6f, 10f, 1f);
            ground.transform.position = new Vector3(0f, 0f, 1.6f);
            var gr = ground.GetComponent<Renderer>();
            if (gr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", new Color(0.10f, 0.12f, 0.09f));
                }
                else
                {
                    mat.color = new Color(0.10f, 0.12f, 0.09f);
                }

                gr.sharedMaterial = mat;
            }

            Object.Destroy(ground.GetComponent<Collider>());

            for (var i = 0; i < 6; i++)
            {
                var z = 0.15f + i * 0.45f;
                var chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chev.name = "Chevron" + i;
                chev.transform.SetParent(transform, false);
                chev.transform.position = new Vector3(0f, 0.02f, z);
                chev.transform.localScale = new Vector3(0.55f - i * 0.04f, 0.02f, 0.10f);
                Object.Destroy(chev.GetComponent<Collider>());
                var cr = chev.GetComponent<Renderer>();
                if (cr != null)
                {
                    var cm = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                    cm.color = new Color(0.83f, 0.69f, 0.32f, 1f);
                    cr.sharedMaterial = cm;
                }
            }

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "EnemyTop";
            enemy.transform.SetParent(transform, false);
            enemy.transform.position = new Vector3(0f, 0.28f, 2.15f);
            enemy.transform.localScale = new Vector3(0.38f, 0.55f, 0.38f);
            Object.Destroy(enemy.GetComponent<Collider>());
            var er = enemy.GetComponent<Renderer>();
            if (er != null)
            {
                var em = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                em.color = new Color(0.22f, 0.07f, 0.12f, 1f);
                er.sharedMaterial = em;
            }

            var actorGo = new GameObject("SirAldricMeshyAnimate");
            actorGo.transform.SetParent(transform, false);
            _actor = actorGo.AddComponent<SirAldricMeshyAnimateActor>();
            _actor.Build();

            var canvas = SurvivalVisuals.Canvas(transform, "SirAldricHud", 80);
            BuildOrbitHud(canvas);
            var caption = pack?.StringOr("theme_a.hero.sir_aldric.demo_caption", "SIR ALDRIC  ·  walk → attack TOP")
                          ?? "SIR ALDRIC  ·  walk → attack TOP";
            var title = SurvivalVisuals.Text(canvas, "Caption", caption, 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0.06f, 0.915f);
            tr.anchorMax = new Vector2(0.94f, 0.97f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            _phase = SurvivalVisuals.Text(canvas, "Phase", "WALK  ·  toward TOP", 20, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            var pr = _phase.rectTransform;
            pr.anchorMin = new Vector2(0.10f, 0.868f);
            pr.anchorMax = new Vector2(0.90f, 0.915f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            var top = SurvivalVisuals.Text(canvas, "TopMark", "▲  TOP  ·  ENEMY", 18, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            var topr = top.rectTransform;
            topr.anchorMin = new Vector2(0.20f, 0.825f);
            topr.anchorMax = new Vector2(0.80f, 0.868f);
            topr.offsetMin = Vector2.zero;
            topr.offsetMax = Vector2.zero;

            var note = SurvivalVisuals.Text(
                canvas,
                "SoT",
                "PILOT Mixamo  ·  RH grip walk  ·  Design DIAG slash iQ1s3nN1330  ·  1/2/3 orbit  ·  HOLD",
                16,
                TextAnchor.MiddleCenter,
                SurvivalVisuals.Mute);
            var nr = note.rectTransform;
            nr.anchorMin = new Vector2(0.04f, 0.03f);
            nr.anchorMax = new Vector2(0.96f, 0.07f);
            nr.offsetMin = Vector2.zero;
            nr.offsetMax = Vector2.zero;

            _booted = true;

            if (SirAldricGameViewCapture.ShouldRunFromCommandLine())
            {
                StartCoroutine(CaptureThenQuit());
            }
        }

        private System.Collections.IEnumerator CaptureThenQuit()
        {
            yield return null;
            yield return null;
            if (_actor != null && _actor.Built && Camera.main != null)
            {
                SirAldricGameViewCapture.CapturePilot(_actor, Camera.main);
            }

            if (Application.isBatchMode)
            {
                Application.Quit(0);
            }
        }

        internal static string[] ResolveMasterPaths(AppFlavorConfig flavor)
        {
            var packRoot = SurvivalArt.ResolvePackRoot(flavor);
            var cwd = Directory.GetCurrentDirectory();
            var data = Application.dataPath ?? Path.Combine(cwd, "Assets");
            var repo = Directory.GetParent(data)?.FullName ?? cwd;
            return new[]
            {
                Path.Combine(packRoot, "art", "heroes", DropFileName),
                Path.Combine(packRoot, "art", "heroes", DropFileName512),
                Path.Combine(repo, "design", "survival-theme-a-fantasy", "heroes", "anim", "sir_aldric", "UNITY_DROP_LOCKED", DropFileName)
            };
        }

        private static ThemePackBinder LoadPack(AppFlavorConfig flavor)
        {
            var packRoot = SurvivalArt.ResolvePackRoot(flavor);
            return ThemePackBinder.Parse(
                File.ReadAllText(Path.Combine(packRoot, "pack.json")),
                File.ReadAllText(Path.Combine(packRoot, "strings", "en.json")),
                File.ReadAllText(Path.Combine(packRoot, "map", "node_pins.json")),
                File.ReadAllText(Path.Combine(packRoot, "ui", "hud_layout.json")));
        }
    }

    /// <summary>uGUI drag on the Game view — Input System pointer, not IMGUI.</summary>
    public sealed class SirAldricOrbitPad : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            if (SirAldricDemo.Live == null || eventData == null)
            {
                return;
            }

            SirAldricDemo.Live.AddOrbit(eventData.delta.x * 0.28f, -eventData.delta.y * 0.20f);
        }
    }
}
