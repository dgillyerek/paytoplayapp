using System.Collections.Generic;
using Grove.Domain.Board;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>Renders the 7×5 greybox board and piece stacks. Call <see cref="Refresh"/> after every domain mutation.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1.15f;
        [SerializeField] private Vector2 origin = new(-3.45f, -2.1f);

        private Transform? _cellsRoot;
        private Transform? _piecesRoot;
        private GameObject? _ghost;
        private TextMesh? _ghostLabel;
        private MeshRenderer? _ghostRenderer;
        private GameObject? _hover;
        private readonly Dictionary<GridPos, GameObject> _pieceViews = new();

        public BoardGrid? Board { get; private set; }
        public float CellSize => cellSize;
        public GridPos? HiddenCell { get; set; }

        public void Bind(BoardGrid board)
        {
            Board = board;
            BuildGrid();
            Refresh();
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
            ApplyPieceVisual(_ghostRenderer!, _ghostLabel!, stack);
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
            var boardColor = new Color(0.18f, 0.32f, 0.2f);
            var alt = new Color(0.22f, 0.38f, 0.24f);
            var size = new Vector3(cellSize * 0.92f, cellSize * 0.92f, 1f);
            for (var x = 0; x < BoardGrid.Columns; x++)
            {
                for (var y = 0; y < BoardGrid.Rows; y++)
                {
                    var pos = new GridPos(x, y);
                    var color = (x + y) % 2 == 0 ? boardColor : alt;
                    GroveVisuals.QuadObject($"Cell_{x}_{y}", _cellsRoot, CellToWorld(pos), size, color);
                }
            }

            var frame = GroveVisuals.QuadObject(
                "BoardFrame",
                _cellsRoot,
                new Vector3(origin.x + 3 * cellSize, origin.y + 2 * cellSize, 0.05f),
                new Vector3(BoardGrid.Columns * cellSize + 0.25f, BoardGrid.Rows * cellSize + 0.25f, 1f),
                new Color(0.08f, 0.14f, 0.09f));
            frame.transform.SetAsFirstSibling();

            _hover = GroveVisuals.QuadObject(
                "Hover",
                _cellsRoot,
                Vector3.zero,
                new Vector3(cellSize * 0.96f, cellSize * 0.96f, 1f),
                new Color(1f, 0.95f, 0.4f, 0.35f));
            _hover.SetActive(false);
        }

        private GameObject CreatePiece(GridPos pos, PieceStack stack)
        {
            var go = GroveVisuals.QuadObject(
                $"Piece_{pos.X}_{pos.Y}",
                _piecesRoot!,
                CellToWorld(pos) + new Vector3(0f, 0f, -0.05f),
                new Vector3(cellSize * 0.72f, cellSize * 0.72f, 1f),
                GroveVisuals.PieceColor(stack.Id.Value));
            var label = GroveVisuals.Label(
                "Label",
                go.transform,
                new Vector3(0f, 0f, -0.02f),
                GroveVisuals.ShortLabel(stack.Id.Value, stack.Count),
                42,
                Color.white);
            label.gameObject.transform.localScale = new Vector3(1f / 0.72f, 1f / 0.72f, 1f);
            return go;
        }

        private void EnsureGhost()
        {
            if (_ghost != null)
            {
                return;
            }

            _ghost = GroveVisuals.QuadObject(
                "Ghost",
                transform,
                Vector3.zero,
                new Vector3(cellSize * 0.78f, cellSize * 0.78f, 1f),
                Color.white);
            _ghostRenderer = _ghost.GetComponent<MeshRenderer>();
            _ghostLabel = GroveVisuals.Label("Label", _ghost.transform, new Vector3(0f, 0f, -0.02f), "", 42, Color.white);
            _ghost.SetActive(false);
        }

        private static void ApplyPieceVisual(MeshRenderer renderer, TextMesh label, PieceStack stack)
        {
            var color = GroveVisuals.PieceColor(stack.Id.Value);
            var mat = renderer.material;
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            label.text = GroveVisuals.ShortLabel(stack.Id.Value, stack.Count);
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
