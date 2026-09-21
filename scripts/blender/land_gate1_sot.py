#!/usr/bin/env python3
"""Land Gate 1 LOCKED SoT folders for the Aldric blockout.

Front / side_R crop from look_targets/02 (helmeted A-pose).
Front is mirrored so the scabbard is **character-RIGHT** (02 front is the
mirrored hip). Back SoT is 01_rear_LOCKED. 02 rear is ignored.

Portrait binaries (face law) are copied when Design drops them — never invent
a face, never substitute the helmeted 00_fullbody roster still.

04 THREE_QUARTER is the helmeted A-pose ¾ from Design. Cape + drawn-sword
00_fullbody is NOT that panel and is not used as a stand-in.
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
UPLOADS = Path("/home/ubuntu/.cursor/projects/workspace/uploads")


def _fig_mask(rgb: np.ndarray) -> np.ndarray:
    a = rgb.astype(np.int16)
    sat = a.max(2) - a.min(2)
    val = a.mean(2)
    return (sat > 12) | (val < 170)


def crop_panel(im: Image.Image, x0: int, x1: int, pad: int = 10) -> Image.Image:
    rgb = np.array(im.convert("RGB"))
    mask = _fig_mask(rgb[:, x0:x1])
    ys, xs = np.where(mask)
    xa, xb = x0 + int(xs.min()) - pad, x0 + int(xs.max()) + pad
    ya, yb = int(ys.min()) - pad, int(ys.max()) + pad
    xa, ya = max(0, xa), max(0, ya)
    xb, yb = min(im.width, xb), min(im.height, yb)
    return im.crop((xa, ya, xb, yb))


def crop_alpha(im: Image.Image, pad: int = 8) -> Image.Image:
    a = np.array(im.convert("RGBA"))
    if a.shape[2] == 4 and int(a[:, :, 3].max()) > 20:
        ys, xs = np.where(a[:, :, 3] > 20)
    else:
        rgb = a[:, :, :3]
        mask = _fig_mask(rgb)
        ys, xs = np.where(mask)
    xa, xb = max(0, int(xs.min()) - pad), min(im.width, int(xs.max()) + pad)
    ya, yb = max(0, int(ys.min()) - pad), min(im.height, int(ys.max()) + pad)
    return im.crop((xa, ya, xb, yb))


def _font(size: int):
    try:
        return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", size)
    except OSError:
        return ImageFont.load_default()


def _placeholder_three_q() -> Image.Image:
    """Honest card — do not substitute cape 00_fullbody as the ¾ SoT."""
    im = Image.new("RGB", (720, 1280), (236, 236, 234))
    d = ImageDraw.Draw(im)
    d.text((40, 80), "04  THREE-QUARTER", fill=(32, 32, 30), font=_font(36))
    d.text((40, 160), "Design helmeted A-pose ¾", fill=(80, 70, 40), font=_font(26))
    d.text((40, 220), "bytes not on this VM.", fill=(80, 70, 40), font=_font(26))
    d.text((40, 320), "Blockout ¾ is the clay still.", fill=(60, 60, 58), font=_font(22))
    d.text((40, 380), "Proportion SoT = 01 / 02 / 03.", fill=(60, 60, 58), font=_font(22))
    d.text((40, 480), "Cape 00_fullbody is NOT ¾.", fill=(140, 40, 40), font=_font(22))
    return im


def _find_design(name: str) -> Path | None:
    exact = GATE / name
    if exact.exists() and exact.stat().st_size > 20000:
        # Skip our own tiny placeholder / leftover cape if we just wrote a card.
        return exact
    look = LOOK / name
    if look.exists() and look.stat().st_size > 20000:
        return look
    if UPLOADS.exists():
        for p in UPLOADS.glob(f"*{name}*"):
            if p.is_file() and p.stat().st_size > 20000:
                return p
        stem = Path(name).stem
        for p in UPLOADS.glob(f"*{stem}*"):
            if p.is_file() and p.stat().st_size > 20000:
                return p
    return None


def _land_portrait() -> None:
    portrait_names = [
        ("05_PORTRAIT_COMPLETE_fullbody.png", GATE / "05_PORTRAIT_COMPLETE_fullbody.png"),
        ("00_portrait_COMPLETE_LOCKED.png", LOOK / "00_portrait_COMPLETE_LOCKED.png"),
    ]
    # Same Design still may land under either name.
    srcs = []
    for name, _dest in portrait_names:
        found = _find_design(name)
        if found:
            srcs.append(found)
    extra = [
        UPLOADS / "05_PORTRAIT_COMPLETE_fullbody.png",
        UPLOADS / "00_portrait_COMPLETE_LOCKED.png",
    ]
    for p in extra:
        if p.exists():
            srcs.append(p)
    if not srcs:
        print("portrait: Design bytes not on VM (face law path reserved, not invented)")
        return
    src = srcs[0]
    dest_gate = GATE / "05_PORTRAIT_COMPLETE_fullbody.png"
    dest_look = LOOK / "00_portrait_COMPLETE_LOCKED.png"
    dest_gate.write_bytes(src.read_bytes())
    dest_look.write_bytes(src.read_bytes())
    print("portrait landed", src, dest_gate.stat().st_size)


def main() -> None:
    GATE.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(LOOK / "02_aldric_turnaround_orthos.png")
    w, _h = sheet.size
    front_raw = crop_panel(sheet, 0, w // 4)
    # 02 front hip is mirrored vs 01_rear. Flip so scabbard = character-RIGHT.
    front = front_raw.transpose(Image.FLIP_LEFT_RIGHT)
    side_r = crop_panel(sheet, 2 * w // 4, 3 * w // 4)
    back = crop_alpha(Image.open(LOOK / "01_rear_LOCKED.png"))

    front.save(GATE / "01_FRONT.png")
    side_r.save(GATE / "02_SIDE_R.png")
    back.save(GATE / "03_BACK.png")

    cape = LOOK / "00_fullbody_LOCKED.png"
    cape_bytes = cape.read_bytes() if cape.exists() else b""
    # Never keep the leftover cape-roster crop as ¾.
    leftover = GATE / "04_THREE_QUARTER.png"
    if leftover.exists():
        leftover.unlink()

    threeq_design = None
    for cand in (
        UPLOADS / "04_THREE_QUARTER.png",
        LOOK / "04_THREE_QUARTER.png",
    ):
        if cand.exists() and cand.stat().st_size > 20000 and cand.read_bytes() != cape_bytes:
            threeq_design = cand
            break
    if threeq_design:
        Image.open(threeq_design).save(GATE / "04_THREE_QUARTER.png")
        print("04 from", threeq_design)
    else:
        _placeholder_three_q().save(GATE / "04_THREE_QUARTER.png")
        print("04 placeholder (cape 00_fullbody is not ¾)")

    _land_portrait()

    front.save(LOOK / "03_turnaround_front_LOCKED.png")
    side_r.save(LOOK / "03_turnaround_side_r_LOCKED.png")
    back.save(LOOK / "03_turnaround_back_LOCKED.png")

    cells = [
        Image.open(GATE / "01_FRONT.png"),
        Image.open(GATE / "02_SIDE_R.png"),
        Image.open(GATE / "03_BACK.png"),
        Image.open(GATE / "04_THREE_QUARTER.png"),
    ]
    labels = ["01_FRONT", "02_SIDE_R", "03_BACK", "04_THREE_QUARTER"]
    ch, cw = 820, 360
    sheet_out = Image.new("RGB", (cw * len(cells), ch + 90), (236, 236, 234))
    d = ImageDraw.Draw(sheet_out)
    d.text((16, 10), "Sir Aldric — TURNAROUND Gate 1  (scabbard = character-RIGHT)", fill=(40, 40, 38), font=_font(18))
    for i, (cell, lab) in enumerate(zip(cells, labels)):
        cell = cell.convert("RGB")
        scale = min((cw - 24) / cell.width, (ch - 24) / cell.height)
        nw, nh = max(1, int(cell.width * scale)), max(1, int(cell.height * scale))
        placed = cell.resize((nw, nh), Image.LANCZOS)
        sheet_out.paste(placed, (i * cw + (cw - nw) // 2, 48 + (ch - 24 - nh) // 2))
        d.text((i * cw + 16, ch + 56), lab, fill=(40, 40, 38), font=_font(16))
    sheet_out.save(GATE / "TURNAROUND_SHEET.png")
    sheet_out.save(LOOK / "03_turnaround_sheet_LOCKED.png")

    (GATE / "README.md").write_text(
        "# Sir Aldric — TURNAROUND Gate 1 LOCKED\n\n"
        "Look law is **LOCKED**. Do not redesign.\n\n"
        "| File | Role |\n"
        "| --- | --- |\n"
        "| `01_FRONT.png` | Ortho front; hip flipped so scabbard is **character-RIGHT** |\n"
        "| `02_SIDE_R.png` | Character-right profile |\n"
        "| `03_BACK.png` | Ortho back from `01_rear_LOCKED` — scabbard viewer-right |\n"
        "| `04_THREE_QUARTER.png` | Helmeted A-pose ¾ (Design panel). Cape `00_fullbody` is **not** ¾ |\n"
        "| `05_PORTRAIT_COMPLETE_fullbody.png` | Face + ornate armor; oval bust = **face law** |\n"
        "| `TURNAROUND_SHEET.png` | Four-up Gate 1 working sheet |\n\n"
        "Hard locks: scabbard character-RIGHT; silver/gold plate volumes; "
        "royal-blue surcoat volume; lion space on the back; proportions match this turnaround.\n"
        "Blockout is helmeted (turnaround). Portrait is face law — not a helmet redesign.\n"
        "If `05_PORTRAIT_COMPLETE_fullbody.png` is missing, Design bytes did not land on the VM; do not invent a face.\n"
    )
    readme = LOOK / "README.md"
    extra = (
        "\n## Gate 1 LOCKED (2026-09-21)\n\n"
        "- `00_portrait_COMPLETE_LOCKED.png` — face law (oval bust). Same still as "
        "`TURNAROUND_GATE1/LOCKED/05_PORTRAIT_COMPLETE_fullbody.png`. "
        "Do not invent a face; helmeted `00_fullbody_LOCKED` is **not** face law.\n"
        "- `03_turnaround_front_LOCKED.png` — 02 front **flipped** so scabbard is character-RIGHT.\n"
        "- `03_turnaround_side_r_LOCKED.png` / `03_turnaround_back_LOCKED.png` — mirrors of Gate 1.\n"
        "- `03_turnaround_sheet_LOCKED.png` — working four-up. **02 rear is not SoT** if mirrored.\n"
        "- Scabbard SoT remains **character-right**.\n"
    )
    text = readme.read_text()
    if "Gate 1 LOCKED (2026-09-21)" in text:
        head = text.split("## Gate 1 LOCKED (2026-09-21)")[0].rstrip()
        readme.write_text(head + extra)
    else:
        readme.write_text(text.rstrip() + extra)
    print("gate1", GATE)
    for p in sorted(GATE.glob("*")):
        print(" ", p.name, p.stat().st_size if p.is_file() else "dir")
    for p in (LOOK / "00_portrait_COMPLETE_LOCKED.png",):
        print(" look", p.name, "yes" if p.exists() else "MISSING")


if __name__ == "__main__":
    main()
