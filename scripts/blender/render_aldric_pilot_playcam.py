#!/usr/bin/env python3
"""Sir Aldric PILOT Play-cam rematch (no Unity Editor on this VM).

Mixamo holefixed mid280k + Standard Walk / Inward Slash. RH sword prop.
Honest label: Play-cam rematch, not Unity Camera.Render, not Meshy website stills.
"""
from __future__ import annotations

import math
import os
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot"
LOOK = PACK / "SirAldric_body_holefixed_walk.fbx"
WALK = PACK / "SirAldric_body_holefixed_walk.fbx"
ATK = PACK / "SirAldric_body_holefixed_slash.fbx"
SWORD = PACK / "SirAldric_PILOT_sword.fbx"
CAPE = PACK / "SirAldric_PILOT_cape.fbx"
ALBEDO = PACK / "mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture.png"
MET = PACK / "mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture_metallic.png"
ROUGH = PACK / "mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture_roughness.png"
PROOF = ROOT / "Docs/Survival/previews/aldric_pilot_20260928"
ART = Path("/opt/cursor/artifacts")

CAM_REAR = ((0.0, 1.75, -2.90), (0.0, 0.85, 0.15))
CAM_FRONT = ((0.0, 1.75, 3.20), (0.0, 0.85, 0.15))
CAM_34 = ((1.70, 1.75, -2.20), (0.0, 0.85, 0.15))
CAM_FOV = 34.0


def u2b(p):
    return (p[0], -p[2], p[1])


def bind_pbr(obj):
    mat = bpy.data.materials.new(obj.name + "_PilotLook")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    albedo = None
    if ALBEDO.exists():
        albedo = bpy.data.images.load(str(ALBEDO))
    if albedo is None:
        for im in bpy.data.images:
            n = im.name.lower()
            if im.size[0] >= 256 and ("texture_0" in n or "basecolor" in n or "image_0" in n):
                if "metallic" in n or "rough" in n or "normal" in n:
                    continue
                albedo = im
                break
    if albedo is None:
        for im in bpy.data.images:
            if im.size[0] >= 256:
                albedo = im
                break
    if albedo is not None:
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = albedo
        try:
            tex.image.colorspace_settings.name = "sRGB"
        except Exception:
            pass
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    if MET.exists():
        mt = nt.nodes.new("ShaderNodeTexImage")
        mt.image = bpy.data.images.load(str(MET))
        try:
            mt.image.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
        nt.links.new(mt.outputs["Color"], bsdf.inputs["Metallic"])
    if ROUGH.exists():
        rt = nt.nodes.new("ShaderNodeTexImage")
        rt.image = bpy.data.images.load(str(ROUGH))
        try:
            rt.image.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
        nt.links.new(rt.outputs["Color"], bsdf.inputs["Roughness"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def setup_studio():
    sc = bpy.context.scene
    world = bpy.data.worlds.new("PilotClear")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.08, 0.09, 0.07, 1)
        bg.inputs[1].default_value = 0.35
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    try:
        sc.display_settings.display_device = "sRGB"
        sc.view_settings.view_transform = "Standard"
        sc.eevee.taa_render_samples = 24
    except Exception:
        pass

    bpy.ops.mesh.primitive_plane_add(size=1, location=u2b((0.0, 0.0, 1.6)))
    ground = bpy.context.active_object
    ground.scale = (6.0, 10.0, 1.0)
    gmat = bpy.data.materials.new("Ground")
    gmat.use_nodes = True
    bs = gmat.node_tree.nodes.get("Principled BSDF")
    if bs:
        bs.inputs["Base Color"].default_value = (0.10, 0.12, 0.09, 1)
        bs.inputs["Roughness"].default_value = 0.95
    ground.data.materials.append(gmat)

    def unlit(color):
        m = bpy.data.materials.new("U")
        m.use_nodes = True
        m.node_tree.nodes.clear()
        o = m.node_tree.nodes.new("ShaderNodeOutputMaterial")
        e = m.node_tree.nodes.new("ShaderNodeEmission")
        e.inputs[0].default_value = (color[0], color[1], color[2], 1)
        m.node_tree.links.new(e.outputs[0], o.inputs["Surface"])
        return m

    gold = unlit((0.83, 0.69, 0.32))
    for i in range(6):
        bpy.ops.mesh.primitive_cube_add(size=1, location=u2b((0.0, 0.02, 0.15 + i * 0.45)))
        c = bpy.context.active_object
        c.scale = (0.55 - i * 0.04, 0.10, 0.02)
        c.data.materials.append(gold)
    bpy.ops.mesh.primitive_cube_add(size=1, location=u2b((0.0, 0.28, 2.15)))
    en = bpy.context.active_object
    en.scale = (0.38, 0.38, 0.55)
    en.data.materials.append(unlit((0.22, 0.07, 0.12)))

    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 2.4
    key.color = (1.0, 0.96, 0.88)
    ko = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(ko)
    ko.rotation_euler = (math.radians(52), 0.0, math.radians(-16))
    fill = bpy.data.lights.new("Fill", "SUN")
    fill.energy = 0.7
    fill.color = (0.82, 0.88, 1.0)
    fo = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fo)
    fo.rotation_euler = (math.radians(70), 0.0, math.radians(35))

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


def pick_action(*needles):
    named = [a for a in bpy.data.actions if any(n.lower() in a.name.lower() for n in needles)]
    pool = named or list(bpy.data.actions)
    return max(pool, key=lambda a: a.frame_range[1] - a.frame_range[0])


def attach_sword(arm):
    if not SWORD.exists():
        return
    bpy.ops.import_scene.fbx(filepath=str(SWORD))
    sword = next(o for o in bpy.data.objects if o.type == "MESH" and "sword" in o.name.lower())
    sword.parent = arm
    sword.parent_type = "BONE"
    sword.parent_bone = "mixamorig:RightHand"
    sword.location = (0.0, 0.08, 0.0)
    sword.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    print("sword parented to", sword.parent_bone)


def attach_cape(arm):
    if not CAPE.exists():
        return
    bpy.ops.import_scene.fbx(filepath=str(CAPE))
    cape = next(o for o in bpy.data.objects if o.type == "MESH" and "cape" in o.name.lower())
    cape.parent = arm
    print("cape parented to armature", arm.name)


def import_mixamo_body(clip_path: Path):
    """Mixamo-skinned holefixed walk mesh. Slash clip mesh discarded."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(LOOK))
    look_arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    look_arm.scale = (1.0, 1.0, 1.0)
    look_meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    before_meshes = set(look_meshes)
    before_arms = {look_arm}
    same = clip_path.resolve() == LOOK.resolve()
    if not same:
        bpy.ops.import_scene.fbx(filepath=str(clip_path))
        for o in list(bpy.data.objects):
            if o.type == "MESH" and o not in before_meshes:
                bpy.data.objects.remove(o, do_unlink=True)
        clip_arm = next(o for o in bpy.data.objects if o.type == "ARMATURE" and o not in before_arms)
        act = max(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0])
        clip_arm.animation_data_create()
        clip_arm.animation_data.action = act
        if look_arm.animation_data:
            look_arm.animation_data_clear()
        look_arm.animation_data_create()
        look_arm.animation_data.action = act
        clip_arm.hide_render = True
        clip_arm.hide_viewport = True
    else:
        act = pick_action("mixamo", "Walk", "Layer")
        look_arm.animation_data_create()
        look_arm.animation_data.action = act
    mesh = next(o for o in bpy.data.objects if o.type == "MESH" and o in before_meshes)
    attach_sword(look_arm)
    print("look_mesh", mesh.name, "arm", look_arm.name, "act", act.name, tuple(act.frame_range))
    print("actions", [a.name for a in bpy.data.actions])
    return mesh, look_arm, act


def pose_at(arm, act, frame):
    if arm.animation_data:
        arm.animation_data.action = act
    bpy.context.scene.frame_set(int(frame))
    bpy.context.view_layer.update()
    hips = arm.pose.bones.get("mixamorig:Hips")
    rarm = arm.pose.bones.get("mixamorig:RightArm")
    print(
        "pose",
        frame,
        "hips",
        tuple(round(x, 3) for x in hips.matrix_basis.to_euler()) if hips else None,
        "RArm",
        tuple(round(x, 3) for x in rarm.matrix_basis.to_euler()) if rarm else None,
    )


def render_walk():
    mesh, arm, act = import_mixamo_body(WALK)
    bind_pbr(mesh)
    still = 18
    print("walk still", still, "mesh", mesh.name)
    cam = setup_studio()
    pose_at(arm, act, still)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_pilot_rear_walk_playcam.png")
    look(cam, *CAM_FRONT)
    render_to(PROOF / "sir_aldric_pilot_front_walk_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_pilot_34_walk_playcam.png")


def render_attack():
    mesh, arm, act = import_mixamo_body(ATK)
    bind_pbr(mesh)
    still = 32
    print("slash still", still, "mesh", mesh.name)
    cam = setup_studio()
    pose_at(arm, act, still)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_pilot_rear_strike_playcam.png")
    look(cam, *CAM_FRONT)
    render_to(PROOF / "sir_aldric_pilot_front_strike_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_pilot_34_strike_playcam.png")


def main():
    only = os.environ.get("ALD_PILOT_ONLY", "all")
    PROOF.mkdir(parents=True, exist_ok=True)
    if only in ("all", "walk"):
        render_walk()
    if only in ("all", "attack"):
        render_attack()
    print("pilot rematch done", only)


if __name__ == "__main__":
    main()
