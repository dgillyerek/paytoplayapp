# Sir Aldric facing SoT (2026-09-25)

Derek ask: walk **and** attack must show the knight **BACK** to camera, walking / striking toward **TOP of screen** (World north). Frontal walk is FAIL.

**No Design PASS claimed.** Path A weight-paint cancelled. Dev wires only.

## Verified math (not guessed)

| Source | What it actually is |
| --- | --- |
| `SirAldricDemo` Play cam | Eye `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30. Camera sits on −Z and looks **+Z**. |
| `SirAldric3DMotion` | Character forward / march = world **+Z** = screen TOP. Enemy / chevrons sit at +Z. |
| `WorldMarchLoop` | Node timer (march seconds → resolve). **Not** a heading. |
| Mixamo Humanoid import of `sir_aldric_meshy_animate_walk.fbx` | Identity instantiate faces **−Z** (face to Play cam) = current frontal FAIL. |
| Actor `RearYawDegrees` | `SirAldric3DMotion.MixamoImportRearYawDegrees` = **180**. `Quaternion.Euler(0, 180, 0)` so `transform.forward = +Z`. Back to camera. Walk root along character forward = toward TOP. |

Unity Y-up. After yaw 180: character-RIGHT = world +X = viewer-right from behind.

## Actor wire

`SirAldricMeshyAnimateActor`:

1. Instantiate walk FBX → `FaceWorldTop` (yaw 180) → `#26` atlas-before-punch → `#25` Walking take-name repair if needed.
2. `AnimationMixerPlayable` input 0 = Walking (loop). Input 1 = Design Attack take `target_character|rigify_clip|BaseLayer`.
3. After `WalkCyclesBeforeAttack` walk cycles, 0.12s crossfade Walking → Attack → back. Same Humanoid, same rear yaw.

## Proof (PRIMARY = rear / back-to-camera)

| File | What Derek should see |
| --- | --- |
| `sir_aldric_rear_still_back_to_camera.png` | Rear still: back of helm / surcoat, TOP = away / +Z. |
| `sir_aldric_rear_walk_pose.png` | Walk-cycle rear frame (back to camera, stride toward TOP). |
| `sir_aldric_rear_still_34.png` | Optional ¾ rear. |
| `sir_aldric_walk_toward_top_rear.mp4` | Walk toward TOP, backside to camera. |
| `sir_aldric_walk_toward_top_rear.gif` | Same loop, gif. |
| `sir_aldric_attack_rear_still.png` | Attack start, back to camera. |
| `sir_aldric_attack_slash_rear.png` | Mid slash, back to camera / toward TOP. |
| `sir_aldric_attack_toward_top_rear.mp4` | Design take `target_character\|rigify_clip\|BaseLayer` (Standing Sword Slash Attack). |

Walk stills / walk MP4 are the existing World punch rear proofs (same Play cam). Attack stills + MP4 are Blender-rendered from the Design attack FBX with Play cam remapped Unity→Blender Z-up and **yaw 180** (verified: yaw 0 is frontal FAIL). Unity Game-view is not on this VM; Play actor uses the same 180° yaw.

Soft toe specular leftover OK. `#25` takeName `target_character|…|Walking` and `#26` FBX albedo-before-punch stay.
