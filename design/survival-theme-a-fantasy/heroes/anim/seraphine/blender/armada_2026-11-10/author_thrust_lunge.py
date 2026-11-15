"""Rebuild Seraphine's attack_thrust_slash as a spear lunge with weight behind it.

Reads a source clip for its ready pose (frame 1) and lunge pose (frame 25),
then re-keys 45 frames with IK on both arms and legs:

  1-5    guard, spear point aimed at the target
  5-11   chamber: sink, turn the right shoulder back, draw the spear to the ribs,
         left hand reaches forward to aim
  11-17  drive: front (left) foot steps, hips and right shoulder turn into the
         thrust, right arm extends along the shaft line, left hand pulls back
  17-23  extension, small overshoot and settle
  23-37  recoil: spear pulled back, front foot recovers
  37-45  back to guard (loops)

The back foot stays planted. The spear (hand local +Z) stays pointed at the
target throughout, so the tip travels along its own line instead of chopping
down.

The source must be the clip from commit ee783954, not this script's output
(re-running on its own output would read the wrong frame 25):

  git show ee783954:Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine/SERAPHINE_attack_thrust_slash.fbx > src.fbx
  blender --background --factory-startup --python author_thrust_lunge.py -- <out.fbx> src.fbx
"""
import bpy
import math
import sys
from mathutils import Matrix, Quaternion, Vector

ART = r"C:\Development\paytoplayapp\Assets\ThemePack\fantasy_kingdom_a\art\preview_new_chars\seraphine"
SRC = ART + r"\SERAPHINE_attack_thrust_slash.fbx"
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else SRC
SRC = argv[1] if len(argv) > 1 else SRC

FIRST, LAST = 0, 44  # FBX time 0..44; the curve keys below are numbered 1..45
P = "mixamorig:"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
before = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=SRC, use_anim=True, anim_offset=0.0, automatic_bone_orientation=False, ignore_leaf_bones=False)
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
    return t * t * (3 - 2 * t)


def track(keys):
    """keys: [(frame, value, ease_into_this_key)]; value is float or Vector."""
    def at(f):
        if f <= keys[0][0]:
            return keys[0][1]
        for (f0, v0, _), (f1, v1, kind) in zip(keys, keys[1:]):
            if f <= f1:
                t = ease(kind, (f - f0) / (f1 - f0))
                return v0 + (v1 - v0) * t if not isinstance(v0, Vector) else v0.lerp(v1, t)
        return keys[-1][1]
    return at


V = Vector
r_ready = READY[P + "RightHand"][2].translation
l_ready = READY[P + "LeftHand"][2].translation
lf_ready = READY[P + "LeftUpLeg.002"][2].translation
rf_ready = READY[P + "RightUpLeg.002"][2].translation
hips_ready = READY[P + "Hips"][2].translation

# Forward (toward the enemy) is -Y. Her right side is -X.
blend_w = track([(1, 0.0, "s"), (5, 0.0, "s"), (11, 0.0, "s"), (17, 1.0, "in"), (19, 1.0, "out"),
                 (23, 1.0, "s"), (29, 0.55, "out"), (37, 0.1, "s"), (45, 0.0, "s")])
# Turn about world Z, degrees. Negative turns the right shoulder back.
twist = track([(1, 0.0, "s"), (5, -2.0, "s"), (11, -22.0, "s"), (17, 18.0, "in"), (19, 21.0, "out"),
               (23, 18.0, "s"), (29, 4.0, "out"), (37, 0.0, "s"), (45, 0.0, "s")])
hips_loc = track([(1, hips_ready, "s"), (5, hips_ready + V((0, 0.01, -0.01)), "s"),
                  (11, hips_ready + V((0.0, 0.07, -0.04)), "s"),
                  (13, hips_ready + V((0.0, 0.03, -0.05)), "in"),
                  (17, hips_ready + V((-0.02, -0.27, -0.09)), "in"),
                  (19, hips_ready + V((-0.02, -0.30, -0.10)), "out"),
                  (23, hips_ready + V((-0.02, -0.27, -0.09)), "s"),
                  (29, hips_ready + V((-0.01, -0.15, -0.06)), "out"),
                  (37, hips_ready + V((0.0, -0.02, -0.01)), "s"),
                  (45, hips_ready, "s")])
rhand = track([(1, r_ready, "s"), (5, r_ready + V((0.0, 0.03, 0.0)), "s"),
               (11, V((-0.25, 0.22, 0.98)), "s"),
               (17, V((-0.14, -1.05, 1.10)), "in"),
               (19, V((-0.13, -1.10, 1.09)), "out"),
               (23, V((-0.14, -1.05, 1.10)), "s"),
               (29, V((-0.27, -0.22, 1.00)), "out"),
               (37, r_ready + V((0.0, -0.02, 0.01)), "s"),
               (45, r_ready, "s")])
# Spear direction (hand local +Z). Aimed at a target's chest about 2 m out.
spear = track([(1, V((0.04, -1.0, 0.10)), "s"), (11, V((0.05, -1.0, 0.03)), "s"),
               (17, V((0.07, -1.0, -0.02)), "in"), (23, V((0.07, -1.0, -0.02)), "s"),
               (29, V((0.05, -1.0, 0.04)), "out"), (45, V((0.04, -1.0, 0.10)), "s")])
lhand = track([(1, l_ready, "s"), (5, l_ready, "s"),
               (11, V((0.08, -0.22, 1.06)), "s"),
               (17, V((0.26, 0.16, 0.96)), "in"),
               (23, V((0.26, 0.14, 0.95)), "s"),
               (29, V((0.18, 0.06, 0.93)), "out"),
               (45, l_ready, "s")])
lfoot = track([(1, lf_ready, "s"), (11, lf_ready, "s"),
               (13, lf_ready + V((0.0, -0.08, 0.07)), "s"),
               (16, V((lf_ready.x + 0.01, -0.46, lf_ready.z + 0.02)), "in"),
               (17, V((lf_ready.x + 0.01, -0.47, lf_ready.z)), "s"),
               (29, V((lf_ready.x + 0.01, -0.47, lf_ready.z)), "s"),
               (33, V((lf_ready.x, -0.32, lf_ready.z + 0.06)), "s"),
               (37, lf_ready, "s"), (45, lf_ready, "s")])

# Leg length, hip joint to ankle, at rest. Used to keep the hips reachable.
def leg_len(side):
    up = READY[P + side + "UpLeg"][2].translation
    return (READY[P + side + "UpLeg.001"][2].translation - up).length + \
           (READY[P + side + "UpLeg.002"][2].translation - READY[P + side + "UpLeg.001"][2].translation).length


L = {"Left": leg_len("Left"), "Right": leg_len("Right")}
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
for key, bone, loc, pole_loc in [
    ("rh", "RightForeArm", r_ready, V((-0.75, 0.55, 0.55))),
    ("lh", "LeftForeArm", l_ready, V((0.75, 0.55, 0.55))),
    ("lf", "LeftUpLeg.001", lf_ready, V((0.15, -1.2, 0.55))),
    ("rf", "RightUpLeg.001", rf_ready, V((-0.15, -1.2, 0.55))),
]:
    t = empty("T_" + key, loc)
    pl = empty("P_" + key, pole_loc)
    targets[key] = (t, pl, ik(P + bone, t, pl))

# IK reaches the tail of the forearm / shin, which is the hand / ankle head.
# Hand and foot orientation come from Copy Rotation empties in world space.
rot_targets = {}
for key, bone in [("rhr", "RightHand"), ("lfr", "LeftUpLeg.002"), ("rfr", "RightUpLeg.002")]:
    e = empty("R_" + key, V())
    e.rotation_mode = "QUATERNION"
    e.rotation_quaternion = READY[P + bone][2].to_quaternion()
    c = pbs[P + bone].constraints.new("COPY_ROTATION")
    c.target = e
    rot_targets[key] = e


def pick_pole_angle(key, elbow_bone):
    """Choose the pole angle that puts the elbow/knee where the ready pose has it."""
    c = targets[key][2]
    want = READY[P + elbow_bone][2].translation
    best = None
    for deg in range(-180, 180, 5):
        c.pole_angle = math.radians(deg)
        bpy.context.view_layer.update()
        err = ((MW @ pbs[P + elbow_bone].head) - want).length
        if best is None or err < best[0]:
            best = (err, deg)
    c.pole_angle = math.radians(best[1])
    print("POLE", key, best[1], "err cm", round(best[0] * 100, 2))


# ---------------------------------------------------------------- author
new = bpy.data.actions.new("attack_thrust_slash")
rig.animation_data.action = new
if hasattr(new, "slots") and hasattr(rig.animation_data, "action_slot"):
    slot = new.slots.new("OBJECT", rig.name)
    rig.animation_data.action_slot = slot

FK = [pb.name for pb in pbs if pb.name not in {
    P + "RightArm", P + "RightForeArm", P + "LeftArm", P + "LeftForeArm",
    P + "LeftUpLeg", P + "LeftUpLeg.001", P + "RightUpLeg", P + "RightUpLeg.001"}]


def turn_world(name, deg):
    m = world(name)
    pivot = m.translation.copy()
    r = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(deg), 4, "Z") @ Matrix.Translation(-pivot)
    pbs[name].matrix = MWI @ (r @ m)
    bpy.context.view_layer.update()


def hand_rot(d):
    z = d.normalized()
    x = (V((-1.0, 0.0, 0.0)) - z * z.x).normalized()
    y = z.cross(x)
    return Matrix((x, y, z)).transposed().to_quaternion()


def set_pose(frame):
    f = frame + 1
    w = blend_w(f)
    for name in FK:
        _, q0, _ = READY[name]
        _, q1, _ = LUNGE[name]
        pbs[name].rotation_quaternion = q0.slerp(q1, max(0.0, min(w, 1.0)))
    bpy.context.view_layer.update()

    # Hips: wanted location, lowered if either leg could not reach its foot.
    want = hips_loc(f).copy()
    feet = {"Left": lfoot(f), "Right": rf_ready}
    for side, foot in feet.items():
        j = want + hip_off[side]
        horiz = V((j.x - foot.x, j.y - foot.y, 0)).length
        reach = 0.985 * L[side]
        if horiz < reach:
            max_z = foot.z + math.sqrt(reach * reach - horiz * horiz) - hip_off[side].z
            want.z = min(want.z, max_z)
    hm = world(P + "Hips")
    hm.translation = want
    pbs[P + "Hips"].matrix = MWI @ hm
    bpy.context.view_layer.update()

    tw = twist(f)
    turn_world(P + "Hips", tw * 0.45)
    turn_world(P + "Spine1", tw * 0.30)
    turn_world(P + "Spine2", tw * 0.25)
    turn_world(P + "Neck", -tw * 0.55)   # eyes stay on the target

    targets["rh"][0].location = rhand(f)
    targets["lh"][0].location = lhand(f)
    targets["lf"][0].location = lfoot(f)
    targets["rf"][0].location = rf_ready
    rot_targets["rhr"].rotation_quaternion = hand_rot(spear(f))
    # Front foot turns out a little when planted forward; back foot stays.
    step = max(0.0, min(1.0, (lf_ready.y - lfoot(f).y) / 0.3))
    rot_targets["lfr"].rotation_quaternion = Quaternion((0, 0, 1), math.radians(-6 * step)) @ READY[P + "LeftUpLeg.002"][2].to_quaternion()
    bpy.context.view_layer.update()


scene.frame_start, scene.frame_end = FIRST, LAST
scene.frame_set(FIRST)
set_pose(FIRST)
pick_pole_angle("rh", "RightForeArm")
pick_pole_angle("lh", "LeftForeArm")
pick_pole_angle("lf", "LeftUpLeg.001")
pick_pole_angle("rf", "RightUpLeg.001")

for f in range(FIRST, LAST + 1):
    scene.frame_set(f)
    set_pose(f)
    for name in FK:
        pbs[name].keyframe_insert("rotation_quaternion", frame=f)
    pbs[P + "Hips"].keyframe_insert("location", frame=f)
    for key in ("rh", "lh", "lf", "rf"):
        targets[key][0].keyframe_insert("location", frame=f)
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

# ---------------------------------------------------------------- report
print("LEG", {k: round(v, 3) for k, v in L.items()})
for f in range(FIRST, LAST + 1, 2):
    scene.frame_set(f)  # printed as FBX frame
    h = MW.to_3x3() @ pbs[P + "RightHand"].matrix.to_3x3()
    z = h.col[2].normalized()
    rh = MW @ pbs[P + "RightHand"].head
    tip = rh + z * (2.02 - 0.42)
    print(f, "hips", tuple(round(c, 2) for c in MW @ pbs[P + "Hips"].head),
          "rh", tuple(round(c, 2) for c in rh), "tip", tuple(round(c, 2) for c in tip),
          "pitch", round(math.degrees(math.asin(max(-1, min(1, z.z)))), 1),
          "lf", tuple(round(c, 2) for c in MW @ pbs[P + "LeftUpLeg.002"].head),
          "rf", tuple(round(c, 2) for c in MW @ pbs[P + "RightUpLeg.002"].head))

for o in bpy.data.objects:
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
