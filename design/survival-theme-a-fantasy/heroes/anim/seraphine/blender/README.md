# Seraphine Blender file

One file: `SERAPHINE.blend` (Blender 4.2.3). It holds the rig, body, props, cape rig and cape and every animation clip as an Action (fake user + muted NLA track). No action assigned = rest pose.

Actions (30 fps): idle (1-299), walk (1-36), attack_thrust (1-72), attack_throw (1-67).

**Thrust export:** the hidden object `SERAPHINE_spear__attack_thrust` is the spear used by attack_thrust. Unhide it and export it in place of `SERAPHINE_spear` when exporting the thrust.

Built from the game-ready exports in ../final/ (unchanged). Cape spring sim is not baked. Full object list, sources and import settings: `README_SERAPHINE_blend.md`.

Unity cape collider specs: ../final/CAPE_ATTACH.md.
