"""Render and measure FIX5 from a clean FBX reload.

The preview plays rig_rest's mesh with a clip loaded from the attack FBX.
This script imports those files the same way (documented axis settings), drives
rig_rest with the clip, and renders constant-scale orthographic frames.

  blender -b --python fix5_render.py -- --mode stills|all
"""
import bpy
import json
import math
import os
import sys
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
MODE = "stills"
for i, a in enumerate(argv):
    if a == "--mode" and i + 1 < len(argv):
        MODE = argv[i + 1]

REPO = "/agent/repos/paytoplayapp"
ART = os.path.join(REPO, "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine")
OUT = "/tmp/sera_fix5/render"
os.makedirs(OUT, exist_ok=True)

# fresh scene
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "OBJECT"
scene.display.shading.background_type = "VIEWPORT"
scene.world = bpy.data.worlds.new("w") if not scene.world else scene.world
scene.render.resolution_x = 960
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.display.shading.background_color = (0.78, 0.79, 0.80)

IMPORT = dict(
    global_scale=1.0,
    use_anim=True,
    anim_offset=0.0,
    automatic_bone_orientation=False,
    ignore_leaf_bones=False,
    use_custom_normals=True,
    use_image_search=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    axis_forward="-Z",
    axis_up="Y",
)


def imported_names():
    return {o.name for o in bpy.data.objects}


def do_import(path):
    before = imported_names()
    bpy.ops.import_scene.fbx(filepath=path, **IMPORT)
    return [bpy.data.objects[n] for n in imported_names() - before]


def paint(obj, rgba):
    obj.color = rgba


rest_objs = do_import(os.path.join(ART, "SERAPHINE_rig_rest.fbx"))
print("REST", [(o.name, o.type) for o in rest_objs])
rig = next(o for o in rest_objs if o.type == "ARMATURE")
body = next(o for o in rest_objs if o.type == "MESH" and "spear" not in o.name.lower() and "wing" not in o.name.lower())
spear = next(o for o in rest_objs if o.type == "MESH" and "spear" in o.name.lower())
wings = [o for o in rest_objs if o.type == "MESH" and "wing" in o.name.lower()]
paint(body, (0.97, 0.97, 0.96, 1))
paint(spear, (0.08, 0.08, 0.08, 1))
for w in wings:
    paint(w, (0.94, 0.95, 0.96, 1))

# bind snapshot before any clip
bind = {b.name: [round(x, 6) for row in b.matrix_local for x in row] for b in rig.data.bones}
bind_parents = {b.name: (b.parent.name if b.parent else "") for b in rig.data.bones}
rest_verts = len(body.data.vertices)

cape_objs = do_import(os.path.join(ART, "SERAPHINE_cape.fbx"))
print("CAPE", [(o.name, o.type) for o in cape_objs])
cape_rig = next(o for o in cape_objs if o.type == "ARMATURE")
cape_mesh = next(o for o in cape_objs if o.type == "MESH")
paint(cape_mesh, (0.55, 0.62, 0.78, 1))
# Same attach as the blend: cape rig child-of body Spine2, identity.
cape_rig.constraints.clear()
con = cape_rig.constraints.new("CHILD_OF")
con.target = rig
con.subtarget = "mixamorig:Spine2"
# zero the inverse so the rest offset stays the one baked in the cape file
bpy.context.view_layer.objects.active = cape_rig
# set inverse at rest
bpy.context.view_layer.update()
con.inverse_matrix = rig.matrix_world.inverted()
# Child-of inverse that keeps the current world transform:
con.inverse_matrix = (rig.matrix_world @ rig.pose.bones["mixamorig:Spine2"].matrix).inverted() @ cape_rig.matrix_world
print("cape faces", len(cape_mesh.data.polygons), "verts", len(cape_mesh.data.vertices))

# floor grid, constant, so steps are visible
for ix in range(-4, 4):
    for iy in range(-4, 4):
        bpy.ops.mesh.primitive_plane_add(size=0.25, location=((ix + 0.5) * 0.25, (iy + 0.5) * 0.25, 0.0))
        tile = bpy.context.active_object
        tile.name = f"tile_{ix}_{iy}"
        if iy in (-1, 0) and ix in (-1, 0):
            paint(tile, (0.15, 0.15, 0.16, 1))
        elif (ix + iy) % 2 == 0:
            paint(tile, (0.42, 0.43, 0.45, 1))
        else:
            paint(tile, (0.58, 0.59, 0.61, 1))

ORTHO = 3.35  # constant for every full-body view
CENTER = Vector((0.0, -0.05, 1.12))


def look_at(obj, target):
    # Camera looks down its local -Z. Local +Y is the view's up, which we pin to world +Z.
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def make_cam(name, loc, scale):
    data = bpy.data.cameras.new(name)
    data.type = "ORTHO"
    data.ortho_scale = scale
    data.clip_start = 0.01
    data.clip_end = 50
    cam = bpy.data.objects.new(name, data)
    scene.collection.objects.link(cam)
    cam.location = loc
    look_at(cam, CENTER)
    return cam


cam_front = make_cam("cam_front34", (1.85, -2.55, 1.25), ORTHO)
cam_side = make_cam("cam_side", (-3.15, 0.0, 1.18), ORTHO)
cam_straight = make_cam("cam_straight", (0.0, -3.6, 1.20), ORTHO)
# Preview lens is 54°. The battle camera sits on a three-quarter at pitch 30.
# Framed on Seraphine alone, from her front-right, high enough to keep feet and the
# upright staff in frame. The camera does not move. Hips stay where HoldHips pins them.
cam_preview = make_cam("cam_preview", (-1.55, -3.15, 1.95), ORTHO)
cam_preview.data.type = "PERSP"
cam_preview.data.sensor_fit = "VERTICAL"
cam_preview.data.angle = math.radians(54)
look_at(cam_preview, CENTER)
# Wrist close-up, constant, from her right-front. Not a collision dodge camera.
cam_wrist = make_cam("cam_wrist", (-1.15, -1.05, 1.45), 1.15)
look_at(cam_wrist, Vector((-0.25, -0.25, 1.25)))


def load_clip(filename):
    objs = do_import(os.path.join(ART, filename))
    src = next(o for o in objs if o.type == "ARMATURE")
    act = src.animation_data.action
    # hide the donor meshes; we drive rig_rest
    for o in objs:
        o.hide_render = True
        o.hide_viewport = True
    # bind compare
    max_diff = 0.0
    for b in src.data.bones:
        other = rig.data.bones.get(b.name)
        if other is None:
            print("MISSING BONE", b.name)
            continue
        A = b.matrix_local
        B = other.matrix_local
        for i in range(4):
            for j in range(4):
                max_diff = max(max_diff, abs(A[i][j] - B[i][j]))
    donor_body = next(o for o in objs if o.type == "MESH" and "spear" not in o.name.lower() and "wing" not in o.name.lower())
    # vertex compare at rest (donor action may pose it — clear)
    src.animation_data.action = None
    bpy.context.view_layer.update()
    # compare a sample of bind-pose vertex positions
    n = min(len(body.data.vertices), len(donor_body.data.vertices))
    vmax = 0.0
    for i in range(0, n, 17):
        vmax = max(vmax, (body.data.vertices[i].co - donor_body.data.vertices[i].co).length)
    print(f"{filename} bones {len(src.data.bones)} bind max diff {max_diff:.6e} vert sample {vmax:.6e} action {act.name} range {tuple(act.frame_range)}")
    rig.animation_data_create()
    rig.animation_data.action = act
    try:
        rig.animation_data.action_slot = act.slots[0]
    except Exception as e:
        print("slot", e)
    return act, max_diff, vmax, len(src.data.bones)


def knee(side):
    a = rig.matrix_world @ rig.pose.bones[f"mixamorig:{side}UpLeg"].head
    b = rig.matrix_world @ rig.pose.bones[f"mixamorig:{side}UpLeg.001"].head
    c = rig.matrix_world @ rig.pose.bones[f"mixamorig:{side}UpLeg.002"].head
    v1 = (a - b).normalized()
    v2 = (c - b).normalized()
    return math.degrees(math.acos(max(-1, min(1, v1.dot(v2)))))


def render_frame(cam, path):
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


clips = {}
for fname, key in (("SERAPHINE_attack_thrust.fbx", "thrust"), ("SERAPHINE_attack_throw.fbx", "throw")):
    act, bind_diff, vert_diff, nb = load_clip(fname)
    start = int(act.frame_range[0])
    end = int(act.frame_range[1])
    rows = []
    prev_l = prev_r = None
    slides = []
    for f in range(start, end + 1):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        hips = rig.matrix_world @ rig.pose.bones["mixamorig:Hips"].head
        lf = rig.matrix_world @ rig.pose.bones["mixamorig:LeftUpLeg.002"].head
        rf = rig.matrix_world @ rig.pose.bones["mixamorig:RightUpLeg.002"].head
        rh = rig.matrix_world @ rig.pose.bones["mixamorig:RightHand"].matrix
        staff = rh.col[0].xyz.normalized()
        head = rig.matrix_world @ rig.pose.bones["mixamorig:Head"].head
        if prev_l is not None:
            for tag, a, b, arc in (
                ("L", prev_l, lf, lf.z),
                ("R", prev_r, rf, rf.z),
            ):
                step = math.hypot(a.x - b.x, a.y - b.y)
                # a slide is horizontal travel while the ankle stays near its planted height
                if step > 0.02 and min(a.z, b.z) < (0.20 if tag == "L" else 0.22):
                    slides.append({"f": f, "foot": tag, "cm": round(step * 100, 2)})
        prev_l, prev_r = lf.copy(), rf.copy()
        rows.append({
            "f": f,
            "hips": [round(c, 3) for c in hips],
            "head_z": round(head.z, 3),
            "l_foot": [round(c, 3) for c in lf],
            "r_foot": [round(c, 3) for c in rf],
            "l_knee": round(knee("Left"), 1),
            "r_knee": round(knee("Right"), 1),
            "staff_elev": round(math.degrees(math.asin(max(-1, min(1, staff.z)))), 1),
            "staff_dir": [round(c, 3) for c in staff],
            "r_hand": [round(c, 3) for c in rh.translation],
        })
    upright = rows[0]["head_z"] > rows[0]["hips"][2]
    print(f"{key} frames {start}-{end} upright {upright} headz {rows[0]['head_z']} hipz {rows[0]['hips'][2]} slides {len(slides)}")
    clips[key] = {"bind_diff": bind_diff, "vert_diff": vert_diff, "bones": nb,
                  "frames": rows, "slides": slides, "action": act.name}

# orientation stills before the long render
scene.frame_set(1)
# thrust action is the last one assigned; set thrust frame 20 and throw 24 explicitly
# reassign
def assign(key):
    # actions live on the hidden donors; find by stored name
    act = bpy.data.actions[clips[key]["action"]]
    rig.animation_data.action = act
    rig.animation_data.action_slot = act.slots[0]


if MODE == "stills":
    # 0.55 s is frame 17 at 30 fps with frame 1 at t = 0. Three views: straight
    # front, strict side, front three-quarter. White body, dark staff, floor grid.
    assign("thrust")
    for f in (8, 14, 17, 22, 24, 28, 36, 46):
        scene.frame_set(f)
        render_frame(cam_straight, os.path.join(OUT, f"still_thrust_f{f:02d}_straight.png"))
        render_frame(cam_side, os.path.join(OUT, f"still_thrust_f{f:02d}_side.png"))
        render_frame(cam_front, os.path.join(OUT, f"still_thrust_f{f:02d}_front.png"))
    assign("throw")
    for f in (12, 14, 16, 22, 24, 28, 34, 42, 52):
        scene.frame_set(f)
        render_frame(cam_straight, os.path.join(OUT, f"still_throw_f{f:02d}_straight.png"))
        render_frame(cam_side, os.path.join(OUT, f"still_throw_f{f:02d}_side.png"))
        render_frame(cam_front, os.path.join(OUT, f"still_throw_f{f:02d}_front.png"))
else:
    jobs = {
        "thrust": [1, 8, 12, 15, 17, 20, 23, 28, 34, 40],
        "throw": [1, 8, 14, 18, 22, 24, 28, 32, 38, 48],
    }
    for key, keys in jobs.items():
        assign(key)
        end = clips[key]["frames"][-1]["f"]
        for f in range(1, end + 1):
            scene.frame_set(f)
            render_frame(cam_front, os.path.join(OUT, f"{key}_front_{f:03d}.png"))
            render_frame(cam_side, os.path.join(OUT, f"{key}_side_{f:03d}.png"))
            render_frame(cam_preview, os.path.join(OUT, f"{key}_preview_{f:03d}.png"))
            render_frame(cam_wrist, os.path.join(OUT, f"{key}_wrist_{f:03d}.png"))
        print("rendered", key)

# drop non-json bits
out_doc = {}
for k, v in clips.items():
    out_doc[k] = {kk: vv for kk, vv in v.items()}
with open(os.path.join(OUT, "reimport_measures.json"), "w") as fh:
    json.dump({
        "ortho_scale": ORTHO,
        "rest_verts": rest_verts,
        "cape_faces": len(cape_mesh.data.polygons),
        "cape_verts": len(cape_mesh.data.vertices),
        "clips": out_doc,
    }, fh)
print("measures written", MODE)
