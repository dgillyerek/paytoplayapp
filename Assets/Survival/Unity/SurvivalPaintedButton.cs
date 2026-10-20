#nullable enable
using Survival.Domain.Theme;
using Survival.Domain.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Painted bottom-bar style shared by the nav buttons (Heroes / Alliance / Bag / Battle) and the village Build
    /// button: gold-bevel frame (theme_a.ui.nav_button, 9-sliced), a painted icon popping out of the top and a bold
    /// outlined label under it, with a squash while pressed. Without pack art it falls back to the blue kit button,
    /// then to a code-drawn gold-lined frame with a gold disc for the icon.
    /// </summary>
    internal static class SurvivalPaintedButton
    {
        public static readonly Color FallbackFill = new Color(0.10f, 0.22f, 0.45f, 0.96f);

        public static Button Create(Transform parent, string name, string iconKey, string label, out Image icon, out Text text, int fontSize = 22)
        {
            Button btn;
            if (HookSkin.Has(BottomNav.ButtonArt))
            {
                btn = HookSkin.Button(parent, name, BottomNav.ButtonArt, FallbackFill, 0.42f);
            }
            else if (HookSkin.Has(HookArtKeys.ButtonBlue))
            {
                btn = HookSkin.Button(parent, name, HookArtKeys.ButtonBlue, FallbackFill, 0.42f);
            }
            else
            {
                // Code-drawn: gold line around a deep blue fill.
                var framed = SurvivalVisuals.Framed(parent, name, FallbackFill, SurvivalVisuals.GoldLine, 3f);
                framed.raycastTarget = true;
                btn = framed.gameObject.AddComponent<Button>();
                btn.targetGraphic = framed;
            }

            var rt = btn.GetComponent<RectTransform>();
            icon = HookSkin.Art(rt, "Icon", iconKey, SurvivalVisuals.Gold);
            icon.raycastTarget = false;
            icon.rectTransform.anchorMin = new Vector2(0.12f, 0.30f);
            icon.rectTransform.anchorMax = new Vector2(0.88f, 1.14f);
            icon.rectTransform.offsetMin = Vector2.zero;
            icon.rectTransform.offsetMax = Vector2.zero;
            text = HookSkin.Outlined(SurvivalVisuals.Text(rt, "T", label, fontSize, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
            text.fontStyle = FontStyle.Bold;
            text.rectTransform.anchorMin = new Vector2(0f, 0.03f);
            text.rectTransform.anchorMax = new Vector2(1f, 0.32f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            btn.transition = Selectable.Transition.None;
            btn.gameObject.AddComponent<PressSquash>();
            return btn;
        }

        /// <summary>Carved wood / stone bar with a gold trim behind the nav buttons (or the flat panel).</summary>
        public static Image Bar(Transform parent, string name)
        {
            var bar = SurvivalVisuals.Image(parent, name, SurvivalVisuals.Panel);
            if (!HookSkin.TrySkin(bar, BottomNav.BarArt, 0.5f))
            {
                HookSkin.TrySkin(bar, HookArtKeys.Panel, 0.5f);
            }

            return bar;
        }
    }

    /// <summary>Pressed feel: squash wide-and-short while held, spring back on release.</summary>
    public sealed class PressSquash : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private static readonly Vector3 Pressed = new Vector3(1.07f, 0.88f, 1f);
        private bool _down;

        public void OnPointerDown(PointerEventData eventData)
        {
            _down = true;
            transform.localScale = Pressed;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_down)
            {
                return;
            }

            _down = false;
            transform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            _down = false;
            transform.localScale = Vector3.one;
        }
    }
}
