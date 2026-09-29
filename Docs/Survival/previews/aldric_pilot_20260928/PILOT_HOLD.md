# HOLD — Sir Aldric PILOT Mixamo separate-portrait (2026-09-28)

**HOLD merge on PR #27 until Derek Play.** Path A / ClipSword / AimChain / Dev weight-paint are not SoT. No Design / Derek PASS. AccuRIG clips-only is **superseded**.

## Wire

| Role | Asset |
| --- | --- |
| Playable Mixamo body | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/pilot/SirAldric_body_holefixed_walk.fbx` |
| Unrigged look mesh (on disk) | `SirAldric_body_holefixed_mid280k.fbx` (no armature — not the Play instance) |
| Walk clip | same walk FBX · Unity takeName **`mixamo.com`** 1–36 (Mixamo Standard Walk) |
| Slash clip | `SirAldric_body_holefixed_slash.fbx` · takeName **`mixamo.com`** **8–40** (Stable Sword Inward Slash) |
| Sword prop | `SirAldric_PILOT_sword.fbx` parented to **`mixamorig:RightHand`** only |
| Cape prop | `SirAldric_PILOT_cape.fbx` on disk (optional soft — not auto-parented; scale/bind leftover) |
| Paint | `pilot/mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture*.png` |

Slash With-Skin mesh is **not instantiated**. `StripEmbeddedClipMeshes` disables Ico / withSkin leftovers. **Generic** CreateFromThisModel. Import **`useFileScale: 0`**. Sword localScale = `1 / Abs(hand.lossyScale)` then normalize world blade to **1.01 m**. Walk→slash is `AnimationMixerPlayable` **0.20 s**. `updateWhenOffscreen`. No Path 1 / Path A / ClipSword / AimChain. **Not a whole-body X-flip.**

**Sword hand:** always `mixamorig:RightHand`. FaceWorldTop yaws 180 and **Abs** scale (never `scale.x = -1`). Derek FAIL `212c6da` whole-body X-flip put the sword on the left hand — discarded. Local Generic Euler extras (`5f5e375`) swung the opposite way on Play-cam — discarded. Prefer RH parent + world-space tip arc.

Light **attack-only** offset after `Evaluate()` (`ApplyAttackWindupLift` in **LateUpdate**): unfold `mixamorig:RightForeArm`, point the RH chain along `AttackSlashReach` **UR → horizontal front +Z (enemy / TOP) → LL**. Sword is a **rigid** `mixamorig:RightHand` child: rest aligns the mesh blade to hand +Y, then a per-frame snap puts it on the **forearm–hand axis**. No world-space `AttackSlashReach` slerp on the sword (Derek FAIL `b2ce8f7` tip trailed the arm). Not AimChain. Not a body X-flip. Blender rematch maps Unity (right, up, front) onto already-rear Mixamo as `(−right, −front, up)`.

### takeName note

Both Mixamo clip FBXs use the FBX AnimationStack **`mixamo.com`**. Blender shows `Armature|mixamo.com|Layer0`. Unity clip names: `Walking` / `Attack`.

### Caveat

The unrigged body FBX has **no skeleton**. Play instantiates the Mixamo-skinned holefixed mesh from the walk export. Mixamo body — not AccuRIG.

**Derek FAIL `bf5a5fa`:** FileScale 0.01 × Mixamo mesh → ~2 cm body; Humanoid sword-only.

**Derek FAIL `9075270`:** giant sword from inherited lossyScale ~88 + hard clip cut. Fix: `useFileScale: 0` + world blade 1.01 m + mixer 0.20 s.

**Derek FAIL `5f5e375`:** local bone Euler extras swung the arc the wrong way on Play-cam.

**Derek FAIL `212c6da`:** whole-body X-flip put the sword on the **left** hand.

**Derek FAIL `b0a9509`:** mid Game-view blade straight **up** (world +Y / HUD TOP). `FrontUp 0.48` plus `FromToRotation(Vector3.forward, desired)` aimed the wrong mesh axis after Unity FBX bake (Design +Z → local +Y). Rematch stills that looked like a forward thrust **are not Unity Game-view** — camera-up ≈ world +Y, so extra +Y made rematch screen-up look like “forward.” Trust Game-view. Fix: `FrontUp 0.06`, `FrontHoldU` plateau, rest-blade `FromToRotation`. Mid is a **horizontal thrust** toward the enemy along the yellow path.

**Derek FAIL `b2ce8f7`:** sword **dragged / trailed** the Mixamo arm. Independent `FromToRotation(restBlade, AttackSlashReach)` + `Slerp` aimed the prop at a slower key path than the hand. Fix: rigid RH parent + snap blade to the live forearm–hand axis every LateUpdate (no sword slerp). Arm extras still provide UR → horizontal front → LL.

## Cam (1080×1920 Scale 1×)

Rear `(0, 1.92, −3.08)` LookAt `(0, 1.12, 0.15)` FOV **42**. Mesh 1×.

## Proofs here

Play-cam rematch: walk f18 / slash start f8 (RH max-reach UR) / mid f20 (horizontal front +Z; rear is **foreshortened** toward the enemy cube, not a sky stick) / finish f32 (max-reach LL). Numeric dumps `sir_aldric_pilot_dump_slash_f8.json` / `f20` / `f32`. **Not Meshy website stills. Not Unity Game-view.** Rematch camera `to_track_quat` can disagree with Unity LookAt on screen-right — Unity Play is the gate.

## Unity Play

1. Play **SirAldric**. Generic + `useFileScale: 0`. Sword parent `mixamorig:RightHand`. Blade ≈ 1.01 m.
2. Walk readable. 0.20 s blend. From Derek’s screen: **RH** only; **tip fully extended** UR → **straight out forward** (toward ENEMY / TOP, horizontal, not sky) → LL the whole slash. Full arc in Scale 1×.
3. HOLD until Derek PASS.
