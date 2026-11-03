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
        partial void EngageTick_KingdomPath();
        partial void EngageTick_Timers();

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
            EngageTick_KingdomPath();
            EngageTick_Timers();
        }

        /// <summary>Is any campaign screen or sheet open (so a feature does not pop over it)?</summary>
        internal bool EngageBusy => _screen != null || _sheet != null || _inBattle;

        /// <summary>
        /// A rail button: slot 0 is the top of the left rail (under the quest tracker), each next slot below it.
        /// Gold filigree frame on a royal disc, a big glyph, a ribbon label and a red count badge.
        /// </summary>
        private RectTransform RailButton(int slot, string name, string glyph, string label, Color disc, Action tap, out Image badge, out Text badgeCount, string? iconKey = null)
        {
            var root = _play!.Root!;
            var parts = SurvivalSideRail.Round(root, RailPrefix + name, iconKey, label, disc);
            var rt = parts.Root;
            var top = 0.300f + (slot * 0.078f);
            HookSkin.Box(rt, 0.018f, 1f - (top + 0.070f), 0.150f, 1f - top);
            if (iconKey == null || parts.Icon.childCount == 0)
            {
                var g = HookSkin.Outlined(SurvivalVisuals.Text(parts.Icon, "Glyph", glyph, 40, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
                g.raycastTarget = false;
                SurvivalVisuals.Stretch(g.rectTransform);
            }

            badge = parts.Dot;
            badgeCount = parts.DotText;
            parts.Button.onClick.AddListener(() =>
            {
                Juice(rt);
                tap();
            });
            // Join the shared side rail (HUD declutter); the Box above is only the no-rail fallback.
            _play.RegisterRail(RailPrefix + name, top, rt);
            return rt;
        }

        /// <summary>Show a rail icon only while there is something to do in it (see RailActionable).</summary>
        private void RailShown(string name, bool shown) => _play?.SetRailShown(RailPrefix + name, shown);

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
