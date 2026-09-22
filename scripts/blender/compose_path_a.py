#!/usr/bin/env python3
"""Path A re-gate: World stills vs e5b132f. Bind NOT claimed.

Walk/bind row is omitted unless mid-walk frames pass a shred check.
Never paste the exploded Path-2 chrome strip again.
"""
from __future__ import annotations

import os
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
LOOK_E5 = PROOF / "look_e5b132f"
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
WALK = PROOF / "walk"
ART = Path("/opt/cursor/artifacts")
DROP = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/PATH_A_RETOPO"


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def cap(im: Image.Image, text: str, h=52):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, h), fill=(16, 16, 14))
    d.text((10, 16), text, fill=(236, 230, 210), font=font(14))
    return im


def load(p: Path) -> Image.Image:
    im = Image.open(p)
    if im.mode != "RGB":
        im = im.convert("RGB")
    return im


def walk_frame_clean(path: Path) -> tuple[bool, str]:
    """Refuse exploded chrome / blob frames. Knight must stay tall + navy."""
    arr = np.array(load(path))
    h, w = arr.shape[:2]
    r, g, b = arr[:, :, 0].astype(np.int16), arr[:, :, 1].astype(np.int16), arr[:, :, 2].astype(np.int16)
    # Two-tone gray studio bg — foreground is anything off that band.
    bg = (np.abs(r.astype(np.int16) - g) < 18) & (np.abs(g - b) < 18) & (r >= 28) & (r <= 95)
    fg = ~bg
    if int(fg.sum()) < 8000:
        return False, f"too-empty fg={int(fg.sum())}"
    ys, xs = np.where(fg)
    fh = int(ys.max() - ys.min()) + 1
    fw = int(xs.max() - xs.min()) + 1
    aspect = fw / max(1, fh)
    width_frac = fw / w
    navy = (b > r + 12) & (r < 110) & (b > 45) & fg
    navy_n = int(navy.sum())
    # Exploded Path-2 remesh filled the frame and lost the navy tabard.
    if aspect > 0.72 or width_frac > 0.78:
        return False, f"blob aspect={aspect:.2f} width_frac={width_frac:.2f}"
    if navy_n < 2500:
        return False, f"navy-missing n={navy_n}"
    if fh / h < 0.42:
        return False, f"short fh={fh}/{h}"
    return True, f"ok aspect={aspect:.2f} navy={navy_n}"


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    tw, th = 320, 568
    pairs = [
        (LOOK_E5 / "world_rear.png", PROOF / "world_rear.png", "rear Play", "e5b132f", "Path A"),
        (LOOK_E5 / "world_front.png", PROOF / "world_front.png", "front", "e5b132f", "Path A"),
        (LOOK_E5 / "world_side_r.png", PROOF / "world_side_r.png", "side R", "e5b132f", "Path A"),
        (LOOK_E5 / "world_34_front.png", PROOF / "world_34_front.png", "front ¾", "e5b132f", "Path A"),
        (LOOK / "01_rear_LOCKED.png", PROOF / "world_rear.png", "rear vs SoT", "01_rear_LOCKED", "Path A"),
    ]
    cells = []
    for a, b, lab, al, bl in pairs:
        if not a.exists() or not b.exists():
            print("skip", lab, a.exists(), b.exists())
            continue
        cells.append((
            cap(load(a).resize((tw, th), Image.LANCZOS), f"{al}  |  {lab}"),
            cap(load(b).resize((tw, th), Image.LANCZOS), f"{bl}  |  {lab}"),
        ))
    walk_row = []
    want_walk = os.environ.get("PATHA_WALK_ROW") == "1"
    for name, lab in (
        ("world_walk_contact_l.png", "contact L"),
        ("world_walk_contact_r.png", "contact R"),
        ("world_walk_pass_l.png", "pass L"),
        ("world_walk_n10.png", "n=10"),
    ):
        p = WALK / name
        if not p.exists() or not want_walk:
            continue
        ok, reason = walk_frame_clean(p)
        print("walk frame", name, "clean" if ok else "SHRED", reason)
        if ok:
            walk_row.append(cap(load(p).resize((tw, th), Image.LANCZOS), f"walk  |  {lab}  |  bind NOT claimed"))
        else:
            walk_row = []
            break
    if not cells:
        raise SystemExit("no Path A still pairs")
    w, h = cells[0][0].size
    rows = len(cells) + (1 if walk_row else 0)
    sheet = Image.new("RGB", (w * 2 if not walk_row else max(w * 2, w * max(len(walk_row), 2)), 130 + h * rows), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((14, 10), "PATH A  |  552b099 stills LOCKED  |  bind NOT claimed", fill=(236, 230, 210), font=font(22))
    d.text((14, 44), "Stills look held. Bind/walk for Design re-gate. No shredded bind strip.", fill=(236, 200, 120), font=font(15))
    d.text((14, 72), "LOOK Meshy knight. Hub PNG HOLD. Walk NOT claimed. Do NOT claim Design PASS.", fill=(180, 176, 160), font=font(14))
    d.text((14, 100), "LEFT = locked e5b132f / SoT.  RIGHT = this Path A World-cam.", fill=(180, 176, 160), font=font(14))
    for i, (a, b) in enumerate(cells):
        sheet.paste(a, (0, 130 + i * h))
        sheet.paste(b, (w, 130 + i * h))
    if walk_row:
        y = 130 + len(cells) * h
        for i, im in enumerate(walk_row[:4]):
            sheet.paste(im, (i * w, y))
    out = PROOF / "gate3_path_a_vs_e5b132f.png"
    sheet.save(out, optimize=True)
    try:
        (ART / out.name).write_bytes(out.read_bytes())
    except OSError as exc:
        print("artifact skip", exc)
    DROP.mkdir(parents=True, exist_ok=True)
    (DROP / out.name).write_bytes(out.read_bytes())
    print("path A sheet", out, out.stat().st_size, "walk_row", bool(walk_row))


if __name__ == "__main__":
    main()
