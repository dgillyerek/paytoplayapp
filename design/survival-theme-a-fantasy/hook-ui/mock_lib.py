"""Tiny uGUI-like layout helpers for 1080x1920 mock composites of the hook screens (mirrors the C# anchors)."""
import json, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
PACK = os.path.join(ROOT, "Assets", "ThemePack", "fantasy_kingdom_a")
W, H = 1080, 1920
PACKJ = json.load(open(os.path.join(PACK, "pack.json")))
HUD = json.load(open(os.path.join(PACK, "ui", "hud_layout.json")))
STR = json.load(open(os.path.join(PACK, "strings", "en.json")))
PINS = json.load(open(os.path.join(PACK, "map", "node_pins.json")))["pins"]
FONT = "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
FONTB = "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf"
_cache = {}

CREAM = (237, 230, 209)
GOLD = (212, 176, 82)
REWARD = (237, 214, 115)
MUTE = (178, 184, 173)
INK = (51, 28, 8)
PINK = (69, 38, 13)
PTITLE = (115, 51, 10)
FREE = (77, 209, 97)
WARN = (255, 140, 115)
HARD = (107, 219, 255)
PANEL = (13, 16, 23, 235)


def S(key, fb=""):
    return STR.get(key, fb)


def art(key):
    rel = PACKJ["art"].get(key)
    if rel is None:
        return None
    if rel not in _cache:
        _cache[rel] = Image.open(os.path.join(PACK, rel)).convert("RGBA")
    return _cache[rel]


def slices(key):
    return PACKJ.get("art_slices", {}).get(key)


class R:
    """Rect in screen px, y from top."""

    def __init__(self, x0, y0, x1, y1):
        self.x0, self.y0, self.x1, self.y1 = x0, y0, x1, y1

    @property
    def w(self):
        return self.x1 - self.x0

    @property
    def h(self):
        return self.y1 - self.y0

    def box(self, ax0, ay0, ax1, ay1):
        """Unity-style child anchors (y from bottom) relative to this rect."""
        return R(self.x0 + ax0 * self.w, self.y1 - ay1 * self.h, self.x0 + ax1 * self.w, self.y1 - ay0 * self.h)

    def center_box(self, cx, cy, w, h):
        return self.box(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)


SCREEN = R(0, 0, W, H)


def hud(key):
    r = HUD[key]
    return R(r["x_min"] * W, r["y_top_min"] * H, r["x_max"] * W, r["y_top_max"] * H)


def paste(dst, img, r, alpha=1.0, rot=0):
    w, h = max(1, int(round(r.w))), max(1, int(round(r.h)))
    im = img.resize((w, h), Image.LANCZOS)
    if rot:
        im = im.rotate(rot, expand=True, resample=Image.BICUBIC)
    if alpha < 1:
        a = im.getchannel("A").point(lambda v: int(v * alpha))
        im.putalpha(a)
    ox = int(round(r.x0 + r.w / 2 - im.width / 2))
    oy = int(round(r.y0 + r.h / 2 - im.height / 2))
    dst.alpha_composite(im, (ox, oy)) if 0 <= ox and 0 <= oy and ox + im.width <= dst.width and oy + im.height <= dst.height else _safe(dst, im, ox, oy)


def _safe(dst, im, ox, oy):
    layer = Image.new("RGBA", dst.size, (0, 0, 0, 0))
    layer.paste(im, (ox, oy), im)
    dst.alpha_composite(layer)


def fit(dst, img, r, alpha=1.0, rot=0, tint=None):
    """preserveAspect."""
    if img is None:
        return
    s = min(r.w / img.width, r.h / img.height)
    w, h = img.width * s, img.height * s
    cx, cy = r.x0 + r.w / 2, r.y0 + r.h / 2
    im = img
    if tint is not None:
        im = tinted(img, tint)
    paste(dst, im, R(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2), alpha, rot)


def tinted(img, tint):
    r, g, b = [c / 255 for c in tint[:3]]
    out = img.copy()
    ch = out.split()
    out = Image.merge("RGBA", (ch[0].point(lambda v: int(v * r)), ch[1].point(lambda v: int(v * g)), ch[2].point(lambda v: int(v * b)), ch[3]))
    if len(tint) == 4:
        out.putalpha(ch[3].point(lambda v: int(v * tint[3] / 255)))
    return out


def sliced(dst, key, r, scale=1.0, tint=None, fallback=PANEL):
    img = art(key)
    if img is None:
        ImageDraw.Draw(dst).rectangle((r.x0, r.y0, r.x1, r.y1), fill=fallback)
        return
    if tint is not None:
        img = tinted(img, tint)
    b = slices(key)
    if not b:
        paste(dst, img, r)
        return
    l, bo, ri, t = b
    W_, H_ = int(round(r.w)), int(round(r.h))
    dl, dr, dt, db = [int(round(v * scale)) for v in (l, ri, t, bo)]
    # shrink borders if the target is smaller (Unity does the same)
    if dl + dr > W_:
        k = W_ / (dl + dr); dl, dr = int(dl * k), int(dr * k)
    if dt + db > H_:
        k = H_ / (dt + db); dt, db = int(dt * k), int(db * k)
    sw, sh = img.size
    xs = [0, l, sw - ri, sw]
    ys = [0, t, sh - bo, sh]
    xd = [0, dl, W_ - dr, W_]
    yd = [0, dt, H_ - db, H_]
    out = Image.new("RGBA", (W_, H_), (0, 0, 0, 0))
    for i in range(3):
        for j in range(3):
            src = img.crop((xs[i], ys[j], xs[i + 1], ys[j + 1]))
            tw, th = xd[i + 1] - xd[i], yd[j + 1] - yd[j]
            if tw <= 0 or th <= 0 or src.width <= 0 or src.height <= 0:
                continue
            out.alpha_composite(src.resize((tw, th), Image.LANCZOS), (xd[i], yd[j]))
    _safe(dst, out, int(round(r.x0)), int(round(r.y0)))


def font(size, bold=False):
    return ImageFont.truetype(FONTB if bold else FONT, int(size))


def text(dst, r, s, size, colour, anchor="mc", bold=False, outline=True, od=2):
    d = ImageDraw.Draw(dst)
    f = font(size, bold)
    ax = {"l": r.x0, "m": (r.x0 + r.x1) / 2, "r": r.x1}[anchor[0]]
    ay = {"t": r.y0, "c": (r.y0 + r.y1) / 2, "b": r.y1}[anchor[1]]
    pa = {"l": "l", "m": "m", "r": "r"}[anchor[0]] + {"t": "a", "c": "m", "b": "d"}[anchor[1]]
    if outline:
        d.text((ax, ay + od * 1.6), s, font=f, fill=(0, 0, 0, 140), anchor=pa)
        d.text((ax, ay), s, font=f, fill=colour, anchor=pa, stroke_width=od, stroke_fill=(10, 8, 15, 230))
    else:
        d.text((ax, ay), s, font=f, fill=colour, anchor=pa)


def dim(dst, a=200):
    dst.alpha_composite(Image.new("RGBA", dst.size, (5, 8, 13, a)))
