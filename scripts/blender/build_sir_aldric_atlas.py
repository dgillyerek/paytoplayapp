#!/usr/bin/env python3
"""Look-targets atlas for Sir Aldric Gate 3 iterate.

Plate tiles carry gold-edge / lame grooves so 1080 World-cam does not read as
flat white cones. Lion + Greek-key cropped from 01_rear_LOCKED (less aggressive
clean so embroidery grain survives).
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"

# Sampled from 01_rear_LOCKED plate (not the washed 218-white).
SILVER = (176, 170, 162)
SILVER_LO = (128, 124, 118)
SILVER_HI = (214, 210, 202)
GOLD = (184, 152, 86)
GOLD_HI = (224, 196, 120)
GOLD_LO = (132, 102, 52)
BLUE = (42, 70, 138)
BROWN = (85, 61, 38)
CREAM = (228, 216, 186)


def _fill_swatch(draw, box, rgb):
    draw.rectangle(box, fill=rgb)
    x0, y0, x1, y1 = box
    hi = tuple(min(255, c + 24) for c in rgb)
    lo = tuple(max(0, c - 32) for c in rgb)
    draw.rectangle((x0, y0, x1, y0 + 8), fill=hi)
    draw.rectangle((x0, y1 - 8, x1, y1), fill=lo)
    cx = (x0 + x1) // 2
    cy = (y0 + y1) // 2
    for dx, dy in ((-36, -28), (36, -28), (-36, 28), (36, 28), (0, 0)):
        draw.ellipse((cx + dx - 3, cy + dy - 3, cx + dx + 3, cy + dy + 3), fill=hi)


def _plate_tile(draw, box):
    """Lame grooves + rivets + C-scroll filigree — readable gold at phone res."""
    x0, y0, x1, y1 = box
    draw.rectangle(box, fill=SILVER)
    h = y1 - y0
    step = max(10, h // 8)
    for i, y in enumerate(range(y0, y1, step)):
        if i % 2 == 0:
            draw.rectangle((x0, y, x1, min(y1, y + 3)), fill=GOLD)
            draw.rectangle((x0, y + 3, x1, min(y1, y + 5)), fill=GOLD_LO)
        else:
            draw.rectangle((x0, y, x1, min(y1, y + 2)), fill=SILVER_LO)
        for x in range(x0 + 18, x1 - 8, 36):
            draw.ellipse((x - 2, y + 6, x + 2, y + 10), fill=SILVER_HI)
    for y in range(y0 + 22, y1 - 18, 36):
        for x in range(x0 + 28, x1 - 24, 52):
            draw.arc((x - 14, y - 10, x + 14, y + 12), 20, 160, fill=GOLD, width=2)
            draw.arc((x + 2, y - 8, x + 28, y + 14), 200, 340, fill=GOLD_HI, width=2)


def _gold_trim_tile(draw, box):
    x0, y0, x1, y1 = box
    draw.rectangle(box, fill=GOLD)
    draw.rectangle((x0, y0, x1, y0 + 6), fill=GOLD_HI)
    draw.rectangle((x0, y1 - 6, x1, y1), fill=GOLD_LO)
    for y in range(y0 + 12, y1 - 8, 14):
        draw.rectangle((x0, y, x1, y + 3), fill=GOLD_HI)


def _clean_lion(src, box):
    """Gold lion only on exact surcoat BLUE — no navy plaque rectangle."""
    crop = src.crop(box).convert("RGBA")
    arr = np.array(crop)
    r, g, b, a = [arr[:, :, i] for i in range(4)]
    gold = (r > 118) & (g > 88) & (r > b + 10) & (a > 16)
    # 1px dilate so the stitch doesn't hole-punch.
    gold = gold | np.pad(gold, ((1, 0), (0, 0)), mode="constant")[:-1, :]
    gold = gold | np.pad(gold, ((0, 1), (0, 0)), mode="constant")[1:, :]
    gold = gold | np.pad(gold, ((0, 0), (1, 0)), mode="constant")[:, :-1]
    gold = gold | np.pad(gold, ((0, 0), (0, 1)), mode="constant")[:, 1:]
    out = np.empty_like(arr)
    out[:] = (*BLUE, 255)
    out[gold] = arr[gold]
    out[..., 3] = 255
    return Image.fromarray(out, "RGBA")


def _draw_greek_key(img, box):
    """Chunky meander so it reads at 1080 from Play World-cam, plus 01_rear hem."""
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    tile = Image.new("RGB", (w, h), CREAM)
    d = ImageDraw.Draw(tile)
    step = 36
    thick = 7
    y_mid = h // 2
    for x in range(-step, w + step, step):
        d.rectangle((x + 2, y_mid - 14, x + 2 + thick, y_mid + 14), fill=GOLD)
        d.rectangle((x + 2, y_mid - 14, x + 22, y_mid - 14 + thick), fill=GOLD)
        d.rectangle((x + 22 - thick, y_mid - 14, x + 22, y_mid + 6), fill=GOLD)
        d.rectangle((x + 10, y_mid + 6 - thick, x + 22, y_mid + 6), fill=GOLD)
        d.rectangle((x + 10, y_mid - 2, x + 10 + thick, y_mid + 14), fill=GOLD)
        d.rectangle((x + 10, y_mid + 14 - thick, x + 32, y_mid + 14), fill=GOLD)
    # Gold rails
    d.rectangle((0, 4, w, 10), fill=GOLD)
    d.rectangle((0, h - 10, w, h - 4), fill=GOLD)
    rear = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
    hem = _clean_lion(rear, (340, 610, 700, 712))
    hem = hem.resize((w, h), Image.LANCZOS)
    ha = np.array(hem)
    gold = (ha[:, :, 0] > 120) & (ha[:, :, 1] > 90) & (ha[:, :, 0] > ha[:, :, 2] + 12)
    base = np.array(tile)
    base[gold] = ha[gold][:, :3]
    img.paste(Image.fromarray(base, "RGB"), (x0, y0))


def build_atlas():
    img = Image.new("RGB", (1024, 1024), (18, 20, 22))
    d = ImageDraw.Draw(img)
    _fill_swatch(d, (16, 16, 240, 240), SILVER)
    _fill_swatch(d, (272, 16, 496, 240), GOLD)
    _fill_swatch(d, (528, 16, 752, 240), BLUE)
    # Flat interior so UV_BLUE matches lion-panel cloth (no highlight plaque).
    d.rectangle((536, 28, 744, 228), fill=BLUE)
    _fill_swatch(d, (784, 16, 1008, 240), BROWN)
    rear = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
    # Tight rampant only (cut the belt bar). Keep cloth grain.
    lion = _clean_lion(rear, (430, 248, 604, 424))
    lion = lion.resize((460, 640), Image.LANCZOS)
    lion = ImageEnhance.Contrast(lion).enhance(1.22)
    lion = lion.filter(ImageFilter.UnsharpMask(radius=1.6, percent=140, threshold=2))
    la = np.array(lion.convert("RGB"))
    gold = (la[:, :, 0] > 118) & (la[:, :, 1] > 88) & (la[:, :, 0] > la[:, :, 2] + 10)
    la[~gold] = BLUE
    h, w = la.shape[:2]
    la[: int(0.10 * h), :] = BLUE
    la[int(0.92 * h) :, :] = BLUE
    la[:, : int(0.08 * w)] = BLUE
    la[:, int(0.92 * w) :] = BLUE
    img.paste(Image.fromarray(la, "RGB"), (24, 286))
    _draw_greek_key(img, (532, 224, 1000, 400))
    _plate_tile(d, (528, 420, 1000, 700))
    _gold_trim_tile(d, (528, 716, 1000, 1000))
    return img


def main():
    PACK3D.mkdir(parents=True, exist_ok=True)
    atlas = build_atlas()
    path = PACK3D / "sir_aldric_atlas.png"
    atlas.save(path, optimize=True)
    print("atlas", path, path.stat().st_size)


if __name__ == "__main__":
    main()
