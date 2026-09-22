#!/usr/bin/env python3
"""Rerunnable Path 2 bind census — islands, bone verts, hang vs A-pose.

Does not skin. Does not claim walk PASS. Writes:
  Assets/ThemePack/.../3d/sir_aldric_meshy_census.json
  Docs/Survival/previews/gate3/CENSUS_BIND.txt
"""
from __future__ import annotations

import json
import math
from collections import defaultdict, deque
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
SRC_GLB = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AI_MESH_PATH2/out/aldric_meshy_retopo.glb"
PROOF = ROOT / "Docs/Survival/previews/gate3"
TARGET_H = 1.86

# Actor hang chain (world). Bone local +Y is world +Y; the ARM CHAIN is −Y.
HANG_ARM_R = Vector((0.22, 1.36, 0.0)), Vector((0.22, 0.84, 0.0))  # shoulder → hand
HANG_ARM_L = Vector((-0.22, 1.36, 0.0)), Vector((-0.22, 0.84, 0.0))
HANG_DIR = Vector((0.0, -1.0, 0.0))


def import_raw():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SRC_GLB))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    me = ob.data
    for v in me.vertices:
        x, y, z = v.co
        v.co = Vector((-x, z, -y))
    ys = [v.co.y for v in me.vertices]
    ymin, ymax = min(ys), max(ys)
    scale = TARGET_H / max(1e-6, ymax - ymin)
    for v in me.vertices:
        v.co = Vector((v.co.x * scale, (v.co.y - ymin) * scale, v.co.z * scale))
    me.update()
    return ob


def islands(me):
    n = len(me.vertices)
    adj = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)
    seen = [False] * n
    out = []
    for i in range(n):
        if seen[i]:
            continue
        q = deque([i])
        seen[i] = True
        ids = []
        while q:
            v = q.popleft()
            ids.append(v)
            for w in adj[v]:
                if not seen[w]:
                    seen[w] = True
                    q.append(w)
        xs = [me.vertices[j].co.x for j in ids]
        ys = [me.vertices[j].co.y for j in ids]
        zs = [me.vertices[j].co.z for j in ids]
        out.append({
            "n": len(ids),
            "ids": ids,
            "min": (min(xs), min(ys), min(zs)),
            "max": (max(xs), max(ys), max(zs)),
            "c": (sum(xs) / len(ids), sum(ys) / len(ids), sum(zs) / len(ids)),
            "span": (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)),
        })
    out.sort(key=lambda x: -x["n"])
    return out


def sheath_like(isle) -> bool:
    """Character-RIGHT elongated island in the sheathed-sword box."""
    cx, cy, cz = isle["c"]
    sx, sy, sz = isle["span"]
    if cx < 0.10:
        return False
    if not (0.15 < cy < 1.20):
        return False
    long_y = sy > 0.18 and sy > sx * 0.7
    thin = min(sx, sz) < 0.14
    return long_y and (thin or sx > 0.08)


def pca_axis(pts: list[Vector]) -> Vector:
    if len(pts) < 4:
        return Vector((0, -1, 0))
    c = Vector((0, 0, 0))
    for p in pts:
        c += p
    c /= len(pts)
    xx = xy = xz = yy = yz = zz = 0.0
    for p in pts:
        d = p - c
        xx += d.x * d.x
        xy += d.x * d.y
        xz += d.x * d.z
        yy += d.y * d.y
        yz += d.y * d.z
        zz += d.z * d.z
    # power iteration
    v = Vector((1, 0, 0))
    for _ in range(24):
        nv = Vector((xx * v.x + xy * v.y + xz * v.z,
                     xy * v.x + yy * v.y + yz * v.z,
                     xz * v.x + yz * v.y + zz * v.z))
        if nv.length < 1e-9:
            break
        v = nv.normalized()
    return v


def mesh_arm_dir(me, side: float) -> dict:
    pts = []
    for v in me.vertices:
        p = v.co
        if p.x * side <= 0:
            continue
        if 0.55 < p.y < 1.48 and abs(p.x) > 0.16:
            pts.append(p.copy())
    axis = pca_axis(pts)
    # flip so it points toward the hand (lower Y or outward X)
    if side > 0 and axis.x < 0:
        axis = -axis
    if side < 0 and axis.x > 0:
        axis = -axis
    # prefer the direction that matches the dominant extent
    xs = [p.x for p in pts]
    ys = [p.y for p in pts]
    if pts and (max(xs) - min(xs) < max(ys) - min(ys)) and axis.y > 0:
        axis = -axis
    hang = HANG_DIR
    dot = max(-1.0, min(1.0, axis.dot(hang)))
    ang = math.degrees(math.acos(abs(dot)))
    if axis.dot(hang) < 0:
        ang = 180.0 - ang
    return {
        "n": len(pts),
        "axis": [round(axis.x, 4), round(axis.y, 4), round(axis.z, 4)],
        "bbox": {
            "x": [round(min(xs), 3), round(max(xs), 3)] if pts else None,
            "y": [round(min(ys), 3), round(max(ys), 3)] if pts else None,
        },
        "angle_to_hang_deg": round(ang, 2),
        "read": "A-POSE (+X)" if abs(axis.x) > abs(axis.y) else "HANG-ISH (−Y)",
    }


def bone_counts_from_mesh_txt() -> dict:
    path = PACK3D / "sir_aldric_meshy.mesh.txt"
    if not path.exists():
        return {}
    counts = defaultdict(int)
    bone = "?"
    for line in path.read_text().splitlines():
        if line.startswith("BONE "):
            bone = line[5:].strip()
        elif line.startswith("V "):
            counts[bone] += 1
    return dict(counts)


def main():
    ob = import_raw()
    me = ob.data
    raw = islands(me)
    sheath = [i for i in raw if sheath_like(i)]
    # overlap groups among sheath-like (duplicate swords)
    dups = []
    used = set()
    for a, ia in enumerate(sheath):
        if a in used:
            continue
        group = [ia]
        used.add(a)
        for b, ib in enumerate(sheath):
            if b in used:
                continue
            # same box if centroids close
            da = Vector(ia["c"]) - Vector(ib["c"])
            if da.length < 0.12:
                group.append(ib)
                used.add(b)
        if len(group) > 1:
            dups.append(group)

    def slim(isle):
        return {
            "n": isle["n"],
            "c": [round(x, 3) for x in isle["c"]],
            "min": [round(x, 3) for x in isle["min"]],
            "max": [round(x, 3) for x in isle["max"]],
            "span": [round(x, 3) for x in isle["span"]],
            "sheath_like": sheath_like(isle),
        }

    arm_r = mesh_arm_dir(me, 1.0)
    arm_l = mesh_arm_dir(me, -1.0)
    hang_chain_r = (HANG_ARM_R[1] - HANG_ARM_R[0]).normalized()
    note = {
        "premiseFailedTwice": "rigid Bone1 / corridor weights on A-pose Meshy shell driven by hang Evaluate() keys",
        "verts": len(me.vertices),
        "faces": len(me.polygons),
        "islands": len(raw),
        "islandSizesTop": [i["n"] for i in raw[:20]],
        "sheathLikeIslands": [slim(i) for i in sheath[:30]],
        "sheathLikeCount": len(sheath),
        "sheathDuplicateGroups": [
            {"members": [slim(x) for x in g], "keepLargest": max(g, key=lambda x: x["n"])["n"]}
            for g in dups
        ],
        "actorHang": {
            "Arm_R_chain": [0.0, -1.0, 0.0],
            "boneLocalPlusY": [0.0, 1.0, 0.0],
            "note": "Evaluate() localRotation assumes hang rest (arms along −Y). Bone gizmos are +Y stubs.",
        },
        "meshArmR": arm_r,
        "meshArmL": arm_l,
        "mismatch": {
            "Arm_R_deg": arm_r["angle_to_hang_deg"],
            "Arm_L_deg": arm_l["angle_to_hang_deg"],
            "failedPremise": abs(arm_r["angle_to_hang_deg"]) > 25,
        },
        "currentMeshTxtBoneVerts": bone_counts_from_mesh_txt(),
    }
    out_json = PACK3D / "sir_aldric_meshy_census.json"
    out_json.write_text(json.dumps(note, indent=2) + "\n")
    lines = [
        "PATH 2 BIND CENSUS (raw import, no weld, no skin)",
        f"verts={note['verts']} faces={note['faces']} islands={note['islands']}",
        f"top island sizes: {note['islandSizesTop']}",
        f"sheath-like islands: {note['sheathLikeCount']}",
    ]
    for i, s in enumerate(note["sheathLikeIslands"][:12]):
        lines.append(f"  sheath[{i}] n={s['n']} c={s['c']} span={s['span']}")
    lines.append(f"duplicate sheath groups: {len(note['sheathDuplicateGroups'])}")
    for g in note["sheathDuplicateGroups"]:
        lines.append(f"  group n={[m['n'] for m in g['members']]} keep={g['keepLargest']}")
    lines.append(f"Actor hang Arm_R chain: {note['actorHang']['Arm_R_chain']}  bone local +Y: {note['actorHang']['boneLocalPlusY']}")
    lines.append(f"Mesh Arm_R PCA {arm_r['axis']}  {arm_r['read']}  angle_to_hang={arm_r['angle_to_hang_deg']} deg  n={arm_r['n']}")
    lines.append(f"Mesh Arm_L PCA {arm_l['axis']}  {arm_l['read']}  angle_to_hang={arm_l['angle_to_hang_deg']} deg  n={arm_l['n']}")
    lines.append(f"MISMATCH (need pose-to-hang before bind): {note['mismatch']}")
    lines.append("current mesh.txt bone verts:")
    for k, v in sorted(note["currentMeshTxtBoneVerts"].items(), key=lambda kv: -kv[1]):
        lines.append(f"  {k:10s} {v}")
    lines.append("OLD PREMISE (failed 89481e5 + 71f0c4a): Bone1 corridor on A-pose shell + hang Evaluate().")
    lines.append("NEW PREMISE (next): A) apply mesh into hang rest, then heat/multi-bone weights; delete dup sheath at GEO.")
    text = "\n".join(lines) + "\n"
    PROOF.mkdir(parents=True, exist_ok=True)
    (PROOF / "CENSUS_BIND.txt").write_text(text)
    print(text)
    print("wrote", out_json)


if __name__ == "__main__":
    main()
