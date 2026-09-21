#!/usr/bin/env python3
"""Hero look = e5b132f Meshy paint PASS. No voxel remesh. No capsule arms.

Derek STOP: remesh/capsule Game-view threw away the painted knight.
6232d5e FAIL: curling paper Arm_* plates (~300°) still read as navy/gold
sheets + tear stretch. New premise: DELETE paper Arm_* islands, build
CLOSED thick-walled hang-arm tubes, reproject e5b132f Image_0 UVs onto
them. Body/tabard/lion/scabbard untouched. Evaluate() 5916447 reused.
Bind/walk NOT claimed.
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


def _tube_frame(a, b):
    an = (b - a).normalized()
    tmp = Vector((0.0, 0.0, 1.0)) if abs(an.dot(Vector((0.0, 0.0, 1.0)))) < 0.85 else Vector((1.0, 0.0, 0.0))
    x = an.cross(tmp).normalized()
    z = x.cross(an).normalized()
    return an, x, z


def _closed_tube_object(name, a, b, r_top=0.050, r_bot=0.034, segs=16, rings=12, wall=0.016):
    """Watertight thick-walled tube. Not a paper shell, not an unpainted capsule."""
    an, x, z = _tube_frame(a, b)
    bm = bmesh.new()
    outer, inner = [], []
    for i in range(rings):
        t = i / (rings - 1)
        c = a.lerp(b, t)
        r = r_top * (1.0 - t) + r_bot * t
        ri = max(0.010, r - wall)
        ring_o, ring_i = [], []
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs
            radial = math.cos(ang) * x + math.sin(ang) * z
            ring_o.append(bm.verts.new(c + radial * r))
            ring_i.append(bm.verts.new(c + radial * ri))
        outer.append(ring_o)
        inner.append(ring_i)
    bm.verts.ensure_lookup_table()

    def quad(vs):
        try:
            bm.faces.new(vs)
        except ValueError:
            pass

    for i in range(rings - 1):
        for k in range(segs):
            k2 = (k + 1) % segs
            quad((outer[i][k], outer[i][k2], outer[i + 1][k2], outer[i + 1][k]))
            quad((inner[i][k2], inner[i][k], inner[i + 1][k], inner[i + 1][k2]))
    for k in range(segs):
        k2 = (k + 1) % segs
        quad((outer[0][k2], outer[0][k], inner[0][k], inner[0][k2]))
        quad((outer[-1][k], outer[-1][k2], inner[-1][k2], inner[-1][k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.update()
    for p in me.polygons:
        # Faceted: a smooth spear reads as a smear/sheet in the walk MP4.
        p.use_smooth = False
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


def _collect_arm_donors(ob):
    """Per-side Image_0 UVs from the paper Arm_* plates (e5b132f albedo)."""
    me = ob.data
    uv_layer = me.uv_layers.active
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    donors = {"Arm_R": [], "Arm_L": []}
    mat_count = defaultdict(int)
    rgb_of, lion = old._sample_albedo(me)
    if uv_layer is None:
        return donors, 0
    for poly in me.polygons:
        names = set()
        for vi in poly.vertices:
            names |= {vg_names.get(g.group, "") for g in me.vertices[vi].groups if g.weight > 0.5}
        gname = "Arm_R" if "Arm_R" in names else ("Arm_L" if "Arm_L" in names else None)
        if not gname:
            continue
        mat_count[poly.material_index] += 1
        for li, vi in zip(poly.loop_indices, poly.vertices):
            # Steel plate only. Navy / gold / lion wrap = horizontal tear bands.
            if lion[vi] or not _is_plate_steel(rgb_of[vi]):
                continue
            uv = uv_layer.data[li].uv
            p = me.vertices[vi].co
            donors[gname].append((p.copy(), (float(uv.x), float(uv.y)), float(p.y)))
    mat_idx = max(mat_count, key=mat_count.get) if mat_count else 0
    print("arm donors", {k: len(v) for k, v in donors.items()}, "mat", mat_idx)
    return donors, mat_idx


def _project_tube_uvs(tube_ob, donors, a, b):
    """One steel Image_0 texel. θ-wrap on 80c42d1 = horizontal tear bands."""
    me = tube_ob.data
    if me.uv_layers.active is None:
        me.uv_layers.new(name="UVMap")
    uv_layer = me.uv_layers.active
    if len(donors) < 8:
        u_m, v_m = 0.5, 0.5
    else:
        us = sorted(d[1][0] for d in donors)
        vs = sorted(d[1][1] for d in donors)
        u_m, v_m = us[len(us) // 2], vs[len(vs) // 2]
    for li in range(len(uv_layer.data)):
        uv_layer.data[li].uv = (u_m, v_m)


def _delete_hang_corridor_ghosts(ob) -> int:
    """Delete Arm/Fore/Hand geo AND leftover Meshy paper in the hang corridor.

    76eab9d census: 6 unjoined tubes + leftover Chest/Spine/Hips paper outboard
    = triple-arm ghost. Pauldrons y>1.30 and scabbard stay.
    """
    me = ob.data
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    arm_bones = {"Arm_L", "Arm_R", "Fore_L", "Fore_R", "Hand_L", "Hand_R"}
    kill = []
    for fi, p in enumerate(me.polygons):
        names = set()
        for vi in p.vertices:
            names |= {vg_names.get(g.group, "") for g in me.vertices[vi].groups if g.weight > 0.5}
        c = p.center
        if names & arm_bones:
            kill.append(fi)
            continue
        if "Scabbard" in names:
            continue
        if c.y > 1.30:
            continue
        if dist_seg(c, _SCAB_A, _SCAB_B) < 0.070:
            continue
        # leftover Meshy hang-arm plates (Chest/Spine/Hips) — outboard of tabard
        if 0.60 < c.y < 1.30 and abs(c.x) > 0.255 and abs(c.z) < 0.24:
            kill.append(fi)
    print("delete hang-corridor ghosts", len(kill), "of", len(me.polygons))
    if kill:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in kill if i < len(bm.faces)], context="FACES")
        bm.verts.ensure_lookup_table()
        loose = [v for v in bm.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm, geom=loose, context="VERTS")
        bm.to_mesh(me)
        bm.free()
        me.update()
    return len(kill)


def _is_plate_steel(rgb: tuple[int, int, int]) -> bool:
    """Keep gray plate. Drop navy tabard + gold trim (those wrap into tear bands)."""
    r, g, b = rgb
    mx, mn = max(r, g, b), min(r, g, b)
    if mx - mn > 45:
        return False
    return 70 < (r + g + b) / 3 < 210


def _closed_bent_arm_object(name, pts, radii, segs=16, rings_per=5, wall=0.024):
    """ONE watertight loft along a polyline. Not three separate tubes."""
    samples = []
    for seg in range(len(pts) - 1):
        a, b = pts[seg], pts[seg + 1]
        r0, r1 = radii[seg], radii[seg + 1]
        for i in range(rings_per):
            if seg > 0 and i == 0:
                continue
            t = i / (rings_per - 1)
            samples.append((a.lerp(b, t), r0 * (1.0 - t) + r1 * t))
    bm = bmesh.new()
    outer, inner = [], []
    for i, (c, r) in enumerate(samples):
        nxt = samples[min(i + 1, len(samples) - 1)][0]
        prv = samples[max(i - 1, 0)][0]
        an, x, z = _tube_frame(prv, nxt)
        ri = max(0.010, r - wall)
        ring_o, ring_i = [], []
        for k in range(segs):
            ang = 2.0 * math.pi * k / segs
            radial = math.cos(ang) * x + math.sin(ang) * z
            ring_o.append(bm.verts.new(c + radial * r))
            ring_i.append(bm.verts.new(c + radial * ri))
        outer.append(ring_o)
        inner.append(ring_i)
    bm.verts.ensure_lookup_table()

    def quad(vs):
        try:
            bm.faces.new(vs)
        except ValueError:
            pass

    for i in range(len(samples) - 1):
        for k in range(segs):
            k2 = (k + 1) % segs
            quad((outer[i][k], outer[i][k2], outer[i + 1][k2], outer[i + 1][k]))
            quad((inner[i][k2], inner[i][k], inner[i + 1][k], inner[i + 1][k2]))
    for k in range(segs):
        k2 = (k + 1) % segs
        quad((outer[0][k2], outer[0][k], inner[0][k], inner[0][k2]))
        quad((outer[-1][k], outer[-1][k2], inner[-1][k2], inner[-1][k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.update()
    for p in me.polygons:
        p.use_smooth = False
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


def replace_hang_arms_closed_tubes(ob) -> dict:
    """ONE closed hang-arm loft per side. 76eab9d ghost = leftover paper + 3 tubes.

    Delete leftover Meshy hang paper. Loft one bent volume shoulder→fist.
    Arm/Fore/Hand weights along that single mesh. Compact steel UV.
    Body/tabard/lion/scabbard not remeshed. Not unpainted capsules.
    """
    donors, mat_idx = _collect_arm_donors(ob)
    killed = _delete_hang_corridor_ghosts(ob)
    # One polyline per side. Moderate +Z — not three sticks, not a 70cm spear.
    specs = [
        (
            "R",
            [Vector((0.25, 1.30, 0.02)), Vector((0.30, 1.12, 0.06)),
             Vector((0.32, 0.96, 0.10)), Vector((0.33, 0.88, 0.12))],
            [0.090, 0.080, 0.068, 0.056],
        ),
        (
            "L",
            [Vector((-0.25, 1.30, 0.02)), Vector((-0.30, 1.12, 0.06)),
             Vector((-0.32, 0.96, 0.10)), Vector((-0.33, 0.88, 0.12))],
            [0.090, 0.080, 0.068, 0.056],
        ),
    ]
    groups = {g.name: g for g in ob.vertex_groups}
    for name in ("Arm_L", "Arm_R", "Fore_L", "Fore_R", "Hand_L", "Hand_R"):
        if name not in groups:
            groups[name] = ob.vertex_groups.new(name=name)
    added = {}
    for suffix, pts, radii in specs:
        side = f"Arm_{suffix}"
        tube = _closed_bent_arm_object(f"HangArm_{suffix}", pts, radii, segs=16, rings_per=5, wall=0.024)
        _project_tube_uvs(tube, donors.get(side, []), pts[0], pts[-1])
        if ob.data.materials:
            for mat in ob.data.materials:
                tube.data.materials.append(mat)
            for p in tube.data.polygons:
                p.material_index = min(mat_idx, len(tube.data.materials) - 1)
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        tube.select_set(True)
        bpy.context.view_layer.objects.active = ob
        before = len(ob.data.vertices)
        bpy.ops.object.join()
        n_new = len(ob.data.vertices) - before
        for i in range(before, len(ob.data.vertices)):
            p = ob.data.vertices[i].co
            t = max(0.0, min(1.0, (1.30 - p.y) / 0.42))
            # Blend across elbow/wrist — exclusive Y-cuts sheared into tear bands.
            if t < 0.28:
                pairs = [(f"Arm_{suffix}", 1.0)]
            elif t < 0.42:
                u = (t - 0.28) / 0.14
                pairs = [(f"Arm_{suffix}", 1.0 - u), (f"Fore_{suffix}", u)]
            elif t < 0.62:
                pairs = [(f"Fore_{suffix}", 1.0)]
            elif t < 0.76:
                u = (t - 0.62) / 0.14
                pairs = [(f"Fore_{suffix}", 1.0 - u), (f"Hand_{suffix}", u)]
            else:
                pairs = [(f"Hand_{suffix}", 1.0)]
            for bone in (f"Arm_{suffix}", f"Fore_{suffix}", f"Hand_{suffix}"):
                groups[bone].remove([i])
            for bone, w in pairs:
                if w > 0.02:
                    groups[bone].add([i], w, "REPLACE")
        added[f"HangArm_{suffix}"] = n_new
        print("joined ONE bent arm", suffix, "new verts", n_new)
    note = {
        "premise": "ONE loft/side; steel texel UV; blended Arm/Fore/Hand — vs 80c42d1 tear",
        "killedGhostFaces": killed,
        "donors": {k: len(v) for k, v in donors.items()},
        "added": added,
        "vertsAfter": len(ob.data.vertices),
        "facesAfter": len(ob.data.polygons),
    }
    print("one hang-arm loft per side", note)
    return note


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
    """Tube verts that lost groups get Arm_ / Fore_ / Hand_ by hang height."""
    groups = {g.name: g for g in ob.vertex_groups}
    for name in ("Arm_L", "Arm_R", "Fore_L", "Fore_R", "Hand_L", "Hand_R"):
        if name not in groups:
            groups[name] = ob.vertex_groups.new(name=name)
    vg_names = {g.index: g.name for g in ob.vertex_groups}
    n = 0
    for i, v in enumerate(ob.data.vertices):
        names = {vg_names.get(g.group, "") for g in v.groups if g.weight > 0.5}
        if names & set(MESH_BONES):
            continue
        p = v.co
        if p.x > 0.16 and 0.80 < p.y < 1.38 and (dist_seg(p, _ARM_R_A, _ARM_R_B) < 0.20 or p.z > 0.03):
            bone = "Hand_R" if p.y < 0.96 else ("Fore_R" if p.y < 1.12 else "Arm_R")
            groups[bone].add([i], 1.0, "REPLACE")
            n += 1
        elif p.x < -0.16 and 0.80 < p.y < 1.38 and (dist_seg(p, _ARM_L_A, _ARM_L_B) < 0.20 or p.z > 0.03):
            bone = "Hand_L" if p.y < 0.96 else ("Fore_L" if p.y < 1.12 else "Arm_L")
            groups[bone].add([i], 1.0, "REPLACE")
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
    tubes = replace_hang_arms_closed_tubes(mesh_ob)
    repair_arm_groups(mesh_ob)
    counts = recount_groups(mesh_ob)
    if counts.get("Scabbard", 0) < 40:
        raise SystemExit(f"scabbard too few: {counts.get('Scabbard')}")
    arm_r = counts.get("Arm_R", 0) + counts.get("Fore_R", 0) + counts.get("Hand_R", 0)
    arm_l = counts.get("Arm_L", 0) + counts.get("Fore_L", 0) + counts.get("Hand_L", 0)
    if arm_r < 80 or arm_l < 80:
        raise SystemExit(f"arm empty: R={arm_r} L={arm_l} {dict(counts)}")
    if counts.get("Fore_R", 0) < 20 or counts.get("Fore_L", 0) < 20:
        raise SystemExit(f"fore empty — 110443d spear relapse: {dict(counts)}")
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
    # Cycle samples Design asked for (n=0,5,10,15 at 16fps) — not one mid-swing still.
    for n in (0, 5, 10, 15):
        old.apply_pose(actor_ob, old.walk_pose(n / 16.0))
        npng = old.WALK / f"world_walk_n{n:02d}.png"
        bpy.context.scene.render.filepath = str(npng)
        bpy.ops.render.render(write_still=True)
        print("cycle still n", n, npng.stat().st_size)
    art = Path("/opt/cursor/artifacts")
    art.mkdir(parents=True, exist_ok=True)
    try:
        (art / "gate3_world_walk_mid_swing.png").write_bytes(mid_p.read_bytes())
        (art / "world_meshy_hang_rest.png").write_bytes(rest_p.read_bytes())
    except OSError as exc:
        print("artifact copy skip", exc)

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
        "retopo": "LOOK path: e5b132f body/tabard/lion/scabbard untouched. 6232d5e curl FAIL. New premise: delete paper Arm_* islands; closed thick-walled hang-arm tubes; reproject Image_0 UVs. No whole-body remesh. No unpainted capsule Game-view.",
        "tubes": tubes,
        "shards": shards,
        "vsFail": "fd9d6f8",
        "oldPremise": "80c42d1 one-loft PASSed ghost; FAIL horizontal tear (θ-wrap UV + exclusive Y-cut shear)",
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
