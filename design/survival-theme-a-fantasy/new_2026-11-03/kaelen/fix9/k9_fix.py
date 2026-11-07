"""Kaelen FIX9 — solve arm arcs from hand position + anatomical swing/twist.

Does not clamp an output Euler. Upper-arm orientation is the two-bone IK pose
whose elbow stays on a stable lateral pole. Forearm twist is only the rotation
about the forearm axis that aims the already-seated sword, and it is limited
to a joint range. Hand-bone local rotation (the grip) is not edited.

The crossthrust wrist and sword-direction samples are kept: that clip already
raises both hands overhead and cuts down/across. Its right upper arm twist
was the full extra revolution. Dualslash hand targets are a higher overhead
and a lower across-body cut; the old forearm ±75° slide was how a low arm
flicked the blade.

Run:
  blender --background --factory-startup KAELEN.blend --python k9_fix.py
"""
import math
import os
import json
import bpy
from mathutils import Vector, Quaternion, Matrix
from mathutils.bvhtree import BVHTree

# The file Blender opened is the FIX8 blend (main 02b3660e). Do not run this on
# the already-solved FIX9 blend or the arcs will be solved a second time.
OUT_DIR = os.environ.get("KAELEN_FIX9_OUT", "/home/ubuntu/k9_out")
OUT_BLEND = os.environ.get("KAELEN_FIX9_BLEND", os.path.join(OUT_DIR, "KAELEN_fix9.blend"))
os.makedirs(OUT_DIR, exist_ok=True)

ARM_NAME = "KAELEN_rig"
# Forearm pronation/supination used only to aim the seated sword.
# Human range is roughly ±80°. This is a joint limit, not an Euler-output clamp.
FOREARM_TWIST_LIMIT = math.radians(75.0)

SIDES = {
    "R": {
        "shoulder": "mixamorig:RightShoulder",
        "upper": "mixamorig:RightArm",
        "fore": "mixamorig:RightArm.001",
        "hand": "mixamorig:RightArm.002",
        "sword": "KAELEN_sword_R",
        "lateral": Vector((-1.0, 0.0, 0.0)),
    },
    "L": {
        "shoulder": "mixamorig:LeftShoulder",
        "upper": "mixamorig:LeftArm",
        "fore": "mixamorig:LeftArm.001",
        "hand": "mixamorig:LeftArm.002",
        "sword": "KAELEN_sword_L",
        "lateral": Vector((1.0, 0.0, 0.0)),
    },
}
FWD = Vector((0.0, -1.0, 0.0))
UP = Vector((0.0, 0.0, 1.0))


def smooth(t):
    t = 0.0 if t < 0 else (1.0 if t > 1 else t)
    return t * t * (3.0 - 2.0 * t)


def lerp(a, b, t):
    return a + (b - a) * t


def vlerp(a, b, t):
    return a.lerp(b, t)


def clamp_len(origin, target, max_len, min_len):
    d = target - origin
    L = d.length
    if L < 1e-8:
        return origin + Vector((0, 0, min_len))
    if L > max_len:
        d = d * (max_len / L)
    elif L < min_len:
        d = d * (min_len / L)
    return origin + d


def swing_twist(q, axis=Vector((0, 1, 0))):
    q = q.normalized()
    axis = axis.normalized()
    xyz = Vector((q.x, q.y, q.z))
    proj = axis * xyz.dot(axis)
    tw = Quaternion((q.w, proj.x, proj.y, proj.z))
    if tw.magnitude < 1e-8:
        tw = Quaternion((1, 0, 0, 0))
    else:
        tw.normalize()
    sw = (q @ tw.inverted()).normalized()
    comp = Vector((tw.x, tw.y, tw.z)).dot(axis)
    ang = 2.0 * math.atan2(comp, tw.w)
    return sw, tw, ang


def unwrap(prev, ang):
    if prev is None:
        return ang
    while ang - prev > math.pi:
        ang -= 2 * math.pi
    while ang - prev < -math.pi:
        ang += 2 * math.pi
    return ang


def twist_about(axis, current, target):
    axis = axis.normalized()
    c = current - axis * current.dot(axis)
    t = target - axis * target.dot(axis)
    if c.length < 1e-6 or t.length < 1e-6:
        return 0.0
    c.normalize()
    t.normalize()
    return math.atan2(axis.dot(c.cross(t)), c.dot(t))


def rot_about(axis, angle):
    return Quaternion(axis.normalized(), angle)


def ik_elbow(shoulder, hand, upper_len, fore_len, pole):
    span = hand - shoulder
    dist = span.length
    maxd = upper_len + fore_len - 1e-4
    mind = abs(upper_len - fore_len) + 1e-4
    dist_c = min(max(dist, mind), maxd)
    direction = span.normalized() if span.length > 1e-8 else Vector((0, 0, 1))
    # If we had to shorten, move the effective hand along the same ray.
    hand_c = shoulder + direction * dist_c
    cos_a = (upper_len * upper_len + dist_c * dist_c - fore_len * fore_len) / (2.0 * upper_len * dist_c)
    cos_a = max(-1.0, min(1.0, cos_a))
    along = upper_len * cos_a
    height = math.sqrt(max(0.0, upper_len * upper_len - along * along))
    pole_v = pole - shoulder
    n = direction.cross(pole_v)
    if n.length < 1e-6:
        n = direction.cross(UP if abs(direction.z) < 0.9 else FWD)
    n.normalize()
    side = n.cross(direction)
    if side.dot(pole_v) < 0.0:
        side = -side
    side.normalize()
    elbow = shoulder + direction * along + side * height
    return elbow, hand_c


def basis_aim(y_axis, pole_hint, head):
    """Armature-space 3x3 whose Y is y_axis and whose X points toward pole_hint."""
    y = y_axis.normalized()
    hint = pole_hint - head
    x = hint - y * y.dot(hint)
    if x.length < 1e-6:
        tmp = UP if abs(y.z) < 0.9 else FWD
        x = tmp.cross(y)
    x.normalize()
    z = x.cross(y).normalized()
    x = y.cross(z).normalized()
    return Matrix(((x.x, y.x, z.x), (x.y, y.y, z.y), (x.z, y.z, z.z)))


def set_world_rotation(arm, pb, rot3, head_world):
    """Point the bone with rot3 (columns X,Y,Z in world) keeping its head."""
    Mw = rot3.to_4x4()
    Mw.translation = head_world
    pb.matrix = arm.matrix_world.inverted() @ Mw
    # Connected bones must not drift off the parent joint.
    if pb.location.length > 1e-4:
        pb.location = Vector((0.0, 0.0, 0.0))


def sword_samples(obj, deps):
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    pts = [mw @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    return pts


def sword_tip_and_axis(pts, hand_world):
    tip = max(pts, key=lambda p: (p - hand_world).length)
    near_sorted = sorted(pts, key=lambda p: (p - hand_world).length)
    n = max(1, len(near_sorted) // 30)
    near = Vector((0, 0, 0))
    for p in near_sorted[:n]:
        near += p
    near /= n
    axis = tip - near
    if axis.length < 1e-6:
        axis = tip - hand_world
    return tip, axis.normalized()


def assign_slot(arm, action):
    ad = arm.animation_data
    ad.action = action
    if hasattr(ad, "action_slot"):
        for sl in action.slots:
            ad.action_slot = sl
            break


def key_quat(pb, frame):
    pb.keyframe_insert(data_path="rotation_quaternion", frame=frame)


def continuous_quats(arm, action, bones, f0, f1):
    """Flip quaternion signs so consecutive keys take the short arc. This does not change poses."""
    assign_slot(arm, action)
    scene = bpy.context.scene
    prev = {b: None for b in bones}
    for f in range(f0, f1 + 1):
        scene.frame_set(f)
        for b in bones:
            pb = arm.pose.bones[b]
            q = pb.rotation_quaternion.copy()
            if prev[b] is not None and prev[b].dot(q) < 0.0:
                q = Quaternion((-q.w, -q.x, -q.y, -q.z))
                pb.rotation_quaternion = q
                key_quat(pb, f)
            prev[b] = q.copy()


def measure(arm, action, f0, f1):
    assign_slot(arm, action)
    scene = bpy.context.scene
    deps = bpy.context.evaluated_depsgraph_get()
    prev = {}
    rows = []
    for f in range(f0, f1 + 1):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        rec = {"f": f}
        for side, spec in SIDES.items():
            for role in ("upper", "fore", "hand"):
                n = spec[role]
                pb = arm.pose.bones[n]
                q = pb.matrix_basis.to_quaternion()
                _, _, ang = swing_twist(q)
                ang = unwrap(prev.get(n), ang)
                prev[n] = ang
                rec[f"{side}_{role}_twist"] = round(math.degrees(ang), 2)
            hand = arm.matrix_world @ arm.pose.bones[spec["hand"]].head
            pts = sword_samples(bpy.data.objects[spec["sword"]], deps)
            tip, axis = sword_tip_and_axis(pts, hand)
            elbow = arm.matrix_world @ arm.pose.bones[spec["upper"]].tail
            shoulder = arm.matrix_world @ arm.pose.bones[spec["upper"]].head
            rec[f"{side}_hand"] = [round(hand.x, 4), round(hand.y, 4), round(hand.z, 4)]
            rec[f"{side}_tip"] = [round(tip.x, 4), round(tip.y, 4), round(tip.z, 4)]
            rec[f"{side}_elbow"] = [round(elbow.x, 4), round(elbow.y, 4), round(elbow.z, 4)]
            rec[f"{side}_sh"] = [round(shoulder.x, 4), round(shoulder.y, 4), round(shoulder.z, 4)]
        rows.append(rec)
    return rows


def worst_upper(rows, side):
    key = f"{side}_upper_twist"
    best = max(rows, key=lambda r: abs(r[key]))
    return best["f"], best[key]


# ---------- pose application ----------

def snapshot_original(arm, action, f0, f1):
    assign_slot(arm, action)
    scene = bpy.context.scene
    deps = bpy.context.evaluated_depsgraph_get()
    data = []
    for f in range(f0, f1 + 1):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        fr = {"f": f, "q": {}}
        for spec in SIDES.values():
            for role in ("upper", "fore", "hand", "shoulder"):
                n = spec[role]
                fr["q"][n] = arm.pose.bones[n].rotation_quaternion.copy()
        for side, spec in SIDES.items():
            hand = arm.matrix_world @ arm.pose.bones[spec["hand"]].head
            pts = sword_samples(bpy.data.objects[spec["sword"]], deps)
            tip, axis = sword_tip_and_axis(pts, hand)
            fr[f"{side}_hand"] = hand.copy()
            fr[f"{side}_dir"] = axis.copy()
            fr[f"{side}_tip"] = tip.copy()
        data.append(fr)
    return data


def pole_for(side, shoulder, hand):
    """Elbow stays lateral. A low hand keeps the elbow high so the forearm slopes down."""
    spec = SIDES[side]
    lateral = spec["lateral"]
    h = hand.z - shoulder.z
    raised = smooth((h + 0.02) / 0.50)
    low = 1.0 - raised
    offset = (
        lateral * (0.48 + 0.06 * low)
        + Vector((0.0, 0.18, 0.0))
        + UP * (0.02 * raised + 0.32 * low)
    )
    return shoulder + offset


def rest_world_matrix(pb):
    """Bone matrix in armature space with zero local rotation and the current parent pose."""
    if pb.parent:
        return pb.parent.matrix @ pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local
    return pb.bone.matrix_local.copy()


def aim_bone_from_rest(arm, pb, target_world, pole_world):
    """Point the bone at target. Twist is the short arc that rolls the rest +X toward the pole.

    The swing is rotation_difference from the rest axis, so a still target does not add a revolution.
    """
    bpy.context.view_layer.update()
    head = arm.matrix_world @ pb.head
    rest = rest_world_matrix(pb)
    # rest is armature space. Convert axes to world.
    Rrest = (arm.matrix_world.to_3x3() @ rest.to_3x3())
    rest_y = (Rrest @ Vector((0.0, 1.0, 0.0))).normalized()
    desired_y = target_world - head
    if desired_y.length < 1e-6:
        desired_y = rest_y
    desired_y.normalize()
    q_swing = rest_y.rotation_difference(desired_y)
    R0 = q_swing.to_matrix() @ Rrest
    y = desired_y
    rest_x = (R0 @ Vector((1.0, 0.0, 0.0)))
    rest_x = rest_x - y * y.dot(rest_x)
    if rest_x.length < 1e-6:
        delta = 0.0
        R = R0
    else:
        rest_x.normalize()
        hint = pole_world - head
        want = hint - y * y.dot(hint)
        if want.length < 1e-6:
            delta = 0.0
            R = R0
        else:
            want.normalize()
            delta = twist_about(y, rest_x, want)
            # Shortest roll only. Never accumulate a second turn.
            while delta > math.pi:
                delta -= 2 * math.pi
            while delta < -math.pi:
                delta += 2 * math.pi
            R = rot_about(y, delta).to_matrix() @ R0
    set_world_rotation(arm, pb, R, head)
    return delta


def local_twist(pb):
    _, _, ang = swing_twist(pb.matrix_basis.to_quaternion())
    return ang


def wrap_pi(ang):
    while ang > math.pi:
        ang -= 2 * math.pi
    while ang < -math.pi:
        ang += 2 * math.pi
    return ang


def force_local_twist(arm, pb, target_ang):
    """Roll the bone about its own axis. Head and tail stay put."""
    cur = local_twist(pb)
    delta = wrap_pi(target_ang - cur)
    if abs(delta) < 1e-4:
        return
    y = ((arm.matrix_world @ pb.matrix).to_3x3() @ Vector((0.0, 1.0, 0.0))).normalized()
    head = arm.matrix_world @ pb.head
    R = (arm.matrix_world @ pb.matrix).to_3x3()
    R2 = rot_about(y, delta).to_matrix() @ R
    set_world_rotation(arm, pb, R2, head)


PREV_TWIST = {}


def clamp_twist_near(arm, pb, guard, limit, max_step):
    """Keep bone roll near the guard and continuous with the previous frame.

    The swing/twist split is unreliable when the arm passes overhead, so a raw
    angle is not allowed to jump. The pose changes by at most max_step, and it
    cannot travel more than `limit` from the guard roll.
    """
    cur = local_twist(pb)
    prev = PREV_TWIST.get(pb.name, guard)
    rel = wrap_pi(cur - prev)
    rel = max(-max_step, min(max_step, rel))
    new = prev + rel
    new = max(guard - limit, min(guard + limit, new))
    force_local_twist(arm, pb, new)
    # Re-read after the correction so the stored state matches the pose.
    PREV_TWIST[pb.name] = local_twist(pb)


# Upper-arm roll stays near the guard. Forearm may pronate through a human range to aim the sword.
UPPER_TWIST_FROM_GUARD = math.radians(55.0)
FOREARM_TWIST_FROM_GUARD = math.radians(100.0)
GUARD_TWIST = {}


def solve_side(arm, side, hand_target, sword_dir, upper_len, fore_len):
    spec = SIDES[side]
    bpy.context.view_layer.update()
    upper = arm.pose.bones[spec["upper"]]
    fore = arm.pose.bones[spec["fore"]]
    shoulder = arm.matrix_world @ upper.head
    pole = pole_for(side, shoulder, hand_target)
    reach = upper_len + fore_len - 0.008
    hand_target = clamp_len(shoulder, hand_target, reach, abs(upper_len - fore_len) + 0.02)
    elbow, hand_c = ik_elbow(shoulder, hand_target, upper_len, fore_len, pole)
    aim_bone_from_rest(arm, upper, elbow, pole)
    bpy.context.view_layer.update()
    clamp_twist_near(arm, upper, GUARD_TWIST[spec["upper"]], UPPER_TWIST_FROM_GUARD, math.radians(16.0))
    bpy.context.view_layer.update()
    elbow_w = arm.matrix_world @ upper.tail
    aim_bone_from_rest(arm, fore, hand_c, pole)
    bpy.context.view_layer.update()
    # Aim the sword with the full short twist, then pull that twist back into the guard window.
    deps = bpy.context.evaluated_depsgraph_get()
    hand_w = arm.matrix_world @ arm.pose.bones[spec["hand"]].head
    pts = sword_samples(bpy.data.objects[spec["sword"]], deps)
    _, cur_axis = sword_tip_and_axis(pts, hand_w)
    fore_y = ((arm.matrix_world @ fore.matrix).to_3x3() @ Vector((0.0, 1.0, 0.0))).normalized()
    delta = wrap_pi(twist_about(fore_y, cur_axis, sword_dir))
    if abs(delta) > 1e-4:
        R = (arm.matrix_world @ fore.matrix).to_3x3()
        R2 = rot_about(fore_y, delta).to_matrix() @ R
        set_world_rotation(arm, fore, R2, elbow_w)
        bpy.context.view_layer.update()
    clamp_twist_near(arm, fore, GUARD_TWIST[spec["fore"]], FOREARM_TWIST_FROM_GUARD, math.radians(20.0))
    bpy.context.view_layer.update()
    return hand_c


def lift_clavicle(arm, pb, radians):
    """Rotate the clavicle about the character-forward axis through its head."""
    M = pb.matrix.copy()
    head_a = M.to_translation()
    q = Quaternion(FWD, radians)
    T = Matrix.Translation(head_a)
    pb.matrix = T @ q.to_matrix().to_4x4() @ T.inverted() @ M
    if pb.location.length > 5e-3:
        pb.location = Vector((0.0, 0.0, 0.0))


LIFT_SIGN = {}


def ensure_lift_sign(arm):
    if LIFT_SIGN:
        return
    bpy.context.view_layer.update()
    for side, spec in SIDES.items():
        pb = arm.pose.bones[spec["shoulder"]]
        base = pb.matrix.copy()
        z0 = (arm.matrix_world @ pb.tail).z
        chosen = 1.0
        for sign in (1.0, -1.0):
            pb.matrix = base.copy()
            lift_clavicle(arm, pb, sign * 0.35)
            bpy.context.view_layer.update()
            z1 = (arm.matrix_world @ pb.tail).z
            pb.matrix = base.copy()
            bpy.context.view_layer.update()
            if z1 > z0 + 0.01:
                chosen = sign
                break
        LIFT_SIGN[side] = chosen
        print("LIFT_SIGN", side, chosen)


def apply_frame(arm, side_targets, lengths, originals, frame, weight, shoulder_lift=None):
    """weight 0 = original local quats, 1 = solved IK quats. weight may be a per-side dict."""
    def w_of(side):
        return weight[side] if isinstance(weight, dict) else weight

    solved = {}
    if shoulder_lift and max(w_of("R"), w_of("L")) > 0.001:
        ensure_lift_sign(arm)
        for side, radians in shoulder_lift.items():
            pb = arm.pose.bones[SIDES[side]["shoulder"]]
            # Start from the original clavicle, then raise.
            pb.rotation_quaternion = originals["q"][SIDES[side]["shoulder"]]
            bpy.context.view_layer.update()
            lift_clavicle(arm, pb, radians * w_of(side))
            bpy.context.view_layer.update()
    if max(w_of("R"), w_of("L")) > 0.001:
        for side in ("R", "L"):
            if w_of(side) <= 0.001:
                continue
            hand_t, sword_d = side_targets[side]
            solve_side(arm, side, hand_t, sword_d, lengths[side][0], lengths[side][1])
            spec = SIDES[side]
            for role in ("upper", "fore"):
                n = spec[role]
                solved[n] = arm.pose.bones[n].rotation_quaternion.copy()
            if shoulder_lift:
                solved[spec["shoulder"]] = arm.pose.bones[spec["shoulder"]].rotation_quaternion.copy()
    roles = ("upper", "fore")
    if shoulder_lift:
        roles = ("shoulder", "upper", "fore")
    for side in ("R", "L"):
        spec = SIDES[side]
        for role in roles:
            n = spec[role]
            pb = arm.pose.bones[n]
            q0 = originals["q"][n]
            w = w_of(side)
            if w <= 0.001 or n not in solved:
                pb.rotation_quaternion = q0
            elif w >= 0.999:
                pb.rotation_quaternion = solved[n]
            else:
                q1 = solved[n]
                if q0.dot(q1) < 0:
                    q1 = Quaternion((-q1.w, -q1.x, -q1.y, -q1.z))
                pb.rotation_quaternion = q0.slerp(q1, w)
            key_quat(pb, frame)


def dualslash_targets(frame, orig, nframes):
    """Higher overhead, then a lower across-body cut, back to the f1 guard."""
    f = frame
    # Right leads, left follows. Both return to the recorded guard.
    guard = orig[0]
    def arc(side, raise_a, raise_b, strike_b, recover_b):
        S_key = f"{side}_sh"
        # Shoulder is not stored per original sample under that name; use the live bone later.
        o = orig[f - 1]
        g = guard
        # Targets are absolute world positions designed around the resting shoulder height,
        # then shifted by how far this frame's shoulder has moved from the guard shoulder.
        return o, g

    # Built after we know live shoulders inside the caller. Here return phase weights and local offsets.
    def offsets(side):
        lat = SIDES[side]["lateral"]
        across = -lat
        if side == "R":
            ra, rb, sb, rec = 3, 12, 24, 36
        else:
            # Left wind-up starts as the right blade is already coming down, and
            # the left cut stays wider so the blades do not meet.
            ra, rb, sb, rec = 16, 28, 42, 52
        overhead = lat * 0.16 + FWD * 0.05 + UP * 0.50
        # Right cuts further forward than the left so the blades pass on two planes.
        down = across * (0.16 if side == "R" else 0.02) + FWD * (0.58 if side == "R" else 0.30) + UP * (-0.42)
        if f <= ra:
            return None  # original
        if f <= rb:
            t = smooth((f - ra) / (rb - ra))
            return ("raise", t, overhead, down)
        if f <= sb:
            t = smooth((f - rb) / (sb - rb))
            return ("strike", t, overhead, down)
        if f <= rec:
            t = smooth((f - sb) / (rec - sb))
            return ("recover", t, overhead, down)
        return None

    return {side: offsets(side) for side in ("R", "L")}


def live_shoulders(arm):
    out = {}
    for side, spec in SIDES.items():
        out[side] = arm.matrix_world @ arm.pose.bones[spec["upper"]].head
    return out


def make_dual_targets(arm, frame, orig, guard_hand, guard_dir):
    spec_phase = dualslash_targets(frame, orig, len(orig))
    shoulders = live_shoulders(arm)
    targets = {}
    o = orig[frame - 1]
    for side in ("R", "L"):
        phase = spec_phase[side]
        S = shoulders[side]
        upper_len = (arm.pose.bones[SIDES[side]["upper"]].bone.length)
        # lengths filled by caller via bone; reach used inside solve
        if phase is None:
            targets[side] = (o[f"{side}_hand"].copy(), o[f"{side}_dir"].copy())
            continue
        kind, t, overhead_off, down_off = phase
        overhead = clamp_len(S, S + overhead_off, 0.50, 0.20)
        down = clamp_len(S, S + down_off, 0.49, 0.18)
        # Push the down hand forward enough that a 0.85 m blade can point down
        # without starting inside the chest.
        g_hand = guard_hand[side]
        g_dir = guard_dir[side]
        if kind == "raise":
            hand = vlerp(g_hand, overhead, t)
            direction = vlerp(g_dir, (SIDES[side]["lateral"] * 0.25 + FWD * 0.35 + UP * 0.85).normalized(), t).normalized()
        elif kind == "strike":
            hand = vlerp(overhead, down, t)
            up_dir = (SIDES[side]["lateral"] * 0.35 + FWD * 0.25 + UP * 0.90).normalized()
            own = SIDES[side]["lateral"]
            dn_dir = (FWD * (0.35 if side == "R" else 0.55) + UP * (-0.85) + own * (0.05 if side == "R" else 0.28)).normalized()
            direction = vlerp(up_dir, dn_dir, t).normalized()
        else:
            hand = vlerp(down, g_hand, t)
            own = SIDES[side]["lateral"]
            dn_dir = (FWD * (0.35 if side == "R" else 0.55) + UP * (-0.85) + own * (0.05 if side == "R" else 0.28)).normalized()
            direction = vlerp(dn_dir, g_dir, t).normalized()
        targets[side] = (hand, direction)
    weights = {}
    for side in ("R", "L"):
        phase = spec_phase[side]
        if phase is None:
            weights[side] = 0.0
        else:
            kind, t, _, _ = phase
            if kind == "raise":
                weights[side] = t
            elif kind == "recover":
                weights[side] = 1.0 - t
            else:
                weights[side] = 1.0
    return targets, weights


def make_cross_targets(frame, orig, n):
    """Keep the recorded hand position and sword direction. Replace only the elbow orbit."""
    o = orig[frame - 1]
    targets = {}
    for side in ("R", "L"):
        targets[side] = (o[f"{side}_hand"].copy(), o[f"{side}_dir"].copy())
    # Full correction through the roll. Ends blend back onto the original guard,
    # whose upper-arm twist is already small (about -35° to -7°).
    if frame <= 4:
        w = smooth((frame - 1) / 4.0)
    elif frame >= n - 3:
        w = smooth((n - frame) / 4.0)
    else:
        w = 1.0
    return targets, w


def bone_lengths(arm):
    out = {}
    for side, spec in SIDES.items():
        out[side] = (
            arm.pose.bones[spec["upper"]].bone.length,
            arm.pose.bones[spec["fore"]].bone.length,
        )
    return out


def edit_fingerprint(arm):
    import hashlib
    h = hashlib.sha256()
    for b in arm.data.bones:
        h.update(b.name.encode())
        h.update(b.parent.name.encode() if b.parent else b"-")
        for v in list(b.head_local) + list(b.tail_local):
            h.update(f"{v:.6f}".encode())
        h.update(str(round(b.length, 6)).encode())
    return h.hexdigest()[:16]


def group_ids(obj, names, thresh):
    ids = set()
    gi = {g.name: g.index for g in obj.vertex_groups}
    for name in names:
        idx = gi.get(name)
        if idx is None:
            continue
        for v in obj.data.vertices:
            for ge in v.groups:
                if ge.group == idx and ge.weight >= thresh:
                    ids.add(v.index)
                    break
    return ids


def mesh_bvh(obj, deps, skip=None):
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world.copy()
    verts = []
    for i, v in enumerate(me.vertices):
        if skip and i in skip:
            continue
        verts.append(mw @ v.co)
    # Build from all verts; overlap test uses a second filtered cloud via from_object when possible.
    bvh = BVHTree.FromObject(ev, deps)
    ev.to_mesh_clear()
    return bvh, mw


def min_dist_bvh(bvh_a, bvh_b):
    # BVH overlap of triangles; if none, sample nearest via find_nearest on a coarse set is expensive.
    overlap = bvh_a.overlap(bvh_b)
    return overlap


def collision_frame(arm, body, swords, body_skip):
    deps = bpy.context.evaluated_depsgraph_get()
    out = {}
    bvhs = {}
    for name, obj in swords.items():
        ev = obj.evaluated_get(deps)
        bvhs[name] = BVHTree.FromObject(ev, deps)
    grip = group_ids(body, ["mixamorig:LeftArm.002", "mixamorig:RightArm.002"], 0.22)
    body_ev = body.evaluated_get(deps)
    me = body_ev.to_mesh()
    mw = body_ev.matrix_world
    verts = [mw @ v.co for v in me.vertices]
    # Drop faces that touch the gripping hand so the seated hilt is not a hit.
    polys = [list(p.vertices) for p in me.polygons if not any(i in grip for i in p.vertices)]
    body_ev.to_mesh_clear()
    body_bvh = BVHTree.FromPolygons(verts, polys)
    for side, name in (("L", "KAELEN_sword_L"), ("R", "KAELEN_sword_R")):
        ov = bvhs[name].overlap(body_bvh)
        out[f"sword{side}_body_tris"] = len(ov)
    ov = bvhs["KAELEN_sword_L"].overlap(bvhs["KAELEN_sword_R"])
    out["sword_sword_tris"] = len(ov)
    # Numeric nearest distance, sampled. Swords are ~17k verts; sample every 8th.
    def samples(obj, step=8):
        ev = obj.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ev.matrix_world
        pts = [mw @ me.vertices[i].co for i in range(0, len(me.vertices), step)]
        ev.to_mesh_clear()
        return pts

    def min_sep(pts, bvh, skip_ids=None, obj_for_ids=None):
        best = 1e9
        hit_n = 0
        for p in pts:
            loc, normal, index, dist = bvh.find_nearest(p)
            if loc is None:
                continue
            if dist < best:
                best = dist
            if dist < 0.0:
                hit_n += 1
        return best, hit_n

    # Positive dist is outside. find_nearest dist is absolute distance, not signed.
    # Use overlap count as the intersection signal and nearest as clearance.
    sL = samples(swords["KAELEN_sword_L"])
    sR = samples(swords["KAELEN_sword_R"])
    dLR = 1e9
    for p in sL:
        loc, nrm, idx, dist = bvhs["KAELEN_sword_R"].find_nearest(p)
        if loc is not None and dist < dLR:
            dLR = dist
    out["sword_sword_cm"] = round(dLR * 100.0, 2)
    for side, pts, skipkey in (("L", sL, "L"), ("R", sR, "R")):
        best = 1e9
        for p in pts:
            loc, nrm, idx, dist = body_bvh.find_nearest(p)
            if loc is None:
                continue
            if dist < best:
                best = dist
        out[f"sword{side}_body_cm"] = round(best * 100.0, 2)
    return out


def main():
    arm = bpy.data.objects[ARM_NAME]
    body = bpy.data.objects["KAELEN_body"]
    swords = {n: bpy.data.objects[n] for n in ("KAELEN_sword_L", "KAELEN_sword_R")}
    before_fp = edit_fingerprint(arm)
    # Head top from the body mesh in rest.
    zmax = max((arm.matrix_world @ body.matrix_world @ v.co).z for v in body.data.vertices) if False else None
    zs = [v.co.z for v in body.data.vertices]
    head_top = max(zs)
    print("HEAD_TOP_Z", round(head_top, 4), "SKELETON", before_fp)

    lengths = bone_lengths(arm)
    print("LENGTHS", {k: (round(a, 4), round(b, 4)) for k, (a, b) in lengths.items()})

    report = {"skeleton_before": before_fp, "head_top_z": head_top, "clips": {}}
    edited_bones = [SIDES[s][r] for s in ("R", "L") for r in ("upper", "fore")]

    for clip, builder in (
        ("attack2_crossthrust", "cross"),
        ("attack_dualslash", "dual"),
    ):
        action = bpy.data.actions[clip]
        f0 = int(round(action.frame_range[0]))
        f1 = int(round(action.frame_range[1]))
        print("SOLVE", clip, f0, f1)
        orig = snapshot_original(arm, action, f0, f1)
        guard_hand = {s: orig[0][f"{s}_hand"].copy() for s in ("R", "L")}
        guard_dir = {s: orig[0][f"{s}_dir"].copy() for s in ("R", "L")}
        assign_slot(arm, action)
        bpy.context.scene.frame_set(f0)
        bpy.context.view_layer.update()
        if builder == "dual":
            ensure_lift_sign(arm)
        GUARD_TWIST.clear()
        PREV_TWIST.clear()
        for spec in SIDES.values():
            for role in ("upper", "fore"):
                n = spec[role]
                _, _, ang = swing_twist(orig[0]["q"][n])
                GUARD_TWIST[n] = ang
                PREV_TWIST[n] = ang
        for f in range(f0, f1 + 1):
            bpy.context.scene.frame_set(f)
            bpy.context.view_layer.update()
            if builder == "cross":
                targets, weight = make_cross_targets(f, orig, f1 - f0 + 1)
                shoulder_lift = None
            else:
                # Raise the clavicle before the hand target is built, so overhead
                # is measured from the lifted shoulder and clears the head.
                for side in ("R", "L"):
                    pb = arm.pose.bones[SIDES[side]["shoulder"]]
                    pb.rotation_quaternion = orig[f - 1]["q"][pb.name]
                bpy.context.view_layer.update()
                for side in ("R", "L"):
                    if side == "R":
                        peak = smooth(1.0 if 8 <= f <= 16 else (0.45 if 4 <= f <= 22 else 0.0))
                    else:
                        peak = smooth(1.0 if 24 <= f <= 32 else (0.45 if 18 <= f <= 40 else 0.0))
                    radians = peak * 0.70 * LIFT_SIGN.get(side, 1.0)
                    pb = arm.pose.bones[SIDES[side]["shoulder"]]
                    if abs(radians) > 1e-4:
                        lift_clavicle(arm, pb, radians)
                    key_quat(pb, f)
                bpy.context.view_layer.update()
                targets, weight = make_dual_targets(arm, f, orig, guard_hand, guard_dir)
                shoulder_lift = None
            base = orig[0] if builder == "dual" else orig[f - 1]
            # If the blades meet, push each hand and sword outward and solve again.
            # This keeps the overhead/down arc and moves the crossing off the other sword.
            saved_twist = dict(PREV_TWIST)
            for attempt in range(4):
                PREV_TWIST.clear()
                PREV_TWIST.update(saved_twist)
                apply_frame(arm, targets, lengths, base, f, weight, shoulder_lift)
                if attempt == 3:
                    break
                c = collision_frame(arm, body, swords, None)
                swords_ok = c["sword_sword_tris"] == 0 and c["sword_sword_cm"] >= 3.5
                body_ok = c["swordL_body_cm"] >= 3.0 and c["swordR_body_cm"] >= 3.0 and c["swordL_body_tris"] == 0 and c["swordR_body_tris"] == 0
                if swords_ok and body_ok:
                    break
                nudged = {}
                for side in ("R", "L"):
                    hand, direc = targets[side]
                    lat = SIDES[side]["lateral"]
                    push = Vector((0.0, 0.0, 0.0))
                    direc_add = Vector((0.0, 0.0, 0.0))
                    if not swords_ok:
                        push += lat * 0.035 + FWD * 0.02
                        direc_add += lat * 0.65 + FWD * 0.25
                    if c[f"sword{side}_body_cm"] < 3.0 or c[f"sword{side}_body_tris"] > 0:
                        push += FWD * 0.05 + UP * 0.02
                        direc_add += FWD * 0.45
                    if direc_add.length < 1e-8:
                        direc_add = lat * 0.2
                    nudged[side] = (hand + push, (direc + direc_add).normalized())
                targets = nudged
                if isinstance(weight, dict):
                    weight = {s: max(weight[s], 0.85) for s in weight}
                else:
                    weight = max(weight, 0.85)
        bones_now = list(edited_bones)
        if builder == "dual":
            bones_now += [SIDES[s]["shoulder"] for s in ("R", "L")]
        continuous_quats(arm, action, bones_now, f0, f1)
        rows = measure(arm, action, f0, f1)
        # collisions on a stride, full integer frames (clips are short)
        col = []
        for f in range(f0, f1 + 1):
            bpy.context.scene.frame_set(f)
            bpy.context.view_layer.update()
            c = collision_frame(arm, body, swords, {"both": set(), "L": set(), "R": set()})
            c["f"] = f
            col.append(c)
        def series(key):
            return [r[key] for r in rows]
        summary = {}
        for side in ("R", "L"):
            tw = series(f"{side}_upper_twist")
            fw = series(f"{side}_fore_twist")
            hz = [r[f"{side}_hand"][2] for r in rows]
            tz = [r[f"{side}_tip"][2] for r in rows]
            summary[side] = {
                "upper_twist_min": min(tw),
                "upper_twist_max": max(tw),
                "upper_twist_span": max(tw) - min(tw),
                "fore_twist_min": min(fw),
                "fore_twist_max": max(fw),
                "fore_twist_span": max(fw) - min(fw),
                "hand_z_min": min(hz),
                "hand_z_max": max(hz),
                "tip_z_min": min(tz),
                "tip_z_max": max(tz),
                "worst_frame": rows[max(range(len(tw)), key=lambda i: abs(tw[i]))]["f"],
                "worst_twist": tw[max(range(len(tw)), key=lambda i: abs(tw[i]))],
            }
        ss = [c["sword_sword_cm"] for c in col]
        summary["sword_sword_min_cm"] = min(ss)
        summary["sword_sword_overlap_frames"] = sum(1 for c in col if c["sword_sword_tris"] > 0)
        for side in ("L", "R"):
            summary[f"sword{side}_body_min_cm"] = min(c[f"sword{side}_body_cm"] for c in col)
            summary[f"sword{side}_body_overlap_frames"] = sum(1 for c in col if c[f"sword{side}_body_tris"] > 0)
        print("RESULT", clip, json.dumps(summary))
        report["clips"][clip] = {"summary": summary, "frames": rows, "collision": col}

    after_fp = edit_fingerprint(arm)
    report["skeleton_after"] = after_fp
    report["skeleton_unchanged"] = before_fp == after_fp
    print("SKELETON_UNCHANGED", report["skeleton_unchanged"], after_fp)
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND, copy=True)
    json.dump(report, open(os.path.join(OUT_DIR, "solve_report.json"), "w"))
    print("SAVED", OUT_BLEND)


if __name__ == "__main__":
    main()
