#!/usr/bin/env python3
"""Path A re-gate: World stills vs e5b132f / turnaround + walk contact. Bind NOT claimed."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
LOOK_E5 = PROOF / "look_e5b132f"
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
WALK = PROOF / "walk"
ART = Path("/opt/cursor/artifacts")


def font(size: int):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def cap(im: Image.Image, text: str, h=52):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, h), fill=(16, 16, 14))
    d.text((10, 16), text, fill=(236, 230, 210), font=font(14))
    return im


def load(p: Path) -> Image.Image:
    im = Image.open(p)
    if im.mode != "RGB":
        im = im.convert("RGB")
    return im


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    tw, th = 320, 568
    pairs = [
        (LOOK_E5 / "world_rear.png", PROOF / "world_rear.png", "rear Play", "e5b132f", "Path A"),
        (LOOK_E5 / "world_front.png", PROOF / "world_front.png", "front", "e5b132f", "Path A"),
        (LOOK_E5 / "world_side_r.png", PROOF / "world_side_r.png", "side R", "e5b132f", "Path A"),
        (LOOK_E5 / "world_34_front.png", PROOF / "world_34_front.png", "front ¾", "e5b132f", "Path A"),
        (LOOK / "01_rear_LOCKED.png", PROOF / "world_rear.png", "rear vs SoT", "01_rear_LOCKED", "Path A"),
    ]
    cells = []
    for a, b, lab, al, bl in pairs:
        if not a.exists() or not b.exists():
            print("skip", lab, a.exists(), b.exists())
            continue
        cells.append((
            cap(load(a).resize((tw, th), Image.LANCZOS), f"{al}  |  {lab}"),
            cap(load(b).resize((tw, th), Image.LANCZOS), f"{bl}  |  {lab}"),
        ))
    walk_row = []
    for name, lab in (
        ("world_walk_contact_l.png", "contact L"),
        ("world_walk_contact_r.png", "contact R"),
        ("world_walk_pass_l.png", "pass L"),
        ("world_walk_n10.png", "n=10"),
    ):
        p = WALK / name
        if p.exists():
            walk_row.append(cap(load(p).resize((tw, th), Image.LANCZOS), f"walk  |  {lab}  |  bind NOT claimed"))
    if not cells:
        raise SystemExit("no Path A still pairs")
    w, h = cells[0][0].size
    rows = len(cells) + (1 if walk_row else 0)
    sheet = Image.new("RGB", (w * 2 if not walk_row else max(w * 2, w * max(len(walk_row), 2)), 130 + h * rows), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    d.text((14, 10), "PATH A  |  clean retopo + Meshy project  |  bind NOT claimed", fill=(236, 230, 210), font=font(22))
    d.text((14, 44), "QuadriFlow mid-poly. Albedo projected from e5b132f Meshy. No tube / capsule / paper-weight hacks.", fill=(236, 200, 120), font=font(15))
    d.text((14, 72), "LOOK Meshy knight. Hub PNG HOLD. Walk NOT claimed. Eye: lion / plate / tabard, 1 arm/side, 1L+1R, planted.", fill=(180, 176, 160), font=font(14))
    d.text((14, 100), "LEFT = locked e5b132f / SoT.  RIGHT = this Path A World-cam.", fill=(180, 176, 160), font=font(14))
    for i, (a, b) in enumerate(cells):
        sheet.paste(a, (0, 130 + i * h))
        sheet.paste(b, (w, 130 + i * h))
    if walk_row:
        y = 130 + len(cells) * h
        for i, im in enumerate(walk_row[:4]):
            sheet.paste(im, (i * w, y))
    out = PROOF / "gate3_path_a_vs_e5b132f.png"
    sheet.save(out, optimize=True)
    try:
        (ART / out.name).write_bytes(out.read_bytes())
    except OSError as exc:
        print("artifact skip", exc)
    print("path A sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
