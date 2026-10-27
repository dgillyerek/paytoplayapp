#nullable enable
using System.Collections.Generic;

namespace Survival.Domain.Audio
{
    /// <summary>Where the volume settings are saved (PlayerPrefs in the game, a dictionary in tests).</summary>
    public interface IAudioPrefs
    {
        bool HasKey(string key);
        float GetFloat(string key, float fallback);
        void SetFloat(string key, float value);
        void Save();
    }

    public sealed class MemoryAudioPrefs : IAudioPrefs
    {
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();
        public int Saves { get; private set; }
        public bool HasKey(string key) => _values.ContainsKey(key);
        public float GetFloat(string key, float fallback) => _values.TryGetValue(key, out var v) ? v : fallback;
        public void SetFloat(string key, float value) => _values[key] = value;
        public void Save() => Saves++;
    }

    /// <summary>
    /// Separate Music and SFX volumes (0..1) and a master mute, saved to prefs on every change and read back at start.
    /// </summary>
    public sealed class AudioSettingsModel
    {
        public const string MusicKey = "survival.audio.music_volume";
        public const string SfxKey = "survival.audio.sfx_volume";
        public const string MuteKey = "survival.audio.muted";
        public const float DefaultMusic = 0.7f;
        public const float DefaultSfx = 1f;

        private readonly IAudioPrefs _prefs;

        public AudioSettingsModel(IAudioPrefs prefs)
        {
            _prefs = prefs;
            MusicVolume = AudioCueDef.Clamp01(prefs.GetFloat(MusicKey, DefaultMusic));
            SfxVolume = AudioCueDef.Clamp01(prefs.GetFloat(SfxKey, DefaultSfx));
            Muted = prefs.GetFloat(MuteKey, 0f) > 0.5f;
        }

        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }
        public bool Muted { get; private set; }

        /// <summary>Raised after any change, so the mixer can apply it to playing sources.</summary>
        public event System.Action? Changed;

        public void SetMusicVolume(float v)
        {
            MusicVolume = AudioCueDef.Clamp01(v);
            _prefs.SetFloat(MusicKey, MusicVolume);
            Commit();
        }

        public void SetSfxVolume(float v)
        {
            SfxVolume = AudioCueDef.Clamp01(v);
            _prefs.SetFloat(SfxKey, SfxVolume);
            Commit();
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            _prefs.SetFloat(MuteKey, muted ? 1f : 0f);
            Commit();
        }

        /// <summary>Final gain for a cue on a bus: cue volume x bus volume, 0 when muted.</summary>
        public float Gain(AudioBus bus, float cueVolume)
        {
            if (Muted)
            {
                return 0f;
            }

            return AudioCueDef.Clamp01(cueVolume) * (bus == AudioBus.Music ? MusicVolume : SfxVolume);
        }

        private void Commit()
        {
            _prefs.Save();
            Changed?.Invoke();
        }
    }
}
