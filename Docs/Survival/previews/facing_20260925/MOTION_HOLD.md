# Derek FAIL — motion HOLD (2026-09-25)

**HOLD merge on PR #27. Do not claim Design PASS.** Path A cancelled. No bone invent / weight-paint.

Derek: rear stills OK. Walk legs too far apart. Attack stretched/squashed — unusable. Want **draw sword → strike forward**, rear / World TOP.

## Attack squash — PROVEN Humanoid retarget mismatch

Walk FBX and this attack FBX share **bind lengths** (Hips 0.137, LeftUpLeg 0.367, LeftArm 0.243) but **not names or hierarchy**.

| | Walk | Attack (current drop) |
| --- | --- | --- |
| Prefix | `mixamorig:*` (32 bones) | none (28 bones, Rigify-style) |
| Spine chain | Hips → Spine → Spine1 → Spine2 → Neck | Hips → **Spine02 → Spine01 → Spine** → neck |
| Mixamo string in file | yes | **no** |
| Rest foot sep | 0.396 m | 0.396 m |

`SirAldricMeshyAnimateActor` **was** instantiating the walk Mixamo Humanoid and mixer-playing the attack clip onto that avatar. Unity Humanoid maps by role; Spine02 vs Spine1 + missing `mixamorig` is a classic squash.

**Dev wire fix (this tip):** attack plays on **its own FBX instance + its own avatar**. Walk instance stays Mixamo + Walking. Visibility swap after `WalkCyclesBeforeAttack`. No cross-avatar mixer. No invented bones.

When Design re-exports attack on the **walk Mixamo rig** (`mixamorig:*`, draw → strike forward): drop over `sir_aldric_meshy_animate_attack.fbx`. `FbxLooksMixamo` becomes true. Native instance still correct (clip+mesh same file). Then retake FULL rear/TOP proof.

Current take `target_character|rigify_clip|BaseLayer` is **Standing Sword Slash** (sword already up) — not draw-then-strike. Content wait is Design.

## Walk wide stance — PROVEN clip-authored, not import

Measured on the walk FBX **in Blender, no Humanoid, no extra root, no foot IK**:

| | Foot separation |
| --- | --- |
| Rest / bind | 0.396 m |
| Walking take min | 0.231 m @ frame 2 |
| Walking take max | **0.719 m @ frame 8** |
| Walking take median | 0.547 m |

That is the Mixamo `Walking` take. `heightFromFeet` / `addHumanoidExtraRoot` were **not** the source (native clip already wide). Tightening stance here would mean editing the take (not a Dev wire). **Wait Design walk iterate.** Import settings left as `#25` / `#26`.

## Proof status

Rear stills remain valid. Walk MP4 still shows the authored wide stride. Attack MP4 is the old standing-slash native render — **not** a motion PASS. New attack + walk proofs when Design drops.
