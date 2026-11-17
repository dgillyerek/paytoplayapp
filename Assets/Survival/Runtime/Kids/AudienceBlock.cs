#nullable enable
using System;

namespace Survival.Domain.Kids
{
    public enum AudienceBlockResult
    {
        /// <summary>Not blocked: nothing to do.</summary>
        NotBlocked,

        /// <summary>Blocked; server account (if any) and device data deleted, only the block record kept.</summary>
        Deleted,

        /// <summary>Blocked because the stored answer was edited: play stays blocked, nothing is deleted (age not known to be under 13).</summary>
        BlockedOnly,

        /// <summary>Blocked, but the server did not confirm the deletion. Nothing local was wiped; retried on next launch.</summary>
        ServerFailed,
    }

    /// <summary>
    /// 13+ audience: what happens when a player is identified as under 13 (first-launch answer, or a later age correction).
    /// Personal information collected by mistake is deleted, server first (via the in-app account deletion), then the device;
    /// afterwards only the under-13 age record is written back so the block holds on this install.
    /// </summary>
    public static class AudienceBlock
    {
        public static void Enforce(ChildSafetyPolicy policy, AccountDeletion deletion, Action<AgeBandDef> keepBlock, Action<AudienceBlockResult> done)
        {
            if (!policy.IsBlocked)
            {
                done(AudienceBlockResult.NotBlocked);
                return;
            }

            if (policy.State.Status != AgeGateStatus.Answered)
            {
                done(AudienceBlockResult.BlockedOnly);
                return;
            }

            var band = policy.Config.Band(policy.BandId) ?? FirstChildBand(policy.Config);
            deletion.Run(r =>
            {
                if (r == DeletionResult.ServerFailed)
                {
                    done(AudienceBlockResult.ServerFailed);
                    return;
                }

                keepBlock(band);
                done(AudienceBlockResult.Deleted);
            });
        }

        private static AgeBandDef FirstChildBand(ChildSafetyConfig c)
        {
            foreach (var b in c.AgeBands)
            {
                if (b.Child)
                {
                    return b;
                }
            }

            return new AgeBandDef("under13", 12, true);
        }
    }
}
