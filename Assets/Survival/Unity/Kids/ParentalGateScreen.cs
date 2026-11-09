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
    /// "Ask a grown-up" before a purchase in child mode: a multiplication on a keypad, a fresh one after each miss, and a
    /// lockout after child_safety.json parental_gate.max_failures misses. No timer, no pressure copy; Cancel always works.
    /// </summary>
    public sealed class ParentalGateScreen : MonoBehaviour
    {
        /// <summary>The <see cref="IParentalGate"/> the purchase consent uses. One session per app run so the lockout sticks.</summary>
        public sealed class Gate : IParentalGate
        {
            private ParentalGateSession? _session;

            public void Ask(string purpose, Action<bool> done)
            {
                var c = KidSafety.Config;
                _session ??= new ParentalGateSession(c.ParentalGateMaxFailures, c.ParentalGateLockoutSeconds);
                Show(_session, purpose, done);
            }
        }

        private readonly StringBuilder _typed = new StringBuilder(4);
        private ParentalGateSession _session = null!;
        private Action<bool>? _done;
        private Text? _question;
        private Text? _answer;
        private Text? _note;

        private static void Show(ParentalGateSession session, string purpose, Action<bool> done)
        {
            var card = KidUi.Modal(null!, "ParentalGateCanvas", 520, 0.07f, 0.20f, 0.93f, 0.80f, out var root);
            var s = root.gameObject.AddComponent<ParentalGateScreen>();
            s._session = session;
            s._done = done;
            s.Build(card, purpose);
        }

        private void Build(RectTransform card, string purpose)
        {
            HookSkin.Header(card, KidUi.S("kids.pgate.title", "Ask a grown-up"), 520f, 96f, 32);
            var why = purpose.StartsWith("iap:", StringComparison.Ordinal)
                ? KidUi.S("kids.pgate.why_iap", "A grown-up needs to say yes before anything is bought with real money.")
                : KidUi.S("kids.pgate.why", "A grown-up needs to say yes before you go on.");
            KidUi.Label(card, "Why", why, 22, 0.06f, 0.78f, 0.94f, 0.88f);
            _question = KidUi.Label(card, "Question", "", 34, 0.06f, 0.68f, 0.94f, 0.77f, SurvivalVisuals.Reward);
            var box = SurvivalVisuals.Framed(card, "AnswerBox", SurvivalVisuals.RowFill, SurvivalVisuals.GoldLine, 2f);
            HookSkin.Box(box.rectTransform, 0.32f, 0.59f, 0.68f, 0.665f);
            _answer = SurvivalVisuals.Text(box.transform, "Answer", "", 34, TextAnchor.MiddleCenter, HookSkin.Cream);
            SurvivalVisuals.Stretch(_answer.rectTransform);
            for (var k = 0; k < 11; k++)
            {
                var col = k < 9 ? k % 3 : k == 9 ? 0 : 1;
                var row = k < 9 ? k / 3 : 3;
                var x0 = 0.20f + col * 0.205f;
                var y1 = 0.575f - row * 0.088f;
                if (k == 9)
                {
                    KidUi.Button(card, "Erase", "\u2190", HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 26, x0, y1 - 0.078f, x0 + 0.19f, y1, Erase);
                    continue;
                }

                var digit = k == 10 ? 0 : k + 1;
                KidUi.Button(card, "Key" + digit, digit.ToString(), HookArtKeys.ButtonBlue, SurvivalVisuals.PinBuild, 28, x0, y1 - 0.078f, x0 + 0.19f, y1, () => Type(digit));
            }

            _note = KidUi.Label(card, "Note", "", 20, 0.06f, 0.13f, 0.94f, 0.19f, new Color(1f, 0.72f, 0.62f, 1f));
            KidUi.Button(card, "Cancel", KidUi.S("kids.pgate.cancel", "Cancel"), HookArtKeys.ButtonRed, SurvivalVisuals.PanelSoft, 24, 0.08f, 0.03f, 0.46f, 0.115f, () => Finish(false));
            KidUi.Button(card, "Ok", KidUi.S("kids.pgate.ok", "OK"), HookArtKeys.ButtonGreen, SurvivalVisuals.MineFill, 24, 0.54f, 0.03f, 0.92f, 0.115f, Submit);
            Refresh();
        }

        private void Type(int d)
        {
            if (_typed.Length < 3)
            {
                _typed.Append((char)('0' + d));
            }

            Refresh();
        }

        private void Erase()
        {
            if (_typed.Length > 0)
            {
                _typed.Length--;
            }

            Refresh();
        }

        private void Refresh()
        {
            var c = _session.Challenge;
            if (_question != null)
            {
                _question.text = _session.IsLockedOut
                    ? KidUi.S("kids.pgate.locked", "Please try again later.")
                    : string.Format(KidUi.S("kids.pgate.question", "What is {0} \u00d7 {1}?"), c.A, c.B);
            }

            if (_answer != null)
            {
                _answer.text = _typed.ToString();
            }
        }

        private void Submit()
        {
            int? value = _typed.Length > 0 ? int.Parse(_typed.ToString()) : null;
            _typed.Clear();
            switch (_session.Submit(value))
            {
                case ParentalGateResult.Passed:
                    Finish(true);
                    return;
                case ParentalGateResult.LockedOut:
                    if (_note != null)
                    {
                        _note.text = KidUi.S("kids.pgate.locked", "Please try again later.");
                    }

                    break;
                default:
                    if (_note != null)
                    {
                        _note.text = KidUi.S("kids.pgate.wrong", "That's not it. Here is a new one.");
                    }

                    break;
            }

            Refresh();
        }

        private void Finish(bool ok)
        {
            var done = _done;
            _done = null;
            Destroy(gameObject);
            done?.Invoke(ok);
        }
    }
}
