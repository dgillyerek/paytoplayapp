"""Put Mixamo's Great Sword Casting on MALZARETH_rig.

The sword mesh is skinned to RightForeArm.001 and its blade runs along that
bone's local X. Mixamo holds the sword in both hands, with the handle along
the right hand's local Z. That aim is copied onto the sword, and the left
arm is solved so the left hand stays on the grip. No bones are added.
"""
import bpy
import os
import shutil
from mathutils import Matrix, Vector

SRC_BLEND = r"C:\Development\paytoplayapp\design\survival-theme-a-fantasy\enemies\anim\malzareth\blender\MALZARETH.blend"
SRC_FBX = r"C:\Users\derek\Downloads\MALZARETH_rigready@Great Sword Casting.fbx"
OUT_DIR = os.path.dirname(os.path.abspath(__file__))
UNITY_DIR = r"C:\Development\paytoplayapp\Assets\ThemePack\fantasy_kingdom_a\art\preview_new_chars\malzareth"
SHEET = r"C:\Users\derek\AppData\Local\Temp\malzareth_gsc"
ACTION = "attack_great_sword_casting"

MAP = {
    "mixamorig:Hips": "mixamorig:Hips",
    "mixamorig:Spine": "mixamorig:Spine",
    "mixamorig:Spine1": "mixamorig:Spine1",
    "mixamorig:Spine2": "mixamorig:Spine2",
    "mixamorig:Neck": "mixamorig:Neck",
    "mixamorig:Head": "mixamorig:Head",
    "mixamorig:LeftShoulder": "mixamorig:LeftShoulder",
    "mixamorig:LeftArm": "mixamorig:LeftArm",
    "mixamorig:LeftForeArm": "mixamorig:LeftForeArm",
    "mixamorig:LeftHand": "mixamorig:LeftHand",
    "mixamorig:RightShoulder": "mixamorig:RightShoulder",
    "mixamorig:RightArm": "mixamorig:RightArm",
    "mixamorig:RightForeArm": "mixamorig:RightForeArm",
    "mixamorig:RightHand": "mixamorig:RightForeArm.001",
    "mixamorig:LeftUpLeg": "mixamorig:LeftUpLeg",
    "mixamorig:LeftLeg": "mixamorig:LeftLeg",
    "mixamorig:LeftFoot": "mixamorig:LeftFoot",
    "mixamorig:RightUpLeg": "mixamorig:RightUpLeg",
    "mixamorig:RightLeg": "mixamorig:RightLeg",
    "mixamorig:RightFoot": "mixamorig:RightFoot",
}
HIPS = "mixamorig:Hips"


def ordered_targets(arm):
    wanted = set(MAP.values())
    return [b.name for b in arm.data.bones if b.name in wanted]


def basis_from(arm, names, rotations, hips_head):
    bones = arm.data.bones
    built = {}
    out = {}
    for name in names:
        bone = bones[name]
        rest = bone.matrix_local
        if bone.parent and bone.parent.name in built:
            parent = built[bone.parent.name] @ (bone.parent.matrix_local.inverted() @ rest)
            head = parent.translation
        else:
            parent = rest
            head = hips_head
        posed = Matrix.LocRotScale(head, rotations[name], None)
        built[name] = posed
        basis = parent.inverted() @ posed
        if bone.parent:
            basis = Matrix.LocRotScale(Vector((0.0, 0.0, 0.0)), basis.to_quaternion(), None)
        out[name] = basis
    return out, built


def posed_chain(arm, names, rotations, hips_head):
    return basis_from(arm, names, rotations, hips_head)


def quat_from_axes(x, y, z):
    return Matrix((
        (x.x, y.x, z.x),
        (x.y, y.y, z.y),
        (x.z, y.z, z.z),
    )).to_quaternion()


def aim_x(direction, twist_hint):
    x = direction.normalized()
    y = twist_hint - x * x.dot(twist_hint)
    if y.length < 1e-5:
        y = Vector((0.0, 0.0, 1.0))
        y = y - x * x.dot(y)
    if y.length < 1e-5:
        y = Vector((0.0, 1.0, 0.0))
        y = y - x * x.dot(y)
    y.normalize()
    z = x.cross(y).normalized()
    y = z.cross(x).normalized()
    return quat_from_axes(x, y, z)


def aim_y(direction, twist_hint):
    y = direction.normalized()
    x = twist_hint - y * y.dot(twist_hint)
    if x.length < 1e-5:
        x = Vector((1.0, 0.0, 0.0))
        x = x - y * y.dot(x)
    if x.length < 1e-5:
        x = Vector((0.0, 0.0, 1.0))
        x = x - y * y.dot(x)
    x.normalize()
    z = x.cross(y).normalized()
    x = y.cross(z).normalized()
    return quat_from_axes(x, y, z)


def bone_length(arm, name):
    bone = arm.data.bones[name]
    return (bone.tail_local - bone.head_local).length


def solve_elbow(root, target, upper_len, fore_len, pole):
    to_t = target - root
    dist = to_t.length
    if dist < 1e-6:
        to_t = Vector((0.0, -1.0, 0.0))
        dist = 1e-6
    reach = upper_len + fore_len - 1e-4
    minimum = abs(upper_len - fore_len) + 1e-4
    dist = min(max(dist, minimum), reach)
    to_n = to_t.normalized()
    cos_a = (upper_len * upper_len + dist * dist - fore_len * fore_len) / (2.0 * upper_len * dist)
    cos_a = max(-1.0, min(1.0, cos_a))
    along = cos_a * upper_len
    height = (max(0.0, upper_len * upper_len - along * along)) ** 0.5
    pole_v = pole - root
    pole_p = pole_v - to_n * pole_v.dot(to_n)
    if pole_p.length < 1e-5:
        pole_p = Vector((0.0, 0.0, 1.0))
        pole_p = pole_p - to_n * pole_p.dot(to_n)
    pole_p.normalize()
    return root + to_n * along + pole_p * height


def assign_action(arm, name):
    if arm.animation_data is None:
        arm.animation_data_create()
    action = bpy.data.actions.new(name)
    arm.animation_data.action = action
    if hasattr(action, "slots"):
        slot = action.slots.new("OBJECT", arm.name) if len(action.slots) == 0 else action.slots[0]
        if hasattr(arm.animation_data, "action_slot"):
            arm.animation_data.action_slot = slot
    return action


def main():
    os.makedirs(SHEET, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    kept = os.path.join(OUT_DIR, "MALZARETH_rigready@Great Sword Casting.fbx")
    if os.path.abspath(SRC_FBX) != os.path.abspath(kept):
        shutil.copy2(SRC_FBX, kept)
    bpy.ops.wm.open_mainfile(filepath=SRC_BLEND)
    arm = bpy.data.objects["MALZARETH_rig"]
    names = ordered_targets(arm)
    missing = [dst for dst in MAP.values() if dst not in arm.data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ", ".join(missing))

    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=SRC_FBX)
    src = next(o for o in bpy.data.objects if o not in before and o.type == "ARMATURE")
    scene = bpy.context.scene
    action = src.animation_data.action
    f0, f1 = [int(round(v)) for v in action.frame_range]
    print("SOURCE", action.name, f0, f1, "bytes", os.path.getsize(SRC_FBX))

    src_world_q = src.matrix_world.to_quaternion()
    tgt_world_q = arm.matrix_world.to_quaternion()
    tgt_world_inv = arm.matrix_world.inverted()
    src_rest = {n: src_world_q @ src.data.bones[n].matrix_local.to_quaternion() for n in MAP}
    tgt_rest = {MAP[n]: tgt_world_q @ arm.data.bones[MAP[n]].matrix_local.to_quaternion() for n in MAP}
    src_hip = src.matrix_world @ src.data.bones[HIPS].head_local
    tgt_hip = arm.matrix_world @ arm.data.bones[HIPS].head_local
    scale = tgt_hip.z / src_hip.z if abs(src_hip.z) > 1e-6 else 1.0
    print("HIP scale", round(scale, 4))

    raw = []
    for frame in range(f0, f1 + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        rotations = {}
        for src_name, dst_name in MAP.items():
            posed = src_world_q @ src.pose.bones[src_name].matrix.to_quaternion()
            world = posed @ src_rest[src_name].inverted() @ tgt_rest[dst_name]
            rotations[dst_name] = tgt_world_q.inverted() @ world
        hip = src.matrix_world @ src.pose.bones[HIPS].head
        hand_q = src_world_q @ src.pose.bones["mixamorig:RightHand"].matrix.to_quaternion()
        blade = (tgt_world_q.inverted() @ (hand_q @ Vector((0.0, 0.0, 1.0)))).normalized()
        twist = tgt_world_q.inverted() @ (hand_q @ Vector((0.0, 1.0, 0.0)))
        rh = src.matrix_world @ src.pose.bones["mixamorig:RightHand"].head
        lh = src.matrix_world @ src.pose.bones["mixamorig:LeftHand"].head
        gap = (lh - rh).length * scale
        raw.append((rotations, hip, blade, twist, gap))

    hx = [h.x for _, h, _b, _t, _g in raw]
    hy = [h.y for _, h, _b, _t, _g in raw]
    frames = []
    for index, (rotations, hip, blade, twist, gap) in enumerate(raw):
        lifted = (hip.z - src_hip.z) * scale
        span = max(1, len(raw) - 1)
        drift_x = (hx[-1] - hx[0]) * index / span
        drift_y = (hy[-1] - hy[0]) * index / span
        sway_x = ((hip.x - hx[0]) - drift_x) * scale
        sway_y = ((hip.y - hy[0]) - drift_y) * scale
        head = Vector((tgt_hip.x + sway_x, tgt_hip.y + sway_y, tgt_hip.z + lifted))
        frames.append((rotations, tgt_world_inv @ head, blade, twist, gap))
    print("FRAMES", len(frames))

    for obj in [o for o in bpy.data.objects if o not in before]:
        bpy.data.objects.remove(obj, do_unlink=True)
    for old in list(bpy.data.actions):
        bpy.data.actions.remove(old)

    assign_action(arm, ACTION)
    upper_len = bone_length(arm, "mixamorig:LeftArm")
    fore_len = bone_length(arm, "mixamorig:LeftForeArm")
    fore_r = bone_length(arm, "mixamorig:RightForeArm")
    for frame, (rotations, hip_head, blade, twist, gap) in enumerate(frames, start=1):
        _bases, built = posed_chain(arm, names, rotations, hip_head)
        wrist = built["mixamorig:RightForeArm"].to_translation()
        wrist = wrist + (built["mixamorig:RightForeArm"].to_quaternion() @ Vector((0.0, fore_r, 0.0)))
        shoulder = built["mixamorig:LeftArm"].to_translation()
        elbow_now = built["mixamorig:LeftForeArm"].to_translation()
        grip = wrist + blade * gap
        elbow = solve_elbow(shoulder, grip, upper_len, fore_len, elbow_now)
        rotations["mixamorig:LeftArm"] = aim_y(elbow - shoulder, rotations["mixamorig:LeftArm"] @ Vector((1.0, 0.0, 0.0)))
        rotations["mixamorig:LeftForeArm"] = aim_y(grip - elbow, rotations["mixamorig:LeftForeArm"] @ Vector((1.0, 0.0, 0.0)))
        rotations["mixamorig:RightForeArm.001"] = aim_x(blade, twist)
        bases, _built = basis_from(arm, names, rotations, hip_head)
        for name in names:
            pose = arm.pose.bones[name]
            pose.rotation_mode = "QUATERNION"
            pose.matrix_basis = bases[name]
        bpy.context.view_layer.update()
        for name in names:
            pose = arm.pose.bones[name]
            pose.keyframe_insert(data_path="rotation_quaternion", frame=frame)
            if name == HIPS:
                pose.keyframe_insert(data_path="location", frame=frame)

    scene.frame_start = 1
    scene.frame_end = len(frames)
    scene.render.fps = 30

    for obj in bpy.data.objects:
        if obj.type == "MESH" and "__" in obj.name:
            obj.hide_render = True
            obj.hide_set(True)
        if obj.name in ("MALZARETH_cape", "MALZARETH_cape_rig"):
            obj.hide_render = True
            obj.hide_set(True)

    sword = bpy.data.objects["MALZARETH_greatsword"]
    body = bpy.data.objects["MALZARETH_body"]
    marks = sorted(set([1, len(frames) // 4, len(frames) // 2, (3 * len(frames)) // 4, len(frames)]))
    for frame in marks:
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        deps = bpy.context.evaluated_depsgraph_get()
        ev = sword.evaluated_get(deps)
        mw = ev.matrix_world
        hand = (arm.matrix_world @ arm.pose.bones["mixamorig:RightForeArm.001"].matrix).to_translation()
        left = (arm.matrix_world @ arm.pose.bones["mixamorig:LeftHand"].head)
        tip = max((mw @ v.co for v in ev.data.vertices), key=lambda v: (v - hand).length)
        hilt = min((mw @ v.co for v in ev.data.vertices), key=lambda v: (v - hand).length)
        body_ev = body.evaluated_get(deps)
        ground = min((body_ev.matrix_world @ v.co).z for v in body_ev.data.vertices)
        print("f%d tip(%.2f,%.2f,%.2f) hilt-lh %.2f hilt-rh %.2f low %.3f" % (
            frame, tip.x, tip.y, tip.z, (hilt - left).length, (hilt - hand).length, ground))

    cam_data = bpy.data.cameras.new("CastCam")
    cam = bpy.data.objects.new("CastCam", cam_data)
    scene.collection.objects.link(cam)
    target = bpy.data.objects.new("CastTarget", None)
    scene.collection.objects.link(target)
    target.location = (0.0, 0.0, 1.15)
    con = cam.constraints.new("TRACK_TO")
    con.target = target
    con.track_axis = "TRACK_NEGATIVE_Z"
    con.up_axis = "UP_Y"
    cam.location = (0.0, -4.6, 1.3)
    scene.camera = cam
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 480
    scene.render.resolution_y = 640
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    for frame in marks:
        if frame == len(frames):
            continue
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(SHEET, "f%03d.png" % frame)
        bpy.ops.render.render(write_still=True)

    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    body.select_set(True)
    sword.select_set(True)
    bpy.context.view_layer.objects.active = arm
    for folder in (OUT_DIR, UNITY_DIR):
        path = os.path.join(folder, "MALZARETH_attack_great_sword_casting.fbx")
        bpy.ops.export_scene.fbx(
            filepath=path,
            use_selection=True,
            object_types={"ARMATURE", "MESH"},
            global_scale=1.0,
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_NONE",
            axis_forward="-Z",
            axis_up="Y",
            primary_bone_axis="Y",
            secondary_bone_axis="X",
            add_leaf_bones=False,
            bake_anim=True,
            bake_anim_use_all_actions=False,
            bake_anim_use_nla_strips=False,
            bake_anim_force_startend_keying=True,
            bake_anim_simplify_factor=0.0,
            bake_anim_step=1.0,
            bake_anim_use_all_bones=True,
            mesh_smooth_type="FACE",
            path_mode="AUTO",
        )
        print("EXPORTED", path, os.path.getsize(path))

    blend_path = os.path.join(OUT_DIR, "MALZARETH_great_sword_casting.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("SAVED", blend_path)


if __name__ == "__main__":
    main()
