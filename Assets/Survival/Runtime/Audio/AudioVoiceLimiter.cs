#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Audio
{
    /// <summary>
    /// Decides whether a one-shot may start now: per cue, at most <see cref="AudioCueDef.MaxVoices"/> copies sounding and
    /// no retrigger inside <see cref="AudioCueDef.MinGap"/> seconds, so a squad of twelve archers firing on the same frame
    /// plays a few bow twangs, not twelve stacked ones. Also picks the small random pitch for each start.
    /// Time is whatever clock the caller passes (unscaled seconds in the game).
    /// </summary>
    public sealed class AudioVoiceLimiter
    {
        private readonly Dictionary<string, List<double>> _endsByCue = new Dictionary<string, List<double>>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _lastStart = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Random _rng;

        public AudioVoiceLimiter(int seed = 1)
        {
            _rng = new Random(seed);
        }

        /// <summary>Voices of <paramref name="cueId"/> still sounding at <paramref name="now"/>.</summary>
        public int Active(string cueId, double now)
        {
            if (!_endsByCue.TryGetValue(cueId, out var ends))
            {
                return 0;
            }

            ends.RemoveAll(e => e <= now);
            return ends.Count;
        }

        /// <summary>
        /// True (and the start is recorded, voice held for <paramref name="clipSeconds"/>) when the cue may play now;
        /// <paramref name="pitch"/> is 1 +/- the cue's jitter.
        /// </summary>
        public bool TryStart(AudioCueDef cue, double now, float clipSeconds, out float pitch)
        {
            pitch = 1f;
            if (cue == null)
            {
                return false;
            }

            if (_lastStart.TryGetValue(cue.Id, out var last) && now - last < cue.MinGap)
            {
                return false;
            }

            if (Active(cue.Id, now) >= cue.MaxVoices)
            {
                return false;
            }

            if (!_endsByCue.TryGetValue(cue.Id, out var ends))
            {
                ends = new List<double>();
                _endsByCue[cue.Id] = ends;
            }

            if (cue.PitchJitter > 0f)
            {
                pitch = 1f + (float)(((_rng.NextDouble() * 2.0) - 1.0) * cue.PitchJitter);
            }

            var length = Math.Max(0.01f, clipSeconds) / Math.Max(0.1f, pitch);
            ends.Add(now + length);
            _lastStart[cue.Id] = now;
            return true;
        }

        public void Reset()
        {
            _endsByCue.Clear();
            _lastStart.Clear();
        }
    }
}
