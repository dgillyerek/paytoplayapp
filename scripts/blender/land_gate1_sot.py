#!/usr/bin/env python3
"""Land Gate 1 LOCKED SoT folders for the Aldric blockout.

Crops front / side_R from look_targets/02 (same knight as Gate 1 sheet).
Back SoT is 01_rear_LOCKED (scabbard = character-RIGHT). 02 rear is ignored
when it mirrors. Portrait binaries were specified this turn; land if present.
"""
from __future__ import annotations

import shutil
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"


def _fig_mask(rgb: np.ndarray) -> np.ndarray:
    a = rgb.astype(np.int16)
    sat = a.max(2) - a.min(2)
    val = a.mean(2)
    return (sat > 12) | (val < 170)


def crop_panel(im: Image.Image, x0: int, x1: int, pad: int = 10) -> Image.Image:
    rgb = np.array(im.convert("RGB"))
    mask = _fig_mask(rgb[:, x0:x1])
    ys, xs = np.where(mask)
    xa, xb = x0 + int(xs.min()) - pad, x0 + int(xs.max()) + pad
    ya, yb = int(ys.min()) - pad, int(ys.max()) + pad
    xa, ya = max(0, xa), max(0, ya)
    xb, yb = min(im.width, xb), min(im.height, yb)
    return im.crop((xa, ya, xb, yb))


def crop_alpha(im: Image.Image, pad: int = 8) -> Image.Image:
    a = np.array(im.convert("RGBA"))
    ys, xs = np.where(a[:, :, 3] > 20)
    xa, xb = max(0, int(xs.min()) - pad), min(im.width, int(xs.max()) + pad)
    ya, yb = max(0, int(ys.min()) - pad), min(im.height, int(ys.max()) + pad)
    return im.crop((xa, ya, xb, yb))


def main() -> None:
    GATE.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(LOOK / "02_aldric_turnaround_orthos.png")
    w, _h = sheet.size
    front = crop_panel(sheet, 0, w // 4)
    side_r = crop_panel(sheet, 2 * w // 4, 3 * w // 4)
    back = crop_alpha(Image.open(LOOK / "01_rear_LOCKED.png"))

    front.save(GATE / "01_FRONT.png")
    side_r.save(GATE / "02_SIDE_R.png")
    back.save(GATE / "03_BACK.png")
    # 3/4 panel lives on the Gate 1 sheet; 00_fullbody is the closest in-repo stand-in
    # until Design drops 04_THREE_QUARTER.png. Do not invent a new 3/4.
    threeq_src = GATE / "04_THREE_QUARTER.png"
    if not threeq_src.exists():
        crop_alpha(Image.open(LOOK / "00_fullbody_LOCKED.png")).save(GATE / "04_THREE_QUARTER.png")

    front.save(LOOK / "03_turnaround_front_LOCKED.png")
    side_r.save(LOOK / "03_turnaround_side_r_LOCKED.png")
    back.save(LOOK / "03_turnaround_back_LOCKED.png")

    # Rebuild a working sheet from locked panels (02 rear scabbard is not SoT).
    cells = [front, side_r, back]
    labels = ["01_FRONT", "02_SIDE_R", "03_BACK"]
    if (GATE / "04_THREE_QUARTER.png").exists():
        cells.append(Image.open(GATE / "04_THREE_QUARTER.png"))
        labels.append("04_THREE_QUARTER")
    ch = 820
    cw = 360
    sheet_out = Image.new("RGB", (cw * len(cells), ch + 70), (236, 236, 234))
    for i, (cell, lab) in enumerate(zip(cells, labels)):
        cell = cell.convert("RGB")
        scale = min((cw - 24) / cell.width, (ch - 24) / cell.height)
        nw, nh = max(1, int(cell.width * scale)), max(1, int(cell.height * scale))
        placed = cell.resize((nw, nh), Image.LANCZOS)
        sheet_out.paste(placed, (i * cw + (cw - nw) // 2, 48 + (ch - 24 - nh) // 2))
    if not (GATE / "TURNAROUND_SHEET.png").exists():
        sheet_out.save(GATE / "TURNAROUND_SHEET.png")
    sheet_out.save(LOOK / "03_turnaround_sheet_LOCKED.png")

    (GATE / "README.md").write_text(
        "# Sir Aldric — TURNAROUND Gate 1 LOCKED\n\n"
        "Look law is **LOCKED**. Do not redesign.\n\n"
        "| File | Role |\n"
        "| --- | --- |\n"
        "| `01_FRONT.png` | Ortho front silhouette / proportions |\n"
        "| `02_SIDE_R.png` | Character-right profile |\n"
        "| `03_BACK.png` | Ortho back — scabbard **character-RIGHT** (from `01_rear_LOCKED`) |\n"
        "| `04_THREE_QUARTER.png` | ¾ (use Design drop when present; else 00_fullbody stand-in) |\n"
        "| `05_PORTRAIT_COMPLETE_fullbody.png` | Face + ornate armor; oval bust = **face law** |\n"
        "| `TURNAROUND_SHEET.png` | Four-up Gate 1 sheet |\n\n"
        "Hard locks: scabbard character-RIGHT; silver/gold plate volumes; "
        "royal-blue surcoat volume; lion space on the back; proportions match this turnaround.\n"
        "Blockout is helmeted (turnaround). Portrait is face law — not a helmet redesign.\n"
    )
    readme = LOOK / "README.md"
    extra = (
        "\n## Gate 1 LOCKED (2026-09-21)\n\n"
        "- `00_portrait_COMPLETE_LOCKED.png` — face law (oval bust). Land Design bytes when present.\n"
        "- `03_turnaround_front_LOCKED.png` / `03_turnaround_side_r_LOCKED.png` / "
        "`03_turnaround_back_LOCKED.png` — mirrors of Gate 1 turnaround.\n"
        "- `03_turnaround_sheet_LOCKED.png` — working four-up. **02 rear is not SoT** if mirrored.\n"
        "- Scabbard SoT remains **character-right**.\n"
    )
    text = readme.read_text()
    if "Gate 1 LOCKED (2026-09-21)" not in text:
        readme.write_text(text.rstrip() + extra)
    print("gate1", GATE)
    for p in sorted(GATE.glob("*")):
        print(" ", p.name, p.stat().st_size if p.is_file() else "dir")


if __name__ == "__main__":
    main()
