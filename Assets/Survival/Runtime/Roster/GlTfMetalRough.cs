using System;

namespace Survival.Domain.Roster
{
    /// <summary>
    /// Meshy / glTF metallic-roughness maps keep roughness in G and metallic in B (R is unused
    /// or occlusion, usually white). URP Lit reads R as metallic and A as smoothness, so a raw
    /// glTF map renders the whole body as a dark chrome mirror. This converts one texel into the
    /// URP metallic-smoothness layout. It only rearranges channels; it does not paint new values.
    /// </summary>
    public static class GlTfMetalRough
    {
        /// <summary>Metallic multiplier to use with a packed map (the map carries the value).</summary>
        public const float PackedMetallicScale = 1f;

        /// <summary>Smoothness multiplier to use with a packed map (the map carries the value).</summary>
        public const float PackedSmoothnessScale = 1f;

        /// <summary>Metallic when a named map cannot be packed: plain dielectric.</summary>
        public const float FallbackMetallic = 0f;

        /// <summary>Smoothness when a named map cannot be packed: modest sheen.</summary>
        public const float FallbackSmoothness = 0.3f;

        /// <summary>glTF texel (linear 0..1) to URP metallic (R) and smoothness (A).</summary>
        public static (float Metallic, float Smoothness) ToUrp(float r, float g, float b)
        {
            var metallic = Clamp01(b);
            var smoothness = 1f - Clamp01(g);
            return (metallic, smoothness);
        }

        /// <summary>
        /// Packs interleaved glTF RGBA texels (linear 0..1) into URP RGBA texels:
        /// RGB = metallic, A = smoothness.
        /// </summary>
        public static float[] PackRgba(float[] gltfRgba)
        {
            if (gltfRgba == null)
            {
                throw new ArgumentNullException(nameof(gltfRgba));
            }

            if (gltfRgba.Length % 4 != 0)
            {
                throw new ArgumentException("RGBA length must be a multiple of 4.", nameof(gltfRgba));
            }

            var packed = new float[gltfRgba.Length];
            for (var i = 0; i < gltfRgba.Length; i += 4)
            {
                var (metallic, smoothness) = ToUrp(gltfRgba[i], gltfRgba[i + 1], gltfRgba[i + 2]);
                packed[i] = metallic;
                packed[i + 1] = metallic;
                packed[i + 2] = metallic;
                packed[i + 3] = smoothness;
            }

            return packed;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
