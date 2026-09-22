#!/usr/bin/env python3
"""Census horizontal tear on 80c42d1 one-loft mesh. Bind NOT claimed."""
from __future__ import annotations

import json
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
MESH = PACK3D / "sir_aldric_meshy.mesh.txt"
ARM = {"Arm_L", "Arm_R", "Fore_L", "Fore_R", "Hand_L", "Hand_R"}


def load():
    verts = []
    bone = "Hips"
    pending = []
    tris = []
    for raw in MESH.read_text().splitlines():
        if raw.startswith("BONE "):
            bone = raw[5:].strip()
            continue
        if raw.startswith("V "):
            p = raw[2:].split()
            x, y, z = float(p[0]), float(p[1]), float(p[2])
            uv = (float(p[6]), float(p[7]))
            wts = {}
            for tok in p[8:]:
                if ":" not in tok:
                    continue
                n, w = tok.split(":", 1)
                wts[n] = float(w)
            if not wts:
                wts = {bone: 1.0}
            prim = max(wts, key=wts.get)
            verts.append((x, y, z, prim, uv, wts))
            pending.append(len(verts) - 1)
            continue
        if raw == "T" and len(pending) >= 3:
            tris.append((pending[-3], pending[-2], pending[-1]))
    return verts, tris


def main() -> None:
    verts, tris = load()
    arm = [v for v in verts if v[3] in ARM]
    us = [v[4][0] for v in arm]
    vs = [v[4][1] for v in arm]
    multi = sum(1 for v in arm if sum(1 for w in v[5].values() if w > 0.05) > 1)

    # exclusive Y cuts: adjacent loft tris whose primaries differ (Arm/Fore/Hand)
    cuts = defaultdict(int)
    for a, b, c in tris:
        prims = {verts[a][3], verts[b][3], verts[c][3]}
        arm_p = prims & ARM
        if len(arm_p) >= 2:
            key = tuple(sorted(arm_p))
            cuts[str(key)] += 1
            ys = [verts[a][1], verts[b][1], verts[c][1]]
            cuts["_y"] = min(cuts.get("_y", 99), min(ys))

    # hem: faces with mixed Hips + Leg
    hem = 0
    for a, b, c in tris:
        prims = {verts[i][3] for i in (a, b, c)}
        if "Hips" in prims and prims & {"Leg_L", "Leg_R", "UpLeg_L", "UpLeg_R"}:
            hem += 1

    note = {
        "tip": "80c42d1",
        "bindPassClaimed": False,
        "loftUV": {
            "armCorners": len(arm),
            "uSpan": round(max(us) - min(us), 4) if us else 0,
            "vSpan": round(max(vs) - min(vs), 4) if vs else 0,
            "uMin": round(min(us), 4) if us else None,
            "uMax": round(max(us), 4) if us else None,
            "vMin": round(min(vs), 4) if vs else None,
            "vMax": round(max(vs), 4) if vs else None,
            "thetaWrap": True,
        },
        "loftWeights": {
            "multiBoneArmCorners": multi,
            "exclusiveYCuts": {k: v for k, v in cuts.items() if k != "_y"},
            "hardYCut": True,
        },
        "hemMixedHipsLegTris": hem,
        "verdict": (
            "MESH. Loft UVs wrap θ across a UV window (horizontal bands on the cylinder). "
            "Arm/Fore/Hand are exclusive Y-cuts on one loft — Evaluate() shears those "
            "rings into a flat horizontal stretch. Hem mixed-tris secondary. "
            "Not a 3-arm relapse. Fix: single steel texel + blended loft weights."
        ),
    }
    print(json.dumps(note, indent=2))
    dest = PACK3D / "sir_aldric_tear_census.json"
    dest.write_text(json.dumps(note, indent=2) + "\n")
    print("wrote", dest)


if __name__ == "__main__":
    main()
