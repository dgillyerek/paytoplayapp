#nullable enable
using System;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity.Kids
{
    /// <summary>Minimal Theme A pieces for the kid-safety screens (age gate, parental gate, consent, delete account).</summary>
    internal static class KidUi
    {
        public static Func<string, string, string>? Strings;

        public static string S(string key, string fallback) => Strings?.Invoke(key, fallback) ?? fallback;

        /// <summary>Full-screen modal: a dim layer that eats every tap, and a Theme A panel. No close button by default.</summary>
        public static RectTransform Modal(Transform parent, string name, int sorting, float x0, float y0, float x1, float y1, out RectTransform root)
        {
            SurvivalVisuals.EnsureEventSystem();
            root = SurvivalVisuals.Canvas(parent, name, sorting);
            var dim = SurvivalVisuals.Image(root, "Dim", new Color(0.03f, 0.03f, 0.05f, 0.94f));
            dim.raycastTarget = true;
            SurvivalVisuals.Stretch(dim.rectTransform);
            var card = HookSkin.Panel(root, "Card", HookArtKeys.Panel, SurvivalVisuals.Panel);
            card.raycastTarget = true;
            HookSkin.Box(card.rectTransform, x0, y0, x1, y1);
            return card.rectTransform;
        }

        public static Text Label(RectTransform parent, string name, string value, int size, float x0, float y0, float x1, float y1, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = HookSkin.Outlined(SurvivalVisuals.Text(parent, name, value, size, anchor, color ?? HookSkin.Cream), 1.5f);
            HookSkin.Box(t.rectTransform, x0, y0, x1, y1);
            HookSkin.Fit(t, Math.Max(10, size / 2), size);
            return t;
        }

        public sealed class Btn
        {
            public Btn(Button button, Image image, Text label, string key, Color fallback)
            {
                Button = button;
                Image = image;
                Label = label;
                Key = key;
                Fallback = fallback;
            }

            public Button Button { get; }
            public Image Image { get; }
            public Text Label { get; }
            public string Key { get; private set; }
            public Color Fallback { get; private set; }

            public void Set(bool enabled)
            {
                Button.interactable = enabled;
                HookSkin.SetButton(Image, Key, enabled ? Fallback : HookSkin.DisabledTint, enabled);
            }

            public void Skin(string key, Color fallback)
            {
                Key = key;
                Fallback = fallback;
                HookSkin.SetButton(Image, key, fallback, Button.interactable);
            }
        }

        public static Btn Button(RectTransform parent, string name, string text, string key, Color fallback, int size, float x0, float y0, float x1, float y1, Action onClick)
        {
            var b = HookSkin.Button(parent, name, key, fallback);
            HookSkin.Box(b.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(b.transform, "Label", text, size, TextAnchor.MiddleCenter, HookSkin.Cream), 1.5f);
            HookSkin.PadLabel(label.rectTransform);
            HookSkin.Fit(label, Math.Max(10, size / 2), size);
            b.onClick.AddListener(() => onClick());
            return new Btn(b, b.GetComponent<Image>(), label, key, fallback);
        }
    }
}
