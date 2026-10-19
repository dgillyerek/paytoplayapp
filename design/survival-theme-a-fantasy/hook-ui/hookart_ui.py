"""Panels, buttons, scroll, ribbon, badge, bars for the Theme A hook UI kit."""
from PIL import ImageDraw
from hookart_base import *


def gold_frame(img, outer_box, r, thick):
    w, h = img.size
    m_out = mask_rrect(w, h, outer_box, r)
    x0, y0, x1, y1 = outer_box
    m_in = mask_rrect(w, h, (x0 + thick, y0 + thick, x1 - thick, y1 - thick), max(2, r - thick))
    ring = ImageChops.subtract(m_out, m_in)
    fill(img, ring, gold_fill(w, h))
    bevel(img, ring, max(2, thick // 4))
    # thin dark line inside / outside for crispness
    edge_o = ImageChops.subtract(m_out, shrink(m_out, max(1, thick // 10)))
    fill(img, edge_o, GOLD_DD + (220,))
    edge_i = ImageChops.subtract(grow(m_in, max(1, thick // 10)), m_in)
    fill(img, edge_i, GOLD_DD + (230,))
    return m_in


def corner_gem(img, cx, cy, s, colour=(40, 110, 230)):
    w, h = img.size
    dia = [(cx, cy - s), (cx + s, cy), (cx, cy + s), (cx - s, cy)]
    m = mask_poly(w, h, [(cx, cy - s * 1.35), (cx + s * 1.35, cy), (cx, cy + s * 1.35), (cx - s * 1.35, cy)])
    fill(img, m, gold_fill(w, h))
    bevel(img, m, max(2, s // 6))
    mg = mask_poly(w, h, dia)
    fill(img, mg, colour)
    fill(img, mask_poly(w, h, [(cx, cy - s), (cx + s, cy), (cx, cy)]), lerp(colour, (255, 255, 255), 0.45))
    fill(img, mask_poly(w, h, [(cx, cy - s), (cx - s, cy), (cx, cy)]), lerp(colour, (255, 255, 255), 0.25))
    fill(img, mask_poly(w, h, [(cx, cy + s), (cx + s, cy), (cx, cy)]), lerp(colour, (0, 0, 0), 0.35))
    specular(img, (cx - s * 0.55, cy - s * 0.75, cx - s * 0.05, cy - s * 0.3), 200)


def panel(W=384, H=384, interior=(NAVY, NAVY_D), gems=True):
    w, h = W * SS, H * SS
    img = canvas(W, H)
    pad = 10 * SS
    box = (pad, pad, w - pad, h - pad)
    r = 22 * SS
    m_all = mask_rrect(w, h, box, r)
    fill(img, m_all, vgrad(w, h, [(0, lerp(interior[0], (255, 255, 255), 0.08)), (0.5, interior[0]), (1, interior[1])]))
    noise_texture(img, m_all, 14, seed=3)
    m_in = gold_frame(img, box, r, 12 * SS)
    # inner hairline gold
    hb = (box[0] + 20 * SS, box[1] + 20 * SS, box[2] - 20 * SS, box[3] - 20 * SS)
    hl = ImageChops.subtract(mask_rrect(w, h, hb, r // 2), mask_rrect(w, h, (hb[0] + 2 * SS, hb[1] + 2 * SS, hb[2] - 2 * SS, hb[3] - 2 * SS), r // 2))
    fill(img, hl, GOLD + (150,))
    # inner vignette
    vig = ImageChops.subtract(m_in, blur(shrink(m_in, 18 * SS), 14 * SS))
    fill(img, vig.point(lambda v: v * 110 // 255), (0, 0, 10))
    if gems:
        for cx, cy in [(box[0] + 14 * SS, box[1] + 14 * SS), (box[2] - 14 * SS, box[1] + 14 * SS), (box[0] + 14 * SS, box[3] - 14 * SS), (box[2] - 14 * SS, box[3] - 14 * SS)]:
            corner_gem(img, cx, cy, 11 * SS)
    img = drop_shadow(img, m_all, (0, 5 * SS), 6 * SS, 160)
    return finish(img, W, H)


BUTTON_COLOURS = {
    "gold": [(0, (255, 226, 130)), (0.45, (238, 176, 52)), (0.55, (214, 146, 30)), (1, (150, 90, 16))],
    "green": [(0, (150, 232, 110)), (0.45, (70, 176, 60)), (0.55, (48, 150, 44)), (1, (22, 92, 28))],
    "blue": [(0, (120, 170, 255)), (0.45, (52, 98, 196)), (0.55, (36, 74, 160)), (1, (16, 36, 92))],
    "red": [(0, (255, 140, 120)), (0.45, (206, 52, 40)), (0.55, (178, 36, 28)), (1, (104, 16, 14))],
    "stone": [(0, (168, 162, 150)), (0.45, (118, 112, 102)), (0.55, (100, 94, 86)), (1, (60, 56, 52))],
}


def button(kind, W=320, H=120):
    w, h = W * SS, H * SS
    img = canvas(W, H)
    pad = 6 * SS
    box = (pad, pad, w - pad, h - pad - 4 * SS)
    r = 26 * SS
    m_all = mask_rrect(w, h, box, r)
    fill(img, m_all, gold_fill(w, h))
    bevel(img, m_all, 3 * SS)
    ib = (box[0] + 7 * SS, box[1] + 7 * SS, box[2] - 7 * SS, box[3] - 7 * SS)
    m_in = mask_rrect(w, h, ib, r - 7 * SS)
    fill(img, m_in, vgrad(w, h, [(t, c) for t, c in BUTTON_COLOURS[kind]]))
    fill(img, ImageChops.subtract(grow(m_in, SS), m_in), GOLD_DD + (230,))
    # gloss band
    gb = (ib[0] + 10 * SS, ib[1] + 4 * SS, ib[2] - 10 * SS, ib[1] + (ib[3] - ib[1]) * 0.45)
    gloss = mask_rrect(w, h, gb, r - 12 * SS)
    fill(img, blur(gloss, 3 * SS).point(lambda v: v * 95 // 255), (255, 255, 255))
    noise_texture(img, m_in, 8, seed=7)
    img = drop_shadow(img, m_all, (0, 5 * SS), 4 * SS, 170)
    return finish(img, W, H)


def chip(W=260, H=84):
    """Resource chip frame: dark navy pill with gold rim."""
    w, h = W * SS, H * SS
    img = canvas(W, H)
    pad = 4 * SS
    box = (pad, pad, w - pad, h - pad)
    r = (h - 2 * pad) // 2
    m_all = mask_rrect(w, h, box, r)
    fill(img, m_all, gold_fill(w, h))
    bevel(img, m_all, 2 * SS)
    ib = (box[0] + 5 * SS, box[1] + 5 * SS, box[2] - 5 * SS, box[3] - 5 * SS)
    m_in = mask_rrect(w, h, ib, r - 5 * SS)
    fill(img, m_in, vgrad(w, h, [(0, (34, 50, 92)), (0.5, (16, 26, 54)), (1, (10, 16, 36))]))
    gloss = mask_rrect(w, h, (ib[0] + 8 * SS, ib[1] + 2 * SS, ib[2] - 8 * SS, ib[1] + (ib[3] - ib[1]) * 0.4), r)
    fill(img, blur(gloss, 2 * SS).point(lambda v: v * 50 // 255), (255, 255, 255))
    return finish(img, W, H)


def bar_frame(W=360, H=48):
    w, h = W * SS, H * SS
    img = canvas(W, H)
    box = (2 * SS, 2 * SS, w - 2 * SS, h - 2 * SS)
    r = (h - 4 * SS) // 2
    m_all = mask_rrect(w, h, box, r)
    fill(img, m_all, gold_fill(w, h))
    bevel(img, m_all, 2 * SS)
    ib = (box[0] + 5 * SS, box[1] + 5 * SS, box[2] - 5 * SS, box[3] - 5 * SS)
    m_in = mask_rrect(w, h, ib, r - 5 * SS)
    fill(img, m_in, vgrad(w, h, [(0, (6, 8, 18)), (1, (26, 30, 48))]))
    return finish(img, W, H)


def bar_fill(kind="green", W=340, H=34):
    w, h = W * SS, H * SS
    img = canvas(W, H)
    r = h // 2
    m = mask_rrect(w, h, (0, 0, w, h), r)
    stops = {
        "green": [(0, (180, 255, 140)), (0.45, (82, 200, 70)), (1, (26, 110, 30))],
        "gold": [(0, (255, 240, 160)), (0.45, (246, 186, 56)), (1, (160, 96, 18))],
        "blue": [(0, (150, 210, 255)), (0.45, (60, 140, 240)), (1, (20, 60, 150))],
    }[kind]
    fill(img, m, vgrad(w, h, stops))
    gl = mask_rrect(w, h, (r // 2, 2 * SS, w - r // 2, h * 0.42), r // 2)
    fill(img, blur(gl, SS).point(lambda v: v * 120 // 255), (255, 255, 255))
    return finish(img, W, H)


def scroll(W=420, H=200):
    """Parchment scroll with rolled ends (quest tracker)."""
    w, h = W * SS, H * SS
    img = canvas(W, H)
    roll = 30 * SS
    body = (roll * 0.7, 14 * SS, w - roll * 0.7, h - 14 * SS)
    m_body = mask_rrect(w, h, body, 6 * SS)
    fill(img, m_body, vgrad(w, h, [(0, (246, 228, 186)), (0.5, (232, 206, 152)), (1, (204, 170, 110))]))
    noise_texture(img, m_body, 30, seed=11)
    burn = ImageChops.subtract(m_body, blur(shrink(m_body, 16 * SS), 12 * SS))
    fill(img, burn.point(lambda v: v * 150 // 255), (120, 70, 20))
    # rolls
    for x0 in (0, w - roll * 1.4):
        rb = (x0, 0, x0 + roll * 1.4, h)
        m_r = mask_rrect(w, h, rb, roll * 0.7)
        fill(img, m_r, Image.new("RGBA", (w, h), (0, 0, 0, 0)))
        col = Image.new("RGBA", (w, h))
        # horizontal gradient for cylinder
        g = vgrad(h, int(roll * 1.4), [(0, (150, 104, 50)), (0.3, (250, 232, 190)), (0.6, (222, 190, 130)), (1, (110, 70, 30))]).rotate(90, expand=True).transpose(Image.FLIP_LEFT_RIGHT)
        col.paste(g, (int(x0), 0))
        fill(img, m_r, col)
        # gold caps
        for cy in (0, h - 10 * SS):
            cap = mask_rrect(w, h, (x0 + 4 * SS, cy, x0 + roll * 1.4 - 4 * SS, cy + 10 * SS), 4 * SS)
            fill(img, cap, gold_fill(w, h))
    union = ImageChops.lighter(m_body, ImageChops.lighter(mask_rrect(w, h, (0, 0, roll * 1.4, h), roll * 0.7), mask_rrect(w, h, (w - roll * 1.4, 0, w, h), roll * 0.7)))
    img = drop_shadow(img, union, (0, 5 * SS), 6 * SS, 150)
    return finish(img, W, H)


def ribbon(W=360, H=110, colour=((236, 70, 52), (176, 30, 26), (110, 14, 12))):
    w, h = W * SS, H * SS
    img = canvas(W, H)
    tail = 46 * SS
    # tails (behind)
    for side in (0, 1):
        if side == 0:
            pts = [(0, h * 0.30), (tail * 1.6, h * 0.30), (tail * 1.6, h * 0.92), (0, h * 0.92), (tail * 0.55, h * 0.61)]
        else:
            pts = [(w, h * 0.30), (w - tail * 1.6, h * 0.30), (w - tail * 1.6, h * 0.92), (w, h * 0.92), (w - tail * 0.55, h * 0.61)]
        m = mask_poly(w, h, pts)
        fill(img, m, vgrad(w, h, [(0, colour[1]), (1, colour[2])]))
        fill(img, ImageChops.subtract(m, shrink(m, 3 * SS)), GOLD + (255,))
    # fold shadows
    for x in (tail * 1.1, w - tail * 1.1 - tail * 0.5):
        m = mask_poly(w, h, [(x, h * 0.7), (x + tail * 0.5, h * 0.7), (x + tail * 0.5 if x < w / 2 else x, h * 0.92), (x if x < w / 2 else x + tail * 0.5, h * 0.92)])
        fill(img, m, colour[2])
    # main band
    band = mask_poly(w, h, [(tail, h * 0.08), (w - tail, h * 0.08), (w - tail, h * 0.72), (tail, h * 0.72)])
    fill(img, band, vgrad(w, h, [(0, colour[0]), (0.5, colour[1]), (1, colour[2])]))
    trim = ImageChops.subtract(band, shrink(band, 5 * SS))
    fill(img, trim, gold_fill(w, h))
    bevel(img, trim, SS)
    gl = mask_poly(w, h, [(tail + 8 * SS, h * 0.14), (w - tail - 8 * SS, h * 0.14), (w - tail - 8 * SS, h * 0.32), (tail + 8 * SS, h * 0.32)])
    fill(img, blur(gl, 2 * SS).point(lambda v: v * 70 // 255), (255, 255, 255))
    img = drop_shadow(img, ImageChops.lighter(band, band), (0, 4 * SS), 4 * SS, 150)
    return finish(img, W, H)


def badge_free(W=200, H=200):
    w, h = W * SS, H * SS
    img = canvas(W, H)
    cx, cy = w / 2, h / 2
    burst = mask_poly(w, h, star_pts(cx, cy, w * 0.48, w * 0.38, 16))
    fill(img, burst, gold_fill(w, h))
    bevel(img, burst, 3 * SS)
    disc = mask_ellipse(w, h, (w * 0.17, h * 0.17, w * 0.83, h * 0.83))
    fill(img, disc, radial(w, h, (140, 240, 100, 255), (24, 110, 30, 255), 0.42, 0.36))
    fill(img, ImageChops.subtract(grow(disc, 2 * SS), disc), GOLD_DD + (255,))
    specular(img, (w * 0.28, h * 0.22, w * 0.62, h * 0.42), 130)
    img = glow_under(img, burst, (255, 230, 120), 10 * SS, 220)
    return finish(img, W, H)


def header(W=720, H=140):
    """Sheet title banner: navy cloth plate with gold frame and crest notches."""
    w, h = W * SS, H * SS
    img = canvas(W, H)
    pts = [(w * 0.04, h * 0.18), (w * 0.96, h * 0.18), (w, h * 0.5), (w * 0.96, h * 0.82), (w * 0.04, h * 0.82), (0, h * 0.5)]
    m = mask_poly(w, h, pts)
    fill(img, m, gold_fill(w, h))
    bevel(img, m, 3 * SS)
    inner = shrink(m, 7 * SS)
    fill(img, inner, vgrad(w, h, [(0, NAVY_L), (0.5, NAVY), (1, NAVY_D)]))
    noise_texture(img, inner, 10, seed=5)
    fill(img, ImageChops.subtract(grow(inner, SS), inner), GOLD_DD + (255,))
    for x in (w * 0.08, w * 0.92):
        corner_gem(img, x, h * 0.5, 13 * SS)
    img = drop_shadow(img, m, (0, 5 * SS), 6 * SS, 170)
    return finish(img, W, H)


def plate(W=300, H=300):
    """Stone plaque with gold rim used behind building art on the base strip."""
    w, h = W * SS, H * SS
    img = canvas(W, H)
    box = (6 * SS, 6 * SS, w - 6 * SS, h - 6 * SS)
    r = 30 * SS
    m = mask_rrect(w, h, box, r)
    fill(img, m, gold_fill(w, h))
    bevel(img, m, 3 * SS)
    inner = mask_rrect(w, h, (box[0] + 9 * SS, box[1] + 9 * SS, box[2] - 9 * SS, box[3] - 9 * SS), r - 9 * SS)
    fill(img, inner, radial(w, h, (64, 96, 160, 255), (12, 22, 48, 255), 0.5, 0.42, 0.9))
    noise_texture(img, inner, 12, seed=9)
    fill(img, ImageChops.subtract(grow(inner, SS), inner), GOLD_DD + (255,))
    img = drop_shadow(img, m, (0, 6 * SS), 6 * SS, 170)
    return finish(img, W, H)
