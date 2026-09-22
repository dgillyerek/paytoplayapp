#!/usr/bin/env python3
"""Write ThemePack mesh/FBX from the saved Path A blend (new Image_0 UVs). No walk."""
from __future__ import annotations

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
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    tgt = bpy.data.objects.get("SirAldricPathA")
    actor = bpy.data.objects.get("AldricArm")
    if tgt is None or actor is None:
        raise SystemExit("missing Path A objects")
    rt.export_fbx(tgt, actor)
    ntris, _buckets = look.export_mesh_txt_v4(tgt)
    path_a.patch_mesh_header()
    print("exported mesh", ntris, "verts", len(tgt.data.vertices))


if __name__ == "__main__":
    main()
