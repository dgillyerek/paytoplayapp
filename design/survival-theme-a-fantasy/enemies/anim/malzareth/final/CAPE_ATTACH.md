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
