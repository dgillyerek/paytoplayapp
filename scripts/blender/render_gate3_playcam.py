#!/usr/bin/env python3
"""Gate 3 World-cam stills at Play march angle (same basis as Game-view).

eye (0, 2.80, −5.40) look-at (0, 0.90, 0.50) FOV 30 vertical, 1080×1920.
Bind-pose mesh.txt + atlas. No walk. TOP = +Z = away.
"""
from __future__ import annotations

import math
from collections import defaultdict
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
MESH = PACK / "sir_aldric_midpoly.mesh.txt"
ATLAS_PATH = PACK / "sir_aldric_atlas.png"
PROOF = ROOT / "Docs/Survival/previews/gate3"
ART = Path("/opt/cursor/artifacts")

W, H = 1080, 1920
CAM_EYE = np.array([0.0, 2.80, -5.40])
CAM_TARGET = np.array([0.0, 0.90, 0.50])
CAM_EYE_34 = np.array([1.20, 2.80, -5.15])
CAM_FOV = 30.0
GROUND = (28, 32, 24)
LIGHT = np.array([0.28, 0.78, -0.55], np.float64)
LIGHT = LIGHT / np.linalg.norm(LIGHT)

BONE_PY = {
    "Head": "head", "Neck": "neck", "Chest": "chest", "Spine": "spine", "Hips": "hips",
    "Arm_L": "arm_l", "Fore_L": "fore_l", "Hand_L": "hand_l",
    "Arm_R": "arm_r", "Fore_R": "fore_r", "Hand_R": "hand_r", "Sword": "sword",
    "UpLeg_L": "up_l", "Leg_L": "leg_l", "Foot_L": "foot_l",
    "UpLeg_R": "up_r", "Leg_R": "leg_r", "Foot_R": "foot_r",
    "Scabbard": "scabbard",
}

ATLAS = np.array(Image.open(ATLAS_PATH).convert("RGB")).astype(np.float32)
AH, AW = ATLAS.shape[:2]


def rot_euler(x, y, z):
    rx, ry, rz = np.radians([x, y, z])
    cx, sx = math.cos(rx), math.sin(rx)
    cy, sy = math.cos(ry), math.sin(ry)
    cz, sz = math.cos(rz), math.sin(rz)
    Rx = np.array([[1, 0, 0, 0], [0, cx, -sx, 0], [0, sx, cx, 0], [0, 0, 0, 1]], np.float64)
    Ry = np.array([[cy, 0, sy, 0], [0, 1, 0, 0], [-sy, 0, cy, 0], [0, 0, 0, 1]], np.float64)
    Rz = np.array([[cz, -sz, 0, 0], [sz, cz, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]], np.float64)
    return Ry @ Rx @ Rz


def trans(p):
    m = np.eye(4)
    m[:3, 3] = p
    return m


def local_of(pos, eul):
    return trans(pos) @ rot_euler(*eul)


def rest_fk():
    z = (0.0, 0.0, 0.0)
    root = local_of((0, 0, 0), z)
    hips = root @ local_of((0, 0.96, 0), z)
    spine = hips @ local_of((0, 0.12, 0), z)
    chest = spine @ local_of((0, 0.18, 0), z)
    neck = chest @ local_of((0, 0.20, 0), z)
    head = neck @ local_of((0, 0.10, 0), z)
    arm_l = chest @ local_of((-0.22, 0.10, 0), z)
    fore_l = arm_l @ local_of((0, -0.28, 0), z)
    hand_l = fore_l @ local_of((0, -0.24, 0), z)
    arm_r = chest @ local_of((0.22, 0.10, 0), z)
    fore_r = arm_r @ local_of((0, -0.28, 0), z)
    hand_r = fore_r @ local_of((0, -0.24, 0), z)
    sword = hand_r @ local_of((0.02, -0.08, 0.06), z)
    up_l = hips @ local_of((-0.11, -0.04, 0), z)
    leg_l = up_l @ local_of((0, -0.42, 0), z)
    foot_l = leg_l @ local_of((0, -0.40, 0.05), z)
    up_r = hips @ local_of((0.11, -0.04, 0), z)
    leg_r = up_r @ local_of((0, -0.42, 0), z)
    foot_r = leg_r @ local_of((0, -0.40, 0.05), z)
    scabbard = hips @ trans((0.20, -0.04, -0.02)) @ rot_euler(18, 0, 22)
    return {
        "head": head, "neck": neck, "chest": chest, "spine": spine, "hips": hips,
        "arm_l": arm_l, "fore_l": fore_l, "hand_l": hand_l,
        "arm_r": arm_r, "fore_r": fore_r, "hand_r": hand_r, "sword": sword,
        "up_l": up_l, "leg_l": leg_l, "foot_l": foot_l,
        "up_r": up_r, "leg_r": leg_r, "foot_r": foot_r,
        "scabbard": scabbard, "root": root,
    }


def load_parts(path: Path):
    groups = defaultdict(list)
    bone = "Hips"
    pending = []
    for raw in path.read_text().splitlines():
        if not raw or raw[0] == "#":
            continue
        if raw.startswith("BONE "):
            bone = raw[5:].strip()
            pending = []
            continue
        if raw.startswith("V "):
            p = raw[2:].split()
            v = np.array([float(p[0]), float(p[1]), float(p[2])], np.float64)
            n = np.array([float(p[3]), float(p[4]), float(p[5])], np.float64)
            uv = np.array([float(p[6]), float(p[7])], np.float64)
            pending.append((v, n, uv))
            continue
        if raw == "T" and len(pending) >= 3:
            (p0, n0, uv0), (p1, n1, uv1), (p2, n2, uv2) = pending[-3:]
            groups[bone].append((p0, p1, p2, n0, n1, n2, uv0, uv1, uv2))
    out = []
    for name, tris in groups.items():
        py = BONE_PY.get(name)
        if py:
            out.append((py, tris))
    return out


def xform_p(m, p):
    h = m @ np.array([p[0], p[1], p[2], 1.0])
    return h[:3]


def xform_n(m, n):
    r = m[:3, :3] @ n
    ln = np.linalg.norm(r)
    return r / ln if ln > 1e-8 else n


def sample_atlas(uv):
    u = min(1.0, max(0.0, float(uv[0])))
    v = min(1.0, max(0.0, float(uv[1])))
    x = min(AW - 1, int(u * (AW - 1)))
    y = min(AH - 1, int((1.0 - v) * (AH - 1)))
    return ATLAS[y, x]


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


def raster_tri_uv(zbuf, cbuf, p0, p1, p2, uv0, uv1, uv2, shade):
    xs = (p0[0], p1[0], p2[0])
    ys = (p0[1], p1[1], p2[1])
    minx = max(0, int(math.floor(min(xs))))
    maxx = min(W - 1, int(math.ceil(max(xs))))
    miny = max(0, int(math.floor(min(ys))))
    maxy = min(H - 1, int(math.ceil(max(ys))))
    if minx > maxx or miny > maxy:
        return
    area = (p1[0] - p0[0]) * (p2[1] - p0[1]) - (p1[1] - p0[1]) * (p2[0] - p0[0])
    if abs(area) < 1e-6:
        return
    for y in range(miny, maxy + 1):
        for x in range(minx, maxx + 1):
            w0 = (p1[0] - x) * (p2[1] - y) - (p1[1] - y) * (p2[0] - x)
            w1 = (p2[0] - x) * (p0[1] - y) - (p2[1] - y) * (p0[0] - x)
            w2 = (p0[0] - x) * (p1[1] - y) - (p0[1] - y) * (p1[0] - x)
            if area < 0:
                w0, w1, w2, a = -w0, -w1, -w2, -area
            else:
                a = area
            if w0 < 0 or w1 < 0 or w2 < 0:
                continue
            b0, b1, b2 = w0 / a, w1 / a, w2 / a
            z = b0 * p0[2] + b1 * p1[2] + b2 * p2[2]
            if z >= zbuf[y, x]:
                continue
            uv = b0 * uv0 + b1 * uv1 + b2 * uv2
            col = sample_atlas(uv) * shade
            zbuf[y, x] = z
            cbuf[y, x] = np.clip(col, 0, 255).astype(np.uint8)


def basis(eye, target):
    forward = target - eye
    forward = forward / np.linalg.norm(forward)
    right = np.cross(np.array([0.0, 1.0, 0.0]), forward)
    right = right / np.linalg.norm(right)
    up = np.cross(forward, right)
    return right, up, forward


def render(eye, target, parts, bones):
    right, up, forward = basis(eye, target)
    zbuf = np.full((H, W), 1e9, np.float32)
    cbuf = np.zeros((H, W, 3), np.uint8)
    cbuf[:] = (22, 24, 20)
    for gz in np.linspace(-0.4, 3.2, 8):
        gx0, gx1 = -2.2, 2.2
        w0 = np.array([gx0, 0.0, gz])
        w1 = np.array([gx1, 0.0, gz])
        w2 = np.array([gx1, 0.0, gz + 0.45])
        w3 = np.array([gx0, 0.0, gz + 0.45])
        col = GROUND if int(round(gz * 4)) % 2 == 0 else (36, 42, 30)
        for a, b, c in ((w0, w1, w2), (w0, w2, w3)):
            prs = [project(p, eye, right, up, forward) for p in (a, b, c)]
            if any(p is None for p in prs):
                continue
            raster_tri_uv(
                zbuf, cbuf, prs[0], prs[1], prs[2],
                np.array([0.12, 0.88]), np.array([0.12, 0.88]), np.array([0.12, 0.88]),
                0.35,
            )
            # paint ground solid
            # overwrite with constant via a tiny fake: done after if needed
    # ground as solid tris
    zbuf[:] = 1e9
    cbuf[:] = (22, 24, 20)
    for gz in np.linspace(-0.4, 3.2, 8):
        w0 = np.array([-2.2, 0.0, gz])
        w1 = np.array([2.2, 0.0, gz])
        w2 = np.array([2.2, 0.0, gz + 0.45])
        w3 = np.array([-2.2, 0.0, gz + 0.45])
        col = np.array(GROUND if int(round(gz * 4)) % 2 == 0 else (36, 42, 30), np.float32)
        for tri in ((w0, w1, w2), (w0, w2, w3)):
            prs = [project(p, eye, right, up, forward) for p in tri]
            if any(p is None for p in prs):
                continue
            raster_solid(zbuf, cbuf, prs[0], prs[1], prs[2], col)
    for bone, tris in parts:
        m = bones[bone]
        for p0, p1, p2, n0, n1, n2, uv0, uv1, uv2 in tris:
            w0, w1, w2 = xform_p(m, p0), xform_p(m, p1), xform_p(m, p2)
            nn = xform_n(m, (n0 + n1 + n2) / 3.0)
            shade = 0.52 + 0.48 * max(0.0, float(np.dot(nn, LIGHT)))
            prs = []
            uvs = []
            ok = True
            for wp, uv in ((w0, uv0), (w1, uv1), (w2, uv2)):
                pr = project(wp, eye, right, up, forward)
                if pr is None:
                    ok = False
                    break
                prs.append(pr)
                uvs.append(uv)
            if ok:
                raster_tri_uv(zbuf, cbuf, prs[0], prs[1], prs[2], uvs[0], uvs[1], uvs[2], shade)
    return Image.fromarray(cbuf, "RGB")


def raster_solid(zbuf, cbuf, p0, p1, p2, col):
    xs = (p0[0], p1[0], p2[0])
    ys = (p0[1], p1[1], p2[1])
    minx = max(0, int(math.floor(min(xs))))
    maxx = min(W - 1, int(math.ceil(max(xs))))
    miny = max(0, int(math.floor(min(ys))))
    maxy = min(H - 1, int(math.ceil(max(ys))))
    area = (p1[0] - p0[0]) * (p2[1] - p0[1]) - (p1[1] - p0[1]) * (p2[0] - p0[0])
    if abs(area) < 1e-6:
        return
    for y in range(miny, maxy + 1):
        for x in range(minx, maxx + 1):
            w0 = (p1[0] - x) * (p2[1] - y) - (p1[1] - y) * (p2[0] - x)
            w1 = (p2[0] - x) * (p0[1] - y) - (p2[1] - y) * (p0[0] - x)
            w2 = (p0[0] - x) * (p1[1] - y) - (p0[1] - y) * (p1[0] - x)
            if area < 0:
                w0, w1, w2, a = -w0, -w1, -w2, -area
            else:
                a = area
            if w0 < 0 or w1 < 0 or w2 < 0:
                continue
            b0, b1, b2 = w0 / a, w1 / a, w2 / a
            z = b0 * p0[2] + b1 * p1[2] + b2 * p2[2]
            if z >= zbuf[y, x]:
                continue
            zbuf[y, x] = z
            cbuf[y, x] = col.astype(np.uint8)


def caption(im: Image.Image, text: str) -> Image.Image:
    d = ImageDraw.Draw(im)
    try:
        f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    except OSError:
        f = ImageFont.load_default()
    d.rectangle((0, 0, W, 64), fill=(16, 16, 14))
    d.text((24, 18), text, fill=(236, 230, 210), font=f)
    return im


def main():
    PROOF.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    parts = load_parts(MESH)
    bones = rest_fk()
    rear = caption(
        render(CAM_EYE, CAM_TARGET, parts, bones),
        "GATE 3  ·  World-cam Play angle  ·  high rear  ·  TOP = away  ·  look NOT claimed",
    )
    q = caption(
        render(CAM_EYE_34, CAM_TARGET, parts, bones),
        "GATE 3  ·  World-cam Play ¾  ·  high rear +X  ·  scabbard character-RIGHT",
    )
    rear.save(PROOF / "world_rear.png", optimize=True)
    q.save(PROOF / "world_rear_34.png", optimize=True)
    rear.save(ART / "gate3_world_rear.png")
    q.save(ART / "gate3_world_rear_34.png")
    print("world_rear", (PROOF / "world_rear.png").stat().st_size)
    print("world_rear_34", (PROOF / "world_rear_34.png").stat().st_size)


if __name__ == "__main__":
    main()
