import sys, os, pathlib; sys.path.insert(0,'.')
from cutout import fit
import numpy as np
from PIL import Image, ImageEnhance
R = sys.argv[1] if len(sys.argv) > 1 else '/workspace/paytoplayapp/Assets/ThemePack/fantasy_kingdom_a/'
OUT = 'art/world/'
L = lambda n: Image.open(f'out/{n}.png').convert('RGBA')
files = {}
def save(img, rel):
    p = R + OUT + rel; os.makedirs(os.path.dirname(p), exist_ok=True); img.save(p, optimize=True); files[rel] = img.size
# Castle tiers share one canvas size so the view scales them by tier, not by source framing.
for t in (1, 2, 3, 4):
    save(fit(L(f'castle_t{t}'), 512, 512, True), f'village/castle_t{t}.png')
save(fit(L('forge'), 512, 512, True), '../hook/painted/buildings/forge.png')
save(fit(L('plot_empty'), 384, 384, True), 'village/plot_empty.png')
save(fit(L('prop_bushes'), 256, 256, True), 'village/prop_bushes.png')
for n in ['village_player', 'village_rival', 'village_ally', 'camp_goblin', 'camp_bandit', 'camp_undead', 'camp_dragon',
          'node_gold', 'node_wood', 'node_food']:
    save(fit(L(n), 320, 320, True), f'map/{n}.png')
for n in ['prop_forest', 'prop_mountains', 'prop_lake']:
    save(fit(L(n), 384, 384, True), f'map/{n}.png')
for n in ['march_player', 'march_rival']:
    save(fit(L(n), 192, 192, True), f'map/{n}.png')
save(fit(L('level_badge'), 128, 128, True), 'ui/level_badge.png')
save(fit(L('icon_world'), 192, 192, True), 'ui/icon_world.png')
save(fit(L('icon_upgrade'), 128, 128, True), 'ui/icon_upgrade.png')

def tileable(src, size=512, bright=1.0, sat=1.0):
    im = Image.open(src).convert('RGB').resize((size, size), Image.LANCZOS)
    if bright != 1.0: im = ImageEnhance.Brightness(im).enhance(bright)
    if sat != 1.0: im = ImageEnhance.Color(im).enhance(sat)
    a = np.asarray(im).astype(np.float32)
    r = np.roll(np.roll(a, size // 2, 0), size // 2, 1)
    u = np.sin(np.linspace(0, np.pi, size)) ** 1.2
    w = np.outer(u, u)[..., None]
    out = a * w + r * (1 - w)
    return Image.fromarray(out.clip(0, 255).astype(np.uint8), 'RGB').convert('RGBA')
save(tileable('src/tile_meadow.jpg', 1024), 'map/tile_meadow.png')
save(tileable('src/tile_meadow.jpg', bright=1.12, sat=1.05), 'village/tile_lawn.png')
save(tileable('src/tile_dirt.jpg', 256), 'map/tile_dirt.png')
for k, v in files.items(): print(k, v)

# World ground: pull the meadow tile toward the painted crusade map's palette so the heart blends in.
terr = Image.open(R + 'art/map/THEME_A_WORLD_TERRAIN_PLAIN_1080x1920.png').convert('RGB')
t = np.asarray(terr).astype(np.float32)
g = np.asarray(Image.open(R + OUT + 'map/tile_meadow.png').convert('RGB')).astype(np.float32)
tm, ts = t.reshape(-1, 3).mean(0), t.reshape(-1, 3).std(0)
gm, gs = g.reshape(-1, 3).mean(0), g.reshape(-1, 3).std(0)
from PIL import ImageFilter
g = np.asarray(Image.fromarray(g.astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6))).astype(np.float32)
gm, gs = g.reshape(-1, 3).mean(0), g.reshape(-1, 3).std(0)
matched = (g - gm) / gs * (ts * 0.45) + tm * 0.97
mix = 0.9
g2 = (g * (1 - mix) + matched * mix).clip(0, 255).astype(np.uint8)
Image.fromarray(g2, 'RGB').convert('RGBA').save(R + OUT + 'map/tile_meadow.png', optimize=True)
# Heart: the crusade map with feathered edges (alpha ramps over the outer 14%).
h, w = t.shape[:2]
def ramp(n, frac):
    x = np.arange(n, dtype=np.float32)
    d = np.minimum(x, n - 1 - x) / (n * frac)
    d = np.clip(d, 0, 1)
    return d * d * (3 - 2 * d)
a = np.outer(ramp(h, 0.14), ramp(w, 0.14 * h / w))
rgba = np.dstack([t, a * 255]).clip(0, 255).astype(np.uint8)
Image.fromarray(rgba, 'RGBA').save(R + OUT + 'map/heart_feathered.png', optimize=True)
print('heart', (w, h))
