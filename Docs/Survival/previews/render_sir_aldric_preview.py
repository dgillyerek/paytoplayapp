#!/usr/bin/env python3
"""Honest Sir Aldric paper-doll preview from the locked rear master.

Crops match SirAldricView / SirAldricMotion.Layout. Pose matches SirAldricMotion.Evaluate.
No AI frames. Output: 1080x1920 stills + gif + mp4 with limb/sword pixel change.
"""
from __future__ import annotations

import math
import shutil
import subprocess
from dataclasses import dataclass
from pathlib import Path

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

TORSO = (0.10, 0.34, 0.86, 1.00)
LEGL = (0.12, 0.00, 0.54, 0.46)
LEGR = (0.42, 0.00, 0.86, 0.46)
SWORD = (0.64, 0.00, 0.98, 0.54)
PIVOT_TORSO = (0.50, 0.18)
PIVOT_LEG = (0.50, 0.90)
PIVOT_SWORD = (0.28, 0.90)


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
        sword_rot, sword_y, sword_x = lerp(0, 42, k), lerp(0, 0.07, k), 0.0
        lunge, torso_rot = 0.0, lerp(0, -7, k)
    elif u < 0.36:
        k = smooth01((u - 0.16) / 0.20)
        sword_rot, sword_y, sword_x = lerp(42, 118, k), lerp(0.07, 0.16, k), lerp(0, -0.03, k)
        lunge, torso_rot = lerp(0, 0.035, k), lerp(-7, -11, k)
    elif u < 0.52:
        k = smooth01((u - 0.36) / 0.16)
        sword_rot, sword_y, sword_x = lerp(118, 178, k), lerp(0.16, 0.30, k), lerp(-0.03, 0.01, k)
        lunge, torso_rot = lerp(0.035, 0.08, k), lerp(-11, 8, k)
    elif u < 0.78:
        k = smooth01((u - 0.52) / 0.26)
        sword_rot, sword_y, sword_x = lerp(178, 58, k), lerp(0.30, 0.08, k), lerp(0.01, 0.02, k)
        lunge, torso_rot = lerp(0.08, 0.015, k), lerp(8, -3, k)
    else:
        k = smooth01((u - 0.78) / 0.22)
        sword_rot, sword_y, sword_x = lerp(58, 0, k), lerp(0.08, 0, k), lerp(0.02, 0, k)
        lunge, torso_rot = lerp(0.015, 0, k), lerp(-3, 0, k)
    return Pose(
        True,
        sword_rot > 22,
        march,
        Bone(0.0, lunge, 0.0, 1.0, 1.0 - lunge * 0.35),
        Bone(0.0, lunge * 0.25, torso_rot),
        Bone(-0.01, 0.01, 4.0),
        Bone(0.012, -0.004, -5.0),
        Bone(sword_x, sword_y, sword_rot),
    )


def opaque_bbox(im: Image.Image):
    box = im.getchannel("A").getbbox()
    if not box:
        return 0, 0, im.width, im.height
    return box


def crop_uv(im: Image.Image, bbox, uv):
    left, top, right, bottom = bbox
    bw, bh = right - left, bottom - top
    x0 = left + uv[0] * bw
    x1 = left + uv[2] * bw
    y0 = top + (1.0 - uv[3]) * bh
    y1 = top + (1.0 - uv[1]) * bh
    return im.crop((int(x0), int(y0), max(int(x0) + 1, int(x1)), max(int(y0) + 1, int(y1))))


def punch_sword(body: Image.Image, bbox, uv) -> Image.Image:
    out = body.copy()
    pix = out.load()
    left, top, right, bottom = bbox
    bw, bh = right - left, bottom - top
    x0 = max(0, int(left + uv[0] * bw) - 1)
    x1 = min(out.width, int(left + uv[2] * bw) + 1)
    y0 = max(0, int(top + (1.0 - uv[3]) * bh) - 1)
    y1 = min(out.height, int(top + (1.0 - uv[1]) * bh) + 1)
    for y in range(y0, y1):
        for x in range(x0, x1):
            r, g, b, a = pix[x, y]
            if a >= 20:
                pix[x, y] = (0, 0, 0, 0)
    return out


def rest_unity(uv, pivot, parent_w, parent_h):
    cx = (uv[0] + (uv[2] - uv[0]) * pivot[0] - 0.5) * parent_w
    cy = (uv[1] + (uv[3] - uv[1]) * pivot[1] - 0.5) * parent_h
    return cx, cy


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
        "LOCKED rear master  ·  paper-doll  ·  no AI frames",
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


def paste_rotated(canvas: Image.Image, sprite: Image.Image, pivot_xy, dest_xy, rot_deg, scale_xy=(1.0, 1.0)):
    sx, sy = scale_xy
    if abs(sx - 1.0) > 0.001 or abs(sy - 1.0) > 0.001:
        nw = max(1, int(sprite.width * sx))
        nh = max(1, int(sprite.height * sy))
        sprite = sprite.resize((nw, nh), Image.BILINEAR)
        pivot_xy = (pivot_xy[0] * sx, pivot_xy[1] * sy)
    px, py = pivot_xy
    pad = int(max(sprite.width, sprite.height) * 1.2) + 8
    layer_w, layer_h = sprite.width + pad * 2, sprite.height + pad * 2
    layer = Image.new("RGBA", (layer_w, layer_h), (0, 0, 0, 0))
    layer.alpha_composite(sprite, (pad, pad))
    cx, cy = pad + px, pad + py
    layer = layer.rotate(rot_deg, resample=Image.BICUBIC, center=(cx, cy), expand=False)
    dest_x = int(round(dest_xy[0] - cx))
    dest_y = int(round(dest_xy[1] - cy))
    # clip composite
    cw, ch = canvas.size
    src = layer
    ox, oy = dest_x, dest_y
    if ox < 0 or oy < 0 or ox + src.width > cw or oy + src.height > ch:
        crop_l = max(0, -ox)
        crop_t = max(0, -oy)
        crop_r = min(src.width, cw - ox)
        crop_b = min(src.height, ch - oy)
        if crop_r <= crop_l or crop_b <= crop_t:
            return
        src = src.crop((crop_l, crop_t, crop_r, crop_b))
        ox = max(0, ox)
        oy = max(0, oy)
    canvas.alpha_composite(src, (ox, oy))


def render_pose(parts, parent_w, parent_h, pose: Pose) -> Image.Image:
    frame = Image.new("RGBA", (W, H), (20, 23, 18, 255))
    ImageDraw.Draw(frame).rectangle((0, int(H * 0.80), W, H), fill=(14, 16, 12, 255))
    root_cx = W * 0.5 + pose.root.x * CHAR_H
    root_py = H * (1.0 - pose.march) - pose.root.y * CHAR_H
    cr = math.radians(pose.root.rot)

    def world(lx, ly):
        lx *= pose.root.sx
        ly *= pose.root.sy
        rx = lx * math.cos(cr) - ly * math.sin(cr)
        ry = lx * math.sin(cr) + ly * math.cos(cr)
        return root_cx + rx, root_py - ry

    order = (
        ("legl", LEGL, PIVOT_LEG, pose.legl),
        ("legr", LEGR, PIVOT_LEG, pose.legr),
        ("torso", TORSO, PIVOT_TORSO, pose.torso),
        ("sword", SWORD, PIVOT_SWORD, pose.sword),
    )
    for name, uv, pivot, bone in order:
        sprite = parts[name]
        rest = rest_unity(uv, pivot, parent_w, parent_h)
        lx = rest[0] + bone.x * CHAR_H
        ly = rest[1] + bone.y * CHAR_H
        dest = world(lx, ly)
        piv = (pivot[0] * sprite.width, (1.0 - pivot[1]) * sprite.height)
        paste_rotated(frame, sprite, piv, dest, bone.rot + pose.root.rot, (bone.sx, bone.sy))
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


def main():
    master = Image.open(MASTER).convert("RGBA")
    bbox = opaque_bbox(master)
    left, top, right, bottom = bbox
    bw, bh = right - left, bottom - top
    parent_h = CHAR_H
    parent_w = CHAR_H * (bw / bh)
    punched = punch_sword(master, bbox, SWORD)
    scale = parent_h / bh
    parts = {
        "torso": crop_uv(punched, bbox, TORSO),
        "legl": crop_uv(punched, bbox, LEGL),
        "legr": crop_uv(punched, bbox, LEGR),
        "sword": crop_uv(master, bbox, SWORD),
    }
    for k, im in list(parts.items()):
        nw = max(1, int(im.width * scale))
        nh = max(1, int(im.height * scale))
        parts[k] = im.resize((nw, nh), Image.LANCZOS)

    fps = 16
    n = int(LOOP * fps)
    raw_dir = Path("/tmp/aldric-paperdoll")
    if raw_dir.exists():
        shutil.rmtree(raw_dir)
    raw_dir.mkdir()
    frames = []
    for i in range(n):
        t = i / fps
        im = render_pose(parts, parent_w, parent_h, evaluate(t))
        rgb = im.convert("RGB")
        rgb.save(raw_dir / f"f_{i:03d}.png")
        frames.append(rgb)
        print(f"{i+1}/{n}", flush=True)

    walk_a = render_pose(parts, parent_w, parent_h, evaluate(WALK_PERIOD * 0.25))
    walk_b = render_pose(parts, parent_w, parent_h, evaluate(WALK_PERIOD * 0.75))
    strike = render_pose(parts, parent_w, parent_h, evaluate(WALK_BLOCK + ATTACK * 0.46))
    recover = render_pose(parts, parent_w, parent_h, evaluate(WALK_BLOCK + ATTACK * 0.92))

    body_a = body_crop_excluding_ui(walk_a)
    body_b = body_crop_excluding_ui(walk_b)
    body_s = body_crop_excluding_ui(strike)
    d_walk = max_channel_delta(body_a, body_b)
    d_strike = max_channel_delta(body_a, body_s)
    m_walk = mean_channel_delta(body_a, body_b)
    m_strike = mean_channel_delta(body_a, body_s)
    proof = (
        f"max |Δ| excluding UI captions: walk opposite-step {d_walk:.0f}/255 "
        f"(mean {m_walk:.1f}); walk vs strike {d_strike:.0f}/255 (mean {m_strike:.1f}). "
        "Limb/sword pixels move; labels are not the only diff."
    )
    print(proof)
    if d_walk < 40 or d_strike < 40:
        raise SystemExit(f"FAIL: motion too weak for Design re-gate: {proof}")

    # contact still: four keyframes
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
        shutil.copy2(p, ART / p.name)
        print("wrote", p, p.stat().st_size)


if __name__ == "__main__":
    main()
