#!/usr/bin/env python3
"""Side-by-side fd9d6f8 BIND FAIL vs this hang-arm / tear-band iterate."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
FAIL = PROOF / "fail_fd9d6f8"
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
        ("mid_swing", "MID-SWING  (fd9 MP4 FAIL frame)"),
        ("pass_l", "PASS L"),
        ("contact_l", "CONTACT L"),
        ("contact_r", "CONTACT R"),
    ]
    cells = []
    for key, label in pairs:
        fail_p = FAIL / f"world_walk_{key}.png"
        now_p = WALK / f"world_walk_{key}.png"
        if not fail_p.exists() or not now_p.exists():
            print("skip missing", key, fail_p.exists(), now_p.exists())
            continue
        fail = Image.open(fail_p).convert("RGB")
        now = Image.open(now_p).convert("RGB")
        tw = 540
        th = int(fail.height * tw / fail.width)
        fail = cap(fail.resize((tw, th), Image.LANCZOS), f"fd9d6f8 BIND FAIL  |  {label}")
        now = cap(now.resize((tw, th), Image.LANCZOS), f"Meshy hang-arm  |  {label}  |  bind NOT claimed")
        cells.append((fail, now))

    w, h = cells[0][0].size
    sheet = Image.new("RGB", (w * 2, 120 + h * len(cells)), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((20, 16), "GATE 3  |  fd9d6f8 BIND FAIL  vs  solid Meshy hang-arms / no tear bands", fill=(236, 230, 210), font=font(24))
    d.text(
        (20, 56),
        "LEFT = fd9d6f8 (LOOK PASS, BIND FAIL: arm sheets + mid/leg tears).  RIGHT = this tip.  Look path unchanged.  Bind NOT claimed.",
        fill=(180, 176, 160),
        font=font(16),
    )
    d.text((20, 86), "Hard checks: solid Meshy-look arm volumes  ·  one scabbard character-RIGHT  ·  no mid/leg tear bands", fill=(180, 176, 160), font=font(16))
    for i, (fail, now) in enumerate(cells):
        sheet.paste(fail, (0, 120 + i * h))
        sheet.paste(now, (w, 120 + i * h))
    out = PROOF / "gate3_path2_vs_fd9d6f8.png"
    sheet.save(out, optimize=True)
    (ART / out.name).write_bytes(out.read_bytes())
    print("vs-fd9", out, out.stat().st_size)

    # hang rest vs frozen fd9 hang rest
    rest_fail = FAIL / "world_meshy_hang_rest.png"
    rest_now = PROOF / "world_meshy_hang_rest.png"
    if rest_fail.exists() and rest_now.exists():
        a = Image.open(rest_fail).convert("RGB")
        b = Image.open(rest_now).convert("RGB")
        tw = 540
        th = int(a.height * tw / a.width)
        a = cap(a.resize((tw, th), Image.LANCZOS), "fd9d6f8 hang rest  |  armpit/elbow shards")
        b = cap(b.resize((tw, th), Image.LANCZOS), "this tip hang rest  |  shards cleaned")
        rest = Image.new("RGB", (tw * 2, 90 + th), (16, 16, 14))
        rd = ImageDraw.Draw(rest)
        rd.text((20, 16), "HANG REST  |  clean armpit/elbow shards  |  LOOK path unchanged", fill=(236, 230, 210), font=font(22))
        rd.text((20, 50), "Same Meshy GLB / e5b132f UVs. Bind NOT claimed.", fill=(180, 176, 160), font=font(16))
        rest.paste(a, (0, 90))
        rest.paste(b, (tw, 90))
        rout = PROOF / "gate3_hang_rest_vs_fd9d6f8.png"
        rest.save(rout, optimize=True)
        (ART / rout.name).write_bytes(rout.read_bytes())
        print("vs-hang", rout, rout.stat().st_size)


if __name__ == "__main__":
    main()
