#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// The left-side engagement rail on the Play HUD (Daily login, Trophy Hall, Crusade Banner, Guild help): one painted
    /// round button per feature with a red count badge. Each feature lives in its own partial file and plugs in through
    /// the EngageAttach_* / EngageTick_* partial methods below (a feature that is not compiled in is simply skipped).
    /// SurvivalEngageDriver calls them once the campaign view is built, so the main view file stays untouched.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        public const string RailPrefix = "EngageRail_";

        partial void EngageAttach_Daily();
        partial void EngageTick_Daily();
        partial void EngageAttach_Trophy();
        partial void EngageTick_Trophy();
        partial void EngageAttach_Crusade();
        partial void EngageTick_Crusade();
        partial void EngageAttach_Guild();
        partial void EngageTick_Guild();

        private bool _engageAttached;

        /// <summary>True once the Play HUD and the campaign are up, so rail features can attach.</summary>
        internal bool EngageReady => Campaign != null && _play != null && _play.Root != null && _overlay != null;

        internal void EngageTick()
        {
            if (!EngageReady)
            {
                return;
            }

            if (!_engageAttached)
            {
                _engageAttached = true;
                EngageAttach_Daily();
                EngageAttach_Trophy();
                EngageAttach_Crusade();
                EngageAttach_Guild();
            }

            EngageTick_Daily();
            EngageTick_Trophy();
            EngageTick_Crusade();
            EngageTick_Guild();
        }

        /// <summary>Is any campaign screen or sheet open (so a feature does not pop over it)?</summary>
        internal bool EngageBusy => _screen != null || _sheet != null || _inBattle;

        /// <summary>
        /// A rail button: slot 0 is the top of the left rail (under the quest tracker), each next slot below it.
        /// Gold filigree frame on a royal disc, a big glyph, a ribbon label and a red count badge.
        /// </summary>
        private RectTransform RailButton(int slot, string name, string glyph, string label, Color disc, Action tap, out Image badge, out Text badgeCount)
        {
            var root = _play!.Root!;
            var btn = SurvivalVisuals.Button(root, RailPrefix + name, new Color(1f, 1f, 1f, 0f));
            btn.transition = Selectable.Transition.None;
            var rt = btn.GetComponent<RectTransform>();
            var top = 0.300f + (slot * 0.078f);
            Box(rt, 0.018f, 1f - (top + 0.070f), 0.150f, 1f - top);

            var glow = HookSkin.Glow(btn.transform, "Glow", new Color(disc.r, disc.g, disc.b, 0.45f));
            Box(glow.rectTransform, -0.18f, -0.12f, 1.18f, 1.22f);
            var ring = SurvivalVisuals.Circle(btn.transform, "GoldRing", Ornaments.Gold);
            ring.preserveAspect = true;
            ring.raycastTarget = false;
            Box(ring.rectTransform, 0.06f, 0.18f, 0.94f, 1.04f);
            var face = SurvivalVisuals.Circle(btn.transform, "Disc", disc);
            Box(face.rectTransform, 0.12f, 0.24f, 0.88f, 0.98f);
            face.preserveAspect = true;
            Ornaments.Sparkles(face.rectTransform, 3, Ornaments.Gold);
            var g = HookSkin.Outlined(SurvivalVisuals.Text(face.transform, "Glyph", glyph, 44, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
            g.raycastTarget = false;
            SurvivalVisuals.Stretch(g.rectTransform);

            var ribbonText = Ornaments.Ribbon(btn.transform, label, new Color(0.55f, 0.12f, 0.12f, 1f), out var ribbon);
            Box(ribbon.rectTransform, -0.10f, -0.02f, 1.10f, 0.30f);
            ribbonText.fontSize = 18;
            HookSkin.Fit(ribbonText, 10, 18);

            badge = SurvivalVisuals.Circle(btn.transform, "Badge", new Color(0.90f, 0.18f, 0.20f, 1f));
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.86f, 0.90f);
            badge.rectTransform.sizeDelta = new Vector2(34f, 34f);
            badge.raycastTarget = false;
            badgeCount = HookSkin.Outlined(SurvivalVisuals.Text(badge.transform, "T", "!", 22, TextAnchor.MiddleCenter, Color.white), 1.2f);
            badgeCount.fontStyle = FontStyle.Bold;
            badgeCount.raycastTarget = false;
            SurvivalVisuals.Stretch(badgeCount.rectTransform);
            badge.gameObject.SetActive(false);

            btn.onClick.AddListener(() =>
            {
                Juice(rt);
                tap();
            });
            return rt;
        }

        /// <summary>Show the badge with a count (hidden at 0; "!" when count is negative).</summary>
        private static void RailBadge(Image? badge, Text? count, int n)
        {
            if (badge == null || count == null)
            {
                return;
            }

            var show = n != 0;
            if (badge.gameObject.activeSelf != show)
            {
                badge.gameObject.SetActive(show);
            }

            count.text = n < 0 ? "!" : n > 99 ? "99+" : n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Hours and minutes left, e.g. "5h 12m", "48m", "3d 4h".</summary>
        internal static string RailTime(double seconds)
        {
            var s = Math.Max(0, (long)Math.Ceiling(seconds));
            var d = s / 86400;
            var h = (s % 86400) / 3600;
            var m = (s % 3600) / 60;
            if (d > 0)
            {
                return d + "d " + h + "h";
            }

            if (h > 0)
            {
                return h + "h " + m + "m";
            }

            return m > 0 ? m + "m " + (s % 60) + "s" : (s % 60) + "s";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallEngageDriver()
        {
            if (FindFirstObjectByType<SurvivalEngageDriver>() != null)
            {
                return;
            }

            var go = new GameObject("SurvivalEngageDriver");
            DontDestroyOnLoad(go);
            go.AddComponent<SurvivalEngageDriver>();
        }
    }

    /// <summary>Finds the Play shell's campaign view and drives the engagement rail features (attach once, tick 4x a second).</summary>
    public sealed class SurvivalEngageDriver : MonoBehaviour
    {
        private SurvivalCampaignView? _view;
        private float _t;

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_t < 0.25f)
            {
                return;
            }

            _t = 0f;
            if (_view == null)
            {
                _view = FindFirstObjectByType<SurvivalCampaignView>();
            }

            if (_view != null)
            {
                _view.EngageTick();
            }
        }
    }
}
