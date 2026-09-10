using System;
using Grove.Domain.Board;
using Grove.Domain.Energy;
using Grove.Domain.Time;

namespace Grove.Domain.Producer
{
    /// <summary>
    /// Fixed garden crate: 4 charges, 20s per charge (data-driven), weighted spit.
    /// First successful tap can be an FTUE free tap (no energy). After that, 1 energy per tap.
    /// </summary>
    public sealed class GardenCrate
    {
        private readonly ProducerDefinition _definition;
        private readonly WeightedTable _table;
        private readonly IRandomSource _rng;
        private int _charges;
        private DateTimeOffset? _rechargeAnchor;
        private string? _lastSpitId;

        public GardenCrate(
            ProducerDefinition definition,
            IRandomSource rng,
            bool ftueFreeTapRemaining = true,
            int? startingCharges = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _table = new WeightedTable(definition.Outputs);
            if (startingCharges is int c && (c < 0 || c > definition.MaxCharges))
            {
                throw new ArgumentOutOfRangeException(nameof(startingCharges), c, "Starting charges out of range.");
            }

            _charges = startingCharges ?? definition.MaxCharges;
            FtueFreeTapRemaining = ftueFreeTapRemaining;
        }

        public ProducerDefinition Definition => _definition;

        public int MaxCharges => _definition.MaxCharges;

        public int RechargeSeconds => _definition.RechargeSeconds;

        public bool FtueFreeTapRemaining { get; private set; }

        public string? LastSpitId => _lastSpitId;

        public int Charges(IClock clock)
        {
            ApplyRecharge(clock);
            return _charges;
        }

        public TimeSpan? TimeUntilNextCharge(IClock clock)
        {
            ApplyRecharge(clock);
            if (_charges >= _definition.MaxCharges || _rechargeAnchor is null)
            {
                return null;
            }

            var due = _rechargeAnchor.Value + TimeSpan.FromSeconds(_definition.RechargeSeconds);
            var remaining = due - clock.UtcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        public SpitResult TrySpit(IEnergySpender energy, IClock clock)
        {
            if (energy is null)
            {
                throw new ArgumentNullException(nameof(energy));
            }

            ApplyRecharge(clock);
            if (_charges <= 0)
            {
                return new SpitResult.Failed("no-charges");
            }

            if (FtueFreeTapRemaining)
            {
                FtueFreeTapRemaining = false;
            }
            else if (!energy.TrySpendProduce())
            {
                return new SpitResult.Failed("no-energy");
            }

            _charges--;
            if (_charges < _definition.MaxCharges && _rechargeAnchor is null)
            {
                _rechargeAnchor = clock.UtcNow;
            }

            var itemId = _table.Roll(_rng, _lastSpitId);
            _lastSpitId = itemId;
            return new SpitResult.Ok(new PieceId(itemId));
        }

        public void ApplyRecharge(IClock clock)
        {
            if (_charges >= _definition.MaxCharges || _rechargeAnchor is null)
            {
                if (_charges >= _definition.MaxCharges)
                {
                    _rechargeAnchor = null;
                }

                return;
            }

            var now = clock.UtcNow;
            if (now < _rechargeAnchor.Value)
            {
                _rechargeAnchor = now;
                return;
            }

            var interval = TimeSpan.FromSeconds(_definition.RechargeSeconds);
            if (interval <= TimeSpan.Zero)
            {
                _charges = _definition.MaxCharges;
                _rechargeAnchor = null;
                return;
            }

            var elapsed = now - _rechargeAnchor.Value;
            var gained = (int)(elapsed.Ticks / interval.Ticks);
            if (gained <= 0)
            {
                return;
            }

            var room = _definition.MaxCharges - _charges;
            var applied = Math.Min(room, gained);
            _charges += applied;
            _rechargeAnchor = _rechargeAnchor.Value + TimeSpan.FromTicks(interval.Ticks * applied);
            if (_charges >= _definition.MaxCharges)
            {
                _charges = _definition.MaxCharges;
                _rechargeAnchor = null;
            }
        }
    }
}
