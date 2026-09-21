#!/usr/bin/env python3
"""Recompose Gate 2 blockout vs Gate 1 turnaround (SoT columns only).

Does NOT remesh or re-render clay. Blockout stills stay the 5de0e16 bytes.
SoT is composited onto light-gray so a transparent rear cutout cannot
become a black void. Mirrors look_targets/03_turnaround_*_LOCKED.png and
03_TURNAROUND_SHEET_LOCKED.png.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/blockout"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
ART = Path("/opt/cursor/artifacts")
STUDIO = (232, 232, 230)


def font(size: int):
    try:
        return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", size)
    except OSError:
        return ImageFont.load_default()


def load_sot(path: Path) -> Image.Image:
    """RGB on light-gray studio. RGBA transparency must not become black."""
    im = Image.open(path)
    if im.mode in ("RGBA", "LA") or (im.mode == "P" and "transparency" in im.info):
        rgba = im.convert("RGBA")
        bg = Image.new("RGB", rgba.size, STUDIO)
        bg.paste(rgba, mask=rgba.split()[-1])
        return bg
    return im.convert("RGB")


def fit(im: Image.Image, box: tuple[int, int], bg=STUDIO) -> Image.Image:
    cell = Image.new("RGB", box, bg)
    scale = min((box[0] - 16) / im.width, (box[1] - 16) / im.height)
    nw, nh = max(1, int(im.width * scale)), max(1, int(im.height * scale))
    placed = im.resize((nw, nh), Image.LANCZOS)
    cell.paste(placed, ((box[0] - nw) // 2, (box[1] - nh) // 2))
    return cell


def rebuild_gate_sheet(panels: list[Image.Image], labels: list[str]) -> Image.Image:
    ch, cw = 820, 360
    sheet = Image.new("RGB", (cw * len(panels), ch + 90), STUDIO)
    d = ImageDraw.Draw(sheet)
    d.text(
        (16, 10),
        "Sir Aldric — TURNAROUND Gate 1  (full figure · light gray · scabbard character-RIGHT)",
        fill=(40, 40, 38),
        font=font(18),
    )
    for i, (cell, lab) in enumerate(zip(panels, labels)):
        scale = min((cw - 24) / cell.width, (ch - 24) / cell.height)
        nw, nh = max(1, int(cell.width * scale)), max(1, int(cell.height * scale))
        placed = cell.resize((nw, nh), Image.LANCZOS)
        sheet.paste(placed, (i * cw + (cw - nw) // 2, 48 + (ch - 24 - nh) // 2))
        d.text((i * cw + 16, ch + 56), lab, fill=(40, 40, 38), font=font(16))
    return sheet


def main() -> None:
    pairs = [
        ("01_FRONT.png", "blockout_front.png", "FRONT", "sot_front.png"),
        ("02_SIDE_R.png", "blockout_side_r.png", "SIDE R", "sot_side_r.png"),
        ("03_BACK.png", "blockout_back.png", "BACK", "sot_back.png"),
        ("04_THREE_QUARTER.png", "blockout_three_quarter.png", "THREE-QUARTER", "sot_three_quarter.png"),
    ]
    sot_rgb = []
    for sot_name, _blk, _label, _col in pairs:
        sot_rgb.append(load_sot(GATE / sot_name))

    # Per-view SoT columns (light-gray, full figure) + look_targets mirrors
    LOOK.mkdir(parents=True, exist_ok=True)
    mirror = {
        "01_FRONT.png": "03_turnaround_front_LOCKED.png",
        "02_SIDE_R.png": "03_turnaround_side_r_LOCKED.png",
        "03_BACK.png": "03_turnaround_back_LOCKED.png",
    }
    for (sot_name, _blk, _label, col_name), rgb in zip(pairs, sot_rgb):
        rgb.save(PROOF / col_name, optimize=True)
        dest = GATE / sot_name
        # Keep Design bytes if they are already RGB studio; rewrite only when
        # the on-disk file had alpha that would black-void the overlay.
        raw = Image.open(dest)
        if raw.mode in ("RGBA", "LA") or (raw.mode == "P" and "transparency" in raw.info):
            rgb.save(dest, optimize=True)
        key = mirror.get(sot_name)
        if key:
            rgb.save(LOOK / key, optimize=True)

    sheet4 = rebuild_gate_sheet(
        sot_rgb,
        ["01_FRONT", "02_SIDE_R", "03_BACK", "04_THREE_QUARTER"],
    )
    sheet4.save(GATE / "TURNAROUND_SHEET.png", optimize=True)
    sheet4.save(LOOK / "03_turnaround_sheet_LOCKED.png", optimize=True)
    sheet4.save(LOOK / "03_TURNAROUND_SHEET_LOCKED.png", optimize=True)

    cw, ch = 420, 780
    sheet = Image.new("RGB", (cw * 2, ch * len(pairs) + 120), (24, 24, 22))
    d = ImageDraw.Draw(sheet)
    title = font(26)
    lab = font(18)
    d.text((24, 18), "GATE 2 BLOCKOUT  ·  vs  GATE 1 TURNAROUND", fill=(236, 230, 210), font=title)
    d.text(
        (24, 54),
        "Silhouette / proportions only  ·  look + motion NOT claimed  ·  clay UNCHANGED 5de0e16",
        fill=(210, 176, 82),
        font=lab,
    )
    d.text(
        (24, 80),
        "SoT = full figure on light gray (no black void)  ·  scabbard character-RIGHT",
        fill=(180, 180, 176),
        font=lab,
    )
    for i, ((_sot_name, blk_name, label, col_name), rgb) in enumerate(zip(pairs, sot_rgb)):
        y = 110 + i * ch
        sot = fit(rgb, (cw, ch), bg=STUDIO)
        blk = fit(Image.open(PROOF / blk_name).convert("RGB"), (cw, ch), bg=(214, 214, 212))
        sheet.paste(sot, (0, y))
        sheet.paste(blk, (cw, y))
        d.text((16, y + 10), f"SoT  {label}", fill=(40, 40, 38), font=lab)
        d.text((cw + 16, y + 10), f"BLOCKOUT  {label}", fill=(40, 40, 38), font=lab)
        sot.save(PROOF / col_name, optimize=True)

    out = PROOF / "blockout_vs_turnaround_sheet.png"
    sheet.save(out, optimize=True)
    ART.mkdir(parents=True, exist_ok=True)
    sheet.save(ART / "aldric_gate2_v3_blockout_vs_turnaround.png")
    for name in ("blockout_front.png", "blockout_back.png", "blockout_side_r.png", "blockout_three_quarter.png"):
        (ART / f"aldric_gate2_v3_{name}").write_bytes((PROOF / name).read_bytes())
    for name in ("sot_front.png", "sot_side_r.png", "sot_back.png", "sot_three_quarter.png"):
        src = PROOF / name
        if src.exists():
            (ART / f"aldric_gate2_v3_{name}").write_bytes(src.read_bytes())
    print("sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
