"""FIX9 diagnosis: anatomical swing-twist vs world-continuous shoulder track.

Reads the FIX8 blend. Does not modify it.
"""
import math
import json
import bpy
from mathutils import Vector, Quaternion, Matrix

OUT = "/home/ubuntu/k9_diag.json"

def q_axis_angle(q: Quaternion):
    q = q.normalized()
    w = max(-1.0, min(1.0, q.w))
    ang = 2.0 * math.acos(w)
    s = math.sqrt(max(0.0, 1.0 - w * w))
    if s < 1e-8:
        axis = Vector((0, 1, 0))
    else:
        axis = Vector((q.x, q.y, q.z)) / s
    if ang > math.pi:
        ang = 2 * math.pi - ang
        axis = -axis
    return ang, axis

def swing_twist(q: Quaternion, axis=Vector((0, 1, 0))):
    """Decompose q into swing * twist, twist about the given axis in this q's space."""
    q = q.normalized()
    axis = axis.normalized()
    xyz = Vector((q.x, q.y, q.z))
    proj = axis * xyz.dot(axis)
    tw = Quaternion((q.w, proj.x, proj.y, proj.z))
    if tw.magnitude < 1e-8:
        tw = Quaternion((1, 0, 0, 0))
    else:
        tw.normalize()
    # pick hemisphere so twist is the short one? NO — keep the sign that matches q
    # so a continuous q yields a continuous twist. Do not flip to shortest.
    sw = q @ tw.inverted()
    sw.normalize()
    # twist angle about axis: 2 atan2(component, w)
    comp = Vector((tw.x, tw.y, tw.z)).dot(axis)
    twist_ang = 2.0 * math.atan2(comp, tw.w)
    return sw, tw, twist_ang

def unwrap(prev, ang):
    if prev is None:
        return ang
    d = ang - prev
    while d > math.pi:
        ang -= 2 * math.pi
        d = ang - prev
    while d < -math.pi:
        ang += 2 * math.pi
        d = ang - prev
    return ang

arm = bpy.data.objects["KAELEN_rig"]
print("ARMATURE", arm.name, "bones", len(arm.data.bones))
for b in arm.data.bones:
    print(f"BONE {b.name} parent={b.parent.name if b.parent else '-'} len={b.length:.4f} "
          f"head={tuple(round(x,3) for x in b.head_local)} tail={tuple(round(x,3) for x in b.tail_local)}")

def action_fcurves(act):
    slot = act.slots[0]
    for layer in act.layers:
        for strip in layer.strips:
            cb = strip.channelbag(slot)
            if cb:
                return cb.fcurves
    return []

print("ACTIONS")
for act in bpy.data.actions:
    fr = act.frame_range
    print(f"ACT {act.name} frames {fr[0]} {fr[1]} fcurves {len(action_fcurves(act))}")

print("OBJECTS")
for ob in bpy.data.objects:
    print(f"OB {ob.name} {ob.type} parent={ob.parent.name if ob.parent else '-'} hide={ob.hide_render}")

# sword local axes relative to hand bones via evaluated rest mesh
def mesh_bounds_world(ob):
    pts = [ob.matrix_world @ v.co for v in ob.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx

bpy.context.view_layer.objects.active = arm
# rest
for act in bpy.data.actions:
    for tr in arm.animation_data.nla_tracks:
        tr.mute = True
arm.animation_data.action = None
bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()

def bone_world(pb):
    return arm.matrix_world @ pb.matrix

report = {"bones": {}, "clips": {}}

# bind sword direction in hand bone space
for side, sword_name, hand_name in (
    ("L", "KAELEN_sword_L", "mixamorig:LeftArm.002"),
    ("R", "KAELEN_sword_R", "mixamorig:RightArm.002"),
):
    sw = bpy.data.objects[sword_name]
    pb = arm.pose.bones[hand_name]
    Mw = bone_world(pb)
    # sword vertices in hand bone space. Bone matrix maps bone-local (Y along bone) to armature.
    # pb.matrix is armature-space bone matrix.
    inv = (arm.matrix_world @ pb.matrix).inverted()
    pts = [(inv @ (sw.matrix_world @ v.co)) for v in sw.data.vertices]
    # farthest from origin ~ tip, nearest along the blade axis
    # use principal direction: from centroid of verts near hand to farthest
    dists = [(p.length, p) for p in pts]
    dists.sort(key=lambda t: t[0])
    near = Vector((0, 0, 0))
    n = max(1, len(dists)//20)
    for _, p in dists[:n]:
        near += p
    near /= n
    tip = dists[-1][1]
    direction = (tip - near).normalized()
    report["bones"][side] = {
        "hand": hand_name,
        "sword_tip_bone_local": [round(x, 4) for x in tip],
        "sword_near_bone_local": [round(x, 4) for x in near],
        "sword_axis_bone_local": [round(x, 4) for x in direction],
        "sword_len_m": round((tip - near).length, 4),
        "nverts": len(pts),
    }
    print("SWORD", side, "axis", direction, "len", (tip-near).length)

ARM_BONES = [
    "mixamorig:LeftShoulder", "mixamorig:LeftArm", "mixamorig:LeftArm.001", "mixamorig:LeftArm.002",
    "mixamorig:RightShoulder", "mixamorig:RightArm", "mixamorig:RightArm.001", "mixamorig:RightArm.002",
]

def eval_clip(action_name):
    act = bpy.data.actions[action_name]
    arm.animation_data.action = act
    f0 = int(round(act.frame_range[0]))
    f1 = int(round(act.frame_range[1]))
    prev_twist = {n: None for n in ARM_BONES}
    prev_world_q = {n: None for n in ARM_BONES}
    frames = []
    world_dot_min = {n: 1.0 for n in ARM_BONES}
    for f in range(f0, f1 + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        rec = {"f": f, "bones": {}}
        for n in ARM_BONES:
            pb = arm.pose.bones[n]
            q = pb.matrix_basis.to_quaternion()
            sw, tw, tang = swing_twist(q, Vector((0, 1, 0)))
            tang_u = unwrap(prev_twist[n], tang)
            prev_twist[n] = tang_u
            raw_deg = math.degrees(tang)
            # world quaternion continuity (the false guarantee)
            W = (arm.matrix_world @ pb.matrix).to_quaternion()
            if prev_world_q[n] is not None:
                dot = prev_world_q[n].dot(W)
                world_dot_min[n] = min(world_dot_min[n], dot)
                if dot < 0:
                    W = Quaternion((-W.w, -W.x, -W.y, -W.z))
            prev_world_q[n] = W.copy()
            head = arm.matrix_world @ pb.head
            tail = arm.matrix_world @ pb.tail
            rec["bones"][n] = {
                "twist_deg": round(math.degrees(tang_u), 3),
                "twist_raw_deg": round(raw_deg, 3),
                "swing_deg": round(math.degrees(q_axis_angle(sw)[0]), 3),
                "basis_deg": round(math.degrees(q_axis_angle(q)[0]), 3),
                "head_z": round(head.z, 4),
                "tail_z": round(tail.z, 4),
                "head": [round(head.x, 4), round(head.y, 4), round(head.z, 4)],
                "tail": [round(tail.x, 4), round(tail.y, 4), round(tail.z, 4)],
                "rotmode": pb.rotation_mode,
            }
        # sword tips world
        for side, sname, hname in (("L", "KAELEN_sword_L", "mixamorig:LeftArm.002"),
                                    ("R", "KAELEN_sword_R", "mixamorig:RightArm.002")):
            swob = bpy.data.objects[sname]
            deps = bpy.context.evaluated_depsgraph_get()
            ev = swob.evaluated_get(deps)
            me = ev.to_mesh()
            mw = ev.matrix_world
            pts = [mw @ v.co for v in me.vertices]
            ev.to_mesh_clear()
            # tip = farthest from hand head
            hand = arm.matrix_world @ arm.pose.bones[hname].head
            tip = max(pts, key=lambda p: (p - hand).length)
            rec["sword_"+side] = {
                "tip": [round(tip.x, 4), round(tip.y, 4), round(tip.z, 4)],
                "tip_z": round(tip.z, 4),
                "hand": [round(hand.x, 4), round(hand.y, 4), round(hand.z, 4)],
            }
        frames.append(rec)
    # summary twist ranges
    summ = {}
    for n in ARM_BONES:
        vals = [fr["bones"][n]["twist_deg"] for fr in frames]
        dmax = max(abs(vals[i]-vals[i-1]) for i in range(1, len(vals))) if len(vals) > 1 else 0
        summ[n] = {
            "twist_min": min(vals),
            "twist_max": max(vals),
            "twist_span": max(vals) - min(vals),
            "twist_end_minus_start": vals[-1] - vals[0],
            "max_step": round(dmax, 3),
            "world_quat_dot_min": round(world_dot_min[n], 4),
            "rotmode": frames[0]["bones"][n]["rotmode"],
        }
    # hand z and sword tip z
    for side in ("L", "R"):
        hz = [fr["sword_"+side]["hand"][2] for fr in frames]
        tz = [fr["sword_"+side]["tip_z"] for fr in frames]
        summ["handz_"+side] = {"min": min(hz), "max": max(hz), "fmax": frames[hz.index(max(hz))]["f"], "fmin": frames[hz.index(min(hz))]["f"]}
        summ["tipz_"+side] = {"min": min(tz), "max": max(tz), "fmax": frames[tz.index(max(tz))]["f"], "fmin": frames[tz.index(min(tz))]["f"]}
    return {"f0": f0, "f1": f1, "summary": summ, "frames": frames}

for name in ("attack2_crossthrust", "attack_dualslash"):
    print("EVAL", name)
    report["clips"][name] = eval_clip(name)
    s = report["clips"][name]["summary"]
    print("SUMMARY", name)
    for k, v in s.items():
        print(" ", k, v)

# rotation modes and key types on right arm for crossthrust
act = bpy.data.actions["attack2_crossthrust"]
print("FCURVES crossthrust arm")
for fc in action_fcurves(act):
    if "Arm" in fc.data_path or "Shoulder" in fc.data_path:
        print(f"  {fc.data_path}[{fc.array_index}] keys={len(fc.keyframe_points)} extrap={fc.extrapolation}")

json.dump(report, open(OUT, "w"))
print("WROTE", OUT)
