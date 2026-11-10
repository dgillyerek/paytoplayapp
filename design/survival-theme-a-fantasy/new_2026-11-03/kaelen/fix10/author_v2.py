"""Author a more legible, full-body Kaelen attack pair and export review FBXs.

Always run against the pre-FIX10 KAELEN.blend so body keys are not applied twice.
"""
import os, sys, json, math
import bpy
from mathutils import Vector, Quaternion, Matrix

ROOT = "C:/Development/paytoplayapp"
FIXDIR = ROOT + "/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix10"
FIX9 = ROOT + "/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9"
os.makedirs(FIXDIR + "/fbx", exist_ok=True)
sys.path.insert(0, FIX9)
import k9_fix as ik
import k9_present as present

FWD = Vector((0, -1, 0))
UP = Vector((0, 0, 1))

def pole_front(side, shoulder, hand):
    """Elbow stays out to the side and in front. Default pole's +Y sits behind the chest and inverts an overhead."""
    lat = ik.SIDES[side]["lateral"]
    return shoulder + lat * 0.55 + FWD * 0.28 + UP * 0.12


ik.pole_for = pole_front
ARM = bpy.data.objects["KAELEN_rig"]
BODY = bpy.data.objects["KAELEN_body"]
SWORDS = {n: bpy.data.objects[n] for n in ("KAELEN_sword_L", "KAELEN_sword_R")}
BODY_BONES = [
    "mixamorig:Hips", "mixamorig:Spine", "mixamorig:Spine1", "mixamorig:Spine2",
    "mixamorig:LeftShoulder", "mixamorig:RightShoulder",
]


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def curve(f, pts):
    if f <= pts[0][0]:
        return pts[0][1]
    if f >= pts[-1][0]:
        return pts[-1][1]
    for (a, x), (b, y) in zip(pts, pts[1:]):
        if a <= f <= b:
            return x + (y - x) * smooth((f - a) / (b - a))
    return pts[-1][1]


def assign(a):
    ik.assign_slot(ARM, a)


def key(pb, f):
    pb.keyframe_insert(data_path="location", frame=f)
    pb.keyframe_insert(data_path="rotation_quaternion", frame=f)


def snapshot_body(action, f0, f1):
    assign(action)
    rows = []
    for f in range(f0, f1 + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        rec = {"q": {}, "loc": {}}
        names = list(BODY_BONES)
        for spec in ik.SIDES.values():
            names += [spec["upper"], spec["fore"], spec["hand"]]
        for name in names:
            pb = ARM.pose.bones[name]
            rec["q"][name] = pb.rotation_quaternion.copy()
            rec["loc"][name] = pb.location.copy()
        rows.append(rec)
    return rows


def restore(snap):
    for name, q in snap["q"].items():
        ARM.pose.bones[name].rotation_quaternion = q.copy()
    for name, loc in snap["loc"].items():
        ARM.pose.bones[name].location = loc.copy()
    bpy.context.view_layer.update()


def apply_body(kind, f, snap):
    restore(snap)
    hips = ARM.pose.bones["mixamorig:Hips"]
    sp = ARM.pose.bones["mixamorig:Spine"]
    sp1 = ARM.pose.bones["mixamorig:Spine1"]
    sp2 = ARM.pose.bones["mixamorig:Spine2"]
    if kind == "cross":
        hy = curve(f, [(1, 0), (6, -12), (12, 4), (17, 10), (22, 4), (32, 0), (48, 0)])
        sy = curve(f, [(1, 0), (6, 10), (12, -4), (17, -9), (22, -3), (32, 0), (48, 0)])
        pitch = curve(f, [(1, 0), (6, -4), (12, 6), (17, 12), (22, 6), (32, 0), (48, 0)])
        forward = curve(f, [(1, 0), (6, 0), (12, 0.07), (17, 0.10), (22, 0.06), (32, 0), (48, 0)])
        dip = curve(f, [(1, 0), (8, 0), (15, -0.04), (17, -0.045), (24, 0), (48, 0)])
    else:
        hy = curve(f, [(1, 0), (6, -14), (13, 12), (19, 4), (24, -12), (31, 12), (38, 3), (46, 0), (58, 0)])
        sy = curve(f, [(1, 0), (6, 12), (13, -10), (19, -3), (24, 10), (31, -10), (38, -3), (46, 0), (58, 0)])
        pitch = curve(f, [(1, 0), (6, -3), (13, 8), (19, 3), (24, -3), (31, 8), (38, 3), (46, 0), (58, 0)])
        forward = curve(f, [(1, 0), (8, 0), (13, 0.04), (19, 0.015), (26, 0.04), (34, 0.015), (42, 0), (58, 0)])
        dip = curve(f, [(1, 0), (8, 0), (13, -0.03), (19, -0.01), (26, -0.03), (34, -0.01), (42, 0), (58, 0)])
    hips.rotation_quaternion = Quaternion((0, 1, 0), math.radians(hy)) @ snap["q"][hips.name]
    sp.rotation_quaternion = Quaternion((0, 1, 0), math.radians(-sy * 0.35)) @ Quaternion((1, 0, 0), math.radians(pitch * 0.28)) @ snap["q"][sp.name]
    sp1.rotation_quaternion = Quaternion((0, 1, 0), math.radians(-sy * 0.40)) @ Quaternion((1, 0, 0), math.radians(pitch * 0.40)) @ snap["q"][sp1.name]
    sp2.rotation_quaternion = Quaternion((0, 1, 0), math.radians(-sy * 0.25)) @ Quaternion((1, 0, 0), math.radians(pitch * 0.32)) @ snap["q"][sp2.name]
    bpy.context.view_layer.update()
    world = ARM.matrix_world @ hips.matrix
    world.translation += FWD * forward + UP * dip
    hips.matrix = ARM.matrix_world.inverted() @ world
    bpy.context.view_layer.update()
    for pb in (hips, sp, sp1, sp2):
        key(pb, f)


def guard_data(orig):
    off = {}
    direc = {}
    for s in ("R", "L"):
        shoulder = ARM.matrix_world @ ARM.pose.bones[ik.SIDES[s]["upper"]].head
        off[s] = orig[0][f"{s}_hand"] - shoulder
        direc[s] = orig[0][f"{s}_dir"].normalized()
    return off, direc


def dual_targets(f, shoulders, off, gd):
    out = {}
    for s in ("R", "L"):
        lat = ik.SIDES[s]["lateral"]
        S = shoulders[s]
        guard = S + off[s]
        ra, peak, hit, rec = (3, 9, 16, 23) if s == "R" else (18, 24, 31, 40)
        high = guard + lat * 0.14 + FWD * 0.08 + UP * 0.36
        low = guard + (-lat) * 0.10 + FWD * 0.18 + UP * (-0.22)
        hdir = (lat * 0.10 + FWD * 0.25 + UP * 0.96).normalized()
        cdir = ((-lat) * 0.35 + FWD * 0.45 + UP * (-0.82)).normalized()
        if f < ra:
            hand, d = guard, gd[s]
        elif f <= peak:
            t = smooth((f - ra) / (peak - ra))
            hand = guard.lerp(high, t)
            d = gd[s].lerp(hdir, t).normalized()
        elif f <= hit:
            t = smooth((f - peak) / (hit - peak))
            hand = high.lerp(low, t)
            d = hdir.lerp(cdir, t).normalized()
        elif f <= rec:
            t = smooth((f - hit) / (rec - hit))
            hand = low.lerp(guard, t)
            d = cdir.lerp(gd[s], t).normalized()
        else:
            hand, d = guard, gd[s]
        out[s] = (hand, d)
    return out


def cross_targets(f, shoulders, off, gd):
    out = {}
    if f <= 7:
        phase, t = "draw", smooth((f - 1) / 6)
    elif f <= 16:
        phase, t = "drive", smooth((f - 7) / 9)
    elif f <= 19:
        phase, t = "impact", 1.0
    elif f <= 34:
        phase, t = "recover", smooth((f - 19) / 15)
    else:
        phase, t = "settle", smooth((f - 34) / 14)
    for s in ("R", "L"):
        lat = ik.SIDES[s]["lateral"]
        S = shoulders[s]
        guard = S + off[s]
        chamber = guard - FWD * 0.10 + lat * 0.03 + UP * 0.03
        hit = guard + FWD * 0.22 + lat * 0.05 + UP * (-0.10)
        fwd = FWD.normalized()
        if phase == "draw":
            hand = guard.lerp(chamber, t)
            d = gd[s].lerp(fwd, t * 0.5).normalized()
        elif phase == "drive":
            hand = chamber.lerp(hit, t)
            d = gd[s].lerp(fwd, t).normalized()
        elif phase == "impact":
            hand, d = hit, fwd
        elif phase == "recover":
            hand = hit.lerp(guard, t)
            d = fwd.lerp(gd[s], t).normalized()
        else:
            hand, d = guard, gd[s]
        out[s] = (hand, d)
    return out


def solve_clearance(targets, lengths, kind):
    saved_prev = dict(ik.PREV_TWIST)
    current = targets
    last = None
    for _ in range(12):
        ik.PREV_TWIST.clear()
        ik.PREV_TWIST.update(saved_prev)
        for s in ("R", "L"):
            ik.solve_side(ARM, s, current[s][0], current[s][1], *lengths[s])
        bpy.context.view_layer.update()
        col = ik.collision_frame(ARM, BODY, SWORDS, None)
        last = col
        body_ok = (
            col["swordL_body_tris"] == 0 and col["swordR_body_tris"] == 0
            and col["swordL_body_cm"] >= 3.0 and col["swordR_body_cm"] >= 3.0
        )
        pair_ok = col["sword_sword_tris"] == 0 and col["sword_sword_cm"] >= 3.0
        if body_ok and pair_ok:
            return current, col
        moved = {}
        for s in ("R", "L"):
            hand, direction = current[s]
            lat = ik.SIDES[s]["lateral"]
            push = Vector((0, 0, 0))
            turn = Vector((0, 0, 0))
            if col[f"sword{s}_body_tris"] or col[f"sword{s}_body_cm"] < 3.0:
                push += lat * 0.045 + UP * 0.02
                if kind == "dual":
                    push += FWD * 0.03
                    turn += lat * 0.12 + FWD * 0.20
                else:
                    turn += FWD * 0.55
            if not pair_ok:
                push += lat * 0.03
                turn += lat * 0.25
            moved[s] = (hand + push, (direction + turn).normalized())
        current = moved
    return current, last


def author(name, kind):
    action = bpy.data.actions[name]
    f0 = int(action.frame_range[0])
    f1 = int(action.frame_range[1])
    orig = ik.snapshot_original(ARM, action, f0, f1)
    body = snapshot_body(action, f0, f1)
    assign(action)
    bpy.context.scene.frame_set(f0)
    bpy.context.view_layer.update()
    off, gd = guard_data(orig)
    lengths = ik.bone_lengths(ARM)
    ik.GUARD_TWIST.clear()
    ik.PREV_TWIST.clear()
    for spec in ik.SIDES.values():
        for role in ("upper", "fore"):
            pb = ARM.pose.bones[spec[role]]
            _, _, ang = ik.swing_twist(pb.rotation_quaternion)
            ik.GUARD_TWIST[pb.name] = ang
            ik.PREV_TWIST[pb.name] = ang
    if kind == "dual":
        ik.ensure_lift_sign(ARM)
    edited = list(BODY_BONES)
    for spec in ik.SIDES.values():
        edited += [spec["upper"], spec["fore"], spec["shoulder"]]
    rows = []
    for f in range(f0, f1 + 1):
        apply_body(kind, f, body[f - f0])
        if kind == "dual":
            for s, spec in ik.SIDES.items():
                t = smooth((f - 3) / 6) * (1 - smooth((f - 9) / 7)) if s == "R" else smooth((f - 18) / 6) * (1 - smooth((f - 24) / 7))
                pb = ARM.pose.bones[spec["shoulder"]]
                if t > 0.001:
                    ik.lift_clavicle(ARM, pb, 0.18 * t * ik.LIFT_SIGN.get(s, 1))
                pb.keyframe_insert(data_path="rotation_quaternion", frame=f)
        else:
            for spec in ik.SIDES.values():
                ARM.pose.bones[spec["shoulder"]].keyframe_insert(data_path="rotation_quaternion", frame=f)
        bpy.context.view_layer.update()
        shoulders = ik.live_shoulders(ARM)
        targets = dual_targets(f, shoulders, off, gd) if kind == "dual" else cross_targets(f, shoulders, off, gd)
        targets, col = solve_clearance(targets, lengths, kind)
        for s in ("R", "L"):
            for role in ("upper", "fore"):
                ARM.pose.bones[ik.SIDES[s][role]].keyframe_insert(data_path="rotation_quaternion", frame=f)
        col["frame"] = f
        rows.append(col)
    ik.continuous_quats(ARM, action, edited, f0, f1)
    overlaps = [r for r in rows if r["swordL_body_tris"] or r["swordR_body_tris"] or r["sword_sword_tris"]]
    result = {
        "action": name,
        "kind": kind,
        "frames": f1 - f0 + 1,
        "overlap_frames": [r["frame"] for r in overlaps],
        "min_clearance_cm": {k: min(r[k] for r in rows) for k in ("swordL_body_cm", "swordR_body_cm", "sword_sword_cm")},
    }
    print("MOTION_RESULT", json.dumps(result))
    if overlaps:
        raise RuntimeError(f"Geometry overlap in {name}: {overlaps[:4]}")
    return result


def main():
    results = [author("attack_dualslash", "dual"), author("attack2_crossthrust", "cross")]
    outblend = FIXDIR + "/KAELEN_fix10.blend"
    bpy.ops.wm.save_as_mainfile(filepath=outblend, copy=True)
    refs = {}
    for clip in ("attack_dualslash", "attack2_crossthrust"):
        refs[clip] = present.sample_poses(clip)
        present.export_clip(clip, FIXDIR + "/fbx/KAELEN_" + clip + ".fbx")
    json.dump(results, open(FIXDIR + "/motion_report.json", "w"), indent=2)
    roundtrip = {
        clip: present.compare_reimport(FIXDIR + "/fbx/KAELEN_" + clip + ".fbx", refs[clip], clip)
        for clip in ("attack_dualslash", "attack2_crossthrust")
    }
    json.dump(roundtrip, open(FIXDIR + "/roundtrip.json", "w"), indent=2, default=str)
    print("ROUNDTRIP", json.dumps(roundtrip))
    if any(v.get("max_length_err_m", 1) > 0.001 for v in roundtrip.values()):
        raise RuntimeError("FBX round-trip >1 mm arm-length error")
    print("AUTHOR_DONE", outblend)


if __name__ == "__main__":
    main()
