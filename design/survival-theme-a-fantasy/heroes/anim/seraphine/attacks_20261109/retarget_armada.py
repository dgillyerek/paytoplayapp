"""Put Mixamo's Armada (capoeira spin kick) on SERAPHINE_rig.

Source is a without-skin FBX for Unity download. Seraphine's shins and feet
use UpLeg.001 and UpLeg.002 instead of Leg and Foot. Rotation is the source's
change from its rest pose, applied to Seraphine's rest. Toes are not driven.
"""
import bpy
import os
from mathutils import Matrix, Vector

SRC_BLEND = r"C:\Development\paytoplayapp\design\survival-theme-a-fantasy\heroes\anim\seraphine\blender\SERAPHINE.blend"
SRC_FBX = r"C:\Development\paytoplayapp\design\survival-theme-a-fantasy\heroes\anim\seraphine\attacks_20261109\mixamo_armada.fbx"
OUT_DIR = os.path.dirname(os.path.abspath(__file__))
UNITY_DIR = r"C:\Development\paytoplayapp\Assets\ThemePack\fantasy_kingdom_a\art\preview_new_chars\seraphine"
SHEET = r"C:\Users\derek\AppData\Local\Temp\seraphine_armada"
ACTION = "attack_armada"

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
    "mixamorig:RightHand": "mixamorig:RightHand",
    "mixamorig:LeftUpLeg": "mixamorig:LeftUpLeg",
    "mixamorig:LeftLeg": "mixamorig:LeftUpLeg.001",
    "mixamorig:LeftFoot": "mixamorig:LeftUpLeg.002",
    "mixamorig:RightUpLeg": "mixamorig:RightUpLeg",
    "mixamorig:RightLeg": "mixamorig:RightUpLeg.001",
    "mixamorig:RightFoot": "mixamorig:RightUpLeg.002",
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
    return out


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
    bpy.ops.wm.open_mainfile(filepath=SRC_BLEND)
    arm = bpy.data.objects["SERAPHINE_rig"]
    names = ordered_targets(arm)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=SRC_FBX)
    src = next(o for o in bpy.data.objects if o not in before and o.type == "ARMATURE")
    missing = [n for n in MAP if n not in src.data.bones]
    if missing:
        raise SystemExit("MISSING source bones: " + ", ".join(missing))
    scene = bpy.context.scene
    action = src.animation_data.action
    f0, f1 = [int(round(v)) for v in action.frame_range]
    print("SOURCE", src.name, action.name, f0, f1)

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
        raw.append((rotations, hip))

    hx = [h.x for _, h in raw]
    hy = [h.y for _, h in raw]
    frames = []
    for index, (rotations, hip) in enumerate(raw):
        lifted = (hip.z - src_hip.z) * scale
        span = max(1, len(raw) - 1)
        drift_x = (hx[-1] - hx[0]) * index / span
        drift_y = (hy[-1] - hy[0]) * index / span
        sway_x = ((hip.x - hx[0]) - drift_x) * scale
        sway_y = ((hip.y - hy[0]) - drift_y) * scale
        head = Vector((tgt_hip.x + sway_x, tgt_hip.y + sway_y, tgt_hip.z + lifted))
        frames.append((rotations, tgt_world_inv @ head))
    print("FRAMES", len(frames))

    for obj in [o for o in bpy.data.objects if o not in before]:
        bpy.data.objects.remove(obj, do_unlink=True)
    for old in list(bpy.data.actions):
        bpy.data.actions.remove(old)

    assign_action(arm, ACTION)
    for frame, (rotations, hip_head) in enumerate(frames, start=1):
        bases = basis_from(arm, names, rotations, hip_head)
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

    for name in ("SERAPHINE_spear__attack_throw", "SERAPHINE_spear__attack_thrust", "SERAPHINE_cape", "SERAPHINE_cape_rig"):
        obj = bpy.data.objects.get(name)
        if obj:
            obj.hide_render = True
            obj.hide_set(True)

    body = bpy.data.objects["SERAPHINE_body"]
    report = []
    samples = sorted(set([1, max(1, len(frames) // 4), len(frames) // 2, (3 * len(frames)) // 4, len(frames)]))
    for frame in samples:
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        deps = bpy.context.evaluated_depsgraph_get()
        ground = min((body.matrix_world @ v.co).z for v in body.evaluated_get(deps).data.vertices)
        left = (arm.matrix_world @ arm.pose.bones["mixamorig:LeftUpLeg.002"].matrix).to_translation()
        right = (arm.matrix_world @ arm.pose.bones["mixamorig:RightUpLeg.002"].matrix).to_translation()
        line = "f%d low %.3f Lfoot(%.2f,%.2f,%.2f) Rfoot(%.2f,%.2f,%.2f)" % (
            frame, ground, left.x, left.y, left.z, right.x, right.y, right.z)
        print(line)
        report.append(line)

    cam_data = bpy.data.cameras.new("ArmadaCam")
    cam = bpy.data.objects.new("ArmadaCam", cam_data)
    scene.collection.objects.link(cam)
    target = bpy.data.objects.new("ArmadaTarget", None)
    scene.collection.objects.link(target)
    target.location = (0.0, 0.0, 1.05)
    con = cam.constraints.new("TRACK_TO")
    con.target = target
    con.track_axis = "TRACK_NEGATIVE_Z"
    con.up_axis = "UP_Y"
    cam.location = (0.0, -4.1, 1.15)
    scene.camera = cam
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 480
    scene.render.resolution_y = 640
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    for frame in samples:
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(SHEET, "f%02d.png" % frame)
        bpy.ops.render.render(write_still=True)

    meshes = [body, bpy.data.objects["SERAPHINE_spear"], bpy.data.objects["SERAPHINE_wing_A"], bpy.data.objects["SERAPHINE_wing_B"]]
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    for folder in (OUT_DIR, UNITY_DIR):
        path = os.path.join(folder, "SERAPHINE_attack_armada.fbx")
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
        print("EXPORTED", path)

    blend_path = os.path.join(OUT_DIR, "SERAPHINE_armada.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("SAVED", blend_path)
    with open(os.path.join(OUT_DIR, "armada_report.txt"), "w", encoding="utf-8") as handle:
        handle.write("\n".join(report))


if __name__ == "__main__":
    main()
