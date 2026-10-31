#nullable enable
using System.Collections.Generic;
using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Theme;
using Survival.Domain.View;
using Survival.Domain.World;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Village Build: the painted Build button (hud_layout build_button), the build menu (type, cost, timer, how many
    /// the Castle allows), placement (free spots glow; tap one, or it auto-places on the next free spot when the
    /// countdown runs out), the construction look, and the PlayerPrefs village save.
    /// </summary>
    public sealed partial class SurvivalHookView
    {
        private const float AutoPlaceSeconds = 10f;

        private RectTransform? _buildButton;
        private RectTransform? _placeBar;
        private Text? _placeHint;
        private Text? _placeAutoText;
        private string? _placingType;
        private float _autoLeft;
        private string? _lastSave;
        private bool _saveCleared;

        private void SetupBuild(SurvivalPlayView play)
        {
            if (_hook == null)
            {
                return;
            }

            var village = play.Village;
            var ids = new List<string>();
            if (village?.Layout != null)
            {
                foreach (var spot in village.Layout.BuildSpots)
                {
                    ids.Add(spot.Id);
                }
            }

            _hook.Build.UseSpots(ids);
            LoadVillage();
            _hook.Build.Built += OnBuilt;
            if (village != null)
            {
                village.SpotTapped += OnSpotTapped;
                if (village.Rig != null)
                {
                    // Looking around while placing restarts the auto-place countdown.
                    village.Rig.Moved += _ =>
                    {
                        if (_placingType != null)
                        {
                            _autoLeft = AutoPlaceSeconds;
                        }
                    };
                }
            }
        }

        // ---------- save ----------

        private void LoadVillage()
        {
            if (_hook == null)
            {
                return;
            }

            VillageSave.Apply(PlayerPrefs.GetString(VillageSave.PrefsKey, ""), _hook.Base, _hook.Build);

            // Timers that ran out while the game was closed finish now, before the buildings are drawn; their
            // congratulations bubbles wait on the buildings.
            _hook.Base.CompleteDueUpgrades();
            _lastSave = VillageSave.Capture(_hook.Base, _hook.Build);
        }

        private void SaveIfChanged()
        {
            if (_hook == null || _saveCleared)
            {
                return;
            }

            var json = VillageSave.Capture(_hook.Base, _hook.Build);
            if (json == _lastSave)
            {
                return;
            }

            _lastSave = json;
            PlayerPrefs.SetString(VillageSave.PrefsKey, json);
            PlayerPrefs.Save();
        }

        private void SaveVillage()
        {
            _lastSave = null;
            SaveIfChanged();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveVillage();
            }
        }

        private void OnApplicationQuit() => SaveVillage();

        private void DebugResetVillage()
        {
            PlayerPrefs.DeleteKey(VillageSave.PrefsKey);
            PlayerPrefs.Save();
            _saveCleared = true;
            Toast(new Vector2(0.5f, 0.5f), S("debug.reset_village", "Village save cleared - restart Play"), Warn);
        }

        // ---------- names ----------

        private string PlotName(string plotId)
        {
            var type = _hook?.Base.Find(plotId)?.Def.BuildType;
            return type != null ? S("build.type." + type, type) : S("hook.plot." + plotId, plotId);
        }

        // ---------- Build button + menu ----------

        private void BuildBuildButton(RectTransform root)
        {
            if (_pack == null || _play?.Village == null || !_pack.Hud.TryGetValue("build_button", out var slot))
            {
                return;
            }

            var btn = SurvivalPaintedButton.Create(root, "BuildButton", BottomNav.IconBuild, S("build.button", "Build"), out _, out _, 28);
            var rt = btn.GetComponent<RectTransform>();
            SurvivalVisuals.Place(rt, slot);
            var glow = HookSkin.Glow(rt, "Glow", new Color(1f, 0.85f, 0.4f, 0.35f));
            Box(glow.rectTransform, -0.12f, -0.10f, 1.12f, 1.25f);
            glow.transform.SetAsFirstSibling();
            _buildButton = rt;
            Wire(btn, OpenBuildMenu);
        }

        private string BuildBlockText(BuildBlock block, BuildableDef t)
        {
            var hq = _hook?.Base.HqLevel ?? 1;
            return block switch
            {
                BuildBlock.Locked => Fmt("build.locked", "Castle Lv {0}", t.NextUnlockCastle(hq) ?? 0),
                BuildBlock.MaxCount => t.NextUnlockCastle(hq) is int n ? Fmt("build.more_at", "More at Castle Lv {0}", n) : S("build.all_built", "All built"),
                BuildBlock.NoSpot => S("build.no_spot", "No free spot"),
                BuildBlock.SpotTaken => S("build.no_spot", "No free spot"),
                BuildBlock.QueueBusy => S("hook.queue_busy", "Builder busy"),
                BuildBlock.Cost => S("hook.need_more", "Not enough resources"),
                _ => S("build.button", "Build")
            };
        }

        private void OpenBuildMenu()
        {
            if (_hook == null)
            {
                return;
            }

            CancelPlacement();
            var hook = _hook;
            var types = hook.Build.Types;
            var panel = OpenSheet("BuildSheet", 0.94f, 0.66f, S("build.title", "Build"));
            if (panel == null)
            {
                return;
            }

            BalanceBar.Above(panel, hook.Wallet, BalanceBarIds.Building);
            var refreshers = new List<System.Action>();
            const float top = 0.84f;
            const float rowH = 0.165f;
            for (var i = 0; i < types.Count; i++)
            {
                var t = types[i];
                var y1 = top - i * rowH;
                var y0 = y1 - rowH + 0.012f;
                var row = HookSkin.Panel(panel, "Row_" + t.Type, HookArtKeys.Plate, SurvivalVisuals.RowFill, 0.5f);
                Box(row.rectTransform, 0.04f, y0, 0.96f, y1);
                var art = HookSkin.Art(row.transform, "Art", HookArtKeys.ForBuildType(t.Type), t.Produces != null ? ResColor(t.Produces) : SurvivalVisuals.PinBuild);
                Box(art.rectTransform, 0.01f, 0.04f, 0.22f, 0.98f);
                var name = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Name", S("build.type." + t.Type, t.Type), 28, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 1.5f);
                name.fontStyle = FontStyle.Bold;
                Box(name.rectTransform, 0.24f, 0.62f, 0.68f, 0.94f);
                var what = SurvivalVisuals.Text(row.transform, "What", S("build.desc." + t.Type, ""), 18, TextAnchor.MiddleLeft, SurvivalVisuals.Mute);
                Box(what.rectTransform, 0.24f, 0.44f, 0.70f, 0.64f);
                var cost = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Cost", "", 19, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
                Box(cost.rectTransform, 0.24f, 0.24f, 0.70f, 0.44f);
                var count = HookSkin.Outlined(SurvivalVisuals.Text(row.transform, "Count", "", 18, TextAnchor.MiddleLeft, SurvivalVisuals.Cream), 1.2f);
                Box(count.rectTransform, 0.24f, 0.04f, 0.70f, 0.24f);
                var label = SheetButton((RectTransform)row.transform, "Go", HookArtKeys.ButtonGold, SurvivalVisuals.MineFill, 0.71f, 0.18f, 0.98f, 0.82f, 22, out var go);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                var goImg = go.GetComponent<Image>();
                var goRt = go.GetComponent<RectTransform>();
                var type = t.Type;
                Wire(go, () =>
                {
                    var block = hook.Build.Check(type);
                    if (block == BuildBlock.None)
                    {
                        CloseSheet();
                        BeginPlacement(type);
                        return;
                    }

                    StartCoroutine(Shake(goRt));
                    Toast(NormCenterOf(goRt), BuildBlockText(block, t), Warn);
                });

                refreshers.Add(() =>
                {
                    var hq = hook.Base.HqLevel;
                    var built = hook.Build.CountOf(type);
                    var max = t.MaxCount(hq);
                    cost.text = CostLine(hook.Build.CostOf(type)) + "   \u23f1 " + TimerQueue.FormatClock(hook.Build.SecondsOf(type));
                    var next = t.NextUnlockCastle(hq);
                    count.text = Fmt2("build.count", "Built {0}/{1}", built, max)
                        + (next is int n ? "   " + Fmt("build.more_at", "More at Castle Lv {0}", n) : "");
                    var block = hook.Build.Check(type);
                    var ok = block == BuildBlock.None;
                    label.text = ok ? S("build.button", "Build") : BuildBlockText(block, t);
                    label.fontSize = ok ? 26 : 17;
                    HookSkin.SetButton(goImg, HookArtKeys.ButtonGold, ok ? SurvivalVisuals.MineFill : Disabled, ok, 0.5f);
                    label.color = ok ? HookSkin.InkFor(HookArtKeys.ButtonGold) : SurvivalVisuals.Cream;
                    art.color = max > 0 ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
                });
            }

            var free = 0;
            foreach (var _ in hook.Build.FreeSpots)
            {
                free++;
            }

            var spots = HookSkin.Outlined(SurvivalVisuals.Text(panel, "Spots", "", 19, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.2f);
            Box(spots.rectTransform, 0.05f, 0.105f, 0.95f, 0.155f);
            var closeText = SheetButton(panel, "Close", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.33f, 0.025f, 0.67f, 0.10f, 22, out var close);
            closeText.text = S("hook.close", "Close");
            Wire(close, CloseSheet);
            _sheetRefresh = () =>
            {
                spots.text = Fmt("build.free_spots", "{0} free spots in your village", free);
                foreach (var r in refreshers)
                {
                    r();
                }
            };
            _sheetRefresh();
        }

        // ---------- placement ----------

        private void BeginPlacement(string type)
        {
            var village = _play?.Village;
            if (_hook == null || village == null || _overlay == null)
            {
                return;
            }

            _placingType = type;
            _autoLeft = AutoPlaceSeconds;
            var first = village.ShowPlacement(_hook.Build.FreeSpots);
            var next = _hook.Build.NextFreeSpot;
            if (next != null && village.SpotPoint(next) is Vector2 p)
            {
                village.Rig?.FocusOn(p.x, p.y);
            }

            if (_placeBar == null)
            {
                var bar = HookSkin.Panel(_overlay, "PlaceBar", HookArtKeys.Panel, SurvivalVisuals.InspectFill, 0.6f);
                bar.raycastTarget = true;
                Box(bar.rectTransform, 0.03f, 0.115f, 0.97f, 0.235f);
                _placeHint = HookSkin.Outlined(SurvivalVisuals.Text(bar.transform, "Hint", "", 26, TextAnchor.MiddleCenter, SurvivalVisuals.Reward), 1.5f);
                _placeHint.fontStyle = FontStyle.Bold;
                Box(_placeHint.rectTransform, 0.03f, 0.56f, 0.97f, 0.94f);
                _placeAutoText = SheetButton(bar.rectTransform, "Auto", HookArtKeys.ButtonGreen, FreeGreen, 0.05f, 0.10f, 0.60f, 0.54f, 22, out var auto);
                var cancelText = SheetButton(bar.rectTransform, "Cancel", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 0.63f, 0.10f, 0.95f, 0.54f, 22, out var cancel);
                cancelText.text = S("build.cancel", "Cancel");
                Wire(auto, () => PlaceAt(null));
                Wire(cancel, CancelPlacement);
                _placeBar = bar.rectTransform;
            }

            _placeBar.gameObject.SetActive(true);
            if (_placeHint != null)
            {
                _placeHint.text = Fmt("build.place_hint", "Tap a glowing spot for your {0}", S("build.type." + type, type));
            }

            StartCoroutine(PopIn(_placeBar));
            if (first != null)
            {
                StartCoroutine(Punch(first, 0.2f, 12));
            }
        }

        private void CancelPlacement()
        {
            _placingType = null;
            _play?.Village?.ShowPlacement(null);
            _placeBar?.gameObject.SetActive(false);
        }

        private void OnSpotTapped(string spotId)
        {
            if (_placingType != null)
            {
                PlaceAt(spotId);
            }
        }

        /// <summary>Build the chosen type on <paramref name="spotId"/>, or on the next free spot when null.</summary>
        private void PlaceAt(string? spotId)
        {
            if (_hook == null || _placingType == null)
            {
                return;
            }

            var type = _placingType;
            CancelPlacement();
            var block = _hook.Build.Check(type, spotId);
            if (block == BuildBlock.QueueBusy && _hook.TempBuilderOfferedForBuild(type, spotId))
            {
                OpenTempBuilder(null, type, spotId);
                return;
            }

            if (block != BuildBlock.None || _hook.Build.TryBuild(type, spotId) == null)
            {
                var t = _hook.Build.Type(type);
                Toast(new Vector2(0.5f, 0.5f), t != null ? BuildBlockText(block, t) : S("build.no_spot", "No free spot"), Warn);
            }
        }

        private void OnBuilt(BasePlot plot, string spotId)
        {
            var village = _play?.Village;
            if (village == null)
            {
                return;
            }

            var host = village.AddHost(plot.Id, spotId);
            if (host == null)
            {
                return;
            }

            var card = BuildCard(village, host, plot);
            _cards[plot.Id] = card;
            var at = NormCenterOf(host);
            SurvivalAudio.Play(Survival.Domain.Audio.AudioCues.UpgradeStart);
            StartCoroutine(Punch(host, 0.3f, 16));
            StartCoroutine(RingBurst(at, SurvivalVisuals.Reward, 0.3f));
            StartCoroutine(SparkBurst(at, SurvivalVisuals.Reward, 18, 0.1f));
            FloatText(at + new Vector2(0f, 0.05f), Fmt("build.started", "{0}: building!", PlotName(plot.Id)), SurvivalVisuals.Reward, 40);
            SaveVillage();
            RefreshAll();
        }

        /// <summary>A timer finished: a construction opens (Lv 1) or an upgrade levels up.</summary>
        private void Finished(PlotCard card, int level, bool wasBuild)
        {
            SurvivalAudio.Play(Survival.Domain.Audio.AudioCues.BuildComplete);
            if (!wasBuild)
            {
                LevelUp(card, level);
                ClaimCongrats(card, card.PlotId);
                return;
            }

            var at = NormCenterOf(card.Art.rectTransform);
            card.Art.color = Color.white;
            StartCoroutine(Punch(card.Root, 0.3f, 16));
            StartCoroutine(LevelBurst(at));
            StartCoroutine(RingBurst(at, SurvivalVisuals.Reward, 0.4f));
            StartCoroutine(SparkBurst(at, Color.white, 24, 0.14f));
            if (_overlay != null)
            {
                StartCoroutine(Confetti(_overlay, at, 28, new Vector2(0f, 0.5f)));
            }

            FloatText(at + new Vector2(0f, 0.06f), Fmt("build.done", "{0} built!", PlotName(card.PlotId)), SurvivalVisuals.Reward, 48);
            SaveVillage();
            RefreshAll();
            ClaimCongrats(card, card.PlotId);
        }

        // ---------- timers that finish on their own ----------

        /// <summary>
        /// A build / upgrade timer ran out (no tap): the level applies, the faded / hammer look clears and the badge
        /// updates, with a sparkle. The reward waits in the congratulations bubble on the building.
        /// </summary>
        private void OnAutoCompleted(BasePlot plot, int level, bool wasBuild)
        {
            if (!_cards.TryGetValue(plot.Id, out var card))
            {
                return;
            }

            var at = NormCenterOf(card.Art.rectTransform);
            card.Art.color = Color.white;
            card.Hammer?.gameObject.SetActive(false);
            StartCoroutine(Punch(card.Root, 0.14f, 10));
            StartCoroutine(RingBurst(at, SurvivalVisuals.Reward, 0.26f));
            StartCoroutine(SparkBurst(at, wasBuild ? Color.white : SurvivalVisuals.Reward, 16, 0.10f));
            if (card.IsCastle)
            {
                var visual = BuildingVisual.For(plot, Tiers);
                if (visual.Tier != card.Tier)
                {
                    card.Tier = visual.Tier;
                    StartCoroutine(GrowCastle(card, visual));
                }
            }
            else if (card.LevelBadge != null)
            {
                StartCoroutine(Punch(card.LevelBadge.rectTransform, 0.4f, 14));
            }

            SaveVillage();
            RefreshAll();
            _ = level;
        }

        /// <summary>Tap the congratulations bubble: the celebration plays and the reward counts into the chips.</summary>
        private void ClaimCongrats(PlotCard card, string plotId)
        {
            var pending = _hook?.Base.PendingFor(plotId);
            if (_hook == null || pending == null)
            {
                return;
            }

            var title = pending.WasBuild
                ? Fmt("build.done", "{0} built!", PlotName(plotId))
                : Fmt2("hook.upgrade_done", "{0} reached Lv {1}!", PlotName(plotId), pending.Level);
            var at = NormCenterOf(card.Congrats.gameObject.activeSelf ? card.Congrats.rectTransform : card.Root);
            StartCoroutine(SparkBurst(at, SurvivalVisuals.Reward, 14, 0.08f));
            var hook = _hook;
            Celebrate(title, pending.Rewards, () => hook.Base.ClaimCompletion(plotId));
            SaveVillage();
            RefreshAll();
        }

        private string CongratsLabel(PendingCompletion pending)
        {
            foreach (var kv in pending.Rewards)
            {
                return "+" + ChipWallet.FormatCompact(kv.Value);
            }

            return S("hook.claim", "Claim");
        }

        private void TickBuild()
        {
            var onVillage = _play != null && _play.Screen.Current == PlayScreen.Village && !_play.Screen.InTransition;
            if (_buildButton != null)
            {
                var show = onVillage && _placingType == null;
                if (_buildButton.gameObject.activeSelf != show)
                {
                    _buildButton.gameObject.SetActive(show);
                }
            }

            if (_placingType == null)
            {
                return;
            }

            if (!onVillage)
            {
                CancelPlacement();
                return;
            }

            _play?.Village?.TickPlacement(Time.unscaledTime);
            _autoLeft -= Time.unscaledDeltaTime;
            if (_placeAutoText != null)
            {
                _placeAutoText.text = Fmt("build.auto", "Auto-place ({0})", Mathf.Max(0, Mathf.CeilToInt(_autoLeft)));
            }

            if (_autoLeft <= 0f)
            {
                PlaceAt(null);
            }
        }
    }
}
