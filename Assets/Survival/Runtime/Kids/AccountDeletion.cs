#nullable enable
using System;

namespace Survival.Domain.Kids
{
    public enum DeletionResult
    {
        /// <summary>Server account scrubbed and everything on the device wiped.</summary>
        Deleted,

        /// <summary>Never signed in: only device data existed, and it is wiped.</summary>
        DeletedLocalOnly,

        /// <summary>The server did not confirm (offline / error). Nothing was wiped, so the player can retry.</summary>
        ServerFailed,
    }

    /// <summary>
    /// In-app "delete my account and data". Order matters: the server first (it needs the sign-in token the device holds),
    /// then the device. If the server step fails nothing local is touched, so the token survives for a retry; the web
    /// deletion page (account.web_deletion_url) is the fallback.
    /// </summary>
    public sealed class AccountDeletion
    {
        private readonly Func<bool> _signedIn;
        private readonly Action<Action<bool>> _deleteOnServer;
        private readonly Action _wipeDevice;

        public AccountDeletion(Func<bool> signedIn, Action<Action<bool>> deleteOnServer, Action wipeDevice)
        {
            _signedIn = signedIn ?? throw new ArgumentNullException(nameof(signedIn));
            _deleteOnServer = deleteOnServer ?? throw new ArgumentNullException(nameof(deleteOnServer));
            _wipeDevice = wipeDevice ?? throw new ArgumentNullException(nameof(wipeDevice));
        }

        public void Run(Action<DeletionResult> done)
        {
            if (!_signedIn())
            {
                _wipeDevice();
                done(DeletionResult.DeletedLocalOnly);
                return;
            }

            _deleteOnServer(ok =>
            {
                if (!ok)
                {
                    done(DeletionResult.ServerFailed);
                    return;
                }

                _wipeDevice();
                done(DeletionResult.Deleted);
            });
        }
    }
}
