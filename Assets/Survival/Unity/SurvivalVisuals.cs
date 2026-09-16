using Survival.Domain.Layout;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survival.Unity
{
    internal static class SurvivalVisuals
    {
        public static readonly Color Panel = new Color(13f / 255f, 16f / 255f, 23f / 255f, 0.92f);
        public static readonly Color PanelSoft = new Color(18f / 255f, 22f / 255f, 32f / 255f, 0.88f);
        public static readonly Color Gold = new Color(0.83f, 0.69f, 0.32f, 1f);
        public static readonly Color Cream = new Color(0.93f, 0.90f, 0.82f, 1f);
        public static readonly Color Mute = new Color(0.70f, 0.72f, 0.68f, 1f);
        public static readonly Color WorldTab = new Color(0.55f, 0.42f, 0.16f, 1f);
        public static readonly Color PinHome = new Color(0.45f, 0.78f, 0.95f, 1f);
        public static readonly Color PinGather = new Color(0.95f, 0.62f, 0.22f, 1f);
        public static readonly Color PinBuild = new Color(0.35f, 0.55f, 0.95f, 1f);
        public static readonly Color PinGuild = new Color(0.40f, 0.62f, 0.95f, 1f);
        public static readonly Color PinFight = new Color(0.72f, 0.38f, 0.95f, 1f);
        public static readonly Color PinExplore = new Color(0.35f, 0.85f, 0.90f, 1f);
        public static readonly Color InspectFill = new Color(0.055f, 0.06f, 0.075f, 0.97f);
        public static readonly Color RowFill = new Color(0.09f, 0.105f, 0.14f, 0.96f);
        public static readonly Color MineFill = new Color(0.74f, 0.58f, 0.20f, 1f);
        public static readonly Color GoldLine = new Color(0.78f, 0.62f, 0.24f, 1f);
        public static readonly Color Reward = new Color(0.93f, 0.84f, 0.45f, 1f);
        public static readonly Color ThreatLine = new Color(0.62f, 0.36f, 0.88f, 1f);
        public static readonly Color AttackFill = new Color(0.28f, 0.12f, 0.40f, 1f);
        public static readonly Color ThreatSpark = new Color(0.78f, 0.42f, 1f, 0.95f);

        private static Font? _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                                new[]
                                {
                                    "Liberation Sans Bold",
                                    "DejaVu Sans Bold",
                                    "Arial Bold",
                                    "Liberation Sans",
                                    "DejaVu Sans",
                                    "Arial"
                                },
                                32)
                            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _font;
            }
        }

        public static void EnsurePlayCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                go.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.20f, 0.28f, 0.16f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform Canvas(Transform parent, string name, int sorting)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sorting;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(PlayHudLayout.ReferenceWidth, PlayHudLayout.ReferenceHeight);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            return go.GetComponent<RectTransform>();
        }

        public static Image Image(Transform parent, string name, Color color, Sprite? sprite = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite ?? SurvivalArt.White();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Circle(Transform parent, string name, Color color)
        {
            var img = Image(parent, name, color, SurvivalArt.Circle());
            img.preserveAspect = true;
            return img;
        }

        public static Image Ring(Transform parent, string name, Color color)
        {
            var img = Image(parent, name, color, SurvivalArt.Ring());
            img.preserveAspect = true;
            return img;
        }

        public static Image Framed(Transform parent, string name, Color fill, Color frame, float pad)
        {
            var outer = Image(parent, name, frame);
            var inner = Image(outer.transform, "Fill", fill);
            Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(pad, pad);
            inner.rectTransform.offsetMax = new Vector2(-pad, -pad);
            return outer;
        }

        public static void AnchorBox(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(x - w * 0.5f, y - h * 0.5f);
            rt.anchorMax = new Vector2(x + w * 0.5f, y + h * 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Text Text(Transform parent, string name, string value, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string name, Color color)
        {
            var img = Image(parent, name, color);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        public static void Place(RectTransform rt, HudRect rect)
        {
            rt.anchorMin = new Vector2(rect.XMin, rect.YMin);
            rt.anchorMax = new Vector2(rect.XMax, rect.YMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
