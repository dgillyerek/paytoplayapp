using System.Collections.Generic;
using Grove.Domain.Board;
using Grove.Domain.Layout;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>Renders the 7×5 Front Garden board with DES-001 sprites. Call <see cref="Refresh"/> after every domain mutation.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private float cellSize = PlayLayout.CellSize;
        [SerializeField] private Vector2 origin = new(PlayLayout.OriginX, PlayLayout.OriginY);

        private Transform? _cellsRoot;
        private Transform? _piecesRoot;
        private Transform? _backdrop;
        private Transform? _surface;
        private Sprite? _backdropSprite;
        private Sprite? _surfaceSprite;
        private GameObject? _ghost;
        private TextMesh? _ghostLabel;
        private SpriteRenderer? _ghostRenderer;
        private GameObject? _hover;
        private readonly Dictionary<GridPos, GameObject> _pieceViews = new();

        public BoardGrid? Board { get; private set; }
        public float CellSize => cellSize;
        public GridPos? HiddenCell { get; set; }

        public Vector3 BoardCenter =>
            new(origin.x + (BoardGrid.Columns - 1) * cellSize * 0.5f,
                origin.y + (BoardGrid.Rows - 1) * cellSize * 0.5f,
                0f);

        public Vector2 BoardWorldSize =>
            new(BoardGrid.Columns * cellSize, BoardGrid.Rows * cellSize);

        public void Bind(BoardGrid board)
        {
            cellSize = PlayLayout.CellSize;
            origin = new Vector2(PlayLayout.OriginX, PlayLayout.OriginY);
            GroveArt.EnsureLoaded();
            Board = board;
            BuildGrid();
            Refresh();
            GroveVisuals.FrameBoard(this);
            CoverPlayfield();
        }

        public void GetWorldBounds(out float minX, out float minY, out float maxX, out float maxY) =>
            PlayLayout.BoardWorldBounds(cellSize, origin.x, origin.y, out minX, out minY, out maxX, out maxY);

        private void LateUpdate()
        {
            GroveVisuals.FrameBoard(this);
            if (_backdrop != null || _surface != null)
            {
                CoverPlayfield();
            }
        }

        public Vector3 CellToWorld(GridPos pos) =>
            new(origin.x + pos.X * cellSize, origin.y + pos.Y * cellSize, 0f);

        public bool TryWorldToCell(Vector3 world, out GridPos pos)
        {
            var localX = Mathf.RoundToInt((world.x - origin.x) / cellSize);
            var localY = Mathf.RoundToInt((world.y - origin.y) / cellSize);
            pos = new GridPos(localX, localY);
            return Board != null && Board.InBounds(pos);
        }

        public void SetHover(GridPos? pos)
        {
            if (_hover == null)
            {
                return;
            }

            if (pos is { } cell && Board != null && Board.InBounds(cell))
            {
                _hover.SetActive(true);
                _hover.transform.position = CellToWorld(cell) + new Vector3(0f, 0f, -0.02f);
            }
            else
            {
                _hover.SetActive(false);
            }
        }

        public void ShowGhost(PieceStack stack, Vector3 world)
        {
            EnsureGhost();
            _ghost!.SetActive(true);
            _ghost.transform.position = world + new Vector3(0f, 0f, -0.2f);
            ApplyPieceVisual(_ghostRenderer!, _ghostLabel!, stack, cellSize * 0.86f);
        }

        public void MoveGhost(Vector3 world)
        {
            if (_ghost != null && _ghost.activeSelf)
            {
                _ghost.transform.position = world + new Vector3(0f, 0f, -0.2f);
            }
        }

        public void HideGhost()
        {
            if (_ghost != null)
            {
                _ghost.SetActive(false);
            }
        }

        public void Punch(GridPos pos)
        {
            if (_pieceViews.TryGetValue(pos, out var go))
            {
                StartCoroutine(PunchRoutine(go.transform));
            }
        }

        public void Refresh()
        {
            if (Board == null)
            {
                return;
            }

            if (_piecesRoot == null)
            {
                var pieces = new GameObject("Pieces");
                pieces.transform.SetParent(transform, false);
                _piecesRoot = pieces.transform;
            }

            foreach (var kv in _pieceViews)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }

            _pieceViews.Clear();

            for (var x = 0; x < BoardGrid.Columns; x++)
            {
                for (var y = 0; y < BoardGrid.Rows; y++)
                {
                    var pos = new GridPos(x, y);
                    if (HiddenCell is { } hidden && hidden.Equals(pos))
                    {
                        continue;
                    }

                    if (!Board.TryGet(pos, out var stack))
                    {
                        continue;
                    }

                    _pieceViews[pos] = CreatePiece(pos, stack);
                }
            }
        }

        private void BuildGrid()
        {
            if (_cellsRoot != null)
            {
                return;
            }

            var root = new GameObject("Cells");
            root.transform.SetParent(transform, false);
            _cellsRoot = root.transform;
            var center = BoardCenter;
            var size = BoardWorldSize;
            var pad = cellSize * 0.85f;

            _backdropSprite = GroveArt.Get(GroveArt.StubBackdrop);
            if (_backdropSprite != null)
            {
                _backdrop = GroveVisuals.SpriteObject(
                    "Backdrop",
                    _cellsRoot,
                    center + new Vector3(0f, 0f, 0.35f),
                    Mathf.Max(size.x, size.y),
                    Color.white,
                    _backdropSprite,
                    -20).transform;
            }

            _surfaceSprite = GroveArt.Get(GroveArt.StubBoardSurface);
            var cellSprite = GroveArt.Get(GroveArt.StubCellEmpty);
            if (_surfaceSprite == null)
            {
                _surfaceSprite = cellSprite;
            }

            if (_surfaceSprite != null)
            {
                _surface = GroveVisuals.SpriteObject(
                    "BoardSurface",
                    _cellsRoot,
                    center + new Vector3(0f, 0f, 0.12f),
                    1f,
                    Color.white,
                    _surfaceSprite,
                    -8).transform;
                GroveVisuals.StretchToRect(
                    _surface,
                    _surfaceSprite,
                    center + new Vector3(0f, 0f, 0.12f),
                    size.x + pad,
                    size.y + pad);
            }

            if (cellSprite != null)
            {
                for (var x = 0; x < BoardGrid.Columns; x++)
                {
                    for (var y = 0; y < BoardGrid.Rows; y++)
                    {
                        var pos = new GridPos(x, y);
                        var cell = GroveVisuals.SpriteObject(
                            $"Cell_{x}_{y}",
                            _cellsRoot,
                            CellToWorld(pos) + new Vector3(0f, 0f, 0.02f),
                            cellSize,
                            Color.white,
                            cellSprite,
                            0);
                        GroveVisuals.CoverRect(
                            cell.transform,
                            cellSprite,
                            CellToWorld(pos) + new Vector3(0f, 0f, 0.02f),
                            cellSize * 1.06f,
                            cellSize * 1.06f);
                    }
                }
            }

            var highlight = GroveArt.Get(GroveArt.StubCellHighlight) ?? cellSprite;
            if (highlight != null)
            {
                _hover = GroveVisuals.SpriteObject(
                    "Hover",
                    _cellsRoot,
                    Vector3.zero,
                    cellSize,
                    Color.white,
                    highlight,
                    2);
                GroveVisuals.CoverRect(_hover.transform, highlight, Vector3.zero, cellSize * 1.08f, cellSize * 1.08f);
                _hover.SetActive(false);
            }
        }

        /// <summary>
        /// Backdrop covers the camera; wood BoardSurface fills the 7×5; cream cells overlap so no
        /// grey/green programmer grid can show between tiles or around the tray.
        /// </summary>
        public void CoverPlayfield()
        {
            var cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            var center = BoardCenter;
            var size = BoardWorldSize;
            var pad = cellSize * 0.85f;

            if (_surface != null && _surfaceSprite != null)
            {
                GroveVisuals.StretchToRect(
                    _surface,
                    _surfaceSprite,
                    center + new Vector3(0f, 0f, 0.12f),
                    size.x + pad,
                    size.y + pad);
            }

            if (_backdrop != null && _backdropSprite != null && cam != null && cam.orthographic)
            {
                var aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
                var viewH = cam.orthographicSize * 2f;
                var viewW = viewH * Mathf.Max(0.01f, aspect);
                var camCenter = new Vector3(cam.transform.position.x, cam.transform.position.y, 0.35f);
                GroveVisuals.CoverRect(_backdrop, _backdropSprite, camCenter, viewW * 1.35f, viewH * 1.35f);
            }
            else if (_backdrop != null && _backdropSprite != null)
            {
                GroveVisuals.CoverRect(
                    _backdrop,
                    _backdropSprite,
                    center + new Vector3(0f, 0f, 0.35f),
                    size.x * 3f,
                    size.y * 3.4f);
            }
        }

        private GameObject CreatePiece(GridPos pos, PieceStack stack)
        {
            var sprite = GroveArt.SpriteForItem(stack.Id.Value);
            var go = GroveVisuals.SpriteObject(
                $"Piece_{pos.X}_{pos.Y}",
                _piecesRoot!,
                CellToWorld(pos) + new Vector3(0f, 0f, -0.05f),
                cellSize * 0.82f,
                Color.white,
                sprite,
                5);
            if (stack.Count > 1)
            {
                GroveVisuals.Label(
                    "Count",
                    go.transform,
                    new Vector3(0.28f, -0.28f, -0.02f),
                    "×" + stack.Count,
                    28,
                    Color.white);
            }

            return go;
        }

        private void EnsureGhost()
        {
            if (_ghost != null)
            {
                return;
            }

            _ghost = GroveVisuals.SpriteObject(
                "Ghost",
                transform,
                Vector3.zero,
                cellSize * 0.86f,
                Color.white,
                GroveArt.Require(GroveArt.StubCellEmpty),
                12);
            _ghostRenderer = _ghost.GetComponent<SpriteRenderer>();
            _ghostLabel = GroveVisuals.Label("Count", _ghost.transform, new Vector3(0.28f, -0.28f, -0.02f), "", 28, Color.white);
            _ghost.SetActive(false);
        }

        private static void ApplyPieceVisual(SpriteRenderer renderer, TextMesh label, PieceStack stack, float worldSize)
        {
            var sprite = GroveArt.SpriteForItem(stack.Id.Value);
            renderer.sprite = sprite;
            renderer.color = Color.white;
            GroveVisuals.FitSprite(renderer.transform, sprite, worldSize);
            if (renderer.sharedMaterial != null)
            {
                GroveVisuals.ApplyColor(renderer.material, Color.white);
            }

            label.text = stack.Count > 1 ? "×" + stack.Count : "";
        }

        private System.Collections.IEnumerator PunchRoutine(Transform target)
        {
            var original = target.localScale;
            var t = 0f;
            while (t < 0.22f && target != null)
            {
                t += Time.deltaTime;
                var k = 1f + Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.22f)) * 0.35f;
                target.localScale = original * k;
                yield return null;
            }

            if (target != null)
            {
                target.localScale = original;
            }
        }
    }
}
