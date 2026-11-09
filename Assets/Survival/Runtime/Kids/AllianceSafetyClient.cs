#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Survival.Domain.Alliance;

namespace Survival.Domain.Kids
{
    /// <summary>
    /// Report / block and the server-side chat mode, on the alliance sign-in (Backend/alliance/src/safety.mjs). The server
    /// enforces the mode: in "presets" it accepts only [[p:id]] bodies and returns only preset messages; in "off" it
    /// refuses chat; restricted players get a generated display name and cannot found an alliance with a typed name.
    /// </summary>
    public sealed class AllianceSafetyClient
    {
        private readonly AllianceService _net;

        public AllianceSafetyClient(AllianceService net) => _net = net ?? throw new ArgumentNullException(nameof(net));

        public HashSet<long> Blocked { get; } = new HashSet<long>();

        public string? LastError => _net.LastError;

        /// <summary>Tell the server this account's chat mode (and use a generated name when restricted). Call after sign-in.</summary>
        public void ApplyPolicy(ChildSafetyPolicy p, string safeName, Action<bool>? done = null) =>
            _net.Request("POST", "/v1/account/child_policy",
                "{\"chat\":" + AllianceService.Q(ChatSafety.ChatModeId(p.Chat)) + ",\"restricted\":" + (p.IsChild ? "true" : "false") + ",\"safeName\":" + AllianceService.Q(safeName) + "}",
                _ => RefreshBlocks(done), done);

        public void Report(long messageId, string reason, Action<bool>? done = null) =>
            _net.Request("POST", "/v1/chat/report",
                "{\"messageId\":" + messageId.ToString(CultureInfo.InvariantCulture) + ",\"reason\":" + AllianceService.Q(reason) + "}",
                _ => done?.Invoke(true), done);

        public void Block(long playerId, Action<bool>? done = null)
        {
            Blocked.Add(playerId);
            _net.Request("POST", "/v1/chat/block", "{\"playerId\":" + playerId.ToString(CultureInfo.InvariantCulture) + "}", _ => done?.Invoke(true), done);
        }

        public void Unblock(long playerId, Action<bool>? done = null)
        {
            Blocked.Remove(playerId);
            _net.Request("POST", "/v1/chat/unblock", "{\"playerId\":" + playerId.ToString(CultureInfo.InvariantCulture) + "}", _ => done?.Invoke(true), done);
        }

        public void RefreshBlocks(Action<bool>? done = null) => _net.Request("GET", "/v1/chat/blocks", null, o =>
        {
            Blocked.Clear();
            foreach (var n in AllianceService.Arr(o, "blocked"))
            {
                Blocked.Add(AllianceService.Long(n, "player_id"));
            }

            done?.Invoke(true);
        }, done);

        /// <summary>Scrubs this account on the server (Backend/alliance/src/deletion.mjs). done(false) when not confirmed.</summary>
        public void DeleteAccount(Action<bool> done) =>
            _net.Request("POST", "/v1/account/delete", "{\"confirm\":\"delete\"}", o => done(AllianceService.Bool(o, "deleted")), done);

        public void SendPreset(string presetId, Action<bool>? done = null) => _net.SendChat(ChatSafety.EncodePreset(presetId), done);

        /// <summary>Report reasons offered in the UI (stored as-is by the server).</summary>
        public static readonly IReadOnlyList<string> ReportReasons = new[] { "mean", "personal_info", "spam", "other" };
    }
}
