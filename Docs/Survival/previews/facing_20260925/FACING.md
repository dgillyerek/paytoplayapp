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
2. `AnimationMixerPlayable` input 0 = Walking (loop). Input 1 = Attack when Design drops a clip.
3. After `WalkCyclesBeforeAttack` walk cycles, 0.12s crossfade Walking → Attack → back. Same Humanoid, same rear yaw.
4. Attack clip is **never** hand-baked. See `ATTACK_FBX_TODO.md`.

## Proof (PRIMARY = rear / back-to-camera)

| File | What Derek should see |
| --- | --- |
| `sir_aldric_rear_still_back_to_camera.png` | Rear still: back of helm / surcoat, TOP = away / +Z. |
| `sir_aldric_rear_walk_pose.png` | Walk-cycle rear frame (back to camera, stride toward TOP). |
| `sir_aldric_rear_still_34.png` | Optional ¾ rear. |
| `sir_aldric_walk_toward_top_rear.mp4` | Walk toward TOP, backside to camera. |
| `sir_aldric_walk_toward_top_rear.gif` | Same loop, gif. |
| `sir_aldric_attack_toward_top_rear.mp4` | **Waiting on Design attack FBX.** Do not use the old capsule `sir_aldric_walk_attack_toward_top.*` as the Animate attack. |

Stills / walk clip are the existing World punch rear proofs (same Play cam). Unity Game-view on this VM is not available; blender is not on PATH. Facing fix is the 180° actor yaw so Play matches these rear stills instead of the frontal instantiate.

Soft toe specular leftover OK. `#25` takeName `target_character|…|Walking` and `#26` FBX albedo-before-punch stay.
