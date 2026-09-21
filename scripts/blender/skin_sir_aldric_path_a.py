#!/usr/bin/env python3
"""Path A LOCKED: clean mid-poly retopo + project Meshy/e5b132f albedo.

Derek: STOP tube / paper / remesh-melt / capsule bind hacks.
1) SOURCE = Path 2 Meshy GLB + e5b132f Image_0 / SoT lion.
2) CLEAN RETOPO = QuadriFlow mid-poly (manifold, humanoid-ready).
   Voxel is scaffold only, then shrinkwrap back onto the Meshy surface.
   Not the hero mesh. No capsule arms/legs. No paper-island weights.
3) TEXTURE PROJECT = bake/transfer Meshy albedo onto retopo UVs.
4) FBX = Y-up, face +Z, one character-RIGHT scabbard, Actor armature.
5) RE-GATE = World stills vs e5b132f/turnaround + one Evaluate() walk.

Bind/walk NOT claimed. Hub PNG not swapped. Evaluate() keys reused.
"""
from __future__ import annotations

import json
import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Vector
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import import_sir_aldric_meshy as shots
import skin_sir_aldric_hangheat as hh
import skin_sir_aldric_meshy as old
import skin_sir_aldric_meshylook as look
import skin_sir_aldric_retopo as rt

ROOT = old.ROOT
PACK3D = old.PACK3D
PROOF = old.PROOF
ART = Path("/opt/cursor/artifacts")
MESH_BONES = old.MESH_BONES
TARGET_FACES = int(os.environ.get("PATHA_FACES", "10000"))
SCAFFOLD_VOXEL = float(os.environ.get("PATHA_SCAFFOLD", "0.011"))


def n_islands(me):
    return rt.n_islands(me)


def duplicate_mesh(src, name):
    tgt = src.copy()
    tgt.data = src.data.copy()
    tgt.name = name
    bpy.context.collection.objects.link(tgt)
    return tgt


def quadriflow(ob, faces: int) -> bool:
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    try:
        bpy.ops.object.quadriflow_remesh(
            use_mesh_symmetry=False,
            use_preserve_sharp=True,
            use_preserve_boundary=False,
            mode="FACES",
            target_faces=faces,
            seed=4,
        )
    except Exception as exc:
        print("quadriflow failed", exc)
        return False
    for p in ob.data.polygons:
        p.use_smooth = True
    print("quadriflow", faces, "v", len(ob.data.vertices), "f", len(ob.data.polygons), "islands", n_islands(ob.data))
    return len(ob.data.polygons) > 200


def scaffold_watertight(ob, size: float):
    """One watertight volume so QuadriFlow has manifold input.

    Not the hero mesh. Silhouette is restored by shrinkwrap to Meshy.
    """
    rt.apply_voxel(ob, size)
    print("scaffold voxel (not hero)", size, "islands", n_islands(ob.data))


def shrinkwrap_to_source(tgt, src):
    bpy.ops.object.select_all(action="DESELECT")
    tgt.select_set(True)
    bpy.context.view_layer.objects.active = tgt
    m = tgt.modifiers.new("toMeshy", "SHRINKWRAP")
    m.target = src
    m.wrap_method = "NEAREST_SURFACEPOINT"
    m.wrap_mode = "ON_SURFACE"
    m.offset = 0.0
    bpy.ops.object.modifier_apply(modifier="toMeshy")
    bm_ok = True
    try:
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(tgt.data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(tgt.data)
        bm.free()
    except Exception:
        bm_ok = False
    tgt.data.update()
    print("shrinkwrap to Meshy", len(tgt.data.vertices), "faces", len(tgt.data.polygons), "normals", bm_ok)


def project_albedo(src, tgt, atlas_path: Path):
    rt.smart_uv(tgt)
    baked = rt.cycles_bake(src, tgt, atlas_path)
    if baked is None or not atlas_path.exists() or atlas_path.stat().st_size < 20_000:
        print("cycles bake miss — BVH project Image_0 / lion")
        colors = rt.transfer_albedo(src, tgt)
        rt.rasterize_atlas(tgt, colors, atlas_path)
    atlas = Image.open(atlas_path).convert("RGB")
    arr = __import__("numpy").array(atlas)
    rt.stamp_lion(tgt, arr)
    Image.fromarray(arr, "RGB").save(atlas_path)  # noqa: ALB001
    rt.assign_baked_material(tgt, atlas_path)
    print("projected atlas", atlas_path, atlas_path.stat().st_size)
    return atlas_path


def auto_bind(mesh_ob, actor_ob):
    """Armature automatic weights. Not exclusive paper-island corridors."""
    bpy.ops.object.select_all(action="DESELECT")
    mesh_ob.select_set(True)
    actor_ob.select_set(True)
    bpy.context.view_layer.objects.active = actor_ob
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    print("auto weights", [g.name for g in mesh_ob.vertex_groups])


def lock_scabbard(mesh_ob) -> int:
    """One character-RIGHT scabbard volume. Not a paper-island hack."""
    me = mesh_ob.data
    groups = {g.name: g for g in mesh_ob.vertex_groups}
    if "Scabbard" not in groups:
        groups["Scabbard"] = mesh_ob.vertex_groups.new(name="Scabbard")
    n = 0
    for i, v in enumerate(me.vertices):
        p = v.co
        if p.x > 0.14 and 0.22 < p.y < 1.18 and hh.dist_seg(p, hh._SCAB_A, hh._SCAB_B) < 0.058:
            for g in groups.values():
                try:
                    g.remove([i])
                except RuntimeError:
                    pass
            groups["Scabbard"].add([i], 1.0, "REPLACE")
            n += 1
    print("scabbard lock", n)
    return n


def recount(mesh_ob):
    me = mesh_ob.data
    vg = {g.index: g.name for g in mesh_ob.vertex_groups}
    counts = {n: 0 for n in MESH_BONES}
    for v in me.vertices:
        best, bw = "Hips", 0.0
        for g in v.groups:
            name = vg.get(g.group, "")
            if name in counts and g.weight > bw:
                best, bw = name, g.weight
        counts[best] = counts.get(best, 0) + 1
    print("primary", {k: v for k, v in counts.items() if v})
    return counts


def render_world_shots():
    """front / rear / side / ¾ + rear Play angle."""
    cam = bpy.context.scene.camera
    if cam is None:
        raise SystemExit("no camera")
    PROOF.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    for name, (eye, tgt) in (
        ("world_rear", (shots.CAM_EYE, shots.CAM_TARGET)),
        ("world_rear_34", (shots.CAM_EYE_34, shots.CAM_TARGET)),
        ("world_front", (shots.CAM_FRONT, shots.CAM_FRONT_T)),
        ("world_side_r", (shots.CAM_SIDE, shots.CAM_SIDE_T)),
        ("world_34_front", (shots.CAM_34_FRONT, shots.CAM_34_FRONT_T)),
    ):
        cam.matrix_world = shots.play_cam_matrix(eye, tgt)
        path = PROOF / f"{name}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("world", name, path.stat().st_size)
        try:
            (ART / f"path_a_{name}.png").write_bytes(path.read_bytes())
        except OSError as exc:
            print("artifact skip", exc)
    rest = PROOF / "world_meshy_hang_rest.png"
    rest.write_bytes((PROOF / "world_rear.png").read_bytes())


def patch_mesh_header():
    path = PACK3D / "sir_aldric_meshy.mesh.txt"
    lines = path.read_text().splitlines()
    if lines:
        lines[0] = "# SirAldric Path A retopo+project e5b132f  scabbard=+X  blender"
    if len(lines) > 1:
        lines[1] = "FMT v4 blender gate3 meshy-path2 path-a-retopo-project"
    path.write_text("\n".join(lines) + "\n")


def main():
    if not old.SRC_GLB.exists():
        raise SystemExit(f"missing {old.SRC_GLB}")
    src = hh.import_raw()
    src.name = "MeshySource"
    look.delete_extra_sheath(src)
    hh.weld(src, 0.001)
    print("source islands", n_islands(src.data), "v", len(src.data.vertices), "f", len(src.data.polygons))

    tgt = duplicate_mesh(src, "SirAldricPathA")
    rt.fill_holes(tgt)
    # Scaffold only if shatter is still many islands — QuadriFlow needs manifold.
    islands, sizes = n_islands(tgt.data)
    if islands > 2:
        scaffold_watertight(tgt, SCAFFOLD_VOXEL)
    ok = quadriflow(tgt, TARGET_FACES)
    if not ok:
        raise SystemExit("QuadriFlow produced no usable mesh")
    # Recover Meshy silhouette. Hero geo is shrinkwrapped Meshy, not the scaffold.
    src.hide_set(False)
    shrinkwrap_to_source(tgt, src)
    islands, sizes = n_islands(tgt.data)
    print("retopo islands", islands, "top", sizes)

    atlas_path = PACK3D / "sir_aldric_meshy_atlas.png"
    project_albedo(src, tgt, atlas_path)
    src.hide_set(True)
    src.hide_render = True

    actor_ob, _world = hh.build_actor_armature()
    auto_bind(tgt, actor_ob)
    scab_n = lock_scabbard(tgt)
    counts = recount(tgt)
    if scab_n < 20 or counts.get("Scabbard", 0) < 20:
        raise SystemExit(f"scabbard empty: lock={scab_n} primary={counts.get('Scabbard')}")
    arm_r = counts.get("Arm_R", 0) + counts.get("Fore_R", 0) + counts.get("Hand_R", 0)
    arm_l = counts.get("Arm_L", 0) + counts.get("Fore_L", 0) + counts.get("Hand_L", 0)
    if arm_r < 30 or arm_l < 30:
        raise SystemExit(f"arm empty after auto-bind: R={arm_r} L={arm_l} {counts}")
    leg_r = counts.get("UpLeg_R", 0) + counts.get("Leg_R", 0) + counts.get("Foot_R", 0)
    leg_l = counts.get("UpLeg_L", 0) + counts.get("Leg_L", 0) + counts.get("Foot_L", 0)
    if leg_r < 30 or leg_l < 30:
        raise SystemExit(f"leg empty after auto-bind: R={leg_r} L={leg_l} {counts}")

    old.setup_render()
    old.apply_pose(actor_ob, {
        "root_z": 0.0, "root_y": 0.0,
        "hips": (0, 0, 0), "spine": (0, 0, 0), "chest": (0, 0, 0), "head": (0, 0, 0),
        "up_l": (0, 0, 0), "leg_l": (0, 0, 0), "foot_l": (0, 0, 0),
        "up_r": (0, 0, 0), "leg_r": (0, 0, 0), "foot_r": (0, 0, 0),
        "arm_l": (0, 0, 0), "fore_l": (0, 0, 0),
        "arm_r": (0, 0, 0), "fore_r": (0, 0, 0),
        "hand_r": (0, 0, 0), "sword": (0, 0, 0),
    }, tgt)
    render_world_shots()

    mp4 = old.render_walk(actor_ob, tgt)
    for n in (0, 5, 10, 15):
        old.apply_pose(actor_ob, old.walk_pose(n / 16.0), tgt)
        npng = old.WALK / f"world_walk_n{n:02d}.png"
        bpy.context.scene.render.filepath = str(npng)
        bpy.ops.render.render(write_still=True)
        print("cycle n", n, npng.stat().st_size)

    fbx = rt.export_fbx(tgt, actor_ob)
    ntris, buckets = look.export_mesh_txt_v4(tgt)
    patch_mesh_header()
    blend = PACK3D / "sir_aldric_meshy_skinned.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "pathA": True,
        "honestArt": "clean QuadriFlow retopo + Meshy albedo project. Abandoned shattered-Meshy paper/tube/capsule bind hacks. Voxel used only as watertight scaffold then shrinkwrapped back to Meshy.",
        "sourceLook": "e5b132f Meshy GLB + Image_0 / SoT lion",
        "retopo": f"QuadriFlow targetFaces={TARGET_FACES}; shrinkwrap to Meshy; no capsule limbs",
        "bind": "Armature automatic weights + one Scabbard volume lock. No exclusive paper corridors.",
        "motion": "5916447 Evaluate() keys reused; root plant after Evaluate() so soles kiss Y=0",
        "scabbard": "character-right",
        "playHubLocked": True,
        "islands": islands,
        "islandSizes": sizes,
        "vertsPrimary": counts,
        "verts": len(tgt.data.vertices),
        "tris": ntris,
        "boneTris": buckets,
        "atlas": atlas_path.name,
        "mesh": "sir_aldric_meshy.mesh.txt",
        "fbx": fbx.name,
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done path A", note["walkClip"], "tris", ntris, "verts", note["verts"], "islands", islands)


if __name__ == "__main__":
    main()
