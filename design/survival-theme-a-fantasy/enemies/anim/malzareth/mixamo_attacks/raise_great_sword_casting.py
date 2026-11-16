"""Key Malzareth's Great Sword Casting as a fast two-handed overhead chop.

The legs, hips and spine start from the first frame of his idle. The sword
pitches straight through the body's centre line and never turns sideways:
a quick wind-up from a low guard to well past vertical with the tip far behind
his head, a hard chop forward and down until the tip nearly touches the ground,
a short hold on the impact, then a recovery to the guard so the preview loop is
seamless. He bends forward and sinks into his knees on the chop with the feet
planted. The right hand drives RightForeArm.001, which holds the sword, and the
left hand is solved onto the handle just below it on every frame. No bones are
added.
"""
import bpy
import math
import os
from mathutils import Matrix, Quaternion, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
SRC_BLEND = os.path.normpath(os.path.join(HERE, "..", "blender", "MALZARETH.blend"))
IDLE = "idle"
UNITY_DIR = r"C:\Development\paytoplayapp\Assets\ThemePack\fantasy_kingdom_a\art\preview_new_chars\malzareth"
SHEET = os.environ.get("MALZ_SHEET", os.path.join(os.environ.get("TEMP", HERE), "malzareth_gsc"))
ACTION = "attack_great_sword_casting"


def ease(t):
    t = max(0.0, min(1.0, t))
    return 0.5 - 0.5 * math.cos(math.pi * t)


def windup(t):
    """Heavy lift: gets moving quickly, then slows a long way into the top."""
    t = max(0.0, min(1.0, t))
    return ease(t ** 0.7)


def chop(t):
    """Accelerates hard into the impact and stops dead."""
    t = max(0.0, min(1.0, t))
    return 0.85 * t ** 2.4 + 0.15 * ease(t)


def settle(t):
    """Fast out of the hit, slow finish."""
    t = max(0.0, min(1.0, t))
    return 1.0 - (1.0 - t) ** 2


# Key poses. elev: blade angle above forward horizontal (160 = 70 degrees past
# vertical, tip behind). fist: right fist on the handle, armature space.
# lean: forward bend of the spine. drop: how far the hips sink.
GUARD = dict(elev=35.0, fist=Vector((-0.04, -0.40, 1.12)), lean=0.0, drop=0.0, back=0.0)
TOP = dict(elev=158.0, fist=Vector((-0.03, -0.33, 2.04)), lean=math.radians(-10.0), drop=0.0, back=0.0)
COCKED = dict(elev=164.0, fist=Vector((-0.03, -0.31, 2.05)), lean=math.radians(-12.0), drop=0.02, back=0.0)
LOW = dict(elev=-24.0, fist=Vector((-0.03, -0.40, 0.95)), lean=math.radians(40.0), drop=0.22, back=0.07)
SETTLED = dict(elev=-27.0, fist=Vector((-0.03, -0.40, 0.94)), lean=math.radians(41.5), drop=0.235, back=0.075)

# (frames, from, to, easing, forward bulge of the fist path)
SEGMENTS = [
    (15, GUARD, TOP, windup, 0.10),     # wind-up, slower
    (5, TOP, COCKED, ease, 0.0),        # keeps creeping back, then a beat
    (5, COCKED, LOW, chop, 0.14),       # chop, three times as fast as the lift
    (7, LOW, SETTLED, settle, 0.0),     # weight sinks into the hit
    (18, SETTLED, GUARD, ease, 0.04),   # recover
]
TOTAL = sum(s[0] for s in SEGMENTS) + 1
HEAD_KEEP = 0.55  # share of the spine lean the head undoes so he keeps looking ahead

# Sword handle in RightForeArm.001 local space: blade along +X, handle offset along Y and Z.
HANDLE_Y = 0.13
HANDLE_Z = 0.03
LEFT_GRIP_X = -0.12
# Sword axis ends (pommel, tip) along bone X, and a point inside his crown the blade must stay clear of.
POMMEL_X = -0.24
TIP_X = 1.30
CROWN_UP = 0.30
CROWN_CLEAR = float(os.environ.get("MALZ_CROWN_CLEAR", "0.26"))

MAP = {
    "mixamorig:Hips": "mixamorig:Hips",
    "mixamorig:Spine": "mixamorig:Spine",
    "mixamorig:Spine1": "mixamorig:Spine1",
    "mixamorig:Spine2": "mixamorig:Spine2",
    "mixamorig:Neck": "mixamorig:Neck",
    "mixamorig:Head": "mixamorig:Head",
    "mixamorig:LeftShoulder": "mixamorig:LeftShoulder",
    "mixamorig:LeftArm": "mixamorig:LeftArm",
    "mixamorig:LeftForeArm": "mixamorig:LeftForeArm",
    "mixamorig:LeftHand": "mixamorig:LeftHand",
    "mixamorig:RightShoulder": "mixamorig:RightShoulder",
    "mixamorig:RightArm": "mixamorig:RightArm",
    "mixamorig:RightForeArm": "mixamorig:RightForeArm",
    "mixamorig:RightHand": "mixamorig:RightForeArm.001",
    "mixamorig:LeftUpLeg": "mixamorig:LeftUpLeg",
    "mixamorig:LeftLeg": "mixamorig:LeftLeg",
    "mixamorig:LeftFoot": "mixamorig:LeftFoot",
    "mixamorig:RightUpLeg": "mixamorig:RightUpLeg",
    "mixamorig:RightLeg": "mixamorig:RightLeg",
    "mixamorig:RightFoot": "mixamorig:RightFoot",
}
HIPS = "mixamorig:Hips"


def sample(frame):
    """Pose parameters at a frame (1-based)."""
    i = frame - 1
    for frames, a, b, curve, bulge in SEGMENTS:
        if i <= frames:
            t = curve(i / frames)
            fist = a["fist"].lerp(b["fist"], t) + Vector((0.0, -bulge * math.sin(math.pi * t), 0.0))
            out = {k: a[k] + (b[k] - a[k]) * t for k in ("elev", "lean", "drop", "back")}
            out["fist"] = fist
            return out
        i -= frames
    return dict(GUARD)


def basis_from(arm, names, rotations, hips_head):
    bones = arm.data.bones
    built = {}
    out = {}
    for name in names:
        bone = bones[name]
        rest = bone.matrix_local
        if bone.parent and bone.parent.name in built:
            parent = built[bone.parent.name] @ (bone.parent.matrix_local.inverted() @ rest)
            head = parent.translation
        else:
            parent = rest
            head = hips_head
        posed = Matrix.LocRotScale(head, rotations[name], None)
        built[name] = posed
        basis = parent.inverted() @ posed
        if bone.parent:
            basis = Matrix.LocRotScale(Vector((0.0, 0.0, 0.0)), basis.to_quaternion(), None)
        out[name] = basis
    return out, built


def quat_from_axes(x, y, z):
    return Matrix((
        (x.x, y.x, z.x),
        (x.y, y.y, z.y),
        (x.z, y.z, z.z),
    )).to_quaternion()


def swing(q, direction):
    """Turn q the shortest way so its Y axis points along direction."""
    now = q @ Vector((0.0, 1.0, 0.0))
    return (now.rotation_difference(direction.normalized()) @ q).normalized()


def twist_y(q):
    """Twist part of q about the bone's Y axis."""
    t = Quaternion((q.w, 0.0, q.y, 0.0))
    if t.magnitude < 1e-8:
        return Quaternion()
    return t.normalized()


def nelder_mead(f, x0, step, iters):
    n = len(x0)
    pts = [list(x0)]
    for i in range(n):
        p = list(x0)
        p[i] += step[i]
        pts.append(p)
    vals = [f(p) for p in pts]
    for _ in range(iters):
        order = sorted(range(n + 1), key=lambda i: vals[i])
        pts = [pts[i] for i in order]
        vals = [vals[i] for i in order]
        c = [sum(p[j] for p in pts[:-1]) / n for j in range(n)]
        worst = pts[-1]
        xr = [c[j] + (c[j] - worst[j]) for j in range(n)]
        fr = f(xr)
        if fr < vals[0]:
            xe = [c[j] + 2.0 * (c[j] - worst[j]) for j in range(n)]
            fe = f(xe)
            pts[-1], vals[-1] = (xe, fe) if fe < fr else (xr, fr)
        elif fr < vals[-2]:
            pts[-1], vals[-1] = xr, fr
        else:
            xc = [c[j] + 0.5 * (worst[j] - c[j]) for j in range(n)]
            fc = f(xc)
            if fc < vals[-1]:
                pts[-1], vals[-1] = xc, fc
            else:
                best = pts[0]
                pts = [best] + [[best[j] + 0.5 * (p[j] - best[j]) for j in range(n)] for p in pts[1:]]
                vals = [vals[0]] + [f(p) for p in pts[1:]]
    return pts[min(range(n + 1), key=lambda i: vals[i])]


def solve_elbow(root, target, upper_len, fore_len, pole):
    to_t = target - root
    dist = to_t.length
    reach = upper_len + fore_len - 1e-4
    minimum = abs(upper_len - fore_len) + 1e-4
    dist = min(max(dist, minimum), reach)
    to_n = to_t.normalized()
    cos_a = (upper_len * upper_len + dist * dist - fore_len * fore_len) / (2.0 * upper_len * dist)
    cos_a = max(-1.0, min(1.0, cos_a))
    along = cos_a * upper_len
    height = max(0.0, upper_len * upper_len - along * along) ** 0.5
    pole_v = pole - root
    pole_p = pole_v - to_n * pole_v.dot(to_n)
    pole_p.normalize()
    return root + to_n * along + pole_p * height, (target - root).length <= reach


def blade_frame(elev_deg):
    """Blade axis (bone X) and hand axis (bone Y), both in the body's sagittal plane."""
    e = math.radians(elev_deg)
    blade = Vector((0.0, -math.cos(e), math.sin(e)))
    hand = Vector((0.0, -math.sin(e), -math.cos(e)))
    side = blade.cross(hand)
    return blade, hand, side


def assign_action(arm, name):
    if arm.animation_data is None:
        arm.animation_data_create()
    action = bpy.data.actions.new(name)
    arm.animation_data.action = action
    if hasattr(action, "slots"):
        slot = action.slots.new("OBJECT", arm.name) if len(action.slots) == 0 else action.slots[0]
        if hasattr(arm.animation_data, "action_slot"):
            arm.animation_data.action_slot = slot
    return action


def start_pose(arm):
    """Body rotations (armature space) from the first frame of his idle, so the cast starts upright."""
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = bpy.data.actions[IDLE]
    if hasattr(arm.animation_data, "action_slot") and len(bpy.data.actions[IDLE].slots):
        arm.animation_data.action_slot = bpy.data.actions[IDLE].slots[0]
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    rotations = {n: arm.pose.bones[n].matrix.to_quaternion() for n in MAP.values()}
    hips_head = arm.pose.bones[HIPS].head.copy()
    arm.animation_data.action = None
    return rotations, hips_head


def main():
    os.makedirs(SHEET, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=SRC_BLEND)
    arm = bpy.data.objects["MALZARETH_rig"]
    names = [b.name for b in arm.data.bones if b.name in set(MAP.values())]
    base, hips_rest = start_pose(arm)
    for old in list(bpy.data.actions):
        bpy.data.actions.remove(old)
    assign_action(arm, ACTION)

    lens = {n: arm.data.bones[n].length for n in names}
    _b, built0 = basis_from(arm, names, base, hips_rest)
    ankles = {side: built0["mixamorig:%sFoot" % side].to_translation() for side in ("Left", "Right")}
    spine_share = {"mixamorig:Spine": 0.3, "mixamorig:Spine1": 0.65}
    upper_body = {"mixamorig:Spine2", "mixamorig:Neck", "mixamorig:Head",
                  "mixamorig:LeftShoulder", "mixamorig:RightShoulder",
                  "mixamorig:LeftArm", "mixamorig:LeftForeArm", "mixamorig:LeftHand",
                  "mixamorig:RightArm", "mixamorig:RightForeArm", "mixamorig:RightForeArm.001"}
    top_span = TOP["elev"] - GUARD["elev"]

    prev = None
    left_sign = 1.0
    unreached = []
    locks = None
    x_best = [0.0] * 5
    drift = []
    last_key = None
    for frame in range(1, TOTAL + 1):
        s = sample(frame)
        elev, fist, lean = s["elev"], s["fist"], s["lean"]
        blade, hand_y, side = blade_frame(elev)
        hips_head = hips_rest + Vector((0.0, s["back"], -s["drop"]))
        up = max(0.0, min(1.0, (elev - GUARD["elev"]) / top_span))

        rotations = dict(base)
        for name in names:
            if name in spine_share:
                rotations[name] = Quaternion((1.0, 0.0, 0.0), lean * spine_share[name]) @ base[name]
            elif name in upper_body:
                rotations[name] = Quaternion((1.0, 0.0, 0.0), lean) @ base[name]
        rotations["mixamorig:Neck"] = Quaternion((1.0, 0.0, 0.0), lean * (1.0 - HEAD_KEEP * 0.4)) @ base["mixamorig:Neck"]
        rotations["mixamorig:Head"] = Quaternion((1.0, 0.0, 0.0), lean * (1.0 - HEAD_KEEP)) @ base["mixamorig:Head"]

        r_wrist = fist - hand_y * HANDLE_Y - side * HANDLE_Z
        l_fist = fist + blade * LEFT_GRIP_X
        l_wrist = l_fist - hand_y * HANDLE_Y - side * HANDLE_Z
        rotations["mixamorig:RightForeArm.001"] = quat_from_axes(blade, hand_y, side)

        _b, built = basis_from(arm, names, rotations, hips_head)
        ok = True
        # Legs: feet stay planted, knees bend forward as the hips sink.
        for prefix in ("Left", "Right"):
            upper = "mixamorig:%sUpLeg" % prefix
            lower = "mixamorig:%sLeg" % prefix
            hip = built[upper].to_translation()
            ankle = ankles[prefix]
            knee, _r = solve_elbow(hip, ankle, lens[upper], lens[lower], hip + Vector((0.0, -0.6, -0.25)))
            src_u = prev[upper] if prev else rotations[upper]
            src_l = prev[lower] if prev else rotations[lower]
            rotations[upper] = swing(src_u, knee - hip)
            rotations[lower] = swing(src_l, ankle - knee)
        # Both hands are rigid on the handle, so their world rotations follow the sword.
        lh_dir = (hand_y * HANDLE_Y + side * HANDLE_Z).normalized()
        if prev is None:
            cand = []
            for sgn in (1.0, -1.0):
                x = (blade * sgn - lh_dir * lh_dir.dot(blade * sgn)).normalized()
                q = quat_from_axes(x, lh_dir, x.cross(lh_dir))
                cand.append((q.rotation_difference(rotations["mixamorig:LeftHand"]).angle, sgn))
            left_sign = min(cand)[1]
        x = (blade * left_sign - lh_dir * lh_dir.dot(blade * left_sign)).normalized()
        rotations["mixamorig:LeftHand"] = quat_from_axes(x, lh_dir, x.cross(lh_dir))

        # Arms: the wrists are locked to the grip pose of the first frame. The fist
        # may drift off the planned path a little, and each elbow may swing around
        # its shoulder-wrist axis, whatever keeps the wrists closest to that pose.
        sides = []
        for prefix, out, hand_name in (("Right", -1.0, "mixamorig:RightForeArm.001"), ("Left", 1.0, "mixamorig:LeftHand")):
            upper = "mixamorig:%sArm" % prefix
            fore = "mixamorig:%sForeArm" % prefix
            sides.append(dict(
                upper=upper, fore=fore, hand=rotations[hand_name],
                shoulder=built[upper].to_translation(),
                pole_off=Vector((out * 0.55, -0.10 - 0.15 * up, -0.35 + 0.15 * up)),
                src_u=prev[upper] if prev else rotations[upper],
                src_f=prev[fore] if prev else rotations[fore],
            ))

        def wrists_for(offset):
            f = fist + offset
            lf = f + blade * LEFT_GRIP_X
            return (f - hand_y * HANDLE_Y - side * HANDLE_Z, lf - hand_y * HANDLE_Y - side * HANDLE_Z)

        def solve_arm(sd, wrist, theta, lock):
            shoulder = sd["shoulder"]
            elbow, reached = solve_elbow(shoulder, wrist, lens[sd["upper"]], lens[sd["fore"]], shoulder + sd["pole_off"])
            axis = (wrist - shoulder).normalized()
            elbow = shoulder + Quaternion(axis, theta) @ (elbow - shoulder)
            f_q = swing(sd["src_f"], wrist - elbow)
            u_q = swing(sd["src_u"], elbow - shoulder)
            if lock is None:
                return f_q, u_q, 0.0, reached
            w_lock, e_lock = lock
            f_q = f_q @ twist_y(w_lock @ (f_q.inverted() @ sd["hand"]).inverted()).inverted()
            rel = f_q.inverted() @ sd["hand"]
            err = rel.rotation_difference(w_lock).angle
            u_q = u_q @ twist_y(e_lock @ (u_q.inverted() @ f_q).inverted()).inverted()
            return f_q, u_q, err, reached

        if locks is None:
            r_w, l_w = wrists_for(Vector())
            locks = []
            for sd, wrist in zip(sides, (r_w, l_w)):
                f_q, u_q, _e, _r = solve_arm(sd, wrist, 0.0, None)
                locks.append((f_q.inverted() @ sd["hand"], u_q.inverted() @ f_q))

        head_m = built["mixamorig:Head"]
        crown = head_m.to_translation() + head_m.to_quaternion() @ Vector((0.0, CROWN_UP, 0.0))

        def crown_gap(offset):
            a = fist + offset + blade * POMMEL_X
            b = fist + offset + blade * TIP_X
            ab = b - a
            t = max(0.0, min(1.0, (crown - a).dot(ab) / ab.length_squared))
            return (crown - (a + ab * t)).length

        def cost(v):
            offset = Vector((v[0], v[1], v[2]))
            total = 1.5 * offset.length + 0.4 * (v[3] * v[3] + v[4] * v[4])
            total += 40.0 * max(0.0, CROWN_CLEAR - crown_gap(offset))
            total += 1.0 * sum((a - b) ** 2 for a, b in zip(v, x_prev))
            for sd, wrist, theta, lock in zip(sides, wrists_for(offset), (v[3], v[4]), locks):
                _f, _u, err, reached = solve_arm(sd, wrist, theta, lock)
                total += err
                if not reached:
                    total += 5.0 + 20.0 * ((wrist - sd["shoulder"]).length - lens[sd["upper"]] - lens[sd["fore"]])
            return total

        x_prev = list(x_best)
        key = (round(elev, 6), tuple(round(c, 6) for c in fist), round(lean, 6), round(s["drop"], 6))
        if key != last_key:
            # Held poses reuse the last solve so the hands don't jitter.
            x_best = nelder_mead(cost, x_best, [0.03, 0.03, 0.03, 0.3, 0.3], 500)
        last_key = key
        offset = Vector(x_best[:3])
        errs = []
        for sd, wrist, theta, lock in zip(sides, wrists_for(offset), x_best[3:], locks):
            f_q, u_q, err, reached = solve_arm(sd, wrist, theta, lock)
            rotations[sd["fore"]] = f_q
            rotations[sd["upper"]] = u_q
            errs.append(math.degrees(err))
            ok = ok and reached
        drift.append((frame, offset.length, errs + [crown_gap(offset)]))

        bases, built = basis_from(arm, names, rotations, hips_head)
        for name in names:
            pose = arm.pose.bones[name]
            pose.rotation_mode = "QUATERNION"
            pose.matrix_basis = bases[name]
        bpy.context.view_layer.update()
        for name in names:
            pose = arm.pose.bones[name]
            pose.keyframe_insert(data_path="rotation_quaternion", frame=frame)
            if name == HIPS:
                pose.keyframe_insert(data_path="location", frame=frame)
        prev = dict(rotations)
        if not ok:
            unreached.append(frame)

    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = TOTAL
    scene.render.fps = 30
    print("UNREACHED", unreached, "TOTAL", TOTAL)
    for frame, moved, errs in drift:
        print("DRIFT f%02d fist moved %.3f wrist off R %.1f L %.1f deg crown gap %.3f" % (frame, moved, errs[0], errs[1], errs[2]))

    for obj in bpy.data.objects:
        if obj.type == "MESH" and "__" in obj.name:
            obj.hide_render = True
            obj.hide_set(True)
        if obj.name in ("MALZARETH_cape", "MALZARETH_cape_rig"):
            obj.hide_render = True
            obj.hide_set(True)

    sword = bpy.data.objects["MALZARETH_greatsword"]
    body = bpy.data.objects["MALZARETH_body"]
    groups = {g.name: g.index for g in body.vertex_groups}
    head_groups = {groups["mixamorig:Head"], groups["mixamorig:Neck"]}
    head_idx = [v.index for v in body.data.vertices
                if any(g.group in head_groups and g.weight > 0.3 for g in v.groups)]
    starts = [1]
    for seg in SEGMENTS:
        starts.append(starts[-1] + seg[0])
    marks = sorted(set(starts + [8, 22, 23, 24]))
    lowest_sword = []
    for frame in range(1, TOTAL + 1):
        scene.frame_set(frame)
        deps = bpy.context.evaluated_depsgraph_get()
        ev = sword.evaluated_get(deps)
        pts = [ev.matrix_world @ v.co for v in ev.data.vertices]
        low_s = min(p.z for p in pts)
        lowest_sword.append(low_s)
        if frame not in marks:
            continue
        bone_m = arm.matrix_world @ arm.pose.bones["mixamorig:RightForeArm.001"].matrix
        d = (bone_m.to_quaternion() @ Vector((1.0, 0.0, 0.0))).normalized()
        rh = bone_m.to_translation()
        lh = arm.matrix_world @ arm.pose.bones["mixamorig:LeftHand"].head
        elev = math.degrees(math.atan2(d.z, -d.y))
        body_ev = body.evaluated_get(deps)
        bpts = [body_ev.matrix_world @ v.co for v in body_ev.data.vertices]
        head_pts = [bpts[i] for i in head_idx]
        near = min((v - q).length for q in pts[::20] for v in head_pts[::10])
        print("f%d elev %.1f dir(%.2f,%.2f,%.2f) lh-rh %.2f sword-low %.3f sword-head %.3f body-low %.3f" % (
            frame, elev, d.x, d.y, d.z, (lh - rh).length, low_s, near, min(v.z for v in bpts)))
    print("SWORD LOWEST", round(min(lowest_sword), 3), "at f", lowest_sword.index(min(lowest_sword)) + 1)

    cam_data = bpy.data.cameras.new("CastCam")
    cam = bpy.data.objects.new("CastCam", cam_data)
    scene.collection.objects.link(cam)
    target = bpy.data.objects.new("CastTarget", None)
    scene.collection.objects.link(target)
    target.location = (0.0, -0.3, 1.25)
    con = cam.constraints.new("TRACK_TO")
    con.target = target
    con.track_axis = "TRACK_NEGATIVE_Z"
    con.up_axis = "UP_Y"
    scene.camera = cam
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 360
    scene.render.resolution_y = 520
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    for view, loc in (("side", (-5.6, -0.3, 1.4)), ("threeq", (-3.6, -4.3, 1.5))):
        cam.location = loc
        for frame in marks:
            scene.frame_set(frame)
            scene.render.filepath = os.path.join(SHEET, "%s_f%03d.png" % (view, frame))
            bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)

    if os.environ.get("MALZ_PREVIEW_ONLY"):
        return

    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    body.select_set(True)
    sword.select_set(True)
    bpy.context.view_layer.objects.active = arm
    for folder in (HERE, UNITY_DIR):
        path = os.path.join(folder, "MALZARETH_attack_great_sword_casting.fbx")
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
        print("EXPORTED", path, os.path.getsize(path))

    blend_path = os.path.join(HERE, "MALZARETH_great_sword_casting.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("SAVED", blend_path, "frames", TOTAL)


if __name__ == "__main__":
    main()
