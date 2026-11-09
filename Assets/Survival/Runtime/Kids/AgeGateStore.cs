#nullable enable
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Survival.Domain.Kids
{
    /// <summary>Key/value device storage (PlayerPrefs in Unity, a dictionary in tests).</summary>
    public interface IKidSafetyStorage
    {
        string? Get(string key);

        void Set(string key, string value);

        void Delete(string key);

        void Save();
    }

    public enum AgeGateStatus
    {
        /// <summary>Never asked on this install: show the gate.</summary>
        Fresh,

        /// <summary>The gate was shown but closed before an answer (back / force-close): show it again.</summary>
        Interrupted,

        /// <summary>Answered; the band is final on this install.</summary>
        Answered,

        /// <summary>The stored answer was edited or removed: child mode, and the gate is not shown again.</summary>
        Tampered,
    }

    public sealed record AgeGateState(AgeGateStatus Status, string? BandId)
    {
        /// <summary>The gate must be shown before play.</summary>
        public bool NeedsGate => Status is AgeGateStatus.Fresh or AgeGateStatus.Interrupted;
    }

    /// <summary>
    /// Stores only the age band, signed with a per-install key so a hand-edited or deleted answer is caught. Hard to
    /// bypass on purpose: once answered the band never changes on this install (deleting the account is the only reset);
    /// a missing or edited answer locks child mode instead of asking again. Clearing all app data or reinstalling does
    /// start fresh, as on any Android app.
    /// </summary>
    public sealed class AgeGateStore
    {
        // Short, non-obvious keys: not a secret, just not "age=13plus" for anyone browsing prefs.
        internal const string SaltKey = "ks.s";
        internal const string RecordKey = "ks.a";
        internal const string PendingKey = "ks.p";
        private const string Pepper = "ptp.kids.v1";
        private const string Version = "v1";

        private readonly IKidSafetyStorage _storage;
        private readonly Func<DateTime> _now;

        public AgeGateStore(IKidSafetyStorage storage, Func<DateTime>? now = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _now = now ?? (() => DateTime.UtcNow);
        }

        public AgeGateState Load()
        {
            var salt = _storage.Get(SaltKey);
            var record = _storage.Get(RecordKey);
            if (!string.IsNullOrEmpty(record))
            {
                return salt != null && TryRead(salt, record!, out var band) ? new AgeGateState(AgeGateStatus.Answered, band) : new AgeGateState(AgeGateStatus.Tampered, null);
            }

            var pending = _storage.Get(PendingKey);
            if (!string.IsNullOrEmpty(pending))
            {
                return salt != null && pending == Sign(salt, "pending")
                    ? new AgeGateState(AgeGateStatus.Interrupted, null)
                    : new AgeGateState(AgeGateStatus.Tampered, null);
            }

            // A salt with neither an answer nor a pending mark means the answer was removed by hand.
            return string.IsNullOrEmpty(salt) ? new AgeGateState(AgeGateStatus.Fresh, null) : new AgeGateState(AgeGateStatus.Tampered, null);
        }

        /// <summary>Call when the gate is shown, so closing the app mid-question re-asks rather than counting as tampering.</summary>
        public void MarkShown()
        {
            var salt = EnsureSalt();
            _storage.Set(PendingKey, Sign(salt, "pending"));
            _storage.Save();
        }

        /// <summary>Saves the answer once. False when an answer already exists (the band cannot be changed by re-asking).</summary>
        public bool Save(AgeBandDef band)
        {
            if (Load().Status is AgeGateStatus.Answered or AgeGateStatus.Tampered)
            {
                return false;
            }

            var salt = EnsureSalt();
            var payload = Version + ";" + band.Id + ";" + (band.Child ? "1" : "0") + ";" + ((long)(_now() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString(CultureInfo.InvariantCulture);
            _storage.Set(RecordKey, payload + ";" + Sign(salt, payload));
            _storage.Delete(PendingKey);
            _storage.Save();
            return true;
        }

        /// <summary>Removes every age-gate key. Only account deletion calls this.</summary>
        public void Clear()
        {
            _storage.Delete(RecordKey);
            _storage.Delete(PendingKey);
            _storage.Delete(SaltKey);
            _storage.Save();
        }

        private string EnsureSalt()
        {
            var salt = _storage.Get(SaltKey);
            if (string.IsNullOrEmpty(salt))
            {
                var bytes = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }

                salt = Convert.ToBase64String(bytes);
                _storage.Set(SaltKey, salt);
            }

            return salt!;
        }

        private static bool TryRead(string salt, string record, out string? band)
        {
            band = null;
            var cut = record.LastIndexOf(';');
            if (cut <= 0)
            {
                return false;
            }

            var payload = record.Substring(0, cut);
            if (!FixedEquals(record.Substring(cut + 1), Sign(salt, payload)))
            {
                return false;
            }

            var parts = payload.Split(';');
            if (parts.Length != 4 || parts[0] != Version || parts[1].Length == 0)
            {
                return false;
            }

            band = parts[1];
            return true;
        }

        private static string Sign(string salt, string payload)
        {
            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(Pepper + salt));
            var mac = h.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var sb = new StringBuilder(32);
            for (var i = 0; i < 16; i++)
            {
                sb.Append(mac[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private static bool FixedEquals(string a, string b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
