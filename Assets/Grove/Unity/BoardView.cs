using Grove.Domain.Board;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>World-space layout for the 7×5 board. Visuals are placeholders (2D UI / sprites come later).</summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 origin = new(-3f, -2f);

        public BoardGrid? Board { get; private set; }
        public float CellSize => cellSize;

        public void Bind(BoardGrid board)
        {
            Board = board;
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
    }
}
