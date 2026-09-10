using System;
using Grove.Domain.Time;

namespace Grove.Domain.Energy
{
    /// <summary>
    /// Capped energy with remainder-accurate regen (1 per 2 minutes by default).
    /// Offline / background time is applied on the next read or spend using <see cref="IClock"/>.
    /// </summary>
    public sealed class EnergyWallet : IEnergySpender
    {
        private readonly EnergyConfig _config;
        private readonly IClock _clock;
        private readonly IEnergyListener? _listener;
        private int _current;
        private DateTimeOffset _lastUpdated;
        private bool _ftueTopUpGranted;
        private int _lastNotified = int.MinValue;

        public EnergyWallet(
            EnergyConfig config,
            IClock clock,
            IEnergyListener? listener = null,
            int? startingEnergy = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _listener = listener;
            if (startingEnergy is int start && start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingEnergy), start, "Starting energy cannot be negative.");
            }

            _current = Math.Min(config.Cap, startingEnergy ?? config.Cap);
            _lastUpdated = clock.UtcNow;
            NotifyChanged();
        }

        public EnergyConfig Config => _config;

        public int Cap => _config.Cap;

        public bool FtueTopUpGranted => _ftueTopUpGranted;

        public int Current
        {
            get
            {
                ApplyRegen(_clock.UtcNow);
                return _current;
            }
        }

        public bool IsEmpty => Current <= 0;

        public DateTimeOffset LastUpdated
        {
            get
            {
                ApplyRegen(_clock.UtcNow);
                return _lastUpdated;
            }
        }

        /// <summary>Apply pending regen (call from a Unity Update if the HUD should tick while idle).</summary>
        public void Tick() => ApplyRegen(_clock.UtcNow);

        public bool TrySpendProduce() => TrySpend(_config.ProduceCost, EnergySpendReason.Produce);

        public bool TrySpendDig() => TrySpend(_config.DigCost, EnergySpendReason.Dig);

        public bool TrySpend(int amount, EnergySpendReason reason)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Spend amount cannot be negative.");
            }

            ApplyRegen(_clock.UtcNow);
            if (amount == 0)
            {
                return true;
            }

            if (_current < amount)
            {
                _listener?.OnEnergyEmpty();
                return false;
            }

            _current -= amount;
            NotifyChanged();
            if (_current == 0)
            {
                _listener?.OnEnergyEmpty();
            }

            return true;
        }

        /// <summary>
        /// FTUE top-up once. When <see cref="EnergyConfig.FtueTopUpToCap"/> is true, fills to cap.
        /// Subsequent calls are no-ops and return false.
        /// </summary>
        public bool TryGrantFtueTopUp()
        {
            ApplyRegen(_clock.UtcNow);
            if (_ftueTopUpGranted)
            {
                return false;
            }

            _ftueTopUpGranted = true;
            if (_config.FtueTopUpToCap)
            {
                _current = _config.Cap;
                _lastUpdated = _clock.UtcNow;
            }

            NotifyChanged();
            return true;
        }

        private void ApplyRegen(DateTimeOffset now)
        {
            if (now < _lastUpdated)
            {
                _lastUpdated = now;
                return;
            }

            if (_current >= _config.Cap)
            {
                _lastUpdated = now;
                return;
            }

            var interval = TimeSpan.FromSeconds(_config.RegenSecondsPerPoint);
            if (interval <= TimeSpan.Zero)
            {
                return;
            }

            var elapsed = now - _lastUpdated;
            var gained = (int)(elapsed.Ticks / interval.Ticks);
            if (gained <= 0)
            {
                return;
            }

            var room = _config.Cap - _current;
            var applied = Math.Min(room, gained);
            _current += applied;
            _lastUpdated += TimeSpan.FromTicks(interval.Ticks * applied);
            if (_current >= _config.Cap)
            {
                _current = _config.Cap;
                _lastUpdated = now;
            }

            NotifyChanged();
        }

        private void NotifyChanged()
        {
            if (_lastNotified == _current)
            {
                return;
            }

            _lastNotified = _current;
            _listener?.OnEnergyChanged(_current, _config.Cap);
        }
    }
}
