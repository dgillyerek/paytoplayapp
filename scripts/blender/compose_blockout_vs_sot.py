#!/usr/bin/env python3
"""Side-by-side Gate 2 blockout vs Gate 1 turnaround (silhouette only)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/blockout"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    try:
        return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", size)
    except OSError:
        return ImageFont.load_default()


def fit(im: Image.Image, box: tuple[int, int], bg=(232, 232, 230)) -> Image.Image:
    im = im.convert("RGB")
    cell = Image.new("RGB", box, bg)
    scale = min((box[0] - 16) / im.width, (box[1] - 16) / im.height)
    nw, nh = max(1, int(im.width * scale)), max(1, int(im.height * scale))
    placed = im.resize((nw, nh), Image.LANCZOS)
    cell.paste(placed, ((box[0] - nw) // 2, (box[1] - nh) // 2))
    return cell


def main() -> None:
    pairs = [
        ("01_FRONT.png", "blockout_front.png", "FRONT"),
        ("02_SIDE_R.png", "blockout_side_r.png", "SIDE R"),
        ("03_BACK.png", "blockout_back.png", "BACK"),
        ("04_THREE_QUARTER.png", "blockout_three_quarter.png", "THREE-QUARTER"),
    ]
    cw, ch = 420, 780
    sheet = Image.new("RGB", (cw * 2, ch * len(pairs) + 120), (24, 24, 22))
    d = ImageDraw.Draw(sheet)
    title = font(26)
    lab = font(18)
    d.text((24, 18), "GATE 2 BLOCKOUT  ·  vs  GATE 1 TURNAROUND", fill=(236, 230, 210), font=title)
    d.text(
        (24, 54),
        "Silhouette / proportions only  ·  look + motion NOT claimed  ·  grey clay",
        fill=(210, 176, 82),
        font=lab,
    )
    d.text(
        (24, 80),
        "Scabbard character-RIGHT  ·  plate / surcoat / lion-space volumes  ·  not a mannequin",
        fill=(180, 180, 176),
        font=lab,
    )
    for i, (sot_name, blk_name, label) in enumerate(pairs):
        y = 110 + i * ch
        sot = fit(Image.open(GATE / sot_name), (cw, ch))
        blk = fit(Image.open(PROOF / blk_name), (cw, ch), bg=(214, 214, 212))
        sheet.paste(sot, (0, y))
        sheet.paste(blk, (cw, y))
        d.text((16, y + 10), f"SoT  {label}", fill=(40, 40, 38), font=lab)
        d.text((cw + 16, y + 10), f"BLOCKOUT  {label}", fill=(40, 40, 38), font=lab)
    out = PROOF / "blockout_vs_turnaround_sheet.png"
    sheet.save(out, optimize=True)
    ART.mkdir(parents=True, exist_ok=True)
    dest = ART / "aldric_gate2_v2_blockout_vs_turnaround.png"
    sheet.save(dest)
    for name in ("blockout_front.png", "blockout_back.png", "blockout_side_r.png", "blockout_three_quarter.png"):
        (ART / f"aldric_gate2_v2_{name}").write_bytes((PROOF / name).read_bytes())
    print("sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
