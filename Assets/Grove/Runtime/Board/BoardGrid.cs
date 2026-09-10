using System;
using System.Collections.Generic;
using Grove.Domain.Orders;

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

        /// <summary>First empty cell in column-major order (x then y). Used by the garden crate spit.</summary>
        public bool TryFindEmpty(out GridPos pos)
        {
            for (var x = 0; x < Columns; x++)
            {
                for (var y = 0; y < Rows; y++)
                {
                    if (_cells[x, y] is null)
                    {
                        pos = new GridPos(x, y);
                        return true;
                    }
                }
            }

            pos = default;
            return false;
        }

        public int CountItem(PieceId id)
        {
            var count = 0;
            for (var x = 0; x < Columns; x++)
            {
                for (var y = 0; y < Rows; y++)
                {
                    var stack = _cells[x, y];
                    if (stack is { } occupant && occupant.Id.Equals(id))
                    {
                        count += occupant.Count;
                    }
                }
            }

            return count;
        }

        public bool Has(IReadOnlyList<OrderRequirement> requirements)
        {
            foreach (var req in requirements)
            {
                if (CountItem(req.Item) < req.Count)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryConsume(IReadOnlyList<OrderRequirement> requirements)
        {
            if (!Has(requirements))
            {
                return false;
            }

            foreach (var req in requirements)
            {
                var remaining = req.Count;
                for (var x = 0; x < Columns && remaining > 0; x++)
                {
                    for (var y = 0; y < Rows && remaining > 0; y++)
                    {
                        var stack = _cells[x, y];
                        if (stack is not { } occupant || !occupant.Id.Equals(req.Item))
                        {
                            continue;
                        }

                        if (occupant.Count <= remaining)
                        {
                            remaining -= occupant.Count;
                            _cells[x, y] = null;
                        }
                        else
                        {
                            _cells[x, y] = occupant.WithCount(occupant.Count - remaining);
                            remaining = 0;
                        }
                    }
                }
            }

            return true;
        }

        public bool TryFindTwoMatching(out GridPos a, out GridPos b)
        {
            var seen = new Dictionary<string, GridPos>();
            for (var x = 0; x < Columns; x++)
            {
                for (var y = 0; y < Rows; y++)
                {
                    if (_cells[x, y] is not { } occupant)
                    {
                        continue;
                    }

                    var key = occupant.Id.Value;
                    if (seen.TryGetValue(key, out var prev))
                    {
                        a = prev;
                        b = new GridPos(x, y);
                        return true;
                    }

                    seen[key] = new GridPos(x, y);
                }
            }

            a = default;
            b = default;
            return false;
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
