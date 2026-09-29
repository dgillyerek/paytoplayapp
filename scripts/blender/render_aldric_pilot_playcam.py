#!/usr/bin/env python3
"""Sir Aldric PILOT Play-cam rematch (no Unity Editor on this VM).

Mixamo holefixed mid280k + Standard Walk / Inward Slash. RH sword prop.
Honest label: Play-cam rematch, not Unity Camera.Render, not Meshy website stills.

Blender Mixamo already faces rear (back to camera). Unity FaceWorldTop yaws 180
(needed because Unity Mixamo faces −Z). Do not scale-X flip the body — that put
the sword on the left hand. Rematch applies the same RH max-reach slash as Unity
ApplyAttackWindupLift: unfold elbow, tip far UR → horizontal front (+Z) → LL.
Derek FAIL b0a9509: rematch mid stills are not Unity Game-view — Game-view mid
was skyward (world +Y). Mid must be a horizontal thrust toward the enemy.
"""
from __future__ import annotations

import math
import os
from pathlib import Path

import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

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

CAM_REAR = ((0.0, 1.92, -3.08), (0.0, 1.12, 0.15))
CAM_FRONT = ((0.0, 1.92, 3.38), (0.0, 1.12, 0.15))
CAM_34 = ((1.82, 1.92, -2.38), (0.0, 1.12, 0.15))
CAM_FOV = 42.0

# Match Unity ApplyAttackWindupLift (max-reach RH arc, elbow unfold).
# FrontUp stays near 0: FrontUp 0.48 read as sky in Unity Game-view (b0a9509).
SLASH_UR_RIGHT = 0.72
SLASH_UR_UP = 0.48
SLASH_UR_FRONT = 0.38
SLASH_FRONT_RIGHT = 0.12
SLASH_FRONT_UP = 0.06
SLASH_FRONT_Z = 1.00
SLASH_LL_RIGHT = -0.85
SLASH_LL_UP = -0.40
SLASH_LL_FRONT = 0.30
SLASH_FRONT_U = 0.32
SLASH_FRONT_HOLD_U = 0.55
SLASH_LL_U = 0.78
SLASH_FIRST = 8
SLASH_LAST = 40
SLASH_MID = 20
HELD_SWORD_REST_X = 90.0


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
        unique = "sir_aldric_pilot_rh_front_thrust_" + path.name.replace("sir_aldric_pilot_", "")
        (ART / unique).write_bytes(path.read_bytes())
    except OSError as exc:
        print("artifact skip", exc)


def pick_action(*needles):
    named = [a for a in bpy.data.actions if any(n.lower() in a.name.lower() for n in needles)]
    pool = named or list(bpy.data.actions)
    return max(pool, key=lambda a: a.frame_range[1] - a.frame_range[0])


def attach_sword(arm, sword_z=0.0):
    if not SWORD.exists():
        return None
    bpy.ops.import_scene.fbx(filepath=str(SWORD))
    sword = next(o for o in bpy.data.objects if o.type == "MESH" and "sword" in o.name.lower())
    sword.parent = arm
    sword.parent_type = "BONE"
    sword.parent_bone = "mixamorig:RightHand"
    sword.location = (0.0, 0.08, 0.0)
    sword.rotation_euler = (math.radians(90.0), 0.0, math.radians(sword_z))
    sword.scale = (1.0, 1.0, 1.0)
    print("sword parented to", sword.parent_bone, "eulerZ", sword_z, "blade~1.01m (Unity compensates parent lossyScale)")
    return sword


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
    # Mixamo Armature Lcl Scaling is 0.01. Leave it and the knight is ~2 cm (Derek FAIL bf5a5fa).
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
    sword = attach_sword(look_arm)
    print("look_mesh", mesh.name, "arm", look_arm.name, "act", act.name, tuple(act.frame_range))
    print("actions", [a.name for a in bpy.data.actions])
    return mesh, look_arm, act, sword


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


def windup_weight(frame):
    _ = frame
    return 1.0


def freeze_pose(arm, act, frame):
    """Evaluate the Mixamo take then disconnect FCurves so extras stick (Unity after Evaluate)."""
    arm.animation_data_create()
    arm.animation_data.action = act
    bpy.context.scene.frame_set(int(frame))
    bpy.context.view_layer.update()
    baked = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}
    arm.animation_data_clear()
    for pb in arm.pose.bones:
        pb.matrix_basis = baked[pb.name]
    bpy.context.view_layer.update()


def uvec(p):
    """Unity direction (x,y,z) → blender."""
    return Vector((p[0], -p[2], p[1]))


def to_unity(v):
    """Blender world (x, y, z) → Unity (x, y, z). Inverse of u2b."""
    return (v.x, v.z, -v.y)


def bone_world(arm, name):
    pb = arm.pose.bones.get(name)
    if pb is None:
        return None
    return (arm.matrix_world @ pb.matrix).to_translation()


def dump_hands(arm, sword, tag, desired_unity=None):
    """Numeric RH-vs-LH + tip reach proof. Rematch stills are not Unity Game-view."""
    lh = bone_world(arm, "mixamorig:LeftHand")
    rh = bone_world(arm, "mixamorig:RightHand")
    head = bone_world(arm, "mixamorig:Head")
    hips = bone_world(arm, "mixamorig:Hips")
    rarm = bone_world(arm, "mixamorig:RightArm")
    rfore = bone_world(arm, "mixamorig:RightForeArm")
    parent = sword.parent_bone if sword is not None else None
    tip_b = None
    tip_u = None
    if sword is not None:
        raw = sword.matrix_world.to_3x3() @ Vector((0.0, 0.0, 1.0))
        if raw.length > 1e-6:
            tip_b = raw.normalized()
            tip_u = to_unity(tip_b)
    lh_u = to_unity(lh) if lh is not None else None
    rh_u = to_unity(rh) if rh is not None else None
    head_u = to_unity(head) if head is not None else None
    rh_char_right = rh is not None and lh is not None and rh.x < lh.x
    elbow = None
    if rarm is not None and rfore is not None and rh is not None:
        upper = (rfore - rarm).normalized()
        lower = (rh - rfore).normalized()
        elbow = math.degrees(upper.angle(lower))
    tip_pos = None
    tip_radial = None
    if rh is not None and tip_b is not None:
        tip_pos = rh + (tip_b * 1.01)
        if hips is not None:
            tip_radial = (tip_pos - hips).length
    hand_radial = (rh - hips).length if rh is not None and hips is not None else None
    print(
        "dump",
        tag,
        "parent",
        parent,
        "RH_u",
        None if rh_u is None else tuple(round(x, 3) for x in rh_u),
        "tip_u",
        None if tip_u is None else tuple(round(x, 3) for x in tip_u),
        "elbow_deg",
        None if elbow is None else round(elbow, 1),
        "hand_radial",
        None if hand_radial is None else round(hand_radial, 3),
        "tip_radial",
        None if tip_radial is None else round(tip_radial, 3),
        "RH_character_right",
        rh_char_right,
    )
    payload = {
        "tag": tag,
        "sword_parent": parent,
        "LH_unity": lh_u,
        "RH_unity": rh_u,
        "head_unity": head_u,
        "tip_unity": tip_u,
        "elbow_deg": elbow,
        "hand_radial": hand_radial,
        "tip_radial": tip_radial,
        "RH_character_right": rh_char_right,
        "desired_unity": None if desired_unity is None else list(desired_unity),
        "label": "Play-cam rematch, not Unity Game-view. Rear +Z is foreshortened; 3/4 shows the horizontal thrust. Trust Game-view over rematch.",
    }
    PROOF.mkdir(parents=True, exist_ok=True)
    path = PROOF / f"sir_aldric_pilot_dump_{tag}.json"
    path.write_text(__import__("json").dumps(payload, indent=2) + "\n")
    print("wrote", path)


def blender_from_unity(right, up, front):
    """Unity after yaw 180: +X right, +Y up, +Z front.

    Blender Mixamo already faces rear: character-right = −X, up = +Z, front = −Y.
    """
    v = Vector((-right, -front, up))
    if v.length < 1e-6:
        return Vector((0.0, -1.0, 0.0))
    return v.normalized()


def attack_slash_u(frame):
    span = float(SLASH_LAST - SLASH_FIRST)
    if span <= 1e-4:
        return 0.0
    return max(0.0, min(1.0, (float(frame) - SLASH_FIRST) / span))


def attack_slash_reach_unity_xyz(u):
    """Unity (right, up, front) before blender_from_unity. Mid hold is +Z, not +Y."""
    u = max(0.0, min(1.0, u))

    def sm(x):
        x = max(0.0, min(1.0, x))
        return x * x * (3.0 - 2.0 * x)

    if u <= SLASH_FRONT_U:
        t = sm(u / SLASH_FRONT_U)
        a = (SLASH_UR_RIGHT, SLASH_UR_UP, SLASH_UR_FRONT)
        b = (SLASH_FRONT_RIGHT, SLASH_FRONT_UP, SLASH_FRONT_Z)
    elif u <= SLASH_FRONT_HOLD_U:
        a = b = (SLASH_FRONT_RIGHT, SLASH_FRONT_UP, SLASH_FRONT_Z)
        t = 1.0
    elif u <= SLASH_LL_U:
        t = sm((u - SLASH_FRONT_HOLD_U) / (SLASH_LL_U - SLASH_FRONT_HOLD_U))
        a = (SLASH_FRONT_RIGHT, SLASH_FRONT_UP, SLASH_FRONT_Z)
        b = (SLASH_LL_RIGHT, SLASH_LL_UP, SLASH_LL_FRONT)
    else:
        a = b = (SLASH_LL_RIGHT, SLASH_LL_UP, SLASH_LL_FRONT)
        t = 1.0
    x = a[0] + (b[0] - a[0]) * t
    y = a[1] + (b[1] - a[1]) * t
    z = a[2] + (b[2] - a[2]) * t
    mag = math.sqrt((x * x) + (y * y) + (z * z))
    if mag < 1e-6:
        return (0.0, 0.06, 1.0)
    return (x / mag, y / mag, z / mag)


def attack_slash_reach_unity(u):
    x, y, z = attack_slash_reach_unity_xyz(u)
    return blender_from_unity(x, y, z)


def rotate_pose_bone(arm, pb, q_world):
    mw = arm.matrix_world @ pb.matrix
    origin = mw.to_translation()
    R = q_world.to_matrix().to_4x4()
    new_mw = Matrix.Translation(origin) @ R @ Matrix.Translation(-origin) @ mw
    pb.matrix = arm.matrix_world.inverted() @ new_mw


def apply_attack_windup(arm, sword, frame):
    """Match Unity ApplyAttackWindupLift: unfold elbow, max-reach UR→front→LL."""
    k = windup_weight(frame)
    rarm = arm.pose.bones.get("mixamorig:RightArm")
    rfore = arm.pose.bones.get("mixamorig:RightForeArm")
    rhand = arm.pose.bones.get("mixamorig:RightHand")
    dump_hands(arm, sword, f"slash_f{int(frame)}_native")
    u = attack_slash_u(frame)
    desired = attack_slash_reach_unity(u)
    if k > 0.001 and rarm is not None and rhand is not None:
        if rfore is not None:
            arm_w = (arm.matrix_world @ rarm.matrix).to_translation()
            fore_w = (arm.matrix_world @ rfore.matrix).to_translation()
            hand_w = (arm.matrix_world @ rhand.matrix).to_translation()
            upper = fore_w - arm_w
            lower = hand_w - fore_w
            if upper.length > 1e-4 and lower.length > 1e-4:
                q = lower.normalized().rotation_difference(upper.normalized())
                ident = Euler((0, 0, 0), "XYZ").to_quaternion()
                rotate_pose_bone(arm, rfore, ident.slerp(q, k))
                bpy.context.view_layer.update()
        arm_w = (arm.matrix_world @ rarm.matrix).to_translation()
        hand_w = (arm.matrix_world @ rhand.matrix).to_translation()
        reach = hand_w - arm_w
        if reach.length > 1e-4:
            q = reach.normalized().rotation_difference(desired)
            ident = Euler((0, 0, 0), "XYZ").to_quaternion()
            rotate_pose_bone(arm, rarm, ident.slerp(q, k))
            bpy.context.view_layer.update()
    if sword is not None:
        rest_q = Euler((math.radians(HELD_SWORD_REST_X), 0.0, 0.0), "XYZ").to_quaternion()
        if k < 0.001:
            sword.rotation_mode = "XYZ"
            sword.rotation_euler = Euler((math.radians(HELD_SWORD_REST_X), 0.0, 0.0), "XYZ")
        else:
            bone_q = Quaternion()
            if sword.parent_type == "BONE" and sword.parent_bone:
                pb = arm.pose.bones.get(sword.parent_bone)
                if pb is not None:
                    bone_q = (arm.matrix_world @ pb.matrix).to_quaternion()
            rest_world = bone_q @ rest_q
            # Blender mesh blade is local +Z (no Unity bakeAxisConversion).
            rest_blade = rest_world @ Vector((0.0, 0.0, 1.0))
            if rest_blade.length < 1e-6:
                rest_blade = Vector((0.0, 0.0, 1.0))
            aim_world = rest_blade.normalized().rotation_difference(desired) @ rest_world
            mixed = rest_world.slerp(aim_world, k)
            local = bone_q.inverted() @ mixed
            sword.rotation_mode = "QUATERNION"
            sword.rotation_quaternion = local
    bpy.context.view_layer.update()
    desired_u = attack_slash_reach_unity_xyz(u)
    print(
        "slash_reach",
        frame,
        "u",
        round(u, 3),
        "k",
        round(k, 3),
        "desired_u",
        tuple(round(c, 3) for c in desired_u),
        "desired_b",
        tuple(round(c, 3) for c in desired),
    )
    dump_hands(arm, sword, f"slash_f{int(frame)}", desired_unity=desired_u)


def pose_slash(arm, act, sword, frame):
    freeze_pose(arm, act, frame)
    apply_attack_windup(arm, sword, frame)


def render_walk():
    mesh, arm, act, _sword = import_mixamo_body(WALK)
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
    mesh, arm, act, sword = import_mixamo_body(ATK)
    bind_pbr(mesh)
    start = SLASH_FIRST
    mid = SLASH_MID
    finish = 32
    print("slash start", start, "mid", mid, "finish", finish, "mesh", mesh.name)
    cam = setup_studio()
    pose_slash(arm, act, sword, start)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_pilot_rear_strike_start_playcam.png")
    look(cam, *CAM_FRONT)
    render_to(PROOF / "sir_aldric_pilot_front_strike_start_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_pilot_34_strike_start_playcam.png")
    pose_slash(arm, act, sword, mid)
    look(cam, *CAM_REAR)
    render_to(PROOF / "sir_aldric_pilot_rear_strike_mid_playcam.png")
    look(cam, *CAM_FRONT)
    render_to(PROOF / "sir_aldric_pilot_front_strike_mid_playcam.png")
    look(cam, *CAM_34)
    render_to(PROOF / "sir_aldric_pilot_34_strike_mid_playcam.png")
    pose_slash(arm, act, sword, finish)
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
