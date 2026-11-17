#!/usr/bin/env python3
"""Lyra's runner/battle shot: a short sparkly arcane bolt (procedural, CC0).

Replaces the earlier medieval-pack mix (pitched-down spear swing + boulder rumble
+ a quiet spell layer) that read as a physical thud. This recipe is a bright
bell/chime cluster, a fast upward pitch sweep, an airy whoosh and a short
high-passed reverb tail — no kick, gunshot or low rumble.

Usage:
  python3 scripts/audio/gen_lyra_magic_shot.py
  python3 scripts/audio/gen_lyra_magic_shot.py --preview /workspace/lyra_fire_preview.wav

Writes sfx_lyra_shot.ogg next to the theme pack audio (same filename / cue as
before). Loudness uses the same active-RMS target as the other squad shots
(scripts/audio/medieval_runner/au.py: rms=-17 dBFS, peak=-1 dBFS).
"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys

import numpy as np
from scipy.signal import butter, sosfilt

SR = 44100
SEED = 20261116
RMS_DB = -17.0
PEAK_DB = -1.0
DURATION = 0.38  # seconds, including the light tail

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_OGG = os.path.normpath(os.path.join(
    HERE, "..", "..", "Assets", "ThemePack", "fantasy_kingdom_a", "audio", "sfx_lyra_shot.ogg"
))


def t_axis(dur: float) -> np.ndarray:
    return np.arange(int(round(dur * SR)), dtype=np.float64) / SR


def fade_edges(y: np.ndarray, attack=0.003, release=0.04) -> np.ndarray:
    y = np.asarray(y, dtype=np.float64).copy()
    a = min(len(y), int(round(attack * SR)))
    r = min(len(y), int(round(release * SR)))
    if a > 0:
        y[:a] *= np.linspace(0.0, 1.0, a)
    if r > 0:
        y[-r:] *= np.linspace(1.0, 0.0, r) ** 2
    return y


def exp_env(n: int, decay: float, attack=0.003) -> np.ndarray:
    t = np.arange(n, dtype=np.float64) / SR
    env = np.exp(-t / max(decay, 1e-4))
    a = min(n, int(round(attack * SR)))
    if a > 0:
        env[:a] *= np.linspace(0.0, 1.0, a)
    return env


def butter_sos(cutoff, kind: str, order=2):
    return butter(order, cutoff, kind, fs=SR, output="sos")


def butter_filt(y: np.ndarray, cutoff, kind: str, order=2) -> np.ndarray:
    return sosfilt(butter_sos(cutoff, kind, order), y).astype(np.float64)


def high_shelf(y: np.ndarray, freq=4000.0, gain_db=5.0) -> np.ndarray:
    """First-order high shelf (RBJ-style) to tilt the bolt brighter."""
    A = 10 ** (gain_db / 40.0)
    w0 = 2 * np.pi * freq / SR
    cosw, sinw = np.cos(w0), np.sin(w0)
    S = 1.0
    alpha = sinw / 2 * np.sqrt((A + 1 / A) * (1 / S - 1) + 2)
    b0 =    A * ((A + 1) + (A - 1) * cosw + 2 * np.sqrt(A) * alpha)
    b1 = -2 * A * ((A - 1) + (A + 1) * cosw)
    b2 =    A * ((A + 1) + (A - 1) * cosw - 2 * np.sqrt(A) * alpha)
    a0 =        (A + 1) - (A - 1) * cosw + 2 * np.sqrt(A) * alpha
    a1 =  2 *     ((A - 1) - (A + 1) * cosw)
    a2 =        (A + 1) - (A - 1) * cosw - 2 * np.sqrt(A) * alpha
    b = np.array([b0, b1, b2]) / a0
    a = np.array([1.0, a1 / a0, a2 / a0])
    # Direct form II
    out = np.zeros_like(y)
    x1 = x2 = y1 = y2 = 0.0
    for i, x in enumerate(y):
        yn = b[0] * x + b[1] * x1 + b[2] * x2 - a[1] * y1 - a[2] * y2
        out[i] = yn
        x2, x1 = x1, x
        y2, y1 = y1, yn
    return out


def pinkish(n: int, rng: np.random.Generator) -> np.ndarray:
    w = rng.standard_normal(n)
    out = w * 0.25
    for coeff, gain in ((0.995, 0.12), (0.97, 0.22), (0.90, 0.35), (0.70, 0.28)):
        acc = 0.0
        buf = np.empty(n)
        for i in range(n):
            acc = coeff * acc + (1 - coeff) * w[i]
            buf[i] = acc
        out += buf * gain
    return out


def comb_reverb(x: np.ndarray, wet=0.28, taps=None) -> np.ndarray:
    """Short, bright Schroeder-ish tail. High-passed so it stays airy, not thuddy."""
    if taps is None:
        # Prime lengths, all under ~55 ms so the tail dies inside DURATION.
        taps = [(887, 0.52), (1279, 0.46), (1597, 0.42), (1999, 0.38)]
    wet_sum = np.zeros_like(x)
    for delay, fb in taps:
        y = np.zeros(len(x) + delay)
        y[: len(x)] += x
        for i in range(delay, len(y)):
            y[i] += fb * y[i - delay]
        wet_sum += y[: len(x)]
    wet_sum /= max(len(taps), 1)
    wet_sum = butter_filt(wet_sum, 900, "high", order=2)
    return x * (1.0 - wet) + wet_sum * wet


def rising_bolt() -> np.ndarray:
    """Fast upward pitch sweep — the bolt leaving the staff."""
    dur = 0.11
    t = t_axis(dur)
    f0, f1 = 1480.0, 3920.0
    inst = f0 * (f1 / f0) ** (t / dur)
    inst *= 1.0 + 0.010 * np.sin(2 * np.pi * 42 * t + 0.4)
    phase = 2 * np.pi * np.cumsum(inst) / SR
    body = np.sin(phase)
    body += 0.42 * np.sin(2 * phase) * np.exp(-t / 0.050)
    body += 0.22 * np.sin(3 * phase) * np.exp(-t / 0.032)
    body += 0.10 * np.sin(4 * phase) * np.exp(-t / 0.020)
    env = exp_env(len(t), decay=0.062, attack=0.003) * (1.0 - 0.20 * (t / dur))
    return body * env


def chime_cluster() -> np.ndarray:
    """Inharmonic glass/crystal partials, struck together (not a rising arpeggio,
    which would collide with the pickup/power-up jingles)."""
    dur = 0.36
    t = t_axis(dur)
    # Bright crystal cluster: energy sits from ~1.8 kHz up so it sparkles
    # rather than ringing like a mid-range church bell or item pickup.
    partials = [
        #  freq,   amp,  decay
        (1867.0,  0.14, 0.110),
        (1977.0,  0.11, 0.105),
        (2489.0,  0.16, 0.100),
        (2637.0,  0.14, 0.095),
        (3135.0,  0.13, 0.090),
        (3520.0,  0.15, 0.085),
        (3951.0,  0.12, 0.078),
        (4186.0,  0.14, 0.072),
        (5274.0,  0.13, 0.065),
        (6271.0,  0.12, 0.058),
        (7040.0,  0.11, 0.052),
        (7902.0,  0.09, 0.046),
        (8839.0,  0.08, 0.040),
        (9960.0,  0.06, 0.034),
        (11180.0, 0.05, 0.028),
    ]
    y = np.zeros_like(t)
    for f, amp, decay in partials:
        # Tiny struck-crystal chirp down in the first milliseconds.
        inst = f * (1.0 + 0.022 * np.exp(-t / 0.014))
        phase = 2 * np.pi * np.cumsum(inst) / SR
        env = np.exp(-t / decay)
        a = min(len(env), int(0.0015 * SR))
        env[:a] *= np.linspace(0.0, 1.0, a)
        y += amp * np.sin(phase) * env
    return y


def airy_whoosh(rng: np.random.Generator) -> np.ndarray:
    """Air around the bolt: a brief high-passed whoosh that rises with the sweep."""
    dur = 0.24
    n = int(round(dur * SR))
    t = np.arange(n) / SR
    noise = pinkish(n, rng)
    # Pre-filter a few bands and crossfade them so the whoosh lifts in pitch.
    low = butter_filt(butter_filt(noise, 1800, "high", 2), 4500, "low", 2)
    mid = butter_filt(butter_filt(noise, 3000, "high", 2), 7000, "low", 2)
    hi = butter_filt(butter_filt(noise, 5000, "high", 2), 12000, "low", 2)
    # 0->1 over the first 90 ms, then hold airy.
    xfade = np.clip(t / 0.09, 0.0, 1.0)
    y = (1 - xfade) * low + xfade * (0.55 * mid + 0.45 * hi)
    swell = np.sin(np.pi * np.clip(t / 0.11, 0, 1)) ** 0.6
    env = exp_env(n, decay=0.110, attack=0.008) * (0.35 + 0.65 * swell)
    y = butter_filt(y * env, 2200, "high", order=2)
    return fade_edges(y, attack=0.006, release=0.07)


def sparkle_grains() -> np.ndarray:
    """A handful of tiny high grains in the first 140 ms — glitter, not a melody."""
    dur = 0.30
    y = np.zeros(int(round(dur * SR)))
    onsets = [0.006, 0.018, 0.034, 0.052, 0.074, 0.098, 0.125]
    freqs = [6048.0, 7380.0, 5124.0, 8122.0, 9470.0, 6840.0, 10512.0]
    amps = [0.16, 0.14, 0.12, 0.14, 0.11, 0.10, 0.09]
    for onset, f, amp in zip(onsets, freqs, amps):
        gd = 0.022
        t = t_axis(gd)
        env = np.exp(-t / 0.007)
        a = min(len(env), int(0.0008 * SR))
        env[:a] *= np.linspace(0.0, 1.0, a)
        grain = amp * np.sin(2 * np.pi * f * t) * env
        s = int(onset * SR)
        e = min(len(y), s + len(grain))
        y[s:e] += grain[: e - s]
    return y


def assemble() -> np.ndarray:
    rng = np.random.default_rng(SEED)
    layers = [
        (rising_bolt(), 0.000, 0.62),
        (chime_cluster(), 0.003, 1.00),
        (chime_cluster() * 0.35, 0.055, 0.55),  # soft delayed shimmer, same cluster
        (airy_whoosh(rng), 0.000, 0.70),
        (sparkle_grains(), 0.004, 0.80),
    ]
    n = int(round(DURATION * SR))
    y = np.zeros(n, dtype=np.float64)
    for sig, offset, gain in layers:
        s = int(offset * SR)
        e = min(n, s + len(sig))
        y[s:e] += sig[: e - s] * gain
    # Keep the body out of the thud range; this is the "not a gunshot" high-pass.
    y = butter_filt(y, 280, "high", order=2)
    y = high_shelf(y, freq=3500.0, gain_db=4.5)
    y = comb_reverb(y, wet=0.34)
    y = fade_edges(y, attack=0.002, release=0.08)
    return y


def norm_rms(y: np.ndarray, rms_db=RMS_DB, peak_db=PEAK_DB) -> np.ndarray:
    """Match medieval_runner/au.py: RMS over samples above 5% of peak, then peak cap."""
    m = np.abs(y)
    pk = m.max()
    act = y[m > pk * 0.05] if (m > pk * 0.05).any() else y
    r = np.sqrt((act ** 2).mean())
    g = 10 ** (rms_db / 20) / max(r, 1e-9)
    if np.abs(y).max() * g > 10 ** (peak_db / 20):
        g *= 10 ** (peak_db / 20) / (np.abs(y).max() * g)
    return (y * g).astype(np.float32)


def write_wav(path: str, y: np.ndarray) -> None:
    pcm = (np.clip(y, -1.0, 1.0) * 32767.0).astype(np.int16)
    subprocess.run(
        [
            "ffmpeg", "-v", "error", "-y",
            "-f", "s16le", "-ar", str(SR), "-ac", "1", "-i", "-",
            "-c:a", "pcm_s16le", path,
        ],
        input=pcm.tobytes(),
        check=True,
    )


def write_ogg(path: str, y: np.ndarray, quality=5) -> None:
    subprocess.run(
        [
            "ffmpeg", "-v", "error", "-y",
            "-f", "f32le", "-ar", str(SR), "-ac", "1", "-i", "-",
            "-map_metadata", "-1", "-fflags", "+bitexact",
            "-c:a", "libvorbis", "-q:a", str(quality), path,
        ],
        input=y.astype(np.float32).tobytes(),
        check=True,
    )


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--ogg", default=DEFAULT_OGG, help="Output OGG path (theme-pack sfx_lyra_shot.ogg)")
    p.add_argument("--preview", default="", help="Also write a 16-bit mono WAV preview")
    args = p.parse_args(argv)
    y = norm_rms(assemble())
    peak = float(np.max(np.abs(y)))
    rms = float(np.sqrt(np.mean(y ** 2)))
    if peak >= 0.999:
        raise SystemExit(f"clipping: peak={peak:.4f}")
    os.makedirs(os.path.dirname(os.path.abspath(args.ogg)) or ".", exist_ok=True)
    write_ogg(args.ogg, y)
    if args.preview:
        write_wav(args.preview, y)
    print(
        f"wrote {args.ogg}  dur={len(y)/SR:.3f}s  "
        f"peak={20*np.log10(max(peak,1e-12)):.2f} dBFS  "
        f"rms={20*np.log10(max(rms,1e-12)):.2f} dBFS"
        + (f"  preview={args.preview}" if args.preview else "")
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
