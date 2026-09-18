#!/usr/bin/env python3
"""3D Animator Sir Aldric preview — high-angle rear, march toward TOP of 1080×1920.

Capsule-sculpted skinned mesh + 01_rear look-target albedo. Not cubes / PNG warp.
"""
from __future__ import annotations

import math
import shutil
import subprocess
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
ART = Path("/opt/cursor/artifacts")
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
W, H = 1080, 1920
WALK_PERIOD = 1.00
WALK_CYCLES = 2
ATTACK = 1.40
WALK_BLOCK = WALK_PERIOD * WALK_CYCLES
LOOP = WALK_BLOCK + ATTACK
MARCH = 0.80

SILVER = (186, 194, 204)
GOLD = (211, 176, 82)
BLUE = (41, 76, 148)
BROWN = (92, 56, 33)
DARK = (46, 51, 56)
GROUND = (28, 32, 24)

CAM_EYE = np.array([0.0, 2.70, -4.60])
CAM_TARGET = np.array([0.0, 0.95, 0.85])
CAM_FOV = 30.0

REAR_IMG = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
REAR = np.array(REAR_IMG)
REAR_RGB = REAR[:, :, :3].astype(np.float32)
REAR_A = REAR[:, :, 3].astype(np.float32) / 255.0
# Content bbox of 01_rear (precomputed).
BX0, BY0, BX1, BY1 = 274, 40, 749, 983


def _strip_painted_scabbard(rgb, alpha):
    """Drop the look-target's painted right-hip sheath so only the 3D Scabbard remains."""
    out = rgb.copy()
    h, w = alpha.shape
    xs = np.arange(w)
    ys = np.arange(h)[:, None]
    mid = 0.5 * (BX0 + BX1)
    right = xs[None, :] > mid + 8
    visible = alpha > 0.12
    shift = max(8, w // 16)
    src = np.roll(out, shift, axis=1)
    for _ in range(3):
        r, g, b = out[..., 0], out[..., 1], out[..., 2]
        brown = (
            visible
            & right
            & (r > 38)
            & (r > b + 18)
            & (g < r * 0.92)
            & (b < 95)
            & ((r + g + b) < 440)
        )
        below_lion = ys > BY0 + 0.40 * (BY1 - BY0)
        gold = (
            visible
            & right
            & below_lion
            & (r > 130)
            & (g > 90)
            & (b < 130)
            & (r > b + 30)
        )
        mask = brown | gold
        out[mask] = src[mask]
        src = np.roll(out, shift, axis=1)
    return out


REAR_RGB = _strip_painted_scabbard(REAR_RGB, REAR_A)


def clamp01(x):
    return 0.0 if x < 0 else 1.0 if x > 1 else x


def lerp(a, b, t):
    return a + (b - a) * t


def smooth01(x):
    x = clamp01(x)
    return x * x * (3 - 2 * x)


def repeat(t, length):
    r = t % length
    return r + length if r < 0 else r


def evaluate(t):
    loop_t = repeat(t, LOOP)
    attacking = loop_t >= WALK_BLOCK
    root_z = loop_t * MARCH
    if attacking:
        return attack_pose(loop_t - WALK_BLOCK, root_z)
    return walk_pose(loop_t, root_z)


def walk_pose(loop_t, root_z):
    phase = loop_t / WALK_PERIOD * math.tau
    s = math.sin(phase)
    c = math.cos(phase)
    swing_l, swing_r = max(0.0, c), max(0.0, -c)
    contact_l, contact_r = max(0.0, s), max(0.0, -s)
    bob = 0.010 + 0.018 * abs(c)
    hips_y, hips_z = 8 * s, 7.5 * c
    left_x = -18 * s - 8 * swing_l
    right_x = 18 * s - 8 * swing_r
    knee_l = 12 + 54 * swing_l + 8 * contact_r
    knee_r = 12 + 54 * swing_r + 8 * contact_l
    foot_l = -12 * contact_l + 20 * contact_r - 6 * swing_l
    foot_r = -12 * contact_r + 20 * contact_l - 6 * swing_r
    return dict(
        attacking=False,
        drawn=False,
        root_z=root_z,
        root_y=bob,
        hips=(0, hips_y, hips_z),
        spine=(5, -11 * s, -hips_z * 0.25),
        chest=(2, -7 * s, 0),
        head=(6, -3 * s, 0),
        up_l=(left_x, 0, -7 * swing_l),
        leg_l=(knee_l, 0, 0),
        foot_l=(foot_l, 0, 0),
        up_r=(right_x, 0, 7 * swing_r),
        leg_r=(knee_r, 0, 0),
        foot_r=(foot_r, 0, 0),
        arm_l=(-20 + 10 * s, 0, 12),
        fore_l=(-12 - 10 * max(0.0, -s), 0, 0),
        arm_r=(-18 - 6 * s, 4, -12),
        fore_r=(-20 - 4 * max(0.0, s), 0, 0),
        hand_r=(0, 0, 0),
        sword=(-6, 0, 8),
        label="WALK  ·  toward TOP",
    )


def attack_pose(attack_t, root_z):
    u = clamp01(attack_t / ATTACK)
    if u < 0.18:
        k = smooth01(u / 0.18)
        arm_x, arm_y, fore_x = lerp(-16, -85, k), lerp(6, 2, k), lerp(-22, -8, k)
        lunge, spine_x, drawn_k = 0.0, lerp(4, 10, k), k
    elif u < 0.40:
        k = smooth01((u - 0.18) / 0.22)
        arm_x, arm_y, fore_x = lerp(-85, -155, k), lerp(2, 0, k), lerp(-8, -28, k)
        lunge, spine_x, drawn_k = lerp(0, 0.04, k), lerp(10, 14, k), 1.0
    elif u < 0.56:
        k = smooth01((u - 0.40) / 0.16)
        arm_x, arm_y, fore_x = lerp(-155, -118, k), lerp(0, 0, k), lerp(-28, -6, k)
        lunge, spine_x, drawn_k = lerp(0.04, 0.10, k), lerp(14, 6, k), 1.0
    elif u < 0.78:
        k = smooth01((u - 0.56) / 0.22)
        arm_x, arm_y, fore_x = lerp(-118, -48, k), lerp(0, 4, k), lerp(-6, -16, k)
        lunge, spine_x, drawn_k = lerp(0.10, 0.02, k), lerp(6, 2, k), 1.0 - k * 0.35
    else:
        k = smooth01((u - 0.78) / 0.22)
        arm_x, arm_y, fore_x = lerp(-48, -16, k), lerp(4, 6, k), lerp(-16, -22, k)
        lunge, spine_x, drawn_k = lerp(0.02, 0, k), lerp(2, 4, k), 1.0 - k
    drawn = drawn_k > 0.22
    strike = drawn and -175 <= arm_x <= -80
    return dict(
        attacking=True,
        drawn=drawn,
        root_z=root_z,
        root_y=lunge,
        hips=(lunge * 20, 0, 0),
        spine=(spine_x, 0, 0),
        chest=(spine_x * 0.4, 0, 0),
        head=(8, 0, 0),
        up_l=(8, 0, 0),
        leg_l=(12, 0, 0),
        foot_l=(-6, 0, 0),
        up_r=(-6, 0, 0),
        leg_r=(16, 0, 0),
        foot_r=(-4, 0, 0),
        arm_l=(-22, 0, 10),
        fore_l=(-18, 0, 0),
        arm_r=(arm_x, arm_y, -8),
        fore_r=(fore_x, 0, 0),
        hand_r=(-12 if drawn else 0, 0, 0),
        sword=(-10 if drawn else -6, 0, 0 if drawn else 8),
        label="ATTACK  ·  strike TOP" if strike else "ATTACK  ·  draw / recover",
    )


def rot_euler(x, y, z):
    x, y, z = np.radians([x, y, z])
    cx, sx = math.cos(x), math.sin(x)
    cy, sy = math.cos(y), math.sin(y)
    cz, sz = math.cos(z), math.sin(z)
    rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]], np.float64)
    ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]], np.float64)
    rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]], np.float64)
    r = ry @ rx @ rz
    m = np.eye(4)
    m[:3, :3] = r
    return m


def trans(p):
    m = np.eye(4)
    m[:3, 3] = p
    return m


def local_of(pos, eul):
    return trans(pos) @ rot_euler(*eul)


def fk(pose):
    root = local_of((0, pose["root_y"], pose["root_z"]), (0, 0, 0))
    hips = root @ local_of((0, 0.96, 0), pose["hips"])
    spine = hips @ local_of((0, 0.12, 0), pose["spine"])
    chest = spine @ local_of((0, 0.18, 0), pose["chest"])
    neck = chest @ local_of((0, 0.20, 0), (0, 0, 0))
    head = neck @ local_of((0, 0.10, 0), pose["head"])
    arm_l = chest @ local_of((-0.22, 0.10, 0), pose["arm_l"])
    fore_l = arm_l @ local_of((0, -0.28, 0), pose["fore_l"])
    hand_l = fore_l @ local_of((0, -0.24, 0), (0, 0, 0))
    arm_r = chest @ local_of((0.22, 0.10, 0), pose["arm_r"])
    fore_r = arm_r @ local_of((0, -0.28, 0), pose["fore_r"])
    hand_r = fore_r @ local_of((0, -0.24, 0), pose["hand_r"])
    sword = hand_r @ local_of((0.02, -0.08, 0.06), pose["sword"])
    up_l = hips @ local_of((-0.11, -0.04, 0), pose["up_l"])
    leg_l = up_l @ local_of((0, -0.42, 0), pose["leg_l"])
    foot_l = leg_l @ local_of((0, -0.40, 0.05), pose["foot_l"])
    up_r = hips @ local_of((0.11, -0.04, 0), pose["up_r"])
    leg_r = up_r @ local_of((0, -0.42, 0), pose["leg_r"])
    foot_r = leg_r @ local_of((0, -0.40, 0.05), pose["foot_r"])
    scabbard = hips @ trans((0.20, -0.04, -0.02)) @ rot_euler(18, 0, 22)
    cape = chest @ trans((0, 0.08, -0.12))
    return {
        "head": head, "neck": neck, "chest": chest, "spine": spine, "hips": hips,
        "arm_l": arm_l, "fore_l": fore_l, "hand_l": hand_l,
        "arm_r": arm_r, "fore_r": fore_r, "hand_r": hand_r, "sword": sword,
        "up_l": up_l, "leg_l": leg_l, "foot_l": foot_l,
        "up_r": up_r, "leg_r": leg_r, "foot_r": foot_r,
        "scabbard": scabbard, "cape": cape, "root": root,
    }


def xform_p(m, p):
    h = m @ np.array([p[0], p[1], p[2], 1.0])
    return h[:3]


def xform_n(m, n):
    nn = m[:3, :3] @ n
    return nn / (np.linalg.norm(nn) + 1e-8)


def uv_of(bind_world):
    u = clamp01((bind_world[0] + 0.32) / 0.64)
    v = clamp01(bind_world[1] / 1.86)
    px = BX0 + u * (BX1 - BX0)
    py = BY1 - v * (BY1 - BY0)
    return px, py


def sample_rear(px, py):
    x = int(np.clip(round(px), 0, REAR.shape[1] - 1))
    y = int(np.clip(round(py), 0, REAR.shape[0] - 1))
    rgb = REAR_RGB[y, x]
    a = REAR_A[y, x]
    if a < 0.12:
        return np.array(SILVER, np.float32)
    return rgb


def capsule_tris(a, b, radius, rings=5, segs=8):
    a = np.array(a, np.float64)
    b = np.array(b, np.float64)
    axis = b - a
    height = np.linalg.norm(axis)
    if height < 1e-5:
        return sphere_tris(a, radius)
    ny = axis / height
    ref = np.array([0.0, 1.0, 0.0]) if abs(ny[1]) < 0.9 else np.array([1.0, 0.0, 0.0])
    nx = np.cross(ref, ny)
    nx /= np.linalg.norm(nx)
    nz = np.cross(ny, nx)
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
            tris.append((p00, p10, p01, n00, n10, n01))
            tris.append((p01, p10, p11, n01, n10, n11))
    tris.extend(sphere_tris(a, radius))
    tris.extend(sphere_tris(b, radius))
    return tris


def sphere_tris(center, radius, slices=6, stacks=4):
    center = np.array(center, np.float64)
    pts = []
    for y in range(stacks + 1):
        v = y / stacks
        phi = v * math.pi
        row = []
        for x in range(slices + 1):
            u = x / slices
            th = u * math.tau
            n = np.array([math.sin(phi) * math.cos(th), math.cos(phi), math.sin(phi) * math.sin(th)])
            row.append((center + n * radius, n))
        pts.append(row)
    tris = []
    for y in range(stacks):
        for x in range(slices):
            p0, n0 = pts[y][x]
            p1, n1 = pts[y][x + 1]
            p2, n2 = pts[y + 1][x]
            p3, n3 = pts[y + 1][x + 1]
            tris.append((p0, p2, p1, n0, n2, n1))
            tris.append((p1, p2, p3, n1, n2, n3))
    return tris


# (bone, local_tris, textured, solid_rgb)
PARTS = []


def add_part(bone, tris, textured, color=SILVER):
    PARTS.append((bone, tris, textured, np.array(color, np.float32)))


def build_parts():
    PARTS.clear()
    add_part("head", capsule_tris((0, 0.02, 0.02), (0, 0.26, 0.02), 0.13), True)
    add_part("head", sphere_tris((0, 0.30, 0), 0.035), False, GOLD)
    add_part("head", capsule_tris((0, 0.08, -0.13), (0, 0.28, -0.13), 0.022), False, GOLD)
    add_part("neck", capsule_tris((0, -0.04, 0), (0, 0.10, 0), 0.07), True)
    add_part("chest", capsule_tris((0, -0.12, 0), (0, 0.16, 0), 0.20), True)
    add_part("chest", sphere_tris((-0.22, 0.12, 0), 0.10), False, SILVER)
    add_part("chest", sphere_tris((0.22, 0.12, 0), 0.10), False, SILVER)
    add_part("chest", sphere_tris((-0.22, 0.18, 0), 0.045), False, GOLD)
    add_part("chest", sphere_tris((0.22, 0.18, 0), 0.045), False, GOLD)
    add_part("spine", capsule_tris((0, -0.04, 0), (0, 0.10, 0), 0.16), True)
    add_part("hips", capsule_tris((0, -0.16, 0), (0, 0.06, 0), 0.18), True)
    add_part("hips", capsule_tris((-0.16, -0.18, 0), (0.16, -0.18, 0), 0.03), False, GOLD)
    add_part("arm_l", capsule_tris((0, -0.02, 0), (0, -0.26, 0), 0.065), True)
    add_part("fore_l", capsule_tris((0, -0.02, 0), (0, -0.22, 0), 0.055), True)
    add_part("hand_l", capsule_tris((0, -0.02, 0), (0, -0.08, 0), 0.05), True)
    add_part("arm_r", capsule_tris((0, -0.02, 0), (0, -0.26, 0), 0.065), True)
    add_part("fore_r", capsule_tris((0, -0.02, 0), (0, -0.22, 0), 0.055), True)
    add_part("hand_r", capsule_tris((0, -0.02, 0), (0, -0.08, 0), 0.05), True)
    add_part("sword", capsule_tris((0.01, 0.02, 0.02), (0.01, -0.58, 0.04), 0.022), False, SILVER)
    add_part("sword", capsule_tris((-0.06, 0.04, 0.02), (0.08, 0.04, 0.02), 0.018), False, GOLD)
    add_part("up_l", capsule_tris((0, -0.02, 0), (0, -0.40, 0), 0.085), True)
    add_part("leg_l", capsule_tris((0, -0.02, 0), (0, -0.36, 0), 0.07), True)
    add_part("foot_l", capsule_tris((0, -0.02, -0.02), (0, -0.02, 0.16), 0.055), True)
    add_part("foot_l", sphere_tris((0, 0.02, 0.04), 0.03), False, GOLD)
    add_part("up_r", capsule_tris((0, -0.02, 0), (0, -0.40, 0), 0.085), True)
    add_part("leg_r", capsule_tris((0, -0.02, 0), (0, -0.36, 0), 0.07), True)
    add_part("foot_r", capsule_tris((0, -0.02, -0.02), (0, -0.02, 0.16), 0.055), True)
    add_part("foot_r", sphere_tris((0, 0.02, 0.04), 0.03), False, GOLD)
    add_part("scabbard", capsule_tris((0.02, 0.08, 0), (0.02, -0.48, 0), 0.032), False, BROWN)
    add_part("scabbard", sphere_tris((0.02, 0.10, 0), 0.04), False, GOLD)


REST_FK = None


def rest_world(bone, local):
    return xform_p(REST_FK[bone], local)


LIGHT = np.array([0.28, 0.78, -0.55], np.float64)
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def project(p, eye, r, u, f):
    d = p - eye
    cam = np.array([np.dot(d, r), np.dot(d, u), np.dot(d, f)])
    if cam[2] < 0.08:
        return None
    fov = math.radians(CAM_FOV)
    fy = 1.0 / math.tan(fov * 0.5)
    aspect = W / float(H)
    ndc_x = fy * cam[0] / (cam[2] * aspect)
    ndc_y = fy * cam[1] / cam[2]
    sx = (ndc_x + 1.0) * 0.5 * W
    sy = (1.0 - ndc_y) * 0.5 * H
    return sx, sy, cam[2]


def raster_tri(zbuf, cbuf, p0, p1, p2, c0, c1, c2):
    xs = (p0[0], p1[0], p2[0])
    ys = (p0[1], p1[1], p2[1])
    minx = max(0, int(math.floor(min(xs))))
    maxx = min(W - 1, int(math.ceil(max(xs))))
    miny = max(0, int(math.floor(min(ys))))
    maxy = min(H - 1, int(math.ceil(max(ys))))
    if maxx < minx or maxy < miny:
        return
    denom = (p1[1] - p2[1]) * (p0[0] - p2[0]) + (p2[0] - p1[0]) * (p0[1] - p2[1])
    if abs(denom) < 1e-8:
        return
    yy, xx = np.mgrid[miny : maxy + 1, minx : maxx + 1]
    a = ((p1[1] - p2[1]) * (xx - p2[0]) + (p2[0] - p1[0]) * (yy - p2[1])) / denom
    b = ((p2[1] - p0[1]) * (xx - p2[0]) + (p0[0] - p2[0]) * (yy - p2[1])) / denom
    c = 1.0 - a - b
    mask = (a >= 0) & (b >= 0) & (c >= 0)
    if not np.any(mask):
        return
    z = a * p0[2] + b * p1[2] + c * p2[2]
    subz = zbuf[miny : maxy + 1, minx : maxx + 1]
    nearer = mask & (z < subz)
    if not np.any(nearer):
        return
    subz[nearer] = z[nearer]
    col = a[..., None] * c0 + b[..., None] * c1 + c[..., None] * c2
    col = np.clip(col, 0, 255).astype(np.uint8)
    dest = cbuf[miny : maxy + 1, minx : maxx + 1]
    dest[nearer] = col[nearer]


def draw_world_tri(zbuf, cbuf, eye, r, u, f, w0, w1, w2, n, color):
    shade = 0.32 + 0.68 * max(0.0, float(np.dot(n, LIGHT)))
    pts = []
    for w in (w0, w1, w2):
        pr = project(w, eye, r, u, f)
        if pr is None:
            return
        pts.append(pr)
    c = np.array(color, np.float32) * shade
    raster_tri(zbuf, cbuf, pts[0], pts[1], pts[2], c, c, c)


def box_world(center, size):
    e = np.array(size) * 0.5
    c = np.array(center)
    corners = np.array(
        [
            c + [-e[0], -e[1], -e[2]],
            c + [e[0], -e[1], -e[2]],
            c + [e[0], e[1], -e[2]],
            c + [-e[0], e[1], -e[2]],
            c + [-e[0], -e[1], e[2]],
            c + [e[0], -e[1], e[2]],
            c + [e[0], e[1], e[2]],
            c + [-e[0], e[1], e[2]],
        ],
        np.float64,
    )
    faces = (
        (0, 1, 2, 3, (0, 0, -1)),
        (5, 4, 7, 6, (0, 0, 1)),
        (4, 0, 3, 7, (-1, 0, 0)),
        (1, 5, 6, 2, (1, 0, 0)),
        (3, 2, 6, 7, (0, 1, 0)),
        (4, 5, 1, 0, (0, -1, 0)),
    )
    return corners, faces


def render_pose(pose):
    bones = fk(pose)
    eye = CAM_EYE
    target = CAM_TARGET
    forward = target - eye
    forward = forward / np.linalg.norm(forward)
    right = np.cross(np.array([0.0, 1.0, 0.0]), forward)
    right = right / np.linalg.norm(right)
    up = np.cross(forward, right)
    zbuf = np.full((H, W), 1e9, np.float32)
    cbuf = np.zeros((H, W, 3), np.uint8)
    cbuf[:] = (18, 20, 16)
    # Ground plane (z toward TOP of screen).
    for gz in np.linspace(-0.4, 3.2, 10):
        gx0, gx1 = -2.2, 2.2
        w0 = np.array([gx0, 0.0, gz])
        w1 = np.array([gx1, 0.0, gz])
        w2 = np.array([gx1, 0.0, gz + 0.36])
        w3 = np.array([gx0, 0.0, gz + 0.36])
        col = GROUND if int(round(gz * 4)) % 2 == 0 else (36, 42, 30)
        n = np.array([0.0, 1.0, 0.0])
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, w0, w1, w2, n, col)
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, w0, w2, w3, n, col)
    for i in range(6):
        z = 0.15 + i * 0.45
        corners, faces = box_world((0.0, 0.02, z), (0.55 - i * 0.04, 0.02, 0.10))
        for a, b, c, d, nloc in faces:
            n = np.array(nloc, np.float64)
            draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[b], corners[c], n, GOLD)
            draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[c], corners[d], n, GOLD)
        corners, faces = box_world((0.0, 0.28, 2.15), (0.38, 0.55, 0.38))
    keep = (56, 18, 30)
    for a, b, c, d, nloc in faces:
        n = np.array(nloc, np.float64)
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[b], corners[c], n, keep)
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[c], corners[d], n, keep)

    for bone, tris, textured, color in PARTS:
        m = bones[bone]
        for p0, p1, p2, n0, n1, n2 in tris:
            w0, w1, w2 = xform_p(m, p0), xform_p(m, p1), xform_p(m, p2)
            nn = xform_n(m, (n0 + n1 + n2) / 3.0)
            shade = 0.34 + 0.66 * max(0.0, float(np.dot(nn, LIGHT)))
            prs = []
            cols = []
            ok = True
            for lp, wp in ((p0, w0), (p1, w1), (p2, w2)):
                pr = project(wp, eye, right, up, forward)
                if pr is None:
                    ok = False
                    break
                prs.append(pr)
                if textured:
                    bw = rest_world(bone, lp)
                    px, py = uv_of(bw)
                    cols.append(sample_rear(px, py) * shade)
                else:
                    cols.append(color * shade)
            if ok:
                raster_tri(zbuf, cbuf, prs[0], prs[1], prs[2], cols[0], cols[1], cols[2])

    im = Image.fromarray(cbuf, "RGB")
    d = ImageDraw.Draw(im)
    try:
        title = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 28)
        phase = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
        note = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
    except OSError:
        title = phase = note = ImageFont.load_default()
    d.text((W / 2, 56), "SIR ALDRIC  ·  walk → attack TOP", fill=(237, 230, 209), font=title, anchor="mm")
    d.text((W / 2, 96), "▲  TOP  ·  ENEMY", fill=(211, 176, 82), font=phase, anchor="mm")
    d.text((W / 2, 134), pose["label"], fill=(211, 176, 82), font=phase, anchor="mm")
    d.text(
        (W / 2, H - 48),
        "3D Animator  ·  high-angle rear  ·  march TOP  ·  no PNG warp",
        fill=(168, 172, 164),
        font=note,
        anchor="mm",
    )
    return im


def body_crop(im):
    return im.crop((80, 160, W - 80, H - 80))


def max_delta(a, b):
    diff = ImageChops.difference(a, b)
    return max(ImageStat.Stat(diff).extrema[c][1] for c in range(3))


def mean_delta(a, b):
    diff = ImageChops.difference(a, b)
    return sum(ImageStat.Stat(diff).mean) / 3.0


def hip_screen_y(pose):
    bones = fk(pose)
    hip = xform_p(bones["hips"], (0, 0, 0))
    forward = CAM_TARGET - CAM_EYE
    forward = forward / np.linalg.norm(forward)
    right = np.cross(np.array([0.0, 1.0, 0.0]), forward)
    right = right / np.linalg.norm(right)
    up = np.cross(forward, right)
    pr = project(hip, CAM_EYE, right, up, forward)
    return pr[1] if pr else None


def main():
    global REST_FK
    REST_FK = fk(evaluate(0.0) | {"root_y": 0.0, "root_z": 0.0})
    build_parts()

    y0 = hip_screen_y(evaluate(0.05))
    y1 = hip_screen_y(evaluate(WALK_BLOCK - 0.05))
    if y0 is None or y1 is None or y1 >= y0 - 8:
        raise SystemExit(f"FAIL orientation: hips must move toward TOP (smaller sy). sy0={y0} sy1={y1}")

    def sword_vs_hip_z(t):
        pose = evaluate(t)
        bones = fk(pose)
        hip = xform_p(bones["hips"], (0, 0, 0))
        tip = xform_p(bones["sword"], (0.01, -0.58, 0.04))
        hand = xform_p(bones["hand_r"], (0, -0.04, 0))
        return hip[2], hand[2], tip[2]

    for t, name in [(WALK_PERIOD * 0.25, "walk"), (WALK_BLOCK + ATTACK * 0.48, "strike")]:
        hz, handz, tipz = sword_vs_hip_z(t)
        if handz < hz - 0.02 or tipz < hz - 0.05:
            raise SystemExit(
                f"FAIL arms/sword toward camera: {name} hipZ={hz:.3f} handZ={handz:.3f} tipZ={tipz:.3f} "
                "(need hand/tip further +Z / TOP than hips)"
            )
        scab = xform_p(fk(evaluate(t))["scabbard"], (0.02, -0.24, 0))
        if scab[0] < 0.12:
            raise SystemExit(f"FAIL scabbard not character-right: {name} scabX={scab[0]:.3f}")

    brown_bones = {bone for bone, _, _, color in PARTS if tuple(int(c) for c in color) == BROWN}
    if brown_bones != {"scabbard"}:
        raise SystemExit(f"FAIL extra brown sheath mesh: {sorted(brown_bones)}")

    fps = 16
    n = int(LOOP * fps)
    raw = Path("/tmp/aldric-3d")
    if raw.exists():
        shutil.rmtree(raw)
    raw.mkdir()
    frames = []
    for i in range(n):
        im = render_pose(evaluate(i / fps))
        im.save(raw / f"f_{i:03d}.png")
        frames.append(im)
        print(f"{i + 1}/{n}", flush=True)

    walk_pass_l = render_pose(evaluate(0.0))
    walk_contact_l = render_pose(evaluate(WALK_PERIOD * 0.25))
    walk_pass_r = render_pose(evaluate(WALK_PERIOD * 0.5))
    walk_contact_r = render_pose(evaluate(WALK_PERIOD * 0.75))
    strike = render_pose(evaluate(WALK_BLOCK + ATTACK * 0.48))

    pass_l = evaluate(0.0)
    pass_r = evaluate(WALK_PERIOD * 0.5)
    contact_l = evaluate(WALK_PERIOD * 0.25)
    if not (52 <= pass_l["leg_l"][0] <= 78 and 52 <= pass_r["leg_r"][0] <= 78):
        raise SystemExit(
            f"FAIL gait-bar pass knee: L={pass_l['leg_l'][0]:.1f} R={pass_r['leg_r'][0]:.1f} (need 52–78, not 90° cartoon)"
        )
    if pass_l["hips"][2] < 5 or pass_r["hips"][2] > -5:
        raise SystemExit(f"FAIL hip drop: passL Z={pass_l['hips'][2]:.1f} passR Z={pass_r['hips'][2]:.1f}")
    if contact_l["hips"][1] <= 4 or contact_l["spine"][1] >= -4:
        raise SystemExit("FAIL shoulder–hip counter-rotation at contact L")
    bones_c = fk(contact_l)
    heel_r = xform_p(bones_c["foot_r"], (0, 0, 0))
    heel_l = xform_p(bones_c["foot_l"], (0, 0, 0))
    if heel_r[1] < heel_l[1] + 0.04:
        raise SystemExit(f"FAIL trailing heel lift at contact L: L={heel_l[1]:.3f} R={heel_r[1]:.3f}")
    step_len = abs(heel_l[2] - heel_r[2])
    expected = MARCH * WALK_PERIOD * 0.5
    if abs(step_len - expected) > 0.26:
        raise SystemExit(f"FAIL stride/speed slide: step={step_len:.3f} vs march-step={expected:.3f}")

    def scabbard_brown(im):
        arr = np.array(im)
        r, g, b = (arr[:, :, 0].astype(np.int16), arr[:, :, 1].astype(np.int16), arr[:, :, 2].astype(np.int16))
        return (r > 70) & (r < 120) & (g > 30) & (g < 80) & (b < 50) & (r > g + 15) & (g > b)

    if any(bone == "cape" for bone, _, _, _ in PARTS):
        raise SystemExit("FAIL cape mesh still present (was the back sheath)")

    for im, name in ((walk_contact_l, "walk"), (strike, "strike")):
        mask = scabbard_brown(im)
        left_upper = int(mask[int(H * 0.24) : int(H * 0.36), int(W * 0.28) : int(W * 0.42)].sum())
        right_hip = int(mask[int(H * 0.42) : int(H * 0.65), int(W * 0.55) : int(W * 0.72)].sum())
        if right_hip < 800:
            raise SystemExit(f"FAIL missing character-right scabbard on {name}: right-hip brown px={right_hip}")
        if left_upper > 200:
            raise SystemExit(f"FAIL back/left sheath on {name}: left-upper brown px={left_upper}")
    d_walk = max_delta(body_crop(walk_contact_l), body_crop(walk_contact_r))
    d_pass = max_delta(body_crop(walk_pass_l), body_crop(walk_contact_l))
    d_strike = max_delta(body_crop(walk_contact_l), body_crop(strike))
    m_walk = mean_delta(body_crop(walk_contact_l), body_crop(walk_contact_r))
    m_strike = mean_delta(body_crop(walk_contact_l), body_crop(strike))
    proof = (
        f"3D Animator rematched to WALK_GAIT_BAR skeleton sample. "
        f"Hips screen-Y {y0:.0f}→{y1:.0f} (toward TOP). "
        f"Pass knee {pass_l['leg_l'][0]:.0f}°/{pass_r['leg_r'][0]:.0f}° (gait-bar ~65°, not 90° cartoon). "
        f"Hip drop + counter-rotate; heel lift; contralateral arms far/TOP. "
        f"step {step_len:.2f}m vs march-step {expected:.2f}m. "
        f"max |Δ| walk {d_walk:.0f}/255; pass vs contact {d_pass:.0f}/255; "
        f"walk vs strike {d_strike:.0f}/255. "
        "Single brown scabbard character-right. High-angle rear +Z = TOP."
    )
    print(proof)
    if d_walk < 18 or d_strike < 18 or d_pass < 18:
        raise SystemExit("FAIL: 3D motion too weak: " + proof)

    cell_w, cell_h = 540, 960
    sheet = Image.new("RGB", (W, H), (18, 20, 16))
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    for i, (im, lab) in enumerate(
        [
            (walk_pass_l, "WALK PASS L"),
            (walk_contact_l, "WALK CONTACT L"),
            (walk_pass_r, "WALK PASS R"),
            (walk_contact_r, "WALK CONTACT R"),
        ]
    ):
        x, y = (i % 2) * cell_w, (i // 2) * cell_h
        sheet.paste(im.resize((cell_w, cell_h), Image.BILINEAR), (x, y))
        ImageDraw.Draw(sheet).text((x + 16, y + 16), lab, fill=(237, 230, 209), font=font)

    still = OUT / "sir_aldric_locked_rear_gameview_1080x1920.png"
    walk_pass_l.save(still, optimize=True)
    sheet_path = OUT / "sir_aldric_walk_draw_strike_recover_sheet.png"
    sheet.save(sheet_path, optimize=True)
    gif_full = OUT / "sir_aldric_walk_attack_toward_top.gif"
    gif_walk = OUT / "sir_aldric_locked_master_walks_toward_top.gif"
    pal = [f.convert("P", palette=Image.ADAPTIVE, colors=64) for f in frames]
    pal[0].save(gif_full, save_all=True, append_images=pal[1:], duration=int(1000 / fps), loop=0, optimize=True)
    nw = int(WALK_BLOCK * fps)
    pal[0].save(gif_walk, save_all=True, append_images=pal[1:nw], duration=int(1000 / fps), loop=0, optimize=True)
    mp4 = OUT / "sir_aldric_walk_attack_toward_top.mp4"
    subprocess.check_call(
        [
            "ffmpeg",
            "-y",
            "-framerate",
            str(fps),
            "-i",
            str(raw / "f_%03d.png"),
            "-pix_fmt",
            "yuv420p",
            "-vf",
            "scale=1080:1920",
            "-crf",
            "18",
            "-movflags",
            "+faststart",
            str(mp4),
        ]
    )
    (OUT / "PIXEL_PROOF.txt").write_text(proof + "\n")
    ART.mkdir(parents=True, exist_ok=True)
    for p in (still, sheet_path, gif_full, gif_walk, mp4, OUT / "PIXEL_PROOF.txt"):
        dest = ART / f"aldric_gait_{p.name}"
        shutil.copy2(p, dest)
        print("wrote", p, p.stat().st_size)


if __name__ == "__main__":
    main()
