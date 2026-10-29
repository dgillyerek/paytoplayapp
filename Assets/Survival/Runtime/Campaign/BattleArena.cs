#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// The configured battle as one open arena (Derek 2026-10-28: "the ground is very basic, lets make it more
    /// interesting. the characters do not need lanes. Lets have the camera slowly fly around at like 30 degrees").
    /// Fighters start in a loose line per side and walk to any foe; <see cref="Separate"/> keeps every pair of bodies
    /// apart (not just a fighter and its target), on top of the <see cref="BattleSpacing"/> stop distance and lunge
    /// clamp. The ground (height, worn dirt, flagstones) and the props ring are pure functions of position, so the
    /// view builds them and the tests check that no prop stands in the fighting area. Presentation layout only.
    /// </summary>
    public static class BattleArena
    {
        /// <summary>Space between two neighbours' bodies in a starting line.</summary>
        public const float StartSideGapMetres = 1.1f;

        /// <summary>Narrowest start slot, so small fighters do not bunch up.</summary>
        public const float MinStartSlotMetres = 1.8f;

        /// <summary>How far the wings of a line sit back (metres back per metre out), a shallow arc facing the foe.</summary>
        public const float StartArc = 0.12f;

        /// <summary>Clear ground beyond the farthest start before any prop.</summary>
        public const float ArenaMarginMetres = 3.2f;

        public const float MinArenaRadiusMetres = 7f;

        /// <summary>Props start this far beyond the arena edge (plus their own footprint).</summary>
        public const float PropClearanceMetres = 1.2f;

        /// <summary>Outer edge of the decorated ring.</summary>
        public const float PropRingMetres = 15f;

        /// <summary>Height of the rolling ground outside the arena; the arena itself stays flat.</summary>
        public const float HillMetres = 0.9f;

        /// <summary>Camera orbit: pitch, one revolution every <see cref="OrbitSeconds"/>.</summary>
        public const float OrbitPitchDegrees = 30f;

        public const float OrbitSeconds = 50f;

        /// <summary>
        /// The orbit sits this much of the all-yaw fit distance (Derek 2026-10-28: "zoom in a little on the fly around"):
        /// 17% closer, so the fight fills the frame and an edge fighter can brush the frame edge on some sides of the turn.
        /// </summary>
        public const float OrbitZoomFactor = 0.83f;

        public static float OrbitDegreesPerSecond => 360f / OrbitSeconds;

        /// <summary>Start points (x, z) for one side's fighters, centred on x = 0, at z = ±halfGap (heroes negative).</summary>
        public static (float X, float Z)[] Starts(IReadOnlyList<float> halfWidths, float halfGap, bool heroes)
        {
            var n = halfWidths.Count;
            var slots = new float[n];
            var total = 0f;
            for (var i = 0; i < n; i++)
            {
                slots[i] = Math.Max(MinStartSlotMetres, (2f * halfWidths[i]) + StartSideGapMetres);
                total += slots[i];
            }

            var result = new (float, float)[n];
            var x = -total * 0.5f;
            var sign = heroes ? -1f : 1f;
            for (var i = 0; i < n; i++)
            {
                var cx = x + (slots[i] * 0.5f);
                result[i] = (cx, sign * (halfGap + (StartArc * Math.Abs(cx))));
                x += slots[i];
            }

            return result;
        }

        /// <summary>Radius of the flat fighting area for these starts.</summary>
        public static float ArenaRadius(IEnumerable<(float X, float Z)> starts)
        {
            var r = 0f;
            foreach (var (x, z) in starts)
            {
                r = Math.Max(r, (float)Math.Sqrt((x * x) + (z * z)));
            }

            return Math.Max(MinArenaRadiusMetres, r + ArenaMarginMetres);
        }

        /// <summary>
        /// Pushes every overlapping pair of standing bodies apart (half each; a pinned body, e.g. one already down,
        /// does not move and the other takes the whole push). Returns the largest overlap found before pushing.
        /// </summary>
        public static float Separate(IList<(float X, float Z)> positions, IReadOnlyList<float> halfDepths, IReadOnlyList<bool>? pinned = null, int passes = 3)
        {
            var worst = 0f;
            for (var pass = 0; pass < passes; pass++)
            {
                var any = false;
                for (var i = 0; i < positions.Count; i++)
                {
                    for (var j = i + 1; j < positions.Count; j++)
                    {
                        var pi = pinned != null && pinned[i];
                        var pj = pinned != null && pinned[j];
                        if (pi && pj)
                        {
                            continue;
                        }

                        var dx = positions[j].X - positions[i].X;
                        var dz = positions[j].Z - positions[i].Z;
                        var d = (float)Math.Sqrt((dx * dx) + (dz * dz));
                        var overlap = BattleSpacing.Overlap(d, halfDepths[i], halfDepths[j]);
                        if (overlap <= 1e-4f)
                        {
                            continue;
                        }

                        if (pass == 0)
                        {
                            worst = Math.Max(worst, overlap);
                        }

                        any = true;
                        float ux, uz;
                        if (d > 1e-5f)
                        {
                            ux = dx / d;
                            uz = dz / d;
                        }
                        else
                        {
                            // Same spot: split along x, by index, so the result is stable.
                            ux = 1f;
                            uz = 0f;
                        }

                        var si = pi ? 0f : (pj ? 1f : 0.5f);
                        var sj = 1f - si;
                        positions[i] = (positions[i].X - (ux * overlap * si), positions[i].Z - (uz * overlap * si));
                        positions[j] = (positions[j].X + (ux * overlap * sj), positions[j].Z + (uz * overlap * sj));
                    }
                }

                if (!any)
                {
                    break;
                }
            }

            return worst;
        }

        /// <summary>Ground height: flat inside the arena (plus a metre), rolling gently into low hills beyond.</summary>
        public static float Height(float x, float z, float arenaRadius)
        {
            var r = (float)Math.Sqrt((x * x) + (z * z));
            var rise = Smooth(arenaRadius + 1f, arenaRadius + 9f, r);
            if (rise <= 0f)
            {
                return 0f;
            }

            var n = (0.6f * Noise(x * 0.11f, z * 0.11f, 11)) + (0.4f * Noise(x * 0.27f, z * 0.27f, 23));
            return rise * HillMetres * (0.35f + n);
        }

        /// <summary>Worn dirt weight 0..1: trampled in the middle of the arena, ragged at its edge, odd patches beyond.</summary>
        public static float Dirt(float x, float z, float arenaRadius)
        {
            var r = (float)Math.Sqrt((x * x) + (z * z));
            var n = Noise(x * 0.35f, z * 0.35f, 5);
            var worn = 1f - Smooth(arenaRadius * 0.45f, arenaRadius * 0.9f, r + ((n - 0.5f) * 2.4f));
            var patch = Smooth(0.72f, 0.82f, Noise(x * 0.09f, z * 0.09f, 41)) * 0.8f;
            return Clamp01(Math.Max(worn * 0.85f, patch));
        }

        /// <summary>Broken flagstone ring weight 0..1 at the arena's edge (an old courtyard), missing stones by noise.</summary>
        public static float Stone(float x, float z, float arenaRadius)
        {
            var r = (float)Math.Sqrt((x * x) + (z * z));
            var band = 1f - Math.Abs(r - arenaRadius) / 1.1f;
            var keep = Smooth(0.35f, 0.5f, Noise(x * 0.5f, z * 0.5f, 77));
            return Clamp01(band * 3f) * keep;
        }

        public enum PropKind
        {
            Tree,
            Pine,
            Bush,
            Rock,
            Pillar,
            BrokenWall,
            Banner,
        }

        public readonly struct Prop
        {
            public readonly PropKind Kind;
            public readonly float X, Z, Scale, YawDegrees;

            public Prop(PropKind kind, float x, float z, float scale, float yawDegrees)
            {
                Kind = kind;
                X = x;
                Z = z;
                Scale = scale;
                YawDegrees = yawDegrees;
            }

            /// <summary>Ground footprint radius (metres), used for spacing and the arena clearance.</summary>
            public float Radius => FootprintOf(Kind) * Scale;

            public float Distance => (float)Math.Sqrt((X * X) + (Z * Z));
        }

        /// <summary>How far each detailed prop reaches from its centre at scale 1 (leaf clusters, plinths, fallen drums, rubble, banner cloth).</summary>
        public static float FootprintOf(PropKind kind) => kind switch
        {
            PropKind.Tree => 1.8f,
            PropKind.Pine => 1.1f,
            PropKind.Bush => 0.95f,
            PropKind.Rock => 1.15f,
            PropKind.Pillar => 1.25f,
            PropKind.BrokenWall => 1.6f,
            _ => 0.9f,
        };

        /// <summary>
        /// The decorated ring around the arena, the same every time for a seed: ruins (pillars, a broken wall, banners)
        /// close in, rocks and bushes between, trees behind. Nothing reaches inside the arena plus
        /// <see cref="PropClearanceMetres"/>, so props never hide a fighter.
        /// </summary>
        public static List<Prop> Props(float arenaRadius, int seed = 7)
        {
            var props = new List<Prop>();
            var rng = new Lcg(seed);
            Ring(props, rng, arenaRadius, PropKind.Pillar, 8, 0.4f, 1.6f, 0.9f, 1.25f);
            Ring(props, rng, arenaRadius, PropKind.Banner, 4, 0.2f, 0.9f, 1f, 1f);
            Ring(props, rng, arenaRadius, PropKind.BrokenWall, 3, 1.2f, 3f, 0.8f, 1.1f);
            Ring(props, rng, arenaRadius, PropKind.Rock, 18, 0.3f, 6f, 0.5f, 1.4f);
            Ring(props, rng, arenaRadius, PropKind.Bush, 22, 0.8f, 8f, 0.6f, 1.2f);
            Ring(props, rng, arenaRadius, PropKind.Tree, 26, 4f, PropRingMetres, 0.8f, 1.3f);
            Ring(props, rng, arenaRadius, PropKind.Pine, 22, 5f, PropRingMetres, 0.85f, 1.35f);
            return props;
        }

        private static void Ring(List<Prop> props, Lcg rng, float arenaRadius, PropKind kind, int count, float near, float far, float minScale, float maxScale)
        {
            var placed = 0;
            for (var attempt = 0; attempt < count * 30 && placed < count; attempt++)
            {
                var scale = minScale + ((maxScale - minScale) * rng.Next());
                var radius = FootprintOf(kind) * scale;
                var inner = arenaRadius + PropClearanceMetres + radius;
                var dist = inner + Math.Max(0f, near - radius) + ((far - near) * rng.Next());
                var angle = rng.Next() * 2f * (float)Math.PI;
                var x = dist * (float)Math.Cos(angle);
                var z = dist * (float)Math.Sin(angle);
                var ok = true;
                foreach (var p in props)
                {
                    var dx = p.X - x;
                    var dz = p.Z - z;
                    var min = p.Radius + radius + 0.2f;
                    if ((dx * dx) + (dz * dz) < min * min)
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                {
                    continue;
                }

                props.Add(new Prop(kind, x, z, scale, rng.Next() * 360f));
                placed++;
            }
        }

        private sealed class Lcg
        {
            private uint _s;

            public Lcg(int seed) => _s = (uint)seed * 2654435761u + 1u;

            public float Next()
            {
                _s = (_s * 1664525u) + 1013904223u;
                return (_s >> 8) / 16777216f;
            }
        }

        /// <summary>Smooth value noise in 0..1.</summary>
        public static float Noise(float x, float z, int seed)
        {
            var x0 = (int)Math.Floor(x);
            var z0 = (int)Math.Floor(z);
            var fx = x - x0;
            var fz = z - z0;
            fx = fx * fx * (3f - (2f * fx));
            fz = fz * fz * (3f - (2f * fz));
            var a = Hash(x0, z0, seed);
            var b = Hash(x0 + 1, z0, seed);
            var c = Hash(x0, z0 + 1, seed);
            var d = Hash(x0 + 1, z0 + 1, seed);
            return Lerp(Lerp(a, b, fx), Lerp(c, d, fx), fz);
        }

        public static float Hash(int x, int z, int seed)
        {
            unchecked
            {
                var h = (uint)((x * 374761393) + (z * 668265263) + (seed * 982451653));
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        private static float Smooth(float e0, float e1, float v)
        {
            var t = Clamp01((v - e0) / (e1 - e0));
            return t * t * (3f - (2f * t));
        }
    }
}
