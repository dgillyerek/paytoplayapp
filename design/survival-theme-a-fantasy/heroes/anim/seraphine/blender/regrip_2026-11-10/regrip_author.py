"""Re-author Seraphine's two preview attacks.

The New Characters Preview instances SERAPHINE_rig_rest and samples bone
curves only. The spear is skinned 100% to RightHand, and the fingers have no
bones, so the palm shape cannot change here. These clips keep the guard's
hand-to-forearm rotation (the bind grip) and move the staff on a path that
stays outside the body, wing, and arm outlines.

Run:
  blender --background --factory-startup --python regrip_author.py
"""
import bpy
import json
import math
import os
from mathutils import Matrix, Quaternion, Vector

REPO = "/agent/repos/paytoplayapp"
ART = os.path.join(REPO, "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine")
DESIGN = os.path.join(REPO, "design/survival-theme-a-fantasy/heroes/anim/seraphine/attacks_20261109")
OUT = os.path.join(REPO, "design/survival-theme-a-fantasy/heroes/anim/seraphine/blender/regrip_2026-11-10")
os.makedirs(OUT, exist_ok=True)

IMPORT = dict(
    global_scale=1.0, use_anim=True, anim_offset=0.0,
    automatic_bone_orientation=False, ignore_leaf_bones=False,
    use_custom_normals=True, use_image_search=False,
    primary_bone_axis="Y", secondary_bone_axis="X",
    axis_forward="-Z", axis_up="Y",
)
P = "mixamorig:"

# Lengths along parent +Y from the imported rest offsets.
L_R_UP, L_R_FORE = 0.239, 0.227
L_L_UP, L_L_FORE = 0.225, 0.236
L_R_THIGH, L_R_SHIN = 0.300, 0.424
L_L_THIGH, L_L_SHIN = 0.332, 0.415


def ease(u):
    u = max(0.0, min(1.0, u))
    return u * u * (3.0 - 2.0 * u)


def sample_scalar(keys, s):
    if s <= keys[0][0]:
        return keys[0][1]
    for (s0, v0), (s1, v1) in zip(keys, keys[1:]):
        if s <= s1:
            t = 0.0 if s1 == s0 else (s - s0) / (s1 - s0)
            return v0 + (v1 - v0) * t
    return keys[-1][1]


def sample_vec(keys, s):
    if s <= keys[0][0]:
        return keys[0][1].copy()
    for (s0, v0), (s1, v1) in zip(keys, keys[1:]):
        if s <= s1:
            t = 0.0 if s1 == s0 else (s - s0) / (s1 - s0)
            return v0.lerp(v1, t)
    return keys[-1][1].copy()


def sample_dir(keys, s):
    if s <= keys[0][0]:
        return keys[0][1].normalized()
    for (s0, v0), (s1, v1) in zip(keys, keys[1:]):
        if s <= s1:
            t = 0.0 if s1 == s0 else (s - s0) / (s1 - s0)
            return v0.slerp(v1, t)
    return keys[-1][1].normalized()


def mat_columns(x, y, z):
    return Matrix((x, y, z)).transposed()


def mat_y_twist(y_dir, guard_quat):
    y = y_dir.normalized()
    gx = guard_quat @ Vector((1.0, 0.0, 0.0))
    x = gx - y * gx.dot(y)
    if x.length < 1e-4:
        gz = guard_quat @ Vector((0.0, 0.0, 1.0))
        x = gz - y * gz.dot(y)
    x.normalize()
    z = x.cross(y).normalized()
    x = y.cross(z).normalized()
    return mat_columns(x, y, z)


def aim_forearm(fore_dir, staff_dir, staff_local):
    """Forearm rotation: +Y along the forearm, roll so staff_local maps onto staff_dir."""
    y = fore_dir.normalized()
    local = staff_local - Vector((0.0, 1.0, 0.0)) * staff_local.dot(Vector((0.0, 1.0, 0.0)))
    world = staff_dir - y * staff_dir.dot(y)
    if local.length < 1e-5 or world.length < 1e-5:
        return mat_y_twist(y, Quaternion())
    # A zero-twist frame whose +Y is y, then roll around y.
    tmp = Vector((1.0, 0.0, 0.0)) if abs(y.z) < 0.85 else Vector((0.0, 0.0, 1.0))
    x0 = tmp.cross(y).normalized()
    z0 = x0.cross(y).normalized()
    r0 = mat_columns(x0, y, z0)
    current = (r0 @ local).normalized()
    want = world.normalized()
    ang = math.atan2(current.cross(want).dot(y), max(-1.0, min(1.0, current.dot(want))))
    rolled = Quaternion(y, ang).to_matrix() @ r0
    return rolled


def ik_joint(root, target, pole, l1, l2):
    delta = target - root
    dist = delta.length
    maxd = l1 + l2 - 0.006
    mind = abs(l1 - l2) + 0.006
    clamped = False
    if dist < 1e-6:
        delta = Vector((0.0, -1.0, 0.0))
        dist = 1.0
    if dist > maxd:
        delta = delta.normalized() * maxd
        dist = maxd
        target = root + delta
        clamped = True
    elif dist < mind:
        delta = delta.normalized() * mind
        dist = mind
        target = root + delta
        clamped = True
    axis = delta.normalized()
    a = (l1 * l1 - l2 * l2 + dist * dist) / (2.0 * dist)
    h2 = max(0.0, l1 * l1 - a * a)
    h = math.sqrt(h2)
    side = pole - root
    side = side - axis * side.dot(axis)
    if side.length < 1e-5:
        side = Vector((1.0, 0.0, 0.0)).cross(axis)
    side.normalize()
    joint = root + axis * a + side * h
    return joint, target, clamped


def cone_forearm(shoulder, wrist, staff_dir, upper, fore, alpha, prev_phi):
    """Pick the forearm direction on the staff cone that reaches the shoulder."""
    direction = staff_dir.normalized()
    tmp = Vector((1.0, 0.0, 0.0)) if abs(direction.z) < 0.9 else Vector((0.0, 1.0, 0.0))
    u = direction.cross(tmp).normalized()
    v = direction.cross(u).normalized()
    best = None
    for i in range(72):
        phi = (2.0 * math.pi * i) / 72.0
        fore_dir = (
            direction * math.cos(alpha)
            + (u * math.cos(phi) + v * math.sin(phi)) * math.sin(alpha)
        ).normalized()
        elbow = wrist - fore_dir * fore
        err = abs((elbow - shoulder).length - upper)
        # Prefer the elbow out to her right (-X), slightly forward (-Y), not up by the ear.
        # Elbow stays out to her right and a little forward, below the shoulder,
        # so the upright staff does not cross the upper arm.
        prefer = shoulder + Vector((-0.28, -0.20, -0.12))
        score = err * 12.0 + (elbow - prefer).length * 0.35
        if prev_phi is not None:
            dphi = (phi - prev_phi + math.pi) % (2.0 * math.pi) - math.pi
            score += abs(dphi) * 0.08
        if best is None or score < best[0]:
            best = (score, phi, fore_dir, elbow, err)
    return best


bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30


def do_import(path):
    before = {o.name for o in bpy.data.objects}
    bpy.ops.import_scene.fbx(filepath=path, **IMPORT)
    return [bpy.data.objects[n] for n in {o.name for o in bpy.data.objects} - before]


rest = do_import(os.path.join(ART, "SERAPHINE_rig_rest.fbx"))
rig = next(o for o in rest if o.type == "ARMATURE")
body = bpy.data.objects["SERAPHINE_body"]
spear = bpy.data.objects["SERAPHINE_spear"]
wing_a = bpy.data.objects["SERAPHINE_wing_A"]
wing_b = bpy.data.objects["SERAPHINE_wing_B"]
bpy.context.view_layer.update()

guard_basis = {pb.name: pb.matrix_basis.copy() for pb in rig.pose.bones}
guard_world = {pb.name: pb.matrix.to_quaternion() for pb in rig.pose.bones}
hips_pb = rig.pose.bones[P + "Hips"]
guard_hip = hips_pb.matrix.translation.copy()

fore_q = guard_world[P + "RightForeArm"]
hand_q = guard_world[P + "RightHand"]
hand_local = fore_q.inverted() @ hand_q
staff_local = hand_local @ Vector((1.0, 0.0, 0.0))
ALPHA = staff_local.angle(Vector((0.0, 1.0, 0.0)))
print(f"guard staff-forearm angle {math.degrees(ALPHA):.2f} deg")

guard_staff_dir = (hand_q @ Vector((1.0, 0.0, 0.0))).normalized()
guard_wrist = rig.pose.bones[P + "RightHand"].head.copy()
guard_l_hand_off = (
    rig.pose.bones[P + "LeftHand"].head - rig.pose.bones[P + "LeftArm"].head
).copy()
guard_l_ankle = rig.pose.bones[P + "LeftUpLeg.002"].head.copy()
guard_r_ankle = rig.pose.bones[P + "RightUpLeg.002"].head.copy()
guard_lfoot_q = guard_world[P + "LeftUpLeg.002"].copy()
guard_rfoot_q = guard_world[P + "RightUpLeg.002"].copy()
print("guard wrist", [round(x, 3) for x in guard_wrist])
print("guard staff", [round(x, 3) for x in guard_staff_dir])
print("ankles", [round(x, 3) for x in guard_l_ankle], [round(x, 3) for x in guard_r_ankle])


def restore_guard():
    for pb in rig.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.matrix_basis = guard_basis[pb.name].copy()
    bpy.context.view_layer.update()


def set_world_rot(pb, rot_m3):
    head = pb.head.copy()
    M = rot_m3.to_4x4()
    M.translation = head
    pb.matrix = M
    bpy.context.view_layer.update()


def add_world_delta(names, delta):
    for name in names:
        pb = rig.pose.bones[name]
        desired = delta @ pb.matrix.to_quaternion()
        set_world_rot(pb, desired.to_matrix())


def solve_right_arm(staff_dir, wrist_target, prev_phi):
    shoulder = rig.pose.bones[P + "RightArm"].head.copy()
    wrist = wrist_target.copy()
    reach = L_R_UP + L_R_FORE - 0.02
    delta = wrist - shoulder
    if delta.length > reach:
        wrist = shoulder + delta.normalized() * reach
    # Pull the wrist in until some cone roll hits the upper-arm length.
    fore_dir = elbow = None
    phi = prev_phi
    err = 1.0
    for _ in range(10):
        _score, phi, fore_dir, elbow, err = cone_forearm(
            shoulder, wrist, staff_dir, L_R_UP, L_R_FORE, ALPHA, prev_phi)
        if err < 0.012:
            break
        wrist = shoulder + (wrist - shoulder) * 0.92
    # Snap the elbow onto the exact upper-arm length, then the wrist onto the forearm.
    elbow = shoulder + (elbow - shoulder).normalized() * L_R_UP
    fore_dir = (wrist - elbow)
    if fore_dir.length < 1e-5:
        fore_dir = Vector((0.0, -1.0, 0.2))
    # Re-seat the wrist on the forearm axis so the bone length stays exact.
    wrist = elbow + fore_dir.normalized() * L_R_FORE
    fore_dir = (wrist - elbow).normalized()
    up_dir = (elbow - shoulder).normalized()
    set_world_rot(rig.pose.bones[P + "RightArm"], mat_y_twist(up_dir, guard_world[P + "RightArm"]))
    fore_m = aim_forearm(fore_dir, staff_dir, staff_local)
    set_world_rot(rig.pose.bones[P + "RightForeArm"], fore_m)
    hand_m = fore_m @ hand_local.to_matrix()
    set_world_rot(rig.pose.bones[P + "RightHand"], hand_m)
    got = (rig.pose.bones[P + "RightHand"].matrix.to_quaternion() @ Vector((1.0, 0.0, 0.0))).normalized()
    aim = math.degrees(got.angle(staff_dir.normalized()))
    return phi, aim, err, wrist


def solve_left_arm(hand_target):
    shoulder = rig.pose.bones[P + "LeftArm"].head.copy()
    pole = shoulder + Vector((0.28, -0.05, -0.02))
    elbow, wrist, _clamped = ik_joint(shoulder, hand_target, pole, L_L_UP, L_L_FORE)
    set_world_rot(rig.pose.bones[P + "LeftArm"], mat_y_twist((elbow - shoulder), guard_world[P + "LeftArm"]))
    set_world_rot(rig.pose.bones[P + "LeftForeArm"], mat_y_twist((wrist - elbow), guard_world[P + "LeftForeArm"]))
    # Leave the left hand at its guard local rotation so the open palm follows the forearm.
    # matrix was overwritten by the parent update; put the guard local basis back.
    rig.pose.bones[P + "LeftHand"].matrix_basis = guard_basis[P + "LeftHand"].copy()
    bpy.context.view_layer.update()


def solve_leg(side, ankle_target, foot_quat):
    thigh_n = P + side + "UpLeg"
    shin_n = P + side + "UpLeg.001"
    foot_n = P + side + "UpLeg.002"
    l1 = L_L_THIGH if side == "Left" else L_R_THIGH
    l2 = L_L_SHIN if side == "Left" else L_R_SHIN
    root = rig.pose.bones[thigh_n].head.copy()
    outward = 0.16 if side == "Left" else -0.16
    pole = root + Vector((outward, -0.42, 0.15))
    knee, ankle, _clamped = ik_joint(root, ankle_target, pole, l1, l2)
    set_world_rot(rig.pose.bones[thigh_n], mat_y_twist((knee - root), guard_world[thigh_n]))
    set_world_rot(rig.pose.bones[shin_n], mat_y_twist((ankle - knee), guard_world[shin_n]))
    foot = rig.pose.bones[foot_n]
    head = foot.head.copy()
    M = foot_quat.to_matrix().to_4x4()
    M.translation = head
    foot.matrix = M
    bpy.context.view_layer.update()
    # Toe stays in its guard local pose on the foot.
    toe = rig.pose.bones[P + side + "UpLeg.003"]
    toe.matrix_basis = guard_basis[toe.name].copy()
    bpy.context.view_layer.update()


def foot_target(guard_ankle, forward, lateral, lift):
    # forward > 0 steps toward -Y. lateral > 0 steps toward +X (her left).
    return Vector((
        guard_ankle.x + lateral,
        guard_ankle.y - forward,
        guard_ankle.z + lift,
    ))


def pose_frame(spec, s, prev_phi):
    restore_guard()
    chest = sample_scalar(spec["chest"], s)
    hip_yaw = chest * 0.58
    pitch = sample_scalar(spec["pitch"], s)
    roll = sample_scalar(spec["roll"], s)
    drop = sample_scalar(spec["drop"], s)
    # Hips: yaw plus a vertical drop. Horizontal root stays put so HoldHips is a no-op.
    hips = rig.pose.bones[P + "Hips"]
    yaw_q = Quaternion(Vector((0.0, 0.0, 1.0)), hip_yaw)
    desired = yaw_q @ hips.matrix.to_quaternion()
    M = desired.to_matrix().to_4x4()
    M.translation = Vector((guard_hip.x, guard_hip.y, guard_hip.z - drop))
    hips.matrix = M
    bpy.context.view_layer.update()
    # Chest leads the hips. Pitch leans toward the enemy (-Y). Roll is a side bend.
    delta = (
        Quaternion(Vector((0.0, 0.0, 1.0)), chest - hip_yaw)
        @ Quaternion(Vector((1.0, 0.0, 0.0)), pitch)
        @ Quaternion(Vector((0.0, 1.0, 0.0)), roll)
    )
    add_world_delta([P + "Spine", P + "Spine1", P + "Spine2"], delta)
    # Head follows part of the chest and stays near it.
    head_delta = Quaternion(Vector((0.0, 0.0, 1.0)), (chest - hip_yaw) * 0.45) @ Quaternion(
        Vector((1.0, 0.0, 0.0)), pitch * 0.35)
    add_world_delta([P + "Neck", P + "Head"], head_delta)

    l_fwd = sample_scalar(spec["l_fwd"], s)
    r_back = sample_scalar(spec["r_back"], s)
    l_lift = sample_scalar(spec["l_lift"], s)
    r_lift = sample_scalar(spec["r_lift"], s)
    solve_leg("Left", foot_target(guard_l_ankle, l_fwd, 0.0, l_lift), guard_lfoot_q)
    solve_leg("Right", foot_target(guard_r_ankle, -r_back, 0.0, r_lift), guard_rfoot_q)

    staff_dir = soften_staff(sample_dir(spec["staff"], s), sample_vec(spec["wrist"], s))
    wrist = sample_vec(spec["wrist"], s)
    phi, aim, err, used = solve_right_arm(staff_dir, wrist, prev_phi)

    # Counter-swing blends in and out so the left arm does not pop off the guard.
    def ramp(a, b, x):
        if x <= a:
            return 0.0
        if x >= b:
            return 1.0
        u = (x - a) / (b - a)
        return u * u * (3.0 - 2.0 * u)

    w = ramp(0.0, 0.18, s) * (1.0 - ramp(0.82, 1.0, s))
    l_hand = guard_l_hand_off.lerp(sample_vec(spec["l_hand"], s), w)
    shoulder_l = rig.pose.bones[P + "LeftArm"].head
    solve_left_arm(shoulder_l + l_hand)
    return phi, aim, err, used


def soften_staff(direction, wrist):
    """Keep the butt in front of the hips. A forward tip throws the 0.85 m butt
    back through the wings, so pull the aim toward vertical until the butt is clear."""
    d = direction.normalized()
    up = Vector((0.0, -0.02, 1.0)).normalized()
    for _ in range(7):
        butt = wrist - d * 0.92
        if butt.y < -0.12 and butt.x < -0.05:
            return d
        d = d.slerp(up, 0.4)
    return d


def track(pairs):
    return [(s, v if not isinstance(v, tuple) else Vector(v)) for s, v in pairs]


def build_specs():
    gdir = guard_staff_dir
    gw = guard_wrist

    def D(x, y, z):
        return Vector((x, y, z)).normalized()

    melee = {
        "frames": 73,
        "name": "attack_standing_melee",
        "chest": [(0.0, 0.0), (0.22, 0.22), (0.42, 0.02), (0.60, -0.24), (0.80, -0.06), (1.0, 0.0)],
        "pitch": [(0.0, 0.0), (0.22, 0.08), (0.55, 0.10), (0.78, 0.04), (1.0, 0.0)],
        "roll": [(0.0, 0.0), (0.22, -0.04), (0.58, 0.04), (1.0, 0.0)],
        "drop": [(0.0, 0.0), (0.24, 0.040), (0.58, 0.030), (0.82, 0.012), (1.0, 0.0)],
        # Left foot steps forward to load the right leg, then the right foot
        # pushes back as the cut lands so the hips (pinned in XZ) sit over the front foot.
        "l_fwd": [(0.0, 0.0), (0.10, 0.0), (0.28, 0.12), (0.72, 0.12), (0.94, 0.0), (1.0, 0.0)],
        "r_back": [(0.0, 0.0), (0.38, 0.0), (0.56, 0.10), (0.74, 0.10), (0.95, 0.0), (1.0, 0.0)],
        "l_lift": [(0.0, 0.0), (0.10, 0.0), (0.19, 0.09), (0.28, 0.0), (0.72, 0.0), (0.83, 0.08), (0.94, 0.0), (1.0, 0.0)],
        "r_lift": [(0.0, 0.0), (0.38, 0.0), (0.47, 0.07), (0.56, 0.0), (0.74, 0.0), (0.84, 0.07), (0.95, 0.0), (1.0, 0.0)],
        "staff": [
            (0.0, gdir),
            (0.22, D(-0.04, -0.05, 0.998)),
            (0.40, D(0.18, -0.04, 0.983)),
            (0.58, D(0.50, -0.04, 0.865)),
            (0.78, D(0.16, -0.03, 0.987)),
            (1.0, gdir),
        ],
        "wrist": [
            (0.0, gw),
            (0.22, gw + Vector((-0.06, -0.12, 0.07))),
            (0.58, gw + Vector((0.02, -0.22, 0.12))),
            (0.78, gw + Vector((0.0, -0.06, 0.03))),
            (1.0, gw),
        ],
        # Offsets from the posed left shoulder. Kept wide of the slash.
        "l_hand": [
            (0.0, Vector((0.22, -0.10, -0.28))),
            (0.20, Vector((0.26, -0.16, -0.10))),
            (0.52, Vector((0.30, 0.08, -0.32))),
            (1.0, Vector((0.22, -0.10, -0.28))),
        ],
    }
    thrust = {
        "frames": 91,
        "name": "attack_thrust_slash",
        "chest": [(0.0, 0.0), (0.22, 0.12), (0.40, 0.04), (0.62, -0.26), (0.82, -0.06), (1.0, 0.0)],
        "pitch": [(0.0, 0.0), (0.24, 0.16), (0.42, 0.22), (0.64, 0.10), (1.0, 0.0)],
        "roll": [(0.0, 0.0), (0.24, -0.03), (0.62, 0.05), (1.0, 0.0)],
        "drop": [(0.0, 0.0), (0.24, 0.035), (0.42, 0.028), (0.70, 0.018), (1.0, 0.0)],
        "l_fwd": [(0.0, 0.0), (0.12, 0.0), (0.32, 0.14), (0.70, 0.14), (0.94, 0.0), (1.0, 0.0)],
        "r_back": [(0.0, 0.0), (0.36, 0.0), (0.50, 0.10), (0.72, 0.10), (0.95, 0.0), (1.0, 0.0)],
        "l_lift": [(0.0, 0.0), (0.12, 0.0), (0.22, 0.09), (0.32, 0.0), (0.70, 0.0), (0.82, 0.08), (0.94, 0.0), (1.0, 0.0)],
        "r_lift": [(0.0, 0.0), (0.36, 0.0), (0.43, 0.07), (0.50, 0.0), (0.72, 0.0), (0.84, 0.07), (0.95, 0.0), (1.0, 0.0)],
        "staff": [
            (0.0, gdir),
            (0.20, D(0.0, -0.08, 0.997)),
            (0.40, D(0.06, -0.10, 0.993)),
            (0.60, D(0.38, -0.04, 0.924)),
            (0.80, D(0.12, -0.04, 0.992)),
            (1.0, gdir),
        ],
        "wrist": [
            (0.0, gw),
            (0.20, gw + Vector((-0.04, -0.08, 0.02))),
            (0.40, gw + Vector((0.00, -0.22, 0.10))),
            (0.60, gw + Vector((0.02, -0.20, 0.14))),
            (1.0, gw),
        ],
        "l_hand": [
            (0.0, Vector((0.22, -0.10, -0.28))),
            (0.20, Vector((0.20, -0.22, -0.18))),
            (0.40, Vector((0.24, -0.18, -0.22))),
            (0.56, Vector((0.32, 0.06, -0.34))),
            (1.0, Vector((0.22, -0.10, -0.28))),
        ],
    }
    return melee, thrust


def key_pose(frame):
    for pb in rig.pose.bones:
        pb.keyframe_insert(data_path="rotation_quaternion", frame=frame)
        if pb.name == P + "Hips" or pb.location.length > 1e-8:
            pb.keyframe_insert(data_path="location", frame=frame)


def new_action(name):
    if rig.animation_data is None:
        rig.animation_data_create()
    act = bpy.data.actions.new(name)
    rig.animation_data.action = act
    if hasattr(act, "slots"):
        slot = act.slots.new("OBJECT", rig.name) if len(act.slots) == 0 else act.slots[0]
        if hasattr(rig.animation_data, "action_slot"):
            rig.animation_data.action_slot = slot
    return act


def author(spec):
    # Keep the action unassigned while solving. An active action rewrites the
    # pose on every view-layer update and fights the IK.
    if rig.animation_data and rig.animation_data.action:
        rig.animation_data.action = None
    bpy.context.view_layer.update()
    act = new_action(spec["name"])
    rig.animation_data.action = None
    n = spec["frames"]
    prev_phi = None
    aims = []
    for frame in range(1, n + 1):
        s = ease((frame - 1) / (n - 1))
        prev_phi, aim, err, _wrist = pose_frame(spec, s, prev_phi)
        rig.animation_data.action = act
        try:
            rig.animation_data.action_slot = act.slots[0]
        except Exception:
            pass
        key_pose(frame)
        rig.animation_data.action = None
        aims.append(aim)
        if frame in (1, n // 3, 2 * n // 3, n) or aim > 25:
            print(f"  {spec['name']} f{frame} s={s:.2f} aim {aim:.1f}deg cone_err {err*100:.1f}cm")
    rig.animation_data.action = act
    try:
        rig.animation_data.action_slot = act.slots[0]
    except Exception:
        pass
    scene.frame_start = 1
    scene.frame_end = n
    print(f"  aim max {max(aims):.1f} mean {sum(aims)/len(aims):.1f}")
    return act


def export_action(name):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    for obj in (body, spear, wing_a, wing_b):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    paths = []
    for folder in (ART, DESIGN, OUT):
        path = os.path.join(folder, f"SERAPHINE_{name}.fbx")
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
        paths.append(path)
        print("EXPORTED", path)
    return paths


specs = build_specs()
# Remove actions created by nothing; author sequentially and export each,
# leaving both actions in the file.
for spec in specs:
    author(spec)
    export_action(spec["name"])

blend_path = os.path.join(OUT, "SERAPHINE_regrip.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("SAVED", blend_path)
