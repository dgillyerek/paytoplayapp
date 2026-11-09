"""Export the new dualslash FBX and render that import, not the authoring blend.

The sword mesh is not rewritten. Unity's preview samples this clip onto
KAELEN_rig_rest.fbx, so this script does not replace that rest asset.
After re-import it checks the hilt is still the bind point in the hand bone.
"""
import os
import hashlib
import bpy
from mathutils import Vector

BLEND = os.environ.get("KAELEN_FIX9_BLEND", "/home/ubuntu/k9_out/dual2/KAELEN_fix9b.blend")
OUT = os.environ.get("KAELEN_FIX9_OUT", "/home/ubuntu/k9_out/dual2")
FBX = os.path.join(OUT, "KAELEN_attack_dualslash.fbx")
os.makedirs(OUT, exist_ok=True)

def mesh_fingerprint(obj):
    h = hashlib.sha256()
    h.update(str(len(obj.data.vertices)).encode())
    for v in obj.data.vertices:
        h.update(f"{v.co.x:.6f},{v.co.y:.6f},{v.co.z:.6f}".encode())
    return h.hexdigest()[:16]


def hilt_in_hand(arm, sw, hand_name):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    pb = arm.pose.bones[hand_name] if hand_name in arm.pose.bones else next(
        b for b in arm.pose.bones if b.name.endswith(hand_name.split(":")[-1]))
    inv = (arm.matrix_world @ pb.matrix).inverted()
    ev = sw.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    local = [inv @ (mw @ v.co) for v in me.vertices]
    ev.to_mesh_clear()
    local.sort(key=lambda p: p.length)
    n = 400
    hilt = sum(local[:n], Vector()) / n
    return hilt


bpy.ops.wm.open_mainfile(filepath=BLEND)
arm = bpy.data.objects["KAELEN_rig"]
act = bpy.data.actions["attack_dualslash"]
arm.animation_data.action = act
arm.animation_data.action_slot = act.slots[0]
for tr in arm.animation_data.nla_tracks:
    tr.mute = True
scene = bpy.context.scene
scene.frame_start = 1
scene.frame_end = 58
src_mesh = {
    name: mesh_fingerprint(bpy.data.objects[name])
    for name in ("KAELEN_body", "KAELEN_sword_L", "KAELEN_sword_R")
}
src_hilt = {}
for f in (1, 16, 37, 58):
    scene.frame_set(f)
    src_hilt[f] = {
        "L": hilt_in_hand(arm, bpy.data.objects["KAELEN_sword_L"], "mixamorig:LeftArm.002"),
        "R": hilt_in_hand(arm, bpy.data.objects["KAELEN_sword_R"], "mixamorig:RightArm.002"),
    }
for ob in bpy.data.objects:
    ob.select_set(False)
for name in ("KAELEN_rig", "KAELEN_body", "KAELEN_sword_L", "KAELEN_sword_R"):
    bpy.data.objects[name].select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=FBX,
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
    mesh_smooth_type="FACE",
    use_armature_deform_only=False,
    path_mode="AUTO",
    bake_anim_use_all_bones=True,
)
print("EXPORTED", FBX, os.path.getsize(FBX))

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(
    filepath=FBX,
    automatic_bone_orientation=False,
    use_custom_normals=True,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    axis_forward="-Z",
    axis_up="Y",
    global_scale=1.0,
    use_anim=True,
    ignore_leaf_bones=False,
    anim_offset=0.0,
)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
body = next(o for o in bpy.data.objects if o.type == "MESH" and "sword" not in o.name.lower())
swords = [o for o in bpy.data.objects if o.type == "MESH" and "sword" in o.name.lower()]
print("IMPORTED", arm.name, "bones", len(arm.data.bones), "action", arm.animation_data.action.name if arm.animation_data and arm.animation_data.action else None)
print("MESH", body.name, "swords", [s.name for s in swords])
scene = bpy.context.scene
by_name = {o.name: o for o in bpy.data.objects if o.type == "MESH"}
for name, fp in src_mesh.items():
    got = mesh_fingerprint(by_name[name])
    print("MESH_HASH", name, got, "match" if got == fp else "DIFFER")
    if got != fp:
        raise RuntimeError(f"{name} vertices differ from the source blend")
worst = 0.0
for f, pair in src_hilt.items():
    scene.frame_set(f)
    for side, bone, sw in (
        ("L", "mixamorig:LeftArm.002", "KAELEN_sword_L"),
        ("R", "mixamorig:RightArm.002", "KAELEN_sword_R"),
    ):
        got = hilt_in_hand(arm, by_name[sw], bone)
        err = (got - pair[side]).length
        worst = max(worst, err)
        print(f"SOCKET f{f} {side} {err*1000:.3f} mm")
if worst > 0.0002:
    raise RuntimeError(f"hand socket drifted {worst*1000:.3f} mm on FBX re-import")
print("SOCKET_OK", round(worst * 1000, 4), "mm")
scene.frame_start = 1
scene.frame_end = 58
if os.environ.get("KAELEN_FIX9_RENDER", "1") == "0":
    print("SKIP RENDER")
    raise SystemExit(0)

scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = 720
scene.render.resolution_y = 900
scene.render.fps = 30
scene.render.image_settings.file_format = "PNG"
shading = scene.display.shading
shading.light = "STUDIO"
shading.color_type = "MATERIAL"
for ob, col in ((body, (0.74, 0.74, 0.76, 1)),):
    mat = bpy.data.materials.new("body")
    mat.diffuse_color = col
    ob.data.materials.clear()
    ob.data.materials.append(mat)
for ob in swords:
    mat = bpy.data.materials.new(ob.name + "m")
    mat.diffuse_color = (0.78, 0.12, 0.09, 1)
    ob.data.materials.clear()
    ob.data.materials.append(mat)

cam_data = bpy.data.cameras.new("CAM")
cam_data.type = "ORTHO"
cam_data.ortho_scale = 3.05
cam_data.clip_start = 0.01
cam_data.clip_end = 40
cam = bpy.data.objects.new("CAM", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

def aim(loc, target, scale):
    cam.location = loc
    cam.rotation_euler = (target - loc).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale

def render(folder, frames):
    os.makedirs(folder, exist_ok=True)
    for f in frames:
        scene.frame_set(f)
        bpy.context.view_layer.update()
        scene.render.filepath = os.path.join(folder, f"f{f:03d}.png")
        bpy.ops.render.render(write_still=True)

all_f = list(range(1, 59))
print("START FRAME", scene.frame_start, "END", scene.frame_end)

aim(Vector((0, -6, 1.35)), Vector((0, 0, 1.35)), 3.05)
render(os.path.join(OUT, "fbx_front"), all_f)
aim(Vector((-6, -0.2, 1.35)), Vector((0, -0.2, 1.35)), 3.05)
render(os.path.join(OUT, "fbx_side"), all_f)
aim(Vector((2.5, -4.3, 2.3)), Vector((0, -0.15, 1.2)), 3.2)
render(os.path.join(OUT, "fbx_qright"), all_f)

# Hand closeups at high 15-17 and low 36-38, palm-readable.
# Find a hand bone.
hand = None
for b in arm.pose.bones:
    if b.name.endswith("LeftArm.002"):
        hand = b
        break
print("HAND", hand.name if hand else None)

def hand_pos(frame, suffix):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    for b in arm.pose.bones:
        if b.name.endswith(suffix):
            return arm.matrix_world @ b.head
    return Vector((0, 0, 1))

for label, frames in (("high", [15, 16, 17]), ("low", [36, 37, 38])):
    for f in frames:
        for side, suf in (("L", "LeftArm.002"), ("R", "RightArm.002")):
            h = hand_pos(f, suf)
            # three views around the fist
            for tag, off, scale in (
                ("palm", Vector((0.0, -0.42, 0.05)), 0.42),
                ("side", Vector((-0.42 if side == "R" else 0.42, -0.08, 0.04)), 0.42),
                ("q", Vector((0.28 if side == "L" else -0.28, -0.32, 0.12)), 0.45),
            ):
                aim(h + off, h + Vector((0, 0, 0.02)), scale)
                folder = os.path.join(OUT, f"grip_{label}_{side}_{tag}")
                os.makedirs(folder, exist_ok=True)
                scene.render.filepath = os.path.join(folder, f"f{f:03d}.png")
                bpy.ops.render.render(write_still=True)
print("RENDER DONE", OUT)
