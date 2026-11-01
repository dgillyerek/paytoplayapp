#nullable enable
using System;
using System.Collections.Generic;

namespace Survival.Domain.Audio
{
    /// <summary>
    /// Which music should be playing: scenes push (owner, cue) when they open and release their owner when they close,
    /// so closing the battle or runner (loaded on top of the hub) falls back to the village ambience. Last pushed wins.
    /// </summary>
    public sealed class MusicStack
    {
        private readonly List<KeyValuePair<object, string>> _stack = new List<KeyValuePair<object, string>>();

        public string Current => _stack.Count == 0 ? "" : _stack[_stack.Count - 1].Value;

        /// <summary>Returns true when <see cref="Current"/> changed.</summary>
        public bool Request(object owner, string cue)
        {
            var before = Current;
            RemoveOwner(owner);
            _stack.Add(new KeyValuePair<object, string>(owner, cue ?? ""));
            return !string.Equals(before, Current, StringComparison.Ordinal);
        }

        /// <summary>Returns true when <see cref="Current"/> changed.</summary>
        public bool Release(object owner)
        {
            var before = Current;
            RemoveOwner(owner);
            return !string.Equals(before, Current, StringComparison.Ordinal);
        }

        private void RemoveOwner(object owner)
        {
            _stack.RemoveAll(p => ReferenceEquals(p.Key, owner));
        }
    }

    /// <summary>Equal-power crossfade gains: the old track fades out as the new one fades in over the same seconds.</summary>
    public static class AudioCrossfade
    {
        /// <summary>(outgoing, incoming) gain at <paramref name="t"/> seconds into a <paramref name="seconds"/> fade.</summary>
        public static (float Out, float In) Gains(float t, float seconds)
        {
            if (seconds <= 0f || t >= seconds)
            {
                return (0f, 1f);
            }

            var u = t <= 0f ? 0f : t / seconds;
            return ((float)Math.Cos(u * Math.PI * 0.5), (float)Math.Sin(u * Math.PI * 0.5));
        }
    }
}
