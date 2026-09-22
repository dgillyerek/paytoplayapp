#!/usr/bin/env python3
"""Image_0 bake onto the saved Path A mid-poly. Stills-first. No new tubes."""
from __future__ import annotations

import json
import os
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
    walk_only = os.environ.get("PATHA_WALK_ONLY") == "1"
    bind_only = os.environ.get("PATHA_BIND_ONLY") == "1"
    stills_only = os.environ.get("PATHA_STILLS_ONLY") == "1" or os.environ.get("SKIN_STILLS_ONLY") == "1"
    src = path_a.ensure_meshy_source(src)
    # Bind-only / walk-only: do NOT peel, shell, or re-atlas the locked
    # 552b099 stills mesh. Stills look stays locked.
    if not walk_only and not bind_only:
        src.hide_set(False)
        src.hide_render = True
        # 8436248 Meshy-face copy shredded (holes / plate poke). Peel
        # remaining inner, then one continuous cloth shell above the plate
        # inside the fitted e5 silhouette. Not a planar card.
        if os.environ.get("PATHA_PEEL", "1") == "1":
            path_a.peel_front_torso_inner(tgt, src, wrap=False)
        if os.environ.get("PATHA_CLOTH_PATCH", "1") == "1":
            path_a.replace_front_cloth_shell(tgt, src)
        # Stills-first: do not full-body re-wrap (rear/side already close).
        if not stills_only:
            path_a.shrinkwrap_to_source(tgt, src, 0.001)
        path_a.project_albedo(src, tgt, atlas_path)
        src.hide_set(True)
        src.hide_render = True
    elif src is not None:
        src.hide_set(True)
        src.hide_render = True

    # Scene already has WorldPlay / lights / ground from the saved blend.
    if bpy.context.scene.camera is None:
        old.setup_render()
    old.apply_pose(actor, rest, tgt)

    counts = None
    if bind_only or (not stills_only and not walk_only):
        counts = path_a.rebind_locked(tgt, actor)
        old.apply_pose(actor, rest, tgt)
        path_a.assert_pose_not_shred(tgt, "rest")

    skip_stills = os.environ.get("PATHA_SKIP_STILLS") == "1"
    if not walk_only and not skip_stills:
        path_a.render_world_shots()

    if stills_only and not bind_only:
        # Front lion strip remaps dest UVs — ThemePack mesh must match the atlas.
        rt.export_fbx(tgt, actor)
        ntris, buckets = look.export_mesh_txt_v4(tgt)
        path_a.patch_mesh_header()
        bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
        counts = path_a.recount(tgt)
        islands, sizes = path_a.n_islands(tgt.data)
        note = {
            "lookPassClaimed": False,
            "walkPassClaimed": False,
            "bindPassClaimed": False,
            "pathA": True,
            "stillsIterate": "8436248-opaque-cloth-shell",
            "honestArt": (
                "Continuous opaque front tabard shell: fitted e5b132f "
                "silhouette, snapped to Meshy outer hit, offset above plate "
                "so it does not intersect. Solid SoT navy + larger lion. "
                "Not a planar card. Walk as-is. Do NOT claim PASS."
            ),
            "sourceLook": "e5b132f Meshy GLB + Image_0 / SoT lion",
            "atlas": atlas_path.name,
            "tris": ntris,
            "verts": len(tgt.data.vertices),
            "islands": islands,
            "islandSizes": sizes,
            "vertsPrimary": counts,
            "boneTris": buckets,
            "stillsOnly": True,
        }
        path_a.write_drop(note)
        print("stills-only — walk clip left as-is; mesh", ntris)
        return

    path_a.purge_shred_walk_previews()
    path_a.set_play_cam()
    for n in (0, 4, 8, 12):
        old.apply_pose(actor, old.walk_pose(n / 16.0), tgt)
        path_a.assert_pose_not_shred(tgt, f"walk_n{n:02d}")
    old.apply_pose(actor, rest, tgt)
    path_a.set_play_cam()
    mp4 = old.render_walk(actor, tgt)
    for n in (0, 5, 10, 15):
        old.apply_pose(actor, old.walk_pose(n / 16.0), tgt)
        npng = old.WALK / f"world_walk_n{n:02d}.png"
        bpy.context.scene.render.filepath = str(npng)
        bpy.ops.render.render(write_still=True)
        print("cycle n", n, npng.stat().st_size)

    old.apply_pose(actor, rest, tgt)
    fbx = rt.export_fbx(tgt, actor)
    ntris, buckets = look.export_mesh_txt_v4(tgt)
    path_a.patch_mesh_header()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))

    if counts is None:
        counts = path_a.recount(tgt)
    else:
        counts = path_a.recount(tgt)
    islands, sizes = path_a.n_islands(tgt.data)
    note = {
        "lookPassClaimed": False,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "pathA": True,
        "stillsIterate": "552b099-opaque-cloth-shell",
        "bindIterate": "spatial-hang-segs+tabard-torso+scabbard",
        "honestArt": (
            "552b099 stills look LOCKED. Bind-only on that retopo: spatial "
            "nearest hang-bone-segment weights, front-tabard torso lock, one "
            "Scabbard volume lock. No peel/shell/atlas. No Path-2 ghost/tear/"
            "tube/capsule remesh. Planted Evaluate() walk toward TOP for "
            "Design re-gate. Do NOT claim Design / bind / walk PASS."
        ),
        "sourceLook": "e5b132f Meshy GLB + Image_0 / SoT lion",
        "retopo": "QuadriFlow mid-poly + shrinkwrap ABOVE_SURFACE; no capsule limbs",
        "bind": (
            "spatial nearest hang-bone-segment (≤4) + front-tabard torso lock "
            "+ one Scabbard volume lock. Not automatic weights."
        ),
        "motion": "5916447 Evaluate() keys reused; root plant after Evaluate() so soles kiss Y=0",
        "scabbard": "character-right",
        "stillsOnly": False,
        "bindOnly": bind_only,
        "walkFramesClean": True,
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
