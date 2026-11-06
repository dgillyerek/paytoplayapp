"""Full staff-mesh check on a clean FBX reload, same axis settings as the preview.

Reports every staff vertex (decorative collar included) against the floor,
and the staff surface against the body, head/hair, and wings.
"""
import bpy
import json
import sys
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
fbx = argv[0]
out = argv[1]
label = argv[2] if len(argv) > 2 else "fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(
    filepath=fbx,
    global_scale=1.0,
    use_anim=True,
    automatic_bone_orientation=False,
    ignore_leaf_bones=False,
    use_custom_normals=True,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    axis_forward="-Z",
    axis_up="Y",
)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
spear = next(o for o in bpy.data.objects if o.type == "MESH" and "spear" in o.name.lower())
body = next(o for o in bpy.data.objects if o.type == "MESH" and "spear" not in o.name.lower() and "wing" not in o.name.lower())
wings = [o for o in bpy.data.objects if o.type == "MESH" and "wing" in o.name.lower()]
act = arm.animation_data.action
arm.animation_data.action_slot = act.slots[0]
start, end = int(act.frame_range[0]), int(act.frame_range[1])
groups = {vg.name: vg.index for vg in body.vertex_groups}
hair = []
hand = []
for v in body.data.vertices:
    for g in v.groups:
        if g.group == groups.get("mixamorig:RightHand") and g.weight > 0.5:
            hand.append(v.index)
            break
        if g.group in (groups.get("mixamorig:Head"), groups.get("mixamorig:HeadTop_End")) and g.weight > 0.4:
            hair.append(v.index)
            break
rows = []
for f in range(start, end + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        deps = bpy.context.evaluated_depsgraph_get()
        se = spear.evaluated_get(deps)
        staff = [se.matrix_world @ v.co for v in se.data.vertices]
        zmin = min(p.z for p in staff)
        below = sum(1 for p in staff if p.z < 0.01)
        be = body.evaluated_get(deps)
        bw = be.matrix_world
        bverts = [bw @ v.co for v in be.data.vertices]
        body_bvh = BVHTree.FromPolygons(bverts, [p.vertices[:] for p in be.data.polygons])
        rh = arm.matrix_world @ arm.pose.bones["mixamorig:RightHand"].head
        best_body = 1e9
        for p in staff:
            if (p - rh).length < 0.12:
                continue
            loc, normal, idx, dist = body_bvh.find_nearest(p)
            if loc is not None and dist < best_body:
                best_body = dist
        best_hair = 1e9
        staff_bvh = BVHTree.FromPolygons(staff, [p.vertices[:] for p in se.data.polygons])
        for j in hair:
            loc, normal, idx, dist = staff_bvh.find_nearest(bverts[j])
            if loc is not None and dist < best_hair:
                best_hair = dist
        best_wing = 1e9
        for w in wings:
            we = w.evaluated_get(deps)
            for i, v in enumerate(we.data.vertices):
                if i % 4:
                    continue
                q = we.matrix_world @ v.co
                loc, normal, idx, dist = staff_bvh.find_nearest(q)
                if loc is not None and dist < best_wing:
                    best_wing = dist
        lf = arm.matrix_world @ arm.pose.bones["mixamorig:LeftUpLeg.002"].head
        rf = arm.matrix_world @ arm.pose.bones["mixamorig:RightUpLeg.002"].head
        rows.append({
        "f": f,
        "staff_zmin_cm": round(zmin * 100, 2),
        "verts_below_1cm": below,
        "staff_body_cm": round(best_body * 100, 2),
        "staff_hair_cm": round(best_hair * 100, 2),
        "staff_wing_cm": round(best_wing * 100, 2),
        "l_ankle_z": round(lf.z, 3),
        "r_ankle_z": round(rf.z, 3),
    })
summary = {
    "label": label,
    "frames": end - start + 1,
    "min_staff_z_cm": min(r["staff_zmin_cm"] for r in rows),
    "frames_below_1cm": [r["f"] for r in rows if r["verts_below_1cm"]],
    "min_body_cm": min(r["staff_body_cm"] for r in rows),
    "min_hair_cm": min(r["staff_hair_cm"] for r in rows),
    "min_wing_cm": min(r["staff_wing_cm"] for r in rows),
    "rows": rows,
}
with open(out, "w") as fh:
    json.dump(summary, fh)
print(label, "zmin", summary["min_staff_z_cm"], "below", summary["frames_below_1cm"],
      "body", summary["min_body_cm"], "hair", summary["min_hair_cm"], "wing", summary["min_wing_cm"])
