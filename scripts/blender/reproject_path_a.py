#!/usr/bin/env python3
"""Re-project e5b132f stills onto the saved Path A mid-poly. No retopo, no new geo."""
from __future__ import annotations

import json
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import skin_sir_aldric_meshy as old
import skin_sir_aldric_meshylook as look
import skin_sir_aldric_path_a as path_a
import skin_sir_aldric_retopo as rt

BLEND = old.PACK3D / "sir_aldric_meshy_skinned.blend"


def main():
    if not BLEND.exists():
        raise SystemExit(f"missing {BLEND}")
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    tgt = bpy.data.objects.get("SirAldricPathA")
    actor = bpy.data.objects.get("AldricArm")
    src = bpy.data.objects.get("MeshySource")
    if tgt is None or actor is None:
        raise SystemExit(f"missing objects tgt={tgt} actor={actor}")
    print("reproject", tgt.name, "v", len(tgt.data.vertices), "f", len(tgt.data.polygons))

    # Rest pose so Play-cam project hits the same silhouette as hang stills.
    rest = {
        "root_z": 0.0, "root_y": 0.0,
        "hips": (0, 0, 0), "spine": (0, 0, 0), "chest": (0, 0, 0), "head": (0, 0, 0),
        "up_l": (0, 0, 0), "leg_l": (0, 0, 0), "foot_l": (0, 0, 0),
        "up_r": (0, 0, 0), "leg_r": (0, 0, 0), "foot_r": (0, 0, 0),
        "arm_l": (0, 0, 0), "fore_l": (0, 0, 0),
        "arm_r": (0, 0, 0), "fore_r": (0, 0, 0),
        "hand_r": (0, 0, 0), "sword": (0, 0, 0),
    }
    actor.location = (0.0, 0.0, 0.0)
    for pb in actor.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
    bpy.context.view_layer.update()

    atlas_path = old.PACK3D / "sir_aldric_meshy_atlas.png"
    if src is not None:
        src.hide_set(False)
        src.hide_render = True
    path_a.project_albedo(src or tgt, tgt, atlas_path)
    if src is not None:
        src.hide_set(True)
        src.hide_render = True

    # Scene already has WorldPlay / lights / ground from the saved blend.
    if bpy.context.scene.camera is None:
        old.setup_render()
    old.apply_pose(actor, rest, tgt)
    path_a.render_world_shots()

    mp4 = old.render_walk(actor, tgt)
    for n in (0, 5, 10, 15):
        old.apply_pose(actor, old.walk_pose(n / 16.0), tgt)
        npng = old.WALK / f"world_walk_n{n:02d}.png"
        bpy.context.scene.render.filepath = str(npng)
        bpy.ops.render.render(write_still=True)
        print("cycle n", n, npng.stat().st_size)

    fbx = rt.export_fbx(tgt, actor)
    ntris, buckets = look.export_mesh_txt_v4(tgt)
    path_a.patch_mesh_header()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))

    counts = path_a.recount(tgt)
    islands, sizes = path_a.n_islands(tgt.data)
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "pathA": True,
        "honestArt": (
            "clean mid-poly retopo + camera-project of frozen e5b132f World stills "
            "onto retopo UVs (structured steel/navy fill for occluded verts). "
            "Abandoned shattered-Meshy paper/tube/capsule bind hacks. "
            "Voxel used only as watertight scaffold; silhouette restored by "
            "shrinkwrap ABOVE_SURFACE onto Meshy. Spatial nearest-bone-segment "
            "weights + one Scabbard volume lock. No exclusive paper corridors."
        ),
        "sourceLook": "e5b132f Meshy GLB + Image_0 / SoT lion",
        "retopo": "QuadriFlow mid-poly + shrinkwrap ABOVE_SURFACE; no capsule limbs",
        "bind": "spatial nearest rest-bone-segment (≤4) + one Scabbard volume lock. Not automatic weights.",
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
        "walkClip": str(mp4.relative_to(old.ROOT)),
        "reproject": True,
    }
    (old.PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    path_a.write_drop(note)
    print("done reproject", ntris, "verts", note["verts"])


if __name__ == "__main__":
    main()
