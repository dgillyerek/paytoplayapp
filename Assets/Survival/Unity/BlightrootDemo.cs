using Survival.Domain.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Blightroot Game view 1080×1920. One dropdown of the exact Mixamo names.
    /// Picking a name plays that creature-pack clip on the Dual Weapon Combo body.
    /// HOLD merge until Derek Game-view PASS.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class BlightrootDemo : MonoBehaviour
    {
        public const string DropdownObjectName = "BlightrootClipDropdown";

        private bool _booted;
        private BlightrootActor? _actor;
        private Dropdown? _dropdown;
        private Text? _phase;

        private void Awake() => Boot();

        private void Start() => Boot();

        private void Update()
        {
            if (!_booted)
            {
                Boot();
            }

            ApplyPlayCam();
            if (_phase == null || _actor == null)
            {
                return;
            }

            var name = _actor.ExactName ?? "—";
            _phase.text = name + "   ·   " + _actor.ClipLength.ToString("0.00") + "s   ·   " + BlightrootMotion.TakeName;
        }

        private void LateUpdate()
        {
            if (_booted)
            {
                ApplyPlayCam();
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

        internal static void ApplyPlayCam()
        {
            var eye = new Vector3(BlightrootMotion.PlayCamX, BlightrootMotion.PlayCamY, BlightrootMotion.PlayCamZ);
            var look = new Vector3(BlightrootMotion.PlayCamLookX, BlightrootMotion.PlayCamLookY, BlightrootMotion.PlayCamLookZ);
            var forward = look - eye;
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
            cam.fieldOfView = BlightrootMotion.PlayCamFovDegrees;
            cam.nearClipPlane = BlightrootMotion.PlayCamNear;
            cam.farClipPlane = BlightrootMotion.PlayCamFar;
            cam.aspect = BlightrootMotion.GameViewWidth / BlightrootMotion.GameViewHeight;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.06f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = eye;
            cam.transform.rotation = rot;
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
