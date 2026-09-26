#!/usr/bin/env python3
"""Sir Aldric SEP yawlock attack Play-cam remap (no Unity Editor on this VM).

ATTACK CLIP ONLY. Walk / painted look / LookSwordScabbard clearance stay 1ac345a.
Plays SirAldric_SEP_meshy_animate_attack_yawlock on its native MeshyRig AccuRIG
body (same 99k SEP mesh as walk). Unity Play Humanoid-retargets this take onto
the walk Mixamo Avatar. Old spin / nospin FBX are not SoT.

Camera: Unity Y-up Play SoT remapped (x, -z, y). Character faces −Y; rear cam
at +Y sees cape-back; slash toward −Y = TOP / away from rear camera.
Honest label: Play-cam remap, not Unity Game-view, not Meshy website stills.
"""
from __future__ import annotations

import math
import os
import shutil
import struct
import subprocess
from pathlib import Path

import bpy
from mathutils import Vector, kdtree

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
ATK = PACK / "SirAldric_SEP_meshy_animate_attack_yawlock.fbx"
BODY = PACK / "SirAldric_SEP_body_nosword_PAINTED_mid200k.fbx"
SWORD = PACK / "SirAldric_SEP_sword_scabbard_PAINTED_mid.fbx"
PAINT = PACK / "sep_paint"
PROOF = ROOT / "Docs/Survival/previews/aldric_sep_attack_yawlock_20260926"
ART = Path("/opt/cursor/artifacts")
UVBIN = PAINT / "sir_aldric_sep_walk_look_uv.bin"

CAM_REAR = ((0.0, 2.80, -5.40), (0.0, 0.90, 0.50))
CAM_FRONT = ((0.0, 2.80, 5.40), (0.0, 0.90, 0.50))
CAM_34 = ((3.20, 2.80, -4.40), (0.0, 0.90, 0.50))
CAM_FOV = 30.0
STRIKE_FR = 25
DRAW_FR = 8


def u2b(p):
    return (p[0], -p[2], p[1])


def world_pts(obj):
    mw = obj.matrix_world
    return [mw @ v.co for v in obj.data.vertices]


def bbox_pts(pts):
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def bind_pbr(obj, stem: str):
    albedo = PAINT / f"{stem}_basecolor.jpg"
    metallic = PAINT / f"{stem}_metallic.jpg"
    roughness = PAINT / f"{stem}_roughness.jpg"
    normal = PAINT / f"{stem}_normal.jpg"
    mat = bpy.data.materials.new(obj.name + "_Look")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(albedo))
    try:
        tex.image.colorspace_settings.name = "sRGB"
    except Exception:
        pass
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    if metallic.exists():
        mt = nt.nodes.new("ShaderNodeTexImage")
        mt.image = bpy.data.images.load(str(metallic))
        try:
            mt.image.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
        nt.links.new(mt.outputs["Color"], bsdf.inputs["Metallic"])
    if roughness.exists():
        rt = nt.nodes.new("ShaderNodeTexImage")
        rt.image = bpy.data.images.load(str(roughness))
        try:
            rt.image.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
        nt.links.new(rt.outputs["Color"], bsdf.inputs["Roughness"])
    if normal.exists() and "Normal" in bsdf.inputs:
        nt_img = nt.nodes.new("ShaderNodeTexImage")
        nt_img.image = bpy.data.images.load(str(normal))
        try:
            nt_img.image.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(nt_img.outputs["Color"], nmap.inputs["Color"])
        nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def stamp_look_uvs(mesh, body):
    if UVBIN.exists() and len(mesh.data.vertices) == 99946:
        raw = UVBIN.read_bytes()
        count = struct.unpack_from("<I", raw, 0)[0]
        if count == len(mesh.data.vertices):
            uvs = []
            for i in range(count):
                u, v = struct.unpack_from("<ff", raw, 4 + i * 8)
                uvs.append(Vector((u, v)))
            if not mesh.data.uv_layers:
                mesh.data.uv_layers.new(name="UVMap")
            uv_layer = mesh.data.uv_layers.active
            for li, loop in enumerate(mesh.data.loops):
                uv_layer.data[li].uv = uvs[loop.vertex_index]
            print("uv sidecar", count)
            return
    bpy.context.view_layer.update()
    wpts = world_pts(mesh)
    bpts = world_pts(body)
    wmin, wmax = bbox_pts(wpts)
    bmin, bmax = bbox_pts(bpts)
    wsize = wmax - wmin
    bsize = bmax - bmin
    scale = min(wsize[i] / max(bsize[i], 1e-6) for i in range(3))
    body.scale = (scale, scale, scale)
    bpy.context.view_layer.update()
    bpts = world_pts(body)
    bmin, bmax = bbox_pts(bpts)
    body.location = (wmin + wmax) / 2 - (bmin + bmax) / 2
    bpy.context.view_layer.update()
    bpts = world_pts(body)
    puv = body.data.uv_layers.active
    p_vert_uv = [Vector((0.0, 0.0)) for _ in body.data.vertices]
    p_vert_n = [0] * len(body.data.vertices)
    for li, loop in enumerate(body.data.loops):
        p_vert_uv[loop.vertex_index] += puv.data[li].uv
        p_vert_n[loop.vertex_index] += 1
    for i, n in enumerate(p_vert_n):
        if n:
            p_vert_uv[i] /= n
    kd = kdtree.KDTree(len(bpts))
    for i, p in enumerate(bpts):
        kd.insert(p, i)
    kd.balance()
    w_uv = [p_vert_uv[kd.find(p)[1]] for p in world_pts(mesh)]
    if not mesh.data.uv_layers:
        mesh.data.uv_layers.new(name="UVMap")
    uv_layer = mesh.data.uv_layers.active
    for li, loop in enumerate(mesh.data.loops):
        uv_layer.data[li].uv = w_uv[loop.vertex_index]
    print("uv stamped", len(w_uv))


def setup_studio(lo_res: bool):
    sc = bpy.context.scene
    world = bpy.data.worlds.new("YawlockStudio")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.07, 0.08, 0.07, 1)
        bg.inputs[1].default_value = 1.0
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 540 if lo_res else 1080
    sc.render.resolution_y = 960 if lo_res else 1920
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.fps = 30
    try:
        sc.eevee.taa_render_samples = 10 if lo_res else 20
    except Exception:
        pass
    bpy.ops.mesh.primitive_plane_add(size=16, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    gmat = bpy.data.materials.new("GroundMat")
    gmat.use_nodes = True
    bs = gmat.node_tree.nodes.get("Principled BSDF")
    if bs:
        bs.inputs["Base Color"].default_value = (0.58, 0.58, 0.56, 1)
        bs.inputs["Roughness"].default_value = 0.92
    ground.data.materials.append(gmat)
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 5.8
    key.color = (1.0, 0.97, 0.90)
    ko = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(ko)
    ko.rotation_euler = (math.radians(52), 0.0, math.radians(18))
    fill = bpy.data.lights.new("Fill", "SUN")
    fill.energy = 2.6
    fill.color = (0.82, 0.88, 1.0)
    fo = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fo)
    fo.rotation_euler = (math.radians(70), 0.0, math.radians(-30))
    rim = bpy.data.lights.new("Rim", "SUN")
    rim.energy = 3.4
    rim.color = (1.0, 0.96, 0.88)
    ro = bpy.data.objects.new("Rim", rim)
    bpy.context.collection.objects.link(ro)
    ro.rotation_euler = (math.radians(28), 0.0, math.radians(180))
    camd = bpy.data.cameras.new("PlayCam")
    camd.lens_unit = "FOV"
    camd.sensor_fit = "VERTICAL"
    camd.angle = math.radians(CAM_FOV)
    camd.clip_start = 0.08
    camd.clip_end = 40.0
    cam = bpy.data.objects.new("PlayCam", camd)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam
    return cam


def look(cam, eye_u, at_u):
    eye = Vector(u2b(eye_u))
    at = Vector(u2b(at_u))
    cam.location = eye
    cam.rotation_euler = (at - eye).to_track_quat("-Z", "Y").to_euler()


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


def bone(arm, *names):
    for n in names:
        if n in arm.pose.bones:
            return arm.pose.bones[n]
    raise KeyError(names)


def place_hip_prop(sword, arm):
    hips = bone(arm, "Hips", "mixamorig:Hips")
    rleg = bone(arm, "RightUpLeg", "mixamorig:RightUpLeg")
    hip_w = arm.matrix_world @ hips.head
    rleg_w = arm.matrix_world @ rleg.head
    sword.scale = (0.28, 0.28, 0.28)
    sword.rotation_euler = (0.0, math.radians(-90), math.radians(-90))
    sword.location = (rleg_w.x - 0.06, hip_w.y, hip_w.z - 0.12)
    bpy.context.view_layer.update()
    return hip_w, rleg_w


def main():
    lo = os.environ.get("ALD_YAWLOCK_PROBE", "0") == "1"
    PROOF.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(ATK))
    for o in list(bpy.data.objects):
        if o.type == "MESH" and "Ico" in o.name:
            bpy.data.objects.remove(o, do_unlink=True)
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    if arm.animation_data:
        arm.animation_data.action = None
    bpy.context.scene.frame_set(3)
    before = {id(o) for o in bpy.data.objects}
    bpy.ops.import_scene.fbx(filepath=str(BODY))
    body = next(o for o in bpy.data.objects if o.type == "MESH" and id(o) not in before)
    stamp_look_uvs(mesh, body)
    body.hide_render = True
    body.hide_viewport = True
    bind_pbr(mesh, "sep_body")

    before = {id(o) for o in bpy.data.objects}
    bpy.ops.import_scene.fbx(filepath=str(SWORD))
    sword = next(o for o in bpy.data.objects if o.type == "MESH" and id(o) not in before)
    sword.name = "LookSwordScabbard"
    bind_pbr(sword, "sword")

    act = max(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0])
    arm.animation_data_create()
    arm.animation_data.action = act
    fr0 = int(round(act.frame_range[0]))
    fr1 = int(round(act.frame_range[1]))
    print("yawlock action", act.name, "frames", fr0, fr1)

    cam = setup_studio(lo)
    bpy.context.scene.frame_set(STRIKE_FR)
    bpy.context.view_layer.update()
    place_hip_prop(sword, arm)
    hips = bone(arm, "Hips")
    mw = arm.matrix_world @ hips.matrix
    fwd = (mw.to_3x3() @ Vector((0, 0, 1))).normalized()
    print("strike hips fwd", tuple(round(x, 3) for x in fwd), "eulZ", round(math.degrees(mw.to_euler("XYZ").z), 3))

    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_sep_yawlock_rear_attack_playcam.png")
    look(cam, *CAM_FRONT)
    render_to(PROOF / "sir_aldric_sep_yawlock_front_attack_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_sep_yawlock_34_attack_playcam.png")

    bpy.context.scene.frame_set(DRAW_FR)
    bpy.context.view_layer.update()
    place_hip_prop(sword, arm)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_sep_yawlock_rear_draw_playcam.png")

    if lo:
        print("probe stills only")
        return

    frames_dir = PROOF / "_yawlock_attack_frames"
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir(parents=True)
    look(cam, *CAM_REAR)
    idx = 0
    step = 2
    for fr in range(fr0, fr1 + 1, step):
        bpy.context.scene.frame_set(fr)
        bpy.context.view_layer.update()
        place_hip_prop(sword, arm)
        idx += 1
        render_to(frames_dir / f"f_{idx:03d}.png")

    mp4 = PROOF / "sir_aldric_sep_yawlock_attack_juice_playcam.mp4"
    subprocess.check_call(
        [
            "ffmpeg", "-y", "-framerate", "15",
            "-i", str(frames_dir / "f_%03d.png"),
            "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18",
            str(mp4),
        ]
    )
    try:
        shutil.copy2(mp4, ART / mp4.name)
    except OSError:
        pass
    print("juice", mp4, mp4.stat().st_size)


if __name__ == "__main__":
    main()
