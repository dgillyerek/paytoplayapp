#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// Kingdom Path on the town screen (Derek 2026-11-02: "take the kingdom path out of the heros popup and put it on the
    /// main screen"). A compact round icon on the shared side rail, under the other rail icons, so it never covers a
    /// building. It is the free progression track, so it always shows; the green FREE badge breathes while a reward is
    /// claimable. Tap opens the Kingdom Path sheet.
    /// </summary>
    public sealed partial class SurvivalCampaignView
    {
        public const string KingdomPathRailName = "KingdomPath";
        private const int KingdomPathRailSlot = 4;

        private bool _pathRailBuilt;
        private Image? _pathFree;

        partial void EngageTick_KingdomPath()
        {
            if (Track == null)
            {
                return;
            }

            if (!_pathRailBuilt)
            {
                _pathRailBuilt = true;
                var root = _play!.Root!;
                var parts = SurvivalSideRail.Round(root, RailPrefix + KingdomPathRailName, null, S("reward_track.rail", "PATH"), new Color(0.20f, 0.42f, 0.70f, 1f));
                var rt = parts.Root;
                var top = 0.300f + (KingdomPathRailSlot * 0.078f);
                HookSkin.Box(rt, 0.018f, 1f - (top + 0.070f), 0.150f, 1f - top);
                var g = HookSkin.Outlined(SurvivalVisuals.Text(parts.Icon, "Glyph", "\u2691", 40, TextAnchor.MiddleCenter, SurvivalVisuals.Cream), 2f);
                g.raycastTarget = false;
                SurvivalVisuals.Stretch(g.rectTransform);
                parts.Button.onClick.AddListener(() =>
                {
                    Juice(rt);
                    OpenRewardTrack();
                });
                _pathFree = parts.Free;
                _play.RegisterRail(RailPrefix + KingdomPathRailName, top, rt);
            }

            var n = Track.ClaimableCount();
            RailShown(KingdomPathRailName, Survival.Domain.View.RailActionable.KingdomPath(n));
            if (_pathFree != null)
            {
                var show = Survival.Domain.View.RailActionable.KingdomPathFree(n);
                if (_pathFree.gameObject.activeSelf != show)
                {
                    _pathFree.gameObject.SetActive(show);
                    if (show)
                    {
                        StartCoroutine(Breathe(_pathFree.rectTransform));
                    }
                }
            }
        }
    }
}
