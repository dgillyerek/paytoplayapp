#!/usr/bin/env python3
"""Weld-census leg ghosts + rest sole Y on sir_aldric_meshy.mesh.txt.

Does not skin. Bind NOT claimed.
"""
from __future__ import annotations

import json
from collections import defaultdict, deque
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
MESH = PACK3D / "sir_aldric_meshy.mesh.txt"
LEG = {"UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R"}
LEG_L = {"UpLeg_L", "Leg_L", "Foot_L"}
LEG_R = {"UpLeg_R", "Leg_R", "Foot_R"}


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
            verts.append((x, y, z, prim, wts))
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
            "thin": bool(min(dx, dy, dz) < 0.014 and max(dx, dy, dz) > 0.08),
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

    def in_leg_corridor(i):
        x, y, z, prim, _w = merged[i]
        if prim == "Scabbard":
            return False
        # hip→foot, outboard of tabard center
        return y < 0.98 and abs(x) > 0.07 and abs(z) < 0.28

    leg_only = islands(merged, adj, lambda i: merged[i][3] in LEG)
    leg_l = islands(merged, adj, lambda i: merged[i][3] in LEG_L)
    leg_r = islands(merged, adj, lambda i: merged[i][3] in LEG_R)
    leftover = islands(
        merged, adj,
        lambda i: in_leg_corridor(i) and merged[i][3] not in LEG and merged[i][3] != "Scabbard",
    )
    paper = [x for x in leftover if x["thin"] or (x["n"] < 120 and x["ymax"] < 0.70)]
    soles = [v[1] for v in merged if v[3] in {"Foot_L", "Foot_R"}]
    hips_low = [v[1] for v in merged if v[3] == "Hips" and abs(v[0]) > 0.08 and v[1] < 0.50]

    print("weld", len(verts), "->", n)
    print("UpLeg/Leg/Foot components", len(leg_only))
    for a in leg_only[:12]:
        print(" ", a)
    print("L components", len(leg_l))
    for a in leg_l[:6]:
        print("  L", a)
    print("R components", len(leg_r))
    for a in leg_r[:6]:
        print("  R", a)
    print("leftover corridor (not leg-bone)", len(leftover), "paperish", len(paper))
    for a in leftover[:10]:
        print("  leftover", a)
    print("foot prim ymin", round(min(soles), 4) if soles else None, "ymax", round(max(soles), 4) if soles else None)
    print("hips-in-corridor y<0.50 n", len(hips_low), "ymin", round(min(hips_low), 4) if hips_low else None)

    cause = (
        f"UpLeg/Leg/Foot are {len(leg_only)} welded components "
        f"(L={len(leg_l)} R={len(leg_r)}; expect 1+1). "
        f"Leftover non-leg islands in the hip→foot corridor: {len(leftover)} "
        f"(paperish {len(paper)}). "
        f"Rest Foot_* ymin={round(min(soles), 3) if soles else None} vs ground Y=0."
    )
    note = {
        "tipEyed": "2820e53",
        "weldVerts": n,
        "rawCorners": len(verts),
        "legBoneComponents": len(leg_only),
        "legL": len(leg_l),
        "legR": len(leg_r),
        "legBoneTop": leg_only[:8],
        "legLTop": leg_l[:6],
        "legRTop": leg_r[:6],
        "leftoverCorridor": len(leftover),
        "leftoverTop": leftover[:8],
        "paperish": len(paper),
        "footYmin": round(min(soles), 4) if soles else None,
        "footYmax": round(max(soles), 4) if soles else None,
        "verdict": {
            "expectOneVolumePerSide": 2,
            "cause": cause,
            "fix": "delete leftover hip/skirt paper in the leg corridor; keep ONE connected L + ONE connected R volume; plant soles on Y=0",
        },
    }
    print("CAUSE", cause)
    dest = PACK3D / "sir_aldric_leg_ghost_census.json"
    dest.write_text(json.dumps(note, indent=2) + "\n")
    print("wrote", dest)


if __name__ == "__main__":
    main()
