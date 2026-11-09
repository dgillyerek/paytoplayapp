#nullable enable
using System;
using Survival.Domain.CloudSave;
using Survival.Domain.Iap;
using Survival.Domain.Ids;
using Survival.Domain.Session;
using Survival.Unity.Kids;
using UnityEngine;

namespace Survival.Unity.CloudSave
{
    /// <summary>
    /// Unity wiring for local-first cloud saves: PlayerPrefs locals, AllianceService GET/PUT /v1/save,
    /// sync on launch / resume / periodic / after purchase.
    /// Cloud upload/download is gated by <see cref="KidSafety.AllowsCloudSave"/> (#205 ConsentGate / IParentalConsentProvider).
    /// </summary>
    public static class SurvivalCloudSave
    {
        private static CloudSaveSync? _sync;
        private static float _periodic;

        public static CloudSaveSync? Sync => _sync;

        public static void Bind(SurvivalSession session, Survival.Domain.Alliance.AllianceService? alliance)
        {
            if (session == null)
            {
                return;
            }

            var transport = new AllianceSaveTransport(alliance);
            _sync = new CloudSaveSync(
                transport,
                allowsCloudSave: () => KidSafety.AllowsCloudSave,
                (k, v) =>
                {
                    PlayerPrefs.SetString(k, v);
                    PlayerPrefs.Save();
                },
                k => PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null);

            ApplyLocal(session);
            _periodic = 0f;
            _sync.PullAndMerge(
                null,
                session.Hook?.Base,
                session.Hook?.Build,
                session.Hook?.Catalog.StarterChain.Id ?? SurvIds.QuestChainStarter,
                session.Hook?.Quests,
                session.Ftue,
                session.World,
                Now(),
                _ => { });
        }

        public static void ApplyLocal(SurvivalSession session)
        {
            if (_sync == null || session == null)
            {
                var chainId = session?.Hook?.Catalog.StarterChain.Id ?? SurvIds.QuestChainStarter;
                if (session?.Hook != null)
                {
                    QuestProgressSave.Apply(PlayerPrefs.GetString(QuestProgressSave.PrefsKey, ""), chainId, session.Hook.Quests);
                    Survival.Domain.Base.VillageSave.Apply(
                        PlayerPrefs.GetString(Survival.Domain.Base.VillageSave.PrefsKey, ""),
                        session.Hook.Base,
                        session.Hook.Build);
                }

                if (session?.Ftue != null)
                {
                    FtueProgressSave.Apply(PlayerPrefs.GetString(FtueProgressSave.PrefsKey, ""), session.Ftue);
                }

                if (session?.World != null)
                {
                    WorldGatherSave.Apply(PlayerPrefs.GetString(WorldGatherSave.PrefsKey, ""), session.World);
                }

                return;
            }

            _sync.ApplyLocal(
                session.Hook?.Catalog.StarterChain.Id ?? SurvIds.QuestChainStarter,
                session.Hook?.Quests,
                session.Ftue,
                session.World,
                session.Hook?.Base,
                session.Hook?.Build);
        }

        public static void Tick(SurvivalSession? session, float dt)
        {
            if (_sync == null || session == null)
            {
                return;
            }

            _periodic += dt;
            if (_periodic < CloudSaveSync.PeriodicSeconds)
            {
                return;
            }

            _periodic = 0f;
            Push(session);
        }

        public static void OnResume(SurvivalSession? session)
        {
            if (_sync == null || session == null)
            {
                return;
            }

            _sync.PullAndMerge(
                null,
                session.Hook?.Base,
                session.Hook?.Build,
                session.Hook?.Catalog.StarterChain.Id ?? SurvIds.QuestChainStarter,
                session.Hook?.Quests,
                session.Ftue,
                session.World,
                Now(),
                _ => { });
        }

        public static void OnPause(SurvivalSession? session) => Push(session);

        /// <summary>Call after a successful IAP grant so currencies sync before a refund clawback can race.</summary>
        public static void AfterPurchase(SurvivalSession? session) => Push(session);

        public static void Push(SurvivalSession? session)
        {
            if (_sync == null || session == null)
            {
                return;
            }

            _sync.Push(
                null,
                session.Hook?.Base,
                session.Hook?.Build,
                session.Hook?.Catalog.StarterChain.Id ?? SurvIds.QuestChainStarter,
                session.Hook?.Quests,
                session.Ftue,
                session.World,
                Now(),
                _ => { });
        }

        private static double Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private sealed class AllianceSaveTransport : ICloudSaveTransport
        {
            private readonly Survival.Domain.Alliance.AllianceService? _alliance;

            public AllianceSaveTransport(Survival.Domain.Alliance.AllianceService? alliance) => _alliance = alliance;

            public void Get(Action<CloudSaveServerState?, string?> done)
            {
                if (_alliance == null || string.IsNullOrEmpty(_alliance.SessionToken))
                {
                    done(null, "not_signed_in");
                    return;
                }

                _alliance.GetSave((version, blob, consent, band, err) =>
                {
                    if (err != null)
                    {
                        done(null, err);
                        return;
                    }

                    done(new CloudSaveServerState
                    {
                        Version = version,
                        BlobJson = blob,
                        ConsentAt = consent,
                        AgeBand = band,
                    }, null);
                });
            }

            public void Put(long baseVersion, string blobJson, bool consentGranted, Action<CloudSaveServerState?, string?> done)
            {
                if (_alliance == null || string.IsNullOrEmpty(_alliance.SessionToken))
                {
                    done(null, "not_signed_in");
                    return;
                }

                _alliance.PutSave(baseVersion, blobJson, consentGranted, (version, blob, merged, consent, err) =>
                {
                    if (err != null)
                    {
                        done(null, err);
                        return;
                    }

                    done(new CloudSaveServerState
                    {
                        Version = version,
                        BlobJson = blob,
                        Merged = merged,
                        ConsentAt = consent,
                    }, null);
                });
            }
        }
    }
}
