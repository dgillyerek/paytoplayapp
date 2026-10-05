# Blenderig SoT → paytoplayapp layout

Mirror into existing Design SoT root (Aldric already lives here):

- `design/survival-theme-a-fantasy/heroes/anim/<name>/blender_rig/`
- `design/survival-theme-a-fantasy/enemies/anim/<name>/blender_rig/`
- `design/survival-theme-a-fantasy/enemies/anim/ironhowl/mixamo/IRONHOWL_rig.fbx` (+ sheet)
- `design/_shared_rig_pipeline/` (creature_pipeline.py, hum_pipeline.py, hum_analyze.py, run_humanoid.sh)

Do NOT put these under Assets/ (Design SoT lives at repo-root `design/` like Sir Aldric).
Do NOT touch ThemePack runtime demo FBXs on PR #31/#32.
Do NOT merge until Derek Game-view PASS.
Use Git LFS for .blend/.fbx if the repo already tracks them that way; otherwise commit as normal binary matching how existing design/*.fbx are stored.
