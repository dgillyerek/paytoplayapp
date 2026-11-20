"""Runner / battle attack SFX for Theme A (fantasy_kingdom_a): layered, round-robin variants.

Every sound is built for a medieval fantasy kingdom: steel, wood, leather, cloth, bowstrings,
creature voices, and organic magic (fire, wind, ice, chapel bells, low choir, earth rumble).
No synthesized electronic, sci-fi, laser, zap, beep, or rising-whine layers.

Layers are CC0 recordings (see LICENSES.md / Docs/audio-credits.md) plus original
acoustic-sounding synthesis (numpy/scipy: swept-band noise whooshes, ringing metal
partials, low choir formants, body thumps, ember crackle, a synthesized stone-hall
convolution tail).

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
WG = 'x_wings_flap_large/'; ICE = 'x_ice_shatters/'; WN = 'x_wind/'
FIRE = 'fire-1.wav'; THUNDER = 'rain-thunder.ogg'; WINDSHORT = 'short wind sound.wav'

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


# ---------------------------------------------------------------- synthesis (acoustic-sounding only)

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
    """Struck metal ringing (a blade or plate): each sine decays on its own tau, slightly detuned pairs beat."""
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


def rumble(rng, dur=0.7, fc=140):
    """Earthy / thunder roll: dark noise with a slow natural flutter, no buzz or tone."""
    n = n_(dur)
    y = lp(rng.standard_normal(n), fc, 4)
    flutter = lp(np.abs(rng.standard_normal(n)), 8, 2)
    flutter = 0.35 + 0.65 * flutter / (np.abs(flutter).max() + 1e-9)
    y = y * flutter * env_ad(n, 0.04, dur * 0.4, hold=0.05)
    return y / (np.abs(y).max() + 1e-9)


def choir(rng, dur, f0, n_voices=5, swell=0.22):
    """Low chapel choir / chant swell: clustered vowel-like harmonics, slow rise, no sparkle or pitch glide."""
    n = n_(dur)
    t = np.arange(n) / SR
    y = np.zeros(n)
    # "oh"/"ah" vowel: strong 1st and 2nd, weaker higher harmonics, dark top
    formants = [(1.0, 1.0), (2.0, 0.55), (3.0, 0.28), (4.05, 0.12)]
    for _ in range(n_voices):
        fv = f0 * (1 + rng.uniform(-0.016, 0.016))
        ph = rng.uniform(0, 2 * np.pi)
        # shallow human vibrato (~5 Hz, 0.3 %), not an electronic LFO
        vib = 1 + 0.003 * np.sin(2 * np.pi * (4.6 + rng.uniform(-0.5, 0.5)) * t + ph)
        inst = np.zeros(n)
        for r, a in formants:
            inst += a * np.sin(2 * np.pi * fv * r * np.cumsum(vib) / SR + ph)
        y += inst
    env = env_ad(n, swell, dur * 0.5, hold=dur * 0.12)
    y = lp(y * env, 2800, 4)
    return y / (np.abs(y).max() + 1e-9)


def ember_crackle(rng, dur, density=90, fc=(400, 3500), tau=0.3):
    """Fireplace ember pops: sparse, mid-band, not a dense electric spark train."""
    n = n_(dur)
    imp = (rng.random(n) < density / SR) * rng.standard_normal(n) * 4
    y = bp(imp + 0.03 * rng.standard_normal(n), fc[0], min(fc[1], SR / 2 - 100))
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


MAX_LEN = {'sfx_atk_lyra': 0.75, 'sfx_atk': 0.9, 'sfx_imp': 0.8, 'sfx_hurt': 0.6, 'sfx_die': 1.1}


def variants(base, count, fn, seed, **kw):
    kw.setdefault('max_len', next((v for k, v in MAX_LEN.items() if base.startswith(k)), 1.0))
    for i in range(count):
        rng = np.random.default_rng(seed * 100 + i)
        finish(f'{base}_{i + 1:02d}.ogg', fn(rng, i), **kw)


def pick(lst, i):
    return lst[i % len(lst)]


# Shared recorded slices

# Thunder cracks in WuxiaScrub's rain-thunder.ogg (the rain bed is ~20 dB quieter).
THUNDER_AT = [21.25, 23.85, 25.35, 28.85]
# Three flaps in AntumDeluge's large-wings file.
WING_AT = [0.00, 1.05, 2.10]
# Fireplace slices in AntumDeluge fire-1.wav.
FIRE_AT = [0.15, 0.7, 1.3, 1.9]


def thunder_crack(i, dur=0.85, rate=1.0):
    return src(THUNDER, dur=dur, rate=rate, start=pick(THUNDER_AT, i), fade=0.15, db=-8)


def wing_flap(i, dur=0.42, rate=1.0):
    return src(WG + 'wings_flap_large.ogg', dur=dur, rate=rate, start=pick(WING_AT, i), fade=0.08, db=-12)


def wind_gust(i, dur=0.55, rate=1.0):
    # Prefer the short recorded gusts; Wind3 is already brief.
    path, start = pick([
        (WN + 'Wind3.ogg', 0.05),
        (WN + 'Wind.ogg', 0.4),
        (WN + 'Wind2.ogg', 0.8),
        (WINDSHORT, 0.3),
    ], i)
    return src(path, dur=dur, rate=rate, start=start, fade=0.1, db=-18)


def fire_bed(i, dur=0.55, rate=1.0):
    return src(FIRE, dur=dur, rate=rate, start=pick(FIRE_AT, i), fade=0.1, db=-20)


def chapel_bell(i, dur=0.55, rate=1.25):
    return src(pick([
        K + 'impactBell_heavy_000.ogg',
        K + 'impactBell_heavy_001.ogg',
        K + 'impactBell_heavy_002.ogg',
        K + 'impactBell_heavy_003.ogg',
        K + 'impactBell_heavy_004.ogg',
    ], i), dur=dur, rate=rate, fade=0.12)


def ice_crackle(i, dur=0.35, rate=1.15):
    return src(pick([
        ICE + 'LedasLuzta.ogg',
        ICE + 'LedasLuzta2.ogg',
        ICE + 'LedasLuzta33.ogg',
        ICE + 'LedasLuzta4.ogg',
        ICE + 'LedasLuzta5.ogg',
    ], i), dur=dur, rate=rate, fade=0.08)


# ---------------------------------------------------------------- Lyra textures

def cloth_rustle(rng, dur=0.12):
    """Robe and sleeve rustle: a quick cluster of soft, mid-band noise scuffs."""
    n = n_(dur)
    y = np.zeros(n)
    for _ in range(5):
        k = n_(rng.uniform(0.015, 0.035))
        s = rng.integers(0, max(1, n - k))
        y[s:s + k] += bp(rng.standard_normal(k), 700, 4500) * np.hanning(k) * rng.uniform(0.4, 1.0)
    return y / (np.abs(y).max() + 1e-9)


def gather_rumble(rng, dur, fc_from=90, fc_to=420):
    """Power drawn into the staff: dark rumble that swells and brightens up to the release."""
    n = n_(dur)
    x = rng.standard_normal(n)
    lo, hi = lp(x, fc_from, 2), lp(x, fc_to, 2)
    ramp = np.linspace(0, 1, n) ** 1.5
    y = (lo * (1 - ramp) + hi * ramp) * ramp ** 1.2
    return y / (np.abs(y).max() + 1e-9)


def flare(rng, dur=0.4):
    """Spell igniting: a torch-like 'fwoosh' with a hard front, flame flutter, quick fall-off."""
    n = n_(dur)
    t = np.arange(n) / SR
    body = bp(rng.standard_normal(n), 250, 3800)
    flutter = lp(rng.standard_normal(n), 30, 2)
    flutter = 0.55 + 0.45 * flutter / (np.abs(flutter).max() + 1e-9)
    e = np.clip(t / 0.008, 0, 1) * np.exp(-t / 0.11)
    y = body * flutter * e
    return y / (np.abs(y).max() + 1e-9)


def lyra_cast(rng, i):
    # Medieval sorceress's spell: robe rustle and a rumble as power gathers in the staff, then the
    # spell ignites in a torch-like flare over a deep launch thump, with crackling sparks and a rush of
    # air carrying it away, in a stone hall. Organic textures only: no voices, chimes or bells.
    rel = pick([0.11, 0.12, 0.1, 0.115], i)  # the moment the spell leaves the staff
    rustle = cloth_rustle(rng, 0.12)
    gather = gather_rumble(rng, rel + 0.01)
    ignite = flare(rng, 0.4)
    boom = thump(rng, f_hi=pick([95, 105, 88, 100], i), f_lo=42, dur=0.35, tau=0.11)
    weight = lp(rng.standard_normal(n_(0.3)), 260, 4) * env_ad(n_(0.3), 0.005, 0.08)
    weight /= np.abs(weight).max() + 1e-9
    sparks = ember_crackle(rng, 0.45, density=pick([140, 170, 120, 155], i), fc=(900, 6000), tau=0.16)
    rush = whoosh(rng, 0.45, 1800, 450, q=0.9, peak_at=0.1)
    y = place([
        (rustle, 0.0, -14),
        (gather, 0.0, -10),
        (boom, rel, -4),
        (weight, rel, -8),
        (ignite, rel, -2),
        (sparks, rel + 0.01, -13),
        (rush, rel + 0.02, -8),
    ])
    return tail(sat(y / (np.abs(y).max() + 1e-9), 0.25), rng, wet=0.2, seconds=0.9, decay=0.26, damp=5000)


# ---- hero attack textures (pure synthesis; no recordings)

def _norm(y):
    return y / (np.abs(y).max() + 1e-9)


def wood_knock(rng, f=420, dur=0.12):
    """Wooden shaft / haft knock or creak: resonant mid-band noise with a short body tone."""
    n = n_(dur)
    t = np.arange(n) / SR
    body = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.025) + 0.5 * np.sin(2 * np.pi * f * 2.7 * t) * np.exp(-t / 0.012)
    noise = bp(rng.standard_normal(n), f * 0.8, f * 4) * np.exp(-t / 0.015)
    return _norm(body + 0.8 * _norm(noise))


def steel_shing(rng, dur=0.22, tau=0.06, f0=None):
    """Blade edge cutting air: bright steel scrape with a brief metallic edge, short."""
    n = n_(dur)
    t = np.arange(n) / SR
    f0 = f0 or rng.uniform(2600, 3400)
    scrape = whoosh(rng, dur, 3500, 7500, q=3.0, peak_at=0.25, bright=1.3)
    edge = partials(rng, f0, [1.0, 1.47, 2.09, 2.83], [tau, tau * 0.7, tau * 0.5, tau * 0.35], [1, 0.6, 0.4, 0.25], dur)
    return _norm(scrape + 0.45 * edge * (1 - np.exp(-t / 0.004)))


def mail_jingle(rng, dur=0.16, count=6):
    """Chainmail / buckle rattle: a few tiny, dry metal ticks."""
    layers = [(np.zeros(n_(dur)), 0, 0)]
    for _ in range(count):
        m = n_(0.02)
        tick = hp(rng.standard_normal(m), 4000) * np.exp(-np.arange(m) / SR / 0.004)
        layers.append((_norm(tick), rng.uniform(0, dur - 0.02), -rng.uniform(0, 8)))
    return _norm(place(layers))


def wing_beat(rng, dur=0.3, fc=700):
    """One big wing downstroke: a soft air push with a feathery flutter, two quick humps."""
    n = n_(dur)
    t = np.arange(n) / SR
    air = lp(rng.standard_normal(n), fc, 3)
    flut = 0.6 + 0.4 * np.sin(2 * np.pi * rng.uniform(28, 38) * t) ** 2
    e = np.exp(-((t - dur * 0.3) / (dur * 0.14)) ** 2) + 0.45 * np.exp(-((t - dur * 0.55) / (dur * 0.1)) ** 2)
    return _norm(air * flut * e)


def thunder_snap(rng, dur=0.7):
    """Natural thunder: a sharp broadband crack that collapses into a rolling low rumble."""
    n = n_(dur)
    t = np.arange(n) / SR
    crack = bp(rng.standard_normal(n), 250, 3500) * np.exp(-t / 0.045)
    crack *= 0.5 + 0.5 * (rng.random(n) < 0.35)
    roll = rumble(rng, dur, 160)
    return _norm(_norm(crack) + 1.3 * roll)


# ================================================================ HERO ATTACKS (the swing / shot)

def string_slap(rng, dur=0.12, f_hi=240, f_lo=85):
    """Bowstring snapping forward against the bow: a short, dull, damped 'thwack' with no sustained pitch."""
    n = n_(dur)
    t = np.arange(n) / SR
    f = f_lo + (f_hi - f_lo) * np.exp(-t / 0.012)
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.022)
    snap = bp(rng.standard_normal(n), 300, 2200) * np.exp(-t / 0.012)
    return _norm(tone + 0.9 * _norm(snap))


def rowan_bow(rng, i):
    # Longbow shot: a faint creak at full draw, the string slapping forward with a dull "thwack",
    # and the arrow leaving in a quick "fwip" that hisses away. No ringing string tone.
    creak = wood_knock(rng, pick([260, 290, 240, 275], i), 0.07)
    slap = string_slap(rng, 0.12, f_hi=pick([240, 260, 225, 250], i), f_lo=pick([85, 92, 80, 88], i))
    fwip = whoosh(rng, 0.11, 1400, 5200, q=2.2, peak_at=0.35)
    away = whoosh(rng, 0.3, 3200, 1300, q=2.0, peak_at=0.08)
    rel = 0.03
    y = place([(creak, 0.0, -18), (slap, rel, -2), (fwip, rel + 0.004, -4), (away, rel + 0.04, -11)])
    return tail(y, rng, wet=0.1, seconds=0.5, decay=0.15)


def oaken_throw(rng, i):
    # Heavy thorn spear hurled: bark armour creaking under the strain, a heavy body heave and stamp,
    # the wooden haft knocking as it leaves the hand, and only a short, low push of air.
    # Weight and wood carry it, not whoosh.
    strain = wood_knock(rng, pick([150, 170, 140, 160], i), 0.2)
    creak2 = wood_knock(rng, pick([230, 250, 215, 240], i), 0.12)
    rustle = cloth_rustle(rng, 0.14)
    stamp = thump(rng, f_hi=pick([120, 130, 112, 125], i), f_lo=45, dur=0.3, tau=0.08)
    grunt_body = lp(rng.standard_normal(n_(0.22)), 300, 4) * env_ad(n_(0.22), 0.01, 0.06)
    haft = wood_knock(rng, pick([360, 400, 340, 380], i), 0.09)
    air = whoosh(rng, 0.2, 220, 900, q=1.0, peak_at=0.4)
    rel = 0.13
    y = place([(rustle, 0, -14), (strain, 0.0, -7), (creak2, 0.06, -12), (stamp, rel, -1), (_norm(grunt_body), rel, -6),
               (haft, rel + 0.02, -6), (air, rel, -14)])
    return tail(y, rng, wet=0.14, seconds=0.6, decay=0.2)


def aldric_swing(rng, i):
    # Knight's longsword: mail rattle and cloth as he winds up, then a full, weighty blade swing.
    # No ringing steel edge at the end: the swing ends on air, not a ting.
    mail = mail_jingle(rng, 0.14)
    rustle = cloth_rustle(rng, 0.1)
    swing = whoosh(rng, 0.38, 380, 2400, q=1.6, peak_at=0.55)
    body = whoosh(rng, 0.3, 180, 700, q=0.9, peak_at=0.6)
    y = place([(rustle, 0, -14), (mail, 0.0, -14), (swing, 0.04, 0), (body, 0.08, -7)])
    return tail(y, rng, wet=0.13, seconds=0.6, decay=0.2)


def kaelen_dual(rng, i):
    # Two light blades: a quick left-right pair of thin, fast swishes, each with a brief steel edge;
    # the second a touch higher and a beat later.
    rustle = cloth_rustle(rng, 0.06)
    a = whoosh(rng, 0.17, 1300, 5200, q=2.0, peak_at=0.55)
    b = whoosh(rng, 0.17, 1700, 6500, q=2.0, peak_at=0.55)
    ea = steel_shing(rng, 0.14, tau=0.03, f0=pick([3300, 3600, 3150, 3450], i))
    eb = steel_shing(rng, 0.14, tau=0.03, f0=pick([3800, 4100, 3650, 3950], i))
    gap = pick([0.085, 0.1, 0.09, 0.11], i)
    y = place([(rustle, 0, -16), (a, 0.0, 0), (ea, 0.07, -13), (b, gap, -1), (eb, gap + 0.07, -13)])
    return tail(y, rng, wet=0.11, seconds=0.5, decay=0.16)


def seraphine_thrust(rng, i):
    # Winged spear-maiden: a big wing downbeat drives her forward into a sharp, straight spear
    # thrust, with a bright glint off the steel tip.
    beat = wing_beat(rng, 0.32, fc=pick([650, 750, 600], i))
    rustle = cloth_rustle(rng, 0.08)
    thrust = whoosh(rng, 0.2, 1100, 5200, q=2.4, peak_at=0.7)
    haft = wood_knock(rng, pick([380, 420, 350], i), 0.08)
    tip = steel_shing(rng, 0.16, tau=0.035, f0=pick([3600, 3900, 3400], i))
    y = place([(beat, 0, -4), (rustle, 0.04, -16), (haft, 0.12, -14), (thrust, 0.1, 0), (tip, 0.22, -12)])
    return tail(y, rng, wet=0.15, seconds=0.6, decay=0.2, damp=5500)


def emberfang_fire(rng, i):
    # Dragon fireball: a deep breathy draw of air, then a roaring gout of flame with a low boom,
    # crackling embers spitting off it.
    draw = whoosh(rng, 0.16, 250, 900, q=0.8, peak_at=0.95)
    gout = flare(rng, 0.5)
    roar = fire_roar(rng, 0.55, 150, 2200)
    boom = thump(rng, f_hi=pick([80, 88, 74], i), f_lo=38, dur=0.35, tau=0.12)
    embers = ember_crackle(rng, 0.5, pick([90, 110, 75], i), (500, 4500), tau=0.22)
    rel = 0.14
    y = place([(draw, 0, -10), (boom, rel, -6), (gout, rel, -1), (roar, rel + 0.01, -5), (embers, rel + 0.04, -14)])
    return tail(sat(_norm(y), 0.3), rng, wet=0.17, seconds=0.8, decay=0.25)


def stormcrest_bolt(rng, i):
    # Storm beast calls down a bolt: a wing gust lifts, the air crackles with static, then a sharp
    # natural thunder crack rolls off into a low rumble.
    gust = wing_beat(rng, 0.3, fc=900)
    wind = whoosh(rng, 0.3, 600, 2200, q=1.0, peak_at=0.8)
    static = ember_crackle(rng, 0.18, density=pick([380, 450, 320], i), fc=(1500, 6000), tau=0.0)
    static = static * np.linspace(0.2, 1, len(static))
    snap = thunder_snap(rng, 0.7)
    rel = 0.16
    y = place([(gust, 0, -8), (wind, 0, -12), (static, rel - 0.14, -15), (snap, rel, -1)])
    return tail(y, rng, wet=0.22, seconds=0.9, decay=0.3, damp=4500)


# ================================================================ IMPACTS (where the hero's hit lands)

def impact_arcane(rng, i):
    # Lyra's bolt bursts: body hit, chapel bell, ice crackle, choir, earth rumble — no sparkle grains.
    hit = src(pick([K + 'impactSoft_medium_000.ogg', K + 'impactSoft_medium_002.ogg', K + 'impactSoft_medium_004.ogg'], i), 0.15)
    body = thump(rng, 160, 60, 0.2, 0.05)
    bell = hp(chapel_bell(i, 0.55, rate=pick([1.15, 1.22, 1.1, 1.28], i)), 300)
    ice = ice_crackle(i, 0.3, rate=pick([1.2, 1.3, 1.15, 1.25], i))
    chant = choir(rng, 0.55, pick([196.0, 220.0, 174.6, 207.6], i), n_voices=4, swell=0.08)
    earth = rumble(rng, 0.45, 130)
    gust = whoosh(rng, 0.2, 2500, 700, q=1.2, peak_at=0.15)
    y = place([
        (hit, 0, -8),
        (body, 0, -8),
        (gust, 0, -12),
        (bell, 0.01, -7),
        (ice, 0.01, -14),
        (chant, 0.02, -12),
        (earth, 0.0, -13),
    ])
    return tail(y, rng, wet=0.30, seconds=1.1, decay=0.33, damp=5000)


def impact_arrow(rng, i):
    # Arrow into flesh/hide: a tight thunk, a wooden shaft knock, a little body, faint shaft quiver.
    thk = src(pick([K + 'impactPunch_medium_000.ogg', K + 'impactPunch_medium_002.ogg', K + 'impactPunch_medium_003.ogg', K + 'impactPunch_medium_004.ogg'], i), 0.18, rate=1.15)
    wood = src(pick([K + 'impactWood_light_001.ogg', K + 'impactWood_light_003.ogg', K + 'impactWood_light_004.ogg'], i), 0.15, rate=1.2)
    fletch = src(pick([
        TS + 'arrow-feathers-01.wav',
        TS + 'arrow-feathers-02.wav',
        TS + 'arrow-feathers-03.wav',
    ], i), 0.12, rate=1.1)
    quiver = partials(rng, pick([310, 280, 340], i), [1, 2.9], [0.06, 0.03], [1, 0.4], 0.18)
    quiver *= 1 + 0.6 * np.sin(2 * np.pi * 38 * np.arange(len(quiver)) / SR)
    y = place([(thk, 0, 0), (wood, 0.0, -7), (fletch, 0.0, -16), (quiver, 0.01, -22), (thump(rng, 140, 60, 0.15, 0.04), 0, -10)])
    return tail(y, rng, wet=0.10, seconds=0.5, decay=0.15)


def impact_spear(rng, i):
    # Oakenshield's thorn spear lands: heavy wood-and-earth thud, shield-like wood knock, stone debris.
    hv = src(pick([K + 'impactWood_heavy_000.ogg', K + 'impactWood_heavy_002.ogg', K + 'impactWood_heavy_003.ogg', K + 'impactWood_heavy_004.ogg'], i), 0.3, rate=0.92)
    soft = src(pick([K + 'impactSoft_heavy_000.ogg', K + 'impactSoft_heavy_002.ogg', K + 'impactSoft_heavy_003.ogg'], i), 0.25)
    knock = src(pick([K + 'impactWood_medium_000.ogg', K + 'impactWood_medium_002.ogg', K + 'impactWood_medium_004.ogg'], i), 0.2, rate=1.05)
    deb = src(pick([R + 'stones_01.ogg', R + 'stones_02.ogg', R + 'stones_03.ogg'], i), 0.35)
    leaves = whoosh(rng, 0.3, 3000, 1500, q=0.8, peak_at=0.1)
    y = place([(hv, 0, 0), (soft, 0, -4), (knock, 0.01, -8), (thump(rng, 110, 40, 0.28, 0.09), 0, -5), (deb, 0.03, -16), (leaves, 0.02, -20)])
    return tail(y, rng, wet=0.14, seconds=0.7, decay=0.22)


def impact_sword(rng, i):
    # Longsword on armour: steel clash, plate clank, flesh under, a ringing recorded blade tail.
    clash = src(pick([SC + 'sword_clash.1.ogg', SC + 'sword_clash.3.ogg', SC + 'sword_clash.5.ogg', SC + 'sword_clash.7.ogg'], i), 0.35)
    plate = src(pick([K + 'impactPlate_medium_000.ogg', K + 'impactPlate_medium_002.ogg', K + 'impactPlate_medium_003.ogg'], i), 0.25)
    ring = src(pick([
        TS + 'sword-knife-clash-15.wav',
        TS + 'sword-knife-clash-22.wav',
        TS + 'sword-knife-clash-30.wav',
        SC + 'sword_clash.5.ogg',
    ], i), 0.4, rate=0.95)
    flesh = src(pick([K + 'impactPunch_heavy_000.ogg', K + 'impactPunch_heavy_002.ogg'], i), 0.15)
    y = place([(clash, 0, -1), (plate, 0, -6), (flesh, 0, -10), (hp(ring, 600), 0.01, -12)])
    return tail(y, rng, wet=0.16, seconds=0.8, decay=0.25)


def impact_dual(rng, i):
    # Kaelen's two blades connect: two quick slices with a light recorded steel tick each.
    s1 = src(pick([KR + 'knifeSlice.ogg', KR + 'knifeSlice2.ogg', R + 'blade_01.ogg'], i), 0.2)
    s2 = src(pick([R + 'blade_02.ogg', KR + 'knifeSlice.ogg', R + 'blade_03.ogg', KR + 'knifeSlice2.ogg'], i), 0.2, rate=1.12)
    t1 = src(pick([TS + 'sword-knife-clash-02.wav', TS + 'sword-knife-clash-08.wav', TS + 'sword-knife-clash-18.wav'], i), 0.12, rate=1.2)
    t2 = src(pick([TS + 'sword-knife-clash-09.wav', TS + 'sword-knife-clash-19.wav', TS + 'sword-knife-clash-25.wav'], i), 0.12, rate=1.28)
    p = src(pick([K + 'impactPunch_medium_001.ogg', K + 'impactPunch_medium_003.ogg'], i), 0.12, rate=1.2)
    gap = pick([0.07, 0.085, 0.075, 0.09], i)
    y = place([(s1, 0, 0), (p, 0, -9), (hp(t1, 1000), 0.003, -14), (s2, gap, -1), (hp(t2, 1000), gap + 0.003, -14), (p, gap, -12)])
    return tail(y, rng, wet=0.12, seconds=0.55, decay=0.17)


def impact_pierce(rng, i):
    # Seraphine's spear pierces: a sharp stab, a short steel scrape, a tight body thud.
    stab = src(pick([SN + 'sword.2.ogg', SN + 'sword.4.ogg', SN + 'sword.6.ogg', SN + 'sword.8.ogg'], i), 0.25)
    punch = src(pick([K + 'impactPunch_medium_000.ogg', K + 'impactPunch_medium_002.ogg', K + 'impactPunch_medium_004.ogg'], i), 0.15)
    scrape = src(pick([
        TS + 'sword-table-leg-scrape-01.wav',
        TS + 'sword-table-leg-scrape-02.wav',
        TS + 'sword-table-leg-scrape-03.wav',
        TS + 'seax-unsheathe-03.wav',
    ], i), 0.18, rate=1.15)
    y = place([(stab, 0, -1), (punch, 0, -5), (thump(rng, 150, 55, 0.18, 0.05), 0, -9), (hp(scrape, 700), 0.01, -14)])
    return tail(y, rng, wet=0.14, seconds=0.6, decay=0.2, damp=5500)


def impact_fire(rng, i):
    # Fireball bursts: a thump, recorded fire flare and crackle, a roaring fire bed.
    fl = src(pick([R + 'spell_fire_03.ogg', R + 'spell_fire_06.ogg', R + 'spell_fire_07.ogg'], i), 0.45)
    bed = fire_bed(i, 0.55, rate=pick([1.05, 0.98, 1.1], i))
    y = place([
        (thump(rng, 120, 45, 0.3, 0.09), 0, -3),
        (fire_roar(rng, 0.4, 150, 1800), 0, -6),
        (fl, 0, -4),
        (bed, 0.02, -7),
        (ember_crackle(rng, 0.55, 70, (400, 3200), tau=0.28), 0.05, -16),
    ])
    return tail(y, rng, wet=0.2, seconds=0.9, decay=0.3)


def impact_storm(rng, i):
    # Lightning strike as weather, not electricity: a thunder crack, earth rumble, wind, a little ice hail.
    crack = thunder_crack(i, dur=0.7, rate=pick([1.08, 1.02, 1.12], i))
    roll = rumble(rng, 0.7, 110)
    gust = wind_gust(i, 0.45, rate=1.15)
    hail = ice_crackle(i, 0.28, rate=1.2)
    y = place([
        (crack, 0, 0),
        (thump(rng, 160, 50, 0.25, 0.07), 0, -5),
        (roll, 0.02, -6),
        (gust, 0.0, -10),
        (hail, 0.02, -16),
    ])
    return tail(y, rng, wet=0.24, seconds=1.1, decay=0.35, damp=4000)


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
    # Shadow sorceress: a recorded eerie voice, a wind-cloth swirl, a dark low chant. No electronic ripple.
    vox = src(pick([C1 + 'weird_01.ogg', C1 + 'weird_03.ogg', C2 + 'weird_07.ogg'], i), 0.3, rate=1.15)
    gust = wind_gust(i, 0.4, rate=1.2)
    cloth = src(pick([KR + 'cloth3.ogg', KR + 'cloth1.ogg', KR + 'cloth4.ogg'], i), 0.2)
    chant = choir(rng, 0.4, pick([98, 87, 110], i), n_voices=4, swell=0.08)
    y = place([(gust, 0, -8), (cloth, 0, -14), (bp(vox, 300, 4500), 0, -4), (chant, 0.02, -12)])
    return tail(y, rng, wet=0.28, seconds=0.8, decay=0.25, damp=4500)


def hurt_blightroot(rng, i):
    # Rot tree-beast: wet bark crunch, a creak.
    cr = src(pick([W + 'wood_cracking_01.ogg', W + 'wood_cracking_02.ogg', W + 'wood_cracking_03.ogg', W + 'wood_cracking_04.ogg'], i), 0.25)
    sl = src(pick([C2 + 'slime_01.ogg', C2 + 'slime_03.ogg', C2 + 'slime_05.ogg'], i), 0.2)
    y = place([(cr, 0, 0), (sl, 0.0, -8), (thump(rng, 100, 45, 0.2, 0.06), 0, -7)])
    return tail(y, rng, wet=0.12, seconds=0.5, decay=0.15)


def hurt_ashwyrm(rng, i):
    # Void-fire wyrm: a scaly thud, a short snarl, a heavy wing beat.
    sn = src(pick([C1 + 'roar_01.ogg', C1 + 'roar_02.ogg', C2 + 'monster_10.ogg', C1 + 'monster_03.ogg'], i), 0.35, rate=0.7)
    sc = src(pick([K + 'impactPlank_medium_000.ogg', K + 'impactPlank_medium_002.ogg', K + 'impactPlank_medium_004.ogg'], i), 0.15, rate=0.8)
    flap = wing_flap(i, 0.4, rate=pick([0.75, 0.7, 0.8], i))
    y = place([(sc, 0, 0), (thump(rng, 90, 38, 0.25, 0.08), 0, -4), (lp(sn, 3500), 0.02, -6), (flap, 0.0, -12)])
    return tail(y, rng, wet=0.14, seconds=0.6, decay=0.2)


def hurt_malzareth(rng, i):
    # Armoured warlord: a plate clank, mail rattle, a gruff grunt.
    pl = src(pick([K + 'impactPlate_heavy_000.ogg', K + 'impactPlate_heavy_002.ogg', K + 'impactPlate_heavy_004.ogg'], i), 0.25)
    mail = src(pick([R + 'chain_01.ogg', R + 'chain_02.ogg', R + 'chain_03.ogg'], i), 0.2)
    gr = src(pick([C2 + 'human_01.ogg', C2 + 'human_03.ogg', C2 + 'human_05.ogg'], i), 0.3, rate=0.78)
    y = place([(pl, 0, 0), (mail, 0.01, -14), (thump(rng, 110, 45, 0.2, 0.06), 0, -6), (lp(gr, 3000), 0.02, -8)])
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
    smoke = whoosh(rng, 0.45, 1800, 400, q=0.8, peak_at=0.2)
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
    # Shadow sorceress fades: a falling eerie cry, a wind swirl, a distant chapel bell. No electronic shimmer.
    vox = src(pick([C1 + 'weird_02.ogg', C1 + 'weird_04.ogg', C2 + 'weird_08.ogg'], i), 0.6, rate=0.9)
    swirl = wind_gust(i, 0.6, rate=0.9)
    air = whoosh(rng, 0.7, 2200, 300, q=1.0, peak_at=0.3)
    bell = hp(chapel_bell(i, 0.65, rate=pick([0.85, 0.8, 0.9], i)), 250)
    chant = choir(rng, 0.7, pick([87, 98, 82], i), n_voices=5, swell=0.15)
    y = place([(bp(vox, 250, 4500), 0, -2), (swirl, 0.02, -8), (air, 0.02, -10), (bell, 0.1, -12), (chant, 0.05, -12)])
    return tail(y, rng, wet=0.32, seconds=1.1, decay=0.35, damp=4500)


def death_blightroot(rng, i):
    # Tree-beast splinters and crashes down.
    br = src(pick([W + 'wood_breaking_01.ogg', W + 'wood_breaking_02.ogg', B + 'bfh1_wood_breaking_01.ogg', B + 'bfh1_wood_breaking_03.ogg'], i), 0.6)
    groan = src(pick([C1 + 'troll_01.ogg', C1 + 'troll_02.ogg', C1 + 'troll_03.ogg'], i), 0.5, rate=0.65)
    y = place([(br, 0, 0), (lp(groan, 2200), 0, -8), (body_fall(rng, i, True), 0.3, -3)])
    return tail(y, rng, wet=0.16, seconds=0.8, decay=0.25)


def death_ashwyrm(rng, i):
    # Wyrm's death roar, a heavy wing collapse, a crash, fire dying out.
    roar = src(pick([C2 + 'roar_04.ogg', C2 + 'roar_05.ogg', C2 + 'roar_06.ogg'], i), 0.6, rate=0.72)
    flap = wing_flap(i, 0.5, rate=pick([0.65, 0.7, 0.6], i))
    bed = fire_bed(i, 0.5, rate=0.85)
    y = place([(lp(roar, 4000), 0, 0), (flap, 0.08, -8), (body_fall(rng, i, True), 0.32, -1), (bed, 0.35, -12)])
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
