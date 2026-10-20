#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Serialization;

namespace Survival.Domain.World
{
    public enum WorldEntityKind
    {
        PlayerVillage,
        RivalVillage,
        AllyVillage,
        EnemyCamp,
        ResourceNode,

        /// <summary>One of the six catalog world nodes (home, quarry, outpost, guild fort, dark keep, ruins).</summary>
        Landmark
    }

    /// <summary>
    /// Something on the kingdom map. Position is in map pixels (origin bottom-left, y up). Type is the camp /
    /// resource type ("goblin", "gold") or the catalog node type for landmarks.
    /// </summary>
    public sealed record WorldEntity(
        string Id,
        WorldEntityKind Kind,
        string Type,
        string Name,
        int Level,
        int Power,
        int Amount,
        float X,
        float Y)
    {
        public bool IsVillage => Kind is WorldEntityKind.PlayerVillage or WorldEntityKind.RivalVillage or WorldEntityKind.AllyVillage;

        public float DistanceTo(WorldEntity other)
        {
            var dx = other.X - X;
            var dy = other.Y - Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>A scripted mock march: owner village -> target, looping out and back so the map is always alive.</summary>
    public sealed record WorldMarchRoute(string OwnerId, string TargetId, float OffsetSeconds);

    public sealed record WorldProp(string Type, float X, float Y, float Scale);

    /// <summary>
    /// map/world_layout.json: the zoomed-out kingdom. The painted crusade map with its six catalog nodes sits at
    /// <see cref="HeartX"/>.. (map pixels); villages, camps, resource nodes, props and mock march routes around it.
    /// </summary>
    public sealed class WorldLayout
    {
        public WorldLayout(
            float width,
            float height,
            float minZoom,
            float maxZoom,
            float startZoom,
            float marchSpeed,
            float minMarchSeconds,
            float maxMarchSeconds,
            int maxPlayerMarches,
            (float X, float Y, float W, float H) heart,
            IReadOnlyList<WorldEntity> entities,
            IReadOnlyList<WorldMarchRoute> routes,
            IReadOnlyList<WorldProp> props)
        {
            Width = width;
            Height = height;
            MinZoom = minZoom;
            MaxZoom = maxZoom;
            StartZoom = startZoom;
            MarchSpeed = marchSpeed;
            MinMarchSeconds = minMarchSeconds;
            MaxMarchSeconds = maxMarchSeconds;
            MaxPlayerMarches = maxPlayerMarches;
            HeartX = heart.X;
            HeartY = heart.Y;
            HeartWidth = heart.W;
            HeartHeight = heart.H;
            Entities = entities;
            Routes = routes;
            Props = props;
        }

        public float Width { get; }
        public float Height { get; }
        public float MinZoom { get; }
        public float MaxZoom { get; }
        public float StartZoom { get; }

        /// <summary>Map pixels per second for marches (duration = distance / speed, clamped).</summary>
        public float MarchSpeed { get; }
        public float MinMarchSeconds { get; }
        public float MaxMarchSeconds { get; }
        public int MaxPlayerMarches { get; }

        /// <summary>Bottom-left corner and size (map pixels) of the painted crusade map holding the six nodes.</summary>
        public float HeartX { get; }
        public float HeartY { get; }
        public float HeartWidth { get; }
        public float HeartHeight { get; }

        public IReadOnlyList<WorldEntity> Entities { get; }
        public IReadOnlyList<WorldMarchRoute> Routes { get; }
        public IReadOnlyList<WorldProp> Props { get; }

        public WorldEntity? Player
        {
            get
            {
                foreach (var e in Entities)
                {
                    if (e.Kind == WorldEntityKind.PlayerVillage)
                    {
                        return e;
                    }
                }

                return null;
            }
        }

        /// <summary>Map position of a catalog node pin (normalized on the crusade map, y from top).</summary>
        public (float X, float Y) PinToMap(float pinX, float pinYFromTop) =>
            (HeartX + pinX * HeartWidth, HeartY + (1f - pinYFromTop) * HeartHeight);

        /// <summary>Layout plus the six catalog nodes as <see cref="WorldEntityKind.Landmark"/> entities.</summary>
        public WorldLayout WithLandmarks(IEnumerable<(string Id, string NodeTypeId, string Name, float PinX, float PinYFromTop)> nodes)
        {
            var list = new List<WorldEntity>(Entities);
            foreach (var n in nodes)
            {
                var (x, y) = PinToMap(n.PinX, n.PinYFromTop);
                list.Add(new WorldEntity(n.Id, WorldEntityKind.Landmark, n.NodeTypeId, n.Name, 0, 0, 0, x, y));
            }

            return new WorldLayout(
                Width, Height, MinZoom, MaxZoom, StartZoom, MarchSpeed, MinMarchSeconds, MaxMarchSeconds, MaxPlayerMarches,
                (HeartX, HeartY, HeartWidth, HeartHeight), list, Routes, Props);
        }

        public static WorldLayout Parse(string json)
        {
            var root = LayoutJson.Root(json, "world_layout");
            var w = LayoutJson.Obj(root, "world");
            var width = LayoutJson.Num(w, "width");
            var height = LayoutJson.Num(w, "height");
            (float X, float Y) At(JsonNode.Obj o) => (LayoutJson.Frac(o, "x") * width, (1f - LayoutJson.Frac(o, "y")) * height);

            var heart = LayoutJson.Obj(root, "heart");
            var hw = LayoutJson.Num(heart, "width");
            var hh = LayoutJson.Num(heart, "height");
            var (hcx, hcy) = At(heart);

            var entities = new List<WorldEntity>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            void Add(WorldEntity e)
            {
                if (!ids.Add(e.Id))
                {
                    throw new FormatException("Duplicate world entity id '" + e.Id + "'.");
                }

                entities.Add(e);
            }

            foreach (var v in LayoutJson.Objs(root, "villages"))
            {
                var kind = LayoutJson.OptStr(v, "kind", "rival") switch
                {
                    "player" => WorldEntityKind.PlayerVillage,
                    "ally" => WorldEntityKind.AllyVillage,
                    "rival" => WorldEntityKind.RivalVillage,
                    var other => throw new FormatException("Unknown village kind '" + other + "'.")
                };
                var (x, y) = At(v);
                Add(new WorldEntity(LayoutJson.Str(v, "id"), kind, "village", LayoutJson.Str(v, "name"), LayoutJson.OptInt(v, "level", 1), LayoutJson.OptInt(v, "power", 0), 0, x, y));
            }

            foreach (var c in LayoutJson.Objs(root, "camps"))
            {
                var (x, y) = At(c);
                Add(new WorldEntity(LayoutJson.Str(c, "id"), WorldEntityKind.EnemyCamp, LayoutJson.Str(c, "type"), LayoutJson.OptStr(c, "name", ""), LayoutJson.OptInt(c, "level", 1), LayoutJson.OptInt(c, "power", 0), LayoutJson.OptInt(c, "reward", 0), x, y));
            }

            foreach (var r in LayoutJson.Objs(root, "resources"))
            {
                var (x, y) = At(r);
                Add(new WorldEntity(LayoutJson.Str(r, "id"), WorldEntityKind.ResourceNode, LayoutJson.Str(r, "type"), LayoutJson.OptStr(r, "name", ""), LayoutJson.OptInt(r, "level", 1), 0, LayoutJson.OptInt(r, "amount", 0), x, y));
            }

            var routes = new List<WorldMarchRoute>();
            foreach (var m in LayoutJson.Objs(root, "marches"))
            {
                var owner = LayoutJson.Str(m, "owner");
                var target = LayoutJson.Str(m, "target");
                routes.Add(new WorldMarchRoute(owner, target, LayoutJson.OptNum(m, "offset", 0f)));
            }

            var props = new List<WorldProp>();
            foreach (var p in LayoutJson.Objs(root, "props"))
            {
                var (x, y) = At(p);
                props.Add(new WorldProp(LayoutJson.Str(p, "type"), x, y, LayoutJson.OptNum(p, "scale", 1f)));
            }

            var layout = new WorldLayout(
                width,
                height,
                LayoutJson.Num(w, "min_zoom"),
                LayoutJson.Num(w, "max_zoom"),
                LayoutJson.OptNum(w, "start_zoom", 1f),
                LayoutJson.OptNum(w, "march_speed", 120f),
                LayoutJson.OptNum(w, "min_march_seconds", 6f),
                LayoutJson.OptNum(w, "max_march_seconds", 45f),
                LayoutJson.OptInt(w, "max_player_marches", 3),
                (hcx - hw * 0.5f, hcy - hh * 0.5f, hw, hh),
                entities,
                routes,
                props);

            if (layout.Player == null)
            {
                throw new FormatException("world_layout needs one village with kind 'player'.");
            }

            foreach (var r in routes)
            {
                // Targets may be catalog landmarks (added later by WithLandmarks); owners must be villages here.
                if (!ids.Contains(r.OwnerId))
                {
                    throw new FormatException("March owner '" + r.OwnerId + "' is not a world entity.");
                }
            }

            return layout;
        }
    }
}
