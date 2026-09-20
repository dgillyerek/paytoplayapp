#!/usr/bin/env python3
"""Headless Blender build: mid-poly Sir Aldric from look_targets SoT.

Path B (Derek). Models / UV / textures / Humanoid-ready rig, then writes:
  - sir_aldric.fbx          Unity Generic + Humanoid map
  - sir_aldric.blend
  - sir_aldric_midpoly.mesh.txt   bone-local runtime bind (Editor-less Actor)
  - sir_aldric_midpoly.obj
  - sir_aldric_3d.json / README

Coordinates are Unity Y-up, +Z forward (TOP), character-right = +X.
Bone names and rest offsets match SirAldric3DActor — do not drift Evaluate().
Scabbard = character-RIGHT per 01_rear_LOCKED (ignore 02 if mirrored).
"""
from __future__ import annotations

import json
import math
import subprocess
import sys
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Matrix, Vector

# ---------------------------------------------------------------------------
# Paths / atlas UVs (V-up, same layout as build_sir_aldric_atlas.py)
# ---------------------------------------------------------------------------
ROOT = Path(__file__).resolve().parents[2]
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
ATLAS = PACK3D / "sir_aldric_atlas.png"

UV_SILVER = (0.125, 0.875)
UV_GOLD = (0.375, 0.875)
UV_BLUE = (0.625, 0.875)
UV_BROWN = (0.875, 0.875)
# lion rect u0,v0,u1,v1  (V-up)
LION = (0.02, 0.08, 0.48, 0.72)
HEM = (0.52, 0.62, 0.98, 0.78)

# Actor rest (Unity). Must match SirAldric3DActor.BuildRig / preview fk().
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
# Unity Humanoid slot ← custom bone (Generic clip keeps custom names).
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


def eul_xyz_deg(x, y, z):
    rx, ry, rz = math.radians(x), math.radians(y), math.radians(z)
    return Matrix.Rotation(ry, 4, "Y") @ Matrix.Rotation(rx, 4, "X") @ Matrix.Rotation(rz, 4, "Z")


def rest_world():
    """Bone rest world matrices — same graph as SirAldric3DActor / preview fk()."""
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


# ---------------------------------------------------------------------------
# bmesh primitives (Unity Y-up world)
# ---------------------------------------------------------------------------
def add_ellipsoid(bm, center, radii, segs=16, rings=10):
    ret = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    c = Vector(center)
    rx, ry, rz = radii
    for v in ret["verts"]:
        v.co = Vector((v.co.x * rx, v.co.y * ry, v.co.z * rz)) + c
    return ret["verts"]


def add_capsule(bm, p0, p1, radius, segs=14):
    a, b = Vector(p0), Vector(p1)
    axis = b - a
    length = axis.length
    if length < 1e-6:
        return add_ellipsoid(bm, a, (radius, radius, radius), segs, segs // 2)
    mid = (a + b) * 0.5
    ret = bmesh.ops.create_cone(
        bm, cap_ends=True, segments=segs, radius1=radius, radius2=radius, depth=length
    )
    quat = Vector((0.0, 0.0, 1.0)).rotation_difference(axis.normalized())
    for v in ret["verts"]:
        v.co = quat @ v.co + mid
    add_ellipsoid(bm, a, (radius, radius, radius), segs, max(6, segs // 2))
    add_ellipsoid(bm, b, (radius, radius, radius), segs, max(6, segs // 2))
    return ret["verts"]


def add_taper(bm, p0, p1, r0, r1, segs=14):
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
    return ret["verts"]


def add_loft(bm, rings):
    """rings: list[list[Vector]] same length. Quad-strip between rings."""
    segs = len(rings[0])
    rows = []
    for ring in rings:
        rows.append([bm.verts.new(Vector(p)) for p in ring])
    bm.verts.ensure_lookup_table()
    for i in range(len(rows) - 1):
        for s in range(segs):
            s1 = (s + 1) % segs
            bm.faces.new((rows[i][s], rows[i][s1], rows[i + 1][s1], rows[i + 1][s]))
    # caps
    bm.faces.new(list(reversed(rows[0])))
    bm.faces.new(rows[-1])
    return [v for row in rows for v in row]


def oval_ring(y, rx, rz, z_off=0.0, segs=20):
    pts = []
    for i in range(segs):
        a = i / segs * math.tau
        pts.append((rx * math.cos(a), y, rz * math.sin(a) + z_off))
    return pts


def assign_uvs(bm, kind):
    """kind: silver/gold/blue/brown/lion_back/hem/mixed_surcoat."""
    uv = bm.loops.layers.uv.new("UVMap")
    bm.faces.ensure_lookup_table()
    bm.normal_update()
    for face in bm.faces:
        n = face.normal
        for loop in face.loops:
            co = loop.vert.co
            loop[uv].uv = Vector(_uv_for(kind, co, n))


def _uv_for(kind, co, n):
    if kind == "silver":
        return UV_SILVER
    if kind == "gold":
        return UV_GOLD
    if kind == "blue":
        return UV_BLUE
    if kind == "brown":
        return UV_BROWN
    if kind == "lion_back":
        return _lion_uv(co)
    if kind == "hem":
        return _hem_uv(co)
    if kind == "surcoat":
        # Hem band
        if co.y < 0.93:
            return _hem_uv(co)
        # Back-facing panel → locked lion (01_rear primary)
        if n.z < -0.15 and 0.98 <= co.y <= 1.46:
            return _lion_uv(co)
        return UV_BLUE
    if kind == "boot":
        # gold trim near ankle / toe
        if co.y > 0.07 or abs(co.z) > 0.09:
            return UV_GOLD
        return UV_SILVER
    if kind == "helm":
        # crest / brow read gold
        if co.y > 1.78 or (abs(co.x) < 0.03 and co.y > 1.68):
            return UV_GOLD
        if abs(co.z) > 0.10 and co.y < 1.66:
            return UV_GOLD
        return UV_SILVER
    if kind == "pauldron":
        if abs(n.y) > 0.55 or co.y < 1.34:
            return UV_GOLD
        return UV_SILVER
    if kind == "gauntlet":
        if co.y > -0.02:
            return UV_GOLD
        return UV_SILVER
    if kind == "greave":
        if abs(co.y + 0.02) < 0.04:
            return UV_GOLD
        return UV_SILVER
    if kind == "scabbard":
        if co.y > 0.06 or co.y < -0.40 or abs(co.y + 0.02) < 0.03:
            return UV_GOLD
        return UV_BROWN
    if kind == "sword":
        if abs(co.y) < 0.06 or co.y > 0.06:
            return UV_GOLD if co.y > 0.02 else UV_SILVER
        return UV_SILVER
    return UV_SILVER


def _lion_uv(co):
    u0, v0, u1, v1 = LION
    u = u0 + (u1 - u0) * _sat((co.x + 0.20) / 0.40)
    v = v0 + (v1 - v0) * _sat((co.y - 1.00) / 0.44)
    return (u, v)


def _hem_uv(co):
    u0, v0, u1, v1 = HEM
    u = u0 + (u1 - u0) * _sat((co.x + 0.26) / 0.52)
    v = v0 + (v1 - v0) * _sat((co.y - 0.80) / 0.14)
    return (u, v)


def _sat(x):
    return 0.0 if x < 0.0 else 1.0 if x > 1.0 else x


def mesh_from_bm(name, bm, kind):
    assign_uvs(bm, kind)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    return obj


# ---------------------------------------------------------------------------
# Knight (world-space, Unity Y-up)
# ---------------------------------------------------------------------------
def build_parts():
    objs = []  # (obj, bone)

    # --- Helm (Head) ---
    bm = bmesh.new()
    add_ellipsoid(bm, (0.0, 1.70, 0.01), (0.115, 0.135, 0.125), segs=18, rings=12)
    add_ellipsoid(bm, (0.0, 1.62, 0.06), (0.10, 0.07, 0.06), segs=14, rings=8)  # visor
    add_taper(bm, (0.0, 1.62, 0.11), (0.0, 1.58, 0.13), 0.055, 0.04, segs=12)
    add_ellipsoid(bm, (0.0, 1.84, 0.0), (0.03, 0.035, 0.03), segs=10, rings=6)  # crest knob
    add_taper(bm, (0.0, 1.68, 0.0), (0.0, 1.84, 0.0), 0.012, 0.018, segs=8)  # crest ridge
    objs.append((mesh_from_bm("Helm", bm, "helm"), "Head"))

    bm = bmesh.new()
    add_capsule(bm, (0.0, 1.50, 0.0), (0.0, 1.58, 0.01), 0.055, segs=12)
    add_ellipsoid(bm, (0.0, 1.50, 0.0), (0.08, 0.03, 0.07), segs=12, rings=6)  # gorget
    objs.append((mesh_from_bm("Gorget", bm, "silver"), "Neck"))

    # --- Surcoat + plate (Chest / Spine / Hips) ---
    bm = bmesh.new()
    segs = 22
    rings = [
        oval_ring(1.50, 0.175, 0.105, 0.00, segs),  # shoulders
        oval_ring(1.38, 0.195, 0.120, -0.01, segs),
        oval_ring(1.22, 0.185, 0.115, -0.01, segs),
        oval_ring(1.08, 0.170, 0.108, 0.00, segs),
        oval_ring(0.96, 0.200, 0.125, 0.01, segs),
        oval_ring(0.88, 0.235, 0.140, 0.02, segs),  # flare
        oval_ring(0.82, 0.250, 0.145, 0.02, segs),  # hem
    ]
    add_loft(bm, rings)
    objs.append((mesh_from_bm("Surcoat", bm, "surcoat"), "Chest"))

    # breast / back plate peeking at collar
    bm = bmesh.new()
    add_ellipsoid(bm, (0.0, 1.36, 0.04), (0.14, 0.10, 0.08), segs=14, rings=8)
    objs.append((mesh_from_bm("Breast", bm, "silver"), "Chest"))

    # gold belt
    bm = bmesh.new()
    add_capsule(bm, (-0.16, 0.99, 0.0), (0.16, 0.99, 0.0), 0.028, segs=10)
    add_ellipsoid(bm, (0.0, 0.99, 0.10), (0.035, 0.03, 0.02), segs=8, rings=5)
    objs.append((mesh_from_bm("Belt", bm, "gold"), "Hips"))

    # pauldrons
    for bone, x in (("Arm_L", -0.24), ("Arm_R", 0.24)):
        bm = bmesh.new()
        add_ellipsoid(bm, (x, 1.42, 0.0), (0.10, 0.08, 0.11), segs=14, rings=9)
        add_ellipsoid(bm, (x, 1.36, 0.0), (0.11, 0.03, 0.12), segs=12, rings=6)  # gold rim
        objs.append((mesh_from_bm(f"Pauldron_{bone}", bm, "pauldron"), bone))

    # --- Arms ---
    for side, x in (("L", -0.22), ("R", 0.22)):
        bm = bmesh.new()
        add_capsule(bm, (x, 1.34, 0.0), (x, 1.10, 0.0), 0.055, segs=12)
        objs.append((mesh_from_bm(f"UpperArm_{side}", bm, "silver"), f"Arm_{side}"))
        bm = bmesh.new()
        add_capsule(bm, (x, 1.08, 0.0), (x, 0.86, 0.0), 0.048, segs=12)
        add_ellipsoid(bm, (x, 1.08, 0.0), (0.06, 0.035, 0.06), segs=10, rings=6)  # elbow cop
        objs.append((mesh_from_bm(f"Fore_{side}", bm, "greave"), f"Fore_{side}"))
        bm = bmesh.new()
        add_ellipsoid(bm, (x, 0.82, 0.01), (0.045, 0.055, 0.04), segs=10, rings=6)
        add_ellipsoid(bm, (x, 0.86, 0.0), (0.05, 0.02, 0.045), segs=8, rings=5)  # cuff
        objs.append((mesh_from_bm(f"Hand_{side}", bm, "gauntlet"), f"Hand_{side}"))

    # --- Sword (held; not a second sheath) ---
    bm = bmesh.new()
    # blade along -Y from Sword bone world ~ (0.24, 0.76, 0.06)
    add_taper(bm, (0.24, 0.78, 0.10), (0.25, 0.22, 0.14), 0.018, 0.008, segs=8)
    add_capsule(bm, (0.18, 0.80, 0.10), (0.30, 0.80, 0.10), 0.012, segs=8)  # guard
    add_ellipsoid(bm, (0.24, 0.84, 0.10), (0.022, 0.025, 0.022), segs=8, rings=5)  # pommel
    add_capsule(bm, (0.24, 0.78, 0.10), (0.24, 0.84, 0.10), 0.012, segs=8)  # grip
    objs.append((mesh_from_bm("SwordMesh", bm, "sword"), "Sword"))

    # --- Legs ---
    for side, x in (("L", -0.11), ("R", 0.11)):
        bm = bmesh.new()
        add_taper(bm, (x, 0.92, 0.0), (x, 0.52, 0.0), 0.080, 0.065, segs=14)
        objs.append((mesh_from_bm(f"Thigh_{side}", bm, "silver"), f"UpLeg_{side}"))
        bm = bmesh.new()
        add_taper(bm, (x, 0.50, 0.0), (x, 0.14, 0.02), 0.062, 0.050, segs=14)
        add_ellipsoid(bm, (x, 0.50, 0.02), (0.07, 0.04, 0.07), segs=10, rings=6)  # poleyn
        objs.append((mesh_from_bm(f"Shin_{side}", bm, "greave"), f"Leg_{side}"))
        # sabaton — silver + gold, not brown leather
        bm = bmesh.new()
        add_ellipsoid(bm, (x, 0.055, 0.07), (0.055, 0.045, 0.11), segs=12, rings=7)
        add_taper(bm, (x, 0.045, 0.12), (x, 0.040, 0.18), 0.045, 0.028, segs=10)
        add_ellipsoid(bm, (x, 0.08, 0.02), (0.05, 0.018, 0.05), segs=8, rings=5)  # gold cuff
        objs.append((mesh_from_bm(f"Boot_{side}", bm, "boot"), f"Foot_{side}"))

    # --- Scabbard character-RIGHT hip only (01 SoT) ---
    bm = bmesh.new()
    # world approx of scabbard bone: (0.20, 0.92, -0.02) then local down -Y rotated
    add_taper(bm, (0.22, 0.98, 0.00), (0.28, 0.48, -0.06), 0.028, 0.020, segs=12)
    add_ellipsoid(bm, (0.22, 1.00, 0.00), (0.032, 0.022, 0.028), segs=8, rings=5)
    add_ellipsoid(bm, (0.25, 0.72, -0.03), (0.026, 0.016, 0.022), segs=8, rings=5)
    add_ellipsoid(bm, (0.28, 0.48, -0.06), (0.024, 0.020, 0.020), segs=8, rings=5)
    objs.append((mesh_from_bm("ScabbardMesh", bm, "scabbard"), "Scabbard"))

    return objs


# ---------------------------------------------------------------------------
# Armature
# ---------------------------------------------------------------------------
def world_head(name, mats):
    return mats[name].to_translation()


def make_armature(mats):
    arm_data = bpy.data.armatures.new("SirAldricArmature")
    arm_data.display_type = "STICK"
    arm_obj = bpy.data.objects.new("SirAldric", arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")

    # Tail points toward the primary child (or along local +Y / -Y).
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
        head = world_head(name, mats)
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
    """Join part meshes into one skinned body; vertex groups = Actor bone names.

    Weights are authored per part *before* join so Hands / Scabbard / Sword stay
    on their bones (closest-bone after join stole Hand_L last run).
    """
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
    bsdf.inputs["Roughness"].default_value = 0.45
    bsdf.inputs["Metallic"].default_value = 0.35
    obj.data.materials.append(mat)


# ---------------------------------------------------------------------------
# Export mesh.txt (bone-local, Actor format)
# ---------------------------------------------------------------------------
def export_mesh_txt(body, mats, path: Path):
    """Write Actor LoadMidPoly format. Verts in bone-local (weight 1)."""
    me = body.data
    me.calc_loop_triangles()
    uv_layer = me.uv_layers.active
    inv = {n: mats[n].inverted() for n in mats}

    # dominant group per vertex
    vg_names = {g.index: g.name for g in body.vertex_groups}
    bone_of = []
    for v in me.vertices:
        if v.groups:
            g = max(v.groups, key=lambda x: x.weight)
            bone_of.append(vg_names.get(g.group, "Hips"))
        else:
            bone_of.append("Hips")

    lines = [
        "# SirAldric Blender mid-poly  look_targets SoT  scabbard=+X  path B",
        "FMT v3 blender",
    ]
    # group triangles by bone
    from collections import defaultdict

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

    # Cape has no mesh (short surcoat only).
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
    print("mesh.txt", path, "tris", sum(len(v) for v in buckets.values()))


def export_obj(body, path: Path):
    me = body.data
    me.calc_loop_triangles()
    uv_layer = me.uv_layers.active
    lines = ["# SirAldric Blender mid-poly — bind pose world (Unity Y-up)", "mtllib sir_aldric.mtl"]
    for v in me.vertices:
        lines.append(f"v {v.co.x:.5f} {v.co.y:.5f} {v.co.z:.5f}")
    for v in me.vertices:
        n = v.normal
        lines.append(f"vn {n.x:.4f} {n.y:.4f} {n.z:.4f}")
    # one uv per loop-tri corner via dummy — write per-vertex first uv
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


def main():
    PACK3D.mkdir(parents=True, exist_ok=True)
    if not ATLAS.exists():
        subprocess.check_call([sys.executable.replace("blender", "python3") if False else "python3",
                               str(ROOT / "scripts/blender/build_sir_aldric_atlas.py")])
    reset_scene()
    mats = rest_world()
    parts = build_parts()
    arm = make_armature(mats)
    body = bind_skin(arm, parts, mats)
    apply_atlas_material(body)

    # FBX — Generic-ready, custom names; Humanoid map is sidecar JSON.
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

    export_mesh_txt(body, mats, PACK3D / "sir_aldric_midpoly.mesh.txt")
    export_obj(body, PACK3D / "sir_aldric_midpoly.obj")
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
                "map": HUMANOID,
                "extras": {"Sword": "Sword", "Scabbard": "Scabbard", "Cape": "Cape (bone only, no mesh)"},
                "forward": "+Z",
                "up": "+Y",
                "characterRight": "+X",
            },
            indent=2,
        )
        + "\n"
    )
    manifest = {
        "name": "SirAldric",
        "track": "blender-fbx",
        "soT": "look_targets/01_rear_LOCKED.png",
        "scabbard": "character-right",
        "atlas": "sir_aldric_atlas.png",
        "mesh": "sir_aldric_midpoly.mesh.txt",
        "fbx": "sir_aldric.fbx",
        "blend": "sir_aldric.blend",
        "lookPassClaimed": False,
    }
    (PACK3D / "sir_aldric_3d.json").write_text(json.dumps(manifest, indent=2) + "\n")
    (PACK3D / "README.md").write_text(
        "# Sir Aldric 3D (Theme A) — Blender / FBX\n\n"
        "**Path B.** Mid-poly modeled in Blender from `look_targets` "
        "(`01_rear_LOCKED` = primary Game-view SoT). Box-atlas iteration is stopped.\n\n"
        "Runtime bind (Editor-less VM): `sir_aldric_midpoly.mesh.txt` + "
        "`sir_aldric_atlas.png` on `SirAldric3DActor` (same bones / `Evaluate()` clip).\n\n"
        "DCC / Unity import: `sir_aldric.fbx` + `sir_aldric.blend`. "
        "Humanoid slot map: `sir_aldric_humanoid.json`. Rest pose is the Actor hang "
        "(not T-pose) so Walk/Attack locks do not regress.\n\n"
        "Must-match:\n"
        "- Silver plate + gold trim (helm crest, pauldrons, gauntlets, greaves, boots)\n"
        "- Royal-blue short surcoat: gold lion rampant + gold Greek-key hem\n"
        "- Brown scabbard + gold fittings on **character-RIGHT** hip only (no back sheath)\n"
        "- Boots silver+gold (not brown leather)\n\n"
        "Look gate is **not claimed** until Derek PASSes the Game-view clip.\n"
        "Play hub stays `SIR_ALDRIC_REAR_MASTER_LOCKED.png`.\n"
    )
    print("done verts", len(body.data.vertices), "faces", len(body.data.polygons))


if __name__ == "__main__":
    main()
