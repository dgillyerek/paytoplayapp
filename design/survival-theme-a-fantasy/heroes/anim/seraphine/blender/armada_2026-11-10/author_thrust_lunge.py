"""Rebuild Seraphine's attack_thrust_slash to follow the reference spear thrust GIF.

The reference is 45 frames at 25 fps (1.8 s). This clip is 54 frames at 30 fps,
the same 1.8 s, and every key below is written in reference frame numbers
(g = 0..44) so it can be checked against the GIF frame by frame.

  g 0-3    guard: side-on, left shoulder to the target. The right (rear) hand
           holds the shaft at her right hip with the elbow back; the left
           (front) hand is out in front, well up the shaft; tip raised
  g 4-14   wind-up: spear lowered level at the waist, rear hand drawn back
           past the hip, front hand stays out in front as the guide
  g 15-17  drive: front (left) foot steps long, shoulders square up, both
           arms push the spear forward; the hands keep their grip on the shaft
  g 18-30  full extension at chest height: front arm long, rear hand beside
           her right ribs with the elbow back, tip aimed at her centre line
  g 31-35  recover: both hands carry the spear back to the hip, tip rising
  g 36-44  guard again (loops)

Hands: each wrist is kept as straight on its forearm as the grip allows and
never twisted (straight_wrists picks the hand's roll about the shaft; any
leftover twist goes into the forearm roll in roll_forearms). The staff leaves
both hands on the thumb side (thumbs point at the tip). In both hands the shaft lies ON the palm,
across the base of the fingers with the thumb hooked behind it: its centre line
is PALM_DEPTH out from the palm side of the hand bone and FINGER_REACH along
the fingers (measured on the hand meshes; the shaft is 2 cm in radius and the
palm skin is 1 cm off the bone). The staff mesh is not centred on its pivot, so
NewCharsPreview.ApplyStaffGrip offsets it (StaffThrustLocalX/Y) to put the
shaft's centre line exactly there. The rig has no finger bones, so the fingers
stay open along the shaft; they cannot close around it.

The spear stays parented to the right hand (NewCharsPreview grip 0.42 m up
the shaft), so the right hand never moves on the shaft. The left hand holds
the shaft LEAD metres ahead of it and keeps that grip through the strike (it
gives 8 cm, to LEAD_HIT, at full extension). Hand
targets follow the body: the rear hand is placed from the hips in guard and
from the right shoulder at the hit. Torso
turns are about each spine bone's own axis, so the lunge lean stays pointed
at the target. Feet are IK-planted and the ball of the back foot never moves.
Knees and the right elbow are IK hinges about their local X axis, limited to
natural flexion, so they cannot bend backward or swing up. Pole targets move
with the limbs (elbows below and outside the arm, knees out over the toes) and
calibrate_pole checks the joint really bends toward its pole. The back foot
keeps the original clip's ankle angle (no Copy Rotation, which wrung the ankle
skin) and its IK target is nudged so the ball of the foot stays planted.

Reads the ready pose (frame 0) and lunge pose (frame 24) from the clip in
commit ee783954 for the spine, neck and head shapes:

  git show ee783954:Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine/SERAPHINE_attack_thrust_slash.fbx > src.fbx
  blender --background --factory-startup --python author_thrust_lunge.py -- <out.fbx> src.fbx

Unity: the .meta clip range must be firstFrame 0, lastFrame 53.
"""
import bpy
import math
import sys
from mathutils import Matrix, Quaternion, Vector

ART = r"C:\Development\paytoplayapp\Assets\ThemePack\fantasy_kingdom_a\art\preview_new_chars\seraphine"
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0]
SRC = argv[1]

FIRST, LAST = 0, 53          # FBX frames at 30 fps
REF_LAST = 44                # reference GIF frames at 25 fps
P = "mixamorig:"
V = Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
before = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=SRC, use_anim=True, anim_offset=0.0,
                         automatic_bone_orientation=False, ignore_leaf_bones=False)
imported = [o for o in bpy.data.objects if o not in before]
rig = next(o for o in imported if o.type == "ARMATURE")
src_action = rig.animation_data.action
pbs = rig.pose.bones
MW = rig.matrix_world
MWI = MW.inverted()


def world(name):
    return MW @ pbs[name].matrix


def snapshot(frame):
    scene.frame_set(frame)
    out = {}
    for pb in pbs:
        pb.rotation_mode = "QUATERNION"
        out[pb.name] = (pb.location.copy(), pb.rotation_quaternion.copy(), world(pb.name).copy())
    return out


for pb in pbs:
    pb.rotation_mode = "QUATERNION"
READY = snapshot(0)
LUNGE = snapshot(24)


# ---------------------------------------------------------------- key helpers
def ease(kind, t):
    if kind == "in":        # accelerate into the hit
        return t * t * t
    if kind == "out":       # snap then settle
        return 1 - (1 - t) ** 3
    if kind == "lin":
        return t
    return t * t * (3 - 2 * t)


def track(keys):
    """keys: [(ref_frame, value, ease_into_this_key)]; value is float or Vector."""
    def at(g):
        if g <= keys[0][0]:
            return keys[0][1]
        for (g0, v0, _), (g1, v1, kind) in zip(keys, keys[1:]):
            if g <= g1:
                t = ease(kind, (g - g0) / (g1 - g0))
                return v0.lerp(v1, t) if isinstance(v0, Vector) else v0 + (v1 - v0) * t
        return keys[-1][1]
    return at


lf_ready = READY[P + "LeftUpLeg.002"][2].translation
rf_ready = READY[P + "RightUpLeg.002"][2].translation
rball_ready = READY[P + "RightUpLeg.003"][2].translation
hips_ready = READY[P + "Hips"][2].translation

# Forward (toward the enemy) is -Y. Her right side is -X. Keys are reference frames.
# Spine/neck/head shape: 0 = upright ready, 1 = the lunge lean.
blend_w = track([(0, 0.0, "s"), (3, 0.0, "s"), (14, 0.05, "s"), (18, 1.05, "in"), (22, 1.0, "out"),
                 (30, 0.95, "s"), (34, 0.2, "s"), (36, 0.0, "s"), (44, 0.0, "s")])
# Torso yaw, degrees about each bone's own long axis. Positive turns the chest to
# her left (right shoulder forward); negative turns her side-on, left shoulder first.
twist = track([(0, -62.0, "s"), (3, -62.0, "s"), (8, -72.0, "s"), (14, -75.0, "s"),
               (18, -45.0, "in"), (22, -43.0, "out"), (30, -45.0, "s"), (35, -50.0, "s"),
               (40, -62.0, "s"), (44, -62.0, "s")])
hips_loc = track([(0, hips_ready, "s"), (3, hips_ready, "s"),
                  (8, hips_ready + V((0.0, 0.06, -0.04)), "s"),
                  (14, hips_ready + V((0.0, 0.07, -0.05)), "s"),
                  (15, hips_ready + V((0.0, 0.04, -0.06)), "s"),
                  (18, hips_ready + V((-0.02, -0.34, -0.06)), "in"),
                  (22, hips_ready + V((-0.02, -0.37, -0.07)), "out"),
                  (30, hips_ready + V((-0.02, -0.34, -0.06)), "s"),
                  (35, hips_ready + V((-0.01, -0.18, -0.07)), "s"),
                  (40, hips_ready + V((0.0, -0.01, -0.01)), "s"),
                  (44, hips_ready, "s")])
# Right (rear, spear) hand in guard / wind-up: palm point on the shaft as an
# offset from the hips. -X is the side her chest is turned to, +Y is away from
# the target, so this is beside her right hip, drawn further back in the wind-up.
rhip = track([(0, V((-0.21, 0.13, 0.14)), "s"), (3, V((-0.21, 0.13, 0.14)), "s"),
              (8, V((-0.21, 0.22, 0.13)), "s"), (14, V((-0.21, 0.24, 0.13)), "s"),
              (36, V((-0.21, 0.18, 0.14)), "s"), (40, V((-0.21, 0.13, 0.14)), "s"),
              (44, V((-0.21, 0.13, 0.14)), "s")])
# 0 = rear hand at the hip, 1 = rear hand driven out to full reach.
hit_w = track([(0, 0.0, "s"), (14, 0.0, "s"), (18, 1.0, "in"), (30, 1.0, "s"),
               (36, 0.0, "s"), (44, 0.0, "s")])
# At the hit the right palm sits here relative to the right shoulder: beside her
# right ribs, under the shoulder, as far forward as the left arm's reach allows
# with the hands LEAD apart. fit_hands draws it back if the left arm is short.
HIT_OFFSET = V((-0.07, -0.18, -0.20))
# Spear direction (hand local +Z): raised at guard, level for the thrust.
spear = track([(0, V((0.06, -0.88, 0.47)), "s"), (3, V((0.06, -0.88, 0.45)), "s"),
               (8, V((0.06, -1.0, 0.02)), "s"), (14, V((0.06, -1.0, 0.01)), "s"),
               (18, V((0.20, -1.0, -0.02)), "in"), (30, V((0.20, -1.0, -0.02)), "s"),
               (36, V((0.05, -1.0, 0.02)), "s"), (40, V((0.06, -0.88, 0.45)), "s"),
               (44, V((0.06, -0.88, 0.47)), "s")])
# The left hand holds the shaft the whole time (1 = on the shaft).
on_shaft = track([(0, 1.0, "s"), (44, 1.0, "s")])
# Arms stop short of straight (fraction of arm length) so the forearms meet the
# shaft at an angle and the wrists can stay in line with them.
REACH = 0.93
# How far up the shaft the left hand sits ahead of the right hand, metres.
# The grip is held for the whole clip. The left hand gives only LEAD - LEAD_HIT
# (8 cm) at full extension, so the rear hand is not jammed into her ribs.
LEAD = 0.55
LEAD_HIT = 0.47
lfoot = track([(0, lf_ready, "s"), (15, lf_ready, "s"),
               (16, lf_ready + V((0.0, -0.14, 0.08)), "s"),
               (18, V((lf_ready.x + 0.02, -0.64, lf_ready.z)), "in"),
               (31, V((lf_ready.x + 0.02, -0.64, lf_ready.z)), "s"),
               (34, V((lf_ready.x + 0.01, -0.45, lf_ready.z + 0.07)), "s"),
               (37, lf_ready, "s"), (44, lf_ready, "s")])


# Back foot: as the hips go forward the heel lifts and the foot pivots on the
# ball, so the straight back leg never forces the boot onto its side.
HEEL_LIFT_MAX = 40.0


def heel_lift(g):
    t = (hips_ready.y - hips_loc(g).y) / 0.30
    return HEEL_LIFT_MAX * max(0.0, min(1.0, t))


def back_foot(g, yaw):
    """Ankle position and world rotation for the back foot."""
    axis = Matrix.Rotation(math.radians(yaw), 3, "Z") @ V((1.0, 0.0, 0.0))
    lift = Matrix.Rotation(math.radians(heel_lift(g)), 3, axis)
    turn = Matrix.Rotation(math.radians(yaw), 3, "Z")
    ankle = rball_ready + lift @ (turn @ (rf_ready - rball_ready))
    rot = (lift @ turn).to_quaternion() @ READY[P + "RightUpLeg.002"][2].to_quaternion()
    return ankle, rot


# ---------------------------------------------------------------- body sizes
def leg_len(side):
    up = READY[P + side + "UpLeg"][2].translation
    kn = READY[P + side + "UpLeg.001"][2].translation
    an = READY[P + side + "UpLeg.002"][2].translation
    return (kn - up).length + (an - kn).length


def arm_len(side):
    a = READY[P + side + "Arm"][2].translation
    e = READY[P + side + "ForeArm"][2].translation
    h = READY[P + side + "Hand"][2].translation
    return (e - a).length + (h - e).length


L = {"Left": leg_len("Left"), "Right": leg_len("Right")}
ARM = {"Left": arm_len("Left"), "Right": arm_len("Right")}
hip_off = {s: READY[P + s + "UpLeg"][2].translation - hips_ready for s in ("Left", "Right")}


# ---------------------------------------------------------------- IK setup
def empty(name, loc):
    e = bpy.data.objects.new(name, None)
    scene.collection.objects.link(e)
    e.location = loc
    return e


def ik(bone, target, pole, chain=2):
    c = pbs[bone].constraints.new("IK")
    c.target = target
    c.pole_target = pole
    c.chain_count = chain
    c.use_tail = True
    return c


targets = {}
for key, bone, pole_loc in [
    ("rh", "RightForeArm", V((-0.75, 0.55, 0.55))),
    ("lh", "LeftForeArm", V((0.8, 0.5, 1.0))),
    ("lf", "LeftUpLeg.001", V((0.15, -1.2, 0.55))),
    ("rf", "RightUpLeg.001", V((-0.15, -1.2, 0.55))),
]:
    t = empty("T_" + key, READY[P + bone][2].translation)
    pl = empty("P_" + key, pole_loc)
    targets[key] = (t, pl, ik(P + bone, t, pl))

rot_targets = {}
# Knees and the right elbow are hinges about their local X axis (measured on
# the original clip). Lock the other axes and allow only natural flexion, so
# IK can never bend a knee backward or swing the elbow up.
for bone, lo, hi in (("LeftUpLeg.001", -150.0, -2.0), ("RightUpLeg.001", -150.0, -2.0),
                     ("RightForeArm", 2.0, 150.0)):
    pb = pbs[P + bone]
    pb.lock_ik_y = True
    pb.lock_ik_z = True
    pb.use_ik_limit_x = True
    pb.ik_min_x = math.radians(lo)
    pb.ik_max_x = math.radians(hi)

# The back (right) foot has no Copy Rotation: it keeps the original clip's
# ankle bend relative to the shin, so the ankle skin is never wrung. Its IK
# target is adjusted every frame so the ball of the foot stays planted.
for key, bone in [("rhr", "RightHand"), ("lhr", "LeftHand"), ("lfr", "LeftUpLeg.002")]:
    e = empty("R_" + key, V())
    e.rotation_mode = "QUATERNION"
    e.rotation_quaternion = READY[P + bone][2].to_quaternion()
    c = pbs[P + bone].constraints.new("COPY_ROTATION")
    c.target = e
    rot_targets[key] = e


def bend_toward_pole(key, lower):
    """1 when the elbow/knee points straight at its pole target, -1 when away."""
    upper = lower.parent
    root = MW @ upper.head
    tip = MW @ lower.tail
    mid = MW @ lower.head
    axis = (tip - root).normalized()
    def off(v):
        w = v - root
        return (w - axis * w.dot(axis)).normalized()
    return off(mid).dot(off(targets[key][1].location))


def calibrate_pole(key, lower_bone, child_bone, roll_weight):
    """Pole angle: joint bends toward its pole, chain keeps its rest roll, the
    hand/foot below does not have to twist to reach its target orientation."""
    c = targets[key][2]
    lower = pbs[P + lower_bone]
    upper = lower.parent
    child = pbs[P + child_bone]
    best = None
    for deg in range(-180, 180, 5):
        c.pole_angle = math.radians(deg)
        bpy.context.view_layer.update()
        bend = bend_toward_pole(key, lower)
        roll = 0.0
        for b in (upper, lower):
            q = (MW @ b.matrix).to_quaternion()
            roll += q.rotation_difference(READY[b.name][2].to_quaternion()).angle
        # Evaluated twist of the hand/foot about its own long axis, relative to
        # the original clip's ready pose (matrix_basis does not include the
        # Copy Rotation result, so measure from the posed matrices).
        rest_local = child.bone.parent.matrix_local.inverted() @ child.bone.matrix_local
        pose_local = lower.matrix.inverted() @ child.matrix
        basis = (rest_local.inverted() @ pose_local).to_quaternion()
        rel = READY[child.name][1].inverted() @ basis
        twist = abs(math.atan2(rel.y, rel.w)) * 2
        twist = min(twist, 2 * math.pi - twist)
        score = 4.0 * (1.0 - bend) + roll_weight * roll + 2.0 * twist
        if best is None or score < best[0]:
            best = (score, deg, bend, twist)
    c.pole_angle = math.radians(best[1])
    print("POLE", key, best[1], "bend", round(best[2], 3), "twist deg", round(math.degrees(best[3]), 1))


def pick_pole_angle(key, elbow_bone, want, match_roll=False):
    """Choose the pole angle that puts the elbow/knee nearest `want`.

    With match_roll the chain must also keep the ready pose's roll: the score
    is the rotation from the ready pose for the upper and lower bone. Matching
    the knee position alone can pick a solution with the shin rolled half a
    turn, which wrings the ankle skin even though the foot itself looks right.
    """
    c = targets[key][2]
    lower = pbs[P + elbow_bone]
    upper = lower.parent
    best = None
    for deg in range(-180, 180, 5):
        c.pole_angle = math.radians(deg)
        bpy.context.view_layer.update()
        if match_roll:
            err = 0.0
            for b in (upper, lower):
                q = (MW @ b.matrix).to_quaternion()
                err += q.rotation_difference(READY[b.name][2].to_quaternion()).angle
        else:
            err = ((MW @ lower.head) - want).length
        if best is None or err < best[0]:
            best = (err, deg)
    c.pole_angle = math.radians(best[1])
    print("POLE", key, best[1], "err", round(best[0], 3))


# ---------------------------------------------------------------- hands
# Hand bones: +Y runs to the fingertips. The palm faces +X on the left hand and
# -X on the right (the thumbs sit on that side of each mesh). The shaft's centre
# line sits PALM_DEPTH out from the bone on the palm side (1 cm of palm + the
# 2 cm shaft radius) and FINGER_REACH along the fingers, just past the thumb tip.
FINGER_REACH, PALM_DEPTH = 0.060, 0.030
# The staff mesh's shaft centre line, off the mesh pivot, in the mesh's x/y.
STAFF_AXIS_X, STAFF_AXIS_Y = -0.0635, 0.0547
STAFF_GRIP = 0.42   # NewCharsPreview StaffThrustGripMetres


def axes_from(y_want, palm_want):
    y = y_want.normalized()
    x = (palm_want - y * y.dot(palm_want)).normalized()
    return x, y, x.cross(y)


def shaft_axes(d):
    """Overhand grip: back of the hand up, fingers draped over the far side."""
    d = d.normalized()
    y = V((-0.35, 0.0, -0.94))
    y = (y - d * d.dot(y)).normalized()
    x = d.cross(y)
    if x.dot(V((-0.94, 0.0, 0.35))) < 0:
        x = -x
    return x, y, x.cross(y)


def hip_axes(yaw):
    """Hand on the back of the left hip: fingers down and forward, palm on the hip."""
    r = Matrix.Rotation(math.radians(yaw), 3, "Z")
    return axes_from(r @ V((0.15, -0.35, -0.92)), r @ V((-1.0, 0.0, 0.0)))


def quat(axes):
    x, y, z = axes
    return Matrix((x, y, z)).transposed().to_quaternion()


def grip_offset(d):
    x, y, _ = shaft_axes(d)
    return -y * FINGER_REACH - x * PALM_DEPTH


def hip_hand(yaw):
    """Wrist target for the hand on the hip, following the pelvis."""
    hips = MW @ pbs[P + "Hips"].head
    r = Matrix.Rotation(math.radians(yaw), 3, "Z")
    return hips + r @ V((0.19, 0.07, 0.11))


def fit_hands(s, rh_want, d):
    """Right wrist within reach; left wrist on the shaft within reach (if gripping)."""
    rs = MW @ pbs[P + "RightArm"].head
    ls = MW @ pbs[P + "LeftArm"].head
    reach_r, reach_l = REACH * ARM["Right"], REACH * ARM["Left"]
    rh = rh_want.copy()
    if (rh - rs).length > reach_r:
        rh = rs + (rh - rs).normalized() * reach_r

    def left_at(r):
        return r + d * s + grip_offset(d)

    # The grip is fixed: if the left hand cannot reach, draw both hands back
    # along the shaft line (the right arm bends), then toward the left shoulder.
    for _ in range(80):
        if (left_at(rh) - ls).length <= reach_l:
            break
        back = rh - d * 0.01
        if (left_at(back) - ls).length < (left_at(rh) - ls).length and (back - rs).length <= reach_r:
            rh = back
            continue
        near = left_at(rh) - d * (left_at(rh) - ls).dot(d)
        rh = rh + (ls - near) * 0.06
        if (rh - rs).length > reach_r:
            rh = rs + (rh - rs).normalized() * reach_r
    return rh, left_at(rh)


# ---------------------------------------------------------------- author
new = bpy.data.actions.new("attack_thrust_slash")
rig.animation_data.action = new
if hasattr(new, "slots") and hasattr(rig.animation_data, "action_slot"):
    rig.animation_data.action_slot = new.slots.new("OBJECT", rig.name)

FK = [pb.name for pb in pbs if pb.name not in {
    P + "RightArm", P + "RightForeArm", P + "LeftArm", P + "LeftForeArm",
    P + "LeftUpLeg", P + "LeftUpLeg.001", P + "RightUpLeg", P + "RightUpLeg.001"}]


def turn_local(name, deg):
    """Yaw a bone about its own long (Y) axis, signed so + turns the chest to her left."""
    pb = pbs[name]
    up = (MW.to_3x3() @ pb.matrix.to_3x3()).col[1]
    sign = 1.0 if up.z >= 0 else -1.0
    pb.rotation_quaternion = pb.rotation_quaternion @ Quaternion((0, 1, 0), math.radians(deg) * sign)
    bpy.context.view_layer.update()


def hand_rot(d):
    z = d.normalized()
    x = (V((-1.0, 0.0, 0.0)) - z * z.x).normalized()
    return Matrix((x, z.cross(x), z)).transposed().to_quaternion()


def ref(frame):
    return frame * REF_LAST / LAST


def forearm_dir(side):
    fa = pbs[P + side + "ForeArm"]
    return ((MW @ fa.tail) - (MW @ fa.head)).normalized()


def forearm_x(side):
    return (MW.to_3x3() @ pbs[P + side + "ForeArm"].matrix.to_3x3()).col[0].normalized()


# Forearm rotation a person has, measured as the hand's twist from the rest pose
# (arms out, palms down). From there the palm can turn up through about 140 deg
# (supination) but barely the other way. Supination is + twist on the right hand
# and - twist on the left (thumbs are on +Z, palms on -X / +X).
SUPINATE = {"Right": 1.0, "Left": -1.0}
SUP_MIN, SUP_MAX = -10.0, 140.0
# A person holds a pole diagonally across the palm (a hammer grip), not square
# to the fingers. The left hand may tilt by up to 35 degrees either way, frame
# by frame. The right hand has one fixed tilt for the whole clip, RIGHT_TILT,
# because the staff is parented to it at one fixed angle; NewCharsPreview turns
# the staff by the same angle about the hand's X axis.
import os
RIGHT_TILT = float(os.environ.get("RIGHT_TILT", "35"))
TILTS = {"Right": (RIGHT_TILT,), "Left": tuple(range(-35, 36, 5))}


def wrist_cost(side, hand_rot3):
    """Wrist bend (deg) of a candidate hand orientation vs its forearm, plus a
    steep penalty for forearm rotation outside what a person can do."""
    fa = pbs[P + side + "ForeArm"]
    hb = pbs[P + side + "Hand"].bone
    fa_w = (MW.to_3x3() @ fa.matrix.to_3x3()).normalized()
    bend = math.degrees(fa_w.col[1].angle(hand_rot3.col[1]))
    rel = (fa_w.inverted() @ hand_rot3).to_quaternion()
    rest = (fa.bone.matrix_local.to_3x3().normalized().inverted() @ hb.matrix_local.to_3x3().normalized()).to_quaternion()
    q = rest.inverted() @ rel
    sup = SUPINATE[side] * math.degrees(2 * math.atan2(q.y, q.w))
    sup = (sup + 180.0) % 360.0 - 180.0
    over = max(0.0, SUP_MIN - sup, sup - SUP_MAX)
    return bend + 4.0 * over, bend, sup


def best_roll(side, base_x, base_y, d):
    """Roll the hand about the shaft (its frame spins around d), and for the
    left hand also tilt the shaft across the palm; keep the least wrist bend
    that stays inside the forearm's rotation range."""
    best = None
    for k in range(72):
        r = Matrix.Rotation(math.radians(k * 5), 3, d)
        x, y0_ = r @ base_x, r @ base_y
        for tilt in TILTS[side]:
            y = Matrix.Rotation(math.radians(tilt), 3, x) @ y0_
            z = x.cross(y)
            m = Matrix((x, y, z)).transposed()
            c = wrist_cost(side, m)
            score = c[0] + 0.4 * abs(tilt)
            prev = PREV_HAND.get(side)
            if prev is not None:   # stay close to last frame's hand so it cannot flip
                score += 0.6 * math.degrees(prev.to_quaternion().rotation_difference(m.to_quaternion()).angle)
            if best is None or score < best[4]:
                best = (c, x, y, m, score)
    PREV_HAND[side] = best[3]
    return best[:4]


PREV_HAND = {}


def straight_wrists(d, rh, on, s):
    """Hands as straight on the forearms as the grip allows; the shaft crosses the palm.

    Right hand: the tip leaves the thumb side (hand +Z, the same way round as
    idle), so the only freedom is the roll about the shaft; pick the roll with
    the least wrist bend inside the forearm's rotation range. Left hand: same
    search plus a diagonal tilt, palm (+X) on the shaft. The wrist positions
    depend on the hand axes, so iterate.
    """
    bpy.context.view_layer.update()
    y0 = V((0.0, 0.0, 1.0)) - d * d.z
    y0 = y0.normalized()
    x0 = y0.cross(d)
    # Both thumbs point up the shaft toward the tip. (The right hand used to hold
    # it tip-out-the-pinky-side, which folded the wrist back over 100 degrees.)
    # The shaft lies on the palm, FINGER_REACH along the fingers from the
    # wrist (NewCharsPreview offsets the staff by the same amount), so the wrist
    # target sits that far back from the grip point. Iterate: the hand axes
    # depend on the forearm, which depends on the wrist target.
    for _ in range(4):
        bpy.context.view_layer.update()
        c, x, y, m = best_roll("Right", x0, y0, d)
        rot_targets["rhr"].rotation_quaternion = m.to_quaternion()
        targets["rh"][0].location = rh - y * FINGER_REACH + x * PALM_DEPTH   # right palm faces -X
    bpy.context.view_layer.update()
    WRIST_LOG["Right"] = c
    if on <= 0.0:
        return
    shaft_pt = rh + d * s
    # left: fingers across the shaft (perpendicular to d), palm +X perpendicular to both
    for _ in range(4):
        bpy.context.view_layer.update()
        c, x, y, m = best_roll("Left", d.cross(y0).normalized() * -1.0, y0, d)
        rot_targets["lhr"].rotation_quaternion = m.to_quaternion()
        targets["lh"][0].location = shaft_pt - y * FINGER_REACH - x * PALM_DEPTH
    bpy.context.view_layer.update()
    WRIST_LOG["Left"] = c


WRIST_LOG = {}


def set_pose(frame):
    g = ref(frame)
    w = blend_w(g)
    for name in FK:
        _, q0, _ = READY[name]
        _, q1, _ = LUNGE[name]
        pbs[name].rotation_quaternion = q0.slerp(q1, max(0.0, min(w, 1.0)))
    bpy.context.view_layer.update()

    # Hips: wanted location, lowered if either leg could not reach its foot.
    want = hips_loc(g).copy()
    tw0 = twist(g)
    back_yaw = 0.0   # no turn-out: it twisted the ankle skin in Unity
    rf_ankle, rf_rot = back_foot(g, back_yaw)
    for side, foot in {"Left": lfoot(g), "Right": rf_ankle}.items():
        j = want + hip_off[side]
        horiz = V((j.x - foot.x, j.y - foot.y, 0)).length
        reach = 0.985 * L[side]
        if horiz < reach:
            want.z = min(want.z, foot.z + math.sqrt(reach * reach - horiz * horiz) - hip_off[side].z)
    hm = world(P + "Hips")
    hm.translation = want
    pbs[P + "Hips"].matrix = MWI @ hm
    bpy.context.view_layer.update()

    tw = twist(g)
    turn_local(P + "Hips", tw * 0.40)
    turn_local(P + "Spine", tw * 0.15)
    turn_local(P + "Spine1", tw * 0.25)
    turn_local(P + "Spine2", tw * 0.20)
    turn_local(P + "Neck", -tw * 0.50)   # eyes stay on the target
    turn_local(P + "Head", -tw * 0.45)

    d = spear(g).normalized()
    hw = hit_w(g)
    # Her right and her back, in the horizontal plane, for the current turn.
    yaw = math.radians(-tw)
    right = V((-math.cos(yaw), math.sin(yaw), 0.0))
    back = V((math.sin(yaw), math.cos(yaw), 0.0))
    rs = MW @ pbs[P + "RightArm"].head
    rh_hip = (MW @ pbs[P + "Hips"].head) + rhip(g)
    rh_hit = rs + HIT_OFFSET
    rh_want = rh_hip.lerp(rh_hit, hw)
    s_lead = LEAD + (LEAD_HIT - LEAD) * hw
    rh, lh_shaft = fit_hands(s_lead, rh_want, d)
    k = on_shaft(g)
    targets["rh"][0].location = rh
    targets["lh"][0].location = lh_shaft
    # Rear elbow: back, out and above the hand while the hand is at the hip
    # (forearm slopes down into the grip). At the hit it is raised out to her
    # right, so the forearm and hand come over the top of the shaft and the
    # butt passes underneath the arm instead of through it.
    # Front elbow: down and a little out to her left.
    r_off = (right * 0.5 + back * 0.35 + V((0.0, 0.0, 0.60))).lerp(right * 0.7 + V((0.0, 0.0, 0.6)), hw)
    l_off = -right * 0.35 + V((0.0, 0.0, -0.9))
    targets["rh"][1].location = rh + r_off
    targets["lh"][1].location = lh_shaft + l_off
    straight_wrists(d, rh, k, s_lead)
    targets["lf"][0].location = lfoot(g)
    targets["rf"][0].location = rf_ankle
    # Front foot points at the target; the back foot turns out as she goes side-on.
    step = max(0.0, min(1.0, (lf_ready.y - lfoot(g).y) / 0.4))
    rot_targets["lfr"].rotation_quaternion = Quaternion((0, 0, 1), math.radians(-6 * step)) @ READY[P + "LeftUpLeg.002"][2].to_quaternion()
    for key, foot, yaw in (("rf", rf_ankle, back_yaw), ("lf", lfoot(g), -6 * step)):
        toe = Matrix.Rotation(math.radians(yaw), 3, "Z") @ V((0.0, -1.0, 0.0))
        targets[key][1].location = foot + toe * 1.2 + V((0.0, 0.0, 0.40))
    bpy.context.view_layer.update()
    plant_back_foot()


def plant_back_foot():
    """Move the right ankle target until the ball of the foot sits on its mark."""
    t = targets["rf"][0]
    for _ in range(8):
        ball = MW @ pbs[P + "RightUpLeg.003"].head
        err = rball_ready - ball
        if err.length < 0.002:
            break
        t.location = t.location + err
        t.location.z = max(t.location.z, rf_ready.z - 0.01)   # heel never below the floor
        bpy.context.view_layer.update()


scene.frame_start, scene.frame_end = FIRST, LAST
scene.frame_set(FIRST)
set_pose(FIRST)
calibrate_pole("rh", "RightForeArm", "RightHand", 0.0)
calibrate_pole("lh", "LeftForeArm", "LeftHand", 0.0)
calibrate_pole("lf", "LeftUpLeg.001", "LeftUpLeg.002", 1.0)
calibrate_pole("rf", "RightUpLeg.001", "RightUpLeg.002", 0.0)
set_pose(FIRST)


for f in range(FIRST, LAST + 1):
    scene.frame_set(f)
    set_pose(f)
    set_pose(f)   # second pass: the wrist solve starts from a settled arm
    for name in FK:
        pbs[name].keyframe_insert("rotation_quaternion", frame=f)
    pbs[P + "Hips"].keyframe_insert("location", frame=f)
    for key in ("rh", "lh", "lf", "rf"):
        targets[key][0].keyframe_insert("location", frame=f)
        targets[key][1].keyframe_insert("location", frame=f)
    for e in rot_targets.values():
        e.keyframe_insert("rotation_quaternion", frame=f)

# Bake IK to plain FK and drop the constraints.
bpy.context.view_layer.objects.active = rig
for o in bpy.data.objects:
    o.select_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode="POSE")
for pb in pbs:
    pb.select = True if hasattr(pb, "select") else None
bpy.ops.nla.bake(frame_start=FIRST, frame_end=LAST, only_selected=False, visual_keying=True,
                 clear_constraints=True, use_current_action=True, bake_types={"POSE"})
bpy.ops.object.mode_set(mode="OBJECT")
rig.animation_data.action.name = src_action.name


# ---------------------------------------------------------------- flat back foot
# Final pass on the right foot and right toe bones only. This rig's right foot
# and toe bones are not mirror images of the left ones at rest (33 and 67 deg
# off), so copying the left bone angles crumples the right boot. Instead the
# left foot's flat ready-pose deformation (posed world x rest world inverse) is
# mirrored across her midline and applied to the right foot and toe, keeping
# the foot's heading. Hips, thigh, knee and shin keys are not touched.
MIRROR = Matrix.Diagonal((-1.0, 1.0, 1.0))


def mirrored_flat(right_name, left_name):
    l_rest = (MW @ rig.data.bones[left_name].matrix_local).to_3x3().normalized()
    r_rest = (MW @ rig.data.bones[right_name].matrix_local).to_3x3().normalized()
    l_pose = READY[left_name][2].to_3x3().normalized()
    deform = l_pose @ l_rest.inverted()
    return (MIRROR @ deform @ MIRROR) @ r_rest


def flatten_back_foot():
    pairs = [(P + "RightUpLeg.002", P + "LeftUpLeg.002"), (P + "RightUpLeg.003", P + "LeftUpLeg.003")]
    flat = {r: mirrored_flat(r, l) for r, l in pairs}
    for f in range(FIRST, LAST + 1):
        scene.frame_set(f)
        for r, _ in pairs:
            pb = pbs[r]
            cur = MW @ pb.matrix
            want = flat[r]
            pb.matrix = MWI @ Matrix.LocRotScale(cur.translation, want.to_quaternion(), cur.to_scale())
            bpy.context.view_layer.update()
            pb.keyframe_insert("rotation_quaternion", frame=f)


flatten_back_foot()


# ---------------------------------------------------------------- forearm roll
# Whatever twist is left between forearm and hand is moved into the forearm's
# own roll (pronation / supination), up to 90 deg, keeping each hand's world
# orientation and position. The wrist then only bends; it does not wring.
def wrist_twist(side):
    fa = pbs[P + side + "ForeArm"]
    hb = pbs[P + side + "Hand"]
    fa_w = (MW.to_3x3() @ fa.matrix.to_3x3()).normalized()
    h_w = (MW.to_3x3() @ hb.matrix.to_3x3()).normalized()
    rel = (fa_w.inverted() @ h_w).to_quaternion()
    rest = (fa.bone.matrix_local.to_3x3().normalized().inverted() @ hb.bone.matrix_local.to_3x3().normalized()).to_quaternion()
    q = rest.inverted() @ rel
    tw = math.degrees(2 * math.atan2(q.y, q.w))
    return (tw + 180.0) % 360.0 - 180.0


def roll_forearms():
    for f in range(FIRST, LAST + 1):
        scene.frame_set(f)
        for side in ("Right", "Left"):
            fa = pbs[P + side + "ForeArm"]
            hb = pbs[P + side + "Hand"]
            hand_w = MW @ hb.matrix
            base = fa.rotation_quaternion.copy()
            tw = max(-90.0, min(90.0, wrist_twist(side)))
            best = None
            for roll in (tw, -tw):
                fa.rotation_quaternion = base @ Quaternion((0, 1, 0), math.radians(roll))
                bpy.context.view_layer.update()
                hb.matrix = MWI @ hand_w
                bpy.context.view_layer.update()
                left = abs(wrist_twist(side))
                if best is None or left < best[0]:
                    best = (left, roll)
            fa.rotation_quaternion = base @ Quaternion((0, 1, 0), math.radians(best[1]))
            bpy.context.view_layer.update()
            hb.matrix = MWI @ hand_w
            bpy.context.view_layer.update()
            fa.keyframe_insert("rotation_quaternion", frame=f)
            hb.keyframe_insert("rotation_quaternion", frame=f)


roll_forearms()


# ---------------------------------------------------------------- level soles
# The boots have heels, so a foot whose bones match the ready pose still has its
# sole tilted toe-up (about 24 deg left, 19 deg right, measured on the mesh).
# Pitch each foot bone toe-down about its ankle until the line from the lowest
# heel point to the lowest toe point is level. Only the foot bones get new keys;
# the toe bones keep their local keys and follow.
BODY = next(o for o in imported if o.type == "MESH" and o.name == "SERAPHINE_body")
_vg = {g.name: g.index for g in BODY.vertex_groups}


def foot_verts(side):
    ids = {_vg[P + side + "UpLeg.002"], _vg[P + side + "UpLeg.003"]}
    return [i for i, v in enumerate(BODY.data.vertices) if any(g.group in ids and g.weight > 0.5 for g in v.groups)]


FOOT_VERTS = {s: foot_verts(s) for s in ("Left", "Right")}


def heading(side):
    a = MW @ pbs[P + side + "UpLeg.002"].head
    b = MW @ pbs[P + side + "UpLeg.003"].head
    return V((b.x - a.x, b.y - a.y, 0.0)).normalized()


def sole_pitch(side):
    """Degrees, toe-up positive, of the heel-bottom to toe-bottom line."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = BODY.evaluated_get(dg)
    me = ev.to_mesh()
    h = heading(side)
    pts = [(BODY.matrix_world @ me.vertices[i].co) for i in FOOT_VERTS[side]]
    ev.to_mesh_clear()
    proj = [(p.dot(h), p.z) for p in pts]
    lo = min(t for t, _ in proj)
    hi = max(t for t, _ in proj)
    span = hi - lo
    back = min(((z, t) for t, z in proj if t < lo + 0.25 * span))
    front = min(((z, t) for t, z in proj if t > hi - 0.25 * span))
    return math.degrees(math.atan2(front[0] - back[0], front[1] - back[1]))


def level_soles():
    for f in range(FIRST, LAST + 1):
        scene.frame_set(f)
        for side in ("Left", "Right"):
            pb = pbs[P + side + "UpLeg.002"]
            # One measured correction: re-measuring after the turn picks a
            # different lowest point on the high heel and over-rotates.
            for _ in range(1):
                pitch = sole_pitch(side)
                axis = V((0.0, 0.0, 1.0)).cross(heading(side)).normalized()
                cur = MW @ pb.matrix
                rot = Matrix.Rotation(math.radians(pitch), 3, axis) @ cur.to_3x3().normalized()
                pb.matrix = MWI @ Matrix.LocRotScale(cur.translation, rot.to_quaternion(), cur.to_scale())
                bpy.context.view_layer.update()
            pb.keyframe_insert("rotation_quaternion", frame=f)
        if f % 9 == 0:
            print("SOLE", f, "corrected")


level_soles()

# ---------------------------------------------------------------- report
# The staff mesh in right-hand space: turned -RIGHT_TILT about the hand's X axis,
# then moved so the shaft's centre line at the grip point lies on the palm seat.
_staff_rot = Matrix.Rotation(math.radians(-RIGHT_TILT), 4, "X")
_staff_loc = V((-PALM_DEPTH, FINGER_REACH, 0.0)) - (_staff_rot.to_3x3() @ V((STAFF_AXIS_X, STAFF_AXIS_Y, STAFF_GRIP)))
STAFF_LOCAL = Matrix.Translation(_staff_loc) @ _staff_rot
print("UNITY thrust staff localPosition (metres, X mirrored) = (%.4f, %.4f, %.4f)  localRotation = Euler(%.1f, 0, 0)"
      % (-_staff_loc.x, _staff_loc.y, _staff_loc.z, -RIGHT_TILT))
print("LEG", {k: round(v, 3) for k, v in L.items()}, "ARM", {k: round(v, 3) for k, v in ARM.items()})
for f in range(FIRST, LAST + 1, 3):
    scene.frame_set(f)
    h = MW.to_3x3() @ pbs[P + "RightHand"].matrix.to_3x3()
    z = h.col[2].normalized()
    rh = MW @ pbs[P + "RightHand"].head
    lm = MW @ pbs[P + "LeftHand"].matrix
    lx, ly = lm.to_3x3().col[0].normalized(), lm.to_3x3().col[1].normalized()
    palm = lm.translation + ly * FINGER_REACH + lx * PALM_DEPTH
    off = (palm - rh) - z * (palm - rh).dot(z)
    def bend_dir(lower_name, pole):
        lo = pbs[P + lower_name]; root = MW @ lo.parent.head; tip = MW @ lo.tail; mid = MW @ lo.head
        ax = (tip - root).normalized(); w = mid - root; o = w - ax * w.dot(ax)
        return o.normalized()
    re = bend_dir("RightForeArm", None); ke = bend_dir("RightUpLeg.001", None); kl = bend_dir("LeftUpLeg.001", None)
    rs_ = MW @ pbs[P + "RightArm"].head; ls_ = MW @ pbs[P + "LeftArm"].head
    rext = ((MW @ pbs[P + "RightHand"].head) - rs_).length / ARM["Right"]
    lext = ((MW @ pbs[P + "LeftHand"].head) - ls_).length / ARM["Left"]
    rel_h = (MW @ pbs[P + "RightForeArm"].head).z - rs_.z
    print(f, "reach R", round(rext, 2), "L", round(lext, 2), "relbow_vs_shoulder_cm", round(rel_h * 100, 1))
    hw = MW @ pbs[P + "RightHand"].matrix
    hl, hr, _ = hw.decompose()
    # The staff mesh as NewCharsPreview places it (same numbers, X not mirrored here).
    st = Matrix.LocRotScale(hl, hr, None) @ STAFF_LOCAL
    butt, tipp = st @ V((STAFF_AXIS_X, STAFF_AXIS_Y, 0)), st @ V((STAFF_AXIS_X, STAFF_AXIS_Y, 2.02))
    def seg_dist(pt):
        ab = tipp - butt; t = max(0.0, min(1.0, (pt - butt).dot(ab) / ab.length_squared))
        return (pt - (butt + ab * t)).length
    head = (MW @ pbs[P + "Head"].head + MW @ pbs[P + "Head"].tail) * 0.5
    chest = MW @ pbs[P + "Spine2"].head
    print(f, "staff_to_head_cm", round(seg_dist(head) * 100, 1), "staff_to_chest_cm", round(seg_dist(chest) * 100, 1),
          "rpalm_on_shaft_cm", round(seg_dist(hl + (hw.to_3x3().normalized() @ V((-PALM_DEPTH, FINGER_REACH, 0)))) * 100, 1),
          "lpalm_on_shaft_cm", round(seg_dist(palm) * 100, 1))
    # Arms vs the shaft: wrist bend (deg), and how far the elbow and the middle of
    # the forearm are from the shaft's centre line, and whether they are above it.
    def line_dist(pt):
        ab = tipp - butt
        t = max(0.0, min(1.0, (pt - butt).dot(ab) / ab.length_squared))
        return (pt - (butt + ab * t)).length, pt.z - (butt + ab * t).z
    arm_txt = []
    for side in ("Right", "Left"):
        fa = pbs[P + side + "ForeArm"]; hb = pbs[P + side + "Hand"]
        el, wr = MW @ fa.head, MW @ hb.head
        bend = math.degrees((MW.to_3x3() @ fa.matrix.to_3x3()).col[1].angle((MW.to_3x3() @ hb.matrix.to_3x3()).col[1]))
        de, he = line_dist(el); dm, hm_ = line_dist((el + wr) * 0.5)
        arm_txt.append("%s forearm_turn %.0f wrist_bend %.0f elbow_to_shaft_cm %.1f (above %.1f) midforearm_cm %.1f (above %.1f)" % (side, SUPINATE[side] * wrist_twist(side), bend, de * 100, he * 100, dm * 100, hm_ * 100))
    print(f, "ARMS", " | ".join(arm_txt))
    print(f, "relbow_dir", tuple(round(c, 2) for c in re), "rknee_dir", tuple(round(c, 2) for c in ke), "lknee_dir", tuple(round(c, 2) for c in kl))
    print(f, "g", round(ref(f), 1), "hips", tuple(round(c, 2) for c in MW @ pbs[P + "Hips"].head),
          "rh", tuple(round(c, 2) for c in rh),
          "pitch", round(math.degrees(math.asin(max(-1, min(1, z.z)))), 1),
          "lpalm_off_shaft_cm", round(off.length * 100, 1), "lead", round((palm - rh).dot(z), 2),
          "lf", tuple(round(c, 2) for c in MW @ pbs[P + "LeftUpLeg.002"].head),
          "rf", tuple(round(c, 2) for c in MW @ pbs[P + "RightUpLeg.002"].head),
          "rball", tuple(round(c, 2) for c in MW @ pbs[P + "RightUpLeg.003"].head))

for o in list(bpy.data.objects):
    if o.type == "EMPTY":
        bpy.data.objects.remove(o)
for o in bpy.data.objects:
    o.select_set(o in imported)
bpy.ops.export_scene.fbx(
    filepath=OUT, use_selection=True, object_types={"ARMATURE", "MESH"},
    global_scale=1.0, apply_unit_scale=True, apply_scale_options="FBX_SCALE_NONE",
    axis_forward="-Z", axis_up="Y", primary_bone_axis="Y", secondary_bone_axis="X",
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0, bake_anim_step=1.0, bake_anim_use_all_bones=True,
    mesh_smooth_type="FACE", path_mode="AUTO",
)
print("EXPORTED", OUT)
