using System;
using Grove.Domain.Board;

namespace Grove.Domain.Layout
{
    /// <summary>Normalized HUD rectangle. Y = 0 at the bottom of the Game view.</summary>
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

        public bool OverlapsPlayfield() => Overlaps(PlayLayout.BoardSafeRect);

        public NormRect Inflate(float padX, float padY) =>
            new NormRect(XMin - padX, YMin - padY, XMax + padX, YMax + padY);
    }

    /// <summary>Orthographic camera pose that fits the 7×5 into <see cref="PlayLayout.BoardSafeRect"/>.</summary>
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
    /// DES-003 hook: bottom safe dock (default) or a future side rail when that PNG lands.
    /// </summary>
    public enum HudDockKind
    {
        Bottom = 0,
        SideRail = 1
    }

    /// <summary>
    /// DEV-019 Play layout. HUD Place() and BoardView camera framing share these bands so
    /// order tray / Maya / teach captions never cover playable cells. Gap ≥ 16dp.
    /// </summary>
    public static class PlayLayout
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const float PortraitAspect = ReferenceWidth / ReferenceHeight;
        public const float MinOrthographicSize = 5.2f;
        public const float MinHudGapDp = 16f;

        /// <summary>Must match <c>Board.unity</c> BoardView serialization.</summary>
        public const float CellSize = 1f;
        public const float OriginX = -3f;
        public const float OriginY = -2f;

        /// <summary>Prefer bottom dock until DES-003 drops a side-rail PNG.</summary>
        public static HudDockKind DockKind => HudDockKind.Bottom;

        public static float MinGapNormX => MinHudGapDp / ReferenceWidth;
        public static float MinGapNormY => MinHudGapDp / ReferenceHeight;

        /// <summary>Clear mid band reserved for the 7×5. World board is framed into this rect.</summary>
        public static readonly NormRect BoardSafeRect = new NormRect(0.04f, 0.259f, 0.96f, 0.859f);

        public static readonly NormRect Playfield = BoardSafeRect;

        public static readonly NormRect Goal = new NormRect(0.08f, 0.928f, 0.92f, 0.982f);
        public static readonly NormRect EnergyPill = new NormRect(0.02f, 0.868f, 0.13f, 0.922f);
        public static readonly NormRect EnergyBar = new NormRect(0.13f, 0.880f, 0.40f, 0.910f);
        public static readonly NormRect EnergyLabel = new NormRect(0.135f, 0.868f, 0.42f, 0.922f);
        public static readonly NormRect CoinIcon = new NormRect(0.72f, 0.868f, 0.84f, 0.922f);
        public static readonly NormRect CoinLabel = new NormRect(0.84f, 0.868f, 0.98f, 0.922f);
        public static readonly NormRect Toast = new NormRect(0.43f, 0.868f, 0.70f, 0.922f);

        public static readonly NormRect DockPlate = new NormRect(0.01f, 0.148f, 0.99f, 0.250f);
        public static readonly NormRect Maya = new NormRect(0.012f, 0.148f, 0.155f, 0.248f);
        public static readonly NormRect MayaBubble = new NormRect(0.160f, 0.210f, 0.985f, 0.248f);
        public static readonly NormRect OrderTray = new NormRect(0.160f, 0.148f, 0.985f, 0.208f);
        public static readonly NormRect Crate = new NormRect(0.34f, 0.012f, 0.66f, 0.118f);
        public static readonly NormRect CrateLabel = new NormRect(0.34f, 0.118f, 0.66f, 0.140f);
        public static readonly NormRect Store = new NormRect(0.02f, 0.022f, 0.20f, 0.085f);

        public static readonly NormRect TeachCrateRing = Crate.Inflate(0.012f, 0.008f);
        public static readonly NormRect TeachCrateCaption = new NormRect(0.67f, 0.018f, 0.98f, 0.118f);
        public static readonly NormRect TeachHudCaption = MayaBubble;
        public static readonly NormRect TeachSkip = new NormRect(0.78f, 0.152f, 0.97f, 0.205f);

        public static NormRect ActiveOrderCard
        {
            get
            {
                var w = (OrderTray.XMax - OrderTray.XMin) / 3f;
                return new NormRect(OrderTray.XMin, OrderTray.YMin, OrderTray.XMin + w, OrderTray.YMax);
            }
        }

        /// <summary>HUD chrome that must never cover playable cells (world teach rings sit on targets, not here).</summary>
        public static NormRect[] OccludingHud() =>
            new[]
            {
                Goal, EnergyPill, EnergyBar, EnergyLabel, CoinIcon, CoinLabel, Toast,
                DockPlate, Maya, MayaBubble, OrderTray, Crate, CrateLabel, Store, TeachCrateCaption
            };

        public static bool MeetsMinHudGap(NormRect chrome)
        {
            if (chrome.Overlaps(BoardSafeRect))
            {
                return false;
            }

            var xOverlap = chrome.XMin < BoardSafeRect.XMax && chrome.XMax > BoardSafeRect.XMin;
            if (!xOverlap)
            {
                return true;
            }

            if (chrome.YMax <= BoardSafeRect.YMin)
            {
                return BoardSafeRect.YMin - chrome.YMax >= MinGapNormY - 0.0001f;
            }

            if (chrome.YMin >= BoardSafeRect.YMax)
            {
                return chrome.YMin - BoardSafeRect.YMax >= MinGapNormY - 0.0001f;
            }

            return false;
        }

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
        /// Zoom and shift the camera so the board AABB sits inside <see cref="BoardSafeRect"/>
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

            var bandW = BoardSafeRect.XMax - BoardSafeRect.XMin;
            var bandH = BoardSafeRect.YMax - BoardSafeRect.YMin;
            var aspect = aspectWidthOverHeight > 0.01f ? aspectWidthOverHeight : PortraitAspect;

            var viewHFromBoardH = boardH / bandH;
            var viewHFromBoardW = boardW / bandW / aspect;
            var viewH = Math.Max(viewHFromBoardH, viewHFromBoardW);
            viewH = Math.Max(viewH, MinOrthographicSize * 2f);

            var playfieldCenterY = (BoardSafeRect.YMin + BoardSafeRect.YMax) * 0.5f;
            var playfieldCenterX = (BoardSafeRect.XMin + BoardSafeRect.XMax) * 0.5f;
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
            return x0 >= BoardSafeRect.XMin - eps
                   && x1 <= BoardSafeRect.XMax + eps
                   && y0 >= BoardSafeRect.YMin - eps
                   && y1 <= BoardSafeRect.YMax + eps;
        }
    }
}
