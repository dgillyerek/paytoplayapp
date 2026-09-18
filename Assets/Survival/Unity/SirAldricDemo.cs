using System.IO;
using Survival.Domain.Flavor;
using Survival.Domain.Heroes;
using Survival.Domain.Ids;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// 1080×1920 Game-view demo: 3D Animator-driven Aldric, high-angle rear, walk+attack toward TOP.
    /// Locked rear PNG remains the Play-hub placeholder until Design PASSes this clip.
    /// </summary>
    public sealed class SirAldricDemo : MonoBehaviour
    {
        public const string DropFileName = "SIR_ALDRIC_REAR_MASTER_LOCKED.png";
        public const string DropFileName512 = "SIR_ALDRIC_REAR_MASTER_LOCKED_512.png";

        private bool _booted;
        private SirAldric3DActor? _actor;
        private Text? _phase;

        private void Awake() => Boot();

        private void OnEnable() => Boot();

        private void Start() => Boot();

        private void Update()
        {
            if (!_booted)
            {
                Boot();
            }

            if (_actor == null || !_actor.Built)
            {
                return;
            }

            var t = Time.unscaledTime;
            if (_phase != null)
            {
                _phase.text = _actor.PhaseLabel(t);
            }

            var pose = SirAldric3DMotion.Evaluate(t);
            if (Camera.main != null)
            {
                var z = pose.RootZ;
                Camera.main.transform.position = new Vector3(0f, 2.45f, z - 5.50f);
                Camera.main.transform.LookAt(new Vector3(0f, 1.00f, z + 0.20f));
            }
        }

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            SurvivalVisuals.EnsurePlayCamera();
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = 28f;
                cam.nearClipPlane = 0.08f;
                cam.farClipPlane = 40f;
                cam.backgroundColor = new Color(0.08f, 0.09f, 0.07f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

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
            ground.transform.localScale = new Vector3(8f, 8f, 1f);
            ground.transform.position = new Vector3(0f, 0f, 1.2f);
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

            var actorGo = new GameObject("SirAldric3D");
            actorGo.transform.SetParent(transform, false);
            _actor = actorGo.AddComponent<SirAldric3DActor>();
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

            var note = SurvivalVisuals.Text(
                canvas,
                "SoT",
                "3D Animator proxy  ·  high-angle rear  ·  PNG still Play placeholder",
                16,
                TextAnchor.MiddleCenter,
                SurvivalVisuals.Mute);
            var nr = note.rectTransform;
            nr.anchorMin = new Vector2(0.04f, 0.03f);
            nr.anchorMax = new Vector2(0.96f, 0.07f);
            nr.offsetMin = Vector2.zero;
            nr.offsetMax = Vector2.zero;

            _booted = true;
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
