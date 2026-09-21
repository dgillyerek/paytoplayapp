#!/usr/bin/env python3
"""After flatten: n=10 A/B/C vs frozen 2820e53 floor (SCENE/GROUND fix)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
FAIL = PROOF / "fail_2820e53_band"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def cap(im: Image.Image, text: str, h=54):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, h), fill=(16, 16, 14))
    d.text((10, 16), text, fill=(236, 230, 210), font=font(14))
    return im


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    before_c = Image.open(FAIL / "C_mp4_n10.png").convert("RGB")
    after_a = Image.open(WALK / "census_cycle_actor_n10.png").convert("RGB")
    after_b = Image.open(WALK / "world_walk_n10.png").convert("RGB")
    after_c = Image.open(WALK / "census_cycle_mp4_n10.png").convert("RGB")
    tw, th = 360, 640
    y0 = before_c.height // 2
    cells = []
    for im, lab in (
        (before_c, "BEFORE C  |  2820e53 MP4 n=10  |  lit floor"),
        (after_a, "AFTER A  |  Actor LBS  |  no ground"),
        (after_b, "AFTER B  |  EEVEE still  |  no ground"),
        (after_c, "AFTER C  |  MP4 n=10  |  no ground"),
    ):
        cells.append(cap(im.resize((tw, th), Image.LANCZOS), lab))
    floors = []
    for im, lab in (
        (before_c, "BEFORE floor"),
        (after_a, "AFTER A floor"),
        (after_b, "AFTER B floor"),
        (after_c, "AFTER C floor"),
    ):
        crop = im.crop((0, y0, 220, im.height)).resize((tw, th), Image.LANCZOS)
        floors.append(cap(crop, lab + "  |  left gutter"))
    w, h = cells[0].size
    sheet = Image.new("RGB", (w * 4, 140 + h * 2), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((14, 10), "GATE 3  |  SCENE/GROUND flatten  |  n=10 after no-ground setup_render  |  bind NOT claimed", fill=(236, 230, 210), font=font(22))
    d.text((14, 44), "Actor was already clean. Backdrop flattened (no lit Ground plane). KEEP one-loft + steel texel.", fill=(236, 200, 120), font=font(16))
    d.text((14, 72), "LOOK e5b132f. Do not claim bind PASS. Hub PNG HOLD. PR #21 HOLD.", fill=(180, 176, 160), font=font(14))
    d.text((14, 100), "BEFORE = frozen 2820e53 C. AFTER = same pose, no ground plane in A/B/C.", fill=(180, 176, 160), font=font(14))
    for i, im in enumerate(cells):
        sheet.paste(im, (i * w, 140))
    for i, im in enumerate(floors):
        sheet.paste(im, (i * w, 140 + h))
    out = PROOF / "gate3_mesh_vs_capture_band_after.png"
    sheet.save(out, optimize=True)
    try:
        (ART / out.name).write_bytes(out.read_bytes())
    except OSError as exc:
        print("artifact copy skip", exc)
    print("band after sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
