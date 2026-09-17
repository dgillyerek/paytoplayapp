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
    /// 1080×1920 Game-view demo: locked rear master only. Walk toward TOP, then attack TOP.
    /// No AI multi-frame packs. Plain dark ground, no rocks.
    /// </summary>
    public sealed class SirAldricDemo : MonoBehaviour
    {
        public const string DropFileName = "SIR_ALDRIC_REAR_MASTER_LOCKED.png";
        public const string DropFileName512 = "SIR_ALDRIC_REAR_MASTER_LOCKED_512.png";

        private bool _booted;
        private SirAldricView? _view;
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

            if (_view == null || !_view.Built)
            {
                return;
            }

            var pose = SirAldricMotion.Evaluate(Time.unscaledTime);
            _view.Apply(pose);
            if (_phase != null)
            {
                _phase.text = pose.Attacking
                    ? (pose.StrikeTowardTop ? "ATTACK  ·  strike TOP" : "ATTACK  ·  draw / recover")
                    : "WALK  ·  toward TOP";
            }
        }

        private void Boot()
        {
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            SurvivalVisuals.EnsurePlayCamera();
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = new Color(0.08f, 0.09f, 0.07f, 1f);
            }

            SurvivalVisuals.EnsureEventSystem();
            var flavor = AppFlavorConfig.FantasyKingdomA;
            ThemePackBinder? pack = null;
            try
            {
                pack = LoadPack(flavor);
                SurvivalArt.EnsureLoaded(flavor, pack);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            var canvas = SurvivalVisuals.Canvas(transform, "SirAldricCanvas", 50);
            var ground = SurvivalVisuals.Image(canvas, "Ground", new Color(0.10f, 0.12f, 0.09f, 1f));
            SurvivalVisuals.Stretch(ground.rectTransform);
            ground.raycastTarget = false;

            var shade = SurvivalVisuals.Image(canvas, "Shade", new Color(0.05f, 0.06f, 0.05f, 0.35f));
            var srt = shade.rectTransform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.22f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            var master = SurvivalArt.Get(SurvIds.ThemeAHeroSirAldricRear)
                         ?? SurvivalArt.Get(SurvIds.ThemeAHeroSirAldricRear512)
                         ?? SirAldricView.LoadMasterPng(ResolveMasterPaths(flavor));

            var body = new GameObject("SirAldric", typeof(RectTransform), typeof(CanvasRenderer));
            _view = body.AddComponent<SirAldricView>();
            _view.Build(canvas, master);

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
                "LOCKED rear master  ·  mesh warp  ·  no AI frames",
                16,
                TextAnchor.MiddleCenter,
                SurvivalVisuals.Mute);
            var nr = note.rectTransform;
            nr.anchorMin = new Vector2(0.08f, 0.03f);
            nr.anchorMax = new Vector2(0.92f, 0.07f);
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
                Path.Combine(repo, "design", "survival-theme-a-fantasy", "heroes", "anim", "sir_aldric", "UNITY_DROP_LOCKED", DropFileName),
                Path.Combine(repo, "design", "survival-theme-a-fantasy", "heroes", "anim", "sir_aldric", "UNITY_DROP_LOCKED", DropFileName512),
                Path.Combine(cwd, "design", "survival-theme-a-fantasy", "heroes", "anim", "sir_aldric", "UNITY_DROP_LOCKED", DropFileName)
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
