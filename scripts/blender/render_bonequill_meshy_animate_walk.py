#!/usr/bin/env python3
"""Bonequill Meshy Animate walk → World stills + rear walk MP4.

Same Play cam as Aldric. Path A cancelled. Do NOT claim Design PASS.
"""
from __future__ import annotations

import math
import shutil
import subprocess
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
FBX = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk.fbx"
ATLAS = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/bonequill_meshy_animate_walk_atlas.png"
PROOF = ROOT / "Docs/Survival/previews/bonequill_20260925"
ART = Path("/opt/cursor/artifacts")
TARGET_H = 1.86
YAW = 180.0
CAM_EYE = (0.0, 2.80, -5.40)
CAM_TARGET = (0.0, 0.90, 0.50)
CAM_EYE_34 = (1.20, 2.80, -5.15)
CAM_FRONT = (0.0, 2.80, 5.40)
CAM_FRONT_T = (0.0, 0.90, -0.50)
CAM_34_FRONT = (-1.20, 2.80, 5.15)
CAM_34_FRONT_T = (0.0, 0.90, 0.0)
CAM_FOV = 30.0
FOOT = {
    "mixamorig:LeftFoot", "mixamorig:RightFoot",
    "mixamorig:LeftToeBase", "mixamorig:RightToeBase",
}


def unity_to_blender(p):
    return (p[0], p[2], p[1])


def play_cam_matrix(eye, target) -> Matrix:
    eye_v = Vector(unity_to_blender(eye))
    tgt_v = Vector(unity_to_blender(target))
    fwd = (tgt_v - eye_v).normalized()
    world_up = Vector((0.0, 0.0, 1.0))
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


def setup_world():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.fps = 24
    try:
        sc.eevee.taa_render_samples = 12
    except AttributeError:
        pass
    world = bpy.data.worlds.new("W")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.10, 0.11, 0.09, 1)
        bg.inputs[1].default_value = 1.1
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 12.0
    key.color = (1.0, 0.96, 0.88)
    key_o = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(key_o)
    key_o.rotation_euler = (math.radians(52), 0.0, math.radians(-16))
    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0, 0, 0))
    ground = bpy.context.active_object
    gmat = bpy.data.materials.new("G")
    gmat.use_nodes = True
    bsdf = gmat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.11, 0.12, 0.10, 1)
    ground.data.materials.append(gmat)
    cam_data = bpy.data.cameras.new("Play")
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(CAM_FOV)
    cam_data.sensor_fit = "VERTICAL"
    cam_data.clip_start = 0.08
    cam_data.clip_end = 40.0
    cam = bpy.data.objects.new("Play", cam_data)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam
    return cam


def mesh_world_pts(mesh_ob, groups=None):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    ev = mesh_ob.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    vg = {g.index: g.name for g in mesh_ob.vertex_groups}
    pts = []
    src = mesh_ob.data
    n = min(len(me.vertices), len(src.vertices))
    for i in range(n):
        if groups:
            names = {vg.get(g.group, "") for g in src.vertices[i].groups if g.weight > 0.35}
            if not (names & groups):
                continue
        pts.append(mw @ me.vertices[i].co)
    ev.to_mesh_clear()
    return pts


def plant(root, mesh_ob):
    pts = mesh_world_pts(mesh_ob, FOOT) or mesh_world_pts(mesh_ob)
    if not pts:
        return
    root.location.z -= min(p.z for p in pts) - 0.002
    bpy.context.view_layer.update()


def assign_albedo(mesh_ob):
    img = None
    if ATLAS.exists():
        img = bpy.data.images.load(str(ATLAS))
    if img is None:
        for im in bpy.data.images:
            n = im.name.lower()
            if "texture_0" in n and "normal" not in n and im.size[0] >= 256:
                img = im
                break
    mat = mesh_ob.data.materials[0] if mesh_ob.data.materials else bpy.data.materials.new("Bonequill")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    if img is not None:
        tex.image = img
        try:
            img.colorspace_settings.name = "sRGB"
        except Exception:
            pass
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if not mesh_ob.data.materials:
        mesh_ob.data.materials.append(mat)
    else:
        mesh_ob.data.materials[0] = mat


def import_fbx():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    setup_world()
    bpy.ops.import_scene.fbx(filepath=str(FBX))
    for o in list(bpy.data.objects):
        if o.type == "MESH" and "Ico" in o.name:
            bpy.data.objects.remove(o, do_unlink=True)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
    assign_albedo(mesh)
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    arm.rotation_mode = "XYZ"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    arm.rotation_euler = (0.0, 0.0, math.radians(YAW))
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    pts = mesh_world_pts(mesh)
    h = max(p.z for p in pts) - min(p.z for p in pts)
    s = TARGET_H / h if h > 0.4 else 1.0
    arm.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    pts = mesh_world_pts(mesh)
    arm.location.x -= sum(p.x for p in pts) / len(pts)
    arm.location.y -= sum(p.y for p in pts) / len(pts)
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    plant(arm, mesh)
    acts = sorted(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0], reverse=True)
    walk = next((a for a in acts if "Walk" in a.name and (a.frame_range[1] - a.frame_range[0]) > 5), acts[0])
    arm.animation_data_create()
    arm.animation_data.action = walk
    print("imported", mesh.name, "v", len(mesh.data.vertices), "walk", walk.name, tuple(walk.frame_range))
    return arm, mesh, walk


def set_cam(cam, eye, tgt):
    cam.matrix_world = play_cam_matrix(eye, tgt)


def render_to(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    ART.mkdir(parents=True, exist_ok=True)
    (ART / path.name).write_bytes(path.read_bytes())
    print("wrote", path, path.stat().st_size)


def pose(arm, mesh, walk, frame):
    arm.animation_data.action = walk
    bpy.context.scene.frame_set(int(round(frame)))
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    plant(arm, mesh)


def main():
    arm, mesh, walk = import_fbx()
    cam = bpy.context.scene.camera
    PROOF.mkdir(parents=True, exist_ok=True)
    frames_dir = PROOF / "_walk_frames"
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir()

    f0, f1 = walk.frame_range
    pose(arm, mesh, walk, f0)
    stills = (
        ("bonequill_world_front.png", CAM_FRONT, CAM_FRONT_T),
        ("bonequill_world_34_front.png", CAM_34_FRONT, CAM_34_FRONT_T),
        ("bonequill_world_rear.png", CAM_EYE, CAM_TARGET),
        ("bonequill_world_rear_34.png", CAM_EYE_34, CAM_TARGET),
    )
    for name, eye, tgt in stills:
        set_cam(cam, eye, tgt)
        render_to(PROOF / name)

    set_cam(cam, CAM_EYE, CAM_TARGET)
    n = int(f1 - f0) + 1
    for i in range(n):
        pose(arm, mesh, walk, f0 + i)
        render_to(frames_dir / f"f_{i:03d}.png")
    mp4 = PROOF / "bonequill_walk_toward_top_rear.mp4"
    subprocess.check_call(
        [
            "ffmpeg", "-y", "-framerate", "24",
            "-i", str(frames_dir / "f_%03d.png"),
            "-pix_fmt", "yuv420p", "-vf", "scale=1080:1920",
            "-crf", "18", "-movflags", "+faststart", str(mp4),
        ]
    )
    print("mp4", mp4, mp4.stat().st_size)
    (ART / mp4.name).write_bytes(mp4.read_bytes())
    print("done bonequill walk wire proofs (no Design PASS)")


if __name__ == "__main__":
    main()
