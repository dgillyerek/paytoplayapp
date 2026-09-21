#!/usr/bin/env python3
"""Path 2 Meshy World-cam stills vs locked turnaround / 01_rear.

Play march angle. Look gate is NOT claimed.
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
    scale = min((box[0] - 16) / im.width, (box[1] - 16) / im.height)
    nw, nh = max(1, int(im.width * scale)), max(1, int(im.height * scale))
    placed = im.resize((nw, nh), Image.LANCZOS)
    cell.paste(placed, ((box[0] - nw) // 2, (box[1] - nh) // 2))
    return cell


def caption(im: Image.Image, text: str) -> Image.Image:
    out = im.copy()
    d = ImageDraw.Draw(out)
    d.rectangle((0, 0, out.width, 64), fill=(16, 16, 14))
    d.text((24, 18), text, fill=(236, 230, 210), font=font(22))
    return out


def main() -> None:
    PROOF.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)

    shots = {
        "world_rear": "PATH 2 paint iterate  ·  UV bleed + SoT lion  ·  look NOT claimed",
        "world_rear_34": "PATH 2 paint iterate  ·  rear ¾  ·  scabbard character-RIGHT",
        "world_front": "PATH 2 paint iterate  ·  World-cam Play front  ·  look NOT claimed",
        "world_side_r": "PATH 2 paint iterate  ·  side R  ·  scabbard character-RIGHT",
        "world_34_front": "PATH 2 paint iterate  ·  front ¾  ·  look NOT claimed",
    }
    caps = {}
    for name, text in shots.items():
        im = caption(load_rgb(PROOF / f"{name}.png", DARK), text)
        im.save(PROOF / f"{name}.png", optimize=True)
        im.save(ART / f"gate3_{name}.png")
        caps[name] = im

    pairs = [
        (caps["world_rear"], "WORLD REAR  (Play cam)", load_rgb(LOOK / "01_rear_LOCKED.png", (8, 8, 8)), "SoT  01_rear_LOCKED"),
        (caps["world_rear_34"], "WORLD REAR ¾  (Play cam +X)", load_rgb(GATE / "03_BACK.png", STUDIO), "SoT  turnaround 03_BACK"),
        (caps["world_front"], "WORLD FRONT  (Play cam)", load_rgb(GATE / "01_FRONT.png", STUDIO), "SoT  turnaround 01_FRONT"),
        (caps["world_side_r"], "WORLD SIDE R  (Play cam)", load_rgb(GATE / "02_SIDE_R.png", STUDIO), "SoT  turnaround 02_SIDE_R"),
        (caps["world_34_front"], "WORLD FRONT ¾  (Play cam)", load_rgb(GATE / "04_THREE_QUARTER.png", STUDIO), "SoT  turnaround 04_THREE_QUARTER"),
    ]

    cw, ch = 480, 820
    header = 140
    sheet = Image.new("RGB", (cw * 2, header + ch * len(pairs)), DARK)
    d = ImageDraw.Draw(sheet)
    d.text((24, 16), "GATE 3  ·  PATH 2 paint iterate  ·  World-cam Play march angle", fill=(236, 230, 210), font=font(24))
    d.text((24, 52), "FOV 30  ·  1080×1920  ·  TOP = +Z = away  ·  NOT beauty portrait cam", fill=(180, 180, 176), font=font(16))
    d.text((24, 78), "UV island bleed + SoT lion planar unwrap  ·  look NOT claimed", fill=(210, 176, 82), font=font(16))
    d.text((24, 104), "Walk / Animator / Play hub HOLD  ·  8babba7 was UV cracks + lion smear", fill=(160, 160, 156), font=font(16))
    y0 = header
    lab = font(15)
    for left, ll, right, rl in pairs:
        sheet.paste(fit(left, (cw, ch), DARK), (0, y0))
        sheet.paste(fit(right, (cw, ch), (18, 18, 16)), (cw, y0))
        d.text((16, y0 + 8), ll, fill=(236, 230, 210), font=lab)
        d.text((cw + 16, y0 + 8), rl, fill=(236, 230, 210), font=lab)
        y0 += ch
    out = PROOF / "gate3_worldcam_vs_sot.png"
    sheet.save(out, optimize=True)
    sheet.save(ART / "gate3_worldcam_vs_sot.png", optimize=True)
    print("sheet", out, out.stat().st_size)


if __name__ == "__main__":
    main()
