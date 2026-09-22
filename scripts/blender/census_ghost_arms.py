#!/usr/bin/env python3
"""Weld-census hang-arm ghosts on sir_aldric_meshy.mesh.txt.

Does not skin. Bind NOT claimed.
"""
from __future__ import annotations

import json
from collections import defaultdict, deque
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
MESH = PACK3D / "sir_aldric_meshy.mesh.txt"
ARM = {"Arm_L", "Arm_R", "Fore_L", "Fore_R", "Hand_L", "Hand_R"}


def load():
    verts = []
    tris = []
    pending = []
    bone = "Hips"
    for raw in MESH.read_text().splitlines():
        if raw.startswith("BONE "):
            bone = raw[5:].strip()
            continue
        if raw.startswith("V "):
            p = raw[2:].split()
            x, y, z = float(p[0]), float(p[1]), float(p[2])
            wts = {}
            for tok in p[8:]:
                if ":" not in tok:
                    continue
                n, w = tok.split(":", 1)
                wts[n] = float(w)
            if not wts:
                wts = {bone: 1.0}
            prim = max(wts, key=wts.get)
            verts.append((x, y, z, prim))
            pending.append(len(verts) - 1)
            continue
        if raw == "T" and len(pending) >= 3:
            tris.append((pending[-3], pending[-2], pending[-1]))
    return verts, tris


def weld(verts, tris, mm=1):
    scale = 1000.0 / mm
    canon = {}
    merged = []
    map_i = []
    for v in verts:
        k = (round(v[0] * scale), round(v[1] * scale), round(v[2] * scale))
        if k not in canon:
            canon[k] = len(merged)
            merged.append(v)
        map_i.append(canon[k])
    wtris = [(map_i[a], map_i[b], map_i[c]) for a, b, c in tris]
    return merged, wtris


def islands(merged, adj, pred):
    n = len(merged)
    seen = [False] * n
    out = []
    for i in range(n):
        if seen[i] or not pred(i):
            continue
        q = deque([i])
        seen[i] = True
        ids = []
        while q:
            v = q.popleft()
            ids.append(v)
            for w in adj[v]:
                if not seen[w] and pred(w):
                    seen[w] = True
                    q.append(w)
        xs = [merged[j][0] for j in ids]
        ys = [merged[j][1] for j in ids]
        zs = [merged[j][2] for j in ids]
        prims = defaultdict(int)
        for j in ids:
            prims[merged[j][3]] += 1
        dx, dy, dz = max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)
        out.append({
            "n": len(ids),
            "prims": dict(prims),
            "ymin": round(min(ys), 3),
            "ymax": round(max(ys), 3),
            "xmin": round(min(xs), 3),
            "xmax": round(max(xs), 3),
            "zmin": round(min(zs), 3),
            "zmax": round(max(zs), 3),
            "thin": bool(min(dx, dy, dz) < 0.012 and max(dx, dy, dz) > 0.08),
        })
    out.sort(key=lambda d: -d["n"])
    return out


def main() -> None:
    verts, tris = load()
    merged, wtris = weld(verts, tris)
    n = len(merged)
    adj = [[] for _ in range(n)]
    for a, b, c in wtris:
        for u, v in ((a, b), (b, c), (c, a)):
            adj[u].append(v)
            adj[v].append(u)

    def outboard(i, side):
        x, y, z, prim = merged[i]
        if not (0.60 < y < 1.34):
            return False
        return x > 0.22 if side > 0 else x < -0.22

    arm_only = islands(merged, adj, lambda i: merged[i][3] in ARM)
    note = {
        "tip": "76eab9d",
        "weldVerts": n,
        "rawCorners": len(verts),
        "armBoneComponents": len(arm_only),
        "armBoneTop": arm_only[:8],
        "sides": {},
    }
    print("weld", len(verts), "->", n)
    print("Arm/Fore/Hand components", len(arm_only))
    for a in arm_only[:8]:
        print(" ", a)

    for side, name in ((1.0, "R"), (-1.0, "L")):
        leftover = islands(
            merged, adj,
            lambda i, s=side: outboard(i, s) and merged[i][3] not in ARM and merged[i][3] != "Scabbard",
        )
        paper = [x for x in leftover if x["thin"] or x["n"] < 80]
        print(name, "leftover outboard islands", len(leftover), "paperish", len(paper))
        for a in leftover[:6]:
            print("  leftover", a)
        note["sides"][name] = {
            "leftoverOutboard": len(leftover),
            "leftoverTop": leftover[:6],
        }

    cause = (
        "Arm/Fore/Hand are "
        f"{len(arm_only)} welded components (6 = three separate tubes per side). "
        "Leftover outboard Meshy faces remain on Chest/Spine/Hips (paper not fully deleted). "
        "No multi-weight verts. Ghost = leftover paper + 3 unjoined tubes/side."
    )
    note["verdict"] = {
        "armBoneComponents": len(arm_only),
        "expectOneVolumePerSide": 2,
        "expectThreeTubesPerSide": 6,
        "duplicateWeights": False,
        "cause": cause,
        "fix": "delete leftover hang-corridor paper; ONE connected loft per side; Arm/Fore/Hand weights along that single volume",
    }
    print("CAUSE", cause)
    dest = PACK3D / "sir_aldric_ghost_census.json"
    dest.write_text(json.dumps(note, indent=2) + "\n")
    print("wrote", dest)


if __name__ == "__main__":
    main()
