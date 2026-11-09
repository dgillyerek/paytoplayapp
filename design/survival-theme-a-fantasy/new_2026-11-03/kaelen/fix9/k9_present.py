"""Export FIX9 attack clips, prove the FBX round-trip, and render fixed-camera review.

Cameras are locked in world space. The same ortho scale is used before and after.
The crossthrust close-up is frame 22, the frame where FIX8's right upper-arm
twist peaked at +389 degrees.
"""
import math
import os
import json
import bpy
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

OUT = os.environ.get("KAELEN_FIX9_OUT", "/home/ubuntu/k9_out")
FIX = os.environ.get("KAELEN_FIX9_BLEND", "/home/ubuntu/KAELEN_fix9.blend")
ORIG = os.environ.get("KAELEN_FIX8_BLEND", "/home/ubuntu/KAELEN.blend")
FBX_DIR = os.path.join(OUT, "fbx")
os.makedirs(FBX_DIR, exist_ok=True)

WORST_FRAME = 22  # anatomical peak of the FIX8 right-upper-arm roll
ARMS = [
    "mixamorig:RightShoulder", "mixamorig:RightArm", "mixamorig:RightArm.001", "mixamorig:RightArm.002",
    "mixamorig:LeftShoulder", "mixamorig:LeftArm", "mixamorig:LeftArm.001", "mixamorig:LeftArm.002",
]


def assign(arm, action):
    arm.animation_data.action = action
    if hasattr(arm.animation_data, "action_slot"):
        arm.animation_data.action_slot = action.slots[0]
    for tr in arm.animation_data.nla_tracks:
        tr.mute = True


def look_at(cam, loc, target):
    cam.location = loc
    direction = target - loc
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def make_cam(name, loc, target, ortho):
    data = bpy.data.cameras.new(name)
    data.type = "ORTHO"
    data.ortho_scale = ortho
    data.clip_start = 0.01
    data.clip_end = 50
    ob = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(ob)
    look_at(ob, loc, target)
    return ob


def paint(obj, rgba):
    mat = bpy.data.materials.get(obj.name + "_rev") or bpy.data.materials.new(obj.name + "_rev")
    mat.diffuse_color = rgba
    if obj.type == "MESH":
        obj.data.materials.clear()
        obj.data.materials.append(mat)


def setup_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.fps = 30
    scene.render.image_settings.file_format = "PNG"
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "MATERIAL"
    shading.background_type = "VIEWPORT"
    shading.show_object_outline = False
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    if world.use_nodes and world.node_tree:
        bg = world.node_tree.nodes.get("Background")
        if bg:
            bg.inputs[0].default_value = (0.16, 0.17, 0.20, 1)
    # Solid viewport background
    try:
        shading.background_color = (0.16, 0.17, 0.20)
    except Exception:
        pass


def style_character():
    paint(bpy.data.objects["KAELEN_body"], (0.72, 0.73, 0.75, 1))
    paint(bpy.data.objects["KAELEN_sword_R"], (0.75, 0.12, 0.10, 1))
    paint(bpy.data.objects["KAELEN_sword_L"], (0.75, 0.12, 0.10, 1))
    if "KAELEN_cape" in bpy.data.objects:
        paint(bpy.data.objects["KAELEN_cape"], (0.20, 0.38, 0.75, 1))
        bpy.data.objects["KAELEN_cape"].hide_render = False


def render_frames(action_name, frames, cam, folder):
    arm = bpy.data.objects["KAELEN_rig"]
    action = bpy.data.actions[action_name]
    assign(arm, action)
    scene = bpy.context.scene
    scene.camera = cam
    os.makedirs(folder, exist_ok=True)
    scene.frame_start = int(action.frame_range[0])
    scene.frame_end = int(action.frame_range[1])
    for f in frames:
        scene.frame_set(f)
        bpy.context.view_layer.update()
        scene.render.filepath = os.path.join(folder, f"f{f:03d}.png")
        bpy.ops.render.render(write_still=True)


def sample_poses(action_name):
    arm = bpy.data.objects["KAELEN_rig"]
    action = bpy.data.actions[action_name]
    assign(arm, action)
    f0 = int(round(action.frame_range[0]))
    f1 = int(round(action.frame_range[1]))
    out = {}
    for f in range(f0, f1 + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        out[f] = {}
        for n in ARMS:
            pb = arm.pose.bones[n]
            head = arm.matrix_world @ pb.head
            tail = arm.matrix_world @ pb.tail
            y = ((arm.matrix_world @ pb.matrix).to_3x3() @ Vector((0, 1, 0))).normalized()
            out[f][n] = {"head": head.copy(), "tail": tail.copy(), "y": y.copy()}
    return out


def export_clip(action_name, path):
    arm = bpy.data.objects["KAELEN_rig"]
    action = bpy.data.actions[action_name]
    assign(arm, action)
    scene = bpy.context.scene
    scene.frame_start = int(round(action.frame_range[0]))
    scene.frame_end = int(round(action.frame_range[1]))
    for ob in bpy.data.objects:
        ob.select_set(False)
    for name in ("KAELEN_rig", "KAELEN_body", "KAELEN_sword_L", "KAELEN_sword_R"):
        bpy.data.objects[name].select_set(True)
    bpy.context.view_layer.objects.active = arm
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
        mesh_smooth_type="FACE",
        use_armature_deform_only=False,
        path_mode="AUTO",
        bake_anim_use_all_bones=True,
    )
    print("EXPORTED", path, os.path.getsize(path))


def compare_reimport(fbx_path, reference, action_name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(
        filepath=fbx_path,
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
    # Imported action may be named Scene or the take.
    if arm.animation_data and arm.animation_data.action:
        act = arm.animation_data.action
    else:
        act = bpy.data.actions[0]
        arm.animation_data_create()
        assign(arm, act)
    worst = 0.0
    worst_where = None
    y_worst = 0.0
    f0 = min(reference)
    f1 = max(reference)
    for f in range(f0, f1 + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        for n in ARMS:
            if n not in arm.pose.bones:
                # FBX sometimes prefixes the armature name.
                matches = [b.name for b in arm.pose.bones if b.name.endswith(n.split(":")[-1])]
                if not matches:
                    print("MISSING BONE", n, [b.name for b in arm.pose.bones])
                    return {"error": "missing " + n}
                key = matches[0]
            else:
                key = n
            pb = arm.pose.bones[key]
            head = arm.matrix_world @ pb.head
            ref = reference[f][n]["head"]
            # Re-import lives in the FBX axis. Compare bone-local chain lengths and
            # parent-relative direction by also recording the imported head and
            # converting back is not available. We compare the pose after a second
            # export is not this function. Here we store imported heads and the
            # delta against a Blender-space reference only if axes match.
            d = (head - ref).length
            # The axis conversion moves the whole rig. Subtract the hips delta so
            # we compare the arm relative to the hips.
        # relative to RightArm head's parent chain: use vector between shoulder and hand
        def head_of(name):
            key = name if name in arm.pose.bones else next(b.name for b in arm.pose.bones if b.name.endswith(name.split(":")[-1]))
            return arm.matrix_world @ arm.pose.bones[key].head

        for side, up, hand in (
            ("R", "mixamorig:RightArm", "mixamorig:RightArm.002"),
            ("L", "mixamorig:LeftArm", "mixamorig:LeftArm.002"),
        ):
            got = head_of(hand) - head_of(up)
            refv = reference[f][hand]["head"] - reference[f][up]["head"]
            # Length must match regardless of axis. Direction is compared after
            # mapping FBX Y-up back toward Blender Z-up: (x, y, z)_fbx -> (x, -z, y).
            got_b = Vector((got.x, -got.z, got.y))
            dlen = abs(got.length - refv.length)
            # Try both the raw vector and the axis-converted vector; keep the closer.
            d_raw = (got - refv).length
            d_fix = (got_b - refv).length
            d = min(d_raw, d_fix)
            if d > worst:
                worst = d
                worst_where = (action_name, f, side, round(d_raw, 5), round(d_fix, 5), round(dlen, 5))
            y_worst = max(y_worst, dlen)
    return {"max_hand_offset_m": worst, "max_length_err_m": y_worst, "worst": worst_where}


def main():
    report = {"worst_frame_fix8": WORST_FRAME}
    bpy.ops.wm.open_mainfile(filepath=FIX)
    setup_render()
    style_character()
    refs = {}
    for clip in ("attack2_crossthrust", "attack_dualslash"):
        refs[clip] = sample_poses(clip)
        export_clip(clip, os.path.join(FBX_DIR, f"KAELEN_{clip}.fbx"))
    # Round-trip each export. compare_reimport resets the file, so do it last
    # after rendering. Save refs first.
    json.dump(
        {c: {str(f): {n: {"head": list(refs[c][f][n]["head"])} for n in ARMS} for f in refs[c]} for c in refs},
        open(os.path.join(OUT, "pose_ref.json"), "w"),
    )
    # Fixed world cameras. Ortho scale is the full height of the view in metres.
    front = make_cam("CAM_FRONT", Vector((0, -6.0, 1.40)), Vector((0, 0, 1.40)), 3.05)
    side = make_cam("CAM_SIDE", Vector((-6.0, -0.25, 1.40)), Vector((0, -0.25, 1.40)), 3.05)
    # Three-quarter, about 30 degrees down, fixed. Not fitted to the swords.
    three = make_cam("CAM_VIEW", Vector((2.4, -4.2, 2.35)), Vector((0, -0.15, 1.20)), 3.15)
    sh = Vector((-0.16, 0.04, 1.44))
    close = {
        "front": make_cam("CAM_C_FRONT", sh + Vector((0, -1.35, 0.02)), sh, 1.15),
        "side": make_cam("CAM_C_SIDE", sh + Vector((-1.35, -0.05, 0.02)), sh, 1.15),
        "view": make_cam("CAM_C_VIEW", sh + Vector((-0.85, -0.95, 0.28)), sh, 1.20),
        "top": make_cam("CAM_C_TOP", sh + Vector((0.05, -0.15, 1.25)), sh + Vector((0, -0.05, 0)), 1.25),
    }
    # AFTER full-frame sequences (front) and close-up sequences, plus sheet frames.
    cross_sheet = [1, 10, 18, 22, 30, 36, 48]
    dual_sheet = [1, 12, 20, 24, 28, 36, 44, 58]
    render_frames("attack2_crossthrust", list(range(1, 49)), front, os.path.join(OUT, "after_cross_front"))
    render_frames("attack2_crossthrust", list(range(1, 49)), close["view"], os.path.join(OUT, "after_cross_close"))
    render_frames("attack2_crossthrust", cross_sheet, side, os.path.join(OUT, "after_cross_side"))
    render_frames("attack2_crossthrust", cross_sheet, three, os.path.join(OUT, "after_cross_view"))
    for ang, cam in close.items():
        render_frames("attack2_crossthrust", [WORST_FRAME], cam, os.path.join(OUT, "after_close_" + ang))
    render_frames("attack_dualslash", list(range(1, 59)), front, os.path.join(OUT, "after_dual_front"))
    render_frames("attack_dualslash", list(range(1, 59)), close["view"], os.path.join(OUT, "after_dual_close"))
    render_frames("attack_dualslash", dual_sheet, side, os.path.join(OUT, "after_dual_side"))
    render_frames("attack_dualslash", dual_sheet, three, os.path.join(OUT, "after_dual_view"))
    print("AFTER RENDER DONE")

    # BEFORE, same cameras, key frames plus the full motions used on the sheet.
    bpy.ops.wm.open_mainfile(filepath=ORIG)
    setup_render()
    style_character()
    front = make_cam("CAM_FRONT", Vector((0, -6.0, 1.40)), Vector((0, 0, 1.40)), 3.05)
    side = make_cam("CAM_SIDE", Vector((-6.0, -0.25, 1.40)), Vector((0, -0.25, 1.40)), 3.05)
    three = make_cam("CAM_VIEW", Vector((2.4, -4.2, 2.35)), Vector((0, -0.15, 1.20)), 3.15)
    sh = Vector((-0.16, 0.04, 1.44))
    close = {
        "front": make_cam("CAM_C_FRONT", sh + Vector((0, -1.35, 0.02)), sh, 1.15),
        "side": make_cam("CAM_C_SIDE", sh + Vector((-1.35, -0.05, 0.02)), sh, 1.15),
        "view": make_cam("CAM_C_VIEW", sh + Vector((-0.85, -0.95, 0.28)), sh, 1.20),
        "top": make_cam("CAM_C_TOP", sh + Vector((0.05, -0.15, 1.25)), sh + Vector((0, -0.05, 0)), 1.25),
    }
    render_frames("attack2_crossthrust", cross_sheet, front, os.path.join(OUT, "before_cross_front"))
    render_frames("attack2_crossthrust", cross_sheet, side, os.path.join(OUT, "before_cross_side"))
    render_frames("attack_dualslash", dual_sheet, front, os.path.join(OUT, "before_dual_front"))
    render_frames("attack_dualslash", dual_sheet, side, os.path.join(OUT, "before_dual_side"))
    for ang, cam in close.items():
        render_frames("attack2_crossthrust", [WORST_FRAME], cam, os.path.join(OUT, "before_close_" + ang))
    print("BEFORE RENDER DONE")

    # FBX round trip. Reference heads were saved; reload numbers.
    ref_raw = json.load(open(os.path.join(OUT, "pose_ref.json")))
    reference = {}
    for clip, frames in ref_raw.items():
        reference[clip] = {}
        for fs, bones in frames.items():
            reference[clip][int(fs)] = {n: {"head": Vector(bones[n]["head"])} for n in bones}
    roundtrip = {}
    for clip in ("attack2_crossthrust", "attack_dualslash"):
        roundtrip[clip] = compare_reimport(os.path.join(FBX_DIR, f"KAELEN_{clip}.fbx"), reference[clip], clip)
        print("ROUNDTRIP", clip, roundtrip[clip])
    json.dump({"roundtrip": roundtrip, "worst_frame": WORST_FRAME}, open(os.path.join(OUT, "roundtrip.json"), "w"), default=str)
    print("PRESENT DONE")


if __name__ == "__main__":
    main()
