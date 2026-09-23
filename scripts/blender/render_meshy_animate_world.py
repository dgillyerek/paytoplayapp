#!/usr/bin/env python3
"""Meshy Animate FBX → Aldric World stills + planted walk. No Path A weights.

Uses the Design-delivered skin/clip AS-IS. Axis convert only
(Z-up Mixamo → Unity Y-up, face +Z, character-RIGHT +X).
Lighting/material PUNCH: stronger key + exposure, FBX metallic/roughness
wired (same albedo, no rebake), navy value lift, gold chrome.
Writes *punch* candidates — does not overwrite unlocked hub PNGs.
Do NOT claim Design / bind / walk PASS. Path A weight-paint CANCELLED.
"""
from __future__ import annotations

import json
import math
import os
import shutil
import subprocess
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
FBX = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AUTO_RIG_PATH/out/sir_aldric_meshy_animate_walk.fbx"
DROP = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AUTO_RIG_PATH"
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
ART = Path("/opt/cursor/artifacts")
TARGET_H = 1.86

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


def _img(*needles: str):
    """First packed/imported image whose name contains all needles (lower)."""
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
    try:
        sc.eevee.taa_render_samples = 32
    except AttributeError:
        pass
    # Gold chrome needs SSR; mute matte was EEVEE without reflections.
    for attr, val in (
        ("use_ssr", True),
        ("use_ssr_halfres", False),
        ("ssr_quality", 0.85),
        ("ssr_max_roughness", 0.55),
        ("use_gtao", True),
        ("gtao_distance", 0.35),
        ("gtao_quality", 0.45),
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
    # Camera rays keep the mute studio grey. Glossy/diffuse rays get a warm
    # HDRI-like punch so gold/plate can chrome without washing the plate.
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
    wrap = bpy.data.lights.new("Wrap", "AREA")
    wrap.energy = 640.0
    wrap.size = 7.0
    wrap.color = (1.0, 0.97, 0.92)
    wrap_o = bpy.data.objects.new("Wrap", wrap)
    bpy.context.collection.objects.link(wrap_o)
    wrap_o.location = (0.0, 2.6, 4.8)
    wrap_o.rotation_euler = (math.radians(110), 0.0, 0.0)
    rim = bpy.data.lights.new("Rim", "SUN")
    rim.energy = 2.6
    rim.angle = 0.20
    rim.color = (1.0, 0.95, 0.86)
    rim_o = bpy.data.objects.new("Rim", rim)
    bpy.context.collection.objects.link(rim_o)
    rim_o.location = (-1.2, 2.8, 3.6)
    rim_o.rotation_euler = (math.radians(125), 0.0, math.radians(-20))
    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.rotation_euler = (math.radians(90.0), 0.0, 0.0)
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


def _link_or_value(nt, bsdf, name, socket=None, value=None):
    if name not in bsdf.inputs:
        return
    if socket is not None:
        nt.links.new(socket, bsdf.inputs[name])
    elif value is not None:
        bsdf.inputs[name].default_value = value


def _mix_sockets(mix):
    """Blender 4 Mix node: Factor / A / B / Result across data types."""
    ins = mix.inputs
    outs = mix.outputs
    fac = ins.get("Factor") or ins[0]
    a = ins.get("A")
    b = ins.get("B")
    if a is None or b is None:
        # RGBA sockets are often indices 6/7 after data_type is set.
        colored = [s for s in ins if s.name in ("A", "B", "Color1", "Color2") or "Color" in s.name]
        if len(colored) >= 2:
            a, b = colored[0], colored[1]
        elif len(ins) >= 8:
            a, b = ins[6], ins[7]
    result = outs.get("Result") or outs[-1]
    return fac, a, b, result


def assign_albedo(mesh_ob):
    """Punch PBR from the same FBX maps. Do not rebake albedo."""
    albedo = _img("texture_0") or _img("texture")
    # Prefer the color map: reject metallic/rough/normal if we grabbed those.
    if albedo is not None:
        n = albedo.name.lower()
        if any(s in n for s in ("metallic", "rough", "normal")):
            albedo = None
            for im in bpy.data.images:
                nn = im.name.lower()
                if "texture_0" in nn and not any(s in nn for s in ("metallic", "rough", "normal")) and im.size[0] >= 256:
                    albedo = im
                    break
    metallic = _img("metallic")
    rough = _img("rough")
    normal = _img("normal")
    print(
        "punch maps",
        "albedo", getattr(albedo, "name", None),
        "metallic", getattr(metallic, "name", None),
        "rough", getattr(rough, "name", None),
        "normal", getattr(normal, "name", None),
    )

    mat = mesh_ob.data.materials[0] if mesh_ob.data.materials else bpy.data.materials.new("AldricPunch")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.name = "Albedo"
    if albedo is not None:
        tex.image = albedo
        try:
            albedo.colorspace_settings.name = "sRGB"
        except Exception:
            pass

    # Navy value lift (keep hue): B-dominant + dark → multiply.
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(tex.outputs["Color"], sep.inputs["Color"])
    # navy = (B > R) * (B > G) * (V < 0.28) where V ~ max(RGB)
    max_rg = nt.nodes.new("ShaderNodeMath")
    max_rg.operation = "MAXIMUM"
    nt.links.new(sep.outputs["Red"], max_rg.inputs[0])
    nt.links.new(sep.outputs["Green"], max_rg.inputs[1])
    vmax = nt.nodes.new("ShaderNodeMath")
    vmax.operation = "MAXIMUM"
    nt.links.new(max_rg.outputs["Value"], vmax.inputs[0])
    nt.links.new(sep.outputs["Blue"], vmax.inputs[1])
    b_gt_r = nt.nodes.new("ShaderNodeMath")
    b_gt_r.operation = "GREATER_THAN"
    nt.links.new(sep.outputs["Blue"], b_gt_r.inputs[0])
    nt.links.new(sep.outputs["Red"], b_gt_r.inputs[1])
    b_gt_g = nt.nodes.new("ShaderNodeMath")
    b_gt_g.operation = "GREATER_THAN"
    nt.links.new(sep.outputs["Blue"], b_gt_g.inputs[0])
    nt.links.new(sep.outputs["Green"], b_gt_g.inputs[1])
    dark = nt.nodes.new("ShaderNodeMath")
    dark.operation = "LESS_THAN"
    nt.links.new(vmax.outputs["Value"], dark.inputs[0])
    dark.inputs[1].default_value = 0.30
    navy_a = nt.nodes.new("ShaderNodeMath")
    navy_a.operation = "MULTIPLY"
    nt.links.new(b_gt_r.outputs["Value"], navy_a.inputs[0])
    nt.links.new(b_gt_g.outputs["Value"], navy_a.inputs[1])
    navy = nt.nodes.new("ShaderNodeMath")
    navy.operation = "MULTIPLY"
    nt.links.new(navy_a.outputs["Value"], navy.inputs[0])
    nt.links.new(dark.outputs["Value"], navy.inputs[1])
    lift = nt.nodes.new("ShaderNodeMix")
    lift.data_type = "RGBA"
    lift.blend_type = "MIX"
    lf, la, lb, lout = _mix_sockets(lift)
    nt.links.new(navy.outputs["Value"], lf)
    nt.links.new(tex.outputs["Color"], la)
    # Same hue, +~45% value on crushed navy only.
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mf, ma, mb, mout = _mix_sockets(mul)
    mf.default_value = 1.0
    nt.links.new(tex.outputs["Color"], ma)
    mb.default_value = (1.65, 1.55, 1.80, 1.0)
    nt.links.new(mout, lb)
    # Mild overall value lift so plate/navy don't sit under the mute floor.
    gain = nt.nodes.new("ShaderNodeMix")
    gain.data_type = "RGBA"
    gain.blend_type = "MULTIPLY"
    gf, ga, gb, gout = _mix_sockets(gain)
    gf.default_value = 1.0
    nt.links.new(lout, ga)
    gb.default_value = (1.18, 1.16, 1.14, 1.0)
    nt.links.new(gout, bsdf.inputs["Base Color"])

    # Gold / plate metal+smooth. Prefer FBX maps, then boost gold.
    r_gt_b = nt.nodes.new("ShaderNodeMath")
    r_gt_b.operation = "SUBTRACT"
    nt.links.new(sep.outputs["Red"], r_gt_b.inputs[0])
    nt.links.new(sep.outputs["Blue"], r_gt_b.inputs[1])
    gold_h = nt.nodes.new("ShaderNodeMath")
    gold_h.operation = "GREATER_THAN"
    nt.links.new(r_gt_b.outputs["Value"], gold_h.inputs[0])
    gold_h.inputs[1].default_value = 0.055
    gold_v = nt.nodes.new("ShaderNodeMath")
    gold_v.operation = "GREATER_THAN"
    nt.links.new(sep.outputs["Red"], gold_v.inputs[0])
    gold_v.inputs[1].default_value = 0.20
    gold = nt.nodes.new("ShaderNodeMath")
    gold.operation = "MULTIPLY"
    nt.links.new(gold_h.outputs["Value"], gold.inputs[0])
    nt.links.new(gold_v.outputs["Value"], gold.inputs[1])

    met_tex = nt.nodes.new("ShaderNodeTexImage")
    met_tex.name = "Metallic"
    if metallic is not None:
        met_tex.image = metallic
        try:
            metallic.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
    # Full metallic-map on steel goes black under studio (no Meshy HDRI).
    # Use the map as a hint; gold gets chrome; plate stays mostly dielectric
    # so silver albedo still reads.
    met_hint = nt.nodes.new("ShaderNodeMath")
    met_hint.operation = "MULTIPLY"
    nt.links.new(met_tex.outputs["Color"], met_hint.inputs[0])
    met_hint.inputs[1].default_value = 0.22
    gold_met = nt.nodes.new("ShaderNodeMath")
    gold_met.operation = "MULTIPLY"
    nt.links.new(gold.outputs["Value"], gold_met.inputs[0])
    gold_met.inputs[1].default_value = 0.88
    met_max = nt.nodes.new("ShaderNodeMath")
    met_max.operation = "MAXIMUM"
    nt.links.new(met_hint.outputs["Value"], met_max.inputs[0])
    nt.links.new(gold_met.outputs["Value"], met_max.inputs[1])
    _link_or_value(nt, bsdf, "Metallic", met_max.outputs["Value"])

    rgh_tex = nt.nodes.new("ShaderNodeTexImage")
    rgh_tex.name = "Roughness"
    if rough is not None:
        rgh_tex.image = rough
        try:
            rough.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
    # Gold: pull roughness toward 0.16 (chrome highlight, not blown white).
    # Plate: slightly smoother than mute matte so rims read, albedo still shows.
    rgh_scale = nt.nodes.new("ShaderNodeMath")
    rgh_scale.operation = "MULTIPLY"
    nt.links.new(rgh_tex.outputs["Color"], rgh_scale.inputs[0])
    rgh_scale.inputs[1].default_value = 0.72
    rgh_mix = nt.nodes.new("ShaderNodeMix")
    rgh_mix.data_type = "FLOAT"
    rf, ra, rb, rout = _mix_sockets(rgh_mix)
    nt.links.new(gold.outputs["Value"], rf)
    nt.links.new(rgh_scale.outputs["Value"], ra)
    rb.default_value = 0.16
    _link_or_value(nt, bsdf, "Roughness", rout, value=0.38)

    if normal is not None:
        ntex = nt.nodes.new("ShaderNodeTexImage")
        ntex.image = normal
        try:
            normal.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = 0.85
        nt.links.new(ntex.outputs["Color"], nmap.inputs["Color"])
        _link_or_value(nt, bsdf, "Normal", nmap.outputs["Normal"])

    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if not mesh_ob.data.materials:
        mesh_ob.data.materials.append(mat)
    else:
        mesh_ob.data.materials[0] = mat


def mesh_world_ys(mesh_ob, groups=None):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    ev = mesh_ob.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    vg = {g.index: g.name for g in mesh_ob.vertex_groups}
    ys = []
    src = mesh_ob.data
    n = min(len(me.vertices), len(src.vertices))
    for i in range(n):
        if groups:
            names = {vg.get(g.group, "") for g in src.vertices[i].groups if g.weight > 0.35}
            if not (names & groups):
                continue
        ys.append((mw @ me.vertices[i].co).y)
    ev.to_mesh_clear()
    return ys


def plant(root, mesh_ob):
    ys = mesh_world_ys(mesh_ob, FOOT) or mesh_world_ys(mesh_ob)
    if not ys:
        return 0.0
    dy = min(ys) - 0.002
    root.location.y -= dy
    bpy.context.view_layer.update()
    return dy


def import_fbx():
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
    # Mixamo after Blender FBX import: Z-up, face −Y, +X = Mixamo left.
    # X−90 → Y-up, face +Z. Negative X scale → Mixamo right on +X so the
    # rear Play cam (viewer-right) shows the character-RIGHT scabbard.
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    arm.rotation_mode = "XYZ"
    arm.rotation_euler = (-math.pi / 2.0, 0.0, 0.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    bpy.context.view_layer.update()
    ys = mesh_world_ys(mesh)
    h = max(ys) - min(ys)
    sx = -1.0
    syz = TARGET_H / h if h > 0.4 else 1.0
    arm.scale = (sx * syz, syz, syz)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
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
    walk = acts[0]
    arm.animation_data_create()
    print(
        "imported", mesh.name, "v", len(mesh.data.vertices), "f", len(mesh.data.polygons),
        "walk", walk.name, "range", tuple(walk.frame_range),
        "fps", bpy.context.scene.render.fps, "bboxY", tuple(round(v, 3) for v in (min(ys), max(ys))),
    )
    return root, arm, mesh, walk


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


def pose_walk(root, arm, mesh, walk, frame: float):
    arm.animation_data.action = walk
    bpy.context.scene.frame_set(int(round(frame)))
    root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    plant(root, mesh)


def rest_pose(root, arm, mesh, rest_act):
    if rest_act is not None:
        arm.animation_data.action = rest_act
        bpy.context.scene.frame_set(int(rest_act.frame_range[0]))
    else:
        arm.animation_data.action = None
        for pb in arm.pose.bones:
            pb.matrix_basis.identity()
    root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    plant(root, mesh)


def write_drop(note: dict):
    DROP.mkdir(parents=True, exist_ok=True)
    punch_readme = DROP / "PUNCH_LIGHT.md"
    punch_readme.write_text(
        "# AUTO_RIG_PATH — World LIGHT + MATERIAL punch (candidates)\n\n"
        "Path A weight-paint **CANCELLED**. Same Meshy Animate FBX albedo "
        "(no rebake). Stronger key + exposure, FBX metallic/roughness wired, "
        "navy value lift, gold smoothness/metal.\n\n"
        "Unlocked hub PNG from `52baf60` is **held** until Design PASSes this punch.\n"
        "**Do NOT claim Design PASS.**\n\n"
        "Punch stills: `world_*_punch.png`. Punch walk: "
        "`sir_aldric_meshy_animate_walk_punch_toward_top.mp4`.\n"
    )
    for name in (
        "world_front_punch.png", "world_34_front_punch.png",
        "world_rear_punch.png", "world_rear_34_punch.png",
        "sir_aldric_meshy_animate_walk_punch_toward_top.mp4",
    ):
        src = PROOF / name
        if src.exists():
            shutil.copy2(src, DROP / name)
    for name in (
        "world_walk_contact_l_punch.png", "world_walk_mid_swing_punch.png",
        "world_walk_pass_l_punch.png",
    ):
        src = WALK / name
        if src.exists():
            shutil.copy2(src, DROP / name)
    (DROP / "meshy_animate_punch.json").write_text(json.dumps(note, indent=2) + "\n")
    print("drop punch", DROP)


def main():
    stills_only = os.environ.get("ALD_STILLS_ONLY") == "1"
    root, arm, mesh, walk = import_fbx()
    cam = setup_world()
    PROOF.mkdir(parents=True, exist_ok=True)
    WALK.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    PREVIEW = ROOT / "Docs/Survival/previews"

    rest_pose(root, arm, mesh, None)
    stills = (
        ("world_front_punch", CAM_FRONT, CAM_FRONT_T),
        ("world_34_front_punch", CAM_34_FRONT, CAM_34_FRONT_T),
        ("world_rear_punch", CAM_EYE, CAM_TARGET),
        ("world_rear_34_punch", CAM_EYE_34, CAM_TARGET),
    )
    for name, eye, tgt in stills:
        set_cam(cam, eye, tgt)
        dest = PROOF / f"{name}.png"
        render_to(dest)
        try:
            (ART / f"{name}.png").write_bytes(dest.read_bytes())
        except OSError:
            pass

    # Hub candidate — do not overwrite unlocked 52baf60 master.
    rear = PROOF / "world_rear_punch.png"
    if rear.exists():
        cand = PREVIEW / "sir_aldric_punch_rear_gameview_1080x1920.png"
        shutil.copy2(rear, cand)
        print("hub candidate", cand, cand.stat().st_size)

    f0, f1 = walk.frame_range
    mp4 = PROOF / "sir_aldric_meshy_animate_walk_punch_toward_top.mp4"
    if not stills_only:
        set_cam(cam, CAM_EYE, CAM_TARGET)
        nclip = int(f1 - f0) + 1
        fps = 24
        bpy.context.scene.render.fps = fps
        try:
            bpy.context.scene.eevee.taa_render_samples = 16
        except AttributeError:
            pass
        cycles = 1
        frames = []
        for _c in range(cycles):
            for i in range(nclip):
                fr = f0 + i
                pose_walk(root, arm, mesh, walk, fr)
                dest = WALK / f"f_{len(frames):03d}.png"
                render_to(dest)
                frames.append(dest)

        mid = f0 + (f1 - f0) * 0.40
        contact = f0 + (f1 - f0) * 0.25
        pose_walk(root, arm, mesh, walk, contact)
        render_to(WALK / "world_walk_contact_l_punch.png")
        pose_walk(root, arm, mesh, walk, mid)
        render_to(WALK / "world_walk_mid_swing_punch.png")
        pose_walk(root, arm, mesh, walk, f0)
        render_to(WALK / "world_walk_pass_l_punch.png")

        subprocess.check_call(
            [
                "ffmpeg", "-y", "-framerate", str(fps),
                "-i", str(WALK / "f_%03d.png"),
                "-pix_fmt", "yuv420p", "-vf", "scale=1080:1920",
                "-crf", "18", "-movflags", "+faststart", str(mp4),
            ]
        )
        print("mp4", mp4, mp4.stat().st_size)
        gif = PREVIEW / "sir_aldric_punch_walks_toward_top.gif"
        subprocess.check_call(
            [
                "ffmpeg", "-y", "-i", str(mp4),
                "-vf",
                "fps=12,scale=540:960:flags=lanczos,split[s0][s1];"
                "[s0]palettegen=max_colors=128:stats_mode=diff[p];"
                "[s1][p]paletteuse=dither=bayer:bayer_scale=3",
                str(gif),
            ]
        )
        print("gif", gif, gif.stat().st_size)
        try:
            (ART / mp4.name).write_bytes(mp4.read_bytes())
            (ART / gif.name).write_bytes(gif.read_bytes())
            for n in (
                "world_walk_contact_l_punch.png",
                "world_walk_mid_swing_punch.png",
            ):
                (ART / n).write_bytes((WALK / n).read_bytes())
        except OSError as exc:
            print("artifact skip", exc)

    note = {
        "lookPassClaimed": False,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "pathAWeightPaint": "CANCELLED",
        "hubOverwrite": False,
        "source": "meshy-animate-walk-fbx-20260922",
        "fbx": str(FBX.relative_to(ROOT)),
        "clip": walk.name,
        "clipFrames": [float(f0), float(f1)],
        "verts": len(mesh.data.vertices),
        "faces": len(mesh.data.polygons),
        "punch": {
            "keyEnergy": 12.5,
            "exposure": 0.22,
            "albedoRebaked": False,
            "navyValueLift": True,
            "goldMetalSmooth": True,
            "fbxMetallicRoughnessWired": True,
            "metallicMapScale": 0.22,
            "lightPathHdrI": True,
        },
        "honestArt": (
            "World LIGHT + MATERIAL punch on Meshy Animate FBX as-is. "
            "Same albedo (no rebake). Path A cancelled. Punch candidates only — "
            "unlocked hub PNG held. Do NOT claim Design PASS."
        ),
        "walkClip": str(mp4.relative_to(ROOT)) if mp4.exists() else None,
    }
    (PROOF / "meshy_animate_punch.json").write_text(json.dumps(note, indent=2) + "\n")
    write_drop(note)
    print("done meshy animate world PUNCH (no Design PASS)")


if __name__ == "__main__":
    main()
