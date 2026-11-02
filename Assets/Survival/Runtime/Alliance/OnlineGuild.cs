#nullable enable
using System.Collections.Generic;
using Survival.Domain.Guild;

namespace Survival.Domain.Alliance
{
    /// <summary>
    /// Guild Help backed by the real alliance when the player is in one; otherwise the on-device <see cref="SimulatedGuild"/>
    /// so the game still works offline. Requests come from the last server answer (the view refreshes it every few seconds).
    /// </summary>
    public sealed class OnlineGuild : IGuildBackend, IGuildAskSink
    {
        private readonly AllianceService _svc;
        private readonly IGuildBackend _offline;

        public OnlineGuild(AllianceService svc, IGuildBackend? offline = null)
        {
            _svc = svc;
            _offline = offline ?? new SimulatedGuild();
        }

        public bool Live => _svc.InAlliance;

        public IReadOnlyList<GuildRequest> OpenRequests(double now)
        {
            if (!Live)
            {
                return _offline.OpenRequests(now);
            }

            var list = new List<GuildRequest>();
            foreach (var r in _svc.Requests)
            {
                if (r.PlayerId != _svc.PlayerId && r.Helps < r.MaxHelps)
                {
                    list.Add(new GuildRequest("net." + r.Id, r.Name, TaskOf(r.TaskKey), r.Helps, r.MaxHelps));
                }
            }

            return list;
        }

        public bool SendHelp(string requestId, double now)
        {
            if (!Live)
            {
                return _offline.SendHelp(requestId, now);
            }

            if (!requestId.StartsWith("net.") || !long.TryParse(requestId.Substring(4), out var id))
            {
                return false;
            }

            foreach (var r in _svc.Requests)
            {
                if (r.Id == id && !r.HelpedByMe && r.Helps < r.MaxHelps)
                {
                    _svc.GiveHelp(new[] { id });
                    return true;
                }
            }

            return false;
        }

        public int HelpsReceived(string timerId, double askedAt, double now)
        {
            if (!Live)
            {
                return _offline.HelpsReceived(timerId, askedAt, now);
            }

            foreach (var m in _svc.Mine)
            {
                if (m.TaskKey == timerId)
                {
                    return System.Math.Min(GuildHelp.MaxIncoming, m.Helps);
                }
            }

            return 0;
        }

        public void Asked(string timerId, double helpSeconds)
        {
            if (Live)
            {
                _svc.RequestHelp(timerId, (int)System.Math.Round(helpSeconds));
            }
        }

        /// <summary>Server task keys are timer ids ("farm.2"); the UI string table wants the building kind ("farm").</summary>
        public static string TaskOf(string taskKey)
        {
            var dot = taskKey.IndexOf('.');
            return dot > 0 ? taskKey.Substring(0, dot) : taskKey;
        }
    }
}
