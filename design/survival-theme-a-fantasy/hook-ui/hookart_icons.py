"""Resource icons, chest, gift, gem tiers and FX sprites for the Theme A hook UI kit."""
import math, random
from PIL import ImageDraw
from hookart_base import *


def _coin(img, cx, cy, rx, ry, thick):
    w, h = img.size
    # edge (cylinder side)
    side = mask_ellipse(w, h, (cx - rx, cy - ry + thick, cx + rx, cy + ry + thick))
    rect = mask_poly(w, h, [(cx - rx, cy), (cx + rx, cy), (cx + rx, cy + thick), (cx - rx, cy + thick)])
    side = ImageChops.lighter(side, rect)
    fill(img, side, vgrad(w, h, [(0, (190, 120, 30)), (1, (110, 64, 12))]))
    for k in range(1, 12):
        x = cx - rx + 2 * rx * k / 12
    face = mask_ellipse(w, h, (cx - rx, cy - ry, cx + rx, cy + ry))
    fill(img, face, radial(w, h, GOLD_L + (255,), (200, 136, 36, 255), cx / w - rx / w * 0.25, cy / h - ry / h * 0.35, 1.2))
    inner = mask_ellipse(w, h, (cx - rx * 0.78, cy - ry * 0.78, cx + rx * 0.78, cy + ry * 0.78))
    fill(img, ImageChops.subtract(face, inner).point(lambda v: v * 120 // 255), (150, 92, 20))
    fill(img, inner, radial(w, h, (255, 222, 120, 255), (214, 150, 44, 255), cx / w, cy / h - ry / h * 0.3, 1.0))
    # embossed star
    st = mask_poly(w, h, [(cx + x * rx * 0.48, cy + y * ry * 0.48) for x, y in [((p[0] - 0) , (p[1])) for p in [(math.cos(math.radians(-90 + i * 36)) * (1 if i % 2 == 0 else 0.45), math.sin(math.radians(-90 + i * 36)) * (1 if i % 2 == 0 else 0.45)) for i in range(10)]]])
    fill(img, ImageChops.offset(st, SS, SS * 2), (140, 84, 16, 200))
    fill(img, st, (255, 236, 160, 255))
    specular(img, (cx - rx * 0.6, cy - ry * 0.8, cx + rx * 0.1, cy - ry * 0.25), 150)


def icon_gold(S=256):
    w = h = S * SS
    img = canvas(S, S)
    rx, ry, t = w * 0.26, h * 0.13, h * 0.05
    for cx, cy in [(w * 0.36, h * 0.74), (w * 0.36, h * 0.66), (w * 0.36, h * 0.58), (w * 0.64, h * 0.76), (w * 0.64, h * 0.68)]:
        _coin(img, cx, cy, rx, ry, t)
    # one upright coin in front
    _coin_up(img, w * 0.55, h * 0.42, w * 0.25)
    m = img.getchannel("A")
    img = drop_shadow(img, m, (0, 3 * SS), 5 * SS, 140)
    return finish(img, S, S)


def _coin_up(img, cx, cy, r):
    w, h = img.size
    rim = mask_ellipse(w, h, (cx - r * 0.92, cy - r, cx + r * 0.92 + r * 0.12, cy + r))
    fill(img, rim, vgrad(w, h, [(0, (176, 110, 24)), (1, (98, 56, 10))]))
    face = mask_ellipse(w, h, (cx - r * 0.92, cy - r, cx + r * 0.92, cy + r))
    fill(img, face, radial(w, h, GOLD_L + (255,), (196, 130, 32, 255), (cx - r * 0.3) / w, (cy - r * 0.4) / h, 1.1))
    inner = mask_ellipse(w, h, (cx - r * 0.7, cy - r * 0.76, cx + r * 0.7, cy + r * 0.76))
    fill(img, ImageChops.subtract(face, inner).point(lambda v: v * 90 // 255), (130, 80, 14))
    st = mask_poly(w, h, star_pts(cx, cy + r * 0.04, r * 0.48, r * 0.2, 5))
    fill(img, ImageChops.offset(st, SS * 2, SS * 3), (120, 70, 10, 220))
    fill(img, st, vgrad(w, h, [(0, (255, 246, 196)), (1, (236, 176, 64))]))
    specular(img, (cx - r * 0.6, cy - r * 0.85, cx, cy - r * 0.3), 170)


def _gem(img, cx, cy, s, base=(40, 200, 240), tilt=0.0):
    """Brilliant-cut gem seen from the front, s = half width."""
    w, h = img.size
    top = cy - s * 0.55
    mid = cy - s * 0.1
    bot = cy + s * 0.95
    P = lambda x, y: (cx + x * s + tilt * (y - cy), y)
    outline = [P(-0.55, top), P(0.55, top), P(1.0, mid), (cx + tilt * (bot - cy), bot), P(-1.0, mid)]
    m = mask_poly(w, h, outline)
    light = lerp(base, (255, 255, 255), 0.55)
    lighter = lerp(base, (255, 255, 255), 0.8)
    dark = lerp(base, (0, 0, 30), 0.45)
    darker = lerp(base, (0, 0, 30), 0.65)
    fill(img, m, base)
    facets = [
        ([P(-0.55, top), P(-0.2, top), P(-0.35, mid)], lighter),
        ([P(-0.2, top), P(0.2, top), P(0.0, mid)], light),
        ([P(0.2, top), P(0.55, top), P(0.35, mid)], base),
        ([P(-0.55, top), P(-0.35, mid), P(-1.0, mid)], light),
        ([P(0.55, top), P(1.0, mid), P(0.35, mid)], dark),
        ([P(-0.2, top), P(0.0, mid), P(-0.35, mid)], base),
        ([P(0.2, top), P(0.35, mid), P(0.0, mid)], lighter),
        ([P(-1.0, mid), P(-0.35, mid), (cx + tilt * (bot - cy), bot)], base),
        ([P(-0.35, mid), P(0.0, mid), (cx + tilt * (bot - cy), bot)], light),
        ([P(0.0, mid), P(0.35, mid), (cx + tilt * (bot - cy), bot)], dark),
        ([P(0.35, mid), P(1.0, mid), (cx + tilt * (bot - cy), bot)], darker),
    ]
    for pts, col in facets:
        fill(img, mask_poly(w, h, pts), col)
    edge = ImageChops.subtract(m, shrink(m, max(1, int(s * 0.04))))
    fill(img, edge, lerp(base, (0, 0, 40), 0.7) + (230,))
    sp = mask_poly(w, h, star_pts(*P(-0.38, top + s * 0.18), s * 0.32, s * 0.06, 4, -90))
    fill(img, sp, (255, 255, 255, 230))


def icon_gem(S=256):
    w = h = S * SS
    img = canvas(S, S)
    _gem(img, w * 0.5, h * 0.42, w * 0.40)
    m = img.getchannel("A")
    img = glow_under(img, m, (90, 220, 255), 9 * SS, 140)
    img = drop_shadow(img, m, (0, 4 * SS), 5 * SS, 120)
    return finish(img, S, S)


def _log(img, x0, y0, x1, y1, seed):
    w, h = img.size
    r = (y1 - y0) / 2
    body = mask_rrect(w, h, (x0, y0, x1, y1), r)
    fill(img, body, vgrad(w, h, [(0, (0, 0, 0))]) if False else Image.new("RGBA", (w, h), (0, 0, 0, 0)))
    col = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    g = vgrad(int(x1 - x0), int(y1 - y0), [(0, (150, 96, 52)), (0.35, (118, 72, 36)), (0.8, (72, 42, 20)), (1, (50, 28, 12))])
    col.paste(g, (int(x0), int(y0)))
    fill(img, body, col)
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for k in range(6):
        yy = y0 + (y1 - y0) * (0.2 + 0.12 * k) + rnd.uniform(-4, 4) * SS
        d.line([(x0 + r, yy), (x1 - r * 1.6, yy + rnd.uniform(-3, 3) * SS)], fill=(54, 30, 12, 140), width=int(SS * 1.6))
    # end grain
    ex = x1 - r * 1.05
    end = mask_ellipse(w, h, (ex - r * 0.75, y0, ex + r * 0.75 + r * 0.25, y1))
    fill(img, end, (110, 70, 34))
    e2 = mask_ellipse(w, h, (ex - r * 0.62, y0 + r * 0.12, ex + r * 0.88, y1 - r * 0.12))
    fill(img, e2, radial(w, h, (244, 206, 140, 255), (196, 146, 84, 255), (ex + r * 0.13) / w, (y0 + r) / h, 1.0))
    for k in range(1, 4):
        rr = r * 0.7 * k / 4
        d.ellipse((ex + r * 0.13 - rr * 0.95, y0 + r - rr, ex + r * 0.13 + rr * 0.95, y0 + r + rr), outline=(160, 108, 56, 200), width=int(SS * 1.5))


def icon_wood(S=256):
    w = h = S * SS
    img = canvas(S, S)
    lh = h * 0.2
    _log(img, w * 0.10, h * 0.30, w * 0.80, h * 0.30 + lh, 1)
    _log(img, w * 0.06, h * 0.50, w * 0.70, h * 0.50 + lh, 2)
    _log(img, w * 0.26, h * 0.50, w * 0.94, h * 0.50 + lh, 3)
    # rope band
    band = mask_poly(w, h, [(w * 0.40, h * 0.28), (w * 0.47, h * 0.28), (w * 0.47, h * 0.72), (w * 0.40, h * 0.72)])
    fill(img, ImageChops.multiply(band, img.getchannel("A")), vgrad(w, h, [(0, (230, 196, 120)), (1, (150, 110, 50))]))
    m = img.getchannel("A")
    img = drop_shadow(img, m, (0, 4 * SS), 5 * SS, 140)
    return finish(img, S, S)


def _block(img, cx, cy, s, tone):
    """Isometric cut stone block centred on its top face."""
    w, h = img.size
    top = [(cx, cy - s * 0.5), (cx + s, cy), (cx, cy + s * 0.5), (cx - s, cy)]
    left = [(cx - s, cy), (cx, cy + s * 0.5), (cx, cy + s * 1.25), (cx - s, cy + s * 0.75)]
    right = [(cx + s, cy), (cx, cy + s * 0.5), (cx, cy + s * 1.25), (cx + s, cy + s * 0.75)]
    base = tone
    for pts, k in [(left, 0.0), (right, -0.28), (top, 0.32)]:
        m = mask_poly(w, h, pts)
        c = lerp(base, (255, 255, 255), k) if k > 0 else lerp(base, (0, 0, 0), -k)
        fill(img, m, c)
        noise_texture(img, m, 22, seed=int(cx + cy) % 97)
        e = ImageChops.subtract(m, shrink(m, SS))
        fill(img, e, (40, 36, 34, 200))
    hl = mask_poly(w, h, [(cx - s, cy), (cx, cy - s * 0.5), (cx + s, cy), (cx + s * 0.92, cy), (cx, cy - s * 0.42), (cx - s * 0.92, cy)])
    fill(img, hl, (255, 255, 240, 120))


def icon_stone(S=256):
    w = h = S * SS
    img = canvas(S, S)
    s = w * 0.22
    _block(img, w * 0.30, h * 0.50, s, (150, 146, 138))
    _block(img, w * 0.70, h * 0.50, s, (138, 134, 128))
    _block(img, w * 0.50, h * 0.62, s, (160, 156, 146))
    _block(img, w * 0.50, h * 0.30, s, (146, 142, 136))
    m = img.getchannel("A")
    img = drop_shadow(img, m, (0, 4 * SS), 5 * SS, 140)
    return finish(img, S, S)


def icon_food(S=256):
    """Burlap grain sack tied with a gold cord, wheat ears spilling from the top."""
    w = h = S * SS
    img = canvas(S, S)
    d = ImageDraw.Draw(img)
    # wheat ears behind the neck
    for k in range(7):
        ang = math.radians(-90 + (k - 3) * 13)
        bx, by = w * 0.5, h * 0.36
        L = h * 0.26
        tx, ty = bx + math.cos(ang) * L, by + math.sin(ang) * L
        d.line([(bx, by), (tx, ty)], fill=(176, 130, 50, 255), width=int(SS * 3))
        ear = Image.new("L", (w, h), 0)
        ImageDraw.Draw(ear).ellipse((tx - w * 0.028, ty - h * 0.07, tx + w * 0.028, ty + h * 0.03), fill=255)
        ear = ear.rotate(-math.degrees(ang) - 90, center=(tx, ty), resample=Image.BICUBIC)
        fill(img, ear, vgrad(w, h, [(0, (255, 226, 130)), (1, (214, 158, 60))]))
        fill(img, ImageChops.subtract(ear, shrink(ear, SS)), (150, 100, 30, 220))
    # sack body
    body = mask_poly(w, h, [(w * 0.38, h * 0.40), (w * 0.62, h * 0.40), (w * 0.80, h * 0.56), (w * 0.84, h * 0.80), (w * 0.74, h * 0.90), (w * 0.26, h * 0.90), (w * 0.16, h * 0.80), (w * 0.20, h * 0.56)])
    body = blur(body, 6 * SS).point(lambda v: 255 if v > 128 else 0)
    fill(img, body, radial(w, h, (230, 196, 140, 255), (128, 90, 50, 255), 0.42, 0.55, 1.0))
    noise_texture(img, body, 26, seed=14)
    for k in range(5):
        x = w * (0.30 + 0.1 * k)
        d.line([(x, h * 0.50), (x + (k - 2) * w * 0.02, h * 0.88)], fill=(120, 84, 44, 110), width=int(SS * 2))
    # neck + cord
    neck = mask_poly(w, h, [(w * 0.36, h * 0.30), (w * 0.64, h * 0.30), (w * 0.60, h * 0.44), (w * 0.40, h * 0.44)])
    fill(img, neck, vgrad(w, h, [(0, (236, 206, 150)), (1, (160, 116, 66))]))
    cord = mask_rrect(w, h, (w * 0.36, h * 0.40, w * 0.64, h * 0.46), 3 * SS)
    fill(img, cord, gold_fill(w, h))
    bevel(img, cord, SS)
    # emblem: wheat stamp in navy
    em = mask_ellipse(w, h, (w * 0.39, h * 0.58, w * 0.61, h * 0.80))
    fill(img, em, NAVY + (220,))
    fill(img, ImageChops.subtract(em, shrink(em, 2 * SS)), GOLD + (255,))
    d.line([(w * 0.5, h * 0.62), (w * 0.5, h * 0.77)], fill=GOLD + (255,), width=int(SS * 2.5))
    for j in range(3):
        y = h * (0.63 + j * 0.04)
        d.line([(w * 0.5, y + h * 0.02), (w * 0.455, y)], fill=GOLD + (255,), width=int(SS * 2.5))
        d.line([(w * 0.5, y + h * 0.02), (w * 0.545, y)], fill=GOLD + (255,), width=int(SS * 2.5))
    specular(img, (w * 0.26, h * 0.52, w * 0.46, h * 0.66), 80)
    m = img.getchannel("A")
    img = drop_shadow(img, m, (0, 4 * SS), 5 * SS, 140)
    return finish(img, S, S)


def icon_speedup(S=256):
    """Gold hourglass with blue glass and sand, small wings."""
    w = h = S * SS
    img = canvas(S, S)
    cx = w / 2
    # wings
    for side in (-1, 1):
        for k in range(3):
            fx = cx + side * w * (0.20 + 0.07 * k)
            fm = mask_ellipse(w, h, (min(fx, fx + side * w * 0.18), h * (0.30 + 0.06 * k), max(fx, fx + side * w * 0.18), h * (0.40 + 0.06 * k)))
            fill(img, fm, lerp((250, 250, 255), (160, 190, 240), k / 3))
            fill(img, ImageChops.subtract(fm, shrink(fm, SS)), (90, 110, 160, 200))
    glass = mask_poly(w, h, [(cx - w * 0.2, h * 0.2), (cx + w * 0.2, h * 0.2), (cx + w * 0.04, h * 0.5), (cx + w * 0.2, h * 0.8), (cx - w * 0.2, h * 0.8), (cx - w * 0.04, h * 0.5)])
    fill(img, glass, (150, 200, 255, 170))
    sand_top = mask_poly(w, h, [(cx - w * 0.13, h * 0.32), (cx + w * 0.13, h * 0.32), (cx + w * 0.03, h * 0.48), (cx - w * 0.03, h * 0.48)])
    sand_bot = mask_poly(w, h, [(cx, h * 0.6), (cx + w * 0.17, h * 0.78), (cx - w * 0.17, h * 0.78)])
    for sm in (sand_top, sand_bot):
        fill(img, sm, vgrad(w, h, [(0, (255, 226, 140)), (1, (214, 150, 50))]))
    ImageDraw.Draw(img).line([(cx, h * 0.48), (cx, h * 0.62)], fill=(240, 196, 100, 255), width=int(SS * 2))
    specular(img, (cx - w * 0.16, h * 0.22, cx - w * 0.06, h * 0.42), 150)
    for y0 in (h * 0.13, h * 0.79):
        cap = mask_rrect(w, h, (cx - w * 0.27, y0, cx + w * 0.27, y0 + h * 0.08), 4 * SS)
        fill(img, cap, gold_fill(w, h))
        bevel(img, cap, 2 * SS)
    for x in (cx - w * 0.23, cx + w * 0.19):
        post = mask_rrect(w, h, (x, h * 0.2, x + w * 0.04, h * 0.8), 2 * SS)
        fill(img, post, gold_fill(w, h))
    m = img.getchannel("A")
    img = glow_under(img, m, (120, 190, 255), 8 * SS, 110)
    img = drop_shadow(img, m, (0, 4 * SS), 5 * SS, 120)
    return finish(img, S, S)


def _chest_body(img, x0, y0, x1, y1):
    w, h = img.size
    body = mask_rrect(w, h, (x0, y0, x1, y1), 4 * SS)
    fill(img, body, vgrad(w, h, [(0, (46, 74, 140)), (1, (16, 28, 64))]))
    # wood planks (navy painted) lines
    d = ImageDraw.Draw(img)
    for k in range(1, 3):
        yy = y0 + (y1 - y0) * k / 3
        d.line([(x0, yy), (x1, yy)], fill=(10, 18, 40, 200), width=int(SS * 1.5))
    noise_texture(img, body, 16, seed=21)
    # gold bands
    bw = (x1 - x0) * 0.09
    for bx in (x0 + (x1 - x0) * 0.16, x1 - (x1 - x0) * 0.16 - bw):
        b = mask_rrect(w, h, (bx, y0, bx + bw, y1), 2 * SS)
        fill(img, b, gold_fill(w, h))
        bevel(img, b, 2 * SS)
    rim = ImageChops.subtract(body, shrink(body, int(SS * 4)))
    fill(img, rim, gold_fill(w, h))
    bevel(img, rim, SS)
    return body


def _lock(img, cx, cy, s):
    w, h = img.size
    plate = mask_rrect(w, h, (cx - s, cy - s * 0.9, cx + s, cy + s * 1.1), 3 * SS)
    fill(img, plate, gold_fill(w, h))
    bevel(img, plate, 2 * SS)
    kh = mask_ellipse(w, h, (cx - s * 0.25, cy - s * 0.25, cx + s * 0.25, cy + s * 0.25))
    fill(img, kh, (40, 20, 4))
    fill(img, mask_poly(w, h, [(cx - s * 0.12, cy), (cx + s * 0.12, cy), (cx + s * 0.2, cy + s * 0.65), (cx - s * 0.2, cy + s * 0.65)]), (40, 20, 4))


def _lid(img, x0, y0, x1, y1):
    w, h = img.size
    lid = mask_rrect(w, h, (x0, y0, x1, y1 + (y1 - y0)), (y1 - y0) * 0.9)
    clip = mask_poly(w, h, [(0, 0), (w, 0), (w, y1), (0, y1)])
    lid = ImageChops.multiply(lid, clip)
    fill(img, lid, vgrad(w, h, [(0, (70, 106, 186)), (0.6, (30, 52, 108)), (1, (20, 36, 80))]))
    noise_texture(img, lid, 16, seed=22)
    bw = (x1 - x0) * 0.09
    for bx in (x0 + (x1 - x0) * 0.16, x1 - (x1 - x0) * 0.16 - bw):
        b = ImageChops.multiply(lid, mask_poly(w, h, [(bx, 0), (bx + bw, 0), (bx + bw, h), (bx, h)]))
        fill(img, b, gold_fill(w, h))
        bevel(img, b, 2 * SS)
    rim = ImageChops.subtract(lid, shrink(lid, int(SS * 4)))
    fill(img, rim, gold_fill(w, h))
    specular(img, (x0 + (x1 - x0) * 0.3, y0 + (y1 - y0) * 0.08, x0 + (x1 - x0) * 0.6, y0 + (y1 - y0) * 0.35), 110)
    return lid


def chest_closed(S=320):
    w = h = S * SS
    img = canvas(S, S)
    x0, x1 = w * 0.12, w * 0.88
    _chest_body(img, x0, h * 0.50, x1, h * 0.86)
    _lid(img, x0 - w * 0.01, h * 0.24, x1 + w * 0.01, h * 0.52)
    _lock(img, w * 0.5, h * 0.53, w * 0.07)
    m = img.getchannel("A")
    img = drop_shadow(img, m, (0, 5 * SS), 6 * SS, 160)
    return finish(img, S, S)


def chest_open(S=320):
    w = h = S * SS
    img = canvas(S, S)
    x0, x1 = w * 0.12, w * 0.88
    # glow from inside
    gl = mask_ellipse(w, h, (w * 0.1, h * 0.05, w * 0.9, h * 0.75))
    fill(img, blur(gl, 30 * SS).point(lambda v: v * 200 // 255), (255, 226, 130))
    # open lid (back, tilted up)
    lid = mask_poly(w, h, [(x0 + w * 0.02, h * 0.50), (x1 - w * 0.02, h * 0.50), (x1 - w * 0.06, h * 0.22), (x0 + w * 0.06, h * 0.22)])
    fill(img, lid, vgrad(w, h, [(0, (30, 52, 108)), (1, (12, 22, 52))]))
    fill(img, ImageChops.subtract(lid, shrink(lid, 4 * SS)), gold_fill(w, h))
    # treasure heap
    rnd = random.Random(8)
    heap = mask_ellipse(w, h, (x0 + w * 0.02, h * 0.42, x1 - w * 0.02, h * 0.62))
    fill(img, heap, radial(w, h, (255, 230, 130, 255), (200, 130, 30, 255), 0.5, 0.48, 1.0))
    for k in range(16):
        cx = rnd.uniform(x0 + w * 0.1, x1 - w * 0.1)
        cy = rnd.uniform(h * 0.44, h * 0.54)
        _coin(img, cx, cy, w * 0.05, h * 0.024, h * 0.012)
    for cx, cy, col in [(w * 0.38, h * 0.44, (40, 200, 240)), (w * 0.62, h * 0.45, (230, 60, 120)), (w * 0.5, h * 0.40, (90, 230, 120))]:
        _gem(img, cx, cy, w * 0.06, col)
    _chest_body(img, x0, h * 0.52, x1, h * 0.86)
    _lock(img, w * 0.5, h * 0.56, w * 0.065)
    for k in range(6):
        sx, sy = rnd.uniform(w * 0.2, w * 0.8), rnd.uniform(h * 0.12, h * 0.42)
        fill(img, mask_poly(w, h, star_pts(sx, sy, w * 0.04, w * 0.008, 4)), (255, 255, 230, 240))
    m = img.getchannel("A")
    img = drop_shadow(img, m, (0, 5 * SS), 6 * SS, 120)
    return finish(img, S, S)


def gift(S=256):
    w = h = S * SS
    img = canvas(S, S)
    box = mask_rrect(w, h, (w * 0.16, h * 0.42, w * 0.84, h * 0.88), 5 * SS)
    fill(img, box, vgrad(w, h, [(0, (60, 96, 180)), (1, (18, 34, 80))]))
    lid = mask_rrect(w, h, (w * 0.10, h * 0.30, w * 0.90, h * 0.46), 5 * SS)
    fill(img, lid, vgrad(w, h, [(0, (90, 130, 220)), (1, (36, 60, 130))]))
    for m in (box, lid):
        noise_texture(img, m, 10, seed=31)
    rib = mask_poly(w, h, [(w * 0.44, h * 0.30), (w * 0.56, h * 0.30), (w * 0.56, h * 0.88), (w * 0.44, h * 0.88)])
    fill(img, rib, gold_fill(w, h))
    bevel(img, rib, 2 * SS)
    for side in (-1, 1):
        loop = mask_ellipse(w, h, (w * 0.5 + side * w * 0.02 - (w * 0.24 if side < 0 else 0), h * 0.12, w * 0.5 + side * w * 0.02 + (w * 0.24 if side > 0 else 0), h * 0.34))
        hole = mask_ellipse(w, h, (w * 0.5 + side * w * 0.07 - (w * 0.12 if side < 0 else 0), h * 0.18, w * 0.5 + side * w * 0.07 + (w * 0.12 if side > 0 else 0), h * 0.28))
        lm = ImageChops.subtract(loop, hole)
        fill(img, lm, gold_fill(w, h))
        bevel(img, lm, 2 * SS)
    knot = mask_ellipse(w, h, (w * 0.43, h * 0.22, w * 0.57, h * 0.34))
    fill(img, knot, gold_fill(w, h))
    bevel(img, knot, 2 * SS)
    m = img.getchannel("A")
    img = drop_shadow(img, m, (0, 4 * SS), 5 * SS, 140)
    return finish(img, S, S)


GEM_COLS = [(40, 200, 240), (60, 150, 255), (90, 220, 250)]


def gems_tier(n, S=256):
    """Gem pile: n = 1 (handful), 2 (pile), 3 (chest of gems)."""
    w = h = S * SS
    img = canvas(S, S)
    rnd = random.Random(40 + n)
    if n == 3:
        x0, x1 = w * 0.12, w * 0.88
        gl = mask_ellipse(w, h, (w * 0.08, h * 0.05, w * 0.92, h * 0.7))
        fill(img, blur(gl, 26 * SS).point(lambda v: v * 170 // 255), (120, 220, 255))
        for k in range(14):
            _gem(img, rnd.uniform(w * 0.24, w * 0.76), rnd.uniform(h * 0.32, h * 0.48), w * rnd.uniform(0.07, 0.10), GEM_COLS[k % 3], rnd.uniform(-0.2, 0.2))
        _chest_body(img, x0, h * 0.5, x1, h * 0.86)
        _lock(img, w * 0.5, h * 0.55, w * 0.065)
    else:
        count = 4 if n == 1 else 10
        spots = sorted([(rnd.uniform(w * 0.25, w * 0.75), rnd.uniform(h * 0.42, h * 0.62) if n == 2 else rnd.uniform(h * 0.5, h * 0.62)) for _ in range(count)], key=lambda p: p[1])
        if n == 2:
            spots += [(w * 0.5, h * 0.36), (w * 0.4, h * 0.42), (w * 0.6, h * 0.42)]
            spots.sort(key=lambda p: p[1])
        for k, (x, y) in enumerate(spots):
            _gem(img, x, y, w * (0.15 if n == 1 else 0.12), GEM_COLS[k % 3], rnd.uniform(-0.25, 0.25))
    m = img.getchannel("A")
    img = glow_under(img, m, (90, 220, 255), 8 * SS, 120)
    img = drop_shadow(img, m, (0, 4 * SS), 5 * SS, 120)
    return finish(img, S, S)


# ---------- FX ----------

def fx_rays(S=512, n=16):
    w = h = S * SS
    img = canvas(S, S)
    cx = cy = w / 2
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    for i in range(n):
        a0 = math.radians(i * 360 / n - 360 / n * 0.22)
        a1 = math.radians(i * 360 / n + 360 / n * 0.22)
        d.polygon([(cx, cy), (cx + math.cos(a0) * w, cy + math.sin(a0) * h), (cx + math.cos(a1) * w, cy + math.sin(a1) * h)], fill=255)
    fall = radial(w, h, (255, 255, 255, 255), (0, 0, 0, 255), 0.5, 0.5, 0.8).getchannel("R")
    m = ImageChops.multiply(blur(m, 3 * SS), fall)
    layer = Image.new("RGBA", (w, h), (255, 236, 170, 0))
    layer.putalpha(m)
    img.alpha_composite(layer)
    return finish(img, S, S)


def fx_glow(S=256):
    w = h = S * SS
    img = canvas(S, S)
    a = radial(w, h, (255, 255, 255, 255), (0, 0, 0, 255), 0.5, 0.5, 0.6).getchannel("R")
    a = a.point(lambda v: int(255 * (v / 255.0) ** 2.2))
    layer = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    layer.putalpha(a)
    img.alpha_composite(layer)
    return finish(img, S, S)


def fx_sparkle(S=128):
    w = h = S * SS
    img = canvas(S, S)
    cx = cy = w / 2
    m = mask_poly(w, h, star_pts(cx, cy, w * 0.48, w * 0.07, 4))
    m2 = mask_poly(w, h, star_pts(cx, cy, w * 0.26, w * 0.05, 4, -45))
    core = blur(mask_ellipse(w, h, (cx - w * 0.12, cy - h * 0.12, cx + w * 0.12, cy + h * 0.12)), 4 * SS)
    a = ImageChops.lighter(ImageChops.lighter(blur(m, SS), blur(m2, SS)), core)
    layer = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    layer.putalpha(a)
    img.alpha_composite(layer)
    return finish(img, S, S)


def fx_burst_ring(S=256):
    w = h = S * SS
    img = canvas(S, S)
    outer = mask_ellipse(w, h, (w * 0.04, h * 0.04, w * 0.96, h * 0.96))
    inner = mask_ellipse(w, h, (w * 0.16, h * 0.16, w * 0.84, h * 0.84))
    ring = blur(ImageChops.subtract(outer, inner), 6 * SS)
    layer = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    layer.putalpha(ring)
    img.alpha_composite(layer)
    return finish(img, S, S)


def fx_confetti(S=64):
    """White confetti strip with soft shading (tinted at runtime)."""
    w, h = S * SS, S * SS // 2
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    m = mask_rrect(w, h, (0, 0, w, h), 4 * SS)
    fill(img, m, vgrad(w, h, [(0, (255, 255, 255)), (0.5, (235, 235, 235)), (1, (180, 180, 180))]))
    return img.resize((S, S // 2), Image.LANCZOS)
