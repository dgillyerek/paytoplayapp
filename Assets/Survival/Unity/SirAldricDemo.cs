using System.IO;
using Survival.Domain.Flavor;
using Survival.Domain.Heroes;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// 1080×1920 Game-view demo: PILOT Mixamo holefixed + Walk + Slash.
    /// Path A / ClipSword HOLD. RH sword prop. Design / Derek PASS not claimed.
    /// Play-cam defaults to the rear SoT; 1/2/3 and Q/E or RMB orbit around the knight.
    /// Input System Keyboard.current (legacy GetKey is dead when the package owns Play).
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

        private void Awake() => Boot();

        private void OnEnable() => Boot();

        private void Start() => Boot();

        private void Update()
        {
            if (!_booted)
            {
                Boot();
            }

            TickOrbit();
            if (_actor == null || !_actor.Built)
            {
                return;
            }

            var t = Time.unscaledTime;
            if (_phase != null)
            {
                _phase.text = _actor.PhaseLabel(t) + "  ·  cam " + Mathf.RoundToInt(_orbitYaw);
            }
        }

        private void OnGUI()
        {
            var ev = Event.current;
            if (ev == null || ev.type != EventType.KeyDown || ev.repeat)
            {
                return;
            }

            ApplyOrbitKey(ev.keyCode);
        }

        private void LateUpdate()
        {
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
            var cam = Camera.main;
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
            SirAldric3DMotion.PlayCamOrbitEye(orbitYawDegrees, orbitPitchDegrees, out var x, out var y, out var z);
            cam.transform.position = new Vector3(x, y, z);
            cam.transform.LookAt(new Vector3(
                SirAldric3DMotion.PlayCamLookX,
                SirAldric3DMotion.PlayCamLookY,
                SirAldric3DMotion.PlayCamLookZ));
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
            var kb = Keyboard.current;
            if (kb != null)
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
            else
            {
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

                var yaw = 0f;
                if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                {
                    yaw -= 1f;
                }

                if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                {
                    yaw += 1f;
                }

                _orbitYaw += yaw * SirAldric3DMotion.PlayCamOrbitYawSpeed * Time.unscaledDeltaTime;
            }

            var mouse = Mouse.current;
            if (mouse != null && (mouse.rightButton.isPressed || mouse.middleButton.isPressed))
            {
                var d = mouse.delta.ReadValue();
                _orbitYaw += d.x * 0.18f;
                _orbitPitch -= d.y * 0.18f;
            }
            else if (mouse == null && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                _orbitYaw += Input.GetAxis("Mouse X") * 140f;
                _orbitPitch -= Input.GetAxis("Mouse Y") * 80f;
            }

            _orbitPitch = Mathf.Clamp(
                _orbitPitch,
                SirAldric3DMotion.PlayCamOrbitPitchMin,
                SirAldric3DMotion.PlayCamOrbitPitchMax);
        }

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            SurvivalVisuals.EnsurePlayCamera();
            _orbitYaw = 0f;
            _orbitPitch = 0f;
            ApplyPlayCam();

            SurvivalVisuals.EnsureEventSystem();
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
                "PILOT Mixamo  ·  hip sheath walk  ·  UR→front→LL  ·  1 rear 2 3/4 3 front  ·  Q/E orbit  ·  HOLD",
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
}
