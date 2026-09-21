#!/usr/bin/env python3
"""Skin Path 2 Meshy look to held Actor bones + Game-view rear walk clip.

Evaluate() / gait tables are COPIED not edited. Look PASS is Design's.
Walk-with-look is NOT claimed. Play hub PNG not swapped.

89481e5 FAIL: A-pose spatial boxes stole tabard→arm slabs, split sheath
(core on Scabbard rest-rotated, shell on Hips), and Y-cuts tore mid/legs.
This bind: weld stacked shells, cloth locked to torso, one scabbard island,
hang-corridor arm/leg volumes. No gait rewrite. No hub swap.
"""
from __future__ import annotations

import json
import math
import os
import subprocess
from collections import defaultdict, deque
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Matrix, Vector
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
SRC_GLB = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AI_MESH_PATH2/out/aldric_meshy_retopo.glb"
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
ART = Path("/opt/cursor/artifacts")
TARGET_H = 1.86

# Actor rest — same as SirAldric3DActor.BuildRig / build_sir_aldric_gate3.BONE_REST.
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
MESH_BONES = [
    "Head", "Neck", "Chest", "Spine", "Hips",
    "Arm_L", "Fore_L", "Hand_L", "Arm_R", "Fore_R", "Hand_R",
    "UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R", "Scabbard",
]

CAM_EYE = (0.0, 2.80, -5.40)
CAM_TARGET = (0.0, 0.90, 0.50)
CAM_FOV = 30.0

# Same WalkPose keys as SirAldric3DMotion / render_sir_aldric_3d_preview.py
WALK_PERIOD = 1.00
WALK_CYCLES = 2
MARCH = 0.80
HIPSY = [-3.0, 5.0, 3.0, -5.0]
HIPSZ = [5.5, 1.5, -5.5, 1.5]
SPINEY = [8.0, -8.0, -8.0, 8.0]
UPLX = [-18.0, -12.0, 8.0, 12.0]
UPLZ = [0.0, 0.0, 8.0, 0.0]
LEGL = [80.0, 12.0, 14.0, 18.0]
FOOTL = [16.0, -12.0, -6.0, 14.0]
UPRX = [8.0, 12.0, -18.0, -12.0]
UPRZ = [-8.0, 0.0, 0.0, 0.0]
LEGR = [14.0, 16.0, 80.0, 12.0]
FOOTR = [-6.0, 14.0, 16.0, -12.0]
ARMLX = [36.0, 32.0, -32.0, -34.0]
ARMRX = [-32.0, -34.0, 24.0, 22.0]


def lerp(a, b, t):
    return a + (b - a) * t


def sample_keys(keys, u):
    n = len(keys)
    x = (u % 1.0) * n
    i0 = int(math.floor(x)) % n
    i1 = (i0 + 1) % n
    return lerp(keys[i0], keys[i1], x - math.floor(x))


def walk_pose(loop_t):
    u = (loop_t / WALK_PERIOD) % 1.0
    hips_y, hips_z = sample_keys(HIPSY, u), sample_keys(HIPSZ, u)
    spine_y = sample_keys(SPINEY, u)
    bob = 0.012 + 0.018 * abs(math.cos(u * math.pi * 2.0))
    return {
        "root_z": loop_t * MARCH,
        "root_y": bob,
        "hips": (0.0, hips_y, hips_z),
        "spine": (5.0, spine_y, -hips_z),
        "chest": (2.0, spine_y * 0.5, 0.0),
        "head": (6.0, spine_y * 0.25, 0.0),
        "up_l": (sample_keys(UPLX, u), 0.0, sample_keys(UPLZ, u)),
        "leg_l": (sample_keys(LEGL, u), 0.0, 0.0),
        "foot_l": (sample_keys(FOOTL, u), 0.0, 0.0),
        "up_r": (sample_keys(UPRX, u), 0.0, sample_keys(UPRZ, u)),
        "leg_r": (sample_keys(LEGR, u), 0.0, 0.0),
        "foot_r": (sample_keys(FOOTR, u), 0.0, 0.0),
        "arm_l": (sample_keys(ARMLX, u), 0.0, -22.0),
        "fore_l": (-18.0, 0.0, 0.0),
        "arm_r": (sample_keys(ARMRX, u), 4.0, 22.0),
        "fore_r": (-22.0, 0.0, 0.0),
        "hand_r": (0.0, 0.0, 0.0),
        "sword": (-6.0, 0.0, 8.0),
    }


POSE_BONE = {
    "Hips": "hips",
    "Spine": "spine",
    "Chest": "chest",
    "Head": "head",
    "Arm_L": "arm_l",
    "Fore_L": "fore_l",
    "Arm_R": "arm_r",
    "Fore_R": "fore_r",
    "Hand_R": "hand_r",
    "Sword": "sword",
    "UpLeg_L": "up_l",
    "Leg_L": "leg_l",
    "Foot_L": "foot_l",
    "UpLeg_R": "up_r",
    "Leg_R": "leg_r",
    "Foot_R": "foot_r",
}


def unity_q(eul):
    """Same as preview FK: Ry @ Rx @ Rz (Unity Euler)."""
    x, y, z = (math.radians(c) for c in eul)
    rx = Matrix.Rotation(x, 4, "X")
    ry = Matrix.Rotation(y, 4, "Y")
    rz = Matrix.Rotation(z, 4, "Z")
    return (ry @ rx @ rz).to_quaternion()


def import_and_orient():
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
    ob.name = "SirAldricMeshy"
    me = ob.data
    for v in me.vertices:
        x, y, z = v.co
        v.co = Vector((-x, z, -y))
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    ys = [v.co.y for v in me.vertices]
    ymin, ymax = min(ys), max(ys)
    scale = TARGET_H / max(1e-6, ymax - ymin)
    for v in me.vertices:
        v.co = Vector((v.co.x * scale, (v.co.y - ymin) * scale, v.co.z * scale))
    me.update()
    # Meshy/decimate stacked duplicate shells (11k islands). Weld so Bone1
    # cannot slide two copies of the same plate/sheath apart.
    bm = bmesh.new()
    bm.from_mesh(me)
    before = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.001)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    print("weld 0.001", before, "->", len(bm.verts), "faces", len(bm.faces))
    bm.free()
    me.update()
    for p in me.polygons:
        p.use_smooth = True
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception:
        pass
    bpy.ops.object.mode_set(mode="OBJECT")
    xs = [v.co.x for v in me.vertices]
    print(
        "oriented",
        round(min(xs), 3), round(max(xs), 3),
        "scabbard+X" if max(xs) > abs(min(xs)) else "WARN",
        "mats", [m.name for m in me.materials if m],
    )
    return ob


def build_armature():
    arm = bpy.data.armatures.new("AldricArm")
    ob = bpy.data.objects.new("AldricArm", arm)
    bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    ebones = {}
    world = {}
    for name, (loc, eul) in BONE_REST.items():
        parent = PARENT[name]
        if parent is None:
            mw = Matrix.Translation(loc)
        else:
            mw = world[parent] @ Matrix.Translation(loc) @ unity_q(eul).to_matrix().to_4x4()
        world[name] = mw
        b = arm.edit_bones.new(name)
        head = mw.to_translation()
        # Bone +Y = world +Y so pose quats match Actor localRotation.
        tail = head + Vector((0.0, 0.08, 0.0))
        b.head = head
        b.tail = tail
        b.roll = 0.0
        ebones[name] = b
    for name, parent in PARENT.items():
        if parent:
            ebones[name].parent = ebones[parent]
            ebones[name].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob, world


_SCAB_A = Vector((0.20, 1.04, -0.04))
_SCAB_B = Vector((0.38, 0.30, 0.05))
_ARM_R_A = Vector((0.23, 1.42, 0.00))
_ARM_R_B = Vector((0.31, 0.66, 0.03))
_ARM_L_A = Vector((-0.23, 1.42, 0.00))
_ARM_L_B = Vector((-0.31, 0.66, 0.03))
_LEG_R_A = Vector((0.11, 0.94, 0.00))
_LEG_R_B = Vector((0.12, 0.08, 0.04))
_LEG_L_A = Vector((-0.11, 0.94, 0.00))
_LEG_L_B = Vector((-0.12, 0.08, 0.04))


def _dist_seg(p: Vector, a: Vector, b: Vector) -> tuple[float, float]:
    ab = b - a
    denom = max(1e-9, ab.length_squared)
    t = max(0.0, min(1.0, (p - a).dot(ab) / denom))
    return (p - (a + t * ab)).length, t


def _sample_albedo(me) -> tuple[list[tuple[int, int, int]], list[bool]]:
    """Per-vertex RGB from Image_0 + lion-card flags."""
    imgs = {img.name: img for img in bpy.data.images if img.size[0] >= 64}
    albedo = None
    for name, img in imgs.items():
        if "Image_0" in name:
            albedo = img
            break
    if albedo is None:
        albedo = next(iter(imgs.values())) if imgs else None
    rgb_of = [(128, 128, 128)] * len(me.vertices)
    lion = [False] * len(me.vertices)
    if albedo is None:
        return rgb_of, lion
    path = Path("/tmp/path2-skin/Image_0.png")
    path.parent.mkdir(parents=True, exist_ok=True)
    albedo.filepath_raw = str(path)
    albedo.file_format = "PNG"
    albedo.save()
    arr = np.array(Image.open(path).convert("RGB"))
    ah, aw = arr.shape[:2]
    uv_layer = me.uv_layers.active
    acc = defaultdict(list)
    for poly in me.polygons:
        mat = me.materials[poly.material_index] if me.materials else None
        is_lion = bool(mat and "lion" in mat.name.lower())
        for li, vi in zip(poly.loop_indices, poly.vertices):
            if is_lion:
                lion[vi] = True
            if uv_layer:
                u, v = uv_layer.data[li].uv
                x = int(max(0, min(aw - 1, float(u) * aw)))
                y = int(max(0, min(ah - 1, (1.0 - float(v)) * ah)))
                acc[vi].append(tuple(int(c) for c in arr[y, x]))
    for i, samples in acc.items():
        n = len(samples)
        rgb_of[i] = (
            sum(s[0] for s in samples) // n,
            sum(s[1] for s in samples) // n,
            sum(s[2] for s in samples) // n,
        )
    return rgb_of, lion


def _is_blue(rgb: tuple[int, int, int]) -> bool:
    r, g, b = rgb
    return b > r + 12 and b > 45 and r < 110


def _assign_all(mesh_ob) -> list[str]:
    """Hang volumes on hang bones. Cloth never on limbs. One scabbard."""
    me = mesh_ob.data
    n = len(me.vertices)
    rgb_of, lion = _sample_albedo(me)
    adj = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)

    cloth = [False] * n
    for i, v in enumerate(me.vertices):
        p = v.co
        d_ar, _ = _dist_seg(p, _ARM_R_A, _ARM_R_B)
        d_al, _ = _dist_seg(p, _ARM_L_A, _ARM_L_B)
        in_arm_tube = (d_ar < 0.070 and p.x > 0.19) or (d_al < 0.070 and p.x < -0.19)
        if (lion[i] or _is_blue(rgb_of[i])) and not in_arm_tube:
            cloth[i] = True
        # Center tabard / hem only — stop before hang-arm x.
        if 0.52 <= p.y <= 1.12 and abs(p.x) < 0.205 and abs(p.z) < 0.22 and not in_arm_tube:
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

    scab_seeds = []
    for i, v in enumerate(me.vertices):
        p = v.co
        if cloth[i] or p.x < 0.155:
            continue
        d, _ = _dist_seg(p, _SCAB_A, _SCAB_B)
        if d < 0.048 and p.y < 1.16:
            scab_seeds.append(i)
    scabbard = flood(
        scab_seeds,
        lambda i: (
            not cloth[i]
            and me.vertices[i].co.x > 0.14
            and me.vertices[i].co.y < 1.18
            and _dist_seg(me.vertices[i].co, _SCAB_A, _SCAB_B)[0] < 0.055
        ),
    )

    def arm_pred(side_x: float):
        a = _ARM_R_A if side_x > 0 else _ARM_L_A
        b = _ARM_R_B if side_x > 0 else _ARM_L_B

        def pred(i, a=a, b=b, side_x=side_x):
            if i in scabbard or cloth[i]:
                return False
            p = me.vertices[i].co
            if p.x * side_x < 0:
                return False
            if p.y < 0.55:
                return False
            d, _ = _dist_seg(p, a, b)
            # Narrow hang corridor. Do not ingest the tabard.
            if d < 0.068 and 0.62 < p.y < 1.48 and abs(p.x) > 0.19:
                return True
            if p.y < 0.86 and abs(p.x) > 0.27 and d < 0.10:
                return True
            return False

        return pred

    arm_r = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x > 0.27 and 0.58 < v.co.y < 0.90
         and i not in scabbard and not cloth[i]],
        arm_pred(1.0),
    )
    arm_l = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x < -0.27 and 0.58 < v.co.y < 0.90 and not cloth[i]],
        arm_pred(-1.0),
    )

    def leg_pred(side_x: float):
        a = _LEG_R_A if side_x > 0 else _LEG_L_A
        b = _LEG_R_B if side_x > 0 else _LEG_L_B

        def pred(i, a=a, b=b, side_x=side_x):
            if i in scabbard or cloth[i]:
                return False
            p = me.vertices[i].co
            if p.x * side_x <= 0:
                return False
            if p.y > 0.96:
                return False
            d, _ = _dist_seg(p, a, b)
            return d < 0.11 and abs(p.x) > 0.04

        return pred

    leg_r = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x > 0.04 and v.co.y < 0.28 and i not in scabbard and not cloth[i]],
        leg_pred(1.0),
    )
    leg_l = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x < -0.04 and v.co.y < 0.28 and not cloth[i]],
        leg_pred(-1.0),
    )

    bone_of = ["Hips"] * n
    for i, v in enumerate(me.vertices):
        p = v.co
        if i in scabbard:
            bone_of[i] = "Scabbard"
            continue
        if i in arm_r:
            # One hang volume per arm. Bone1 splits at elbow shred a thin
            # plate surface into stacked sheets under Evaluate().
            bone_of[i] = "Arm_R"
            continue
        if i in arm_l:
            bone_of[i] = "Arm_L"
            continue
        if i in leg_r:
            if p.y < 0.22:
                bone_of[i] = "Foot_R"
            elif p.y < 0.56:
                bone_of[i] = "Leg_R"
            else:
                bone_of[i] = "UpLeg_R"
            continue
        if i in leg_l:
            if p.y < 0.22:
                bone_of[i] = "Foot_L"
            elif p.y < 0.56:
                bone_of[i] = "Leg_L"
            else:
                bone_of[i] = "UpLeg_L"
            continue
        if cloth[i]:
            if p.y > 1.28:
                bone_of[i] = "Chest"
            elif p.y > 1.10:
                bone_of[i] = "Spine"
            else:
                bone_of[i] = "Hips"
            continue
        if p.y > 1.50:
            bone_of[i] = "Head"
        elif p.y > 1.42 and abs(p.x) < 0.18:
            bone_of[i] = "Neck"
        elif p.y > 1.28:
            bone_of[i] = "Chest"
        elif p.y > 1.10:
            bone_of[i] = "Spine"
        else:
            bone_of[i] = "Hips"

    # Spatial hang-arm fill. Flood often stops at the cloth/forearm overlap;
    # the painted hang arm must ride Arm/Fore/Hand as one volume.
    for i, v in enumerate(me.vertices):
        if i in scabbard or cloth[i]:
            continue
        p = v.co
        d_r, _ = _dist_seg(p, _ARM_R_A, _ARM_R_B)
        d_l, _ = _dist_seg(p, _ARM_L_A, _ARM_L_B)
        if p.x > 0.19 and d_r < 0.072 and 0.60 < p.y < 1.52:
            arm_r.add(i)
            bone_of[i] = "Arm_R"
        elif p.x < -0.19 and d_l < 0.072 and 0.60 < p.y < 1.52:
            arm_l.add(i)
            bone_of[i] = "Arm_L"

    locked = scabbard | arm_r | arm_l | leg_r | leg_l
    for _ in range(3):
        nxt = bone_of[:]
        for i in range(n):
            if i in scabbard:
                continue
            neigh = [bone_of[j] for j in adj[i]]
            if not neigh:
                continue
            winner = max(set(neigh), key=neigh.count)
            if winner != bone_of[i] and neigh.count(winner) >= max(2, (len(neigh) * 2) // 3):
                # cloth must not migrate onto limbs
                if cloth[i] and winner not in ("Hips", "Spine", "Chest", "Neck"):
                    continue
                if i in locked and winner == "Scabbard":
                    nxt[i] = winner
                    continue
                if i in locked and winner.startswith(("Arm", "Fore", "Hand", "UpLeg", "Leg", "Foot")):
                    nxt[i] = winner
                    continue
                if i not in locked:
                    nxt[i] = winner
        bone_of = nxt

    print(
        "islands scab", len(scabbard),
        "armR", len(arm_r), "armL", len(arm_l),
        "legR", len(leg_r), "legL", len(leg_l),
        "cloth", sum(cloth),
    )
    return bone_of


def skin_mesh(mesh_ob, arm_ob):
    bone_of = _assign_all(mesh_ob)
    counts = defaultdict(int)
    groups = {n: mesh_ob.vertex_groups.new(name=n) for n in MESH_BONES}
    for i, bone in enumerate(bone_of):
        if bone not in groups:
            bone = "Hips"
        groups[bone].add([i], 1.0, "REPLACE")
        counts[bone] += 1
    print("weights", dict(counts))
    if counts.get("Scabbard", 0) < 180:
        raise SystemExit(f"scabbard verts too few: {counts.get('Scabbard')}")
    if counts.get("Scabbard", 0) > 1800:
        raise SystemExit(f"scabbard stole the arm: {counts.get('Scabbard')}")
    if counts.get("Head", 0) < 80:
        raise SystemExit(f"head verts too few: {counts.get('Head')}")
    if counts.get("Arm_R", 0) < 200 or counts.get("Arm_L", 0) < 200:
        raise SystemExit(f"arm volume empty: R={counts.get('Arm_R')} L={counts.get('Arm_L')}")
    if counts.get("Arm_R", 0) > 4000 or counts.get("Arm_L", 0) > 4000:
        raise SystemExit(f"arm stole tabard: R={counts.get('Arm_R')} L={counts.get('Arm_L')}")
    mod = mesh_ob.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_ob
    mesh_ob.parent = arm_ob
    return counts


def play_cam_matrix(eye, target) -> Matrix:
    eye_v = Vector(eye)
    fwd = (Vector(target) - eye_v).normalized()
    world_up = Vector((0.0, 1.0, 0.0))
    right = world_up.cross(fwd).normalized()
    up = fwd.cross(right).normalized()
    return Matrix(
        (
            (right.x, up.x, -fwd.x, eye_v.x),
            (right.y, up.y, -fwd.y, eye_v.y),
            (right.z, up.z, -fwd.z, eye_v.z),
            (0.0, 0.0, 0.0, 1.0),
        )
    )


def setup_render():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    try:
        sc.eevee.taa_render_samples = 12
    except AttributeError:
        pass
    try:
        sc.view_settings.view_transform = "Standard"
    except Exception:
        pass
    world = bpy.data.worlds.new("WalkWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.12, 0.13, 0.11, 1.0)
        bg.inputs[1].default_value = 1.15
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 7.2
    key.angle = 0.08
    key_o = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(key_o)
    key_o.location = (1.4, 3.6, -4.2)
    key_o.rotation_euler = (math.radians(55), 0.0, math.radians(18))
    fill = bpy.data.lights.new("Fill", "AREA")
    fill.energy = 320.0
    fill.size = 5.0
    fill_o = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fill_o)
    fill_o.location = (-2.2, 2.4, -1.5)
    fill_o.rotation_euler = (math.radians(70), 0.0, math.radians(-25))
    # Ground plane back: Design HARD FAIL 2820e53 = float + 4-leg.
    # Contact proof needs soles ON this plane. Banding census is deferred.
    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    ground.name = "Ground"
    mat = bpy.data.materials.new("GroundMat")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.10, 0.11, 0.09, 1)
        bsdf.inputs["Roughness"].default_value = 0.9
    ground.data.materials.append(mat)
    cam_data = bpy.data.cameras.new("WorldPlay")
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(CAM_FOV)
    cam_data.sensor_fit = "VERTICAL"
    cam_data.clip_start = 0.08
    cam_data.clip_end = 40.0
    cam = bpy.data.objects.new("WorldPlay", cam_data)
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    cam.matrix_world = play_cam_matrix(CAM_EYE, CAM_TARGET)
    return cam


def apply_pose(arm_ob, pose, mesh_ob=None):
    """Evaluate() keys, then optional sole plant. Does not rewrite gait locks."""
    arm_ob.location = (0.0, pose["root_y"], pose["root_z"])
    for bname, key in POSE_BONE.items():
        pb = arm_ob.pose.bones.get(bname)
        if not pb:
            continue
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = unity_q(pose[key])
    if mesh_ob is not None:
        plant_soles(arm_ob, mesh_ob)


def plant_soles(arm_ob, mesh_ob):
    """Snap root Y so the lowest Foot_* sole kisses ground Y=0.

    2820e53 float: rest Foot ymin~0.026 + bob 0.012–0.030 + stance-knee
    lift. Gait keys stay; this is a root bind offset after Evaluate().
    """
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    ev = mesh_ob.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    vg = {g.index: g.name for g in mesh_ob.vertex_groups}
    foot = {"Foot_L", "Foot_R"}
    ys = []
    src = mesh_ob.data
    n = min(len(me.vertices), len(src.vertices))
    for i in range(n):
        names = {vg.get(g.group, "") for g in src.vertices[i].groups if g.weight > 0.45}
        if names & foot:
            ys.append((mw @ me.vertices[i].co).y)
    ev.to_mesh_clear()
    if not ys:
        return 0.0
    dy = min(ys) - 0.002
    arm_ob.location.y -= dy
    bpy.context.view_layer.update()
    return dy


def render_walk(arm_ob, mesh_ob=None):
    WALK.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    fps = 16
    stills_only = os.environ.get("SKIN_STILLS_ONLY") == "1"
    if not stills_only:
        duration = WALK_PERIOD * WALK_CYCLES
        n = int(duration * fps)
        for i in range(n):
            t = i / fps
            apply_pose(arm_ob, walk_pose(t), mesh_ob)
            path = WALK / f"f_{i:03d}.png"
            bpy.context.scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            print("frame", i, path.stat().st_size)

    stills = {
        "pass_l": 0.0,
        "contact_l": WALK_PERIOD * 0.25,
        "pass_r": WALK_PERIOD * 0.50,
        "contact_r": WALK_PERIOD * 0.75,
    }
    for name, t in stills.items():
        apply_pose(arm_ob, walk_pose(t), mesh_ob)
        path = WALK / f"world_walk_{name}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("still", name, path.stat().st_size)

    mp4 = PROOF / "sir_aldric_path2_walk_toward_top.mp4"
    if not stills_only:
        subprocess.check_call(
            [
                "ffmpeg", "-y", "-framerate", str(fps),
                "-i", str(WALK / "f_%03d.png"),
                "-pix_fmt", "yuv420p", "-vf", "scale=1080:1920",
                "-crf", "18", "-movflags", "+faststart", str(mp4),
            ]
        )
        print("mp4", mp4, mp4.stat().st_size)
        dest = ART / mp4.name
        dest.write_bytes(mp4.read_bytes())
    for name in stills:
        src = WALK / f"world_walk_{name}.png"
        (ART / f"gate3_{src.name}").write_bytes(src.read_bytes())
    return mp4


def combine_atlas_and_remap(ob):
    """Single atlas for Actor: Image_0 + lion cards in a top strip."""
    me = ob.data
    imgs = {img.name: img for img in bpy.data.images if img.size[0] >= 64}
    albedo = None
    for name, img in imgs.items():
        if "Image_0" in name or name == "Image_0":
            albedo = img
            break
    if albedo is None:
        albedo = next(iter(imgs.values()))
    a_path = Path("/tmp/path2-skin/Image_0.png")
    a_path.parent.mkdir(exist_ok=True)
    albedo.filepath_raw = str(a_path)
    albedo.file_format = "PNG"
    albedo.save()
    base = np.array(Image.open(a_path).convert("RGB"))
    bh, bw = base.shape[:2]

    lion_imgs = {}
    for mat in me.materials:
        if not mat or not mat.use_nodes:
            continue
        lname = mat.name.lower()
        if "lion" not in lname:
            continue
        for n in mat.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image:
                p = Path("/tmp/path2-skin") / f"{mat.name}.png"
                n.image.filepath_raw = str(p)
                n.image.file_format = "PNG"
                n.image.save()
                lion_imgs[mat.name] = (np.array(Image.open(p).convert("RGB")), mat)
                break

    strip = 640
    atlas_h = bh + strip
    atlas = np.zeros((atlas_h, bw, 3), np.uint8)
    atlas[strip : strip + bh] = base
    # cards at top of PNG (y=0)
    slots = {}
    x0 = 0
    for name, (arr, _mat) in lion_imgs.items():
        card = Image.fromarray(arr).resize((512, strip), Image.LANCZOS)
        atlas[0:strip, x0 : x0 + 512] = np.array(card)
        slots[name] = (x0 / bw, 0.0, (x0 + 512) / bw, strip / atlas_h)
        x0 += 512
        print("atlas slot", name, slots[name])

    out_png = PACK3D / "sir_aldric_meshy_atlas.png"
    Image.fromarray(atlas, "RGB").save(out_png)
    print("atlas", out_png, out_png.stat().st_size)

    # Remap UVs. Blender v=0 bottom; PIL sample uses (1-v).
    # Image_0 lives in PNG y=strip..strip+bh → v in [0, bh/atlas_h]
    uv = me.uv_layers.active.data
    mat_of = [me.polygons[i].material_index for i in range(len(me.polygons))]
    lion_face = set()
    for pi, poly in enumerate(me.polygons):
        mat = me.materials[poly.material_index] if me.materials else None
        if mat and mat.name in slots:
            lion_face.add(pi)
    v_img0 = bh / atlas_h
    for poly in me.polygons:
        mat = me.materials[poly.material_index] if me.materials else None
        if mat and mat.name in slots:
            u0, v0, u1, v1 = slots[mat.name]
            # PNG top strip: v_blender high = PNG top = v~1 in sample space.
            # slot stored as PNG-space (y=0 top). Blender v=0 is bottom of atlas.
            # PNG y=0..strip → blender v = 1 - y/atlas_h .. 1 - 0 = (atlas_h-strip)/atlas_h .. 1
            bv0 = 1.0 - (strip / atlas_h)  # bottom of strip in blender v
            bv1 = 1.0
            for li in poly.loop_indices:
                u, v = uv[li].uv
                uv[li].uv = Vector((u0 + float(u) * (u1 - u0), bv0 + float(v) * (bv1 - bv0)))
        else:
            for li in poly.loop_indices:
                u, v = uv[li].uv
                uv[li].uv = Vector((float(u), float(v) * v_img0))
    print("uv remap lion faces", len(lion_face), "img0 v scale", round(v_img0, 4))
    return out_png


def export_mesh_txt(mesh_ob, world):
    me = mesh_ob.data
    me.calc_loop_triangles()
    uv_layer = me.uv_layers.active
    inv = {n: world[n].inverted() for n in world}
    vg_names = {g.index: g.name for g in mesh_ob.vertex_groups}
    bone_of = []
    for v in me.vertices:
        if v.groups:
            g = max(v.groups, key=lambda x: x.weight)
            bone_of.append(vg_names.get(g.group, "Hips"))
        else:
            bone_of.append("Hips")
    lines = [
        "# SirAldric Path 2 Meshy look  clay-volume-law 5de0e16  scabbard=+X  blender",
        "FMT v3 blender gate3 meshy-path2",
    ]
    buckets = defaultdict(list)
    for tri in me.loop_triangles:
        bones = [bone_of[i] for i in tri.vertices]
        bone = max(set(bones), key=bones.count)
        if bone not in inv:
            bone = "Hips"
        iw = inv[bone]
        pts, nrms, uvs = [], [], []
        for i, li in zip(tri.vertices, tri.loops):
            loc = iw @ me.vertices[i].co.to_4d()
            pts.append((loc.x, loc.y, loc.z))
            ln = (iw.to_3x3() @ me.vertices[i].normal).normalized()
            nrms.append((ln.x, ln.y, ln.z))
            if uv_layer:
                uv = uv_layer.data[li].uv
                uvs.append((float(uv.x), float(uv.y)))
            else:
                uvs.append((0.5, 0.5))
        buckets[bone].append((pts, nrms, uvs))
    for bone in MESH_BONES:
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
    path = PACK3D / "sir_aldric_meshy.mesh.txt"
    path.write_text("\n".join(lines) + "\n")
    ntris = sum(len(v) for v in buckets.values())
    print("mesh.txt", path, "tris", ntris, "bytes", path.stat().st_size)
    if "BONE Scabbard" not in path.read_text()[:5000] and "BONE Scabbard" not in path.read_text():
        raise SystemExit("missing BONE Scabbard")
    if "BONE Cape" in path.read_text():
        raise SystemExit("Cape mesh leaked")
    meta = PACK3D / "sir_aldric_meshy.mesh.txt.meta"
    meta.write_text(
        "fileFormatVersion: 2\n"
        "guid: c2e52d0000000000000000000000bb03\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )
    return ntris, {k: len(v) for k, v in buckets.items()}


def main():
    if not SRC_GLB.exists():
        raise SystemExit(f"missing {SRC_GLB}")
    mesh_ob = import_and_orient()
    arm_ob, world = build_armature()
    counts = skin_mesh(mesh_ob, arm_ob)
    setup_render()
    mp4 = render_walk(arm_ob)
    atlas = combine_atlas_and_remap(mesh_ob)
    ntris, buckets = export_mesh_txt(mesh_ob, world)
    blend = PACK3D / "sir_aldric_meshy_skinned.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "bind": "weld 1mm stacked shells; cloth locked to torso; one scabbard island; one Bone1 hang-arm volume per side (no elbow split)",
        "motion": "5916447 Evaluate() keys reused — gait/weave/forward-swing not edited",
        "scabbard": "character-right",
        "playHubLocked": True,
        "vertsWeighted": counts,
        "tris": ntris,
        "boneTris": buckets,
        "atlas": atlas.name,
        "mesh": "sir_aldric_meshy.mesh.txt",
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done skin", note["walkClip"], "tris", ntris)


if __name__ == "__main__":
    main()
