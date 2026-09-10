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

        public bool Contains(NormRect inner) =>
            inner.XMin >= XMin - 0.0001f
            && inner.XMax <= XMax + 0.0001f
            && inner.YMin >= YMin - 0.0001f
            && inner.YMax <= YMax + 0.0001f;

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
    /// DEV-019 Play layout per DES-003 mock + handoff.
    /// Design Y% is from the TOP of 1080×1920; Unity / this type use Y = 0 at the bottom.
    /// Mock stack: energy L + Maya R, small Goal pill under that row, board, then order dock.
    /// Orders live only in the dock (Design 70–100%). Cells live in the board band (~10–66%).
    /// Gap ≥ 16dp.
    /// </summary>
    public static class PlayLayout
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const float PortraitAspect = ReferenceWidth / ReferenceHeight;
        public const float MinOrthographicSize = 5.2f;
        public const float MinHudGapDp = 16f;
        public const int InventorySlotCount = 5;

        /// <summary>Portrait 1080×1920 reference type. Minimum arm's-length readable size.</summary>
        public const int TypeMinReadable = 28;
        public const int TypeOrderBody = 30;
        public const int TypeOrderActive = 34;
        public const int TypeButton = 38;
        public const int TypeHud = 40;
        public const int TypeGoal = 36;
        public const int TypeTeach = 36;
        public const int TypeTeachRing = 30;
        public const int TypeCrate = 32;
        public const int TypeSplash = 44;
        public const int TypeToast = 34;
        public const int TypeCoin = 42;

        /// <summary>Must match <c>Board.unity</c> BoardView serialization.</summary>
        public const float CellSize = 1f;
        public const float OriginX = -3f;
        public const float OriginY = -2f;

        /// <summary>Design top-bar / board / dock edges, Y from the TOP of the 1080×1920 frame.</summary>
        public const float DesignTopBarTop = 0f;
        public const float DesignEnergyBottom = 0.08f;
        public const float DesignTopBarBottom = 0.10f;
        public const float DesignBoardTop = 0.10f;
        public const float DesignBoardBottom = 0.66f;
        public const float DesignDockTop = 0.70f;
        public const float DesignDockBottom = 1f;

        /// <summary>Prefer bottom dock until DES-003 drops a side-rail PNG.</summary>
        public static HudDockKind DockKind => HudDockKind.Bottom;

        public static float MinGapNormX => MinHudGapDp / ReferenceWidth;
        public static float MinGapNormY => MinHudGapDp / ReferenceHeight;

        /// <summary>Design Y from the top (0–1) → Unity Y from the bottom.</summary>
        public static float FromDesignTop(float designTop01) => 1f - designTop01;

        public static readonly NormRect TopBarBand = BandFromDesign(0f, DesignTopBarTop, 1f, DesignTopBarBottom);
        public static readonly NormRect EnergyBand = BandFromDesign(0f, DesignTopBarTop, 1f, DesignEnergyBottom);
        public static readonly NormRect BoardBand = BandFromDesign(0f, DesignBoardTop, 1f, DesignBoardBottom);
        public static readonly NormRect DockBand = BandFromDesign(0f, DesignDockTop, 1f, DesignDockBottom);

        /// <summary>
        /// 7×5 cell rect inside the board band. Inset from the left so the Garden Crate
        /// can sit left/center on the board without covering cells. Centered under the Goal pill
        /// like DES003_PlayHud_LayoutMock.
        /// </summary>
        public static readonly NormRect BoardSafeRect = new NormRect(0.18f, 0.365f, 0.84f, 0.830f);

        public static readonly NormRect Playfield = BoardSafeRect;

        public static readonly NormRect EnergyPill = new NormRect(0.06f, 0.928f, 0.145f, 0.988f);
        public static readonly NormRect EnergyBar = new NormRect(0.145f, 0.942f, 0.30f, 0.974f);
        public static readonly NormRect CoinIcon = new NormRect(0.30f, 0.930f, 0.385f, 0.986f);
        public static readonly NormRect CoinLabel = new NormRect(0.385f, 0.928f, 0.50f, 0.988f);
        public static readonly NormRect EnergyLabel = new NormRect(0.50f, 0.928f, 0.64f, 0.988f);
        public static readonly NormRect Maya = new NormRect(0.80f, 0.905f, 0.975f, 0.995f);
        public static readonly NormRect Goal = new NormRect(0.34f, 0.872f, 0.66f, 0.918f);
        public static readonly NormRect Toast = new NormRect(0.14f, 0.840f, 0.76f, 0.868f);
        public static readonly NormRect MayaBubble = new NormRect(0.76f, 0.840f, 0.975f, 0.900f);

        /// <summary>
        /// Cream dock fill behind orders + inventory. Not <c>UI_OrderDock_Panel</c> —
        /// that PNG is a 5-slot inventory bar and must not be stretched over the order cards.
        /// </summary>
        public static readonly NormRect DockPlate = new NormRect(0.04f, 0.000f, 0.96f, 0.300f);
        public static readonly NormRect OrderTray = new NormRect(0.08f, 0.125f, 0.92f, 0.288f);
        public static readonly NormRect InventoryBar = new NormRect(0.22f, 0.012f, 0.76f, 0.108f);
        public static readonly NormRect Store = new NormRect(0.04f, 0.012f, 0.20f, 0.108f);

        public static readonly NormRect Crate = new NormRect(0.025f, 0.48f, 0.165f, 0.655f);
        public static readonly NormRect CrateLabel = new NormRect(0.025f, 0.658f, 0.165f, 0.715f);

        public static readonly NormRect TeachCrateRing = Crate.Inflate(0.008f, 0.008f);
        /// <summary>Readable teach line between dock and board — not a 38px sliver.</summary>
        public static readonly NormRect TeachHudCaption = new NormRect(0.08f, 0.305f, 0.92f, 0.358f);
        public static readonly NormRect TeachSkip = new NormRect(0.78f, 0.012f, 0.96f, 0.108f);

        public static NormRect ActiveOrderCard
        {
            get
            {
                var w = (OrderTray.XMax - OrderTray.XMin) / 3f;
                return new NormRect(OrderTray.XMin, OrderTray.YMin, OrderTray.XMin + w, OrderTray.YMax);
            }
        }

        public static NormRect InventorySlotLocal(int index)
        {
            var i = index < 0 ? 0 : (index >= InventorySlotCount ? InventorySlotCount - 1 : index);
            var w = 1f / InventorySlotCount;
            return new NormRect(i * w + 0.03f, 0.06f, (i + 1) * w - 0.03f, 0.94f);
        }

        /// <summary>Permanent HUD chrome that must never cover playable cells.</summary>
        public static NormRect[] OccludingHud() =>
            new[]
            {
                Goal, EnergyPill, EnergyBar, EnergyLabel, CoinIcon, CoinLabel, Toast,
                DockPlate, Maya, MayaBubble, OrderTray, InventoryBar, Crate, CrateLabel, Store
            };

        public static bool MeetsMinHudGap(NormRect chrome)
        {
            if (chrome.Overlaps(BoardSafeRect))
            {
                return false;
            }

            var xOverlap = chrome.XMin < BoardSafeRect.XMax && chrome.XMax > BoardSafeRect.XMin;
            var yOverlap = chrome.YMin < BoardSafeRect.YMax && chrome.YMax > BoardSafeRect.YMin;

            if (xOverlap)
            {
                if (chrome.YMax <= BoardSafeRect.YMin)
                {
                    return BoardSafeRect.YMin - chrome.YMax >= MinGapNormY - 0.0001f;
                }

                if (chrome.YMin >= BoardSafeRect.YMax)
                {
                    return chrome.YMin - BoardSafeRect.YMax >= MinGapNormY - 0.0001f;
                }
            }

            if (yOverlap)
            {
                if (chrome.XMax <= BoardSafeRect.XMin)
                {
                    return BoardSafeRect.XMin - chrome.XMax >= MinGapNormX - 0.0001f;
                }

                if (chrome.XMin >= BoardSafeRect.XMax)
                {
                    return chrome.XMin - BoardSafeRect.XMax >= MinGapNormX - 0.0001f;
                }
            }

            return true;
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

        private static NormRect BandFromDesign(float xMin, float designTop, float xMax, float designBottom) =>
            new NormRect(xMin, FromDesignTop(designBottom), xMax, FromDesignTop(designTop));
    }
}
