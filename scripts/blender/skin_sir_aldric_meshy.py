#!/usr/bin/env python3
"""Skin Path 2 Meshy look to held Actor bones + Game-view rear walk clip.

Evaluate() / gait tables are COPIED not edited. Look PASS is Design's.
Walk-with-look is NOT claimed. Play hub PNG not swapped.
"""
from __future__ import annotations

import json
import math
import subprocess
from collections import defaultdict
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


_SCAB_A = Vector((0.22, 1.00, -0.04))
_SCAB_B = Vector((0.45, 0.28, 0.06))
_SCAB_AB = _SCAB_B - _SCAB_A
_SCAB_AB2 = _SCAB_AB.length_squared


def _dist_scabbard(p: Vector) -> float:
    t = max(0.0, min(1.0, (p - _SCAB_A).dot(_SCAB_AB) / _SCAB_AB2))
    return (p - (_SCAB_A + t * _SCAB_AB)).length


def assign_bone(p: Vector) -> str:
    """A-pose Meshy → hang Actor bones. Spatial, rigid (Bone1)."""
    x, y, z = p.x, p.y, p.z
    # A-pose arms first (out to ±X). Do NOT let Scabbard steal the hand.
    if y > 0.58 and abs(x) > 0.175 and z > -0.03:
        if x > 0:
            if y > 1.24:
                return "Arm_R"
            if y > 0.96:
                return "Fore_R"
            return "Hand_R"
        if y > 1.24:
            return "Arm_L"
        if y > 0.96:
            return "Fore_L"
        return "Hand_L"
    # Sheathed sword + brown scabbard along character-RIGHT hip (thin axis).
    if x > 0.18 and _dist_scabbard(p) < 0.042:
        return "Scabbard"
    if y > 1.50:
        return "Head"
    if y > 1.42 and abs(x) < 0.18:
        return "Neck"
    if y < 0.86 and abs(x) > 0.035:
        if y < 0.22:
            return "Foot_R" if x > 0 else "Foot_L"
        if y < 0.56:
            return "Leg_R" if x > 0 else "Leg_L"
        return "UpLeg_R" if x > 0 else "UpLeg_L"
    if y > 1.28:
        return "Chest"
    if y > 1.10:
        return "Spine"
    return "Hips"


def skin_mesh(mesh_ob, arm_ob):
    counts = defaultdict(int)
    groups = {n: mesh_ob.vertex_groups.new(name=n) for n in MESH_BONES}
    for v in mesh_ob.data.vertices:
        bone = assign_bone(v.co)
        if bone not in groups:
            bone = "Hips"
        groups[bone].add([v.index], 1.0, "REPLACE")
        counts[bone] += 1
    print("weights", dict(counts))
    if counts.get("Scabbard", 0) < 200:
        raise SystemExit(f"scabbard verts too few: {counts.get('Scabbard')}")
    if counts.get("Scabbard", 0) > 2200:
        raise SystemExit(f"scabbard stole the arm: {counts.get('Scabbard')}")
    if counts.get("Head", 0) < 80:
        raise SystemExit(f"head verts too few: {counts.get('Head')}")
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


def apply_pose(arm_ob, pose):
    arm_ob.location = (0.0, pose["root_y"], pose["root_z"])
    for bname, key in POSE_BONE.items():
        pb = arm_ob.pose.bones.get(bname)
        if not pb:
            continue
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = unity_q(pose[key])


def render_walk(arm_ob):
    WALK.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    fps = 16
    duration = WALK_PERIOD * WALK_CYCLES
    n = int(duration * fps)
    frames = []
    for i in range(n):
        t = i / fps
        apply_pose(arm_ob, walk_pose(t))
        path = WALK / f"f_{i:03d}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        frames.append(path)
        print("frame", i, path.stat().st_size)

    stills = {
        "pass_l": 0.0,
        "contact_l": WALK_PERIOD * 0.25,
        "pass_r": WALK_PERIOD * 0.50,
        "contact_r": WALK_PERIOD * 0.75,
    }
    for name, t in stills.items():
        apply_pose(arm_ob, walk_pose(t))
        path = WALK / f"world_walk_{name}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("still", name, path.stat().st_size)

    mp4 = PROOF / "sir_aldric_path2_walk_toward_top.mp4"
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
        "bind": "Path 2 Meshy A-pose mesh on hang Actor bones, rigid Bone1",
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
