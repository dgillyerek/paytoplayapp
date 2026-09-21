#!/usr/bin/env python3
"""Hero look = e5b132f Meshy paint PASS. No voxel remesh. No capsule arms.

Derek STOP: remesh/capsule Game-view threw away the painted knight.
cc77d8a mesh-vs-capture: A Actor LBS + B Blender + C MP4 n=10 all tear
at mid-swing → MESH (thin XY hang-arm cards + solidify rims), not capture.
This bind keeps Path 2 GLB verts/UVs/albedo and curls Arm_* plates around
the hang-arm axis so mid-swing is a tube, not a card. No exclusive-weight
iterate. Evaluate() 5916447 reused. Bind/walk NOT claimed.
"""
from __future__ import annotations

import json
import math
import os
import sys
from collections import defaultdict, deque
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import skin_sir_aldric_hangheat as hh
import skin_sir_aldric_meshy as old

ROOT = old.ROOT
PACK3D = old.PACK3D
MESH_BONES = old.MESH_BONES
PROOF = old.PROOF

_SCAB_A = hh._SCAB_A
_SCAB_B = hh._SCAB_B
_ARM_R_A = old._ARM_R_A
_ARM_R_B = old._ARM_R_B
_ARM_L_A = old._ARM_L_A
_ARM_L_B = old._ARM_L_B


def dist_seg(p, a, b):
    return hh.dist_seg(p, a, b)


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
        ids = [i]
        while q:
            v = q.popleft()
            for w in adj[v]:
                if not seen[w]:
                    seen[w] = True
                    q.append(w)
                    ids.append(w)
        out.append(ids)
    out.sort(key=len, reverse=True)
    return out


def sheath_frac(me, ids):
    hit = 0
    for i in ids:
        p = me.vertices[i].co
        if p.x > 0.13 and 0.20 < p.y < 1.20 and dist_seg(p, _SCAB_A, _SCAB_B) < 0.080:
            hit += 1
    return hit / max(1, len(ids))


def delete_extra_sheath(ob) -> dict:
    """Keep one sheath island. Delete other sheath-like islands at GEO."""
    me = ob.data
    isls = islands(me)
    sheath = [ids for ids in isls if 24 <= len(ids) <= 6000 and sheath_frac(me, ids) >= 0.55]
    sheath.sort(key=len, reverse=True)
    print("sheath islands", [len(s) for s in sheath[:10]], "of", len(isls), "mesh islands")
    drop = set()
    keep = set(sheath[0]) if sheath else set()
    for ids in sheath[1:]:
        drop.update(ids)
    if drop:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.verts[i] for i in drop if i < len(bm.verts)], context="VERTS")
        bm.to_mesh(me)
        bm.free()
        me.update()
    print("sheath keep", len(keep), "drop", len(drop))
    return {"sheathKeep": len(keep), "sheathDrop": len(drop), "sheathN": len(sheath), "meshIslands": len(isls)}


def delete_armpit_elbow_shards(ob) -> dict:
    """Only hairline slivers in the pit/elbow — never the Meshy gauntlet/plate."""
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    kill = []
    for f in bm.faces:
        c = f.calc_center_median()
        area = f.calc_area()
        edges = [e.calc_length() for e in f.edges]
        aspect = max(edges) / max(min(edges), 1e-6)
        pit = 1.22 < c.y < 1.40 and 0.16 < abs(c.x) < 0.24 and abs(c.z) < 0.08
        if pit and area < 0.00025 and aspect > 12:
            kill.append(f)
    print("shard faces", len(kill), "of", len(bm.faces))
    if kill:
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    me.update()
    return {"shardFaces": len(kill), "vertsAfter": len(me.vertices)}


def _wrap_fade(y: float) -> float:
    """Full curl on the hanging tube; none at pauldron / hand."""
    if y >= 1.32 or y <= 0.68:
        return 0.0
    if y > 1.24:
        return (1.32 - y) / 0.08
    if y < 0.78:
        return (y - 0.68) / 0.10
    return 1.0


def _arm_face_islands(me, arm_vert):
    """Connected Arm_* face groups (stacked Meshy lames are separate islands)."""
    adj = [[] for _ in range(len(me.polygons))]
    vert_faces = defaultdict(list)
    for fi, p in enumerate(me.polygons):
        if not all(arm_vert[i] for i in p.vertices):
            continue
        for vi in p.vertices:
            vert_faces[vi].append(fi)
    arm_faces = [fi for fi, p in enumerate(me.polygons) if all(arm_vert[i] for i in p.vertices)]
    for faces in vert_faces.values():
        for a in faces:
            for b in faces:
                if a != b:
                    adj[a].append(b)
    seen = set()
    out = []
    for fi in arm_faces:
        if fi in seen:
            continue
        q = deque([fi])
        seen.add(fi)
        ids = [fi]
        while q:
            cur = q.popleft()
            for w in adj[cur]:
                if w not in seen:
                    seen.add(w)
                    q.append(w)
                    ids.append(w)
        out.append(ids)
    out.sort(key=len, reverse=True)
    return out


def drop_stacked_arm_cards(ob) -> dict:
    """Keep the outer painted hang-arm plate per side. Delete inner slat islands.

    Mid-swing gold bands are stacked Meshy lames (not capture). Hands / pauldrons
    stay. Same atlas UVs on the kept plate.
    """
    me = ob.data
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    arm_vert = [False] * len(me.vertices)
    side_of = [0] * len(me.vertices)
    for i, v in enumerate(me.vertices):
        names = {vg_names.get(g.group, "") for g in v.groups if g.weight > 0.5}
        if "Arm_R" in names:
            arm_vert[i] = True
            side_of[i] = 1
        elif "Arm_L" in names:
            arm_vert[i] = True
            side_of[i] = -1
    islands = _arm_face_islands(me, arm_vert)
    print("arm face islands", [len(x) for x in islands[:12]], "of", len(islands))

    def island_meta(ids):
        cs = [me.polygons[i].center for i in ids]
        ys = [c.y for c in cs]
        xs = [c.x for c in cs]
        cy = sum(ys) / len(ys)
        cx = sum(xs) / len(xs)
        votes = 0
        for fi in ids:
            for vi in me.polygons[fi].vertices:
                votes += side_of[vi]
        side = 1 if votes >= 0 else -1
        a, b = (_ARM_R_A, _ARM_R_B) if side > 0 else (_ARM_L_A, _ARM_L_B)
        rs = [dist_seg(c, a, b) for c in cs]
        return {
            "ids": ids,
            "n": len(ids),
            "y": cy,
            "x": cx,
            "votes": votes,
            "side": side,
            "r": sum(rs) / len(rs),
            "hand": cy < 0.80,
            "pauldron": cy > 1.26,
        }

    metas = [island_meta(ids) for ids in islands if ids]
    for m in metas:
        print(
            "arm island n", m["n"], "side", m["side"], "y", round(m["y"], 3),
            "x", round(m["x"], 3), "r", round(m["r"], 3), "votes", m["votes"],
            "hand" if m["hand"] else ("pauldron" if m["pauldron"] else "mid"),
        )
    kill = []
    kept = {"Arm_R": 0, "Arm_L": 0, "hand": 0, "pauldron": 0, "drop": 0}
    for side in (1, -1):
        mid = [m for m in metas if m["side"] == side and not m["hand"] and not m["pauldron"] and m["n"] >= 8]
        mid.sort(key=lambda m: -m["n"])
        primary_n = mid[0]["n"] if mid else 0
        for i, m in enumerate(mid):
            key = "Arm_R" if side > 0 else "Arm_L"
            # Never delete a near-primary island (that ate the other arm once).
            # Only drop small inner slats.
            if i == 0 or m["n"] >= max(80, 0.25 * primary_n):
                kept[key] += m["n"]
                continue
            kill.extend(m["ids"])
            kept["drop"] += m["n"]
        for m in metas:
            if m["side"] != side:
                continue
            if m["hand"]:
                kept["hand"] += m["n"]
            elif m["pauldron"]:
                kept["pauldron"] += m["n"]
    print("drop stacked arm cards", kept, "kill faces", len(kill))
    if kill:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in kill if i < len(bm.faces)], context="FACES")
        bm.to_mesh(me)
        bm.free()
        me.update()
    return {"islands": len(islands), "killFaces": len(kill), **kept}


def wrap_hang_arm_plates(ob, theta_span_deg=300.0) -> dict:
    """Curl the kept Arm_* plate around the hang-arm axis. Same verts / UVs.

    Stacked lames are dropped first. Across-plate width → θ. Constant r so
    leftover stack cannot read as gold slats. Gap faces the ribs.
    """
    me = ob.data
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    sides = {
        "Arm_R": (_ARM_R_A, _ARM_R_B, Vector((1.0, 0.0, 0.0))),
        "Arm_L": (_ARM_L_A, _ARM_L_B, Vector((-1.0, 0.0, 0.0))),
    }
    back = Vector((0.0, 0.0, -1.0))
    half = math.radians(theta_span_deg) * 0.5
    moved = {"Arm_R": 0, "Arm_L": 0}
    stats = {}

    for gname, (a, b, outward) in sides.items():
        rows = []
        for v in me.vertices:
            names = {vg_names.get(g.group, "") for g in v.groups if g.weight > 0.5}
            if gname not in names:
                continue
            fade = _wrap_fade(v.co.y)
            if fade <= 1e-4:
                continue
            ab = b - a
            t = max(0.0, min(1.0, (v.co - a).dot(ab) / max(1e-9, ab.length_squared)))
            c = a + t * ab
            off = v.co - c
            u = off.dot(outward)
            w = off.dot(back)
            rows.append((v, fade, c, u, w, off.length))
        if len(rows) < 12:
            print("wrap skip", gname, "n", len(rows))
            continue
        bands = defaultdict(list)
        for row in rows:
            bands[int(row[0].co.y * 25.0)].append(row)
        us = [r[3] for r in rows]
        rs = [r[5] for r in rows]
        stats[gname] = {
            "n": len(rows),
            "u": (round(min(us), 3), round(max(us), 3)),
            "r": (round(min(rs), 3), round(max(rs), 3)),
        }
        for band in bands.values():
            bu = [r[3] for r in band]
            u_lo, u_hi = min(bu), max(bu)
            span = max(0.018, u_hi - u_lo)
            # Size r so the plate width covers the arc — a shallow 200°
            # card still reads as slats when the swing turns the gap to camera.
            r_tube = max(0.034, min(0.050, span / max(1e-3, 2.0 * half)))
            for v, fade, c, u, w, r0 in band:
                u_n = (2.0 * (u - u_lo) / span) - 1.0
                u_n = max(-1.0, min(1.0, u_n))
                theta = u_n * half
                target = c + r_tube * (math.cos(theta) * outward + math.sin(theta) * back)
                v.co = v.co.lerp(target, fade)
                moved[gname] += 1

    me.update()
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        names = set()
        for vi in p.vertices:
            names |= {vg_names.get(g.group, "") for g in me.vertices[vi].groups if g.weight > 0.5}
        if names & {"Arm_L", "Arm_R"}:
            p.use_smooth = True
    me.update()
    print("wrap hang-arm", moved, "span", theta_span_deg, "stats", stats)
    return {"moved": moved, "thetaSpanDeg": theta_span_deg, "stats": stats, "vertsAfter": len(me.vertices)}


def _in_leg_tube(p):
    if p.y > 0.96 or abs(p.x) < 0.08:
        return False
    if p.x > 0:
        return dist_seg(p, old._LEG_R_A, old._LEG_R_B) < 0.078
    return dist_seg(p, old._LEG_L_A, old._LEG_L_B) < 0.078


def assign_exclusive(mesh_ob):
    """Cloth/hem first (never steals leg tubes). Distal hang-arms → Arm_*. One scabbard."""
    me = mesh_ob.data
    n = len(me.vertices)
    rgb_of, lion = old._sample_albedo(me)
    adj = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)

    cloth = [False] * n
    for i, v in enumerate(me.vertices):
        p = v.co
        if _in_leg_tube(p):
            continue
        d_r = dist_seg(p, _ARM_R_A, _ARM_R_B)
        d_l = dist_seg(p, _ARM_L_A, _ARM_L_B)
        distal = ((d_r < 0.048 and p.x > 0.27) or (d_l < 0.048 and p.x < -0.27)) and p.y < 1.18
        if (lion[i] or old._is_blue(rgb_of[i])) and not distal:
            cloth[i] = True
        if 0.42 <= p.y <= 1.30 and abs(p.x) < 0.23 and abs(p.z) < 0.28:
            cloth[i] = True
        if 0.38 <= p.y <= 0.78 and abs(p.x) < 0.26 and abs(p.z) < 0.32:
            cloth[i] = True

    def flood(seeds, pred):
        seen = set()
        q = deque(seeds)
        while q:
            i = q.popleft()
            if i in seen:
                continue
            seen.add(i)
            for j in adj[i]:
                if j not in seen and pred(j):
                    q.append(j)
        return seen

    scab = set()
    for i, v in enumerate(me.vertices):
        p = v.co
        if cloth[i] or p.x < 0.15:
            continue
        if 0.22 < p.y < 1.18 and dist_seg(p, _SCAB_A, _SCAB_B) < 0.055:
            scab.add(i)
    scab = flood(
        list(scab),
        lambda i: (
            not cloth[i]
            and me.vertices[i].co.x > 0.14
            and 0.18 < me.vertices[i].co.y < 1.20
            and dist_seg(me.vertices[i].co, _SCAB_A, _SCAB_B) < 0.062
        ),
    )

    def arm_pred(side):
        a = _ARM_R_A if side > 0 else _ARM_L_A
        b = _ARM_R_B if side > 0 else _ARM_L_B

        def pred(i, a=a, b=b, side=side):
            if i in scab or cloth[i]:
                return False
            p = me.vertices[i].co
            if p.x * side < 0.26:
                return False
            # hanging tube only — below armpit/pauldron, above hem
            if not (0.62 < p.y < 1.30):
                return False
            return dist_seg(p, a, b) < 0.066

        return pred

    arm_r = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x > 0.28 and 0.62 < v.co.y < 0.94 and i not in scab and not cloth[i]],
        arm_pred(1.0),
    )
    arm_l = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x < -0.28 and 0.62 < v.co.y < 0.94 and not cloth[i]],
        arm_pred(-1.0),
    )
    for i, v in enumerate(me.vertices):
        if i in scab or cloth[i]:
            continue
        p = v.co
        if p.x > 0.26 and 0.62 < p.y < 1.30 and dist_seg(p, _ARM_R_A, _ARM_R_B) < 0.070:
            arm_r.add(i)
        elif p.x < -0.26 and 0.62 < p.y < 1.30 and dist_seg(p, _ARM_L_A, _ARM_L_B) < 0.070:
            arm_l.add(i)

    bone_of = ["Hips"] * n
    for i, v in enumerate(me.vertices):
        p = v.co
        if i in scab:
            bone_of[i] = "Scabbard"
            continue
        if i in arm_r:
            bone_of[i] = "Arm_R"
            continue
        if i in arm_l:
            bone_of[i] = "Arm_L"
            continue
        if cloth[i]:
            bone_of[i] = "Chest" if p.y > 1.16 else ("Spine" if p.y > 1.04 else "Hips")
            continue
        # full hang-leg tubes hip→foot — hem lock must not Y-cut the shin
        if _in_leg_tube(p):
            if p.x > 0:
                bone_of[i] = "Foot_R" if p.y < 0.20 else ("Leg_R" if p.y < 0.50 else "UpLeg_R")
            else:
                bone_of[i] = "Foot_L" if p.y < 0.20 else ("Leg_L" if p.y < 0.50 else "UpLeg_L")
            continue
        if p.y > 1.52:
            bone_of[i] = "Head"
        elif p.y > 1.42 and abs(p.x) < 0.18:
            bone_of[i] = "Neck"
        elif p.y > 1.26:
            bone_of[i] = "Chest"
        elif p.y > 1.08:
            bone_of[i] = "Spine"
        else:
            bone_of[i] = "Hips"

    for g in list(mesh_ob.vertex_groups):
        if g.name in MESH_BONES:
            mesh_ob.vertex_groups.remove(g)
    groups = {name: mesh_ob.vertex_groups.new(name=name) for name in MESH_BONES}
    counts = defaultdict(int)
    for i, bone in enumerate(bone_of):
        groups[bone].add([i], 1.0, "REPLACE")
        counts[bone] += 1
    print(
        "exclusive", dict(counts),
        "cloth", sum(cloth), "scab", len(scab),
        "armR", len(arm_r), "armL", len(arm_l),
    )
    return dict(counts)


def recount_groups(ob):
    me = ob.data
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    counts = defaultdict(int)
    for v in me.vertices:
        best, bw = "Hips", -1.0
        for g in v.groups:
            name = vg_names.get(g.group, "")
            if name in MESH_BONES and g.weight > bw:
                best, bw = name, g.weight
        counts[best] += 1
    print("recount", dict(counts))
    return dict(counts)


def repair_arm_groups(ob):
    """Solidify verts in the hang-arm tubes get Arm_* if they lost groups."""
    groups = {g.name: g for g in ob.vertex_groups}
    for name in ("Arm_L", "Arm_R"):
        if name not in groups:
            groups[name] = ob.vertex_groups.new(name=name)
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    n = 0
    for i, v in enumerate(ob.data.vertices):
        names = {vg_names.get(g.group, "") for g in v.groups if g.weight > 0.5}
        if names & set(MESH_BONES):
            continue
        p = v.co
        if p.x > 0.22 and 0.70 < p.y < 1.38 and dist_seg(p, _ARM_R_A, _ARM_R_B) < 0.088:
            groups["Arm_R"].add([i], 1.0, "REPLACE")
            n += 1
        elif p.x < -0.22 and 0.70 < p.y < 1.38 and dist_seg(p, _ARM_L_A, _ARM_L_B) < 0.088:
            groups["Arm_L"].add([i], 1.0, "REPLACE")
            n += 1
    print("repair arm verts", n)
    return n


def export_mesh_txt_v4(mesh_ob):
    me = mesh_ob.data
    me.calc_loop_triangles()
    uv_layer = me.uv_layers.active
    vg_names = {g.index: g.name for g in mesh_ob.vertex_groups}

    def weights_of(vi):
        pairs = []
        for g in me.vertices[vi].groups:
            name = vg_names.get(g.group, "")
            if name in MESH_BONES and g.weight > 0.02:
                pairs.append((name, float(g.weight)))
        pairs.sort(key=lambda x: -x[1])
        pairs = pairs[:4]
        s = sum(w for _, w in pairs) or 1.0
        return [(n, w / s) for n, w in pairs] or [("Hips", 1.0)]

    bone_of = [weights_of(i)[0][0] for i in range(len(me.vertices))]
    lines = [
        "# SirAldric Path 2 Meshy-look e5b132f  clay-volume-law 5de0e16  scabbard=+X  blender",
        "FMT v4 blender gate3 meshy-path2 meshy-look-e5b132f",
    ]
    buckets = defaultdict(list)
    for tri in me.loop_triangles:
        bones = [bone_of[i] for i in tri.vertices]
        primary = max(set(bones), key=bones.count)
        rec = []
        for i, li in zip(tri.vertices, tri.loops):
            p = me.vertices[i].co
            nrm = me.vertices[i].normal
            if uv_layer:
                uv = uv_layer.data[li].uv
                uvt = (float(uv.x), float(uv.y))
            else:
                uvt = (0.5, 0.5)
            rec.append((p, nrm, uvt, weights_of(i)))
        buckets[primary].append(rec)
    for bone in MESH_BONES:
        tris = buckets.get(bone, [])
        if not tris:
            continue
        lines.append(f"BONE {bone}")
        for rec in tris:
            for p, nrm, uv, wts in rec:
                wt = " ".join(f"{bn}:{w:.3f}" for bn, w in wts)
                lines.append(
                    f"V {p.x:.5f} {p.y:.5f} {p.z:.5f} "
                    f"{nrm.x:.4f} {nrm.y:.4f} {nrm.z:.4f} "
                    f"{uv[0]:.4f} {uv[1]:.4f} {wt}"
                )
            lines.append("T")
    path = PACK3D / "sir_aldric_meshy.mesh.txt"
    path.write_text("\n".join(lines) + "\n")
    ntris = sum(len(v) for v in buckets.values())
    text = path.read_text()
    print("mesh.txt", path, "tris", ntris, "bytes", path.stat().st_size)
    if "FMT v4" not in text or "BONE Scabbard" not in text:
        raise SystemExit("mesh.txt missing FMT v4 / Scabbard")
    if "BONE Cape" in text:
        raise SystemExit("Cape mesh leaked")
    return ntris, {k: len(v) for k, v in buckets.items()}


def export_fbx(mesh_ob, actor_ob):
    path = PACK3D / "sir_aldric_path2_clean.fbx"
    bpy.ops.object.select_all(action="DESELECT")
    mesh_ob.select_set(True)
    actor_ob.select_set(True)
    bpy.context.view_layer.objects.active = actor_ob
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        apply_unit_scale=True,
        armature_nodetype="NULL",
        axis_forward="Z",
        axis_up="Y",
        use_armature_deform_only=True,
        mesh_smooth_type="FACE",
    )
    print("fbx", path, path.stat().st_size)
    return path


def main():
    if not old.SRC_GLB.exists():
        raise SystemExit(f"missing {old.SRC_GLB}")
    # Original Meshy + paint-PASS materials/UVs. No remesh.
    mesh_ob = hh.import_raw()
    mesh_ob.name = "SirAldricMeshy"
    sheath = delete_extra_sheath(mesh_ob)
    hh.weld(mesh_ob, 0.001)
    # weld can merge remaining sheath shards — re-delete extras
    sheath2 = delete_extra_sheath(mesh_ob)
    shards = delete_armpit_elbow_shards(mesh_ob)
    counts = assign_exclusive(mesh_ob)
    stacked = drop_stacked_arm_cards(mesh_ob)
    wrap = wrap_hang_arm_plates(mesh_ob)
    wrap["stacked"] = stacked
    repair_arm_groups(mesh_ob)
    counts = recount_groups(mesh_ob)
    if counts.get("Scabbard", 0) < 40:
        raise SystemExit(f"scabbard too few: {counts.get('Scabbard')}")
    if counts.get("Arm_R", 0) < 80 or counts.get("Arm_L", 0) < 80:
        raise SystemExit(f"arm empty: R={counts.get('Arm_R')} L={counts.get('Arm_L')}")
    if counts.get("Arm_R", 0) > 8000 or counts.get("Arm_L", 0) > 8000:
        raise SystemExit(f"arm stole tabard: R={counts.get('Arm_R')} L={counts.get('Arm_L')}")

    actor_ob, _world = hh.build_actor_armature()
    hh.attach_actor(mesh_ob, actor_ob)

    # Game-view FIRST on original Meshy UVs + Image_0 / lion-card mats.
    # combine_atlas_and_remap mutates UVs for Unity — doing it before
    # EEVEE would sample the wrong Image_0 and throw away e5b132f.
    old.setup_render()
    old.apply_pose(actor_ob, {
        "root_z": 0.0, "root_y": 0.0,
        "hips": (0, 0, 0), "spine": (0, 0, 0), "chest": (0, 0, 0), "head": (0, 0, 0),
        "up_l": (0, 0, 0), "leg_l": (0, 0, 0), "foot_l": (0, 0, 0),
        "up_r": (0, 0, 0), "leg_r": (0, 0, 0), "foot_r": (0, 0, 0),
        "arm_l": (0, 0, 0), "fore_l": (0, 0, 0),
        "arm_r": (0, 0, 0), "fore_r": (0, 0, 0),
        "hand_r": (0, 0, 0), "sword": (0, 0, 0),
    })
    rest = old.WALK
    rest.mkdir(parents=True, exist_ok=True)
    rest_p = PROOF / "world_meshy_hang_rest.png"
    bpy.context.scene.render.filepath = str(rest_p)
    bpy.ops.render.render(write_still=True)
    print("hang rest", rest_p, rest_p.stat().st_size)

    mp4 = old.render_walk(actor_ob)
    # mid-swing was the tell on 61e / c840 MP4 FAILs
    old.apply_pose(actor_ob, old.walk_pose(old.WALK_PERIOD * 0.625))
    mid_p = old.WALK / "world_walk_mid_swing.png"
    bpy.context.scene.render.filepath = str(mid_p)
    bpy.ops.render.render(write_still=True)
    print("mid swing", mid_p, mid_p.stat().st_size)
    art = Path("/opt/cursor/artifacts")
    art.mkdir(parents=True, exist_ok=True)
    (art / "gate3_world_walk_mid_swing.png").write_bytes(mid_p.read_bytes())
    (art / "world_meshy_hang_rest.png").write_bytes(rest_p.read_bytes())

    atlas = old.combine_atlas_and_remap(mesh_ob)
    ntris, buckets = export_mesh_txt_v4(mesh_ob)
    fbx = export_fbx(mesh_ob, actor_ob)
    blend = PACK3D / "sir_aldric_meshy_skinned.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "sourceLook": "e5b132f Meshy GLB + paint iterate (Image_0 bleed + SoT lion cards)",
        "albedoPreserved": "original GLB UVs kept; atlas = Image_0 + lion-card strip (combine_atlas_and_remap, same as e5b132f)",
        "retopo": "LOOK path unchanged (e5b132f GLB UVs + Image_0/lion). Mesh-vs-capture cc77d8a = MESH. Bind: curl Arm_* plates around hang-arm axis (same verts/UVs). No solidify rims. No remesh/capsule. Hem does not Y-cut leg tubes.",
        "wrap": wrap,
        "shards": shards,
        "vsFail": "fd9d6f8",
        "oldPremise": "voxel remesh / capsule-arm Game-view (61e023d / c840b73) — Derek STOP, threw away paint PASS",
        "motion": "5916447 Evaluate() keys reused",
        "scabbard": "character-right",
        "playHubLocked": True,
        "sheath": {"pass1": sheath, "pass2": sheath2},
        "vertsPrimary": counts,
        "verts": len(mesh_ob.data.vertices),
        "tris": ntris,
        "boneTris": buckets,
        "atlas": atlas.name if hasattr(atlas, "name") else str(atlas),
        "mesh": "sir_aldric_meshy.mesh.txt",
        "fbx": fbx.name,
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done meshylook", note["walkClip"], "tris", ntris, "verts", note["verts"])


if __name__ == "__main__":
    main()
