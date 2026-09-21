#!/usr/bin/env python3
"""A/B/C mesh-vs-capture at n=0,5,10,15 — full cycle, not one mid-swing still."""
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


def cap(im: Image.Image, text: str):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, 54), fill=(16, 16, 14))
    d.text((10, 16), text, fill=(236, 230, 210), font=font(14))
    return im


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    frames = [0, 5, 10, 15]
    rows = []
    for n in frames:
        a = WALK / f"census_cycle_actor_n{n:02d}.png"
        b = WALK / f"world_walk_n{n:02d}.png"
        c = WALK / f"census_cycle_mp4_n{n:02d}.png"
        if not (a.exists() and b.exists() and c.exists()):
            print("skip incomplete n", n, a.exists(), b.exists(), c.exists())
            continue
        cells = []
        for p, lab in (
            (a, f"A LBS  n={n}"),
            (b, f"B EEVEE  n={n}"),
            (c, f"C MP4  n={n}"),
        ):
            im = Image.open(p).convert("RGB")
            cells.append(cap(im.resize((360, 640), Image.LANCZOS), lab))
        rows.append(cells)
    if not rows:
        raise SystemExit("no cycle ABC rows")
    w, h = rows[0][0].size
    sheet = Image.new("RGB", (w * 3, 100 + h * len(rows)), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((14, 10), "GATE 3  |  cycle A/B/C  |  n=0,5,10,15  |  bind NOT claimed", fill=(236, 230, 210), font=font(22))
    d.text((14, 44), "A = Actor LBS (no encoder).  B = Blender EEVEE.  C = walk MP4.  All must be closed hang-arm volumes — not one t=0.625 still.", fill=(180, 176, 160), font=font(14))
    d.text((14, 70), "LOOK e5b132f.  One loft + steel texel.  SCENE/GROUND flatten (no lit floor).  Bind NOT claimed.", fill=(180, 176, 160), font=font(14))
    for ri, cells in enumerate(rows):
        for ci, im in enumerate(cells):
            sheet.paste(im, (ci * w, 100 + ri * h))
    out = PROOF / "gate3_mesh_vs_capture_cycle.png"
    sheet.save(out, optimize=True)
    (ART / out.name).write_bytes(out.read_bytes())
    print("cycle sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
