#nullable enable
using Survival.Domain.Crusade;
using Survival.Domain.Guild;
using Survival.Domain.Taps;

namespace Survival.Domain.View
{
    /// <summary>
    /// "Is there something to do in here?" One predicate per side-rail feature. A rail icon only shows while its
    /// predicate is true: a reward to claim, a task not yet done, help to give or ask, a free spin, an active event,
    /// or an offer that can still be bought. Fact overloads (bools and counts) are what the tests pin down; the
    /// object overloads read them off the live domain objects.
    /// </summary>
    public static class RailActionable
    {
        /// <summary>Kingdom Path: the free progression track, so it is always on the rail (compact); the FREE badge shows only with a reward to claim.</summary>
        public static bool KingdomPath(int claimable) => true;

        /// <summary>Kingdom Path FREE badge: at least one reached reward not claimed yet.</summary>
        public static bool KingdomPathFree(int claimable) => claimable > 0;

        /// <summary>Active Timers (hourglass): at least one timer running or finished and waiting.</summary>
        public static bool Timers(int count) => count > 0;

        /// <summary>Daily chest: today's chest not claimed yet.</summary>
        public static bool Daily(bool canClaim) => canClaim;

        /// <summary>Trophy Hall: at least one trophy earned and not claimed.</summary>
        public static bool Trophy(int readyCount) => readyCount > 0;

        /// <summary>Crusade Banner: a tier reward to claim, or the premium banner is still for sale.</summary>
        public static bool Crusade(int readyCount, bool hasPremium) => readyCount > 0 || !hasPremium;

        public static bool Crusade(CrusadeBanner banner) => Crusade(banner.ReadyCount, banner.HasPremium);

        /// <summary>Guild Help: guildmates to help, or own timers to ask help on.</summary>
        public static bool Guild(int helpable, int askable) => helpable > 0 || askable > 0;

        public static bool Guild(GuildHelp guild) => Guild(guild.Helpable, guild.AskableCount);

        /// <summary>Oracle / Crystal Orb: glowing (reveal), a task in progress, or a finished task to claim. Hidden while resting.</summary>
        public static bool Orb(OrbState state) => state != OrbState.Resting;

        public static bool Orb(CrystalOrb orb) => Orb(orb.State);

        /// <summary>Bounty Board: a poster to claim, one being worked on, or one offered to accept.</summary>
        public static bool Bounty(int ready, int active, bool offered) => ready > 0 || active > 0 || offered;

        public static bool Bounty(BountyBoard board)
        {
            var offered = false;
            foreach (var b in board.Slots)
            {
                if (b.StateAt(board.Now) == BountyState.Offered)
                {
                    offered = true;
                    break;
                }
            }

            return Bounty(board.ReadyCount, board.ActiveCount, offered);
        }

        /// <summary>Wishing Well: a free wish is waiting (paid wishes alone do not keep the icon up).</summary>
        public static bool Well(bool freeAvailable) => freeAvailable;

        public static bool Well(WishingWell well) => Well(well.FreeAvailable);

        /// <summary>Wandering Merchant: in town with a free gift or anything still for sale.</summary>
        public static bool Merchant(bool here, bool freeWaiting, bool anyUnbought) => here && (freeWaiting || anyUnbought);

        public static bool Merchant(WanderingMerchant merchant)
        {
            var left = false;
            foreach (var o in merchant.Offers)
            {
                if (!o.Bought)
                {
                    left = true;
                    break;
                }
            }

            return Merchant(merchant.IsHere, merchant.HasFreeWaiting, left);
        }

        /// <summary>Events: an event is running (the pack has event text that has not ended).</summary>
        public static bool Events(string? eventBody) => !string.IsNullOrWhiteSpace(eventBody);

        /// <summary>Runner: a level left to play (no replays once every level is beaten).</summary>
        public static bool Runner(bool allBeaten) => !allBeaten;
    }
}
