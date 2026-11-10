"""Two new Seraphine spear attacks on SERAPHINE_rig only.

Crescent: a horizontal cut that starts cocked on her right and breaks across the front.
Rise: a low chamber by the right hip that rips up through the space in front of her.

The old throw and thrust actions are not read and are not copied.
"""
import bpy
import math
import os
from mathutils import Euler

SRC = r"C:\Development\paytoplayapp\design\survival-theme-a-fantasy\heroes\anim\seraphine\blender\SERAPHINE.blend"
OUT_DIR = os.path.dirname(os.path.abspath(__file__))
UNITY_DIR = r"C:\Development\paytoplayapp\Assets\ThemePack\fantasy_kingdom_a\art\preview_new_chars\seraphine"
SHEET = r"C:\Users\derek\AppData\Local\Temp\seraphine_keys"
FPS = 30
FRAME_START = 1
FRAME_END = 48

# Local XYZ degrees from the rest pose, plus hips location in bone space
# (hips Y is up, hips Z is forward).
READY = {
    "loc": (0.0, -0.05, 0.02),
    "mixamorig:Hips": (4, -8, 0),
    "mixamorig:Spine": (2, -6, 0),
    "mixamorig:Spine1": (2, -4, 0),
    "mixamorig:Spine2": (-6, -8, 0),
    "mixamorig:Neck": (-4, 4, 0),
    "mixamorig:Head": (-6, 8, 0),
    "mixamorig:RightShoulder": (0, 0, -10),
    "mixamorig:RightArm": (50, -38, -22),
    "mixamorig:RightForeArm": (32, 0, 10),
    "mixamorig:RightHand": (0, -58, -6),
    "mixamorig:LeftShoulder": (0, 0, 8),
    "mixamorig:LeftArm": (46, 8, -16),
    "mixamorig:LeftForeArm": (38, 0, 4),
    "mixamorig:LeftHand": (0, 0, 10),
    "mixamorig:LeftUpLeg": (26, 0, 4),
    "mixamorig:LeftUpLeg.001": (-42, 0, 0),
    "mixamorig:LeftUpLeg.002": (14, 0, 0),
    "mixamorig:LeftUpLeg.003": (8, 0, 0),
    "mixamorig:RightUpLeg": (-8, 0, -4),
    "mixamorig:RightUpLeg.001": (16, 0, 0),
    "mixamorig:RightUpLeg.002": (-6, 0, 0),
    "mixamorig:RightUpLeg.003": (-4, 0, 0),
}

COCKED = {
    "loc": (0.0, -0.08, -0.03),
    "mixamorig:Hips": (2, -24, -4),
    "mixamorig:Spine": (0, -12, -2),
    "mixamorig:Spine1": (-2, -14, -4),
    "mixamorig:Spine2": (-4, -18, -6),
    "mixamorig:Neck": (4, 10, 2),
    "mixamorig:Head": (6, 16, 4),
    "mixamorig:RightShoulder": (0, 0, -16),
    "mixamorig:RightArm": (-8, 28, -52),
    "mixamorig:RightForeArm": (22, 0, -14),
    "mixamorig:RightHand": (0, 42, 16),
    "mixamorig:LeftShoulder": (0, 0, 6),
    "mixamorig:LeftArm": (12, 0, 18),
    "mixamorig:LeftForeArm": (16, 0, 0),
    "mixamorig:LeftHand": (0, 0, 0),
    "mixamorig:LeftUpLeg": (6, 0, 3),
    "mixamorig:LeftUpLeg.001": (-10, 0, 0),
    "mixamorig:LeftUpLeg.002": (4, 0, 0),
    "mixamorig:LeftUpLeg.003": (2, 0, 0),
    "mixamorig:RightUpLeg": (10, 0, -8),
    "mixamorig:RightUpLeg.001": (-16, 0, 0),
    "mixamorig:RightUpLeg.002": (6, 0, 0),
    "mixamorig:RightUpLeg.003": (4, 0, 0),
}

CRESCENT_HIT = {
    "loc": (0.02, -0.04, 0.07),
    "mixamorig:Hips": (-2, 26, 4),
    "mixamorig:Spine": (-4, 8, 2),
    "mixamorig:Spine1": (-4, 10, 3),
    "mixamorig:Spine2": (-6, 12, 4),
    "mixamorig:Neck": (-6, -8, -3),
    "mixamorig:Head": (-10, -14, -4),
    "mixamorig:RightShoulder": (0, 0, 8),
    "mixamorig:RightArm": (64, -16, 34),
    "mixamorig:RightForeArm": (18, 0, 16),
    "mixamorig:RightHand": (0, -18, -12),
    "mixamorig:LeftShoulder": (0, 0, 14),
    "mixamorig:LeftArm": (-18, 0, 36),
    "mixamorig:LeftForeArm": (8, 0, 0),
    "mixamorig:LeftHand": (0, 0, -8),
    "mixamorig:LeftUpLeg": (30, 0, 4),
    "mixamorig:LeftUpLeg.001": (-46, 0, 0),
    "mixamorig:LeftUpLeg.002": (16, 0, 0),
    "mixamorig:LeftUpLeg.003": (10, 0, 0),
    "mixamorig:RightUpLeg": (-8, 0, -4),
    "mixamorig:RightUpLeg.001": (14, 0, 0),
    "mixamorig:RightUpLeg.002": (-8, 0, 0),
    "mixamorig:RightUpLeg.003": (-4, 0, 0),
}

CRESCENT_FOLLOW = {
    "loc": (0.02, -0.045, 0.05),
    "mixamorig:Hips": (0, 18, 3),
    "mixamorig:Spine": (-2, 6, 2),
    "mixamorig:Spine1": (-2, 8, 2),
    "mixamorig:Spine2": (-4, 10, 3),
    "mixamorig:Neck": (-2, -4, 0),
    "mixamorig:Head": (-4, -6, -2),
    "mixamorig:RightShoulder": (0, 0, 10),
    "mixamorig:RightArm": (28, -8, 46),
    "mixamorig:RightForeArm": (14, 0, 8),
    "mixamorig:RightHand": (0, 8, -16),
    "mixamorig:LeftShoulder": (0, 0, 10),
    "mixamorig:LeftArm": (6, 0, 28),
    "mixamorig:LeftForeArm": (10, 0, 0),
    "mixamorig:LeftHand": (0, 0, 0),
    "mixamorig:LeftUpLeg": (22, 0, 4),
    "mixamorig:LeftUpLeg.001": (-34, 0, 0),
    "mixamorig:LeftUpLeg.002": (10, 0, 0),
    "mixamorig:LeftUpLeg.003": (6, 0, 0),
    "mixamorig:RightUpLeg": (-6, 0, -3),
    "mixamorig:RightUpLeg.001": (12, 0, 0),
    "mixamorig:RightUpLeg.002": (-4, 0, 0),
    "mixamorig:RightUpLeg.003": (-2, 0, 0),
}

CHAMBER = {
    "loc": (0.0, -0.16, 0.04),
    "mixamorig:Hips": (6, -18, -4),
    "mixamorig:Spine": (-12, -8, 0),
    "mixamorig:Spine1": (-10, -6, -2),
    "mixamorig:Spine2": (-12, -6, -4),
    "mixamorig:Neck": (8, 6, 0),
    "mixamorig:Head": (14, 10, 2),
    "mixamorig:RightShoulder": (0, 0, -8),
    "mixamorig:RightArm": (24, -8, -40),
    "mixamorig:RightForeArm": (46, 0, 8),
    "mixamorig:RightHand": (0, -24, 32),
    "mixamorig:LeftShoulder": (0, 0, 4),
    "mixamorig:LeftArm": (28, 0, -6),
    "mixamorig:LeftForeArm": (34, 0, 0),
    "mixamorig:LeftHand": (0, 0, 8),
    "mixamorig:LeftUpLeg": (52, 0, 6),
    "mixamorig:LeftUpLeg.001": (-82, 0, 0),
    "mixamorig:LeftUpLeg.002": (18, 0, 0),
    "mixamorig:LeftUpLeg.003": (12, 0, 0),
    "mixamorig:RightUpLeg": (46, 0, -8),
    "mixamorig:RightUpLeg.001": (-74, 0, 0),
    "mixamorig:RightUpLeg.002": (14, 0, 0),
    "mixamorig:RightUpLeg.003": (8, 0, 0),
}

RISE_HIT = {
    "loc": (0.0, -0.02, 0.07),
    "mixamorig:Hips": (-8, 14, 4),
    "mixamorig:Spine": (10, 6, 0),
    "mixamorig:Spine1": (12, 8, 2),
    "mixamorig:Spine2": (16, 12, 3),
    "mixamorig:Neck": (-8, -4, 0),
    "mixamorig:Head": (-16, -8, -2),
    "mixamorig:RightShoulder": (0, 0, -6),
    "mixamorig:RightArm": (72, -46, -8),
    "mixamorig:RightForeArm": (10, 0, 4),
    "mixamorig:RightHand": (0, -72, -22),
    "mixamorig:LeftShoulder": (0, 0, -6),
    "mixamorig:LeftArm": (58, 0, -28),
    "mixamorig:LeftForeArm": (18, 0, 0),
    "mixamorig:LeftHand": (10, 0, 12),
    "mixamorig:LeftUpLeg": (14, 0, 3),
    "mixamorig:LeftUpLeg.001": (-22, 0, 0),
    "mixamorig:LeftUpLeg.002": (8, 0, 0),
    "mixamorig:LeftUpLeg.003": (4, 0, 0),
    "mixamorig:RightUpLeg": (-4, 0, -3),
    "mixamorig:RightUpLeg.001": (8, 0, 0),
    "mixamorig:RightUpLeg.002": (-4, 0, 0),
    "mixamorig:RightUpLeg.003": (-2, 0, 0),
}

RISE_FOLLOW = {
    "loc": (0.02, -0.04, 0.04),
    "mixamorig:Hips": (-2, 22, 3),
    "mixamorig:Spine": (4, 8, 2),
    "mixamorig:Spine1": (4, 8, 2),
    "mixamorig:Spine2": (6, 10, 3),
    "mixamorig:Neck": (-4, -6, 0),
    "mixamorig:Head": (-8, -10, -2),
    "mixamorig:RightShoulder": (0, 0, 8),
    "mixamorig:RightArm": (36, -12, 40),
    "mixamorig:RightForeArm": (12, 0, 6),
    "mixamorig:RightHand": (0, -8, -14),
    "mixamorig:LeftShoulder": (0, 0, 12),
    "mixamorig:LeftArm": (8, 0, 24),
    "mixamorig:LeftForeArm": (8, 0, 0),
    "mixamorig:LeftHand": (0, 0, 0),
    "mixamorig:LeftUpLeg": (16, 0, 3),
    "mixamorig:LeftUpLeg.001": (-24, 0, 0),
    "mixamorig:LeftUpLeg.002": (8, 0, 0),
    "mixamorig:LeftUpLeg.003": (4, 0, 0),
    "mixamorig:RightUpLeg": (-4, 0, -2),
    "mixamorig:RightUpLeg.001": (8, 0, 0),
    "mixamorig:RightUpLeg.002": (-2, 0, 0),
    "mixamorig:RightUpLeg.003": (0, 0, 0),
}

WIDE = {k: v for k, v in COCKED.items()}
WIDE["loc"] = (0.0, -0.05, 0.0)
WIDE["mixamorig:Hips"] = (2, -14, -3)

ATTACKS = {
    "attack_crescent": [
        (1, READY, "smooth"),
        (8, WIDE, "smooth"),
        (14, COCKED, "in"),
        (24, CRESCENT_HIT, "out"),
        (33, CRESCENT_FOLLOW, "smooth"),
        (48, READY, "smooth"),
    ],
    "attack_rise": [
        (1, READY, "smooth"),
        (12, CHAMBER, "in"),
        (24, RISE_HIT, "out"),
        (34, RISE_FOLLOW, "smooth"),
        (48, READY, "smooth"),
    ],
}


def ease(t, kind):
    t = min(1.0, max(0.0, t))
    if kind == "in":
        return t ** 2.4
    if kind == "out":
        return 1.0 - (1.0 - t) ** 2.2
    return t * t * (3.0 - 2.0 * t)


def lerp(a, b, t):
    return a + (b - a) * t


def pose_at(keys, frame):
    for i in range(len(keys) - 1):
        f0, p0, _ = keys[i]
        f1, p1, kind = keys[i + 1]
        if frame <= f1 or i == len(keys) - 2:
            span = max(1, f1 - f0)
            t = ease((frame - f0) / span, kind)
            names = set(p0) | set(p1)
            out = {}
            for name in names:
                a = p0.get(name, (0, 0, 0))
                b = p1.get(name, (0, 0, 0))
                out[name] = tuple(lerp(a[k], b[k], t) for k in range(3))
            return out
    return keys[-1][1]


def clear_pose(arm):
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)


def apply_pose(arm, pose):
    clear_pose(arm)
    for name, e in pose.items():
        if name == "loc":
            arm.pose.bones["mixamorig:Hips"].location = e
            continue
        pb = arm.pose.bones[name]
        pb.rotation_euler = tuple(math.radians(v) for v in e)
    bpy.context.view_layer.update()


def spear_tip(arm, spear):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = spear.evaluated_get(deps)
    mw = ev.matrix_world
    hand = (arm.matrix_world @ arm.pose.bones["mixamorig:RightHand"].matrix).to_translation()
    verts = [mw @ v.co for v in ev.data.vertices]
    tip = max(verts, key=lambda v: (v - hand).length)
    return tip, hand


def lowest_z(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    mw = ev.matrix_world
    return min((mw @ v.co).z for v in ev.data.vertices)


def assign_action(arm, name):
    if arm.animation_data is None:
        arm.animation_data_create()
    action = bpy.data.actions.new(name)
    arm.animation_data.action = action
    slot = None
    if hasattr(action, "slots"):
        slot = action.slots.new("OBJECT", arm.name) if len(action.slots) == 0 else action.slots[0]
        if hasattr(arm.animation_data, "action_slot"):
            arm.animation_data.action_slot = slot
    return action


def key_pose(arm, pose, frame):
    apply_pose(arm, pose)
    for pb in arm.pose.bones:
        if pb.rotation_mode != "XYZ":
            pb.rotation_mode = "XYZ"
        pb.rotation_mode = "QUATERNION"
        pb.keyframe_insert(data_path="rotation_quaternion", frame=frame)
    hips = arm.pose.bones["mixamorig:Hips"]
    hips.keyframe_insert(data_path="location", frame=frame)


def build_action(arm, name, keys):
    action = assign_action(arm, name)
    for frame in range(FRAME_START, FRAME_END + 1):
        key_pose(arm, pose_at(keys, frame), frame)
    scene = bpy.context.scene
    scene.frame_start = FRAME_START
    scene.frame_end = FRAME_END
    if hasattr(action, "frame_range"):
        pass
    return action


def inside_body(point):
    return abs(point.x) < 0.28 and -0.16 < point.y < 0.22 and 0.75 < point.z < 1.72


def check_path(arm, spear, keys, label):
    hits = []
    for frame in range(FRAME_START, FRAME_END + 1, 2):
        apply_pose(arm, pose_at(keys, frame))
        tip, hand = spear_tip(arm, spear)
        for amount in (0.0, 0.35, 0.7, 1.0):
            point = hand.lerp(tip, amount)
            if inside_body(point):
                hits.append("%s f%d (%.2f,%.2f,%.2f)" % (label, frame, point.x, point.y, point.z))
                break
    if hits:
        print("CLIP", " | ".join(hits))
    else:
        print("CLEAR", label)


def export_fbx(arm, meshes, path):
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
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


def setup_camera(scene):
    cam_data = bpy.data.cameras.new("AttackCam")
    cam = bpy.data.objects.new("AttackCam", cam_data)
    scene.collection.objects.link(cam)
    cam_data.lens = 42
    target = bpy.data.objects.new("AttackTarget", None)
    scene.collection.objects.link(target)
    target.location = (0.0, 0.0, 1.05)
    con = cam.constraints.new("TRACK_TO")
    con.target = target
    con.track_axis = "TRACK_NEGATIVE_Z"
    con.up_axis = "UP_Y"
    scene.camera = cam
    return cam


def render_frame(scene, cam, loc, path):
    cam.location = loc
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


# Bone, euler axis, min degrees, max degrees. Right arm aims the spear. Left arm reaches the shaft.
ARM_PARAMS = [
    ("mixamorig:RightShoulder", 2, -35, 35),
    ("mixamorig:RightArm", 0, -35, 100),
    ("mixamorig:RightArm", 1, -90, 90),
    ("mixamorig:RightArm", 2, -80, 70),
    ("mixamorig:RightForeArm", 0, -8, 85),
    ("mixamorig:RightForeArm", 2, -45, 45),
    ("mixamorig:RightHand", 1, -120, 120),
    ("mixamorig:RightHand", 2, -80, 80),
    ("mixamorig:LeftShoulder", 2, -30, 30),
    ("mixamorig:LeftArm", 0, -30, 90),
    ("mixamorig:LeftArm", 2, -50, 50),
    ("mixamorig:LeftForeArm", 0, -8, 85),
]

# Tip then right-hand target. +X is her left, -Y is forward, +Z is up. Tip stays ~1.3 m from the hand.
SPEAR_TARGETS = {
    "ready": ((0.24, -1.43, 1.82), (-0.25, -0.45, 1.15)),
    "wide": ((-1.25, -0.15, 1.75), (-0.55, -0.2, 1.35)),
    "cocked": ((-1.20, 0.55, 1.90), (-0.55, 0.05, 1.40)),
    "crescent_hit": ((1.05, -1.19, 1.31), (-0.05, -0.55, 1.25)),
    "crescent_follow": ((1.38, -0.64, 0.97), (0.15, -0.35, 1.15)),
    "chamber": ((-0.67, -1.44, 0.53), (-0.40, -0.20, 0.75)),
    "rise_hit": ((0.15, -1.20, 2.45), (-0.05, -0.50, 1.40)),
    "rise_follow": ((0.85, -0.89, 2.21), (0.10, -0.30, 1.35)),
}


def solve_arms(arm, spear, pose, tip_target, hand_target):
    from mathutils import Vector
    current = {k: list(v) for k, v in pose.items()}
    tip_target = Vector(tip_target)
    hand_target = Vector(hand_target)

    def score_of(candidate):
        apply_pose(arm, {k: tuple(v) for k, v in candidate.items()})
        tip, hand = spear_tip(arm, spear)
        err = (tip - tip_target).length * 5.0 + (hand - hand_target).length * 1.2
        for amount in (0.3, 0.55, 0.8):
            point = hand.lerp(tip, amount)
            if abs(point.x) < 0.16 and point.y > -0.2 and 0.9 < point.z < 1.62:
                err += 1.6
            if abs(point.x) < 0.13 and abs(point.y) < 0.14 and point.z > 1.5:
                err += 1.3
        left = (arm.matrix_world @ arm.pose.bones["mixamorig:LeftHand"].matrix).to_translation()
        err += (left - hand.lerp(tip, 0.4)).length * 0.45
        return err

    best = score_of(current)
    for step in (28, 14, 7, 3):
        for _unused in range(2):
            for bone, axis, low, high in ARM_PARAMS:
                base = current[bone][axis]
                chosen = base
                chosen_score = best
                for delta in (-step, step):
                    trial = base + delta
                    if trial < low or trial > high:
                        continue
                    current[bone][axis] = trial
                    trial_score = score_of(current)
                    if trial_score < chosen_score - 0.0001:
                        chosen_score = trial_score
                        chosen = trial
                current[bone][axis] = chosen
                best = chosen_score
    solved = {k: tuple(round(v, 2) for v in values) for k, values in current.items()}
    return solved, best


def main():
    os.makedirs(SHEET, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=SRC)
    arm = bpy.data.objects["SERAPHINE_rig"]
    spear = bpy.data.objects["SERAPHINE_spear"]
    body = bpy.data.objects["SERAPHINE_body"]
    for name in ("SERAPHINE_spear__attack_throw", "SERAPHINE_spear__attack_thrust", "SERAPHINE_cape", "SERAPHINE_cape_rig"):
        obj = bpy.data.objects.get(name)
        if obj:
            obj.hide_render = True
            obj.hide_set(True)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)

    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 480
    scene.render.resolution_y = 640
    scene.render.film_transparent = False
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    cam = setup_camera(scene)

    named = (
        ("ready", READY),
        ("wide", WIDE),
        ("cocked", COCKED),
        ("crescent_hit", CRESCENT_HIT),
        ("crescent_follow", CRESCENT_FOLLOW),
        ("chamber", CHAMBER),
        ("rise_hit", RISE_HIT),
        ("rise_follow", RISE_FOLLOW),
    )
    for _label, pose in named:
        loc = list(pose["loc"])
        loc[1] += 0.06
        pose["loc"] = tuple(loc)
    for label, pose in named:
        tip_target, hand_target = SPEAR_TARGETS[label]
        solved, err = solve_arms(arm, spear, pose, tip_target, hand_target)
        pose.clear()
        pose.update(solved)
        print("SOLVE %s err %.3f" % (label, err))

    report = []
    for label, pose in (
        ("ready", READY),
        ("wide", WIDE),
        ("cocked", COCKED),
        ("crescent_hit", CRESCENT_HIT),
        ("crescent_follow", CRESCENT_FOLLOW),
        ("chamber", CHAMBER),
        ("rise_hit", RISE_HIT),
        ("rise_follow", RISE_FOLLOW),
    ):
        apply_pose(arm, pose)
        tip, hand = spear_tip(arm, spear)
        ground = lowest_z(body)
        line = "%s tip(%.2f,%.2f,%.2f) hand(%.2f,%.2f,%.2f) lowest %.3f" % (
            label, tip.x, tip.y, tip.z, hand.x, hand.y, hand.z, ground)
        print(line)
        report.append(line)
        render_frame(scene, cam, (0.0, -4.1, 1.15), os.path.join(SHEET, label + "_front.png"))
        if label in ("crescent_hit", "rise_hit", "chamber", "cocked"):
            render_frame(scene, cam, (3.3, -1.2, 1.2), os.path.join(SHEET, label + "_side.png"))

    meshes = [
        body,
        spear,
        bpy.data.objects["SERAPHINE_wing_A"],
        bpy.data.objects["SERAPHINE_wing_B"],
    ]
    for name, keys in ATTACKS.items():
        check_path(arm, spear, keys, name)
        for action in list(bpy.data.actions):
            bpy.data.actions.remove(action)
        build_action(arm, name, keys)
        fbx_name = "SERAPHINE_%s.fbx" % name
        export_fbx(arm, meshes, os.path.join(OUT_DIR, fbx_name))
        export_fbx(arm, meshes, os.path.join(UNITY_DIR, fbx_name))

    blend_path = os.path.join(OUT_DIR, "SERAPHINE_attacks.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("SAVED", blend_path)
    with open(os.path.join(OUT_DIR, "pose_report.txt"), "w", encoding="utf-8") as handle:
        handle.write("\n".join(report))


if __name__ == "__main__":
    main()
