#!/usr/bin/env python3
"""FAIL (8babba7) vs this paint iterate — UV seams + lion only.

Proof sheet for Design re-eye. Does NOT claim look PASS.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
FAIL = Path("/tmp/path2-fail-8babba7")
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
ART = Path("/opt/cursor/artifacts")
DARK = (24, 24, 22)


def font(size: int):
    try:
        return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", size)
    except OSError:
        return ImageFont.load_default()


def load(path: Path) -> Image.Image:
    return Image.open(path).convert("RGB")


def crop(im: Image.Image, box) -> Image.Image:
    return im.crop(box)


def fit(im: Image.Image, box, bg) -> Image.Image:
    cell = Image.new("RGB", box, bg)
    scale = min((box[0] - 12) / im.width, (box[1] - 12) / im.height)
    nw, nh = max(1, int(im.width * scale)), max(1, int(im.height * scale))
    placed = im.resize((nw, nh), Image.LANCZOS)
    cell.paste(placed, ((box[0] - nw) // 2, (box[1] - nh) // 2))
    return cell


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    fail_r = load(FAIL / "world_rear.png")
    new_r = load(PROOF / "world_rear.png")
    fail_f = load(FAIL / "world_front.png")
    new_f = load(PROOF / "world_front.png")
    sot_r = load(LOOK / "01_rear_LOCKED.png")
    sot_f = load(GATE / "01_FRONT.png")

    # 1080×1920 stills: lion upper-mid, neck/shoulders above.
    # FAIL stills have a 64px caption bar; raw new stills do not.
    rows = [
        (
            crop(fail_r, (300, 500, 780, 950)),
            crop(new_r, (300, 500, 780, 950)),
            crop(sot_r, (360, 200, 660, 500)),
            "REAR LION  ·  8babba7 smear  |  this iterate  |  SoT 01_rear_LOCKED",
        ),
        (
            crop(fail_f, (300, 500, 780, 920)),
            crop(new_f, (300, 500, 780, 920)),
            crop(sot_f, (240, 270, 530, 520)),
            "FRONT LION  ·  8babba7 smear  |  this iterate  |  SoT 01_FRONT",
        ),
        (
            crop(fail_r, (250, 300, 830, 560)),
            crop(new_r, (250, 300, 830, 560)),
            crop(sot_r, (200, 80, 820, 280)),
            "NECK / SHOULDERS  ·  UV crack lines  |  bleed iterate  |  SoT",
        ),
    ]

    cw, ch = 320, 420
    header = 130
    sheet = Image.new("RGB", (cw * 3, header + ch * len(rows)), DARK)
    d = ImageDraw.Draw(sheet)
    d.text((20, 14), "GATE 3  ·  PATH 2 paint iterate  ·  UV + lion delta vs 8babba7", fill=(236, 230, 210), font=font(22))
    d.text((20, 48), "Look NOT claimed. Motion / Play hub HOLD. Proof only: seams + heraldry vs FAIL stills.", fill=(210, 176, 82), font=font(15))
    d.text((20, 76), "Cols: FAIL 8babba7  ·  this iterate (UV bleed + SoT lion cards)  ·  locked SoT", fill=(170, 170, 166), font=font(14))
    d.text((20, 100), "Honest: neck groove / leftover gold specks remain. Hang not applied (A-pose).", fill=(160, 160, 156), font=font(14))
    y0 = header
    for a, b, c, title in rows:
        d.text((16, y0 + 6), title, fill=(236, 230, 210), font=font(13))
        sheet.paste(fit(a, (cw, ch - 28), DARK), (0, y0 + 28))
        sheet.paste(fit(b, (cw, ch - 28), (18, 18, 16)), (cw, y0 + 28))
        sheet.paste(fit(c, (cw, ch - 28), (18, 18, 16)), (cw * 2, y0 + 28))
        y0 += ch
    out = PROOF / "gate3_uv_lion_delta.png"
    sheet.save(out, optimize=True)
    sheet.save(ART / "gate3_uv_lion_delta.png", optimize=True)
    print("delta", out, out.stat().st_size)


if __name__ == "__main__":
    main()
