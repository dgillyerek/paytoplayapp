#!/usr/bin/env python3
"""Look-targets atlas for the Blender Sir Aldric mesh (path B).

Crops lion + Greek-key hem from 01_rear_LOCKED (primary Game-view SoT).
Do not invent a new palette — swatches are sampled from the locked still.
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"

# Sampled from 01_rear_LOCKED (not invented primaries).
SILVER = (218, 214, 208)
GOLD = (174, 146, 100)
BLUE = (48, 78, 148)
BROWN = (85, 61, 38)
CREAM = (236, 228, 210)


def _fill_swatch(draw, box, rgb):
    draw.rectangle(box, fill=rgb)
    x0, y0, x1, y1 = box
    hi = tuple(min(255, c + 28) for c in rgb)
    lo = tuple(max(0, c - 28) for c in rgb)
    draw.rectangle((x0, y0, x1, y0 + 10), fill=hi)
    draw.rectangle((x0, y1 - 10, x1, y1), fill=lo)
    # faint plate rivets
    cx = (x0 + x1) // 2
    cy = (y0 + y1) // 2
    for dx, dy in ((-36, -28), (36, -28), (-36, 28), (36, 28), (0, 0)):
        draw.ellipse((cx + dx - 3, cy + dy - 3, cx + dx + 3, cy + dy + 3), fill=hi)


def _clean_on_blue(src, box):
    crop = src.crop(box).convert("RGBA")
    arr = np.array(crop)
    r, g, b, a = [arr[:, :, i] for i in range(4)]
    white = (r > 230) & (g > 230) & (b > 230)
    empty = a < 20
    keep_gold = (r > 130) & (g > 100) & (b < 170) & (r > b + 15)
    keep_blue = (b > 55) & (b > r + 15) & (a > 20)
    keep = keep_gold | keep_blue
    out = arr.copy()
    out[white | empty | ~keep] = (*BLUE, 255)
    out[keep_gold] = arr[keep_gold]
    out[keep_gold, 3] = 255
    out[keep_blue & ~keep_gold] = arr[keep_blue & ~keep_gold]
    out[keep_blue & ~keep_gold, 3] = 255
    return Image.fromarray(out, "RGBA")


def _draw_greek_key(img, box):
    x0, y0, x1, y1 = box
    tile = Image.new("RGB", (x1 - x0, y1 - y0), CREAM)
    d = ImageDraw.Draw(tile)
    w, h = tile.size
    step = 28
    thick = 5
    y_mid = h // 2
    for x in range(-step, w + step, step):
        d.rectangle((x + 2, y_mid - 10, x + 2 + thick, y_mid + 10), fill=GOLD)
        d.rectangle((x + 2, y_mid - 10, x + 18, y_mid - 10 + thick), fill=GOLD)
        d.rectangle((x + 18 - thick, y_mid - 10, x + 18, y_mid + 4), fill=GOLD)
        d.rectangle((x + 8, y_mid + 4 - thick, x + 18, y_mid + 4), fill=GOLD)
        d.rectangle((x + 8, y_mid - 2, x + 8 + thick, y_mid + 10), fill=GOLD)
        d.rectangle((x + 8, y_mid + 10 - thick, x + 26, y_mid + 10), fill=GOLD)
    rear = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
    hem = _clean_on_blue(rear, (352, 618, 692, 700))
    hem = hem.resize((w, h), Image.BILINEAR)
    ha = np.array(hem)
    gold = (ha[:, :, 0] > 130) & (ha[:, :, 1] > 100) & (ha[:, :, 0] > ha[:, :, 2] + 15)
    base = np.array(tile)
    base[gold] = ha[gold][:, :3]
    img.paste(Image.fromarray(base, "RGB"), (x0, y0))


def build_atlas():
    img = Image.new("RGB", (1024, 1024), (20, 22, 24))
    d = ImageDraw.Draw(img)
    _fill_swatch(d, (16, 16, 240, 240), SILVER)
    _fill_swatch(d, (272, 16, 496, 240), GOLD)
    _fill_swatch(d, (528, 16, 752, 240), BLUE)
    _fill_swatch(d, (784, 16, 1008, 240), BROWN)
    rear = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
    lion = _clean_on_blue(rear, (380, 190, 640, 500))
    lion = lion.resize((460, 640), Image.LANCZOS)
    img.paste(lion.convert("RGB"), (24, 286))
    _draw_greek_key(img, (532, 224, 1000, 388))
    _fill_swatch(d, (528, 420, 752, 560), SILVER)
    _fill_swatch(d, (784, 420, 1008, 560), GOLD)
    img = img.filter(ImageFilter.SMOOTH)
    return img


def main():
    PACK3D.mkdir(parents=True, exist_ok=True)
    atlas = build_atlas()
    path = PACK3D / "sir_aldric_atlas.png"
    atlas.save(path, optimize=True)
    print("atlas", path, path.stat().st_size)


if __name__ == "__main__":
    main()
