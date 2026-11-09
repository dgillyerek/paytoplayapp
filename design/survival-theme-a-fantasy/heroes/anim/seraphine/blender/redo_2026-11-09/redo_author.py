"""Seraphine redo 2026-11-09.

Rebuilds attack_thrust and attack_throw from scratch on Derek's locked
24-bone skeleton. The walk bone curves are copied unchanged from the shipped
walk FBX. The right-hand fingers (no finger bones on this rig) are curled in
the bind mesh so the palm closes on the shaft; that is the only mesh edit,
and it is not written to rig_rest, idle, cape or the atlas.

    blender -b --python redo_author.py -- preview
    blender -b --python redo_author.py -- export
"""
import bpy
import hashlib
import math
import os
import sys
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree

ART = "/agent/repos/paytoplayapp/Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine"
FINAL = "/agent/repos/paytoplayapp/design/survival-theme-a-fantasy/heroes/anim/seraphine/final"
OUT_PREVIEW = "/tmp/redo_prev"
P = "mixamorig:"
IMPORT = dict(
    global_scale=1.0, use_anim=True, anim_offset=0.0,
    automatic_bone_orientation=False, ignore_leaf_bones=False,
    use_custom_normals=True, use_image_search=False,
    primary_bone_axis="Y", secondary_bone_axis="X",
    axis_forward="-Z", axis_up="Y",
)
EXPORT = dict(
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
    mesh_smooth_type="FACE",
    use_armature_deform_only=False,
    path_mode="AUTO",
    use_mesh_modifiers=True,
)

FWD = Vector((0.0, -1.0, 0.0))
UP = Vector((0.0, 0.0, 1.0))
PHASE = "preview"
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if "export" in argv:
    PHASE = "export"


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def sample(keys, frame):
    if frame <= keys[0][0]:
        return keys[0][1]
    if frame >= keys[-1][0]:
        return keys[-1][1]
    for i in range(len(keys) - 1):
        f0, v0 = keys[i]
        f1, v1 = keys[i + 1]
        if f0 <= frame <= f1:
            t = smooth((frame - f0) / max(1e-6, (f1 - f0)))
            return v0 + (v1 - v0) * t
    return keys[-1][1]


def sample_vec(keys, frame):
    if frame <= keys[0][0]:
        return keys[0][1].copy()
    if frame >= keys[-1][0]:
        return keys[-1][1].copy()
    for i in range(len(keys) - 1):
        f0, v0 = keys[i]
        f1, v1 = keys[i + 1]
        if f0 <= frame <= f1:
            t = smooth((frame - f0) / max(1e-6, (f1 - f0)))
            return v0.lerp(v1, t)
    return keys[-1][1].copy()


def bone_hash(arm):
    h = hashlib.sha256()
    for b in arm.data.bones:
        h.update(b.name.encode())
        h.update((b.parent.name if b.parent else "-").encode())
        for v in list(b.head_local) + list(b.tail_local):
            h.update(f"{v:.6f}".encode())
        h.update(f"{b.matrix_local}".encode())
    return h.hexdigest()[:16]


def mesh_hash(obj):
    h = hashlib.sha256()
    h.update(str(len(obj.data.vertices)).encode())
    for v in obj.data.vertices:
        h.update(f"{v.co.x:.6f},{v.co.y:.6f},{v.co.z:.6f}".encode())
    return h.hexdigest()[:16]


def weight_hash(obj):
    h = hashlib.sha256()
    for vg in obj.vertex_groups:
        h.update(vg.name.encode())
    for v in obj.data.vertices:
        for g in v.groups:
            h.update(f"{g.group}:{g.weight:.5f}".encode())
    return h.hexdigest()[:16]


def axes_from_y(y, prev_x, hint):
    y = y.normalized()
    x = None
    if prev_x is not None:
        x = prev_x - y * prev_x.dot(y)
    if x is None or x.length < 1e-6:
        h = hint.copy()
        h = h - y * h.dot(y)
        if h.length < 1e-6:
            h = Vector((1.0, 0.0, 0.0)).cross(y)
            if h.length < 1e-6:
                h = Vector((0.0, 1.0, 0.0)).cross(y)
        x = y.cross(h.normalized())
    x = x.normalized()
    z = x.cross(y).normalized()
    x = y.cross(z).normalized()
    return x, y, z


def axes_from_x(x, prev_y, hint):
    x = x.normalized()
    y = None
    if prev_y is not None:
        y = prev_y - x * prev_y.dot(x)
    if y is None or y.length < 1e-6:
        h = hint - x * hint.dot(x)
        if h.length < 1e-6:
            h = Vector((0.0, 0.0, 1.0)).cross(x)
            if h.length < 1e-6:
                h = Vector((0.0, 1.0, 0.0)).cross(x)
        y = h.normalized()
    else:
        y = y.normalized()
    z = x.cross(y).normalized()
    y = z.cross(x).normalized()
    return x, y, z


def mat_from_axes(x, y, z, head):
    m = Matrix((x, y, z)).transposed().to_4x4()
    m.translation = head
    return m


def ik_two(root, target, pole, L1, L2, shorten=0.94):
    delta = target - root
    dist = delta.length
    hi = (L1 + L2) * shorten
    lo = abs(L1 - L2) + 0.02
    if dist < 1e-6:
        delta = Vector((0.0, -1.0, 0.0))
        dist = 1e-6
    if dist > hi:
        target = root + delta.normalized() * hi
        dist = hi
    elif dist < lo:
        target = root + delta.normalized() * lo
        dist = lo
    n = (target - root).normalized()
    cos_a = (L1 * L1 + dist * dist - L2 * L2) / (2.0 * L1 * dist)
    cos_a = max(-1.0, min(1.0, cos_a))
    sin_a = math.sqrt(max(0.0, 1.0 - cos_a * cos_a))
    pv = pole - root
    pv = pv - n * pv.dot(n)
    if pv.length < 1e-5:
        pv = Vector((0.0, 0.0, 1.0)).cross(n)
        if pv.length < 1e-5:
            pv = Vector((1.0, 0.0, 0.0)).cross(n)
    pv.normalize()
    joint = root + (n * cos_a + pv * sin_a).normalized() * L1
    return joint, target


# ---------------------------------------------------------------------------
# scene from the shipped rest FBX
# ---------------------------------------------------------------------------
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(ART, "SERAPHINE_rig_rest.fbx"), **IMPORT)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
body = next(o for o in bpy.data.objects if o.type == "MESH" and "body" in o.name.lower())
spear = next(o for o in bpy.data.objects if o.type == "MESH" and "spear" in o.name.lower())
wings = [o for o in bpy.data.objects if o.type == "MESH" and "wing" in o.name.lower()]
arm.name = "SERAPHINE_rig"
print("BONE HASH BEFORE", bone_hash(arm), "nbones", len(arm.data.bones))
print("MESH", mesh_hash(body), "WEIGHT", weight_hash(body), "SPEAR", mesh_hash(spear))

for pb in arm.pose.bones:
    pb.rotation_mode = "QUATERNION"

REST = {b.name: b.matrix_local.copy() for b in arm.data.bones}
REST_X = {b.name: b.matrix_local.col[0].xyz.normalized() for b in arm.data.bones}
REST_Y = {b.name: b.matrix_local.col[1].xyz.normalized() for b in arm.data.bones}
REST_Z = {b.name: b.matrix_local.col[2].xyz.normalized() for b in arm.data.bones}


def bone_len(a, b):
    return (arm.data.bones[P + a].head_local - arm.data.bones[P + b].head_local).length


L_UP_R = bone_len("RightArm", "RightForeArm")
L_FORE_R = bone_len("RightForeArm", "RightHand")
L_UP_L = bone_len("LeftArm", "LeftForeArm")
L_FORE_L = bone_len("LeftForeArm", "LeftHand")
L_THIGH_R = bone_len("RightUpLeg", "RightUpLeg.001")
L_SHIN_R = bone_len("RightUpLeg.001", "RightUpLeg.002")
L_THIGH_L = bone_len("LeftUpLeg", "LeftUpLeg.001")
L_SHIN_L = bone_len("LeftUpLeg.001", "LeftUpLeg.002")
FOOT_LEN_R = bone_len("RightUpLeg.002", "RightUpLeg.003")
FOOT_LEN_L = bone_len("LeftUpLeg.002", "LeftUpLeg.003")
print(f"reach R {L_UP_R+L_FORE_R:.3f} L {L_UP_L+L_FORE_L:.3f} "
      f"leg R {L_THIGH_R+L_SHIN_R:.3f} L {L_THIGH_L+L_SHIN_L:.3f}")

# sole drop: rest ankle head z minus the lowest foot vertex
bpy.context.view_layer.update()
deps = bpy.context.evaluated_depsgraph_get()
be = body.evaluated_get(deps)
mw = be.matrix_world
groups = {vg.name: vg.index for vg in body.vertex_groups}


def sole_z(side):
    ids = []
    for suffix in (".002", ".003"):
        gi = groups[P + side + "UpLeg" + suffix]
        for v in body.data.vertices:
            for g in v.groups:
                if g.group == gi and g.weight > 0.4:
                    ids.append(v.index)
                    break
    zs = [(mw @ body.data.vertices[i].co).z for i in ids]
    return min(zs) if zs else 0.0


ANKLE_Z = {}
for side in ("Left", "Right"):
    head_z = arm.data.bones[P + side + "UpLeg.002"].head_local.z
    drop = head_z - sole_z(side)
    ANKLE_Z[side] = drop + 0.008
    print(side, "ankle plant z", round(ANKLE_Z[side], 3), "rest head", round(head_z, 3), "sole", round(sole_z(side), 3))

# shaft axis in the right-hand BIND frame. The imported pose matrix is not
# the bind matrix; bone.matrix_local is the space the curl and the grip share.
Hinv = arm.data.bones[P + "RightHand"].matrix_local.inverted()
sverts = [Hinv @ v.co.copy() for v in spear.data.vertices]
xorn = max(p.x for p in sverts)
handle = [p for p in sverts if p.x < xorn - 0.35]
hc = sum(handle, Vector()) / len(handle)
hd = Vector((1.0, 0.0, 0.0))
for _ in range(12):
    acc = Vector()
    for pnt in handle:
        d = pnt - hc
        acc += d * d.dot(hd)
    hd = acc.normalized()
if hd.x < 0.0:
    hd = -hd
t = -hc.dot(hd)
SHAFT_LOCAL = hc + hd * t
print("shaft local", tuple(round(c, 4) for c in SHAFT_LOCAL))
if abs(SHAFT_LOCAL.y - 0.061) > 0.03 or abs(SHAFT_LOCAL.z + 0.077) > 0.03:
    raise SystemExit("shaft axis is not the bind grip; refusing to curl the hand")

# right-hand finger curl around that axis. +70 deg closed the hand on the
# shipped mesh without putting triangles through the shaft.
CURL_DEG = 70.0
orig_co = [v.co.copy() for v in body.data.vertices]
gi_rh = groups[P + "RightHand"]
Bhand = arm.data.bones[P + "RightHand"].matrix_local.copy()
Binv = Bhand.inverted()
y0, z0 = SHAFT_LOCAL.y, SHAFT_LOCAL.z
max_move = 0.0
n_moved = 0
for v, co in zip(body.data.vertices, orig_co):
    w = max((g.weight for g in v.groups if g.group == gi_rh), default=0.0)
    if w < 0.3:
        continue
    local = Binv @ co
    tt = max(0.0, min(1.0, (local.y - 0.02) / 0.08))
    tt = tt * tt * (3.0 - 2.0 * tt) * min(1.0, (w - 0.3) / 0.5)
    if tt <= 0.0:
        continue
    yy, zz = local.y - y0, local.z - z0
    ang = math.radians(CURL_DEG) * tt
    cc, ss = math.cos(ang), math.sin(ang)
    local.y = y0 + cc * yy - ss * zz
    local.z = z0 + ss * yy + cc * zz
    new = Bhand @ local
    max_move = max(max_move, (new - co).length)
    v.co = new
    n_moved += 1
body.data.update()
print(f"curl moved {n_moved} verts, max {max_move*100:.1f} cm")
print("MESH AFTER CURL", mesh_hash(body), "WEIGHT AFTER", weight_hash(body))

# left palm centroid in the left-hand frame (open hand, used as the guide)
bpy.context.view_layer.update()
deps = bpy.context.evaluated_depsgraph_get()
be = body.evaluated_get(deps)
bmw = be.matrix_world
lh = arm.pose.bones[P + "LeftHand"]
LHinv = (arm.matrix_world @ lh.matrix).inverted()
gi_lh = groups[P + "LeftHand"]
palm_pts = []
for v in be.data.vertices:
    w = max((g.weight for g in v.groups if g.group == gi_lh), default=0.0)
    if w > 0.5:
        palm_pts.append(LHinv @ (bmw @ v.co))
PALM_L = sum(palm_pts, Vector()) / len(palm_pts)
print("left palm local", tuple(round(c, 4) for c in PALM_L), "n", len(palm_pts))


def joint_head(pb):
    if pb.parent is None:
        return pb.bone.head_local.copy()
    rel = pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local
    return (pb.parent.matrix @ rel).translation.copy()


def set_mat(pb, head, x, y, z):
    pb.matrix = mat_from_axes(x, y, z, head)


PREV_X = {}
PREV_Y = {}
PREV_Q = {}


def pose_bone_y(pb, head, direction, hint):
    key = pb.name
    x, y, z = axes_from_y(direction, PREV_X.get(key, REST_X[key]), hint)
    set_mat(pb, head, x, y, z)
    PREV_X[key] = x.copy()
    return x


def pose_hand(pb, head, x_dir, hint):
    key = pb.name
    x, y, z = axes_from_x(x_dir, PREV_Y.get(key, REST_Y[key]), hint)
    set_mat(pb, head, x, y, z)
    PREV_Y[key] = y.copy()


def pose_hand_clear(pb, head, x_dir, elbow):
    """Twist the grip so the shaft sits off the forearm, not through it."""
    key = pb.name
    base = PREV_Y.get(key, REST_Y[key]).copy()
    best = None
    x_dir = x_dir.normalized()
    prev = PREV_Y.get(key)
    for deg in range(0, 360, 12):
        hint = Matrix.Rotation(math.radians(deg), 3, x_dir) @ base
        x, y, z = axes_from_x(x_dir, hint, UP)
        R = Matrix((x, y, z)).transposed()
        shaft_pt = head + R @ SHAFT_LOCAL
        ab = head - elbow
        denom = max(1e-8, ab.length_squared)
        t = max(0.0, min(1.0, (shaft_pt - elbow).dot(ab) / denom))
        dist = (shaft_pt - (elbow + ab * t)).length
        # stay on the same twist as last frame; a free search flips the wrist
        if prev is not None:
            dist -= y.angle(prev) * 0.55
        if y.z > 0.92:
            dist -= 0.08
        if best is None or dist > best[0]:
            best = (dist, x, y, z)
    set_mat(pb, head, best[1], best[2], best[3])
    PREV_Y[key] = best[2].copy()


def yaw_pitch(rest_m, yaw, pitch):
    R = Matrix.Rotation(pitch, 3, Vector((1.0, 0.0, 0.0))) @ Matrix.Rotation(yaw, 3, UP)
    return R @ rest_m.to_3x3()


def pose_torso(hip_pos, hip_yaw, chest_yaw, chest_pitch, head_yaw, head_pitch):
    hips = arm.pose.bones[P + "Hips"]
    rot = yaw_pitch(REST[hips.name], hip_yaw, 0.0)
    x, y, z = rot.col[0].xyz, rot.col[1].xyz, rot.col[2].xyz
    set_mat(hips, hip_pos, x, y, z)
    bpy.context.view_layer.update()
    spine_names = ["Spine", "Spine1", "Spine2"]
    for i, name in enumerate(spine_names):
        u = (i + 1) / len(spine_names)
        pb = arm.pose.bones[P + name]
        head = joint_head(pb)
        rot = yaw_pitch(REST[pb.name], hip_yaw * (1.0 - u) + chest_yaw * u, chest_pitch * u)
        set_mat(pb, head, rot.col[0].xyz, rot.col[1].xyz, rot.col[2].xyz)
        bpy.context.view_layer.update()
    neck = arm.pose.bones[P + "Neck"]
    head_b = arm.pose.bones[P + "Head"]
    for pb, yaw, pitch in ((neck, (chest_yaw + head_yaw) * 0.5, chest_pitch * 0.5),
                           (head_b, head_yaw, head_pitch)):
        # head yaw is absolute here, already small
        h = joint_head(pb)
        rot = yaw_pitch(REST[pb.name], yaw, pitch)
        set_mat(pb, h, rot.col[0].xyz, rot.col[1].xyz, rot.col[2].xyz)
        bpy.context.view_layer.update()


def pose_leg(side, ankle, lifted):
    thigh = arm.pose.bones[P + side + "UpLeg"]
    shin = arm.pose.bones[P + side + "UpLeg.001"]
    foot = arm.pose.bones[P + side + "UpLeg.002"]
    toe = arm.pose.bones[P + side + "UpLeg.003"]
    bpy.context.view_layer.update()
    root = joint_head(thigh)
    L1 = L_THIGH_L if side == "Left" else L_THIGH_R
    L2 = L_SHIN_L if side == "Left" else L_SHIN_R
    outward = 0.07 if side == "Left" else -0.07
    pole = Vector((root.x + outward, root.y - 0.45, root.z - 0.15))
    target = Vector((ankle.x, ankle.y, ANKLE_Z[side] + (0.08 if lifted else 0.0)))
    knee, target = ik_two(root, target, pole, L1, L2, shorten=0.97)
    pose_bone_y(thigh, root, knee - root, FWD)
    bpy.context.view_layer.update()
    pose_bone_y(shin, knee, target - knee, FWD)
    bpy.context.view_layer.update()
    # the bind foot already plants the sole; keep that orientation
    rot = REST[foot.name].to_3x3()
    if lifted:
        rot = Matrix.Rotation(math.radians(22.0), 3, Vector((1.0, 0.0, 0.0))) @ rot
    set_mat(foot, target, rot.col[0].xyz, rot.col[1].xyz, rot.col[2].xyz)
    bpy.context.view_layer.update()
    foot_y = foot.matrix.col[1].xyz.normalized()
    flen = FOOT_LEN_L if side == "Left" else FOOT_LEN_R
    toe_rot = REST[toe.name].to_3x3()
    if lifted:
        toe_rot = Matrix.Rotation(math.radians(22.0), 3, Vector((1.0, 0.0, 0.0))) @ toe_rot
    set_mat(toe, target + foot_y * flen, toe_rot.col[0].xyz, toe_rot.col[1].xyz, toe_rot.col[2].xyz)
    bpy.context.view_layer.update()


def pose_arm(side, wrist, x_dir, palm_hint):
    upper = arm.pose.bones[P + side + "Arm"]
    fore = arm.pose.bones[P + side + "ForeArm"]
    hand = arm.pose.bones[P + side + "Hand"]
    bpy.context.view_layer.update()
    root = joint_head(upper)
    L1 = L_UP_L if side == "Left" else L_UP_R
    L2 = L_FORE_L if side == "Left" else L_FORE_R
    outward = 0.35 if side == "Left" else -0.35
    # elbow stays in front of the wing root
    pole = Vector((root.x + outward, root.y - 0.28, root.z - 0.12))
    elbow, wrist = ik_two(root, wrist, pole, L1, L2, shorten=0.93)
    pose_bone_y(upper, root, elbow - root, FWD)
    bpy.context.view_layer.update()
    pose_bone_y(fore, elbow, wrist - elbow, FWD)
    bpy.context.view_layer.update()
    if side == "Right":
        pose_hand_clear(hand, wrist, x_dir, elbow)
    else:
        pose_hand(hand, wrist, x_dir, palm_hint)
    bpy.context.view_layer.update()
    return wrist


def reset_prev():
    PREV_X.clear()
    PREV_Y.clear()
    PREV_Q.clear()


def key_all(frame):
    for pb in arm.pose.bones:
        q = pb.rotation_quaternion.copy()
        prev = PREV_Q.get(pb.name)
        if prev is not None and q.dot(prev) < 0.0:
            q.negate()
            pb.rotation_quaternion = q
        PREV_Q[pb.name] = q.copy()
        pb.keyframe_insert(data_path="location", frame=frame)
        pb.keyframe_insert(data_path="rotation_quaternion", frame=frame)
        pb.keyframe_insert(data_path="scale", frame=frame)


def new_action(name):
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = act
    # first insert creates the slot in Blender 5
    return act


def assign_slot():
    act = arm.animation_data.action
    if act.slots:
        arm.animation_data.action_slot = act.slots[0]


# ---------------------------------------------------------------------------
# thrust: upright lunge, feet planted, hips rotate into a two-hand drive
# ---------------------------------------------------------------------------
# hip position, yaw, chest yaw, chest pitch, head yaw
# staff center (a point on the shaft), staff direction (toward the ornament)
THRUST_HIP = [
    (1, Vector((0.00, -0.02, 0.84))),
    (10, Vector((-0.015, 0.02, 0.835))),
    (18, Vector((0.01, -0.05, 0.825))),
    (26, Vector((0.005, -0.04, 0.83))),
    (48, Vector((0.00, -0.02, 0.84))),
]
THRUST_HY = [(1, 0.0), (10, 0.42), (18, -0.32), (26, -0.12), (48, 0.0)]
THRUST_CY = [(1, 0.0), (10, 0.18), (18, -0.10), (26, -0.04), (48, 0.0)]
THRUST_CP = [(1, 0.0), (10, -0.04), (18, 0.08), (26, 0.04), (48, 0.0)]
# The shaft center stays far enough forward that the butt never reaches the wings.
THRUST_C = [
    (1, Vector((-0.04, -0.26, 1.16))),
    (10, Vector((-0.08, -0.14, 1.14))),
    (18, Vector((0.00, -0.42, 1.20))),
    (26, Vector((-0.02, -0.36, 1.18))),
    (48, Vector((-0.04, -0.26, 1.16))),
]
THRUST_D = [
    (1, Vector((0.04, -0.42, 0.90))),
    (10, Vector((0.02, -0.22, 0.97))),
    (18, Vector((0.02, -0.62, 0.78))),
    (26, Vector((0.02, -0.55, 0.83))),
    (48, Vector((0.04, -0.42, 0.90))),
]
# constant planted feet: left lead, right trail, about hip width
FOOT_L = Vector((0.11, -0.18, 0.0))
FOOT_R = Vector((-0.10, 0.06, 0.0))


def norm(v):
    return v.normalized()


BUTT_M = 0.82


HEAD_P = Vector((0.0, -0.02, 1.62))


def clear_line(origin, direction):
    """Staff line misses the head, the right thigh and the floor."""
    D = direction.normalized()
    for _ in range(14):
        bad = False
        for s in (-0.75, -0.4, 0.15, 0.55, 1.0, 1.25):
            p = origin + D * s
            if p.z < 0.07:
                bad = True
            if abs(s) > 0.18 and (p - HEAD_P).length < 0.22:
                bad = True
            if abs(s) > 0.18 and -0.22 < p.x < 0.08 and -0.02 < p.y < 0.22 and 0.2 < p.z < 0.9:
                bad = True
            # wing slab: behind the shoulders, both sides
            if abs(s) > 0.12 and p.y > 0.08 and 0.45 < p.z < 1.7 and abs(p.x) < 0.75:
                bad = True
        if not bad:
            return D
        away = Vector((origin.x - HEAD_P.x, origin.y - HEAD_P.y, 0.0))
        if away.length < 1e-4:
            away = Vector((-1.0, -0.2, 0.0))
        D = (D * 0.45 + away.normalized() * 0.3 + UP * 0.35 + FWD * 0.2).normalized()
    return D


def safe_dir(origin, direction):
    """Point the ornament forward-up and park the butt outside the right thigh."""
    # butt sits to her right, in front of the rear leg, clear of the floor
    butt = Vector((origin.x - 0.38, min(origin.y + 0.02, -0.05), 0.55))
    aimed = (origin - butt)
    if aimed.length < 1e-4:
        aimed = direction
    D = aimed.normalized()
    # keep some of the authored forward lean, but never enough to drop the
    # butt back into the thigh or the wings
    D = (D * 0.72 + direction.normalized() * 0.28).normalized()
    for _ in range(8):
        b = origin - D * BUTT_M
        if b.x < -0.28 and b.y < 0.02 and b.z > 0.08:
            return D
        D = (D * 0.6 + Vector((-0.2, -0.15, 1.0)).normalized() * 0.4).normalized()
    return D


def do_thrust_frame(frame):
    hip = sample_vec(THRUST_HIP, frame)
    hy = sample(THRUST_HY, frame)
    cy = sample(THRUST_CY, frame)
    cp = sample(THRUST_CP, frame)
    head_yaw = max(-0.18, min(0.18, cy * 0.6))
    pose_torso(hip, hy, cy, cp, head_yaw, 0.04)
    C = sample_vec(THRUST_C, frame) + NUDGE
    D = safe_dir(C, sample_vec(THRUST_D, frame))
    # right wrist sits off the shaft by the bind grip offset
    # build a provisional hand rotation so the offset is in world
    x, y, z = axes_from_x(D, PREV_Y.get(P + "RightHand", REST_Y[P + "RightHand"]), UP)
    R = Matrix((x, y, z)).transposed()
    wrist_r = C - D * 0.12 - R @ SHAFT_LOCAL
    pose_arm("Right", wrist_r, D, UP)
    # shaft as it actually sits in the posed right hand
    hw = arm.pose.bones[P + "RightHand"].matrix
    shaft_pt = hw @ SHAFT_LOCAL
    shaft_dir = hw.col[0].xyz.normalized()
    # left guide hand: palm beside the shaft, not driven through it
    shoulder = joint_head(arm.pose.bones[P + "LeftArm"])
    reach = (L_UP_L + L_FORE_L) * 0.90
    away = shaft_pt - Vector((0.0, -0.05, 1.15))
    away = away - shaft_dir * away.dot(shaft_dir)
    if away.length < 1e-4:
        away = Vector((1.0, 0.0, 0.0))
    away.normalize()
    best = None
    for dist in (0.16, 0.22, 0.28):
        for sign in (1.0, -1.0):
            grip = shaft_pt + shaft_dir * dist + away * (0.09 * sign)
            lx, ly, lz = axes_from_x(shaft_dir, PREV_Y.get(P + "LeftHand", REST_Y[P + "LeftHand"]), UP)
            RL = Matrix((lx, ly, lz)).transposed()
            wrist_l = grip - RL @ PALM_L
            gap = (wrist_l - shoulder).length
            # prefer the side that keeps the palm off the chest
            chest_pen = max(0.0, 0.18 - (grip - Vector((0.0, -0.05, 1.15))).length)
            score = (0 if gap <= reach else gap - reach, chest_pen, abs(dist - 0.22))
            if best is None or score < best[0]:
                best = (score, wrist_l, shaft_dir)
    pose_arm("Left", best[1], best[2], UP)
    pose_leg("Left", FOOT_L, False)
    pose_leg("Right", FOOT_R, False)


# ---------------------------------------------------------------------------
# throw: javelin cock, step, whip, the staff leaves and comes back
# ---------------------------------------------------------------------------
THROW_HIP = [
    (1, Vector((0.00, -0.01, 0.845))),
    (12, Vector((-0.02, 0.03, 0.84))),
    (24, Vector((0.015, -0.04, 0.83))),
    (36, Vector((0.01, -0.02, 0.835))),
    (56, Vector((0.00, -0.01, 0.845))),
]
THROW_HY = [(1, 0.0), (12, 0.38), (22, -0.15), (30, -0.05), (56, 0.0)]
THROW_CY = [(1, 0.05), (12, 0.10), (22, -0.22), (32, -0.06), (56, 0.05)]
THROW_CP = [(1, 0.0), (12, -0.06), (24, 0.10), (40, 0.02), (56, 0.0)]
# right wrist and the direction the ornament points while it is in the hand
# The ornament is the long end (+X, ~1.3 m). The butt is the short end
# (~0.8 m). A vertical hold needs the hand high enough that the butt
# stays off the ground, and the throw has to pass BESIDE the head.
THROW_W = [
    (1, Vector((-0.24, -0.04, 1.22))),
    (12, Vector((-0.36, -0.24, 1.46))),
    (22, Vector((-0.28, -0.34, 1.40))),
    (32, Vector((0.06, -0.18, 1.06))),
    (46, Vector((-0.22, -0.06, 1.22))),
    (56, Vector((-0.24, -0.04, 1.22))),
]
THROW_D = [
    (1, Vector((0.08, -0.28, 0.96))),
    (12, Vector((0.18, -0.08, 0.98))),
    (22, Vector((0.0, -0.35, 0.94))),
    (32, Vector((0.10, -0.50, 0.20))),
    (46, Vector((0.08, -0.28, 0.96))),
    (56, Vector((0.08, -0.28, 0.96))),
]
# free left arm, a balance, not on the staff
THROW_LW = [
    (1, Vector((0.28, -0.24, 1.02))),
    (12, Vector((0.14, -0.40, 1.12))),
    (24, Vector((0.04, -0.42, 1.16))),
    (40, Vector((0.24, -0.28, 1.02))),
    (56, Vector((0.28, -0.24, 1.02))),
]
# left foot steps forward and plants; right foot stays
THROW_LF = [
    (1, Vector((0.10, -0.06, 0.0))),
    (8, Vector((0.10, -0.06, 0.0))),
    (16, Vector((0.12, -0.30, 0.0))),
    (56, Vector((0.12, -0.30, 0.0))),
]
THROW_RF = Vector((-0.10, 0.08, 0.0))


def do_throw_frame(frame):
    hip = sample_vec(THROW_HIP, frame)
    pose_torso(
        hip,
        sample(THROW_HY, frame),
        sample(THROW_CY, frame),
        sample(THROW_CP, frame),
        max(-0.18, min(0.18, sample(THROW_CY, frame) * 0.5)),
        0.02,
    )
    wrist = sample_vec(THROW_W, frame) + NUDGE
    D = clear_line(wrist + Vector((-0.02, -0.04, 0.05)), sample_vec(THROW_D, frame))
    pose_arm("Right", wrist, D, Vector((0.0, -0.3, 1.0)))
    ld = norm(sample_vec(THROW_LW, frame) - joint_head(arm.pose.bones[P + "LeftArm"]))
    pose_arm("Left", sample_vec(THROW_LW, frame) + L_NUDGE, ld, UP)
    # step: the left foot is in the air between frame 8 and 16
    lifted = 8 < frame < 16
    pose_leg("Left", sample_vec(THROW_LF, frame), lifted)
    pose_leg("Right", THROW_RF, False)


def skinned_staff_matrix():
    """Rest-space -> world for the spear while it is skinned to the right hand."""
    B_rest = arm.data.bones[P + "RightHand"].matrix_local
    B_pose = arm.pose.bones[P + "RightHand"].matrix
    return B_pose @ B_rest.inverted()


def set_spear_F(F):
    B_rest = arm.data.bones[P + "RightHand"].matrix_local
    B_pose = arm.pose.bones[P + "RightHand"].matrix
    L = B_rest @ B_pose.inverted() @ F
    if spear.parent is arm:
        spear.matrix_world = L
    else:
        spear.matrix_world = L


def key_spear(frame):
    if spear.rotation_mode != "QUATERNION":
        spear.rotation_mode = "QUATERNION"
    if spear.animation_data is None:
        spear.animation_data_create()
    spear.keyframe_insert(data_path="location", frame=frame)
    spear.keyframe_insert(data_path="rotation_quaternion", frame=frame)
    spear.keyframe_insert(data_path="scale", frame=frame)
    ad = spear.animation_data
    if ad.action and ad.action.slots:
        ad.action_slot = ad.action.slots[0]


# ---------------------------------------------------------------------------
# build
# ---------------------------------------------------------------------------
scene = bpy.context.scene
scene.render.fps = 30

# Walk: exact fcurve copy from the shipped clip, played on this rig.
before = {o.name for o in bpy.data.objects}
bpy.ops.import_scene.fbx(filepath=os.path.join(ART, "SERAPHINE_walk.fbx"), **IMPORT)
imported = [bpy.data.objects[n] for n in {o.name for o in bpy.data.objects} - before]
src_arm = next(o for o in imported if o.type == "ARMATURE")
src_act = src_arm.animation_data.action
print("walk action", src_act.name, "range", tuple(src_act.frame_range))
walk_act = src_act.copy()
walk_act.name = "SERAPHINE_walk"
walk_act.use_fake_user = True
# drop the imported duplicate so the export selection is the curled rig
for o in imported:
    bpy.data.objects.remove(o, do_unlink=True)

if arm.animation_data is None:
    arm.animation_data_create()
arm.animation_data.action = walk_act
if walk_act.slots:
    arm.animation_data.action_slot = walk_act.slots[0]
scene.frame_set(int(walk_act.frame_range[0]))
bpy.context.view_layer.update()
print("walk frame0 hand", tuple(round(c, 3) for c in (arm.matrix_world @ arm.pose.bones[P + "RightHand"].head)))

if PHASE == "preview":
    os.makedirs(OUT_PREVIEW, exist_ok=True)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.background_type = "VIEWPORT"
    scene.display.shading.background_color = (0.45, 0.52, 0.38)
    scene.render.resolution_x = 640
    scene.render.resolution_y = 800
    scene.render.image_settings.file_format = "PNG"
    body.color = (0.75, 0.62, 0.48, 1)
    spear.color = (0.72, 0.55, 0.22, 1)
    for w in wings:
        w.color = (0.92, 0.93, 0.90, 1)
    bpy.ops.mesh.primitive_plane_add(size=6, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.color = (0.55, 0.62, 0.40, 1)
    cam_data = bpy.data.cameras.new("cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 3.1
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    def snap(label, frame):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        focus = Vector((0.0, -0.05, 1.05))
        views = {
            "front": focus + Vector((0.0, -3.4, 0.15)),
            "side": focus + Vector((-3.4, 0.0, 0.15)),
        }
        for vn, eye in views.items():
            cam.location = eye
            cam.rotation_euler = (focus - eye).normalized().to_track_quat("-Z", "Y").to_euler()
            scene.render.filepath = os.path.join(OUT_PREVIEW, f"{label}_{vn}.png")
            bpy.ops.render.render(write_still=True)
        hh = arm.matrix_world @ arm.pose.bones[P + "RightHand"].head
        lf = arm.matrix_world @ arm.pose.bones[P + "LeftUpLeg.002"].head
        rf = arm.matrix_world @ arm.pose.bones[P + "RightUpLeg.002"].head
        print(label, "Rhand", tuple(round(c, 2) for c in hh),
              "Lfoot", tuple(round(c, 2) for c in lf),
              "Rfoot", tuple(round(c, 2) for c in rf))

    # grip close-up at rest, before any attack pose
    arm.animation_data.action = None
    scene.frame_set(1)
    bpy.context.view_layer.update()
    cam_data.ortho_scale = 0.55
    hw = arm.matrix_world @ arm.pose.bones[P + "RightHand"].matrix
    focus = hw.translation + hw.col[1].xyz * 0.04
    for vn, off in (("along", hw.col[0].xyz), ("side", -hw.col[2].xyz)):
        eye = focus + off * 0.45
        cam.location = eye
        cam.rotation_euler = (focus - eye).normalized().to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(OUT_PREVIEW, f"grip_{vn}.png")
        bpy.ops.render.render(write_still=True)
    cam_data.ortho_scale = 3.1

    reset_prev()
    new_action("preview_thrust")
    for f in (1, 10, 18, 26, 40, 48):
        do_thrust_frame(f)
        key_all(f)
        assign_slot()
        snap(f"thrust_{f:02d}", f)
    reset_prev()
    new_action("preview_throw")
    for f in (1, 12, 22, 32, 46, 56):
        do_throw_frame(f)
        # flight preview: from frame 22 the spear translates along the release dir
        if f >= 22:
            S = skinned_staff_matrix()
            # use the release pose captured at 22; for later frames translate that
            if f == 22:
                do_throw_frame.__dict__["Srel"] = S.copy()
                set_spear_F(S)
            else:
                D = norm(sample_vec(THROW_D, 22))
                flown = min(2.2, 0.18 * (f - 22))
                set_spear_F(Matrix.Translation(D * flown) @ do_throw_frame.__dict__["Srel"])
        else:
            spear.location = Vector((0, 0, 0))
            spear.rotation_mode = "QUATERNION"
            spear.rotation_quaternion = Quaternion((1, 0, 0, 0))
        key_all(f)
        assign_slot()
        snap(f"throw_{f:02d}", f)
    print("PREVIEW DONE", OUT_PREVIEW)
    sys.exit(0)

# ---------------------------------------------------------------------------
# export phase: bake every frame
# ---------------------------------------------------------------------------
OUT_FBX = "/tmp/redo_fbx"
os.makedirs(OUT_FBX, exist_ok=True)


def mesh_overlaps():
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    be = body.evaluated_get(deps)
    se = spear.evaluated_get(deps)
    bv = [be.matrix_world @ v.co for v in be.data.vertices]
    sv = [se.matrix_world @ v.co for v in se.data.vertices]
    bb = BVHTree.FromPolygons(bv, [p.vertices[:] for p in be.data.polygons])
    sb = BVHTree.FromPolygons(sv, [p.vertices[:] for p in se.data.polygons])
    n = len(sb.overlap(bb))
    for w in wings:
        we = w.evaluated_get(deps)
        wv = [we.matrix_world @ v.co for v in we.data.vertices]
        wb = BVHTree.FromPolygons(wv, [p.vertices[:] for p in we.data.polygons])
        n += len(sb.overlap(wb))
        n += len(wb.overlap(bb))
    return n


def bake_clip(name, frame_end, pose_fn, spear_fn):
    global NUDGE, L_NUDGE, FLY_EXTRA
    reset_prev()
    act = new_action(name)
    spear.animation_data_clear()
    spear.location = Vector((0, 0, 0))
    spear.rotation_mode = "QUATERNION"
    spear.rotation_quaternion = Quaternion((1, 0, 0, 0))
    spear.scale = Vector((1, 1, 1))
    for f in range(1, frame_end + 1):
        NUDGE = Vector((0.0, 0.0, 0.0))
        L_NUDGE = Vector((0.0, 0.0, 0.0))
        FLY_EXTRA = Vector((0.0, 0.0, 0.0))
        saved_x = {k: v.copy() for k, v in PREV_X.items()}
        saved_y = {k: v.copy() for k, v in PREV_Y.items()}
        n = 1
        for attempt in range(8):
            PREV_X.clear()
            PREV_X.update(saved_x)
            PREV_Y.clear()
            PREV_Y.update(saved_y)
            pose_fn(f)
            if spear_fn:
                spear_fn(f)
            n = mesh_overlaps()
            if n == 0:
                break
            # in the hand, step the grip out and forward; in flight, push the spear aside
            if spear_fn is throw_spear and 22 < f < 52:
                FLY_EXTRA += Vector((-0.12, -0.08, 0.06))
            else:
                NUDGE += Vector((-0.04, -0.055, 0.015))
                L_NUDGE += Vector((0.0, -0.05, -0.02))
        if n:
            print(" still overlapping", name, f, n, "after nudge", tuple(round(c, 2) for c in NUDGE))
        key_all(f)
        if f == 1:
            assign_slot()
        if spear_fn:
            key_spear(f)
        if f % 12 == 0 or f == 1:
            print(" baked", name, f, "overlaps", n)
    scene.frame_start = 1
    scene.frame_end = frame_end
    if spear.animation_data and spear.animation_data.action:
        scene.frame_set(min(30, frame_end))
        bpy.context.view_layer.update()
        print(" spear check f30 loc", tuple(round(c, 3) for c in spear.location),
              "action", spear.animation_data.action.name)
    # select the export set
    for o in bpy.data.objects:
        o.select_set(False)
    arm.select_set(True)
    body.select_set(True)
    spear.select_set(True)
    for w in wings:
        w.select_set(True)
    bpy.context.view_layer.objects.active = arm
    path = os.path.join(OUT_FBX, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, **EXPORT)
    print("exported", path, os.path.getsize(path))
    return path


def thrust_spear(frame):
    spear.location = Vector((0, 0, 0))
    spear.rotation_quaternion = Quaternion((1, 0, 0, 0))
    spear.scale = Vector((1, 1, 1))


# release transform captured while baking the throw
RELEASE = {}
NUDGE = Vector((0.0, 0.0, 0.0))
L_NUDGE = Vector((0.0, 0.0, 0.0))
FLY_EXTRA = Vector((0.0, 0.0, 0.0))


def throw_spear(frame):
    # The spear is unparented for this clip (see below). Its matrix IS the
    # rigid pose, so it can leave the hand. In-hand frames use the same
    # rigid transform the skin would have produced.
    S = skinned_staff_matrix()
    if frame < 22:
        F = S
    elif frame == 22:
        RELEASE["S"] = S.copy()
        F = S
    elif frame <= 36:
        u = smooth((frame - 22) / 14.0)
        fly = Vector((-0.15, -1.0, 0.22)).normalized()
        F = Matrix.Translation(fly * (2.6 * u) + FLY_EXTRA) @ RELEASE["S"]
    else:
        u = smooth(min(1.0, (frame - 36) / 14.0))
        fly = Vector((-0.15, -1.0, 0.22)).normalized()
        far = Matrix.Translation(fly * 2.6 + FLY_EXTRA) @ RELEASE["S"]
        q0 = far.to_quaternion()
        q1 = S.to_quaternion()
        if q0.dot(q1) < 0.0:
            q1 = -q1
        loc = far.to_translation().lerp(S.to_translation(), u)
        arc = math.sin(u * math.pi) * Vector((-0.9, -0.25, 0.45)) + FLY_EXTRA
        F = Matrix.Translation(loc + arc) @ q0.slerp(q1, u).to_matrix().to_4x4()
    spear.matrix_world = F


# walk export uses the copied action and the curled mesh, spear unmoved
arm.animation_data.action = walk_act
if walk_act.slots:
    arm.animation_data.action_slot = walk_act.slots[0]
spear.animation_data_clear()
spear.location = Vector((0, 0, 0))
spear.rotation_mode = "QUATERNION"
spear.rotation_quaternion = Quaternion((1, 0, 0, 0))
spear.scale = Vector((1, 1, 1))
w0, w1 = int(round(walk_act.frame_range[0])), int(round(walk_act.frame_range[1]))
scene.frame_start = w0
scene.frame_end = w1
for o in bpy.data.objects:
    o.select_set(False)
arm.select_set(True)
body.select_set(True)
spear.select_set(True)
for w in wings:
    w.select_set(True)
bpy.context.view_layer.objects.active = arm
walk_path = os.path.join(OUT_FBX, "SERAPHINE_walk.fbx")
bpy.ops.export_scene.fbx(filepath=walk_path, **EXPORT)
print("exported walk", walk_path, "frames", w0, w1, os.path.getsize(walk_path))

bake_clip("SERAPHINE_attack_thrust", 48, do_thrust_frame, thrust_spear)
# The FBX exporter does not bake transform keys on objects parented to an
# armature. Detach the spear so the throw can carry it out of the hand.
for mod in list(spear.modifiers):
    spear.modifiers.remove(mod)
spear.parent = None
bake_clip("SERAPHINE_attack_throw", 56, do_throw_frame, throw_spear)
print("BONE HASH AFTER", bone_hash(arm))
print("EXPORT PHASE DONE")
