using Survival.Domain.Audio;
using Survival.Domain.Roster;
using Survival.Domain.Runner;
using Survival.Domain.Theme;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>Theme A sound: config lookup, every mapping lands on a shipped file, voice limiting, saved volume prefs.</summary>
public class AudioTests
{
    private const string PackRel = "Assets/ThemePack/fantasy_kingdom_a";

    private static readonly string Root = FindRepoRoot();

    private static string PackDir => Path.Combine(Root, PackRel);

    private static string ConfigRel => ThemeAudioConfig.ConfigPathFromPack(File.ReadAllText(Path.Combine(PackDir, "pack.json")));

    private static ThemeAudioConfig Config => ThemeAudioConfig.Parse(File.ReadAllText(Path.Combine(PackDir, ConfigRel)));

    private static AudioCueDef Cue(string id = "x", int maxVoices = 2, float minGap = 0.05f, float jitter = 0f) =>
        new AudioCueDef(id, id + ".ogg", AudioBus.Sfx, false, 1f, maxVoices, minGap, jitter);

    // ---------- theme pack lookup ----------

    [Fact]
    public void PackJson_points_at_the_audio_config()
    {
        Assert.Equal("audio/audio.json", ConfigRel);
        Assert.True(File.Exists(Path.Combine(PackDir, ConfigRel)));
    }

    [Fact]
    public void Binder_exposes_the_audio_config_path()
    {
        var pack = File.ReadAllText(Path.Combine(PackDir, "pack.json"));
        var binder = ThemePackBinder.Parse(pack, "{}", "{\"pins\":[]}", "{}");
        Assert.Equal("audio/audio.json", binder.AudioConfigPath);
    }

    [Fact]
    public void Pack_without_audio_key_is_silent()
    {
        Assert.Equal("", ThemeAudioConfig.ConfigPathFromPack("{\"id\":\"other\"}"));
        var empty = ThemeAudioConfig.Parse("{}");
        Assert.Empty(empty.Cues);
        Assert.Empty(AudioEventMap.Runner(empty, new RunnerEvent(RunnerEventKind.Hit, 1, 2, 0f, 0f, 1f, false)));
    }

    [Fact]
    public void FilePath_is_config_folder_plus_file()
    {
        Assert.True(Config.TryCue(AudioCues.UiTap, out var tap));
        Assert.Equal("Assets/ThemePack/fantasy_kingdom_a/audio/ui_tap.ogg",
            ThemeAudioConfig.FilePath("Assets/ThemePack/fantasy_kingdom_a/audio/audio.json", tap));
    }

    // ---------- every mapping lands on an existing file ----------

    [Fact]
    public void Every_cue_file_exists_with_a_meta()
    {
        var cfg = Config;
        Assert.True(cfg.Cues.Count >= 40);
        var dir = Path.GetDirectoryName(Path.Combine(PackDir, ConfigRel))!;
        foreach (var cue in cfg.Cues.Values)
        {
            var file = Path.Combine(dir, cue.File);
            Assert.True(File.Exists(file), cue.Id + " -> missing " + cue.File);
            Assert.True(File.Exists(file + ".meta"), cue.Id + " -> missing meta for " + cue.File);
            Assert.EndsWith(".ogg", cue.File);
            Assert.True(new FileInfo(file).Length < 400_000, cue.File + " is larger than expected");
        }
    }

    [Fact]
    public void Every_audio_file_is_used_and_licensed()
    {
        var dir = Path.Combine(PackDir, "audio");
        var used = Config.Cues.Values.Select(c => c.File).ToHashSet();
        var licenses = File.ReadAllText(Path.Combine(dir, "LICENSES.md"));
        Assert.Contains("CC0", licenses);
        foreach (var f in Directory.GetFiles(dir, "*.ogg"))
        {
            var name = Path.GetFileName(f);
            Assert.Contains(name, used);
            Assert.Contains("`" + name + "`", licenses);
        }
    }

    [Fact]
    public void Every_referenced_cue_is_defined()
    {
        var cfg = Config;
        foreach (var id in cfg.ReferencedCues().Concat(AudioCues.CodeCues))
        {
            Assert.True(cfg.TryCue(id, out _), "undefined cue " + id);
        }
    }

    [Fact]
    public void Loops_are_music_and_flagged_loop()
    {
        var cfg = Config;
        foreach (var id in new[] { AudioCues.MusicVillage, AudioCues.MusicRunner, AudioCues.MusicBattle })
        {
            Assert.True(cfg.TryCue(id, out var c));
            Assert.True(c.Loop, id);
            Assert.Equal(AudioBus.Music, c.Bus);
        }

        Assert.All(cfg.Cues.Values.Where(c => c.Bus == AudioBus.Sfx), c => Assert.False(c.Loop, c.Id));
        Assert.InRange(cfg.MusicCrossfadeSeconds, 0.2f, 5f);
    }

    [Fact]
    public void Every_runner_event_kind_is_mapped_or_deliberately_silent()
    {
        var cfg = Config;
        foreach (RunnerEventKind kind in Enum.GetValues(typeof(RunnerEventKind)))
        {
            if (AudioEventMap.SilentRunnerEvents.Contains(kind) || kind == RunnerEventKind.Fired || kind == RunnerEventKind.HazardHit)
            {
                continue;
            }

            var cues = AudioEventMap.Runner(cfg, new RunnerEvent(kind, 1, 2, 0f, 0f, 1f, false));
            Assert.True(cues.Count > 0, "runner event " + kind + " has no sound");
        }

        Assert.Empty(AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.FoeSpawned, 1, 0, 0f, 0f, 0f, false)));
    }

    [Fact]
    public void Runner_shots_hazards_and_variants()
    {
        var cfg = Config;
        string Shot(string unit) => AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.Fired, 1, 0, 0f, 0f, 0f, false, false, unit)).Single();
        Assert.Equal("sfx.shot.lyra", Shot("lyra"));
        Assert.Equal("sfx.shot.rowan", Shot("rowan"));
        Assert.Equal("sfx.shot.oakenshield", Shot("oakenshield"));
        Assert.Equal("sfx.enemy.projectile", Shot("bonequill"));
        Assert.Equal("sfx.enemy.projectile", Shot("vespera"));
        // The boss's Fired comes with its wind-up, which has its own growl.
        Assert.Empty(AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.Fired, 1, 0, 0f, 0f, 0f, false, false, "ironhowl")));

        foreach (RunnerHazardKind kind in Enum.GetValues(typeof(RunnerHazardKind)))
        {
            var cues = AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.HazardHit, 1, 2, 0f, 0f, 1f, true, false, RunnerHazardDef.Name(kind)));
            Assert.Single(cues);
        }

        Assert.Equal("sfx.crate.hit", AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.Hit, 1, 2, 0f, 0f, 1f, false, true)).Single());
        Assert.Equal("sfx.enemy.hit", AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.Hit, 1, 2, 0f, 0f, 1f, false)).Single());
        Assert.Equal(AudioCues.EnemyDeathBig, AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.FoeKilled, 1, 0, 0f, 0f, 0f, true)).Single());
        Assert.Contains(AudioCues.JingleLose, AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.Lost, 0, 0, 0f, 0f, 0f, false)));
        Assert.Contains(AudioCues.JingleWin, AudioEventMap.Runner(cfg, new RunnerEvent(RunnerEventKind.Won, 0, 0, 0f, 0f, 0f, false)));
    }

    [Fact]
    public void Every_battle_impact_kind_is_mapped()
    {
        var cfg = Config;
        foreach (BattleImpactKind kind in Enum.GetValues(typeof(BattleImpactKind)))
        {
            Assert.NotEqual("", AudioEventMap.BattleImpact(cfg, "Lyra", "", kind));
        }
    }

    [Fact]
    public void Every_boss_attack_has_its_own_sound()
    {
        var cfg = Config;
        var ironhowl = LockedBattleRoster.All.Single(f => f.Name == "Ironhowl");
        Assert.True(AudioEventMap.IsBoss(cfg, "Ironhowl"));
        for (var i = 0; i < LockedBattleRotation.Count(ironhowl); i++)
        {
            var label = LockedBattleRotation.AttackAt(ironhowl, i).Label;
            Assert.True(cfg.BossAttacks.ContainsKey(label), "Ironhowl attack '" + label + "' has no sound");
        }

        Assert.Equal("sfx.boss.sweep", AudioEventMap.BattleImpact(cfg, "Ironhowl", "backhand sweep", BattleImpactKind.IronClaw));
        Assert.Equal("sfx.boss.charge", AudioEventMap.BattleImpact(cfg, "Ironhowl", "shoulder charge", BattleImpactKind.IronClaw));
        Assert.Equal(AudioCues.EnemyDeathBig, AudioEventMap.BattleDefeat(cfg, "Ironhowl", false));
        Assert.Equal(AudioCues.HeroDeath, AudioEventMap.BattleDefeat(cfg, "Lyra", true));
    }

    [Fact]
    public void Battle_shots_name_real_fighters_and_heroes_have_their_own()
    {
        var cfg = Config;
        var names = LockedBattleRoster.All.Select(f => f.Name).ToHashSet();
        Assert.All(cfg.BattleShots.Keys, n => Assert.Contains(n, names));
        Assert.All(cfg.BattleBosses, n => Assert.Contains(n, names));
        Assert.Equal("sfx.shot.lyra", AudioEventMap.BattleLaunch(cfg, "Lyra"));
        Assert.Equal("sfx.shot.rowan", AudioEventMap.BattleLaunch(cfg, "Rowan"));
        Assert.Equal("sfx.shot.oakenshield", AudioEventMap.BattleLaunch(cfg, "Oakenshield"));
        Assert.Equal("", AudioEventMap.BattleLaunch(cfg, "Nightfang"));
    }

    [Fact]
    public void Collect_by_resource_falls_back_to_generic()
    {
        var cfg = Config;
        Assert.Equal("eco.collect.food", AudioEventMap.Collect(cfg, "food"));
        Assert.Equal("eco.collect.gold", AudioEventMap.Collect(cfg, "soft"));
        Assert.Equal(AudioCues.CollectResource, AudioEventMap.Collect(cfg, "something_new"));
    }

    // ---------- rate limiting ----------

    [Fact]
    public void Limiter_blocks_retrigger_inside_min_gap()
    {
        var lim = new AudioVoiceLimiter();
        var cue = Cue(maxVoices: 8, minGap: 0.1f);
        Assert.True(lim.TryStart(cue, 0.0, 1f, out _));
        Assert.False(lim.TryStart(cue, 0.05, 1f, out _));
        Assert.True(lim.TryStart(cue, 0.11, 1f, out _));
    }

    [Fact]
    public void Limiter_caps_voices_until_one_ends()
    {
        var lim = new AudioVoiceLimiter();
        var cue = Cue(maxVoices: 2, minGap: 0f);
        Assert.True(lim.TryStart(cue, 0.0, 1f, out _));
        Assert.True(lim.TryStart(cue, 0.1, 1f, out _));
        Assert.False(lim.TryStart(cue, 0.2, 1f, out _));
        Assert.Equal(2, lim.Active(cue.Id, 0.2));
        Assert.True(lim.TryStart(cue, 1.05, 1f, out _));
        // Another cue has its own budget.
        Assert.True(lim.TryStart(Cue("y", maxVoices: 1), 0.2, 1f, out _));
    }

    [Fact]
    public void Same_frame_volley_plays_once()
    {
        var lim = new AudioVoiceLimiter();
        var cue = Cue(maxVoices: 4, minGap: 0.03f);
        var started = Enumerable.Range(0, 12).Count(n => lim.TryStart(cue, 5.0, 0.4f, out _));
        Assert.Equal(1, started);
    }

    [Fact]
    public void Pitch_jitter_stays_in_range_and_zero_jitter_is_exact()
    {
        var lim = new AudioVoiceLimiter(7);
        var cue = Cue(maxVoices: 100, minGap: 0f, jitter: 0.05f);
        var pitches = new List<float>();
        for (var i = 0; i < 50; i++)
        {
            Assert.True(lim.TryStart(cue, i, 0.1f, out var p));
            pitches.Add(p);
        }

        Assert.All(pitches, p => Assert.InRange(p, 0.95f, 1.05f));
        Assert.True(pitches.Distinct().Count() > 10);
        Assert.True(lim.TryStart(Cue("flat", jitter: 0f), 0, 0.1f, out var flat));
        Assert.Equal(1f, flat);
    }

    [Fact]
    public void Cue_def_clamps_bad_values()
    {
        var c = new AudioCueDef("c", "c.ogg", AudioBus.Sfx, false, 3f, 0, -1f, 2f);
        Assert.Equal(1f, c.Volume);
        Assert.True(c.MaxVoices >= 1);
        Assert.True(c.MinGap >= 0f);
        Assert.InRange(c.PitchJitter, 0f, 0.5f);
    }

    // ---------- volume prefs ----------

    [Fact]
    public void Volume_defaults_then_persists_across_sessions()
    {
        var prefs = new MemoryAudioPrefs();
        var a = new AudioSettingsModel(prefs);
        Assert.Equal(AudioSettingsModel.DefaultMusic, a.MusicVolume);
        Assert.Equal(AudioSettingsModel.DefaultSfx, a.SfxVolume);
        Assert.False(a.Muted);

        var changed = 0;
        a.Changed += () => changed++;
        a.SetMusicVolume(0.25f);
        a.SetSfxVolume(0.5f);
        a.SetMuted(true);
        Assert.Equal(3, changed);
        Assert.True(prefs.Saves >= 3);

        var b = new AudioSettingsModel(prefs);
        Assert.Equal(0.25f, b.MusicVolume);
        Assert.Equal(0.5f, b.SfxVolume);
        Assert.True(b.Muted);
    }

    [Fact]
    public void Volume_clamps_and_mute_silences_both_buses()
    {
        var s = new AudioSettingsModel(new MemoryAudioPrefs());
        s.SetMusicVolume(4f);
        s.SetSfxVolume(-1f);
        Assert.Equal(1f, s.MusicVolume);
        Assert.Equal(0f, s.SfxVolume);
        s.SetSfxVolume(0.5f);
        Assert.Equal(0.4f, s.Gain(AudioBus.Sfx, 0.8f), 3);
        Assert.Equal(0.8f, s.Gain(AudioBus.Music, 0.8f), 3);
        s.SetMuted(true);
        Assert.Equal(0f, s.Gain(AudioBus.Sfx, 1f));
        Assert.Equal(0f, s.Gain(AudioBus.Music, 1f));
    }

    // ---------- music ----------

    [Fact]
    public void Music_stack_falls_back_when_a_scene_closes()
    {
        var m = new MusicStack();
        object village = new object(), runner = new object(), battle = new object();
        Assert.True(m.Request(village, AudioCues.MusicVillage));
        Assert.True(m.Request(runner, AudioCues.MusicRunner));
        Assert.Equal(AudioCues.MusicRunner, m.Current);
        Assert.True(m.Release(runner));
        Assert.Equal(AudioCues.MusicVillage, m.Current);
        Assert.True(m.Request(battle, AudioCues.MusicBattle));
        Assert.False(m.Request(battle, AudioCues.MusicBattle));
        Assert.True(m.Release(battle));
        Assert.False(m.Release(battle));
        Assert.Equal(AudioCues.MusicVillage, m.Current);
    }

    [Fact]
    public void Crossfade_is_equal_power_and_ends_on_the_new_track()
    {
        var (o0, i0) = AudioCrossfade.Gains(0f, 1.2f);
        var (o1, i1) = AudioCrossfade.Gains(1.2f, 1.2f);
        var (om, im) = AudioCrossfade.Gains(0.6f, 1.2f);
        Assert.Equal(1f, o0, 3);
        Assert.Equal(0f, i0, 3);
        Assert.Equal(0f, o1, 3);
        Assert.Equal(1f, i1, 3);
        Assert.Equal(1f, (om * om) + (im * im), 3);
    }

    // ---------- the player build carries the clips ----------

    [Fact]
    public void Player_bake_includes_theme_audio()
    {
        Assert.Equal("ThemePack/fantasy_kingdom_a/audio", PlayerBakeManifest.AudioDir);
        Assert.True(PlayerBakeManifest.IsAudioClip(PlayerBakeManifest.AudioDir + "/ui_tap.ogg"));
        Assert.False(PlayerBakeManifest.IsAudioClip(PlayerBakeManifest.AudioDir + "/audio.json"));
        Assert.True(PlayerBakeManifest.IsRawFile(PlayerBakeManifest.AudioDir + "/audio.json"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
