# Rowan remesh compare (Derek Blender/Meshy-path)

- **Purpose:** Remesh replay of Design LOOK_SOT pack for side-by-side vs tip `ROWAN_blenderig`.
- **Source GLB:** Same as original blenderig — `LOOK_SOT/mesh_source/Rowan_fullbody.glb` (~1.64M → target 200k faces).
- **Look QC:** Guided by `SoT_fullbody_LOCKED.png` (passed via `--look`). Portrait also copied for reference.
- **No rear turnaround invented:** Rowan never locked a multi-view / rear SoT (unlike Aldric `TURNAROUND_GATE1`). Do not invent `SoT_rear_LOCKED.png`.
- **Name:** `ROWAN_remesh` so outputs do not overwrite repo blenderig (`ROWAN_blenderig.*`).
- **Pipeline:** `hum_pipeline.py` stages prep → build → pose → export → verify (Blender 4.3.2).
- **Side-by-side in this folder:** locked SoTs, `current_blenderig_sheet.jpg`, `PASS_blenderig.md`, plus remesh outputs.
