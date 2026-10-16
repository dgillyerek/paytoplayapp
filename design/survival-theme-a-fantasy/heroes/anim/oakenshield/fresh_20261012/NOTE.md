# Oakenshield fresh rebuild (2026-10-12) — HOLD

Derek: "start over with a fresh body image where everything faces forward and rerig. reimplement the same ideas"; approved the halfway look (`source/OAKENSHIELD_FRONT_BACK_LOCKED.jpg`).

- **Model:** Design's Meshy image-to-3D (`source/MESHY_SOURCE.md5`; GLB 1.88M tris, not committed). Merged duplicate verts, decimated (collapse) to 199,999 faces / 99,788 verts, scaled to 1.80 m, feet on 0, centred. Meshy UVs and maps kept: base colour, normal, metallic-roughness (2048²).
- **Rig (Blender 5.2.2, `work/rig.py`):** 20 mixamorig bones (no ToeBase), placed from mesh cross-sections. Max 4 influences.
  - Face/skull/crown: 100% `Head` above a tilted plane (chin 1.44 m front → skull base 1.49 m back), blend band ±1.2 cm only.
  - Leaf skirt (everything off the leg cores below the belt): 100% `Hips`, zero leg weight.
  - 127 protruding pieces (shoulder/collar leaves, crown spikes, bracer and vine bits), found by a 5 mm voxel core test, each 100% on one bone with a 2.5 cm root ring.
  - Thighs ramp Hips→UpLeg over 12 cm down the leg (no crease); knee/elbow/wrist blends ±3–5 cm.
- **Actions (`work/anim.py`):** rest = bind pose, arms lowered 20°, body square to camera. Walk = in place, 1 s, 30 fps, ankles 19.0–19.5 cm apart, head forward, plus (2026-10-13, Derek) a subtle Head-bone nod 0–2.5° forward twice per cycle, peaking at the footfalls (`work/bob.py`); rest and attack FBXs unchanged. Attack = #48 thorn spear beats (conjure f3–8, cock f10, throw f14 at 11 m/s, follow f20, rest f30), feet planted, head forward, RightHand held at its f10 rotation f8–14 so the spear stays pointing forward.
- **QC (`work/sheet.json`):** face/skull deviation from the rigid Head transform = 0.000 cm in every frame of rest/walk/attack. Neck–head band max 0.4 cm (walk) / 2.6 cm (attack, neck twist).
- Sheets: `OAKENSHIELD_fresh_{rest,walk,attack}_sheet.jpg` (front + face close-up; attack also side).
- Unity: ThemePack `OAKENSHIELD_blenderig*.fbx`, Generic (`animationType: 2`), Survival → Oakenshield Demo.
- **2026-10-15 side-step attack (`work/sidestep.py`, Derek on #65):** body turns side-on to her right (hips -62° at f10), LEFT foot steps 32 cm forward f2–8 and takes the weight, hips whip back square for the f14 release, left foot steps back f21–28. Back foot pivots on its ball. Two-bone leg IK, planted-foot drift < 4 mm. Head kept at rest orientation (face locked), weights untouched (leaf pieces rigid). f0/f30 equal the old take. Only `OAKENSHIELD_blenderig_attack.fbx` changed (export settings as `work/export.py`).
