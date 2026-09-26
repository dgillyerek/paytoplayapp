#!/usr/bin/env python3
"""Sir Aldric 2H yawlock draw-parent Play-cam remap.

ATTACK PROP ONLY. Look mid450k / walk AccuRIG unchanged. No Path A.
Split mid80k: empty LookScabbard stays LEFT hip; LookSword (blade+hilt)
parents to RightHand on draw and follows 2H grip for overhead→strike.

Attack SoT: SirAldric_SEP_meshy_animate_attack_2h_downstrike_yawlock
take target_character|Scene frames 4–92. Juice uses that same action
and the same prop function as the stills.

Camera: Unity Y-up Play SoT remapped (x, -z, y).
Honest label: Play-cam remap, not Unity Game-view, not Meshy website stills.
"""
from __future__ import annotations

import math
import os
import shutil
import subprocess
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector, Matrix, kdtree

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
ATK = PACK / "SirAldric_SEP_meshy_animate_attack_2h_downstrike_yawlock.fbx"
BODY = PACK / "SirAldric_SEP_body_nosword_PAINTED_mid450k.glb"
SWORD = PACK / "SirAldric_SEP_sword_scabbard_PAINTED_mid80k.glb"
PAINT = PACK / "sep_paint"
PROOF = ROOT / "Docs/Survival/previews/aldric_sep_2h_drawparent_20260926"
ART = Path("/opt/cursor/artifacts")

CAM_REAR = ((0.0, 2.80, -5.40), (0.0, 0.90, 0.50))
CAM_34 = ((3.20, 2.80, -4.40), (0.0, 0.90, 0.50))
CAM_FOV = 30.0
DRAW_FR = 6
OVERHEAD_FR = 30
STRIKE_FR = 39
# Scene take starts ~4. Do not parent before the authored draw.
AXIS = Vector((0.7319, 0.0, 0.6814))
T_HILT = 0.70
R_CORE = 0.11


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
    """Stamp mid450k UVs onto the attack AccuRIG. Does NOT write the walk sidecar."""
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
    for p in world_pts(walk):
        _co, idx, _dist = kd.find(p)
        w_uv.append(p_vert_uv[idx].copy())
    if not walk.data.uv_layers:
        walk.data.uv_layers.new(name="UVMap")
    uv_layer = walk.data.uv_layers.active
    for li, loop in enumerate(walk.data.loops):
        uv_layer.data[li].uv = w_uv[loop.vertex_index]
    print("uv stamped verts", len(w_uv), "(no sidecar write)")


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
        sc.eevee.taa_render_samples = 8 if lo_res else 24
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


def _clear_parent(obj):
    mw = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_parent_inverse = Matrix.Identity(4)
    obj.matrix_world = mw


def split_look_prop(src):
    mesh = src.data
    pts = [v.co.copy() for v in mesh.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c = (mn + mx) / 2
    labels = []
    for p in pts:
        d = p - c
        t = d.dot(AXIS)
        r = (d - t * AXIS).length
        labels.append(t > T_HILT or r < R_CORE)

    def make(name, want_sword):
        obj = src.copy()
        obj.data = src.data.copy()
        obj.name = name
        bpy.context.collection.objects.link(obj)
        _clear_parent(obj)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bm.verts.ensure_lookup_table()
        kill = [v for v in bm.verts if labels[v.index] != want_sword]
        bmesh.ops.delete(bm, geom=kill, context="VERTS")
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()
        print(name, "verts", len(obj.data.vertices), "faces", len(obj.data.polygons))
        return obj

    sword = make("LookSword", True)
    scab = make("LookScabbard", False)
    src.hide_render = True
    src.hide_viewport = True
    hilt = max(sword.data.vertices, key=lambda v: (v.co - c).dot(AXIS)).co.copy()
    tip = min(sword.data.vertices, key=lambda v: (v.co - c).dot(AXIS)).co.copy()
    print("hilt", tuple(round(x, 3) for x in hilt), "tip", tuple(round(x, 3) for x in tip))
    return sword, scab, hilt, tip


def place_scabbard(scab, arm):
    hips = bone(arm, "Hips", "mixamorig:Hips")
    lleg = bone(arm, "LeftUpLeg", "mixamorig:LeftUpLeg")
    hip_w = arm.matrix_world @ hips.head
    lleg_w = arm.matrix_world @ lleg.head
    _clear_parent(scab)
    scab.scale = (0.28, 0.28, 0.28)
    scab.rotation_euler = (0.0, math.radians(90), math.radians(-90))
    scab.location = (lleg_w.x + 0.06, hip_w.y, hip_w.z - 0.12)
    bpy.context.view_layer.update()


def sheath_sword(sword, scab):
    _clear_parent(sword)
    sword.scale = scab.scale.copy()
    sword.rotation_euler = scab.rotation_euler.copy()
    sword.location = scab.location.copy()
    bpy.context.view_layer.update()


def grip_sword(sword, arm, hilt, tip, two_hand: bool):
    rh = bone(arm, "RightHand", "mixamorig:RightHand")
    lh = bone(arm, "LeftHand", "mixamorig:LeftHand")
    mw_rh = arm.matrix_world @ rh.matrix
    rh_w = mw_rh.translation
    lh_w = (arm.matrix_world @ lh.matrix).translation
    # Hold the grip (just below the pommel), not a midpoint that floats in air.
    hold = hilt.lerp(tip, 0.12)
    hy = (mw_rh.to_3x3() @ Vector((0, 1, 0))).normalized()
    if two_hand:
        across = (lh_w - rh_w)
        if across.length > 0.05:
            hy = (hy * 0.55 + across.normalized() * 0.45).normalized()
    tip_dir = (tip - hilt)
    if tip_dir.length < 1e-6:
        tip_dir = Vector((-1, 0, 0))
    tip_dir.normalize()
    rot = tip_dir.rotation_difference(hy)
    hold_off = rot @ (hold * 0.28)
    _clear_parent(sword)
    sword.scale = (0.28, 0.28, 0.28)
    sword.rotation_euler = rot.to_euler()
    sword.location = rh_w - hold_off
    bpy.context.view_layer.update()
    print(
        "grip RH", tuple(round(x, 3) for x in rh_w),
        "twoH", two_hand,
        "LH", tuple(round(x, 3) for x in lh_w),
    )


def apply_props(arm, sword, scab, hilt, tip, fr: int):
    """Same function stills and juice call. Scene take: draw f6, overhead f30, strike f39."""
    place_scabbard(scab, arm)
    if fr >= DRAW_FR:
        grip_sword(sword, arm, hilt, tip, two_hand=fr >= OVERHEAD_FR)
    else:
        sheath_sword(sword, scab)


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
    src = next(o for o in bpy.data.objects if o.type == "MESH" and id(o) not in before)
    bind_pbr(src, "sword")
    sword, scab, hilt, tip = split_look_prop(src)
    bind_pbr(sword, "sword")
    bind_pbr(scab, "sword")
    return sword, scab, hilt, tip


def pick_scene_action():
    named = [a for a in bpy.data.actions if "Scene" in a.name or "yawlock" in a.name.lower()
             or "2h" in a.name.lower() or "downstrike" in a.name.lower()]
    if named:
        act = max(named, key=lambda a: a.frame_range[1] - a.frame_range[0])
    else:
        act = max(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0])
    print("attack action", act.name, "range", tuple(act.frame_range), "n_actions", len(bpy.data.actions))
    return act


def juice(cam, arm, sword, scab, hilt, tip, fr0, fr1, frames_dir, mp4):
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir(parents=True)
    look(cam, *CAM_REAR)
    try:
        bpy.context.scene.eevee.taa_render_samples = 16
    except Exception:
        pass
    # Every Scene frame so juice matches stills (f6 / f30 / f39 are in the set).
    idx = 0
    for fr in range(fr0, fr1 + 1):
        bpy.context.scene.frame_set(fr)
        bpy.context.view_layer.update()
        apply_props(arm, sword, scab, hilt, tip, fr)
        idx += 1
        render_to(frames_dir / f"f_{idx:03d}.png")
        print("juice frame", fr, "idx", idx)
    subprocess.check_call(
        [
            "ffmpeg", "-y", "-framerate", "30",
            "-i", str(frames_dir / "f_%03d.png"),
            "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18",
            str(mp4),
        ]
    )
    try:
        shutil.copy2(mp4, ART / mp4.name)
    except OSError:
        pass
    print("juice", mp4, mp4.stat().st_size, "frames", idx, "span", fr0, fr1)


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
    sword, scab, hilt, tip = import_look(mesh)
    act = pick_scene_action()
    arm.animation_data_create()
    arm.animation_data.action = act
    fr0 = 4
    fr1 = 92
    print("bound Scene take", act.name, "play", fr0, fr1)
    cam = setup_studio(lo)
    hips = bone(arm, "Hips")

    def still(fr, tag):
        bpy.context.scene.frame_set(fr)
        bpy.context.view_layer.update()
        mw = arm.matrix_world @ hips.matrix
        fwd = (mw.to_3x3() @ Vector((0, 0, 1))).normalized()
        print(tag, "fr", fr, "hips fwd", tuple(round(x, 3) for x in fwd),
              "eulZ", round(math.degrees(mw.to_euler("XYZ").z), 3))
        apply_props(arm, sword, scab, hilt, tip, fr)
        look(cam, *CAM_REAR)
        render_to(PROOF / f"sir_aldric_sep_2h_dp_rear_{tag}_playcam.png")
        look(cam, *CAM_34)
        render_to(PROOF / f"sir_aldric_sep_2h_dp_34_{tag}_playcam.png")

    still(STRIKE_FR, "strike")
    still(DRAW_FR, "draw")
    still(OVERHEAD_FR, "overhead")
    if lo:
        print("probe stills only — skip juice")
        return
    juice(cam, arm, sword, scab, hilt, tip, fr0, fr1, PROOF / "_2h_dp_attack_frames",
          PROOF / "sir_aldric_sep_2h_dp_attack_juice_playcam.mp4")


def main():
    lo = os.environ.get("ALD_2H_DP_PROBE", "0") == "1"
    PROOF.mkdir(parents=True, exist_ok=True)
    render_attack(lo)


if __name__ == "__main__":
    main()
