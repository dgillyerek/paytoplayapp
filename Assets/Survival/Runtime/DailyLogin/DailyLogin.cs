#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Campaign;
using Survival.Domain.Clock;
using Survival.Domain.Economy;
using Survival.Domain.Ids;
using Survival.Domain.Serialization;

namespace Survival.Domain.DailyLogin
{
    /// <summary>How one calendar box looks.</summary>
    public enum LoginDayState
    {
        /// <summary>Already taken this week.</summary>
        Claimed,

        /// <summary>Today's box: tap CLAIM.</summary>
        Ready,

        /// <summary>A later day.</summary>
        Upcoming
    }

    /// <summary>
    /// Daily login chest: a 7-day calendar with rising rewards and a big chest on day 7. One claim per local calendar
    /// day (midnight in the player's time zone). Missing days never resets the week: the next claim simply takes the
    /// next box, so a player who skips Tuesday still gets day 2 on Wednesday. After day 7 the next day starts a new week
    /// at day 1. Rewards are plain wallet resources; the free-pace runner model never reads this.
    /// Saved in its own file (daily_login.json).
    /// </summary>
    public sealed class DailyLogin
    {
        public const string FileName = "daily_login.json";
        public const int Days = 7;
        private const double DaySeconds = 86400.0;

        /// <summary>Day 1..7 rewards. Day 7 is the big chest.</summary>
        public static readonly IReadOnlyList<IReadOnlyDictionary<string, int>> DefaultRewards = new IReadOnlyDictionary<string, int>[]
        {
            new Dictionary<string, int> { [SurvIds.ResSoft] = 5000 },
            new Dictionary<string, int> { [SurvIds.ResSpeedupM] = 15, [SurvIds.ResFood] = 3000 },
            new Dictionary<string, int> { [SurvIds.ResHard] = 20 },
            new Dictionary<string, int> { [SurvIds.ResSoft] = 10000, [SurvIds.ResWood] = 5000 },
            new Dictionary<string, int> { [SurvIds.ResHeroXp] = 1, [SurvIds.ResSpeedupM] = 30 },
            new Dictionary<string, int> { [SurvIds.ResHard] = 40 },
            new Dictionary<string, int> { [SurvIds.ResHard] = 100, [SurvIds.ResSoft] = 30000, [SurvIds.ResSpeedupM] = 60, [SurvIds.ResHeroXp] = 2 }
        };

        public DailyLogin(IGameClock clock, double utcOffsetSeconds = 0, IReadOnlyList<IReadOnlyDictionary<string, int>>? rewards = null)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            UtcOffsetSeconds = utcOffsetSeconds;
            Rewards = rewards ?? DefaultRewards;
            if (Rewards.Count != Days)
            {
                throw new ArgumentException("The login calendar needs exactly 7 days of rewards.");
            }
        }

        public IGameClock Clock { get; }

        /// <summary>Local time offset (seconds east of UTC) so the day flips at the player's midnight.</summary>
        public double UtcOffsetSeconds { get; set; }

        public IReadOnlyList<IReadOnlyDictionary<string, int>> Rewards { get; }

        /// <summary>Boxes claimed in the current week (0..7).</summary>
        public int ClaimedThisWeek { get; private set; }

        /// <summary>Local day number of the last claim (-1 = never).</summary>
        public long LastClaimDay { get; private set; } = -1;

        /// <summary>Completed weeks (for stats / later bonuses).</summary>
        public int Weeks { get; private set; }

        public event Action? Changed;

        public long Today => (long)Math.Floor((Clock.Now + UtcOffsetSeconds) / DaySeconds);

        public bool CanClaim => Today != LastClaimDay;

        /// <summary>The week as the calendar should show it: a finished week stays all-ticked until the next day opens a new one.</summary>
        public int ShownClaimed => ClaimedThisWeek >= Days && CanClaim ? 0 : ClaimedThisWeek;

        /// <summary>Day (1..7) the next claim pays.</summary>
        public int NextDay => (ClaimedThisWeek >= Days ? 0 : ClaimedThisWeek) + 1;

        /// <summary>Seconds until the next box opens (0 when one is ready now).</summary>
        public double SecondsUntilNext => CanClaim ? 0 : Math.Max(0, ((Today + 1) * DaySeconds) - (Clock.Now + UtcOffsetSeconds));

        public LoginDayState State(int dayIndex)
        {
            var shown = ShownClaimed;
            if (dayIndex < shown)
            {
                return LoginDayState.Claimed;
            }

            return dayIndex == shown && CanClaim ? LoginDayState.Ready : LoginDayState.Upcoming;
        }

        /// <summary>Takes today's box into the wallet. Null when today's box is already taken.</summary>
        public IReadOnlyDictionary<string, int>? Claim(Wallet wallet)
        {
            if (!CanClaim)
            {
                return null;
            }

            if (ClaimedThisWeek >= Days)
            {
                ClaimedThisWeek = 0;
            }

            var reward = Rewards[ClaimedThisWeek];
            wallet.Grant(reward);
            ClaimedThisWeek++;
            if (ClaimedThisWeek == Days)
            {
                Weeks++;
            }

            LastClaimDay = Today;
            Changed?.Invoke();
            return reward;
        }

        public string Save()
        {
            var w = new JsonWriter();
            w.Begin();
            w.Field("version", 1);
            w.Field("claimed", ClaimedThisWeek);
            w.Field("last_day", (double)LastClaimDay);
            w.Field("weeks", Weeks);
            w.End();
            return w.ToString();
        }

        public bool Load(string json)
        {
            try
            {
                if (JsonRead.Parse(json) is not JsonNode.Obj o)
                {
                    return false;
                }

                ClaimedThisWeek = Math.Max(0, Math.Min(Days, CampaignCatalog.Int(o, "claimed", 0)));
                LastClaimDay = (long)CampaignCatalog.Num(o, "last_day", -1);
                Weeks = Math.Max(0, CampaignCatalog.Int(o, "weeks", 0));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
