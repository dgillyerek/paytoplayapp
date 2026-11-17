#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Ids;
using Survival.Domain.Layout;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.Theme;
using Survival.Domain.View;
using Survival.Domain.World;
using UnityEngine;
using Survival.Unity.Kids;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>Split out of <see cref="SurvivalCampaignView"/>; draws into the view's overlay and uses its helpers.</summary>
    internal sealed class BattleFlowScreen
    {
        private readonly SurvivalCampaignView _v;
        private const int LoadingTipCount = 6;
        private readonly List<string> _lastLineupHeroes = new List<string>();
        private int _battleTrips;

        internal BattleFlowScreen(SurvivalCampaignView view)
        {
            _v = view;
        }

        // ---------- Battle Setup ----------

        internal void OpenSetup(StageDef? stage)
        {
            if (_v.Campaign == null || _v._overlay == null || stage == null || _v._inBattle)
            {
                return;
            }

            var panel = _v.OpenScreen(SurvivalCampaignView.SetupScreenName, _v.S("campaign.setup.title", "BATTLE"));
            var setup = _v.Campaign.Setup(stage);
            _v._setup = setup;

            // Stage strip, or the World camp's name plate.
            if (stage.CampId != null)
            {
                var plate = HookSkin.Panel(panel, "CampPlate", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
                HookSkin.Box(plate.rectTransform, 0.06f, 0.865f, 0.94f, 0.935f);
                var t = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "T", _v.Fmt("campaign.setup.camp", "World camp: {0}", stage.Name), 28, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.5f);
                t.fontStyle = FontStyle.Bold;
                SurvivalVisuals.Stretch(t.rectTransform);
            }
            else
            {
                _stripChapter = stage.Chapter;
                var strip = SurvivalVisuals.Image(panel, "StageStrip", new Color(0f, 0f, 0f, 0f)).rectTransform;
                strip.GetComponent<Image>().raycastTarget = false;
                HookSkin.Box(strip, 0f, 0.858f, 1f, 0.948f);
                BuildStageStrip(strip, stage);
            }

            // Bounty strip.
            var bounty = HookSkin.Panel(panel, "Bounty", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            HookSkin.Box(bounty.rectTransform, 0.04f, 0.795f, 0.96f, 0.852f);
            var bountyText = HookSkin.Outlined(SurvivalVisuals.Text(bounty.transform, "T", "", 22, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.5f);
            HookSkin.Box(bountyText.rectTransform, 0.03f, 0f, 0.72f, 1f);
            var claimText = SurvivalCampaignView.SheetButton(bounty.rectTransform, "Claim", HookArtKeys.ButtonGreen, SurvivalCampaignView.FreeGreen, 0.74f, 0.1f, 0.99f, 0.9f, 20, out var claimBtn);
            claimText.text = _v.S("hook.claim", "Claim");
            var claimRt = claimBtn.GetComponent<RectTransform>();
            _v.Wire(claimBtn, () =>
            {
                var b = _v.Campaign.CurrentBounty;
                if (b == null || !_v.Campaign.BountyReady)
                {
                    return;
                }

                var rows = new Dictionary<string, int>();
                foreach (var pair in b.Rewards)
                {
                    rows[pair.Key] = pair.Value;
                }

                _v._hook?.Sparks(claimRt, SurvivalCampaignView.FreeGreen, 14);
                if (_v._hook != null)
                {
                    _v._hook.Celebrate(_v.S("campaign.bounty_done", "Bounty complete!"), rows, () => _v.Campaign.ClaimBounty());
                }
                else
                {
                    _v.Campaign.ClaimBounty();
                }

                _v.SaveNow();
                _v._refresh?.Invoke();
            });

            // Lanes: villains on top, heroes below.
            var n = setup.LaneCount;
            var refreshers = new List<Action>();
            var vs = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Vs", "VS", 56, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 3f);
            vs.fontStyle = FontStyle.Bold;
            HookSkin.Box(vs.rectTransform, 0.3f, 0.528f, 0.7f, 0.578f);
            for (var lane = 0; lane < n; lane++)
            {
                var cx = 0.5f + (lane - ((n - 1) * 0.5f)) * 0.315f;
                var x0 = cx - 0.135f;
                var x1 = cx + 0.135f;
                var enemy = setup.EnemyAt(lane);
                var edef = enemy != null ? _v.Campaign.Catalog.Hero(enemy.HeroId) : null;
                if (edef != null && enemy != null)
                {
                    _v.Portrait(panel, edef, x0, 0.62f, x1, 0.62f + 0.27f * SurvivalCampaignView.PanelAspect, out _);
                    var es = setup.EnemyStats(lane);
                    var el = HookSkin.Outlined(SurvivalVisuals.Text(panel, "EnemyLabel" + lane, _v.HeroName(edef.Id) + "  Lv " + enemy.Level + "\n\u2694 " + ChipWallet.FormatGrouped(es.Power), 21, TextAnchor.UpperCenter, new Color(1f, 0.78f, 0.82f, 1f)), 1.5f);
                    HookSkin.Box(el.rectTransform, x0 - 0.02f, 0.578f, x1 + 0.02f, 0.62f);
                }

                var laneTag = HookSkin.Outlined(SurvivalVisuals.Text(panel, "LaneTag" + lane, _v.Fmt("campaign.lane", "LANE {0}", lane + 1), 18, TextAnchor.MiddleCenter, SurvivalVisuals.Mute), 1.2f);
                HookSkin.Box(laneTag.rectTransform, x0, 0.515f, x1, 0.535f);
                refreshers.Add(HeroSlot(panel, lane, x0, x1));
            }

            // Power comparison.
            var mine = HookSkin.Outlined(SurvivalVisuals.Text(panel, "MyPower", "", 26, TextAnchor.MiddleLeft, SurvivalCampaignView.HeroGlow), 1.5f);
            mine.fontStyle = FontStyle.Bold;
            HookSkin.Box(mine.rectTransform, 0.05f, 0.262f, 0.42f, 0.312f);
            var theirs = HookSkin.Outlined(SurvivalVisuals.Text(panel, "EnemyPower", "", 26, TextAnchor.MiddleRight, SurvivalCampaignView.VillainGlow), 1.5f);
            theirs.fontStyle = FontStyle.Bold;
            HookSkin.Box(theirs.rectTransform, 0.58f, 0.262f, 0.95f, 0.312f);
            var odds = SurvivalVisuals.Image(panel, "Odds", SurvivalCampaignView.FreeGreen);
            HookSkin.Box(odds.rectTransform, 0.40f, 0.266f, 0.60f, 0.308f);
            var oddsText = HookSkin.Outlined(SurvivalVisuals.Text(odds.transform, "T", "", 22, TextAnchor.MiddleCenter, Color.white), 1.5f);
            oddsText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(oddsText.rectTransform);
            var balance = SurvivalVisuals.Image(panel, "Balance", SurvivalCampaignView.VillainFrame);
            HookSkin.Box(balance.rectTransform, 0.05f, 0.246f, 0.95f, 0.258f);
            var balanceFill = SurvivalVisuals.Image(balance.transform, "Fill", SurvivalCampaignView.HeroGlow);

            // Hero pool.
            var poolLabel = HookSkin.Outlined(SurvivalVisuals.Text(panel, "PoolLabel", _v.S("campaign.pool", "YOUR HEROES \u00b7 tap to place"), 20, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
            HookSkin.Box(poolLabel.rectTransform, 0.05f, 0.205f, 0.70f, 0.235f);
            var autoText = SurvivalCampaignView.SheetButton(panel, "Auto", HookArtKeys.ButtonBlue, SurvivalCampaignView.HeroFrame, 0.74f, 0.200f, 0.95f, 0.240f, 20, out var autoBtn);
            autoText.text = _v.S("campaign.auto", "AUTO");
            _v.Wire(autoBtn, () => setup.AutoFill());
            var owned = new List<string>(_v.Campaign.Roster.OwnedIds());
            for (var i = 0; i < owned.Count && i < 6; i++)
            {
                var px = 0.04f + i * 0.155f;
                refreshers.Add(PoolCard(panel, owned[i], px, 0.10f, px + 0.145f, 0.10f + 0.145f * SurvivalCampaignView.PanelAspect));
            }

            var close = SurvivalCampaignView.SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.04f, 0.022f, 0.32f, 0.085f, 24, out var closeBtn);
            close.text = _v.S("hook.close", "Close");
            _v.Wire(closeBtn, _v.CloseScreen);
            var fightText = SurvivalCampaignView.SheetButton(panel, "Fight", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.36f, 0.018f, 0.96f, 0.090f, 40, out var fightBtn);
            fightText.text = _v.S("campaign.fight", "FIGHT!");
            var fightImg = fightBtn.GetComponent<Image>();
            var fightRt = fightBtn.GetComponent<RectTransform>();
            _v.Wire(fightBtn, () =>
            {
                var check = setup.CanFight;
                if (check != SetupCheck.Ok)
                {
                    _v._hook?.Say(fightRt, check == SetupCheck.NoHeroes ? _v.S("campaign.need_heroes", "Place at least one hero") : _v.S("campaign.stage_locked", "Clear the stage before first"), SurvivalCampaignView.Warn);
                    return;
                }

                _v.StartCoroutine(RunBattle(setup.Build()));
            });

            _v._refresh = () =>
            {
                var b = _v.Campaign.CurrentBounty;
                bountyText.text = b == null
                    ? _v.S("campaign.bounty_all", "All bounties claimed!")
                    : _v.Fmt("campaign.bounty", "BOUNTY: {0}  {1}/{2}", BountyName(b), _v.Campaign.BountyProgress(b), b.Count);
                claimBtn.gameObject.SetActive(_v.Campaign.BountyReady);
                mine.text = _v.Fmt("campaign.my_power", "YOU \u2694 {0}", ChipWallet.FormatGrouped(setup.HeroPower));
                theirs.text = _v.Fmt("campaign.enemy_power", "{0} \u2694 FOES", ChipWallet.FormatGrouped(setup.EnemyPower));
                var o = setup.Odds;
                oddsText.text = OddsName(o);
                odds.color = OddsColor(o);
                var total = Mathf.Max(1, setup.HeroPower + setup.EnemyPower);
                HookSkin.Box(balanceFill.rectTransform, 0f, 0f, setup.HeroPower / (float)total, 1f);
                var ok = setup.CanFight == SetupCheck.Ok;
                HookSkin.SetButton(fightImg, HookArtKeys.ButtonGold, ok ? SurvivalVisuals.MineFill : SurvivalCampaignView.Disabled, ok, 0.5f);
                fightText.color = ok ? HookSkin.InkFor(HookArtKeys.ButtonGold) : SurvivalVisuals.Cream;
                foreach (var r in refreshers)
                {
                    r();
                }
            };
            setup.Changed += () =>
            {
                if (ReferenceEquals(_v._setup, setup))
                {
                    _v._refresh?.Invoke();
                }
            };
            _v._refresh();
        }

        /// <summary>One hero lane slot: the placed hero's framed portrait (tap to take it out) or an empty "+" slot.</summary>
        private Action HeroSlot(RectTransform panel, int lane, float x0, float x1)
        {
            var y0 = 0.345f;
            var y1 = y0 + 0.27f * SurvivalCampaignView.PanelAspect;
            var btn = SurvivalVisuals.Button(panel, "Lane" + lane, new Color(1f, 1f, 1f, 0f));
            btn.transition = Selectable.Transition.None;
            var rt = btn.GetComponent<RectTransform>();
            HookSkin.Box(rt, x0, y0, x1, y1);
            var empty = SurvivalVisuals.Image(rt, "Empty", new Color(SurvivalCampaignView.HeroFrame.r, SurvivalCampaignView.HeroFrame.g, SurvivalCampaignView.HeroFrame.b, 0.55f));
            SurvivalVisuals.Stretch(empty.rectTransform);
            var ring = SurvivalVisuals.Image(empty.transform, "Line", new Color(SurvivalCampaignView.HeroGlow.r, SurvivalCampaignView.HeroGlow.g, SurvivalCampaignView.HeroGlow.b, 0.35f));
            SurvivalVisuals.Stretch(ring.rectTransform);
            ring.rectTransform.offsetMin = new Vector2(8f, 8f);
            ring.rectTransform.offsetMax = new Vector2(-8f, -8f);
            var plus = HookSkin.Outlined(SurvivalVisuals.Text(empty.transform, "T", "+\n" + _v.S("campaign.slot_empty", "Tap a hero"), 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            SurvivalVisuals.Stretch(plus.rectTransform);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(panel, "HeroLabel" + lane, "", 21, TextAnchor.UpperCenter, new Color(0.78f, 0.88f, 1f, 1f)), 1.5f);
            HookSkin.Box(label.rectTransform, x0 - 0.02f, 0.30f, x1 + 0.02f, y0);
            RectTransform? shown = null;
            var shownId = "";
            _v.Wire(btn, () =>
            {
                if (_v._setup != null && _v._setup.HeroAt(lane) != null)
                {
                    _v._setup.Clear(lane);
                }
            });

            return () =>
            {
                var id = _v._setup?.HeroAt(lane) ?? "";
                if (id != shownId)
                {
                    if (shown != null)
                    {
                        UnityEngine.Object.Destroy(shown.gameObject);
                        shown = null;
                    }

                    shownId = id;
                    var def = id.Length > 0 ? _v.Campaign!.Catalog.Hero(id) : null;
                    if (def != null)
                    {
                        shown = _v.Portrait(rt, def, 0f, 0f, 1f, 1f, out var art);
                        art.raycastTarget = false;
                        _v.StartCoroutine(HookSkin.PopIn(shown));
                        _v._hook?.Sparks(rt, SurvivalCampaignView.HeroGlow, 8);
                    }
                }

                empty.gameObject.SetActive(id.Length == 0);
                if (id.Length > 0)
                {
                    var p = _v.Campaign!.Roster.Get(id)!;
                    label.text = _v.HeroName(id) + "  Lv " + p.Level + "\n\u2694 " + ChipWallet.FormatGrouped(_v.Campaign.Roster.Stats(id).Power);
                }
                else
                {
                    label.text = "";
                }
            };
        }

        private Action PoolCard(RectTransform panel, string heroId, float x0, float y0, float x1, float y1)
        {
            var def = _v.Campaign!.Catalog.Hero(heroId)!;
            var btn = SurvivalVisuals.Button(panel, "Pool_" + heroId, new Color(1f, 1f, 1f, 0f));
            btn.transition = Selectable.Transition.None;
            var rt = btn.GetComponent<RectTransform>();
            HookSkin.Box(rt, x0, y0, x1, y1);
            _v.Portrait(rt, def, 0f, 0f, 1f, 1f, out var art);
            var tag = SurvivalVisuals.Circle(rt, "Lane", SurvivalVisuals.MineFill);
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0.84f, 0.84f);
            tag.rectTransform.sizeDelta = new Vector2(44f, 44f);
            var tagText = SurvivalVisuals.Text(tag.transform, "T", "", 24, TextAnchor.MiddleCenter, HookSkin.InkFor(HookArtKeys.ButtonGold));
            tagText.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(tagText.rectTransform);
            var lv = HookSkin.Outlined(SurvivalVisuals.Text(rt, "Lv", "", 18, TextAnchor.LowerCenter, SurvivalVisuals.Cream), 1.5f);
            HookSkin.Box(lv.rectTransform, 0f, 0.02f, 1f, 0.3f);
            _v.Wire(btn, () => _v._setup?.Toggle(heroId));
            return () =>
            {
                var lane = _v._setup?.LaneOf(heroId) ?? -1;
                tag.gameObject.SetActive(lane >= 0);
                tagText.text = (lane + 1).ToString(CultureInfo.InvariantCulture);
                art.color = lane >= 0 ? new Color(0.55f, 0.55f, 0.6f, 1f) : Color.white;
                lv.text = "Lv " + (_v.Campaign.Roster.Get(heroId)?.Level ?? 1);
            };
        }

        // ---------- the battle round trip ----------

        private IEnumerator RunBattle(BattleLineup lineup)
        {
            if (_v._inBattle || _v.Campaign == null)
            {
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(SurvivalCampaignView.BattleSceneName))
            {
                Debug.LogWarning("Battle: scene '" + SurvivalCampaignView.BattleSceneName + "' is not in Build Settings.");
                if (_v._screen != null && _v._screen.transform is RectTransform screenRt)
                {
                    _v._hook?.Say(screenRt, _v.S("campaign.no_scene", "BattleLocked scene missing from Build Settings"), SurvivalCampaignView.Warn);
                }

                yield break;
            }

            _v._inBattle = true;
            _lastLineupHeroes.Clear();
            foreach (var h in lineup.Heroes)
            {
                _lastLineupHeroes.Add(h.CharacterId);
            }

            _v.SaveNow();
            _v.CloseSheet();
            _v.CloseScreen();
            var stage = lineup.Stage;
            var playScene = _v.gameObject.scene;
            _battleTrips++;

            // Root cause of "Display 1 No cameras rendering": Play's cameras went off before BattleLocked's camera
            // existed (the additive load spans several frames), and on the way back BattleLocked's camera was
            // unloaded before Play's came back on. The loading screen's floor camera lives outside both scenes and
            // renders the whole time; Play is only hidden once the screen covers the view, and the screen only
            // fades once the destination camera has drawn a frame.
            var into = BattleLoadingScreen.Show(BattleLoadingInfo.Into(lineup, _v.S("battle.loading.into", "To battle!"), LoadingTip(stage.Id)), _v.S("battle.loading.vs", "VS"));
            yield return into.WaitCovered();
            var hidden = HidePlay(playScene);
            var render = RenderState.Capture();
            BattleReport? report = null;
            void OnDone(BattleReport r) => report = r;
            BattleHandoff.Completed += OnDone;
            BattleHandoff.Begin(lineup);
            var load = SceneManager.LoadSceneAsync(SurvivalCampaignView.BattleSceneName, LoadSceneMode.Additive);
            while (load != null && !load.isDone)
            {
                into.Progress(load.progress);
                yield return null;
            }

            into.Progress(1f);
            var battle = SceneManager.GetSceneByName(SurvivalCampaignView.BattleSceneName);
            if (battle.IsValid() && battle.isLoaded)
            {
                SceneManager.SetActiveScene(battle);
                into.RevealOnFirstFrameOf(battle);
            }
            else
            {
                // Load failed: bring Play straight back under the screen.
                render.Restore();
                ShowPlay(hidden);
                into.RevealOnFirstFrameOf(playScene);
            }

            while (report == null && battle.IsValid() && battle.isLoaded)
            {
                yield return null;
            }

            BattleHandoff.Completed -= OnDone;
            var finished = report ?? BattleHandoff.Result;
            var back = BattleLoadingScreen.Show(BattleLoadingInfo.Back(ReturnHeadline(finished), stage.Name, LoadingTip(stage.Id)), _v.S("battle.loading.vs", "VS"));
            yield return back.WaitCovered();
            if (playScene.IsValid())
            {
                SceneManager.SetActiveScene(playScene);
            }

            if (battle.IsValid() && battle.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(battle);
                while (unload != null && !unload.isDone)
                {
                    back.Progress(unload.progress);
                    yield return null;
                }
            }

            back.Progress(1f);
            render.Restore();
            ShowPlay(hidden);
            back.RevealOnFirstFrameOf(playScene);
            var taken = BattleHandoff.TakeResult();
            _v._inBattle = false;
            var result = report ?? taken ?? new BattleReport(stage.Id, stage.CampId, false, 0, 0, 0, false, true);
            ShowResult(result, stage);
            foreach (var (title, rows) in _v._deferred)
            {
                _v._hook?.Celebrate(title, rows);
            }

            _v._deferred.Clear();
        }

        private string LoadingTip(string stageId)
        {
            var tips = new List<string>();
            for (var i = 1; i <= LoadingTipCount; i++)
            {
                var t = _v.S("battle.tip." + i, "");
                if (t.Length > 0)
                {
                    tips.Add(t);
                }
            }

            if (tips.Count == 0)
            {
                tips.Add("Level up heroes in the Heroes screen to raise their power.");
            }

            return BattleTransition.Tip(tips, BattleTransition.StableSeed(stageId) + _battleTrips);
        }

        private string ReturnHeadline(BattleReport? r) =>
            r == null ? _v.S("battle.loading.back", "Back to the village")
            : r.Retreated ? _v.S("battle.loading.retreat", "Retreat!")
            : r.Won ? _v.S("battle.loading.victory", "Victory!")
            : _v.S("battle.loading.defeat", "Defeat");

        /// <summary>Play stays loaded (village, world and timers keep running); its canvases, cameras and listeners switch off.</summary>
        private static List<Behaviour> HidePlay(Scene scene)
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

        private static void ShowPlay(List<Behaviour> hidden)
        {
            foreach (var b in hidden)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }
        }

        /// <summary>The battle sets ambient light and fog at boot; put Play's back afterwards.</summary>
        private sealed class RenderState
        {
            private UnityEngine.Rendering.AmbientMode _ambientMode;
            private Color _ambient;
            private bool _fog;
            private FogMode _fogMode;
            private Color _fogColor;
            private float _fogStart;
            private float _fogEnd;
            private float _fogDensity;
            private Material? _skybox;

            public static RenderState Capture() => new RenderState
            {
                _ambientMode = RenderSettings.ambientMode,
                _ambient = RenderSettings.ambientLight,
                _fog = RenderSettings.fog,
                _fogMode = RenderSettings.fogMode,
                _fogColor = RenderSettings.fogColor,
                _fogStart = RenderSettings.fogStartDistance,
                _fogEnd = RenderSettings.fogEndDistance,
                _fogDensity = RenderSettings.fogDensity,
                _skybox = RenderSettings.skybox
            };

            public void Restore()
            {
                RenderSettings.ambientMode = _ambientMode;
                RenderSettings.ambientLight = _ambient;
                RenderSettings.fog = _fog;
                RenderSettings.fogMode = _fogMode;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogStartDistance = _fogStart;
                RenderSettings.fogEndDistance = _fogEnd;
                RenderSettings.fogDensity = _fogDensity;
                RenderSettings.skybox = _skybox;
            }
        }

        // ---------- result ----------

        private void ShowResult(BattleReport result, StageDef stage)
        {
            if (_v.Campaign == null || _v._overlay == null)
            {
                return;
            }

            var firstClear = result.Won && stage.CampId == null && !_v.Campaign.IsCleared(stage.Id);
            var preview = BattleRewards.For(stage, result.Won, firstClear, _v.Campaign.Catalog.LossGoldShare).AsRows();
            _v._hook?.HoldChips(preview);
            var bundle = _v.Campaign.ApplyResult(result, stage);
            var rows = bundle.AsRows();
            if (result.Won)
            {
                _v._session?.Hook?.Quests.Report("quest.objective.win_battle", stage.Id, 1);
            }

            _v.SaveNow();

            var panel = _v.OpenScreen(SurvivalCampaignView.ResultScreenName, result.Won ? _v.S("campaign.victory", "VICTORY!") : _v.S("campaign.defeat", "DEFEAT"));
            var glow = HookSkin.Glow(panel, "Glow", result.Won ? new Color(1f, 0.85f, 0.4f, 0.5f) : new Color(0.6f, 0.3f, 0.3f, 0.35f));
            HookSkin.Box(glow.rectTransform, 0.1f, 0.62f, 0.9f, 0.95f);
            for (var i = 0; i < _lastLineupHeroes.Count; i++)
            {
                var def = _v.Campaign.Catalog.Hero(_lastLineupHeroes[i]);
                if (def == null)
                {
                    continue;
                }

                var cx = 0.5f + (i - ((_lastLineupHeroes.Count - 1) * 0.5f)) * 0.27f;
                _v.Portrait(panel, def, cx - 0.12f, 0.70f, cx + 0.12f, 0.70f + 0.24f * SurvivalCampaignView.PanelAspect, out var art);
                if (!result.Won)
                {
                    art.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                }
            }

            var title = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Stage", stage.Name, 34, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 2f);
            title.fontStyle = FontStyle.Bold;
            HookSkin.Box(title.rectTransform, 0.05f, 0.635f, 0.95f, 0.685f);
            var extra = (result.TimedOut ? "  \u00b7  " + _v.S("campaign.timeout", "time up") : "") + (result.Retreated ? "  \u00b7  " + _v.S("campaign.retreated", "retreated") : "");
            var summary = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Summary", _v.Fmt("campaign.summary", "Foes defeated {0}  \u00b7  Heroes standing {1}  \u00b7  {2}", result.EnemiesDefeated, result.HeroesStanding, TimerQueue.FormatClock(result.Seconds)) + extra, 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
            HookSkin.Box(summary.rectTransform, 0.03f, 0.595f, 0.97f, 0.635f);
            if (bundle.FirstClear)
            {
                var first = SurvivalVisuals.Image(panel, "FirstClear", new Color(0.85f, 0.25f, 0.30f, 1f));
                HookSkin.TrySkin(first, HookArtKeys.RibbonRed, 0.5f);
                HookSkin.Box(first.rectTransform, 0.28f, 0.545f, 0.72f, 0.592f);
                var ft = HookSkin.Outlined(SurvivalVisuals.Text(first.transform, "T", _v.S("campaign.first_clear", "FIRST CLEAR!"), 24, TextAnchor.MiddleCenter, Color.white), 1.5f);
                ft.fontStyle = FontStyle.Bold;
                HookSkin.Box(ft.rectTransform, 0.1f, 0.2f, 0.9f, 0.95f);
            }

            var y = 0.53f;
            foreach (var pair in rows)
            {
                var row = HookSkin.Panel(panel, "Reward_" + pair.Key, HookArtKeys.Chip, SurvivalVisuals.RowFill, 0.6f);
                HookSkin.Box(row.rectTransform, 0.22f, y - 0.05f, 0.78f, y);
                var icon = HookSkin.ResIcon(row.transform, "Icon", pair.Key, SurvivalCampaignView.ShardPurple, "\u2726");
                HookSkin.Box(icon.rectTransform, 0.02f, -0.3f, 0.2f, 1.3f);
                var t = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "T", "+" + ChipWallet.FormatGrouped(pair.Value) + "  " + RowName(pair.Key), 26, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.5f);
                t.fontStyle = FontStyle.Bold;
                HookSkin.Box(t.rectTransform, 0.24f, 0f, 1f, 1f);
                y -= 0.058f;
            }

            if (rows.Count == 0)
            {
                var none = HookSkin.Outlined(SurvivalVisuals.Text(panel, "NoRewards", _v.S("campaign.no_rewards", "No rewards this time"), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Mute), 1.5f);
                HookSkin.Box(none.rectTransform, 0.1f, y - 0.05f, 0.9f, y);
                y -= 0.058f;
            }

            var b = _v.Campaign.CurrentBounty;
            if (b != null)
            {
                var ready = _v.Campaign.BountyReady;
                var bl = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Bounty", _v.Fmt("campaign.bounty", "BOUNTY: {0}  {1}/{2}", BountyName(b), _v.Campaign.BountyProgress(b), b.Count) + (ready ? "  \u2714 " + _v.S("campaign.bounty_ready", "claim it in Battle") : ""), 22, TextAnchor.MiddleCenter, ready ? SurvivalCampaignView.FreeGreen : SurvivalVisuals.Cream), 1.5f);
                HookSkin.Box(bl.rectTransform, 0.04f, y - 0.045f, 0.96f, y);
            }

            if (!result.Won)
            {
                var tip = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Tip", _v.S("campaign.tip", "Train and star up your heroes, then try again!"), 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                HookSkin.Box(tip.rectTransform, 0.04f, 0.19f, 0.96f, 0.23f);
            }

            var celebrateTitle = result.Won ? _v.S("campaign.victory", "VICTORY!") : _v.S("campaign.defeat", "DEFEAT");
            var collect = SurvivalCampaignView.SheetButton(panel, "Collect", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, result.Won ? 0.25f : 0.52f, 0.06f, result.Won ? 0.75f : 0.96f, 0.15f, 34, out var collectBtn);
            collect.text = result.Won ? _v.S("campaign.collect", "COLLECT") : _v.S("campaign.continue", "CONTINUE");
            SurvivalCampaignView.FitButtonLabel(collect, 34);
            _v.Wire(collectBtn, () =>
            {
                _v.CloseScreen();
                CelebrateOrRelease(celebrateTitle, rows);
            });
            if (!result.Won)
            {
                var up = SurvivalCampaignView.SheetButton(panel, "Upgrade", HookArtKeys.ButtonBlue, SurvivalCampaignView.HeroFrame, 0.04f, 0.06f, 0.48f, 0.15f, 28, out var upBtn);
                up.text = _v.S("campaign.upgrade", "UPGRADE HEROES");
                SurvivalCampaignView.FitButtonLabel(up, 28);
                _v.Wire(upBtn, () =>
                {
                    CelebrateOrRelease(celebrateTitle, rows);
                    _v._foesTab = false;
                    _v.OpenHeroes();
                });
            }

            _v.RefreshBadges();
        }

        /// <summary>Rewards are already in the wallet (the chips were held); the celebration counts them up.</summary>
        private void CelebrateOrRelease(string title, Dictionary<string, int> rows)
        {
            if (_v._hook == null)
            {
                return;
            }

            if (rows.Count > 0)
            {
                _v._hook.Celebrate(title, rows);
            }
            else
            {
                _v._hook.ReleaseChips(rows);
            }
        }

        /// <summary>The chapter shown in the Battle setup's stage strip (0 = Prologue). The arrows change it without rebuilding the screen.</summary>
        internal int _stripChapter;

        /// <summary>
        /// Chapter header (\u25c0 name \u00b7 Castle gate \u25b6, chapter chest with a FREE badge) over that chapter's stage buttons.
        /// Only <paramref name="strip"/> is rebuilt when the chapter changes; the lanes below stay as they are.
        /// </summary>
        internal void BuildStageStrip(RectTransform strip, StageDef current)
        {
            if (_v.Campaign == null)
            {
                return;
            }

            for (var i = strip.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(strip.GetChild(i).gameObject);
            }

            var cat = _v.Campaign.Catalog;
            var chapters = _v.Campaign.ChapterCount;
            var ch = Mathf.Clamp(_stripChapter, 0, chapters - 1);
            var def = cat.Chapter(ch);
            var stages = ch == 0 ? cat.PrologueStages : cat.StagesIn(ch);

            // Header row.
            var head = HookSkin.Panel(strip, "ChapterPlate", HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.45f);
            HookSkin.Box(head.rectTransform, 0.13f, 0.56f, 0.70f, 1f);
            var cleared = 0;
            foreach (var st in stages)
            {
                if (_v.Campaign.IsCleared(st.Id))
                {
                    cleared++;
                }
            }

            var title = ch == 0
                ? _v.Fmt("campaign.chapter.prologue", "Prologue  {0}/{1}", cleared, stages.Count)
                : _v.Fmt("campaign.chapter.title", "Ch {0} \u00b7 {1}  {2}/{3}", ch, def?.Name ?? "", cleared, stages.Count);
            var t = HookSkin.Outlined(SurvivalVisuals.Text(head.transform, "T", title, 22, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.6f);
            t.fontStyle = FontStyle.Bold;
            SurvivalVisuals.Stretch(t.rectTransform);

            void Arrow(string name, string glyph, float x0, float x1, int to)
            {
                var label = SurvivalCampaignView.SheetButton(strip, name, HookArtKeys.ButtonBlue, SurvivalCampaignView.HeroFrame, x0, 0.56f, x1, 1f, 26, out var btn);
                label.text = glyph;
                label.color = SurvivalVisuals.Cream;
                var ok = to >= 0 && to < chapters;
                if (!ok)
                {
                    btn.GetComponent<Image>().color = SurvivalCampaignView.Disabled;
                }

                _v.Wire(btn, () =>
                {
                    if (to >= 0 && to < _v.Campaign.ChapterCount)
                    {
                        _stripChapter = to;
                        BuildStageStrip(strip, current);
                    }
                });
            }

            Arrow("ChapterPrev", "\u25c0", 0.025f, 0.12f, ch - 1);
            Arrow("ChapterNext", "\u25b6", 0.71f, 0.805f, ch + 1);

            // Chapter chest: Claim (FREE) once every stage is cleared, otherwise the gate / progress.
            var ready = ch > 0 && _v.Campaign.ChestReady(ch);
            var claimed = ch > 0 && !ready && _v.Campaign.ChapterCleared(ch);
            var chestText = SurvivalCampaignView.SheetButton(strip, "ChapterChest", ready ? HookArtKeys.ButtonGreen : HookArtKeys.ButtonBlue, ready ? SurvivalCampaignView.FreeGreen : SurvivalCampaignView.HeroFrame, 0.815f, 0.56f, 0.975f, 1f, 18, out var chestBtn);
            chestText.color = SurvivalVisuals.Cream;
            chestText.text = ch == 0 ? "" : claimed ? "\u2713 " + _v.S("campaign.chest.done", "Chest") : ready ? _v.S("hook.claim", "Claim") : _v.S("campaign.chest", "Chest");
            if (ch == 0)
            {
                chestBtn.gameObject.SetActive(false);
            }
            else if (!ready)
            {
                chestBtn.GetComponent<Image>().color = SurvivalCampaignView.Disabled;
            }

            if (ready)
            {
                var ribbon = SurvivalArt.Get(HookArtKeys.BadgeFree);
                var free = ribbon != null ? SurvivalVisuals.Image(chestBtn.transform, "Free", Color.white, ribbon) : SurvivalVisuals.Image(chestBtn.transform, "Free", SurvivalCampaignView.FreeGreen);
                free.preserveAspect = true;
                free.raycastTarget = false;
                HookSkin.Box(free.rectTransform, 0.55f, 0.62f, 1.12f, 1.22f);
                var ft = HookSkin.Outlined(SurvivalVisuals.Text(free.transform, "T", _v.S("hook.free", "FREE"), 13, TextAnchor.MiddleCenter, Color.white), 1.4f);
                ft.fontStyle = FontStyle.Bold;
                ft.raycastTarget = false;
                SurvivalVisuals.Stretch(ft.rectTransform);
            }

            var chestRt = chestBtn.GetComponent<RectTransform>();
            _v.Wire(chestBtn, () =>
            {
                if (!_v.Campaign.ChestReady(ch))
                {
                    _v._hook?.Say(chestRt, _v.S("campaign.chest.locked", "Clear every stage in this chapter to open its chest"), SurvivalCampaignView.Warn);
                    return;
                }

                var rows = new Dictionary<string, int>();
                foreach (var pair in cat.Chapter(ch)!.Chest)
                {
                    rows[pair.Key] = pair.Value;
                }

                _v._hook?.Sparks(chestRt, SurvivalCampaignView.FreeGreen, 14);
                if (_v._hook != null)
                {
                    _v._hook.Celebrate(_v.S("campaign.chest.title", "Chapter chest!"), rows, () => _v.Campaign.ClaimChest(ch));
                }
                else
                {
                    _v.Campaign.ClaimChest(ch);
                }

                _v.SaveNow();
                BuildStageStrip(strip, current);
            });

            // Stage buttons for this chapter.
            var w = 0.95f / Mathf.Max(1, stages.Count);
            for (var i = 0; i < stages.Count; i++)
            {
                var s = stages[i];
                var on = s.Id == current.Id;
                var lk = _v.Campaign.Lock(s.Id);
                var open = lk == StageLock.Open;
                var key = s.Boss ? HookArtKeys.ButtonGold : HookArtKeys.ButtonBlue;
                var x0 = 0.025f + i * w;
                var label = SurvivalCampaignView.SheetButton(strip, "Stage_" + s.Id, key, s.Boss ? SurvivalVisuals.MineFill : SurvivalCampaignView.HeroFrame, x0 + 0.003f, 0f, x0 + w - 0.003f, on ? 0.54f : 0.50f, stages.Count > 6 ? 17 : 19, out var sb);
                var num = ch == 0 ? (i + 1).ToString() : s.Number.ToString();
                var mark = _v.Campaign.IsCleared(s.Id) ? " \u2713" : lk == StageLock.Castle ? "\n\u2302" + cat.CastleGate(s) : "";
                label.text = s.Boss ? (s.Number == cat.BattleCampaign?.StagesPerChapter ? "\u2620" : "\u2605") + num + mark : num + mark;
                if (ch == 0)
                {
                    label.text += "\n" + s.Name;
                }

                label.color = open ? SurvivalVisuals.Cream : SurvivalVisuals.Mute;
                if (on)
                {
                    var ring = SurvivalVisuals.Image(sb.transform, "Selected", new Color(1f, 0.86f, 0.35f, 0.9f));
                    ring.raycastTarget = false;
                    HookSkin.Box(ring.rectTransform, 0.08f, -0.10f, 0.92f, 0.0f);
                }

                if (!open)
                {
                    sb.GetComponent<Image>().color = SurvivalCampaignView.Disabled;
                }

                var target = s;
                var sbRt = sb.GetComponent<RectTransform>();
                _v.Wire(sb, () =>
                {
                    switch (_v.Campaign.Lock(target.Id))
                    {
                        case StageLock.Open:
                            OpenSetup(target);
                            break;
                        case StageLock.Castle:
                            _v._hook?.Say(sbRt, _v.Fmt("campaign.stage_castle", "Castle Lv {0} needed \u2014 upgrade it in town (speed-ups make it faster)", cat.CastleGate(target)), SurvivalCampaignView.Warn);
                            break;
                        default:
                            _v._hook?.Say(sbRt, _v.S("campaign.stage_locked", "Clear the stage before first"), SurvivalCampaignView.Warn);
                            break;
                    }
                });
            }
        }

        internal string OddsName(BattleOdds o) => o switch
        {
            BattleOdds.Easy => _v.S("campaign.odds.easy", "EASY"),
            BattleOdds.Even => _v.S("campaign.odds.even", "EVEN"),
            BattleOdds.Hard => _v.S("campaign.odds.hard", "HARD"),
            _ => _v.S("campaign.odds.very_hard", "VERY HARD")
        };

        internal static Color OddsColor(BattleOdds o) => o switch
        {
            BattleOdds.Easy => new Color(0.30f, 0.78f, 0.38f, 1f),
            BattleOdds.Even => new Color(0.86f, 0.68f, 0.22f, 1f),
            BattleOdds.Hard => new Color(0.90f, 0.42f, 0.22f, 1f),
            _ => new Color(0.80f, 0.20f, 0.24f, 1f)
        };

        internal string BountyName(BountyDef b) => b.Kind switch
        {
            "win" => _v.Fmt("campaign.bounty.win", "Win {0} battles", b.Count),
            "clear" => _v.Fmt("campaign.bounty.clear", "Clear {0}", _v.Campaign?.Catalog.Stage(b.Target)?.Name ?? b.Target),
            "hero_level" => _v.Fmt("campaign.bounty.level", "Train a hero to Lv {0}", b.Count),
            _ => b.Id
        };

        internal string RowName(string key) =>
            key.StartsWith("shard.", StringComparison.Ordinal)
                ? _v.Fmt("campaign.shards_of", "{0} shards", _v.HeroName(key.Substring(6)))
                : _v.S(key + ".name", key);
    }
}
