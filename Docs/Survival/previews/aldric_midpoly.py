#!/usr/bin/env python3
"""Mid-poly Sir Aldric: atlas + bone-local mesh matching look_targets SoT.

Not a DCC sculpt. Plate/surcoat volumes + locked lion / Greek-key crops so the
high-angle rear Game view reads as the painted knight, not a grey capsule.
Scabbard = character-right (+X) per 01_rear_LOCKED. 02 turnaround is ref only.
"""
from __future__ import annotations

import json
import math
import uuid
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[3]
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"

# Sampled from 01_rear_LOCKED (not invented primaries).
SILVER = (218, 214, 208)
GOLD = (174, 146, 100)
BLUE = (48, 78, 148)
BROWN = (85, 61, 38)
CREAM = (236, 228, 210)

# Atlas UV centers / rects (U right, V up — Unity style).
UV = {
    "silver": (0.125, 0.875),
    "gold": (0.375, 0.875),
    "blue": (0.625, 0.875),
    "brown": (0.875, 0.875),
    "lion": (0.02, 0.08, 0.48, 0.72),  # u0,v0,u1,v1
    "hem": (0.52, 0.62, 0.98, 0.78),
}


def _uv(name):
    return UV[name]


def box_tris(center, size, uv):
    c = np.array(center, np.float64)
    e = np.array(size, np.float64) * 0.5
    # 8 corners: -x-y-z, +x-y-z, +x+y-z, -x+y-z, -x-y+z, +x-y+z, +x+y+z, -x+y+z
    p = np.array(
        [
            c + [-e[0], -e[1], -e[2]],
            c + [e[0], -e[1], -e[2]],
            c + [e[0], e[1], -e[2]],
            c + [-e[0], e[1], -e[2]],
            c + [-e[0], -e[1], e[2]],
            c + [e[0], -e[1], e[2]],
            c + [e[0], e[1], e[2]],
            c + [-e[0], e[1], e[2]],
        ]
    )
    faces = (
        (0, 1, 2, 3, (0, 0, -1)),
        (5, 4, 7, 6, (0, 0, 1)),
        (4, 0, 3, 7, (-1, 0, 0)),
        (1, 5, 6, 2, (1, 0, 0)),
        (3, 2, 6, 7, (0, 1, 0)),
        (4, 5, 1, 0, (0, -1, 0)),
    )
    u = np.array(uv, np.float64)
    tris = []
    for a, b, c2, d, n in faces:
        n = np.array(n, np.float64)
        tris.append((p[a], p[b], p[c2], n, n, n, u, u, u))
        tris.append((p[a], p[c2], p[d], n, n, n, u, u, u))
    return tris


def ellipsoid_tris(center, radii, uv, slices=8, stacks=5):
    c = np.array(center, np.float64)
    rx, ry, rz = radii
    u = np.array(uv, np.float64)
    pts = []
    for y in range(stacks + 1):
        v = y / stacks
        phi = v * math.pi
        row = []
        for x in range(slices + 1):
            th = x / slices * math.tau
            n = np.array([math.sin(phi) * math.cos(th), math.cos(phi), math.sin(phi) * math.sin(th)])
            p = c + n * np.array([rx, ry, rz])
            nn = n / np.array([rx, ry, rz])
            nn = nn / (np.linalg.norm(nn) + 1e-8)
            row.append((p, nn))
        pts.append(row)
    tris = []
    for y in range(stacks):
        for x in range(slices):
            p0, n0 = pts[y][x]
            p1, n1 = pts[y][x + 1]
            p2, n2 = pts[y + 1][x]
            p3, n3 = pts[y + 1][x + 1]
            tris.append((p0, p2, p1, n0, n2, n1, u, u, u))
            tris.append((p1, p2, p3, n1, n2, n3, u, u, u))
    return tris


def capsule_tris(a, b, radius, uv, rings=4, segs=7):
    a = np.array(a, np.float64)
    b = np.array(b, np.float64)
    axis = b - a
    height = np.linalg.norm(axis)
    if height < 1e-5:
        return ellipsoid_tris(a, (radius, radius, radius), uv)
    ny = axis / height
    ref = np.array([0.0, 1.0, 0.0]) if abs(ny[1]) < 0.9 else np.array([1.0, 0.0, 0.0])
    nx = np.cross(ref, ny)
    nx /= np.linalg.norm(nx)
    nz = np.cross(ny, nx)
    u = np.array(uv, np.float64)
    rings_pts = []
    for i in range(rings + 1):
        t = i / rings
        p = a * (1 - t) + b * t
        ring = []
        for s in range(segs):
            ang = s / segs * math.tau
            radial = math.cos(ang) * nx + math.sin(ang) * nz
            ring.append((p + radial * radius, radial))
        rings_pts.append(ring)
    tris = []
    for i in range(rings):
        for s in range(segs):
            s1 = (s + 1) % segs
            p00, n00 = rings_pts[i][s]
            p01, n01 = rings_pts[i][s1]
            p10, n10 = rings_pts[i + 1][s]
            p11, n11 = rings_pts[i + 1][s1]
            tris.append((p00, p10, p01, n00, n10, n01, u, u, u))
            tris.append((p01, p10, p11, n01, n10, n11, u, u, u))
    tris.extend(ellipsoid_tris(a, (radius, radius, radius), uv, slices=segs, stacks=3))
    tris.extend(ellipsoid_tris(b, (radius, radius, radius), uv, slices=segs, stacks=3))
    return tris


def _lion_uv(x, y, x0=-0.15, x1=0.15, y0=-0.06, y1=0.20):
    # Tight crop of the gold lion body in the atlas (not the blue padding).
    lu0, lv0, lu1, lv1 = 0.08, 0.30, 0.39, 0.63
    u = lu0 + (lu1 - lu0) * float(np.clip((x - x0) / (x1 - x0), 0, 1))
    v = lv0 + (lv1 - lv0) * float(np.clip((y - y0) / (y1 - y0), 0, 1))
    return (u, v)


def _back_uv(p, n, solid):
    # Lion lives on the tilted badge (lion_panel_tris), not smeared across the skirt.
    return solid


def ellipsoid_tris_surcoat(center, radii, slices=10, stacks=6):
    c = np.array(center, np.float64)
    rx, ry, rz = radii
    solid = _uv("blue")
    pts = []
    for y in range(stacks + 1):
        phi = y / stacks * math.pi
        row = []
        for x in range(slices + 1):
            th = x / slices * math.tau
            n = np.array([math.sin(phi) * math.cos(th), math.cos(phi), math.sin(phi) * math.sin(th)])
            p = c + n * np.array([rx, ry, rz])
            nn = n / np.array([rx, ry, rz])
            nn = nn / (np.linalg.norm(nn) + 1e-8)
            row.append((p, nn, _back_uv(p, nn, solid)))
        pts.append(row)
    tris = []
    for y in range(stacks):
        for x in range(slices):
            p0, n0, u0 = pts[y][x]
            p1, n1, u1 = pts[y][x + 1]
            p2, n2, u2 = pts[y + 1][x]
            p3, n3, u3 = pts[y + 1][x + 1]
            tris.append((p0, p2, p1, n0, n2, n1, u0, u2, u1))
            tris.append((p1, p2, p3, n1, n2, n3, u1, u2, u3))
    return tris


def flare_tris(y0, y1, r0, r1, uv, rings=5, segs=12, z_back=0.04):
    """Open flared skirt. Back (−Z) is slightly larger so the hem reads from high-rear."""
    solid = np.array(uv, np.float64)
    rings_pts = []
    for i in range(rings + 1):
        t = i / rings
        y = y0 * (1 - t) + y1 * t
        rad = r0 * (1 - t) + r1 * t
        ring = []
        for s in range(segs):
            ang = s / segs * math.tau
            # ang 0 = +X; sin=−1 at −Z (camera / back).
            back = 1.0 + z_back * max(0.0, -math.sin(ang))
            p = np.array([math.cos(ang) * rad * back, y, math.sin(ang) * rad * back])
            n = np.array([math.cos(ang), 0.15, math.sin(ang)])
            n = n / (np.linalg.norm(n) + 1e-8)
            ring.append((p, n, _back_uv(p, n, solid)))
        rings_pts.append(ring)
    tris = []
    for i in range(rings):
        for s in range(segs):
            s1 = (s + 1) % segs
            p00, n00, u00 = rings_pts[i][s]
            p01, n01, u01 = rings_pts[i][s1]
            p10, n10, u10 = rings_pts[i + 1][s]
            p11, n11, u11 = rings_pts[i + 1][s1]
            tris.append((p00, p10, p01, n00, n10, n01, u00, u10, u01))
            tris.append((p01, p10, p11, n01, n10, n11, u01, u10, u11))
    return tris


def hem_ring_tris(y, r, h=0.035, segs=16):
    """Greek-key band: U wraps the atlas hem strip."""
    u0, v0, u1, v1 = UV["hem"]
    tris = []
    for s in range(segs):
        a0 = s / segs * math.tau
        a1 = (s + 1) / segs * math.tau
        uu0 = u0 + (u1 - u0) * (s / segs)
        uu1 = u0 + (u1 - u0) * ((s + 1) / segs)
        r_out, r_in = r + 0.012, r - 0.004
        y0, y1 = y, y + h
        def pt(rad, ang, yy):
            return np.array([math.cos(ang) * rad, yy, math.sin(ang) * rad])
        n = np.array([math.cos((a0 + a1) * 0.5), 0.0, math.sin((a0 + a1) * 0.5)])
        # outer face (visible)
        p00, p10 = pt(r_out, a0, y0), pt(r_out, a1, y0)
        p01, p11 = pt(r_out, a0, y1), pt(r_out, a1, y1)
        uv00, uv10 = (uu0, v0), (uu1, v0)
        uv01, uv11 = (uu0, v1), (uu1, v1)
        tris.append((p00, p10, p11, n, n, n, uv00, uv10, uv11))
        tris.append((p00, p11, p01, n, n, n, uv00, uv11, uv01))
        # top lip gold
        ug = _uv("gold")
        nup = np.array([0.0, 1.0, 0.0])
        tris.append((p01, p11, pt(r_in, a1, y1), nup, nup, nup, ug, ug, ug))
        tris.append((p01, pt(r_in, a1, y1), pt(r_in, a0, y1), nup, nup, nup, ug, ug, ug))
    return tris


def lion_panel_tris():
    """Tilted back badge facing the high-rear camera so the locked lion reads."""
    # Atlas lion body (gold on blue), not the padded full tile.
    u0, v0, u1, v1 = 0.08, 0.30, 0.39, 0.63
    n = np.array([0.0, 0.42, -0.91])
    n = n / np.linalg.norm(n)
    tangent = np.array([1.0, 0.0, 0.0])
    bitan = np.cross(n, tangent)
    bitan = bitan / (np.linalg.norm(bitan) + 1e-8)
    center = np.array([0.0, 0.05, -0.155])
    hw, hh = 0.135, 0.165
    nu, nv = 7, 8
    tris = []
    for iy in range(nv):
        ty0, ty1 = iy / nv, (iy + 1) / nv
        for ix in range(nu):
            tx0, tx1 = ix / nu, (ix + 1) / nu
            def corner(tx, ty):
                p = center + tangent * hw * (tx * 2 - 1) + bitan * hh * (ty * 2 - 1)
                uv = (u0 + (u1 - u0) * tx, v0 + (v1 - v0) * ty)
                return p, uv
            p00, uv00 = corner(tx0, ty0)
            p10, uv10 = corner(tx1, ty0)
            p11, uv11 = corner(tx1, ty1)
            p01, uv01 = corner(tx0, ty1)
            tris.append((p00, p10, p11, n, n, n, uv00, uv10, uv11))
            tris.append((p00, p11, p01, n, n, n, uv00, uv11, uv01))
    return tris


def _fill_swatch(draw, box, rgb):
    draw.rectangle(box, fill=rgb)
    # Soft bevel so plates are not flat plastic.
    x0, y0, x1, y1 = box
    hi = tuple(min(255, c + 28) for c in rgb)
    lo = tuple(max(0, c - 28) for c in rgb)
    draw.rectangle((x0, y0, x1, y0 + 10), fill=hi)
    draw.rectangle((x0, y1 - 10, x1, y1), fill=lo)


def _clean_on_blue(src, box):
    crop = src.crop(box).convert("RGBA")
    arr = np.array(crop)
    r, g, b, a = [arr[:, :, i] for i in range(4)]
    white = (r > 230) & (g > 230) & (b > 230)
    empty = a < 20
    # keep gold + blue; replace paper/empty with royal blue
    keep_gold = (r > 130) & (g > 100) & (b < 170) & (r > b + 15)
    keep_blue = (b > 55) & (b > r + 15) & (a > 20)
    keep = keep_gold | keep_blue
    out = arr.copy()
    out[white | empty | ~keep] = (*BLUE, 255)
    # gold stays
    out[keep_gold] = arr[keep_gold]
    out[keep_gold, 3] = 255
    out[keep_blue & ~keep_gold] = arr[keep_blue & ~keep_gold]
    out[keep_blue & ~keep_gold, 3] = 255
    return Image.fromarray(out, "RGBA")


def _draw_greek_key(img, box):
    """Procedural gold-on-cream Greek key, then composite over the locked hem crop."""
    x0, y0, x1, y1 = box
    tile = Image.new("RGB", (x1 - x0, y1 - y0), CREAM)
    d = ImageDraw.Draw(tile)
    w, h = tile.size
    step = 28
    thick = 5
    y_mid = h // 2
    for x in range(-step, w + step, step):
        # classic square spiral motif
        d.rectangle((x + 2, y_mid - 10, x + 2 + thick, y_mid + 10), fill=GOLD)
        d.rectangle((x + 2, y_mid - 10, x + 18, y_mid - 10 + thick), fill=GOLD)
        d.rectangle((x + 18 - thick, y_mid - 10, x + 18, y_mid + 4), fill=GOLD)
        d.rectangle((x + 8, y_mid + 4 - thick, x + 18, y_mid + 4), fill=GOLD)
        d.rectangle((x + 8, y_mid - 2, x + 8 + thick, y_mid + 10), fill=GOLD)
        d.rectangle((x + 8, y_mid + 10 - thick, x + 26, y_mid + 10), fill=GOLD)
    # overlay locked hem gold if present
    rear = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
    hem = _clean_on_blue(rear, (352, 618, 692, 700))
    hem = hem.resize((w, h), Image.BILINEAR)
    ha = np.array(hem)
    gold = (ha[:, :, 0] > 130) & (ha[:, :, 1] > 100) & (ha[:, :, 0] > ha[:, :, 2] + 15)
    base = np.array(tile)
    base[gold] = ha[gold][:, :3]
    tile = Image.fromarray(base, "RGB")
    img.paste(tile, (x0, y0))


def build_atlas():
    img = Image.new("RGB", (1024, 1024), (20, 22, 24))
    d = ImageDraw.Draw(img)
    _fill_swatch(d, (16, 16, 240, 240), SILVER)
    _fill_swatch(d, (272, 16, 496, 240), GOLD)
    _fill_swatch(d, (528, 16, 752, 240), BLUE)
    _fill_swatch(d, (784, 16, 1008, 240), BROWN)
    rear = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
    # Lion crop from 01_rear (primary Game-view SoT).
    lion = _clean_on_blue(rear, (380, 190, 640, 500))
    lion = lion.resize((460, 640), Image.LANCZOS)
    # Atlas V-up: paste in lower-left in image space (y grows down) → UV v 0.08–0.72
    # image y = 1024*(1-v)
    # v1=0.72 → y=286; v0=0.08 → y=942; height=656
    img.paste(lion.convert("RGB"), (24, 286))
    _draw_greek_key(img, (532, 224, 1000, 388))
    # extra gold/silver plates for boots
    _fill_swatch(d, (528, 420, 752, 560), SILVER)
    _fill_swatch(d, (784, 420, 1008, 560), GOLD)
    return img


def knight_parts():
    """Bone-local mid-poly. Names match SirAldric3DActor."""
    s, g, bl, br = _uv("silver"), _uv("gold"), _uv("blue"), _uv("brown")
    parts = []

    def add(bone, tris):
        parts.append((bone, tris))

    # Helm — silver bowl, gold crest + visor rim (crest reads from high-rear).
    add("Head", ellipsoid_tris((0, 0.14, 0.01), (0.125, 0.155, 0.135), s, slices=10, stacks=6))
    add("Head", box_tris((0, 0.30, 0.0), (0.035, 0.08, 0.04), g))
    add("Head", box_tris((0, 0.34, 0.0), (0.07, 0.025, 0.03), g))
    add("Head", capsule_tris((0, 0.06, -0.12), (0, 0.22, -0.12), 0.016, g, rings=3, segs=6))
    add("Head", box_tris((0, 0.02, 0.0), (0.10, 0.02, 0.12), g))
    add("Neck", capsule_tris((0, -0.03, 0), (0, 0.08, 0), 0.065, s, rings=3, segs=6))
    add("Neck", box_tris((0, 0.02, 0), (0.14, 0.03, 0.12), g))

    # Torso — royal-blue surcoat; back-facing tris carry the locked lion.
    add("Chest", ellipsoid_tris_surcoat((0, 0.02, 0), (0.18, 0.17, 0.13), slices=12, stacks=7))
    add("Chest", lion_panel_tris())
    add("Spine", ellipsoid_tris_surcoat((0, 0.02, 0), (0.16, 0.11, 0.12), slices=10, stacks=5))
    add("Hips", ellipsoid_tris_surcoat((0, -0.04, 0), (0.17, 0.13, 0.13), slices=10, stacks=5))
    add("Hips", flare_tris(-0.10, -0.38, 0.17, 0.22, bl, rings=5, segs=14))
    add("Hips", hem_ring_tris(-0.40, 0.22, h=0.05))

    # Belt + character-left pouch (01_rear). Gold buckle.
    add("Hips", box_tris((0, -0.16, 0), (0.34, 0.035, 0.22), br))
    add("Hips", box_tris((0, -0.16, -0.12), (0.06, 0.04, 0.03), g))
    add("Hips", box_tris((-0.16, -0.20, -0.06), (0.07, 0.06, 0.04), br))

    # Pauldrons — silver plates + gold rims.
    for x in (-0.22, 0.22):
        add("Chest", box_tris((x, 0.12, 0.0), (0.14, 0.10, 0.16), s))
        add("Chest", box_tris((x, 0.18, 0.0), (0.10, 0.03, 0.12), g))
        add("Chest", ellipsoid_tris((x, 0.12, 0.0), (0.09, 0.07, 0.09), s, slices=7, stacks=4))

    # Arms — silver plate, gold joints / gauntlet cuffs.
    for side, bone_a, bone_f, bone_h in (
        ("L", "Arm_L", "Fore_L", "Hand_L"),
        ("R", "Arm_R", "Fore_R", "Hand_R"),
    ):
        add(bone_a, capsule_tris((0, -0.01, 0), (0, -0.24, 0), 0.058, s, rings=4, segs=7))
        add(bone_a, box_tris((0, -0.01, 0), (0.10, 0.04, 0.10), g))
        add(bone_f, capsule_tris((0, -0.01, 0), (0, -0.20, 0), 0.048, s, rings=4, segs=7))
        add(bone_f, box_tris((0, -0.01, 0), (0.08, 0.035, 0.08), g))
        add(bone_h, box_tris((0, -0.04, 0.01), (0.07, 0.09, 0.05), s))
        add(bone_h, box_tris((0, -0.01, 0.01), (0.075, 0.02, 0.055), g))

    # Sword — silver blade, gold crossguard + pommel (held, not a second sheath).
    add("Sword", box_tris((0.01, -0.28, 0.03), (0.018, 0.56, 0.04), s))
    add("Sword", box_tris((0.01, 0.04, 0.03), (0.14, 0.02, 0.03), g))
    add("Sword", ellipsoid_tris((0.01, 0.08, 0.03), (0.025, 0.03, 0.025), g, slices=6, stacks=3))

    # Legs — silver cuisse / greave, gold poleyn. Boots silver+gold, not leather.
    for up, lo, foot in (("UpLeg_L", "Leg_L", "Foot_L"), ("UpLeg_R", "Leg_R", "Foot_R")):
        add(up, capsule_tris((0, -0.02, 0), (0, -0.38, 0), 0.078, s, rings=4, segs=7))
        add(lo, capsule_tris((0, -0.02, 0), (0, -0.34, 0), 0.062, s, rings=4, segs=7))
        add(lo, box_tris((0, -0.02, 0), (0.09, 0.04, 0.09), g))
        add(foot, box_tris((0, -0.01, 0.06), (0.09, 0.07, 0.18), s))
        add(foot, box_tris((0, 0.02, 0.02), (0.08, 0.03, 0.08), g))
        add(foot, box_tris((0, -0.02, 0.14), (0.08, 0.04, 0.05), g))

    # Single scabbard — character-right hip. Flat sheath + gold fittings. No back tube.
    add("Scabbard", box_tris((0.02, -0.18, 0.0), (0.045, 0.52, 0.028), br))
    add("Scabbard", box_tris((0.02, 0.10, 0.0), (0.055, 0.04, 0.036), g))
    add("Scabbard", box_tris((0.02, -0.02, 0.0), (0.05, 0.02, 0.032), g))
    add("Scabbard", box_tris((0.02, -0.42, 0.0), (0.04, 0.05, 0.026), g))
    add("Scabbard", ellipsoid_tris((0.02, 0.12, 0.0), (0.03, 0.03, 0.03), g, slices=6, stacks=3))

    return parts


BONE_PY = {
    "Head": "head",
    "Neck": "neck",
    "Chest": "chest",
    "Spine": "spine",
    "Hips": "hips",
    "Arm_L": "arm_l",
    "Fore_L": "fore_l",
    "Hand_L": "hand_l",
    "Arm_R": "arm_r",
    "Fore_R": "fore_r",
    "Hand_R": "hand_r",
    "Sword": "sword",
    "UpLeg_L": "up_l",
    "Leg_L": "leg_l",
    "Foot_L": "foot_l",
    "UpLeg_R": "up_r",
    "Leg_R": "leg_r",
    "Foot_R": "foot_r",
    "Scabbard": "scabbard",
}


def renderer_parts():
    """(python_bone, tris_with_uv) for the Game-view rasterizer."""
    out = []
    for bone, tris in knight_parts():
        out.append((BONE_PY[bone], tris))
    return out


def write_mesh_txt(path: Path):
    """Compact bone-local mesh for the Unity actor."""
    lines = ["# SirAldric mid-poly  look_targets SoT  scabbard=+X", "FMT v3"]
    for bone, tris in knight_parts():
        lines.append(f"BONE {bone}")
        for t in tris:
            p0, p1, p2, n0, n1, n2, uv0, uv1, uv2 = t
            def fmt(p, n, uv):
                return (
                    f"{p[0]:.5f} {p[1]:.5f} {p[2]:.5f} "
                    f"{n[0]:.4f} {n[1]:.4f} {n[2]:.4f} "
                    f"{float(uv[0]):.4f} {float(uv[1]):.4f}"
                )
            lines.append("V " + fmt(p0, n0, uv0))
            lines.append("V " + fmt(p1, n1, uv1))
            lines.append("V " + fmt(p2, n2, uv2))
            lines.append("T")
    path.write_text("\n".join(lines) + "\n")


def write_obj(path: Path):
    verts = []
    uvs = []
    norms = []
    faces = []
    for bone, tris in knight_parts():
        for t in tris:
            p0, p1, p2, n0, n1, n2, uv0, uv1, uv2 = t
            i = len(verts)
            verts.extend((p0, p1, p2))
            norms.extend((n0, n1, n2))
            uvs.extend((uv0, uv1, uv2))
            faces.append((i + 1, i + 2, i + 3))
    lines = ["# SirAldric mid-poly — bind pose, bone-local chunks concatenated", "mtllib sir_aldric.mtl"]
    for v in verts:
        lines.append(f"v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}")
    for uv in uvs:
        lines.append(f"vt {float(uv[0]):.4f} {float(uv[1]):.4f}")
    for n in norms:
        lines.append(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}")
    lines.append("usemtl AldricAtlas")
    for a, b, c in faces:
        lines.append(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}")
    path.write_text("\n".join(lines) + "\n")


def _unity_meta(guid: str, tex=True) -> str:
    if not tex:
        return f"fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 1
  textureType: 8
  textureShape: 1
  nPOTScale: 0
  alphaIsTransparency: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    wrapU: 1
    wrapV: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_assets():
    PACK3D.mkdir(parents=True, exist_ok=True)
    atlas = build_atlas()
    atlas_path = PACK3D / "sir_aldric_atlas.png"
    atlas.save(atlas_path, optimize=True)
    (PACK3D / "sir_aldric_atlas.png.meta").write_text(_unity_meta("a1d41c0000000000000000000000aa01"))
    write_mesh_txt(PACK3D / "sir_aldric_midpoly.mesh.txt")
    (PACK3D / "sir_aldric_midpoly.mesh.txt.meta").write_text(_unity_meta("a1d41c0000000000000000000000aa02", tex=False))
    write_obj(PACK3D / "sir_aldric_midpoly.obj")
    (PACK3D / "sir_aldric_midpoly.obj.meta").write_text(_unity_meta("a1d41c0000000000000000000000aa03", tex=False))
    (PACK3D / "sir_aldric.mtl").write_text(
        "newmtl AldricAtlas\nmap_Kd sir_aldric_atlas.png\nKd 1 1 1\n"
    )
    (PACK3D / "README.md").write_text(
        "# Sir Aldric 3D (Theme A)\n\n"
        "Runtime SoT: `sir_aldric_midpoly.mesh.txt` + `sir_aldric_atlas.png` bound by "
        "`SirAldric3DActor` (same bones / `Evaluate()` clip as the motion hold).\n\n"
        "- Atlas: silver / gold / royal-blue / brown swatches + **lion** and **Greek-key** "
        "from `look_targets/01_rear_LOCKED.png` (primary Game-view SoT).\n"
        "- Scabbard is **character-right** (+X). Ignore `02` if a panel mirrors.\n"
        "- `sir_aldric_midpoly.obj` is a bind-pose dump for DCC, not a Humanoid FBX.\n"
        "- Look gate is **not claimed** until Derek PASSes the Game-view clip.\n"
    )
    manifest = {
        "name": "SirAldric",
        "soT": "look_targets/01_rear_LOCKED.png",
        "scabbard": "character-right",
        "atlas": "sir_aldric_atlas.png",
        "mesh": "sir_aldric_midpoly.mesh.txt",
        "lookPassClaimed": False,
    }
    (PACK3D / "sir_aldric_3d.json").write_text(json.dumps(manifest, indent=2) + "\n")
    return atlas_path


if __name__ == "__main__":
    p = write_assets()
    print("wrote", p, p.stat().st_size)
    print("mesh", (PACK3D / "sir_aldric_midpoly.mesh.txt").stat().st_size)
