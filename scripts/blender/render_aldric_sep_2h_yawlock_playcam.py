#!/usr/bin/env python3
"""Sir Aldric SEP mid450k look + 2H yawlock Play-cam remap.

Walk AccuRIG + Walking clip unchanged. Look: mid450k / mid80k, sep_paint UV.
LookSwordScabbard on character-LEFT hip (Derek: RH draws from LEFT sheath).
Attack SoT: 2H downstrike yawlock only. No Path A.

Camera: Unity Y-up Play SoT remapped (x, -z, y).
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
WALK = PACK / "SirAldric_SEP_meshy_animate_walk.fbx"
ATK = PACK / "SirAldric_SEP_meshy_animate_attack_2h_downstrike_yawlock.fbx"
BODY = PACK / "SirAldric_SEP_body_nosword_PAINTED_mid450k.glb"
SWORD = PACK / "SirAldric_SEP_sword_scabbard_PAINTED_mid80k.glb"
PAINT = PACK / "sep_paint"
PROOF = ROOT / "Docs/Survival/previews/aldric_sep_2h_yawlock_20260926"
ART = Path("/opt/cursor/artifacts")
UVBIN = PAINT / "sir_aldric_sep_walk_look_uv.bin"

# Unity Play SoT. Remap (x, y, z) → (x, -z, y) so this Mixamo FBX's
# face (−Y) puts the cape-back toward the Unity rear eye at z = −5.40.
CAM_REAR = ((0.0, 2.80, -5.40), (0.0, 0.90, 0.50))
CAM_FRONT = ((0.0, 2.80, 5.40), (0.0, 0.90, 0.50))
CAM_34 = ((3.20, 2.80, -4.40), (0.0, 0.90, 0.50))
CAM_FOV = 30.0


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


def stamp_look_uvs(walk, body):
    if walk.animation_data:
        pass
    bpy.context.view_layer.update()
    wpts = world_pts(walk)
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
    w_uv = []
    dsum = 0.0
    for p in world_pts(walk):
        _co, idx, dist = kd.find(p)
        w_uv.append(p_vert_uv[idx].copy())
        dsum += dist
    if not walk.data.uv_layers:
        walk.data.uv_layers.new(name="UVMap")
    uv_layer = walk.data.uv_layers.active
    for li, loop in enumerate(walk.data.loops):
        uv_layer.data[li].uv = w_uv[loop.vertex_index]
    buf = bytearray()
    buf += struct.pack("<I", len(w_uv))
    for u in w_uv:
        buf += struct.pack("<ff", float(u.x), float(u.y))
    UVBIN.write_bytes(buf)
    print("uv sidecar", UVBIN, "verts", len(w_uv), "mean_dist", dsum / max(len(w_uv), 1))
    return w_uv


def setup_studio(lo_res: bool):
    sc = bpy.context.scene
    world = bpy.data.worlds.new("PaintStudio")
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
        sc.eevee.taa_render_samples = 12 if lo_res else 24
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
    # Rim so the navy cape / gold lions read from Play rear (not a clay-white lift).
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
    """Character-LEFT hip. Mixamo LeftUpLeg = +X; MeshyRig LeftUpLeg same side.

    Mirror of the 1ac345a RIGHT-hip hang: outward +0.06 on +X, Euler (0, 90, −90).
    """
    hips = bone(arm, "mixamorig:Hips", "Hips")
    lleg = bone(arm, "mixamorig:LeftUpLeg", "LeftUpLeg")
    hip_w = arm.matrix_world @ hips.head
    lleg_w = arm.matrix_world @ lleg.head
    sword.scale = (0.28, 0.28, 0.28)
    sword.rotation_euler = (0.0, math.radians(90), math.radians(-90))
    sword.location = (lleg_w.x + 0.06, hip_w.y, hip_w.z - 0.12)
    bpy.context.view_layer.update()
    return hip_w, lleg_w


def import_look(mesh):
    before = {id(o) for o in bpy.data.objects}
    bpy.ops.import_scene.gltf(filepath=str(BODY))
    body = next(o for o in bpy.data.objects if o.type == "MESH" and id(o) not in before)
    print("look body", body.name, "faces", len(body.data.polygons))
    stamp_look_uvs(mesh, body)
    body.hide_render = True
    body.hide_viewport = True
    bind_pbr(mesh, "sep_body")
    before = {id(o) for o in bpy.data.objects}
    bpy.ops.import_scene.gltf(filepath=str(SWORD))
    sword = next(o for o in bpy.data.objects if o.type == "MESH" and id(o) not in before)
    sword.name = "LookSwordScabbard"
    print("look sword faces", len(sword.data.polygons))
    bind_pbr(sword, "sword")
    return sword


def juice(cam, arm, sword, fr0, fr1, step, frames_dir, mp4, fps):
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir(parents=True)
    look(cam, *CAM_REAR)
    idx = 0
    for fr in range(fr0, fr1 + 1, step):
        bpy.context.scene.frame_set(fr)
        bpy.context.view_layer.update()
        place_hip_prop(sword, arm)
        idx += 1
        render_to(frames_dir / f"f_{idx:03d}.png")
    subprocess.check_call(
        [
            "ffmpeg", "-y", "-framerate", str(fps),
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


def render_walk(lo):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(WALK))
    for o in list(bpy.data.objects):
        if o.type == "MESH" and "Ico" in o.name:
            bpy.data.objects.remove(o, do_unlink=True)
    walk = next(o for o in bpy.data.objects if o.type == "MESH")
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    if arm.animation_data:
        arm.animation_data.action = None
    sword = import_look(walk)
    act = max(
        bpy.data.actions,
        key=lambda a: (a.frame_range[1] - a.frame_range[0]) if "Walking" in a.name else -1,
    )
    arm.animation_data_create()
    arm.animation_data.action = act
    fr0 = int(act.frame_range[0])
    fr1 = int(act.frame_range[1])
    still_fr = fr0 + max(1, int(round(0.35 * (fr1 - fr0))))
    print("walk action", act.name, "frames", fr0, fr1, "still", still_fr)
    cam = setup_studio(lo)
    bpy.context.scene.frame_set(still_fr)
    bpy.context.view_layer.update()
    hip_w, lleg_w = place_hip_prop(sword, arm)
    print("LEFT hip", tuple(round(x, 3) for x in hip_w), "LeftUpLeg", tuple(round(x, 3) for x in lleg_w))
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_sep_2h_rear_walk_playcam.png")
    look(cam, *CAM_FRONT)
    render_to(PROOF / "sir_aldric_sep_2h_front_walk_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_sep_2h_34_walk_playcam.png")
    if lo:
        return
    juice(cam, arm, sword, fr0, fr1, 1, PROOF / "_2h_walk_frames",
          PROOF / "sir_aldric_sep_2h_walk_juice_playcam.mp4", 30)


def render_attack(lo):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(ATK))
    for o in list(bpy.data.objects):
        if o.type == "MESH" and "Ico" in o.name:
            bpy.data.objects.remove(o, do_unlink=True)
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    if arm.animation_data:
        arm.animation_data.action = None
    sword = import_look(mesh)
    act = max(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0])
    arm.animation_data_create()
    arm.animation_data.action = act
    fr0 = int(round(act.frame_range[0]))
    fr1 = int(round(act.frame_range[1]))
    print("attack action", act.name, "frames", fr0, fr1)
    cam = setup_studio(lo)
    bpy.context.scene.frame_set(39)
    bpy.context.view_layer.update()
    hips = bone(arm, "Hips")
    mw = arm.matrix_world @ hips.matrix
    fwd = (mw.to_3x3() @ Vector((0, 0, 1))).normalized()
    print("strike hips fwd", tuple(round(x, 3) for x in fwd), "eulZ", round(math.degrees(mw.to_euler("XYZ").z), 3))
    place_hip_prop(sword, arm)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_sep_2h_rear_strike_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_sep_2h_34_strike_playcam.png")
    bpy.context.scene.frame_set(6)
    bpy.context.view_layer.update()
    place_hip_prop(sword, arm)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_sep_2h_rear_draw_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_sep_2h_34_draw_playcam.png")
    bpy.context.scene.frame_set(30)
    bpy.context.view_layer.update()
    place_hip_prop(sword, arm)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_sep_2h_rear_overhead_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_sep_2h_34_overhead_playcam.png")
    if lo:
        return
    juice(cam, arm, sword, fr0, fr1, 2, PROOF / "_2h_attack_frames",
          PROOF / "sir_aldric_sep_2h_attack_juice_playcam.mp4", 15)


def main():
    lo = os.environ.get("ALD_2H_PROBE", "0") == "1"
    PROOF.mkdir(parents=True, exist_ok=True)
    render_walk(lo)
    render_attack(lo)
    if lo:
        print("probe stills only")


if __name__ == "__main__":
    main()
