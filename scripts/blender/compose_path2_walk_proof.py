#!/usr/bin/env python3
"""Caption Path 2 walk stills + 2x2 sheet. Walk-with-look is NOT claimed."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    caps = {
        "pass_l": "PASS L  |  arm-islands  |  bind NOT claimed",
        "contact_l": "CONTACT L  |  one scabbard character-RIGHT  |  TOP away",
        "pass_r": "PASS R  |  Evaluate() 5916447 reused",
        "contact_r": "CONTACT R  |  Play hub PNG still locked",
    }
    f_sm, f_lg = font(22), font(28)
    cells = []
    for name, text in caps.items():
        src = WALK / f"world_walk_{name}.png"
        im = Image.open(src).convert("RGB")
        d = ImageDraw.Draw(im)
        d.rectangle((0, 0, im.width, 64), fill=(16, 16, 14))
        d.text((24, 18), text, fill=(236, 230, 210), font=f_sm)
        im.save(src)
        (ART / f"gate3_{src.name}").write_bytes(src.read_bytes())
        cells.append(im)

    w, h = cells[0].size
    sheet = Image.new("RGB", (w * 2, h * 2 + 110), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((24, 18), "GATE 3  |  PATH 2 arm-islands  |  Game-view rear  |  bind NOT claimed", fill=(236, 230, 210), font=f_lg)
    d.text(
        (24, 56),
        "Play cam (0, 2.80, -5.40)  |  FOV 30  |  TOP = +Z  |  Evaluate() HOLD  |  walk NOT claimed",
        fill=(180, 176, 160),
        font=f_sm,
    )
    for i, im in enumerate(cells):
        sheet.paste(im, ((i % 2) * w, 110 + (i // 2) * h))
    out = PROOF / "gate3_path2_walk_phases.png"
    sheet.save(out)
    (ART / out.name).write_bytes(out.read_bytes())
    print("sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
