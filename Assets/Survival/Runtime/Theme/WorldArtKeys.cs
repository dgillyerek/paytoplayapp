#nullable enable
using System.Collections.Generic;
using Survival.Domain.Base;
using Survival.Domain.World;

namespace Survival.Domain.Theme
{
    /// <summary>
    /// Theme content keys for the World (kingdom map) and Village views. Systems ask for a key; pack.json "art"
    /// decides the PNG. Castle tier keys live in <see cref="CastleTiers"/> / village_layout.json.
    /// </summary>
    public static class WorldArtKeys
    {
        public const string MapTile = "theme_a.map.tile.meadow";
        public const string RoadTile = "theme_a.map.tile.dirt";
        public const string MapHeart = "theme_a.map.heart";
        public const string VillageLawn = "theme_a.village.tile.lawn";
        public const string PlotEmpty = "theme_a.village.plot_empty";
        public const string IconWorld = "theme_a.ui.icon_world";
        public const string IconUpgrade = "theme_a.ui.icon_upgrade";
        public const string MarchPlayer = "theme_a.world.march.player";
        public const string MarchRival = "theme_a.world.march.rival";

        public static readonly string[] CampTypes = { "goblin", "bandit", "undead", "dragon" };
        public static readonly string[] ResourceTypes = { "gold", "wood", "food", "stone" };
        public static readonly string[] MapProps = { "forest", "mountains", "lake" };
        public static readonly string[] VillageProps = { "bushes" };

        public static string Village(WorldEntityKind kind) => kind switch
        {
            WorldEntityKind.PlayerVillage => "theme_a.world.village.player",
            WorldEntityKind.AllyVillage => "theme_a.world.village.ally",
            _ => "theme_a.world.village.rival"
        };

        public static string Camp(string type) => "theme_a.world.camp." + type;

        public static string ResourceNode(string type) => "theme_a.world.node." + type;

        public static string MapProp(string type) => "theme_a.map.prop." + type;

        /// <summary>Village props first look for a village-specific key, then fall back to the map prop.</summary>
        public static string VillageProp(string type) => "theme_a.village.prop." + type;

        /// <summary>Art for any map entity (landmarks use their node_pins content key, passed in).</summary>
        public static string For(WorldEntity e, string? landmarkKey = null) => e.Kind switch
        {
            WorldEntityKind.EnemyCamp => Camp(e.Type),
            WorldEntityKind.ResourceNode => ResourceNode(e.Type),
            WorldEntityKind.Landmark => landmarkKey ?? "",
            _ => Village(e.Kind)
        };

        public static IReadOnlyList<string> Required
        {
            get
            {
                var keys = new List<string>
                {
                    MapTile, RoadTile, MapHeart, VillageLawn, PlotEmpty, IconWorld, IconUpgrade, MarchPlayer, MarchRival,
                    LevelBadge.ArtKey,
                    Village(WorldEntityKind.PlayerVillage), Village(WorldEntityKind.RivalVillage), Village(WorldEntityKind.AllyVillage)
                };
                foreach (var t in CampTypes)
                {
                    keys.Add(Camp(t));
                }

                foreach (var t in ResourceTypes)
                {
                    keys.Add(ResourceNode(t));
                }

                foreach (var t in MapProps)
                {
                    keys.Add(MapProp(t));
                }

                foreach (var t in VillageProps)
                {
                    keys.Add(VillageProp(t));
                }

                foreach (var tier in CastleTiers.Default)
                {
                    keys.Add(tier.ArtKey);
                }

                return keys;
            }
        }
    }
}
