#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Survival.Domain.Serialization;

namespace Survival.Domain.Campaign
{
    /// <summary>
    /// Save data for heroes and battles (survival_save.json): wallet balances, every hero's owned / level / stars /
    /// shards, running training timers (absolute clock seconds), cleared stages, wins, bounty step, the runner level
    /// reached and the one-time store offers bought.
    /// </summary>
    public static class CampaignSave
    {
        public const int Version = 1;
        public const string FileName = "survival_save.json";

        public static string Write(CampaignSession s)
        {
            var w = new JsonWriter();
            w.Begin();
            w.Field("version", Version);
            w.Field("saved_at", s.Clock.Now);
            w.Key("wallet");
            w.Begin();
            foreach (var res in CampaignSession.SavedResources)
            {
                w.Field(res, s.Wallet.Get(res));
            }

            w.End();
            w.Key("heroes");
            w.Begin();
            foreach (var p in s.Roster.All)
            {
                w.Key(p.Id);
                w.Begin();
                w.Field("owned", p.Owned);
                w.Field("level", p.Level);
                w.Field("stars", p.Stars);
                w.Field("shards", p.Shards);
                w.End();
            }

            w.End();
            w.Key("training");
            w.BeginArray();
            foreach (var t in s.Roster.Training)
            {
                w.Begin();
                w.Field("hero", t.HeroId);
                w.Field("started_at", t.StartedAt);
                w.Field("duration", t.DurationSeconds);
                w.End();
            }

            w.EndArray();
            w.Key("cleared");
            w.BeginArray();
            var cleared = new List<string>(s.Cleared);
            cleared.Sort(StringComparer.Ordinal);
            foreach (var c in cleared)
            {
                w.Value(c);
            }

            w.EndArray();
            w.Field("wins", s.Wins);
            w.Field("battles", s.Battles);
            w.Field("bounty", s.BountyIndex);
            w.Field("runner_unlocked", s.RunnerUnlocked);
            w.Key("bought_once");
            w.BeginArray();
            var bought = new List<string>(s.BoughtOnce);
            bought.Sort(StringComparer.Ordinal);
            foreach (var b in bought)
            {
                w.Value(b);
            }

            w.EndArray();
            w.End();
            return w.ToString();
        }

        public static bool Read(CampaignSession s, string json)
        {
            JsonNode root;
            try
            {
                root = JsonRead.Parse(json);
            }
            catch (FormatException)
            {
                return false;
            }

            if (root is not JsonNode.Obj o)
            {
                return false;
            }

            if (o.TryGet("wallet", out var wn) && wn is JsonNode.Obj wallet)
            {
                foreach (var res in CampaignSession.SavedResources)
                {
                    if (wallet.TryGet(res, out var v) && v is JsonNode.Num n)
                    {
                        var want = Math.Max(0, (int)Math.Round(n.Value));
                        s.Wallet.Add(res, want - s.Wallet.Get(res));
                    }
                }
            }

            if (o.TryGet("heroes", out var hn) && hn is JsonNode.Obj heroes)
            {
                foreach (var pair in heroes.Fields)
                {
                    if (pair.Value is not JsonNode.Obj h)
                    {
                        continue;
                    }

                    s.Roster.Restore(new HeroProgress(pair.Key)
                    {
                        Owned = CampaignCatalog.Bool(h, "owned"),
                        Level = CampaignCatalog.Int(h, "level", 1),
                        Stars = CampaignCatalog.Int(h, "stars", 1),
                        Shards = CampaignCatalog.Int(h, "shards", 0)
                    });
                }
            }

            foreach (var t in CampaignCatalog.Items(o, "training"))
            {
                if (t is JsonNode.Obj to && to.TryGet("hero", out var id) && id is JsonNode.Str ids)
                {
                    s.Roster.RestoreTraining(new TrainingTimer(ids.Value, CampaignCatalog.Num(to, "started_at", s.Clock.Now), CampaignCatalog.Num(to, "duration", 0)));
                }
            }

            var cleared = new List<string>();
            foreach (var c in CampaignCatalog.Items(o, "cleared"))
            {
                if (c is JsonNode.Str cs)
                {
                    cleared.Add(cs.Value);
                }
            }

            s.RestoreCounters(CampaignCatalog.Int(o, "wins", 0), CampaignCatalog.Int(o, "battles", 0), CampaignCatalog.Int(o, "bounty", 0), cleared);
            s.RestoreRunner(CampaignCatalog.Int(o, "runner_unlocked", 1));
            var once = new List<string>();
            foreach (var b in CampaignCatalog.Items(o, "bought_once"))
            {
                if (b is JsonNode.Str bs)
                {
                    once.Add(bs.Value);
                }
            }

            s.RestoreBought(once);
            return true;
        }
    }

    /// <summary>Minimal JSON writer (the domain has a reader only).</summary>
    internal sealed class JsonWriter
    {
        private readonly StringBuilder _sb = new();
        private readonly Stack<bool> _first = new();
        private bool _afterKey;

        public void Begin() => Open('{');

        public void End() => Close('}');

        public void BeginArray() => Open('[');

        public void EndArray() => Close(']');

        public void Key(string key)
        {
            Comma();
            Str(key);
            _sb.Append(':');
            _afterKey = true;
        }

        public void Field(string key, int v)
        {
            Key(key);
            Raw(v.ToString(CultureInfo.InvariantCulture));
        }

        public void Field(string key, double v)
        {
            Key(key);
            Raw(v.ToString("R", CultureInfo.InvariantCulture));
        }

        public void Field(string key, bool v)
        {
            Key(key);
            Raw(v ? "true" : "false");
        }

        public void Field(string key, string v)
        {
            Key(key);
            Value(v);
        }

        public void Value(string v)
        {
            Comma();
            Str(v);
        }

        public override string ToString() => _sb.ToString();

        private void Raw(string s)
        {
            Comma();
            _sb.Append(s);
        }

        private void Open(char c)
        {
            Comma();
            _sb.Append(c);
            _first.Push(true);
        }

        private void Close(char c)
        {
            _first.Pop();
            _sb.Append(c);
        }

        private void Comma()
        {
            if (_afterKey)
            {
                _afterKey = false;
                return;
            }

            if (_first.Count == 0)
            {
                return;
            }

            if (_first.Peek())
            {
                _first.Pop();
                _first.Push(false);
            }
            else
            {
                _sb.Append(',');
            }
        }

        private void Str(string s)
        {
            _sb.Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"':
                        _sb.Append("\\\"");
                        break;
                    case '\\':
                        _sb.Append("\\\\");
                        break;
                    case '\n':
                        _sb.Append("\\n");
                        break;
                    case '\r':
                        _sb.Append("\\r");
                        break;
                    case '\t':
                        _sb.Append("\\t");
                        break;
                    default:
                        if (ch < 0x20)
                        {
                            _sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            _sb.Append(ch);
                        }

                        break;
                }
            }

            _sb.Append('"');
        }
    }
}
