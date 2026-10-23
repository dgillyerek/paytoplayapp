#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Campaign
{
    /// <summary>Theme A pack.json art for the battle loading screen (each piece has a code-drawn fallback).</summary>
    public static class BattleLoadingArtKeys
    {
        public const string Backdrop = "theme_a.battle.loading_backdrop";
        public const string Emblem = "theme_a.battle.crossed_swords";

        public static IEnumerable<string> All()
        {
            yield return Backdrop;
            yield return Emblem;
        }
    }

    public enum BattleTransitionPhase
    {
        Hidden,
        FadingIn,
        Holding,
        FadingOut
    }

    /// <summary>
    /// Timing of the painted loading screen between Play and BattleLocked (both ways). It fades in over the
    /// current view, holds while the scene loads or unloads, and only fades out once the destination camera has
    /// rendered a frame and the screen has been up at least <see cref="MinShowSeconds"/> (so it never flashes).
    /// The bar eases towards the reported load progress, never goes backwards, and fills when the target renders.
    /// </summary>
    public sealed class BattleTransition
    {
        public const double MinShowSeconds = 0.6;
        public const double FadeInSeconds = 0.18;
        public const double FadeOutSeconds = 0.35;

        /// <summary>
        /// Last-resort net, counted from the moment the screen went up: if the destination has still not drawn by
        /// then (a trip that threw part-way, a scene that never shows a camera), the screen clears anyway rather
        /// than freezing the game under it.
        /// </summary>
        public const double ShowLimitSeconds = 20.0;

        /// <summary>The bar tops out here until the destination camera has actually drawn.</summary>
        public const float LoadedBarShare = 0.9f;

        private const float BarEasePerSecond = 6f;

        private double _shownAt;
        private double _phaseAt;
        private double _lastStep;
        private float _target;
        private bool _rendered;

        public BattleTransitionPhase Phase { get; private set; } = BattleTransitionPhase.Hidden;

        /// <summary>Overlay opacity, 0 (gone) to 1 (fully covering).</summary>
        public float Alpha { get; private set; }

        /// <summary>Bar fill as drawn, 0..1.</summary>
        public float Bar { get; private set; }

        /// <summary>True once the screen fully covers the view (safe to swap scenes and cameras underneath).</summary>
        public bool Covered => Phase == BattleTransitionPhase.Holding || (Phase == BattleTransitionPhase.FadingOut && Alpha >= 1f);

        public bool TargetRendered => _rendered;

        /// <summary>True when the screen cleared on <see cref="ShowLimitSeconds"/> or <see cref="Release"/>, not on a frame.</summary>
        public bool GaveUp { get; private set; }

        public bool Done => Phase == BattleTransitionPhase.Hidden;

        /// <summary>Show the screen. <paramref name="instant"/> skips the fade-in (nothing worth showing underneath).</summary>
        public void Begin(double now, bool instant = false)
        {
            _shownAt = now;
            _phaseAt = now;
            _lastStep = now;
            _target = 0f;
            _rendered = false;
            GaveUp = false;
            Bar = 0f;
            Phase = instant ? BattleTransitionPhase.Holding : BattleTransitionPhase.FadingIn;
            Alpha = instant ? 1f : 0f;
        }

        /// <summary>Scene load / unload progress, 0..1 (Unity's AsyncOperation.progress).</summary>
        public void ReportProgress(float progress)
        {
            var p = Clamp01(progress) * LoadedBarShare;
            if (p > _target)
            {
                _target = p;
            }
        }

        /// <summary>The destination camera has drawn its first frame: the bar fills and the fade-out may start.</summary>
        public void MarkTargetRendered()
        {
            _rendered = true;
            _target = 1f;
        }

        /// <summary>Stop waiting for the destination (the trip failed): the screen fades out after the minimum show time.</summary>
        public void Release()
        {
            if (!_rendered)
            {
                GaveUp = true;
            }

            MarkTargetRendered();
        }

        /// <summary>Advance to <paramref name="now"/> (seconds, unscaled).</summary>
        public void Step(double now)
        {
            var dt = Math.Max(0.0, now - _lastStep);
            _lastStep = now;
            if (Phase == BattleTransitionPhase.Hidden)
            {
                return;
            }

            if (!_rendered && now - _shownAt >= ShowLimitSeconds)
            {
                Release();
            }

            var ease = (float)(1.0 - Math.Exp(-BarEasePerSecond * dt));
            Bar = Math.Max(Bar, Bar + ((_target - Bar) * ease));
            if (_target >= 1f && _rendered && Phase == BattleTransitionPhase.FadingOut)
            {
                Bar = 1f;
            }

            switch (Phase)
            {
                case BattleTransitionPhase.FadingIn:
                    Alpha = Fraction(now - _phaseAt, FadeInSeconds);
                    if (Alpha >= 1f)
                    {
                        Enter(BattleTransitionPhase.Holding, now);
                    }

                    break;
                case BattleTransitionPhase.Holding:
                    Alpha = 1f;
                    if (_rendered && now - _shownAt >= MinShowSeconds)
                    {
                        Bar = 1f;
                        Enter(BattleTransitionPhase.FadingOut, now);
                    }

                    break;
                case BattleTransitionPhase.FadingOut:
                    Alpha = 1f - Fraction(now - _phaseAt, FadeOutSeconds);
                    if (Alpha <= 0f)
                    {
                        Alpha = 0f;
                        Phase = BattleTransitionPhase.Hidden;
                    }

                    break;
            }
        }

        /// <summary>Tip line for the screen: a stable pick per <paramref name="seed"/> (stage id, trip count).</summary>
        public static string Tip(IReadOnlyList<string> tips, int seed)
        {
            if (tips.Count == 0)
            {
                return "";
            }

            var i = seed % tips.Count;
            return tips[i < 0 ? i + tips.Count : i];
        }

        /// <summary>Deterministic string hash (string.GetHashCode is randomised per process).</summary>
        public static int StableSeed(string s)
        {
            unchecked
            {
                var h = 23;
                foreach (var c in s)
                {
                    h = (h * 31) + c;
                }

                return h & int.MaxValue;
            }
        }

        private void Enter(BattleTransitionPhase phase, double now)
        {
            Phase = phase;
            _phaseAt = now;
            Alpha = phase == BattleTransitionPhase.FadingOut || phase == BattleTransitionPhase.Holding ? 1f : Alpha;
        }

        private static float Fraction(double t, double span) => span <= 0 ? 1f : Clamp01((float)(t / span));

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>What the loading screen shows: a headline, the stage, both sides' portraits by lane, a tip.</summary>
    public sealed record BattleLoadingInfo(
        string Headline,
        string StageName,
        IReadOnlyList<string> HeroPortraits,
        IReadOnlyList<string> EnemyPortraits,
        string Tip,
        bool Returning)
    {
        /// <summary>Into battle: heroes in lane order on the left, the stage's enemies on the right.</summary>
        public static BattleLoadingInfo Into(BattleLineup lineup, string headline, string tip)
        {
            var heroes = new List<LineupFighter>(lineup.Heroes);
            heroes.Sort((a, b) => a.Lane.CompareTo(b.Lane));
            var enemies = new List<LineupFighter>(lineup.Enemies);
            enemies.Sort((a, b) => a.Lane.CompareTo(b.Lane));
            return new BattleLoadingInfo(headline, lineup.Stage.Name, heroes.ConvertAll(h => h.PortraitKey), enemies.ConvertAll(e => e.PortraitKey), tip, false);
        }

        /// <summary>Back to the village: no VS row, the headline says how it went.</summary>
        public static BattleLoadingInfo Back(string headline, string stageName, string tip) =>
            new BattleLoadingInfo(headline, stageName, Array.Empty<string>(), Array.Empty<string>(), tip, true);
    }
}
