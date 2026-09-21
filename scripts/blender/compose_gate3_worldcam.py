#!/usr/bin/env python3
"""Gate 3 World-cam stills vs 01_rear_LOCKED + turnaround BACK.

Play march angle (high rear, TOP = away). Look gate is NOT claimed.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PROOF = ROOT / "Docs/Survival/previews/gate3"
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
ART = Path("/opt/cursor/artifacts")
STUDIO = (232, 232, 230)
DARK = (24, 24, 22)


def font(size: int):
    try:
        return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", size)
    except OSError:
        return ImageFont.load_default()


def load_rgb(path: Path, bg=STUDIO) -> Image.Image:
    im = Image.open(path)
    if im.mode in ("RGBA", "LA") or (im.mode == "P" and "transparency" in im.info):
        rgba = im.convert("RGBA")
        out = Image.new("RGB", rgba.size, bg)
        out.paste(rgba, mask=rgba.split()[-1])
        return out
    return im.convert("RGB")


def fit(im: Image.Image, box: tuple[int, int], bg) -> Image.Image:
    cell = Image.new("RGB", box, bg)
    scale = min((box[0] - 20) / im.width, (box[1] - 20) / im.height)
    nw, nh = max(1, int(im.width * scale)), max(1, int(im.height * scale))
    placed = im.resize((nw, nh), Image.LANCZOS)
    cell.paste(placed, ((box[0] - nw) // 2, (box[1] - nh) // 2))
    return cell


def main() -> None:
    PROOF.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    rear = load_rgb(PROOF / "world_rear.png", DARK)
    q = load_rgb(PROOF / "world_rear_34.png", DARK)
    sot = load_rgb(LOOK / "01_rear_LOCKED.png", (8, 8, 8))
    back = load_rgb(GATE / "03_BACK.png", STUDIO)

    cw, ch = 480, 850
    sheet = Image.new("RGB", (cw * 2, ch * 2 + 130), DARK)
    d = ImageDraw.Draw(sheet)
    title = font(26)
    lab = font(16)
    d.text((24, 16), "GATE 3 GAME MESH  ·  World-cam Play march angle", fill=(236, 230, 210), font=title)
    d.text(
        (24, 52),
        "high rear  ·  TOP = away  ·  FOV 30  ·  1080×1920  ·  NOT beauty portrait cam",
        fill=(180, 180, 176),
        font=lab,
    )
    d.text(
        (24, 78),
        "FAIL iterate: segmented plate/helm/heraldry/sabatons  ·  look + walk NOT claimed",
        fill=(210, 176, 82),
        font=lab,
    )
    d.text(
        (24, 104),
        "Hang pose vs 01_rear  ·  turnaround BACK is A-pose (honest pose gap on arms)",
        fill=(160, 160, 156),
        font=lab,
    )
    pairs = [
        (rear, "WORLD REAR  (Play cam)", sot, "SoT  01_rear_LOCKED"),
        (q, "WORLD REAR ¾  (Play cam +X)", back, "SoT  turnaround 03_BACK"),
    ]
    y0 = 130
    for left, ll, right, rl in pairs:
        sheet.paste(fit(left, (cw, ch), DARK), (0, y0))
        sheet.paste(fit(right, (cw, ch), (18, 18, 16)), (cw, y0))
        d.text((16, y0 + 8), ll, fill=(236, 230, 210), font=lab)
        d.text((cw + 16, y0 + 8), rl, fill=(236, 230, 210), font=lab)
        y0 += ch
    out = PROOF / "gate3_worldcam_vs_sot.png"
    sheet.save(out, optimize=True)
    sheet.save(ART / "gate3_worldcam_vs_sot.png", optimize=True)
    rear.save(ART / "gate3_world_rear.png")
    q.save(ART / "gate3_world_rear_34.png")
    print("sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
