#!/usr/bin/env python3
"""n=10 A/B/C + lower-half + floor-only crops. Frozen 2820e53 stills."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
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
    a = Image.open(FAIL / "A_actor_lbs_n10.png").convert("RGB")
    b = Image.open(FAIL / "B_eevee_n10.png").convert("RGB")
    c = Image.open(FAIL / "C_mp4_n10.png").convert("RGB")
    tw, th = 360, 640
    full = [
        cap(a.resize((tw, th), Image.LANCZOS), "A  Actor LBS  |  n=10  |  no encoder"),
        cap(b.resize((tw, th), Image.LANCZOS), "B  EEVEE still  |  n=10  |  no Unity GV"),
        cap(c.resize((tw, th), Image.LANCZOS), "C  MP4 n=10  |  encoder"),
    ]
    y0 = a.height // 2
    lowers = []
    for im, lab in (
        (a, "A lower half"),
        (b, "B lower half"),
        (c, "C lower half"),
    ):
        crop = im.crop((0, y0, im.width, im.height)).resize((tw, th), Image.LANCZOS)
        lowers.append(cap(crop, lab + "  |  tabard/legs clean; floor bands"))
    # Left gutter: no character — horizon / lit-floor vs world only.
    floors = []
    for im, lab in (
        (a, "A floor only"),
        (b, "B floor only"),
        (c, "C floor only"),
    ):
        crop = im.crop((0, y0, 220, im.height)).resize((tw, th), Image.LANCZOS)
        floors.append(cap(crop, lab + "  |  lit plane vs world bg"))
    w, h = full[0].size
    sheet = Image.new("RGB", (w * 3, 140 + h * 3), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((14, 10), "GATE 3  |  n=10 mesh-vs-capture  |  2820e53 lower-half banding  |  bind NOT claimed", fill=(236, 230, 210), font=font(22))
    d.text((14, 44), "VERDICT: SCENE/GROUND — banding is only floor/background in A+B+C. Not character bind. Not capture-only.", fill=(236, 200, 120), font=font(16))
    d.text((14, 72), "A+B+C: tabard navy + gold hem + greaves clean. Horizon = lit Ground plane vs world bg (sharpest on A).", fill=(180, 176, 160), font=font(14))
    d.text((14, 100), "KEEP one-loft + steel texel. LOOK e5b132f. Flatten backdrop and re-drop MP4. Do not claim bind PASS.", fill=(180, 176, 160), font=font(14))
    for i, im in enumerate(full):
        sheet.paste(im, (i * w, 140))
    for i, im in enumerate(lowers):
        sheet.paste(im, (i * w, 140 + h))
    for i, im in enumerate(floors):
        sheet.paste(im, (i * w, 140 + h * 2))
    out = PROOF / "gate3_mesh_vs_capture_band_n10.png"
    sheet.save(out, optimize=True)
    try:
        (ART / out.name).write_bytes(out.read_bytes())
    except OSError as exc:
        print("artifact copy skip", exc)
    print("band verdict sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
