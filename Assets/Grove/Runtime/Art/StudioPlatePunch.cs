using System;
using System.Collections.Generic;

namespace Grove.Domain.Art
{
    /// <summary>
    /// Punches studio backdrop plates (cream / navy / brown) to true alpha so
    /// piece and HUD icons sit in cells without opaque blobs.
    /// </summary>
    public static class StudioPlatePunch
    {
        public const int ColorThreshold = 44;
        public const float EdgeBarrier = 16f;
        public const int CornerPatch = 12;

        public static bool ShouldPunch(string stub, string category)
        {
            if (string.IsNullOrEmpty(stub))
            {
                return false;
            }

            if (string.Equals(category, "piece", StringComparison.Ordinal))
            {
                return true;
            }

            return stub switch
            {
                "ENV_FG_GardenCrate_Idle" => true,
                "HUD_EnergyPill" => true,
                "HUD_Wallet_Coin" => true,
                "UI_GoalPill" => true,
                "UI_Btn_Deliver" => true,
                "UI_Teach_Banner" => true,
                "UI_Badge_Starter" => true,
                "MAYA_Portrait_Happy" => true,
                "MAYA_Portrait_Neutral" => true,
                _ => false
            };
        }

        public static bool CornersTransparent(byte[] rgba, int width, int height)
        {
            if (!Valid(rgba, width, height))
            {
                return false;
            }

            return Alpha(rgba, 0, 0, width) < 16
                   && Alpha(rgba, width - 1, 0, width) < 16
                   && Alpha(rgba, 0, height - 1, width) < 16
                   && Alpha(rgba, width - 1, height - 1, width) < 16;
        }

        /// <summary>
        /// Tight content rect (Unity / PNG bottom-left if <paramref name="rgba"/> is bottom-up).
        /// Used to crop FullRect piece sprites so cream RGB outside the object cannot draw a plate.
        /// </summary>
        public static bool TryOpaqueRect(
            byte[] rgba,
            int width,
            int height,
            int alphaMin,
            int pad,
            out int x,
            out int y,
            out int w,
            out int h)
        {
            x = 0;
            y = 0;
            w = 0;
            h = 0;
            if (!Valid(rgba, width, height))
            {
                return false;
            }

            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;
            for (var py = 0; py < height; py++)
            {
                for (var px = 0; px < width; px++)
                {
                    if (rgba[(py * width + px) * 4 + 3] < alphaMin)
                    {
                        continue;
                    }

                    if (px < minX)
                    {
                        minX = px;
                    }

                    if (py < minY)
                    {
                        minY = py;
                    }

                    if (px > maxX)
                    {
                        maxX = px;
                    }

                    if (py > maxY)
                    {
                        maxY = py;
                    }
                }
            }

            if (maxX < 0)
            {
                return false;
            }

            x = Math.Max(0, minX - pad);
            y = Math.Max(0, minY - pad);
            var x1 = Math.Min(width - 1, maxX + pad);
            var y1 = Math.Min(height - 1, maxY + pad);
            w = x1 - x + 1;
            h = y1 - y + 1;
            return w >= 2 && h >= 2;
        }

        /// <summary>
        /// Zero RGB on fully transparent pixels so a FullRect / broken URP material cannot
        /// reconstruct the studio cream plate from leftover color.
        /// </summary>
        public static int ClearTransparentRgb(byte[] rgba, int width, int height)
        {
            if (!Valid(rgba, width, height))
            {
                return 0;
            }

            var n = 0;
            var count = width * height;
            for (var i = 0; i < count; i++)
            {
                var o = i * 4;
                if (rgba[o + 3] >= 8)
                {
                    continue;
                }

                if (rgba[o] == 0 && rgba[o + 1] == 0 && rgba[o + 2] == 0)
                {
                    continue;
                }

                rgba[o] = 0;
                rgba[o + 1] = 0;
                rgba[o + 2] = 0;
                n++;
            }

            return n;
        }

        /// <summary>In-place RGBA punch. Returns pixels set to alpha 0.</summary>
        public static int Punch(byte[] rgba, int width, int height)
        {
            if (!Valid(rgba, width, height))
            {
                return 0;
            }

            if (CornersTransparent(rgba, width, height))
            {
                ClearTransparentRgb(rgba, width, height);
                return 0;
            }

            var plates = CornerPlates(rgba, width, height);
            var creamPlate = IsCreamPlate(plates);
            var color = ColorFlood(rgba, width, height, ColorThreshold, plates);
            var edge = EdgeFlood(rgba, width, height, EdgeBarrier);
            var colorFrac = Fraction(color);
            var edgeFrac = Fraction(edge);
            var colorInner = InnerKeep(rgba, width, height, color);
            var edgeInner = InnerKeep(rgba, width, height, edge);
            var colorOk = colorInner >= 0.08f && colorFrac >= 0.18f && colorFrac <= 0.96f;
            var edgeOk = edgeInner >= 0.05f && edgeFrac >= 0.25f && edgeFrac <= 0.97f;

            byte[] chosen;
            if (colorOk && colorInner >= 0.90f && colorFrac >= 0.25f)
            {
                chosen = color;
            }
            else if (colorOk && edgeFrac >= 0.94f && colorFrac >= 0.75f)
            {
                chosen = color;
            }
            else if (edgeOk && edgeFrac > colorFrac + 0.05f && edgeInner >= 0.10f)
            {
                chosen = edge;
            }
            else if (colorOk)
            {
                chosen = color;
            }
            else if (edgeOk)
            {
                chosen = edge;
            }
            else
            {
                chosen = colorInner >= edgeInner ? color : edge;
            }

            var punched = Apply(rgba, width, height, chosen);
            if (creamPlate)
            {
                punched += PunchCreamHalo(rgba, width, height);
            }

            ClearTransparentRgb(rgba, width, height);
            return punched;
        }

        private static bool Valid(byte[] rgba, int width, int height) =>
            rgba != null && width >= 8 && height >= 8 && rgba.Length >= width * height * 4;

        private static byte Alpha(byte[] rgba, int x, int y, int width) =>
            rgba[(y * width + x) * 4 + 3];

        private static float Dist(byte[] rgba, int i, int r, int g, int b)
        {
            var dr = rgba[i] - r;
            var dg = rgba[i + 1] - g;
            var db = rgba[i + 2] - b;
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private static float DistPix(byte[] rgba, int i, int j)
        {
            var dr = rgba[i] - rgba[j];
            var dg = rgba[i + 1] - rgba[j + 1];
            var db = rgba[i + 2] - rgba[j + 2];
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private static List<int[]> CornerPlates(byte[] rgba, int width, int height)
        {
            var plates = new List<int[]>(4);
            var origins = new[]
            {
                new[] { 0, 0 },
                new[] { width - CornerPatch, 0 },
                new[] { 0, height - CornerPatch },
                new[] { width - CornerPatch, height - CornerPatch }
            };
            for (var p = 0; p < origins.Length; p++)
            {
                var rs = new List<int>(CornerPatch * CornerPatch);
                var gs = new List<int>(CornerPatch * CornerPatch);
                var bs = new List<int>(CornerPatch * CornerPatch);
                var ox = origins[p][0];
                var oy = origins[p][1];
                for (var y = oy; y < oy + CornerPatch; y++)
                {
                    for (var x = ox; x < ox + CornerPatch; x++)
                    {
                        var i = (y * width + x) * 4;
                        if (rgba[i + 3] < 8)
                        {
                            continue;
                        }

                        rs.Add(rgba[i]);
                        gs.Add(rgba[i + 1]);
                        bs.Add(rgba[i + 2]);
                    }
                }

                if (rs.Count == 0)
                {
                    continue;
                }

                plates.Add(new[] { Median(rs), Median(gs), Median(bs) });
            }

            return plates;
        }

        private static int Median(List<int> values)
        {
            values.Sort();
            return values[values.Count / 2];
        }

        private static bool MatchesPlate(byte[] rgba, int index, List<int[]> plates, float threshold)
        {
            if (rgba[index + 3] < 8)
            {
                return true;
            }

            var best = 9999f;
            for (var p = 0; p < plates.Count; p++)
            {
                var d = Dist(rgba, index, plates[p][0], plates[p][1], plates[p][2]);
                if (d < best)
                {
                    best = d;
                }
            }

            return best <= threshold;
        }

        private static bool IsCreamPlate(List<int[]> plates)
        {
            for (var p = 0; p < plates.Count; p++)
            {
                var l = (plates[p][0] + plates[p][1] + plates[p][2]) / 3;
                var sat = Math.Max(plates[p][0], Math.Max(plates[p][1], plates[p][2]))
                          - Math.Min(plates[p][0], Math.Min(plates[p][1], plates[p][2]));
                if (l >= 170 && sat <= 60)
                {
                    return true;
                }
            }

            return false;
        }

        private static byte[] ColorFlood(byte[] rgba, int width, int height, float threshold, List<int[]> plates)
        {
            var mark = new byte[width * height];
            if (plates == null || plates.Count == 0)
            {
                return mark;
            }

            var q = new Queue<int>();
            void Push(int i)
            {
                if (mark[i] != 0)
                {
                    return;
                }

                mark[i] = 1;
                q.Enqueue(i);
            }

            for (var x = 0; x < width; x++)
            {
                if (MatchesPlate(rgba, x * 4, plates, threshold))
                {
                    Push(x);
                }

                if (MatchesPlate(rgba, ((height - 1) * width + x) * 4, plates, threshold))
                {
                    Push((height - 1) * width + x);
                }
            }

            for (var y = 0; y < height; y++)
            {
                if (MatchesPlate(rgba, y * width * 4, plates, threshold))
                {
                    Push(y * width);
                }

                if (MatchesPlate(rgba, (y * width + width - 1) * 4, plates, threshold))
                {
                    Push(y * width + width - 1);
                }
            }

            Flood4(width, height, q, mark, i => MatchesPlate(rgba, i * 4, plates, threshold));
            return mark;
        }

        private static byte[] EdgeFlood(byte[] rgba, int width, int height, float barrier)
        {
            var n = width * height;
            var grad = new float[n];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    var best = 0f;
                    if (x > 0)
                    {
                        best = Math.Max(best, DistPix(rgba, i * 4, (i - 1) * 4));
                    }

                    if (y > 0)
                    {
                        best = Math.Max(best, DistPix(rgba, i * 4, (i - width) * 4));
                    }

                    grad[i] = best;
                }
            }

            var mark = new byte[n];
            var q = new Queue<int>();
            void Push(int i)
            {
                if (mark[i] != 0)
                {
                    return;
                }

                mark[i] = 1;
                q.Enqueue(i);
            }

            for (var x = 0; x < width; x++)
            {
                Push(x);
                Push((height - 1) * width + x);
            }

            for (var y = 0; y < height; y++)
            {
                Push(y * width);
                Push(y * width + width - 1);
            }

            Flood4(width, height, q, mark, i => rgba[i * 4 + 3] < 8 || grad[i] < barrier);
            return mark;
        }

        private static void Flood4(int width, int height, Queue<int> q, byte[] mark, Func<int, bool> canVisit)
        {
            while (q.Count > 0)
            {
                var i = q.Dequeue();
                var x = i % width;
                var y = i / width;
                Try(x + 1, y);
                Try(x - 1, y);
                Try(x, y + 1);
                Try(x, y - 1);
            }

            void Try(int nx, int ny)
            {
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    return;
                }

                var ni = ny * width + nx;
                if (mark[ni] != 0 || !canVisit(ni))
                {
                    return;
                }

                mark[ni] = 1;
                q.Enqueue(ni);
            }
        }

        private static float Fraction(byte[] mark)
        {
            var n = 0;
            for (var i = 0; i < mark.Length; i++)
            {
                if (mark[i] != 0)
                {
                    n++;
                }
            }

            return mark.Length == 0 ? 0f : n / (float)mark.Length;
        }

        private static float InnerKeep(byte[] rgba, int width, int height, byte[] mark)
        {
            var x0 = width * 3 / 10;
            var x1 = width * 7 / 10;
            var y0 = height * 3 / 10;
            var y1 = height * 7 / 10;
            var total = 0;
            var keep = 0;
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    total++;
                    var i = y * width + x;
                    if (mark[i] == 0 && rgba[i * 4 + 3] >= 8)
                    {
                        keep++;
                    }
                }
            }

            return total == 0 ? 0f : keep / (float)total;
        }

        private static int Apply(byte[] rgba, int width, int height, byte[] mark)
        {
            var punched = 0;
            var n = width * height;
            for (var i = 0; i < n; i++)
            {
                if (mark[i] == 0)
                {
                    continue;
                }

                if (rgba[i * 4 + 3] >= 8)
                {
                    punched++;
                }

                rgba[i * 4 + 3] = 0;
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    if (mark[i] != 0 || rgba[i * 4 + 3] < 8)
                    {
                        continue;
                    }

                    if (NeighborMarked(mark, width, height, x, y))
                    {
                        rgba[i * 4 + 3] = (byte)(rgba[i * 4 + 3] * 45 / 100);
                    }
                }
            }

            return punched;
        }

        private static bool NeighborMarked(byte[] mark, int width, int height, int x, int y)
        {
            return (x + 1 < width && mark[y * width + x + 1] != 0)
                   || (x > 0 && mark[y * width + x - 1] != 0)
                   || (y + 1 < height && mark[(y + 1) * width + x] != 0)
                   || (y > 0 && mark[(y - 1) * width + x] != 0);
        }

        private static int PunchCreamHalo(byte[] rgba, int width, int height)
        {
            var mark = new byte[width * height];
            var q = new Queue<int>();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    if (rgba[i * 4 + 3] >= 8)
                    {
                        continue;
                    }

                    TryCream(x + 1, y);
                    TryCream(x - 1, y);
                    TryCream(x, y + 1);
                    TryCream(x, y - 1);
                }
            }

            Flood4(width, height, q, mark, IsCream);
            if (InnerKeep(rgba, width, height, mark) < 0.08f)
            {
                return 0;
            }

            return Apply(rgba, width, height, mark);

            bool IsCream(int i)
            {
                if (rgba[i * 4 + 3] < 8)
                {
                    return true;
                }

                var r = rgba[i * 4];
                var g = rgba[i * 4 + 1];
                var b = rgba[i * 4 + 2];
                var l = (r + g + b) / 3;
                var sat = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
                return l >= 165 && sat <= 55;
            }

            void TryCream(int nx, int ny)
            {
                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    return;
                }

                var ni = ny * width + nx;
                if (mark[ni] != 0 || !IsCream(ni))
                {
                    return;
                }

                mark[ni] = 1;
                q.Enqueue(ni);
            }
        }
    }
}
