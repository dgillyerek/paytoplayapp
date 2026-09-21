#!/usr/bin/env python3
"""Gate 3: mid-poly Sir Aldric GAME MESH grown from Gate 2 clay volumes.

Volume law = blockout tip 5de0e16 (do not redesign proportions).
Clean loft topology with extra rings at shoulders / elbows / hips / knees.
NO voxel remesh. Mid-poly mobile (target ~4k–12k tris).

Blender stores Unity Y-up (Actor / mesh.txt). Character-RIGHT = +X.
Hang pose for World-cam vs 01_rear_LOCKED (Play). Sheathed only — no drawn sword.
Scabbard character-RIGHT only. Cape bone exists, no cape mesh.

World-cam = Play march angle: eye (0, 2.80, −5.40) look-at (0, 0.90, 0.50)
FOV 30 vertical, 1080×1920. TOP = +Z = away. NOT a beauty portrait cam.
"""
from __future__ import annotations

import json
import math
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
ATLAS = PACK3D / "sir_aldric_atlas.png"
PROOF = ROOT / "Docs/Survival/previews/gate3"

UV_SILVER = (0.125, 0.875)
UV_GOLD = (0.375, 0.875)
UV_BLUE = (0.625, 0.875)
UV_BROWN = (0.875, 0.875)
LION = (0.08, 0.24, 0.42, 0.64)
HEM = (0.52, 0.62, 0.98, 0.78)

# Actor rest — do not drift Evaluate().
BONE_REST = {
    "Root": ((0.0, 0.0, 0.0), (0.0, 0.0, 0.0)),
    "Hips": ((0.0, 0.96, 0.0), (0.0, 0.0, 0.0)),
    "Spine": ((0.0, 0.12, 0.0), (0.0, 0.0, 0.0)),
    "Chest": ((0.0, 0.18, 0.0), (0.0, 0.0, 0.0)),
    "Neck": ((0.0, 0.20, 0.0), (0.0, 0.0, 0.0)),
    "Head": ((0.0, 0.10, 0.0), (0.0, 0.0, 0.0)),
    "Arm_L": ((-0.22, 0.10, 0.0), (0.0, 0.0, 0.0)),
    "Fore_L": ((0.0, -0.28, 0.0), (0.0, 0.0, 0.0)),
    "Hand_L": ((0.0, -0.24, 0.0), (0.0, 0.0, 0.0)),
    "Arm_R": ((0.22, 0.10, 0.0), (0.0, 0.0, 0.0)),
    "Fore_R": ((0.0, -0.28, 0.0), (0.0, 0.0, 0.0)),
    "Hand_R": ((0.0, -0.24, 0.0), (0.0, 0.0, 0.0)),
    "Sword": ((0.02, -0.08, 0.06), (0.0, 0.0, 0.0)),
    "UpLeg_L": ((-0.11, -0.04, 0.0), (0.0, 0.0, 0.0)),
    "Leg_L": ((0.0, -0.42, 0.0), (0.0, 0.0, 0.0)),
    "Foot_L": ((0.0, -0.40, 0.05), (0.0, 0.0, 0.0)),
    "UpLeg_R": ((0.11, -0.04, 0.0), (0.0, 0.0, 0.0)),
    "Leg_R": ((0.0, -0.42, 0.0), (0.0, 0.0, 0.0)),
    "Foot_R": ((0.0, -0.40, 0.05), (0.0, 0.0, 0.0)),
    "Scabbard": ((0.20, -0.04, -0.02), (18.0, 0.0, 22.0)),
    "Cape": ((0.0, 0.08, -0.12), (0.0, 0.0, 0.0)),
}
PARENT = {
    "Root": None,
    "Hips": "Root",
    "Spine": "Hips",
    "Chest": "Spine",
    "Neck": "Chest",
    "Head": "Neck",
    "Arm_L": "Chest",
    "Fore_L": "Arm_L",
    "Hand_L": "Fore_L",
    "Arm_R": "Chest",
    "Fore_R": "Arm_R",
    "Hand_R": "Fore_R",
    "Sword": "Hand_R",
    "UpLeg_L": "Hips",
    "Leg_L": "UpLeg_L",
    "Foot_L": "Leg_L",
    "UpLeg_R": "Hips",
    "Leg_R": "UpLeg_R",
    "Foot_R": "Leg_R",
    "Scabbard": "Hips",
    "Cape": "Chest",
}
HUMANOID = {
    "Hips": "Hips",
    "Spine": "Spine",
    "Chest": "Chest",
    "Neck": "Neck",
    "Head": "Head",
    "LeftUpperArm": "Arm_L",
    "LeftLowerArm": "Fore_L",
    "LeftHand": "Hand_L",
    "RightUpperArm": "Arm_R",
    "RightLowerArm": "Fore_R",
    "RightHand": "Hand_R",
    "LeftUpperLeg": "UpLeg_L",
    "LeftLowerLeg": "Leg_L",
    "LeftFoot": "Foot_L",
    "RightUpperLeg": "UpLeg_R",
    "RightLowerLeg": "Leg_R",
    "RightFoot": "Foot_R",
}

# Play World-cam (Unity Y-up). TOP = +Z = away.
CAM_EYE = (0.0, 2.80, -5.40)
CAM_TARGET = (0.0, 0.90, 0.50)
CAM_FOV = 30.0
# Slight character-left offset so character-RIGHT scabbard reads on the ¾.
CAM_EYE_34 = (1.20, 2.80, -5.15)


def clay_to_unity(x, y_fwd, z_up):
    """Gate 2 clay is Blender Z-up, +Y face. Unity Y-up, +Z face."""
    return (x, z_up, y_fwd)


def clay_radii(rx, ry_fwd, rz_up):
    return (rx, rz_up, ry_fwd)


def eul_xyz_deg(x, y, z):
    rx, ry, rz = math.radians(x), math.radians(y), math.radians(z)
    return Matrix.Rotation(ry, 4, "Y") @ Matrix.Rotation(rx, 4, "X") @ Matrix.Rotation(rz, 4, "Z")


def rest_world():
    mats = {}
    order = [
        "Root", "Hips", "Spine", "Chest", "Neck", "Head",
        "Arm_L", "Fore_L", "Hand_L", "Arm_R", "Fore_R", "Hand_R", "Sword",
        "UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R",
        "Scabbard", "Cape",
    ]
    for name in order:
        loc, eul = BONE_REST[name]
        local = Matrix.Translation(Vector(loc)) @ eul_xyz_deg(*eul)
        par = PARENT[name]
        mats[name] = (mats[par] @ local) if par else local
    return mats


def _sat(x):
    return 0.0 if x < 0.0 else 1.0 if x > 1.0 else x


def oval_ring(y, rx, rz, z_off=0.0, segs=16):
    return [
        (rx * math.cos(i / segs * math.tau), y, rz * math.sin(i / segs * math.tau) + z_off)
        for i in range(segs)
    ]


def add_loft(bm, rings, cap=True):
    segs = len(rings[0])
    rows = []
    for ring in rings:
        rows.append([bm.verts.new(Vector(p)) for p in ring])
    bm.verts.ensure_lookup_table()
    for i in range(len(rows) - 1):
        for s in range(segs):
            s1 = (s + 1) % segs
            bm.faces.new((rows[i][s], rows[i][s1], rows[i + 1][s1], rows[i + 1][s]))
    if cap:
        bm.faces.new(list(reversed(rows[0])))
        bm.faces.new(rows[-1])
    return [v for row in rows for v in row]


def _frame(axis):
    n = Vector(axis).normalized()
    arb = Vector((0.0, 1.0, 0.0)) if abs(n.y) < 0.92 else Vector((1.0, 0.0, 0.0))
    ihat = n.cross(arb).normalized()
    jhat = n.cross(ihat).normalized()
    return ihat, jhat


def limb_tube(bm, p0, p1, r0, r1, segs=12):
    """Quad loft with packed rings at both joints (shoulder/elbow or hip/knee)."""
    a, b = Vector(p0), Vector(p1)
    axis = b - a
    if axis.length < 1e-6:
        return
    ihat, jhat = _frame(axis)
    # Extra loops at 0 / 1 = deformation rings at the joints.
    ts = (0.00, 0.03, 0.08, 0.18, 0.32, 0.50, 0.68, 0.82, 0.92, 0.97, 1.00)
    rings = []
    for t in ts:
        r = r0 * (1.0 - t) + r1 * t
        c = a.lerp(b, t)
        rings.append([
            c + (ihat * math.cos(k / segs * math.tau) + jhat * math.sin(k / segs * math.tau)) * r
            for k in range(segs)
        ])
    add_loft(bm, rings)


def add_ellipsoid(bm, center, radii, segs=14, rings=8):
    ret = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    c = Vector(center)
    rx, ry, rz = radii
    for v in ret["verts"]:
        v.co = Vector((v.co.x * rx, v.co.y * ry, v.co.z * rz)) + c


def add_taper(bm, p0, p1, r0, r1, segs=12):
    a, b = Vector(p0), Vector(p1)
    axis = b - a
    length = max(axis.length, 1e-6)
    mid = (a + b) * 0.5
    ret = bmesh.ops.create_cone(
        bm, cap_ends=True, segments=segs, radius1=r0, radius2=r1, depth=length
    )
    quat = Vector((0.0, 0.0, 1.0)).rotation_difference(axis.normalized())
    for v in ret["verts"]:
        v.co = quat @ v.co + mid


def flatten_fwd(bm, z_min, scale, y_lo, y_hi):
    """Pull +Z (Unity forward / visor) toward a plane — clay flatten_front."""
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        if v.co.z > z_min and y_lo <= v.co.y <= y_hi:
            v.co.z = z_min + (v.co.z - z_min) * scale


def _uv_for(kind, co, n):
    if kind == "silver":
        return UV_SILVER
    if kind == "gold":
        return UV_GOLD
    if kind == "blue":
        return UV_BLUE
    if kind == "brown":
        return UV_BROWN
    if kind == "surcoat":
        if n.z < -0.18 and 1.00 <= co.y <= 1.42:
            return _lion_uv(co)
        return UV_BLUE
    if kind == "hem":
        return _hem_uv(co)
    if kind == "boot":
        if co.y > 0.07 or abs(co.z) > 0.10:
            return UV_GOLD
        return UV_SILVER
    if kind == "helm":
        if co.y > 1.80:
            return UV_GOLD
        if abs(co.x) < 0.016 and co.y > 1.72 and n.z < -0.15:
            return UV_GOLD
        return UV_SILVER
    if kind == "pauldron":
        if co.y < 1.37 and abs(n.y) > 0.25:
            return UV_GOLD
        return UV_SILVER
    if kind == "gauntlet":
        if co.y > 0.86:
            return UV_GOLD
        return UV_SILVER
    if kind == "cop":
        if abs(co.y - 1.14) < 0.04 or abs(co.y - 0.50) < 0.04:
            return UV_GOLD
        return UV_SILVER
    if kind == "scabbard":
        if co.y > 0.98 or co.y < 0.50:
            return UV_GOLD
        return UV_BROWN
    return UV_SILVER


def _lion_uv(co):
    u0, v0, u1, v1 = LION
    u = u0 + (u1 - u0) * _sat((co.x + 0.145) / 0.29)
    v = v0 + (v1 - v0) * _sat((co.y - 1.04) / 0.36)
    return (u, v)


def _hem_uv(co):
    u0, v0, u1, v1 = HEM
    u = u0 + (u1 - u0) * _sat((co.x + 0.26) / 0.52)
    v = v0 + (v1 - v0) * _sat((co.y - 0.72) / 0.14)
    return (u, v)


def assign_uvs(bm, kind):
    uv = bm.loops.layers.uv.new("UVMap")
    bm.faces.ensure_lookup_table()
    bm.normal_update()
    for face in bm.faces:
        n = face.normal
        for loop in face.loops:
            loop[uv].uv = Vector(_uv_for(kind, loop.vert.co, n))


def mesh_from_bm(name, bm, kind):
    assign_uvs(bm, kind)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    for p in me.polygons:
        p.use_smooth = True
    return obj


def build_parts():
    """Grow plates from clay 5de0e16 volumes. Hang arms for Play World-cam."""
    objs = []
    segs = 16

    # --- Closed armet (clay helm @ 1.68, visor plane, pointed crown) ---
    bm = bmesh.new()
    add_loft(
        bm,
        [
            oval_ring(1.52, 0.088, 0.100, 0.00, segs),  # gorget
            oval_ring(1.56, 0.100, 0.118, 0.02, segs),
            oval_ring(1.62, 0.108, 0.140, 0.04, segs),  # visor height
            oval_ring(1.68, 0.112, 0.155, 0.04, segs),  # clay equator
            oval_ring(1.74, 0.100, 0.138, 0.02, segs),
            oval_ring(1.80, 0.070, 0.090, 0.01, segs),
            oval_ring(1.86, 0.018, 0.018, 0.00, segs),  # crest knob
        ],
    )
    flatten_fwd(bm, 0.16, 0.55, 1.56, 1.74)
    # Thin gold crest / nape (01_rear) — not a visor T from behind.
    add_taper(bm, (0.0, 1.56, -0.12), (0.0, 1.84, -0.04), 0.008, 0.006, 8)
    add_taper(bm, (0.0, 1.78, 0.00), (0.0, 1.86, 0.00), 0.010, 0.016, 8)
    objs.append((mesh_from_bm("Helm", bm, "helm"), "Head"))

    bm = bmesh.new()
    add_loft(
        bm,
        [
            oval_ring(1.48, 0.062, 0.070, 0.00, 12),
            oval_ring(1.52, 0.088, 0.100, 0.00, 12),
            oval_ring(1.56, 0.070, 0.080, 0.01, 12),
        ],
    )
    objs.append((mesh_from_bm("Gorget", bm, "silver"), "Neck"))

    # --- A-line royal-blue surcoat (clay loft 1.48 → 0.74) + hip loops ---
    bm = bmesh.new()
    add_loft(
        bm,
        [
            oval_ring(1.48, 0.168, 0.100, 0.02, segs),
            oval_ring(1.40, 0.180, 0.108, 0.02, segs),
            oval_ring(1.34, 0.188, 0.115, 0.02, segs),
            oval_ring(1.26, 0.184, 0.112, 0.015, segs),
            oval_ring(1.18, 0.178, 0.110, 0.01, segs),
            oval_ring(1.10, 0.174, 0.108, 0.01, segs),
            oval_ring(1.04, 0.172, 0.108, 0.01, segs),  # hip loop
            oval_ring(0.99, 0.186, 0.116, 0.015, segs),  # hip loop
            oval_ring(0.96, 0.198, 0.122, 0.02, segs),
            oval_ring(0.90, 0.214, 0.130, 0.025, segs),
            oval_ring(0.86, 0.228, 0.138, 0.03, segs),
            oval_ring(0.80, 0.240, 0.144, 0.03, segs),
            oval_ring(0.74, 0.248, 0.148, 0.03, segs),
        ],
    )
    objs.append((mesh_from_bm("Surcoat", bm, "surcoat"), "Chest"))

    # Lion plaque — clay back (0, −0.12, 1.22). Subdivided so rampant reads.
    bm = bmesh.new()
    nu, nv = 12, 16
    grid = []
    for j in range(nv + 1):
        ty = j / nv
        y = 1.04 + 0.36 * ty
        z = -0.12 - 0.02 * ty
        row = []
        for i in range(nu + 1):
            tx = i / nu
            x = -0.145 + 0.29 * tx
            row.append(bm.verts.new(Vector((x, y, z))))
        grid.append(row)
    bm.verts.ensure_lookup_table()
    for j in range(nv):
        for i in range(nu):
            # Winding so normal faces −Z (World-cam / 01_rear).
            bm.faces.new((grid[j][i], grid[j + 1][i], grid[j + 1][i + 1], grid[j][i + 1]))
    uv = bm.loops.layers.uv.new("UVMap")
    u0, v0, u1, v1 = LION
    bm.faces.ensure_lookup_table()
    for face in bm.faces:
        for loop in face.loops:
            co = loop.vert.co
            tx = _sat((co.x + 0.145) / 0.29)
            ty = _sat((co.y - 1.04) / 0.36)
            loop[uv].uv = Vector((u0 + (u1 - u0) * tx, v0 + (v1 - v0) * ty))
    me = bpy.data.meshes.new("LionBadge")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("LionBadge", me)
    bpy.context.collection.objects.link(obj)
    for p in me.polygons:
        p.use_smooth = True
    objs.append((obj, "Chest"))

    # Greek-key hem band (clay 0.72–0.80)
    bm = bmesh.new()
    add_loft(
        bm,
        [
            oval_ring(0.72, 0.215, 0.118, 0.02, segs),
            oval_ring(0.72, 0.252, 0.150, 0.03, segs),
            oval_ring(0.80, 0.250, 0.148, 0.03, segs),
            oval_ring(0.80, 0.214, 0.116, 0.02, segs),
        ],
        cap=True,
    )
    objs.append((mesh_from_bm("HemKey", bm, "hem"), "Hips"))

    # Breastplate mass under collar (clay 1.34)
    bm = bmesh.new()
    add_ellipsoid(bm, clay_to_unity(0.0, 0.06, 1.34), clay_radii(0.155, 0.095, 0.110), 14, 8)
    flatten_fwd(bm, 0.10, 0.50, 1.22, 1.46)
    objs.append((mesh_from_bm("Breast", bm, "silver"), "Chest"))

    # Belt + character-LEFT pouch (clay)
    bm = bmesh.new()
    add_loft(
        bm,
        [
            oval_ring(0.975, 0.175, 0.118, 0.02, 16),
            oval_ring(0.990, 0.175, 0.118, 0.02, 16),
            oval_ring(1.005, 0.175, 0.118, 0.02, 16),
        ],
    )
    add_ellipsoid(bm, clay_to_unity(0.0, 0.12, 0.99), clay_radii(0.030, 0.018, 0.022), 8, 5)
    objs.append((mesh_from_bm("Belt", bm, "gold"), "Hips"))

    bm = bmesh.new()
    add_ellipsoid(bm, clay_to_unity(-0.16, 0.06, 0.96), clay_radii(0.042, 0.030, 0.038), 10, 6)
    objs.append((mesh_from_bm("Pouch", bm, "brown"), "Hips"))

    # Pauldrons (clay ±0.22, z=1.44)
    for bone, s in (("Arm_L", -1.0), ("Arm_R", 1.0)):
        bm = bmesh.new()
        add_ellipsoid(bm, clay_to_unity(s * 0.22, 0.02, 1.44), clay_radii(0.115, 0.078, 0.080), 14, 8)
        add_ellipsoid(bm, clay_to_unity(s * 0.23, 0.02, 1.37), clay_radii(0.118, 0.085, 0.028), 12, 6)
        objs.append((mesh_from_bm(f"Pauldron_{bone}", bm, "pauldron"), bone))

    # --- Hang plate arms (clay radii, Actor hang positions) + elbow loops ---
    for side, s in (("L", -1.0), ("R", 1.0)):
        x = s * 0.22
        bm = bmesh.new()
        limb_tube(bm, (x, 1.40, 0.02), (x, 1.14, 0.02), 0.062, 0.056, 12)
        objs.append((mesh_from_bm(f"UpperArm_{side}", bm, "silver"), f"Arm_{side}"))
        bm = bmesh.new()
        add_ellipsoid(bm, (x, 1.14, 0.02), (0.066, 0.050, 0.060), 12, 8)
        objs.append((mesh_from_bm(f"Elbow_{side}", bm, "cop"), f"Fore_{side}"))
        bm = bmesh.new()
        limb_tube(bm, (x, 1.14, 0.02), (x, 0.88, 0.02), 0.054, 0.048, 12)
        objs.append((mesh_from_bm(f"Fore_{side}", bm, "silver"), f"Fore_{side}"))
        bm = bmesh.new()
        add_ellipsoid(bm, (x, 0.82, 0.02), (0.050, 0.060, 0.042), 10, 6)
        add_ellipsoid(bm, (x, 0.88, 0.02), (0.054, 0.022, 0.044), 8, 5)
        objs.append((mesh_from_bm(f"Hand_{side}", bm, "gauntlet"), f"Hand_{side}"))

    # --- Legs + knee loops + pointed sabatons (clay fused ankles) ---
    for side, s in (("L", -1.0), ("R", 1.0)):
        x = s * 0.105
        bm = bmesh.new()
        limb_tube(bm, (x, 0.92, 0.02), (x, 0.52, 0.03), 0.080, 0.066, 12)
        objs.append((mesh_from_bm(f"Thigh_{side}", bm, "silver"), f"UpLeg_{side}"))
        bm = bmesh.new()
        add_ellipsoid(bm, (x, 0.50, 0.04), (0.074, 0.048, 0.070), 12, 8)
        objs.append((mesh_from_bm(f"Knee_{side}", bm, "cop"), f"Leg_{side}"))
        bm = bmesh.new()
        limb_tube(bm, (x, 0.50, 0.03), (x, 0.08, 0.06), 0.062, 0.050, 12)
        objs.append((mesh_from_bm(f"Shin_{side}", bm, "silver"), f"Leg_{side}"))
        bm = bmesh.new()
        add_ellipsoid(bm, (x, 0.045, 0.10), (0.054, 0.038, 0.095), 12, 7)
        add_taper(bm, (x, 0.038, 0.14), (x, 0.028, 0.24), 0.040, 0.018, 10)
        add_ellipsoid(bm, (x, 0.08, 0.04), (0.048, 0.016, 0.048), 8, 5)
        objs.append((mesh_from_bm(f"Boot_{side}", bm, "boot"), f"Foot_{side}"))

    # --- Character-RIGHT sheathed scabbard (clay 0.20,−0.04,1.02 → 0.28,−0.10,0.42) ---
    bm = bmesh.new()
    limb_tube(bm, clay_to_unity(0.20, -0.04, 1.02), clay_to_unity(0.28, -0.10, 0.42), 0.028, 0.016, 10)
    add_ellipsoid(bm, clay_to_unity(0.20, -0.04, 1.02), clay_radii(0.032, 0.022, 0.020), 8, 5)
    add_ellipsoid(bm, clay_to_unity(0.28, -0.10, 0.42), clay_radii(0.016, 0.014, 0.018), 8, 5)
    add_taper(bm, clay_to_unity(0.14, -0.04, 1.04), clay_to_unity(0.28, -0.04, 1.04), 0.010, 0.010, 8)
    add_ellipsoid(bm, clay_to_unity(0.20, -0.04, 1.10), clay_radii(0.014, 0.012, 0.036), 8, 5)
    objs.append((mesh_from_bm("ScabbardMesh", bm, "scabbard"), "Scabbard"))

    return objs


def make_armature(mats):
    arm_data = bpy.data.armatures.new("SirAldricArmature")
    arm_data.display_type = "STICK"
    arm_obj = bpy.data.objects.new("SirAldric", arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")
    tails = {
        "Root": mats["Hips"].to_translation(),
        "Hips": mats["Spine"].to_translation(),
        "Spine": mats["Chest"].to_translation(),
        "Chest": mats["Neck"].to_translation(),
        "Neck": mats["Head"].to_translation(),
        "Head": mats["Head"].to_translation() + Vector((0.0, 0.16, 0.0)),
        "Arm_L": mats["Fore_L"].to_translation(),
        "Fore_L": mats["Hand_L"].to_translation(),
        "Hand_L": mats["Hand_L"].to_translation() + Vector((0.0, -0.10, 0.0)),
        "Arm_R": mats["Fore_R"].to_translation(),
        "Fore_R": mats["Hand_R"].to_translation(),
        "Hand_R": mats["Hand_R"].to_translation() + Vector((0.0, -0.10, 0.0)),
        "Sword": mats["Sword"].to_translation() + Vector((0.0, -0.16, 0.04)),
        "UpLeg_L": mats["Leg_L"].to_translation(),
        "Leg_L": mats["Foot_L"].to_translation(),
        "Foot_L": mats["Foot_L"].to_translation() + Vector((0.0, 0.0, 0.12)),
        "UpLeg_R": mats["Leg_R"].to_translation(),
        "Leg_R": mats["Foot_R"].to_translation(),
        "Foot_R": mats["Foot_R"].to_translation() + Vector((0.0, 0.0, 0.12)),
        "Scabbard": mats["Scabbard"].to_translation() + (mats["Scabbard"].to_3x3() @ Vector((0.0, -0.22, 0.0))),
        "Cape": mats["Cape"].to_translation() + Vector((0.0, -0.08, -0.06)),
    }
    ebs = {}
    for name in BONE_REST:
        eb = arm_data.edit_bones.new(name)
        head = mats[name].to_translation()
        tail = tails[name]
        if (tail - head).length < 0.04:
            tail = head + Vector((0.0, 0.08, 0.0))
        eb.head = head
        eb.tail = tail
        eb.use_deform = name != "Root"
        ebs[name] = eb
    for name, par in PARENT.items():
        if par:
            ebs[name].parent = ebs[par]
            ebs[name].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_obj


def bind_skin(arm_obj, parts, mats):
    deform = [n for n in BONE_REST if n not in ("Root", "Cape")]
    for obj, bone in parts:
        if bone not in {g.name for g in obj.vertex_groups}:
            obj.vertex_groups.new(name=bone)
        idx = [v.index for v in obj.data.vertices]
        obj.vertex_groups[bone].add(idx, 1.0, "REPLACE")
    bpy.ops.object.select_all(action="DESELECT")
    for obj, _bone in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0][0]
    bpy.ops.object.join()
    body = bpy.context.view_layer.objects.active
    body.name = "SirAldricMesh"
    for name in deform:
        if name not in {g.name for g in body.vertex_groups}:
            body.vertex_groups.new(name=name)
    body.parent = arm_obj
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_obj
    mod.use_vertex_groups = True
    return body


def apply_atlas_material(obj):
    mat = bpy.data.materials.new("AldricAtlas")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    if ATLAS.exists():
        img = bpy.data.images.load(str(ATLAS), check_existing=True)
        tex.image = img
        tex.image.colorspace_settings.name = "sRGB"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bsdf.inputs["Roughness"].default_value = 0.42
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = 0.28
    obj.data.materials.append(mat)


def aim_camera(cam, loc, target):
    """Blender track. Play-cam laterality is the numpy raster (up × forward)."""
    cam.location = Vector(loc)
    direction = Vector(target) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_world_render():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    try:
        sc.eevee.taa_render_samples = 24
    except AttributeError:
        pass
    world = bpy.data.worlds.new("WorldCam")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.14, 0.15, 0.13, 1.0)
        bg.inputs[1].default_value = 1.2
    else:
        world.color = (0.08, 0.09, 0.07)


def add_world_lights():
    # Unity space: Y-up, camera behind at −Z.
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 5.8
    key.angle = 0.08
    key_o = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(key_o)
    key_o.location = (1.4, 3.6, -4.2)
    key_o.rotation_euler = (math.radians(55), math.radians(0), math.radians(18))
    fill = bpy.data.lights.new("Fill", "AREA")
    fill.energy = 220.0
    fill.size = 5.0
    fill_o = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fill_o)
    fill_o.location = (-2.2, 2.4, -1.5)
    fill_o.rotation_euler = (math.radians(70), 0.0, math.radians(-25))
    rim = bpy.data.lights.new("Rim", "SUN")
    rim.energy = 2.2
    rim_o = bpy.data.objects.new("Rim", rim)
    bpy.context.collection.objects.link(rim_o)
    rim_o.location = (0.0, 2.2, 3.4)
    rim_o.rotation_euler = (math.radians(40), math.radians(180), 0.0)


def add_ground():
    # Unity XZ ground (same as SirAldricDemo Quad).
    bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0.0, 0.0, 1.6))
    ground = bpy.context.active_object
    ground.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    ground.name = "Ground"
    mat = bpy.data.materials.new("GroundMat")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.10, 0.11, 0.09, 1)
        bsdf.inputs["Roughness"].default_value = 0.9
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = 0.0
    ground.data.materials.append(mat)
    return ground


def make_cam(name):
    cam_data = bpy.data.cameras.new(name)
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(CAM_FOV)
    cam_data.sensor_fit = "VERTICAL"
    cam_data.clip_start = 0.08
    cam_data.clip_end = 40.0
    cam = bpy.data.objects.new(name, cam_data)
    bpy.context.collection.objects.link(cam)
    return cam


def render_world_cams(arm, body):
    """Play-angle stills. Mesh + camera both Unity Y-up (same as SirAldricDemo).

    Armature modifier is rest-identity on bind-pose verts; disable it so bone
    rest eulers (Scabbard 22° Z) cannot double-transform the authored +X hip.
    """
    PROOF.mkdir(parents=True, exist_ok=True)
    setup_world_render()
    for mod in body.modifiers:
        if mod.type == "ARMATURE":
            mod.show_render = False
            mod.show_viewport = False
    bpy.context.view_layer.update()
    add_ground()
    add_world_lights()
    cam = make_cam("WorldPlay")
    bpy.context.scene.camera = cam
    shots = {
        "eevee_rear": (CAM_EYE, CAM_TARGET),
        "eevee_rear_34": (CAM_EYE_34, CAM_TARGET),
    }
    paths = {}
    for name, (eye, tgt) in shots.items():
        aim_camera(cam, eye, tgt)
        path = PROOF / f"{name}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        paths[name] = path
        print("render", path, path.stat().st_size)
    for mod in body.modifiers:
        if mod.type == "ARMATURE":
            mod.show_render = True
            mod.show_viewport = True
    return paths


def export_mesh_txt(body, mats, path: Path):
    me = body.data
    me.calc_loop_triangles()
    uv_layer = me.uv_layers.active
    inv = {n: mats[n].inverted() for n in mats}
    vg_names = {g.index: g.name for g in body.vertex_groups}
    bone_of = []
    for v in me.vertices:
        if v.groups:
            g = max(v.groups, key=lambda x: x.weight)
            bone_of.append(vg_names.get(g.group, "Hips"))
        else:
            bone_of.append("Hips")
    lines = [
        "# SirAldric Gate 3 Blender mid-poly  clay-volume-law 5de0e16  scabbard=+X  blender",
        "FMT v3 blender gate3",
    ]
    buckets = defaultdict(list)
    for tri in me.loop_triangles:
        bones = [bone_of[i] for i in tri.vertices]
        bone = max(set(bones), key=bones.count)
        iw = inv[bone]
        pts, nrms, uvs = [], [], []
        for i, li in zip(tri.vertices, tri.loops):
            w = Vector(me.vertices[i].co)
            loc = iw @ w.to_4d()
            pts.append((loc.x, loc.y, loc.z))
            nw = Vector(me.vertices[i].normal)
            ln = (iw.to_3x3() @ nw).normalized()
            nrms.append((ln.x, ln.y, ln.z))
            if uv_layer:
                uv = uv_layer.data[li].uv
                uvs.append((float(uv.x), float(uv.y)))
            else:
                uvs.append(UV_SILVER)
        buckets[bone].append((pts, nrms, uvs))
    for bone in [
        "Head", "Neck", "Chest", "Spine", "Hips",
        "Arm_L", "Fore_L", "Hand_L", "Arm_R", "Fore_R", "Hand_R", "Sword",
        "UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R", "Scabbard",
    ]:
        tris = buckets.get(bone, [])
        if not tris:
            continue
        lines.append(f"BONE {bone}")
        for pts, nrms, uvs in tris:
            for p, n, uv in zip(pts, nrms, uvs):
                lines.append(
                    f"V {p[0]:.5f} {p[1]:.5f} {p[2]:.5f} "
                    f"{n[0]:.4f} {n[1]:.4f} {n[2]:.4f} "
                    f"{uv[0]:.4f} {uv[1]:.4f}"
                )
            lines.append("T")
    path.write_text("\n".join(lines) + "\n")
    ntris = sum(len(v) for v in buckets.values())
    print("mesh.txt", path, "tris", ntris)
    return ntris


def export_obj(body, path: Path):
    me = body.data
    me.calc_loop_triangles()
    uv_layer = me.uv_layers.active
    lines = ["# SirAldric Gate 3 mid-poly — bind pose world (Unity Y-up)", "mtllib sir_aldric.mtl"]
    for v in me.vertices:
        lines.append(f"v {v.co.x:.5f} {v.co.y:.5f} {v.co.z:.5f}")
    for v in me.vertices:
        n = v.normal
        lines.append(f"vn {n.x:.4f} {n.y:.4f} {n.z:.4f}")
    vert_uv = [(0.0, 0.0)] * len(me.vertices)
    if uv_layer:
        for li, loop in enumerate(me.loops):
            vert_uv[loop.vertex_index] = (float(uv_layer.data[li].uv.x), float(uv_layer.data[li].uv.y))
    for u, v in vert_uv:
        lines.append(f"vt {u:.4f} {v:.4f}")
    lines.append("usemtl AldricAtlas")
    for tri in me.loop_triangles:
        a, b, c = (i + 1 for i in tri.vertices)
        lines.append(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}")
    path.write_text("\n".join(lines) + "\n")


def unity_meta(guid: str, tex=False, fbx=False) -> str:
    if fbx:
        return (
            f"fileFormatVersion: 2\nguid: {guid}\nModelImporter:\n"
            "  serializedVersion: 24\n  externalObjects: {}\n"
            "  globalScale: 1\n  useFileScale: 1\n  animationType: 3\n"
            "  humanoidOversampling: 1\n  addHumanoidExtraRoot: 1\n"
            "  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        )
    if not tex:
        return (
            f"fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n"
            "  externalObjects: {}\n  userData: \n  assetBundleName: \n"
            "  assetBundleVariant: \n"
        )
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 1
  textureType: 8
  textureShape: 1
  nPOTScale: 0
  alphaIsTransparency: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    wrapU: 1
    wrapV: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def write_sidecars(ntris: int, nverts: int, nfaces: int):
    (PACK3D / "sir_aldric.mtl").write_text("newmtl AldricAtlas\nmap_Kd sir_aldric_atlas.png\nKd 1 1 1\n")
    (PACK3D / "sir_aldric_atlas.png.meta").write_text(unity_meta("a1d41c0000000000000000000000aa01", tex=True))
    (PACK3D / "sir_aldric_midpoly.mesh.txt.meta").write_text(unity_meta("a1d41c0000000000000000000000aa02"))
    (PACK3D / "sir_aldric_midpoly.obj.meta").write_text(unity_meta("a1d41c0000000000000000000000aa03"))
    (PACK3D / "sir_aldric.fbx.meta").write_text(unity_meta("a1d41c0000000000000000000000aa04", fbx=True))
    (PACK3D / "sir_aldric.blend.meta").write_text(unity_meta("a1d41c0000000000000000000000aa05"))
    (PACK3D / "sir_aldric_humanoid.json").write_text(
        json.dumps(
            {
                "avatar": "Humanoid",
                "rest": "Actor hang (arms local −Y) — do not retarget to T-pose or Evaluate() regresses",
                "bindPose": "Gate 3 clay-volume hang (World-cam vs 01_rear). Joint loops at shoulder/elbow/hip/knee.",
                "map": HUMANOID,
                "extras": {"Sword": "Sword (no drawn mesh — sheathed only)", "Scabbard": "Scabbard", "Cape": "Cape (bone only, no mesh)"},
                "forward": "+Z",
                "up": "+Y",
                "characterRight": "+X",
                "tris": ntris,
                "lookPassClaimed": False,
            },
            indent=2,
        )
        + "\n"
    )
    manifest = {
        "name": "SirAldric",
        "track": "blender-fbx",
        "gate": 3,
        "volumeLaw": "5de0e16",
        "soT": "look_targets/01_rear_LOCKED.png",
        "turnaround": "TURNAROUND_GATE1/LOCKED/",
        "scabbard": "character-right",
        "atlas": "sir_aldric_atlas.png",
        "mesh": "sir_aldric_midpoly.mesh.txt",
        "fbx": "sir_aldric.fbx",
        "blend": "sir_aldric.blend",
        "verts": nverts,
        "faces": nfaces,
        "tris": ntris,
        "lookPassClaimed": False,
        "walkPaused": True,
        "playHubLocked": True,
    }
    (PACK3D / "sir_aldric_3d.json").write_text(json.dumps(manifest, indent=2) + "\n")
    (PACK3D / "README.md").write_text(
        "# Sir Aldric 3D (Theme A) — Gate 3 game mesh\n\n"
        "**Volume law:** Gate 2 clay `5de0e16` (do not redesign proportions).\n"
        "Clean loft topology with joint loops at shoulders / elbows / hips / knees. "
        "Mid-poly mobile. No voxel remesh.\n\n"
        "World-cam stills: Play march angle `(0, 2.80, −5.40)` look-at `(0, 0.90, 0.50)` "
        "FOV 30, 1080×1920, TOP = +Z = away. **Not** a beauty portrait cam.\n\n"
        "Runtime bind: `sir_aldric_midpoly.mesh.txt` + `sir_aldric_atlas.png` on "
        "`SirAldric3DActor` (same bones / `Evaluate()`). Walk / Animator / Play hub "
        "stay paused until Derek look PASS.\n\n"
        "Must-match:\n"
        "- Silver plate + gold trim (helm crest, pauldrons, gauntlets, greaves, boots)\n"
        "- Royal-blue short surcoat: gold lion rampant + gold Greek-key hem\n"
        "- Brown scabbard + gold fittings on **character-RIGHT** hip only\n"
        "- Boots silver+gold (not brown leather)\n"
        "- Helm BACK vs `01_rear_LOCKED` / turnaround (World-cam is rear; face law not visible)\n\n"
        "Look gate is **not claimed**.\n"
        "Play hub stays `SIR_ALDRIC_REAR_MASTER_LOCKED.png`.\n"
    )


def main():
    PACK3D.mkdir(parents=True, exist_ok=True)
    if not ATLAS.exists():
        subprocess.check_call(["python3", str(ROOT / "scripts/blender/build_sir_aldric_atlas.py")])
    reset_scene()
    mats = rest_world()
    parts = build_parts()
    arm = make_armature(mats)
    body = bind_skin(arm, parts, mats)
    apply_atlas_material(body)

    nverts = len(body.data.vertices)
    nfaces = len(body.data.polygons)
    print("mesh verts", nverts, "faces", nfaces)

    render_world_cams(arm, body)

    # Drop render-only ground/lights from the DCC file (keep game mesh + armature).
    for ob in list(bpy.data.objects):
        if ob.name in {"Ground", "Key", "Fill", "Rim", "WorldPlay"}:
            bpy.data.objects.remove(ob, do_unlink=True)

    fbx = PACK3D / "sir_aldric.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(fbx),
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        axis_forward="Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        path_mode="COPY",
        embed_textures=True,
    )
    print("fbx", fbx, fbx.stat().st_size)

    blend = PACK3D / "sir_aldric.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    print("blend", blend, blend.stat().st_size)

    ntris = export_mesh_txt(body, mats, PACK3D / "sir_aldric_midpoly.mesh.txt")
    export_obj(body, PACK3D / "sir_aldric_midpoly.obj")
    write_sidecars(ntris, nverts, nfaces)
    print("done verts", nverts, "faces", nfaces, "tris", ntris)


if __name__ == "__main__":
    main()
