using System;

namespace Grove.Domain.Juice
{
    /// <summary>
    /// Style bible §5 hero-loop motion: crate tap, merge pop, deliver.
    /// Unity samples these curves; headless tests lock premium-casual feel
    /// (not linear lerp / instant swap). Design owns visual PASS.
    /// Product scope: existing Front Garden Orders 1–6 and WF/Herb/Tools spit only —
    /// mock props (gnomes, lanterns, etc.) are not new item chains.
    /// </summary>
    public static class HeroJuice
    {
        public const float CrateTapMinSeconds = 0.080f;
        public const float CrateTapMaxSeconds = 0.120f;
        public const float MergePopMinSeconds = 0.120f;
        public const float MergePopMaxSeconds = 0.180f;
        public const float DeliverMinSeconds = 0.200f;
        public const float DeliverMaxSeconds = 0.350f;
        public const float TierUpMaxSeconds = 0.200f;

        /// <summary>Soft squash + spit launch + pip tick (mid of 80–120ms).</summary>
        public const float CrateTapSeconds = 0.100f;

        /// <summary>Spit flight shares the crate-tap window (overlaps squash).</summary>
        public const float CrateSpitSeconds = 0.110f;

        /// <summary>Juicy pop + sparkle (mid of 120–180ms).</summary>
        public const float MergePopSeconds = 0.155f;

        /// <summary>Card settle + reward fly (mid of 200–350ms).</summary>
        public const float DeliverSeconds = 0.280f;

        public const float SparkleSeconds = 0.155f;
        public const float ChargePipTickSeconds = 0.090f;
        public const float MayaFlashSeconds = 0.220f;
        public const int SparkleQuadCount = 6;

        /// <summary>Soft gold sparkle tint (Travel Town / Merge Mansion class, not white flash).</summary>
        public const float SparkleR = 1.00f;
        public const float SparkleG = 0.86f;
        public const float SparkleB = 0.42f;
        public const float SparkleA = 0.92f;

        public static float Clamp01(float t) => t < 0f ? 0f : (t > 1f ? 1f : t);

        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        /// <summary>Ease-out cubic — starts snappy, settles. Not linear.</summary>
        public static float EaseOutCubic(float t)
        {
            t = 1f - Clamp01(t);
            return 1f - t * t * t;
        }

        /// <summary>Ease-in-out cubic for card settle body.</summary>
        public static float EaseInOutCubic(float t)
        {
            t = Clamp01(t);
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - (float)Math.Pow(-2f * t + 2f, 3d) / 2f;
        }

        /// <summary>Back overshoot that returns to 1. Default s is juicier than Unity's 1.70158.</summary>
        public static float EaseOutBack(float t, float s = 2.4f)
        {
            t = Clamp01(t) - 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        /// <summary>
        /// Crate squash: wider/shorter at mid, identity at rest.
        /// Soft — not a cartoon flatten.
        /// </summary>
        public static void SquashScale(float t, out float sx, out float sy)
        {
            t = Clamp01(t);
            var dip = (float)Math.Sin(Math.PI * t);
            sx = 1f + 0.16f * dip;
            sy = 1f - 0.14f * dip;
        }

        /// <summary>Charge pip punch on the crate badge.</summary>
        public static float PipTickScale(float t)
        {
            var dip = (float)Math.Sin(Math.PI * Clamp01(t));
            return 1f + 0.26f * dip;
        }

        /// <summary>
        /// Merge result scale: readable start (~0.72), overshoot past 1, settle at 1.
        /// </summary>
        public static float MergePopScale(float t)
        {
            t = Clamp01(t);
            return 1f - 0.28f * (1f - t) * (1f - t) + 0.26f * (float)Math.Sin(Math.PI * t);
        }

        /// <summary>Delivered order card: slight compress, overshoot, settle.</summary>
        public static float CardSettleScale(float t)
        {
            t = Clamp01(t);
            return 1f - 0.07f * (1f - t) * (1f - t) + 0.09f * (float)Math.Sin(Math.PI * t);
        }

        /// <summary>Upward bounce in the same unit space as <paramref name="amplitude"/>.</summary>
        public static float CardSettleOffsetY(float t, float amplitude)
        {
            t = Clamp01(t);
            return amplitude * (float)Math.Sin(Math.PI * t) * (1f - 0.35f * t);
        }

        /// <summary>Spit / reward flyer scale: small launch, readable land.</summary>
        public static float FlyerScale(float t)
        {
            t = Clamp01(t);
            return Lerp(0.62f, 1f, EaseOutBack(t, 2.1f));
        }

        /// <summary>Sparkle quad: expand then fade (alpha 1→0, scale 0.35→1.15).</summary>
        public static void SparkleQuad(float t, out float scale, out float alpha)
        {
            t = Clamp01(t);
            scale = Lerp(0.35f, 1.15f, EaseOutCubic(t));
            alpha = (1f - t) * (1f - t) * SparkleA;
        }

        /// <summary>
        /// Quadratic arc. Control point sits above the midpoint by <paramref name="loft"/>.
        /// Progress uses ease-out so the land is not a default lerp.
        /// </summary>
        public static void ArcPoint(
            float t,
            float x0,
            float y0,
            float x1,
            float y1,
            float loft,
            out float x,
            out float y)
        {
            var u = EaseOutCubic(t);
            var inv = 1f - u;
            var cx = (x0 + x1) * 0.5f;
            var cy = (float)Math.Max(y0, y1) + loft;
            x = inv * inv * x0 + 2f * inv * u * cx + u * u * x1;
            y = inv * inv * y0 + 2f * inv * u * cy + u * u * y1;
        }

        /// <summary>Reward fly progress (Maya / wallets). Ease-out, not linear.</summary>
        public static float DeliverFlyT(float t) => EaseOutCubic(t);

        public static bool InCrateTapWindow(float seconds) =>
            seconds >= CrateTapMinSeconds && seconds <= CrateTapMaxSeconds;

        public static bool InMergePopWindow(float seconds) =>
            seconds >= MergePopMinSeconds && seconds <= MergePopMaxSeconds;

        public static bool InDeliverWindow(float seconds) =>
            seconds >= DeliverMinSeconds && seconds <= DeliverMaxSeconds;
    }
}
