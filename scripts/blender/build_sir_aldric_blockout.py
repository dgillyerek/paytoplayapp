#!/usr/bin/env python3
"""Gate 2: grey-clay mid-poly Sir Aldric BLOCKOUT from Gate 1 turnaround.

Helmeted knight matching LOCKED turnaround silhouette / proportions.
NOT box cylinders. Grey clay only — no textures, no walk, no Play hub.

Blender Z-up. Character faces +Y. Character-RIGHT = +X. Height 1.86 m.
"""
from __future__ import annotations

import math
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
PROOF = ROOT / "Docs/Survival/previews/blockout"
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"

HEIGHT = 1.86


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.unit_settings.system = "METRIC"
    sc.unit_settings.scale_length = 1.0
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x = 720
    sc.render.resolution_y = 1280
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "SINGLE"
    sh.single_color = (0.58, 0.58, 0.60)
    sh.show_cavity = True
    sh.cavity_type = "BOTH"
    sh.cavity_ridge_factor = 1.0
    sh.cavity_valley_factor = 1.25
    sh.show_shadows = True
    sh.show_specular_highlight = True
    world = bpy.data.worlds.new("ClayWorld")
    sc.world = world
    world.color = (0.78, 0.78, 0.76)


def _ellip(bm, center, radii, segs=14, rings=9):
    ret = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    c = Vector(center)
    rx, ry, rz = radii
    for v in ret["verts"]:
        v.co = Vector((v.co.x * rx, v.co.y * ry, v.co.z * rz)) + c


def _bone(bm, p0, p1, r0, r1, segs=12):
    a, b = Vector(p0), Vector(p1)
    axis = b - a
    length = max(axis.length, 1e-6)
    mid = (a + b) * 0.5
    ret = bmesh.ops.create_cone(
        bm, cap_ends=True, segments=segs, radius1=r0, radius2=r1, depth=length
    )
    quat = Vector((0.0, 0.0, 1.0)).rotation_difference(axis.normalized())
    for v in ret["verts"]:
        v.co = quat @ v.co + mid


def _ring(z, rx, ry, segs=22, y_off=0.0):
    return [
        (rx * math.cos(i / segs * math.tau), ry * math.sin(i / segs * math.tau) + y_off, z)
        for i in range(segs)
    ]


def build_knight():
    """Overlapping mid-poly volumes → voxel remesh = one clay knight."""
    bm = bmesh.new()

    # Great helm + gorget (connected, taller than a ball)
    _ellip(bm, (0.0, 0.03, 1.72), (0.120, 0.148, 0.165), 18, 12)
    _ellip(bm, (0.0, 0.11, 1.64), (0.102, 0.078, 0.072), 14, 8)
    _ellip(bm, (0.0, 0.00, 1.54), (0.095, 0.100, 0.060), 14, 7)
    _ellip(bm, (0.0, 0.00, 1.86), (0.022, 0.022, 0.018), 8, 6)
    _bone(bm, (0.0, 0.00, 1.52), (0.0, 0.01, 1.40), 0.078, 0.110, 12)

    # Solid A-line surcoat — heavy Z overlap so remesh does not ring
    _ellip(bm, (0.0, 0.03, 1.38), (0.200, 0.138, 0.165), 16, 10)
    _ellip(bm, (0.0, 0.03, 1.18), (0.178, 0.128, 0.165), 16, 10)
    _ellip(bm, (0.0, 0.02, 0.98), (0.172, 0.120, 0.155), 16, 10)
    _ellip(bm, (0.0, 0.01, 0.80), (0.210, 0.138, 0.140), 16, 10)
    _ellip(bm, (0.0, 0.00, 0.70), (0.230, 0.148, 0.070), 16, 8)
    _ellip(bm, (0.0, 0.08, 1.28), (0.150, 0.105, 0.115), 14, 8)

    # Pauldrons overlapping torso + upper arm
    _ellip(bm, (-0.22, 0.02, 1.44), (0.130, 0.112, 0.095), 14, 9)
    _ellip(bm, (0.22, 0.02, 1.44), (0.130, 0.112, 0.095), 14, 9)

    # A-pose plate-arm mass
    for s in (-1.0, 1.0):
        _bone(bm, (s * 0.16, 0.02, 1.40), (s * 0.34, 0.05, 1.12), 0.072, 0.060, 12)
        _ellip(bm, (s * 0.34, 0.05, 1.12), (0.062, 0.056, 0.052), 10, 6)
        _bone(bm, (s * 0.34, 0.05, 1.12), (s * 0.46, 0.06, 0.84), 0.058, 0.048, 12)
        _ellip(bm, (s * 0.48, 0.06, 0.78), (0.052, 0.046, 0.062), 10, 7)

    # Plate-leg mass + sabatons
    for s in (-1.0, 1.0):
        _bone(bm, (s * 0.10, 0.02, 0.80), (s * 0.105, 0.03, 0.50), 0.092, 0.072, 12)
        _ellip(bm, (s * 0.105, 0.03, 0.50), (0.075, 0.066, 0.055), 10, 6)
        _bone(bm, (s * 0.105, 0.03, 0.50), (s * 0.10, 0.05, 0.10), 0.068, 0.052, 12)
        _ellip(bm, (s * 0.10, 0.08, 0.055), (0.056, 0.105, 0.044), 12, 7)
        _ellip(bm, (s * 0.10, 0.14, 0.035), (0.040, 0.058, 0.032), 8, 5)

    # Belt, left pouch, character-RIGHT sheathed scabbard
    _ellip(bm, (0.0, 0.02, 0.98), (0.165, 0.115, 0.032), 14, 6)
    _ellip(bm, (-0.16, 0.05, 0.96), (0.048, 0.032, 0.036), 8, 5)
    _bone(bm, (0.18, -0.01, 1.00), (0.26, -0.05, 0.48), 0.036, 0.022, 10)
    _ellip(bm, (0.18, -0.01, 1.00), (0.032, 0.024, 0.022), 8, 5)
    _ellip(bm, (0.19, -0.01, 1.10), (0.016, 0.014, 0.040), 8, 5)
    _bone(bm, (0.12, -0.01, 1.09), (0.26, -0.01, 1.09), 0.010, 0.010, 8)

    me = bpy.data.meshes.new("SirAldricBlockout")
    bm.to_mesh(me)
    bm.free()
    body = bpy.data.objects.new("SirAldricBlockout", me)
    bpy.context.collection.objects.link(body)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)

    rem = body.modifiers.new("Remesh", "REMESH")
    rem.mode = "VOXEL"
    rem.voxel_size = 0.014
    rem.adaptivity = 0.0
    bpy.ops.object.modifier_apply(modifier="Remesh")
    sm = body.modifiers.new("Smooth", "SMOOTH")
    sm.factor = 0.55
    sm.iterations = 5
    bpy.ops.object.modifier_apply(modifier="Smooth")
    for p in body.data.polygons:
        p.use_smooth = True
    return body


def add_camera(name, loc, target, ortho=2.20):
    cam_data = bpy.data.cameras.new(name)
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.new(name, cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = loc
    # aim
    direction = Vector(target) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return cam


def render_view(cam, path: Path):
    bpy.context.scene.camera = cam
    bpy.context.scene.render.filepath = str(path)
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print("render", path, path.stat().st_size)


def main():
    PACK.mkdir(parents=True, exist_ok=True)
    PROOF.mkdir(parents=True, exist_ok=True)
    reset()
    body = build_knight()
    # Ground hint (tiny plane so feet read)
    bpy.ops.mesh.primitive_plane_add(size=2.4, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    mat = bpy.data.materials.new("GroundMat")
    mat.diffuse_color = (0.82, 0.82, 0.80, 1)
    ground.data.materials.append(mat)

    mid = (0.0, 0.0, 0.93)
    views = {
        "front": add_camera("CamFront", (0.0, 4.6, 0.93), mid, 2.15),
        "side_r": add_camera("CamSideR", (4.6, 0.0, 0.93), mid, 2.15),
        "back": add_camera("CamBack", (0.0, -4.6, 0.93), mid, 2.15),
        "three_quarter": add_camera("Cam3Q", (3.2, -3.2, 1.05), mid, 2.20),
    }
    for name, cam in views.items():
        render_view(cam, PROOF / f"blockout_{name}.png")

    blend = PACK / "sir_aldric_blockout.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    print("blend", blend, blend.stat().st_size, "verts", len(body.data.vertices))


if __name__ == "__main__":
    main()
