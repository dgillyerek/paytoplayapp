#nullable enable
using System.Collections.Generic;
using Survival.Domain.Economy;
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// "What you have" bar for spend screens: one compact gold-framed pill per resource (real icon + abbreviated amount).
    /// Polls the wallet every frame (a few int reads), so it follows every spend or gain; a changed pill pops and flashes
    /// green on a gain, red on a spend.
    /// </summary>
    public sealed class BalanceBar : MonoBehaviour
    {
        private static readonly Color Gain = new Color(0.49f, 0.92f, 0.52f, 1f);
        private static readonly Color Spend = new Color(1f, 0.42f, 0.36f, 1f);
        private const float FlashSeconds = 0.45f;

        private Wallet _wallet = null!;
        private readonly List<Pill> _pills = new List<Pill>();

        private sealed class Pill
        {
            public string Id = "";
            public int Shown = int.MinValue;
            public Text Amount = null!;
            public RectTransform Rect = null!;
            public float Flash;
            public Color FlashColor;
        }

        /// <summary>Bar inside the panel's own anchor box (x0..x1, y0..y1).</summary>
        public static BalanceBar Inside(RectTransform panel, Wallet wallet, IReadOnlyList<string> ids, float x0, float y0, float x1, float y1)
        {
            var bar = Make(panel, wallet, ids);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return bar;
        }

        /// <summary>Bar floating just above a compact sheet (clear of its header ribbon), so the sheet layout stays put.</summary>
        public static BalanceBar Above(RectTransform panel, Wallet wallet, IReadOnlyList<string> ids, float height = 62f)
        {
            var bar = Make(panel, wallet, ids);
            var rt = (RectTransform)bar.transform;
            var w = Mathf.Min(1f, 0.20f * ids.Count + 0.04f);
            rt.anchorMin = new Vector2(0.5f - w * 0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f + w * 0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = new Vector2(0f, 58f);
            return bar;
        }

        private static BalanceBar Make(RectTransform panel, Wallet wallet, IReadOnlyList<string> ids)
        {
            var go = new GameObject("BalanceBar", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var bar = go.AddComponent<BalanceBar>();
            bar._wallet = wallet;
            var n = Mathf.Max(1, ids.Count);
            const float gap = 0.012f;
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                var x0 = i / (float)n + gap * 0.5f;
                var x1 = (i + 1) / (float)n - gap * 0.5f;
                var plate = HookSkin.Panel(go.transform, "Pill_" + id, HookArtKeys.Chip, new Color(0.10f, 0.07f, 0.05f, 0.92f), 0.4f);
                plate.raycastTarget = false;
                Box(plate.rectTransform, x0, 0f, x1, 1f);
                Ornaments.Bevel(plate.transform, 0.7f);
                Ornaments.Frame(plate.transform, Ornaments.Gold, 1f);
                var icon = HookSkin.ResIcon(plate.transform, "Icon", id, SurvivalVisuals.Reward, "\u2726");
                icon.raycastTarget = false;
                Box(icon.rectTransform, 0.03f, -0.12f, 0.33f, 1.12f);
                var amount = HookSkin.Outlined(SurvivalVisuals.Text(plate.transform, "Amount", "", 26, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 1.5f);
                amount.fontStyle = FontStyle.Bold;
                amount.raycastTarget = false;
                HookSkin.Fit(amount, 14, 26);
                Box(amount.rectTransform, 0.33f, 0.06f, 0.96f, 0.94f);
                bar._pills.Add(new Pill { Id = id, Amount = amount, Rect = plate.rectTransform });
            }

            bar.Sync(false);
            return bar;
        }

        private void Update()
        {
            Sync(true);
            var dt = Time.unscaledDeltaTime;
            foreach (var p in _pills)
            {
                if (p.Flash <= 0f)
                {
                    continue;
                }

                p.Flash = Mathf.Max(0f, p.Flash - dt);
                var k = p.Flash / FlashSeconds;
                p.Rect.localScale = Vector3.one * (1f + 0.14f * k * k);
                p.Amount.color = Color.Lerp(SurvivalVisuals.Cream, p.FlashColor, k);
            }
        }

        private void Sync(bool flash)
        {
            if (_wallet == null)
            {
                return;
            }

            foreach (var p in _pills)
            {
                var v = _wallet.Get(p.Id);
                if (v == p.Shown)
                {
                    continue;
                }

                if (flash && p.Shown != int.MinValue)
                {
                    p.Flash = FlashSeconds;
                    p.FlashColor = v > p.Shown ? Gain : Spend;
                }

                p.Shown = v;
                p.Amount.text = BalanceBarIds.Format(p.Id, v);
            }
        }

        private static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
