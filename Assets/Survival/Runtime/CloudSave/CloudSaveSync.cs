#nullable enable
using System;
using Survival.Domain.Base;
using Survival.Domain.Iap;
using Survival.Domain.Campaign;
using Survival.Domain.Ftue;
using Survival.Domain.Quest;
using Survival.Domain.World;

namespace Survival.Domain.CloudSave
{
    /// <summary>Transport for GET/PUT /v1/save. Unity wires AllianceService; tests use a fake.</summary>
    public interface ICloudSaveTransport
    {
        void Get(Action<CloudSaveServerState?, string?> done);
        void Put(long baseVersion, string blobJson, bool consentGranted, Action<CloudSaveServerState?, string?> done);
    }

    public sealed class CloudSaveServerState
    {
        public long Version { get; set; }
        public string? BlobJson { get; set; }
        public bool Merged { get; set; }
        public string? ConsentAt { get; set; }
        public string AgeBand { get; set; } = AgeBands.Unknown;
    }

    /// <summary>
    /// Local-first cloud save. Always writes local prefs/files; uploads/downloads only when the
    /// allowsCloudSave gate is true (Unity: KidSafety.AllowsCloudSave from #205). Sync after purchase is an explicit call.
    /// </summary>
    public sealed class CloudSaveSync
    {
        public const double PeriodicSeconds = 120;

        private readonly ICloudSaveTransport _transport;
        private readonly Func<bool> _allowsCloudSave;
        private readonly Action<string, string> _localSet;
        private readonly Func<string, string?> _localGet;

        /// <param name="allowsCloudSave">
        /// Gate from kids-safety (#205): typically <c>() => KidSafety.AllowsCloudSave</c>, which uses
        /// <c>ConsentGate</c> + <c>IParentalConsentProvider</c>. Default true (domain/tests without kids UI).
        /// </param>
        public CloudSaveSync(
            ICloudSaveTransport transport,
            Func<bool>? allowsCloudSave = null,
            Action<string, string>? localSet = null,
            Func<string, string?>? localGet = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _allowsCloudSave = allowsCloudSave ?? (() => true);
            _localSet = localSet ?? ((_, _) => { });
            _localGet = localGet ?? (_ => null);
        }

        public long ServerVersion { get; private set; }
        public string? ConsentAt { get; private set; }
        public string? LastError { get; private set; }
        public bool CloudEnabled { get; private set; } = true;

        public CloudSaveDocument Capture(
            CampaignSession? campaign,
            BaseEconomy? villageEconomy,
            VillageBuild? villageBuild,
            string? questChainId,
            QuestSystem? quests,
            FtueSystem? ftue,
            WorldMap? world,
            double nowSeconds)
        {
            var doc = new CloudSaveDocument { SavedAt = nowSeconds };
            if (campaign != null)
            {
                doc.CampaignJson = CampaignSave.Write(campaign);
            }

            if (villageEconomy != null && villageBuild != null)
            {
                doc.VillageJson = VillageSave.Capture(villageEconomy, villageBuild);
            }

            if (quests != null && !string.IsNullOrEmpty(questChainId))
            {
                QuestProgressSave.FillDocument(doc, questChainId!, quests);
            }

            if (ftue != null)
            {
                doc.FtueStepId = ftue.CurrentStepId;
            }

            if (world != null)
            {
                WorldGatherSave.FillDocument(doc, world, nowSeconds);
            }

            return doc;
        }

        public void PersistLocal(
            CloudSaveDocument doc,
            string? questChainId,
            QuestSystem? quests,
            FtueSystem? ftue,
            WorldMap? world,
            double nowSeconds)
        {
            if (!string.IsNullOrEmpty(doc.VillageJson))
            {
                _localSet(VillageSave.PrefsKey, doc.VillageJson!);
            }

            if (quests != null && !string.IsNullOrEmpty(questChainId))
            {
                _localSet(QuestProgressSave.PrefsKey, QuestProgressSave.Capture(questChainId!, quests));
            }

            if (ftue != null)
            {
                _localSet(FtueProgressSave.PrefsKey, FtueProgressSave.Capture(ftue));
            }

            if (world != null)
            {
                _localSet(WorldGatherSave.PrefsKey, WorldGatherSave.Capture(world, nowSeconds));
            }

            _localSet("survival.cloud.blob.v1", doc.Write());
            _localSet("survival.cloud.server_version", ServerVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public void ApplyLocal(
            string? questChainId,
            QuestSystem? quests,
            FtueSystem? ftue,
            WorldMap? world,
            BaseEconomy? villageEconomy,
            VillageBuild? villageBuild)
        {
            if (villageEconomy != null && villageBuild != null)
            {
                VillageSave.Apply(_localGet(VillageSave.PrefsKey), villageEconomy, villageBuild);
            }

            if (quests != null && !string.IsNullOrEmpty(questChainId))
            {
                QuestProgressSave.Apply(_localGet(QuestProgressSave.PrefsKey), questChainId!, quests);
            }

            if (ftue != null)
            {
                FtueProgressSave.Apply(_localGet(FtueProgressSave.PrefsKey), ftue);
            }

            if (world != null)
            {
                WorldGatherSave.Apply(_localGet(WorldGatherSave.PrefsKey), world);
            }

            if (long.TryParse(_localGet("survival.cloud.server_version"), out var v))
            {
                ServerVersion = v;
            }
        }

        public void ApplyRemote(
            CloudSaveDocument doc,
            CampaignSession? campaign,
            BaseEconomy? villageEconomy,
            VillageBuild? villageBuild,
            string? questChainId,
            QuestSystem? quests,
            FtueSystem? ftue,
            WorldMap? world)
        {
            if (campaign != null && !string.IsNullOrEmpty(doc.CampaignJson))
            {
                campaign.Load(doc.CampaignJson!);
            }

            if (villageEconomy != null && villageBuild != null && !string.IsNullOrEmpty(doc.VillageJson))
            {
                VillageSave.Apply(doc.VillageJson, villageEconomy, villageBuild);
            }

            if (quests != null && !string.IsNullOrEmpty(questChainId))
            {
                QuestProgressSave.ApplyDocument(doc, questChainId!, quests);
            }

            if (ftue != null && !string.IsNullOrEmpty(doc.FtueStepId))
            {
                ftue.AdvanceTo(doc.FtueStepId!);
            }

            if (world != null)
            {
                WorldGatherSave.ApplyDocument(doc, world);
            }
        }

        /// <summary>Pull server save and merge into local. No-ops offline.</summary>
        public void PullAndMerge(
            CampaignSession? campaign,
            BaseEconomy? villageEconomy,
            VillageBuild? villageBuild,
            string? questChainId,
            QuestSystem? quests,
            FtueSystem? ftue,
            WorldMap? world,
            double nowSeconds,
            Action<bool>? done = null)
        {
            if (!CloudEnabled || !_allowsCloudSave())
            {
                if (!_allowsCloudSave())
                {
                    LastError = "consent_required";
                }

                done?.Invoke(false);
                return;
            }

            _transport.Get((state, err) =>
            {
                if (state == null)
                {
                    LastError = err ?? "offline";
                    done?.Invoke(false);
                    return;
                }

                LastError = null;
                ServerVersion = state.Version;
                ConsentAt = state.ConsentAt;
                var remote = string.IsNullOrEmpty(state.BlobJson) ? null : CloudSaveDocument.Parse(state.BlobJson!);
                var local = Capture(campaign, villageEconomy, villageBuild, questChainId, quests, ftue, world, nowSeconds);
                var merged = CloudSaveMerge.Merge(remote, local);
                ApplyRemote(merged, campaign, villageEconomy, villageBuild, questChainId, quests, ftue, world);
                PersistLocal(merged, questChainId, quests, ftue, world, nowSeconds);
                done?.Invoke(true);
            });
        }

        /// <summary>Push local save. Under-13 requires consent (pluggable). Always persists locally first.</summary>
        public void Push(
            CampaignSession? campaign,
            BaseEconomy? villageEconomy,
            VillageBuild? villageBuild,
            string? questChainId,
            QuestSystem? quests,
            FtueSystem? ftue,
            WorldMap? world,
            double nowSeconds,
            Action<bool>? done = null)
        {
            var doc = Capture(campaign, villageEconomy, villageBuild, questChainId, quests, ftue, world, nowSeconds);
            PersistLocal(doc, questChainId, quests, ftue, world, nowSeconds);
            if (!CloudEnabled)
            {
                done?.Invoke(false);
                return;
            }

            if (!_allowsCloudSave())
            {
                LastError = "consent_required";
                done?.Invoke(false);
                return;
            }

            _transport.Put(ServerVersion, doc.Write(), consentGranted: true, (state, err) =>
            {
                if (state == null)
                {
                    LastError = err ?? "offline";
                    done?.Invoke(false);
                    return;
                }

                LastError = null;
                ServerVersion = state.Version;
                ConsentAt = state.ConsentAt;
                if (!string.IsNullOrEmpty(state.BlobJson) && state.Merged)
                {
                    var merged = CloudSaveDocument.Parse(state.BlobJson!);
                    if (merged != null)
                    {
                        ApplyRemote(merged, campaign, villageEconomy, villageBuild, questChainId, quests, ftue, world);
                        PersistLocal(merged, questChainId, quests, ftue, world, nowSeconds);
                    }
                }

                done?.Invoke(true);
            });
        }
    }
}
