#nullable enable
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Theme-pack UI kit for the hook slice: ornate 9-slice panels and buttons, gold-framed bars, resource icons.
    /// Every call asks for a <see cref="HookArtKeys"/> content key and falls back to the flat procedural look when
    /// the active ThemePack does not ship that art, so a white-label pack can swap or omit any piece.
    /// </summary>
    internal static class HookSkin
    {
        // Derek 2026-10-30: "black on gold is impossible". Text on any kit art is light cream with a thick dark outline;
        // the old dark inks are kept as names only so every caller turns light.
        public static readonly Color Cream = new Color(1f, 0.957f, 0.839f, 1f); // #FFF4D6
        public static readonly Color OutlineInk = new Color(0.10f, 0.05f, 0.02f, 0.96f);
        public static readonly Color Ink = Cream;
        public static readonly Color ParchmentInk = Cream;
        public static readonly Color ParchmentTitle = new Color(1f, 0.86f, 0.48f, 1f);
        public static readonly Color DisabledTint = new Color(0.40f, 0.38f, 0.50f, 1f);

        public static bool Has(string key) => SurvivalArt.Get(key) != null;

        /// <summary>Sliced panel art, or the old flat gold-lined frame.</summary>
        public static Image Panel(Transform parent, string name, string key, Color fallbackFill, float borderScale = 1f)
        {
            var sprite = SurvivalArt.Get(key);
            if (sprite == null)
            {
                var flat = SurvivalVisuals.Framed(parent, name, fallbackFill, SurvivalVisuals.GoldLine, 2f);
                Ornaments.Dress(flat, Ornaments.Gold, inset: Ornaments.Navy);
                return flat;
            }

            var img = SurvivalVisuals.Image(parent, name, Color.white, sprite);
            ApplySliced(img, borderScale);
            Ornaments.Dress(img, Ornaments.Gold);
            return img;
        }

        /// <summary>Re-skins an existing flat Image with pack art when present.</summary>
        public static bool TrySkin(Image img, string key, float borderScale = 1f)
        {
            var sprite = SurvivalArt.Get(key);
            if (sprite == null)
            {
                return false;
            }

            img.sprite = sprite;
            img.color = Color.white;
            ApplySliced(img, borderScale);
            return true;
        }

        public static void ApplySliced(Image img, float borderScale)
        {
            if (SurvivalArt.IsSliced(img.sprite))
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.fillCenter = true;
                img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, borderScale);
            }
            else
            {
                img.type = UnityEngine.UI.Image.Type.Simple;
            }
        }

        /// <summary>Plain art (buildings, icons, chest) with preserved aspect, or a coloured disc.</summary>
        public static Image Art(Transform parent, string name, string key, Color fallback)
        {
            var sprite = SurvivalArt.Get(key);
            var img = sprite != null
                ? SurvivalVisuals.Image(parent, name, Color.white, sprite)
                : SurvivalVisuals.Circle(parent, name, fallback);
            img.preserveAspect = true;
            return img;
        }

        /// <summary>Resource icon (gold, stone, wood, food, gems, speed-up) or a disc with a letter glyph.</summary>
        public static Image ResIcon(Transform parent, string name, string resourceId, Color fallback, string glyph)
        {
            // Hero shards ("shard.{hero id}", battle rewards) share one painted shard icon.
            var sprite = resourceId.StartsWith("shard.", System.StringComparison.Ordinal)
                ? SurvivalArt.Get(Survival.Domain.Campaign.CampaignArtKeys.IconShard)
                : SurvivalArt.Get(HookArtKeys.ForResource(resourceId));
            if (sprite != null)
            {
                var img = SurvivalVisuals.Image(parent, name, Color.white, sprite);
                img.preserveAspect = true;
                return img;
            }

            // No painted icon: the procedural Theme A icon (tome, banner, hourglass, coin, wheat, gem, shard) before a glyph disc.
            var drawn = Ornaments.IconFor(resourceId);
            if (drawn != null)
            {
                var icon = SurvivalVisuals.Image(parent, name, Color.white, drawn);
                icon.preserveAspect = true;
                return icon;
            }

            var disc = SurvivalVisuals.Circle(parent, name, fallback);
            var g = SurvivalVisuals.Text(disc.transform, "Glyph", glyph, 24, TextAnchor.MiddleCenter, SurvivalVisuals.Panel);
            SurvivalVisuals.Stretch(g.rectTransform);
            return disc;
        }

        public static Button Button(Transform parent, string name, string key, Color fallback, float borderScale = 0.5f)
        {
            var btn = SurvivalVisuals.Button(parent, name, fallback);
            var img = btn.GetComponent<Image>();
            TrySkin(img, key, borderScale);
            Ornaments.Dress(img, Ornaments.Gold, button: true, fill: key == HookArtKeys.ButtonGold ? Ornaments.AmberFill : Ornaments.DeepFill);
            var colors = btn.colors;
            colors.pressedColor = new Color(0.78f, 0.74f, 0.68f, 1f);
            btn.colors = colors;
            return btn;
        }

        /// <summary>Swap a button to another kit colour; disabled greys the art (or uses the flat fallback colour).</summary>
        public static void SetButton(Image img, string key, Color fallback, bool enabled, float borderScale = 0.5f)
        {
            var sprite = SurvivalArt.Get(key);
            if (sprite == null)
            {
                img.color = fallback;
                return;
            }

            if (img.sprite != sprite)
            {
                img.sprite = sprite;
                ApplySliced(img, borderScale);
            }

            img.color = enabled ? Color.white : DisabledTint;
        }

        /// <summary>Ink colour that reads on a kit button (dark on gold, white on blue / green / red).</summary>
        public static Color InkFor(string buttonKey) => Cream;

        /// <summary>Gold-framed progress bar. The returned fill is a horizontal Filled image when art exists.</summary>
        public static Image Bar(Transform parent, string name, out Image fill)
        {
            var frameSprite = SurvivalArt.Get(HookArtKeys.BarFrame);
            var frame = SurvivalVisuals.Image(parent, name, frameSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.55f), frameSprite);
            if (frameSprite != null)
            {
                ApplySliced(frame, 0.5f);
            }

            var fillSprite = SurvivalArt.Get(HookArtKeys.BarFillGreen);
            fill = SurvivalVisuals.Image(frame.transform, "Fill", Color.white, fillSprite);
            var rt = fill.rectTransform;
            if (fillSprite != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(5f, 4f);
                rt.offsetMax = new Vector2(-5f, -4f);
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
                fill.fillAmount = 0f;
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(0f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            return frame;
        }

        /// <summary>Set bar progress. <paramref name="fillKey"/> picks green / gold / blue art; colour is the flat fallback.</summary>
        public static void SetBar(Image fill, float fraction, string fillKey, Color fallback)
        {
            fraction = Mathf.Clamp01(fraction);
            if (fill.type == UnityEngine.UI.Image.Type.Filled)
            {
                var sprite = SurvivalArt.Get(fillKey);
                if (sprite != null && fill.sprite != sprite)
                {
                    fill.sprite = sprite;
                }

                fill.fillAmount = fraction;
                fill.color = Color.white;
                return;
            }

            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(fraction, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fill.color = fallback;
        }

        /// <summary>Readable game text: dark outline plus a soft drop shadow.</summary>
        public static Text Outlined(Text text, float distance = 2.2f, float alpha = 0.96f)
        {
            // Never thinner than 2px, near-black brown, plus a drop shadow below.
            distance = Mathf.Max(2f, distance);
            var outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(OutlineInk.r, OutlineInk.g, OutlineInk.b, Mathf.Max(0.9f, alpha));
            outline.effectDistance = new Vector2(distance, -distance);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(0f, -distance * 1.6f);
            return text;
        }

        /// <summary>
        /// Best-fit a label into its rect: it shrinks between <paramref name="min"/> and <paramref name="max"/> instead of
        /// spilling over the edges (labels are built with overflow on, which lets long words run past a button).
        /// </summary>
        public static Text Fit(Text text, int min, int max)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(min, max);
            text.resizeTextMaxSize = max;
            return text;
        }

        /// <summary>Stretch a label over its button, inset so it clears the painted rim.</summary>
        public static void PadLabel(RectTransform rect, float padX = 0.08f, float padY = 0.14f)
        {
            rect.anchorMin = new Vector2(padX, padY);
            rect.anchorMax = new Vector2(1f - padX, 1f - padY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Title banner straddling the top edge of a sheet panel, with the title on it.</summary>
        public static Text Header(RectTransform panel, string title, float width, float height, int size)
        {
            var sprite = SurvivalArt.Get(HookArtKeys.Header);
            RectTransform host;
            if (sprite != null)
            {
                var img = SurvivalVisuals.Image(panel, "Header", Color.white, sprite);
                ApplySliced(img, 0.75f);
                host = img.rectTransform;
            }
            else
            {
                host = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
                host.SetParent(panel, false);
            }

            host.anchorMin = new Vector2(0.5f, 1f);
            host.anchorMax = new Vector2(0.5f, 1f);
            host.pivot = new Vector2(0.5f, 0.5f);
            host.sizeDelta = new Vector2(width, height);
            host.anchoredPosition = new Vector2(0f, -height * 0.12f);
            var text = SurvivalVisuals.Text(host, "Title", title, size, TextAnchor.MiddleCenter, SurvivalVisuals.Reward);
            SurvivalVisuals.Stretch(text.rectTransform);
            return Outlined(text, 2.5f);
        }

        /// <summary>Soft additive-looking glow disc behind something (FREE badge, chest, level-up).</summary>
        public static Image Glow(Transform parent, string name, Color color)
        {
            var sprite = SurvivalArt.Get(HookArtKeys.FxGlow);
            var img = sprite != null
                ? SurvivalVisuals.Image(parent, name, color, sprite)
                : SurvivalVisuals.Circle(parent, name, color);
            img.preserveAspect = true;
            return img;
        }
    }
}
