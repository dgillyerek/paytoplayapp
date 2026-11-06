"""Seraphine FIX5 — fresh thrust and throw.

Replaces attack_thrust and attack_throw only. Idle, walk, cape mesh, skeleton,
bind and weights are not edited.

Choreography (30 fps), whole-body keys, not slowed micro-offsets.

Thrust (frames 1-40, hit at frame 20):
  rear-foot load, left lead step, right foot pivots, hips unwind before the
  arm extends, then a short sharp drive and a recoil. Feet return to idle.

Throw (frames 1-48, release at frame 24):
  weight settles onto the right leg, deep contralateral left step, staff rises
  angled over the throwing shoulder (not horizontal over the head), hips then
  torso then arm whip, release, follow-through, trailing right-foot recovery
  step, both feet return to idle.

Run:
  blender -b SERAPHINE.blend --python fix5_author.py -- --out /tmp/sera_fix5
"""
import bpy
import json
import math
import os
import sys
from mathutils import Vector, Matrix, Euler, Quaternion
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = "/tmp/sera_fix5"
for i, a in enumerate(argv):
    if a == "--out" and i + 1 < len(argv):
        OUT = argv[i + 1]
os.makedirs(OUT, exist_ok=True)

FPS = 30
ARM_NAME = "SERAPHINE_rig"
P = "mixamorig:"

# ---------------------------------------------------------------------------
# scene
# ---------------------------------------------------------------------------
arm = bpy.data.objects[ARM_NAME]
scene = bpy.context.scene
scene.render.fps = FPS
scene.frame_set(1)

if arm.animation_data is None:
    arm.animation_data_create()
ad = arm.animation_data
ad.action = bpy.data.actions["idle"]
ad.action_slot = bpy.data.actions["idle"].slots[0]
bpy.context.view_layer.update()

idle_basis = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}
idle_loc = {pb.name: pb.location.copy() for pb in arm.pose.bones}
idle_world = {pb.name: (arm.matrix_world @ pb.matrix).copy() for pb in arm.pose.bones}
idle_head = {pb.name: (arm.matrix_world @ pb.head).copy() for pb in arm.pose.bones}

IDLE_RH = idle_head[P + "RightHand"].copy()
IDLE_LH = idle_head[P + "LeftHand"].copy()
IDLE_HZ = (arm.matrix_world @ arm.pose.bones[P + "Hips"].head).z
IDLE_LF = idle_head[P + "LeftUpLeg.002"].copy()
IDLE_RF = idle_head[P + "RightUpLeg.002"].copy()
L_LEG = (idle_head[P + "LeftUpLeg"] - idle_head[P + "LeftUpLeg.001"]).length
L_SHIN = (idle_head[P + "LeftUpLeg.001"] - idle_head[P + "LeftUpLeg.002"]).length
R_LEG = (idle_head[P + "RightUpLeg"] - idle_head[P + "RightUpLeg.001"]).length
R_SHIN = (idle_head[P + "RightUpLeg.001"] - idle_head[P + "RightUpLeg.002"]).length
R_UP = (idle_head[P + "RightArm"] - idle_head[P + "RightForeArm"]).length
R_FORE = (idle_head[P + "RightForeArm"] - idle_head[P + "RightHand"]).length
L_UP = (idle_head[P + "LeftArm"] - idle_head[P + "LeftForeArm"]).length
L_FORE = (idle_head[P + "LeftForeArm"] - idle_head[P + "LeftHand"]).length
print(f"lengths Lleg {L_LEG:.3f}+{L_SHIN:.3f} Rleg {R_LEG:.3f}+{R_SHIN:.3f} "
      f"Rarm {R_UP:.3f}+{R_FORE:.3f} Larm {L_UP:.3f}+{L_FORE:.3f}")
print(f"idle hips z {IDLE_HZ:.3f} Lfoot {tuple(round(c,3) for c in IDLE_LF)} "
      f"Rfoot {tuple(round(c,3) for c in IDLE_RF)}")
print(f"idle Rhand {tuple(round(c,3) for c in IDLE_RH)}")

# Staff axis in the idle hand: hand local +X is the ornament direction.
# Lengths measured from the deformed spear so the grip stays the skinned one.
spear = bpy.data.objects["SERAPHINE_spear"]
deps = bpy.context.evaluated_depsgraph_get()
spear_e = spear.evaluated_get(deps)
rh_w = arm.matrix_world @ arm.pose.bones[P + "RightHand"].matrix
sverts = [spear_e.matrix_world @ v.co for v in spear_e.data.vertices]
center = sum(sverts, Vector()) / len(sverts)
axis = Vector((0, 0, 1))
for _ in range(16):
    acc = Vector()
    for v in sverts:
        d = v - center
        acc += d * axis.dot(d)
    axis.normalize()
    axis = acc.normalized()
hx = rh_w.col[0].xyz.normalized()
if axis.dot(hx) < 0:
    axis = -axis
# ornament is the end along +hand X
projs = [axis.dot(v - rh_w.translation) for v in sverts]
ORN_LEN = max(projs)  # meters from the hand head toward the ornament
BUTT_LEN = -min(projs)
print(f"staff ornament {ORN_LEN:.3f} m past hand, butt {BUTT_LEN:.3f} m, axis {tuple(round(c,3) for c in axis)}")

# Which local euler on each clavicle raises / brings the arm forward.
def _bone_delta(name, euler_tuple):
    pb = arm.pose.bones[name]
    pb.matrix_basis = idle_basis[name] @ Euler(euler_tuple).to_matrix().to_4x4()
    bpy.context.view_layer.update()
    return (arm.matrix_world @ pb.head) - idle_head[name] if "Shoulder" not in name else (
        (arm.matrix_world @ arm.pose.bones[P + ("RightArm" if "Right" in name else "LeftArm")].head)
        - idle_head[P + ("RightArm" if "Right" in name else "LeftArm")]
    )

def _restore_all():
    for pb in arm.pose.bones:
        pb.matrix_basis = idle_basis[pb.name].copy()
    bpy.context.view_layer.update()

def find_clavicle_axes(side):
    """Return (lift_idx, lift_sign, fwd_idx, fwd_sign) on local XYZ."""
    name = P + side + "Shoulder"
    arm_name = P + side + "Arm"
    best_lift = (0, -1, -1)
    best_fwd = (0, -1, -1)
    for idx in range(3):
        for sign in (1, -1):
            _restore_all()
            e = [0, 0, 0]
            e[idx] = sign * math.radians(20)
            arm.pose.bones[name].matrix_basis = idle_basis[name] @ Euler(e).to_matrix().to_4x4()
            bpy.context.view_layer.update()
            d = (arm.matrix_world @ arm.pose.bones[arm_name].head) - idle_head[arm_name]
            if d.z > best_lift[0]:
                best_lift = (d.z, idx, sign)
            # forward is -Y
            if -d.y > best_fwd[0]:
                best_fwd = (-d.y, idx, sign)
    _restore_all()
    print(f"clavicle {side} lift axis {best_lift} forward {best_fwd}")
    return best_lift, best_fwd

CLAV = {
    "Right": find_clavicle_axes("Right"),
    "Left": find_clavicle_axes("Left"),
}

def head_nod_sign():
    _restore_all()
    top_name = P + "HeadTop_End"
    before = (arm.matrix_world @ arm.pose.bones[top_name].head).copy()
    arm.pose.bones[P + "Head"].matrix_basis = idle_basis[P + "Head"] @ Euler((math.radians(20), 0, 0)).to_matrix().to_4x4()
    bpy.context.view_layer.update()
    after = arm.matrix_world @ arm.pose.bones[top_name].head
    d = after - before
    _restore_all()
    # Positive local X that moves the crown forward (-Y) is a chin-down? Crown moving -Y and down is a forward nod.
    sign = -1.0 if d.y > 0 else 1.0
    print(f"head +X crown delta {tuple(round(c,3) for c in d)} nod sign {sign}")
    return sign

NOD_SIGN = head_nod_sign()

# ---------------------------------------------------------------------------
# tracks
# ---------------------------------------------------------------------------
def ease_in(t):
    return t * t * t

def ease_out(t):
    return 1 - (1 - t) ** 3

def smooth(t):
    return t * t * (3 - 2 * t)

def linear(t):
    return t

def snap(t):
    """Slow then a hard acceleration. Used on the strike."""
    return t ** 4

class Track:
    def __init__(self, keys, eases=None):
        self.keys = keys
        self.eases = eases or [smooth] * (len(keys) - 1)

    def at(self, f):
        if f <= self.keys[0][0]:
            return self.keys[0][1]
        if f >= self.keys[-1][0]:
            return self.keys[-1][1]
        for i in range(len(self.keys) - 1):
            f0, v0 = self.keys[i]
            f1, v1 = self.keys[i + 1]
            if f <= f1:
                t = 0 if f1 == f0 else (f - f0) / (f1 - f0)
                t = self.eases[i](max(0.0, min(1.0, t)))
                return v0 + (v1 - v0) * t
        return self.keys[-1][1]


def slerp_dir(a, b, t):
    a = a.normalized()
    b = b.normalized()
    dot = max(-1.0, min(1.0, a.dot(b)))
    if dot > 0.9995:
        return (a * (1 - t) + b * t).normalized()
    ang = math.acos(dot)
    return ((math.sin((1 - t) * ang) * a + math.sin(t * ang) * b) / math.sin(ang)).normalized()


class DirTrack:
    def __init__(self, keys, eases=None):
        self.keys = [(f, Vector(v).normalized()) for f, v in keys]
        self.eases = eases or [smooth] * (len(keys) - 1)

    def at(self, f):
        if f <= self.keys[0][0]:
            return self.keys[0][1].copy()
        if f >= self.keys[-1][0]:
            return self.keys[-1][1].copy()
        for i in range(len(self.keys) - 1):
            f0, v0 = self.keys[i]
            f1, v1 = self.keys[i + 1]
            if f <= f1:
                t = 0 if f1 == f0 else (f - f0) / (f1 - f0)
                t = self.eases[i](max(0.0, min(1.0, t)))
                return slerp_dir(v0, v1, t)
        return self.keys[-1][1].copy()


class VecTrack:
    def __init__(self, keys, eases=None):
        self.keys = [(f, Vector(v)) for f, v in keys]
        self.eases = eases or [smooth] * (len(keys) - 1)

    def at(self, f):
        if f <= self.keys[0][0]:
            return self.keys[0][1].copy()
        if f >= self.keys[-1][0]:
            return self.keys[-1][1].copy()
        for i in range(len(self.keys) - 1):
            f0, v0 = self.keys[i]
            f1, v1 = self.keys[i + 1]
            if f <= f1:
                t = 0 if f1 == f0 else (f - f0) / (f1 - f0)
                t = self.eases[i](max(0.0, min(1.0, t)))
                return v0.lerp(v1, t)
        return self.keys[-1][1].copy()


UP = Vector((-0.02, -0.05, 1.0))
# Ornament slightly toward her left (positive X) so the short butt stays out
# on her right instead of swinging back through the torso.
# Thrust line ~38° above horizontal, forward.
THRUST_LINE = Vector((0.08, -0.78, 0.60))
# Cock: up and tipped back, not horizontal, butt falls in front of the right hip.
COCK = Vector((0.08, 0.34, 0.94))
RELEASE = Vector((0.06, -0.75, 0.62))
FOLLOW = Vector((0.10, -0.68, 0.62))

# Visual yaw: + = pelvis/chest turned to HER right (right shoulder back).
# Converted to local Y (opposite sign) when applied.

def thrust_params(f):
    hip_v = Track([
        (1, 0), (8, 22), (14, 26), (17, 8), (20, -6), (24, -2), (32, 0), (40, 0),
    ], [smooth, smooth, ease_in, snap, ease_out, smooth, smooth]).at(f)
    # Shoulder visual yaw lags the hips, then comes through on the snap.
    sh_v = Track([
        (1, 0), (8, 6), (14, 8), (17, 10), (20, -4), (24, 0), (32, 0), (40, 0),
    ], [smooth, smooth, smooth, snap, ease_out, smooth, smooth]).at(f)
    hip_pitch = Track([
        (1, 0), (8, 10), (14, 12), (17, 14), (20, 24), (23, 12), (32, 2), (40, 0),
    ], [smooth, smooth, smooth, snap, ease_out, smooth, smooth]).at(f)
    chest_pitch = Track([
        (1, 0), (8, 4), (17, 6), (20, 12), (24, 4), (40, 0),
    ], [smooth, smooth, snap, ease_out, smooth]).at(f)
    hip_roll = Track([
        (1, 0), (8, 10), (14, 6), (20, 0), (40, 0),
    ]).at(f)
    lift = Track([
        (1, 0), (12, 8), (16, 22), (20, 26), (26, 8), (36, 0), (40, 0),
    ], [smooth, smooth, snap, ease_out, smooth, smooth]).at(f)
    nod = Track([
        (1, 0), (8, 6), (20, -4), (40, 0),
    ]).at(f)  # + chin down
    head_yaw = Track([
        (1, 0), (14, 4), (20, -3), (40, 0),
    ]).at(f)
    hip_drop = Track([
        (1, 0), (8, 0.09), (15, 0.12), (20, 0.05), (23, 0.09), (34, 0.02), (40, 0),
    ], [smooth, smooth, snap, ease_out, smooth, smooth]).at(f)
    # Right hand, offsets from idle until the line is set, then absolute-ish.
    hand = VecTrack([
        (1, IDLE_RH),
        (8, IDLE_RH + Vector((-0.04, 0.02, -0.06))),    # load: hand stays right, staff still up
        (14, Vector((-0.50, -0.18, 1.05))),             # chambered on the line, butt outside the hip
        (17, Vector((-0.48, -0.30, 1.10))),
        (20, Vector((-0.44, -0.52, 1.16))),             # snap: hand drives forward, still right of the chest
        (23, Vector((-0.48, -0.36, 1.10))),             # recoil
        (32, IDLE_RH + Vector((-0.03, 0.01, 0.02))),
        (40, IDLE_RH),
    ], [smooth, smooth, smooth, snap, ease_out, smooth, smooth]).at(f)
    staff = DirTrack([
        (1, UP),
        (8, Vector((-0.12, -0.02, 1.0))),               # still up, drifts right
        (14, THRUST_LINE),
        (23, THRUST_LINE),
        (32, Vector((-0.16, -0.08, 1.0))),              # recover up the right side
        (40, UP),
    ], [smooth, smooth, linear, smooth, smooth]).at(f)
    guide = Track([
        (1, 0), (10, 0), (15, 1), (22, 1), (30, 0), (40, 0),
    ], [smooth, smooth, linear, smooth, smooth]).at(f)
    return dict(hip_v=hip_v, sh_v=sh_v, hip_pitch=hip_pitch, chest_pitch=chest_pitch,
                hip_roll=hip_roll, lift=lift, nod=nod, head_yaw=head_yaw,
                hip_drop=hip_drop, hand=hand, staff=staff, guide=guide, throw=False)


def throw_params(f):
    hip_v = Track([
        (1, 0), (8, 16), (14, 32), (18, -6), (24, -8), (32, 0), (48, 0),
    ], [smooth, smooth, ease_in, smooth, smooth, smooth]).at(f)
    sh_v = Track([
        (1, 0), (8, 5), (14, 10), (18, 14), (22, 2), (24, -12), (30, -4), (40, 0), (48, 0),
    ], [smooth, smooth, smooth, ease_in, snap, ease_out, smooth, smooth]).at(f)
    hip_pitch = Track([
        (1, 0), (8, 6), (14, 8), (24, 16), (30, 22), (40, 4), (48, 0),
    ]).at(f)
    chest_pitch = Track([
        (1, 0), (14, 4), (24, 10), (30, 16), (48, 0),
    ]).at(f)
    hip_roll = Track([
        (1, 0), (8, 8), (14, 4), (24, -2), (48, 0),
    ]).at(f)
    lift = Track([
        (1, 0), (10, 12), (14, 26), (22, 24), (30, 8), (42, 0), (48, 0),
    ]).at(f)
    nod = Track([
        (1, 0), (14, -6), (24, 2), (48, 0),
    ]).at(f)  # chin up slightly as the staff rises
    head_yaw = Track([
        (1, 0), (14, 5), (24, -2), (48, 0),
    ]).at(f)
    hip_drop = Track([
        (1, 0), (8, 0.06), (14, 0.13), (18, 0.11), (24, 0.06), (32, 0.04), (48, 0),
    ]).at(f)
    hand = VecTrack([
        (1, IDLE_RH),
        (8, IDLE_RH + Vector((-0.05, 0.00, 0.08))),
        (14, Vector((-0.50, -0.08, 1.48))),   # right of the head, shoulder height
        (18, Vector((-0.52, -0.04, 1.54))),   # still cocked while the hips whip
        (22, Vector((-0.46, -0.28, 1.40))),
        (24, Vector((-0.40, -0.50, 1.26))),   # release, forward and still right of the chest
        (28, Vector((-0.44, -0.46, 1.16))),   # follow-through stays outside the torso
        (36, IDLE_RH + Vector((-0.04, -0.02, 0.06))),
        (48, IDLE_RH),
    ], [smooth, smooth, linear, ease_in, snap, ease_out, smooth, smooth]).at(f)
    staff = DirTrack([
        (1, UP),
        (8, Vector((-0.18, -0.02, 1.0))),
        (14, COCK),
        (18, COCK),
        (22, Vector((-0.12, -0.40, 0.82))),
        (24, RELEASE),
        (28, FOLLOW),
        (36, Vector((-0.20, -0.06, 1.0))),
        (48, UP),
    ], [smooth, smooth, linear, ease_in, snap, ease_out, smooth, smooth]).at(f)
    # Left arm balance: 0 = idle, 1 = reaching forward, then pulls in.
    guide = Track([
        (1, 0), (8, 0.3), (14, 1), (24, 1), (32, 0.2), (42, 0), (48, 0),
    ]).at(f)
    return dict(hip_v=hip_v, sh_v=sh_v, hip_pitch=hip_pitch, chest_pitch=chest_pitch,
                hip_roll=hip_roll, lift=lift, nod=nod, head_yaw=head_yaw,
                hip_drop=hip_drop, hand=hand, staff=staff, guide=guide, throw=True)


# Foot plans. XY changes only across a step phase (airborne). Plant phases hold XY.
# (frame, x, y, yaw_deg, heel_m)  yaw + turns toes toward her left.
THRUST_LFOOT = [
    (1, IDLE_LF.x, IDLE_LF.y, 0, 0),
    (6, IDLE_LF.x, IDLE_LF.y, 0, 0.0),
    (9, IDLE_LF.x, IDLE_LF.y, 0, 0.045),  # heel lifts as the rear foot takes the load
    (15, 0.12, -0.30, 6, 0),              # deep lead plant
    (26, 0.12, -0.30, 4, 0),
    (34, IDLE_LF.x, IDLE_LF.y, 0, 0),
    (40, IDLE_LF.x, IDLE_LF.y, 0, 0),
]
THRUST_RFOOT = [
    (1, IDLE_RF.x, IDLE_RF.y, 0, 0),
    (8, IDLE_RF.x, IDLE_RF.y, 0, 0),
    (14, IDLE_RF.x, IDLE_RF.y, -8, 0.02),
    (20, IDLE_RF.x, IDLE_RF.y, 28, 0.07),   # rear pivot + heel up as hips drive
    (26, IDLE_RF.x, IDLE_RF.y, 10, 0.02),
    (34, IDLE_RF.x, IDLE_RF.y, 0, 0),
    (40, IDLE_RF.x, IDLE_RF.y, 0, 0),
]
THROW_LFOOT = [
    (1, IDLE_LF.x, IDLE_LF.y, 0, 0),
    (7, IDLE_LF.x, IDLE_LF.y, 0, 0.03),     # heel lifts as weight goes to the right
    (14, 0.14, -0.36, 4, 0),                # deep contralateral plant
    (28, 0.14, -0.36, 2, 0),
    (38, IDLE_LF.x, IDLE_LF.y, 0, 0),
    (48, IDLE_LF.x, IDLE_LF.y, 0, 0),
]
THROW_RFOOT = [
    (1, IDLE_RF.x, IDLE_RF.y, 0, 0),
    (14, IDLE_RF.x, IDLE_RF.y, -6, 0.02),
    (18, IDLE_RF.x, IDLE_RF.y, 18, 0.06),   # pivot under the hip whip
    (20, IDLE_RF.x, IDLE_RF.y, 18, 0.06),
    (28, -0.08, -0.20, 8, 0),               # trailing recovery step, forward
    (36, -0.08, -0.20, 4, 0),
    (46, IDLE_RF.x, IDLE_RF.y, 0, 0),
    (48, IDLE_RF.x, IDLE_RF.y, 0, 0),
]


def foot_sample(keys, frame, plant_z, lift_h=0.12):
    if frame <= keys[0][0]:
        f, x, y, yaw, heel = keys[0]
        return Vector((x, y, plant_z + heel)), yaw, 0.0
    if frame >= keys[-1][0]:
        f, x, y, yaw, heel = keys[-1]
        return Vector((x, y, plant_z + heel)), yaw, 0.0
    for i in range(len(keys) - 1):
        f0, x0, y0, yaw0, h0 = keys[i]
        f1, x1, y1, yaw1, h1 = keys[i + 1]
        if frame <= f1:
            t = 0 if f1 == f0 else (frame - f0) / (f1 - f0)
            moving = math.hypot(x1 - x0, y1 - y0) > 0.02
            te = smooth(t)
            x = x0 + (x1 - x0) * te
            y = y0 + (y1 - y0) * te
            yaw = yaw0 + (yaw1 - yaw0) * te
            heel = h0 + (h1 - h0) * te
            arc = math.sin(math.pi * t) * lift_h if moving else 0.0
            z = plant_z + heel + arc
            return Vector((x, y, z)), yaw, arc
    f, x, y, yaw, heel = keys[-1]
    return Vector((x, y, plant_z + heel)), yaw, 0.0


# ---------------------------------------------------------------------------
# solvers
# ---------------------------------------------------------------------------
def basis_from_world(pb, world_mat):
    if pb.parent:
        rest_local = pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local
        return (pb.parent.matrix @ rest_local).inverted() @ world_mat
    return pb.bone.matrix_local.inverted() @ world_mat


def set_world_keep_loc(pb, rot3, head):
    M = rot3.to_4x4()
    M.translation = head
    basis = basis_from_world(pb, M)
    q = basis.to_quaternion()
    out = q.to_matrix().to_4x4()
    out.translation = idle_loc[pb.name]
    pb.rotation_mode = "QUATERNION"
    pb.matrix_basis = out


def apply_local(pb, rx, ry, rz):
    e = Euler((rx, ry, rz))
    pb.matrix_basis = idle_basis[pb.name] @ e.to_matrix().to_4x4()
    pb.matrix_basis.translation = idle_loc[pb.name]


def ik_two(root, target, pole, L1, L2):
    delta = target - root
    dist = delta.length
    hi = L1 + L2 - 0.01
    lo = abs(L1 - L2) + 0.01
    if dist < 1e-5:
        delta = Vector((0, -1, 0))
        dist = 1e-5
    clamped = False
    if dist > hi:
        target = root + delta.normalized() * hi
        dist = hi
        clamped = True
    elif dist < lo:
        target = root + delta.normalized() * lo
        dist = lo
        clamped = True
    n = (target - root).normalized()
    cos_a = (L1 * L1 + dist * dist - L2 * L2) / (2 * L1 * dist)
    cos_a = max(-1.0, min(1.0, cos_a))
    sin_a = math.sqrt(max(0.0, 1 - cos_a * cos_a))
    pv = pole - root
    pv = pv - n * pv.dot(n)
    if pv.length < 1e-5:
        pv = Vector((0, 0, 1)).cross(n)
        if pv.length < 1e-5:
            pv = Vector((1, 0, 0)).cross(n)
    pv.normalize()
    bone1 = (n * cos_a + pv * sin_a).normalized()
    joint = root + bone1 * L1
    return joint, target, clamped


def mat_y(direction, z_hint):
    y = direction.normalized()
    z = z_hint - y * z_hint.dot(y)
    if z.length < 1e-5:
        z = Vector((1, 0, 0)).cross(y)
        if z.length < 1e-5:
            z = Vector((0, 1, 0)).cross(y)
    z.normalize()
    x = y.cross(z).normalized()
    z = x.cross(y).normalized()
    M = Matrix((x, y, z)).transposed()
    return M


def knee_angle(hip, knee, ankle):
    v1 = (hip - knee).normalized()
    v2 = (ankle - knee).normalized()
    return math.degrees(math.acos(max(-1, min(1, v1.dot(v2)))))


def _capsule(p, a, b, radius):
    ab = b - a
    t = (p - a).dot(ab) / max(1e-8, ab.length_squared)
    t = max(0.0, min(1.0, t))
    return (p - (a + ab * t)).length < radius


def _in_box(p, x0, x1, y0, y1, z0, z1):
    return x0 <= p.x <= x1 and y0 <= p.y <= y1 and z0 <= p.z <= z1


def staff_hits_keepout(hand, direction):
    """Coarse volumes: torso, head, thighs, wing slab, upper cape. Grip zone skipped."""
    d = direction.normalized()
    s = -BUTT_LEN
    while s <= ORN_LEN + 1e-6:
        if abs(s) > 0.13:
            p = hand + d * s
            if _capsule(p, Vector((0, -0.02, 0.78)), Vector((0, -0.04, 1.42)), 0.20):
                return True
            if _capsule(p, Vector((0, -0.02, 0.88)), Vector((0, -0.02, 1.02)), 0.22):
                return True
            if (p - Vector((0.0, -0.06, 1.58))).length < 0.15:
                return True
            if _capsule(p, Vector((-0.10, -0.04, 0.86)), Vector((-0.15, -0.02, 0.48)), 0.12):
                return True
            if _capsule(p, Vector((0.10, -0.04, 0.86)), Vector((0.14, 0.00, 0.48)), 0.12):
                return True
            if _in_box(p, -0.78, 0.80, 0.15, 0.45, 0.42, 1.74):
                return True
            if _in_box(p, -0.28, 0.28, 0.10, 0.42, 0.55, 1.40):
                return True
        s += 0.04
    return False


def safest_direction(hand, desired):
    desired = desired.normalized()
    if not staff_hits_keepout(hand, desired):
        return desired, False
    # Fall back toward a right-side vertical, which is the idle-clear family.
    safe = Vector((-0.06, -0.06, 1.0)).normalized()
    if staff_hits_keepout(hand, safe):
        safe = Vector((0.0, -0.02, 1.0)).normalized()
    best = safe
    lo, hi = 0.0, 1.0
    for _ in range(8):
        mid = (lo + hi) * 0.5
        trial = slerp_dir(safe, desired, mid)
        if staff_hits_keepout(hand, trial):
            hi = mid
        else:
            best = trial
            lo = mid
    return best, True


def best_hand_rot(staff_dir, forearm_dir):
    """Hand +X along the staff. Twist around it keeps the wrist near the idle 26° bend."""
    x = staff_dir.normalized()
    ref = Vector((0, 0, 1)).cross(x)
    if ref.length < 1e-4:
        ref = Vector((1, 0, 0)).cross(x)
    ref.normalize()
    best = None
    for deg in range(0, 360, 8):
        z = Matrix.Rotation(math.radians(deg), 3, x) @ ref
        z.normalize()
        y = z.cross(x).normalized()
        ang = math.degrees(y.angle(forearm_dir))
        # Idle bends the wrist ~26° off a straight forearm, hinge shared.
        cost = abs(ang - 26.0)
        # Palm/hinge should not point straight up (broken wrist).
        if z.z > 0.55:
            cost += 25
        # Prefer the hinge somewhat across the body or down so the palm reads inward.
        if z.z < -0.15:
            cost -= 4
        if best is None or cost < best[0]:
            best = (cost, y, z, ang)
    return best[1], best[2], best[3]


def clavicle_euler(side, lift_deg, fwd_deg):
    (lz, li, ls), (fz, fi, fs) = CLAV[side]
    e = [0.0, 0.0, 0.0]
    e[li] += math.radians(lift_deg) * ls
    if fi == li:
        e[fi] += math.radians(fwd_deg) * fs
    else:
        e[fi] += math.radians(fwd_deg) * fs
    return e


def pose_character(params, frame, foot_l_keys, foot_r_keys, warnings):
    for pb in arm.pose.bones:
        pb.matrix_basis = idle_basis[pb.name].copy()

    # Hips: visual yaw + is her right. Local Y + brings the right shoulder forward, so local Y = -visual.
    hip_yaw = math.radians(-params["hip_v"])
    hip_pitch = math.radians(params["hip_pitch"])
    hip_roll = math.radians(params["hip_roll"])
    drop = params["hip_drop"]
    hip_pb = arm.pose.bones[P + "Hips"]
    idle_rot = idle_world[P + "Hips"].to_3x3()
    rot = idle_rot @ Euler((hip_pitch, hip_yaw, hip_roll)).to_matrix()
    head = Vector((idle_world[P + "Hips"].translation.x,
                   idle_world[P + "Hips"].translation.y,
                   IDLE_HZ - drop))
    Mw = rot.to_4x4()
    Mw.translation = head
    basis = hip_pb.bone.matrix_local.inverted() @ Mw
    q = basis.to_quaternion()
    hb = q.to_matrix().to_4x4()
    hb.translation = basis.translation
    hip_pb.matrix_basis = hb
    bpy.context.view_layer.update()

    # Chest visual yaw -> extra local Y on the spine, opposite the hip's contribution.
    # extra local Y = hip_v - sh_v  (see header math). Distribute down the chain.
    extra = math.radians(params["hip_v"] - params["sh_v"])
    splits = (0.45, 0.35, 0.20)
    pitch_x = math.radians(params["chest_pitch"])
    pitch_splits = (0.40, 0.35, 0.25)
    for name, s, ps in zip(("Spine", "Spine1", "Spine2"), splits, pitch_splits):
        apply_local(arm.pose.bones[P + name], pitch_x * ps, extra * s, 0.0)

    # Shoulders broaden before the strike. Forward component opens the arm path.
    lift = params["lift"]
    fwd = lift * 0.35
    for side in ("Right", "Left"):
        e = clavicle_euler(side, lift, fwd if side == "Right" else fwd * 0.5)
        apply_local(arm.pose.bones[P + side + "Shoulder"], *e)

    # Restrained head. Nod sign makes +nod drop the chin.
    nod = math.radians(params["nod"]) * NOD_SIGN
    hy = math.radians(params["head_yaw"])
    apply_local(arm.pose.bones[P + "Neck"], nod * 0.45, hy * 0.35, 0.0)
    apply_local(arm.pose.bones[P + "Head"], nod * 0.55, hy * 0.45, 0.0)
    bpy.context.view_layer.update()

    # Feet.
    l_tgt, l_yaw, l_arc = foot_sample(foot_l_keys, frame, IDLE_LF.z, 0.13 if params["throw"] else 0.12)
    r_tgt, r_yaw, r_arc = foot_sample(foot_r_keys, frame, IDLE_RF.z, 0.12)

    # Drop the pelvis further if a planted-or-reaching foot cannot be reached.
    for _try in range(8):
        lj = arm.matrix_world @ arm.pose.bones[P + "LeftUpLeg"].head
        rj = arm.matrix_world @ arm.pose.bones[P + "RightUpLeg"].head
        need = 0.0
        for joint, tgt, L1, L2 in (
            (lj, l_tgt, L_LEG, L_SHIN),
            (rj, r_tgt, R_LEG, R_SHIN),
        ):
            horiz = math.hypot(tgt.x - joint.x, tgt.y - joint.y)
            vert = tgt.z - joint.z
            reach = L1 + L2 - 0.02
            # vertical gap we would need
            if horiz >= reach:
                need = max(need, 0.04)
            else:
                max_vert = -math.sqrt(max(0.0, reach * reach - horiz * horiz))  # joint.z + max_vert = lowest? 
                # joint is ABOVE the foot, vert = foot.z - joint.z is negative.
                # |vert| must be <= sqrt(reach^2 - horiz^2)
                limit = math.sqrt(max(0.0, reach * reach - horiz * horiz))
                if abs(tgt.z - joint.z) > limit:
                    need = max(need, abs(tgt.z - joint.z) - limit + 0.01)
        if need < 0.005:
            break
        # Lower hips in Z (location along world up). Re-apply by translating basis.
        hip_pb.matrix_basis.translation = hip_pb.matrix_basis.translation + Vector((0, -need, 0))
        # local Y is up on this bone; subtracting Y drops world Z. Verify after update.
        bpy.context.view_layer.update()
        got = (arm.matrix_world @ hip_pb.head).z
        # If local Y wasn't world up, correct using the measured delta.
        want = (arm.matrix_world @ hip_pb.head).z - need
        # direct world fix
        Mw = hip_pb.matrix.copy()
        # hip_pb.matrix is armature space; arm is at identity
        cur = arm.matrix_world @ hip_pb.head
        Mw.translation = Vector((cur.x, cur.y, cur.z - need))
        basis = hip_pb.bone.matrix_local.inverted() @ Mw
        q = basis.to_quaternion()
        hb = q.to_matrix().to_4x4()
        hb.translation = basis.translation
        hip_pb.matrix_basis = hb
        bpy.context.view_layer.update()
    else:
        warnings.append(f"f{frame} reach still short")

    def solve_leg(side, target, yaw, L1, L2):
        up = arm.pose.bones[P + side + "UpLeg"]
        shin = arm.pose.bones[P + side + "UpLeg.001"]
        foot = arm.pose.bones[P + side + "UpLeg.002"]
        joint = arm.matrix_world @ up.head
        outward = 1.0 if side == "Left" else -1.0
        flat = Vector((target.x - joint.x, target.y - joint.y, 0))
        if flat.length < 0.05:
            flat = Vector((0, -1, 0))
        flat.normalize()
        yaw_r = Matrix.Rotation(math.radians(yaw), 3, "Z")
        flat = yaw_r @ flat
        pole_dir = (flat * 0.8 + Vector((outward, 0, 0)) * 0.35).normalized()
        pole = joint + pole_dir * 0.7 + Vector((0, 0, 0.15))
        knee, tgt, clamped = ik_two(joint, target, pole, L1, L2)
        if clamped:
            warnings.append(f"f{frame} {side} leg clamped")
        # thigh
        z_hint = (pole - joint).cross((knee - joint))
        # use pole as the direction the knee should bulge: local hinge
        set_world_keep_loc(up, mat_y(knee - joint, (pole - joint)), joint)
        bpy.context.view_layer.update()
        knee_h = arm.matrix_world @ shin.head
        set_world_keep_loc(shin, mat_y(tgt - knee_h, (pole - knee_h)), knee_h)
        bpy.context.view_layer.update()
        # Foot: idle orientation, yawed, heel pitched so the toe stays down when the heel is up.
        ankle = arm.matrix_world @ foot.head
        idle_f = idle_world[foot.name].to_3x3()
        yaw_m = Matrix.Rotation(math.radians(yaw), 3, "Z")
        # Heel up -> negative pitch around the foot's lateral (world X) tips the toe down
        # relative to a lifted ankle. Scale pitch with the heel lift encoded in target.z over plant.
        heel = max(0.0, target.z - (IDLE_LF.z if side == "Left" else IDLE_RF.z) - 0.02)
        pitch = Matrix.Rotation(-heel * 4.5, 3, "X")
        set_world_keep_loc(foot, yaw_m @ pitch @ idle_f, ankle)
        bpy.context.view_layer.update()
        return knee_angle(joint, arm.matrix_world @ shin.head, arm.matrix_world @ foot.head), tgt

    lk, l_used = solve_leg("Left", l_tgt, l_yaw, L_LEG, L_SHIN)
    rk, r_used = solve_leg("Right", r_tgt, r_yaw, R_LEG, R_SHIN)

    # Arms.
    r_sh = arm.matrix_world @ arm.pose.bones[P + "RightArm"].head
    hand = params["hand"].copy()
    # Keep the hand on the reachable side of the shoulder.
    hand, _ht, hclamp = ik_two(r_sh, hand, r_sh + Vector((-0.3, 0.2, -0.5)), R_UP, R_FORE)
    # ik_two returns the elbow when given those lengths; we only wanted the clamp of the target.
    # Recompute target clamp properly:
    delta = params["hand"] - r_sh
    reach = R_UP + R_FORE - 0.015
    if delta.length > reach:
        hand = r_sh + delta.normalized() * reach
        warnings.append(f"f{frame} right hand clamped {delta.length:.2f}->{reach:.2f}")
    else:
        hand = params["hand"].copy()

    staff_dir, redirected = safest_direction(hand, params["staff"])
    if redirected:
        warnings.append(
            f"f{frame} staff redirected elev {math.degrees(math.asin(max(-1, min(1, staff_dir.z)))):.0f} "
            f"from {math.degrees(math.asin(max(-1, min(1, params['staff'].normalized().z)))):.0f}"
        )
    params["staff"] = staff_dir

    if params["throw"]:
        # Cock: elbow high and back. Release: elbow down.
        u = max(0.0, min(1.0, (frame - 14) / 10.0)) if frame >= 14 else 0.0
        if frame < 14:
            u = 0.0
        pole = r_sh + Vector((-0.55, 0.40, 0.25)).lerp(Vector((-0.28, -0.10, -0.55)), max(0, min(1, (frame - 16) / 8)))
    else:
        pole = r_sh + Vector((-0.40, 0.20, -0.65))
    elbow, hand_used, _ = ik_two(r_sh, hand, pole, R_UP, R_FORE)
    upb = arm.pose.bones[P + "RightArm"]
    set_world_keep_loc(upb, mat_y(elbow - r_sh, pole - r_sh), r_sh)
    bpy.context.view_layer.update()
    elb_h = arm.matrix_world @ arm.pose.bones[P + "RightForeArm"].head
    fore_dir = (hand_used - elb_h)
    if fore_dir.length < 1e-5:
        fore_dir = Vector((0, -1, 0))
    y_h, z_h, wrist_ang = best_hand_rot(params["staff"], fore_dir.normalized())
    # Forearm shares the hinge (hand Z) and points at the wrist.
    set_world_keep_loc(arm.pose.bones[P + "RightForeArm"], mat_y(fore_dir, z_h), elb_h)
    bpy.context.view_layer.update()
    wrist_h = arm.matrix_world @ arm.pose.bones[P + "RightHand"].head
    x_h = params["staff"].normalized()
    y_h = z_h.cross(x_h).normalized()
    z_h = x_h.cross(y_h).normalized()
    hand_m = Matrix((x_h, y_h, z_h)).transposed()
    set_world_keep_loc(arm.pose.bones[P + "RightHand"], hand_m, wrist_h)
    bpy.context.view_layer.update()

    # Left arm.
    l_sh = arm.matrix_world @ arm.pose.bones[P + "LeftArm"].head
    g = max(0.0, min(1.0, params["guide"]))
    if params["throw"]:
        # Balance arm: reaches forward-left, then pulls in. Not on the staff.
        reach_pt = Vector((0.22, -0.48, 1.42))
        pull_pt = Vector((0.10, -0.10, 1.05))
        # guide 1 = reach, falling guide pulls in
        tgt = reach_pt.lerp(pull_pt, max(0.0, min(1.0, (frame - 24) / 8.0))) if frame >= 20 else reach_pt
        if frame < 12:
            tgt = IDLE_LH.lerp(reach_pt, g)
    else:
        # Forward hand on the shaft, chest height, just ahead of the rear grip.
        # Kept off the axis so the open fingers do not stab the mesh.
        staff_dir = params["staff"].normalized()
        shaft_pt = wrist_h + staff_dir * 0.18
        side = staff_dir.cross(Vector((0, 0, 1)))
        if side.length < 1e-4:
            side = Vector((1, 0, 0))
        side.normalize()
        if side.x < 0:
            side = -side
        tgt = shaft_pt + side * 0.08 + Vector((0, -0.02, -0.05))
    tgt = IDLE_LH.lerp(tgt, g)
    if g > 0.04:
        delta = tgt - l_sh
        reach = L_UP + L_FORE - 0.015
        if delta.length > reach:
            tgt = l_sh + delta.normalized() * reach
        pole_l = l_sh + Vector((0.45, 0.05, -0.45))
        elbow_l, tgt_used, _ = ik_two(l_sh, tgt, pole_l, L_UP, L_FORE)
        set_world_keep_loc(arm.pose.bones[P + "LeftArm"], mat_y(elbow_l - l_sh, pole_l - l_sh), l_sh)
        bpy.context.view_layer.update()
        elb_l = arm.matrix_world @ arm.pose.bones[P + "LeftForeArm"].head
        set_world_keep_loc(arm.pose.bones[P + "LeftForeArm"], mat_y(tgt_used - elb_l, Vector((0, -0.2, -1))), elb_l)
        bpy.context.view_layer.update()
        wh = arm.matrix_world @ arm.pose.bones[P + "LeftHand"].head
        finger = wh - elb_l
        if finger.length < 1e-4:
            finger = Vector((0, -1, 0))
        # Fingers continue the forearm. Palm hint points at the shaft / the other wrist.
        set_world_keep_loc(arm.pose.bones[P + "LeftHand"], mat_y(finger, wrist_h - wh), wh)
        bpy.context.view_layer.update()
        if g < 0.999:
            # blend back toward idle so the return is the idle pose, not a second IK solution
            for nm in (P + "LeftArm", P + "LeftForeArm", P + "LeftHand", P + "LeftShoulder"):
                pb = arm.pose.bones[nm]
                q = idle_basis[nm].to_quaternion().slerp(pb.matrix_basis.to_quaternion(), g)
                M = q.to_matrix().to_4x4()
                M.translation = idle_loc[nm]
                pb.matrix_basis = M
            bpy.context.view_layer.update()

    # Toes stay on the idle local pose (parented to the foot).
    for nm in (P + "LeftUpLeg.003", P + "RightUpLeg.003", P + "LeftToe_End"):
        arm.pose.bones[nm].matrix_basis = idle_basis[nm].copy()

    bpy.context.view_layer.update()
    return dict(l_knee=lk, r_knee=rk, l_arc=l_arc, r_arc=r_arc, wrist_ang=wrist_ang,
                l_foot=l_used, r_foot=r_used)


# ---------------------------------------------------------------------------
# actions
# ---------------------------------------------------------------------------
def new_action(name):
    old = bpy.data.actions.get(name)
    if old:
        bpy.data.actions.remove(old)
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    slot = act.slots.new("OBJECT", ARM_NAME)
    layer = act.layers.new("keys")
    layer.strips.new(type="KEYFRAME")
    return act, slot


def key_everything(frame, prev_quats):
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        q = pb.matrix_basis.to_quaternion()
        prev = prev_quats.get(pb.name)
        if prev is not None and q.dot(prev) < 0:
            q = -q
        prev_quats[pb.name] = q.copy()
        pb.rotation_quaternion = q
        pb.location = pb.matrix_basis.translation
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        pb.keyframe_insert("location", frame=frame)


def linearize(act):
    slot = act.slots[0]
    cb = act.layers[0].strips[0].channelbag(slot)
    for fc in cb.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = "LINEAR"


def build(name, nframes, param_fn, lfoot, rfoot):
    print(f"\n=== build {name} {nframes}f ===")
    act, slot = new_action(name)
    ad.action = act
    ad.action_slot = slot
    warnings = []
    prev = {}
    rows = []
    # Force exact idle on the last frame by posing normally then overwriting if needed.
    for f in range(1, nframes + 1):
        if f == 1 or f == nframes:
            for pb in arm.pose.bones:
                pb.matrix_basis = idle_basis[pb.name].copy()
            bpy.context.view_layer.update()
            info = dict(l_knee=0, r_knee=0, l_arc=0, r_arc=0, wrist_ang=0,
                        l_foot=IDLE_LF, r_foot=IDLE_RF)
            # still measure knees from the idle pose
            info["l_knee"] = knee_angle(
                arm.matrix_world @ arm.pose.bones[P + "LeftUpLeg"].head,
                arm.matrix_world @ arm.pose.bones[P + "LeftUpLeg.001"].head,
                arm.matrix_world @ arm.pose.bones[P + "LeftUpLeg.002"].head)
            info["r_knee"] = knee_angle(
                arm.matrix_world @ arm.pose.bones[P + "RightUpLeg"].head,
                arm.matrix_world @ arm.pose.bones[P + "RightUpLeg.001"].head,
                arm.matrix_world @ arm.pose.bones[P + "RightUpLeg.002"].head)
            params = param_fn(f)
        else:
            params = param_fn(f)
            info = pose_character(params, f, lfoot, rfoot, warnings)
        key_everything(f, prev)
        rh = arm.matrix_world @ arm.pose.bones[P + "RightHand"].head
        staff_dir = (arm.matrix_world @ arm.pose.bones[P + "RightHand"].matrix).col[0].xyz.normalized()
        hip_e = arm.pose.bones[P + "Hips"].matrix_basis.to_euler("XYZ")
        rows.append({
            "f": f,
            "hip_z": round((arm.matrix_world @ arm.pose.bones[P + "Hips"].head).z, 3),
            "hip_euler_deg": [round(math.degrees(a), 1) for a in hip_e],
            "l_knee": round(info["l_knee"], 1),
            "r_knee": round(info["r_knee"], 1),
            "l_foot": [round(c, 3) for c in (arm.matrix_world @ arm.pose.bones[P + "LeftUpLeg.002"].head)],
            "r_foot": [round(c, 3) for c in (arm.matrix_world @ arm.pose.bones[P + "RightUpLeg.002"].head)],
            "l_arc": round(info["l_arc"], 3),
            "r_arc": round(info["r_arc"], 3),
            "r_hand": [round(c, 3) for c in rh],
            "staff_dir": [round(c, 3) for c in staff_dir],
            "staff_elev_deg": round(math.degrees(math.asin(max(-1, min(1, staff_dir.z)))), 1),
            "wrist_ang": round(info["wrist_ang"], 1),
        })
    linearize(act)
    # NLA
    replaced = False
    for tr in ad.nla_tracks:
        for st in tr.strips:
            if st.action and st.action.name == name:
                st.action = act
                replaced = True
    if not replaced:
        tr = ad.nla_tracks.new()
        tr.name = name
        tr.mute = True
        tr.strips.new(name, 1, act)
    print(f"warnings {len(warnings)}")
    for w in warnings[:24]:
        print(" ", w)
    return rows, warnings


thrust_rows, thrust_warn = build("attack_thrust", 40, thrust_params, THRUST_LFOOT, THRUST_RFOOT)
throw_rows, throw_warn = build("attack_throw", 48, throw_params, THROW_LFOOT, THROW_RFOOT)

# Put idle back so opening the file is the rest/idle state the README describes.
ad.action = bpy.data.actions["idle"]
ad.action_slot = bpy.data.actions["idle"].slots[0]

with open(os.path.join(OUT, "authored_samples.json"), "w") as fh:
    json.dump({"thrust": thrust_rows, "throw": throw_rows,
               "thrust_warnings": thrust_warn, "throw_warnings": throw_warn,
               "ornament_m": ORN_LEN, "butt_m": BUTT_LEN}, fh, indent=1)

# ---------------------------------------------------------------------------
# export the two clips only
# ---------------------------------------------------------------------------
REPO = "/agent/repos/paytoplayapp"
DESTS = [
    os.path.join(REPO, "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine"),
    os.path.join(REPO, "design/survival-theme-a-fantasy/heroes/anim/seraphine/final"),
]

for o in bpy.data.objects:
    o.select_set(False)
export_names = ["SERAPHINE_rig", "SERAPHINE_body", "SERAPHINE_spear", "SERAPHINE_wing_A", "SERAPHINE_wing_B"]
for n in export_names:
    bpy.data.objects[n].select_set(True)
for n in ("SERAPHINE_spear__attack_thrust", "SERAPHINE_spear__attack_throw"):
    if n in bpy.data.objects:
        bpy.data.objects[n].hide_render = True
        bpy.data.objects[n].hide_viewport = True

bpy.context.view_layer.objects.active = arm


def export_clip(action_name, filename):
    act = bpy.data.actions[action_name]
    ad.action = act
    ad.action_slot = act.slots[0]
    start, end = act.frame_range
    scene.frame_start = int(start)
    scene.frame_end = int(end)
    for dest in DESTS:
        path = os.path.join(dest, filename)
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
            mesh_smooth_type="FACE",
            use_armature_deform_only=False,
            path_mode="AUTO",
        )
        print("exported", path, os.path.getsize(path))


export_clip("attack_thrust", "SERAPHINE_attack_thrust.fbx")
export_clip("attack_throw", "SERAPHINE_attack_throw.fbx")

ad.action = bpy.data.actions["idle"]
ad.action_slot = bpy.data.actions["idle"].slots[0]

blend_path = os.path.join(REPO, "design/survival-theme-a-fantasy/heroes/anim/seraphine/blender/SERAPHINE.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path, copy=False)
print("saved", blend_path)
