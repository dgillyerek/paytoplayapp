using System.Collections.Generic;
using System.Text;
using Grove.Domain.Board;
using Grove.Domain.Commerce;
using Grove.Domain.Energy;
using Grove.Domain.Layout;
using Grove.Domain.Merge;
using Grove.Domain.Orders;
using Grove.Domain.Producer;
using UnityEngine;
using UnityEngine.UI;

namespace Grove.Unity
{
    /// <summary>
    /// Production-feel HUD: energy + coins (gems hidden), Maya 2D portrait, 3-slot order tray
    /// in a top row, crate at the bottom, DEV-018 splash sequence, then crate→merge→deliver
    /// coach marks. Place() rects come from <see cref="Grove.Domain.Layout.PlayLayout"/> so
    /// chrome cannot cover the 7×5.
    /// </summary>
    public sealed class PrototypeHud : MonoBehaviour, IEnergyListener
    {
        private Text _energyText = null!;
        private Text _coinText = null!;
        private Text _crateText = null!;
        private Text _toastText = null!;
        private Text _goalText = null!;
        private Button _crateButton = null!;
        private Image _crateImage = null!;
        private Image _energyFill = null!;
        private Image _mayaImage = null!;
        private Image _splashImage = null!;
        private Text _splashCaption = null!;
        private GameObject _splashRoot = null!;
        private GameObject _energyEmptyRoot = null!;
        private RectTransform _trayRoot = null!;
        private readonly List<OrderCardUi> _cards = new List<OrderCardUi>();
        private readonly List<SplashBeat> _splashes = new List<SplashBeat>();
        private int _trayCompleted = -1;
        private float _toastUntil;
        private bool _introFinished;
        private FirstRunCoach _coach = null!;
        private GameObject _coachRoot = null!;
        private RectTransform _coachPointerRect = null!;
        private RectTransform _coachCaptionRect = null!;
        private Image _coachPointer = null!;
        private Text _coachText = null!;
        private DragResult? _coachSeenDrag;

        public GroveBootstrap? Host { get; set; }

        public bool SplashBlocking => _splashRoot != null && _splashRoot.activeSelf;

        public void Build()
        {
            GroveArt.EnsureLoaded();
            GroveVisuals.EnsureEventSystem();

            var canvasGo = new GameObject("PrototypeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(PlayLayout.ReferenceWidth, PlayLayout.ReferenceHeight);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.GetComponent<RectTransform>();

            var goal = Host != null ? Host.Catalog.Copy.Goal : "Restore the Front Garden";
            var goalCard = GroveVisuals.UiImage(root, "GoalBanner", Color.white, GroveArt.Get(GroveArt.StubOrderCard), true);
            Place(goalCard.rectTransform, PlayLayout.Goal);
            _goalText = GroveVisuals.UiText(goalCard.transform, "Goal", goal, 32, TextAnchor.MiddleCenter, new Color(0.18f, 0.32f, 0.16f));
            Stretch(_goalText.rectTransform);

            var energyPill = GroveVisuals.UiImage(root, "EnergyPill", Color.white, GroveArt.Get(GroveArt.StubEnergyPill), true);
            Place(energyPill.rectTransform, PlayLayout.EnergyPill);
            _energyFill = Bar(root, "EnergyBar", PlayLayout.EnergyBar, new Color(0.12f, 0.16f, 0.12f, 0.85f));
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_energyFill.transform, false);
            Stretch(fillGo.AddComponent<RectTransform>());
            _energyFill = fillGo.AddComponent<Image>();
            _energyFill.sprite = GroveVisuals.WhiteSprite;
            _energyFill.color = new Color(0.35f, 0.85f, 0.55f);
            _energyFill.type = Image.Type.Filled;
            _energyFill.fillMethod = Image.FillMethod.Horizontal;
            _energyFill.raycastTarget = false;
            _energyText = GroveVisuals.UiText(root, "EnergyLabel", "100/100", 22, TextAnchor.MiddleLeft, Color.white);
            Place(_energyText.rectTransform, PlayLayout.EnergyLabel);

            var coinIcon = GroveVisuals.UiImage(root, "CoinIcon", Color.white, GroveArt.Get(GroveArt.StubCoin), true);
            Place(coinIcon.rectTransform, PlayLayout.CoinIcon);
            _coinText = GroveVisuals.UiText(root, "Coins", "0", 26, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.55f));
            Place(_coinText.rectTransform, PlayLayout.CoinLabel);

            var gem = GroveVisuals.UiImage(root, "GemIcon", Color.white, GroveArt.Get(GroveArt.StubGem), true);
            Place(gem.rectTransform, new Vector2(0.86f, 0.84f), new Vector2(0.98f, 0.91f));
            gem.gameObject.SetActive(false);

            _mayaImage = GroveVisuals.UiImage(root, "Maya", Color.white, GroveArt.Get(GroveArt.StubMayaNeutral), true);
            Place(_mayaImage.rectTransform, PlayLayout.Maya);

            var tray = new GameObject("OrderTray");
            tray.transform.SetParent(root, false);
            _trayRoot = tray.AddComponent<RectTransform>();
            Place(_trayRoot, PlayLayout.OrderTray);

            _toastText = GroveVisuals.UiText(root, "Toast", "", 22, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.7f));
            Place(_toastText.rectTransform, PlayLayout.Toast);

            _crateButton = GroveVisuals.UiButton(
                root,
                "CrateButton",
                "",
                Color.white,
                new Vector2(280, 220),
                GroveArt.Get(GroveArt.StubCrateCharged) ?? GroveArt.Get(GroveArt.StubCrateIdle));
            Place(_crateButton.GetComponent<RectTransform>(), PlayLayout.Crate);
            _crateImage = _crateButton.targetGraphic as Image ?? _crateButton.GetComponent<Image>();
            _crateButton.onClick.AddListener(OnCrate);
            _crateText = GroveVisuals.UiText(root, "CrateCharges", "Charges 30/30", 20, TextAnchor.MiddleCenter, Color.white);
            Place(_crateText.rectTransform, PlayLayout.CrateLabel);

            var store = GroveVisuals.UiButton(
                root,
                "StoreButton",
                "STARTER",
                Color.white,
                new Vector2(200, 64),
                GroveArt.Get(GroveArt.StubButton));
            Place(store.GetComponent<RectTransform>(), PlayLayout.Store);
            store.onClick.AddListener(OnStarterPack);

            BuildEnergyEmpty(root);
            BuildCoach(root);
            BuildSplash(root);
            QueueIntroSplashes();
            RebuildTray();
        }

        public void Toast(string message)
        {
            if (_toastText == null)
            {
                return;
            }

            _toastText.text = message;
            _toastUntil = Time.unscaledTime + 2.2f;
        }

        public void OnEnergyChanged(int current, int cap)
        {
            if (_energyText != null)
            {
                _energyText.text = $"{current}/{cap}";
            }

            if (_energyFill != null && cap > 0)
            {
                _energyFill.fillAmount = current / (float)cap;
            }
        }

        public void OnEnergyEmpty()
        {
            if (_energyEmptyRoot != null)
            {
                _energyEmptyRoot.SetActive(true);
            }

            Toast("Out of energy — wait or use Starter Pack.");
        }

        private void Update()
        {
            if (Host == null)
            {
                return;
            }

            if (_toastUntil > 0f && Time.unscaledTime > _toastUntil)
            {
                _toastText.text = !SplashBlocking && Host.Drag != null ? Host.Drag.LastFeedback : "";
            }
            else if (_toastUntil <= 0f && !SplashBlocking && Host.Drag != null && _toastText.text != Host.Drag.LastFeedback)
            {
                _toastText.text = Host.Drag.LastFeedback;
            }

            if (Host.Energy != null)
            {
                Host.Energy.Tick();
                OnEnergyChanged(Host.Energy.Current, Host.Energy.Cap);
            }

            if (_coinText != null)
            {
                _coinText.text = Host.Coins.ToString();
            }

            if (Host.Crate != null)
            {
                var charges = Host.Crate.Charges(Host.Clock);
                _crateText.text = $"Charges {charges}/{Host.Crate.MaxCharges}";
                var energyOk = Host.Crate.FtueFreeTapRemaining || Host.Energy == null || !Host.Energy.IsEmpty;
                _crateButton.interactable = !SplashBlocking && charges > 0 && energyOk;
                var crateStub = charges <= 0
                    ? GroveArt.StubCrateEmpty
                    : GroveArt.StubCrateCharged;
                var crateSprite = GroveArt.Get(crateStub) ?? GroveArt.Get(GroveArt.StubCrateIdle);
                if (crateSprite != null && _crateImage != null)
                {
                    _crateImage.sprite = crateSprite;
                    _crateImage.preserveAspect = true;
                }
            }

            if (Host.Orders != null && Host.Orders.CompletedCount != _trayCompleted)
            {
                RebuildTray();
            }

            RefreshTrayBodies();
            TickCoach();
        }

        private void QueueIntroSplashes()
        {
            if (Host?.Splash == null)
            {
                return;
            }

            var boot = Host.Splash.TryConsumeBoot();
            if (boot != null)
            {
                _splashes.Add(boot);
            }

            var area = Host.Splash.TryConsumeAreaStart();
            if (area != null)
            {
                _splashes.Add(area);
            }

            ShowNextSplash();
        }

        private void BuildSplash(RectTransform root)
        {
            _splashRoot = new GameObject("SplashOverlay");
            _splashRoot.transform.SetParent(root, false);
            var overlay = _splashRoot.AddComponent<RectTransform>();
            Stretch(overlay);
            _splashRoot.AddComponent<RectMask2D>();

            var dim = GroveVisuals.UiImage(_splashRoot.transform, "Dim", new Color(0.05f, 0.04f, 0.02f, 1f), raycastTarget: true);
            Stretch(dim.rectTransform);
            var hit = dim.gameObject.AddComponent<Button>();
            hit.transition = Selectable.Transition.None;
            hit.onClick.AddListener(DismissSplash);

            _splashImage = GroveVisuals.UiImage(_splashRoot.transform, "Art", Color.white, null, true);
            var artRect = _splashImage.rectTransform;
            artRect.anchorMin = new Vector2(0.5f, 0.5f);
            artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.anchoredPosition = Vector2.zero;
            artRect.sizeDelta = new Vector2(1920f, 1080f);
            _splashImage.raycastTarget = false;
            var fitter = _splashImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            _splashCaption = GroveVisuals.UiText(
                _splashRoot.transform,
                "Caption",
                "",
                28,
                TextAnchor.MiddleCenter,
                Color.white);
            Place(_splashCaption.rectTransform, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.12f));
            _splashRoot.SetActive(false);
        }

        private void BuildEnergyEmpty(RectTransform root)
        {
            _energyEmptyRoot = new GameObject("EnergyEmpty");
            _energyEmptyRoot.transform.SetParent(root, false);
            var rect = _energyEmptyRoot.AddComponent<RectTransform>();
            Stretch(rect);
            var dim = GroveVisuals.UiImage(_energyEmptyRoot.transform, "Dim", new Color(0f, 0f, 0f, 0.55f), raycastTarget: true);
            Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => _energyEmptyRoot.SetActive(false));
            var modal = GroveVisuals.UiImage(
                _energyEmptyRoot.transform,
                "Modal",
                Color.white,
                GroveArt.Get(GroveArt.StubEnergyEmpty),
                true);
            Place(modal.rectTransform, new Vector2(0.12f, 0.32f), new Vector2(0.88f, 0.68f));
            _energyEmptyRoot.SetActive(false);
        }

        private void ShowNextSplash()
        {
            if (_splashes.Count == 0)
            {
                _splashRoot.SetActive(false);
                if (!_introFinished && Host != null)
                {
                    _introFinished = true;
                    StartCoach();
                }
                else
                {
                    RefreshCoach();
                }

                return;
            }

            var beat = _splashes[0];
            _splashes.RemoveAt(0);
            var art = GroveArt.Get(beat.ArtStub);
            _splashImage.sprite = art;
            _splashImage.preserveAspect = true;
            _splashImage.color = Color.white;
            var fitter = _splashImage.GetComponent<AspectRatioFitter>();
            if (fitter != null && art != null && art.rect.height > 0.01f)
            {
                fitter.aspectRatio = art.rect.width / art.rect.height;
            }

            _splashCaption.text = beat.Caption + "\nTap to continue";
            _splashRoot.SetActive(true);
        }

        private void DismissSplash() => ShowNextSplash();

        private void BuildCoach(RectTransform root)
        {
            var copy = Host != null ? Host.Catalog.Copy : PresentationCopy.Default;
            _coach = new FirstRunCoach(copy);
            _coachRoot = new GameObject("CoachMarks");
            _coachRoot.transform.SetParent(root, false);
            var coachRect = _coachRoot.AddComponent<RectTransform>();
            Stretch(coachRect);

            _coachPointer = GroveVisuals.UiImage(
                _coachRoot.transform,
                "Pointer",
                Color.white,
                GroveArt.Get(GroveArt.StubMergeSparkle) ?? GroveArt.Get(GroveArt.StubCellHighlight),
                true);
            _coachPointerRect = _coachPointer.rectTransform;
            Place(_coachPointerRect, PlayLayout.CoachCratePointer);

            var caption = GroveVisuals.UiImage(
                _coachRoot.transform,
                "Caption",
                Color.white,
                GroveArt.Get(GroveArt.StubOrderCard),
                false,
                raycastTarget: true);
            _coachCaptionRect = caption.rectTransform;
            Place(_coachCaptionRect, PlayLayout.CoachCrateCaption);
            var skip = caption.gameObject.AddComponent<Button>();
            skip.targetGraphic = caption;
            skip.onClick.AddListener(SkipCoach);

            _coachText = GroveVisuals.UiText(
                caption.transform,
                "Label",
                "",
                20,
                TextAnchor.MiddleCenter,
                new Color(0.16f, 0.28f, 0.14f));
            Stretch(_coachText.rectTransform);
            _coachText.raycastTarget = false;
            _coachRoot.SetActive(false);
        }

        private void StartCoach()
        {
            if (_coach == null)
            {
                return;
            }

            _coach.Start();
            RefreshCoach();
            if (_coach.Current != null)
            {
                Toast(Host != null ? Host.Catalog.Copy.Goal : PresentationCopy.Default.Goal);
            }
        }

        private void SkipCoach()
        {
            _coach?.TryAdvance(CoachAdvance.Skip);
            RefreshCoach();
        }

        private void TickCoach()
        {
            if (_coachRoot != null && SplashBlocking)
            {
                _coachRoot.SetActive(false);
                return;
            }

            if (_coach == null || !_coach.Started || _coach.IsComplete)
            {
                return;
            }

            var last = Host?.Drag?.LastResult;
            if (last != null && !ReferenceEquals(last, _coachSeenDrag))
            {
                _coachSeenDrag = last;
                if (last is DragResult.Applied applied && applied.Merge != null &&
                    _coach.TryAdvance(CoachAdvance.Merge))
                {
                    RefreshCoach();
                }
            }

            if (_coachRoot != null && _coachRoot.activeSelf && _coachPointerRect != null)
            {
                var k = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.08f;
                _coachPointerRect.localScale = new Vector3(k, k, 1f);
            }
        }

        private void RefreshCoach()
        {
            if (_coachRoot == null || _coach == null)
            {
                return;
            }

            if (SplashBlocking || !_coach.Started || _coach.IsComplete || _coach.Current == null)
            {
                _coachRoot.SetActive(false);
                return;
            }

            var mark = _coach.Current;
            _coachText.text = mark.Caption + "\n(Got it)";
            var art = GroveArt.Get(mark.ArtStub) ?? GroveArt.Get(GroveArt.StubMergeSparkle);
            if (art != null)
            {
                _coachPointer.sprite = art;
                _coachPointer.preserveAspect = true;
            }

            switch (mark.AdvanceOn)
            {
                case CoachAdvance.Crate:
                    Place(_coachPointerRect, PlayLayout.CoachCratePointer);
                    Place(_coachCaptionRect, PlayLayout.CoachCrateCaption);
                    break;
                case CoachAdvance.Merge:
                    Place(_coachPointerRect, PlayLayout.CoachMergePointer);
                    Place(_coachCaptionRect, PlayLayout.CoachMergeCaption);
                    break;
                default:
                    Place(_coachPointerRect, PlayLayout.CoachDeliverPointer);
                    Place(_coachCaptionRect, PlayLayout.CoachDeliverCaption);
                    break;
            }

            _coachRoot.SetActive(true);
            _coachPointerRect.localScale = Vector3.one;
            _coachRoot.transform.SetAsLastSibling();
            if (_energyEmptyRoot != null)
            {
                _energyEmptyRoot.transform.SetAsLastSibling();
            }

            if (_splashRoot != null)
            {
                _splashRoot.transform.SetAsLastSibling();
            }
        }

        private void RebuildTray()
        {
            if (_trayRoot == null || Host?.Orders == null)
            {
                return;
            }

            for (var i = _trayRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_trayRoot.GetChild(i).gameObject);
            }

            _cards.Clear();
            _trayCompleted = Host.Orders.CompletedCount;
            var visible = Host.Orders.Visible;
            var count = visible.Count;
            for (var i = 0; i < count; i++)
            {
                _cards.Add(BuildCard(visible[i], i, count, i == 0));
            }

            if (Host.Orders.MilestoneReached && _mayaImage != null)
            {
                var happy = GroveArt.Get(GroveArt.StubMayaHappy);
                if (happy != null)
                {
                    _mayaImage.sprite = happy;
                }
            }
        }

        private OrderCardUi BuildCard(OrderSpec order, int index, int total, bool active)
        {
            var width = 1f / Mathf.Max(1, total);
            var xmin = index * width;
            var xmax = (index + 1) * width;
            var card = GroveVisuals.UiImage(_trayRoot, order.Id, Color.white, GroveArt.Get(GroveArt.StubOrderCard), false);
            Place(card.rectTransform, new Vector2(xmin + 0.01f, 0.04f), new Vector2(xmax - 0.01f, 0.96f));

            var iconRow = new GameObject("Icons");
            iconRow.transform.SetParent(card.transform, false);
            var iconRect = iconRow.AddComponent<RectTransform>();
            Place(iconRect, new Vector2(0.04f, 0.12f), new Vector2(0.28f, 0.88f));
            for (var r = 0; r < order.Requirements.Count; r++)
            {
                var req = order.Requirements[r];
                var icon = GroveVisuals.UiImage(
                    iconRow.transform,
                    "Req" + r,
                    Color.white,
                    GroveArt.SpriteForItem(req.Item.Value),
                    true);
                var t = order.Requirements.Count == 1 ? 0.5f : r / (float)(order.Requirements.Count - 1);
                Place(icon.rectTransform, new Vector2(t * 0.45f, 0.05f), new Vector2(0.55f + t * 0.45f, 0.95f));
            }

            var body = GroveVisuals.UiText(
                card.transform,
                "Body",
                "",
                active ? 16 : 14,
                TextAnchor.UpperLeft,
                new Color(0.18f, 0.28f, 0.16f));
            Place(body.rectTransform, new Vector2(0.30f, 0.08f), new Vector2(active ? 0.68f : 0.96f, 0.92f));

            Button? deliver = null;
            if (active)
            {
                deliver = GroveVisuals.UiButton(
                    card.transform,
                    "Deliver",
                    "DELIVER",
                    Color.white,
                    new Vector2(120, 40),
                    GroveArt.Get(GroveArt.StubButton));
                Place(deliver.GetComponent<RectTransform>(), new Vector2(0.68f, 0.18f), new Vector2(0.97f, 0.82f));
                deliver.onClick.AddListener(OnDeliver);
            }

            var ui = new OrderCardUi
            {
                Spec = order,
                Body = body,
                Deliver = deliver,
                IsActive = active
            };
            ui.Body.text = FormatOrder(order, active);
            return ui;
        }

        private void RefreshTrayBodies()
        {
            if (Host?.Orders == null)
            {
                return;
            }

            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.Body != null)
                {
                    card.Body.text = FormatOrder(card.Spec, card.IsActive);
                }

                if (card.Deliver != null)
                {
                    card.Deliver.interactable = !SplashBlocking && card.IsActive && Host.Orders.CanDeliver(Host.Session.Board);
                }
            }
        }

        private string FormatOrder(OrderSpec order, bool active)
        {
            var text = new StringBuilder();
            if (Host != null)
            {
                var number = active ? Host.Orders.CompletedCount + 1 : IndexOf(order) + 1;
                text.Append(number);
                text.Append('/');
                text.Append(Host.Orders.TotalCount);
                text.Append(' ');
            }

            text.Append(order.Title);
            foreach (var req in order.Requirements)
            {
                var name = Host != null && Host.Catalog.Items.TryGet(req.Item, out var def)
                    ? def.DisplayName
                    : req.Item.Value;
                var have = Host != null ? Host.Session.Board.CountItem(req.Item) : 0;
                text.Append('\n');
                text.Append(have);
                text.Append('/');
                text.Append(req.Count);
                text.Append(' ');
                text.Append(name);
            }

            if (order.CoinReward > 0)
            {
                text.Append('\n');
                text.Append('+');
                text.Append(order.CoinReward);
                text.Append('c');
            }

            return text.ToString();
        }

        private int IndexOf(OrderSpec order)
        {
            if (Host?.Orders == null)
            {
                return 0;
            }

            var all = Host.Orders.All;
            for (var i = 0; i < all.Count; i++)
            {
                if (all[i].Id == order.Id)
                {
                    return i;
                }
            }

            return Host.Orders.CompletedCount;
        }

        private void OnCrate()
        {
            if (SplashBlocking)
            {
                return;
            }

            if (Host?.CrateTap == null)
            {
                Toast("Crate not ready.");
                return;
            }

            var result = Host.CrateTap.TryTap();
            Host.BoardView?.Refresh();
            switch (result)
            {
                case SpitResult.Ok ok:
                    var name = Host.Catalog.Items.TryGet(ok.Item, out var def) ? def.DisplayName : ok.Item.Value;
                    Toast($"Crate produced {name}.");
                    _coach?.TryAdvance(CoachAdvance.Crate);
                    RefreshCoach();
                    break;
                case SpitResult.Failed failed:
                    Toast(failed.Reason switch
                    {
                        "no-charges" => "Crate recharging…",
                        "no-energy" => "No energy.",
                        "board-full" => "Board is full — merge some pieces.",
                        _ => failed.Reason
                    });
                    break;
            }
        }

        private void OnDeliver()
        {
            if (SplashBlocking || Host?.Orders == null)
            {
                return;
            }

            var delivered = Host.Orders.Active;
            if (delivered == null)
            {
                return;
            }

            if (!Host.Orders.TryDeliver(Host.Session.Board))
            {
                Toast("Need the required piece on the board.");
                return;
            }

            Host.GrantCoins(delivered.CoinReward);
            Host.BoardView?.Refresh();
            RebuildTray();
            _coach?.TryAdvance(CoachAdvance.Deliver);
            RefreshCoach();

            if (Host.Splash != null)
            {
                var milestone = Host.Splash.TryConsumeMilestoneBeat(Host.Orders.MilestoneReached);
                if (milestone != null)
                {
                    _splashes.Add(milestone);
                    ShowNextSplash();
                    Toast(milestone.Caption);
                    return;
                }
            }

            var complete = SplashBeat.OrderComplete();
            _splashes.Add(complete);
            ShowNextSplash();
            Toast(delivered.Title + " complete!");
        }

        private async void OnStarterPack()
        {
            if (SplashBlocking || Host?.Store == null)
            {
                return;
            }

            var result = await Host.Store.PurchaseAsync("com.grove.starter_pack");
            if (result is not PurchaseResult.Success)
            {
                Toast("Purchase failed.");
                return;
            }

            var granted = 0;
            for (var i = 0; i < 3; i++)
            {
                if (!Host.Session.Board.TryFindEmpty(out var pos))
                {
                    break;
                }

                Host.Session.Board.Place(pos, new PieceStack(GroveCatalog.WildflowerT1, 1));
                granted++;
            }

            Host.Energy?.TryGrantFtueTopUp();
            Host.BoardView?.Refresh();
            Toast($"Starter pack: {granted}× Seed on the board.");
        }

        private static Image Bar(RectTransform parent, string name, NormRect band, Color bg)
        {
            var image = GroveVisuals.UiImage(parent, name, bg);
            Place(image.rectTransform, band);
            return image;
        }

        private static void Place(RectTransform rect, NormRect band) =>
            Place(rect, new Vector2(band.XMin, band.YMin), new Vector2(band.XMax, band.YMax));

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class OrderCardUi
        {
            public OrderSpec Spec = null!;
            public Text Body = null!;
            public Button? Deliver;
            public bool IsActive;
        }
    }
}
