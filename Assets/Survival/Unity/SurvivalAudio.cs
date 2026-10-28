#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Audio;
using Survival.Domain.Flavor;
using Survival.Domain.Runner;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// The one place sound plays from. Created on first use and kept across scenes (DontDestroyOnLoad).
    /// Reads the theme pack's audio config (pack.json "audio"), so cue ids map to that pack's files; a pack without
    /// one is simply silent. One-shots come from a small AudioSource pool, gated per cue by <see cref="AudioVoiceLimiter"/>
    /// (voice cap, retrigger gap, small pitch spread). Music / ambience runs on two sources that crossfade when the
    /// requested track changes (<see cref="MusicStack"/>: the runner and the battle push their track, closing them falls
    /// back to the village). Music and SFX volume and mute live in <see cref="Settings"/>, saved in PlayerPrefs.
    /// Presentation only: every entry point swallows its own errors and nothing here touches game state.
    /// </summary>
    public sealed class SurvivalAudio : MonoBehaviour
    {
        private const int PoolSize = 16;

        private static SurvivalAudio? _instance;
        private static bool _quitting;

        private readonly Dictionary<string, AudioClip?> _clips = new Dictionary<string, AudioClip?>(StringComparer.Ordinal);
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private readonly MusicStack _music = new MusicStack();
        private readonly AudioVoiceLimiter _limiter = new AudioVoiceLimiter(20261026);

        private ThemeAudioConfig _config = ThemeAudioConfig.Empty;
        private string _configPath = "";
        private AudioSettingsModel? _settings;

        private AudioSource? _musicA;
        private AudioSource? _musicB;
        private bool _aIsCurrent = true;
        private AudioCueDef? _currentMusic;
        private AudioCueDef? _outgoingMusic;
        private float _fadeT;
        private float _fadeSeconds;
        private bool _fading;

        private AudioListener? _ear;
        private float _earCheck;

        public static AudioSettingsModel? Settings => Ensure()?._settings;

        public static ThemeAudioConfig Config => Ensure()?._config ?? ThemeAudioConfig.Empty;

        /// <summary>Play a one-shot cue (dropped when its voice cap or retrigger gap says no).</summary>
        public static void Play(string cue)
        {
            var a = Ensure();
            if (a == null || string.IsNullOrEmpty(cue))
            {
                return;
            }

            try
            {
                a.PlayCue(cue);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SurvivalAudio: " + cue + ": " + ex.Message);
            }
        }

        public static void Play(IReadOnlyList<string> cues)
        {
            for (var i = 0; i < cues.Count; i++)
            {
                Play(cues[i]);
            }
        }

        /// <summary>The runner's step events (shots, hits, pickups, traps, the boss, win / lose).</summary>
        public static void Runner(RunnerEvent e)
        {
            if (Ensure() == null)
            {
                return;
            }

            Play(AudioEventMap.Runner(Config, e));
        }

        /// <summary><paramref name="owner"/> wants <paramref name="cue"/> as the music until it calls <see cref="ReleaseMusic"/>.</summary>
        public static void RequestMusic(object owner, string cue)
        {
            var a = Ensure();
            if (a == null || owner == null)
            {
                return;
            }

            if (a._music.Request(owner, cue))
            {
                a.ApplyMusic();
            }
        }

        public static void ReleaseMusic(object owner)
        {
            if (_instance == null || _quitting || owner == null)
            {
                return;
            }

            if (_instance._music.Release(owner))
            {
                _instance.ApplyMusic();
            }
        }

        /// <summary>The UI button click (every kit button gets it from <see cref="SurvivalVisuals.Button"/>).</summary>
        public static void Tap() => Play(AudioCues.UiTap);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _quitting = false;
        }

        private static SurvivalAudio? Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }

            if (_quitting || !Application.isPlaying)
            {
                return null;
            }

            var go = new GameObject("SurvivalAudio");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SurvivalAudio>();
            _instance.Init();
            return _instance;
        }

        private void Init()
        {
            _settings = new AudioSettingsModel(new PlayerPrefsAudioPrefs());
            _settings.Changed += ApplyVolumes;
            for (var i = 0; i < PoolSize; i++)
            {
                _pool.Add(NewSource("Sfx" + i));
            }

            _musicA = NewSource("MusicA");
            _musicB = NewSource("MusicB");
            _ear = gameObject.AddComponent<AudioListener>();
            _ear.enabled = false;
            CheckListener();
            LoadConfig();
        }

        private AudioSource NewSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.loop = false;
            return s;
        }

        private void LoadConfig()
        {
            try
            {
                var flavor = AppFlavorConfig.FantasyKingdomA;
                var packRoot = SurvivalArt.ResolvePackRoot(flavor);
                var packJson = PlayerAssets.ReadAllText(Path.Combine(packRoot, "pack.json"));
                var rel = ThemeAudioConfig.ConfigPathFromPack(packJson);
                if (rel.Length == 0)
                {
                    return;
                }

                _config = ThemeAudioConfig.Parse(PlayerAssets.ReadAllText(Path.Combine(packRoot, rel.Replace('/', Path.DirectorySeparatorChar))));
                _configPath = "Assets/" + flavor.PackPath.TrimEnd('/') + "/" + rel;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SurvivalAudio: no sound config (" + ex.Message + "); the game runs silent.");
                _config = ThemeAudioConfig.Empty;
            }
        }

        private AudioClip? Clip(AudioCueDef cue)
        {
            if (_clips.TryGetValue(cue.Id, out var cached))
            {
                return cached;
            }

            AudioClip? clip = null;
            try
            {
                clip = PlayerAssets.Load<AudioClip>(ThemeAudioConfig.FilePath(_configPath, cue));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SurvivalAudio: " + cue.File + ": " + ex.Message);
            }

            if (clip == null)
            {
                Debug.LogWarning("SurvivalAudio: clip not found for " + cue.Id + " (" + cue.File + ")");
            }

            _clips[cue.Id] = clip;
            return clip;
        }

        private void PlayCue(string id)
        {
            if (_settings == null || !_config.TryCue(id, out var cue) || cue.Bus == AudioBus.Music)
            {
                return;
            }

            var gain = _settings.Gain(AudioBus.Sfx, cue.Volume);
            if (gain <= 0.0001f)
            {
                return;
            }

            var clip = Clip(cue);
            if (clip == null)
            {
                return;
            }

            if (!_limiter.TryStart(cue, Time.unscaledTime, clip.length, out var pitch))
            {
                return;
            }

            var src = FreeSource();
            if (src == null)
            {
                return;
            }

            src.clip = clip;
            src.volume = gain;
            src.pitch = pitch;
            src.loop = false;
            src.Play();
        }

        private AudioSource? FreeSource()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].isPlaying)
                {
                    return _pool[i];
                }
            }

            return null;
        }

        private void ApplyMusic()
        {
            try
            {
                var id = _music.Current;
                AudioCueDef? next = null;
                if (id.Length > 0 && _config.TryCue(id, out var cue))
                {
                    next = cue;
                }

                if (next != null && _currentMusic != null && string.Equals(next.Id, _currentMusic.Id, StringComparison.Ordinal))
                {
                    return;
                }

                var outgoing = _aIsCurrent ? _musicA : _musicB;
                var incoming = _aIsCurrent ? _musicB : _musicA;
                if (incoming == null || outgoing == null)
                {
                    return;
                }

                incoming.Stop();
                var clip = next != null ? Clip(next) : null;
                if (clip != null)
                {
                    incoming.clip = clip;
                    incoming.loop = next!.Loop;
                    incoming.pitch = 1f;
                    incoming.volume = 0f;
                    incoming.Play();
                }
                else
                {
                    next = null;
                }

                _outgoingMusic = _currentMusic;
                _currentMusic = next;
                _aIsCurrent = !_aIsCurrent;
                _fadeT = 0f;
                _fadeSeconds = _config.MusicCrossfadeSeconds;
                _fading = true;
                StepFade(0f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SurvivalAudio music: " + ex.Message);
            }
        }

        private void StepFade(float dt)
        {
            var current = _aIsCurrent ? _musicA : _musicB;
            var other = _aIsCurrent ? _musicB : _musicA;
            if (_settings == null || current == null || other == null)
            {
                return;
            }

            _fadeT += dt;
            var g = AudioCrossfade.Gains(_fadeT, _fadeSeconds);
            current.volume = _currentMusic != null ? g.In * _settings.Gain(AudioBus.Music, _currentMusic.Volume) : 0f;
            other.volume = _outgoingMusic != null ? g.Out * _settings.Gain(AudioBus.Music, _outgoingMusic.Volume) : 0f;
            if (_fading && _fadeT >= _fadeSeconds)
            {
                _fading = false;
                other.Stop();
                _outgoingMusic = null;
            }
        }

        private void ApplyVolumes()
        {
            if (!_fading)
            {
                StepFade(0f);
            }

            if (_settings != null && _settings.Muted)
            {
                foreach (var s in _pool)
                {
                    s.Stop();
                }
            }
        }

        private void Update()
        {
            if (_fading)
            {
                StepFade(Time.unscaledDeltaTime);
            }

            _earCheck -= Time.unscaledDeltaTime;
            if (_earCheck <= 0f)
            {
                _earCheck = 0.5f;
                CheckListener();
            }
        }

        /// <summary>Sound needs exactly one live AudioListener: ours switches on only while no scene camera has one.</summary>
        private void CheckListener()
        {
            if (_ear == null)
            {
                return;
            }

            var others = 0;
#if UNITY_2023_1_OR_NEWER
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
#else
            var listeners = FindObjectsOfType<AudioListener>();
#endif
            foreach (var l in listeners)
            {
                if (l != _ear && l.isActiveAndEnabled)
                {
                    others++;
                }
            }

            var want = others == 0;
            if (_ear.enabled != want)
            {
                _ear.enabled = want;
            }
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private sealed class PlayerPrefsAudioPrefs : IAudioPrefs
        {
            public bool HasKey(string key) => PlayerPrefs.HasKey(key);
            public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
            public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
            public void Save() => PlayerPrefs.Save();
        }
    }
}
