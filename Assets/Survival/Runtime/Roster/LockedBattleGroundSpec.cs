namespace Survival.Domain.Roster
{
    /// <summary>
    /// Battle ground that matches the world map (Theme A world terrain,
    /// art/map/THEME_A_WORLD_TERRAIN_PLAIN_1080x1920.png). The grass tile is built only from grass
    /// pixels of that map (no trees, rocks or paths), so it repeats without a visible pattern. The
    /// dirt track takes the map's path colour (RGB 149, 133, 97) over the same grass grain. One worn
    /// track runs down each lane. The horizon is the map grass faded into a light haze, and the
    /// ground fades into it with distance fog instead of ending on a flat dark colour.
    /// Presentation only.
    /// </summary>
    public static class LockedBattleGroundSpec
    {
        public const string MapDir = "ThemePack/fantasy_kingdom_a/art/map";
        public const string WorldTerrainFileName = "THEME_A_WORLD_TERRAIN_PLAIN_1080x1920.png";
        public const string GrassTileFileName = "THEME_A_BATTLE_GRASS_TILE_512.png";
        public const string DirtTrackFileName = "THEME_A_BATTLE_DIRT_TRACK_256x512.png";
        public const string GrassTileRel = MapDir + "/" + GrassTileFileName;
        public const string DirtTrackRel = MapDir + "/" + DirtTrackFileName;

        /// <summary>Ground plane size. Big enough that its edge is always lost in the fog.</summary>
        public const float GroundSizeMetres = 200f;

        /// <summary>One grass tile covers this many metres.</summary>
        public const float GrassTileMetres = 3f;

        /// <summary>Worn dirt track down each lane: width, length along the lane, and length one texture repeat covers.</summary>
        public const float TrackWidthMetres = 1.0f;
        public const float TrackLengthMetres = 7.5f;
        public const float TrackRepeatMetres = 2.5f;

        /// <summary>Soft contact shadow under each fighter: darkness, and its size as a multiple of the body's footprint.</summary>
        public const float ContactShadowAlpha = 0.55f;
        public const float ContactShadowFootprintScale = 1.15f;
        public const float ContactShadowMinMetres = 0.45f;

        /// <summary>Cap so spread wings (Ashwyrm, Stormcrest, Emberfang) do not throw a blob wider than the body standing on the ground.</summary>
        public const float ContactShadowMaxMetres = 1.3f;

        /// <summary>Map grass mean (0.39, 0.41, 0.20) faded halfway to a light sky haze.</summary>
        public const float HorizonR = 0.53f;
        public const float HorizonG = 0.58f;
        public const float HorizonB = 0.47f;

        /// <summary>Linear distance fog from the camera, in metres.</summary>
        public const float FogStartMetres = 16f;
        public const float FogEndMetres = 48f;
    }
}
