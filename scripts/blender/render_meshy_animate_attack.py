#!/usr/bin/env python3
"""Meshy Animate ATTACK FBX → rear/TOP stills + slash MP4.

Same Play cam as walk. Path A cancelled. Do NOT claim Design PASS.
Design clip: Meshy Lionguard Knight · Standing Sword Slash Attack
FBX take: Armature|clip0|baselayer
"""
from __future__ import annotations

import json
import math
import os
import shutil
import subprocess
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
FBX = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx"
PROOF = ROOT / "Docs/Survival/previews/facing_20260925"
ART = Path("/opt/cursor/artifacts")
TARGET_H = 1.86

CAM_EYE = (0.0, 2.80, -5.40)
CAM_TARGET = (0.0, 0.90, 0.50)
CAM_EYE_34 = (1.20, 2.80, -5.15)
CAM_FOV = 30.0

FOOT = {
    "mixamorig:LeftFoot", "mixamorig:RightFoot",
    "mixamorig:LeftToeBase", "mixamorig:RightToeBase",
    "LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase",
}


def unity_to_blender(p):
    """Unity Y-up (x, y, z) → Blender Z-up (x, z, y)."""
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


def _img(*needles: str):
    hits = []
    for im in bpy.data.images:
        n = im.name.lower()
        if all(s in n for s in needles) and im.size[0] >= 64:
            hits.append(im)
    hits.sort(key=lambda im: (-im.size[0], im.name))
    return hits[0] if hits else None


def setup_world():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    sc.render.fps = 30
    try:
        sc.eevee.taa_render_samples = 16
    except AttributeError:
        pass
    for attr, val in (
        ("use_ssr", True),
        ("use_ssr_halfres", False),
        ("ssr_quality", 0.75),
        ("use_gtao", True),
    ):
        if hasattr(sc.eevee, attr):
            setattr(sc.eevee, attr, val)
    try:
        sc.view_settings.view_transform = "Standard"
        sc.view_settings.look = "None"
        sc.view_settings.exposure = 0.22
        sc.view_settings.gamma = 1.0
    except Exception:
        pass
    world = bpy.data.worlds.new("WorldPunch")
    sc.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    wout = nt.nodes.new("ShaderNodeOutputWorld")
    studio = nt.nodes.new("ShaderNodeBackground")
    studio.inputs[0].default_value = (0.12, 0.13, 0.11, 1.0)
    studio.inputs[1].default_value = 1.15
    env = nt.nodes.new("ShaderNodeBackground")
    env.inputs[0].default_value = (1.15, 1.08, 0.96, 1.0)
    env.inputs[1].default_value = 3.4
    lp = nt.nodes.new("ShaderNodeLightPath")
    wmix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(lp.outputs["Is Camera Ray"], wmix.inputs["Fac"])
    nt.links.new(env.outputs["Background"], wmix.inputs[1])
    nt.links.new(studio.outputs["Background"], wmix.inputs[2])
    nt.links.new(wmix.outputs["Shader"], wout.inputs["Surface"])
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 12.5
    key.angle = 0.12
    key.color = (1.0, 0.97, 0.90)
    key_o = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(key_o)
    key_o.location = (1.4, 3.6, -4.2)
    key_o.rotation_euler = (math.radians(55), 0.0, math.radians(18))
    fill = bpy.data.lights.new("Fill", "AREA")
    fill.energy = 720.0
    fill.size = 6.0
    fill.color = (0.90, 0.93, 1.0)
    fill_o = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fill_o)
    fill_o.location = (-2.2, 2.4, -1.5)
    fill_o.rotation_euler = (math.radians(70), 0.0, math.radians(-25))
    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    gmat = bpy.data.materials.new("GroundMat")
    gmat.use_nodes = True
    bsdf = gmat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.11, 0.12, 0.10, 1)
        bsdf.inputs["Roughness"].default_value = 0.88
    ground.data.materials.append(gmat)
    cam_data = bpy.data.cameras.new("WorldPlay")
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(CAM_FOV)
    cam_data.sensor_fit = "VERTICAL"
    cam_data.clip_start = 0.08
    cam_data.clip_end = 40.0
    cam = bpy.data.objects.new("WorldPlay", cam_data)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam
    return cam


def assign_albedo(mesh_ob):
    albedo = _img("texture_0") or _img("texture") or (bpy.data.images[0] if bpy.data.images else None)
    mat = mesh_ob.data.materials[0] if mesh_ob.data.materials else bpy.data.materials.new("AldricAttack")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    if albedo is not None:
        tex.image = albedo
        try:
            albedo.colorspace_settings.name = "sRGB"
        except Exception:
            pass
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = (0.55, 0.55, 0.58, 1)
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = 0.35
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = 0.38
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if not mesh_ob.data.materials:
        mesh_ob.data.materials.append(mat)
    else:
        mesh_ob.data.materials[0] = mat
    print("attack albedo", getattr(albedo, "name", None), getattr(albedo, "size", None))


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
        return 0.0
    dz = min(p.z for p in pts) - 0.002
    root.location.z -= dz
    bpy.context.view_layer.update()
    return dz


def import_fbx(yaw_deg: float, flip_x: bool):
    if not FBX.exists():
        raise SystemExit(f"missing {FBX}")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(FBX))
    junk = [o for o in bpy.data.objects if o.type == "MESH" and "Ico" in o.name]
    for o in junk:
        bpy.data.objects.remove(o, do_unlink=True)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
    assign_albedo(mesh)
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    arm.rotation_mode = "XYZ"
    # Importer already stands the Y-up FBX on Blender Z-up (arm X=+90).
    # Apply that, then yaw around Z so back faces Play cam (Unity −Z / Blender −Y).
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    arm.rotation_euler = (0.0, 0.0, math.radians(yaw_deg))
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    bpy.context.view_layer.update()
    pts = mesh_world_pts(mesh)
    zs = [p.z for p in pts]
    h = max(zs) - min(zs) if zs else 1.0
    sx = -1.0 if flip_x else 1.0
    syz = TARGET_H / h if h > 0.4 else 1.0
    arm.scale = (sx * syz, syz, syz)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    pts = mesh_world_pts(mesh)
    cx = sum(p.x for p in pts) / len(pts)
    cy = sum(p.y for p in pts) / len(pts)
    arm.location.x -= cx
    arm.location.y -= cy
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    try:
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(mesh.data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(mesh.data)
        bm.free()
    except Exception:
        pass
    mesh.data.update()
    root = arm
    plant(root, mesh)
    acts = sorted(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0], reverse=True)
    clip = acts[0] if acts else None
    if clip is not None:
        arm.animation_data_create()
        arm.animation_data.action = clip
    print(
        "imported", mesh.name, "v", len(mesh.data.vertices),
        "clip", getattr(clip, "name", None),
        "range", tuple(clip.frame_range) if clip else None,
        "yaw", yaw_deg, "flip_x", flip_x,
        "bones", [b.name for b in arm.data.bones][:12],
    )
    return root, arm, mesh, clip


def set_cam(cam, eye, tgt):
    cam.matrix_world = play_cam_matrix(eye, tgt)


def render_to(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print("wrote", path, path.stat().st_size)
    try:
        ART.mkdir(parents=True, exist_ok=True)
        (ART / path.name).write_bytes(path.read_bytes())
    except OSError as exc:
        print("artifact skip", exc)


def pose(root, arm, mesh, clip, frame: float):
    if clip is not None:
        arm.animation_data.action = clip
        bpy.context.scene.frame_set(int(round(frame)))
    root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    plant(root, mesh)


def main():
    mode = os.environ.get("ALD_ATTACK_MODE", "proof")
    yaw = float(os.environ.get("ALD_YAW", "180"))
    flip = os.environ.get("ALD_FLIP_X", "1") != "0"
    root, arm, mesh, clip = import_fbx(yaw_deg=yaw, flip_x=flip)
    cam = setup_world()
    PROOF.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    set_cam(cam, CAM_EYE, CAM_TARGET)

    fr0 = int(clip.frame_range[0]) if clip else 1
    fr1 = int(clip.frame_range[1]) if clip else 89
    mid = (fr0 + fr1) // 2

    if mode == "probe":
        bpy.context.scene.render.resolution_x = 540
        bpy.context.scene.render.resolution_y = 960
        for label, fr in (("start", fr0), ("mid", mid), ("end", fr1)):
            pose(root, arm, mesh, clip, fr)
            render_to(PROOF / f"_probe_yaw{int(yaw)}_flip{int(flip)}_{label}.png")
        return

    # Rear stills at start + peak slash (mid).
    pose(root, arm, mesh, clip, fr0)
    render_to(PROOF / "sir_aldric_attack_rear_still.png")
    pose(root, arm, mesh, clip, mid)
    render_to(PROOF / "sir_aldric_attack_slash_rear.png")
    set_cam(cam, CAM_EYE_34, CAM_TARGET)
    pose(root, arm, mesh, clip, mid)
    render_to(PROOF / "sir_aldric_attack_slash_rear_34.png")
    set_cam(cam, CAM_EYE, CAM_TARGET)

    frames_dir = PROOF / "_attack_frames"
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir(parents=True)
    step = max(1, (fr1 - fr0) // 36)
    idx = 0
    for fr in range(fr0, fr1 + 1, step):
        pose(root, arm, mesh, clip, fr)
        idx += 1
        render_to(frames_dir / f"f_{idx:03d}.png")

    mp4 = PROOF / "sir_aldric_attack_toward_top_rear.mp4"
    cmd = [
        "ffmpeg", "-y", "-framerate", "12",
        "-i", str(frames_dir / "f_%03d.png"),
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18",
        str(mp4),
    ]
    subprocess.check_call(cmd)
    shutil.copy2(mp4, ART / mp4.name)
    print("attack mp4", mp4, mp4.stat().st_size)

    note = {
        "fbx": str(FBX.relative_to(ROOT)),
        "take": "target_character|rigify_clip|BaseLayer",
        "discovery": "Meshy Lionguard Knight · Standing Sword Slash Attack",
        "yaw": yaw,
        "flip_x": flip,
        "cam": {"eye": CAM_EYE, "look": CAM_TARGET},
        "frames": [fr0, fr1],
        "design_pass_claimed": False,
        "path_a": False,
    }
    (PROOF / "attack_proof.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done", note)


if __name__ == "__main__":
    main()
