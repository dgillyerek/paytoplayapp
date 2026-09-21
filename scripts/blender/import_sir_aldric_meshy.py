#!/usr/bin/env python3
"""Path 2 Meshy retopo → Unity Y-up look mesh + Play-angle World-cam stills.

LOOK only. Does not touch Evaluate() / Animator / Play hub PNG.
Source drop: design/.../AI_MESH_PATH2/out/aldric_meshy_retopo.glb
"""
from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
SRC_GLB = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AI_MESH_PATH2/out/aldric_meshy_retopo.glb"
PROOF = ROOT / "Docs/Survival/previews/gate3"
ART = Path("/opt/cursor/artifacts")
TARGET_H = 1.86

# Play World-cam (Unity Y-up). TOP = +Z = away.
CAM_EYE = (0.0, 2.80, -5.40)
CAM_TARGET = (0.0, 0.90, 0.50)
CAM_EYE_34 = (1.20, 2.80, -5.15)
CAM_FRONT = (0.0, 2.80, 5.40)
CAM_FRONT_T = (0.0, 0.90, -0.50)
CAM_SIDE = (5.40, 2.80, 0.0)
CAM_SIDE_T = (0.50, 0.90, 0.0)
CAM_34_FRONT = (-1.20, 2.80, 5.15)
CAM_34_FRONT_T = (0.0, 0.90, 0.0)
CAM_FOV = 30.0


def play_cam_matrix(eye, target) -> Matrix:
    """Same laterality as render_gate3_playcam.py: right = world_up × forward."""
    eye_v = Vector(eye)
    fwd = (Vector(target) - eye_v).normalized()
    world_up = Vector((0.0, 1.0, 0.0))
    right = world_up.cross(fwd).normalized()
    up = fwd.cross(right).normalized()
    # Blender camera looks down local −Z, +Y = up, +X = right.
    return Matrix(
        (
            (right.x, up.x, -fwd.x, eye_v.x),
            (right.y, up.y, -fwd.y, eye_v.y),
            (right.z, up.z, -fwd.z, eye_v.z),
            (0.0, 0.0, 0.0, 1.0),
        )
    )


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def import_and_orient():
    bpy.ops.import_scene.gltf(filepath=str(SRC_GLB))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    if not meshes:
        raise RuntimeError("no mesh in GLB")
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = "SirAldricMeshy"

    me = ob.data
    # Import: Z-up, faces −Y, scabbard −X.
    # Unity Y-up: (−x, z, −y) → face +Z, scabbard +X, up +Y.
    for v in me.vertices:
        x, y, z = v.co
        v.co = Vector((-x, z, -y))
    me.update()

    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    me.calc_loop_triangles()

    ys = [v.co.y for v in me.vertices]
    ymin, ymax = min(ys), max(ys)
    h = ymax - ymin
    scale = TARGET_H / h if h > 1e-4 else 1.0
    for v in me.vertices:
        v.co = Vector((v.co.x * scale, (v.co.y - ymin) * scale, v.co.z * scale))
    me.update()

    xs = [v.co.x for v in me.vertices]
    zs = [v.co.z for v in me.vertices]
    ys = [v.co.y for v in me.vertices]
    print(
        "oriented bbox x", round(min(xs), 3), round(max(xs), 3),
        "y", round(min(ys), 3), round(max(ys), 3),
        "z", round(min(zs), 3), round(max(zs), 3),
        "scabbard+X" if max(xs) > abs(min(xs)) else "WARN scabbard maybe -X",
        "faces+Z" if max(zs) > 0.05 else "WARN forward",
        "tris", len(me.loop_triangles),
    )
    # Flip normals if the crown points inward (mean normal of top verts).
    top = [v for v in me.vertices if v.co.y > TARGET_H * 0.92]
    if top:
        n = Vector((0, 0, 0))
        for v in top:
            n += v.normal
        n.normalize()
        print("crown normal", tuple(round(c, 3) for c in n))
        if n.y < 0:
            print("flipping normals")
            bm = bmesh.new()
            bm.from_mesh(me)
            bmesh.ops.reverse_faces(bm, faces=bm.faces)
            bm.to_mesh(me)
            bm.free()
            me.update()
    for p in me.polygons:
        p.use_smooth = True
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception:
        pass
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def setup_world_render():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = 1080
    sc.render.resolution_y = 1920
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    try:
        sc.eevee.taa_render_samples = 32
    except AttributeError:
        pass
    world = bpy.data.worlds.new("WorldCam")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.12, 0.13, 0.11, 1.0)
        bg.inputs[1].default_value = 1.15
    try:
        sc.view_settings.view_transform = "Standard"
    except Exception:
        pass


def add_lights():
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
    rim = bpy.data.lights.new("Rim", "SUN")
    rim.energy = 2.4
    rim_o = bpy.data.objects.new("Rim", rim)
    bpy.context.collection.objects.link(rim_o)
    rim_o.location = (0.0, 2.2, 3.4)
    rim_o.rotation_euler = (math.radians(40), math.radians(180), 0.0)


def add_ground():
    bpy.ops.mesh.primitive_plane_add(size=10.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    ground.name = "Ground"
    mat = bpy.data.materials.new("GroundMat")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.10, 0.11, 0.09, 1)
        bsdf.inputs["Roughness"].default_value = 0.9
    ground.data.materials.append(mat)


def make_cam():
    cam_data = bpy.data.cameras.new("WorldPlay")
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(CAM_FOV)
    cam_data.sensor_fit = "VERTICAL"
    cam_data.clip_start = 0.08
    cam_data.clip_end = 40.0
    cam = bpy.data.objects.new("WorldPlay", cam_data)
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    return cam


def render_shots(cam):
    PROOF.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    shots = {
        "world_rear": (CAM_EYE, CAM_TARGET),
        "world_rear_34": (CAM_EYE_34, CAM_TARGET),
        "world_front": (CAM_FRONT, CAM_FRONT_T),
        "world_side_r": (CAM_SIDE, CAM_SIDE_T),
        "world_34_front": (CAM_34_FRONT, CAM_34_FRONT_T),
    }
    for name, (eye, tgt) in shots.items():
        cam.matrix_world = play_cam_matrix(eye, tgt)
        path = PROOF / f"{name}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("render", path, path.stat().st_size)


def export_look(ob):
    PACK3D.mkdir(parents=True, exist_ok=True)
    for name in ("Ground", "Key", "Fill", "Rim", "WorldPlay"):
        o = bpy.data.objects.get(name)
        if o:
            bpy.data.objects.remove(o, do_unlink=True)

    # Keep ThemePack GLB as the Design drop bytes (do not re-export — glTF
    # +Y-up convert would scramble the Unity-Y mesh stored in this .blend).
    blend = PACK3D / "sir_aldric_meshy.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    print("blend", blend, blend.stat().st_size)

    ntris = len(ob.data.loop_triangles)
    nverts = len(ob.data.vertices)
    manifest = {
        "name": "SirAldric",
        "track": "meshy-path2",
        "gate": 3,
        "source": "AI_MESH_PATH2/out/aldric_meshy_retopo.glb",
        "note": "Path 2 Meshy + UV bleed + SoT lion cards. LOOK stills only. lookPassClaimed false. Not scripted loft.",
        "soT": "look_targets/01_rear_LOCKED.png",
        "turnaround": "TURNAROUND_GATE1/LOCKED/",
        "scabbard": "character-right",
        "glb": "sir_aldric_meshy_retopo.glb",
        "blend": "sir_aldric_meshy.blend",
        "verts": nverts,
        "tris": ntris,
        "lookPassClaimed": False,
        "walkPaused": True,
        "playHubLocked": True,
        "motionBindUnchanged": "sir_aldric_midpoly.mesh.txt + Evaluate() HOLD",
    }
    (PACK3D / "sir_aldric_meshy.json").write_text(json.dumps(manifest, indent=2) + "\n")
    # Do not clobber loft Actor bind pointers used by tests.
    existing = {}
    p3d = PACK3D / "sir_aldric_3d.json"
    if p3d.exists():
        try:
            existing = json.loads(p3d.read_text())
        except Exception:
            existing = {}
    existing.update(
        {
            "lookTrack": "meshy-path2",
            "lookGlb": "sir_aldric_meshy_retopo.glb",
            "lookBlend": "sir_aldric_meshy.blend",
            "lookTris": ntris,
            "lookPassClaimed": False,
            "walkPaused": True,
            "playHubLocked": True,
        }
    )
    p3d.write_text(json.dumps(existing, indent=2) + "\n")


def main():
    if not SRC_GLB.exists():
        raise SystemExit(f"missing {SRC_GLB}")
    reset_scene()
    ob = import_and_orient()
    setup_world_render()
    add_ground()
    add_lights()
    cam = make_cam()
    render_shots(cam)
    export_look(ob)
    print("done Path 2 Meshy look stills")


if __name__ == "__main__":
    main()
