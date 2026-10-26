#!/usr/bin/env python3
"""Procedural sound set for ThemePack fantasy_kingdom_a (Kingdom Defense).

Every clip in Assets/ThemePack/fantasy_kingdom_a/audio is synthesised here from oscillators, filtered noise and
simple physical models (plucked string, struck bar/bell modes). No samples or recordings are used, so the output is
original work, released by the project under CC0 1.0 (see the LICENSES.md next to the clips).

Needs python3 + numpy + scipy, and ffmpeg with libvorbis for the .ogg encode.
Usage:  python3 scripts/audio/gen_theme_a_audio.py [out_dir]
The output is deterministic (fixed seeds), so re-running reproduces the same clips.

Loops are rendered circularly: every event is added modulo the loop length and noise beds / reverb are filtered with
circular (FFT) convolution, so the end of each loop flows into its start with no seam.
"""
import os
import subprocess
import sys
import tempfile

import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Assets", "ThemePack", "fantasy_kingdom_a", "audio")
rng = np.random.default_rng(20261026)


# ---------------------------------------------------------------- building blocks

def ns(dur):
    return int(round(dur * SR))


def t_axis(dur):
    return np.arange(ns(dur)) / SR


def noise(dur):
    return rng.standard_normal(int(round(dur * SR)))


def env_exp(dur, decay, attack=0.002):
    t = t_axis(dur)
    e = np.exp(-t / max(decay, 1e-4))
    if attack > 0:
        e *= np.clip(t / attack, 0, 1)
    return e


def env_adsr(dur, a, d, s, r):
    n = ns(dur)
    t = np.arange(n) / SR
    e = np.where(t < a, t / max(a, 1e-5), s + (1 - s) * np.exp(-(t - a) / max(d, 1e-5)))
    rel = int(r * SR)
    if rel > 0 and rel < n:
        e[-rel:] *= np.linspace(1, 0, rel) ** 2
    return e


def sweep(f0, f1, dur, curve="exp"):
    t = t_axis(dur)
    if curve == "exp":
        return f0 * (f1 / f0) ** (t / dur)
    return f0 + (f1 - f0) * (t / dur)


def osc(freq, kind="sine", phase=0.0):
    """freq: array of instantaneous Hz."""
    ph = 2 * np.pi * np.cumsum(freq) / SR + phase
    if kind == "sine":
        return np.sin(ph)
    if kind == "saw":
        return 2 * ((ph / (2 * np.pi)) % 1.0) - 1
    if kind == "square":
        return np.sign(np.sin(ph))
    if kind == "tri":
        return 2 * np.abs(2 * ((ph / (2 * np.pi)) % 1.0) - 1) - 1
    raise ValueError(kind)


def tone(f, dur, kind="sine"):
    return osc(np.full(int(round(dur * SR)), float(f)), kind)


def lp(x, fc, order=2):
    sos = signal.butter(order, min(fc, SR * 0.45), "low", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def hp(x, fc, order=2):
    sos = signal.butter(order, fc, "high", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def bp(x, lo, hi, order=2):
    sos = signal.butter(order, [lo, min(hi, SR * 0.45)], "band", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def sweep_bp(x, f0, f1, q=2.0):
    """Band-pass whose centre glides f0 -> f1 (block-wise state-variable filter)."""
    n = len(x)
    fc = f0 * (f1 / f0) ** (np.arange(n) / max(n - 1, 1))
    y = np.zeros(n)
    low = band = 0.0
    damp = 1.0 / q
    for i in range(n):
        f = 2 * np.sin(np.pi * min(fc[i], SR / 6) / SR)
        high = x[i] - low - damp * band
        band += f * high
        low += f * band
        y[i] = band
    return y


def pluck(f, dur, damp=0.996, bright=0.5):
    """Karplus-Strong string."""
    n = int(round(dur * SR))
    p = max(2, int(SR / f))
    buf = rng.uniform(-1, 1, p)
    buf = lp(buf, 1500 + 8000 * bright)
    out = np.zeros(n)
    idx = 0
    for i in range(n):
        out[i] = buf[idx]
        nxt = (idx + 1) % p
        buf[idx] = damp * 0.5 * (buf[idx] + buf[nxt])
        idx = nxt
    tail = max(1, int(n * 0.35))
    out[-tail:] *= np.linspace(1, 0, tail) ** 2
    return out


def modes(freqs, decays, amps, dur):
    """Struck object: damped sinusoids."""
    t = t_axis(dur)
    y = np.zeros_like(t)
    for f, d, a in zip(freqs, decays, amps):
        y += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * np.exp(-t / d)
    return y


def bell(f, dur, decay=1.2, bright=1.0):
    ratios = [0.5, 1.0, 1.19, 1.56, 2.0, 2.51, 2.66, 3.01, 4.1]
    amps = [0.35, 1.0, 0.5, 0.35, 0.45, 0.25, 0.2, 0.15, 0.1 * bright]
    decs = [decay * 1.4, decay, decay * 0.7, decay * 0.6, decay * 0.5, decay * 0.35, decay * 0.3, decay * 0.25, decay * 0.15]
    return modes([f * r for r in ratios], decs, amps, dur)


def chime(f, dur, decay=0.5):
    return modes([f, f * 2.0, f * 3.0, f * 4.2], [decay, decay * 0.5, decay * 0.3, decay * 0.15], [1, 0.4, 0.2, 0.1], dur)


def thump(f0=120, f1=45, dur=0.35, decay=0.12):
    return osc(sweep(f0, f1, dur)) * env_exp(dur, decay, 0.001)


def burst(dur, decay, lo=None, hi=None):
    x = noise(dur) * env_exp(dur, decay, 0.0005)
    if lo and hi:
        return bp(x, lo, hi)
    if hi:
        return lp(x, hi)
    if lo:
        return hp(x, lo)
    return x


def whoosh(dur, f0, f1, q=1.5, peak=0.5):
    t = t_axis(dur)
    e = np.where(t < dur * peak, (t / (dur * peak)) ** 2, ((dur - t) / (dur * (1 - peak))) ** 1.5)
    return sweep_bp(noise(dur), f0, f1, q) * e


def growl(f0, f1, dur, formant=(500, 1200), rough=0.3):
    f = sweep(f0, f1, dur)
    f = f * (1 + 0.04 * np.sin(2 * np.pi * 7 * t_axis(dur))) * (1 + rough * 0.1 * lp(noise(dur), 30))
    src = osc(f, "saw") + 0.3 * noise(dur) * rough
    y = bp(src, formant[0] * 0.6, formant[0] * 1.6) + 0.6 * bp(src, formant[1] * 0.7, formant[1] * 1.4)
    return y


def grunt(f0, f1, dur, vowel=(650, 1100)):
    f = sweep(f0, f1, dur) * (1 + 0.02 * lp(noise(dur), 40))
    src = osc(f, "saw")
    y = bp(src, vowel[0] * 0.8, vowel[0] * 1.25, 2) + 0.7 * bp(src, vowel[1] * 0.85, vowel[1] * 1.2, 2) + 0.15 * bp(src, 2400, 3200, 2)
    return y * env_adsr(dur, 0.015, dur * 0.4, 0.4, dur * 0.4)


def mix(*parts):
    n = max(int(start * SR) + len(p) for start, p in parts)
    y = np.zeros(n)
    for start, p in parts:
        s = int(start * SR)
        e = min(n, s + len(p))
        if s < n:
            y[s:e] += p[: e - s]
    return y


def at(start, x):
    return (start, x)


def pad(x, total):
    n = ns(total)
    return np.pad(x, (0, max(0, n - len(x))))[:n]


def room(x, size=0.25, wet=0.18, tail=0.6):
    """Short convolution reverb (decaying noise IR), returns x + tail."""
    ir = noise(tail) * env_exp(tail, size * 0.6)
    ir = lp(ir, 5000)
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    y = signal.fftconvolve(np.pad(x, (0, len(ir))), ir)[: len(x) + len(ir)]
    return np.pad(x, (0, len(ir))) + wet * y


def fade_tail(x, dur=0.02):
    n = min(len(x), int(dur * SR))
    x = x.copy()
    x[-n:] *= np.linspace(1, 0, n)
    return x


def trim(x, thresh=1e-4):
    nz = np.nonzero(np.abs(x) > thresh * np.max(np.abs(x)))[0]
    if len(nz) == 0:
        return x
    return x[: nz[-1] + 1]


def norm(x, peak=0.89):
    m = np.max(np.abs(x))
    return x * (peak / m) if m > 0 else x


# ---------------------------------------------------------------- circular (loop) helpers

class Loop:
    def __init__(self, seconds, stereo=True):
        self.n = int(round(seconds * SR))
        self.L = np.zeros(self.n)
        self.R = np.zeros(self.n)

    def add(self, start, x, gain=1.0, pan=0.0):
        s = int(round(start * SR)) % self.n
        idx = (s + np.arange(len(x))) % self.n
        gl = gain * np.cos((pan + 1) * np.pi / 4)
        gr = gain * np.sin((pan + 1) * np.pi / 4)
        np.add.at(self.L, idx, x * gl)
        np.add.at(self.R, idx, x * gr)

    def bed(self, x, gain=1.0, pan=0.0):
        assert len(x) == self.n
        self.add(0, x, gain, pan)

    def reverb(self, wet=0.2, decay=0.8, tail=2.0, hi=4500):
        ir_l = lp(noise(tail) * env_exp(tail, decay * 0.5), hi)
        ir_r = lp(noise(tail) * env_exp(tail, decay * 0.5), hi)
        for ir, ch in ((ir_l, "L"), (ir_r, "R")):
            ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
            k = np.zeros(self.n)
            k[: len(ir)] = ir[: self.n]
            src = (self.L + self.R) * 0.5
            wetsig = np.real(np.fft.ifft(np.fft.fft(src) * np.fft.fft(k)))
            setattr(self, ch, getattr(self, ch) + wet * wetsig)

    def stereo(self):
        return np.stack([self.L, self.R], axis=1)


def circ_noise(n, lo=None, hi=None, tilt=0.0, seed=None):
    """Noise that is exactly periodic over n samples, band-shaped in the frequency domain."""
    g = np.random.default_rng(seed) if seed is not None else rng
    spec = np.fft.rfft(g.standard_normal(n))
    f = np.fft.rfftfreq(n, 1 / SR)
    shape = np.ones_like(f)
    with np.errstate(divide="ignore"):
        if tilt:
            shape *= np.where(f > 0, (np.maximum(f, 20) / 1000.0) ** (-tilt), 0)
    if lo:
        shape *= 1 / np.sqrt(1 + (lo / np.maximum(f, 1)) ** 4)
    if hi:
        shape *= 1 / np.sqrt(1 + (f / hi) ** 4)
    y = np.fft.irfft(spec * shape, n)
    return y / (np.std(y) + 1e-12)


def periodic_lfo(n, periods, seed=None, depth=1.0):
    """Smooth 0..1 modulation built from cosines whose cycle counts divide the loop exactly."""
    g = np.random.default_rng(seed) if seed is not None else rng
    t = np.arange(n) / n
    y = np.zeros(n)
    for k in periods:
        y += np.cos(2 * np.pi * k * t + g.uniform(0, 6.28)) / k ** 0.5
    y = (y - y.min()) / (y.max() - y.min() + 1e-12)
    return 1 - depth + depth * y


# ---------------------------------------------------------------- instruments (music)

def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7, "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def hz(name):
    letter = name[:-1]
    octave = int(name[-1])
    return midi(12 * (octave + 1) + NOTE[letter])


def kick(dur=0.45, big=1.0):
    body = osc(sweep(130 * big ** 0.3, 42, dur)) * env_exp(dur, 0.16 * big, 0.001)
    click = burst(0.02, 0.004, 1000, 6000) * 0.3
    return pad(body + pad(click, dur), dur)


def taiko(dur=0.9, f=72):
    body = osc(sweep(f * 1.35, f * 0.8, dur)) * env_exp(dur, 0.28, 0.001)
    skin = lp(noise(dur), 900) * env_exp(dur, 0.05, 0.001) * 0.6
    return body + skin


def snare(dur=0.25, tight=1.0):
    n = bp(noise(dur), 1200, 8000) * env_exp(dur, 0.07 * tight, 0.0005)
    t = osc(sweep(240, 180, dur)) * env_exp(dur, 0.04, 0.0005) * 0.6
    return n + t


def tom(f, dur=0.5):
    return osc(sweep(f * 1.3, f * 0.85, dur)) * env_exp(dur, 0.16, 0.001) + 0.2 * lp(noise(dur), 1500) * env_exp(dur, 0.03)


def crash(dur=2.0):
    x = hp(noise(dur), 4500) * env_exp(dur, 0.6, 0.003)
    x += 0.3 * modes([3200, 4700, 5900, 7300], [0.6, 0.5, 0.4, 0.3], [1, 0.8, 0.6, 0.5], dur)
    return x


def saws(f, dur, detune=(0.0, -0.08, 0.07), cutoff=2500):
    y = sum(osc(np.full(ns(dur), f * 2 ** (d / 12)), "saw") for d in detune) / len(detune)
    return lp(y, cutoff, 2)


def bass(f, dur, cutoff=700):
    y = osc(np.full(ns(dur), f), "saw") * 0.7 + 0.5 * tone(f, dur)
    return lp(y, cutoff, 2) * env_adsr(dur, 0.005, 0.12, 0.6, 0.04)


def string_stac(f, dur):
    return saws(f, dur, cutoff=3000) * env_adsr(dur, 0.008, 0.08, 0.25, min(0.05, dur * 0.5))


def brass(chord, dur, bright=1.0):
    y = sum(saws(hz(n), dur, cutoff=1400 * bright) + 0.6 * saws(hz(n), dur, cutoff=3500 * bright) * env_exp(dur, 0.12) for n in chord)
    return y / len(chord) * env_adsr(dur, 0.03, 0.25, 0.55, min(0.25, dur * 0.4))


def pad_choir(chord, dur):
    t = t_axis(dur)
    y = np.zeros_like(t)
    for n in chord:
        for d in (-0.1, 0.1):
            f = hz(n) * 2 ** (d / 12) * (1 + 0.004 * np.sin(2 * np.pi * 5.2 * t + rng.uniform(0, 6)))
            y += osc(f, "saw")
    y = bp(y, 400, 900) + 0.5 * bp(y, 1000, 1600)
    return y / len(chord) * env_adsr(dur, dur * 0.3, dur, 1.0, dur * 0.3)


# ---------------------------------------------------------------- loops

def village_ambience():
    sec = 32.0
    lo = Loop(sec)
    n = lo.n
    # wind: two decorrelated low noise beds with slow periodic gusts
    gust = periodic_lfo(n, [1, 2, 3, 5], seed=1, depth=0.75)
    for side, seed in ((-0.6, 11), (0.6, 12)):
        w = circ_noise(n, lo=60, hi=700, tilt=0.6, seed=seed)
        lo.bed(w * gust * 0.05, pan=side)
    leaves = circ_noise(n, lo=2500, hi=9000, seed=13) * (gust ** 3) * 0.012
    lo.bed(leaves, pan=0.3)
    # distant town murmur (heavily low-passed "voices") and a far smithy
    murmur = np.zeros(n)
    for v in range(5):
        syl = circ_noise(n, hi=5, seed=40 + v)
        syl = np.clip(syl, 0, None) ** 1.5
        voice = circ_noise(n, lo=250, hi=1100, seed=50 + v)
        murmur += voice * syl
    murmur = murmur / (np.std(murmur) + 1e-9)
    lo.bed(murmur * 0.006 * periodic_lfo(n, [1, 2], seed=3, depth=0.5), pan=-0.2)
    for start in (3.1, 3.55, 4.0, 14.2, 14.6, 25.4, 25.8, 26.25):
        clink = modes([1850, 2900, 4100], [0.25, 0.15, 0.08], [1, 0.5, 0.3], 0.6)
        lo.add(start, lp(clink, 2500), 0.012, pan=0.55)
    # a far bell, once per loop
    lo.add(9.0, lp(bell(392, 5.0, decay=1.8), 2500), 0.03, pan=-0.4)
    lo.add(10.4, lp(bell(330, 5.0, decay=1.8), 2500), 0.022, pan=-0.4)
    # birds
    def chirp_up(f0):
        d = rng.uniform(0.05, 0.09)
        return osc(sweep(f0, f0 * rng.uniform(1.3, 1.7), d)) * np.sin(np.pi * t_axis(d) / d) ** 2

    def warble(f0, dur):
        t = t_axis(dur)
        f = f0 * (1 + 0.12 * np.sin(2 * np.pi * rng.uniform(18, 28) * t)) * (1 + 0.15 * t / dur)
        return osc(f) * np.sin(np.pi * t / dur) ** 1.5

    def whistle(f0):
        a = osc(sweep(f0, f0 * 0.98, 0.18)) * np.sin(np.pi * t_axis(0.18) / 0.18)
        b = osc(sweep(f0 * 0.8, f0 * 0.78, 0.22)) * np.sin(np.pi * t_axis(0.22) / 0.22)
        return mix(at(0, a), at(0.26, b))

    t = 0.4
    while t < sec - 0.2:
        kind = rng.integers(0, 3)
        pan = rng.uniform(-0.9, 0.9)
        if kind == 0:
            f0 = rng.uniform(2800, 4200)
            for k in range(rng.integers(3, 7)):
                lo.add(t + k * rng.uniform(0.09, 0.13), chirp_up(f0 * rng.uniform(0.95, 1.05)), 0.035, pan)
        elif kind == 1:
            lo.add(t, warble(rng.uniform(2500, 3800), rng.uniform(0.5, 1.1)), 0.025, pan)
        else:
            lo.add(t, whistle(rng.uniform(1900, 2600)), 0.03, pan)
        t += rng.uniform(1.2, 3.2)
    lo.reverb(wet=0.25, decay=1.2, tail=2.5)
    return lo.stereo(), sec


def runner_march():
    bpm = 112
    beat = 60 / bpm
    bars = 8
    sec = bars * 4 * beat
    lo = Loop(sec)
    prog = [("D", ["D4", "F4", "A4"]), ("D", ["D4", "F4", "A4"]), ("Bb", ["D4", "F4", "Bb4"]), ("Bb", ["D4", "F4", "Bb4"]),
            ("G", ["D4", "G4", "Bb4"]), ("G", ["D4", "G4", "Bb4"]), ("A", ["C#4", "E4", "A4"]), ("A", ["C#4", "E4", "A4"])]
    snare_pat = [1.0, 0, 0.3, 0.5, 0.9, 0, 0.3, 0.3, 1.0, 0, 0.3, 0.5, 0.8, 0.5, 0.6, 0.7]
    for b, (root, chord) in enumerate(prog):
        t0 = b * 4 * beat
        lo.add(t0, kick(0.5), 0.55)
        lo.add(t0 + 2 * beat, kick(0.5), 0.45)
        if b % 2 == 0:
            lo.add(t0, taiko(1.0, 62), 0.5, -0.1)
        for i, v in enumerate(snare_pat):
            if v > 0:
                lo.add(t0 + i * beat / 4, snare(0.22, 0.8 if v < 0.6 else 1.0), 0.18 * v, 0.15)
        rootf = hz(root + "2")
        pattern = [1, 1, 2, 1, 1, 1.5, 1, 1.335]
        for i, m in enumerate(pattern):
            lo.add(t0 + i * beat / 2, bass(rootf * m if m != 1.5 else rootf * 1.5, beat / 2 * 0.9), 0.32)
        arp = chord + [chord[1]]
        for i in range(8):
            lo.add(t0 + i * beat / 2, string_stac(hz(arp[i % 4]), beat / 2 * 0.6), 0.09, -0.35 + 0.1 * (i % 2))
        if b % 2 == 0:
            lo.add(t0, brass([chord[0][:-1] + "3"] + chord, beat * 3.2), 0.16, 0.25)
    lo.add(0, crash(2.2), 0.06, 0.4)
    lo.reverb(wet=0.22, decay=0.7, tail=1.6)
    return lo.stereo(), sec


def battle_loop():
    bpm = 140
    beat = 60 / bpm
    bars = 8
    sec = bars * 4 * beat
    lo = Loop(sec)
    prog = [("E", ["E4", "G4", "B4"]), ("E", ["E4", "G4", "B4"]), ("C", ["E4", "G4", "C5"]), ("D", ["D4", "F#4", "A4"]),
            ("E", ["E4", "G4", "B4"]), ("E", ["E4", "G4", "B4"]), ("A", ["E4", "A4", "C5"]), ("B", ["D#4", "F#4", "B4"])]
    melody = ["E5", "G5", "F#5", "D5", "E5", "B4", "C5", "D#5"]
    for b, (root, chord) in enumerate(prog):
        t0 = b * 4 * beat
        for q in range(4):
            lo.add(t0 + q * beat, kick(0.4), 0.5)
        lo.add(t0 + beat, snare(0.25), 0.3, 0.1)
        lo.add(t0 + 3 * beat, snare(0.25), 0.3, 0.1)
        for s in range(16):
            if s % 4 != 0 and (s % 4 == 1 or s % 4 == 3):
                lo.add(t0 + s * beat / 4, tom(110 if s % 8 < 4 else 90, 0.3), 0.14, -0.3 + 0.6 * (s % 2))
        if b % 4 == 0:
            lo.add(t0, crash(2.0), 0.08, -0.4)
            lo.add(t0, taiko(1.2, 55), 0.55)
        rootf = hz(root + "2")
        for s in range(16):
            gallop = s % 4 != 1
            if gallop:
                lo.add(t0 + s * beat / 4, bass(rootf, beat / 4 * 0.85, 900), 0.28)
        arp = [chord[0], chord[1], chord[2], chord[1]]
        for s in range(16):
            lo.add(t0 + s * beat / 4, string_stac(hz(arp[s % 4]) * (2 if s >= 8 else 1), beat / 4 * 0.6), 0.07, 0.35 - 0.1 * (s % 2))
        lo.add(t0, brass([melody[b][:-1] + str(int(melody[b][-1]) - 1), melody[b]], beat * 3.6, 1.2), 0.18, -0.2)
        lo.add(t0, pad_choir([chord[0][:-1] + "3", chord[1][:-1] + "3", chord[2][:-1] + "3"], beat * 4.2), 0.08, 0.0)
    lo.reverb(wet=0.22, decay=0.8, tail=1.8)
    return lo.stereo(), sec


def to_stereo_oneshot(x, width=0.0):
    return np.stack([x, x], axis=1)


def boss_sting():
    dur = 4.2
    hit = taiko(2.0, 48) * 1.2 + pad(kick(1.0, 2.0), 2.0) * 0.8
    sub = tone(36, 2.5) * env_exp(2.5, 0.9, 0.01) * 0.6
    cluster = brass(["D3", "Eb3", "A3", "D4", "Eb4"], 3.0, 1.3) * 1.2
    gong = lp(bell(98, 4.0, decay=1.6), 3000) * 0.5
    riser = whoosh(0.7, 300, 3000, 1.2, 0.95) * 0.5
    y = mix(at(0, riser), at(0.65, hit), at(0.65, sub), at(0.65, cluster), at(0.7, gong))
    y = room(pad(y, dur), 0.6, 0.25, 1.5)
    return to_stereo_oneshot(fade_tail(trim(y)))


def jingle_win():
    notes = [("C5", 0.0), ("E5", 0.13), ("G5", 0.26), ("C6", 0.42)]
    parts = []
    for n, s in notes:
        parts.append(at(s, brass([n], 0.5 if n != "C6" else 1.4, 1.4)))
        parts.append(at(s, chime(hz(n), 1.2, 0.5) * 0.4))
    parts.append(at(0.42, brass(["C4", "E4", "G4", "C5"], 1.5, 1.2) * 0.8))
    parts.append(at(0.42, hp(noise(1.6), 6000) * env_exp(1.6, 0.4) * 0.08))
    y = room(mix(*parts), 0.4, 0.2, 1.2)
    return to_stereo_oneshot(fade_tail(trim(y)))


def jingle_lose():
    notes = [("A4", 0.0, 0.42), ("F4", 0.4, 0.42), ("D4", 0.8, 0.42), ("C#4", 1.2, 1.4)]
    parts = [at(s, brass([n], d, 0.8)) for n, s, d in notes]
    parts.append(at(1.2, brass(["D3", "F3", "A3"], 1.6, 0.6) * 0.7))
    parts.append(at(1.2, taiko(1.2, 50) * 0.5))
    y = room(mix(*parts), 0.5, 0.25, 1.4)
    return to_stereo_oneshot(fade_tail(trim(y)))


# ---------------------------------------------------------------- one-shots (mono)

def sfx():
    S = {}
    # heroes
    blast = osc(sweep(900, 2400, 0.12)) * env_exp(0.12, 0.05) * 0.4
    shimmer = hp(noise(0.35), 3000) * env_exp(0.35, 0.09) * 0.3 + 0.25 * modes([2100, 3150, 4720], [0.12, 0.09, 0.06], [1, 0.7, 0.5], 0.35)
    S["sfx_lyra_shot"] = mix(at(0, thump(150, 42, 0.4, 0.09) * 1.2), at(0, burst(0.25, 0.04, None, 1800) * 0.9), at(0, blast), at(0.01, shimmer))
    twang = pluck(196, 0.45, 0.993, 0.8) * 0.8
    S["sfx_rowan_shot"] = mix(at(0, burst(0.01, 0.002, 2000, 8000) * 0.4), at(0, twang), at(0.02, whoosh(0.32, 2500, 700, 1.4, 0.25) * 0.9))
    snap = modes([620, 1350, 2300], [0.05, 0.03, 0.02], [1, 0.6, 0.4], 0.15)
    rustle = bp(noise(0.3), 2000, 7000) * env_exp(0.3, 0.07) * (1 + 0.5 * np.sin(2 * np.pi * 45 * t_axis(0.3)))
    S["sfx_oakenshield_throw"] = mix(at(0, snap * 0.9), at(0, rustle * 0.35), at(0.03, whoosh(0.28, 1600, 500, 1.6, 0.3) * 0.8))
    # combat
    S["sfx_enemy_hit"] = mix(at(0, thump(160, 60, 0.2, 0.05)), at(0, burst(0.12, 0.025, 200, 2500) * 0.7), at(0, burst(0.04, 0.006, 2500, 7000) * 0.2))
    poof = whoosh(0.5, 900, 250, 1.2, 0.15) * 0.6
    S["sfx_enemy_death"] = mix(at(0, thump(140, 45, 0.35, 0.1)), at(0, growl(170, 70, 0.55, (450, 1100)) * env_adsr(0.55, 0.01, 0.2, 0.5, 0.3) * 0.5), at(0.05, poof))
    S["sfx_enemy_death_big"] = room(mix(at(0, kick(1.0, 2.0)), at(0, growl(120, 45, 1.4, (380, 900), 0.6) * env_adsr(1.4, 0.03, 0.5, 0.6, 0.7) * 0.7),
                                         at(0.1, lp(noise(1.6), 500) * env_exp(1.6, 0.5) * 0.4), at(0.2, whoosh(1.2, 700, 150, 1.0, 0.2) * 0.5)), 0.5, 0.2, 1.0)
    hiss = bp(noise(0.45), 1500, 6000) * env_adsr(0.45, 0.04, 0.15, 0.5, 0.2) * 0.3
    S["sfx_enemy_projectile"] = mix(at(0, osc(sweep(1400, 700, 0.4)) * env_adsr(0.4, 0.02, 0.2, 0.4, 0.15) * 0.25), at(0, hiss), at(0, whoosh(0.4, 600, 2000, 1.5, 0.6) * 0.6))
    S["sfx_crate_hit"] = mix(at(0, modes([310, 720, 1530], [0.06, 0.035, 0.02], [1, 0.6, 0.3], 0.15)), at(0, burst(0.03, 0.005, 800, 5000) * 0.3))
    S["sfx_hero_hurt"] = mix(at(0, thump(130, 55, 0.2, 0.05) * 0.8), at(0, burst(0.08, 0.02, 200, 3000) * 0.5), at(0.01, grunt(160, 120, 0.22) * 0.5))
    S["sfx_hero_death"] = room(mix(at(0, thump(110, 40, 0.5, 0.15)), at(0.01, grunt(170, 85, 0.9, (600, 1000)) * 0.6), at(0.35, lp(noise(0.8), 400) * env_exp(0.8, 0.2) * 0.5),
                                    at(0.3, brass(["D3", "F3"], 1.2, 0.5) * 0.25)), 0.4, 0.2, 0.8)
    S["sfx_follower_down"] = mix(at(0, thump(180, 70, 0.2, 0.05) * 0.7), at(0, osc(sweep(700, 250, 0.35)) * env_exp(0.35, 0.12) * 0.35), at(0.02, whoosh(0.3, 1500, 400, 1.2, 0.2) * 0.4))
    # runner pickups / obstacles
    crack = mix(at(0, burst(0.08, 0.012, 500, 6000)), at(0, modes([280, 560, 1240, 2100], [0.08, 0.05, 0.03, 0.02], [1, 0.7, 0.5, 0.3], 0.2)))
    pop = osc(sweep(500, 1500, 0.09)) * env_exp(0.09, 0.04) * 0.6
    sparkle = sum(chime(f, 0.5, 0.18) for f in (1568, 2093, 2637)) * 0.2
    S["sfx_pickup_pop"] = mix(at(0, crack * 0.8), at(0.02, pop), at(0.06, sparkle))
    up = [at(i * 0.07, chime(f, 0.6, 0.25) * 0.5) for i, f in enumerate((523, 659, 784, 1047, 1319))]
    S["sfx_powerup"] = room(mix(*up, at(0, whoosh(0.5, 500, 4000, 1.0, 0.85) * 0.4), at(0.3, hp(noise(0.5), 5000) * env_exp(0.5, 0.15) * 0.1)), 0.3, 0.2, 0.6)
    S["sfx_follower_join"] = mix(at(0, chime(784, 0.4, 0.15) * 0.6), at(0.08, chime(1175, 0.4, 0.18) * 0.6), at(0, pop * 0.5))
    metal = modes([1730, 2760, 4100, 5650], [0.15, 0.1, 0.07, 0.05], [1, 0.7, 0.5, 0.4], 0.35)
    S["sfx_hazard_spikes"] = mix(at(0, burst(0.02, 0.003, 2000, 9000) * 0.5), at(0, metal * 0.6), at(0, thump(200, 80, 0.15, 0.04) * 0.5))
    rollrum = lp(noise(0.6), 300) * env_adsr(0.6, 0.01, 0.2, 0.4, 0.3) * 0.5
    S["sfx_hazard_log"] = mix(at(0, kick(0.5, 1.4)), at(0, modes([140, 330, 610], [0.12, 0.08, 0.05], [1, 0.6, 0.3], 0.3) * 0.6), at(0.05, rollrum))
    crackles = np.zeros(ns(0.7))
    for _ in range(18):
        s = int(rng.uniform(0, 0.6) * SR)
        c = burst(0.006, 0.0015, 1500, 9000)
        crackles[s:s + len(c)] += c[: len(crackles) - s] * rng.uniform(0.2, 0.6)
    roar = lp(noise(0.7), 1200) * env_adsr(0.7, 0.04, 0.2, 0.5, 0.35)
    S["sfx_hazard_fire"] = mix(at(0, whoosh(0.6, 300, 1500, 0.9, 0.2) * 0.8), at(0, roar * 0.5), at(0, crackles * 0.8))
    # boss
    rumble = lp(noise(2.2), 140, 4) * 2.5 + tone(34, 2.2) * 0.4 * (1 + 0.3 * np.sin(2 * np.pi * 6 * t_axis(2.2)))
    debris = np.zeros(ns(2.2))
    for _ in range(25):
        s = int(rng.uniform(0.1, 1.9) * SR)
        c = modes([rng.uniform(600, 2500)], [0.02], [1], 0.06)
        debris[s:s + len(c)] += c[: len(debris) - s] * rng.uniform(0.05, 0.15)
    S["sfx_boss_quake_rumble"] = (rumble + debris) * env_adsr(2.2, 0.25, 0.8, 0.7, 0.8)
    horn = lambda f, d: lp(osc(np.full(ns(d), f), "saw") + 0.5 * osc(np.full(ns(d), f * 1.5), "square"), 2500) * env_adsr(d, 0.01, 0.1, 0.8, 0.05)
    S["sfx_boss_dodge_warning"] = mix(at(0, horn(587, 0.18) * 0.4), at(0.22, horn(587, 0.18) * 0.4), at(0.44, horn(784, 0.35) * 0.45))
    S["sfx_ironhowl_growl"] = growl(95, 75, 0.9, (350, 800), 0.7) * env_adsr(0.9, 0.12, 0.4, 0.7, 0.3) * 0.8
    meat = mix(at(0, thump(120, 40, 0.4, 0.1)), at(0, burst(0.15, 0.03, 150, 2000) * 0.8))
    S["sfx_ironhowl_punch"] = mix(at(0, whoosh(0.2, 400, 1200, 1.2, 0.8) * 0.6), at(0.17, meat))
    S["sfx_ironhowl_ground_pound"] = room(mix(at(0, kick(1.2, 2.5) * 1.2), at(0, lp(noise(1.4), 250, 4) * env_exp(1.4, 0.4) * 2.0),
                                               at(0.05, debris[: int(1.2 * SR)] * 1.5), at(0, burst(0.08, 0.02, 300, 3000) * 0.6)), 0.6, 0.2, 1.0)
    S["sfx_ironhowl_sweep"] = mix(at(0, whoosh(0.55, 250, 1600, 1.0, 0.7) * 1.0), at(0.45, meat * 0.8))
    steps = [at(i * 0.11, kick(0.25, 0.8) * (0.3 + 0.1 * i)) for i in range(5)]
    S["sfx_ironhowl_charge"] = mix(*steps, at(0.1, growl(110, 150, 0.5, (400, 900), 0.6) * env_adsr(0.5, 0.05, 0.2, 0.6, 0.2) * 0.4), at(0.55, meat * 1.1))
    # battle impacts
    S["sfx_magic_impact"] = mix(at(0, thump(200, 70, 0.25, 0.06) * 0.6), at(0, hp(noise(0.4), 2500) * env_exp(0.4, 0.08) * 0.35),
                                at(0, modes([1320, 1980, 2970], [0.2, 0.15, 0.1], [1, 0.6, 0.4], 0.4) * 0.25), at(0, whoosh(0.3, 3000, 800, 1.2, 0.1) * 0.4))
    S["sfx_sword_hit"] = mix(at(0, modes([2350, 3480, 5100, 6900], [0.2, 0.15, 0.1, 0.07], [1, 0.7, 0.5, 0.3], 0.45) * 0.4),
                             at(0, burst(0.05, 0.008, 1500, 9000) * 0.6), at(0, thump(180, 70, 0.15, 0.04) * 0.5))
    S["sfx_arrow_hit"] = mix(at(0, modes([420, 950], [0.05, 0.03], [1, 0.5], 0.12)), at(0, burst(0.05, 0.01, 300, 3000) * 0.6), at(0.0, thump(170, 80, 0.1, 0.03) * 0.5))
    # UI / economy
    S["ui_tap"] = mix(at(0, modes([1250, 2600], [0.02, 0.01], [1, 0.4], 0.06)), at(0, osc(sweep(900, 1300, 0.04)) * env_exp(0.04, 0.012) * 0.5))
    buzz = lambda d: lp(osc(np.full(ns(d), 140.0), "square"), 900) * env_adsr(d, 0.005, 0.05, 0.7, 0.02)
    S["ui_deny"] = mix(at(0, buzz(0.09) * 0.5), at(0.12, buzz(0.11) * 0.5))
    coin = lambda f: modes([f, f * 2.76, f * 5.4], [0.25, 0.12, 0.06], [1, 0.5, 0.3], 0.5)
    S["ui_store_open"] = room(mix(at(0, whoosh(0.3, 500, 2500, 1.2, 0.7) * 0.5), at(0.12, modes([190, 260], [0.1, 0.08], [1, 0.5], 0.3) * 0.4),
                                  at(0.22, coin(2200) * 0.4), at(0.3, coin(2600) * 0.35), at(0.36, chime(1568, 0.6, 0.3) * 0.3)), 0.3, 0.2, 0.6)
    arp = [at(i * 0.06, chime(f, 0.8, 0.35) * 0.5) for i, f in enumerate((784, 988, 1175, 1568, 1976, 2349))]
    S["ui_gift_celebrate"] = room(mix(*arp, at(0.36, brass(["G4", "B4", "D5", "G5"], 1.0, 1.5) * 0.45),
                                      at(0.3, hp(noise(1.0), 5000) * env_exp(1.0, 0.3) * 0.12), at(0, pop * 0.6)), 0.4, 0.2, 0.8)
    S["collect_food"] = mix(at(0, bp(noise(0.25), 1500, 6000) * env_exp(0.25, 0.06) * 0.4), at(0.03, pluck(523, 0.3, 0.99, 0.6) * 0.5),
                            at(0.1, pluck(784, 0.35, 0.99, 0.6) * 0.5), at(0.03, pop * 0.4))
    coins = [at(i * rng.uniform(0.035, 0.06) + 0.0, coin(rng.uniform(2000, 3200)) * rng.uniform(0.25, 0.5)) for i in range(9)]
    S["collect_gold"] = mix(*coins)
    S["collect_resource"] = mix(at(0, modes([280, 610, 1180], [0.06, 0.04, 0.02], [1, 0.6, 0.3], 0.18) * 0.8), at(0.1, modes([330, 700, 1400], [0.06, 0.04, 0.02], [1, 0.6, 0.3], 0.18) * 0.7),
                                at(0, burst(0.03, 0.005, 1000, 6000) * 0.25))
    hammer = lambda: mix(at(0, modes([1600, 2450, 3900], [0.12, 0.08, 0.05], [1, 0.6, 0.4], 0.3) * 0.5), at(0, burst(0.02, 0.004, 1000, 7000) * 0.4), at(0, thump(220, 120, 0.1, 0.03) * 0.4))
    S["build_complete"] = room(mix(at(0, hammer()), at(0.16, hammer()), at(0.32, hammer() * 1.2), at(0.5, brass(["C4", "E4", "G4", "C5"], 1.1, 1.3) * 0.5),
                                   at(0.5, sum(chime(f, 0.9, 0.4) for f in (1047, 1319, 1568)) * 0.2)), 0.4, 0.2, 0.8)
    saw_strokes = [at(0.2 + i * 0.14, bp(noise(0.12), 2000, 6000) * np.sin(np.pi * t_axis(0.12) / 0.12) * 0.25) for i in range(3)]
    S["upgrade_start"] = mix(at(0, hammer()), at(0.12, hammer() * 0.8), *saw_strokes)
    S["purchase_success"] = room(mix(at(0, coin(2400) * 0.5), at(0.05, coin(3000) * 0.4), at(0.1, coin(2700) * 0.4),
                                     at(0.18, chime(1319, 0.8, 0.35) * 0.5), at(0.28, chime(1760, 0.9, 0.4) * 0.5)), 0.3, 0.2, 0.6)
    return S


# ---------------------------------------------------------------- write

def write_ogg(name, data, quality):
    path = os.path.join(OUT, name + ".ogg")
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        wavfile.write(tmp.name, SR, (np.clip(data, -1, 1) * 32767).astype(np.int16))
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", tmp.name, "-map_metadata", "-1", "-fflags", "+bitexact",
                        "-c:a", "libvorbis", "-q:a", str(quality), path], check=True)
    os.unlink(tmp.name)
    return path


def loop_norm(st, rms_db):
    rms = np.sqrt(np.mean(st ** 2))
    st = st * (10 ** (rms_db / 20) / (rms + 1e-12))
    peak = np.max(np.abs(st))
    return st * (0.9 / peak) if peak > 0.9 else st


def main():
    os.makedirs(OUT, exist_ok=True)
    loops = {
        "amb_village_loop": (village_ambience, -24),
        "mus_runner_march_loop": (runner_march, -19),
        "mus_battle_loop": (battle_loop, -18),
    }
    for name, (fn, db) in loops.items():
        st, sec = fn()
        write_ogg(name, loop_norm(st, db), 2)
        print(f"{name}: {sec:.3f}s loop")
    for name, fn in (("sting_boss_incoming", boss_sting), ("jingle_win", jingle_win), ("jingle_lose", jingle_lose)):
        st = fn()
        write_ogg(name, norm(st, 0.89), 3)
        print(name)
    for name, x in sfx().items():
        x = fade_tail(trim(np.asarray(x, dtype=float)), 0.01)
        write_ogg(name, norm(x, 0.89), 3)
        print(name)


if __name__ == "__main__":
    main()
