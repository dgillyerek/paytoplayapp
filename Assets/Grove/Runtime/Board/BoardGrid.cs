using System;

namespace Grove.Domain.Board
{
    /// <summary>DEV-001 playfield: 7 columns × 5 rows.</summary>
    public sealed class BoardGrid
    {
        public const int Columns = 7;
        public const int Rows = 5;

        private readonly PieceStack?[,] _cells = new PieceStack?[Columns, Rows];

        public int CellCount => Columns * Rows;

        public bool InBounds(GridPos pos) =>
            pos.X >= 0 && pos.X < Columns && pos.Y >= 0 && pos.Y < Rows;

        public PieceStack? this[GridPos pos]
        {
            get
            {
                EnsureInBounds(pos);
                return _cells[pos.X, pos.Y];
            }
            set
            {
                EnsureInBounds(pos);
                _cells[pos.X, pos.Y] = value;
            }
        }

        public bool TryGet(GridPos pos, out PieceStack stack)
        {
            if (!InBounds(pos) || _cells[pos.X, pos.Y] is not { } occupant)
            {
                stack = default;
                return false;
            }

            stack = occupant;
            return true;
        }

        public void Clear(GridPos pos)
        {
            EnsureInBounds(pos);
            _cells[pos.X, pos.Y] = null;
        }

        public void Place(GridPos pos, PieceStack stack)
        {
            if (this[pos] is not null)
            {
                throw new InvalidOperationException($"Cell {pos} is occupied.");
            }

            this[pos] = stack;
        }

        public int OccupiedCount
        {
            get
            {
                var count = 0;
                for (var x = 0; x < Columns; x++)
                {
                    for (var y = 0; y < Rows; y++)
                    {
                        if (_cells[x, y] is not null)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        private void EnsureInBounds(GridPos pos)
        {
            if (!InBounds(pos))
            {
                throw new ArgumentOutOfRangeException(nameof(pos), pos, $"Cell {pos} is outside the {Columns}x{Rows} board.");
            }
        }
    }
}
