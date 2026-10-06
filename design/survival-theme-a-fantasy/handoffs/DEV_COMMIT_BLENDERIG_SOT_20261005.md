# Design → Dev: Commit blenderig SoT into GitHub solution (Derek 2026-10-05)

Derek: these live on Design box only today — put them in `paytoplayapp` so they are in the solution on GitHub (not just tip-swap FBXs into demos).

## Include (deliverables only — ~563 MB)

For each of 9 characters, from Design box paths under `/workspace/design/survival-theme-a-fantasy/`:

| Character | Folder | Files |
|-----------|--------|-------|
| Rowan | `heroes/anim/rowan/blender_rig/` | `ROWAN_blenderig.blend`, `.fbx`, `_walk.fbx`, `_sheet.jpg`, `PASS.md` |
| Lyra | `heroes/anim/lyra/blender_rig/` | same pattern |
| Stormcrest | `heroes/anim/stormcrest/blender_rig/` | same |
| Oakenshield | `heroes/anim/oakenshield/blender_rig/` | same |
| Emberfang | `heroes/anim/emberfang/blender_rig/` | `EMBERFANG_dragonrig.blend`, `.fbx`, `_wingflap.fbx`, `_sheet.jpg` (+ PASS if present) |
| Vespera | `enemies/anim/vespera/blender_rig/` | blenderig + walk |
| Bonequill | `enemies/anim/bonequill/blender_rig/` | blenderig + walk |
| Nightfang | `enemies/anim/nightfang/blender_rig/` | blenderig + trot |
| Ashwyrm | `enemies/anim/ashwyrm/blender_rig/` | blenderig + wingflap (**post wing-weight fix**) |

Also commit Design pipeline scripts (tiny): `/workspace/design/_shared_rig_pipeline/` (`creature_pipeline.py`, `hum_pipeline.py`, `hum_analyze.py`, `run_humanoid.sh`).

## Exclude
- `work/` scratch blends
- `broken_clips_backup/`
- `*.blend1` autosaves
- preview MP4s under `handoffs/anim_previews_*` (optional; skip unless you want them)

## Suggested repo layout
Mirror Design relative path under Assets, e.g.:
`Assets/Design/ThemeA/.../heroes/anim/<name>/blender_rig/`
(or your existing Design SoT folder if one already exists — use that)

Use Git LFS for `.blend` / `.fbx` if the repo already uses it for character assets.

## Do NOT
- Re-rig / re-weight
- Merge to main until Derek Game-view PASS on demos (PR #31 paint / #32 blenderig)

## Done when
All listed blend+fbx+sheet+PASS (+ pipeline scripts) are on a tip Derek can pull, and you reply with PR + tip SHA + on-disk paths in the solution.
