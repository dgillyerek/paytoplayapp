#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Ids;

namespace Survival.Domain.World
{
    /// <summary>An axis-aligned box in map pixels (origin bottom-left, y up).</summary>
    public readonly struct MapRect
    {
        public MapRect(float x0, float y0, float x1, float y1)
        {
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
        }

        public float X0 { get; }
        public float Y0 { get; }
        public float X1 { get; }
        public float Y1 { get; }

        public bool Overlaps(MapRect o, float gap = 0f) =>
            X0 < o.X1 + gap && o.X0 < X1 + gap && Y0 < o.Y1 + gap && o.Y0 < Y1 + gap;
    }

    /// <summary>
    /// Where a map place's building art and its name plate sit. Shared by the world view (which draws them) and
    /// the layout test (which proves no two places overlap), so both always use the same sizes.
    /// </summary>
    public static class WorldSpacing
    {
        /// <summary>Art pivot: the entity position sits this far up from the bottom of its art.</summary>
        public const float ArtPivotY = 0.28f;

        /// <summary>Painted art rarely fills its box edge to edge; this share of the width counts as the building.</summary>
        public const float FootprintWidth = 0.8f;

        public const float PlateWidth = 290f;
        public const float PlateHeight = 52f;
        public const float PlateHeightWithSub = 78f;

        /// <summary>The plate's top edge sits this share of the art size below the entity position.</summary>
        public const float PlateDrop = 0.30f;

        /// <summary>Plates counter-scale with zoom (to stay legible) between these bounds.</summary>
        public const float PlateMinScale = 0.75f;
        public const float PlateMaxScale = 1.2f;

        /// <summary>Minimum clear gap (map pixels) between any two buildings / plates.</summary>
        public const float MinGap = 8f;

        public static float PlateScale(float zoom) => Clamp(0.8f / zoom, PlateMinScale, PlateMaxScale);

        public static float SizeFor(WorldEntity e) => e.Kind switch
        {
            WorldEntityKind.PlayerVillage => 330f,
            WorldEntityKind.RivalVillage or WorldEntityKind.AllyVillage => 270f,
            WorldEntityKind.EnemyCamp => e.Type == "dragon" ? 330f : e.Level >= 10 ? 270f : 240f,
            WorldEntityKind.ResourceNode => 220f,
            _ => e.Type == SurvIds.WorldNodeTypeFight ? 320f : 260f
        };

        /// <summary>Places with a second plate line (level, power, amount). Landmarks show only their name.</summary>
        public static bool HasSubLine(WorldEntity e) => e.Kind != WorldEntityKind.Landmark;

        public static MapRect Footprint(WorldEntity e)
        {
            var s = SizeFor(e);
            var hw = s * FootprintWidth * 0.5f;
            return new MapRect(e.X - hw, e.Y - s * ArtPivotY, e.X + hw, e.Y + s * (1f - ArtPivotY));
        }

        public static MapRect Plate(WorldEntity e, float scale)
        {
            var top = e.Y - SizeFor(e) * PlateDrop;
            var w = PlateWidth * scale;
            var h = (HasSubLine(e) ? PlateHeightWithSub : PlateHeight) * scale;
            return new MapRect(e.X - w * 0.5f, top - h, e.X + w * 0.5f, top);
        }

        /// <summary>
        /// Every pair of places whose buildings or plates touch (at the largest plate scale), as "a/b" strings.
        /// A place's own plate may sit over its own building's base. Empty when the layout is clean.
        /// </summary>
        public static List<string> Overlaps(IReadOnlyList<WorldEntity> entities, float gap = MinGap)
        {
            var bad = new List<string>();
            for (var i = 0; i < entities.Count; i++)
            {
                var a = entities[i];
                var ab = Footprint(a);
                var al = Plate(a, PlateMaxScale);
                for (var j = i + 1; j < entities.Count; j++)
                {
                    var b = entities[j];
                    var bb = Footprint(b);
                    var bl = Plate(b, PlateMaxScale);
                    if (ab.Overlaps(bb, gap)) bad.Add(a.Id + " building / " + b.Id + " building");
                    if (ab.Overlaps(bl, gap)) bad.Add(a.Id + " building / " + b.Id + " label");
                    if (al.Overlaps(bb, gap)) bad.Add(a.Id + " label / " + b.Id + " building");
                    if (al.Overlaps(bl, gap)) bad.Add(a.Id + " label / " + b.Id + " label");
                }
            }

            return bad;
        }

        /// <summary>
        /// Nudges <paramref name="movable"/> places (online castles) apart until no building or plate overlaps another or
        /// any <paramref name="fixedPlaces"/> place, using the same sizes and gap as <see cref="Overlaps"/>. Each push is
        /// along the shorter way out, so castles stay as close to their real tile as the rule allows.
        /// </summary>
        public static List<WorldEntity> Separate(IReadOnlyList<WorldEntity> movable, IReadOnlyList<WorldEntity> fixedPlaces, float gap = MinGap, int maxRounds = 200)
        {
            var list = new List<WorldEntity>(movable);
            for (var round = 0; round < maxRounds; round++)
            {
                var moved = false;
                for (var i = 0; i < list.Count; i++)
                {
                    for (var j = 0; j < list.Count + fixedPlaces.Count; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }

                        var other = j < list.Count ? list[j] : fixedPlaces[j - list.Count];
                        var (dx, dy) = Push(list[i], other, gap);
                        if (dx == 0f && dy == 0f)
                        {
                            continue;
                        }

                        if (j < list.Count)
                        {
                            // two movable castles share the push
                            list[i] = list[i] with { X = list[i].X + dx * 0.5f, Y = list[i].Y + dy * 0.5f };
                            list[j] = list[j] with { X = list[j].X - dx * 0.5f, Y = list[j].Y - dy * 0.5f };
                        }
                        else
                        {
                            list[i] = list[i] with { X = list[i].X + dx, Y = list[i].Y + dy };
                        }

                        moved = true;
                    }
                }

                if (!moved)
                {
                    break;
                }
            }

            return list;
        }

        /// <summary>Smallest move of <paramref name="a"/> that clears every box pair with <paramref name="b"/> (0,0 when clear).</summary>
        private static (float Dx, float Dy) Push(WorldEntity a, WorldEntity b, float gap)
        {
            var aBoxes = new[] { Footprint(a), Plate(a, PlateMaxScale) };
            var bBoxes = new[] { Footprint(b), Plate(b, PlateMaxScale) };
            float bestX = 0f, bestY = 0f;
            foreach (var p in aBoxes)
            {
                foreach (var q in bBoxes)
                {
                    if (!p.Overlaps(q, gap))
                    {
                        continue;
                    }

                    // +1 px so the boxes end strictly apart
                    var right = q.X1 + gap - p.X0 + 1f;
                    var left = p.X1 + gap - q.X0 + 1f;
                    var up = q.Y1 + gap - p.Y0 + 1f;
                    var down = p.Y1 + gap - q.Y0 + 1f;
                    var preferRight = a.X > b.X || (a.X == b.X && string.CompareOrdinal(a.Id, b.Id) > 0);
                    var preferUp = a.Y > b.Y || (a.Y == b.Y && string.CompareOrdinal(a.Id, b.Id) > 0);
                    var mx = preferRight ? right : -left;
                    var my = preferUp ? up : -down;
                    if (Math.Abs(mx) <= Math.Abs(my))
                    {
                        if (Math.Abs(mx) > Math.Abs(bestX)) bestX = mx;
                    }
                    else if (Math.Abs(my) > Math.Abs(bestY))
                    {
                        bestY = my;
                    }
                }
            }

            return (bestX, bestY);
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
