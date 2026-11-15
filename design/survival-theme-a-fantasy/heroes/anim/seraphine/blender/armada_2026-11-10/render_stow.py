"""Blender playback of the Seraphine staff stow, using the same numbers as NewCharsPreview.

Spine2 mount local position (-0.185664, -0.488309, -0.501672)
local rotation quaternion xyzw (-0.014996, 0.008718, 0.500424, 0.865607)
Stow 0.20 s, return delay 0.14 s then 0.20 s, arc 0.60 m along the spine's local -Z.
Pose crossfade is the preview's 0.22 s. Plates are labeled afterwards.

  blender --background --factory-startup --python render_stow.py
"""
import bpy
import math
import os
import numpy as np
from mathutils import Matrix, Quaternion, Vector

REPO = "/agent/repos/paytoplayapp"
ART = os.path.join(REPO, "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine")
OUT = os.path.dirname(os.path.abspath(__file__))
FRAMES = "/tmp/sera/stow_frames"
os.makedirs(FRAMES, exist_ok=True)

MOUNT_POS = np.array([-0.185664, -0.488309, -0.501672], np.float64)
# mathutils Quaternion is (w, x, y, z). Unity stores (x, y, z, w).
MOUNT_Q = Quaternion((0.865607, -0.014996, 0.008718, 0.500424))
STOW = 0.20
RET_DELAY = 0.14
RET = 0.20
POSE = 0.22
ARC = 0.60

IMPORT = dict(
    global_scale=1.0, use_anim=True, automatic_bone_orientation=False,
    ignore_leaf_bones=False, use_custom_normals=True, use_image_search=False,
)
BODY = {
    "mixamorig:Hips", "mixamorig:Spine", "mixamorig:Spine1", "mixamorig:Spine2",
    "mixamorig:Neck", "mixamorig:Head", "mixamorig:HeadTop_End",
    "mixamorig:LeftUpLeg", "mixamorig:LeftUpLeg.001", "mixamorig:LeftUpLeg.002", "mixamorig:LeftUpLeg.003",
    "mixamorig:RightUpLeg", "mixamorig:RightUpLeg.001", "mixamorig:RightUpLeg.002", "mixamorig:RightUpLeg.003",
}
RARM = {"mixamorig:RightShoulder", "mixamorig:RightArm", "mixamorig:RightForeArm"}
LARM = {"mixamorig:LeftShoulder", "mixamorig:LeftArm", "mixamorig:LeftForeArm", "mixamorig:LeftHand"}


def fib(n):
    i = np.arange(n, dtype=np.float64)
    phi = (1.0 + 5.0 ** 0.5) / 2.0
    z = 1.0 - (2.0 * i + 1.0) / n
    r = np.sqrt(np.maximum(0.0, 1.0 - z * z))
    th = 2.0 * math.pi * i / phi
    d = np.stack([r * np.cos(th), r * np.sin(th), z], 1)
    return np.vstack([d, -d, np.eye(3), -np.eye(3)])


DIRS = fib(360)


def gap(a, b):
    g = (b @ DIRS.T).min(0) - (a @ DIRS.T).max(0)
    i = int(np.argmax(g))
    best = float(g[i])
    n = DIRS[i].copy()
    step = 0.16
    rng = np.random.default_rng(4)
    for _ in range(12):
        pert = n + rng.normal(0.0, step, size=(20, 3))
        pert /= np.linalg.norm(pert, axis=1, keepdims=True)
        gg = (b @ pert.T).min(0) - (a @ pert.T).max(0)
        j = int(np.argmax(gg))
        if gg[j] > best + 1e-7:
            best = float(gg[j])
            n = pert[j]
            step *= 0.7
        else:
            step *= 0.5
        if step < 1.5e-4:
            break
    return best


def dom_mask(obj, wanted):
    names = [g.name for g in obj.vertex_groups]
    ids = {names.index(n) for n in wanted if n in names}
    mask = np.zeros(len(obj.data.vertices), dtype=bool)
    for v in obj.data.vertices:
        if not v.groups:
            continue
        best = max(v.groups, key=lambda g: g.weight)
        mask[v.index] = best.group in ids
    return mask


def world_pts(obj, mask=None):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    co = np.empty((len(me.vertices), 3))
    me.vertices.foreach_get("co", co.ravel())
    ev.to_mesh_clear()
    w = (np.c_[co, np.ones(len(co))] @ np.array(ev.matrix_world).T)[:, :3]
    return w if mask is None else w[mask]


def hull(pts):
    return pts[np.unique(np.argmax(pts @ DIRS.T, 0))]


def smooth(u):
    u = max(0.0, min(1.0, u))
    return u * u * (3.0 - 2.0 * u)


bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "OBJECT"
scene.display.shading.background_type = "VIEWPORT"
scene.display.shading.background_color = (0.55, 0.62, 0.46)
scene.render.resolution_x = 960
scene.render.resolution_y = 720

bpy.ops.import_scene.fbx(filepath=os.path.join(ART, "SERAPHINE_rig_rest.fbx"), **IMPORT)
rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
spear = next(o for o in bpy.data.objects if o.type == "MESH" and "spear" in o.name.lower())
body = next(o for o in bpy.data.objects if o.type == "MESH" and "body" in o.name.lower())
wings = [o for o in bpy.data.objects if o.type == "MESH" and "wing" in o.name.lower()]
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    if "spear" in obj.name.lower():
        obj.color = (0.86, 0.55, 0.12, 1)
    elif "wing" in obj.name.lower():
        obj.color = (0.93, 0.93, 0.88, 1)
    else:
        obj.color = (0.74, 0.58, 0.44, 1)
mb, mr, ml = dom_mask(body, BODY), dom_mask(body, RARM), dom_mask(body, LARM)
for pb in rig.pose.bones:
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.location = (0, 0, 0)
bpy.context.view_layer.update()


def bone_world(name):
    return np.array(rig.matrix_world @ rig.pose.bones[name].matrix, np.float64)


H0 = bone_world("mixamorig:RightHand")
staff = (np.c_[world_pts(spear), np.ones(len(spear.data.vertices))] @ np.linalg.inv(H0).T)[:, :3]
staff_h = hull(staff)
mesh = bpy.data.meshes.new("staff_rigid")
mesh.from_pydata([tuple(p) for p in staff], [], [tuple(p.vertices) for p in spear.data.polygons])
rigid = bpy.data.objects.new("staff_rigid", mesh)
scene.collection.objects.link(rigid)
rigid.color = (0.90, 0.55, 0.10, 1)
spear.hide_render = True

bpy.ops.import_scene.fbx(filepath=os.path.join(ART, "SERAPHINE_cape.fbx"), **IMPORT)
cape_arm = next(o for o in bpy.data.objects if o.type == "ARMATURE" and "cape" in o.name)
cape_mesh = next(o for o in bpy.data.objects if o.type == "MESH" and "cape" in o.name)
cape_mesh.color = (0.45, 0.28, 0.55, 1)
cr = np.array(cape_arm.pose.bones["cape_root"].matrix, np.float64)


def align_cape(sw):
    cape_arm.matrix_world = Matrix((sw @ np.linalg.inv(cr)).tolist())
    bpy.context.view_layer.update()


def load_action(path):
    before = {o.name for o in bpy.data.objects}
    bpy.ops.import_scene.fbx(filepath=path, **IMPORT)
    objs = [bpy.data.objects[n] for n in {o.name for o in bpy.data.objects} - before]
    arm = next(o for o in objs if o.type == "ARMATURE")
    for obj in objs:
        obj.hide_render = True
    return arm.animation_data.action


if rig.animation_data is None:
    rig.animation_data_create()
idle_act = load_action(os.path.join(ART, "SERAPHINE_idle.fbx"))
walk_act = load_action(os.path.join(ART, "SERAPHINE_walk.fbx"))
thrust_act = load_action(os.path.join(ART, "SERAPHINE_attack_thrust_slash.fbx"))
armada_act = load_action(os.path.join(ART, "SERAPHINE_attack_armada.fbx"))


def use(act, frame):
    rig.animation_data.action = act
    try:
        rig.animation_data.action_slot = act.slots[0]
    except Exception:
        pass
    scene.frame_set(int(frame))
    bpy.context.view_layer.update()


def capture():
    out = {}
    for pb in rig.pose.bones:
        local = (pb.parent.matrix.inverted() @ pb.matrix) if pb.parent else pb.matrix.copy()
        out[pb.name] = local.copy()
    return out


def apply(a, b, w):
    if rig.animation_data:
        rig.animation_data.action = None
    for pb in rig.pose.bones:
        la, lb = a[pb.name], b[pb.name]
        loc = la.to_translation().lerp(lb.to_translation(), w)
        quat = la.to_quaternion().slerp(lb.to_quaternion(), w)
        local = Matrix.LocRotScale(loc, quat, Vector((1, 1, 1)))
        pb.matrix = (pb.parent.matrix @ local) if pb.parent else local
    bpy.context.view_layer.update()


def pin():
    rig.location = (0, 0, 0)
    bpy.context.view_layer.update()
    rest = rig.data.bones["mixamorig:Hips"].head_local
    head = rig.matrix_world @ rig.pose.bones["mixamorig:Hips"].head
    rig.location.x += rest.x - head.x
    rig.location.y += rest.y - head.y
    bpy.context.view_layer.update()


def mount_matrix():
    m = np.eye(4)
    m[:3, :3] = np.array(MOUNT_Q.to_matrix())
    m[:3, 3] = MOUNT_POS
    return m


def place_staff(weight):
    sw = bone_world("mixamorig:Spine2")
    hw = bone_world("mixamorig:RightHand")
    world = sw @ mount_matrix()
    p0, p1 = hw[:3, 3], world[:3, 3]
    q0 = Quaternion(Matrix(hw[:3, :3].tolist()).to_quaternion())
    q1 = Quaternion(Matrix(world[:3, :3].tolist()).to_quaternion())
    p = (1.0 - weight) * p0 + weight * p1
    q = q0.slerp(q1, weight)
    back = -sw[:3, 2]
    back = back / np.linalg.norm(back)
    p = p + back * (math.sin(math.pi * weight) * ARC)
    rigid.matrix_world = Matrix.LocRotScale(Vector(p.tolist()), q, Vector((1, 1, 1)))
    align_cape(sw)
    return sw


def parts():
    return (
        hull(world_pts(body, mb)),
        hull(world_pts(body, mr)),
        hull(world_pts(body, ml)),
        hull(np.vstack([world_pts(w) for w in wings])),
        hull(world_pts(cape_mesh)),
    )


def measure(tag, mins):
    pts = hull(world_pts(rigid))
    names = ("body", "right_arm", "left_arm", "wings", "cape")
    vals = parts()
    rec = {}
    for name, cloud in zip(names, vals):
        g = gap(pts, cloud)
        rec[name] = g
        slot = mins[name]
        if g < slot["dist"]:
            slot["dist"] = g
            slot["where"] = tag
    return rec


mins = {k: {"dist": 1e9, "where": ""} for k in ("body", "right_arm", "left_arm", "wings", "cape")}

ref = np.load("/tmp/sera/mount_final.npz")["M"]
got = mount_matrix()
print("mount matrix err", float(np.max(np.abs(ref - got))), flush=True)

# in-hand unchanged
for name, act, frame in (("idle", idle_act, 1), ("walk", walk_act, 1), ("thrust", thrust_act, 1)):
    use(act, frame)
    pin()
    hw = bone_world("mixamorig:RightHand")
    rigid_w = (np.c_[staff, np.ones(len(staff))] @ hw.T)[:, :3]
    skin = world_pts(spear)
    err = float(np.linalg.norm(rigid_w - skin, axis=1).max())
    print("in-hand", name, "max mm", round(err * 1000.0, 3), flush=True)

# mounted armada, every frame
print("mounted armada", flush=True)
for frame in range(1, 51):
    use(armada_act, frame)
    pin()
    place_staff(1.0)
    measure("armada f%d" % frame, mins)

# cameras
for ix in range(-5, 5):
    for iy in range(-5, 5):
        bpy.ops.mesh.primitive_plane_add(size=0.4, location=((ix + 0.5) * 0.4, (iy + 0.5) * 0.4, 0.0))
        tile = bpy.context.active_object
        tile.color = (0.62, 0.68, 0.48, 1) if (ix + iy) % 2 == 0 else (0.50, 0.56, 0.40, 1)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def make_cam(name, loc, ortho=None, fov=None, target=(0.0, -0.05, 1.15)):
    data = bpy.data.cameras.new(name)
    if ortho:
        data.type = "ORTHO"
        data.ortho_scale = ortho
    else:
        data.type = "PERSP"
        data.sensor_fit = "VERTICAL"
        data.angle = math.radians(fov)
    data.clip_start = 0.01
    data.clip_end = 80
    cam = bpy.data.objects.new(name, data)
    scene.collection.objects.link(cam)
    cam.location = loc
    look_at(cam, target)
    return cam


cam_front = make_cam("front", (0.0, -4.8, 1.25), ortho=3.6)
cam_side = make_cam("side", (-4.8, -0.15, 1.20), ortho=3.6)
look = Vector((0.0, -0.05, 1.15))
eye = look + Vector((-0.612372, -0.612372, 0.5)) * 4.7
cam_preview = make_cam("preview", eye, fov=54, target=look)


def shot(folder, index):
    for cam, cname, rx, ry in (
        (cam_front, "front", 960, 720),
        (cam_side, "side", 960, 720),
        (cam_preview, "preview", 540, 960),
    ):
        scene.camera = cam
        scene.render.resolution_x = rx
        scene.render.resolution_y = ry
        path = os.path.join(FRAMES, cname)
        os.makedirs(path, exist_ok=True)
        scene.render.filepath = os.path.join(path, "f%04d.png" % index)
        bpy.ops.render.render(write_still=True)


index = 1
# lead-in idle
for i in range(8):
    use(idle_act, 1)
    pin()
    place_staff(0.0)
    measure("idle hold", mins)
    shot(FRAMES, index)
    index += 1

# press Armada: time advances with the clip, pose crossfades from idle, staff stows
length_frames = 50
for i in range(length_frames):
    t = i / 30.0
    use(idle_act, 1 + i)
    idle_p = capture()
    use(armada_act, 1 + i)
    arm_p = capture()
    apply(idle_p, arm_p, smooth(min(1.0, t / POSE)))
    pin()
    place_staff(smooth(min(1.0, t / STOW)))
    measure("stow t%.3f" % t, mins)
    shot(FRAMES, index)
    index += 1

# return to idle
use(armada_act, 50)
arm_end = capture()
for i in range(0, 14):
    t = i / 30.0
    use(idle_act, 1)
    idle_p = capture()
    apply(arm_end, idle_p, smooth(min(1.0, t / POSE)))
    pin()
    u = 0.0 if t < RET_DELAY else (t - RET_DELAY) / RET
    place_staff(1.0 - smooth(u))
    measure("return t%.3f" % t, mins)
    shot(FRAMES, index)
    index += 1

for i in range(8):
    use(idle_act, 1)
    pin()
    place_staff(0.0)
    shot(FRAMES, index)
    index += 1

print("FRAMES", index - 1, flush=True)
for key, slot in mins.items():
    print("MIN", key, round(slot["dist"] * 100.0, 2), slot["where"], flush=True)
print("RENDER DONE")
