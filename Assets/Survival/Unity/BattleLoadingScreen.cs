#nullable enable
using System.Collections;
using Survival.Domain.Campaign;
using Survival.Domain.Layout;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Painted loading screen for the Play ⇄ BattleLocked round trip. It owns two things, both outside every
    /// game scene (DontDestroyOnLoad) so neither scene's hide / unload can touch them:
    /// <list type="bullet">
    /// <item>a floor camera (lowest depth, culls nothing, clears to dusk) that keeps Display 1 rendering while
    /// Play's cameras are off and BattleLocked's camera does not exist yet, or the reverse on the way back;</item>
    /// <item>a screen-space overlay canvas (backdrop, crossed swords, stage banner, VS portraits, bar, tip) that
    /// stays up from the tap until the destination camera has rendered a frame, at least 0.6 s, then fades.</item>
    /// </list>
    /// Every piece of art is a pack.json key with a code-drawn fallback.
    /// </summary>
    internal sealed class BattleLoadingScreen : MonoBehaviour
    {
        public const string RootName = "BattleLoadingScreen";
        public const int Sorting = 32000;

        /// <summary>Safety net: if no destination camera ever draws (broken scene), still clear the screen.</summary>
        private const float RenderWaitLimitSeconds = 10f;

        private static readonly Color Dusk = new Color(0.07f, 0.06f, 0.11f, 1f);
        private static readonly Color HeroRing = new Color(CampaignArtKeys.HeroFrameRgb[0], CampaignArtKeys.HeroFrameRgb[1], CampaignArtKeys.HeroFrameRgb[2], 1f);
        private static readonly Color VillainRing = new Color(CampaignArtKeys.VillainFrameRgb[0], CampaignArtKeys.VillainFrameRgb[1], CampaignArtKeys.VillainFrameRgb[2], 1f);

        private readonly BattleTransition _t = new BattleTransition();
        private Camera? _floor;
        private CanvasGroup? _group;
        private Image? _barFill;
        private RectTransform? _emblem;
        private Scene _target;
        private bool _watching;
        private float _watchStarted;
        private bool _warnedGaveUp;

        public bool Covered => _t.Covered;

        public bool Done => _t.Done;

        /// <summary>True for the floor camera, so battle camera drivers leave it alone.</summary>
        public static bool IsLoadingCamera(Camera? cam) => cam != null && cam.GetComponentInParent<BattleLoadingScreen>() != null;

        /// <summary>Put the screen up (fading in over the current view) and start the floor camera at once.</summary>
        public static BattleLoadingScreen Show(BattleLoadingInfo info, string wordVs)
        {
            var go = new GameObject(RootName);
            DontDestroyOnLoad(go);
            var screen = go.AddComponent<BattleLoadingScreen>();
            screen.Build(info, wordVs);
            screen._t.Begin(Time.unscaledTimeAsDouble);
            screen.Apply();
            return screen;
        }

        /// <summary>Scene load / unload progress for the bar.</summary>
        public void Progress(float p) => _t.ReportProgress(p);

        /// <summary>
        /// From now on, the first frame any camera in <paramref name="target"/> renders lets the screen fade out
        /// (after the minimum show time). An invalid scene accepts any game camera but the floor.
        /// </summary>
        public void RevealOnFirstFrameOf(Scene target)
        {
            _target = target;
            _watching = true;
            _watchStarted = Time.unscaledTime;
        }

        /// <summary>
        /// Stop waiting for a destination frame and fade out (after the minimum show time). Used when the trip fails
        /// part-way, so the screen never freezes the game under it.
        /// </summary>
        public void Release(string why)
        {
            if (!_t.TargetRendered)
            {
                Debug.LogWarning("Loading screen released: " + why);
            }

            _t.Release();
        }

        public IEnumerator WaitCovered()
        {
            while (this != null && !_t.Covered && !_t.Done)
            {
                yield return null;
            }
        }

        public IEnumerator WaitDone()
        {
            while (this != null && !_t.Done)
            {
                yield return null;
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraSrp;
            Camera.onPostRender += OnPostRenderBuiltin;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraSrp;
            Camera.onPostRender -= OnPostRenderBuiltin;
        }

        private void Update()
        {
            if (_watching && !_t.TargetRendered && Time.unscaledTime - _watchStarted > RenderWaitLimitSeconds)
            {
                Debug.LogWarning("Battle loading: no camera in '" + _target.name + "' rendered; clearing the loading screen anyway.");
                _t.MarkTargetRendered();
            }

            _t.Step(Time.unscaledTimeAsDouble);
            if (_t.GaveUp && !_warnedGaveUp)
            {
                _warnedGaveUp = true;
                Debug.LogWarning("Battle loading: destination never rendered within " + BattleTransition.ShowLimitSeconds + " s of the screen going up; clearing it anyway.");
            }

            Apply();
            if (_t.Done)
            {
                // The destination camera is rendering by now, so the floor can go with the overlay.
                Destroy(gameObject);
            }
        }

        private void OnEndCameraSrp(ScriptableRenderContext context, Camera cam)
        {
            _ = context;
            Saw(cam);
        }

        private void OnPostRenderBuiltin(Camera cam) => Saw(cam);

        private void Saw(Camera cam)
        {
            if (!_watching || _t.TargetRendered || cam == null || cam == _floor || cam.cameraType != CameraType.Game || !cam.isActiveAndEnabled)
            {
                return;
            }

            if (_target.IsValid() && cam.gameObject.scene != _target)
            {
                return;
            }

            _t.MarkTargetRendered();
        }

        private void Apply()
        {
            if (_group != null)
            {
                _group.alpha = _t.Alpha;
                _group.blocksRaycasts = !_t.Done;
            }

            if (_barFill != null)
            {
                HookSkin.SetBar(_barFill, _t.Bar, HookArtKeys.BarFillGold, SurvivalVisuals.Reward);
            }

            if (_emblem != null)
            {
                var s = 1f + (0.035f * Mathf.Sin(Time.unscaledTime * 3.2f));
                _emblem.localScale = new Vector3(s, s, 1f);
            }
        }

        // ---------- build ----------

        private void Build(BattleLoadingInfo info, string wordVs)
        {
            var camGo = new GameObject("LoadingFloorCamera");
            camGo.transform.SetParent(transform, false);
            _floor = camGo.AddComponent<Camera>();
            _floor.depth = -100f;
            _floor.cullingMask = 0;
            _floor.clearFlags = CameraClearFlags.SolidColor;
            _floor.backgroundColor = Dusk;
            _floor.orthographic = true;
            _floor.useOcclusionCulling = false;
            _floor.allowHDR = false;
            _floor.allowMSAA = false;

            var root = SurvivalVisuals.Canvas(transform, "LoadingCanvas", Sorting);
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;

            // Backdrop: painted battlefield, or a dusk gradient drawn in code.
            var backSprite = SurvivalArt.Get(BattleLoadingArtKeys.Backdrop);
            var back = SurvivalVisuals.Image(root, "Backdrop", backSprite != null ? Color.white : Dusk, backSprite);
            back.raycastTarget = true; // swallow taps while loading
            SurvivalVisuals.Stretch(back.rectTransform);
            if (backSprite != null)
            {
                Cover(back, backSprite);
            }
            else
            {
                Band(root, "Sky", new Color(0.42f, 0.24f, 0.30f, 1f), 0.55f, 1f);
                Band(root, "Glow", new Color(0.85f, 0.55f, 0.25f, 0.55f), 0.52f, 0.62f);
                Band(root, "Field", new Color(0.13f, 0.17f, 0.10f, 1f), 0f, 0.55f);
            }

            Band(root, "ShadeTop", new Color(0f, 0f, 0f, 0.35f), 0.88f, 1f);
            Band(root, "ShadeLow", new Color(0.02f, 0.02f, 0.05f, 0.62f), 0f, 0.42f);

            // Emblem: painted crossed swords + shield, or two crossed blades and a disc.
            var emblemSprite = SurvivalArt.Get(BattleLoadingArtKeys.Emblem);
            var glow = HookSkin.Glow(root, "EmblemGlow", new Color(1f, 0.78f, 0.35f, 0.55f));
            SurvivalVisuals.AnchorBox(glow.rectTransform, 0.5f, 0.66f, 0.78f, 0.44f);
            glow.preserveAspect = true;
            if (emblemSprite != null)
            {
                var em = SurvivalVisuals.Image(root, "Emblem", Color.white, emblemSprite);
                em.preserveAspect = true;
                SurvivalVisuals.AnchorBox(em.rectTransform, 0.5f, 0.66f, 0.46f, 0.26f);
                _emblem = em.rectTransform;
            }
            else
            {
                _emblem = CodeSwords(root);
            }

            // Stage banner.
            var banner = HookSkin.Panel(root, "Banner", HookArtKeys.Header, new Color(0.45f, 0.12f, 0.10f, 1f), 0.75f);
            SurvivalVisuals.AnchorBox(banner.rectTransform, 0.5f, 0.505f, 0.86f, 0.085f);
            var stage = HookSkin.Outlined(SurvivalVisuals.Text(banner.transform, "Stage", info.StageName, 50, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 2.5f);
            stage.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(stage.rectTransform);
            stage.rectTransform.offsetMin = new Vector2(70f, 4f);
            stage.rectTransform.offsetMax = new Vector2(-70f, -4f);
            stage.resizeTextForBestFit = true;
            stage.resizeTextMaxSize = 50;
            stage.resizeTextMinSize = 24;
            stage.horizontalOverflow = HorizontalWrapMode.Wrap;

            var head = HookSkin.Outlined(SurvivalVisuals.Text(root, "Headline", info.Headline, 64, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 3f);
            head.fontStyle = FontStyle.Bold;
            SurvivalVisuals.AnchorBox(head.rectTransform, 0.5f, 0.9f, 0.95f, 0.07f);

            if (!info.Returning && (info.HeroPortraits.Count > 0 || info.EnemyPortraits.Count > 0))
            {
                Faces(root, info.HeroPortraits, 0.03f, 0.43f, HeroRing);
                Faces(root, info.EnemyPortraits, 0.57f, 0.97f, VillainRing);
                var vs = HookSkin.Outlined(SurvivalVisuals.Text(root, "VS", wordVs, 76, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 3.5f);
                vs.fontStyle = FontStyle.BoldAndItalic;
                SurvivalVisuals.AnchorBox(vs.rectTransform, 0.5f, 0.36f, 0.2f, 0.08f);
            }

            // Progress bar and tip.
            var bar = HookSkin.Bar(root, "Bar", out var fill);
            SurvivalVisuals.AnchorBox(bar.rectTransform, 0.5f, 0.17f, 0.78f, 0.03f);
            _barFill = fill;
            var tip = HookSkin.Outlined(SurvivalVisuals.Text(root, "Tip", info.Tip, 32, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
            tip.horizontalOverflow = HorizontalWrapMode.Wrap;
            SurvivalVisuals.AnchorBox(tip.rectTransform, 0.5f, 0.1f, 0.86f, 0.08f);
        }

        /// <summary>Portrait discs for one side, in lane order, centred inside [x0, x1].</summary>
        private static void Faces(RectTransform root, System.Collections.Generic.IReadOnlyList<string> keys, float x0, float x1, Color ring)
        {
            var n = Mathf.Clamp(keys.Count, 0, 5);
            if (n == 0)
            {
                return;
            }

            var span = x1 - x0;
            var size = Mathf.Min(0.15f, span / Mathf.Max(1, n) * 0.96f);
            var aspect = PlayHudLayout.ReferenceWidth / (float)PlayHudLayout.ReferenceHeight;
            for (var i = 0; i < n; i++)
            {
                var row = n > 3 ? i % 2 : 0;
                var cx = x0 + (span * (i + 0.5f) / n);
                var cy = 0.36f + (n > 3 ? (row == 0 ? 0.022f : -0.022f) : 0f);
                var frame = SurvivalVisuals.Circle(root, "Face" + i, ring);
                SurvivalVisuals.AnchorBox(frame.rectTransform, cx, cy, size, size * aspect);
                var trim = SurvivalVisuals.Ring(frame.transform, "Trim", SurvivalVisuals.GoldLine);
                SurvivalVisuals.Stretch(trim.rectTransform);
                var sprite = SurvivalArt.Get(keys[i]);
                if (sprite != null)
                {
                    var mask = SurvivalVisuals.Circle(frame.transform, "Mask", Color.white);
                    SurvivalVisuals.Stretch(mask.rectTransform);
                    mask.rectTransform.offsetMin = new Vector2(7f, 7f);
                    mask.rectTransform.offsetMax = new Vector2(-7f, -7f);
                    mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                    var art = SurvivalVisuals.Image(mask.transform, "Art", Color.white, sprite);
                    art.preserveAspect = true;
                    SurvivalVisuals.Stretch(art.rectTransform);
                }

                trim.transform.SetAsLastSibling();
            }
        }

        /// <summary>Code-drawn emblem: two crossed blades with gold guards over a blue shield disc.</summary>
        private static RectTransform CodeSwords(RectTransform root)
        {
            var host = new GameObject("Emblem", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(root, false);
            SurvivalVisuals.AnchorBox(host, 0.5f, 0.66f, 0.46f, 0.26f);
            foreach (var angle in new[] { 40f, -40f })
            {
                var blade = SurvivalVisuals.Image(host, "Blade", new Color(0.82f, 0.85f, 0.9f, 1f));
                var rt = blade.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(34f, 420f);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                var guard = SurvivalVisuals.Image(blade.transform, "Guard", SurvivalVisuals.Gold);
                guard.rectTransform.anchorMin = guard.rectTransform.anchorMax = new Vector2(0.5f, 0.12f);
                guard.rectTransform.sizeDelta = new Vector2(120f, 26f);
            }

            var shield = SurvivalVisuals.Circle(host, "Shield", SurvivalVisuals.GoldLine);
            shield.rectTransform.anchorMin = shield.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            shield.rectTransform.sizeDelta = new Vector2(230f, 230f);
            var face = SurvivalVisuals.Circle(shield.transform, "Face", HeroRing);
            SurvivalVisuals.Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(14f, 14f);
            face.rectTransform.offsetMax = new Vector2(-14f, -14f);
            return host;
        }

        private static void Band(RectTransform root, string name, Color c, float y0, float y1)
        {
            var img = SurvivalVisuals.Image(root, name, c);
            img.rectTransform.anchorMin = new Vector2(0f, y0);
            img.rectTransform.anchorMax = new Vector2(1f, y1);
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>Fill the screen with the backdrop without stretching it (crop the overflow).</summary>
        private static void Cover(Image img, Sprite sprite)
        {
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        }
    }
}
