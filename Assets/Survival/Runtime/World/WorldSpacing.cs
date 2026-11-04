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

        /// <summary>Online map tiles (resource nodes, camps) are small unlabeled markers: empty name, no plate.</summary>
        public static bool HasPlate(WorldEntity e) => e.Name.Length > 0;

        public static float SizeFor(WorldEntity e) => !HasPlate(e)
            ? (e.Kind == WorldEntityKind.EnemyCamp ? 150f : 130f)
            : e.Kind switch
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
                var al = HasPlate(a) ? Plate(a, PlateMaxScale) : (MapRect?)null;
                for (var j = i + 1; j < entities.Count; j++)
                {
                    var b = entities[j];
                    var bb = Footprint(b);
                    var bl = HasPlate(b) ? Plate(b, PlateMaxScale) : (MapRect?)null;
                    if (ab.Overlaps(bb, gap)) bad.Add(a.Id + " building / " + b.Id + " building");
                    if (bl is { } blv && ab.Overlaps(blv, gap)) bad.Add(a.Id + " building / " + b.Id + " label");
                    if (al is { } alv && alv.Overlaps(bb, gap)) bad.Add(a.Id + " label / " + b.Id + " building");
                    if (al is { } alv2 && bl is { } blv2 && alv2.Overlaps(blv2, gap)) bad.Add(a.Id + " label / " + b.Id + " label");
                }
            }

            return bad;
        }

        /// <summary>
        /// Nudges <paramref name="movable"/> places (online castles) apart until no building or plate overlaps another or
        /// any <paramref name="fixedPlaces"/> place, using the same sizes and gap as <see cref="Overlaps"/>. Each push is
        /// along the shorter way out, so castles stay as close to their real tile as the rule allows.
        /// </summary>
        public static List<WorldEntity> Separate(
            IReadOnlyList<WorldEntity> movable,
            IReadOnlyList<WorldEntity> fixedPlaces,
            float gap = MinGap,
            int maxRounds = 200,
            IReadOnlyList<MapRect>? obstacles = null,
            MapRect? bounds = null)
        {
            obstacles ??= Array.Empty<MapRect>();
            var list = new List<WorldEntity>(movable);
            for (var round = 0; round < maxRounds; round++)
            {
                var moved = false;
                for (var i = 0; i < list.Count; i++)
                {
                    // scenery first: step out of painted trees / rocks / props the shortest way, staying inside bounds
                    foreach (var o in obstacles)
                    {
                        var (sx, sy) = PushOff(list[i], o, gap, bounds);
                        if (sx != 0f || sy != 0f)
                        {
                            list[i] = list[i] with { X = list[i].X + sx, Y = list[i].Y + sy };
                            moved = true;
                        }
                    }

                    if (bounds is { } bb)
                    {
                        var (cx, cy) = Contain(list[i], bb);
                        if (cx != 0f || cy != 0f)
                        {
                            list[i] = list[i] with { X = list[i].X + cx, Y = list[i].Y + cy };
                            moved = true;
                        }
                    }

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

            // An unlabeled tile marker boxed in with no clear spot is left off rather than drawn over a building or plate.
            var kept = new List<WorldEntity>(list.Count);
            foreach (var e in list)
            {
                if (HasPlate(e) || (!Blocked(e, kept, fixedPlaces, gap) && SceneryHit(e, obstacles, 0f) == null))
                {
                    kept.Add(e);
                }
            }

            return kept;
        }

        /// <summary>Smallest move of <paramref name="a"/> that clears every box pair with <paramref name="b"/> (0,0 when clear).</summary>
        private static (float Dx, float Dy) Push(WorldEntity a, WorldEntity b, float gap)
        {
            var aBoxes = Boxes(a);
            var bBoxes = Boxes(b);
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

        private static bool Blocked(WorldEntity e, List<WorldEntity> kept, IReadOnlyList<WorldEntity> fixedPlaces, float gap)
        {
            foreach (var o in kept)
            {
                var (dx, dy) = Push(e, o, gap);
                if (dx != 0f || dy != 0f) return true;
            }

            foreach (var o in fixedPlaces)
            {
                var (dx, dy) = Push(e, o, gap);
                if (dx != 0f || dy != 0f) return true;
            }

            return false;
        }

        /// <summary>The first scenery box the place's building or plate touches (null when clear).</summary>
        public static MapRect? SceneryHit(WorldEntity e, IReadOnlyList<MapRect> scenery, float gap = 0f)
        {
            foreach (var box in Boxes(e))
            {
                foreach (var s in scenery)
                {
                    if (box.Overlaps(s, gap)) return s;
                }
            }

            return null;
        }

        /// <summary>Every place whose building or plate sits on scenery, as "id on scenery" strings. Empty when clean.</summary>
        public static List<string> OnScenery(IReadOnlyList<WorldEntity> entities, IReadOnlyList<MapRect> scenery)
        {
            var bad = new List<string>();
            foreach (var e in entities)
            {
                if (SceneryHit(e, scenery) is { } s)
                {
                    bad.Add(e.Id + " on scenery (" + s.X0 + "," + s.Y0 + ")-(" + s.X1 + "," + s.Y1 + ")");
                }
            }

            return bad;
        }

        /// <summary>Share of the clearing decal's half-size (from its centre) that is fully opaque grass; the rest fades out.</summary>
        public const float ClearingSolid = 0.6f;

        /// <summary>
        /// The soft grass clearing drawn under a place on the painted heart, so baked trees and rocks never show around its
        /// building. Its solid core is an ellipse through the building footprint's corners; the fade runs beyond that.
        /// The name plate is an opaque panel, so it hides whatever is under it and needs no clearing of its own.
        /// </summary>
        public static MapRect Clearing(WorldEntity e)
        {
            var f = Footprint(e);
            var k = 1.4143f / ClearingSolid; // ellipse through the box corners = box * sqrt(2); the solid core is ClearingSolid of the decal
            var cx = (f.X0 + f.X1) * 0.5f;
            var cy = (f.Y0 + f.Y1) * 0.5f;
            var hw = (f.X1 - f.X0) * 0.5f * k;
            var hh = (f.Y1 - f.Y0) * 0.5f * k;
            return new MapRect(cx - hw, cy - hh, cx + hw, cy + hh);
        }

        /// <summary>Drawn size (square) of a decorative map prop; the view draws it at this size.</summary>
        public static float PropSize(WorldProp p) => (p.Type == "mountains" ? 440f : p.Type == "lake" ? 360f : 340f) * p.Scale;

        /// <summary>Prop pivot: the prop position sits this far up from the bottom of its art.</summary>
        public const float PropPivotY = 0.3f;

        /// <summary>The visible part of a prop (painted art has soft transparent margins).</summary>
        public static MapRect PropFootprint(WorldProp p)
        {
            var s = PropSize(p);
            var hw = s * 0.4f;
            var bottom = p.Y - s * PropPivotY + s * 0.1f;
            return new MapRect(p.X - hw, bottom, p.X + hw, bottom + s * 0.8f);
        }

        /// <summary>Props left after dropping any whose footprint touches a place's building or plate.</summary>
        public static List<WorldProp> ClearProps(IReadOnlyList<WorldProp> props, IReadOnlyList<WorldEntity> entities)
        {
            var kept = new List<WorldProp>(props.Count);
            foreach (var p in props)
            {
                var f = PropFootprint(p);
                var hit = false;
                foreach (var e in entities)
                {
                    if (SceneryHit(e, new[] { f }, MinGap) != null)
                    {
                        hit = true;
                        break;
                    }
                }

                if (!hit) kept.Add(p);
            }

            return kept;
        }

        private static (float Dx, float Dy) PushOff(WorldEntity a, MapRect o, float gap, MapRect? bounds)
        {
            float bestX = 0f, bestY = 0f;
            foreach (var p in Boxes(a))
            {
                if (!p.Overlaps(o, gap)) continue;
                var cands = new[]
                {
                    (o.X1 + gap - p.X0 + 1f, 0f), (-(p.X1 + gap - o.X0 + 1f), 0f),
                    (0f, o.Y1 + gap - p.Y0 + 1f), (0f, -(p.Y1 + gap - o.Y0 + 1f)),
                };
                float mx = 0f, my = 0f, best = float.MaxValue;
                foreach (var (cx, cy) in cands)
                {
                    var len = Math.Abs(cx) + Math.Abs(cy);
                    if (bounds is { } b)
                    {
                        var (ox, oy) = Contain(a with { X = a.X + cx, Y = a.Y + cy }, b);
                        if (ox != 0f || oy != 0f) len += 100000f; // leaving the bounds is a last resort
                    }

                    if (len < best)
                    {
                        best = len;
                        mx = cx;
                        my = cy;
                    }
                }

                if (Math.Abs(mx) > Math.Abs(bestX)) bestX = mx;
                if (Math.Abs(my) > Math.Abs(bestY)) bestY = my;
            }

            return (bestX, bestY);
        }

        /// <summary>Move that pulls the place's building and plate back inside <paramref name="b"/>.</summary>
        private static (float Dx, float Dy) Contain(WorldEntity a, MapRect b)
        {
            float dx = 0f, dy = 0f;
            foreach (var p in Boxes(a))
            {
                if (p.X0 < b.X0) dx = Math.Max(dx, b.X0 - p.X0);
                if (p.X1 > b.X1) dx = Math.Min(dx, b.X1 - p.X1);
                if (p.Y0 < b.Y0) dy = Math.Max(dy, b.Y0 - p.Y0);
                if (p.Y1 > b.Y1) dy = Math.Min(dy, b.Y1 - p.Y1);
            }

            return (dx, dy);
        }

        private static MapRect[] Boxes(WorldEntity e) =>
            HasPlate(e) ? new[] { Footprint(e), Plate(e, PlateMaxScale) } : new[] { Footprint(e) };

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
