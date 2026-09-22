#!/usr/bin/env python3
"""Meshy Animate FBX → Aldric World stills + planted walk. No Path A weights.

Uses the Design-delivered skin/clip AS-IS. Axis convert only
(Z-up Mixamo → Unity Y-up, face +Z, character-RIGHT +X).
Do NOT claim Design / bind / walk PASS.
"""
from __future__ import annotations

import json
import math
import shutil
import subprocess
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
FBX = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AUTO_RIG_PATH/out/sir_aldric_meshy_animate_walk.fbx"
DROP = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AUTO_RIG_PATH"
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
ART = Path("/opt/cursor/artifacts")
TARGET_H = 1.86

CAM_EYE = (0.0, 2.80, -5.40)
CAM_TARGET = (0.0, 0.90, 0.50)
CAM_EYE_34 = (1.20, 2.80, -5.15)
CAM_FRONT = (0.0, 2.80, 5.40)
CAM_FRONT_T = (0.0, 0.90, -0.50)
CAM_34_FRONT = (-1.20, 2.80, 5.15)
CAM_34_FRONT_T = (0.0, 0.90, 0.0)
CAM_FOV = 30.0

FOOT = {
    "mixamorig:LeftFoot", "mixamorig:RightFoot",
    "mixamorig:LeftToeBase", "mixamorig:RightToeBase",
}


def play_cam_matrix(eye, target) -> Matrix:
    eye_v = Vector(eye)
    fwd = (Vector(target) - eye_v).normalized()
    world_up = Vector((0.0, 1.0, 0.0))
    right = world_up.cross(fwd).normalized()
    up = fwd.cross(right).normalized()
    return Matrix(
        (
            (right.x, up.x, -fwd.x, eye_v.x),
            (right.y, up.y, -fwd.y, eye_v.y),
            (right.z, up.z, -fwd.z, eye_v.z),
            (0.0, 0.0, 0.0, 1.0),
        )
    )


def setup_world():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    try:
        sc.eevee.taa_render_samples = 24
    except AttributeError:
        pass
    try:
        sc.view_settings.view_transform = "Standard"
    except Exception:
        pass
    world = bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.12, 0.13, 0.11, 1.0)
        bg.inputs[1].default_value = 1.15
    key = bpy.data.lights.new("Key", "SUN")
    key.energy = 7.2
    key.angle = 0.08
    key_o = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(key_o)
    key_o.location = (1.4, 3.6, -4.2)
    key_o.rotation_euler = (math.radians(55), 0.0, math.radians(18))
    fill = bpy.data.lights.new("Fill", "AREA")
    fill.energy = 320.0
    fill.size = 5.0
    fill_o = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fill_o)
    fill_o.location = (-2.2, 2.4, -1.5)
    fill_o.rotation_euler = (math.radians(70), 0.0, math.radians(-25))
    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    ground.name = "Ground"
    gmat = bpy.data.materials.new("GroundMat")
    gmat.use_nodes = True
    bsdf = gmat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.10, 0.11, 0.09, 1)
        bsdf.inputs["Roughness"].default_value = 0.9
    ground.data.materials.append(gmat)
    cam_data = bpy.data.cameras.new("WorldPlay")
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(CAM_FOV)
    cam_data.sensor_fit = "VERTICAL"
    cam_data.clip_start = 0.08
    cam_data.clip_end = 40.0
    cam = bpy.data.objects.new("WorldPlay", cam_data)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam
    return cam


def assign_albedo(mesh_ob):
    img = None
    for im in bpy.data.images:
        if im.name.startswith("texture_0") and "metallic" not in im.name and "rough" not in im.name and "normal" not in im.name:
            if im.size[0] >= 256:
                img = im
                break
    if img is None:
        for im in bpy.data.images:
            if im.size[0] >= 512 and "normal" not in im.name.lower():
                img = im
                break
    mat = mesh_ob.data.materials[0] if mesh_ob.data.materials else bpy.data.materials.new("AldricAlbedo")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    if img is not None:
        tex.image = img
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if not mesh_ob.data.materials:
        mesh_ob.data.materials.append(mat)
    else:
        mesh_ob.data.materials[0] = mat


def mesh_world_ys(mesh_ob, groups=None):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    ev = mesh_ob.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    vg = {g.index: g.name for g in mesh_ob.vertex_groups}
    ys = []
    src = mesh_ob.data
    n = min(len(me.vertices), len(src.vertices))
    for i in range(n):
        if groups:
            names = {vg.get(g.group, "") for g in src.vertices[i].groups if g.weight > 0.35}
            if not (names & groups):
                continue
        ys.append((mw @ me.vertices[i].co).y)
    ev.to_mesh_clear()
    return ys


def plant(root, mesh_ob):
    ys = mesh_world_ys(mesh_ob, FOOT) or mesh_world_ys(mesh_ob)
    if not ys:
        return 0.0
    dy = min(ys) - 0.002
    root.location.y -= dy
    bpy.context.view_layer.update()
    return dy


def import_fbx():
    if not FBX.exists():
        raise SystemExit(f"missing {FBX}")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(FBX))
    junk = [o for o in bpy.data.objects if o.type == "MESH" and "Ico" in o.name]
    for o in junk:
        bpy.data.objects.remove(o, do_unlink=True)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
    assign_albedo(mesh)
    root = bpy.data.objects.new("AldricWorldRoot", None)
    bpy.context.collection.objects.link(root)
    # Mixamo after Blender FBX import: Z-up, face −Y, +X = character-LEFT.
    # Unity World: Y-up, face +Z, character-RIGHT +X. (x,y,z) → (−x, z, −y)
    root.matrix_world = Matrix((
        (-1.0, 0.0, 0.0, 0.0),
        (0.0, 0.0, 1.0, 0.0),
        (0.0, -1.0, 0.0, 0.0),
        (0.0, 0.0, 0.0, 1.0),
    ))
    arm.parent = root
    bpy.context.view_layer.update()
    ys = mesh_world_ys(mesh)
    h = max(ys) - min(ys)
    if h > 0.4:
        root.scale = (TARGET_H / h,) * 3
        bpy.context.view_layer.update()
    plant(root, mesh)
    acts = sorted(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0], reverse=True)
    walk = acts[0]
    arm.animation_data_create()
    print(
        "imported", mesh.name, "v", len(mesh.data.vertices), "f", len(mesh.data.polygons),
        "walk", walk.name, "range", tuple(walk.frame_range),
        "fps", bpy.context.scene.render.fps,
    )
    return root, arm, mesh, walk


def set_cam(cam, eye, tgt):
    cam.matrix_world = play_cam_matrix(eye, tgt)


def render_to(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print("wrote", path, path.stat().st_size)
    try:
        ART.mkdir(parents=True, exist_ok=True)
        (ART / path.name).write_bytes(path.read_bytes())
    except OSError as exc:
        print("artifact skip", exc)


def pose_walk(root, arm, mesh, walk, frame: float):
    arm.animation_data.action = walk
    bpy.context.scene.frame_set(int(round(frame)))
    root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    plant(root, mesh)


def rest_pose(root, arm, mesh, rest_act):
    if rest_act is not None:
        arm.animation_data.action = rest_act
        bpy.context.scene.frame_set(int(rest_act.frame_range[0]))
    else:
        arm.animation_data.action = None
        for pb in arm.pose.bones:
            pb.matrix_basis.identity()
    root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    plant(root, mesh)


def write_drop(note: dict):
    DROP.mkdir(parents=True, exist_ok=True)
    (DROP / "README.md").write_text(
        "# AUTO_RIG_PATH — Meshy Animate skinned walk (PROOF ONLY)\n\n"
        "Path A weight-paint **CANCELLED**. Design delivered Meshy Animate "
        "FBX (`Walking` clip). Skin/weights used **AS-IS**.\n\n"
        "- FBX: `out/sir_aldric_meshy_animate_walk.fbx` (also ThemePack 3d/ for Unity Humanoid import)\n"
        "- Unity: `SirAldricMeshyAnimateActor` instantiates the FBX and plays `Walking`. "
        "No Path A weight painting, no remesh-melt proxy, no Hand_R sheath hacks.\n"
        "- Stills: World front / ¾ / rear. Walk: planted Game-view toward TOP.\n"
        "- **Do NOT claim Design PASS. Hub PNG HOLD. PR #21 HOLD.**\n\n"
        f"verts={note.get('verts')} faces={note.get('faces')} clip={note.get('clip')}\n"
    )
    for name in (
        "world_front.png", "world_34_front.png", "world_rear.png",
        "world_rear_34.png",
        "sir_aldric_meshy_animate_walk_toward_top.mp4",
    ):
        src = PROOF / name
        if src.exists():
            shutil.copy2(src, DROP / name)
    for name in (
        "world_walk_contact_l.png", "world_walk_mid_swing.png",
        "world_walk_pass_l.png", "f_001.png", "f_008.png",
    ):
        src = WALK / name
        if src.exists():
            shutil.copy2(src, DROP / name)
    (DROP / "meshy_animate_proof.json").write_text(json.dumps(note, indent=2) + "\n")
    print("drop", DROP)


def main():
    root, arm, mesh, walk = import_fbx()
    cam = setup_world()
    PROOF.mkdir(parents=True, exist_ok=True)
    WALK.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)

    rest_act = min(bpy.data.actions, key=lambda a: a.frame_range[1] - a.frame_range[0])
    if rest_act == walk:
        rest_act = None
    rest_pose(root, arm, mesh, rest_act)
    for name, eye, tgt in (
        ("world_front", CAM_FRONT, CAM_FRONT_T),
        ("world_34_front", CAM_34_FRONT, CAM_34_FRONT_T),
        ("world_rear", CAM_EYE, CAM_TARGET),
        ("world_rear_34", CAM_EYE_34, CAM_TARGET),
    ):
        set_cam(cam, eye, tgt)
        render_to(PROOF / f"{name}.png")
        try:
            (ART / f"meshy_animate_{name}.png").write_bytes((PROOF / f"{name}.png").read_bytes())
        except OSError:
            pass

    set_cam(cam, CAM_EYE, CAM_TARGET)
    f0, f1 = walk.frame_range
    nclip = int(f1 - f0) + 1
    fps = 24
    bpy.context.scene.render.fps = fps
    cycles = 2
    frames = []
    for c in range(cycles):
        for i in range(nclip):
            fr = f0 + i
            pose_walk(root, arm, mesh, walk, fr)
            dest = WALK / f"f_{len(frames):03d}.png"
            render_to(dest)
            frames.append(dest)

    # Contact / mid-swing stills from first cycle.
    mid = f0 + (f1 - f0) * 0.40
    contact = f0 + (f1 - f0) * 0.25
    pose_walk(root, arm, mesh, walk, contact)
    render_to(WALK / "world_walk_contact_l.png")
    pose_walk(root, arm, mesh, walk, mid)
    render_to(WALK / "world_walk_mid_swing.png")
    pose_walk(root, arm, mesh, walk, f0)
    render_to(WALK / "world_walk_pass_l.png")

    mp4 = PROOF / "sir_aldric_meshy_animate_walk_toward_top.mp4"
    subprocess.check_call(
        [
            "ffmpeg", "-y", "-framerate", str(fps),
            "-i", str(WALK / "f_%03d.png"),
            "-pix_fmt", "yuv420p", "-vf", "scale=1080:1920",
            "-crf", "18", "-movflags", "+faststart", str(mp4),
        ]
    )
    print("mp4", mp4, mp4.stat().st_size)
    try:
        (ART / mp4.name).write_bytes(mp4.read_bytes())
        (ART / "meshy_animate_walk_contact_l.png").write_bytes(
            (WALK / "world_walk_contact_l.png").read_bytes()
        )
        (ART / "meshy_animate_walk_mid_swing.png").write_bytes(
            (WALK / "world_walk_mid_swing.png").read_bytes()
        )
    except OSError as exc:
        print("artifact skip", exc)

    note = {
        "lookPassClaimed": False,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "pathAWeightPaint": "CANCELLED",
        "source": "meshy-animate-walk-fbx-20260922",
        "fbx": str(FBX.relative_to(ROOT)),
        "clip": walk.name,
        "clipFrames": [float(f0), float(f1)],
        "verts": len(mesh.data.vertices),
        "faces": len(mesh.data.polygons),
        "honestArt": (
            "Meshy Animate skinned walk FBX wired as-is (Mixamo humanoid). "
            "No Path A weight paint. World stills + planted walk toward TOP. "
            "Do NOT claim Design PASS. Hub HOLD. PR #21 HOLD."
        ),
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PROOF / "meshy_animate_proof.json").write_text(json.dumps(note, indent=2) + "\n")
    write_drop(note)
    print("done meshy animate world proof")


if __name__ == "__main__":
    main()
