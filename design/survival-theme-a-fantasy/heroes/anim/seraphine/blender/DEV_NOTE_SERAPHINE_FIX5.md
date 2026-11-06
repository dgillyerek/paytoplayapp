# Seraphine FIX5 — fresh thrust and throw

Branch `cursor/seraphine-fix5-attacks-d044`. Base `main` `02b3660e`. Not a merge. Desktop Unity was not run.

Derek on FIX4: both attacks are completely unnatural. FIX4 slowed the old arcs (thrust 4.733 s / 143 f, throw 4.400 s / 133 f) and kept the feet nearly still. That premise — small torso and limb offsets, played slowly, will read as a person — is false. These clips replace the attack keyframes. They do not patch them.

## Rebuilt

Only `attack_thrust` and `attack_throw`.

- Thrust, 40 frames, **1.30 s**. Hit at frame 20 (the preview's contact is at half the clip).
  Rear-foot load (hips drop 12 cm, right knee to 95°), left lead step of **31.7 cm** with the ankle rising to 14.3 cm off the plant, right heel rise of **7.0 cm** and a pivot, then the hips unwind before the arms extend. The hand's forward speed peaks at frame 20; the hip-yaw speed peaks at frame 17. Recoil is frames 20–23. Both feet are back on the idle marks at frame 40.
  The staff drops from the idle vertical (82°) to **37°** above horizontal at the hit and is driven along that line. It is not leveled flat: a flat line puts the 0.80 m trailing butt through the wings.
- Throw, 48 frames, **1.57 s**. Release at frame 24.
  Deep left step of **37.7 cm** (ankle lift 14.5 cm), staff cocked **up over the right shoulder** (about 70° above horizontal at frame 14–18, not horizontal and not over the crown), hips whip through while the staff is still cocked (hip-yaw speed peaks at frame 18, hand speed at frame 24), release at **39.5°** above horizontal, follow-through, then the right foot steps through **20.5 cm** and both feet return to idle.
- Right wrist vs forearm stays near the idle bend on the thrust (25–43°; idle is 26°). On the throw it peaks at **71°** at frame 22 while the forearm is still catching the whip, then comes back through 56° at the release.

## Inherited, unchanged

- 24-bone skeleton, bind matrices, body weights, body vertices. Clean FBX reimport of each new clip against `SERAPHINE_rig_rest.fbx`: bind max diff **0**, sampled rest-vertex diff **0**.
- Idle and walk actions and their FBX files. Frame 1 and the last frame of each attack are the idle pose.
- `SERAPHINE_cape.fbx` byte-identical to main (sha256 `ca3f388fe8f80a9e96f3342d75dccc5f73d1493d50b8c8ad4fc752b320024869`): 10,977 verts, 11,773 faces, 0 removed.
- The one staff mesh in `rig_rest` (ornament 1.298 m past the right hand, butt 0.802 m). No per-clip staff, no mesh cuts, no Unity C# edits.
- Hips X/Y stay on the idle mark. The preview pins hip XZ anyway; the steps are the feet moving under that pelvis.

## Measured on the reimported clip driving rig_rest

| | thrust | throw |
|---|---|---|
| length | 1.30 s (f1–40) | 1.57 s (f1–48) |
| left step (ankle Y) | 0.017 → −0.300 m | 0.017 → −0.360 m |
| left ankle height | 0.156 → 0.299 m | 0.156 → 0.301 m |
| right ankle Y | held at 0.005, then idle | 0.005 → −0.200 m on the recovery step |
| right ankle height | 0.178 → 0.248 m (heel) | up to 0.330 m while the recovery foot swings |
| knee interior L / R | 81.7–153.4° / 94.6–157.4° | 87.3–153.4° / 70.7–157.4° |
| hip height | 0.936 → 0.816 m | 0.936 → 0.793 m |
| staff elevation | 86.9° → 37.4° | 89.2° → 39.5° (cock stays ~70°) |
| planted-foot XY drift | 2.4 cm on the left foot at the f15 plant boundary only | 2.1–2.9 cm on the left foot at liftoff/plant boundaries (f8, f14, f30, f37) |

Staff vs body outside a 14 cm grip ball around the right hand, measured in the authoring file on the same poses: minimum **7.66 cm** (the idle shaft-to-thigh gap). Attacks were not closer. Wings ≥ 5.0 cm (throw f24), braid/hair ≥ 20 cm, cape ≥ 23 cm. Those are distances, not a claim that the motion is finished because nothing intersects.

## Proof

Constant orthographic scale **3.35** for every full-body frame. Front three-quarter and strict side.

- `design/survival-theme-a-fantasy/new_2026-11-03/seraphine/sheets/fix5/SERAPHINE_FIX5_thrust_SHEET.png`
- `design/survival-theme-a-fantasy/new_2026-11-03/seraphine/sheets/fix5/SERAPHINE_FIX5_throw_SHEET.png`
- `.../SERAPHINE_FIX5_thrust_front_side.mp4` (1.33 s, 30 fps, both views)
- `.../SERAPHINE_FIX5_throw_front_side.mp4` (1.60 s, 30 fps, both views)
- `.../SERAPHINE_FIX5_thrust_hands_quarter.mp4`
- `.../SERAPHINE_FIX5_throw_hands_quarter.mp4`
- Reimport numbers: `sheets/fix5/reimport_measures.json`

Rebuild: `blender -b SERAPHINE.blend --python blender/fix5/fix5_author.py`
Sheets from the exported FBX: `blender -b --python blender/fix5/fix5_render.py -- --mode all`

## Limitations

- Fingers have no bones. Both hands stay the open idle shape. The right hand's grip is the skinned staff. The left hand on the thrust sits beside the shaft; it is not a closed fist.
- The cape in the proof is the original mesh child-of Spine2. It is not a new spring simulation, so the cloth does not react and can visually stack on the wings in the side view.
- Throw wrist peaks at 71° (hand bone vs forearm) at frame 22. It is not clamped; the forearm has not caught the staff aim yet.
- Throw right knee reaches 71° interior while that foot is in the air on the recovery step.
- No in-engine Unity playback, crossfade, or cloth solve was run.
