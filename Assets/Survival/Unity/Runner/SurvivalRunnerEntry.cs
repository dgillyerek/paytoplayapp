#nullable enable
using System.Collections;
using System.Collections.Generic;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Play's way into the runner: a painted "Runner" event button (hud slot runner_button) that loads RunnerLevel
    /// additively. Both ways go through the shared painted loading screen (#76's BattleLoadingScreen). Its floor
    /// camera renders the whole hand-off, so Display 1 never shows "No cameras rendering". Play is hidden only once
    /// the screen covers it, and the screen only fades once the destination scene's camera has drawn a frame. Every
    /// finished run, retries included, is paid once on the way back through the village celebration (wallet + hero
    /// shards, saved with the heroes).
    /// </summary>
    public sealed class SurvivalRunnerEntry : MonoBehaviour
    {
        public const string ButtonName = "RunnerButton";
        public const string Slot = "runner_button";

        private SurvivalPlayView? _play;
        private SurvivalHookView? _hook;
        private SurvivalCampaignView? _campaign;
        private ThemePackBinder? _pack;
        private bool _busy;
        private int _trips;

        public RectTransform? Button { get; private set; }

        public void Build(SurvivalPlayView play, ThemePackBinder pack, SurvivalHookView? hook, SurvivalCampaignView? campaign)
        {
            if (Button != null || play.Root == null)
            {
                return;
            }

            _play = play;
            _pack = pack;
            _hook = hook;
            _campaign = campaign;
            var btn = SurvivalPaintedButton.Create(play.Root, ButtonName, RunnerArtKeys.EventIcon, pack.StringOr("runner.button", "Runner"), out _, out _, 22);
            Button = btn.GetComponent<RectTransform>();
            if (pack.Hud.TryGetValue(Slot, out var slot))
            {
                SurvivalVisuals.Place(Button, slot);
            }
            else
            {
                Button.anchorMin = new Vector2(0.8f, 0.36f);
                Button.anchorMax = new Vector2(0.975f, 0.45f);
                Button.offsetMin = Button.offsetMax = Vector2.zero;
            }

            btn.onClick.AddListener(OnRunnerTapped);
        }

        /// <summary>The one entry point: open the highest runner level the save has open (the result screen lists the rest).</summary>
        public void OnRunnerTapped()
        {
            if (_busy)
            {
                return;
            }

            if (_hook != null && Button != null)
            {
                _hook.Juice(Button);
            }

            var unlocked = _campaign != null && _campaign.Campaign != null ? _campaign.Campaign.RunnerUnlocked : 1;
            var (catalog, level) = LevelFor(unlocked);
            StartCoroutine(RunRunner(catalog, level, level?.Id ?? "runner.thornwood_road", unlocked));
        }

        /// <summary>Runner level <paramref name="unlocked"/> (clamped to the levels there are); none when the catalog is unreadable (Thornwood Road opens).</summary>
        private static (RunnerCatalog? Catalog, RunnerLevelDef? Level) LevelFor(int unlocked)
        {
            try
            {
                var path = System.IO.Path.Combine(SurvivalBootstrap.ResolveCatalogDir(), RunnerCatalog.FileName);
                var catalog = RunnerCatalog.Parse(PlayerAssets.ReadAllText(path));
                return (catalog, catalog.ByNumber(unlocked));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Runner: level list unreadable, opening Level 1. " + ex.Message);
                return (null, null);
            }
        }

        private string S(string key, string fallback) => _pack != null ? _pack.StringOr(key, fallback) : fallback;

        private string LevelName(RunnerLevelDef? level) =>
            level == null ? S("runner.level.thornwood_road", "Thornwood Road") : S(level.Title, level.DisplayName.Length > 0 ? level.DisplayName : "Runner");

        private string Tip(string levelId)
        {
            var tips = new List<string>();
            for (var i = 1; i <= RunnerLoading.TipCount; i++)
            {
                var t = S(RunnerLoading.TipPrefix + i, "");
                if (t.Length > 0)
                {
                    tips.Add(t);
                }
            }

            if (tips.Count == 0)
            {
                tips.Add("Steer through boxes to grow your squad.");
            }

            return Survival.Domain.Campaign.BattleTransition.Tip(tips, Survival.Domain.Campaign.BattleTransition.StableSeed(levelId) + _trips);
        }

        private IEnumerator RunRunner(RunnerCatalog? catalog, RunnerLevelDef? level, string levelId, int unlocked)
        {
            if (!Application.CanStreamedLevelBeLoaded(RunnerLevelView.SceneName))
            {
                Debug.LogWarning("Runner: scene '" + RunnerLevelView.SceneName + "' is not in Build Settings (Survival > Use Theme A Flavor adds it).");
                if (_hook != null && Button != null)
                {
                    _hook.Say(Button, "Runner scene missing from Build Settings", new Color(1f, 0.55f, 0.45f, 1f));
                }

                yield break;
            }

            _busy = true;
            _trips++;
            _campaign?.SaveNow();
            var playScene = gameObject.scene;
            var levelName = LevelName(level);
            var tip = Tip(levelId);
            var vs = S("battle.loading.vs", "VS");
            var info = catalog != null && level != null
                ? RunnerLoading.Into(catalog, level, levelName, S(RunnerLoading.IntoKey, "Onto the road!"), tip)
                : RunnerLoading.Back(S(RunnerLoading.IntoKey, "Onto the road!"), levelName, tip);

            // The painted loading screen's floor camera keeps Display 1 rendering while Play's cameras are off and
            // the runner's camera doesn't exist yet. Play is only hidden once the screen covers it. Everything after
            // the screen goes up runs under try / finally: if any step throws, the village comes back and the
            // screen is released instead of freezing the game under it (it also clears itself after
            // BattleTransition.ShowLimitSeconds whatever happens).
            var into = BattleLoadingScreen.Show(info, vs);
            BattleLoadingScreen? back = null;
            var hidden = new List<Behaviour>();
            System.Action? render = null;
            var runner = default(Scene);
            var finished = false;
            try
            {
                yield return into.WaitCovered();
                render = Capture();
                hidden = Hide(playScene);
                RunnerHandoff.Begin(levelId, unlocked);
                var load = SceneManager.LoadSceneAsync(RunnerLevelView.SceneName, LoadSceneMode.Additive);
                if (load != null)
                {
                    // Scene activation must not be held back (nothing else may leave it off).
                    load.allowSceneActivation = true;
                }

                while (load != null && !load.isDone)
                {
                    into.Progress(load.progress);
                    yield return null;
                }

                into.Progress(1f);
                runner = SceneManager.GetSceneByName(RunnerLevelView.SceneName);
                if (!runner.IsValid() || !runner.isLoaded)
                {
                    Debug.LogError("Runner: scene '" + RunnerLevelView.SceneName + "' did not load; back to the village.");
                    Restore(playScene, hidden, render);
                    into.RevealOnFirstFrameOf(playScene);
                    RunnerHandoff.Take();
                    finished = true;
                    yield break;
                }

                SceneManager.SetActiveScene(runner);
                into.RevealOnFirstFrameOf(runner);

                while (!RunnerHandoff.Done && runner.IsValid() && runner.isLoaded)
                {
                    yield return null;
                }

                // The way back: cover the runner, unload it, then Play's cameras come back and the screen fades on
                // their first frame.
                var results = new List<RunnerResult>();
                foreach (var r in RunnerHandoff.Runs)
                {
                    results.Add(r.Result);
                }

                var headlineKey = RunnerLoading.BackHeadlineKey(results);
                var headline = headlineKey == RunnerLoading.VictoryKey ? S(headlineKey, "Victory!") : headlineKey == RunnerLoading.DefeatKey ? S(headlineKey, "Defeat") : S(headlineKey, "Back to the village");
                back = BattleLoadingScreen.Show(RunnerLoading.Back(headline, levelName, Tip(levelId + ".back")), vs);
                yield return back.WaitCovered();
                if (playScene.IsValid())
                {
                    SceneManager.SetActiveScene(playScene);
                }

                if (runner.IsValid() && runner.isLoaded)
                {
                    var unload = SceneManager.UnloadSceneAsync(runner);
                    while (unload != null && !unload.isDone)
                    {
                        back.Progress(unload.progress);
                        yield return null;
                    }
                }

                back.Progress(1f);
                Restore(playScene, hidden, render);
                back.RevealOnFirstFrameOf(playScene);
                finished = true;
            }
            finally
            {
                if (!finished)
                {
                    // A step threw: put the village back under the screen and let the screen go.
                    Debug.LogError("Runner: the trip to the runner failed part-way; bringing the village back.");
                    if (runner.IsValid() && runner.isLoaded)
                    {
                        SceneManager.UnloadSceneAsync(runner);
                    }

                    Restore(playScene, hidden, render);
                    if (into != null)
                    {
                        into.Release("runner trip failed");
                    }

                    if (back != null)
                    {
                        back.Release("runner trip failed");
                    }

                    // Runs already banked still pay.
                    try
                    {
                        Pay(RunnerHandoff.Take());
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                _busy = false;
            }

            Pay(RunnerHandoff.Take());
        }

        /// <summary>Play's cameras, canvases and listeners back on, its render settings back, and it is the active scene again.</summary>
        private static void Restore(Scene playScene, List<Behaviour> hidden, System.Action? render)
        {
            render?.Invoke();
            Show(hidden);
            hidden.Clear();
            if (playScene.IsValid() && playScene.isLoaded)
            {
                SceneManager.SetActiveScene(playScene);
            }
        }

        private void Pay(List<(RunnerResult Result, Survival.Domain.Campaign.RewardBundle Bundle)> runs)
        {
            if (runs.Count == 0)
            {
                return;
            }

            var bundles = new List<Survival.Domain.Campaign.RewardBundle>();
            foreach (var r in runs)
            {
                bundles.Add(r.Bundle);
            }

            var rows = RunnerRewards.Rows(bundles);
            void Grant()
            {
                var campaign = _campaign != null ? _campaign.Campaign : null;
                if (campaign == null)
                {
                    return;
                }

                foreach (var b in bundles)
                {
                    RunnerRewards.Grant(b, campaign);
                }

                _campaign!.SaveNow();
            }

            var title = _pack != null ? _pack.StringOr("runner.rewards_title", "Runner rewards") : "Runner rewards";
            if (_hook != null && rows.Count > 0)
            {
                _hook.Celebrate(title, rows, Grant);
            }
            else
            {
                Grant();
            }
        }

        private static void Show(List<Behaviour> hidden)
        {
            foreach (var b in hidden)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }
        }

        private static List<Behaviour> Hide(Scene scene)
        {
            var hidden = new List<Behaviour>();
            if (!scene.IsValid())
            {
                return hidden;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var c in root.GetComponentsInChildren<Canvas>(false))
                {
                    if (c.enabled && c.isRootCanvas)
                    {
                        c.enabled = false;
                        hidden.Add(c);
                    }
                }

                foreach (var cam in root.GetComponentsInChildren<Camera>(false))
                {
                    if (cam.enabled)
                    {
                        cam.enabled = false;
                        hidden.Add(cam);
                    }
                }

                foreach (var ear in root.GetComponentsInChildren<AudioListener>(false))
                {
                    if (ear.enabled)
                    {
                        ear.enabled = false;
                        hidden.Add(ear);
                    }
                }
            }

            return hidden;
        }

        /// <summary>The runner sets its own ambient light and fog; returns a call that puts Play's back.</summary>
        private static System.Action Capture()
        {
            var mode = RenderSettings.ambientMode;
            var ambient = RenderSettings.ambientLight;
            var sky = RenderSettings.ambientSkyColor;
            var equator = RenderSettings.ambientEquatorColor;
            var ground = RenderSettings.ambientGroundColor;
            var fog = RenderSettings.fog;
            var fogMode = RenderSettings.fogMode;
            var fogColor = RenderSettings.fogColor;
            var fogStart = RenderSettings.fogStartDistance;
            var fogEnd = RenderSettings.fogEndDistance;
            var skybox = RenderSettings.skybox;
            return () =>
            {
                RenderSettings.ambientMode = mode;
                RenderSettings.ambientLight = ambient;
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
                RenderSettings.fog = fog;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
                RenderSettings.skybox = skybox;
            };
        }
    }
}
