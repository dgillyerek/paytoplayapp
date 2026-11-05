# MALZARETH cape — attach notes

Asset: `MALZARETH_cape.fbx` (cape mesh + its own armature `MALZARETH_cape_rig`), texture `MALZARETH_cape_1024.png`.
The body FBXs in this folder (rig_rest, idle, walk, attack*) do NOT contain the cape.

## Parent
- Parent bone: `mixamorig:Spine2` (upper spine/shoulders; there are no leg weights).
- `cape_root` was built from Spine2's head, tail and roll, so its bind matrix equals Spine2's (error ~1e-7).
- Local offset (0,0,0), local rotation (0,0,0), scale 1. Spine2 head in rest: [0.0007, 0.021, 1.3877] m.
- In Unity, parent `cape_root` under the body's Spine2 with an identity local transform.
  Or use a ParentConstraint with zero offset.

## Chains (children of cape_root)
{
 "L": [
  "cape_L_01",
  "cape_L_02",
  "cape_L_03"
 ],
 "C": [
  "cape_C_01",
  "cape_C_02",
  "cape_C_03"
 ],
 "R": [
  "cape_R_01",
  "cape_R_02",
  "cape_R_03"
 ]
}
Spring roots: `cape_L_01`, `cape_C_01`, `cape_R_01`.

## Spring-bone settings (VRM SpringBone style; the PREVIEW used these)
- stiffness 1.0, drag 0.4, gravity 0.4 (down), dt 1/30
- joint hitRadius 0.1 m vs leg/foot colliders, 0.0 vs torso colliders
- The preview also collides the L–C and C–R row segments with the capsules, so legs cannot slip between columns.
  If your spring solver has no segment collision, use hitRadius 0.10 on all joints for the legs.

## Capsule colliders (bone head→tail; radius = 75th-percentile body radius)
| bone | radius m | joint hit m |
|---|---|---|
| mixamorig:Hips | 0.279 | 0.0 |
| mixamorig:Spine | 0.173 | 0.0 |
| mixamorig:Spine1 | 0.195 | 0.0 |
| mixamorig:Spine2 | 0.16 | 0.0 |
| mixamorig:LeftUpLeg | 0.158 | 0.1 |
| mixamorig:RightUpLeg | 0.153 | 0.1 |
| mixamorig:LeftLeg | 0.085 | 0.1 |
| mixamorig:RightLeg | 0.086 | 0.1 |

There are no arm colliders: arms may swing behind the back-only cape.

## Shape
- Faces trimmed from the front: 5106. The cape is back-only/open at the front.
- Pushed out from the body with a smooth offset field: target 4.5 cm, min rest clearance 0.032 m, median 0.097 m, 100% of verts ≥3 cm.

## Derek 20-bone skeleton (2026-11-04)
- Rig is now 20 mixamorig bones: no RightHand (the right hand and greatsword ride `mixamorig:RightForeArm.001`), no finger, ToeBase, Toe_End or HeadTop_End bones.
- The Foot capsules have no ToeBase child any more. Use the Foot bone's own head to tail as the capsule.
- Cape parent is still `mixamorig:Spine2` with the same rest head, so the attach is unchanged. Collider radii above were re-measured on the new weights.

## Foot capsules on the 20-bone rig (2026-11-04)
No ToeBase child, so each Foot capsule runs along the Foot bone itself, head to tail. The 4th column is the Foot bone length measured in the blend; `NewCharsPreview.cs` builds a bone-local capsule from it.

| bone | radius m | joint hit m | bone length m |
|---|---|---|---|
| mixamorig:LeftFoot | 0.05 | 0.1 | 0.188 |
| mixamorig:RightFoot | 0.05 | 0.1 | 0.189 |

`NewCharsPreview.cs` maps RightForeArm → RightForeArm.001 when this file names `mixamorig:RightForeArm.001` (Malzareth only; he has no arm capsules today).

## FIX2 cape (2026-11-04)
The cape mesh and spring-root bones are refit closer to the body. The Spine2 parent and attach are unchanged.

- **Rest gap:** minimum 2.4 cm. Upper-cape local-min p5/50/95 is 2.4/2.5/5.2 cm (was 2.18/5.7/7.53). This supersedes the "Shape" numbers above.
- **Spring-root moves (m, world):**

| bone | move |
|---|---|
| cape_L_01 | (-0.024, -0.036, 0) |
| cape_C_01 | (-0.007, -0.050, 0) |
| cape_R_01 | (+0.022, -0.031, 0) |

  The other chain bones follow by a similar amount.
- **Colliders:** the capsule radii in the table above are unchanged. Torso joint hit stays 0.0 and no clamp is used: clamping pushed the cape into the body.
- **Verified on the re-imported FBXs, every frame, full-res body mesh.** Only the gripping right hand's verts (RightForeArm.001 weight >= 0.05) are excluded from sword-to-body.

| clip | frames | min cape-to-body | min sword-to-body | min sword-to-cape | max knee change (RLeg/RUpLeg/LLeg/LUpLeg deg/f) |
|---|---|---|---|---|---|
| idle | 299 | 1.12 cm | 6.35 cm | >15 cm | 0.9/0.4/1.6/0.5 |
| walk | 36 | 1.08 cm | 6.90 cm | 7.9 cm | 19.1/8.4/16.8/6.9 |
| attack_greatsword | 56 | 0.83 cm (f20) | 1.79 cm | >15 cm | 27.1/20.1/18.3/12.8 |
| attack2_overhead_cleave | 69 | 0.97 cm (f24) | 2.14 cm | >15 cm | 6.8/7.6/7.6/10.9 |

- **Remaining shortfall:** attack_greatsword and attack2 each have one collar vertex a little under 1 cm. Pushing those verts further made the spring sim unstable (walk went to -1.96 cm), so this refit is the limit before the drape loosens badly.
- **Unity:** `NewCharsPreview.cs` plays `MALZARETH_attack2_overhead_cleave.fbx` as his 4th clip (button OVERHEAD_CLEAVE). The refit cape replaced `MALZARETH_cape.fbx` in place, so the preview loads it with no code change.

## FIX3 attack2 — two-handed overhead (2026-11-05)
Re-keyed `attack2_overhead_cleave` (1–69) as a two-handed overhead: both hands on the hilt (LH just below RH on the grip), eased wind-up above the head, straight-down centerline chop to a strong low stop, then recover. Same 20-bone Derek skeleton (hash unchanged). Right knee kept calm (≤6.8°/f); feet planted (0 slip / 0 drift).

Verified on re-imported FBX every frame (grip hands excluded: `RightForeArm.001`, `LeftHand`):

| check | min | frame |
|---|---|---|
| sword → body (excl. gripping hands + forearms) | 5.41 cm | f21 Head |
| sword → body (excl. gripping hands only; forearm wrap expected) | grip-chain only | — |
| cape → body | 1.41 cm | f51 |
| sword → cape | >15 cm | all |
| RightLeg max deg/f | 6.78 | — |
| foot contact slip | 0 cm/f | — |

Cape mesh/springs unchanged from FIX2; fresh spring sim on the new clip (`COLCLAMP=0`). Preview code: no new clip name — still `attack2_overhead_cleave`; no .cs changes.
