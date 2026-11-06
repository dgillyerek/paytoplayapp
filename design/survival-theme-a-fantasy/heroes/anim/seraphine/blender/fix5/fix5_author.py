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
IDLE_HAND_R = rh_w.to_3x3()
IDLE_FORE_R = (arm.matrix_world @ arm.pose.bones[P + "RightForeArm"].matrix).to_3x3()
# Every staff vertex in the right-hand bone frame. The posed hand carries the whole mesh.
_inv_hand = rh_w.inverted()
STAFF_LOCAL = [_inv_hand @ v for v in sverts]
# The two geometric ends: extreme vertices along the hand's +X (ornament) and -X (butt).
_proj = [p.x for p in STAFF_LOCAL]
STAFF_ORN_LOCAL = STAFF_LOCAL[_proj.index(max(_proj))]
STAFF_BUTT_LOCAL = STAFF_LOCAL[_proj.index(min(_proj))]

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
# Ornament crosses the FRONT toward her left, so the left palm can meet the
# shaft in front of the breastplate. The butt then falls out to her right,
# in front of the wings, instead of back through them.
# Steep enough that the butt drops in front of the hip, not back into the wings.
# The left palm holds the lower station, toward the butt, at chest height.
# Tip stays to HER right of the face so it does not cross the braid.
THRUST_LINE = Vector((-0.16, -0.46, 0.86))
# Over the throwing shoulder: up and back on HER right, not across the crown.
COCK = Vector((-0.42, 0.62, 0.66))
RELEASE = Vector((0.06, -0.75, 0.62))
FOLLOW = Vector((0.10, -0.68, 0.62))

# Visual yaw: + = pelvis/chest turned to HER right (right shoulder back).
# Converted to local Y (opposite sign) when applied.

def thrust_params(f):
    # Yaw stays small while both feet are still under her (f8-9 was a thigh cross).
    # The big turn happens after the lead foot has stepped out.
    hip_v = Track([
        (1, 0), (8, 6), (14, 10), (18, 32), (22, -8), (26, -4), (36, 0), (52, 0),
    ], [smooth, smooth, smooth, ease_in, ease_out, smooth, smooth]).at(f)
    # Chest comes through after the hips and turns the right shoulder forward,
    # which lays the shaft across the front of the armor for the left palm.
    sh_v = Track([
        (1, 0), (8, 2), (18, 6), (22, -10), (26, -18), (36, -4), (52, 0),
    ], [smooth, smooth, smooth, snap, ease_out, smooth]).at(f)
    hip_pitch = Track([
        (1, 0), (8, 6), (18, 8), (22, 10), (26, 22), (30, 10), (40, 2), (52, 0),
    ], [smooth, smooth, smooth, snap, ease_out, smooth, smooth]).at(f)
    chest_pitch = Track([
        (1, 0), (18, 4), (26, 12), (32, 4), (52, 0),
    ]).at(f)
    hip_roll = Track([
        (1, 0), (8, 6), (18, 4), (26, 0), (52, 0),
    ]).at(f)
    lift = Track([
        (1, 0), (16, 8), (22, 12), (26, 14), (34, 4), (52, 0),
    ]).at(f)
    nod = Track([
        (1, 0), (8, 4), (26, -3), (52, 0),
    ]).at(f)  # + chin down
    head_yaw = Track([
        (1, 0), (18, 3), (26, -2), (52, 0),
    ]).at(f)
    hip_drop = Track([
        (1, 0), (8, 0.05), (18, 0.10), (26, 0.04), (30, 0.07), (40, 0.02), (52, 0),
    ]).at(f)
    # Right hand, offsets from idle until the line is set, then absolute-ish.
    hand = VecTrack([
        (1, IDLE_RH),
        (10, IDLE_RH + Vector((-0.03, 0.02, -0.02))),
        (18, Vector((-0.16, -0.22, 1.16))),
        (22, Vector((-0.12, -0.30, 1.22))),
        (26, Vector((-0.08, -0.38, 1.26))),   # skinned grip, forward and up; left palm is lower on the shaft
        (30, Vector((-0.14, -0.28, 1.18))),
        (40, IDLE_RH + Vector((-0.02, 0.0, 0.02))),
        (52, IDLE_RH),
    ], [smooth, smooth, smooth, snap, ease_out, smooth, smooth]).at(f)
    staff = DirTrack([
        (1, UP),
        (10, Vector((0.0, -0.04, 1.0))),
        (18, THRUST_LINE),
        (30, THRUST_LINE),
        (38, Vector((0.02, -0.06, 1.0))),
        (52, UP),
    ], [smooth, smooth, linear, smooth, smooth]).at(f)
    # Left arm is a counterbalance on HER left. It never reaches across to the staff.
    # Seated from f8 through f36. The on and off ramps are 7 frames, not a hole in the middle.
    guide = Track([
        (1, 0), (8, 1), (36, 1), (52, 0),
    ], [smooth, linear, smooth]).at(f)
    return dict(hip_v=hip_v, sh_v=sh_v, hip_pitch=hip_pitch, chest_pitch=chest_pitch,
                hip_roll=hip_roll, lift=lift, nod=nod, head_yaw=head_yaw,
                hip_drop=hip_drop, hand=hand, staff=staff, guide=guide, throw=False)


def throw_params(f):
    hip_v = Track([
        (1, 0), (12, 14), (20, 26), (28, -4), (36, -2), (46, 0), (54, 0),
    ], [smooth, smooth, ease_in, smooth, smooth, smooth]).at(f)
    sh_v = Track([
        (1, 0), (12, 6), (20, 12), (26, 8), (32, -8), (40, -2), (48, 0), (54, 0),
    ], [smooth, smooth, smooth, ease_in, ease_out, smooth, smooth]).at(f)
    hip_pitch = Track([
        (1, 0), (8, 6), (14, 8), (24, 16), (30, 22), (40, 4), (54, 0),
    ]).at(f)
    chest_pitch = Track([
        (1, 0), (14, 4), (24, 10), (30, 16), (54, 0),
    ]).at(f)
    hip_roll = Track([
        (1, 0), (8, 8), (14, 4), (24, -2), (54, 0),
    ]).at(f)
    lift = Track([
        (1, 0), (10, 12), (14, 26), (22, 24), (30, 8), (42, 0), (54, 0),
    ]).at(f)
    nod = Track([
        (1, 0), (14, -6), (24, 2), (54, 0),
    ]).at(f)  # chin up slightly as the staff rises
    head_yaw = Track([
        (1, 0), (14, 5), (24, -2), (54, 0),
    ]).at(f)
    hip_drop = Track([
        (1, 0), (8, 0.06), (14, 0.13), (18, 0.11), (24, 0.06), (32, 0.04), (54, 0),
    ]).at(f)
    hand = VecTrack([
        (1, IDLE_RH),
        (8, IDLE_RH + Vector((-0.05, 0.00, 0.08))),
        (14, Vector((-0.62, 0.28, 1.46))),    # further behind the right shoulder
        (18, Vector((-0.64, 0.32, 1.50))),    # still cocked while the hips whip
        (22, Vector((-0.48, -0.10, 1.36))),
        (24, Vector((-0.36, -0.42, 1.22))),   # release, forward, still on her right
        (30, Vector((-0.42, -0.16, 1.16))),   # recovery stays on the right, clear of the left wing
        (36, IDLE_RH + Vector((-0.04, -0.02, 0.06))),
        (54, IDLE_RH),
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
        (54, UP),
    ], [smooth, smooth, linear, ease_in, snap, ease_out, smooth, smooth]).at(f)
    # Left arm balance: 0 = idle, 1 = reaching forward, then pulls in.
    guide = Track([
        (1, 0), (8, 0.3), (14, 1), (24, 1), (32, 0.2), (42, 0), (54, 0),
    ]).at(f)
    return dict(hip_v=hip_v, sh_v=sh_v, hip_pitch=hip_pitch, chest_pitch=chest_pitch,
                hip_roll=hip_roll, lift=lift, nod=nod, head_yaw=head_yaw,
                hip_drop=hip_drop, hand=hand, staff=staff, guide=guide, throw=True)


# Foot phases. Ankle XY changes only while the foot is fully in the air.
# A heel-up keeps the toe contact fixed and orbits the ankle around it.
# phase: ("plant", f0, f1, x, y, yaw0, yaw1)
#        ("heel",  f0, f1, x, y, yaw0, yaw1, heel0_deg, heel1_deg)
#        ("air",   f0, f_up, f_down, f1, x0, y0, x1, y1, lift, yaw0, yaw1)
# air: [f0, f_up) lift in place, [f_up, f_down] travel at full height, (f_down, f1] lower in place.


def _lerp(a, b, t):
    return a + (b - a) * t


def _phase_end(p):
    return p[4] if p[0] == "air" else p[2]


def sample_phases(phases, frame, plant_z, foot_info):
    """Return ankle world, foot rotation 3x3, airborne flag, heel degrees."""
    phase = phases[-1]
    for p in phases:
        if frame <= _phase_end(p):
            phase = p
            break
    kind = phase[0]
    idle_R = foot_info["rot"]
    if kind == "plant":
        _, f0, f1, x, y, yaw0, yaw1 = phase
        t = 0 if f1 == f0 else max(0, min(1, (frame - f0) / (f1 - f0)))
        yaw = _lerp(yaw0, yaw1, t)
        R = Matrix.Rotation(math.radians(yaw), 3, "Z") @ idle_R
        return Vector((x, y, plant_z)), R, False, 0.0
    if kind == "heel":
        _, f0, f1, x, y, yaw0, yaw1, h0, h1 = phase
        t = 0 if f1 == f0 else max(0, min(1, (frame - f0) / (f1 - f0)))
        yaw = _lerp(yaw0, yaw1, smooth(t))
        heel = _lerp(h0, h1, smooth(t))
        # (x, y) is where the ankle sits when the sole is flat. The toe contact
        # is forward of that and stays fixed while the heel rises or the foot pivots.
        toe = Vector((x, y, plant_z)) + idle_R @ foot_info["off"]
        toe.z = foot_info["contact_z"]
        ankle, R = orbit_ankle(foot_info, toe.x, toe.y, yaw, heel)
        return ankle, R, False, heel
    # air
    _, f0, f_up, f_down, f1, x0, y0, x1, y1, lift, yaw0, yaw1 = phase
    if frame <= f_up:
        t = 0 if f_up == f0 else (frame - f0) / (f_up - f0)
        z = plant_z + smooth(max(0, min(1, t))) * lift
        x, y = x0, y0
        yaw = yaw0
    elif frame >= f_down:
        t = 0 if f1 == f_down else (frame - f_down) / (f1 - f_down)
        z = plant_z + (1 - smooth(max(0, min(1, t)))) * lift
        x, y = x1, y1
        yaw = yaw1
    else:
        t = 0 if f_down == f_up else (frame - f_up) / (f_down - f_up)
        x = _lerp(x0, x1, smooth(t))
        y = _lerp(y0, y1, smooth(t))
        z = plant_z + lift
        yaw = _lerp(yaw0, yaw1, t)
    R = Matrix.Rotation(math.radians(yaw), 3, "Z") @ idle_R
    return Vector((x, y, z)), R, True, 0.0


def orbit_ankle(info, toe_x, toe_y, yaw_deg, heel_deg):
    """Toe contact stays put. Ankle swings up onto the toe. Sign chosen once at init."""
    contact = Vector((toe_x, toe_y, info["contact_z"]))
    yaw = Matrix.Rotation(math.radians(yaw_deg), 3, "Z")
    pitch = Matrix.Rotation(info["heel_sign"] * math.radians(heel_deg), 3, "X")
    R = yaw @ pitch @ info["rot"]
    ankle = contact - R @ info["off"]
    return ankle, R


# Filled after the idle pose is known.
FOOT = {}


def measure_feet():
    """Toe contact and the ankle offset, plus which pitch sign actually raises the heel."""
    body = bpy.data.objects["SERAPHINE_body"]
    deps = bpy.context.evaluated_depsgraph_get()
    be = body.evaluated_get(deps)
    mw = be.matrix_world
    idx = {vg.name: vg.index for vg in body.vertex_groups}

    def weighted(names):
        out = []
        gis = [idx[n] for n in names if n in idx]
        for v in be.data.vertices:
            w = 0.0
            for g in v.groups:
                if g.group in gis:
                    w += g.weight
            if w > 0.45:
                out.append(mw @ v.co)
        return out

    for side, plant in (("Left", IDLE_LF), ("Right", IDLE_RF)):
        names = [P + side + "UpLeg.002", P + side + "UpLeg.003"]
        vs = weighted(names)
        if len(vs) < 20:
            vs = weighted([P + side + "UpLeg.002"])
        # Toe = the low verts that are farthest forward (-Y). Heel = low verts farthest back.
        low = sorted(vs, key=lambda p: p.z)[: max(30, len(vs) // 5)]
        toe_pt = min(low, key=lambda p: p.y)
        heel_pt = max(low, key=lambda p: p.y)
        rot = idle_world[P + side + "UpLeg.002"].to_3x3()
        ankle = idle_head[P + side + "UpLeg.002"].copy()
        off = rot.inverted() @ (Vector(toe_pt) - ankle)
        info = {
            "rot": rot,
            "off": off,
            "contact_z": float(toe_pt.z),
            "heel_local": rot.inverted() @ (Vector(heel_pt) - ankle),
            "heel_sign": -1.0,
            "toe_rest": Vector(toe_pt),
            "heel_rest": Vector(heel_pt),
        }
        # Pick the pitch sign that lifts the heel point.
        best = None
        for sign in (1.0, -1.0):
            info["heel_sign"] = sign
            ank, R = orbit_ankle(info, toe_pt.x, toe_pt.y, 0.0, 25.0)
            heel_w = ank + R @ info["heel_local"]
            if best is None or heel_w.z > best[0]:
                best = (heel_w.z, sign, ank.z)
        info["heel_sign"] = best[1]
        FOOT[side] = info
        print(f"foot {side} toe z {info['contact_z']:.3f} heel-sign {info['heel_sign']} "
              f"ankle-at-25deg z {best[2]:.3f} heel z {best[0]:.3f}")


measure_feet()

# Which left-hand axis faces the sternum at idle. That is the palm side.
_lh = arm.matrix_world @ arm.pose.bones[P + "LeftHand"].matrix
_inward = Vector((0.0, -0.06, _lh.translation.z)) - _lh.translation
if _inward.length < 1e-4:
    _inward = Vector((-1.0, 0.0, 0.0))
_inward.normalize()
_best = None
for _i in range(3):
    for _s in (1.0, -1.0):
        _d = (_lh.col[_i].xyz.normalized() * _s).dot(_inward)
        if _best is None or _d > _best[0]:
            _best = (_d, _i, _s)
LEFT_PALM_AXIS, LEFT_PALM_SIGN = _best[1], _best[2]
print(f"left palm axis {LEFT_PALM_AXIS} sign {LEFT_PALM_SIGN:.0f} inward-dot {_best[0]:.2f}")


def thrust_left_phases():
    x0, y0 = IDLE_LF.x, IDLE_LF.y
    return [
        ("plant", 1, 8, x0, y0, 0, 0),
        ("heel", 8, 12, x0, y0, 0, 0, 0, 22),
        ("air", 12, 14, 18, 22, x0, y0, 0.12, -0.32, 0.17, 0, 6),
        ("plant", 22, 36, 0.12, -0.32, 6, 4),
        ("air", 36, 38, 42, 46, 0.12, -0.32, x0, y0, 0.17, 4, 0),
        ("plant", 46, 52, x0, y0, 0, 0),
    ]


def thrust_right_phases():
    x0, y0 = IDLE_RF.x, IDLE_RF.y
    return [
        ("plant", 1, 18, x0, y0, 0, 0),
        ("heel", 18, 26, x0, y0, 0, 28, 0, 26),   # toe planted, heel up, foot pivots with the hips
        ("heel", 26, 34, x0, y0, 28, 0, 26, 0),
        ("plant", 34, 52, x0, y0, 0, 0),
    ]


def throw_left_phases():
    x0, y0 = IDLE_LF.x, IDLE_LF.y
    return [
        ("plant", 1, 8, x0, y0, 0, 0),
        ("heel", 8, 11, x0, y0, 0, 0, 0, 18),
        ("air", 11, 13, 17, 20, x0, y0, 0.14, -0.34, 0.17, 0, 4),
        ("plant", 20, 38, 0.14, -0.34, 4, 2),
        ("air", 38, 40, 44, 48, 0.14, -0.34, x0, y0, 0.17, 2, 0),
        ("plant", 48, 54, x0, y0, 0, 0),
    ]


def throw_right_phases():
    x0, y0 = IDLE_RF.x, IDLE_RF.y
    # Recovery step after the release, then a held idle plant on the start mark.
    return [
        ("plant", 1, 22, x0, y0, 0, 0),
        ("heel", 22, 28, x0, y0, 0, 16, 0, 20),
        ("air", 28, 30, 34, 38, x0, y0, -0.06, -0.14, 0.17, 16, 4),
        ("plant", 38, 44, -0.06, -0.14, 4, 2),
        ("air", 44, 46, 49, 52, -0.06, -0.14, x0, y0, 0.17, 2, 0),
        ("plant", 52, 54, x0, y0, 0, 0),
    ]


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

    # Feet. XY of a supporting foot does not change. A step lifts, travels, then plants.
    l_tgt, l_rot, l_air, l_heel = sample_phases(foot_l_keys, frame, IDLE_LF.z, FOOT["Left"])
    r_tgt, r_rot, r_air, r_heel = sample_phases(foot_r_keys, frame, IDLE_RF.z, FOOT["Right"])
    l_arc = (l_tgt.z - IDLE_LF.z) if l_air else 0.0
    r_arc = (r_tgt.z - IDLE_RF.z) if r_air else 0.0

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

    def solve_leg(side, target, foot_rot, L1, L2):
        up = arm.pose.bones[P + side + "UpLeg"]
        shin = arm.pose.bones[P + side + "UpLeg.001"]
        foot = arm.pose.bones[P + side + "UpLeg.002"]
        joint = arm.matrix_world @ up.head
        outward = 0.55 if side == "Left" else -0.55
        flat = Vector((target.x - joint.x, target.y - joint.y, 0))
        if flat.length < 0.05:
            flat = Vector((0, -1, 0))
        flat.normalize()
        # Knees track over the toes and stay apart. This is what kept the thighs
        # crossed at the narrow load (f8-9) when the poles sat too close together.
        pole_dir = (flat * 0.65 + Vector((outward, 0, 0))).normalized()
        pole = joint + pole_dir * 0.75 + Vector((0, 0, 0.05))
        knee, tgt, clamped = ik_two(joint, target, pole, L1, L2)
        if clamped:
            warnings.append(f"f{frame} {side} leg clamped")
        set_world_keep_loc(up, mat_y(knee - joint, (pole - joint)), joint)
        bpy.context.view_layer.update()
        knee_h = arm.matrix_world @ shin.head
        set_world_keep_loc(shin, mat_y(tgt - knee_h, (pole - knee_h)), knee_h)
        bpy.context.view_layer.update()
        ankle = arm.matrix_world @ foot.head
        set_world_keep_loc(foot, foot_rot, ankle)
        bpy.context.view_layer.update()
        return knee_angle(joint, arm.matrix_world @ shin.head, arm.matrix_world @ foot.head), tgt

    lk, l_used = solve_leg("Left", l_tgt, l_rot, L_LEG, L_SHIN)
    rk, r_used = solve_leg("Right", r_tgt, r_rot, R_LEG, R_SHIN)

    # The spear is skinned along the right hand's +X. Idle holds that shaft
    # across the palm, so the forearm is nearly perpendicular to it. Keeping
    # that relationship is what stops the wrist folding backwards. The ornament
    # is 1.30 m and the butt is 0.80 m, so a flat line puts one of those ends
    # through the wings, the cape, or the face. Both attacks stay steep.
    r_sh = arm.matrix_world @ arm.pose.bones[P + "RightArm"].head
    l_sh = arm.matrix_world @ arm.pose.bones[P + "LeftArm"].head
    g = max(0.0, min(1.0, params["guide"]))
    if params["throw"]:
        # Motion target: the hand travels up and BEHIND the right shoulder,
        # staff tilted up-and-back but outboard so the long head misses the
        # wing. Wrist stays the straight across-the-palm grip. Release is a
        # smooth forward turn, not a two-frame snap. Recovery stays on her
        # right and comes down in front of the wing, then to the idle hold.
        cock = Vector((-0.42, 0.36, 0.84)).normalized()
        release = Vector((-0.08, -0.38, 0.92)).normalized()
        # From the posed right shoulder: out to her right, behind the joint, and up.
        # A world-space point further back is outside the arm and the solver
        # was dragging the hand forward, which erased the lay-back.
        cock_hand = r_sh + Vector((-0.30, 0.14, 0.18))
        release_hand = r_sh + Vector((-0.10, -0.28, 0.16))
        recover_hand = r_sh + Vector((-0.22, -0.06, 0.02))
        if frame <= 16:
            u = smooth((frame - 1) / 15.0)
            staff_dir = slerp_dir(UP, cock, u)
            wrist_target = IDLE_RH.lerp(cock_hand, u)
        elif frame <= 24:
            staff_dir = cock.copy()
            wrist_target = cock_hand.copy()
        elif frame <= 34:
            u = smooth((frame - 24) / 10.0)
            staff_dir = slerp_dir(cock, release, u)
            wrist_target = cock_hand.lerp(release_hand, u)
        elif frame <= 46:
            u = smooth((frame - 34) / 12.0)
            staff_dir = slerp_dir(release, UP, u)
            wrist_target = release_hand.lerp(recover_hand, u)
        else:
            u = smooth(min(1.0, (frame - 46) / 8.0))
            staff_dir = UP.copy()
            wrist_target = recover_hand.lerp(IDLE_RH, u)
        elbow_hint = Vector((-0.80, 0.20, 0.30)) if frame < 30 else Vector((-0.45, -0.20, 0.05))
        grip_u = 0.0
    else:
        # Motion target: one shaft. Right palm is the upper grip. Left palm is
        # 22 cm toward the butt, fixed from frame 8 through the drive and the
        # recovery (frame 42). The pair translates together. The line is steep
        # and forward so the butt stays in front of the hip.
        D_LINE = Vector((0.0, -0.16, 0.99)).normalized()
        if frame <= 8:
            grip_u = smooth((frame - 1) / 7.0)
            drive = 0.0
        elif frame <= 20:
            grip_u = 1.0
            drive = smooth((frame - 8) / 12.0) * 0.05
        elif frame <= 32:
            grip_u = 1.0
            drive = 0.05 + smooth((frame - 20) / 12.0) * 0.10
        elif frame <= 42:
            grip_u = 1.0
            drive = 0.15 * (1.0 - smooth((frame - 32) / 10.0))
        else:
            grip_u = max(0.0, 1.0 - smooth(min(1.0, (frame - 42) / 10.0)))
            drive = 0.0
        # Upper grip in front of the chest. The lower palm is 22 cm down the
        # same line, so it stays forward of the breastplate instead of in it.
        rear_pose = Vector((0.08, -0.38, 1.26)) + Vector((0.0, -drive, drive * 0.04))
        staff_dir = slerp_dir(UP, D_LINE, grip_u)
        wrist_target = IDLE_RH.lerp(rear_pose, grip_u)
        elbow_hint = Vector((-0.35, -0.30, -0.35))

    deps = bpy.context.evaluated_depsgraph_get()

    def mesh_bvh(name):
        ob = bpy.data.objects[name]
        ev = ob.evaluated_get(deps)
        vs = [ev.matrix_world @ v.co for v in ev.data.vertices]
        return BVHTree.FromPolygons(vs, [p.vertices[:] for p in ev.data.polygons])

    bvh_body = mesh_bvh("SERAPHINE_body")
    bvh_wing_a = mesh_bvh("SERAPHINE_wing_A")
    bvh_wing_b = mesh_bvh("SERAPHINE_wing_B")
    bvh_cape = mesh_bvh("SERAPHINE_cape")

    def clearance(wrist, hand_R, extra_skip):
        M = hand_R.to_4x4()
        M.translation = wrist
        zmin = 99.0
        best = {"body": 99.0, "wing": 99.0, "cape": 99.0}
        hit = None
        skips = [wrist] + extra_skip
        for p in STAFF_LOCAL[::5]:
            w = M @ p
            if any((w - s).length < 0.13 for s in skips):
                continue
            if w.z < zmin:
                zmin = w.z
            for key, bvh in (("body", bvh_body), ("wing", bvh_wing_a), ("cape", bvh_cape)):
                loc, _n, _i, dist = bvh.find_nearest(w)
                if loc is not None and dist < best[key]:
                    best[key] = dist
                    if hit is None or dist < hit[0]:
                        hit = (dist, w.copy(), key)
            loc, _n, _i, dist = bvh_wing_b.find_nearest(w)
            if loc is not None and dist < best["wing"]:
                best["wing"] = dist
                if hit is None or dist < hit[0]:
                    hit = (dist, w.copy(), "wing")
        best["z"] = zmin
        return best, hit

    def orient_hand(fore, staff):
        """Hand +X on the shaft. Fingers follow the forearm, so the wrist stays straight."""
        x = staff.normalized()
        y = fore - x * fore.dot(x)
        if y.length < 0.2:
            y = x.cross(Vector((0.0, 0.0, 1.0)))
            if y.length < 1e-5:
                y = x.cross(Vector((1.0, 0.0, 0.0)))
        y.normalize()
        z = x.cross(y).normalized()
        return Matrix((x, y, z)).transposed()

    solved = None
    for _nudge in range(6):
        sdir = staff_dir.normalized()
        wrist = wrist_target.copy()
        reach = R_UP + R_FORE - 0.025
        delta = wrist - r_sh
        if delta.length > reach:
            wrist = r_sh + delta.normalized() * reach
        idle_x = IDLE_HAND_R.col[0].xyz.normalized()
        prefer = (idle_x.rotation_difference(sdir).to_matrix() @ IDLE_HAND_R).col[1].xyz
        prefer = prefer - sdir * prefer.dot(sdir)
        if prefer.length < 1e-5:
            prefer = sdir.cross(Vector((0.0, 0.0, 1.0)))
        prefer.normalize()
        best = None
        # Small twists only. A bigger roll is what folded the wrist backwards.
        for deg in range(-35, 36, 5):
            fore = Matrix.Rotation(math.radians(deg), 3, sdir) @ prefer
            elbow = wrist - fore * R_FORE
            gap = abs((elbow - r_sh).length - R_UP)
            cost = gap * 8.0 + abs(deg) / 35.0
            if (elbow - (r_sh + elbow_hint)).length > 0.55:
                cost += 0.25
            if best is None or cost < best[0]:
                best = (cost, fore, elbow, gap, abs(deg))
        fore, elbow, gap, twist = best[1], best[2], best[3], best[4]
        if gap > 0.025:
            elbow = r_sh + (elbow - r_sh).normalized() * max(0.05, R_UP - 0.008)
            wrist = elbow + fore * R_FORE
        if params["throw"] and 12 <= frame <= 26:
            # Do not let the elbow correction drag the cock back in front of the shoulder.
            wrist.y = max(wrist.y, r_sh.y + 0.08)
            wrist.x = min(wrist.x, r_sh.x - 0.16)
        hand_R = orient_hand(fore, sdir)
        seat = wrist - sdir * 0.22
        extra = [seat] if not params["throw"] else []
        c, hit = clearance(wrist, hand_R, extra)
        reach_l = L_UP + L_FORE - 0.02
        left_short = (not params["throw"]) and grip_u > 0.35 and (seat - l_sh).length > reach_l
        mesh_ok = c["z"] > 0.05 and c["body"] > 0.02 and c["wing"] > 0.025 and c["cape"] > 0.02
        ok = mesh_ok and not left_short
        solved = (wrist, sdir, fore, elbow, hand_R, c, ok, twist)
        if ok:
            break
        where = ""
        if hit is not None:
            where = f" @{tuple(round(v, 2) for v in hit[1])} {hit[2]}"
        warnings.append(
            f"f{frame} clear z{c['z']*100:.0f} body{c['body']*100:.0f} "
            f"wing{c['wing']*100:.0f} cape{c['cape']*100:.0f} twist{twist}"
            f"{' left' if left_short else ''}{where}"
        )
        if left_short and mesh_ok:
            # Move the whole shaft toward the left hand. Spacing stays 22 cm.
            wrist_target = wrist + (l_sh - wrist).normalized() * 0.04 + Vector((0.0, -0.02, 0.0))
        elif params["throw"] and 12 <= frame <= 32:
            # Keep the lay-back. Clear the wing by going further out and up.
            staff_dir = slerp_dir(sdir, Vector((sdir.x - 0.25, max(0.18, sdir.y), 1.05)), 0.40)
            wrist_target = wrist + Vector((-0.025, 0.02, 0.02))
        else:
            staff_dir = slerp_dir(sdir, Vector((min(sdir.x, 0.0) - 0.05, sdir.y * 0.6, 1.0)), 0.4)
            if hit is not None and hit[0] < 0.04:
                away = wrist - hit[1]
                if away.length > 1e-4:
                    wrist_target = wrist + away.normalized() * 0.04 + Vector((0.0, -0.02, 0.02))
                else:
                    wrist_target = wrist + Vector((0.0, -0.04, 0.04))
            else:
                wrist_target = wrist + Vector((0.0, -0.03, 0.03))

    wrist_target, staff_dir, fore_y, elbow, hand_R, _c, _ok, _twist = solved
    set_world_keep_loc(arm.pose.bones[P + "RightArm"], mat_y(elbow - r_sh, elbow_hint), r_sh)
    bpy.context.view_layer.update()
    elb_h = arm.matrix_world @ arm.pose.bones[P + "RightForeArm"].head
    set_world_keep_loc(arm.pose.bones[P + "RightForeArm"], mat_y(wrist_target - elb_h, staff_dir), elb_h)
    bpy.context.view_layer.update()
    wrist_h = arm.matrix_world @ arm.pose.bones[P + "RightHand"].head
    elb_h = arm.matrix_world @ arm.pose.bones[P + "RightForeArm"].head
    fy = (wrist_h - elb_h)
    if fy.length < 1e-5:
        fy = fore_y
    fy = fy.normalized()
    hand_R = orient_hand(fy, staff_dir)
    set_world_keep_loc(arm.pose.bones[P + "RightHand"], hand_R, wrist_h)
    bpy.context.view_layer.update()
    staff_dir_now = (arm.matrix_world @ arm.pose.bones[P + "RightHand"].matrix).col[0].xyz.normalized()
    wrist_ang = math.degrees(hand_R.col[1].xyz.normalized().angle(fy))

    # Left hand. Thrust: the point 22 cm toward the tip on the SAME shaft the
    # right hand just posed. It is not a guard and it is not retargeted for
    # four frames. Throw: counterbalance wide on her left, in front, never
    # across the face and never into the right wing.
    if params["throw"]:
        wide = Vector((0.40, -0.34, 0.98))
        if frame <= 16:
            tgt = IDLE_LH.lerp(wide, smooth(frame / 16.0))
        elif frame <= 40:
            tgt = wide
        else:
            tgt = wide.lerp(IDLE_LH, smooth(min(1.0, (frame - 40) / 12.0)))
        palm_at = None
    else:
        onto = Vector((0.20, -0.95, 0.05)).normalized()
        # Lower station, toward the butt. Twenty-two cm up the steep shaft
        # puts this palm in the braid. Down the shaft it stays at the chest.
        on_shaft = wrist_h - staff_dir_now * 0.22
        via = Vector((0.30, -0.42, 1.02))
        if grip_u <= 0.02:
            tgt = IDLE_LH.copy()
            palm_at = None
        elif grip_u < 0.45:
            tgt = IDLE_LH.lerp(via, grip_u / 0.45)
            palm_at = -onto
        elif grip_u < 0.85:
            tgt = via.lerp(on_shaft, (grip_u - 0.45) / 0.40)
            palm_at = -onto
        else:
            tgt = on_shaft
            palm_at = -onto
    if (tgt - IDLE_LH).length > 0.02:
        # Thrust keeps the palm on the shaft even if the arm is straight.
        # Throw may shorten a counterbalance that is past the arm.
        if params["throw"] and (tgt - l_sh).length > L_UP + L_FORE - 0.02:
            tgt = l_sh + (tgt - l_sh).normalized() * (L_UP + L_FORE - 0.02)
        pole_l = Vector((0.42, -0.58, 0.92))
        elbow_l, tgt_used, _ = ik_two(l_sh, tgt, pole_l, L_UP, L_FORE)
        set_world_keep_loc(arm.pose.bones[P + "LeftArm"], mat_y(elbow_l - l_sh, pole_l - l_sh), l_sh)
        bpy.context.view_layer.update()
        elb_l = arm.matrix_world @ arm.pose.bones[P + "LeftForeArm"].head
        set_world_keep_loc(arm.pose.bones[P + "LeftForeArm"], mat_y(tgt_used - elb_l, Vector((0.25, -0.3, -1))), elb_l)
        bpy.context.view_layer.update()
        wh = arm.matrix_world @ arm.pose.bones[P + "LeftHand"].head
        if palm_at is not None and grip_u > 0.4:
            finger_dir = staff_dir_now - palm_at * staff_dir_now.dot(palm_at)
            if finger_dir.length < 1e-4:
                finger_dir = Vector((0, -1, 0))
            finger_dir.normalize()
            cols = [Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))]
            cols[LEFT_PALM_AXIS] = (palm_at * LEFT_PALM_SIGN).normalized()
            finger_axis = 1 if LEFT_PALM_AXIS != 1 else 0
            side = finger_dir - cols[LEFT_PALM_AXIS] * finger_dir.dot(cols[LEFT_PALM_AXIS])
            if side.length < 1e-4:
                side = Vector((0, 0, 1)).cross(cols[LEFT_PALM_AXIS])
            cols[finger_axis] = side.normalized()
            third = 3 - LEFT_PALM_AXIS - finger_axis
            cols[third] = cols[LEFT_PALM_AXIS].cross(cols[finger_axis]).normalized()
            if cols[0].dot(cols[1].cross(cols[2])) < 0:
                cols[third] = -cols[third]
            set_world_keep_loc(arm.pose.bones[P + "LeftHand"], Matrix((cols[0], cols[1], cols[2])).transposed(), wh)
        else:
            finger = wh - elb_l
            if finger.length < 1e-4:
                finger = Vector((0, -1, 0))
            set_world_keep_loc(arm.pose.bones[P + "LeftHand"], mat_y(finger, Vector((0, 0, -1))), wh)
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
        lh = arm.matrix_world @ arm.pose.bones[P + "LeftHand"].head
        staff_dir = (arm.matrix_world @ arm.pose.bones[P + "RightHand"].matrix).col[0].xyz.normalized()
        along = (lh - rh).dot(staff_dir)
        radial = (lh - (rh + staff_dir * along)).length
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
            "l_on_shaft_cm": round(radial * 100, 2),
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


def audit_mesh(name, nframes):
    """Real evaluated verts. Both staff ends, the whole staff, head, left arm, legs, ground."""
    from mathutils.bvhtree import BVHTree
    body = bpy.data.objects["SERAPHINE_body"]
    spear = bpy.data.objects["SERAPHINE_spear"]
    act = bpy.data.actions[name]
    ad.action = act
    ad.action_slot = act.slots[0]
    groups = {vg.name: vg.index for vg in body.vertex_groups}

    def ids(names, thresh=0.4):
        gis = [groups[n] for n in names if n in groups]
        out = []
        for v in body.data.vertices:
            for g in v.groups:
                if g.group in gis and g.weight >= thresh:
                    out.append(v.index)
                    break
        return out

    Pfx = P
    hair_i = ids([Pfx + "Head", Pfx + "HeadTop_End"])
    torso_i = ids([Pfx + "Hips", Pfx + "Spine", Pfx + "Spine1", Pfx + "Spine2"])
    lhand_i = ids([Pfx + "LeftHand"])
    larm_i = ids([Pfx + "LeftArm", Pfx + "LeftForeArm", Pfx + "LeftHand"])
    rleg_i = ids([Pfx + "RightUpLeg", Pfx + "RightUpLeg.001", Pfx + "RightUpLeg.002", Pfx + "RightUpLeg.003"])
    lleg_i = ids([Pfx + "LeftUpLeg", Pfx + "LeftUpLeg.001", Pfx + "LeftUpLeg.002", Pfx + "LeftUpLeg.003"])
    print(f"audit {name} regions hair {len(hair_i)} torso {len(torso_i)} lhand {len(lhand_i)} larm {len(larm_i)} lleg {len(lleg_i)} rleg {len(rleg_i)}")

    def world_of(obj, indices=None):
        deps = bpy.context.evaluated_depsgraph_get()
        e = obj.evaluated_get(deps)
        mw = e.matrix_world
        if indices is None:
            return [mw @ v.co for v in e.data.vertices]
        return [mw @ e.data.vertices[i].co for i in indices]

    def mindist(pts, bvh):
        best = 1e9
        inside = 0
        for p in pts:
            loc, normal, idx, dist = bvh.find_nearest(p)
            if loc is None:
                continue
            if dist < best:
                best = dist
            if dist < 0.012 and normal is not None and (p - loc).dot(normal) < 0:
                inside += 1
        return best, inside

    hits = []
    for f in range(1, nframes + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        staff = world_of(spear)
        zmin = min(p.z for p in staff)
        # Ends: farthest vertices along the hand +X axis.
        hx = (arm.matrix_world @ arm.pose.bones[P + "RightHand"].matrix).col[0].xyz.normalized()
        origin = arm.matrix_world @ arm.pose.bones[P + "RightHand"].head
        projs = [(hx.dot(p - origin), p) for p in staff]
        orn = max(projs, key=lambda t: t[0])[1]
        butt = min(projs, key=lambda t: t[0])[1]
        body_vs = world_of(body)
        def take(indices):
            return [body_vs[i] for i in indices]
        hair, torso, lhand, larm, lleg, rleg = map(take, (hair_i, torso_i, lhand_i, larm_i, lleg_i, rleg_i))
        bvh_torso = BVHTree.FromPolygons(torso, []) if False else None
        # Point clouds: build a BVH from a triangulated dummy by using find via closest brute for small sets
        # Torso/head are large. Use BVHTree.FromPolygons on the full body and filter by region of the nearest... 
        # Faster: BVH of staff for body-region queries, and a body BVH for staff queries.
        deps = bpy.context.evaluated_depsgraph_get()
        be = body.evaluated_get(deps)
        bm = be.matrix_world
        bverts = [bm @ v.co for v in be.data.vertices]
        bpolys = [p.vertices[:] for p in be.data.polygons]
        body_bvh = BVHTree.FromPolygons(bverts, bpolys)
        staff_bvh = BVHTree.FromPolygons(staff, [p.vertices[:] for p in spear.evaluated_get(deps).data.polygons])
        best_wing = 1e9
        best_cape = 1e9
        for wname, slot in (("SERAPHINE_wing_A", "wing"), ("SERAPHINE_wing_B", "wing"), ("SERAPHINE_cape", "cape")):
            we = bpy.data.objects[wname].evaluated_get(deps)
            acc = 1e9
            for i, v in enumerate(we.data.vertices):
                if i % 8:
                    continue
                loc, _n, _i, dist = staff_bvh.find_nearest(we.matrix_world @ v.co)
                if loc is not None and dist < acc:
                    acc = dist
            if slot == "wing":
                best_wing = min(best_wing, acc)
            else:
                best_cape = acc

        def region_vs_bvh(indices, bvh):
            best, inside = 1e9, 0
            for i in indices:
                p = bverts[i]
                loc, normal, idx, dist = bvh.find_nearest(p)
                if loc is None:
                    continue
                if dist < best:
                    best = dist
                if dist < 0.012 and normal is not None and (p - loc).dot(normal) < 0:
                    inside += 1
            return best, inside

        staff_body, staff_in = mindist(staff, body_bvh)
        hair_s, hair_in = region_vs_bvh(hair_i, staff_bvh)
        larm_s, larm_in = region_vs_bvh(larm_i, staff_bvh)
        # left arm vs torso: query left-arm verts against body but ignore hits that are the arm itself
        # Use torso-only point BVH via FromPointCloud if available, else brute nearest on a subsample.
        torso_bvh = BVHTree.FromPolygons(
            [bverts[i] for i in torso_i],
            [],
        ) if False else None
        # Polygons that are mostly torso: skip, use closest torso vertex (subsampled).
        torso_pts = [bverts[i] for i in torso_i[::2]]
        hair_pts = [bverts[i] for i in hair_i[::2]]
        rleg_pts = [bverts[i] for i in rleg_i[::2]]
        lleg_pts = [bverts[i] for i in lleg_i[::2]]

        def cloud_min(src_idx, cloud):
            best, near = 1e9, 0
            for i in src_idx[::3]:
                p = bverts[i]
                d = min((p - c).length for c in cloud[::3]) if False else None
            # KD-free: compare against a BVH built from the cloud as tiny edges is heavy.
            # Use a coarse grid.
            return _grid_min(src_idx, cloud)

        row = {
            "f": f,
            "staff_zmin_cm": round(zmin * 100, 2),
            "butt_z_cm": round(butt.z * 100, 2),
            "orn_z_cm": round(orn.z * 100, 2),
            "butt": [round(c, 3) for c in butt],
            "staff_body_cm": round(staff_body * 100, 2),
            "staff_body_in": staff_in,
            "hair_staff_cm": round(hair_s * 100, 2),
            "hair_in": hair_in,
            "larm_staff_cm": round(larm_s * 100, 2),
            "larm_staff_in": larm_in,
            "wing_cm": round(best_wing * 100, 2),
            "cape_cm": round(best_cape * 100, 2),
        }
        # limb vs limb with a 4cm voxel so a real intersection (dist < 1.5cm) is caught
        def closest(src, dst, limit=0.04):
            # dst bucketed
            buckets = {}
            for p in dst:
                key = (int(p.x / 0.05), int(p.y / 0.05), int(p.z / 0.05))
                buckets.setdefault(key, []).append(p)
            best = 1e9
            close = 0
            for i in src:
                p = bverts[i]
                key = (int(p.x / 0.05), int(p.y / 0.05), int(p.z / 0.05))
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        for dz in (-1, 0, 1):
                            for q in buckets.get((key[0] + dx, key[1] + dy, key[2] + dz), ()):
                                d = (p - q).length
                                if d < best:
                                    best = d
                                if d < limit:
                                    close += 1
            return best, close

        lh_t, lh_tn = closest(lhand_i, torso_pts, 0.02)
        la_t, la_tn = closest([i for i in larm_i if i not in set(lhand_i)], torso_pts, 0.025)
        la_h, la_hn = closest(larm_i, hair_pts, 0.02)
        lh_r, lh_rn = closest(lhand_i, rleg_pts, 0.02)
        # Ignore the crotch, where the two thigh roots are supposed to meet.
        lleg_low = [i for i in lleg_i[::2] if bverts[i].z < 0.72]
        rleg_low = [p for p in rleg_pts if p.z < 0.72]
        ll_r, ll_rn = closest(lleg_low, rleg_low, 0.02)
        row.update({
            "lhand_torso_cm": round(lh_t * 100, 2),
            "lhand_torso_n": lh_tn,
            "larm_torso_cm": round(la_t * 100, 2),
            "larm_torso_n": la_tn,
            "larm_head_cm": round(la_h * 100, 2),
            "larm_head_n": la_hn,
            "lhand_rleg_cm": round(lh_r * 100, 2),
            "lhand_rleg_n": lh_rn,
            "legs_cm": round(ll_r * 100, 2),
            "legs_n": ll_rn,
        })
        bad = (
            zmin < 0.01 or butt.z < 0.01 or orn.z < 0.01
            or hair_in or staff_in > 30
            or lh_tn > 8 or la_tn > 8 or la_hn > 4 or lh_rn > 8 or ll_rn > 12
            or best_wing < 0.02 or best_cape < 0.02
        )
        if bad or f in (8, 9, 14, 15, 16, 17, 18, 19, 20, 21, 27, 30, 32, 34):
            print(
                f"  f{f:02d} zmin {row['staff_zmin_cm']:6.1f} butt {row['butt_z_cm']:6.1f} "
                f"legs {row['legs_cm']:5.1f}/{row['legs_n']} "
                f"LhT {row['lhand_torso_cm']:5.1f}/{row['lhand_torso_n']} "
                f"LaT {row['larm_torso_cm']:5.1f}/{row['larm_torso_n']} "
                f"LaH {row['larm_head_cm']:5.1f}/{row['larm_head_n']} "
                f"LhR {row['lhand_rleg_cm']:5.1f}/{row['lhand_rleg_n']} "
                f"hair {row['hair_staff_cm']:5.1f} wing {row['wing_cm']:5.1f} cape {row['cape_cm']:5.1f}"
            )
        if bad:
            hits.append(row)
    return hits


thrust_rows, thrust_warn = build("attack_thrust", 52, thrust_params, thrust_left_phases(), thrust_right_phases())
throw_rows, throw_warn = build("attack_throw", 54, throw_params, throw_left_phases(), throw_right_phases())


def _both_air(rows, tag):
    """Ankles both clearly up at once. A heel-up keeps the toe down, so it is not counted."""
    bad = []
    for r in rows:
        # Air phases store a raised ankle. Heel-up also raises the ankle, so this
        # only flags a large lift on BOTH feet, which a heel pivot does not do.
        if r["l_foot"][2] > 0.28 and r["r_foot"][2] > 0.30:
            bad.append(r["f"])
    print(tag, "both-feet-high frames", bad)


_both_air(thrust_rows, "thrust")
_both_air(throw_rows, "throw")
for tag, rows in (("thrust", thrust_rows), ("throw", throw_rows)):
    seated = [r for r in rows if 8 <= r["f"] <= (42 if tag == "thrust" else 0)]
    if tag == "thrust":
        worst = max(seated, key=lambda r: r["l_on_shaft_cm"])
        print(f"thrust left-to-shaft cm during seated grip: max {worst['l_on_shaft_cm']} at f{worst['f']}")
print("\n=== mesh audit thrust ===")
thrust_hits = audit_mesh("attack_thrust", 52)
print("thrust bad frames", len(thrust_hits))
print("\n=== mesh audit throw ===")
throw_hits = audit_mesh("attack_throw", 54)
print("throw bad frames", len(throw_hits))

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
