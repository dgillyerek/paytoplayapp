"""Fresh hand-authored Seraphine attacks.  Does not modify the source blend."""
import bpy
import math
import os
from mathutils import Matrix, Vector

REPO = r"C:\Development\paytoplayapp"
OUT = os.path.join(REPO, "design/survival-theme-a-fantasy/heroes/anim/seraphine/blender/manual_v1")
P = "mixamorig:"
arm = bpy.data.objects["SERAPHINE_rig"]
ad = arm.animation_data_create()
idle = bpy.data.actions["idle"]
ad.action = idle
ad.action_slot = idle.slots[0]
bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
idle_basis = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}

def new_action(name):
    old = bpy.data.actions.get(name)
    if old:
        bpy.data.actions.remove(old)
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    slot = act.slots.new("OBJECT", arm.name)
    layer = act.layers.new("hand keyed")
    layer.strips.new(type="KEYFRAME")
    ad.action = act
    ad.action_slot = slot
    return act

def reset_pose():
    for pb in arm.pose.bones:
        pb.matrix_basis = idle_basis[pb.name].copy()

def local_turn(bone, axis, degrees):
    pb = arm.pose.bones[P + bone]
    pb.matrix_basis = pb.matrix_basis @ Matrix.Rotation(math.radians(degrees), 4, axis)

def aim_staff(direction):
    """Aim the hand's staff axis after posing the shoulder and elbow."""
    hand = arm.pose.bones[P + "RightHand"]
    world = arm.matrix_world @ hand.matrix
    current = (world.to_3x3() @ Vector((1, 0, 0))).normalized()
    rotation = current.rotation_difference(Vector(direction).normalized())
    hand.matrix = arm.matrix_world.inverted() @ rotation.to_matrix().to_4x4() @ world

def key(frame):
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        pb.keyframe_insert("location", frame=frame)

def pose(frame, turns, staff=None):
    reset_pose()
    for bone, axis, degrees in turns:
        local_turn(bone, axis, degrees)
    bpy.context.view_layer.update()
    if staff is not None:
        aim_staff(staff)
    bpy.context.view_layer.update()
    key(frame)

def smooth_action(action):
    bag = action.layers[0].strips[0].channelbag(action.slots[0])
    for curve in bag.fcurves:
        for point in curve.keyframe_points:
            point.interpolation = "BEZIER"

# The body turns first, then the arm and staff arrive a few frames later.
# The end poses are the unmodified idle pose so NewCharsPreview crossfades cleanly.
THRUST = {
    1: ([], None),
    9: ([("Hips", "Z", -5), ("Spine", "Z", -8), ("Spine1", "Z", -10),
         ("Spine2", "Z", -8), ("RightShoulder", "Z", -12), ("RightArm", "Y", -20),
         ("RightForeArm", "Y", -18), ("LeftShoulder", "Z", 8), ("LeftArm", "Y", 14)], Vector((0.20, -0.72, 0.66))),
    18: ([("Hips", "Z", 12), ("Spine", "Z", 20), ("Spine1", "Z", 18),
          ("Spine2", "Z", 12), ("RightShoulder", "Z", 18), ("RightArm", "Y", 34),
          ("RightForeArm", "Y", 24), ("LeftShoulder", "Z", -14), ("LeftArm", "Y", -24),
          ("LeftForeArm", "Y", -20), ("Neck", "Z", 8), ("Head", "Z", 6)], Vector((0.02, -0.98, 0.16))),
    24: ([("Hips", "Z", 7), ("Spine", "Z", 10), ("Spine1", "Z", 8),
          ("Spine2", "Z", 6), ("RightShoulder", "Z", 8), ("RightArm", "Y", 18),
          ("RightForeArm", "Y", 14), ("LeftShoulder", "Z", -7), ("LeftArm", "Y", -12),
          ("LeftForeArm", "Y", -10), ("Neck", "Z", 4), ("Head", "Z", 3)], Vector((0.04, -0.94, 0.30))),
    40: ([], None), 60: ([], None),
}

CAST = {
    1: ([], None),
    10: ([("Hips", "Z", -8), ("Spine", "Z", -14), ("Spine1", "Z", -14),
          ("Spine2", "Z", -10), ("RightShoulder", "Z", -18), ("RightArm", "Y", -28),
          ("RightForeArm", "Y", -20), ("LeftShoulder", "Z", 14), ("LeftArm", "Y", 22),
          ("LeftForeArm", "Y", 16), ("Neck", "Z", -5), ("Head", "Z", -5)], Vector((-0.40, 0.28, 0.87))),
    20: ([("Hips", "Z", 15), ("Spine", "Z", 25), ("Spine1", "Z", 22),
          ("Spine2", "Z", 16), ("RightShoulder", "Z", 22), ("RightArm", "Y", 30),
          ("RightForeArm", "Y", 22), ("LeftShoulder", "Z", -18), ("LeftArm", "Y", -28),
          ("LeftForeArm", "Y", -20), ("Neck", "Z", 10), ("Head", "Z", 8)], Vector((0.08, -0.88, 0.46))),
    28: ([("Hips", "Z", 9), ("Spine", "Z", 12), ("Spine1", "Z", 10),
          ("Spine2", "Z", 8), ("RightShoulder", "Z", 12), ("RightArm", "Y", 18),
          ("RightForeArm", "Y", 14), ("LeftShoulder", "Z", -10), ("LeftArm", "Y", -16),
          ("LeftForeArm", "Y", -12), ("Neck", "Z", 5), ("Head", "Z", 4)], Vector((0.10, -0.96, 0.22))),
    45: ([], None), 60: ([], None),
}

def build(name, poses):
    action = new_action(name)
    for frame, (turns, staff) in poses.items():
        pose(frame, turns, staff)
    smooth_action(action)
    return action

thrust = build("attack_thrust", THRUST)
cast = build("attack_throw", CAST)

def export(action, filename):
    ad.action = action
    ad.action_slot = action.slots[0]
    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1, 60
    for obj in bpy.data.objects: obj.select_set(False)
    for name in ("SERAPHINE_rig", "SERAPHINE_body", "SERAPHINE_spear", "SERAPHINE_wing_A", "SERAPHINE_wing_B"):
        bpy.data.objects[name].select_set(True)
    bpy.context.view_layer.objects.active = arm
    for dest in (os.path.join(REPO, "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine"),
                 os.path.join(REPO, "design/survival-theme-a-fantasy/heroes/anim/seraphine/final")):
        bpy.ops.export_scene.fbx(filepath=os.path.join(dest, filename), use_selection=True,
            object_types={"ARMATURE", "MESH"}, global_scale=1.0, apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_NONE", axis_forward="-Z", axis_up="Y",
            primary_bone_axis="Y", secondary_bone_axis="X", add_leaf_bones=False,
            bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
            bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
            bake_anim_step=1.0, mesh_smooth_type="FACE", use_armature_deform_only=False, path_mode="AUTO")

export(thrust, "SERAPHINE_attack_thrust.fbx")
export(cast, "SERAPHINE_attack_throw.fbx")
ad.action = idle
ad.action_slot = idle.slots[0]
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "SERAPHINE_manual_v1.blend"), copy=False)
