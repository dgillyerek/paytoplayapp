# Malzareth Blender file

One file: `MALZARETH.blend` (Blender 4.2.3). It holds the rig, body, props, cape rig and cape and every animation clip as an Action (fake user + muted NLA track). No action assigned = rest pose.

Actions (30 fps): idle (1-299), walk (1-36), attack_greatsword (1-56), attack2_overhead_cleave (1-69). Last saved with Blender 5.2.2 (FIX2, 2026-11-04).

Built from the game-ready exports in ../final/ (unchanged). Cape spring sim is not baked. Full object list, sources and import settings: `README_MALZARETH_blend.md`.

Unity cape collider specs: ../final/CAPE_ATTACH.md.

FIX2 (2026-11-04): snugger cape, knee-fixed attack_greatsword, new one-handed attack2_overhead_cleave. Details in README_MALZARETH_blend.md.
