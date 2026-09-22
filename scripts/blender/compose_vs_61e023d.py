#!/usr/bin/env python3
"""Side-by-side 61e023d FAIL vs this bind iterate. Bind NOT claimed."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
FAIL = PROOF / "fail_61e023d"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def cap(im: Image.Image, text: str):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, 70), fill=(16, 16, 14))
    d.text((20, 20), text, fill=(236, 230, 210), font=font(22))
    return im


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    pairs = [
        ("contact_l", "CONTACT L"),
        ("pass_l", "PASS L"),
        ("mid_swing", "MID-SWING  (61e MP4 FAIL frame)"),
        ("contact_r", "CONTACT R"),
    ]
    cells = []
    for key, label in pairs:
        fail_p = FAIL / f"world_walk_{key}.png"
        now_p = WALK / f"world_walk_{key}.png"
        if not fail_p.exists() or not now_p.exists():
            print("skip missing", fail_p.exists(), now_p.exists(), key)
            continue
        fail = Image.open(fail_p).convert("RGB")
        now = Image.open(now_p).convert("RGB")
        tw = 540
        th = int(fail.height * tw / fail.width)
        fail = cap(fail.resize((tw, th), Image.LANCZOS), f"61e023d FAIL  |  {label}")
        now = cap(now.resize((tw, th), Image.LANCZOS), f"arm-islands  |  {label}  |  bind NOT claimed")
        cells.append((fail, now))

    w, h = cells[0][0].size
    sheet = Image.new("RGB", (w * 2, 120 + h * len(cells)), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((20, 16), "GATE 3  |  61e023d FAIL MP4  vs  detached hang-arm volumes", fill=(236, 230, 210), font=font(26))
    d.text(
        (20, 56),
        "LEFT = remesh+heat/split (Design retracted bind PASS).  RIGHT = arms cut off body + exclusive weights.  Bind NOT claimed.",
        fill=(180, 176, 160),
        font=font(17),
    )
    d.text((20, 86), "Hard checks: solid hang arms  ·  one scabbard character-RIGHT  ·  no mid/leg tear bands", fill=(180, 176, 160), font=font(17))
    for i, (fail, now) in enumerate(cells):
        sheet.paste(fail, (0, 120 + i * h))
        sheet.paste(now, (w, 120 + i * h))
    out = PROOF / "gate3_path2_vs_61e023d.png"
    sheet.save(out, optimize=True)
    (ART / out.name).write_bytes(out.read_bytes())
    print("vs-61e", out, out.stat().st_size)


if __name__ == "__main__":
    main()
