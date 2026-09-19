#!/usr/bin/env python3
"""QUARANTINED 2D PNG mesh-warp preview. Hero system is 3D Animator — see render_sir_aldric_3d_preview.py."""

Ports SirAldricMotion.Evaluate + SirAldricWarp.Displace. One connected sprite;
no hard-cut floating limbs. Output: 1080x1920 stills + gif + mp4.
"""
from __future__ import annotations

import math
import shutil
import subprocess
from collections import deque
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat

ROOT = Path(__file__).resolve().parents[3]
MASTER = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/SIR_ALDRIC_REAR_MASTER_LOCKED.png"
OUT = Path(__file__).resolve().parent
ART = Path("/opt/cursor/artifacts")
W, H = 1080, 1920
CHAR_H = 760.0

WALK_PERIOD = 0.70
WALK_CYCLES = 2
ATTACK = 1.35
WALK_BLOCK = WALK_PERIOD * WALK_CYCLES
LOOP = WALK_BLOCK + ATTACK

HIP_L, FOOT_L = (0.36, 0.41), (0.33, 0.03)
HIP_R, FOOT_R = (0.64, 0.41), (0.67, 0.03)
HILT, TIP = (0.76, 0.50), (0.92, 0.13)
SPINE = (0.50, 0.50)
LEG_RADIUS = 0.16
SWORD_RADIUS = 0.085
TORSO_RADIUS = 0.42
GRID_COLS, GRID_ROWS = 28, 36


def repeat(t: float, length: float) -> float:
    r = t % length
    return r + length if r < 0 else r


def clamp01(x: float) -> float:
    return 0.0 if x < 0 else 1.0 if x > 1 else x


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def smooth01(x: float) -> float:
    x = clamp01(x)
    return x * x * (3 - 2 * x)


@dataclass
class Bone:
    x: float = 0.0
    y: float = 0.0
    rot: float = 0.0
    sx: float = 1.0
    sy: float = 1.0


@dataclass
class Pose:
    attacking: bool
    drawn: bool
    march: float
    root: Bone
    torso: Bone
    legl: Bone
    legr: Bone
    sword: Bone


def evaluate(t: float) -> Pose:
    loop_t = repeat(t, LOOP)
    attacking = loop_t >= WALK_BLOCK
    speed = 0.018 if attacking else 0.11
    march = 0.22 + 0.52 * repeat(t * speed, 1.0)
    if attacking:
        return attack_pose(loop_t - WALK_BLOCK, march)
    return walk_pose(loop_t, march)


def walk_pose(loop_t: float, march: float) -> Pose:
    phase = loop_t / WALK_PERIOD * math.tau
    step = math.sin(phase)
    bob = 0.034 * abs(math.sin(phase))
    squash = 1.0 - 0.045 * abs(math.sin(phase))
    sway = 6.5 * step
    return Pose(
        False,
        False,
        march,
        Bone(0.0, bob, sway * 0.12, 1.0, squash),
        Bone(0.0, bob * 0.40, sway),
        Bone(-0.024 * step, 0.125 * step, 20.0 * step),
        Bone(0.024 * step, -0.125 * step, -20.0 * step),
        Bone(0.004 * step, 0.010 * step, 5.0 * step),
    )


def attack_pose(attack_t: float, march: float) -> Pose:
    u = clamp01(attack_t / ATTACK)
    if u < 0.16:
        k = smooth01(u / 0.16)
        sword_rot, lunge, torso_rot = lerp(0, 42, k), 0.0, lerp(0, -7, k)
    elif u < 0.36:
        k = smooth01((u - 0.16) / 0.20)
        sword_rot, lunge, torso_rot = lerp(42, 118, k), lerp(0, 0.035, k), lerp(-7, -11, k)
    elif u < 0.52:
        k = smooth01((u - 0.36) / 0.16)
        sword_rot, lunge, torso_rot = lerp(118, 178, k), lerp(0.035, 0.08, k), lerp(-11, 8, k)
    elif u < 0.78:
        k = smooth01((u - 0.52) / 0.26)
        sword_rot, lunge, torso_rot = lerp(178, 58, k), lerp(0.08, 0.015, k), lerp(8, -3, k)
    else:
        k = smooth01((u - 0.78) / 0.22)
        sword_rot, lunge, torso_rot = lerp(58, 0, k), lerp(0.015, 0, k), lerp(-3, 0, k)
    return Pose(
        True,
        sword_rot > 22,
        march,
        Bone(0.0, lunge, 0.0, 1.0, 1.0 - lunge * 0.35),
        Bone(0.0, lunge * 0.25, torso_rot),
        Bone(-0.01, 0.01, 4.0),
        Bone(0.012, -0.004, -5.0),
        Bone(0.0, 0.0, sword_rot),
    )


def bone_weights(u, v, pivot, tip, radius):
    dx = tip[0] - pivot[0]
    dy = tip[1] - pivot[1]
    len2 = dx * dx + dy * dy
    if len2 < 1e-8:
        return 0.0, 0.0
    t = ((u - pivot[0]) * dx + (v - pivot[1]) * dy) / len2
    along = clamp01(t)
    px = pivot[0] + dx * along
    py = pivot[1] + dy * along
    dist = math.hypot(u - px, v - py)
    radial = 1.0 - smooth01((dist - 0.015) / radius)
    return along, radial


def torso_weight(u, v):
    rise = smooth01((v - 0.38) / 0.16)
    head = 1.0 - smooth01((v - 0.86) / 0.12)
    mid = 1.0 - smooth01((abs(u - 0.50) - 0.12) / TORSO_RADIUS)
    return rise * head * mid


def rotate_about(x, y, u, v, px, py, rot_deg, weight):
    if abs(rot_deg) < 1e-4 or weight <= 1e-5:
        return x, y
    rad = math.radians(rot_deg)
    c, s = math.cos(rad), math.sin(rad)
    rx, ry = u - px, v - py
    qx = px + rx * c - ry * s
    qy = py + rx * s + ry * c
    return x + (qx - u) * weight, y + (qy - v) * weight


def bend(x, y, u, v, pivot, tip, rot, tx, ty, radius, along_from_hip=True):
    along, radial = bone_weights(u, v, pivot, tip, radius)
    w = (along if along_from_hip else 1.0) * radial
    if w <= 1e-5:
        return x, y
    x, y = rotate_about(x, y, u, v, pivot[0], pivot[1], rot, w)
    return x + tx * w, y + ty * w


def displace(u, v, pose: Pose):
    x, y = u, v
    x, y = bend(x, y, u, v, HIP_L, FOOT_L, pose.legl.rot, pose.legl.x, pose.legl.y, LEG_RADIUS, True)
    x, y = bend(x, y, u, v, HIP_R, FOOT_R, pose.legr.rot, pose.legr.x, pose.legr.y, LEG_RADIUS, True)
    tw = torso_weight(u, v)
    x, y = rotate_about(x, y, u, v, SPINE[0], SPINE[1], pose.torso.rot, tw)
    x += pose.torso.x * tw
    y += pose.torso.y * tw
    x, y = bend(x, y, u, v, HILT, TIP, pose.sword.rot, pose.sword.x, pose.sword.y, SWORD_RADIUS, False)
    return x, y


def sample_bilinear(arr, x, y):
    h, w = arr.shape[:2]
    if x < 0 or y < 0 or x >= w - 1 or y >= h - 1:
        return None
    x0 = int(math.floor(x))
    y0 = int(math.floor(y))
    fx = x - x0
    fy = y - y0
    c00 = arr[y0, x0].astype(np.float32)
    c10 = arr[y0, x0 + 1].astype(np.float32)
    c01 = arr[y0 + 1, x0].astype(np.float32)
    c11 = arr[y0 + 1, x0 + 1].astype(np.float32)
    return (c00 * (1 - fx) * (1 - fy) + c10 * fx * (1 - fy) + c01 * (1 - fx) * fy + c11 * fx * fy)


def raster_tri(dest, wgt, src, s0, s1, s2, d0, d1, d2, score):
    dh, dw = dest.shape[:2]
    xs = (d0[0], d1[0], d2[0])
    ys = (d0[1], d1[1], d2[1])
    minx = max(0, int(math.floor(min(xs))))
    maxx = min(dw - 1, int(math.ceil(max(xs))))
    miny = max(0, int(math.floor(min(ys))))
    maxy = min(dh - 1, int(math.ceil(max(ys))))
    if maxx < minx or maxy < miny:
        return
    denom = (d1[1] - d2[1]) * (d0[0] - d2[0]) + (d2[0] - d1[0]) * (d0[1] - d2[1])
    if abs(denom) < 1e-8:
        return
    yy, xx = np.mgrid[miny : maxy + 1, minx : maxx + 1]
    a = ((d1[1] - d2[1]) * (xx - d2[0]) + (d2[0] - d1[0]) * (yy - d2[1])) / denom
    b = ((d2[1] - d0[1]) * (xx - d2[0]) + (d0[0] - d2[0]) * (yy - d2[1])) / denom
    c = 1.0 - a - b
    mask = (a >= -0.01) & (b >= -0.01) & (c >= -0.01)
    if not np.any(mask):
        return
    sx = a * s0[0] + b * s1[0] + c * s2[0]
    sy = a * s0[1] + b * s1[1] + c * s2[1]
    h, w = src.shape[:2]
    sx = np.clip(sx, 0, w - 1.001)
    sy = np.clip(sy, 0, h - 1.001)
    x0 = np.floor(sx).astype(np.int32)
    y0 = np.floor(sy).astype(np.int32)
    fx = (sx - x0)[..., None]
    fy = (sy - y0)[..., None]
    c00 = src[y0, x0].astype(np.float32)
    c10 = src[y0, np.clip(x0 + 1, 0, w - 1)].astype(np.float32)
    c01 = src[np.clip(y0 + 1, 0, h - 1), x0].astype(np.float32)
    c11 = src[np.clip(y0 + 1, 0, h - 1), np.clip(x0 + 1, 0, w - 1)].astype(np.float32)
    col = c00 * (1 - fx) * (1 - fy) + c10 * fx * (1 - fy) + c01 * (1 - fx) * fy + c11 * fx * fy
    sub_d = dest[miny : maxy + 1, minx : maxx + 1]
    sub_w = wgt[miny : maxy + 1, minx : maxx + 1]
    m = mask & (score >= sub_w - 1e-4)
    sub_d[m] = col[m]
    sub_w[m] = np.maximum(sub_w[m], score)


def travel(s0, s1, s2, d0, d1, d2):
    t = 0.0
    for s, d in ((s0, d0), (s1, d1), (s2, d2)):
        t = max(t, math.hypot(d[0] - s[0], d[1] - s[1]))
    return 1.0 + t


def warp_sprite(src: np.ndarray, pose: Pose, pad: int = 128) -> Image.Image:
    sh, sw = src.shape[:2]
    cols, rows = GRID_COLS, GRID_ROWS
    su = np.zeros((rows + 1, cols + 1, 2), np.float32)
    du = np.zeros((rows + 1, cols + 1, 2), np.float32)
    for j in range(rows + 1):
        v = j / rows
        for i in range(cols + 1):
            u = i / cols
            nu, nv = displace(u, v, pose)
            su[j, i] = (u * (sw - 1), (1.0 - v) * (sh - 1))
            du[j, i] = (nu * (sw - 1) + pad, (1.0 - nv) * (sh - 1) + pad)
    dest = np.zeros((sh + pad * 2, sw + pad * 2, 4), np.float32)
    wgt = np.zeros(dest.shape[:2], np.float32)
    for j in range(rows):
        for i in range(cols):
            s00, s10 = su[j, i], su[j, i + 1]
            s01, s11 = su[j + 1, i], su[j + 1, i + 1]
            d00, d10 = du[j, i], du[j, i + 1]
            d01, d11 = du[j + 1, i], du[j + 1, i + 1]
            raster_tri(dest, wgt, src, s00, s01, s11, d00, d01, d11, score=travel(s00, s01, s11, d00, d01, d11))
            raster_tri(dest, wgt, src, s00, s11, s10, d00, d11, d10, score=travel(s00, s11, s10, d00, d11, d10))
    dest[:, :, 3] *= (wgt > 0).astype(np.float32)
    return Image.fromarray(np.clip(dest, 0, 255).astype(np.uint8), "RGBA")


def font(size, bold=False):
    path = (
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
        if bold
        else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
    )
    try:
        return ImageFont.truetype(path, size)
    except OSError:
        return ImageFont.load_default()


TITLE, PHASE, NOTE = font(28, True), font(22, True), font(18)


def caption(im: Image.Image, phase: str):
    d = ImageDraw.Draw(im)
    d.text((W / 2, 72), "SIR ALDRIC  ·  walk → attack TOP", fill=(237, 230, 209, 255), font=TITLE, anchor="mm")
    d.text((W / 2, 118), phase, fill=(211, 176, 82, 255), font=PHASE, anchor="mm")
    d.text(
        (W / 2, H - 52),
        "LOCKED rear master  ·  mesh warp  ·  no AI frames",
        fill=(168, 172, 164, 255),
        font=NOTE,
        anchor="mm",
    )


def phase_label(pose: Pose) -> str:
    if not pose.attacking:
        return "WALK  ·  toward TOP"
    if pose.drawn and pose.sword.rot >= 90:
        return "ATTACK  ·  strike TOP"
    return "ATTACK  ·  draw / recover"


def render_pose(src: np.ndarray, pose: Pose) -> Image.Image:
    warped = warp_sprite(src, pose)
    sh, sw = src.shape[:2]
    scale = CHAR_H / sh
    nw = max(1, int(warped.width * scale))
    nh = max(1, int(warped.height * scale))
    sprite = warped.resize((nw, nh), Image.BILINEAR)
    frame = Image.new("RGBA", (W, H), (20, 23, 18, 255))
    ImageDraw.Draw(frame).rectangle((0, int(H * 0.80), W, H), fill=(14, 16, 12, 255))
    # rest sprite (without pad) is sw x sh; pad is 96 source px → scale
    pad_px = 128 * scale
    rest_w, rest_h = sw * scale, sh * scale
    root_cx = W * 0.5 + pose.root.x * CHAR_H
    # pivot 0.08 from bottom of unpadded sprite
    root_py = H * (1.0 - pose.march) - pose.root.y * CHAR_H
    cr = math.radians(pose.root.rot)
    # unpadded bottom-center in warped image: x = pad + rest_w/2, y = pad + rest_h
    local_pivot = (pad_px + rest_w * 0.5, pad_px + rest_h * (1.0 - 0.08))

    def world(lx, ly):
        lx *= pose.root.sx
        ly *= pose.root.sy
        rx = lx * math.cos(cr) - ly * math.sin(cr)
        ry = lx * math.sin(cr) + ly * math.cos(cr)
        return root_cx + rx, root_py - ry

    sx, sy = pose.root.sx, pose.root.sy
    if abs(sx - 1.0) > 0.001 or abs(sy - 1.0) > 0.001:
        nw = max(1, int(sprite.width * sx))
        nh = max(1, int(sprite.height * sy))
        sprite = sprite.resize((nw, nh), Image.BILINEAR)
        local_pivot = (local_pivot[0] * sx, local_pivot[1] * sy)
    dest = world(0.0, 0.0)
    if abs(pose.root.rot) > 0.05:
        pad_r = int(max(sprite.size) * 0.25) + 8
        layer = Image.new("RGBA", (sprite.width + pad_r * 2, sprite.height + pad_r * 2), (0, 0, 0, 0))
        layer.alpha_composite(sprite, (pad_r, pad_r))
        cx, cy = pad_r + local_pivot[0], pad_r + local_pivot[1]
        layer = layer.rotate(pose.root.rot, resample=Image.BICUBIC, center=(cx, cy), expand=False)
        paste = (int(round(dest[0] - cx)), int(round(dest[1] - cy)))
        frame.alpha_composite(layer, paste)
    else:
        paste = (int(round(dest[0] - local_pivot[0])), int(round(dest[1] - local_pivot[1])))
        frame.alpha_composite(sprite, paste)
    caption(frame, phase_label(pose))
    return frame


def body_crop_excluding_ui(im: Image.Image) -> Image.Image:
    return im.crop((0, 150, W, H - 90))


def max_channel_delta(a: Image.Image, b: Image.Image) -> float:
    diff = ImageChops.difference(a.convert("RGB"), b.convert("RGB"))
    return max(ImageStat.Stat(diff).extrema[c][1] for c in range(3))


def mean_channel_delta(a: Image.Image, b: Image.Image) -> float:
    diff = ImageChops.difference(a.convert("RGB"), b.convert("RGB"))
    return sum(ImageStat.Stat(diff).mean) / 3.0


def alpha_components(im: Image.Image) -> int:
    a = np.array(im.convert("RGBA"))[150 : H - 90, :, 3] > 20
    # downsample
    a = a[::8, ::8]
    h, w = a.shape
    seen = np.zeros_like(a, dtype=np.uint8)
    n = 0
    for y in range(h):
        for x in range(w):
            if not a[y, x] or seen[y, x]:
                continue
            n += 1
            q = deque([(y, x)])
            seen[y, x] = 1
            while q:
                cy, cx = q.popleft()
                for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
                    if 0 <= ny < h and 0 <= nx < w and a[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = 1
                        q.append((ny, nx))
    return n


def main():
    master = Image.open(MASTER).convert("RGBA")
    box = master.getchannel("A").getbbox()
    crop = master.crop(box)
    src = np.array(crop)
    fps = 16
    n = int(LOOP * fps)
    raw_dir = Path("/tmp/aldric-meshwarp")
    if raw_dir.exists():
        shutil.rmtree(raw_dir)
    raw_dir.mkdir()
    frames = []
    for i in range(n):
        t = i / fps
        im = render_pose(src, evaluate(t))
        rgb = im.convert("RGB")
        rgb.save(raw_dir / f"f_{i:03d}.png")
        frames.append(rgb)
        print(f"{i + 1}/{n}", flush=True)

    walk_a = render_pose(src, evaluate(WALK_PERIOD * 0.25))
    walk_b = render_pose(src, evaluate(WALK_PERIOD * 0.75))
    strike = render_pose(src, evaluate(WALK_BLOCK + ATTACK * 0.46))
    recover = render_pose(src, evaluate(WALK_BLOCK + ATTACK * 0.92))

    body_a = body_crop_excluding_ui(walk_a)
    body_b = body_crop_excluding_ui(walk_b)
    body_s = body_crop_excluding_ui(strike)
    d_walk = max_channel_delta(body_a, body_b)
    d_strike = max_channel_delta(body_a, body_s)
    m_walk = mean_channel_delta(body_a, body_b)
    m_strike = mean_channel_delta(body_s, body_a)
    rest = render_pose(src, evaluate(0.0))
    c_rest = alpha_components(rest)
    c_walk = alpha_components(walk_a)
    c_strike = alpha_components(strike)
    proof = (
        f"max |Δ| excluding UI: walk opposite-step {d_walk:.0f}/255 (mean {m_walk:.1f}); "
        f"walk vs strike {d_strike:.0f}/255 (mean {m_strike:.1f}). "
        f"silhouette components rest/walk/strike={c_rest}/{c_walk}/{c_strike} "
        "(connected mesh warp; no hard-cut limbs)."
    )
    print(proof)
    if d_walk < 25 or d_strike < 25:
        raise SystemExit(f"FAIL: motion too weak: {proof}")
    if c_walk > c_rest + 1 or c_strike > c_rest + 1:
        raise SystemExit(f"FAIL: silhouette split (detached limbs): {proof}")

    cell_w, cell_h = 540, 960
    sheet = Image.new("RGB", (W, H), (20, 23, 18))
    keys = [
        (walk_a, "WALK L"),
        (walk_b, "WALK R"),
        (strike, "STRIKE TOP"),
        (recover, "RECOVER"),
    ]
    for i, (im, lab) in enumerate(keys):
        thumb = im.resize((cell_w, cell_h), Image.BILINEAR)
        x, y = (i % 2) * cell_w, (i // 2) * cell_h
        sheet.paste(thumb, (x, y))
        ImageDraw.Draw(sheet).text((x + 16, y + 16), lab, fill=(237, 230, 209), font=PHASE)

    still_path = OUT / "sir_aldric_locked_rear_gameview_1080x1920.png"
    walk_a.convert("RGB").save(still_path, optimize=True)
    sheet_path = OUT / "sir_aldric_walk_draw_strike_recover_sheet.png"
    sheet.save(sheet_path, optimize=True)

    gif_full = OUT / "sir_aldric_walk_attack_toward_top.gif"
    gif_walk = OUT / "sir_aldric_locked_master_walks_toward_top.gif"
    pal = [f.convert("P", palette=Image.ADAPTIVE, colors=64) for f in frames]
    pal[0].save(gif_full, save_all=True, append_images=pal[1:], duration=int(1000 / fps), loop=0, optimize=True)
    n_walk = int(WALK_BLOCK * fps)
    pal[0].save(gif_walk, save_all=True, append_images=pal[1:n_walk], duration=int(1000 / fps), loop=0, optimize=True)

    mp4 = OUT / "sir_aldric_walk_attack_toward_top.mp4"
    subprocess.check_call(
        [
            "ffmpeg",
            "-y",
            "-framerate",
            str(fps),
            "-i",
            str(raw_dir / "f_%03d.png"),
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
    for p in (still_path, sheet_path, gif_full, gif_walk, mp4, OUT / "PIXEL_PROOF.txt"):
        uniq = ART / f"meshwarp_{p.name}"
        shutil.copy2(p, uniq)
        print("wrote", p, p.stat().st_size, "and", uniq)


if __name__ == "__main__":
    main()
