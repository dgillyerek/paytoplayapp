using System;
using System.Collections.Generic;

namespace Grove.Domain.Layout
{
    /// <summary>
    /// Headless text fit for Play HUD. Conservative Bold advance so Unity UI Text
    /// cannot wrap mid-word (Wildflo/wer) or clip Maya copy at 1080×1920.
    /// </summary>
    public static class PlayCopy
    {
        /// <summary>Wide glyphs (W, m) in Arial/Liberation Bold ≈ 0.62–0.64em.</summary>
        public const float BoldAdvanceEm = 0.64f;

        public const float LineHeightEm = 1.15f;

        public static float LineWidthPx(string line, int typePx) =>
            (line ?? string.Empty).Length * typePx * BoldAdvanceEm;

        public static float LinesHeightPx(int lines, int typePx) =>
            Math.Max(1, lines) * typePx * LineHeightEm;

        public static int MaxLines(float heightPx, int typePx)
        {
            var line = typePx * LineHeightEm;
            return line <= 0.01f ? 1 : Math.Max(1, (int)(heightPx / line));
        }

        public static List<string> WrapWords(string text, float widthPx, int typePx)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return lines;
            }

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var current = "";
            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var trial = current.Length == 0 ? word : current + " " + word;
                if (current.Length == 0 || LineWidthPx(trial, typePx) <= widthPx)
                {
                    current = trial;
                    continue;
                }

                lines.Add(current);
                current = word;
            }

            if (current.Length > 0)
            {
                lines.Add(current);
            }

            return lines;
        }

        public static bool TokenFits(string token, int typePx, float widthPx) =>
            LineWidthPx(token ?? "", typePx) <= widthPx + 0.01f;

        /// <summary>DES-006: STARTER / item names stay one line; ellipsis instead of mid-word wrap.</summary>
        public static string Ellipsize(string text, float widthPx, int typePx)
        {
            var value = text ?? "";
            if (TokenFits(value, typePx, widthPx))
            {
                return value;
            }

            const string ellipsis = "…";
            var budget = widthPx - LineWidthPx(ellipsis, typePx);
            if (budget <= 0f || value.Length == 0)
            {
                return ellipsis;
            }

            var n = value.Length;
            while (n > 0 && LineWidthPx(value.Substring(0, n), typePx) > budget)
            {
                n--;
            }

            return n <= 0 ? ellipsis : value.Substring(0, n) + ellipsis;
        }

        public static bool FitsWrapped(string text, float widthPx, float heightPx, int typePx)
        {
            var lines = WrapWords(text, widthPx, typePx);
            if (lines.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                if (!TokenFits(lines[i], typePx, widthPx))
                {
                    return false;
                }
            }

            return LinesHeightPx(lines.Count, typePx) <= heightPx + 0.01f;
        }
    }
}
