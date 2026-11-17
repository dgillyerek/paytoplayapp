using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Catalog;
using Survival.Domain.Clock;
using Survival.Domain.Flavor;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.Theme;
using Survival.Unity.Kids;
using Survival.Unity.Ota;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>Theme A flavor entry: splash keep still (fill + ~5s dwell), then Play world-map shell.</summary>
    public sealed class SurvivalBootstrap : MonoBehaviour
    {
        [SerializeField] private bool skipSplash;

        public SurvivalSession Session { get; private set; } = null!;

        private bool _booted;
        private GameObject? _splashRoot;
        private Text? _loadingText;
        private Image? _progressFill;
        private Text? _progressLabel;
        private string _loadingStem = "Loading";
        private float _splashStart;

        private void Awake() => TryBoot();

        private void OnEnable() => TryBoot();

        private void Start() => TryBoot();

        private void Update()
        {
            if (!_booted)
            {
                TryBoot();
            }

            if (_loadingText != null)
            {
                _loadingText.text = SplashDwell.FormatLoading(_loadingStem, Time.realtimeSinceStartup - _splashStart);
            }
        }

        private void TryBoot()
        {
            SurvivalVisuals.EnsurePlayCamera();
            if (_booted || !Application.isPlaying)
            {
                return;
            }

            try
            {
                Boot();
                _booted = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _booted = true;
            }
        }

        private void Boot()
        {
            if (skipSplash)
            {
                LoadSession();
                AfterAgeGate(ShowPlay);
                return;
            }

            StartCoroutine(SplashThenPlay());
        }

        private IEnumerator SplashThenPlay()
        {
            var flavor = AppFlavorConfig.FantasyKingdomA;
            var pack = LoadPack(flavor);
            ShowSplash(pack);
            _splashStart = Time.realtimeSinceStartup;
            KidUi.Strings = pack.StringOr;

            // Over-the-air step (Docs/OTA.md): remote config, minimum version, guarded balance patches, content download.
            OtaBootResult? ota = null;
            yield return OtaBoot.Run(flavor.BundleId, SetProgress, r => ota = r);
            if (ota != null && ota.Update == Survival.Domain.Ota.UpdateRequirement.Required)
            {
                _loadingText = null;
                UpdateRequiredScreen.Show(ota.StoreUrl);
                yield break;
            }

            if (ota != null && ota.ContentChanged)
            {
                try
                {
                    SurvivalArt.ResetForContentUpdate();
                    pack = LoadPack(flavor);
                }
                catch (Exception ex)
                {
                    // A broken content update must not stop the game: drop the overlays and go on with bundled content.
                    Debug.LogException(ex);
                    PlayerAssets.SetOverlays(null);
                    PlayerAssets.TextFilter = null;
                    SurvivalArt.ResetForContentUpdate();
                    pack = LoadPack(flavor);
                }
            }

            try
            {
                LoadSession(flavor, pack);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            while (!SplashDwell.CanAdvance(Time.realtimeSinceStartup - _splashStart, contentReady: true))
            {
                yield return null;
            }

            AfterAgeGate(AdvanceFromSplash);
        }

        /// <summary>
        /// Families neutral age gate (kids/child_safety.json): asked once per install before play. Until it is answered the
        /// child-safety policy is child mode, so nothing that collects data or sells runs first.
        /// </summary>
        private void AfterAgeGate(Action next)
        {
            if (!KidSafety.NeedsAgeGate)
            {
                next();
                return;
            }

            AgeGateScreen.Show(transform, next);
        }

        private void LoadSession() =>
            LoadSession(AppFlavorConfig.FantasyKingdomA, LoadPack(AppFlavorConfig.FantasyKingdomA));

        private void LoadSession(AppFlavorConfig flavor, ThemePackBinder pack)
        {
            SurvivalArt.EnsureLoaded(flavor, pack);
            KidSafety.Load(flavor);
            KidUi.Strings = pack.StringOr;
            KidSafety.InstallPurchaseConsent();
            var catalogDir = ResolveCatalogDir();
            var catalog = CatalogLoader.LoadFromDirectory(catalogDir, PlayerAssets.ReadAllText);
            Session = TryLoadHookSession(this, flavor, pack, catalog, catalogDir) ?? new SurvivalSession(flavor, pack, catalog);
        }

        /// <summary>World + quest hook slice: base_economy / quests / gifts / stores + Theme A store/products.json. Purchases: Iap.SurvivalPurchases (sandbox in Editor / dev builds, live on release Android).</summary>
        private static SurvivalSession? TryLoadHookSession(MonoBehaviour runner, AppFlavorConfig flavor, ThemePackBinder pack, SurvivalCatalog catalog, string catalogDir)
        {
            if (!PlayerAssets.FileExists(Path.Combine(catalogDir, HookCatalogLoader.BaseEconomyFileName)))
            {
                return null;
            }

            try
            {
                var hook = HookCatalogLoader.LoadFromDirectory(catalogDir, PlayerAssets.ReadAllText);
                var productsPath = Path.Combine(SurvivalArt.ResolvePackRoot(flavor), "store", "products.json");
                var products = PlayerAssets.FileExists(productsPath)
                    ? HookCatalogLoader.ParseProducts(PlayerAssets.ReadAllText(productsPath))
                    : new Dictionary<string, ProductDef>();
                // Live billing on release Android builds (server-validated); the free sandbox only in the Editor / dev builds.
                var purchases = Iap.SurvivalPurchases.Create(runner, SurvivalArt.ResolvePackRoot(flavor), products);
                if (purchases.IsSandbox)
                {
                    // Child mode in the Editor / dev builds: same age-band + parental gate as the live service (#201).
                    purchases = new Survival.Domain.Kids.GatedPurchaseService(purchases, Iap.SurvivalPurchases.AgeGate());
                }

                var session = new SurvivalSession(
                    flavor,
                    pack,
                    catalog,
                    hook,
                    products,
                    new WallClock(),
                    purchases,
                    Environment.TickCount);
                if (purchases is Survival.Domain.Iap.PurchaseService live && session.Hook != null)
                {
                    var hookSession = session.Hook;
                    live.Unsolicited += outcome => hookSession.ApplyUnsolicitedPurchase(outcome);
                }

                return session;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        private static ThemePackBinder LoadPack(AppFlavorConfig flavor)
        {
            var packRoot = SurvivalArt.ResolvePackRoot(flavor);
            return ThemePackBinder.Parse(
                PlayerAssets.ReadAllText(Path.Combine(packRoot, "pack.json")),
                PlayerAssets.ReadAllText(Path.Combine(packRoot, "strings", "en.json")),
                PlayerAssets.ReadAllText(Path.Combine(packRoot, "map", "node_pins.json")),
                PlayerAssets.ReadAllText(Path.Combine(packRoot, "ui", "hud_layout.json")));
        }

        private void ShowSplash(ThemePackBinder pack)
        {
            SurvivalVisuals.EnsureEventSystem();
            var root = SurvivalVisuals.Canvas(transform, "SurvivalSplashCanvas", 80);
            _splashRoot = root.gameObject;
            _loadingStem = pack.StringOr("flavor.splash.loading", "Loading");

            SurvivalArt.EnsureLoaded(AppFlavorConfig.FantasyKingdomA, pack);
            var keepSprite = SurvivalArt.Get("splash") ?? SurvivalArt.Get("flavor.splash");
            var bleed = SurvivalVisuals.Image(root, "Bleed", Color.white, SurvivalArt.EdgeBleed(keepSprite));
            SurvivalVisuals.Stretch(bleed.rectTransform);
            bleed.preserveAspect = false;
            bleed.raycastTarget = false;

            var sky = keepSprite != null ? keepSprite.texture.GetPixel(keepSprite.texture.width / 2, keepSprite.texture.height - 1) : new Color(0.09f, 0.39f, 0.70f);
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = sky;
            }

            var img = SurvivalVisuals.Image(root, "Splash", Color.white, keepSprite);
            SurvivalVisuals.Stretch(img.rectTransform);
            img.preserveAspect = true;
            img.raycastTarget = false;
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (keepSprite != null && keepSprite.rect.height > 1f)
            {
                fitter.aspectRatio = keepSprite.rect.width / keepSprite.rect.height;
            }
            else
            {
                fitter.aspectRatio = 1080f / 1920f;
            }

            var cover = SurvivalVisuals.Image(img.transform, "LoadingCover", new Color(47f / 255f, 34f / 255f, 25f / 255f, 1f));
            var crt = cover.rectTransform;
            crt.anchorMin = new Vector2(0.20f, 0.076f);
            crt.anchorMax = new Vector2(0.80f, 0.112f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            _loadingText = SurvivalVisuals.Text(
                cover.transform,
                "Loading",
                SplashDwell.FormatLoading(_loadingStem, 0f),
                26,
                TextAnchor.MiddleCenter,
                new Color(0.64f, 0.60f, 0.54f, 1f));
            SurvivalVisuals.Stretch(_loadingText.rectTransform);

            // OTA progress bar just above the loading line: config check, catalog check, download (by bytes), unpack.
            var track = SurvivalVisuals.Image(img.transform, "OtaProgress", new Color(47f / 255f, 34f / 255f, 25f / 255f, 0.85f));
            var trt = track.rectTransform;
            trt.anchorMin = new Vector2(0.20f, 0.122f);
            trt.anchorMax = new Vector2(0.80f, 0.134f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            track.raycastTarget = false;
            _progressFill = SurvivalVisuals.Image(track.transform, "Fill", new Color(0.93f, 0.74f, 0.32f, 1f));
            var frt = _progressFill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            _progressFill.raycastTarget = false;
            _progressLabel = SurvivalVisuals.Text(img.transform, "OtaLabel", "", 18, TextAnchor.MiddleCenter, new Color(0.64f, 0.60f, 0.54f, 1f));
            var lrt = _progressLabel.rectTransform;
            lrt.anchorMin = new Vector2(0.15f, 0.136f);
            lrt.anchorMax = new Vector2(0.85f, 0.160f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }

        /// <summary>OTA boot progress on the splash: bar 0..1 and a status line ("Downloading new content 3.2 / 12.5 MB").</summary>
        private void SetProgress(float bar, string label)
        {
            if (_progressFill != null)
            {
                _progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(bar), 1f);
            }

            if (_progressLabel != null)
            {
                _progressLabel.text = bar >= 1f ? "" : label;
            }
        }

        private void AdvanceFromSplash()
        {
            _loadingText = null;
            if (_splashRoot != null)
            {
                Destroy(_splashRoot);
                _splashRoot = null;
            }

            if (SceneExists("Play"))
            {
                SceneManager.LoadScene("Play");
                return;
            }

            ShowPlay();
        }

        private void ShowPlay()
        {
            var view = GetComponent<SurvivalPlayView>() ?? gameObject.AddComponent<SurvivalPlayView>();
            view.Session = Session;
            view.Build();
        }

        private static bool SceneExists(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string ResolveCatalogDir()
        {
            var data = Application.dataPath;
            if (!string.IsNullOrEmpty(data))
            {
                var fromAssets = Path.Combine(data, "Survival", "Resources", "Survival");
                if (PlayerAssets.FileExists(Path.Combine(fromAssets, CatalogLoader.WorldFileName)))
                {
                    return fromAssets;
                }
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Survival", "Resources", "Survival"));
        }
    }
}
