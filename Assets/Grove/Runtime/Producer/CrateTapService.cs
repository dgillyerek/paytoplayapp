using Grove.Domain.Board;
using Grove.Domain.Energy;
using Grove.Domain.Time;

namespace Grove.Domain.Producer
{
    /// <summary>Places a crate spit on the first empty board cell. Fails closed (no spend) when the board is full.</summary>
    public sealed class CrateTapService
    {
        private readonly GardenCrate _crate;
        private readonly BoardGrid _board;
        private readonly IEnergySpender _energy;
        private readonly IClock _clock;

        public CrateTapService(GardenCrate crate, BoardGrid board, IEnergySpender energy, IClock clock)
        {
            _crate = crate;
            _board = board;
            _energy = energy;
            _clock = clock;
        }

        public GardenCrate Crate => _crate;

        public SpitResult TryTap()
        {
            if (!_board.TryFindEmpty(out var pos))
            {
                return new SpitResult.Failed("board-full");
            }

            var result = _crate.TrySpit(_energy, _clock);
            if (result is SpitResult.Ok ok)
            {
                _board.Place(pos, new PieceStack(ok.Item, 1));
            }

            return result;
        }
    }
}
