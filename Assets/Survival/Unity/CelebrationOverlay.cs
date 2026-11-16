#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Base;
using Survival.Domain.Clock;
using Survival.Domain.Gift;
using Survival.Domain.Ids;
using Survival.Domain.Quest;
using Survival.Domain.CloudSave;
using Survival.Domain.Session;
using Survival.Domain.Store;
using Survival.Domain.Theme;
using Survival.Domain.World;
using Survival.Domain.View;
using UnityEngine;
using Survival.Unity.Kids;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>Split out of <see cref="SurvivalHookView"/>; draws into the view's overlay and uses its helpers.</summary>
    internal sealed class CelebrationOverlay
    {
        private readonly SurvivalHookView _v;
        private bool _celebrating;
        private readonly Queue<IEnumerator> _celebrations = new();

        /// <summary>A celebration is on screen or waiting.</summary>
        internal bool Celebrating => _celebrating;

        internal CelebrationOverlay(SurvivalHookView view)
        {
            _v = view;
        }

        internal void Queue(string title, IReadOnlyDictionary<string, int> rewards)
        {
            _celebrations.Enqueue(Celebration(title, rewards));
            if (!_celebrating)
            {
                _v.StartCoroutine(RunCelebrations());
            }
        }

        private IEnumerator RunCelebrations()
        {
            _celebrating = true;
            while (_celebrations.Count > 0)
            {
                yield return _v.StartCoroutine(_celebrations.Dequeue());
            }

            _celebrating = false;
        }

        private IEnumerator Celebration(string title, IReadOnlyDictionary<string, int> rewards)
        {
            if (_v._overlay == null)
            {
                _v.Release(rewards);
                yield break;
            }

            var dim = SurvivalVisuals.Button(_v._overlay, "Celebration", new Color(SurvivalHookView.Dim.r, SurvivalHookView.Dim.g, SurvivalHookView.Dim.b, 0f));
            dim.transition = Selectable.Transition.None;
            SurvivalVisuals.Stretch(dim.GetComponent<RectTransform>());
            var dimImg = dim.GetComponent<Image>();
            var tapped = false;
            dim.onClick.AddListener(() => tapped = true);
            var host = dim.transform;
            const float cy = 0.55f;

            // Rays: two painted ray wheels turning against each other (procedural quads if the pack has none).
            var rays = new GameObject("Rays", typeof(RectTransform)).GetComponent<RectTransform>();
            rays.SetParent(host, false);
            SurvivalVisuals.AnchorBox(rays, 0.5f, cy, 0f, 0f);
            RectTransform? rays2 = null;
            var raySprite = SurvivalArt.Get(HookArtKeys.FxRays);
            if (raySprite != null)
            {
                var wheel = SurvivalVisuals.Image(rays, "Wheel", new Color(1f, 0.88f, 0.5f, 0f), raySprite);
                wheel.rectTransform.sizeDelta = new Vector2(1500f, 1500f);
                var inner = SurvivalVisuals.Image(host, "Rays2", new Color(1f, 0.95f, 0.75f, 0f), raySprite);
                rays2 = inner.rectTransform;
                SurvivalVisuals.AnchorBox(rays2, 0.5f, cy, 0f, 0f);
                rays2.sizeDelta = new Vector2(950f, 950f);
            }
            else
            {
                for (var r = 0; r < 12; r++)
                {
                    var ray = SurvivalVisuals.Image(rays, "Ray" + r, new Color(1f, 0.86f, 0.40f, 0f));
                    var rayRt = ray.rectTransform;
                    rayRt.anchorMin = new Vector2(0.5f, 0.5f);
                    rayRt.anchorMax = new Vector2(0.5f, 0.5f);
                    rayRt.pivot = new Vector2(0.5f, 0f);
                    rayRt.sizeDelta = new Vector2(70f, 900f);
                    rayRt.localRotation = Quaternion.Euler(0f, 0f, r * 30f);
                }
            }

            var glow = HookSkin.Glow(host, "Glow", new Color(1f, 0.85f, 0.4f, 0f));
            SurvivalVisuals.AnchorBox(glow.rectTransform, 0.5f, cy, 0.9f, 0.9f * SurvivalHookView.Aspect);

            var chestRt = new GameObject("Chest", typeof(RectTransform)).GetComponent<RectTransform>();
            chestRt.SetParent(host, false);
            const float chestW = 0.46f;
            SurvivalVisuals.AnchorBox(chestRt, 0.5f, 0.66f, chestW, chestW * SurvivalHookView.Aspect);
            Image? chestImg = null;
            RectTransform? lid = null;
            var openSprite = SurvivalArt.Get(HookArtKeys.ChestOpen);
            if (HookSkin.Has(HookArtKeys.ChestClosed))
            {
                chestImg = HookSkin.Art(chestRt, "Art", HookArtKeys.ChestClosed, SurvivalHookView.ChestWood);
                SurvivalVisuals.Stretch(chestImg.rectTransform);
            }
            else
            {
                SurvivalHookView.BuildChestArt(chestRt, out lid);
            }

            var titleHost = new GameObject("TitleHost", typeof(RectTransform)).GetComponent<RectTransform>();
            titleHost.SetParent(host, false);
            SurvivalVisuals.AnchorBox(titleHost, 0.5f, 0.765f, 0.94f, 0.085f);
            var ribbon = SurvivalArt.Get(HookArtKeys.RibbonBlue);
            if (ribbon != null)
            {
                var band = SurvivalVisuals.Image(titleHost, "Ribbon", Color.white, ribbon);
                SurvivalVisuals.Stretch(band.rectTransform);
            }

            var titleText = SurvivalVisuals.Text(titleHost, "Title", title, 50, TextAnchor.MiddleCenter, SurvivalVisuals.Reward);
            HookSkin.Box(titleText.rectTransform, 0.12f, 0.22f, 0.88f, 0.98f);
            titleText.fontStyle = FontStyle.Bold;
            HookSkin.Outlined(titleText, 3f);
            titleHost.localScale = Vector3.zero;

            // A: drop in with overshoot.
            var elapsed = 0f;
            while (elapsed < 0.32f)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / 0.32f);
                dimImg.color = new Color(SurvivalHookView.Dim.r, SurvivalHookView.Dim.g, SurvivalHookView.Dim.b, Mathf.Lerp(0f, SurvivalHookView.Dim.a, u));
                var back = 1f + 2.2f * Mathf.Pow(u - 1f, 3f) + 1.2f * Mathf.Pow(u - 1f, 2f);
                SurvivalVisuals.AnchorBox(chestRt, 0.5f, Mathf.Lerp(0.70f, 0.53f, u), chestW, chestW * SurvivalHookView.Aspect);
                chestRt.localScale = Vector3.one * Mathf.Max(0f, back);
                glow.color = new Color(1f, 0.85f, 0.4f, 0.35f * u);
                yield return null;
            }

            chestRt.localScale = Vector3.one;

            // B: shake, building glow.
            elapsed = 0f;
            while (elapsed < 0.48f)
            {
                elapsed += Time.unscaledDeltaTime;
                var k = elapsed / 0.48f;
                chestRt.localRotation = Quaternion.Euler(0f, 0f, 8f * Mathf.Sin(elapsed * 52f) * (0.4f + k));
                chestRt.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(elapsed * 34f));
                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(0.35f, 0.9f, k));
                glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 0.8f, k);
                yield return null;
            }

            chestRt.localRotation = Quaternion.identity;
            chestRt.localScale = Vector3.one;

            // C: burst: chest opens, white flash, rays, rings, sparkles, three confetti cannons, reward rows.
            if (chestImg != null && openSprite != null)
            {
                chestImg.sprite = openSprite;
            }

            var center = new Vector2(0.5f, 0.54f);
            var flash = SurvivalVisuals.Image(host, "Flash", new Color(1f, 1f, 0.92f, 0.85f));
            SurvivalVisuals.Stretch(flash.rectTransform);
            _v.StartCoroutine(_v.RingBurst(center, SurvivalVisuals.Reward, 0.7f));
            _v.StartCoroutine(_v.RingBurst(center, Color.white, 0.45f));
            _v.StartCoroutine(_v.SparkBurst(center, SurvivalVisuals.Reward, 28, 0.22f));
            _v.StartCoroutine(_v.Confetti(host, center, 110, Vector2.zero));
            _v.StartCoroutine(_v.Confetti(host, new Vector2(0.02f, 0.30f), 45, new Vector2(0.75f, 0.35f)));
            _v.StartCoroutine(_v.Confetti(host, new Vector2(0.98f, 0.30f), 45, new Vector2(-0.75f, 0.35f)));
            var sparkles = Twinkles(host, center, 10);
            var rewardRows = new List<RewardRowView>();
            foreach (var pair in rewards)
            {
                if (rewardRows.Count >= 4)
                {
                    _v.Release(pair.Key);
                    continue;
                }

                rewardRows.Add(new RewardRowView(pair.Key, RewardRow(host, pair.Key, pair.Value)));
            }

            elapsed = 0f;
            while (elapsed < 0.55f)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / 0.55f);
                if (lid != null)
                {
                    lid.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 62f, u));
                    lid.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 60f, u));
                }

                flash.color = new Color(1f, 1f, 0.92f, Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(u * 3f)));
                var pop = u < 0.3f ? Mathf.Lerp(1.25f, 0.95f, u / 0.3f) : Mathf.Lerp(0.95f, 1f, (u - 0.3f) / 0.7f);
                chestRt.localScale = Vector3.one * pop;
                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(1f, 0.7f, u));
                glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.25f, u);
                var ts = u < 0.7f ? Mathf.Lerp(0f, 1.15f, u / 0.7f) : Mathf.Lerp(1.15f, 1f, (u - 0.7f) / 0.3f);
                titleHost.localScale = Vector3.one * ts;
                for (var k = 0; k < rewardRows.Count; k++)
                {
                    var rowRt = rewardRows[k].Rt;
                    var y = 0.36f - k * 0.058f;
                    SurvivalVisuals.AnchorBox(rowRt, 0.5f, Mathf.Lerp(0.54f, y, u), 0.66f, 0.05f);
                    rowRt.localScale = Vector3.one * (u < 0.8f ? Mathf.Lerp(0f, 1.15f, u / 0.8f) : Mathf.Lerp(1.15f, 1f, (u - 0.8f) / 0.2f));
                }

                SetRayAlpha(rays, Mathf.Lerp(0f, raySprite != null ? 0.75f : 0.22f, u));
                if (rays2 != null)
                {
                    SetImageAlpha(rays2, Mathf.Lerp(0f, 0.55f, u));
                }

                yield return null;
            }

            UnityEngine.Object.Destroy(flash.gameObject);
            var hint = HookSkin.Outlined(SurvivalVisuals.Text(host, "Hint", _v.S("hook.continue", "Tap to continue"), 24, TextAnchor.MiddleCenter, SurvivalVisuals.Cream));
            SurvivalVisuals.AnchorBox(hint.rectTransform, 0.5f, 0.12f, 0.8f, 0.04f);
            tapped = false;
            elapsed = 0f;
            while (!tapped && elapsed < 3.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                rays.localRotation = Quaternion.Euler(0f, 0f, -elapsed * 18f);
                if (rays2 != null)
                {
                    rays2.localRotation = Quaternion.Euler(0f, 0f, elapsed * 30f);
                }

                var blink = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(elapsed * 3f));
                hint.color = new Color(SurvivalVisuals.Cream.r, SurvivalVisuals.Cream.g, SurvivalVisuals.Cream.b, blink);
                glow.rectTransform.localScale = Vector3.one * (1.25f + 0.06f * Mathf.Sin(elapsed * 5f));
                chestRt.anchoredPosition = new Vector2(0f, 6f * Mathf.Sin(elapsed * 3.5f));
                AnimateTwinkles(sparkles, elapsed);
                yield return null;
            }

            // D: rewards fly to the HUD, overlay fades.
            foreach (var row in rewardRows)
            {
                _v.StartCoroutine(_v.FlyReward(row.Resource, _v.NormCenterOf(row.Rt), 12));
                row.Rt.gameObject.SetActive(false);
            }

            hint.gameObject.SetActive(false);
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(elapsed / 0.3f);
                dimImg.color = new Color(SurvivalHookView.Dim.r, SurvivalHookView.Dim.g, SurvivalHookView.Dim.b, Mathf.Lerp(SurvivalHookView.Dim.a, 0f, u));
                chestRt.localScale = Vector3.one * Mathf.Lerp(1f, 0f, u);
                titleHost.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, u);
                var tc = titleText.color;
                titleText.color = new Color(tc.r, tc.g, tc.b, 1f - u);
                glow.color = new Color(1f, 0.85f, 0.4f, Mathf.Lerp(0.7f, 0f, u));
                SetRayAlpha(rays, Mathf.Lerp(raySprite != null ? 0.75f : 0.22f, 0f, u));
                if (rays2 != null)
                {
                    SetImageAlpha(rays2, Mathf.Lerp(0.55f, 0f, u));
                }

                foreach (var s in sparkles)
                {
                    SetImageAlpha(s, 1f - u);
                }

                yield return null;
            }

            UnityEngine.Object.Destroy(dim.gameObject);
        }

        private sealed class RewardRowView
        {
            public RewardRowView(string resource, RectTransform rt)
            {
                Resource = resource;
                Rt = rt;
            }

            public string Resource { get; }
            public RectTransform Rt { get; }
        }

        private static void SetRayAlpha(RectTransform rays, float alpha)
        {
            for (var i = 0; i < rays.childCount; i++)
            {
                var img = rays.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    var c = img.color;
                    img.color = new Color(c.r, c.g, c.b, alpha);
                }
            }
        }

        private static void SetImageAlpha(RectTransform rt, float alpha)
        {
            var img = rt.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        /// <summary>Twinkling sparkle stars scattered around the chest.</summary>
        private static List<RectTransform> Twinkles(Transform host, Vector2 around, int count)
        {
            var list = new List<RectTransform>();
            var sprite = SurvivalArt.Get(HookArtKeys.FxSparkle);
            for (var i = 0; i < count; i++)
            {
                var img = sprite != null
                    ? SurvivalVisuals.Image(host, "Twinkle" + i, new Color(1f, 0.95f, 0.75f, 1f), sprite)
                    : SurvivalVisuals.Circle(host, "Twinkle" + i, new Color(1f, 0.95f, 0.75f, 1f));
                var ang = i / (float)count * Mathf.PI * 2f + 0.4f;
                var dist = 0.20f + 0.08f * ((i * 37) % 5) / 4f;
                var p = around + new Vector2(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist * SurvivalHookView.Aspect * 1.3f);
                var size = 0.05f + 0.03f * (i % 3);
                SurvivalVisuals.AnchorBox(img.rectTransform, p.x, p.y, size, size * SurvivalHookView.Aspect);
                list.Add(img.rectTransform);
            }

            return list;
        }

        private static void AnimateTwinkles(List<RectTransform> sparkles, float t)
        {
            for (var i = 0; i < sparkles.Count; i++)
            {
                var s = Mathf.Max(0f, Mathf.Sin(t * 4.2f + i * 1.7f));
                sparkles[i].localScale = Vector3.one * (0.3f + 0.9f * s);
                sparkles[i].localRotation = Quaternion.Euler(0f, 0f, t * 90f + i * 20f);
            }
        }

        private RectTransform RewardRow(Transform host, string res, int amount)
        {
            var chipSprite = SurvivalArt.Get(HookArtKeys.Chip);
            var row = chipSprite != null
                ? SurvivalVisuals.Image(host, "Reward_" + res, Color.white, chipSprite).rectTransform
                : new GameObject("Reward_" + res, typeof(RectTransform)).GetComponent<RectTransform>();
            if (chipSprite != null)
            {
                row.SetParent(host, false);
                HookSkin.ApplySliced(row.GetComponent<Image>(), 0.6f);
            }
            else
            {
                row.SetParent(host, false);
            }

            var icon = HookSkin.ResIcon(row, "Icon", res, SurvivalHookView.ResColor(res), SurvivalHookView.ResGlyph(res));
            HookSkin.Box(icon.rectTransform, 0.02f, -0.35f, 0.24f, 1.35f);
            var label = HookSkin.Outlined(SurvivalVisuals.Text(row, "T", "+" + ChipWallet.FormatGrouped(amount) + " " + _v.ResName(res), 36, TextAnchor.MiddleLeft, SurvivalVisuals.Reward), 2.5f);
            label.fontStyle = FontStyle.Bold;
            HookSkin.Box(label.rectTransform, 0.27f, 0f, 1f, 1f);
            row.localScale = Vector3.zero;
            return row;
        }
    }
}
