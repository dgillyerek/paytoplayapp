#!/usr/bin/env python3
"""Hang-rest Meshy-look vs e5b132f paint PASS world_rear. Bind NOT claimed."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    frozen = PROOF / "look_e5b132f" / "world_rear.png"
    left = Image.open(frozen if frozen.exists() else PROOF / "world_rear.png").convert("RGB")
    right = Image.open(PROOF / "world_meshy_hang_rest.png").convert("RGB")
    w, h = left.size
    right = right.resize((w, h), Image.LANCZOS)
    f_sm, f_lg = font(22), font(28)
    sheet = Image.new("RGB", (w * 2, h + 110), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((24, 18), "GATE 3  |  Meshy-look hang rest vs e5b132f paint PASS  |  bind NOT claimed", fill=(236, 230, 210), font=f_lg)
    d.text(
        (24, 56),
        "LEFT e5b132f world_rear  |  RIGHT Path A retopo+project  |  no tube/capsule/paper hacks  |  bind NOT claimed",
        fill=(180, 176, 160),
        font=f_sm,
    )
    sheet.paste(left, (0, 110))
    sheet.paste(right, (w, 110))
    cap_l = ImageDraw.Draw(sheet)
    cap_l.rectangle((0, 110, w, 174), fill=(16, 16, 14))
    cap_l.text((24, 128), "e5b132f paint PASS  |  world_rear", fill=(236, 230, 210), font=f_sm)
    cap_l.rectangle((w, 110, w * 2, 174), fill=(16, 16, 14))
    cap_l.text((w + 24, 128), "this tip  |  Meshy hang rest  |  same GLB UVs", fill=(236, 230, 210), font=f_sm)
    out = PROOF / "gate3_meshy_hang_vs_e5b132f.png"
    sheet.save(out)
    (ART / out.name).write_bytes(out.read_bytes())
    print("sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
