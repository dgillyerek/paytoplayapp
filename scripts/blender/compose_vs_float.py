#!/usr/bin/env python3
"""2820e53 FAIL float/4-leg vs this planted one-L+one-R walk."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
FAIL = PROOF / "fail_2820e53_float"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def cap(im: Image.Image, text: str):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, 70), fill=(16, 16, 14))
    d.text((16, 20), text, fill=(236, 230, 210), font=font(18))
    return im


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    pairs = [
        ("contact_l", "contact L"),
        ("contact_r", "contact R"),
        ("pass_l", "pass L"),
        ("pass_r", "pass R"),
    ]
    cells = []
    for name, label in pairs:
        fail_p = FAIL / f"{name}.png"
        now_p = WALK / f"world_walk_{name}.png"
        if not fail_p.exists() or not now_p.exists():
            print("skip", name, fail_p.exists(), now_p.exists())
            continue
        fail = cap(
            Image.open(fail_p).convert("RGB").resize((400, 711), Image.LANCZOS),
            f"2820e53 FAIL  |  {label}  |  float + 4-leg",
        )
        now = cap(
            Image.open(now_p).convert("RGB").resize((400, 711), Image.LANCZOS),
            f"this  |  {label}  |  plant + 1L/1R  |  bind NOT claimed",
        )
        cells.append((fail, now))
    if not cells:
        raise SystemExit("no vs-float pairs")
    w, h = cells[0][0].size
    sheet = Image.new("RGB", (w * 2, 110 + h * len(cells)), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((16, 12), "GATE 3  |  2820e53 FAIL float/4-leg  vs  planted 1L+1R  |  bind NOT claimed", fill=(236, 230, 210), font=font(22))
    d.text((16, 48), "LEFT = 2820e53 (Hips paper in greave tube + unplanted root).  RIGHT = delete paper; plant soles on Y=0.", fill=(180, 176, 160), font=font(15))
    d.text((16, 74), "KEEP one-loft + steel texel. LOOK e5b132f. Eye: one L + one R, soles on ground, no hover.", fill=(180, 176, 160), font=font(15))
    for i, (fail, now) in enumerate(cells):
        sheet.paste(fail, (0, 110 + i * h))
        sheet.paste(now, (w, 110 + i * h))
    out = PROOF / "gate3_path2_vs_2820e53_float.png"
    sheet.save(out, optimize=True)
    try:
        (ART / out.name).write_bytes(out.read_bytes())
    except OSError as exc:
        print("artifact copy skip", exc)
    print("vs-float", out, out.stat().st_size)


if __name__ == "__main__":
    main()
