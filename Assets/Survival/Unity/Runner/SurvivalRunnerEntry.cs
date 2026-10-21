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
    /// additively. Play stays loaded; it is only hidden once the runner scene (with its own camera) is in, and shown
    /// again before the runner unloads, so a camera is rendering every frame. Every finished run, retries included,
    /// is paid once on the way back through the village celebration (wallet + hero shards, saved with the heroes).
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

        /// <summary>The one entry point: open the first runner level.</summary>
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

            StartCoroutine(RunRunner("runner.thornwood_road"));
        }

        private IEnumerator RunRunner(string levelId)
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
            _campaign?.SaveNow();
            var playScene = gameObject.scene;
            var render = Capture();
            RunnerHandoff.Begin(levelId);
            var load = SceneManager.LoadSceneAsync(RunnerLevelView.SceneName, LoadSceneMode.Additive);
            while (load != null && !load.isDone)
            {
                yield return null;
            }

            var runner = SceneManager.GetSceneByName(RunnerLevelView.SceneName);
            var hidden = new List<Behaviour>();
            if (runner.IsValid() && runner.isLoaded)
            {
                // The runner's camera exists now; hide Play in the same frame.
                hidden = Hide(playScene);
                SceneManager.SetActiveScene(runner);
            }

            while (!RunnerHandoff.Done && runner.IsValid() && runner.isLoaded)
            {
                yield return null;
            }

            // Play back on first, then the runner goes.
            foreach (var b in hidden)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }

            if (playScene.IsValid())
            {
                SceneManager.SetActiveScene(playScene);
            }

            if (runner.IsValid() && runner.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(runner);
                while (unload != null && !unload.isDone)
                {
                    yield return null;
                }
            }

            render();
            _busy = false;
            Pay(RunnerHandoff.Take());
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
