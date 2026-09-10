using System;
using Grove.Domain.Board;

namespace Grove.Domain.Layout
{
    /// <summary>
    /// Normalized HUD / playfield bands for portrait Play Mode (1080×1920 reference).
    /// Y = 0 at the bottom of the Game view. The 7×5 board must sit entirely inside
    /// the mid playfield; chrome (goal, energy, Maya, order tray, crate) stays outside it.
    /// </summary>
    public readonly struct NormRect
    {
        public NormRect(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        public float XMin { get; }
        public float YMin { get; }
        public float XMax { get; }
        public float YMax { get; }

        public bool Overlaps(NormRect other) =>
            XMin < other.XMax && XMax > other.XMin && YMin < other.YMax && YMax > other.YMin;

        public bool OverlapsPlayfield() => Overlaps(PlayLayout.Playfield);
    }

    /// <summary>Orthographic camera pose that fits the 7×5 into <see cref="PlayLayout.Playfield"/>.</summary>
    public readonly struct CameraFrame
    {
        public CameraFrame(float centerX, float centerY, float orthographicSize)
        {
            CenterX = centerX;
            CenterY = centerY;
            OrthographicSize = orthographicSize;
        }

        public float CenterX { get; }
        public float CenterY { get; }
        public float OrthographicSize { get; }
    }

    /// <summary>
    /// Shared Play layout. Unity HUD Place() rects and BoardView camera framing both
    /// read these numbers so the order tray cannot cover the grid.
    /// </summary>
    public static class PlayLayout
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const float PortraitAspect = ReferenceWidth / ReferenceHeight;
        public const float MinOrthographicSize = 5.2f;

        /// <summary>Must match <c>Board.unity</c> BoardView serialization.</summary>
        public const float CellSize = 1f;
        public const float OriginX = -3f;
        public const float OriginY = -2f;

        public static readonly NormRect Playfield = new NormRect(0.04f, 0.235f, 0.96f, 0.700f);

        public static readonly NormRect Goal = new NormRect(0.08f, 0.900f, 0.92f, 0.950f);
        public static readonly NormRect EnergyPill = new NormRect(0.02f, 0.848f, 0.13f, 0.895f);
        public static readonly NormRect EnergyBar = new NormRect(0.13f, 0.858f, 0.40f, 0.888f);
        public static readonly NormRect EnergyLabel = new NormRect(0.135f, 0.848f, 0.42f, 0.895f);
        public static readonly NormRect CoinIcon = new NormRect(0.72f, 0.848f, 0.84f, 0.895f);
        public static readonly NormRect CoinLabel = new NormRect(0.84f, 0.848f, 0.98f, 0.895f);
        public static readonly NormRect Maya = new NormRect(0.015f, 0.718f, 0.175f, 0.842f);
        public static readonly NormRect OrderTray = new NormRect(0.180f, 0.718f, 0.985f, 0.842f);
        public static readonly NormRect Toast = new NormRect(0.10f, 0.700f, 0.90f, 0.716f);
        public static readonly NormRect Crate = new NormRect(0.32f, 0.045f, 0.68f, 0.175f);
        public static readonly NormRect CrateLabel = new NormRect(0.32f, 0.175f, 0.68f, 0.220f);
        public static readonly NormRect Store = new NormRect(0.03f, 0.048f, 0.24f, 0.105f);

        public static readonly NormRect CoachCratePointer = new NormRect(0.38f, 0.055f, 0.62f, 0.165f);
        public static readonly NormRect CoachCrateCaption = new NormRect(0.70f, 0.048f, 0.97f, 0.175f);
        public static readonly NormRect CoachMergePointer = new NormRect(0.42f, 0.42f, 0.58f, 0.58f);
        public static readonly NormRect CoachMergeCaption = new NormRect(0.10f, 0.668f, 0.90f, 0.716f);
        public static readonly NormRect CoachDeliverPointer = new NormRect(0.36f, 0.735f, 0.50f, 0.830f);
        public static readonly NormRect CoachDeliverCaption = new NormRect(0.18f, 0.668f, 0.90f, 0.716f);

        /// <summary>HUD chrome that must never cover the 7×5 (coach pointers over the board are sparkles, not occluders).</summary>
        public static NormRect[] OccludingHud() =>
            new[]
            {
                Goal, EnergyPill, EnergyBar, EnergyLabel, CoinIcon, CoinLabel,
                Maya, OrderTray, Toast, Crate, CrateLabel, Store
            };

        public static void BoardWorldBounds(out float minX, out float minY, out float maxX, out float maxY) =>
            BoardWorldBounds(CellSize, OriginX, OriginY, out minX, out minY, out maxX, out maxY);

        public static void BoardWorldBounds(
            float cellSize,
            float originX,
            float originY,
            out float minX,
            out float minY,
            out float maxX,
            out float maxY)
        {
            minX = originX - cellSize * 0.5f;
            minY = originY - cellSize * 0.5f;
            maxX = originX + (BoardGrid.Columns - 0.5f) * cellSize;
            maxY = originY + (BoardGrid.Rows - 0.5f) * cellSize;
        }

        /// <summary>
        /// Zoom and shift the camera so the board AABB sits inside the mid playfield band
        /// for the given width/height aspect (portrait 1080×1920 = 0.5625).
        /// </summary>
        public static CameraFrame FitBoard(float minX, float minY, float maxX, float maxY, float aspectWidthOverHeight)
        {
            const float worldPad = 0.15f;
            minX -= worldPad;
            minY -= worldPad;
            maxX += worldPad;
            maxY += worldPad;

            var boardW = Math.Max(0.01f, maxX - minX);
            var boardH = Math.Max(0.01f, maxY - minY);
            var boardCx = (minX + maxX) * 0.5f;
            var boardCy = (minY + maxY) * 0.5f;

            var bandW = Playfield.XMax - Playfield.XMin;
            var bandH = Playfield.YMax - Playfield.YMin;
            var aspect = aspectWidthOverHeight > 0.01f ? aspectWidthOverHeight : PortraitAspect;

            var viewHFromBoardH = boardH / bandH;
            var viewHFromBoardW = boardW / bandW / aspect;
            var viewH = Math.Max(viewHFromBoardH, viewHFromBoardW);
            viewH = Math.Max(viewH, MinOrthographicSize * 2f);

            var playfieldCenterY = (Playfield.YMin + Playfield.YMax) * 0.5f;
            var playfieldCenterX = (Playfield.XMin + Playfield.XMax) * 0.5f;
            var camY = boardCy + viewH * (0.5f - playfieldCenterY);
            var camX = boardCx + viewH * aspect * (0.5f - playfieldCenterX);
            return new CameraFrame(camX, camY, viewH * 0.5f);
        }

        public static void WorldToNdc(
            float worldX,
            float worldY,
            CameraFrame cam,
            float aspectWidthOverHeight,
            out float ndcX,
            out float ndcY)
        {
            var aspect = aspectWidthOverHeight > 0.01f ? aspectWidthOverHeight : PortraitAspect;
            var viewH = cam.OrthographicSize * 2f;
            var viewW = viewH * aspect;
            ndcX = (worldX - (cam.CenterX - viewW * 0.5f)) / viewW;
            ndcY = (worldY - (cam.CenterY - viewH * 0.5f)) / viewH;
        }

        public static bool BoardFitsPlayfield(float minX, float minY, float maxX, float maxY, CameraFrame cam, float aspect)
        {
            WorldToNdc(minX, minY, cam, aspect, out var x0, out var y0);
            WorldToNdc(maxX, maxY, cam, aspect, out var x1, out var y1);
            const float eps = 0.002f;
            return x0 >= Playfield.XMin - eps
                   && x1 <= Playfield.XMax + eps
                   && y0 >= Playfield.YMin - eps
                   && y1 <= Playfield.YMax + eps;
        }
    }
}
