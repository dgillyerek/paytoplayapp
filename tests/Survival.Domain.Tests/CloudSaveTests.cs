#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.CloudSave;
using Survival.Domain.Ids;
using Survival.Domain.Quest;
using Xunit;

namespace Survival.Domain.Tests
{
    public class CloudSaveTests
    {
        [Fact]
        public void Quest_progress_is_keyed_by_chain_id()
        {
            var doc = new CloudSaveDocument();
            doc.Quests[SurvIds.QuestChainStarter] = new CloudSaveDocument.QuestProgress { Index = 12, Progress = 3 };
            var json = doc.Write();
            var round = CloudSaveDocument.Parse(json);
            Assert.NotNull(round);
            Assert.True(round!.Quests.ContainsKey(SurvIds.QuestChainStarter));
            Assert.Equal(12, round.Quests[SurvIds.QuestChainStarter].Index);
            Assert.Equal(3, round.Quests[SurvIds.QuestChainStarter].Progress);
        }

        [Fact]
        public void Merge_quests_takes_max_index_and_gather_takes_min_available()
        {
            var a = new CloudSaveDocument();
            a.Quests[SurvIds.QuestChainStarter] = new CloudSaveDocument.QuestProgress { Index = 2, Progress = 5 };
            a.Gather["theme_a.node.gather_quarry"] = new CloudSaveDocument.GatherStock { Available = 8400, UpdatedAt = 1 };

            var b = new CloudSaveDocument();
            b.Quests[SurvIds.QuestChainStarter] = new CloudSaveDocument.QuestProgress { Index = 9, Progress = 0 };
            b.Gather["theme_a.node.gather_quarry"] = new CloudSaveDocument.GatherStock { Available = 5880, UpdatedAt = 2 };

            var m = CloudSaveMerge.Merge(a, b);
            Assert.Equal(9, m.Quests[SurvIds.QuestChainStarter].Index);
            Assert.Equal(0, m.Quests[SurvIds.QuestChainStarter].Progress);
            Assert.Equal(5880, m.Gather["theme_a.node.gather_quarry"].Available);
        }

        [Fact]
        public void QuestSystem_Restore_survives_round_trip_via_QuestProgressSave()
        {
            var chain = new QuestChainDef(SurvIds.QuestChainStarter, new[]
            {
                new QuestStepDef("q1", SurvIds.QuestObjectiveCollect, "b1", 1, new Dictionary<string, int>()),
                new QuestStepDef("q2", SurvIds.QuestObjectiveGather, "n1", 1, new Dictionary<string, int>()),
                new QuestStepDef("q3", SurvIds.QuestObjectiveReachLevel, "hq", 2, new Dictionary<string, int>()),
            });
            var quests = new QuestSystem(chain);
            quests.Restore(2, 0);
            Assert.Equal(2, quests.Index);

            var json = QuestProgressSave.Capture(chain.Id, quests);
            var fresh = new QuestSystem(chain);
            Assert.True(QuestProgressSave.Apply(json, chain.Id, fresh));
            Assert.Equal(2, fresh.Index);
        }

        [Fact]
        public void World_gather_remaining_stock_round_trips_in_document()
        {
            var doc = new CloudSaveDocument();
            doc.Gather["theme_a.node.gather_quarry"] = new CloudSaveDocument.GatherStock
            {
                Available = 6720,
                UpdatedAt = 100,
            };
            var round = CloudSaveDocument.Parse(doc.Write());
            Assert.NotNull(round);
            Assert.Equal(6720, round!.Gather["theme_a.node.gather_quarry"].Available);
            Assert.Equal(100, round.Gather["theme_a.node.gather_quarry"].UpdatedAt);
        }

        [Fact]
        public void Cloud_push_stays_local_when_AllowsCloudSave_is_false()
        {
            var store = new Dictionary<string, string>(StringComparer.Ordinal);
            CloudSaveServerState? lastPut = null;
            var transport = new FakeTransport(
                get: done => done(new CloudSaveServerState { Version = 0, BlobJson = null }, null),
                put: (v, blob, consent, done) =>
                {
                    lastPut = new CloudSaveServerState { Version = v + 1, BlobJson = blob };
                    done(lastPut, null);
                });

            var sync = new CloudSaveSync(
                transport,
                allowsCloudSave: () => false,
                (k, v) => store[k] = v,
                k => store.TryGetValue(k, out var v) ? v : null);

            var pushed = false;
            sync.Push(null, null, null, SurvIds.QuestChainStarter, null, null, null, 1, ok => pushed = ok);
            Assert.False(pushed);
            Assert.Equal("consent_required", sync.LastError);
            Assert.Null(lastPut);
            Assert.True(store.ContainsKey("survival.cloud.blob.v1"));
        }

        [Fact]
        public void Cloud_push_and_pull_run_when_AllowsCloudSave_is_true()
        {
            CloudSaveServerState? lastPut = null;
            var transport = new FakeTransport(
                get: done => done(new CloudSaveServerState { Version = 0 }, null),
                put: (v, blob, consent, done) =>
                {
                    Assert.True(consent);
                    lastPut = new CloudSaveServerState { Version = 1, BlobJson = blob, ConsentAt = "now" };
                    done(lastPut, null);
                });

            var sync = new CloudSaveSync(transport, allowsCloudSave: () => true);
            var ok = false;
            sync.Push(null, null, null, SurvIds.QuestChainStarter, null, null, null, 1, r => ok = r);
            Assert.True(ok);
            Assert.NotNull(lastPut);
            Assert.Equal(1, sync.ServerVersion);

            ok = false;
            sync.PullAndMerge(null, null, null, SurvIds.QuestChainStarter, null, null, null, 2, r => ok = r);
            Assert.True(ok);
        }

        [Fact]
        public void Parse_accepts_legacy_singular_quest()
        {
            var json = "{\"schema\":1,\"savedAt\":1,\"quest\":{\"chainId\":\"quest.chain.starter\",\"index\":4,\"progress\":2}}";
            var doc = CloudSaveDocument.Parse(json);
            Assert.NotNull(doc);
            Assert.Equal(4, doc!.Quests[SurvIds.QuestChainStarter].Index);
            Assert.Equal(2, doc.Quests[SurvIds.QuestChainStarter].Progress);
        }


        [Fact]
        public void Ftue_step_round_trips_in_document()
        {
            var doc = new CloudSaveDocument { FtueStepId = SurvIds.FtueStepFirstMarch };
            var round = CloudSaveDocument.Parse(doc.Write());
            Assert.NotNull(round);
            Assert.Equal(SurvIds.FtueStepFirstMarch, round!.FtueStepId);

            var merged = CloudSaveMerge.Merge(
                new CloudSaveDocument { FtueStepId = SurvIds.FtueStepBoot },
                new CloudSaveDocument { FtueStepId = SurvIds.FtueStepFirstBattle });
            Assert.Equal(SurvIds.FtueStepFirstBattle, merged.FtueStepId);
        }

        [Fact]
        public void World_gather_regenAt_round_trips_in_document()
        {
            var doc = new CloudSaveDocument();
            doc.Gather["theme_a.node.gather_quarry"] = new CloudSaveDocument.GatherStock
            {
                Available = 4200,
                UpdatedAt = 50,
                RegenAt = 9999.5,
            };
            var round = CloudSaveDocument.Parse(doc.Write());
            Assert.NotNull(round);
            var g = round!.Gather["theme_a.node.gather_quarry"];
            Assert.Equal(4200, g.Available);
            Assert.Equal(50, g.UpdatedAt);
            Assert.Equal(9999.5, g.RegenAt);

            var a = new CloudSaveDocument();
            a.Gather["theme_a.node.gather_quarry"] = new CloudSaveDocument.GatherStock { Available = 100, RegenAt = 10 };
            var b = new CloudSaveDocument();
            b.Gather["theme_a.node.gather_quarry"] = new CloudSaveDocument.GatherStock { Available = 80, RegenAt = 20 };
            var m = CloudSaveMerge.Merge(a, b);
            Assert.Equal(80, m.Gather["theme_a.node.gather_quarry"].Available);
            Assert.Equal(20, m.Gather["theme_a.node.gather_quarry"].RegenAt);
        }

        [Fact]
        public void Quest_index_survives_deep_into_a_32_step_chain()
        {
            var steps = new QuestStepDef[32];
            for (var i = 0; i < 32; i++)
            {
                steps[i] = new QuestStepDef("q" + i, SurvIds.QuestObjectiveCollect, "b1", 1, new Dictionary<string, int>());
            }

            var chain = new QuestChainDef(SurvIds.QuestChainStarter, steps);
            var quests = new QuestSystem(chain);
            quests.Restore(31, 0);
            Assert.Equal(31, quests.Index);

            var doc = new CloudSaveDocument();
            QuestProgressSave.FillDocument(doc, chain.Id, quests);
            var round = CloudSaveDocument.Parse(doc.Write());
            var fresh = new QuestSystem(chain);
            QuestProgressSave.ApplyDocument(round!, chain.Id, fresh);
            Assert.Equal(31, fresh.Index);
            Assert.Equal(0, fresh.Progress);
        }

        private sealed class FakeTransport : ICloudSaveTransport
        {
            private readonly Action<Action<CloudSaveServerState?, string?>> _get;
            private readonly Action<long, string, bool, Action<CloudSaveServerState?, string?>> _put;

            public FakeTransport(
                Action<Action<CloudSaveServerState?, string?>> get,
                Action<long, string, bool, Action<CloudSaveServerState?, string?>> put)
            {
                _get = get;
                _put = put;
            }

            public void Get(Action<CloudSaveServerState?, string?> done) => _get(done);

            public void Put(long baseVersion, string blobJson, bool consentGranted, Action<CloudSaveServerState?, string?> done) =>
                _put(baseVersion, blobJson, consentGranted, done);
        }
    }
}
