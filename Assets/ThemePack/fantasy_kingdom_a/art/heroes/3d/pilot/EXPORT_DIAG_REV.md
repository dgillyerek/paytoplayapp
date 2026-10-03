# Sir Aldric — DIAG time-reversed slash export

**Status:** DIAG_REV staged; spatial Mirror NOT used.
**Date:** 2026-09-30 ~14:14 ET
**SoT:** https://www.youtube.com/watch?v=iQ1s3nN1330
**Clarification tip:** 43b33fb — Derek wants end-of-loop as initial movement: raise above head first, then strike down and left. Whole-clip **playback reverse**, not Mixamo Mirror / L-R flip.

## Source
- Base: `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx`
- Base MD5: `72412be4405bea372184dd5f5e17e186`
- DIAG bake (unchanged source): Stable Sword Inward Slash — Emphasis 50, Angle 80, Overdrive 30, Arm-Space 50, Mirror **off**

## Reverse method
- Tool: Blender 4.3.2 headless
- Operation: reverse all fcurve keyframe times within action frame range (1..86) for `Armature|mixamo.com|Layer0`; swap handle sides; bake-export FBX
- **Not** Mixamo Mirror; **not** Unity timescale/mirror hack
- Derek Game-view GIF reference saved: `derek_gif_43b33fb/derek_diag_gameview.gif`

## Staged artifact
- File: `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG_REV.fbx`
- MD5: `0beb3c77a1f53c1604dd3c725b1ad789`
- Soft: Blender re-export (With Skin armature+mesh); may differ slightly from raw Mixamo binary packaging — Game-view is hard gate. Walk FBX untouched.
