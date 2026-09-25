using Survival.Domain.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// 1080×1920 Game-view demo: Bonequill Meshy Animate walk AS-IS.
    /// Path A cancelled. Body-only leftover noted. No hub unlock. No Design PASS.
    /// </summary>
    public sealed class BonequillDemo : MonoBehaviour
    {
        private bool _booted;
        private BonequillMeshyAnimateActor? _actor;
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

            if (_phase != null)
            {
                _phase.text = _actor.PhaseLabel(Time.unscaledTime);
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
                cam.fieldOfView = 30f;
                cam.nearClipPlane = 0.08f;
                cam.farClipPlane = 40f;
                cam.backgroundColor = new Color(0.08f, 0.09f, 0.07f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0f, 2.80f, -5.40f);
                cam.transform.LookAt(new Vector3(0f, 0.90f, 0.50f));
            }

            SurvivalVisuals.EnsureEventSystem();

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

            UnityEngine.Object.Destroy(ground.GetComponent<Collider>());

            for (var i = 0; i < 6; i++)
            {
                var z = 0.15f + i * 0.45f;
                var chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chev.name = "Chevron" + i;
                chev.transform.SetParent(transform, false);
                chev.transform.position = new Vector3(0f, 0.02f, z);
                chev.transform.localScale = new Vector3(0.55f - i * 0.04f, 0.02f, 0.10f);
                UnityEngine.Object.Destroy(chev.GetComponent<Collider>());
                var cr = chev.GetComponent<Renderer>();
                if (cr != null)
                {
                    var cm = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                    cm.color = new Color(0.83f, 0.69f, 0.32f, 1f);
                    cr.sharedMaterial = cm;
                }
            }

            var actorGo = new GameObject("BonequillMeshyAnimate");
            actorGo.transform.SetParent(transform, false);
            _actor = actorGo.AddComponent<BonequillMeshyAnimateActor>();
            _actor.Build();

            var canvas = SurvivalVisuals.Canvas(transform, "BonequillHud", 80);
            var title = SurvivalVisuals.Text(canvas, "Caption", "BONEQUILL  ·  Meshy Animate walk", 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream);
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

            var top = SurvivalVisuals.Text(canvas, "TopMark", "▲  TOP", 18, TextAnchor.MiddleCenter, SurvivalVisuals.Gold);
            var topr = top.rectTransform;
            topr.anchorMin = new Vector2(0.20f, 0.825f);
            topr.anchorMax = new Vector2(0.80f, 0.868f);
            topr.offsetMin = Vector2.zero;
            topr.offsetMax = Vector2.zero;

            var note = SurvivalVisuals.Text(
                canvas,
                "SoT",
                "walk-only  ·  body-only leftover  ·  HOLD  ·  no Design PASS",
                16,
                TextAnchor.MiddleCenter,
                SurvivalVisuals.Mute);
            var nr = note.rectTransform;
            nr.anchorMin = new Vector2(0.04f, 0.03f);
            nr.anchorMax = new Vector2(0.96f, 0.07f);
            nr.offsetMin = Vector2.zero;
            nr.offsetMax = Vector2.zero;

            _booted = true;
            Debug.Log(BonequillAnimateWalk.SoftLeftover);
        }
    }
}
