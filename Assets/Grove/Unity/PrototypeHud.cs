using System.Text;
using Grove.Domain.Board;
using Grove.Domain.Commerce;
using Grove.Domain.Energy;
using Grove.Domain.Merge;
using Grove.Domain.Orders;
using Grove.Domain.Producer;
using Grove.Domain.Time;
using UnityEngine;
using UnityEngine.UI;

namespace Grove.Unity
{
    /// <summary>Playable HUD: energy, crate tap, scripted orders 1–6, toast, optional FakeStore starter pack.</summary>
    public sealed class PrototypeHud : MonoBehaviour, IEnergyListener
    {
        private Text _energyText = null!;
        private Text _crateText = null!;
        private Text _orderText = null!;
        private Text _toastText = null!;
        private Text _hintText = null!;
        private Button _crateButton = null!;
        private Button _deliverButton = null!;
        private Image _energyFill = null!;
        private float _toastUntil;

        public GroveBootstrap? Host { get; set; }

        public void Build()
        {
            GroveVisuals.EnsureEventSystem();

            var canvasGo = new GameObject("PrototypeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = canvasGo.GetComponent<RectTransform>();

            _energyFill = Bar(root, "EnergyBar", new Vector2(0.04f, 0.93f), new Vector2(0.42f, 0.98f), new Color(0.15f, 0.15f, 0.18f));
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_energyFill.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            Stretch(fillRect);
            _energyFill = fillGo.AddComponent<Image>();
            _energyFill.sprite = GroveVisuals.WhiteSprite;
            _energyFill.color = new Color(0.35f, 0.85f, 0.45f);
            _energyFill.type = Image.Type.Filled;
            _energyFill.fillMethod = Image.FillMethod.Horizontal;

            _energyText = GroveVisuals.UiText(root, "EnergyLabel", "Energy 100/100", 26, TextAnchor.MiddleLeft, Color.white);
            Place(_energyText.rectTransform, new Vector2(0.05f, 0.93f), new Vector2(0.42f, 0.98f));

            _orderText = GroveVisuals.UiText(root, "Order", "Order 1", 24, TextAnchor.UpperRight, Color.white);
            Place(_orderText.rectTransform, new Vector2(0.45f, 0.82f), new Vector2(0.97f, 0.98f));

            _toastText = GroveVisuals.UiText(root, "Toast", "", 28, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.6f));
            Place(_toastText.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.82f));

            var goal = Host != null ? Host.Catalog.Copy.Goal : "Restore the Front Garden";
            _hintText = GroveVisuals.UiText(
                root,
                "Hint",
                goal + " — CRATE → merge 3 matching pieces → DELIVER. Maya, 2D portrait later.",
                22,
                TextAnchor.LowerCenter,
                new Color(0.9f, 0.92f, 0.85f));
            Place(_hintText.rectTransform, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.28f));

            _crateButton = GroveVisuals.UiButton(root, "CrateButton", "CRATE", new Color(0.2f, 0.45f, 0.28f), new Vector2(280, 96));
            Place(_crateButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.12f));
            _crateButton.onClick.AddListener(OnCrate);

            _crateText = GroveVisuals.UiText(root, "CrateCharges", "Charges 30/30", 22, TextAnchor.MiddleCenter, Color.white);
            Place(_crateText.rectTransform, new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.17f));

            _deliverButton = GroveVisuals.UiButton(root, "DeliverButton", "DELIVER", new Color(0.55f, 0.35f, 0.15f), new Vector2(240, 72));
            Place(_deliverButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.72f), new Vector2(0.96f, 0.80f));
            _deliverButton.onClick.AddListener(OnDeliver);

            var store = GroveVisuals.UiButton(root, "StoreButton", "STARTER PACK", new Color(0.45f, 0.32f, 0.12f), new Vector2(240, 64));
            Place(store.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.26f, 0.11f));
            store.onClick.AddListener(OnStarterPack);
        }

        public void Toast(string message)
        {
            _toastText.text = message;
            _toastUntil = Time.unscaledTime + 2.2f;
        }

        public void OnEnergyChanged(int current, int cap)
        {
            if (_energyText != null)
            {
                _energyText.text = $"Energy {current}/{cap}";
            }

            if (_energyFill != null && cap > 0)
            {
                _energyFill.fillAmount = current / (float)cap;
            }
        }

        public void OnEnergyEmpty()
        {
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
                _toastText.text = Host.Drag != null ? Host.Drag.LastFeedback : "";
            }
            else if (_toastUntil <= 0f && Host.Drag != null && _toastText.text != Host.Drag.LastFeedback)
            {
                _toastText.text = Host.Drag.LastFeedback;
            }

            if (Host.Energy != null)
            {
                Host.Energy.Tick();
                OnEnergyChanged(Host.Energy.Current, Host.Energy.Cap);
            }

            if (Host.Crate != null)
            {
                var charges = Host.Crate.Charges(Host.Clock);
                _crateText.text = $"Charges {charges}/{Host.Crate.MaxCharges}";
                var energyOk = Host.Crate.FtueFreeTapRemaining || Host.Energy == null || !Host.Energy.IsEmpty;
                _crateButton.interactable = charges > 0 && energyOk;
            }

            var order = Host.Orders?.Active;
            if (order == null)
            {
                _orderText.text = Host.Orders != null && Host.Orders.MilestoneReached
                    ? Host.Catalog.Copy.MilestoneSplash
                    : "All orders complete. Nice.";
                _deliverButton.interactable = false;
            }
            else
            {
                _orderText.text = FormatActiveOrder(order);
                _deliverButton.interactable = Host.Orders.CanDeliver(Host.Session.Board);
            }
        }

        private void OnCrate()
        {
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
            if (Host?.Orders == null)
            {
                return;
            }

            var title = Host.Orders.Active?.Title ?? "Order";
            if (Host.Orders.TryDeliver(Host.Session.Board))
            {
                Host.BoardView?.Refresh();
                var splash = Host.Splash != null
                    ? Host.Splash.TryConsumeMilestone(Host.Orders.MilestoneReached)
                    : null;
                Toast(!string.IsNullOrEmpty(splash) ? splash : $"{title} complete!");
            }
            else
            {
                Toast("Need the required piece on the board.");
            }
        }

        private async void OnStarterPack()
        {
            if (Host?.Store == null)
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
            Toast($"Starter pack: {granted}× Wildflower T1 on the board.");
        }

        private string FormatActiveOrder(OrderSpec order)
        {
            var text = new StringBuilder();
            var copy = Host!.Catalog.Copy;
            text.Append(copy.Goal);
            text.Append('\n');
            text.Append(order.Title);
            text.Append(" · ");
            text.Append(Host.Orders.CompletedCount + 1);
            text.Append('/');
            text.Append(Host.Orders.TotalCount);
            foreach (var req in order.Requirements)
            {
                var name = Host.Catalog.Items.TryGet(req.Item, out var def) ? def.DisplayName : req.Item.Value;
                var have = Host.Session.Board.CountItem(req.Item);
                text.Append('\n');
                text.Append(have);
                text.Append('/');
                text.Append(req.Count);
                text.Append(' ');
                text.Append(name);
            }

            if (order.CoinReward > 0 || order.XpReward > 0)
            {
                text.Append('\n');
                text.Append('+');
                text.Append(order.CoinReward);
                text.Append(" coins, +");
                text.Append(order.XpReward);
                text.Append(" XP");
            }

            var line = order.SpokenLine;
            if (!string.IsNullOrEmpty(line))
            {
                text.Append('\n');
                text.Append(copy.NpcDisplayName);
                text.Append(": ");
                text.Append(line);
            }

            return text.ToString();
        }

        private static Image Bar(RectTransform parent, string name, Vector2 min, Vector2 max, Color bg)
        {
            var image = GroveVisuals.UiImage(parent, name, bg);
            Place(image.rectTransform, min, max);
            return image;
        }

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
    }
}
