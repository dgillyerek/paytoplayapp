"""Reimport the exported FBXs and measure / render them.

The numbers come from the exported files, not from the authoring scene.
Blender renders are not Unity. This script does not claim otherwise.

    blender -b --python redo_proof.py -- /tmp/redo_fbx/SERAPHINE_walk.fbx walk
"""
import bpy
import hashlib
import json
import math
import os
import sys
from mathutils import Vector, Quaternion
from mathutils.bvhtree import BVHTree

ART = "/agent/repos/paytoplayapp/Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine"
PROOF = "/agent/repos/paytoplayapp/design/survival-theme-a-fantasy/heroes/anim/seraphine/blender/redo_2026-11-09"
P = "mixamorig:"
IMPORT = dict(
    global_scale=1.0, use_anim=True, anim_offset=0.0,
    automatic_bone_orientation=False, ignore_leaf_bones=False,
    use_custom_normals=True, use_image_search=False,
    primary_bone_axis="Y", secondary_bone_axis="X",
    axis_forward="-Z", axis_up="Y",
)

argv = sys.argv[sys.argv.index("--") + 1:]
FBX = argv[0]
LABEL = argv[1]
FRAMES_DIR = os.path.join(PROOF, "frames", LABEL)
os.makedirs(FRAMES_DIR, exist_ok=True)


def bone_hash(arm):
    h = hashlib.sha256()
    for b in arm.data.bones:
        h.update(b.name.encode())
        h.update((b.parent.name if b.parent else "-").encode())
        for v in list(b.head_local) + list(b.tail_local):
            h.update(f"{v:.6f}".encode())
        h.update(f"{b.matrix_local}".encode())
    return h.hexdigest()[:16]


def weight_hash(obj):
    h = hashlib.sha256()
    for vg in obj.vertex_groups:
        h.update(vg.name.encode())
    for v in obj.data.vertices:
        for g in v.groups:
            h.update(f"{g.group}:{g.weight:.5f}".encode())
    return h.hexdigest()[:16]


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, **IMPORT)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE" and "cape" not in o.name.lower())
body = next(o for o in bpy.data.objects if o.type == "MESH" and "body" in o.name.lower())
spear = next(o for o in bpy.data.objects if o.type == "MESH" and "spear" in o.name.lower())
wings = [o for o in bpy.data.objects if o.type == "MESH" and "wing" in o.name.lower()]

# cape, attached so it follows Spine2 from the bind relationship
before = {o.name for o in bpy.data.objects}
bpy.ops.import_scene.fbx(filepath=os.path.join(ART, "SERAPHINE_cape.fbx"), **IMPORT)
cape_new = [bpy.data.objects[n] for n in {o.name for o in bpy.data.objects} - before]
cape_rig = next(o for o in cape_new if o.type == "ARMATURE")
cape_mesh = next(o for o in cape_new if o.type == "MESH")
bpy.context.view_layer.update()
spine_rest = arm.data.bones[P + "Spine2"].matrix_local.copy()
cape_rest_world = cape_mesh.matrix_world.copy()
# verts of the cape in armature space at rest
cape_bind = [cape_mesh.matrix_world @ v.co for v in cape_mesh.data.vertices]
cape_polys = [p.vertices[:] for p in cape_mesh.data.polygons]

act = arm.animation_data.action
if act.slots:
    arm.animation_data.action_slot = act.slots[0]
f0, f1 = int(round(act.frame_range[0])), int(round(act.frame_range[1]))
print(LABEL, "frames", f0, f1, "hash", bone_hash(arm), "weights", weight_hash(body))

groups = {vg.name: vg.index for vg in body.vertex_groups}


def ids_for(name, thresh=0.45):
    gi = groups.get(name)
    if gi is None:
        return []
    out = []
    for v in body.data.vertices:
        for g in v.groups:
            if g.group == gi and g.weight > thresh:
                out.append(v.index)
                break
    return out


HAND_R = ids_for(P + "RightHand")
HAND_L = ids_for(P + "LeftHand")
FOOT = {
    "Left": ids_for(P + "LeftUpLeg.002", 0.35) + ids_for(P + "LeftUpLeg.003", 0.35),
    "Right": ids_for(P + "RightUpLeg.002", 0.35) + ids_for(P + "RightUpLeg.003", 0.35),
}

scene = bpy.context.scene
scene.render.fps = 30
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "OBJECT"
scene.display.shading.background_type = "VIEWPORT"
scene.display.shading.background_color = (0.46, 0.55, 0.38)
scene.render.resolution_x = 640
scene.render.resolution_y = 800
scene.render.image_settings.file_format = "PNG"
body.color = (0.78, 0.66, 0.52, 1)
spear.color = (0.62, 0.46, 0.18, 1)
for w in wings:
    w.color = (0.93, 0.93, 0.90, 1)
cape_mesh.color = (0.45, 0.55, 0.72, 1)
bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, 0))
ground = bpy.context.active_object
ground.color = (0.55, 0.63, 0.40, 1)
cam_data = bpy.data.cameras.new("cam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = 3.15
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

# move the cape mesh with Spine2 so the render matches the numeric test
def cape_world_verts(spine_pose):
    delta = spine_pose @ spine_rest.inverted()
    return [delta @ p for p in cape_bind]


rows = []
prev_q = None
prev_ankle = {"Left": None, "Right": None}
bone_names = [b.name for b in arm.data.bones]

for f in range(f0, f1 + 1):
    scene.frame_set(f)
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    be = body.evaluated_get(deps)
    se = spear.evaluated_get(deps)
    bw = be.matrix_world
    bverts = [bw @ v.co for v in be.data.vertices]
    sverts = [se.matrix_world @ v.co for v in se.data.vertices]
    spoly = [p.vertices[:] for p in se.data.polygons]
    bpoly = [p.vertices[:] for p in be.data.polygons]
    sb = BVHTree.FromPolygons(sverts, spoly)
    bb = BVHTree.FromPolygons(bverts, bpoly)
    staff_body = len(sb.overlap(bb))
    staff_wing = 0
    wing_body = 0
    wverts_all = []
    wpolys_all = []
    offset = 0
    for w in wings:
        we = w.evaluated_get(deps)
        wv = [we.matrix_world @ v.co for v in we.data.vertices]
        wp = [tuple(i + offset for i in p.vertices) for p in we.data.polygons]
        wverts_all.extend(wv)
        wpolys_all.extend(wp)
        offset += len(wv)
    if wverts_all:
        wb = BVHTree.FromPolygons(wverts_all, wpolys_all)
        staff_wing = len(sb.overlap(wb))
        wing_body = len(wb.overlap(bb))
    spine_pose = arm.pose.bones[P + "Spine2"].matrix.copy()
    cv = cape_world_verts(spine_pose)
    cb = BVHTree.FromPolygons(cv, cape_polys)
    staff_cape = len(sb.overlap(cb))
    # move the visible cape
    cape_mesh.matrix_world = (spine_pose @ spine_rest.inverted()) @ cape_rest_world

    def gap(indices):
        if not indices:
            return None, 0
        dists = []
        for i in indices:
            hit = sb.find_nearest(bverts[i])
            if hit[0] is not None:
                dists.append(hit[3])
        dists.sort()
        close = sum(1 for d in dists if d < 0.02)
        return (dists[0] if dists else None), close

    gr, cr = gap(HAND_R)
    gl, cl = gap(HAND_L)
    # feet
    feet = {}
    for side in ("Left", "Right"):
        zs = [bverts[i].z for i in FOOT[side]]
        ankle = arm.matrix_world @ arm.pose.bones[P + side + "UpLeg.002"].head
        sole = min(zs) if zs else ankle.z
        planted = sole < 0.025
        slide = 0.0
        if planted and prev_ankle[side] is not None and prev_ankle[side][0]:
            slide = (Vector((ankle.x, ankle.y)) - Vector((prev_ankle[side][1], prev_ankle[side][2]))).length
        prev_ankle[side] = (planted, ankle.x, ankle.y)
        feet[side] = {"sole_cm": round(sole * 100, 2), "planted": planted, "slide_cm": round(slide * 100, 3),
                      "xy": [round(ankle.x, 4), round(ankle.y, 4)]}
    # joint angular step
    qs = {}
    max_deg = 0.0
    max_bone = ""
    for n in bone_names:
        q = arm.pose.bones[n].matrix.to_quaternion()
        qs[n] = q
        if prev_q is not None:
            d = abs(prev_q[n].dot(q))
            d = max(-1.0, min(1.0, d))
            deg = math.degrees(2.0 * math.acos(d))
            if deg > max_deg:
                max_deg = deg
                max_bone = n
    prev_q = qs
    row = {
        "f": f,
        "staff_body": staff_body,
        "staff_wing": staff_wing,
        "staff_cape": staff_cape,
        "wing_body": wing_body,
        "gap_r_cm": None if gr is None else round(gr * 100, 3),
        "gap_l_cm": None if gl is None else round(gl * 100, 3),
        "close_r_2cm": cr,
        "close_l_2cm": cl,
        "max_joint_deg": round(max_deg, 2),
        "max_joint_bone": max_bone,
        "feet": feet,
        "staff_zmin_cm": round(min(p.z for p in sverts) * 100, 2),
    }
    rows.append(row)
    if f == f0 or (f - f0) % 8 == 0 or f == f1:
        print(LABEL, f, "body", staff_body, "wing", staff_wing, "cape", staff_cape,
              "gapR", row["gap_r_cm"], "gapL", row["gap_l_cm"], "ddeg", row["max_joint_deg"])

    if "norender" in argv:
        continue
    # cameras
    focus = Vector((0.0, -0.05, 1.05))
    views = {
        "front": focus + Vector((0.0, -3.5, 0.1)),
        "side": focus + Vector((-3.5, 0.15, 0.1)),
        "rear34": focus + Vector((2.2, 2.4, 0.35)),
    }
    for vn, eye in views.items():
        cam_data.ortho_scale = 3.15
        cam.location = eye
        cam.rotation_euler = (focus - eye).normalized().to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(FRAMES_DIR, f"{vn}_{f:03d}.png")
        bpy.ops.render.render(write_still=True)
    # hand closeup on walk every frame, and on the thrust strike
    want_close = LABEL == "walk" or (LABEL == "thrust" and 14 <= (f - f0 + 1) <= 24)
    if want_close:
        hw = arm.matrix_world @ arm.pose.bones[P + "RightHand"].matrix
        focus_h = hw.translation + hw.col[1].xyz * 0.04
        cam_data.ortho_scale = 0.42
        eye = focus_h - hw.col[2].xyz * 0.4
        cam.location = eye
        cam.rotation_euler = (focus_h - eye).normalized().to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(FRAMES_DIR, f"hand_{f:03d}.png")
        bpy.ops.render.render(write_still=True)

summary = {
    "clip": LABEL,
    "fbx": FBX,
    "frames": [f0, f1],
    "skeleton_hash": bone_hash(arm),
    "weight_hash": weight_hash(body),
    "intersections": {
        "staff_body_max": max(r["staff_body"] for r in rows),
        "staff_wing_max": max(r["staff_wing"] for r in rows),
        "staff_cape_max": max(r["staff_cape"] for r in rows),
        "wing_body_max": max(r["wing_body"] for r in rows),
        "frames_staff_body": [r["f"] for r in rows if r["staff_body"]],
        "frames_staff_wing": [r["f"] for r in rows if r["staff_wing"]],
        "frames_staff_cape": [r["f"] for r in rows if r["staff_cape"]],
        "frames_wing_body": [r["f"] for r in rows if r["wing_body"]],
    },
    "gap_r_cm_min": min(r["gap_r_cm"] for r in rows if r["gap_r_cm"] is not None),
    "gap_l_cm_min": min(r["gap_l_cm"] for r in rows if r["gap_l_cm"] is not None),
    "max_joint_deg": max(r["max_joint_deg"] for r in rows),
    "max_joint_at": max(rows, key=lambda r: r["max_joint_deg"])["f"],
    "max_joint_bone": max(rows, key=lambda r: r["max_joint_deg"])["max_joint_bone"],
    "max_foot_slide_cm": max(max(r["feet"]["Left"]["slide_cm"], r["feet"]["Right"]["slide_cm"]) for r in rows),
    "staff_zmin_cm": min(r["staff_zmin_cm"] for r in rows),
    "rows": rows,
}
out_json = os.path.join(PROOF, f"metrics_{LABEL}.json")
with open(out_json, "w") as fh:
    json.dump(summary, fh)
print("WROTE", out_json)
print("MAX", {k: summary[k] for k in ("gap_r_cm_min", "gap_l_cm_min", "max_joint_deg", "max_foot_slide_cm", "staff_zmin_cm")})
print("IX", summary["intersections"])
