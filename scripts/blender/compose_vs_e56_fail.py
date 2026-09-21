#!/usr/bin/env python3
"""Side-by-side e56aeb1 FAIL vs this remesh tip. Walk-with-look NOT claimed."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
FAIL = PROOF / "fail_e56aeb1"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def cap(im: Image.Image, text: str, fill=(236, 230, 210)):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, 70), fill=(16, 16, 14))
    d.text((20, 20), text, fill=fill, font=font(22))
    return im


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    pairs = [
        ("contact_l", "CONTACT L"),
        ("pass_l", "PASS L"),
        ("contact_r", "CONTACT R"),
        ("pass_r", "PASS R"),
    ]
    cells = []
    for key, label in pairs:
        fail = Image.open(FAIL / f"world_walk_{key}.png").convert("RGB")
        now = Image.open(WALK / f"world_walk_{key}.png").convert("RGB")
        # half-height so the 2x4 sheet stays readable
        tw = 540
        th = int(fail.height * tw / fail.width)
        fail = cap(fail.resize((tw, th), Image.LANCZOS), f"e56aeb1 FAIL  |  {label}")
        now = cap(now.resize((tw, th), Image.LANCZOS), f"retopo+skin  |  {label}  |  walk NOT claimed")
        cells.append((fail, now))

    w, h = cells[0][0].size
    sheet = Image.new("RGB", (w * 2, 120 + h * 4), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((20, 16), "GATE 3  |  e56aeb1 FAIL  vs  retopo+skin FBX", fill=(236, 230, 210), font=font(28))
    d.text(
        (20, 56),
        "LEFT = hang-heat on Meshy shatter (abandoned).  RIGHT = remesh mid-poly + one scabbard + hang skin.  Walk NOT claimed.",
        fill=(180, 176, 160),
        font=font(18),
    )
    d.text((20, 86), "Hard checks: solid hang arms  ·  one scabbard character-RIGHT  ·  no mid/leg tear bands", fill=(180, 176, 160), font=font(18))
    for i, (fail, now) in enumerate(cells):
        sheet.paste(fail, (0, 120 + i * h))
        sheet.paste(now, (w, 120 + i * h))
    out = PROOF / "gate3_path2_vs_e56aeb1.png"
    sheet.save(out, optimize=True)
    (ART / out.name).write_bytes(out.read_bytes())
    print("vs-e56", out, out.stat().st_size)


if __name__ == "__main__":
    main()
