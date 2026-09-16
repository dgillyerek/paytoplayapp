using System;
using System.IO;
using Survival.Domain.Catalog;
using Survival.Domain.Flavor;
using Survival.Domain.Session;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>Theme A flavor entry: splash keep still, then Play world-map shell.</summary>
    public sealed class SurvivalBootstrap : MonoBehaviour
    {
        [SerializeField] private bool skipSplash;

        public SurvivalSession Session { get; private set; } = null!;

        private bool _booted;
        private GameObject? _splashRoot;

        private void Awake() => TryBoot();

        private void OnEnable() => TryBoot();

        private void Start() => TryBoot();

        private void Update()
        {
            if (!_booted)
            {
                TryBoot();
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
            var flavor = AppFlavorConfig.FantasyKingdomA;
            var packRoot = SurvivalArt.ResolvePackRoot(flavor);
            var pack = ThemePackBinder.Parse(
                File.ReadAllText(Path.Combine(packRoot, "pack.json")),
                File.ReadAllText(Path.Combine(packRoot, "strings", "en.json")),
                File.ReadAllText(Path.Combine(packRoot, "map", "node_pins.json")),
                File.ReadAllText(Path.Combine(packRoot, "ui", "hud_layout.json")));
            var catalogDir = ResolveCatalogDir();
            var catalog = CatalogLoader.LoadFromDirectory(catalogDir);
            Session = new SurvivalSession(flavor, pack, catalog);
            SurvivalArt.EnsureLoaded(flavor, pack);

            if (skipSplash)
            {
                ShowPlay();
                return;
            }

            ShowSplash();
        }

        private void ShowSplash()
        {
            SurvivalVisuals.EnsureEventSystem();
            var root = SurvivalVisuals.Canvas(transform, "SurvivalSplashCanvas", 80);
            _splashRoot = root.gameObject;
            var img = SurvivalVisuals.Image(root, "Splash", Color.white, SurvivalArt.Get("splash") ?? SurvivalArt.Get("flavor.splash"));
            SurvivalVisuals.Stretch(img.rectTransform);
            img.preserveAspect = true;
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(AdvanceFromSplash);
        }

        private void AdvanceFromSplash()
        {
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
                if (File.Exists(Path.Combine(fromAssets, CatalogLoader.WorldFileName)))
                {
                    return fromAssets;
                }
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Survival", "Resources", "Survival"));
        }
    }
}
