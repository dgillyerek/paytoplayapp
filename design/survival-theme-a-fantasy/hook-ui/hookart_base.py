"""Shared drawing helpers for the Theme A hook UI kit (supersampled PIL)."""
import math
from PIL import Image, ImageDraw, ImageFilter, ImageChops

SS = 4  # supersample factor

NAVY = (27, 48, 96)       # #1b3060 hero blue
NAVY_D = (14, 26, 56)
NAVY_L = (52, 84, 150)
GOLD = (232, 186, 82)
GOLD_L = (255, 232, 150)
GOLD_D = (140, 92, 28)
GOLD_DD = (86, 52, 14)


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def finish(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(len(a)))


def vgrad(w, h, stops):
    """Vertical gradient RGBA image; stops = [(t, (r,g,b[,a])), ...]."""
    col = Image.new("RGBA", (1, h))
    px = col.load()
    for y in range(h):
        t = y / max(1, h - 1)
        for i in range(len(stops) - 1):
            t0, c0 = stops[i]
            t1, c1 = stops[i + 1]
            if t0 <= t <= t1:
                k = (t - t0) / max(1e-6, t1 - t0)
                c = lerp(c0 + (255,) if len(c0) == 3 else c0, c1 + (255,) if len(c1) == 3 else c1, k)
                px[0, y] = c
                break
    return col.resize((w, h))


def radial(w, h, inner, outer, cx=0.5, cy=0.5, power=1.0):
    import numpy as np
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    dx = (xs - cx * w) / (w * 0.5)
    dy = (ys - cy * h) / (h * 0.5)
    d = np.clip(np.sqrt(dx * dx + dy * dy), 0, 1) ** power
    a = np.array(inner, np.float32); b = np.array(outer, np.float32)
    arr = a[None, None, :] + (b - a)[None, None, :] * d[..., None]
    return Image.fromarray(arr.round().astype(np.uint8), "RGBA")


def mask_rrect(w, h, box, r):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle(box, radius=r, fill=255)
    return m


def mask_poly(w, h, pts):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return m


def mask_ellipse(w, h, box):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).ellipse(box, fill=255)
    return m


def fill(img, mask, src):
    """Composite src (RGBA image or colour) onto img through mask."""
    if isinstance(src, tuple):
        src = Image.new("RGBA", img.size, src if len(src) == 4 else src + (255,))
    elif src.size != img.size:
        full = Image.new("RGBA", img.size, (0, 0, 0, 0))
        full.paste(src, (0, 0))
        src = full
    layer = src.copy()
    a = ImageChops.multiply(layer.getchannel("A"), mask)
    layer.putalpha(a)
    img.alpha_composite(layer)


def shrink(mask, px):
    """Erode a soft mask by px using a distance transform (fast for large radii)."""
    if px <= 0:
        return mask
    import numpy as np
    from scipy.ndimage import distance_transform_edt
    a = np.asarray(mask, dtype=np.float32) / 255.0
    inside = a > 0.5
    d = distance_transform_edt(inside)
    out = np.clip(d - px + 0.5, 0, 1) * 255
    return Image.fromarray(out.astype(np.uint8), "L")


def grow(mask, px):
    if px <= 0:
        return mask
    import numpy as np
    from scipy.ndimage import distance_transform_edt
    a = np.asarray(mask, dtype=np.float32) / 255.0
    d = distance_transform_edt(a <= 0.5)
    out = np.clip(px + 0.5 - d, 0, 1) * 255
    return Image.fromarray(out.astype(np.uint8), "L")


def blur(mask, r):
    return mask.filter(ImageFilter.GaussianBlur(r))


def gold_fill(w, h):
    return vgrad(w, h, [(0, GOLD_L), (0.35, GOLD), (0.62, (196, 140, 50)), (0.8, GOLD_L), (1, GOLD_D)])


def bevel(img, mask, depth, light=110, dark=120):
    """Inner bevel: highlight top-left edge, shadow bottom-right edge."""
    inner = shrink(mask, depth)
    edge = ImageChops.subtract(mask, inner)
    w, h = img.size
    off_hi = ImageChops.offset(mask, depth, depth)
    hi = ImageChops.subtract(edge, off_hi)
    off_lo = ImageChops.offset(mask, -depth, -depth)
    lo = ImageChops.subtract(edge, off_lo)
    fill(img, blur(hi, depth * 0.4).point(lambda v: v * light // 255), (255, 255, 230))
    fill(img, blur(lo, depth * 0.4).point(lambda v: v * dark // 255), (30, 14, 0))


def drop_shadow(img, mask, off=(0, 6), r=8, alpha=150):
    sh = blur(ImageChops.offset(mask, off[0], off[1]), r).point(lambda v: v * alpha // 255)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    layer.putalpha(sh)
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.alpha_composite(layer)
    out.alpha_composite(img)
    return out


def glow_under(img, mask, colour, r, alpha=200):
    g = blur(grow(mask, int(r * 0.4)), r).point(lambda v: min(255, v * alpha // 255))
    layer = Image.new("RGBA", img.size, colour + (0,))
    layer.putalpha(g)
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.alpha_composite(layer)
    out.alpha_composite(img)
    return out


def star_pts(cx, cy, r_out, r_in, n, rot=-90):
    pts = []
    for i in range(n * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(rot + i * 180.0 / n)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def specular(img, box, alpha=120):
    w, h = img.size
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).ellipse(box, fill=alpha)
    fill(img, blur(m, (box[2] - box[0]) * 0.18), (255, 255, 255))


def noise_texture(img, mask, amount=18, seed=1):
    import random
    rnd = random.Random(seed)
    w, h = img.size
    g = 9 * SS
    n = Image.new("L", (w // g + 1, h // g + 1))
    n.putdata([128 + rnd.randint(-amount, amount) for _ in range((w // g + 1) * (h // g + 1))])
    n = n.resize((w, h), Image.BICUBIC)
    n = n.filter(ImageFilter.GaussianBlur(2 * SS))
    dark = n.point(lambda v: int(max(0, 128 - v) * 1.4))
    light = n.point(lambda v: int(max(0, v - 128) * 0.9))
    fill(img, ImageChops.multiply(dark, mask), (0, 0, 0))
    fill(img, ImageChops.multiply(light, mask), (255, 240, 210))
