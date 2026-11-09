#nullable enable
using System;
using System.Text;
using Survival.Domain.Kids;
using Survival.Domain.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity.Kids
{
    /// <summary>
    /// First-launch neutral age question: pick a birth month, type a birth year, Continue. No default answer, no "13+" hint,
    /// no wording that a certain age gets more, and no close / back (Android back does nothing here). The month and year
    /// are thrown away once the band is saved. Closing the app mid-question asks again next launch.
    /// </summary>
    public sealed class AgeGateScreen : MonoBehaviour
    {
        private static readonly string[] MonthFallback = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        private readonly StringBuilder _year = new StringBuilder(4);
        private readonly KidUi.Btn[] _months = new KidUi.Btn[12];
        private int? _month;
        private Text? _yearText;
        private Text? _error;
        private KidUi.Btn? _continue;
        private Action? _done;

        /// <summary>Shows the gate over everything; <paramref name="done"/> runs once the answer is saved.</summary>
        public static void Show(Transform parent, Action done)
        {
            KidSafety.Store.MarkShown();
            var card = KidUi.Modal(parent, "AgeGateCanvas", 500, 0.05f, 0.14f, 0.95f, 0.86f, out var root);
            var screen = root.gameObject.AddComponent<AgeGateScreen>();
            screen._done = done;
            screen.Build(card);
        }

        private void Build(RectTransform card)
        {
            HookSkin.Header(card, KidUi.S("kids.age_gate.title", "Welcome!"), 520f, 96f, 34);
            KidUi.Label(card, "Prompt", KidUi.S("kids.age_gate.prompt", "When were you born?"), 32, 0.06f, 0.83f, 0.94f, 0.90f);

            KidUi.Label(card, "MonthHead", KidUi.S("kids.age_gate.month", "Month"), 22, 0.06f, 0.77f, 0.94f, 0.815f, SurvivalVisuals.Reward);
            for (var i = 0; i < 12; i++)
            {
                var col = i % 4;
                var row = i / 4;
                var x0 = 0.06f + col * 0.22f;
                var y1 = 0.765f - row * 0.07f;
                var m = i + 1;
                _months[i] = KidUi.Button(card, "Month" + m, KidUi.S("kids.age_gate.month." + m, MonthFallback[i]), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 22, x0, y1 - 0.062f, x0 + 0.20f, y1, () => PickMonth(m));
            }

            KidUi.Label(card, "YearHead", KidUi.S("kids.age_gate.year", "Year"), 22, 0.06f, 0.50f, 0.94f, 0.545f, SurvivalVisuals.Reward);
            var box = SurvivalVisuals.Framed(card, "YearBox", SurvivalVisuals.RowFill, SurvivalVisuals.GoldLine, 2f);
            HookSkin.Box(box.rectTransform, 0.30f, 0.435f, 0.70f, 0.495f);
            _yearText = SurvivalVisuals.Text(box.transform, "Year", "", 34, TextAnchor.MiddleCenter, HookSkin.Cream);
            SurvivalVisuals.Stretch(_yearText.rectTransform);

            // Keypad: 1-9, then erase / 0.
            for (var k = 0; k < 11; k++)
            {
                var col = k < 9 ? k % 3 : k == 9 ? 0 : 1;
                var row = k < 9 ? k / 3 : 3;
                var x0 = 0.20f + col * 0.205f;
                var y1 = 0.425f - row * 0.068f;
                if (k == 9)
                {
                    KidUi.Button(card, "Erase", "\u2190", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 26, x0, y1 - 0.060f, x0 + 0.19f, y1, Erase);
                    continue;
                }

                var digit = k == 10 ? 0 : k + 1;
                KidUi.Button(card, "Key" + digit, digit.ToString(), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 28, x0, y1 - 0.060f, x0 + 0.19f, y1, () => Type(digit));
            }

            _error = KidUi.Label(card, "Error", "", 20, 0.06f, 0.13f, 0.94f, 0.17f, new Color(1f, 0.72f, 0.62f, 1f));
            _continue = KidUi.Button(card, "Continue", KidUi.S("kids.age_gate.continue", "Continue"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 28, 0.25f, 0.03f, 0.75f, 0.12f, Submit);
            Refresh();
        }

        private void PickMonth(int m)
        {
            _month = m;
            Refresh();
        }

        private void Type(int digit)
        {
            if (_year.Length < 4)
            {
                _year.Append((char)('0' + digit));
            }

            Refresh();
        }

        private void Erase()
        {
            if (_year.Length > 0)
            {
                _year.Length--;
            }

            Refresh();
        }

        private int? Year => _year.Length == 4 ? int.Parse(_year.ToString()) : null;

        private void Refresh()
        {
            for (var i = 0; i < 12; i++)
            {
                var on = _month == i + 1;
                _months[i].Skin(on ? HookArtKeys.ButtonGold : HookArtKeys.ButtonBlue, on ? SurvivalVisuals.MineFill : SurvivalVisuals.PinBuild);
            }

            if (_yearText != null)
            {
                _yearText.text = _year.Length == 0 ? "- - - -" : _year.ToString();
            }

            _continue?.Set(_month != null && _year.Length == 4);
        }

        private void Submit()
        {
            var config = KidSafety.Config;
            var today = DateTime.Now;
            var err = AgeGate.Validate(Year, _month, today, config);
            if (err != AgeEntryError.None)
            {
                if (_error != null)
                {
                    _error.text = err == AgeEntryError.MissingMonth
                        ? KidUi.S("kids.age_gate.err_month", "Please pick a month.")
                        : KidUi.S("kids.age_gate.err_year", "Please check the year.");
                }

                return;
            }

            KidSafety.SaveAnswer(AgeGate.Classify(Year!.Value, _month!.Value, today, config));
            _year.Clear();
            _month = null;
            var done = _done;
            _done = null;
            Destroy(gameObject);
            done?.Invoke();
        }
    }
}
