#!/usr/bin/env python3
"""Side-by-side 76eab9d FAIL MP4 frames vs this stubby-L hang-arm iterate."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
FAIL = PROOF / "fail_76eab9d"
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
    frames = [(0, "n=0"), (5, "n=5"), (10, "n=10 mid-swing"), (15, "n=15")]
    cells = []
    for n, label in frames:
        fail_p = FAIL / f"n_{n}.png"
        now_p = WALK / f"census_cycle_mp4_n{n:02d}.png"
        if not now_p.exists():
            now_p = WALK / f"world_walk_n{n:02d}.png"
        if not fail_p.exists() or not now_p.exists():
            print("skip", n, fail_p.exists(), now_p.exists())
            continue
        fail = cap(
            Image.open(fail_p).convert("RGB").resize((400, 711), Image.LANCZOS),
            f"76eab9d FAIL MP4  |  {label}",
        )
        now = cap(
            Image.open(now_p).convert("RGB").resize((400, 711), Image.LANCZOS),
            f"this MP4  |  {label}  |  bind NOT claimed",
        )
        cells.append((fail, now))
    if not cells:
        raise SystemExit("no vs-76eab9d pairs")
    w, h = cells[0][0].size
    sheet = Image.new("RGB", (w * 2, 110 + h * len(cells)), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text(
        (16, 12),
        "GATE 3  |  76eab9d FAIL MP4 frames  vs  this walk MP4  |  bind NOT claimed",
        fill=(236, 230, 210),
        font=font(22),
    )
    d.text(
        (16, 48),
        "LEFT = 76eab9d FAIL (triple-arm ghost).  RIGHT = ONE loft/side.  Look path e5b132f.",
        fill=(180, 176, 160),
        font=font(15),
    )
    d.text(
        (16, 74),
        "Both columns are MP4 frames. Eye n=0 and n=15 — not one mid-swing still.",
        fill=(180, 176, 160),
        font=font(15),
    )
    for i, (fail, now) in enumerate(cells):
        sheet.paste(fail, (0, 110 + i * h))
        sheet.paste(now, (w, 110 + i * h))
    out = PROOF / "gate3_path2_vs_76eab9d.png"
    sheet.save(out, optimize=True)
    (ART / out.name).write_bytes(out.read_bytes())
    print("vs-76eab9d", out, out.stat().st_size)


if __name__ == "__main__":
    main()
