# STOP — walk RH glued to painted sword (path B)

Derek Play FAIL (2026-09-25): during **WALK** (before draw/attack), right-hand fingers look stuck to / glued on the sword.

**Path B.** Root cause is the Meshy Animate **Walking clip + single painted mesh**, not a runtime parent mismatch. No fake bind. Path A weight-paint / hang-skin / retopo-bind stays **CANCELLED**. No Design PASS. HOLD merge.

## What runtime already does (not the FAIL)

`SirAldricMeshyAnimateActor` creates a clip-only `ClipSword` cube, parents it to `RightHand`, and **`SetActive(false)` for the whole walk block**. Grip / cube is enabled only after draw (`SirAldricHumanoidAttack.SwordDrawn`, k > 0.35). Derek is not seeing that cube during walk.

Preferred fix 1 (reparent sword to scabbard/hip during walk, to `RightHand` on draw) **cannot apply** — no separate sword object:

| Check | Result |
| --- | --- |
| Separate sword / scabbard object | **None.** Objects = `Icosphere` (junk, hidden), mesh `output_unwrapped`, armature `target_character`. |
| `Scabbard` / `Sword` / `Sheath` bone or ASCII | **None** in `sir_aldric_meshy_animate_walk.fbx` (byte search). |
| Parentable sheath transform | **None.** Painted blade+hilt lives in the one body mesh. |

Preferred fix 2 (Animator open-RH override) **does not apply**: the glue is the **whole gauntlet sitting on the painted hilt**, not a finger-curl curve we can open. Finger set is only `mixamorig:RightHandMiddle4` / `LeftHandMiddle4`. Walk take keys them **static identity** (2 keys, span 0). Opening Middle4 would not lift the gauntlet off the hilt.

## What drives the glue

1. **Painted hip sword is mesh-bound to the leg/hip, not the hand.** Hip-right slab (character-right / −X in the Blender import) dominant weights: `RightUpLeg` 642, `RightForeArm` 126, `RightHand` 94. RightHand cluster itself is compact (PCA 0.27 × 0.16 × 0.12 m, **0 verts >18 cm from the hand bone**) — not a blade welded to the wrist.
2. **Walking take `target_character|…|Walking` (Blender action `…|target_chara`, frames 1–26) drives `mixamorig:RightHand` through that hip hilt.** Wrist quaternion animates (x span 0.28). Hand–hips distance 0.33–0.42 m. Deformed RH verts inside the hip-right slab: **185/185** at f1 / f14 / f26, 89 at f8, 112 at f20.
3. **Fingers are not animated.** Middle4 rest is identity. The gauntlet rest mesh + the walk hand pose **reads as a grip** when the hand lands on the painted pommel.

## Eye evidence (already on this PR)

| File | What |
| --- | --- |
| `sir_aldric_rear_still_back_to_camera.png` | Rest / plant: painted scabbard on character-right hip, **RH clear**. |
| `rest_rh_clear_of_scabbard_crop.png` | Crop of that still. |
| `sir_aldric_rear_walk_pose.png` | Walk rear: **RH gauntlet on the painted hilt**. |
| `walk_rh_on_painted_scabbard_crop.png` | Crop of that walk frame. |

Not Unity Game-view. Same walk FBX Derek Plays. ClipSword is off in both stills.

## Design re-export ask

Re-export `sir_aldric_meshy_animate_walk.fbx` Walking so:

1. **Walk / idle:** RH pendulum **clears** the painted hip scabbard. Gauntlet must not sit on the pommel / blade. Sheathed pose, not a grip.
2. **Prefer a separate sword mesh + Scabbard / hip bone** so runtime can keep it sheathed and reparent to `RightHand` only when draw starts. A single `output_unwrapped` mesh cannot be reparented.
3. Optional: a real finger set with an **open-hand** walk rest. `RightHandMiddle4` alone cannot open the gauntlet.

Do **not** ask Dev for weight-paint, hang-skin, or a retopo-bind. Path A cancelled.

## Unity Play repro (Derek gate)

1. Open the project in Unity 6.3 LTS. Play **SirAldric** (same Play cam: `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30, RearYaw 180).
2. Watch the **first two walk cycles** (`WalkCyclesBeforeAttack = 2`) **before** draw starts.
3. Character-right hip (viewer-right from behind): painted scabbard stays on the hip; RH gauntlet lands on the hilt. Hierarchy: `ClipSword` exists under `RightHand` but is **inactive** until draw.
4. After draw, clip cube may appear in the hand — that is attack-only, not this FAIL.

No Design PASS. HOLD merge until Derek Play-gates the Design re-export.
