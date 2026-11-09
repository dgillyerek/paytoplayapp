"""Reauthor attack_dualslash only.

Both arms rise together to a broad overhead, then both come down in a wide
vertical scissor. The sword is not slid on the mesh. Every sword vertex is
already weight 1 on Arm.002, so that bone's pose matrix is the rigid socket.
The bind axis is captured once in the hand bone and then aimed with that
matrix. Vertices, weights, and the runtime rest mesh are not written.

Does not touch attack2_crossthrust, edit bones, mesh, or weights.
"""
import math
import os
import json
import hashlib
import bpy
from mathutils import Vector, Quaternion, Matrix
from mathutils.bvhtree import BVHTree

OUT = os.environ.get("KAELEN_FIX9_OUT", "/home/ubuntu/k9_out/dual2")
os.makedirs(OUT, exist_ok=True)

SIDES = {
    "R": {"shoulder": "mixamorig:RightShoulder", "upper": "mixamorig:RightArm",
          "fore": "mixamorig:RightArm.001", "hand": "mixamorig:RightArm.002",
          "sword": "KAELEN_sword_R", "lateral": Vector((-1, 0, 0))},
    "L": {"shoulder": "mixamorig:LeftShoulder", "upper": "mixamorig:LeftArm",
          "fore": "mixamorig:LeftArm.001", "hand": "mixamorig:LeftArm.002",
          "sword": "KAELEN_sword_L", "lateral": Vector((1, 0, 0))},
}
FWD = Vector((0, -1, 0))
UP = Vector((0, 0, 1))
# Keep the wrist cuff aligned with the fingers. A large forearm roll is what
# made the palm look open and the blade look like it was cutting through it.
FORE_LIMIT = math.radians(22.0)
FORE_STEP = math.radians(8.0)
UPPER_LIMIT = math.radians(50.0)
UPPER_STEP = math.radians(14.0)


def smooth(t):
    t = 0.0 if t < 0 else (1.0 if t > 1 else t)
    return t * t * (3 - 2 * t)


def wrap(a):
    while a > math.pi:
        a -= 2 * math.pi
    while a < -math.pi:
        a += 2 * math.pi
    return a


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
    comp = Vector((tw.x, tw.y, tw.z)).dot(axis)
    return 2 * math.atan2(comp, tw.w)


def twist_about(axis, current, target):
    axis = axis.normalized()
    c = current - axis * current.dot(axis)
    t = target - axis * target.dot(axis)
    if c.length < 1e-6 or t.length < 1e-6:
        return 0.0
    c.normalize()
    t.normalize()
    return math.atan2(axis.dot(c.cross(t)), c.dot(t))


def mesh_fingerprint(obj):
    h = hashlib.sha256()
    h.update(str(len(obj.data.vertices)).encode())
    for v in obj.data.vertices:
        h.update(f"{v.co.x:.6f},{v.co.y:.6f},{v.co.z:.6f}".encode())
    return h.hexdigest()[:16]


def bone_hash(arm):
    h = hashlib.sha256()
    for b in arm.data.bones:
        h.update(b.name.encode())
        h.update((b.parent.name if b.parent else "-").encode())
        for v in list(b.head_local) + list(b.tail_local):
            h.update(f"{v:.6f}".encode())
        h.update(f"{b.matrix_local}".encode())
    return h.hexdigest()[:16]


def action_hash(action):
    h = hashlib.sha256()
    slot = action.slots[0]
    for layer in action.layers:
        for strip in layer.strips:
            cb = strip.channelbag(slot)
            if not cb:
                continue
            for fc in cb.fcurves:
                h.update(fc.data_path.encode())
                h.update(bytes([fc.array_index]))
                for k in fc.keyframe_points:
                    h.update(f"{k.co.x:.4f}:{k.co.y:.6f}".encode())
    return h.hexdigest()[:16]


def assign(arm, action):
    arm.animation_data.action = action
    arm.animation_data.action_slot = action.slots[0]
    for tr in arm.animation_data.nla_tracks:
        tr.mute = True


def rest_world(pb):
    if pb.parent:
        return pb.parent.matrix @ pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local
    return pb.bone.matrix_local.copy()


def set_world_rot(arm, pb, rot3, head_world):
    M = rot3.to_4x4()
    M.translation = head_world
    pb.matrix = arm.matrix_world.inverted() @ M
    if pb.bone.use_connect:
        pb.location = Vector()


def local_twist(pb):
    return swing_twist(pb.matrix_basis.to_quaternion())


def force_twist(arm, pb, target):
    cur = local_twist(pb)
    delta = wrap(target - cur)
    if abs(delta) < 1e-4:
        return
    y = ((arm.matrix_world @ pb.matrix).to_3x3() @ Vector((0, 1, 0))).normalized()
    head = arm.matrix_world @ pb.head
    R = (arm.matrix_world @ pb.matrix).to_3x3()
    set_world_rot(arm, pb, Quaternion(y, delta).to_matrix() @ R, head)


PREV = {}


def limit_twist(arm, pb, guard, limit, step):
    cur = local_twist(pb)
    prev = PREV.get(pb.name, guard)
    rel = wrap(cur - prev)
    rel = max(-step, min(step, rel))
    new = max(guard - limit, min(guard + limit, prev + rel))
    force_twist(arm, pb, new)
    PREV[pb.name] = local_twist(pb)


def ik_elbow(shoulder, hand, ua, fa, pole):
    span = hand - shoulder
    dist = span.length
    maxd = ua + fa - 0.01
    mind = abs(ua - fa) + 0.02
    dist = min(max(dist, mind), maxd)
    direction = span.normalized() if span.length > 1e-6 else UP
    hand_c = shoulder + direction * dist
    cos_a = (ua * ua + dist * dist - fa * fa) / (2 * ua * dist)
    cos_a = max(-1, min(1, cos_a))
    along = ua * cos_a
    height = math.sqrt(max(0, ua * ua - along * along))
    n = direction.cross(pole - shoulder)
    if n.length < 1e-6:
        n = direction.cross(UP)
    n.normalize()
    side = n.cross(direction)
    if side.dot(pole - shoulder) < 0:
        side = -side
    side.normalize()
    return shoulder + direction * along + side * height, hand_c


def aim_from_rest(arm, pb, target, pole):
    bpy.context.view_layer.update()
    head = arm.matrix_world @ pb.head
    Rrest = arm.matrix_world.to_3x3() @ rest_world(pb).to_3x3()
    rest_y = (Rrest @ Vector((0, 1, 0))).normalized()
    desired = target - head
    if desired.length < 1e-6:
        desired = rest_y
    desired.normalize()
    q = rest_y.rotation_difference(desired)
    R0 = q.to_matrix() @ Rrest
    y = desired
    rest_x = R0 @ Vector((1, 0, 0))
    rest_x = rest_x - y * y.dot(rest_x)
    hint = (pole - head) - y * y.dot(pole - head)
    if rest_x.length > 1e-6 and hint.length > 1e-6:
        delta = wrap(twist_about(y, rest_x.normalized(), hint.normalized()))
        R0 = Quaternion(y, delta).to_matrix() @ R0
    set_world_rot(arm, pb, R0, head)


def sword_samples(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    pts = [mw @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    return pts


def sword_tip_hilt(pts, hand):
    tip = max(pts, key=lambda p: (p - hand).length)
    near = sorted(pts, key=lambda p: (p - hand).length)
    hilt = sum(near[: max(30, len(near)//40)], Vector()) / max(30, len(near)//40)
    axis = tip - hilt
    if axis.length < 1e-6:
        axis = tip - hand
    return tip, hilt, axis.normalized()


def lift_clavicle(arm, pb, radians):
    M = pb.matrix.copy()
    head = M.to_translation()
    q = Quaternion(FWD, radians)
    T = Matrix.Translation(head)
    pb.matrix = T @ q.to_matrix().to_4x4() @ T.inverted() @ M
    if pb.location.length > 5e-3:
        pb.location = Vector()


def group_ids(obj, names, thresh):
    gi = {g.name: g.index for g in obj.vertex_groups}
    ids = set()
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


def collision(arm, body, swords, grip):
    deps = bpy.context.evaluated_depsgraph_get()
    bvhs = {}
    for n, ob in swords.items():
        bvhs[n] = BVHTree.FromObject(ob.evaluated_get(deps), deps)
    ev = body.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    verts = [mw @ v.co for v in me.vertices]
    polys = [list(p.vertices) for p in me.polygons if not any(i in grip for i in p.vertices)]
    ev.to_mesh_clear()
    body_bvh = BVHTree.FromPolygons(verts, polys)
    out = {}
    out["sword_sword_tris"] = len(bvhs["KAELEN_sword_L"].overlap(bvhs["KAELEN_sword_R"]))
    for side, name in (("L", "KAELEN_sword_L"), ("R", "KAELEN_sword_R")):
        out[f"sword{side}_body_tris"] = len(bvhs[name].overlap(body_bvh))
    def samples(ob, step=6):
        ev = ob.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ev.matrix_world
        pts = [mw @ me.vertices[i].co for i in range(0, len(me.vertices), step)]
        ev.to_mesh_clear()
        return pts
    sL, sR = samples(swords["KAELEN_sword_L"]), samples(swords["KAELEN_sword_R"])
    d = 1e9
    for p in sL:
        loc, n, i, dist = bvhs["KAELEN_sword_R"].find_nearest(p)
        if loc is not None and dist < d:
            d = dist
    out["sword_sword_cm"] = round(d * 100, 2)
    for side, pts in (("L", sL), ("R", sR)):
        best = 1e9
        for p in pts:
            loc, n, i, dist = body_bvh.find_nearest(p)
            if loc is not None and dist < best:
                best = dist
        out[f"sword{side}_body_cm"] = round(best * 100, 2)
    return out


def main():
    arm = bpy.data.objects["KAELEN_rig"]
    body = bpy.data.objects["KAELEN_body"]
    swords = {n: bpy.data.objects[n] for n in ("KAELEN_sword_L", "KAELEN_sword_R")}
    before_bones = bone_hash(arm)
    cross = bpy.data.actions["attack2_crossthrust"]
    cross_before = action_hash(cross)
    dual = bpy.data.actions["attack_dualslash"]
    f0, f1 = 1, int(round(dual.frame_range[1]))
    assert f1 == 58, f1
    assign(arm, dual)
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    guard_q = {}
    guard_twist = {}
    for spec in SIDES.values():
        for role in ("upper", "fore", "hand", "shoulder"):
            n = spec[role]
            guard_q[n] = arm.pose.bones[n].rotation_quaternion.copy()
            if role in ("upper", "fore"):
                guard_twist[n] = swing_twist(guard_q[n])
                PREV[n] = guard_twist[n]
    # clavicle lift sign
    lift_sign = {}
    for side, spec in SIDES.items():
        pb = arm.pose.bones[spec["shoulder"]]
        base = pb.matrix.copy()
        z0 = (arm.matrix_world @ pb.tail).z
        chosen = 1.0
        for sign in (1.0, -1.0):
            pb.matrix = base.copy()
            lift_clavicle(arm, pb, sign * 0.4)
            bpy.context.view_layer.update()
            z1 = (arm.matrix_world @ pb.tail).z
            pb.matrix = base.copy()
            bpy.context.view_layer.update()
            if z1 > z0 + 0.008:
                chosen = sign
                break
        lift_sign[side] = chosen
    print("LIFT", lift_sign)
    lengths = {s: (arm.pose.bones[SIDES[s]["upper"]].bone.length,
                   arm.pose.bones[SIDES[s]["fore"]].bone.length) for s in SIDES}
    # sword identity at rest. These buffers must still match at the end.
    sword_ids = {}
    mesh_before = {"KAELEN_body": mesh_fingerprint(body)}
    for side, spec in SIDES.items():
        ob = swords[spec["sword"]]
        sword_ids[side] = (ob.name, len(ob.data.vertices),
                           round(sum(v.co.length for v in ob.data.vertices), 3))
        mesh_before[ob.name] = mesh_fingerprint(ob)
        # A Child Of / bone parent on top of this skin would move the sword twice.
        if ob.constraints:
            raise RuntimeError(f"{ob.name} already has a constraint; do not also offset its vertices")

    def targets_for(frame, shoulders):
        # 1-5 guard, 5-16 both up, 16-20 hold high, 20-34 both down,
        # 34-40 hold low, 40-58 recover. 58 frames, no duration change.
        t_up = smooth((frame - 5) / 11) if frame >= 5 else 0.0
        if frame < 16:
            phase, u = "up", t_up
        elif frame < 20:
            phase, u = "high", 1.0
        elif frame < 34:
            phase, u = "down", smooth((frame - 20) / 14)
        elif frame < 40:
            phase, u = "low", 1.0
        else:
            phase, u = "recover", smooth((frame - 40) / 18)
        out = {}
        for side in ("R", "L"):
            S = shoulders[side]
            lat = SIDES[side]["lateral"]
            # Depth offset: left blade rides 9 cm further forward so the scissor
            # is broad and the blades do not occupy the same plane.
            depth = 0.10 if side == "L" else 0.0
            # Overhead spends reach going up and out. The down cut spends it
            # going down, so the hands and the blades both finish lower than the guard.
            high = S + lat * 0.30 + FWD * (0.04 + depth) + UP * 0.52
            low = S + lat * 0.16 + FWD * (0.16 + depth * 0.4) + UP * (-0.46)
            high_dir = (lat * 0.42 + FWD * 0.12 + UP * 0.90).normalized()
            low_dir = (lat * 0.15 + FWD * 0.35 + UP * (-0.92)).normalized()
            if phase == "up":
                hand = Vector(guard_hand[side]).lerp(high, u)
                direc = Vector(guard_dir[side]).lerp(high_dir, u).normalized()
            elif phase == "high":
                hand, direc = high, high_dir
            elif phase == "down":
                hand = high.lerp(low, u)
                direc = high_dir.lerp(low_dir, u).normalized()
            elif phase == "low":
                hand, direc = low, low_dir
            else:
                hand = low.lerp(guard_hand[side], u)
                direc = low_dir.lerp(guard_dir[side], u).normalized()
            # reach clamp around the (possibly lifted) shoulder
            ua, fa = lengths[side]
            reach = ua + fa - 0.012
            delta = hand - S
            if delta.length > reach:
                hand = S + delta.normalized() * reach
            out[side] = (hand, direc)
        return out, phase

    # guard hand/sword from frame 1 before we touch keys
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    guard_hand = {}
    guard_dir = {}
    for side, spec in SIDES.items():
        hand = arm.matrix_world @ arm.pose.bones[spec["hand"]].head
        pts = sword_samples(swords[spec["sword"]])
        tip, hilt, axis = sword_tip_hilt(pts, hand)
        guard_hand[side] = hand.copy()
        guard_dir[side] = axis.copy()
    # Bind socket: sword axis in the hand bone, at the guard pose. Later frames
    # reconstruct the axis with the hand matrix only. The mesh is not sampled
    # again for aiming, and it is never written.
    bind_axis = {}
    for side, spec in SIDES.items():
        pb = arm.pose.bones[spec["hand"]]
        hand_m = arm.matrix_world @ pb.matrix
        bind_axis[side] = (hand_m.inverted().to_3x3() @ guard_dir[side]).normalized()
        print("SOCKET", side, [round(c, 5) for c in bind_axis[side]])

    grip = group_ids(body, ["mixamorig:LeftArm.002", "mixamorig:RightArm.002"], 0.22)

    def solve_frame(frame, hand_targets):
        for side in ("R", "L"):
            spec = SIDES[side]
            upper = arm.pose.bones[spec["upper"]]
            fore = arm.pose.bones[spec["fore"]]
            hand_pb = arm.pose.bones[spec["hand"]]
            # lock the grip orientation every frame
            hand_pb.rotation_quaternion = guard_q[spec["hand"]]
            bpy.context.view_layer.update()
            shoulder = arm.matrix_world @ upper.head
            ua, fa = lengths[side]
            pole = shoulder + SIDES[side]["lateral"] * 0.5 + Vector((0, 0.16, 0.22))
            elbow, hand_c = ik_elbow(shoulder, hand_targets[side][0], ua, fa, pole)
            aim_from_rest(arm, upper, elbow, pole)
            bpy.context.view_layer.update()
            limit_twist(arm, upper, guard_twist[spec["upper"]], UPPER_LIMIT, UPPER_STEP)
            bpy.context.view_layer.update()
            elbow_w = arm.matrix_world @ upper.tail
            aim_from_rest(arm, fore, hand_c, pole)
            bpy.context.view_layer.update()
            # Aim with the rigid socket. Hand local rotation stays the guard,
            # so the fist and the sword (same bone) keep the bind offset.
            hand_pb.rotation_quaternion = guard_q[spec["hand"]]
            bpy.context.view_layer.update()
            Rhand = (arm.matrix_world @ hand_pb.matrix).to_3x3()
            cur_axis = (Rhand @ bind_axis[side]).normalized()
            y = ((arm.matrix_world @ fore.matrix).to_3x3() @ Vector((0, 1, 0))).normalized()
            delta = wrap(twist_about(y, cur_axis, hand_targets[side][1]))
            if abs(delta) > 1e-3:
                R = (arm.matrix_world @ fore.matrix).to_3x3()
                set_world_rot(arm, fore, Quaternion(y, delta).to_matrix() @ R, elbow_w)
                bpy.context.view_layer.update()
            limit_twist(arm, fore, guard_twist[spec["fore"]], FORE_LIMIT, FORE_STEP)
            bpy.context.view_layer.update()
            hand_pb.rotation_quaternion = guard_q[spec["hand"]]

    rows = []
    for f in range(f0, f1 + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        # restore clavicles then lift on the way up
        for side, spec in SIDES.items():
            arm.pose.bones[spec["shoulder"]].rotation_quaternion = guard_q[spec["shoulder"]]
        bpy.context.view_layer.update()
        # provisional shoulders for the phase, then lift, then real targets
        shoulders = {s: arm.matrix_world @ arm.pose.bones[SIDES[s]["upper"]].head for s in SIDES}
        _, phase = targets_for(f, shoulders)
        lift = 0.78 if phase in ("up", "high", "down") else 0.0
        if phase == "up":
            lift *= smooth((f - 5) / 11) if f >= 5 else 0
        if phase == "down":
            lift *= 1 - smooth((f - 20) / 14)
        if phase == "recover":
            lift = 0
        for side, spec in SIDES.items():
            if abs(lift) > 1e-3:
                lift_clavicle(arm, arm.pose.bones[spec["shoulder"]], lift * lift_sign[side])
            arm.pose.bones[spec["shoulder"]].keyframe_insert("rotation_quaternion", frame=f)
        bpy.context.view_layer.update()
        shoulders = {s: arm.matrix_world @ arm.pose.bones[SIDES[s]["upper"]].head for s in SIDES}
        tg, phase = targets_for(f, shoulders)
        # separation retries
        for attempt in range(5):
            solve_frame(f, tg)
            for spec in SIDES.values():
                for role in ("upper", "fore", "hand"):
                    arm.pose.bones[spec[role]].keyframe_insert("rotation_quaternion", frame=f)
            bpy.context.view_layer.update()
            c = collision(arm, body, swords, grip)
            ok = (c["sword_sword_tris"] == 0 and c["sword_sword_cm"] >= 4.0
                  and c["swordL_body_tris"] == 0 and c["swordR_body_tris"] == 0
                  and c["swordL_body_cm"] >= 3.0 and c["swordR_body_cm"] >= 3.0)
            if ok or attempt == 4:
                c["f"] = f
                c["attempt"] = attempt
                break
            nudged = {}
            for side in ("R", "L"):
                hand, direc = tg[side]
                lat = SIDES[side]["lateral"]
                push = lat * 0.03 + FWD * (0.03 if side == "L" else 0.0)
                if c[f"sword{side}_body_cm"] < 3 or c[f"sword{side}_body_tris"]:
                    push += FWD * 0.04 + UP * 0.03
                nudged[side] = (hand + push, (direc + lat * 0.25 + FWD * 0.2).normalized())
            tg = nudged
        # record
        rec = {"f": f, "phase": phase, **{k: c[k] for k in c}}
        for side, spec in SIDES.items():
            hand = arm.matrix_world @ arm.pose.bones[spec["hand"]].head
            pts = sword_samples(swords[spec["sword"]])
            tip, hilt, axis = sword_tip_hilt(pts, hand)
            rec[f"{side}_hand_z"] = round(hand.z, 4)
            rec[f"{side}_tip_z"] = round(tip.z, 4)
            rec[f"{side}_upper"] = round(math.degrees(local_twist(arm.pose.bones[spec["upper"]])), 2)
            rec[f"{side}_fore"] = round(math.degrees(local_twist(arm.pose.bones[spec["fore"]])), 2)
            rec[f"{side}_sword"] = swords[spec["sword"]].name
            rec[f"{side}_hilt_palm_cm"] = None
        rows.append(rec)

    after_bones = bone_hash(arm)
    cross_after = action_hash(cross)
    mesh_after = {"KAELEN_body": mesh_fingerprint(body)}
    for spec in SIDES.values():
        mesh_after[spec["sword"]] = mesh_fingerprint(swords[spec["sword"]])
    if mesh_after != mesh_before:
        raise RuntimeError(f"mesh vertices changed {mesh_before} -> {mesh_after}")
    # continuous quat signs on dual only
    assign(arm, dual)
    prev = {n: None for spec in SIDES.values() for n in (spec["upper"], spec["fore"], spec["hand"], spec["shoulder"])}
    for f in range(f0, f1 + 1):
        bpy.context.scene.frame_set(f)
        for n, p in prev.items():
            pb = arm.pose.bones[n]
            q = pb.rotation_quaternion.copy()
            if p is not None and p.dot(q) < 0:
                q.negate()
                pb.rotation_quaternion = q
                pb.keyframe_insert("rotation_quaternion", frame=f)
            prev[n] = q.copy()

    summary = {
        "frames": 58,
        "skeleton_unchanged": before_bones == after_bones,
        "skeleton_hash": after_bones,
        "mesh_vertices_unchanged": mesh_after == mesh_before,
        "mesh_fingerprint": mesh_after,
        "socket": "Arm.002 pose matrix times the bind axis; vertices not written",
        "crossthrust_action_unchanged": cross_before == cross_after,
        "crossthrust_hash": cross_after,
        "sword_identity": sword_ids,
        "hand_z_max": {s: max(r[f"{s}_hand_z"] for r in rows) for s in "RL"},
        "hand_z_min": {s: min(r[f"{s}_hand_z"] for r in rows) for s in "RL"},
        "tip_z_max": {s: max(r[f"{s}_tip_z"] for r in rows) for s in "RL"},
        "tip_z_min": {s: min(r[f"{s}_tip_z"] for r in rows) for s in "RL"},
        "upper_twist_span": {s: round(max(r[f"{s}_upper"] for r in rows) - min(r[f"{s}_upper"] for r in rows), 2) for s in "RL"},
        "fore_twist_span": {s: round(max(r[f"{s}_fore"] for r in rows) - min(r[f"{s}_fore"] for r in rows), 2) for s in "RL"},
        "sword_sword_min_cm": min(r["sword_sword_cm"] for r in rows),
        "sword_sword_overlap_frames": sum(1 for r in rows if r["sword_sword_tris"]),
        "swordL_body_min_cm": min(r["swordL_body_cm"] for r in rows),
        "swordR_body_min_cm": min(r["swordR_body_cm"] for r in rows),
        "body_overlap_frames": sum(1 for r in rows if r["swordL_body_tris"] or r["swordR_body_tris"]),
    }
    # high frame = both tips high, low frame = both tips low
    def both_high(r):
        return min(r["L_tip_z"], r["R_tip_z"])
    def both_low(r):
        return max(r["L_tip_z"], r["R_tip_z"])
    summary["high_frame"] = max(rows, key=both_high)["f"]
    summary["low_frame"] = min(rows, key=both_low)["f"]
    print("SUMMARY", json.dumps(summary, indent=2))
    json.dump({"summary": summary, "frames": rows}, open(os.path.join(OUT, "dual_report.json"), "w"), indent=2)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "KAELEN_fix9b.blend"), copy=True)
    print("SAVED", os.path.join(OUT, "KAELEN_fix9b.blend"))


if __name__ == "__main__":
    main()
