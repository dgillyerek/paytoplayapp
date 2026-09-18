#!/usr/bin/env python3
"""3D Animator-matching Sir Aldric preview (high-angle rear, 1080x1920).

Ports SirAldric3DMotion + the runtime proxy boxes. Not PNG warp / paper-doll.
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
W, H = 1080, 1920
WALK_PERIOD = 0.80
WALK_CYCLES = 2
ATTACK = 1.40
WALK_BLOCK = WALK_PERIOD * WALK_CYCLES
LOOP = WALK_BLOCK + ATTACK

SILVER = (186, 194, 204)
GOLD = (211, 176, 82)
BLUE = (41, 76, 148)
BROWN = (92, 56, 33)
DARK = (46, 51, 56)
GROUND = (26, 31, 23)


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
    root_z = repeat(t * 0.22, 0.85)
    if attacking:
        return attack_pose(loop_t - WALK_BLOCK, root_z)
    return walk_pose(loop_t, root_z)


def walk_pose(loop_t, root_z):
    phase = loop_t / WALK_PERIOD * math.tau
    step = math.sin(phase)
    bob = 0.028 * abs(math.sin(phase))
    sway = 5.5 * step
    left_x, right_x = 38 * step, -38 * step
    knee_l = 10 + 48 * max(0.0, -step)
    knee_r = 10 + 48 * max(0.0, step)
    return dict(
        attacking=False,
        drawn=False,
        root_z=root_z,
        root_y=bob,
        hips=(0, sway * 0.15, 0),
        spine=(4, sway, 0),
        chest=(0, sway * 0.4, 0),
        head=(-6, 0, 0),
        up_l=(left_x, 0, 0),
        leg_l=(knee_l, 0, 0),
        foot_l=(-8 - 10 * max(0.0, step), 0, 0),
        up_r=(right_x, 0, 0),
        leg_r=(knee_r, 0, 0),
        foot_r=(-8 - 10 * max(0.0, -step), 0, 0),
        arm_l=(12 - 22 * step, 0, 8),
        fore_l=(18 + 12 * max(0.0, step), 0, 0),
        arm_r=(18 + 6 * step, 12, -10),
        fore_r=(28, 0, 0),
        hand_r=(0, 0, 0),
        sword=(8, 0, 18),
        label="WALK  ·  toward TOP",
    )


def attack_pose(attack_t, root_z):
    u = clamp01(attack_t / ATTACK)
    if u < 0.18:
        k = smooth01(u / 0.18)
        arm_x, arm_y, fore_x = lerp(18, -70, k), lerp(12, 6, k), lerp(28, 8, k)
        lunge, spine_x, drawn_k = 0.0, lerp(4, -8, k), k
    elif u < 0.40:
        k = smooth01((u - 0.18) / 0.22)
        arm_x, arm_y, fore_x = lerp(-70, -150, k), lerp(6, 2, k), lerp(8, -12, k)
        lunge, spine_x, drawn_k = lerp(0, 0.04, k), lerp(-8, -14, k), 1.0
    elif u < 0.56:
        k = smooth01((u - 0.40) / 0.16)
        arm_x, arm_y, fore_x = lerp(-150, -118, k), lerp(2, 0, k), lerp(-12, 18, k)
        lunge, spine_x, drawn_k = lerp(0.04, 0.10, k), lerp(-14, 8, k), 1.0
    elif u < 0.78:
        k = smooth01((u - 0.56) / 0.22)
        arm_x, arm_y, fore_x = lerp(-118, -40, k), lerp(0, 8, k), lerp(22, 20, k)
        lunge, spine_x, drawn_k = lerp(0.10, 0.02, k), lerp(8, 0, k), 1.0 - k * 0.35
    else:
        k = smooth01((u - 0.78) / 0.22)
        arm_x, arm_y, fore_x = lerp(-40, 18, k), lerp(8, 12, k), lerp(20, 28, k)
        lunge, spine_x, drawn_k = lerp(0.02, 0, k), lerp(0, 4, k), 1.0 - k
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
        head=(-8, 0, 0),
        up_l=(8, 0, 0),
        leg_l=(12, 0, 0),
        foot_l=(-6, 0, 0),
        up_r=(-6, 0, 0),
        leg_r=(16, 0, 0),
        foot_r=(-4, 0, 0),
        arm_l=(16, 0, 10),
        fore_l=(20, 0, 0),
        arm_r=(arm_x, arm_y, -8),
        fore_r=(fore_x, 0, 0),
        hand_r=(-15 if drawn else 0, 0, 0),
        sword=(-8 if drawn else 8, 0, 0 if drawn else 18),
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
    sword = hand_r @ local_of((0.02, -0.08, 0.02), pose["sword"])
    up_l = hips @ local_of((-0.11, -0.04, 0), pose["up_l"])
    leg_l = up_l @ local_of((0, -0.42, 0), pose["leg_l"])
    foot_l = leg_l @ local_of((0, -0.40, 0.05), pose["foot_l"])
    up_r = hips @ local_of((0.11, -0.04, 0), pose["up_r"])
    leg_r = up_r @ local_of((0, -0.42, 0), pose["leg_r"])
    foot_r = leg_r @ local_of((0, -0.40, 0.05), pose["foot_r"])
    scabbard = hips @ trans((0.20, -0.04, -0.02)) @ rot_euler(18, 0, 22)
    return {
        "head": head,
        "neck": neck,
        "chest": chest,
        "spine": spine,
        "hips": hips,
        "arm_l": arm_l,
        "fore_l": fore_l,
        "hand_l": hand_l,
        "arm_r": arm_r,
        "fore_r": fore_r,
        "hand_r": hand_r,
        "sword": sword,
        "up_l": up_l,
        "leg_l": leg_l,
        "foot_l": foot_l,
        "up_r": up_r,
        "leg_r": leg_r,
        "foot_r": foot_r,
        "scabbard": scabbard,
    }


BOXES = [
    ("head", (0, 0.14, 0.02), (0.24, 0.28, 0.26), SILVER),
    ("head", (0, 0.30, 0), (0.04, 0.10, 0.04), GOLD),
    ("head", (0, 0.10, 0.14), (0.16, 0.08, 0.04), DARK),
    ("neck", (0, 0.02, 0), (0.16, 0.18, 0.16), SILVER),
    ("chest", (0, 0.02, 0), (0.40, 0.36, 0.22), SILVER),
    ("chest", (0, -0.02, -0.12), (0.28, 0.28, 0.04), BLUE),
    ("chest", (0, 0.02, -0.135), (0.10, 0.14, 0.02), GOLD),
    ("chest", (-0.22, 0.12, 0), (0.16, 0.14, 0.16), SILVER),
    ("chest", (0.22, 0.12, 0), (0.16, 0.14, 0.16), SILVER),
    ("chest", (-0.22, 0.18, 0), (0.10, 0.04, 0.10), GOLD),
    ("chest", (0.22, 0.18, 0), (0.10, 0.04, 0.10), GOLD),
    ("spine", (0, 0.02, 0), (0.30, 0.16, 0.18), SILVER),
    ("hips", (0, -0.06, 0), (0.36, 0.22, 0.20), BLUE),
    ("hips", (0, -0.16, 0), (0.38, 0.05, 0.18), GOLD),
    ("arm_l", (0, -0.14, 0), (0.12, 0.30, 0.12), SILVER),
    ("fore_l", (0, -0.12, 0), (0.10, 0.26, 0.10), SILVER),
    ("hand_l", (0, -0.04, 0), (0.10, 0.10, 0.10), SILVER),
    ("arm_r", (0, -0.14, 0), (0.12, 0.30, 0.12), SILVER),
    ("fore_r", (0, -0.12, 0), (0.10, 0.26, 0.10), SILVER),
    ("hand_r", (0, -0.04, 0), (0.10, 0.10, 0.10), SILVER),
    ("sword", (0.01, -0.28, 0.02), (0.035, 0.62, 0.045), SILVER),
    ("sword", (0.01, 0.04, 0.02), (0.12, 0.04, 0.08), GOLD),
    ("up_l", (0, -0.20, 0), (0.15, 0.44, 0.16), SILVER),
    ("leg_l", (0, -0.18, 0), (0.14, 0.42, 0.15), SILVER),
    ("foot_l", (0, -0.02, 0.06), (0.13, 0.08, 0.24), SILVER),
    ("foot_l", (0, 0.02, 0.04), (0.10, 0.03, 0.10), GOLD),
    ("up_r", (0, -0.20, 0), (0.15, 0.44, 0.16), SILVER),
    ("leg_r", (0, -0.18, 0), (0.14, 0.42, 0.15), SILVER),
    ("foot_r", (0, -0.02, 0.06), (0.13, 0.08, 0.24), SILVER),
    ("foot_r", (0, 0.02, 0.04), (0.10, 0.03, 0.10), GOLD),
    ("scabbard", (0.02, -0.22, 0), (0.055, 0.52, 0.055), BROWN),
    ("scabbard", (0.02, 0.06, 0), (0.08, 0.05, 0.08), GOLD),
]

FACES = (
    (0, 1, 2, 3, (0, 0, -1)),
    (5, 4, 7, 6, (0, 0, 1)),
    (4, 0, 3, 7, (-1, 0, 0)),
    (1, 5, 6, 2, (1, 0, 0)),
    (3, 2, 6, 7, (0, 1, 0)),
    (4, 5, 1, 0, (0, -1, 0)),
)
LIGHT = np.array([0.35, 0.82, -0.45], np.float64)
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def box_corners(center, size):
    e = np.array(size) * 0.5
    c = np.array(center)
    return np.array(
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


def xform_p(m, p):
    h = m @ np.array([p[0], p[1], p[2], 1.0])
    return h[:3]


def project(p, eye, r, u, f):
    d = p - eye
    cam = np.array([np.dot(d, r), np.dot(d, u), np.dot(d, f)])
    if cam[2] < 0.08:
        return None
    fov = math.radians(28)
    fy = 1.0 / math.tan(fov * 0.5)
    aspect = W / float(H)
    ndc_x = fy * cam[0] / (cam[2] * aspect)
    ndc_y = fy * cam[1] / cam[2]
    sx = (ndc_x + 1.0) * 0.5 * W
    sy = (1.0 - ndc_y) * 0.5 * H
    return sx, sy, cam[2]


def raster_tri(color, zbuf, cbuf, p0, p1, p2, shade):
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
    col = np.clip(np.array(color) * shade, 0, 255).astype(np.uint8)
    cbuf[miny : maxy + 1, minx : maxx + 1][nearer] = col


def render_pose(pose):
    bones = fk(pose)
    z = pose["root_z"]
    eye = np.array([0.0, 2.45, z - 5.50])
    target = np.array([0.0, 1.00, z + 0.20])
    forward = target - eye
    forward = forward / np.linalg.norm(forward)
    right = np.cross(np.array([0.0, 1.0, 0.0]), forward)
    right = right / np.linalg.norm(right)
    up = np.cross(forward, right)
    zbuf = np.full((H, W), 1e9, np.float32)
    cbuf = np.zeros((H, W, 3), np.uint8)
    cbuf[:] = (20, 23, 18)
    cbuf[int(H * 0.80) :] = GROUND

    for bone, center, size, color in BOXES:
        m = bones[bone]
        corners = [xform_p(m, p) for p in box_corners(center, size)]
        for a, b, c, d, nloc in FACES:
            n = m[:3, :3] @ np.array(nloc, np.float64)
            n = n / (np.linalg.norm(n) + 1e-8)
            shade = 0.28 + 0.72 * max(0.0, float(np.dot(n, LIGHT)))
            pts = []
            ok = True
            for idx in (a, b, c):
                pr = project(corners[idx], eye, right, up, forward)
                if pr is None:
                    ok = False
                    break
                pts.append(pr)
            if ok:
                raster_tri(color, zbuf, cbuf, pts[0], pts[1], pts[2], shade)
            pts = []
            ok = True
            for idx in (a, c, d):
                pr = project(corners[idx], eye, right, up, forward)
                if pr is None:
                    ok = False
                    break
                pts.append(pr)
            if ok:
                raster_tri(color, zbuf, cbuf, pts[0], pts[1], pts[2], shade)

    im = Image.fromarray(cbuf, "RGB")
    d = ImageDraw.Draw(im)
    try:
        title = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 28)
        phase = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
        note = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
    except OSError:
        title = phase = note = ImageFont.load_default()
    d.text((W / 2, 72), "SIR ALDRIC  ·  walk → attack TOP", fill=(237, 230, 209), font=title, anchor="mm")
    d.text((W / 2, 118), pose["label"], fill=(211, 176, 82), font=phase, anchor="mm")
    d.text(
        (W / 2, H - 52),
        "3D Animator proxy  ·  high-angle rear  ·  no PNG warp",
        fill=(168, 172, 164),
        font=note,
        anchor="mm",
    )
    return im


def body_crop(im):
    return im.crop((0, 150, W, H - 90))


def max_delta(a, b):
    diff = ImageChops.difference(a, b)
    return max(ImageStat.Stat(diff).extrema[c][1] for c in range(3))


def mean_delta(a, b):
    diff = ImageChops.difference(a, b)
    return sum(ImageStat.Stat(diff).mean) / 3.0


def main():
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

    walk_a = render_pose(evaluate(WALK_PERIOD * 0.25))
    walk_b = render_pose(evaluate(WALK_PERIOD * 0.75))
    strike = render_pose(evaluate(WALK_BLOCK + ATTACK * 0.48))
    recover = render_pose(evaluate(WALK_BLOCK + ATTACK * 0.92))
    d_walk = max_delta(body_crop(walk_a), body_crop(walk_b))
    d_strike = max_delta(body_crop(walk_a), body_crop(strike))
    m_walk = mean_delta(body_crop(walk_a), body_crop(walk_b))
    m_strike = mean_delta(body_crop(walk_a), body_crop(strike))
    proof = (
        f"3D Animator proxy (not PNG warp). max |Δ| excluding UI: walk opposite-step "
        f"{d_walk:.0f}/255 (mean {m_walk:.1f}); walk vs strike {d_strike:.0f}/255 (mean {m_strike:.1f}). "
        "Connected 3D blockout; sword on character-right hip; high-angle rear toward TOP."
    )
    print(proof)
    if d_walk < 20 or d_strike < 20:
        raise SystemExit("FAIL: 3D motion too weak: " + proof)

    cell_w, cell_h = 540, 960
    sheet = Image.new("RGB", (W, H), (20, 23, 18))
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    for i, (im, lab) in enumerate(
        [(walk_a, "WALK L"), (walk_b, "WALK R"), (strike, "STRIKE TOP"), (recover, "RECOVER")]
    ):
        x, y = (i % 2) * cell_w, (i // 2) * cell_h
        sheet.paste(im.resize((cell_w, cell_h), Image.BILINEAR), (x, y))
        ImageDraw.Draw(sheet).text((x + 16, y + 16), lab, fill=(237, 230, 209), font=font)

    still = OUT / "sir_aldric_locked_rear_gameview_1080x1920.png"
    walk_a.save(still, optimize=True)
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
        dest = ART / f"aldric3d_{p.name}"
        shutil.copy2(p, dest)
        print("wrote", p, p.stat().st_size)


if __name__ == "__main__":
    main()
