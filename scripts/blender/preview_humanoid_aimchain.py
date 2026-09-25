#!/usr/bin/env python3
"""QA only: apply SirAldricHumanoidAttack AimChain on the WALK Mixamo FBX.

NOT Unity Game-view. Measures bind lengths (stretch=FAIL) and writes labeled
preview frames. Path A cancelled. No Design PASS.
"""
from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[2]
FBX = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_walk.fbx"
OUT = ROOT / "Docs/Survival/previews/facing_20260925/_humanoid_aimchain_preview"
ART = Path("/opt/cursor/artifacts")
TARGET_H = 1.86
YAW = 180.0
CAM_EYE = (0.0, 2.80, -5.40)
CAM_TARGET = (0.0, 0.90, 0.50)
CAM_FOV = 30.0
SECONDS = 1.40


def clamp01(x: float) -> float:
    return 0.0 if x < 0.0 else 1.0 if x > 1.0 else x


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def smooth01(x: float) -> float:
    x = clamp01(x)
    return x * x * (3.0 - 2.0 * x)


def evaluate(attack_t: float):
    u = clamp01(attack_t / SECONDS)
    if u < 0.20:
        k = smooth01(u / 0.20)
        return ("Draw", lerp(-0.14, -0.20, k), lerp(-0.02, -0.10, k), lerp(0.04, -0.05, k), lerp(2, 6, k), k > 0.35)
    if u < 0.38:
        k = smooth01((u - 0.20) / 0.18)
        return ("Guard", lerp(-0.20, -0.08, k), lerp(-0.10, 0.22, k), lerp(-0.05, 0.22, k), lerp(6, 10, k), True)
    if u < 0.62:
        k = smooth01((u - 0.38) / 0.24)
        return ("Strike", lerp(-0.08, 0.04, k), lerp(0.22, 0.12, k), lerp(0.22, 0.72, k), lerp(10, 14, k), True)
    r = smooth01((u - 0.62) / 0.38)
    return ("Recover", lerp(0.04, -0.14, r), lerp(0.12, -0.02, r), lerp(0.72, 0.04, r), lerp(14, 2, r), r < 0.55)


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


def mesh_world_pts(mesh_ob):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    ev = mesh_ob.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    pts = [mw @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    return pts


def plant(root, mesh_ob):
    pts = mesh_world_pts(mesh_ob)
    if not pts:
        return
    root.location.z -= min(p.z for p in pts) - 0.002
    bpy.context.view_layer.update()


def setup():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.image_settings.file_format = "PNG"
    sc.render.fps = 30
    try:
        sc.eevee.taa_render_samples = 8
    except AttributeError:
        pass
    world = bpy.data.worlds.new("W")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.10, 0.11, 0.09, 1)
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 10.0
    key.color = (1.0, 0.96, 0.88)
    key_o = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(key_o)
    key_o.rotation_euler = (math.radians(52), 0.0, math.radians(-16))
    bpy.ops.mesh.primitive_plane_add(size=10.0, location=(0, 0, 0))
    cam_data = bpy.data.cameras.new("Play")
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(CAM_FOV)
    cam_data.sensor_fit = "VERTICAL"
    cam = bpy.data.objects.new("Play", cam_data)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam
    cam.matrix_world = play_cam_matrix(CAM_EYE, CAM_TARGET)
    return cam


def import_walk():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    setup()
    bpy.ops.import_scene.fbx(filepath=str(FBX))
    for o in list(bpy.data.objects):
        if o.type == "MESH" and "Ico" in o.name:
            bpy.data.objects.remove(o, do_unlink=True)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
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
    clip = acts[0] if acts else None
    if clip:
        arm.animation_data_create()
        arm.animation_data.action = clip
        bpy.context.scene.frame_set(int(clip.frame_range[1]))
        bpy.context.view_layer.update()
        plant(arm, mesh)
        planted = {pb.name: pb.matrix.copy() for pb in arm.pose.bones}
        arm.animation_data_clear()
        bpy.context.view_layer.update()
        for pb in arm.pose.bones:
            pb.matrix = planted[pb.name]
        bpy.context.view_layer.update()
        plant(arm, mesh)
    return arm, mesh


def bone_world(arm, name):
    pb = arm.pose.bones[name]
    return arm.matrix_world @ pb.matrix.translation


def bone_len(arm, a, b):
    return (bone_world(arm, b) - bone_world(arm, a)).length


def apply_world_rot(arm, name, rot: Quaternion):
    pb = arm.pose.bones[name]
    R = rot.to_matrix().to_4x4()
    world = arm.matrix_world @ pb.matrix
    new_world = R @ world
    pb.matrix = arm.matrix_world.inverted() @ new_world


def aim_chain(arm, target: Vector):
    upper, lower, hand = "mixamorig:RightArm", "mixamorig:RightForeArm", "mixamorig:RightHand"
    from_v = bone_world(arm, hand) - bone_world(arm, upper)
    to_v = target - bone_world(arm, upper)
    if from_v.length > 1e-4 and to_v.length > 1e-4:
        apply_world_rot(arm, upper, from_v.normalized().rotation_difference(to_v.normalized()))
        bpy.context.view_layer.update()
    from_v = bone_world(arm, hand) - bone_world(arm, lower)
    to_v = target - bone_world(arm, lower)
    if from_v.length > 1e-4 and to_v.length > 1e-4:
        apply_world_rot(arm, lower, from_v.normalized().rotation_difference(to_v.normalized()))
        bpy.context.view_layer.update()


def make_sword(arm):
    bpy.ops.mesh.primitive_cube_add()
    sword = bpy.context.active_object
    sword.name = "ClipSword"
    sword.scale = (0.017, 0.011, 0.28)
    bpy.ops.object.transform_apply(scale=True)
    sword.parent = arm
    sword.parent_type = "BONE"
    sword.parent_bone = "mixamorig:RightHand"
    sword.location = (0.0, 0.28, 0.0)
    sword.rotation_euler = (0, 0, 0)
    return sword


def render(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    ART.mkdir(parents=True, exist_ok=True)
    (ART / path.name).write_bytes(path.read_bytes())
    print("wrote", path, path.stat().st_size)


def main():
    arm, mesh = import_walk()
    rest_u = bone_len(arm, "mixamorig:RightArm", "mixamorig:RightForeArm")
    rest_l = bone_len(arm, "mixamorig:RightForeArm", "mixamorig:RightHand")
    rest_th = bone_len(arm, "mixamorig:RightUpLeg", "mixamorig:RightLeg")
    planted = {pb.name: pb.matrix.copy() for pb in arm.pose.bones}
    make_sword(arm)
    OUT.mkdir(parents=True, exist_ok=True)
    report = []
    frames = [
        (0.08, "draw"),
        (0.30, "guard"),
        (0.52 * SECONDS, "mid_strike"),
        (1.20, "recover"),
    ]
    for t, label in frames:
        for pb in arm.pose.bones:
            pb.matrix = planted[pb.name]
        bpy.context.view_layer.update()
        phase, hx, hy, hz, lean, drawn = evaluate(t)
        hips_pos = bone_world(arm, "mixamorig:Hips")
        # Same as actor: world character space, not hips-bone axes.
        # Unity (x,y,z) = right, up, forward+Z → Blender (x, z, y).
        target = Vector((hips_pos.x + hx, hips_pos.y + hz, hips_pos.z + hy))
        aim_chain(arm, target)
        lu = bone_len(arm, "mixamorig:RightArm", "mixamorig:RightForeArm")
        ll = bone_len(arm, "mixamorig:RightForeArm", "mixamorig:RightHand")
        lt = bone_len(arm, "mixamorig:RightUpLeg", "mixamorig:RightLeg")
        hand = bone_world(arm, "mixamorig:RightHand")
        stretch = max(abs(lu - rest_u), abs(ll - rest_l), abs(lt - rest_th))
        row = {
            "label": label,
            "phase": phase,
            "hand_blender_xyz": [round(hand.x, 3), round(hand.y, 3), round(hand.z, 3)],
            "target": [round(target.x, 3), round(target.y, 3), round(target.z, 3)],
            "stretch_m": round(stretch, 5),
            "drawn": drawn,
        }
        report.append(row)
        print("QA", row)
        if stretch > 0.01:
            raise SystemExit(f"STRETCH FAIL {stretch}")
        render(OUT / f"aimchain_{label}.png")

    (OUT / "QA.txt").write_text(
        "NOT Unity Game-view. AimChain QA on walk Mixamo FBX.\n" + "\n".join(str(r) for r in report) + "\n",
        encoding="utf-8",
    )
    print("aimchain QA ok", report)


if __name__ == "__main__":
    main()
