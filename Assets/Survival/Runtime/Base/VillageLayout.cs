#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;
using Survival.Domain.Theme;

namespace Survival.Domain.Base
{
    /// <summary>One painted castle look: used from <see cref="MinLevel"/> up, drawn at <see cref="Scale"/> x its plot footprint.</summary>
    public sealed record CastleTier(int Tier, int MinLevel, string ArtKey, float Scale);

    /// <summary>
    /// Castle level -> painted tier. Each tier is distinct art and a larger footprint, so upgrading the Castle
    /// visibly grows it. Other buildings keep their art and show a level badge instead (<see cref="LevelBadge"/>).
    /// </summary>
    public static class CastleTiers
    {
        public const string ArtPrefix = "theme_a.village.castle.tier_";

        public static IReadOnlyList<CastleTier> Default { get; } = new[]
        {
            new CastleTier(1, 1, ArtPrefix + "1", 0.78f),
            new CastleTier(2, 3, ArtPrefix + "2", 0.92f),
            new CastleTier(3, 5, ArtPrefix + "3", 1.08f),
            new CastleTier(4, 7, ArtPrefix + "4", 1.26f)
        };

        /// <summary>Highest tier whose MinLevel is reached (levels below the first tier use the first).</summary>
        public static CastleTier For(int level, IReadOnlyList<CastleTier>? tiers = null)
        {
            tiers ??= Default;
            if (tiers.Count == 0)
            {
                throw new ArgumentException("No castle tiers.", nameof(tiers));
            }

            var best = tiers[0];
            foreach (var t in tiers)
            {
                if (level >= t.MinLevel && t.MinLevel >= best.MinLevel)
                {
                    best = t;
                }
            }

            return best;
        }

        /// <summary>True when going from <paramref name="fromLevel"/> to <paramref name="toLevel"/> swaps the castle art.</summary>
        public static bool TierChanged(int fromLevel, int toLevel, IReadOnlyList<CastleTier>? tiers = null) =>
            For(fromLevel, tiers).Tier != For(toLevel, tiers).Tier;

        /// <summary>The next tier's unlock level, or null at the top tier.</summary>
        public static int? NextTierLevel(int level, IReadOnlyList<CastleTier>? tiers = null)
        {
            tiers ??= Default;
            int? next = null;
            foreach (var t in tiers)
            {
                if (t.MinLevel > level && (next == null || t.MinLevel < next))
                {
                    next = t.MinLevel;
                }
            }

            return next;
        }

        /// <summary>Tiers must start at level 1, rise strictly in level, and grow strictly in scale.</summary>
        public static void Validate(IReadOnlyList<CastleTier> tiers)
        {
            if (tiers.Count == 0 || tiers[0].MinLevel != 1)
            {
                throw new FormatException("castle_tiers must start at min_level 1.");
            }

            for (var i = 1; i < tiers.Count; i++)
            {
                if (tiers[i].MinLevel <= tiers[i - 1].MinLevel || tiers[i].Scale <= tiers[i - 1].Scale)
                {
                    throw new FormatException("castle_tiers must rise in min_level and scale.");
                }
            }
        }
    }

    /// <summary>The little numbered level sign on Farms, Lumber Mills, Forges (every non-Castle building).</summary>
    public static class LevelBadge
    {
        public const string ArtKey = "theme_a.ui.level_badge";

        public static bool Shows(BasePlotDef def) => !string.Equals(def.BuildingId, SurvIds.BuildingHq, StringComparison.Ordinal);

        public static string Text(int level) => Math.Max(1, level).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>How a village building is drawn at its current level.</summary>
    public sealed record BuildingVisual(string ArtKey, float Scale, bool ShowBadge, string BadgeText, int Tier)
    {
        public static BuildingVisual For(BasePlotDef def, int level, IReadOnlyList<CastleTier>? tiers = null)
        {
            if (!LevelBadge.Shows(def))
            {
                var tier = CastleTiers.For(level, tiers);
                return new BuildingVisual(tier.ArtKey, tier.Scale, false, "", tier.Tier);
            }

            return new BuildingVisual(HookArtKeys.ForPlot(def.Id), 1f, true, LevelBadge.Text(level), 0);
        }

        public static BuildingVisual For(BasePlot plot, IReadOnlyList<CastleTier>? tiers = null) => For(plot.Def, plot.Level, tiers);
    }

    /// <summary>A building spot in the village: normalized center (x, y from top) and footprint width (fraction of map width).</summary>
    public sealed record VillagePlotSpot(string PlotId, float X, float YFromTop, float Size);

    /// <summary>An empty plot ("room for more"): shows a sign until the castle reaches <see cref="UnlockCastleLevel"/>.</summary>
    public sealed record VillageEmptySpot(string Id, float X, float YFromTop, float Size, int UnlockCastleLevel);

    public sealed record VillageProp(string Type, float X, float YFromTop, float Scale);

    /// <summary>A dirt road segment between two normalized points; width in map pixels.</summary>
    public sealed record VillageRoad(float X0, float Y0FromTop, float X1, float Y1FromTop, float Width);

    /// <summary>map/village_layout.json: the zoomed-in base the Village view scrolls around.</summary>
    public sealed class VillageLayout
    {
        public VillageLayout(
            float width,
            float height,
            float minZoom,
            float maxZoom,
            float startZoom,
            IReadOnlyList<CastleTier> castleTiers,
            IReadOnlyList<VillagePlotSpot> plots,
            IReadOnlyList<VillageEmptySpot> emptyPlots,
            IReadOnlyList<VillageProp> props,
            IReadOnlyList<VillageRoad> roads)
        {
            Width = width;
            Height = height;
            MinZoom = minZoom;
            MaxZoom = maxZoom;
            StartZoom = startZoom;
            CastleTiers = castleTiers;
            Plots = plots;
            EmptyPlots = emptyPlots;
            Props = props;
            Roads = roads;
        }

        public float Width { get; }
        public float Height { get; }
        public float MinZoom { get; }
        public float MaxZoom { get; }
        public float StartZoom { get; }
        public IReadOnlyList<CastleTier> CastleTiers { get; }
        public IReadOnlyList<VillagePlotSpot> Plots { get; }
        public IReadOnlyList<VillageEmptySpot> EmptyPlots { get; }
        public IReadOnlyList<VillageProp> Props { get; }
        public IReadOnlyList<VillageRoad> Roads { get; }

        public VillagePlotSpot? SpotFor(string plotId)
        {
            foreach (var s in Plots)
            {
                if (string.Equals(s.PlotId, plotId, StringComparison.Ordinal))
                {
                    return s;
                }
            }

            return null;
        }

        /// <summary>Map position (bottom-left origin, y up) of a normalized point.</summary>
        public (float X, float Y) ToMap(float x, float yFromTop) => (x * Width, (1f - yFromTop) * Height);

        public static VillageLayout Parse(string json)
        {
            var root = LayoutJson.Root(json, "village_layout");
            var v = LayoutJson.Obj(root, "village");
            var tiers = new List<CastleTier>();
            foreach (var t in LayoutJson.Objs(root, "castle_tiers"))
            {
                tiers.Add(new CastleTier(
                    LayoutJson.OptInt(t, "tier", tiers.Count + 1),
                    LayoutJson.OptInt(t, "min_level", 1),
                    LayoutJson.Str(t, "art"),
                    LayoutJson.Num(t, "scale")));
            }

            IReadOnlyList<CastleTier> castle = tiers.Count > 0 ? tiers : Base.CastleTiers.Default;
            Base.CastleTiers.Validate(castle);

            var plots = new List<VillagePlotSpot>();
            foreach (var p in LayoutJson.Objs(root, "plots"))
            {
                plots.Add(new VillagePlotSpot(LayoutJson.Str(p, "plot"), LayoutJson.Frac(p, "x"), LayoutJson.Frac(p, "y"), LayoutJson.Num(p, "size")));
            }

            var empty = new List<VillageEmptySpot>();
            foreach (var p in LayoutJson.Objs(root, "empty_plots"))
            {
                empty.Add(new VillageEmptySpot(
                    LayoutJson.Str(p, "id"),
                    LayoutJson.Frac(p, "x"),
                    LayoutJson.Frac(p, "y"),
                    LayoutJson.Num(p, "size"),
                    LayoutJson.OptInt(p, "unlock_castle", 0)));
            }

            var props = new List<VillageProp>();
            foreach (var p in LayoutJson.Objs(root, "props"))
            {
                props.Add(new VillageProp(LayoutJson.Str(p, "type"), LayoutJson.Frac(p, "x"), LayoutJson.Frac(p, "y"), LayoutJson.OptNum(p, "scale", 1f)));
            }

            var roads = new List<VillageRoad>();
            foreach (var r in LayoutJson.Objs(root, "roads"))
            {
                roads.Add(new VillageRoad(
                    LayoutJson.Frac(r, "x0"),
                    LayoutJson.Frac(r, "y0"),
                    LayoutJson.Frac(r, "x1"),
                    LayoutJson.Frac(r, "y1"),
                    LayoutJson.OptNum(r, "width", 90f)));
            }

            return new VillageLayout(
                LayoutJson.Num(v, "width"),
                LayoutJson.Num(v, "height"),
                LayoutJson.Num(v, "min_zoom"),
                LayoutJson.Num(v, "max_zoom"),
                LayoutJson.OptNum(v, "start_zoom", 1f),
                castle,
                plots,
                empty,
                props,
                roads);
        }
    }
}
