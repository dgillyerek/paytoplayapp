"""Runner / battle attack SFX for Theme A (fantasy_kingdom_a): layered, round-robin variants.

Every sound = swing/whoosh + impact (flesh, armour, wood or magic, by weapon) + short tail (convolution reverb with a
synthesized room impulse). Layers are CC0 recordings (see LICENSES.md / Docs/audio-credits.md) plus original synthesis
(numpy/scipy: swept-band noise whooshes, inharmonic metal partials, arcane shimmer, thumps, crackles).

Rebuild:  AUDIO_SRC=<folder with the CC0 packs unpacked as x_*/kenney_*> python3 build_attack_sfx.py [out_dir]
Fixed seeds, so a rebuild with the same packs gives the same files. Output: mono 44.1 kHz Ogg Vorbis, peak <= -3 dBFS.
"""
import os, sys
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve, stft, istft

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from au import SR, load, trim, tail_trim, highpass, lowpass, write  # noqa: E402

SRC = os.environ.get('AUDIO_SRC', '.')
OUT = sys.argv[1] if len(sys.argv) > 1 else 'out_attack'
os.makedirs(OUT, exist_ok=True)

M = 'x_medieval/'; K = 'kenney_impact-sounds/Audio/'; KR = 'kenney_rpg-audio/Audio/'
C1 = 'x_80-CC0-creature-SFX_0/'; C2 = 'x_80-CC0-creature-sfx-2/'; R = 'x_80-CC0-RPG-SFX_0/'
B = 'x_sfx_breaking_and_falling/'; W = 'x_100-CC0-wood-metal-SFX/'; SC = 'x_sword_clash_-_starninjas_0/'
SN = 'x_sword_-_starninjas_1/sword - StarNinjas/'; TS = 'x_weapons-apparel/sfx/'

PEAK_DB = -4.6  # sample peak before Vorbis; the encoded file decodes with a true peak <= -3 dBFS
_cache = {}


# ---------------------------------------------------------------- basics

def n_(sec):
    return max(1, int(sec * SR))


def src(path, dur=None, rate=1.0, db=-30, fade=0.08, start=None):
    """A CC0 recording, mono, onset-trimmed, optionally re-pitched (rate > 1 = higher, shorter)."""
    key = (path, rate)
    if key not in _cache:
        _cache[key] = load(os.path.join(SRC, path), 1, rate)[:, 0]
    if path.startswith(M) and db == -30:
        db = -16
    x = _cache[key][:, None]
    return trim(x, start=start, dur=dur, db=db, fade=fade)[:, 0]


def place(layers):
    """layers: (signal, offset_seconds, gain_db). Returns the summed mono signal."""
    n = max(int(o * SR) + len(s) for s, o, g in layers)
    out = np.zeros(n, np.float64)
    for s, o, g in layers:
        i = int(o * SR)
        out[i:i + len(s)] += s * (10 ** (g / 20))
    return out


def bp(x, lo, hi, order=2):
    return sosfilt(butter(order, [lo, hi], 'band', fs=SR, output='sos'), x)


def lp(x, fc, order=4):
    return sosfilt(butter(order, fc, 'low', fs=SR, output='sos'), x)


def hp(x, fc, order=2):
    return sosfilt(butter(order, fc, 'high', fs=SR, output='sos'), x)


def env_ad(n, attack, decay_tau, hold=0.0):
    t = np.arange(n) / SR
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    d = np.exp(-np.maximum(t - attack - hold, 0) / decay_tau)
    return a * d


def fade_out(x, sec=0.03):
    f = min(len(x), n_(sec))
    x = x.copy()
    x[-f:] *= np.linspace(1, 0, f) ** 2
    return x


# ---------------------------------------------------------------- synthesis

def whoosh(rng, dur, f_from, f_to, q=1.6, peak_at=0.6, bright=1.0):
    """Swept band-pass noise with a swell-and-release envelope: the air of a swing or a passing projectile."""
    n = n_(dur)
    noise = rng.standard_normal(n + 2048)
    f, t, Z = stft(noise, SR, nperseg=1024, noverlap=768)
    tt = np.clip(t / dur, 0, 1)
    centre = f_from * (f_to / f_from) ** tt  # exponential sweep
    bw = centre / q
    mask = np.exp(-0.5 * ((f[:, None] - centre[None, :]) / bw[None, :]) ** 2)
    if bright != 1.0:
        mask *= (1 + (f[:, None] / 4000.0)) ** (bright - 1)
    _, y = istft(Z * mask, SR, nperseg=1024, noverlap=768)
    y = y[:n]
    tn = np.linspace(0, 1, n)
    e = np.where(tn < peak_at, (tn / peak_at) ** 2.2, np.exp(-(tn - peak_at) / max(1 - peak_at, 1e-3) * 4.0))
    y = y * e
    return y / (np.abs(y).max() + 1e-9)


def partials(rng, f0, ratios, decays, amps, dur, detune=0.003, beat=0.0):
    """Inharmonic struck partials (a blade or plate ringing): each sine decays on its own tau, slightly detuned pairs beat."""
    n = n_(dur)
    t = np.arange(n) / SR
    y = np.zeros(n)
    for r, d, a in zip(ratios, decays, amps):
        f = f0 * r * (1 + rng.uniform(-detune, detune))
        ph = rng.uniform(0, 2 * np.pi)
        tone = np.sin(2 * np.pi * f * t + ph)
        if beat:
            tone = 0.5 * (tone + np.sin(2 * np.pi * f * (1 + beat) * t + ph * 1.3))
        y += a * tone * np.exp(-t / d)
    att = np.clip(t / 0.0015, 0, 1)
    return y * att / (np.abs(y).max() + 1e-9)


def thump(rng, f_hi=130, f_lo=45, dur=0.22, tau=0.07):
    """Body: a sine that drops in pitch, fast decay (the low 'oomph' under an impact)."""
    n = n_(dur)
    t = np.arange(n) / SR
    f = f_lo + (f_hi - f_lo) * np.exp(-t / 0.03)
    ph = 2 * np.pi * np.cumsum(f) / SR
    y = np.sin(ph) * env_ad(n, 0.002, tau)
    return y / (np.abs(y).max() + 1e-9)


def click(rng, dur=0.012, fc=3500):
    n = n_(dur)
    y = rng.standard_normal(n) * np.exp(-np.arange(n) / (n / 5))
    y = bp(y, fc * 0.5, min(fc * 2.0, SR / 2 - 100))
    return y / (np.abs(y).max() + 1e-9)


def shimmer(rng, dur, base, up=1.5, n_par=7, sparkle=0.5, tremolo=11.0):
    """Arcane shimmer: a cluster of bell-like sines gliding up, tremolo, plus scattered high grains (sparkles)."""
    n = n_(dur)
    t = np.arange(n) / SR
    y = np.zeros(n)
    ratios = [1, 1.5, 2.0, 2.52, 3.0, 4.0, 5.04, 6.0][:n_par]
    glide = up ** (t / dur)
    for i, r in enumerate(ratios):
        f = base * r * (1 + rng.uniform(-0.004, 0.004))
        ph = 2 * np.pi * np.cumsum(f * glide) / SR
        y += (0.8 ** i) * np.sin(ph + rng.uniform(0, 6.28)) * np.exp(-t / (dur * (0.5 + 0.4 * rng.random())))
    y *= 0.75 + 0.25 * np.sin(2 * np.pi * tremolo * t)
    if sparkle:
        sp = np.zeros(n)
        for _ in range(int(28 * dur * 4)):
            i = rng.integers(0, max(1, n - n_(0.03)))
            g = n_(rng.uniform(0.008, 0.025))
            f = rng.uniform(2500, 7500)
            tt = np.arange(g) / SR
            sp[i:i + g] += np.sin(2 * np.pi * f * tt) * np.hanning(g) * rng.uniform(0.3, 1.0)
        y = y / (np.abs(y).max() + 1e-9) + sparkle * sp * np.exp(-t / (dur * 0.7)) / (np.abs(sp).max() + 1e-9)
    return y / (np.abs(y).max() + 1e-9)


def crackle(rng, dur, density=900, fc=(1500, 9000), tau=None):
    """Electric / fire crackle: a sparse random impulse train through a band, decaying."""
    n = n_(dur)
    imp = (rng.random(n) < density / SR) * rng.standard_normal(n) * 4
    y = bp(imp + 0.05 * rng.standard_normal(n), fc[0], min(fc[1], SR / 2 - 100))
    if tau:
        y *= np.exp(-np.arange(n) / SR / tau)
    return y / (np.abs(y).max() + 1e-9)


def fire_roar(rng, dur, lo=200, hi=2400):
    """Fire burst: low-passed noise with a slow random amplitude flutter."""
    n = n_(dur)
    y = bp(rng.standard_normal(n), lo, hi)
    flutter = lp(rng.standard_normal(n), 14, 2)
    flutter = 0.6 + 0.4 * flutter / (np.abs(flutter).max() + 1e-9)
    y = y * flutter * env_ad(n, 0.04, dur * 0.35)
    return y / (np.abs(y).max() + 1e-9)


# ---------------------------------------------------------------- the tail

def room_ir(rng, seconds=0.9, damp=4500, predelay=0.012, decay=0.35):
    """A synthesized hall impulse: exponentially decaying noise, darker as it decays, with a few early reflections."""
    n = n_(seconds)
    t = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-t / decay)
    early = lp(ir, damp)
    late = lp(ir, damp * 0.4)
    mixw = np.clip(t / seconds * 2, 0, 1)
    ir = early * (1 - mixw) + late * mixw
    for d, g in ((0.007, 0.5), (0.013, 0.35), (0.021, 0.3), (0.029, 0.2)):
        i = n_(d)
        if i < n:
            ir[i] += g * rng.choice([-1, 1])
    ir = np.concatenate([np.zeros(n_(predelay)), ir])
    return ir / np.sqrt((ir ** 2).sum())


def tail(x, rng, wet=0.18, seconds=0.9, decay=0.3, damp=4500):
    """Dry + convolution reverb (high-passed so the low end stays tight); returns the longer signal."""
    ir = room_ir(rng, seconds, damp=damp, decay=decay)
    w = fftconvolve(hp(x, 250), ir)
    out = np.zeros(len(w))
    out[:len(x)] += x
    return out + wet * w * (np.abs(x).max() / (np.abs(w).max() + 1e-9))


def sat(x, amt=0.4):
    k = 1 + amt * 3
    return np.tanh(x * k) / np.tanh(k)


# ---------------------------------------------------------------- output

LEVEL = {}


def finish(name, y, rms_db=-18.0, max_len=1.2, hp_fc=55):
    y = hp(np.asarray(y, np.float64), hp_fc)
    cut = len(y) > n_(max_len)
    y = y[:n_(max_len)]
    y = tail_trim(y[:, None].astype(np.float32), db=-54)[:, 0].astype(np.float64)
    # A tail cut at max_len fades over its last 0.2 s so the reverb dies away instead of stopping.
    y = fade_out(y, 0.2 if cut else 0.04)
    m = np.abs(y)
    act = y[m > m.max() * 0.05]
    r = np.sqrt((act ** 2).mean())
    g = 10 ** (rms_db / 20) / max(r, 1e-9)
    pk = m.max() * g
    if pk > 10 ** (PEAK_DB / 20):
        g *= 10 ** (PEAK_DB / 20) / pk
    y = (y * g).astype(np.float32)
    write(y[:, None], os.path.join(OUT, name), q=4)
    LEVEL[name] = (20 * np.log10(np.abs(y).max()), 20 * np.log10(np.sqrt((y[np.abs(y) > np.abs(y).max() * 0.05] ** 2).mean())), len(y) / SR)
    print(f'{name:34s} {len(y)/SR:5.2f}s peak {LEVEL[name][0]:6.1f} dBFS  active rms {LEVEL[name][1]:6.1f}')


MAX_LEN = {'sfx_atk': 0.9, 'sfx_imp': 0.8, 'sfx_hurt': 0.6, 'sfx_die': 1.1}


def variants(base, count, fn, seed, **kw):
    kw.setdefault('max_len', next((v for k, v in MAX_LEN.items() if base.startswith(k)), 1.0))
    for i in range(count):
        rng = np.random.default_rng(seed * 100 + i)
        finish(f'{base}_{i + 1:02d}.ogg', fn(rng, i), **kw)


def pick(lst, i):
    return lst[i % len(lst)]


# ================================================================ HERO ATTACKS (the swing / shot)

def lyra_cast(rng, i):
    # Staff thrust + arcane bolt leaving: short staff swish, a bright gliding chime, sparkle, a soft low 'whum'.
    sw = src(M + 'Spear Swing.wav', 0.35, rate=pick([1.25, 1.35, 1.18, 1.3], i))
    air = whoosh(rng, 0.32, 900, 4200, q=1.4, peak_at=0.35)
    chime = shimmer(rng, 0.55, base=pick([660, 740, 698, 784], i), up=1.6, sparkle=0.6)
    whum = thump(rng, 220, 110, 0.25, 0.08)
    y = place([(sw, 0, -8), (air, 0.0, -10), (chime, 0.03, -4), (whum, 0.02, -12)])
    return tail(y, rng, wet=0.30, seconds=1.0, decay=0.32, damp=7000)


def rowan_bow(rng, i):
    # Longbow: string release + arrow passing away + a low string thrum.
    shot = src(pick([M + 'English Longbow Shoot.wav', M + 'Scythian Recurve Shoot.wav'], i), 0.4, rate=pick([1.0, 1.04, 0.97, 1.02], i))
    pas = src(pick([M + 'Scythian Recurve Arrow Pass.wav', M + 'Crossbow Bolt Pass.wav', M + 'Scythian Recurve Arrow Passby.wav'], i), 0.45, rate=1.1)
    thrum = partials(rng, pick([98, 104, 92, 110], i), [1, 2, 3, 4.02], [0.09, 0.06, 0.04, 0.03], [1, 0.5, 0.3, 0.15], 0.3)
    air = whoosh(rng, 0.3, 2500, 900, q=2.0, peak_at=0.15)
    y = place([(shot, 0, 0), (thrum, 0.004, -12), (pas, 0.05, -7), (air, 0.03, -15)])
    return tail(y, rng, wet=0.14, seconds=0.7, decay=0.22)


def oaken_throw(rng, i):
    # Heavy thorn spear hurled: deep swing, a wooden creak as the arm loads, leaves/cloth flutter.
    sw = src(M + 'Spear Swing.wav', 0.5, rate=pick([0.82, 0.9, 0.86, 0.94], i))
    creak = src(pick([KR + 'creak1.ogg', KR + 'creak2.ogg', KR + 'creak3.ogg'], i), 0.25, rate=1.2)
    cloth = src(pick([KR + 'cloth1.ogg', KR + 'cloth3.ogg', KR + 'cloth4.ogg'], i), 0.25)
    air = whoosh(rng, 0.45, 300, 1600, q=1.2, peak_at=0.5)
    y = place([(creak, 0, -14), (cloth, 0.0, -12), (sw, 0.04, -1), (air, 0.05, -8)])
    return tail(y, rng, wet=0.16, seconds=0.8, decay=0.25)


def aldric_swing(rng, i):
    # Knight's longsword: a full sabre swing with a steel 'shing' as the blade turns.
    sw = src(pick([M + 'Sabre Swing.wav', M + 'Katana Swing.wav', M + 'Seax Swing.wav'], i), 0.4, rate=pick([0.92, 0.96, 0.9, 1.0], i))
    shing = partials(rng, pick([1850, 2100, 1960, 2240], i), [1, 2.32, 3.87], [0.09, 0.05, 0.03], [0.6, 0.35, 0.2], 0.25, beat=0.004)
    air = whoosh(rng, 0.32, 600, 2600, q=1.5, peak_at=0.55)
    y = place([(sw, 0, 0), (air, 0.0, -8), (hp(shing, 1200), 0.02, -20)])
    return tail(y, rng, wet=0.13, seconds=0.6, decay=0.2)


def kaelen_dual(rng, i):
    # Two blades: a quick left-right pair of light swishes, the second higher and a beat later.
    a = src(pick([M + 'Katana Swing.wav', M + 'Dagger Swing.wav', M + 'Seax Swing.wav'], i), 0.28, rate=1.12)
    b = src(pick([M + 'Dagger Swing.wav', M + 'Katana Swing.wav', M + 'Sabre Swing.wav', M + 'Dagger Swing.wav'], i), 0.28, rate=1.3)
    air1 = whoosh(rng, 0.2, 1500, 5000, q=1.8, peak_at=0.5)
    air2 = whoosh(rng, 0.2, 2000, 6500, q=1.8, peak_at=0.5)
    gap = pick([0.085, 0.1, 0.09, 0.11], i)
    y = place([(a, 0, 0), (air1, 0, -9), (b, gap, -1), (air2, gap, -9)])
    return tail(y, rng, wet=0.12, seconds=0.55, decay=0.18)


def seraphine_thrust(rng, i):
    # Winged spear-maiden's thrust: short sharp forward whoosh, a light metallic 'tsing', a wing flutter.
    sw = src(M + 'Spear Swing.wav', 0.3, rate=pick([1.45, 1.55, 1.5, 1.6], i))
    air = whoosh(rng, 0.2, 1200, 6000, q=2.2, peak_at=0.7)
    tsing = partials(rng, pick([2600, 2900, 2750], i), [1, 2.71, 4.4], [0.07, 0.04, 0.02], [0.7, 0.3, 0.15], 0.2, beat=0.005)
    wing = whoosh(rng, 0.25, 400, 900, q=0.9, peak_at=0.3)
    y = place([(wing, 0, -14), (sw, 0.03, 0), (air, 0.03, -8), (hp(tsing, 1500), 0.09, -18)])
    return tail(y, rng, wet=0.15, seconds=0.6, decay=0.2, damp=6500)


def emberfang_fire(rng, i):
    # Dragon fireball: a breathy intake, then a roaring fire whoosh with crackles.
    breath = src(pick([C1 + 'breath.ogg', C2 + 'breath_02.ogg'], i), 0.3, rate=0.75)
    fire = src(pick([R + 'spell_fire_01.ogg', R + 'spell_fire_02.ogg', R + 'spell_fire_04.ogg', R + 'spell_fire_05.ogg'], i), 0.6)
    roar = fire_roar(rng, 0.55)
    cr = crackle(rng, 0.5, 500, (1500, 8000), tau=0.2)
    y = place([(breath, 0, -16), (fire, 0.05, -2), (roar, 0.06, -7), (cr, 0.1, -20)])
    return tail(y, rng, wet=0.18, seconds=0.9, decay=0.28)


def stormcrest_bolt(rng, i):
    # Storm bolt: electric zap with a rising whine, sharp crackle, thunder rumble under.
    n = n_(0.4)
    t = np.arange(n) / SR
    zap = np.sign(np.sin(2 * np.pi * np.cumsum(pick([180, 210, 160, 240], i) * (1 + 6 * t)) / SR))
    zap = lp(zap, 5000) * env_ad(n, 0.003, 0.08)
    cr = crackle(rng, 0.35, 1800, (2000, 12000), tau=0.09)
    rumble = lp(rng.standard_normal(n_(0.6)), 180) * env_ad(n_(0.6), 0.04, 0.2)
    air = whoosh(rng, 0.3, 3000, 900, q=1.2, peak_at=0.2)
    y = place([(zap, 0, -12), (cr, 0, -4), (rumble / (np.abs(rumble).max() + 1e-9), 0.03, -8), (air, 0, -12)])
    return tail(y, rng, wet=0.22, seconds=1.0, decay=0.3)


# ================================================================ IMPACTS (where the hero's hit lands)

def impact_arcane(rng, i):
    # Lyra's bolt bursts: bright magic pop, falling bell partials, low body, sparkle tail.
    burst = whoosh(rng, 0.12, 6000, 1500, q=1.0, peak_at=0.05)
    bell = partials(rng, pick([880, 990, 932, 1046], i), [1, 2.0, 2.76, 5.4], [0.18, 0.12, 0.07, 0.04], [1, 0.5, 0.4, 0.2], 0.5)
    sp = shimmer(rng, 0.45, base=pick([1320, 1480, 1397], i), up=0.7, n_par=4, sparkle=0.8)
    body = thump(rng, 160, 60, 0.2, 0.05)
    hit = src(pick([K + 'impactSoft_medium_000.ogg', K + 'impactSoft_medium_002.ogg', K + 'impactSoft_medium_004.ogg'], i), 0.15)
    y = place([(burst, 0, -6), (hit, 0, -10), (body, 0, -8), (bell, 0.004, -7), (sp, 0.02, -12)])
    return tail(y, rng, wet=0.32, seconds=1.1, decay=0.33, damp=7500)


def impact_arrow(rng, i):
    # Arrow into flesh/hide: a tight thunk, a wooden shaft quiver, a little body.
    thk = src(pick([K + 'impactPunch_medium_000.ogg', K + 'impactPunch_medium_002.ogg', K + 'impactPunch_medium_003.ogg', K + 'impactPunch_medium_004.ogg'], i), 0.18, rate=1.15)
    wood = src(pick([K + 'impactWood_light_001.ogg', K + 'impactWood_light_003.ogg', K + 'impactWood_light_004.ogg'], i), 0.15, rate=1.2)
    quiver = partials(rng, pick([310, 280, 340], i), [1, 2.9], [0.06, 0.03], [1, 0.4], 0.18)
    quiver *= 1 + 0.6 * np.sin(2 * np.pi * 38 * np.arange(len(quiver)) / SR)
    y = place([(thk, 0, 0), (wood, 0.0, -7), (quiver, 0.01, -20), (thump(rng, 140, 60, 0.15, 0.04), 0, -10)])
    return tail(y, rng, wet=0.10, seconds=0.5, decay=0.15)


def impact_spear(rng, i):
    # Oakenshield's thorn spear lands: heavy wood-and-earth thud, splinters, leafy debris.
    hv = src(pick([K + 'impactWood_heavy_000.ogg', K + 'impactWood_heavy_002.ogg', K + 'impactWood_heavy_003.ogg', K + 'impactWood_heavy_004.ogg'], i), 0.3, rate=0.92)
    soft = src(pick([K + 'impactSoft_heavy_000.ogg', K + 'impactSoft_heavy_002.ogg', K + 'impactSoft_heavy_003.ogg'], i), 0.25)
    deb = src(pick([R + 'stones_01.ogg', R + 'stones_02.ogg', R + 'stones_03.ogg'], i), 0.35)
    leaves = whoosh(rng, 0.3, 3000, 1500, q=0.8, peak_at=0.1)
    y = place([(hv, 0, 0), (soft, 0, -4), (thump(rng, 110, 40, 0.28, 0.09), 0, -5), (deb, 0.03, -16), (leaves, 0.02, -20)])
    return tail(y, rng, wet=0.14, seconds=0.7, decay=0.22)


def impact_sword(rng, i):
    # Longsword on armour: steel clash, plate clank, a ringing tail.
    clash = src(pick([SC + 'sword_clash.1.ogg', SC + 'sword_clash.3.ogg', SC + 'sword_clash.5.ogg', SC + 'sword_clash.7.ogg'], i), 0.35)
    plate = src(pick([K + 'impactPlate_medium_000.ogg', K + 'impactPlate_medium_002.ogg', K + 'impactPlate_medium_003.ogg'], i), 0.25)
    ring = partials(rng, pick([1240, 1390, 1310, 1170], i), [1, 2.32, 4.25, 6.63], [0.25, 0.14, 0.08, 0.05], [1, 0.6, 0.35, 0.2], 0.6, beat=0.003)
    flesh = src(pick([K + 'impactPunch_heavy_000.ogg', K + 'impactPunch_heavy_002.ogg'], i), 0.15)
    y = place([(clash, 0, -1), (plate, 0, -6), (flesh, 0, -10), (hp(ring, 800), 0.004, -15)])
    return tail(y, rng, wet=0.16, seconds=0.8, decay=0.25)


def impact_dual(rng, i):
    # Kaelen's two blades connect: two quick slices with a light steel tick each.
    s1 = src(pick([KR + 'knifeSlice.ogg', KR + 'knifeSlice2.ogg', R + 'blade_01.ogg'], i), 0.2)
    s2 = src(pick([R + 'blade_02.ogg', KR + 'knifeSlice.ogg', R + 'blade_03.ogg', KR + 'knifeSlice2.ogg'], i), 0.2, rate=1.12)
    t1 = partials(rng, 2300, [1, 2.4], [0.05, 0.03], [1, 0.4], 0.15)
    t2 = partials(rng, 2650, [1, 2.4], [0.05, 0.03], [1, 0.4], 0.15)
    p = src(pick([K + 'impactPunch_medium_001.ogg', K + 'impactPunch_medium_003.ogg'], i), 0.12, rate=1.2)
    gap = pick([0.07, 0.085, 0.075, 0.09], i)
    y = place([(s1, 0, 0), (p, 0, -9), (hp(t1, 1500), 0.003, -18), (s2, gap, -1), (hp(t2, 1500), gap + 0.003, -18), (p, gap, -12)])
    return tail(y, rng, wet=0.12, seconds=0.55, decay=0.17)


def impact_pierce(rng, i):
    # Seraphine's spear pierces: a sharp stab, a short steel scrape, a tight body thud.
    stab = src(pick([SN + 'sword.2.ogg', SN + 'sword.4.ogg', SN + 'sword.6.ogg', SN + 'sword.8.ogg'], i), 0.25)
    punch = src(pick([K + 'impactPunch_medium_000.ogg', K + 'impactPunch_medium_002.ogg', K + 'impactPunch_medium_004.ogg'], i), 0.15)
    ring = partials(rng, pick([1700, 1900, 1800], i), [1, 2.71], [0.12, 0.06], [1, 0.4], 0.35, beat=0.004)
    y = place([(stab, 0, -1), (punch, 0, -5), (thump(rng, 150, 55, 0.18, 0.05), 0, -9), (hp(ring, 1000), 0.01, -18)])
    return tail(y, rng, wet=0.14, seconds=0.6, decay=0.2, damp=6500)


def impact_fire(rng, i):
    # Fireball bursts: a thump, a fire flare, embers crackling away.
    fl = src(pick([R + 'spell_fire_03.ogg', R + 'spell_fire_06.ogg', R + 'spell_fire_07.ogg'], i), 0.45)
    y = place([(thump(rng, 120, 45, 0.3, 0.09), 0, -3), (fire_roar(rng, 0.4, 150, 1800), 0, -5), (fl, 0, -4), (crackle(rng, 0.6, 400, (1500, 9000), tau=0.25), 0.05, -16)])
    return tail(y, rng, wet=0.2, seconds=0.9, decay=0.3)


def impact_storm(rng, i):
    # Lightning strike: a hard crack, buzzing arc, thunder roll.
    n = n_(0.7)
    crack = crackle(rng, 0.12, 6000, (1000, 14000), tau=0.025)
    buzz = crackle(rng, 0.4, 1200, (2500, 9000), tau=0.12)
    roll = lp(rng.standard_normal(n), 140) * env_ad(n, 0.03, 0.25)
    y = place([(crack, 0, 0), (thump(rng, 160, 50, 0.25, 0.07), 0, -4), (buzz, 0.01, -10), (roll / (np.abs(roll).max() + 1e-9), 0.02, -5)])
    return tail(y, rng, wet=0.25, seconds=1.1, decay=0.35)


# ================================================================ ENEMY HURT (the enemy's own reaction, quieter layer)

def hurt_generic(rng, i):
    # Flesh hit for any enemy: fleshy smack + body thump + a short grunt.
    sm = src(pick([K + 'impactPunch_heavy_001.ogg', K + 'impactPunch_heavy_003.ogg', K + 'impactPunch_heavy_004.ogg', K + 'impactPunch_heavy_000.ogg'], i), 0.18)
    gr = src(pick([C1 + 'grunt_02.ogg', C1 + 'grunt_03.ogg', C1 + 'grunt_04.ogg', C2 + 'grunt_07.ogg'], i), 0.22, rate=0.8)
    y = place([(sm, 0, 0), (thump(rng, 120, 50, 0.2, 0.05), 0, -6), (lp(gr, 2500), 0.02, -10)])
    return tail(y, rng, wet=0.08, seconds=0.4, decay=0.12)


def hurt_nightfang(rng, i):
    # Shadow wolf: a short yelp-snarl.
    v = src(pick([C1 + 'barking_01.ogg', C1 + 'barking_02.ogg', C2 + 'attack_01.ogg', C2 + 'attack_03.ogg'], i), 0.3, rate=pick([0.85, 0.8, 0.9, 0.75], i))
    y = place([(bp(v, 200, 5000), 0, 0), (thump(rng, 120, 55, 0.15, 0.04), 0, -10)])
    return tail(y, rng, wet=0.1, seconds=0.45, decay=0.14)


def hurt_bonequill(rng, i):
    # Skeleton archer: dry bone clacks rattling.
    a = src(pick([K + 'impactWood_light_000.ogg', K + 'impactWood_light_002.ogg', K + 'impactWood_light_004.ogg'], i), 0.1, rate=1.5)
    b = src(pick([K + 'impactGeneric_light_001.ogg', K + 'impactGeneric_light_003.ogg', K + 'impactGeneric_light_000.ogg'], i), 0.1, rate=1.4)
    layers = [(a, 0, 0)]
    t = 0.0
    for k in range(4):
        t += rng.uniform(0.025, 0.05)
        layers.append((pick([a, b], k), t, -5 - 3 * k))
    return tail(place(layers), rng, wet=0.12, seconds=0.5, decay=0.15)


def hurt_vespera(rng, i):
    # Shadow sorceress: a ghostly hiss and a dark ripple.
    hiss = whoosh(rng, 0.3, 5000, 1800, q=1.2, peak_at=0.15)
    vox = src(pick([C1 + 'weird_01.ogg', C1 + 'weird_03.ogg', C2 + 'weird_07.ogg'], i), 0.3, rate=1.15)
    rip = shimmer(rng, 0.3, base=pick([330, 311, 349], i), up=0.6, n_par=4, sparkle=0.0, tremolo=24)
    y = place([(hiss, 0, -4), (bp(vox, 400, 5000), 0, -6), (rip, 0, -10)])
    return tail(y, rng, wet=0.3, seconds=0.8, decay=0.25)


def hurt_blightroot(rng, i):
    # Rot tree-beast: wet bark crunch, a creak.
    cr = src(pick([W + 'wood_cracking_01.ogg', W + 'wood_cracking_02.ogg', W + 'wood_cracking_03.ogg', W + 'wood_cracking_04.ogg'], i), 0.25)
    sl = src(pick([C2 + 'slime_01.ogg', C2 + 'slime_03.ogg', C2 + 'slime_05.ogg'], i), 0.2)
    y = place([(cr, 0, 0), (sl, 0.0, -8), (thump(rng, 100, 45, 0.2, 0.06), 0, -7)])
    return tail(y, rng, wet=0.12, seconds=0.5, decay=0.15)


def hurt_ashwyrm(rng, i):
    # Void-fire wyrm: a scaly thud and a short snarl.
    sn = src(pick([C1 + 'roar_01.ogg', C1 + 'roar_02.ogg', C2 + 'monster_10.ogg', C1 + 'monster_03.ogg'], i), 0.35, rate=0.7)
    sc = src(pick([K + 'impactPlank_medium_000.ogg', K + 'impactPlank_medium_002.ogg', K + 'impactPlank_medium_004.ogg'], i), 0.15, rate=0.8)
    y = place([(sc, 0, 0), (thump(rng, 90, 38, 0.25, 0.08), 0, -4), (lp(sn, 3500), 0.02, -6)])
    return tail(y, rng, wet=0.14, seconds=0.6, decay=0.2)


def hurt_malzareth(rng, i):
    # Armoured warlord: a plate clank and a gruff grunt.
    pl = src(pick([K + 'impactPlate_heavy_000.ogg', K + 'impactPlate_heavy_002.ogg', K + 'impactPlate_heavy_004.ogg'], i), 0.25)
    gr = src(pick([C2 + 'human_01.ogg', C2 + 'human_03.ogg', C2 + 'human_05.ogg'], i), 0.3, rate=0.78)
    y = place([(pl, 0, 0), (thump(rng, 110, 45, 0.2, 0.06), 0, -6), (lp(gr, 3000), 0.02, -8)])
    return tail(y, rng, wet=0.12, seconds=0.55, decay=0.17)


# ================================================================ ENEMY DEATHS

def body_fall(rng, i, heavy=False):
    s = src(pick([K + 'impactSoft_heavy_001.ogg', K + 'impactSoft_heavy_003.ogg', K + 'impactSoft_heavy_004.ogg'], i), 0.3, rate=0.85 if heavy else 1.0)
    return place([(s, 0, 0), (thump(rng, 100 if heavy else 130, 40, 0.3, 0.1 if heavy else 0.07), 0, -3)])


def death_generic(rng, i):
    cry = src(pick([C2 + 'die_01.ogg', C2 + 'die_02.ogg', C2 + 'die_03.ogg', C2 + 'die_04.ogg'], i), 0.5, rate=0.82)
    y = place([(lp(cry, 3500), 0, -2), (body_fall(rng, i), pick([0.22, 0.26, 0.2, 0.28], i), -2)])
    return tail(y, rng, wet=0.14, seconds=0.7, decay=0.22)


def death_nightfang(rng, i):
    yelp = src(pick([C1 + 'hurt_01.ogg', C1 + 'hurt_03.ogg', C1 + 'howl.ogg', C1 + 'hurt_02.ogg'], i), 0.45, rate=pick([1.05, 1.0, 1.15, 0.95], i))
    smoke = whoosh(rng, 0.45, 2500, 500, q=0.8, peak_at=0.2)
    y = place([(bp(yelp, 250, 6000), 0, 0), (body_fall(rng, i), 0.2, -3), (smoke, 0.15, -14)])
    return tail(y, rng, wet=0.16, seconds=0.8, decay=0.24)


def death_bonequill(rng, i):
    # Skeleton collapses: a clattering pile of bones.
    layers = []
    t = 0.0
    for k in range(9):
        s = src(pick([K + 'impactWood_light_000.ogg', K + 'impactWood_light_001.ogg', K + 'impactWood_light_002.ogg', K + 'impactWood_light_003.ogg', K + 'impactGeneric_light_002.ogg', K + 'impactGeneric_light_004.ogg'], i + k), 0.1, rate=rng.uniform(1.3, 1.7))
        layers.append((s, t, -2 - 1.2 * k))
        t += rng.uniform(0.03, 0.07)
    fall = src(pick([W + 'wood_falling_01.ogg', W + 'wood_falling_03.ogg', W + 'wood_falling_05.ogg'], i), 0.5, rate=1.3)
    layers.append((fall, 0.02, -6))
    return tail(place(layers), rng, wet=0.14, seconds=0.6, decay=0.2)


def death_vespera(rng, i):
    # Shadow sorceress dissolves: a falling ghostly cry, a dark swirl, fading shimmer.
    vox = src(pick([C1 + 'weird_02.ogg', C1 + 'weird_04.ogg', C2 + 'weird_08.ogg'], i), 0.6, rate=0.9)
    swirl = whoosh(rng, 0.7, 4000, 300, q=1.0, peak_at=0.3)
    sh = shimmer(rng, 0.7, base=pick([523, 494, 554], i), up=0.5, n_par=5, sparkle=0.5)
    y = place([(bp(vox, 300, 5000), 0, -2), (swirl, 0.02, -6), (sh, 0.08, -10)])
    return tail(y, rng, wet=0.35, seconds=1.1, decay=0.33)


def death_blightroot(rng, i):
    # Tree-beast splinters and crashes down.
    br = src(pick([W + 'wood_breaking_01.ogg', W + 'wood_breaking_02.ogg', B + 'bfh1_wood_breaking_01.ogg', B + 'bfh1_wood_breaking_03.ogg'], i), 0.6)
    groan = src(pick([C1 + 'troll_01.ogg', C1 + 'troll_02.ogg', C1 + 'troll_03.ogg'], i), 0.5, rate=0.65)
    y = place([(br, 0, 0), (lp(groan, 2200), 0, -8), (body_fall(rng, i, True), 0.3, -3)])
    return tail(y, rng, wet=0.16, seconds=0.8, decay=0.25)


def death_ashwyrm(rng, i):
    # Wyrm's death roar and a heavy crash, embers hiss out.
    roar = src(pick([C2 + 'roar_04.ogg', C2 + 'roar_05.ogg', C2 + 'roar_06.ogg'], i), 0.6, rate=0.72)
    hiss = fire_roar(rng, 0.5, 1500, 7000)
    y = place([(lp(roar, 4000), 0, 0), (body_fall(rng, i, True), 0.32, -1), (hiss, 0.35, -16)])
    return tail(y, rng, wet=0.2, seconds=1.0, decay=0.3)


def death_malzareth(rng, i):
    # Armoured warlord falls: a pained grunt, then plate and greatsword clattering down.
    gr = src(pick([C2 + 'human_02.ogg', C2 + 'human_04.ogg', C2 + 'human_06.ogg'], i), 0.4, rate=0.75)
    mf = src(pick([W + 'metal_falling_01.ogg', W + 'metal_falling_02.ogg', B + 'bfh1_metal_falling_01.ogg', B + 'bfh1_metal_falling_03.ogg'], i), 0.6)
    y = place([(lp(gr, 3000), 0, -3), (mf, 0.22, 0), (body_fall(rng, i, True), 0.24, -3)])
    return tail(y, rng, wet=0.16, seconds=0.9, decay=0.27)


SOUNDS = [
    # (file base, variants, builder, seed, rms)
    ('sfx_atk_lyra_cast', 4, lyra_cast, 11, -18),
    ('sfx_atk_rowan_bow', 4, rowan_bow, 12, -18),
    ('sfx_atk_oakenshield_throw', 4, oaken_throw, 13, -18),
    ('sfx_atk_aldric_swing', 4, aldric_swing, 14, -18),
    ('sfx_atk_kaelen_dual', 4, kaelen_dual, 15, -18),
    ('sfx_atk_seraphine_thrust', 3, seraphine_thrust, 16, -18),
    ('sfx_atk_emberfang_fire', 3, emberfang_fire, 17, -18),
    ('sfx_atk_stormcrest_bolt', 3, stormcrest_bolt, 18, -18),
    ('sfx_imp_arcane', 4, impact_arcane, 21, -18),
    ('sfx_imp_arrow', 4, impact_arrow, 22, -18),
    ('sfx_imp_spear', 4, impact_spear, 23, -17),
    ('sfx_imp_sword', 4, impact_sword, 24, -18),
    ('sfx_imp_dual', 4, impact_dual, 25, -18),
    ('sfx_imp_pierce', 3, impact_pierce, 26, -18),
    ('sfx_imp_fire', 3, impact_fire, 27, -18),
    ('sfx_imp_storm', 3, impact_storm, 28, -18),
    ('sfx_hurt_generic', 4, hurt_generic, 31, -19),
    ('sfx_hurt_nightfang', 4, hurt_nightfang, 32, -19),
    ('sfx_hurt_bonequill', 3, hurt_bonequill, 33, -19),
    ('sfx_hurt_vespera', 3, hurt_vespera, 34, -19),
    ('sfx_hurt_blightroot', 3, hurt_blightroot, 35, -19),
    ('sfx_hurt_ashwyrm', 3, hurt_ashwyrm, 36, -19),
    ('sfx_hurt_malzareth', 3, hurt_malzareth, 37, -19),
    ('sfx_die_generic', 4, death_generic, 41, -18),
    ('sfx_die_nightfang', 3, death_nightfang, 42, -18),
    ('sfx_die_bonequill', 3, death_bonequill, 43, -18),
    ('sfx_die_vespera', 3, death_vespera, 44, -18),
    ('sfx_die_blightroot', 3, death_blightroot, 45, -18),
    ('sfx_die_ashwyrm', 3, death_ashwyrm, 46, -18),
    ('sfx_die_malzareth', 3, death_malzareth, 47, -18),
]

if __name__ == '__main__':
    only = os.environ.get('ONLY', '')
    for base, count, fn, seed, rms in SOUNDS:
        if only and only not in base:
            continue
        variants(base, count, fn, seed, rms_db=rms)
