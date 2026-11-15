"""Play Seraphine's preview attacks the way NewCharsPreview does.

Instances SERAPHINE_rig_rest and samples the clip action onto that rig.
Horizontal hips are pinned to the bind, matching HoldHips. Renders front,
side, and the preview camera (yaw 135, pitch 30, 54 degree vertical FOV).
Plates are labeled Blender, not Unity.

  blender --background --factory-startup --python render_preview.py
"""
import bpy
import math
import os
from mathutils import Vector

REPO = "/agent/repos/paytoplayapp"
ART = os.path.join(REPO, "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/seraphine")
OUT = os.path.dirname(os.path.abspath(__file__))
FRAMES = "/tmp/sera/armada_frames"
os.makedirs(FRAMES, exist_ok=True)

IMPORT = dict(
    global_scale=1.0, use_anim=True, anim_offset=0.0,
    automatic_bone_orientation=False, ignore_leaf_bones=False,
    use_custom_normals=True, use_image_search=False,
)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "OBJECT"
scene.display.shading.background_type = "VIEWPORT"
scene.display.shading.background_color = (0.55, 0.62, 0.46)
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False


def do_import(path):
    before = {o.name for o in bpy.data.objects}
    bpy.ops.import_scene.fbx(filepath=path, **IMPORT)
    return [bpy.data.objects[n] for n in {o.name for o in bpy.data.objects} - before]


def paint(obj, rgba):
    obj.color = rgba


rest = do_import(os.path.join(ART, "SERAPHINE_rig_rest.fbx"))
rig = next(o for o in rest if o.type == "ARMATURE")
for obj in rest:
    if obj.type != "MESH":
        continue
    if "spear" in obj.name.lower():
        paint(obj, (0.86, 0.55, 0.12, 1))
    elif "wing" in obj.name.lower():
        paint(obj, (0.93, 0.93, 0.88, 1))
    else:
        paint(obj, (0.74, 0.58, 0.44, 1))

for ix in range(-5, 5):
    for iy in range(-5, 5):
        bpy.ops.mesh.primitive_plane_add(size=0.4, location=((ix + 0.5) * 0.4, (iy + 0.5) * 0.4, 0.0))
        tile = bpy.context.active_object
        if (ix + iy) % 2 == 0:
            paint(tile, (0.62, 0.68, 0.48, 1))
        else:
            paint(tile, (0.50, 0.56, 0.40, 1))


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


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


cam_front = make_cam("front", (0.0, -4.8, 1.25), ortho=3.35)
cam_side = make_cam("side", (-4.8, -0.15, 1.20), ortho=3.35)
# LockedBattleCamera yaw 135, pitch 30. This FBX faces -Y, so Unity's eye
# (sin yaw, sin pitch, cos yaw) lands on (-X, -Y) with +Z up.
LOOK = Vector((0.0, -0.05, 1.15))
DIST = 4.7
EYE = LOOK + Vector((-0.612372, -0.612372, 0.5)) * DIST
cam_preview = make_cam("preview", EYE, fov=54, target=LOOK)

font = bpy.data.curves.new("label", "FONT")
font.body = "BLENDER  —  not Unity"
font.size = 0.11
font.align_x = "LEFT"
label = bpy.data.objects.new("label", font)
scene.collection.objects.link(label)
label.color = (1, 1, 1, 1)

rest_head = rig.data.bones["mixamorig:Hips"].head_local.copy()


def pin_hips():
    rig.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    head = rig.matrix_world @ rig.pose.bones["mixamorig:Hips"].head
    rig.location.x += rest_head.x - head.x
    rig.location.y += rest_head.y - head.y
    bpy.context.view_layer.update()


def stick_label(cam, grip=False):
    label.parent = cam
    if cam.data.type == "ORTHO":
        half = cam.data.ortho_scale * 0.5
        label.location = (-half * 0.92, half * 0.82, -2.2)
        label.scale = (0.45, 0.45, 0.45) if grip else (1, 1, 1)
    else:
        label.location = (-0.42, 0.62, -1.6)
        label.scale = (0.7, 0.7, 0.7)
    label.rotation_euler = (0, 0, 0)


clip_objs = []


def load_clip(filename):
    global clip_objs
    for obj in clip_objs:
        bpy.data.objects.remove(obj, do_unlink=True)
    clip_objs = do_import(os.path.join(ART, filename))
    src = next(o for o in clip_objs if o.type == "ARMATURE")
    for obj in clip_objs:
        obj.hide_render = True
        obj.hide_set(True)
    act = src.animation_data.action
    if rig.animation_data is None:
        rig.animation_data_create()
    rig.animation_data.action = act
    try:
        rig.animation_data.action_slot = act.slots[0]
    except Exception as exc:
        print("slot", exc)
    return act


def render_clip(key, filename, grip_frames):
    act = load_clip(filename)
    start, end = int(round(act.frame_range[0])), int(round(act.frame_range[1]))
    scene.frame_start, scene.frame_end = start, end
    for cam, cname, rx, ry in (
        (cam_front, "front", 960, 720),
        (cam_side, "side", 960, 720),
        (cam_preview, "preview", 540, 960),
    ):
        stick_label(cam)
        scene.camera = cam
        scene.render.resolution_x = rx
        scene.render.resolution_y = ry
        folder = os.path.join(FRAMES, "%s_%s" % (key, cname))
        os.makedirs(folder, exist_ok=True)
        for frame in range(start, end + 1):
            scene.frame_set(frame)
            pin_hips()
            scene.render.filepath = os.path.join(folder, "f%04d.png" % frame)
            bpy.ops.render.render(write_still=True)
        print("rendered", key, cname, end, flush=True)

    scene.render.resolution_x = 720
    scene.render.resolution_y = 720
    hand = rig.pose.bones["mixamorig:RightHand"]
    for frame in grip_frames:
        scene.frame_set(frame)
        pin_hips()
        M = rig.matrix_world @ hand.matrix
        origin = M.translation + M.to_3x3() @ Vector((0.02, 0.04, -0.02))
        for axis_name, offset in (
            ("palm", Vector((0.05, 0.42, 0.08))),
            ("side", Vector((0.28, 0.12, 0.16))),
        ):
            loc = origin + M.to_3x3() @ offset
            cam = make_cam("grip_%s_%s_%s" % (key, frame, axis_name), loc, ortho=0.55, target=origin)
            stick_label(cam, grip=True)
            scene.camera = cam
            path = os.path.join(OUT, "grips", "%s_f%02d_%s.png" % (key, frame, axis_name))
            os.makedirs(os.path.dirname(path), exist_ok=True)
            scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            bpy.data.objects.remove(cam, do_unlink=True)
    print("grips", key, flush=True)


render_clip("armada", "SERAPHINE_attack_armada.fbx", [1, 7, 25, 42, 49])
render_clip("thrust", "SERAPHINE_attack_thrust_slash.fbx", [1, 13, 42, 78, 91])
print("RENDER DONE")
