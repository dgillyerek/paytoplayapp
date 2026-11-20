#nullable enable
using System;
using System.Collections.Generic;
using Survival.Domain.Serialization;

namespace Survival.Domain.Audio
{
    public enum AudioBus
    {
        Sfx,
        Music
    }

    /// <summary>
    /// One cue: its file (relative to the audio config's folder) or round-robin variants, and how it is mixed.
    /// </summary>
    public sealed class AudioCueDef
    {
        public AudioCueDef(string id, string file, AudioBus bus, bool loop, float volume, int maxVoices, float minGap, float pitchJitter)
            : this(id, new[] { file }, bus, loop, volume, maxVoices, minGap, pitchJitter, 0f)
        {
        }

        public AudioCueDef(
            string id,
            IReadOnlyList<string> files,
            AudioBus bus,
            bool loop,
            float volume,
            int maxVoices,
            float minGap,
            float pitchJitter,
            float volumeJitter)
        {
            if (files == null || files.Count == 0)
            {
                throw new ArgumentException("audio cue " + id + " needs at least one file.", nameof(files));
            }

            Id = id;
            Files = files;
            File = files[0];
            Bus = bus;
            Loop = loop;
            Volume = Clamp01(volume);
            MaxVoices = Math.Max(1, maxVoices);
            MinGap = Math.Max(0f, minGap);
            PitchJitter = Math.Max(0f, Math.Min(0.5f, pitchJitter));
            VolumeJitter = Math.Max(0f, Math.Min(0.5f, volumeJitter));
        }

        public string Id { get; }

        /// <summary>The first (or only) file.</summary>
        public string File { get; }

        /// <summary>Round-robin variants: each start picks one (never the same one twice in a row when there are several).</summary>
        public IReadOnlyList<string> Files { get; }

        /// <summary>Random gain spread, the start plays at volume x (1 - this .. 1), so repeats do not sound identical.</summary>
        public float VolumeJitter { get; }
        public AudioBus Bus { get; }
        public bool Loop { get; }
        public float Volume { get; }

        /// <summary>Most copies of this cue sounding at once; a further trigger is dropped.</summary>
        public int MaxVoices { get; }

        /// <summary>Seconds a retrigger must wait after the last start; sooner triggers are dropped.</summary>
        public float MinGap { get; }

        /// <summary>Random pitch spread, +/- this fraction, so repeated hits do not sound machine-gunned.</summary>
        public float PitchJitter { get; }

        internal static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>
    /// The theme pack's sound config: cue id to file and mix settings, plus the event maps that say which cue plays
    /// for a runner event, a battle shot or impact, a boss attack or a farm collection. Plain data, no UnityEngine.
    /// </summary>
    public sealed class ThemeAudioConfig
    {
        private ThemeAudioConfig()
        {
        }

        public IReadOnlyDictionary<string, AudioCueDef> Cues { get; private set; } = new Dictionary<string, AudioCueDef>();
        public float MusicCrossfadeSeconds { get; private set; } = 1f;

        /// <summary>Runner shooter unit id (lyra, bonequill...) to the cue for its Fired event.</summary>
        public IReadOnlyDictionary<string, string> RunnerShots { get; private set; } = new Dictionary<string, string>();

        /// <summary>RunnerEventKind name (plus ".box" / ".big" variants) to the cues it plays.</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> RunnerEvents { get; private set; } = new Dictionary<string, IReadOnlyList<string>>();

        public IReadOnlyDictionary<string, string> RunnerHazards { get; private set; } = new Dictionary<string, string>();

        /// <summary>Runner attacker unit id (rowan, sir_aldric...) to the cue where its hit lands on an enemy (its weapon's impact).</summary>
        public IReadOnlyDictionary<string, string> RunnerImpacts { get; private set; } = new Dictionary<string, string>();

        /// <summary>Runner enemy unit id (nightfang, bonequill...) to its own hurt sound, layered under the weapon impact.</summary>
        public IReadOnlyDictionary<string, string> RunnerFoeHurt { get; private set; } = new Dictionary<string, string>();

        /// <summary>Runner enemy unit id to its own death sound (falls back to the FoeKilled event cue).</summary>
        public IReadOnlyDictionary<string, string> RunnerFoeDeath { get; private set; } = new Dictionary<string, string>();

        /// <summary>BattleImpactKind name to the cue played where the hit lands.</summary>
        public IReadOnlyDictionary<string, string> BattleImpacts { get; private set; } = new Dictionary<string, string>();

        /// <summary>Battle fighter name to the cue played when its shot leaves (ranged / caster fighters).</summary>
        public IReadOnlyDictionary<string, string> BattleShots { get; private set; } = new Dictionary<string, string>();

        public IReadOnlyList<string> BattleBosses { get; private set; } = Array.Empty<string>();

        /// <summary>Boss attack label ("claw swipe", "ground pound"...) to its cue, overriding the impact sound.</summary>
        public IReadOnlyDictionary<string, string> BossAttacks { get; private set; } = new Dictionary<string, string>();

        /// <summary>Resource id a building produces (food, soft, wood...) to the collect cue.</summary>
        public IReadOnlyDictionary<string, string> EconomyCollect { get; private set; } = new Dictionary<string, string>();

        public static ThemeAudioConfig Empty { get; } = new ThemeAudioConfig();

        public bool TryCue(string id, out AudioCueDef cue)
        {
            if (!string.IsNullOrEmpty(id) && Cues.TryGetValue(id, out var c))
            {
                cue = c;
                return true;
            }

            cue = null!;
            return false;
        }

        /// <summary>"Assets/.../pack/audio/x.ogg" style path of a cue's file: config folder + file.</summary>
        public static string FilePath(string configPath, AudioCueDef cue) => FilePath(configPath, cue, 0);

        /// <summary>Path of variant <paramref name="variant"/> of a cue (clamped to its variants).</summary>
        public static string FilePath(string configPath, AudioCueDef cue, int variant)
        {
            var p = (configPath ?? "").Replace('\\', '/');
            var slash = p.LastIndexOf('/');
            var dir = slash >= 0 ? p.Substring(0, slash + 1) : "";
            var i = Math.Max(0, Math.Min(cue.Files.Count - 1, variant));
            return dir + cue.Files[i];
        }

        /// <summary>pack.json "audio": the pack-relative path of the sound config, "" when the pack has none.</summary>
        public static string ConfigPathFromPack(string packJson)
        {
            return JsonRead.Parse(packJson) is JsonNode.Obj pack && pack.TryGet("audio", out var n) && n is JsonNode.Str s ? s.Value : "";
        }

        public static ThemeAudioConfig Parse(string json)
        {
            if (JsonRead.Parse(json) is not JsonNode.Obj root)
            {
                throw new FormatException("audio config must be a JSON object.");
            }

            var dVolume = 0.8f;
            var dVoices = 4;
            var dGap = 0.03f;
            var dJitter = 0.04f;
            var dVolJitter = 0f;
            if (root.TryGet("defaults", out var dn) && dn is JsonNode.Obj d)
            {
                dVolume = Num(d, "volume", dVolume);
                dVoices = (int)Num(d, "max_voices", dVoices);
                dGap = Num(d, "min_gap", dGap);
                dJitter = Num(d, "pitch_jitter", dJitter);
                dVolJitter = Num(d, "volume_jitter", dVolJitter);
            }

            var cues = new Dictionary<string, AudioCueDef>(StringComparer.Ordinal);
            if (root.TryGet("cues", out var cn) && cn is JsonNode.Obj cueObj)
            {
                foreach (var pair in cueObj.Fields)
                {
                    var files = new List<string>();
                    if (pair.Value is JsonNode.Obj co)
                    {
                        if (co.TryGet("file", out var fn) && fn is JsonNode.Str file && file.Value.Length > 0)
                        {
                            files.Add(file.Value);
                        }

                        if (co.TryGet("files", out var fl) && fl is JsonNode.Arr fa)
                        {
                            foreach (var item in fa.Items)
                            {
                                if (item is JsonNode.Str fs && fs.Value.Length > 0)
                                {
                                    files.Add(fs.Value);
                                }
                            }
                        }
                    }

                    if (pair.Value is not JsonNode.Obj c || files.Count == 0)
                    {
                        throw new FormatException($"audio cue {pair.Key} needs a file.");
                    }

                    var music = c.TryGet("bus", out var bn) && bn is JsonNode.Str bus && string.Equals(bus.Value, "music", StringComparison.Ordinal);
                    var loop = c.TryGet("loop", out var ln) && ln is JsonNode.Bool lb && lb.Value;
                    cues[pair.Key] = new AudioCueDef(
                        pair.Key,
                        files,
                        music ? AudioBus.Music : AudioBus.Sfx,
                        loop,
                        Num(c, "volume", dVolume),
                        (int)Num(c, "max_voices", music ? 1 : dVoices),
                        Num(c, "min_gap", music ? 0f : dGap),
                        Num(c, "pitch_jitter", music ? 0f : dJitter),
                        Num(c, "volume_jitter", music ? 0f : dVolJitter));
                }
            }

            var bosses = new List<string>();
            if (root.TryGet("battle_bosses", out var bb) && bb is JsonNode.Arr arr)
            {
                foreach (var item in arr.Items)
                {
                    if (item is JsonNode.Str s)
                    {
                        bosses.Add(s.Value);
                    }
                }
            }

            return new ThemeAudioConfig
            {
                Cues = cues,
                MusicCrossfadeSeconds = Math.Max(0f, Num(root, "music_crossfade", 1f)),
                RunnerShots = Map(root, "runner_shots", StringComparer.OrdinalIgnoreCase),
                RunnerEvents = MultiMap(root, "runner_events"),
                RunnerHazards = Map(root, "runner_hazards", StringComparer.OrdinalIgnoreCase),
                RunnerImpacts = Map(root, "runner_impacts", StringComparer.OrdinalIgnoreCase),
                RunnerFoeHurt = Map(root, "runner_foe_hurt", StringComparer.OrdinalIgnoreCase),
                RunnerFoeDeath = Map(root, "runner_foe_death", StringComparer.OrdinalIgnoreCase),
                BattleImpacts = Map(root, "battle_impacts", StringComparer.Ordinal),
                BattleShots = Map(root, "battle_shots", StringComparer.Ordinal),
                BattleBosses = bosses,
                BossAttacks = Map(root, "boss_attacks", StringComparer.OrdinalIgnoreCase),
                EconomyCollect = Map(root, "economy_collect", StringComparer.Ordinal)
            };
        }

        /// <summary>Every cue id any event map names (used by tests: each must be a defined cue).</summary>
        public IEnumerable<string> ReferencedCues()
        {
            foreach (var v in RunnerShots.Values) yield return v;
            foreach (var list in RunnerEvents.Values)
            {
                foreach (var v in list) yield return v;
            }

            foreach (var v in RunnerHazards.Values) yield return v;
            foreach (var v in RunnerImpacts.Values) yield return v;
            foreach (var v in RunnerFoeHurt.Values) yield return v;
            foreach (var v in RunnerFoeDeath.Values) yield return v;
            foreach (var v in BattleImpacts.Values) yield return v;
            foreach (var v in BattleShots.Values) yield return v;
            foreach (var v in BossAttacks.Values) yield return v;
            foreach (var v in EconomyCollect.Values) yield return v;
        }

        private static float Num(JsonNode.Obj o, string key, float fallback) =>
            o.TryGet(key, out var n) && n is JsonNode.Num num ? (float)num.Value : fallback;

        private static IReadOnlyDictionary<string, string> Map(JsonNode.Obj root, string key, StringComparer cmp)
        {
            var map = new Dictionary<string, string>(cmp);
            if (root.TryGet(key, out var n) && n is JsonNode.Obj o)
            {
                foreach (var pair in o.Fields)
                {
                    if (pair.Value is JsonNode.Str s)
                    {
                        map[pair.Key] = s.Value;
                    }
                }
            }

            return map;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> MultiMap(JsonNode.Obj root, string key)
        {
            var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            if (root.TryGet(key, out var n) && n is JsonNode.Obj o)
            {
                foreach (var pair in o.Fields)
                {
                    var list = new List<string>();
                    if (pair.Value is JsonNode.Str s)
                    {
                        list.Add(s.Value);
                    }
                    else if (pair.Value is JsonNode.Arr a)
                    {
                        foreach (var item in a.Items)
                        {
                            if (item is JsonNode.Str si)
                            {
                                list.Add(si.Value);
                            }
                        }
                    }

                    map[pair.Key] = list;
                }
            }

            return map;
        }
    }
}
