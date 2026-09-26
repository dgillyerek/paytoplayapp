# HOLD — Sir Aldric SEP Meshy Animate (2026-09-26)

**HOLD merge on PR #27 until Derek Play PASS.** Design re-gates after. No Design PASS. Path A CANCELLED.

## SoT

Design SEP drop wired on the existing `#27` branch:

| File | Clip | Rig |
| --- | --- | --- |
| `SirAldric_SEP_meshy_animate_walk.fbx` | `Walking` · take `target_character\|target_character\|Walking` · 1–26 @ 30 FPS | Mixamo `mixamorig:*` |
| `SirAldric_SEP_meshy_animate_attack.fbx` | `Attack` · take `target_character\|rigify_clip\|BaseLayer` · 3–92 @ 30 FPS | MeshyRig `Hips` / `Spine02` |

Both import as Humanoid (`animationType: 3`). Actor instantiates the **walk** FBX, yaws **180°** (Mixamo face −Z → world +Z / TOP), and plays both clips on that Avatar. Attack is Humanoid-retargeted onto the walk instance (same Humanoid). Empty-hand draw→slash — body-only AccuRIG, no sword/scabbard prop.

## Discarded as SoT (still on disk, not played)

- Old fused Meshy walk `sir_aldric_meshy_animate_walk.fbx`
- Standing Sword Slash `sir_aldric_meshy_animate_attack.fbx`
- Path A binds / CLEAN_SWORD_SPLIT
- ClipSword cube / AimChain / `SirAldricHumanoidAttack` procedural pose (leftover math only)

## Soft leftovers (honest)

- No separate sword / scabbard mesh. Attack is empty-hand.
- Neither SEP FBX embeds an atlas. Actor does **not** bind the old fused-walk atlas onto this 99k mesh. Play-cam proofs are clay.
- Walk Mixamo vs attack MeshyRig bone names differ; Unity Humanoid retarget is the Play path. This VM has no Unity Editor — Derek Play is the gate.
- Native attack FBX Play-cam juice (`sir_aldric_sep_attack_juice_playcam.mp4`) shows a spin that finishes facing the camera / slash down-screen. Design handoff said no turning, forward slash toward enemy ahead. Unity plays that clip on the **rear-yawed walk Avatar** — Derek Play decides if the retarget holds rear/TOP. Do not counter-yaw in code.

## Proofs in this folder

Play-cam remap (Unity Y-up SoT: eye `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30, 1080×1920). **Not Meshy website stills. Not Unity Game-view** (no Editor on this VM).

Unity Game-view capturer now writes painted-look proofs under `../aldric_sep_paint_20260925/` (`Survival → Capture Sir Aldric SEP painted walk` or `-aldric-capture`). This clay folder stays as the `c88ea55` motion PASS archive.

## Unity Play repro

1. Unity 6.3 LTS. Play **SirAldric**. Rear cam as above. Enemy cube = TOP / +Z.
2. Two walk cycles, then SEP Draw Slash Forward toward TOP.
3. Walk: RH clear of hip (no painted sword on this body). Attack: empty-hand slash, not ClipSword.
4. HOLD until Derek Play PASS. Design re-gates after.
