#!/usr/bin/env python3
"""Gate 2: grey-clay mid-poly Sir Aldric BLOCKOUT from Gate 1 turnaround.

Helmeted A-pose knight matching LOCKED turnaround silhouette / proportions.
NOT box cylinders. NOT a remesh-melted mannequin. Grey clay only.

Blender Z-up. Character faces +Y. Character-RIGHT = +X. Height 1.86 m (crest).
Scabbard character-RIGHT only. Lion *space* = flat back plaque. No cape.
No textures, no walk, no Play hub.
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
    sh.single_color = (0.62, 0.62, 0.64)
    sh.show_cavity = True
    sh.cavity_type = "BOTH"
    sh.cavity_ridge_factor = 1.15
    sh.cavity_valley_factor = 1.45
    sh.show_shadows = True
    sh.show_specular_highlight = True
    world = bpy.data.worlds.new("ClayWorld")
    sc.world = world
    world.color = (0.80, 0.80, 0.78)


def _ellip(bm, center, radii, segs=16, rings=10):
    ret = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    c = Vector(center)
    rx, ry, rz = radii
    for v in ret["verts"]:
        v.co = Vector((v.co.x * rx, v.co.y * ry, v.co.z * rz)) + c


def _bone(bm, p0, p1, r0, r1, segs=14):
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


def _capsule(bm, p0, p1, r, segs=14):
    _bone(bm, p0, p1, r, r, segs)
    _ellip(bm, p0, (r, r, r), segs, max(6, segs // 2))
    _ellip(bm, p1, (r, r, r), segs, max(6, segs // 2))


def oval_ring(z, rx, ry, segs=24, y_off=0.0):
    return [
        (rx * math.cos(i / segs * math.tau), ry * math.sin(i / segs * math.tau) + y_off, z)
        for i in range(segs)
    ]


def add_loft(bm, rings):
    segs = len(rings[0])
    rows = []
    for ring in rings:
        rows.append([bm.verts.new(Vector(p)) for p in ring])
    bm.verts.ensure_lookup_table()
    for i in range(len(rows) - 1):
        for s in range(segs):
            s1 = (s + 1) % segs
            bm.faces.new((rows[i][s], rows[i][s1], rows[i + 1][s1], rows[i + 1][s]))
    bm.faces.new(list(reversed(rows[0])))
    bm.faces.new(rows[-1])


def flatten_front(bm, y_min, scale=0.55, z_lo=0.0, z_hi=9.0):
    """Pull +Y verts into a visor / breast plane so helm is not a ball."""
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        if v.co.y > y_min and z_lo <= v.co.z <= z_hi:
            v.co.y = y_min + (v.co.y - y_min) * scale


def build_knight():
    """Armor-shaped volumes joined as one clay mesh. No remesh melt."""
    bm = bmesh.new()

    # --- Closed armet (pointed crown + visor plane + brow + gorget) ---
    _ellip(bm, (0.0, 0.04, 1.68), (0.112, 0.168, 0.148), 20, 14)
    flatten_front(bm, 0.12, 0.34, 1.52, 1.80)
    # Visor plate — thin in depth so it does not read as a clown-nose ball
    _ellip(bm, (0.0, 0.15, 1.64), (0.098, 0.026, 0.052), 16, 10)
    flatten_front(bm, 0.16, 0.40, 1.56, 1.72)
    _bone(bm, (0.0, 0.02, 1.78), (0.0, 0.00, 1.86), 0.055, 0.018, 12)  # pointed crown
    _ellip(bm, (0.0, 0.00, 1.86), (0.018, 0.018, 0.016), 8, 6)  # crest knob
    _ellip(bm, (0.0, 0.00, 1.52), (0.088, 0.100, 0.048), 14, 8)  # gorget
    _capsule(bm, (0.0, 0.00, 1.48), (0.0, 0.02, 1.56), 0.062, 12)

    # --- Breastplate mass (silver volume, sits under collar) ---
    _ellip(bm, (0.0, 0.06, 1.34), (0.155, 0.095, 0.110), 16, 10)
    flatten_front(bm, 0.10, 0.50, 1.22, 1.46)

    # --- A-line royal-blue surcoat volume (cloth, hem mid-thigh) ---
    segs = 24
    add_loft(
        bm,
        [
            oval_ring(1.48, 0.168, 0.100, segs, 0.02),
            oval_ring(1.34, 0.188, 0.115, segs, 0.02),
            oval_ring(1.18, 0.178, 0.110, segs, 0.01),
            oval_ring(1.04, 0.172, 0.108, segs, 0.01),
            oval_ring(0.96, 0.198, 0.122, segs, 0.02),
            oval_ring(0.86, 0.228, 0.138, segs, 0.03),
            oval_ring(0.74, 0.248, 0.148, segs, 0.03),
        ],
    )
    # Hem band (Greek-key *space*)
    add_loft(
        bm,
        [
            oval_ring(0.72, 0.252, 0.150, segs, 0.03),
            oval_ring(0.80, 0.250, 0.148, segs, 0.03),
        ],
    )

    # Lion space — flat back plaque (readable heraldry placement)
    _ellip(bm, (0.0, -0.12, 1.22), (0.145, 0.018, 0.175), 14, 8)

    # --- Pauldrons (shells over the shoulder, not floating balls) ---
    for s in (-1.0, 1.0):
        _ellip(bm, (s * 0.22, 0.02, 1.44), (0.115, 0.100, 0.088), 16, 10)
        _ellip(bm, (s * 0.24, 0.02, 1.38), (0.125, 0.108, 0.040), 14, 8)  # rim

    # --- A-pose plate arms (~22°) + elbow cops + gauntlets ---
    for s in (-1.0, 1.0):
        _capsule(bm, (s * 0.18, 0.02, 1.40), (s * 0.32, 0.04, 1.14), 0.062, 14)
        _ellip(bm, (s * 0.32, 0.04, 1.14), (0.066, 0.060, 0.050), 12, 8)
        _capsule(bm, (s * 0.32, 0.04, 1.14), (s * 0.44, 0.05, 0.88), 0.054, 14)
        _ellip(bm, (s * 0.45, 0.06, 0.80), (0.050, 0.042, 0.060), 12, 8)
        _ellip(bm, (s * 0.44, 0.05, 0.88), (0.054, 0.044, 0.022), 10, 6)

    # --- Plate legs + poleyns + fused pointed sabatons (overlap so remesh cannot pinch) ---
    for s in (-1.0, 1.0):
        _bone(bm, (s * 0.105, 0.02, 0.92), (s * 0.108, 0.03, 0.52), 0.080, 0.066, 14)
        _ellip(bm, (s * 0.108, 0.04, 0.50), (0.074, 0.070, 0.048), 12, 8)
        _bone(bm, (s * 0.108, 0.03, 0.50), (s * 0.102, 0.06, 0.06), 0.062, 0.050, 14)
        _capsule(bm, (s * 0.102, 0.05, 0.14), (s * 0.102, 0.08, 0.04), 0.048, 12)
        _ellip(bm, (s * 0.102, 0.10, 0.045), (0.054, 0.100, 0.040), 14, 8)
        _ellip(bm, (s * 0.102, 0.16, 0.038), (0.042, 0.070, 0.032), 12, 7)
        _bone(bm, (s * 0.102, 0.18, 0.036), (s * 0.102, 0.22, 0.030), 0.036, 0.022, 10)

    # --- Belt + character-LEFT pouch ---
    _ellip(bm, (0.0, 0.02, 0.99), (0.175, 0.118, 0.028), 16, 7)
    _ellip(bm, (0.0, 0.12, 0.99), (0.030, 0.018, 0.022), 8, 6)
    _ellip(bm, (-0.16, 0.06, 0.96), (0.042, 0.030, 0.038), 10, 7)

    # --- Character-RIGHT sheathed scabbard (hangs slightly back) ---
    _bone(bm, (0.20, -0.04, 1.02), (0.28, -0.10, 0.42), 0.028, 0.016, 12)
    _ellip(bm, (0.20, -0.04, 1.02), (0.032, 0.022, 0.020), 10, 6)
    _bone(bm, (0.14, -0.04, 1.04), (0.28, -0.04, 1.04), 0.010, 0.010, 8)
    _ellip(bm, (0.20, -0.04, 1.10), (0.014, 0.012, 0.036), 8, 6)
    _ellip(bm, (0.28, -0.10, 0.42), (0.016, 0.014, 0.018), 8, 5)

    me = bpy.data.meshes.new("SirAldricBlockout")
    bm.to_mesh(me)
    bm.free()
    body = bpy.data.objects.new("SirAldricBlockout", me)
    bpy.context.collection.objects.link(body)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)

    # Fuse overlaps only — keep helm / hem / scabbard / sabaton readable.
    rem = body.modifiers.new("Remesh", "REMESH")
    rem.mode = "VOXEL"
    rem.voxel_size = 0.007
    rem.adaptivity = 0.0
    bpy.ops.object.modifier_apply(modifier="Remesh")
    # One light pass so voxels don't glitter; do not melt into a mannequin.
    sm = body.modifiers.new("Smooth", "SMOOTH")
    sm.factor = 0.18
    sm.iterations = 2
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
    bpy.ops.mesh.primitive_plane_add(size=2.6, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    mat = bpy.data.materials.new("GroundMat")
    mat.diffuse_color = (0.84, 0.84, 0.82, 1)
    ground.data.materials.append(mat)

    mid = (0.0, 0.0, 0.93)
    views = {
        "front": add_camera("CamFront", (0.0, 4.6, 0.93), mid, 2.10),
        "side_r": add_camera("CamSideR", (4.6, 0.0, 0.93), mid, 2.10),
        "back": add_camera("CamBack", (0.0, -4.6, 0.93), mid, 2.10),
        # Front-right ¾ — helmeted A-pose, scabbard near-side
        "three_quarter": add_camera("Cam3Q", (3.70, 2.90, 1.08), mid, 2.16),
    }
    for name, cam in views.items():
        render_view(cam, PROOF / f"blockout_{name}.png")

    blend = PACK / "sir_aldric_blockout.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    print("blend", blend, blend.stat().st_size, "verts", len(body.data.vertices))


if __name__ == "__main__":
    main()
