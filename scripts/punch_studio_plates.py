#!/usr/bin/env python3
"""Punch studio backdrop plates on Area1 piece/HUD PNGs to true RGBA alpha."""

from __future__ import annotations

import collections
import math
import statistics
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Assets" / "Grove" / "Art" / "Area1"

TARGETS = [
    "Items/Wildflower/WF_T01_Seed.png",
    "Items/Wildflower/WF_T02_Sprout.png",
    "Items/Wildflower/WF_T03_Bud.png",
    "Items/Wildflower/WF_T04_Wildflower.png",
    "Items/Wildflower/WF_T05_Bouquet.png",
    "Items/Wildflower/WF_T06_FlowerBox.png",
    "Items/Herb/HB_T01_HerbSprig.png",
    "Items/Herb/HB_T02_HerbPot.png",
    "Items/Herb/HB_T03_HerbBasket.png",
    "Items/Tools/TL_T01_Twig.png",
    "Items/Tools/TL_T02_Stick.png",
    "Items/Tools/TL_T03_HandRake.png",
    "Env/ENV_FG_GardenCrate_Idle.png",
    "Maya/MAYA_Portrait_Happy.png",
    "Maya/MAYA_Portrait_Neutral.png",
    "UI/HUD_EnergyPill.png",
    "UI/HUD_Wallet_Coin.png",
    "UI/UI_GoalPill.png",
    "UI/UI_Btn_Deliver.png",
    "UI/UI_Teach_Banner.png",
    "UI/UI_Badge_Starter.png",
]


def decode_rgba(path: Path):
    data = path.read_bytes()
    w, h, bit, color, *_ = struct.unpack(">IIBBBBB", data[16:29])
    pos = 8
    idat = bytearray()
    while pos + 12 <= len(data):
        length = struct.unpack(">I", data[pos : pos + 4])[0]
        typ = data[pos + 4 : pos + 8]
        chunk = data[pos + 8 : pos + 8 + length]
        if typ == b"IDAT":
            idat += chunk
        if typ == b"IEND":
            break
        pos += 12 + length
    raw = zlib.decompress(bytes(idat))
    bpp = 4 if color == 6 else 3
    stride = w * bpp
    rgba = bytearray(w * h * 4)
    prev = bytearray(stride)
    row = bytearray(stride)
    i = 0

    def paeth(a, b, c):
        p = a + b - c
        pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
        if pa <= pb and pa <= pc:
            return a
        return b if pb <= pc else c

    for y in range(h):
        filt = raw[i]
        i += 1
        row[:] = raw[i : i + stride]
        i += stride
        for x in range(stride):
            left = row[x - bpp] if x >= bpp else 0
            up = prev[x]
            ul = prev[x - bpp] if x >= bpp else 0
            if filt == 1:
                row[x] = (row[x] + left) & 255
            elif filt == 2:
                row[x] = (row[x] + up) & 255
            elif filt == 3:
                row[x] = (row[x] + ((left + up) // 2)) & 255
            elif filt == 4:
                row[x] = (row[x] + paeth(left, up, ul)) & 255
        if color == 6:
            rgba[y * w * 4 : (y + 1) * w * 4] = row
        else:
            for x in range(w):
                o = (y * w + x) * 4
                rgba[o] = row[x * 3]
                rgba[o + 1] = row[x * 3 + 1]
                rgba[o + 2] = row[x * 3 + 2]
                rgba[o + 3] = 255
        prev[:] = row
    return w, h, rgba, color


def write_png(path: Path, w: int, h: int, rgba: bytearray) -> None:
    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    raw = bytearray()
    for y in range(h):
        raw.append(0)
        raw.extend(rgba[y * w * 4 : (y + 1) * w * 4])
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
    )


def d3(p, q) -> float:
    return math.sqrt((p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2 + (p[2] - q[2]) ** 2)


def corners_transparent(rgba, w, h) -> bool:
    idx = [0, (w - 1) * 4, (h - 1) * w * 4, ((h - 1) * w + w - 1) * 4]
    return all(rgba[i + 3] < 16 for i in idx)


def corner_plates(rgba, w, h, patch=12):
    plates = []
    for ox, oy in ((0, 0), (w - patch, 0), (0, h - patch), (w - patch, h - patch)):
        samp = []
        for y in range(oy, oy + patch):
            for x in range(ox, ox + patch):
                i = (y * w + x) * 4
                if rgba[i + 3] >= 8:
                    samp.append((rgba[i], rgba[i + 1], rgba[i + 2]))
        if samp:
            plates.append(tuple(int(statistics.median([s[c] for s in samp])) for c in range(3)))
    return plates


def is_cream(plates) -> bool:
    for r, g, b in plates:
        l = (r + g + b) / 3
        sat = max(r, g, b) - min(r, g, b)
        if l >= 170 and sat <= 60:
            return True
    return False


def color_flood(w, h, rgba, plates, thresh=44.0):
    n = w * h
    mark = bytearray(n)
    if not plates:
        return mark
    q = collections.deque()

    def match(i):
        if rgba[i * 4 + 3] < 8:
            return True
        p = rgba[i * 4 : i * 4 + 3]
        return min(d3(p, pl) for pl in plates) <= thresh

    def push(i):
        if not mark[i]:
            mark[i] = 1
            q.append(i)

    for x in range(w):
        if match(x):
            push(x)
        if match((h - 1) * w + x):
            push((h - 1) * w + x)
    for y in range(h):
        if match(y * w):
            push(y * w)
        if match(y * w + w - 1):
            push(y * w + w - 1)
    while q:
        i = q.popleft()
        x, y = i % w, i // w
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h:
                ni = ny * w + nx
                if not mark[ni] and match(ni):
                    push(ni)
    return mark


def edge_flood(w, h, rgba, barrier=16.0):
    n = w * h
    grad = [0.0] * n
    for y in range(h):
        for x in range(w):
            i = y * w + x
            best = 0.0
            if x > 0:
                best = max(best, d3(rgba[i * 4 : i * 4 + 3], rgba[(i - 1) * 4 : (i - 1) * 4 + 3]))
            if y > 0:
                best = max(best, d3(rgba[i * 4 : i * 4 + 3], rgba[(i - w) * 4 : (i - w) * 4 + 3]))
            grad[i] = best
    mark = bytearray(n)
    q = collections.deque()

    def push(i):
        if not mark[i]:
            mark[i] = 1
            q.append(i)

    for x in range(w):
        push(x)
        push((h - 1) * w + x)
    for y in range(h):
        push(y * w)
        push(y * w + w - 1)
    while q:
        i = q.popleft()
        x, y = i % w, i // w
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if not (0 <= nx < w and 0 <= ny < h):
                continue
            ni = ny * w + nx
            if mark[ni]:
                continue
            if rgba[ni * 4 + 3] < 8 or grad[ni] < barrier:
                push(ni)
    return mark


def inner_keep(w, h, rgba, mark):
    x0, x1 = w * 3 // 10, w * 7 // 10
    y0, y1 = h * 3 // 10, h * 7 // 10
    tot = keep = 0
    for y in range(y0, y1):
        for x in range(x0, x1):
            tot += 1
            i = y * w + x
            if mark[i] == 0 and rgba[i * 4 + 3] >= 8:
                keep += 1
    return keep / max(1, tot)


def apply(rgba, mark, w, h):
    out = bytearray(rgba)
    n = w * h
    for i in range(n):
        if mark[i]:
            out[i * 4 + 3] = 0
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if mark[i] or out[i * 4 + 3] < 8:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and mark[ny * w + nx]:
                    out[i * 4 + 3] = int(out[i * 4 + 3] * 0.45)
                    break
    return out


def cream_halo(w, h, rgba):
    n = w * h
    mark = bytearray(n)
    q = collections.deque()

    def cream(i):
        if rgba[i * 4 + 3] < 8:
            return True
        r, g, b = rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2]
        return (r + g + b) / 3 >= 165 and max(r, g, b) - min(r, g, b) <= 55

    def push(i):
        if not mark[i]:
            mark[i] = 1
            q.append(i)

    for y in range(h):
        for x in range(w):
            i = y * w + x
            if rgba[i * 4 + 3] >= 8:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h:
                    ni = ny * w + nx
                    if not mark[ni] and cream(ni):
                        push(ni)
    while q:
        i = q.popleft()
        x, y = i % w, i // w
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h:
                ni = ny * w + nx
                if not mark[ni] and cream(ni):
                    push(ni)
    if inner_keep(w, h, rgba, mark) < 0.08:
        return rgba
    return apply(rgba, mark, w, h)


def punch(w, h, rgba):
    if corners_transparent(rgba, w, h):
        return rgba, "skip"
    plates = corner_plates(rgba, w, h)
    cm = color_flood(w, h, rgba, plates, 44)
    em = edge_flood(w, h, rgba, 16)
    cf, ci = sum(cm) / len(cm), inner_keep(w, h, rgba, cm)
    ef, ei = sum(em) / len(em), inner_keep(w, h, rgba, em)
    c_ok = ci >= 0.08 and 0.18 <= cf <= 0.96
    e_ok = ei >= 0.05 and 0.25 <= ef <= 0.97
    if c_ok and ci >= 0.90 and cf >= 0.25:
        pick, mark = "color", cm
    elif c_ok and ef >= 0.94 and cf >= 0.75:
        pick, mark = "color", cm
    elif e_ok and ef > cf + 0.05 and ei >= 0.10:
        pick, mark = "edge", em
    elif c_ok:
        pick, mark = "color", cm
    elif e_ok:
        pick, mark = "edge", em
    else:
        pick, mark = ("color", cm) if ci >= ei else ("edge", em)
    out = apply(rgba, mark, w, h)
    if is_cream(plates):
        out = cream_halo(w, h, out)
    return out, pick


def main() -> None:
    for rel in TARGETS:
        path = ROOT / rel
        w, h, rgba, color = decode_rgba(path)
        out, pick = punch(w, h, rgba)
        write_png(path, w, h, out)
        trans = sum(1 for i in range(3, len(out), 4) if out[i] < 8)
        print(f"{rel:42} {w}x{h} ct{color}->{6} pick={pick:5} trans={100 * trans / (w * h):5.1f}%")


if __name__ == "__main__":
    main()
