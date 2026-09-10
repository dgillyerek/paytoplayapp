using Grove.Domain.Board;
using Grove.Domain.Commerce;
using Grove.Domain.Merge;
using UnityEngine;

namespace Grove.Unity
{
    /// <summary>Wires DEV-001 domain (7×5 board, 3→1 merge, FakeStore) into a scene.</summary>
    public sealed class GroveBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardView boardView = null!;
        [SerializeField] private DragMergeController dragController = null!;

        public MergeSession Session { get; private set; } = null!;
        public FakeStore Store { get; private set; } = null!;

        private void Awake()
        {
            var board = new BoardGrid();
            Session = new MergeSession(board, GroveCatalog.CreateDefault());
            Store = new FakeStore();

            SeedDemoBoard(board);

            if (boardView != null)
            {
                boardView.Bind(board);
            }

            if (dragController != null)
            {
                dragController.Bind(Session, boardView);
            }
        }

        /// <summary>Places a 3-merge puzzle: two pebbles stacked path plus a third pebble.</summary>
        internal static void SeedDemoBoard(BoardGrid board)
        {
            board.Place(new GridPos(1, 1), new PieceStack(GroveCatalog.Pebble, 1));
            board.Place(new GridPos(2, 1), new PieceStack(GroveCatalog.Pebble, 2));
            board.Place(new GridPos(3, 2), new PieceStack(GroveCatalog.Sprout, 1));
        }
    }
}
